using System.Globalization;
using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using L3Lint.Core.Fixes;

namespace L3Lint.Core.Tests.Properties;

/// <summary>
/// RFC 6902 behavior of <see cref="JsonPatch"/>, which <c>fix --write</c> and the MCP <c>suggest_fixes</c> tool rely on
/// (US-06). Pointers are escaped by hand from RFC 6901 ("~" becomes "~0", then "/" becomes "~1"), so the test does not
/// reuse the code under test to build its input.
/// </summary>
public sealed class JsonPatchProperties
{
    [Property(MaxTest = 1000)]
    public Property Add_then_remove_leaves_the_document_unchanged() =>
        Prop.ForAll(PatchCases().ToArbitrary(), input =>
        {
            var document = JsonNode.Parse(input.Document)!;
            var before = document.DeepClone();
            var (pointer, removePointer) = Target(document, input);
            var value = JsonNode.Parse(input.Value);

            var added = JsonPatch.Apply(document, [new PatchOperation("add", pointer, value)]);
            var restored = JsonPatch.Apply(added, [new PatchOperation("remove", removePointer, null)]);

            JsonNode.DeepEquals(restored, before).ShouldBeTrue($"{pointer} on {input.Document}");
            JsonNode.DeepEquals(document, before).ShouldBeTrue("Apply must not change its input document.");
        });

    [Property(MaxTest = 1000)]
    public Property Add_puts_the_value_at_the_pointer() =>
        Prop.ForAll(PatchCases().ToArbitrary(), input =>
        {
            var document = JsonNode.Parse(input.Document)!;
            var (pointer, resolvedPointer) = Target(document, input);
            var value = JsonNode.Parse(input.Value);

            var added = JsonPatch.Apply(document, [new PatchOperation("add", pointer, value)]);

            JsonNode.DeepEquals(Resolve(added, resolvedPointer), value).ShouldBeTrue($"{pointer} on {input.Document}");
        });

    [Property(MaxTest = 1000)]
    public Property Replace_then_replace_back_restores_the_value() =>
        Prop.ForAll(PatchCases().ToArbitrary(), input =>
        {
            var document = JsonNode.Parse(input.Document)!;
            var before = document.DeepClone();
            var existing = Leaves(document, string.Empty).ToList();
            if (existing.Count == 0)
            {
                return;
            }

            var (pointer, original) = existing[input.Pick % existing.Count];
            var replaced = JsonPatch.Apply(document, [new PatchOperation("replace", pointer, JsonNode.Parse(input.Value))]);
            var restored = JsonPatch.Apply(replaced, [new PatchOperation("replace", pointer, original?.DeepClone())]);

            JsonNode.DeepEquals(restored, before).ShouldBeTrue($"{pointer} on {input.Document}");
        });

    /// <summary>One generated case: a document, a value, and numbers that pick the target container and position.</summary>
    public sealed record PatchCase(string Document, string Value, string Key, int Pick, int Position, bool UseDash);

    /// <summary>Picks a container in the document and returns the add pointer and the pointer of the added value.</summary>
    private static (string Add, string Added) Target(JsonNode document, PatchCase input)
    {
        var containers = Containers(document, string.Empty).ToList();
        var (at, container) = containers[input.Pick % containers.Count];
        if (container is JsonObject obj)
        {
            // A key the object does not have yet, so add inserts rather than replaces.
            var key = input.Key;
            while (obj.ContainsKey(key))
            {
                key += "~";
            }

            var pointer = at + "/" + Escape(key);
            return (pointer, pointer);
        }

        var array = (JsonArray)container;
        if (input.UseDash)
        {
            return (at + "/-", at + "/" + array.Count.ToString(CultureInfo.InvariantCulture));
        }

        var index = (input.Position % (array.Count + 1)).ToString(CultureInfo.InvariantCulture);
        return (at + "/" + index, at + "/" + index);
    }

    private static IEnumerable<(string Pointer, JsonNode Container)> Containers(JsonNode node, string at)
    {
        switch (node)
        {
            case JsonObject obj:
                yield return (at, obj);
                foreach (var (key, child) in obj)
                {
                    if (child is not null)
                    {
                        foreach (var inner in Containers(child, at + "/" + Escape(key)))
                        {
                            yield return inner;
                        }
                    }
                }

                break;
            case JsonArray array:
                yield return (at, array);
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is { } child)
                    {
                        foreach (var inner in Containers(child, at + "/" + i.ToString(CultureInfo.InvariantCulture)))
                        {
                            yield return inner;
                        }
                    }
                }

                break;
        }
    }

    private static IEnumerable<(string Pointer, JsonNode? Value)> Leaves(JsonNode node, string at)
    {
        IEnumerable<(string, JsonNode?)> children = node switch
        {
            JsonObject obj => obj.Select(p => (at + "/" + Escape(p.Key), p.Value)),
            JsonArray array => array.Select((v, i) => (at + "/" + i.ToString(CultureInfo.InvariantCulture), v)),
            _ => [],
        };
        foreach (var (pointer, value) in children)
        {
            yield return (pointer, value);
            if (value is not null)
            {
                foreach (var inner in Leaves(value, pointer))
                {
                    yield return inner;
                }
            }
        }
    }

    private static JsonNode? Resolve(JsonNode root, string pointer)
    {
        var node = root;
        foreach (var raw in pointer.Split('/').Skip(1))
        {
            var segment = raw.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            node = node switch
            {
                JsonObject obj => obj[segment],
                JsonArray array => array[int.Parse(segment, CultureInfo.InvariantCulture)],
                _ => throw new InvalidOperationException("No container at " + pointer),
            };
            if (node is null)
            {
                return null;
            }
        }

        return node;
    }

    private static string Escape(string key) => key.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static Gen<PatchCase> PatchCases() =>
        from document in Objects(3)
        from value in Values(2)
        from key in Keys()
        from pick in Gen.Choose(0, 1000)
        from position in Gen.Choose(0, 1000)
        from useDash in Gen.Elements(true, false)
        select new PatchCase(document.ToJsonString(), value?.ToJsonString() ?? "null", key, pick, position, useDash);

    private static Gen<string> Keys() => Gen.OneOf(
        Gen.Elements("total", "lineItems", "", "a/b", "~", "~1", "~0x", "a~/b", "unit cost", "" + (char)0xE9 + "t" + (char)0xE9, "0", "-"),
        from length in Gen.Choose(1, 6)
        from chars in Gen.ListOf(Gen.Elements("ab~/z0 ".ToCharArray()), length)
        select new string([.. chars]));

    private static Gen<JsonNode?> Values(int depth) => depth == 0
        ? Scalars()
        : Gen.Frequency(
            (4, Scalars()),
            (1, Objects(depth - 1).Select(o => (JsonNode?)o)),
            (1, Arrays(depth - 1).Select(a => (JsonNode?)a)));

    private static Gen<JsonNode?> Scalars() => Gen.OneOf(
        Gen.Constant<JsonNode?>(null),
        Gen.Elements(true, false).Select(b => (JsonNode?)JsonValue.Create(b)),
        Gen.Choose(-100_000, 100_000).Select(i => (JsonNode?)JsonValue.Create(i / 100m)),
        Keys().Select(s => (JsonNode?)JsonValue.Create(s)));

    private static Gen<JsonObject> Objects(int depth) =>
        from count in Gen.Choose(0, 4)
        from keys in Gen.ListOf(Keys(), count)
        from values in Gen.ListOf(Values(depth), count)
        select Build(keys, values);

    private static Gen<JsonArray> Arrays(int depth) =>
        from count in Gen.Choose(0, 4)
        from values in Gen.ListOf(Values(depth), count)
        select new JsonArray([.. values]);

    private static JsonObject Build(IEnumerable<string> keys, IEnumerable<JsonNode?> values)
    {
        var obj = new JsonObject();
        foreach (var (key, value) in keys.Zip(values))
        {
            obj[key] = value;
        }

        return obj;
    }
}
