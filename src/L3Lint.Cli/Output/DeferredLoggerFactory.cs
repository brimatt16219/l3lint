using Microsoft.Extensions.Logging;

namespace L3Lint.Cli.Output;

/// <summary>
/// A logger factory that creates the real one only when a message at an enabled level is written.
/// </summary>
/// <remarks>
/// Setting up the console logger takes about 20 ms of a cold start (NFR-002: under 300 ms), and at the default
/// level most runs never log. Loggers from this factory answer <see cref="ILogger.IsEnabled"/> from the minimum
/// level alone, so the source-generated log methods skip disabled messages without creating anything.
/// </remarks>
internal sealed class DeferredLoggerFactory(LogLevel minimum, Func<ILoggerFactory> create) : ILoggerFactory
{
    private readonly Lazy<ILoggerFactory> _factory = new(create);

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new DeferredLogger(this, categoryName);

    /// <inheritdoc/>
    public void AddProvider(ILoggerProvider provider) => _factory.Value.AddProvider(provider);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_factory.IsValueCreated)
        {
            _factory.Value.Dispose();
        }
    }

    private bool IsEnabled(LogLevel level) => level != LogLevel.None && level >= minimum;

    private sealed class DeferredLogger(DeferredLoggerFactory owner, string category) : ILogger
    {
        private ILogger? _inner;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => Inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => owner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (owner.IsEnabled(logLevel))
            {
                Inner.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        // Two threads may both create the inner logger; both come from the same factory, so either is fine.
        private ILogger Inner => _inner ??= owner._factory.Value.CreateLogger(category);
    }
}
