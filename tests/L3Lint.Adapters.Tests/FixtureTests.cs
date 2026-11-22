using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;

namespace L3Lint.Adapters.Tests;

/// <summary>
/// The adapter fixture corpus (fixtures/adapters): every fixture is detected as its declared format, maps to the
/// hand-written canonical transactions, and produces exactly the hand-written findings (FR-003 to FR-008, US-02, US-14).
/// The expectations were written from the mapping table and the rule catalog, never copied from adapter output.
/// </summary>
public sealed class FixtureTests
{
    private static readonly string[] Formats = ["stripe", "adyen", "braintree", "cybersource", "authorizenet", "checkoutcom", "csv"];

    public static TheoryData<string> All() => [.. Fixture.AllIds()];

    public static TheoryData<string> Mappable() => [.. Fixture.AllIds().Where(id => !Fixture.Load(id).IsTie)];

    [Fact]
    public void Corpus_has_ten_fixtures_per_adapter_and_one_deliberate_tie()
    {
        var fixtures = Fixture.AllIds().Select(Fixture.Load).ToList();

        fixtures.Count.ShouldBe(70);
        foreach (var format in Formats)
        {
            fixtures.Count(f => f.Folder == format).ShouldBe(10, format);
        }

        fixtures.Count(f => f.IsTie).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(All))]
    public void Fixture_is_detected_as_its_declared_format(string id)
    {
        var fixture = Fixture.Load(id);

        Detect(fixture).ShouldBeTrue(id);
    }

    [Fact]
    public void Detection_accuracy_over_the_corpus_is_at_least_99_percent()
    {
        var fixtures = Fixture.AllIds().Select(Fixture.Load).ToList();

        var correct = fixtures.Count(Detect);
        var accuracy = (double)correct / fixtures.Count;

        TestContext.Current.SendDiagnosticMessage($"Detection accuracy: {correct}/{fixtures.Count} = {accuracy:P1}");
        accuracy.ShouldBeGreaterThanOrEqualTo(0.99);
    }

    [Theory]
    [MemberData(nameof(Mappable))]
    public void Fixture_maps_to_its_hand_written_canonical_transactions(string id)
    {
        var fixture = Fixture.Load(id);
        var adapter = Linters.Adapter(fixture.Format);
        var document = fixture.Document();
        if (!fixture.IsCsv)
        {
            // JSON documents reach adapters already scrubbed (ADR-0004); the pipeline's own scrub stage does it here.
            document = CardNumberScrubber.ScrubJson(document, adapter, []).Document;
        }

        var results = adapter.Map(document, fixture.AdapterOptions());
        var expected = fixture.ExpectedCanonical();

        results.Count.ShouldBe(expected.Count, id);
        for (var i = 0; i < expected.Count; i++)
        {
            var actual = JsonNode.Parse(CanonicalJson.Serialize(results[i].Transaction));
            var difference = JsonEquivalence.FirstDifference(expected[i], actual);
            difference.ShouldBeNull($"{id} transaction {i}: {difference}\n{actual}");
        }
    }

    [Theory]
    [MemberData(nameof(Mappable))]
    public void Fixture_produces_exactly_the_expected_findings(string id)
    {
        var fixture = Fixture.Load(id);

        var result = Linters.All.Lint(fixture.Document(), fixture.LintOptions());

        result.InputErrors.ShouldBeEmpty(id);
        var actual = result.Findings
            .Select(f => Fixture.Key(f.TransactionIndex, f.RuleId, f.Path.Canonical))
            .Order(StringComparer.Ordinal)
            .ToList();
        actual.ShouldBe(fixture.ExpectedFindings(), id + "\nactual:\n" + string.Join("\n", actual));
    }

    private static bool Detect(Fixture fixture)
    {
        var document = fixture.Document();
        if (!fixture.IsTie)
        {
            try
            {
                return Linters.Registry.Resolve(null, document).Name == fixture.Format;
            }
            catch (FormatDetectionException)
            {
                return false;
            }
        }

        try
        {
            Linters.Registry.Resolve(null, document);
            return false;
        }
        catch (FormatDetectionException ex)
        {
            var names = ex.Candidates.Select(c => c.Split(' ')[0]).Order(StringComparer.Ordinal);
            return names.SequenceEqual(fixture.Candidates.Order(StringComparer.Ordinal));
        }
    }
}
