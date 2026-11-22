using L3Lint.Core;
using L3Lint.Hosting.Runs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace L3Lint.Hosting;

/// <summary>
/// Registers l3lint in a dependency injection container (FR-031).
/// </summary>
public static class L3LintServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILinter"/> as a thread-safe singleton with the five shipped rule packs and seven
    /// adapters, plus <see cref="LintRunner"/> for batch runs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets default options and adds custom rules, checks, adapters or packs.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// Rule packs load when <see cref="ILinter"/> is first resolved; after that the linter does no I/O (US-09). An
    /// invalid pack throws <see cref="Core.Rules.RulePackException"/> at that point.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddL3Lint(options =&gt;
    /// {
    ///     options.Processor = "generic";
    ///     options.FailOn = Severity.Error;
    ///     options.AddRule&lt;PoPrefixRule&gt;();
    /// });
    ///
    /// app.MapPost("/invoices/{id}/charge", (string id, InvoiceService invoices, ILinter linter) =&gt;
    /// {
    ///     LintResult result = linter.Lint(invoices.ToCanonical(id));
    ///     return result.Summary.Errors &gt; 0
    ///         ? Results.UnprocessableEntity(result.Findings.Where(f =&gt; f.Severity == Severity.Error))
    ///         : Results.Ok();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddL3Lint(this IServiceCollection services, Action<L3LintOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new L3LintOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILinter>(provider =>
        {
            var builder = options.Configure(L3LintFactory.CreateBuilder()).UseTimeProvider(provider.GetRequiredService<TimeProvider>());
            if (provider.GetService<ILoggerFactory>() is { } loggerFactory)
            {
                builder.UseLoggerFactory(loggerFactory);
            }

            return builder.Build();
        });
        services.TryAddSingleton(provider => new LintRunner(provider.GetRequiredService<ILinter>(), provider.GetService<ILogger<LintRunner>>()));
        return services;
    }
}
