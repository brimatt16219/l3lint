using System.Globalization;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// Kind <c>maxCount</c>: a collection may hold at most <c>max</c> items. The profile value for
/// <c>profileKey</c> beats the default; with neither, the check is skipped (AMEX-FMT-002 runs only under
/// profiles that forward a limited number of lines).
/// </summary>
internal sealed class MaxCountCheck(string collection, int? max, string? profileKey) : IRuleCheck
{
    public RuleCheck Description { get; } = new(
        "maxCount",
        [collection],
        [new("max", max?.ToString(CultureInfo.InvariantCulture) ?? "none"), new("profileKey", profileKey ?? "none")]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var limit = (profileKey is null ? null : context.Profile.GetLimit(profileKey)) ?? max;
        if (limit is not { } maximum)
        {
            return;
        }

        var count = Count(context.Transaction);
        if (count <= maximum)
        {
            return;
        }

        var arguments = CheckArguments.For(collection, null);
        arguments["count"] = CheckArguments.Number(count);
        arguments["max"] = CheckArguments.Number(maximum);
        sink.Report(new FindingDraft(rule, collection)
        {
            Actual = CheckArguments.Number(count),
            Expected = string.Format(CultureInfo.InvariantCulture, Resources.Strings.ExpectedMaxCount, maximum),
            Arguments = arguments,
        });
    }

    private int Count(Transaction transaction) => collection == "/amex/adviceText"
        ? transaction.Amex.AdviceText?.Count ?? 0
        : transaction.LineItems.Count;
}
