using L3Lint.Core.Input;

namespace L3Lint.Core;

/// <summary>
/// An input that could not be linted (exit code 2), reported next to the results of the inputs that could.
/// </summary>
/// <param name="SourceName">The input file or label.</param>
/// <param name="Kind">The kind of problem.</param>
/// <param name="Message">What is wrong and how to fix it.</param>
/// <param name="Line">The 1-based line, when known.</param>
/// <param name="Candidates">For ambiguous formats, the adapters that scored too close to choose.</param>
public sealed record InputError(string SourceName, InputErrorKind Kind, string Message, int? Line = null, IReadOnlyList<string>? Candidates = null)
{
    /// <summary>Creates an input error from an exception.</summary>
    /// <param name="exception">The input exception.</param>
    /// <param name="fallbackName">The name to use when the exception has none.</param>
    /// <returns>The input error.</returns>
    public static InputError From(InputException exception, string fallbackName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new InputError(
            exception.SourceName ?? fallbackName,
            exception.Kind,
            exception.Message,
            exception.Line > 0 ? exception.Line : null,
            exception is Adapters.FormatDetectionException detection ? detection.Candidates : null);
    }
}
