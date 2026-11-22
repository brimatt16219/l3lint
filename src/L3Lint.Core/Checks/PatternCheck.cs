using System.Text.RegularExpressions;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// Kind <c>pattern</c>: text must match a regular expression. Expressions come from pack data, so they are
/// compiled once at load with a 100 ms match timeout (NFR-006).
/// </summary>
internal sealed class PatternCheck(IReadOnlyList<FieldSelector> selectors, Regex regex) : IRuleCheck
{
    public RuleCheck Description { get; } = new("pattern", [.. selectors.Select(s => s.Path)], [new("regex", regex.ToString())]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var selector in selectors)
        {
            var count = selector.Count(context);
            for (var i = 0; i < count; i++)
            {
                var value = selector.Read(context, i);
                if (value.Kind != FieldValueKind.Text || value.Text is null || Matches(value.Text))
                {
                    continue;
                }

                var pointer = selector.PointerAt(i);
                sink.Report(new FindingDraft(rule, pointer)
                {
                    Actual = value.Text,
                    Arguments = CheckArguments.For(pointer, selector.DisplayIndex(i)),
                });
            }
        }
    }

    private bool Matches(string text)
    {
        try
        {
            return regex.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            // A value that takes this long is hostile or absurd; treat it as not matching so it is reported.
            return false;
        }
    }
}
