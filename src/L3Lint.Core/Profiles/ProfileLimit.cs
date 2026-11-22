namespace L3Lint.Core.Profiles;

/// <summary>
/// One profile limit with its source.
/// </summary>
/// <param name="Value">The limit, for example 200 line items.</param>
/// <param name="Source">The public page that states it.</param>
/// <param name="Note">Context, for example "first 4 lines forwarded to Amex".</param>
public sealed record ProfileLimit(int Value, Uri Source, string? Note = null);
