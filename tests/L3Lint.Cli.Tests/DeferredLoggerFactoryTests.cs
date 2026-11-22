using L3Lint.Cli.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace L3Lint.Cli.Tests;

/// <summary>The CLI sets up the console logger only when something is logged (cold start, NFR-002).</summary>
public sealed partial class DeferredLoggerFactoryTests
{
    [Fact]
    public void Disabled_levels_never_create_the_real_factory()
    {
        var created = 0;
        using var factory = new DeferredLoggerFactory(LogLevel.Warning, () =>
        {
            created++;
            return NullLoggerFactory.Instance;
        });
        var logger = factory.CreateLogger("test");

        logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
        logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
        logger.IsEnabled(LogLevel.None).ShouldBeFalse();
        Information(logger, "not written");
        Debug(logger, "not written");

        created.ShouldBe(0);
    }

    [Fact]
    public void The_first_enabled_message_creates_the_factory_once_and_reaches_it()
    {
        var created = 0;
        var provider = new RecordingProvider();
        using var factory = new DeferredLoggerFactory(LogLevel.Warning, () =>
        {
            created++;
            return LoggerFactory.Create(builder => builder.AddProvider(provider));
        });
        var first = factory.CreateLogger("one");
        var second = factory.CreateLogger("two");

        Warning(first, "first");
        Error(second, "second");
        Information(first, "below the level");

        created.ShouldBe(1);
        provider.Messages.ShouldBe(["one: first", "two: second"]);
    }

    // Source-generated, like the CLI's own log methods: each checks IsEnabled before formatting anything.
    [LoggerMessage(Level = LogLevel.Debug, Message = "{Text}")]
    private static partial void Debug(ILogger logger, string text);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Text}")]
    private static partial void Information(ILogger logger, string text);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Text}")]
    private static partial void Warning(ILogger logger, string text);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Text}")]
    private static partial void Error(ILogger logger, string text);

    private sealed class RecordingProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Recorder(this, categoryName);

        public void Dispose() => GC.SuppressFinalize(this);

        private sealed class Recorder(RecordingProvider owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                owner.Messages.Add(category + ": " + formatter(state, exception));
        }
    }
}
