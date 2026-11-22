using System.CommandLine;
using System.Globalization;
using L3Lint.Hosting;

namespace L3Lint.Cli;

/// <summary>
/// The context and rule-selection options shared by <c>lint</c>, <c>fix</c> and <c>convert</c> (Interfaces tab,
/// "lint options"). One instance per command.
/// </summary>
internal sealed class ContextOptions
{
    public Option<string> Format { get; } = new("--format", "-f") { Description = Resources.Strings.OptFormat, HelpName = "auto|canonical|stripe|adyen|braintree|cybersource|authorizenet|checkoutcom|csv" };

    public Option<string> Map { get; } = new("--map") { Description = Resources.Strings.OptMap, HelpName = "path" };

    public Option<string> Currency { get; } = new("--currency") { Description = Resources.Strings.OptCurrency, HelpName = "XXX" };

    public Option<string> Brand { get; } = new("--brand") { Description = Resources.Strings.OptBrand, HelpName = "auto|visa|mastercard|amex|discover|all" };

    public Option<string> CardProduct { get; } = new("--card-product") { Description = Resources.Strings.OptCardProduct, HelpName = "consumer|business|corporate|purchasing|fleet|commercialPrepaid" };

    public Option<string> Mcc { get; } = new("--mcc") { Description = Resources.Strings.OptMcc, HelpName = "nnnn" };

    public Option<string> MerchantCountry { get; } = new("--merchant-country") { Description = Resources.Strings.OptMerchantCountry, HelpName = "XX" };

    public Option<string> Processor { get; } = new("--processor") { Description = Resources.Strings.OptProcessor, HelpName = "profile|path" };

    public Option<string> AsOf { get; } = new("--as-of") { Description = Resources.Strings.OptAsOf, HelpName = "YYYY-MM-DD" };

    public Option<string> Config { get; } = new("--config") { Description = Resources.Strings.OptConfig, HelpName = "path" };

    public Option<string[]> RulePack { get; } = new("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path", AllowMultipleArgumentsPerToken = false };

    public Option<string[]> Only { get; } = new("--only") { Description = Resources.Strings.OptOnly, HelpName = "rule|glob", AllowMultipleArgumentsPerToken = true };

    public Option<string[]> Disable { get; } = new("--disable") { Description = Resources.Strings.OptDisable, HelpName = "rule|glob", AllowMultipleArgumentsPerToken = true };

    public Option<string[]> Severity { get; } = new("--severity") { Description = Resources.Strings.OptSeverity, HelpName = "RULE=level" };

    public Option<string> Rates { get; } = new("--rates") { Description = Resources.Strings.OptRates, HelpName = "path" };

    public Option<bool> StrictPci { get; } = new("--strict-pci") { Description = Resources.Strings.OptStrictPci };

    public Option<bool> StrictProfiles { get; } = new("--strict-profiles") { Description = Resources.Strings.OptStrictProfiles };

    public void AddTo(Command command)
    {
        foreach (var option in new Option[] { Format, Map, Currency, Brand, CardProduct, Mcc, MerchantCountry, Processor, AsOf, Config, RulePack, Only, Disable, Severity, Rates, StrictPci, StrictProfiles })
        {
            command.Add(option);
        }
    }

    /// <summary>Reads the flags into overrides. Bad dates are usage errors.</summary>
    public LintOverrides ToOverrides(ParseResult result, bool verbose, IReadOnlyList<string> outputs, string? failOn, int? maxFindings) => new()
    {
        Format = result.GetValue(Format),
        CsvMap = result.GetValue(Map),
        Currency = result.GetValue(Currency),
        Brand = result.GetValue(Brand),
        CardProduct = result.GetValue(CardProduct),
        Mcc = result.GetValue(Mcc),
        MerchantCountry = result.GetValue(MerchantCountry),
        Processor = result.GetValue(Processor),
        AsOf = ParseDate(result.GetValue(AsOf)),
        RulePacks = result.GetValue(RulePack) ?? [],
        Only = result.GetValue(Only) ?? [],
        Disable = result.GetValue(Disable) ?? [],
        Severity = result.GetValue(Severity) ?? [],
        Rates = result.GetValue(Rates),
        StrictPci = result.GetValue(StrictPci),
        StrictProfiles = result.GetValue(StrictProfiles),
        Verbose = verbose,
        Outputs = outputs,
        FailOn = failOn,
        MaxFindings = maxFindings,
    };

    /// <summary>Loads the config file: --config when given, else the nearest .l3lint.yaml walking up.</summary>
    public L3LintConfiguration LoadConfiguration(ParseResult result, string workingDirectory)
    {
        if (result.GetValue(Config) is { } explicitPath)
        {
            var full = Path.GetFullPath(Path.Combine(workingDirectory, explicitPath));
            return File.Exists(full)
                ? L3LintConfiguration.Load(full)
                : throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorConfigMissing, explicitPath));
        }

        return L3LintConfiguration.FindNearest(workingDirectory) is { } found ? L3LintConfiguration.Load(found) : L3LintConfiguration.Default;
    }

    private static DateOnly? ParseDate(string? text)
    {
        if (text is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorAsOf, text));
    }
}
