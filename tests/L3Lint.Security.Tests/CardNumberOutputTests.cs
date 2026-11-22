namespace L3Lint.Security.Tests;

/// <summary>
/// Card numbers are masked before any rule, log or output sees them (NFR-007, FR-033, US-12; Architecture tab,
/// threat "A real card number in a payload"). Every output format, the fix and convert commands, and the GitHub
/// annotations must hold only the masked form, never the number in raw or digit-only form.
/// </summary>
[Collection(ConsoleGroup.Name)]
public sealed class CardNumberOutputTests : IDisposable
{
    private readonly ConsoleCli _cli = new();

    [Fact]
    public async Task Every_report_format_holds_only_masked_numbers()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);

        var run = await _cli.RunAsync(
            "lint", payload,
            "--output", "text",
            "--output", "json=" + _cli.PathOf("out/report.json"),
            "--output", "sarif=" + _cli.PathOf("out/report.sarif"),
            "--output", "junit=" + _cli.PathOf("out/report.xml"),
            "--output", "markdown=" + _cli.PathOf("out/report.md"),
            "--annotations", "github",
            "--verbose",
            "--log-level", "trace",
            "--no-color");

        run.ExitCode.ShouldBe(1);
        var outputs = new Dictionary<string, string>
        {
            ["stdout (text and annotations)"] = run.Out,
            ["stderr"] = run.Error,
            ["json"] = _cli.Read("out/report.json"),
            ["sarif"] = _cli.Read("out/report.sarif"),
            ["junit"] = _cli.Read("out/report.xml"),
            ["markdown"] = _cli.Read("out/report.md"),
        };
        foreach (var (name, text) in outputs)
        {
            ShouldHoldNoRawNumber(name, text);
        }

        // Positive control: the findings exist and name the masked values.
        outputs["json"].ShouldContain("GEN-PCI-001");
        outputs["json"].ShouldContain(SecretPayloads.Visa.Masked);
        outputs["sarif"].ShouldContain("GEN-PCI-001");
    }

    [Fact]
    public async Task Fix_write_and_dry_run_hold_only_masked_numbers()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);

        var written = await _cli.RunAsync("fix", payload, "--write", _cli.PathOf("fixed/payload.json"), "--no-color");
        var dryRun = await _cli.RunAsync("fix", payload, "--dry-run", "--no-color");

        written.ExitCode.ShouldBe(0);
        ShouldHoldNoRawNumber("fix --write file", _cli.Read("fixed/payload.json"));
        ShouldHoldNoRawNumber("fix --write streams", written.All);
        ShouldHoldNoRawNumber("fix --dry-run", dryRun.All);
        _cli.Read("fixed/payload.json").ShouldContain(SecretPayloads.Visa.Masked);
    }

    [Fact]
    public async Task Convert_holds_only_masked_numbers()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);

        var mapping = await _cli.RunAsync("convert", payload, "--to", "canonical");
        var canonical = await _cli.RunAsync("convert", payload, "--to", "canonical", "--canonical-only");

        mapping.ExitCode.ShouldBe(0);
        canonical.ExitCode.ShouldBe(0);
        ShouldHoldNoRawNumber("convert", mapping.All);
        ShouldHoldNoRawNumber("convert --canonical-only", canonical.All);
        canonical.Out.ShouldContain(SecretPayloads.Visa.Masked);
    }

    [Fact]
    public async Task A_gateway_payload_is_masked_before_mapping()
    {
        // Stripe line names are free text, so the number is masked in the source document before the adapter runs.
        var stripe = File.ReadAllText(Path.Combine(Repository.Root, "fixtures", "adapters", "stripe", "01-complete-level3.json"))
            .Replace("Nitrile gloves L box", SecretPayloads.Visa.Spaced, StringComparison.Ordinal)
            .Replace("PO-48820", SecretPayloads.Mastercard.Digits, StringComparison.Ordinal);
        var payload = _cli.Write("stripe.json", stripe);

        var lint = await _cli.RunAsync("lint", payload, "--format", "stripe", "--output", "json", "--output", "sarif=" + _cli.PathOf("stripe.sarif"), "--no-color");
        var convert = await _cli.RunAsync("convert", payload, "--format", "stripe", "--to", "canonical");
        var fix = await _cli.RunAsync("fix", payload, "--format", "stripe", "--write", _cli.PathOf("stripe.fixed.json"));

        lint.Out.ShouldContain("GEN-PCI-001");
        ShouldHoldNoRawNumber("stripe lint", lint.All);
        ShouldHoldNoRawNumber("stripe sarif", _cli.Read("stripe.sarif"));
        ShouldHoldNoRawNumber("stripe convert", convert.All);
        ShouldHoldNoRawNumber("stripe fix --write", _cli.Read("stripe.fixed.json") + fix.All);
    }

    [Fact]
    public async Task A_csv_cell_is_masked_in_every_output()
    {
        var folder = Path.Combine(Repository.Root, "fixtures", "adapters", "csv");
        var csv = _cli.Write("cards.csv", File.ReadAllText(Path.Combine(folder, "05-card-number-in-cell.csv")));
        var map = _cli.Write("cards.map.yaml", File.ReadAllText(Path.Combine(folder, "05-card-number-in-cell.map.yaml")));

        var lint = await _cli.RunAsync(
            "lint", csv, "--format", "csv", "--map", map,
            "--output", "json", "--output", "junit=" + _cli.PathOf("csv.xml"), "--output", "markdown=" + _cli.PathOf("csv.md"), "--no-color");
        var convert = await _cli.RunAsync("convert", csv, "--format", "csv", "--map", map, "--to", "canonical");
        var fix = await _cli.RunAsync("fix", csv, "--format", "csv", "--map", map, "--write", _cli.PathOf("csv.fixed.ndjson"));

        lint.Out.ShouldContain("GEN-PCI-001");
        ShouldHoldNoRawNumber("csv lint", lint.All + _cli.Read("csv.xml") + _cli.Read("csv.md"));
        ShouldHoldNoRawNumber("csv convert", convert.All);
        ShouldHoldNoRawNumber("csv fix --write", _cli.Read("csv.fixed.ndjson") + fix.All);
    }

    [Fact]
    public async Task Strict_pci_stops_the_run_without_echoing_the_number()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);

        var run = await _cli.RunAsync("lint", payload, "--strict-pci", "--output", "json", "--no-color");

        run.ExitCode.ShouldBe(1);
        ShouldHoldNoRawNumber("--strict-pci", run.All);
    }

    public void Dispose() => _cli.Dispose();

    private static void ShouldHoldNoRawNumber(string output, string text)
    {
        foreach (var card in SecretPayloads.Cards)
        {
            foreach (var raw in card.RawForms)
            {
                text.ShouldNotContain(raw, Case.Sensitive, $"{output} holds the card number {raw}.");
            }
        }
    }
}
