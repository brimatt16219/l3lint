using System.Globalization;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// A resolved field selector: a canonical pointer with at most one <c>*</c> segment (ADR-0012).
/// </summary>
/// <remarks>Values are read by index without allocating; pointers are built only when a finding is reported.</remarks>
internal sealed class FieldSelector
{
    private readonly SelectorKind _kind;

    private FieldSelector(string path, CanonicalField field, SelectorKind kind)
    {
        Path = path;
        Field = field;
        _kind = kind;
    }

    private enum SelectorKind
    {
        Header,
        Line,
        LineCollection,
        AdviceLine,
    }

    public string Path { get; }

    public CanonicalField Field { get; }

    public bool IsWildcard => _kind is SelectorKind.Line or SelectorKind.AdviceLine;

    public static FieldSelector Parse(string path)
    {
        if (!CanonicalFields.TryGet(path, out var field))
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackUnknownField, path));
        }

        var kind = path == "/lineItems" ? SelectorKind.LineCollection
            : path == "/amex/adviceText/*" ? SelectorKind.AdviceLine
            : field.IsLineField ? SelectorKind.Line
            : SelectorKind.Header;
        return new FieldSelector(path, field, kind);
    }

    public int Count(in RuleContext context) => _kind switch
    {
        SelectorKind.Line => context.Transaction.LineItems.Count,
        SelectorKind.AdviceLine => context.Transaction.Amex.AdviceText?.Count ?? 0,
        _ => 1,
    };

    public FieldValue Read(in RuleContext context, int index)
    {
        var state = context.State;
        if (state.HasInvalidFields && state.IsInvalid(PointerAt(index)))
        {
            return FieldValue.Invalid;
        }

        return _kind switch
        {
            SelectorKind.Line => Field.ReadLine!(context.Transaction.LineItems[index]),
            SelectorKind.AdviceLine => FieldValue.Of(context.Transaction.Amex.AdviceText![index]),
            SelectorKind.LineCollection => FieldValue.OfList(context.Transaction.LineItems.Count),
            _ => Field.ReadHeader!(context.Transaction),
        };
    }

    public string PointerAt(int index) => _kind switch
    {
        SelectorKind.Line => "/lineItems/" + index.ToString(CultureInfo.InvariantCulture) + "/" + Field.Name,
        SelectorKind.AdviceLine => "/amex/adviceText/" + index.ToString(CultureInfo.InvariantCulture),
        _ => Path,
    };

    /// <summary>Returns the 1-based line or advice-line number for messages, or null for header fields.</summary>
    public int? DisplayIndex(int index) => IsWildcard ? index + 1 : null;
}
