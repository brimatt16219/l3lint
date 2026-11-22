using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;

namespace L3Lint.Core.Schemas;

/// <summary>
/// Validates JSON against one of the embedded schemas with JsonSchema.Net.
/// </summary>
/// <remarks>
/// Each schema is built once into a private registry with remote fetching off, so validation never touches
/// the network (D3). Instances are immutable and safe to use from any thread.
/// </remarks>
internal sealed class SchemaValidator
{
    // Lazy, so threads that ask for the same schema at once (packs load in parallel) build it only once.
    private static readonly ConcurrentDictionary<string, Lazy<SchemaValidator>> Cache = new(StringComparer.Ordinal);

    // A pass/fail evaluation stops at the first failure and builds no output tree, so valid payloads (the common
    // case) cost a fraction of a detailed evaluation (US-09). The detailed list runs only when something failed.
    private static readonly EvaluationOptions Quick = new()
    {
        OutputFormat = OutputFormat.Flag,
        RequireFormatValidation = false,
    };

    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = false,
    };

    private readonly JsonSchema _schema;

    private SchemaValidator(JsonSchema schema) => _schema = schema;

    /// <summary>Gets the validator for an embedded schema.</summary>
    /// <param name="name">One of <see cref="SchemaCatalog.Names"/>.</param>
    public static SchemaValidator For(string name) => Cache.GetOrAdd(name, static n => new Lazy<SchemaValidator>(() =>
    {
        var build = new BuildOptions { SchemaRegistry = new SchemaRegistry() };
        return new SchemaValidator(JsonSchema.FromText(SchemaCatalog.GetText(n), build));
    })).Value;

    /// <summary>Validates an instance and returns every violation, deepest location first per branch, deduplicated by pointer and keyword.</summary>
    public IReadOnlyList<SchemaError> Validate(JsonElement instance)
    {
        if (_schema.Evaluate(instance, Quick).IsValid)
        {
            return [];
        }

        var results = _schema.Evaluate(instance, Options);

        var errors = new List<SchemaError>();
        var seen = new HashSet<(string, string)>();
        foreach (var detail in results.Details ?? [])
        {
            if (detail.IsValid || detail.Errors is null)
            {
                continue;
            }

            var pointer = detail.InstanceLocation.ToString();
            foreach (var (keyword, message) in detail.Errors)
            {
                // Composite keywords only repeat their children's errors.
                if (keyword is "allOf" or "anyOf" or "oneOf" or "$ref" or "if" or "then" or "properties" or "items" or "additionalProperties")
                {
                    continue;
                }

                if (seen.Add((pointer, keyword)))
                {
                    errors.Add(new SchemaError(pointer, keyword, message ?? string.Empty));
                }
            }
        }

        if (errors.Count == 0)
        {
            errors.Add(new SchemaError(string.Empty, "schema", string.Empty));
        }

        return errors;
    }
}
