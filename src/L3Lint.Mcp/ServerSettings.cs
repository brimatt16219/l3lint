using System.Globalization;

namespace L3Lint.Mcp;

/// <summary>
/// Command-line settings of <c>l3lint-mcp</c>. stdio is the default transport; <c>--http</c> serves Streamable
/// HTTP on 127.0.0.1 only (Interfaces tab, "Transports").
/// </summary>
internal sealed record ServerSettings
{
    /// <summary>The default HTTP port (Interfaces tab).</summary>
    public const int DefaultPort = 7341;

    /// <summary>The environment variable that can carry the bearer token, so it stays out of process listings.</summary>
    public const string TokenVariable = "L3LINT_MCP_TOKEN";

    public bool Http { get; init; }

    public int Port { get; init; } = DefaultPort;

    /// <summary>Gets the bearer token every HTTP request must carry, or <see langword="null"/> for none.</summary>
    public string? Token { get; init; }

    /// <summary>Gets extra browser origins allowed to call the HTTP endpoint, besides localhost.</summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    public bool ShowHelp { get; init; }

    public bool ShowVersion { get; init; }

    /// <summary>Gets the folder used to resolve relative paths; tools never read files, so this only matters for defaults.</summary>
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Parses the command line. Throws <see cref="ArgumentException"/> with a readable message on bad input.</summary>
    public static ServerSettings Parse(IReadOnlyList<string> args, Func<string, string?> getVariable)
    {
        var settings = new ServerSettings { Token = NullIfEmpty(getVariable(TokenVariable)) };
        var origins = new List<string>();
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--http":
                    settings = settings with { Http = true };
                    break;
                case "--port":
                    var port = Value(args, ref i);
                    settings = settings with
                    {
                        Port = int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number is > 0 and < 65536
                            ? number
                            : throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorPort, port)),
                    };
                    break;
                case "--token":
                    settings = settings with { Token = NullIfEmpty(Value(args, ref i)) };
                    break;
                case "--allow-origin":
                    origins.Add(Value(args, ref i).TrimEnd('/'));
                    break;
                case "--help" or "-h" or "-?":
                    settings = settings with { ShowHelp = true };
                    break;
                case "--version":
                    settings = settings with { ShowVersion = true };
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorArgument, args[i]));
            }
        }

        return settings with { AllowedOrigins = origins };
    }

    private static string Value(IReadOnlyList<string> args, ref int i)
    {
        if (i + 1 >= args.Count)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorMissingValue, args[i]));
        }

        i++;
        return args[i];
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
