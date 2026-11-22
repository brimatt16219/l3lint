using System.Globalization;
using L3Lint.Core.Fields;

namespace L3Lint.Core.Schemas;

/// <summary>
/// Turns schema violations into plain messages from resources (NFR-011). The validator's own text is never shown.
/// </summary>
internal static class SchemaMessages
{
    /// <summary>Describes a violation in a config, pack, profile, map or rate table file.</summary>
    public static string Describe(SchemaError error)
    {
        var where = error.Pointer.Length == 0 ? "/" : error.Pointer;
        var template = error.Keyword switch
        {
            "type" => Resources.Strings.SchemaWrongType,
            "enum" or "const" => Resources.Strings.SchemaNotAllowedValue,
            "pattern" => Resources.Strings.SchemaWrongFormat,
            "minLength" or "maxLength" => Resources.Strings.SchemaWrongLength,
            "minimum" or "maximum" or "exclusiveMinimum" or "exclusiveMaximum" => Resources.Strings.SchemaOutOfRange,
            "multipleOf" => Resources.Strings.SchemaTooManyDecimals,
            "required" => Resources.Strings.SchemaMissingKey,
            "false" or "" or "propertyNames" => Resources.Strings.SchemaUnknownKey,
            "minItems" or "maxItems" or "minProperties" or "maxProperties" => Resources.Strings.SchemaWrongCount,
            _ => Resources.Strings.SchemaMismatch,
        };
        return string.Format(CultureInfo.InvariantCulture, template, where);
    }

    /// <summary>Describes a canonical payload violation with the field's own expectation.</summary>
    public static string DescribeCanonical(SchemaError error)
    {
        var where = error.Pointer.Length == 0 ? "/" : error.Pointer;
        var field = CanonicalFields.FindByPointer(error.Pointer);
        if (error.Keyword is "false" or "" or "propertyNames")
        {
            return string.Format(CultureInfo.InvariantCulture, Resources.Strings.CanonicalUnknownField, where);
        }

        if (error.Keyword == "required")
        {
            return Resources.Strings.CanonicalSchemaVersionRequired;
        }

        if (field is null)
        {
            return Describe(error);
        }

        var expectation = error.Keyword switch
        {
            "type" => Expectation(field),
            "enum" => Expectation(field),
            _ when field.Constraint is not null => Resources.Strings.ResourceManager.GetString(field.Constraint, CultureInfo.InvariantCulture) ?? Expectation(field),
            _ => Expectation(field),
        };
        return string.Format(CultureInfo.InvariantCulture, Resources.Strings.CanonicalFieldMustBe, where, expectation);
    }

    private static string Expectation(CanonicalField field) => field.Kind switch
    {
        FieldKind.Decimal => Resources.Strings.KindDecimal,
        FieldKind.Integer => Resources.Strings.KindInteger,
        FieldKind.Boolean => Resources.Strings.KindBoolean,
        FieldKind.Date => Resources.Strings.KindDate,
        FieldKind.TextList => Resources.Strings.KindTextList,
        FieldKind.Collection => Resources.Strings.KindCollection,
        FieldKind.Enum => string.Format(CultureInfo.InvariantCulture, Resources.Strings.KindEnum, string.Join(", ", field.EnumValues ?? [])),
        _ => Resources.Strings.KindText,
    };
}
