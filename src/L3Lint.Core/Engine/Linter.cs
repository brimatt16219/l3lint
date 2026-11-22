using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>Default <see cref="ILinter"/>. Built by <see cref="LinterBuilder"/>.</summary>
internal sealed class Linter : ILinter
{
    private readonly LintEngine _engine;

    public Linter(LintEngine engine) => _engine = engine;

    public string EngineVersion => EngineInfo.Version;

    public IReadOnlyList<RulePackInfo> RulePacks => _engine.Packs;

    public IAdapterRegistry Adapters => _engine.Adapters;

    public LintResult Lint(Transaction transaction, LintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var run = _engine.CreateRun(options ?? _engine.Defaults);
        var result = _engine.LintTransaction(transaction, run, 0, batch: null);
        var sha = Convert.ToHexStringLower(SHA256.HashData(CanonicalJson.SerializeToUtf8Bytes(transaction)));
        return _engine.Assemble([result], run, sha, inputName: null, "canonical", [], stopped: false);
    }

    public LintResult Lint(RawDocument document, LintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var run = _engine.CreateRun(options ?? _engine.Defaults);
        return _engine.LintDocument(document, run, 0, 0, batch: null);
    }

    public MapResult Map(RawDocument document, LintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return _engine.MapDocument(document, _engine.CreateRun(options ?? _engine.Defaults));
    }

    public async IAsyncEnumerable<TransactionResult> LintManyAsync(
        IAsyncEnumerable<Transaction> transactions,
        LintOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        var session = new LintSession(_engine, options ?? _engine.Defaults);
        var index = 0;
        await foreach (var transaction in transactions.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return session.Lint(transaction, index++);
        }

        if (session.Complete(index) is { } batch)
        {
            yield return batch;
        }
    }

    public ILintSession CreateSession(LintOptions? options = null) => new LintSession(_engine, options ?? _engine.Defaults);

    public RuleMetadata Explain(string ruleId) =>
        TryGetRule(ruleId, out var rule) ? rule : throw new KeyNotFoundException(string.Format(System.Globalization.CultureInfo.InvariantCulture, Resources.Strings.RuleUnknown, ruleId));

    public bool TryGetRule(string ruleId, [NotNullWhen(true)] out RuleMetadata? rule)
    {
        ArgumentNullException.ThrowIfNull(ruleId);
        rule = _engine.AllMetadata.FirstOrDefault(r => string.Equals(r.Id, ruleId, StringComparison.OrdinalIgnoreCase))
            ?? EngineDiagnostics.All.FirstOrDefault(r => string.Equals(r.Id, ruleId, StringComparison.OrdinalIgnoreCase));
        return rule is not null;
    }

    public IReadOnlyList<RuleMetadata> Rules(RuleQuery? query = null) =>
        query is null ? _engine.AllMetadata : [.. _engine.AllMetadata.Where(query.Matches)];
}
