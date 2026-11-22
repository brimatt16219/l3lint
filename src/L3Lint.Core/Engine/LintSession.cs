using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Engine;

/// <summary>Default <see cref="ILintSession"/>.</summary>
internal sealed class LintSession : ILintSession
{
    private readonly LintEngine _engine;
    private readonly RunContext _run;
    private readonly BatchAggregator _batch;
    private int _completed;

    public LintSession(LintEngine engine, LintOptions options)
    {
        _engine = engine;
        _run = engine.CreateRun(options);
        _batch = engine.CreateBatch(options);
        Options = options;
    }

    public LintOptions Options { get; }

    public LintResult Lint(RawDocument document, int firstIndex, int input)
    {
        ArgumentNullException.ThrowIfNull(document);
        return _engine.LintDocument(document, _run, firstIndex, input, _batch);
    }

    public TransactionResult Lint(Transaction transaction, int index)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return _engine.LintTransaction(transaction, _run, index, _batch);
    }

    public TransactionResult? Complete(int index)
    {
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            throw new InvalidOperationException(Resources.Strings.SessionCompleted);
        }

        return LintEngine.CompleteBatch(_batch, _run, index);
    }
}
