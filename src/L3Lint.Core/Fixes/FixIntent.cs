namespace L3Lint.Core.Fixes;

/// <summary>
/// A fix in canonical terms, as a rule proposes it. The engine translates it into the source format.
/// </summary>
/// <param name="Operations">Patch operations against canonical pointers, with canonical values (major units).</param>
/// <param name="NeedsHuman">
/// <see langword="true"/> when the operations are only a proposal a person must confirm, such as mapping BX
/// to PCE. Proposals are listed, never applied (US-06).
/// </param>
/// <param name="Note">A short explanation of a proposal.</param>
public sealed record FixIntent(IReadOnlyList<PatchOperation> Operations, bool NeedsHuman = false, string? Note = null);
