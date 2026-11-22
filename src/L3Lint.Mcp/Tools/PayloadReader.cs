using System.Globalization;
using System.Text;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Core.Profiles;
using L3Lint.Hosting;

namespace L3Lint.Mcp.Tools;

/// <summary>
/// Turns tool arguments into a document and lint options. The payload may be a JSON object or a string holding
/// JSON, at most 2 MB. Only built-in processor profiles are accepted, so a tool call can never make the server
/// read a local file.
/// </summary>
internal static class PayloadReader
{
    /// <summary>The largest payload a tool accepts (Interfaces tab, <c>lint_payload</c>).</summary>
    public const int MaxPayloadBytes = 2 * 1024 * 1024;

    public static RawDocument Read(JsonElement payload)
    {
        var bytes = payload.ValueKind switch
        {
            JsonValueKind.String => Encoding.UTF8.GetBytes(payload.GetString()!),
            JsonValueKind.Object => Encoding.UTF8.GetBytes(payload.GetRawText()),
            _ => throw new UsageException(Resources.Strings.ErrorPayloadKind),
        };
        if (bytes.Length > MaxPayloadBytes)
        {
            throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorPayloadTooLarge, bytes.Length, MaxPayloadBytes));
        }

        return RawDocument.ParseJson(bytes, "payload", InputLimits.Default with { MaxFileBytes = MaxPayloadBytes });
    }

    public static LintOptions Options(string? format, LintContext? context, string? failOn, string workingDirectory)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new UsageException(Resources.Strings.ErrorCsvFormat);
        }

        if (context?.Processor is { } processor && !ProcessorProfiles.TryGet(processor, out _))
        {
            throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorProcessor, processor, string.Join(", ", ProcessorProfiles.All.Select(p => p.Id))));
        }

        DateOnly? asOf = null;
        if (context?.AsOf is { } text)
        {
            asOf = DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorAsOf, text));
        }

        var overrides = new LintOverrides
        {
            Format = format,
            Currency = context?.Currency,
            Brand = context?.Brand,
            CardProduct = context?.CardProduct,
            Mcc = context?.Mcc,
            MerchantCountry = context?.MerchantCountry,
            Processor = context?.Processor,
            AsOf = asOf,
            FailOn = failOn,
        };
        return SettingsResolver.Resolve(L3LintConfiguration.Default, overrides, workingDirectory).Options;
    }
}
