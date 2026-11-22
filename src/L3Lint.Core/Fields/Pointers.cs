using System.Globalization;

namespace L3Lint.Core.Fields;

/// <summary>Builds concrete canonical pointers.</summary>
internal static class Pointers
{
    public static string Line(int index) => "/lineItems/" + index.ToString(CultureInfo.InvariantCulture);

    public static string Line(int index, string field) => Line(index) + "/" + field;
}
