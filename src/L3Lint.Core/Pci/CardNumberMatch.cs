using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Pci;

/// <summary>
/// A value that looked like a card number and was masked before any other stage saw it.
/// </summary>
/// <param name="SourcePointer">Where the value was in the source format.</param>
/// <param name="CanonicalPointer">The canonical field it maps to, or <see langword="null"/> when it maps to none.</param>
/// <param name="MaskedValue">Asterisks plus the last four digits, for example ************1111.</param>
/// <param name="Brand">The brand whose prefix and length the value matched.</param>
/// <param name="IsCodeField">Whether the value sat in a code field (product code, commodity code, sequence), where a match is only a warning.</param>
/// <param name="Location">A known file position (CSV); <see langword="null"/> for JSON, where the token index supplies it.</param>
public sealed record CardNumberMatch(
    string SourcePointer,
    string? CanonicalPointer,
    string MaskedValue,
    CardBrand Brand,
    bool IsCodeField,
    SourceLocation? Location = null);
