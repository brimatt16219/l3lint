using System.CommandLine;
using System.Globalization;

namespace L3Lint.Cli;

/// <summary>
/// Input arguments accept any token, so a mistyped option such as <c>--fial-on</c> would otherwise be read as a
/// file name. Tokens that start with a dash (other than "-" for standard input) are rejected as unknown options.
/// A file whose name starts with a dash can still be passed as <c>./-name.json</c>.
/// </summary>
internal static class InputArguments
{
    public static T RejectOptionLikeTokens<T>(T argument)
        where T : Argument
    {
        argument.Validators.Add(result =>
        {
            foreach (var token in result.Tokens)
            {
                if (token.Value.Length > 1 && token.Value[0] == '-')
                {
                    result.AddError(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownOption, token.Value));
                }
            }
        });
        return argument;
    }
}
