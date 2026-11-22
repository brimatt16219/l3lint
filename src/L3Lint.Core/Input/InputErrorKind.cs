namespace L3Lint.Core.Input;

/// <summary>
/// Why an input could not be linted.
/// </summary>
public enum InputErrorKind
{
    /// <summary>The input is larger than the file size limit.</summary>
    TooLarge,

    /// <summary>The input is not well-formed JSON or CSV.</summary>
    Malformed,

    /// <summary>The JSON nesting is deeper than the limit.</summary>
    TooDeep,

    /// <summary>A JSON object repeats a property name, so its meaning is ambiguous.</summary>
    DuplicateProperty,

    /// <summary>The JSON value is not an object.</summary>
    NotAnObject,

    /// <summary>A transaction has more line items than the limit.</summary>
    TooManyLineItems,

    /// <summary>No adapter recognized the format, or two adapters scored too close to choose.</summary>
    UnknownFormat,

    /// <summary>The selected adapter needs something the input or options do not provide, such as a CSV column map.</summary>
    Unsupported,
}
