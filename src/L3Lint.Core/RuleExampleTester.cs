using System.Globalization;
using L3Lint.Core.Estimates;
using L3Lint.Core.Input;
using L3Lint.Core.Rules;

namespace L3Lint.Core;

/// <summary>
/// Runs each rule's embedded pass and fail examples (FR-020, <c>l3lint rules test</c>).
/// </summary>
/// <remarks>
/// Each rule runs alone (<see cref="LintOptions.OnlyRules"/>) with the context its examples declare, so one
/// rule's examples never depend on another rule. The passing example must not raise the rule; the failing
/// example must. Batch rules lint the failing example <see cref="BatchOptions.MinTransactions"/> times in one
/// run and the passing example once. Expectations come from the pack, never from earlier runs.
/// </remarks>
public static class RuleExampleTester
{
    /// <summary>
    /// Tests every rule that ships examples.
    /// </summary>
    /// <param name="linter">The linter whose rules to test.</param>
    /// <param name="rules">The rules; rules without examples are reported as failures.</param>
    /// <returns>One outcome per rule, in the order given.</returns>
    public static IReadOnlyList<RuleExampleOutcome> Run(ILinter linter, IEnumerable<RuleMetadata> rules)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentNullException.ThrowIfNull(rules);
        return [.. rules.Select(rule => Run(linter, rule))];
    }

    /// <summary>
    /// Tests one rule.
    /// </summary>
    /// <param name="linter">The linter.</param>
    /// <param name="rule">The rule.</param>
    /// <returns>The outcome.</returns>
    public static RuleExampleOutcome Run(ILinter linter, RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.Examples is not { } examples)
        {
            return new RuleExampleOutcome(rule.Id, false, false, Resources.Strings.ExamplesMissing);
        }

        var context = examples.Context;
        var options = new LintOptions
        {
            OnlyRules = new HashSet<string>(StringComparer.Ordinal) { rule.Id },
            AsOf = context.AsOf,
            Processor = context.Processor,
            StrictProfiles = context.StrictProfiles,
            Rates = context.RatesYaml is { } yaml ? RateTable.Parse(yaml, rule.Id + " example rates") : null,
            Format = "canonical",
        };

        try
        {
            var passFired = Fires(linter, rule, examples.PassJson, examples.PassFile, options, batchCopies: 1);
            var failFired = Fires(linter, rule, examples.FailJson, examples.FailFile, options, batchCopies: rule.IsBatch ? options.Batch.MinTransactions : 1);
            var detail = (passFired, failFired) switch
            {
                (false, true) => null,
                (true, true) => Resources.Strings.ExamplesPassFired,
                (false, false) => Resources.Strings.ExamplesFailSilent,
                _ => Resources.Strings.ExamplesBothWrong,
            };
            return new RuleExampleOutcome(rule.Id, !passFired, failFired, detail);
        }
        catch (InputException ex)
        {
            return new RuleExampleOutcome(rule.Id, false, false, string.Format(CultureInfo.InvariantCulture, Resources.Strings.ExamplesUnreadable, ex.Message));
        }
    }

    private static bool Fires(ILinter linter, RuleMetadata rule, string json, string name, LintOptions options, int batchCopies)
    {
        var document = RawDocument.ParseJson(System.Text.Encoding.UTF8.GetBytes(json), name);
        if (!rule.IsBatch)
        {
            return linter.Lint(document, options).Findings.Any(f => f.RuleId == rule.Id);
        }

        var session = linter.CreateSession(options);
        for (var i = 0; i < batchCopies; i++)
        {
            session.Lint(document, i, i);
        }

        return session.Complete(batchCopies)?.Findings.Any(f => f.RuleId == rule.Id) ?? false;
    }
}
