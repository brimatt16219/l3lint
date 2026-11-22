using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Pci;

namespace L3Lint.Core;

/// <summary>
/// The canonical form of a document, without linting: pipeline stages 2 to 4 (detect, scrub, map).
/// </summary>
/// <param name="Format">The adapter that read the document.</param>
/// <param name="Transactions">One mapping result per transaction, with path maps, derived fields and notes.</param>
/// <param name="CardNumbers">Card numbers masked before mapping.</param>
/// <param name="Document">The scrubbed source document (masked JSON), which fixes are applied to.</param>
public sealed record MapResult(string Format, IReadOnlyList<AdapterResult> Transactions, IReadOnlyList<CardNumberMatch> CardNumbers, RawDocument Document);
