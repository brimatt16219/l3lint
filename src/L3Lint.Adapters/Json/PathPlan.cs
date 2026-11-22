using L3Lint.Core.Adapters;
using L3Lint.Core.Fields;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Builds the path map of a JSON document from its routes (FR-007). Every one of the 52 canonical fields gets an
/// entry: routed fields point at their source location (present or not), the rest resolve to <see langword="null"/>.
/// </summary>
internal static class PathPlan
{
    /// <summary>Builds the path map.</summary>
    /// <param name="routes">The routes of the document's shape.</param>
    /// <param name="lines">The line plan, for line keys.</param>
    /// <param name="converters">Converters back to source values.</param>
    /// <param name="lineRoutes">Routes for line fields when they differ per line; <see langword="null"/> uses <paramref name="routes"/>.</param>
    /// <param name="lineOverrides">Per-line routes chosen while reading, keyed by line index and field name.</param>
    /// <returns>The immutable path map.</returns>
    public static PathMap Build(
        RouteSet routes,
        LinePlan lines,
        SourceConverters converters,
        Func<int, RouteSet>? lineRoutes = null,
        IReadOnlyDictionary<(int Index, string Field), Route>? lineOverrides = null)
    {
        var builder = new PathMapBuilder();
        foreach (var field in CanonicalFields.All)
        {
            if (!field.IsLineField)
            {
                var route = routes.Header(field.Path);
                builder.Field(field.Path, route is null ? null : new SourceField(route.Source.Format(0), converters.For(route, field.Path)));
                continue;
            }

            var name = field.Name;
            builder.LineField(name, index =>
            {
                Route? route = null;
                if (lineOverrides is not null && lineOverrides.TryGetValue((index, name), out var chosen))
                {
                    route = chosen;
                }

                route ??= (lineRoutes?.Invoke(index) ?? routes).Line(name);
                return route is null ? null : new SourceField(route.Source.Format(lines.Key(index)), converters.For(route, field.Path));
            });
        }

        builder.LineItems(
            routes.Collection is { } collection ? new SourceField(collection) : null,
            index => (lineRoutes?.Invoke(index) ?? routes).Item is { } item ? new SourceField(item.Format(lines.Key(index))) : null);
        builder.AdviceText(null, static _ => null);
        return builder.Build();
    }
}
