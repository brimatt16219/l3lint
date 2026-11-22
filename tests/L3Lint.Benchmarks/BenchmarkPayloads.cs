using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Hosting;

namespace L3Lint.Benchmarks;

/// <summary>Payloads and a linter shared by the benchmarks, built from the spec's worked example.</summary>
public static class BenchmarkPayloads
{
    private static readonly Lazy<string> Spec = new(() => Resource("canonical-example.json"));

    /// <summary>A linter with the five shipped packs and the six gateway adapters, as the CLI builds it.</summary>
    public static ILinter CreateLinter() =>
        L3LintFactory.CreateBuilder().UseTimeProvider(new FixedClock(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero))).Build();

    /// <summary>The spec example as written: two lines, one Visa warning.</summary>
    public static string SpecExample => Spec.Value;

    /// <summary>A Stripe PaymentIntent body from the adapter fixtures (two lines, minor units).</summary>
    public static string Stripe => Resource("stripe.json");

    /// <summary>
    /// The spec example with <paramref name="lines"/> copies of line 1 (10 x 12.50 less 5.00 = 120.00, tax 8.40), and
    /// the header tax and amount recomputed with the spec's formulas so the arithmetic rules pass.
    /// </summary>
    public static string WithLines(int lines, string id = "INV-20417")
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        var template = root["lineItems"]![0]!.ToJsonString();
        var items = new JsonArray();
        for (var i = 1; i <= lines; i++)
        {
            var line = JsonNode.Parse(template)!.AsObject();
            line["sequence"] = i;
            items.Add(line);
        }

        root["id"] = id;
        root["lineItems"] = items;
        root["tax"]!["amount"] = 8.40m * lines;
        root["amount"] = (120.00m * lines) + (8.40m * lines) + 15.00m;
        return root.ToJsonString();
    }

    /// <summary>One compact record per line, each with its own invoice ID.</summary>
    public static string WriteNdjson(string path, int records)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(false));
        var template = JsonNode.Parse(SpecExample)!.ToJsonString();
        for (var i = 0; i < records; i++)
        {
            writer.Write(template.Replace("INV-20417", "INV-" + i.ToString("D7", CultureInfo.InvariantCulture), StringComparison.Ordinal));
            writer.Write('\n');
        }

        return path;
    }

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("L3Lint.Benchmarks." + name)
            ?? throw new InvalidOperationException("Missing embedded resource " + name);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
