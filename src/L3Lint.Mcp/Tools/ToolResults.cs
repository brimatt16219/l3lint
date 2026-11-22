using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Hosting;
using ModelContextProtocol.Protocol;

namespace L3Lint.Mcp.Tools;

/// <summary>
/// Builds tool results: structured content plus a short text summary for clients that only read text, and
/// <c>isError: true</c> results with a message the model can act on (Interfaces tab, "Errors the model can act on").
/// </summary>
internal static class ToolResults
{
    /// <summary>A successful result.</summary>
    public static CallToolResult Success(string summary, string structuredJson)
    {
        using var document = JsonDocument.Parse(structuredJson);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = summary }],
            StructuredContent = document.RootElement.Clone(),
        };
    }

    /// <summary>A tool execution error the model can read and correct.</summary>
    public static CallToolResult Error(string message) => new()
    {
        Content = [new TextContentBlock { Text = message }],
        IsError = true,
    };

    /// <summary>
    /// Runs a tool body and turns input, usage and configuration problems into tool execution errors. Anything
    /// else is a bug and propagates to the SDK, which reports it without internal details.
    /// </summary>
    public static CallToolResult Run(Func<CallToolResult> body)
    {
        try
        {
            return body();
        }
        catch (FormatDetectionException ex) when (ex.Candidates.Count > 0)
        {
            return Error(ex.Message + " " + string.Format(CultureInfo.InvariantCulture, Resources.Strings.HintFormat, string.Join(", ", ex.Candidates)));
        }
        catch (InputException ex)
        {
            return Error(ex.Line > 0
                ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorAtLine, ex.Line, ex.Column, ex.Message)
                : ex.Message);
        }
        catch (UsageException ex)
        {
            return Error(ex.Message);
        }
        catch (ConfigurationException ex)
        {
            return Error(ex.Message);
        }
    }
}
