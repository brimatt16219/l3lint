namespace L3Lint.Reporting;

/// <summary>
/// One input of a run.
/// </summary>
/// <param name="Path">The path as given (relative paths stay relative), or "-" for stdin.</param>
/// <param name="Format">The format it was read as, for example "stripe".</param>
/// <param name="Sha256">The lowercase hex SHA-256 of the file's bytes.</param>
/// <param name="Transactions">How many transactions it held.</param>
public sealed record ReportInput(string Path, string Format, string Sha256, int Transactions);
