using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace L3Lint.Mcp.Content;

/// <summary>The two prompts (Interfaces tab, "Resources and prompts").</summary>
[McpServerPromptType]
internal static class ReviewPrompts
{
    [McpServerPrompt(Name = "review_payload", Title = "Review a payload")]
    [Description("Lint a payload, explain each error in plain words, propose the patch and list what needs a human decision.")]
    public static string ReviewPayload([Description("The payment request body as JSON.")] string payload) =>
        string.Format(CultureInfo.InvariantCulture, Resources.Strings.PromptReview, payload, Paragraph);

    [McpServerPrompt(Name = "explain_downgrade", Title = "Explain a downgrade")]
    [Description("Given a payload and the interchange category it settled in, list the findings that explain the downgrade.")]
    public static string ExplainDowngrade(
        [Description("The payment request body as JSON.")] string payload,
        [Description("The interchange category the transaction settled in, as shown on the processor statement.")] string category) =>
        string.Format(CultureInfo.InvariantCulture, Resources.Strings.PromptDowngrade, category, Paragraph, payload);

    // Resource strings hold one line each; the paragraph break is added here.
    private const string Paragraph = "\n\n";
}
