using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// The compiled <c>appliesTo</c> gates of a pack rule. Unknown card product or MCC never skips a rule
/// except through <c>mccIn</c>, which needs a known MCC (ADR-0032).
/// </summary>
internal sealed class CompiledApplicability(RuleApplicability source)
{
    private readonly IReadOnlyList<CompiledCondition> _when = [.. source.When.Select(c => new CompiledCondition(c))];
    private readonly IReadOnlyList<CompiledCondition> _unless = [.. source.Unless.Select(c => new CompiledCondition(c))];

    public IEnumerable<string> ListNames =>
        source.MccIn.Concat(source.MccNotIn)
            .Concat(source.When.Concat(source.Unless).Where(c => c.Operator == "inList").Select(c => c.Operands[0]));

    public bool Holds(in RuleContext context)
    {
        var transaction = context.Transaction;
        if (source.CardProducts.Count > 0 && transaction.Card.Product is { } product && product != CardProduct.Unknown
            && !source.CardProducts.Contains(product))
        {
            return false;
        }

        var mcc = transaction.Merchant.Mcc;
        if (source.MccIn.Count > 0 && (mcc is null || !InAny(context, source.MccIn, mcc)))
        {
            return false;
        }

        if (source.MccNotIn.Count > 0 && mcc is not null && InAny(context, source.MccNotIn, mcc))
        {
            return false;
        }

        foreach (var condition in _when)
        {
            if (!condition.Holds(context))
            {
                return false;
            }
        }

        if (_unless.Count > 0)
        {
            var all = true;
            foreach (var condition in _unless)
            {
                if (!condition.Holds(context))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                return false;
            }
        }

        return true;
    }

    private static bool InAny(in RuleContext context, IReadOnlyList<string> lists, string mcc)
    {
        foreach (var list in lists)
        {
            if (context.Lists.Contains(list, mcc))
            {
                return true;
            }
        }

        return false;
    }
}
