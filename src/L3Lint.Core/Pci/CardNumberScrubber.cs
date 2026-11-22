using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core.Adapters;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Pci;

/// <summary>
/// Pipeline stage 3: finds card numbers in every string and number token of a JSON document and masks them
/// in a copy before mapping (FR-033, NFR-007, ADR-0004).
/// </summary>
/// <remarks>
/// Clean documents (the common case) are returned unchanged with no copy. A document with matches is
/// re-parsed from a masked tree, so every later stage, log line and output sees only masked values.
/// </remarks>
internal static class CardNumberScrubber
{
    public static ScrubResult ScrubJson(RawDocument document, IPayloadAdapter adapter, IReadOnlyList<PointerPattern> allowFields)
    {
        var found = new List<(string Pointer, string Digits, CardBrand Brand)>();
        var segments = new List<string>();
        Visit(document.Json, segments, found);
        if (found.Count == 0)
        {
            return new ScrubResult(document, []);
        }

        var matches = new List<CardNumberMatch>(found.Count);
        var toMask = new List<(string Pointer, string Masked)>(found.Count);
        foreach (var (pointer, digits, brand) in found)
        {
            var canonical = adapter.ToCanonicalPointer(pointer);
            if (canonical is not null && PointerPattern.AnyMatch(allowFields, canonical))
            {
                continue;
            }

            var masked = CardNumberDetector.Mask(digits);
            var isCode = canonical is not null && CanonicalFields.FindByPointer(canonical) is { IsCode: true };
            matches.Add(new CardNumberMatch(pointer, canonical, masked, brand, isCode));
            toMask.Add((pointer, masked));
        }

        if (toMask.Count == 0)
        {
            return new ScrubResult(document, []);
        }

        var root = JsonNode.Parse(document.Json.GetRawText())!;
        foreach (var (pointer, masked) in toMask)
        {
            Replace(root, pointer, masked);
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(root, PciJsonContext.Default.JsonNode);
        using var maskedDocument = JsonDocument.Parse(bytes);
        return new ScrubResult(document.WithMaskedJson(maskedDocument.RootElement.Clone()), matches);
    }

    /// <summary>Masks one CSV cell or other loose value; used by text-based adapters.</summary>
    public static bool TryMask(string value, out string masked, out CardBrand brand)
    {
        if (CardNumberDetector.TryDetect(value, out var digits, out brand))
        {
            masked = CardNumberDetector.Mask(digits);
            return true;
        }

        masked = value;
        return false;
    }

    private static void Visit(JsonElement element, List<string> segments, List<(string, string, CardBrand)> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    segments.Add(property.Name);
                    Visit(property.Value, segments, found);
                    segments.RemoveAt(segments.Count - 1);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    segments.Add(index++.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Visit(item, segments, found);
                    segments.RemoveAt(segments.Count - 1);
                }

                break;
            case JsonValueKind.String:
                Check(element.GetString().AsSpan(), segments, found);
                break;
            case JsonValueKind.Number:
                Check(element.GetRawText().AsSpan(), segments, found);
                break;
        }
    }

    private static void Check(ReadOnlySpan<char> value, List<string> segments, List<(string, string, CardBrand)> found)
    {
        if (CardNumberDetector.TryDetect(value, out var digits, out var brand))
        {
            found.Add((JsonPointerText.Join(segments), digits, brand));
        }
    }

    private static void Replace(JsonNode root, string pointer, string masked)
    {
        var parts = JsonPointerText.Split(pointer);
        var parent = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            parent = parent is JsonArray array ? array[int.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture)]! : parent[parts[i]]!;
        }

        var last = parts[^1];
        if (parent is JsonArray list)
        {
            list[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = JsonValue.Create(masked);
        }
        else
        {
            parent[last] = JsonValue.Create(masked);
        }
    }
}
