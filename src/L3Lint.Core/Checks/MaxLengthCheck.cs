using System.Text;
using L3Lint.Core.Fields;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>One maxLength limit: a field, a default maximum, a profile key and optional brand scope.</summary>
internal sealed record LengthLimit(FieldSelector Selector, int? Max, string? ProfileKey, IReadOnlyList<CardBrand> Brands);

/// <summary>
/// Kind <c>maxLength</c>: text longer than the brand or processor maximum. The profile value for
/// <c>profileKey</c> beats the default; with neither, the limit is skipped. Lengths count Unicode characters.
/// </summary>
internal sealed class MaxLengthCheck(IReadOnlyList<LengthLimit> limits) : IRuleCheck
{
    public RuleCheck Description { get; } = new(
        "maxLength",
        [.. limits.Select(l => l.Selector.Path)],
        [.. limits.Select(l => new KeyValuePair<string, string>(l.Selector.Path, Describe(l)))]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var limit in limits)
        {
            if (limit.Brands.Count > 0 && !limit.Brands.Any(context.EvaluatedBrands.Contains))
            {
                continue;
            }

            var max = (limit.ProfileKey is null ? null : context.Profile.GetLimit(limit.ProfileKey)) ?? limit.Max;
            if (max is not { } maximum)
            {
                continue;
            }

            var count = limit.Selector.Count(context);
            for (var i = 0; i < count; i++)
            {
                var value = limit.Selector.Read(context, i);
                if (value.Kind != FieldValueKind.Text || value.Text is null)
                {
                    continue;
                }

                var length = Length(value.Text);
                if (length <= maximum)
                {
                    continue;
                }

                var pointer = limit.Selector.PointerAt(i);
                var arguments = CheckArguments.For(pointer, limit.Selector.DisplayIndex(i));
                arguments["max"] = CheckArguments.Number(maximum);
                arguments["length"] = CheckArguments.Number(length);
                sink.Report(new FindingDraft(rule, pointer)
                {
                    Actual = value.Text,
                    Expected = string.Format(System.Globalization.CultureInfo.InvariantCulture, Resources.Strings.ExpectedMaxLength, maximum),
                    Arguments = arguments,
                    Fix = MechanicalFixes.Trim(pointer, value.Text, maximum),
                });
            }
        }
    }

    /// <summary>Counts Unicode scalar values, so an accented letter counts once.</summary>
    public static int Length(string text)
    {
        var length = 0;
        foreach (var _ in text.EnumerateRunes())
        {
            length++;
        }

        return length;
    }

    private static string Describe(LengthLimit limit)
    {
        var builder = new StringBuilder();
        if (limit.Max is { } max)
        {
            builder.Append(max.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (limit.ProfileKey is not null)
        {
            builder.Append(builder.Length > 0 ? " (profile key " : "(profile key ").Append(limit.ProfileKey).Append(')');
        }

        if (limit.Brands.Count > 0)
        {
            builder.Append(" for ").Append(string.Join(", ", limit.Brands.Select(CanonicalNames.ToName)));
        }

        return builder.ToString();
    }
}
