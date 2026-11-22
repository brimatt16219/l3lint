using System.Globalization;

namespace L3Lint.AgentEval;

/// <summary>Command-line options of the agent evaluation.</summary>
internal sealed record EvalOptions(int Limit, string Model, string? Output)
{
    /// <summary>The default model (the current Claude Opus).</summary>
    public const string DefaultModel = "claude-opus-5-5";

    public const string Usage = "Usage: L3Lint.AgentEval [--limit N] [--model ID] [--output report.md]. Default: 40 fixtures, model " + DefaultModel + ".";

    public static EvalOptions? Parse(IReadOnlyList<string> args)
    {
        var limit = 40;
        var model = DefaultModel;
        string? output = null;
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--limit" when i + 1 < args.Count && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0:
                    limit = n;
                    i++;
                    break;
                case "--model" when i + 1 < args.Count:
                    model = args[++i];
                    break;
                case "--output" when i + 1 < args.Count:
                    output = args[++i];
                    break;
                default:
                    return null;
            }
        }

        return new EvalOptions(limit, model, output);
    }
}

/// <summary>Whether the Anthropic SDK has credentials to use; the SDK reads these variables itself.</summary>
internal static class Credentials
{
    private static readonly string[] Variables = ["ANTHROPIC_API_KEY", "ANTHROPIC_AUTH_TOKEN", "ANTHROPIC_PROFILE"];

    public static bool Configured(Func<string, string?> getVariable) => Variables.Any(name => !string.IsNullOrEmpty(getVariable(name)));
}
