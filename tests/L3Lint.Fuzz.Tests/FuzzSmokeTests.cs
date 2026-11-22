using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace L3Lint.Fuzz.Tests;

/// <summary>
/// A deterministic fuzz smoke run for every <c>dotnet test</c> (T-43, NFR-006). Each target from the SharpFuzz
/// harness gets thousands of seeded mutations of the repository's own inputs. Every input must end in a normal
/// result or one of the documented exception types: <c>InputException</c> (and its subclass
/// <c>FormatDetectionException</c>), <c>YamlFormatException</c>, <c>ConfigurationException</c> or
/// <c>RulePackException</c>. The long coverage-guided runs use the same targets under libFuzzer or AFL
/// (tests/L3Lint.Fuzz/README.md).
/// </summary>
public sealed class FuzzSmokeTests
{
    /// <summary>One seed per target, fixed so every run feeds the same inputs.</summary>
    [Theory]
    [InlineData("json", 5000, 20261019)]
    [InlineData("config", 5000, 20261020)]
    [InlineData("csv", 5000, 20261021)]
    [InlineData("rulepack", 2000, 20261022)]
    public void Target_ends_every_input_in_a_result_or_a_documented_error(string target, int count, int seed)
    {
        var inputs = new Mutator(seed).Inputs(target, Seeds.For(target), count);
        var failures = new Dictionary<string, Failure>(StringComparer.Ordinal);
        var accepted = 0;
        var rejected = 0;

        for (var i = 0; i < inputs.Count; i++)
        {
            try
            {
                if (FuzzTargets.Run(target, inputs[i]) == FuzzOutcome.Accepted)
                {
                    accepted++;
                }
                else
                {
                    rejected++;
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                var signature = Signature(ex);
                if (!failures.TryGetValue(signature, out var failure) || inputs[i].Length < failure.Input.Length)
                {
                    failures[signature] = new Failure(inputs[i], i, ex, (failure?.Count ?? 0) + 1);
                }
                else
                {
                    failures[signature] = failure with { Count = failure.Count + 1 };
                }
            }
        }

        failures.ShouldBeEmpty(Describe(target, seed, count, failures));

        // The mutations reach both outcomes, so the run is neither all-valid nor all-garbage.
        accepted.ShouldBeGreaterThan(count / 100, $"{target}: only {accepted} of {count} inputs were accepted");
        rejected.ShouldBeGreaterThan(count / 100, $"{target}: only {rejected} of {count} inputs were rejected");
    }

    [Fact]
    public void The_same_seed_gives_the_same_inputs()
    {
        var first = new Mutator(7).Inputs("json", Seeds.For("json"), 200);
        var second = new Mutator(7).Inputs("json", Seeds.For("json"), 200);

        first.Select(Convert.ToHexString).ShouldBe(second.Select(Convert.ToHexString));
    }

    [Theory]
    [InlineData("json", "tests/L3Lint.Core.Tests/SpecExamples/canonical-example.json")]
    [InlineData("json", "fixtures/adapters/stripe/01-complete-level3.json")]
    [InlineData("config", "tests/L3Lint.Core.Tests/SpecExamples/config-example.yaml")]
    [InlineData("csv", "fixtures/adapters/csv/01-erp-two-invoices.csv")]
    [InlineData("rulepack", "src/L3Lint.Rules.General/Pack/general.rules.yaml")]
    [InlineData("rulepack", "src/L3Lint.Rules.Visa/Pack/visa.rules.yaml")]
    public void Main_seeds_are_accepted(string target, string relativePath)
    {
        // These seeds are valid inputs for their target, so a rejection here would mean the harness itself is broken.
        var seed = File.ReadAllBytes(Path.Combine(Seeds.Root, relativePath));

        FuzzTargets.Run(target, seed).ShouldBe(FuzzOutcome.Accepted);
    }

    /// <summary>The exception type and the first l3lint frame it came from, so one bug reports once.</summary>
    private static string Signature(Exception exception)
    {
        var frame = new StackTrace(exception).GetFrames()
            .Select(f => f.GetMethod())
            .FirstOrDefault(m => m?.DeclaringType?.FullName is { } name && name.StartsWith("L3Lint.", StringComparison.Ordinal) && !name.StartsWith("L3Lint.Fuzz", StringComparison.Ordinal));
        return exception.GetType().FullName + " at " + (frame is null ? "(no l3lint frame)" : frame.DeclaringType!.FullName + "." + frame.Name);
    }

    private static string Describe(string target, int seed, int count, Dictionary<string, Failure> failures)
    {
        var folder = Path.Combine(Path.GetTempPath(), "l3lint-fuzz-smoke", target);
        Directory.CreateDirectory(folder);
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"{target}: {failures.Values.Sum(f => f.Count)} of {count} inputs (seed {seed}) threw an undocumented exception.\n");
        foreach (var (signature, failure) in failures.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            var file = Path.Combine(folder, Convert.ToHexStringLower(SHA256.HashData(failure.Input))[..16] + ".bin");
            File.WriteAllBytes(file, failure.Input);
            var preview = Encoding.UTF8.GetString(failure.Input[..Math.Min(failure.Input.Length, 300)]).Replace("\n", "\\n", StringComparison.Ordinal);
            builder.Append(CultureInfo.InvariantCulture, $"\n- {signature} ({failure.Count} inputs)\n");
            builder.Append(CultureInfo.InvariantCulture, $"  message: {failure.Exception.Message.Split('\n')[0]}\n");
            builder.Append(CultureInfo.InvariantCulture, $"  smallest input #{failure.Index}, {failure.Input.Length} bytes, saved to {file}\n");
            builder.Append(CultureInfo.InvariantCulture, $"  repro: dotnet L3Lint.Fuzz.dll {target} repro {file}\n");
            builder.Append(CultureInfo.InvariantCulture, $"  starts: {preview}\n");
        }

        return builder.ToString();
    }

    private sealed record Failure(byte[] Input, int Index, Exception Exception, int Count);
}
