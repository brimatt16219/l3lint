using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace L3Lint.Adapters.Tests.Support;

/// <summary>
/// Compares two JSON trees by meaning: the same keys and array lengths, equal strings and booleans, and numbers
/// equal as decimals (so 12.5 equals 12.50). Returns the pointer of the first difference.
/// </summary>
internal static class JsonEquivalence
{
    public static string? FirstDifference(JsonNode? expected, JsonNode? actual, string at = "")
    {
        switch (expected)
        {
            case null:
                return actual is null ? null : at + " (expected nothing, got " + actual.ToJsonString() + ")";
            case JsonObject expectedObject:
                if (actual is not JsonObject actualObject)
                {
                    return at + " (expected an object, got " + Describe(actual) + ")";
                }

                foreach (var (key, value) in expectedObject)
                {
                    if (!actualObject.ContainsKey(key))
                    {
                        return at + "/" + key + " (missing)";
                    }

                    if (FirstDifference(value, actualObject[key], at + "/" + key) is { } inner)
                    {
                        return inner;
                    }
                }

                foreach (var (key, value) in actualObject)
                {
                    if (!expectedObject.ContainsKey(key))
                    {
                        return at + "/" + key + " (unexpected " + Describe(value) + ")";
                    }
                }

                return null;
            case JsonArray expectedArray:
                if (actual is not JsonArray actualArray)
                {
                    return at + " (expected an array, got " + Describe(actual) + ")";
                }

                if (expectedArray.Count != actualArray.Count)
                {
                    return at + " (expected " + expectedArray.Count + " items, got " + actualArray.Count + ")";
                }

                for (var i = 0; i < expectedArray.Count; i++)
                {
                    if (FirstDifference(expectedArray[i], actualArray[i], at + "/" + i) is { } inner)
                    {
                        return inner;
                    }
                }

                return null;
            default:
                var expectedValue = (JsonValue)expected;
                if (actual is not JsonValue actualValue || expectedValue.GetValueKind() != actualValue.GetValueKind())
                {
                    return at + " (expected " + expected.ToJsonString() + ", got " + Describe(actual) + ")";
                }

                if (expectedValue.GetValueKind() == JsonValueKind.Number)
                {
                    var left = decimal.Parse(expectedValue.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                    var right = decimal.Parse(actualValue.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                    return left == right ? null : at + " (expected " + left + ", got " + right + ")";
                }

                return expected.ToJsonString() == actual.ToJsonString() ? null : at + " (expected " + expected.ToJsonString() + ", got " + actual.ToJsonString() + ")";
        }
    }

    private static string Describe(JsonNode? node) => node is null ? "nothing" : node.ToJsonString();
}
