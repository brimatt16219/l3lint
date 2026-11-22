using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Detectors;

/// <summary>Line-by-line scans shared by the general data quality rules and their stricter Visa counterparts.</summary>
internal static class LineScans
{
    /// <summary>Lines whose description is a placeholder (GEN-DQ-001, VISA-DQ-001).</summary>
    public static IEnumerable<int> PlaceholderDescriptions(RuleContext context)
    {
        var placeholders = context.State.Run.Placeholders;
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Description is { } description && !string.IsNullOrWhiteSpace(description)
                && DataQuality.IsPlaceholderDescription(description, placeholders))
            {
                yield return i;
            }
        }
    }

    /// <summary>Lines whose description is the merchant name (GEN-DQ-002, VISA-DQ-001).</summary>
    public static IEnumerable<int> MerchantNameDescriptions(RuleContext context)
    {
        var facts = context.Facts;
        foreach (var line in facts.Lines)
        {
            if (DataQuality.IsMerchantName(line.NormalizedDescription, facts.NormalizedMerchantName))
            {
                yield return line.Index;
            }
        }
    }

    /// <summary>Placeholder product codes and units, plus commodity codes when asked (GEN-DQ-003, VISA-DQ-002).</summary>
    public static IEnumerable<(int Index, string Field, string Value)> PlaceholderCodes(RuleContext context, bool includeCommodity)
    {
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.ProductCode is { } product && DataQuality.IsPlaceholderCode(product, line.Sequence ?? i + 1))
            {
                yield return (i, "productCode", product);
            }

            if (includeCommodity && line.CommodityCode is { } commodity && DataQuality.IsPlaceholderCode(commodity, line.Sequence ?? i + 1))
            {
                yield return (i, "commodityCode", commodity);
            }

            if (line.UnitOfMeasure is { } unit && DataQuality.IsPlaceholderCode(unit, line.Sequence ?? i + 1))
            {
                yield return (i, "unitOfMeasure", unit);
            }
        }
    }

    /// <summary>The canonical pointer of a line field.</summary>
    public static string Pointer(int index, string field) => Pointers.Line(index, field);
}
