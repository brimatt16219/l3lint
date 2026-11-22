using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace L3Lint.Core.Yaml;

/// <summary>
/// Reads YAML into a JSON tree so one JSON Schema can validate rule packs, config, profiles, CSV maps and
/// rate tables.
/// </summary>
/// <remarks>
/// Scalars follow the YAML 1.2 core schema: plain <c>true</c>, <c>false</c>, <c>null</c>, integers and
/// floats become JSON literals; everything else, including dates and quoted values, stays a string. Type
/// tags are refused, so a YAML file can never ask for an object to be constructed. Aliases are expanded
/// under a node budget to stop expansion attacks, and nesting is limited before YamlDotNet builds its
/// recursive node tree, so a deeply nested file cannot overflow the stack.
/// </remarks>
internal static partial class YamlJson
{
    /// <summary>The deepest nesting of mappings and lists accepted. No l3lint file format needs more than 8.</summary>
    public const int MaxDepth = 32;

    private const int NodeBudget = 200_000;

    /// <summary>Parses one YAML document into a JSON node.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <returns>The JSON tree, or <see langword="null"/> for an empty document.</returns>
    /// <exception cref="YamlFormatException">The text is not valid YAML or uses a refused feature.</exception>
    public static JsonNode? Parse(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        var stream = new YamlStream();
        using var reader = new StringReader(yaml);
        var parser = new DepthLimitedParser(new Parser(reader));
        try
        {
            stream.Load(parser);
        }
        catch (YamlException ex)
        {
            throw new YamlFormatException(ex.Message, (int)ex.Start.Line, (int)ex.Start.Column, ex);
        }
        catch (ArgumentException ex)
        {
            // YamlDotNet reports a few malformed inputs, such as repeated complex keys, with framework exceptions.
            throw new YamlFormatException(ex.Message, parser.Line, parser.Column, ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new YamlFormatException(ex.Message, parser.Line, parser.Column, ex);
        }

        if (stream.Documents.Count == 0)
        {
            return null;
        }

        if (stream.Documents.Count > 1)
        {
            var second = stream.Documents[1].RootNode.Start;
            throw new YamlFormatException(Resources.Strings.YamlSingleDocument, (int)second.Line, (int)second.Column, null);
        }

        var budget = NodeBudget;
        return Convert(stream.Documents[0].RootNode, 0, ref budget);
    }

    private static JsonNode? Convert(YamlNode node, int depth, ref int budget)
    {
        // An alias can point at its own parent, which makes a loop the text-depth limit never sees.
        if (depth > MaxDepth)
        {
            throw new YamlFormatException(
                string.Format(CultureInfo.InvariantCulture, Resources.Strings.YamlTooDeep, MaxDepth),
                (int)node.Start.Line,
                (int)node.Start.Column,
                null);
        }

        if (--budget < 0)
        {
            throw new YamlFormatException(Resources.Strings.YamlTooLarge, (int)node.Start.Line, (int)node.Start.Column, null);
        }

        if (!node.Tag.IsEmpty && !IsStandardTag(node.Tag.Value))
        {
            throw new YamlFormatException(
                string.Format(CultureInfo.InvariantCulture, Resources.Strings.YamlTagRefused, node.Tag.Value),
                (int)node.Start.Line,
                (int)node.Start.Column,
                null);
        }

        switch (node)
        {
            case YamlMappingNode mapping:
                var obj = new JsonObject();
                foreach (var (keyNode, valueNode) in mapping.Children)
                {
                    var key = keyNode is YamlScalarNode scalarKey ? scalarKey.Value ?? string.Empty : keyNode.ToString();
                    if (obj.ContainsKey(key))
                    {
                        throw new YamlFormatException(
                            string.Format(CultureInfo.InvariantCulture, Resources.Strings.YamlDuplicateKey, key),
                            (int)keyNode.Start.Line,
                            (int)keyNode.Start.Column,
                            null);
                    }

                    obj[key] = Convert(valueNode, depth + 1, ref budget);
                }

                return obj;
            case YamlSequenceNode sequence:
                var array = new JsonArray();
                foreach (var child in sequence.Children)
                {
                    array.Add(Convert(child, depth + 1, ref budget));
                }

                return array;
            case YamlScalarNode scalar:
                return ConvertScalar(scalar);
            default:
                return null;
        }
    }

    private static bool IsStandardTag(string tag) =>
        tag is "!" or "tag:yaml.org,2002:str" or "tag:yaml.org,2002:int" or "tag:yaml.org,2002:float"
            or "tag:yaml.org,2002:bool" or "tag:yaml.org,2002:null" or "tag:yaml.org,2002:map" or "tag:yaml.org,2002:seq";

    private static JsonValue? ConvertScalar(YamlScalarNode scalar)
    {
        var text = scalar.Value ?? string.Empty;
        var stringTag = !scalar.Tag.IsEmpty && scalar.Tag.Value is "!" or "tag:yaml.org,2002:str";
        if (scalar.Style != ScalarStyle.Plain || stringTag)
        {
            return JsonValue.Create(text);
        }

        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL":
                return null;
            case "true" or "True" or "TRUE":
                return JsonValue.Create(true);
            case "false" or "False" or "FALSE":
                return JsonValue.Create(false);
        }

        if (IntegerPattern().IsMatch(text) && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (FloatPattern().IsMatch(text) && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(text);
    }

    /// <summary>Stops reading as soon as mappings and lists nest deeper than <see cref="MaxDepth"/>.</summary>
    private sealed class DepthLimitedParser(IParser inner) : IParser
    {
        private int _depth;

        public ParsingEvent? Current => inner.Current;

        public int Line => (int)(inner.Current?.Start.Line ?? 0);

        public int Column => (int)(inner.Current?.Start.Column ?? 0);

        public bool MoveNext()
        {
            if (!inner.MoveNext())
            {
                return false;
            }

            if (inner.Current is MappingEnd or SequenceEnd)
            {
                _depth--;
            }
            else if (inner.Current is MappingStart or SequenceStart)
            {
                _depth++;
                if (_depth > MaxDepth)
                {
                    throw new YamlFormatException(
                        string.Format(CultureInfo.InvariantCulture, Resources.Strings.YamlTooDeep, MaxDepth),
                        Line,
                        Column,
                        null);
                }
            }

            return true;
        }
    }

    [GeneratedRegex("^[-+]?[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex FloatPattern();
}
