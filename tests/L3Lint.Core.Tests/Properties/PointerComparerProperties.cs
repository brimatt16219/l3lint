using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using L3Lint.Core.Fields;

namespace L3Lint.Core.Tests.Properties;

/// <summary>
/// <see cref="PointerComparer"/> orders the JSON Pointers l3lint reports (NFR-001: findings sort by transaction,
/// then JSON path, then rule ID). It must be a total order on document pointers, and array indexes must sort as
/// numbers, so /lineItems/2 comes before /lineItems/10.
/// </summary>
/// <remarks>
/// The pointers are shaped like the ones a JSON document produces: each segment is an RFC 6901 array index
/// (0 or a digit 1 to 9 followed by digits), a property name that starts with a letter, as in every canonical and
/// gateway field name, or an unusual key that mixes digits, punctuation and letters (such as "1a", "007" or "-x"),
/// which only unmapped gateway keys can produce.
/// </remarks>
public sealed class PointerComparerProperties
{
    private static readonly PointerComparer Comparer = PointerComparer.Instance;

    [Property(MaxTest = 1000)]
    public Property Comparison_is_reflexive_and_antisymmetric() =>
        Prop.ForAll(Pointers().Two().ToArbitrary(), pair =>
        {
            var (x, y) = pair;
            Comparer.Compare(x, x).ShouldBe(0);
            Math.Sign(Comparer.Compare(x, y)).ShouldBe(-Math.Sign(Comparer.Compare(y, x)), $"{x} vs {y}");
            (Comparer.Compare(x, y) == 0).ShouldBe(string.Equals(x, y, StringComparison.Ordinal), $"{x} vs {y}");
        });

    [Property(MaxTest = 1000)]
    public Property Comparison_is_transitive() =>
        Prop.ForAll(Pointers().Three().ToArbitrary(), triple =>
        {
            var (x, y, z) = triple;
            if (Comparer.Compare(x, y) <= 0 && Comparer.Compare(y, z) <= 0)
            {
                Comparer.Compare(x, z).ShouldBeLessThanOrEqualTo(0, $"{x} <= {y} <= {z}");
            }

            if (Comparer.Compare(x, y) >= 0 && Comparer.Compare(y, z) >= 0)
            {
                Comparer.Compare(x, z).ShouldBeGreaterThanOrEqualTo(0, $"{x} >= {y} >= {z}");
            }
        });

    [Property(MaxTest = 1000)]
    public Property Sorting_gives_a_consistent_sequence() =>
        Prop.ForAll(Gen.Choose(0, 30).SelectMany(count => Gen.ListOf(Pointers(), count)).ToArbitrary(), list =>
        {
            var sorted = list.Order(Comparer).ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                for (var j = i + 1; j < sorted.Count; j++)
                {
                    Comparer.Compare(sorted[i], sorted[j]).ShouldBeLessThanOrEqualTo(0, $"{sorted[i]} before {sorted[j]}");
                }
            }

            // The same set in any input order sorts to the same output (NFR-001).
            list.AsEnumerable().Reverse().Order(Comparer).ShouldBe(sorted);
        });

    [Property(MaxTest = 1000)]
    public Property Array_indexes_compare_as_numbers() =>
        Prop.ForAll(Gen.Choose(0, int.MaxValue).Two().Zip(Pointers()).ToArbitrary(), input =>
        {
            var ((a, b), prefix) = input;
            var left = $"{prefix}/lineItems/{a.ToString(CultureInfo.InvariantCulture)}/total";
            var right = $"{prefix}/lineItems/{b.ToString(CultureInfo.InvariantCulture)}/total";

            Math.Sign(Comparer.Compare(left, right)).ShouldBe(Math.Sign(a.CompareTo(b)), $"{left} vs {right}");
        });

    [Property(MaxTest = 1000)]
    public Property A_pointer_sorts_before_the_pointers_below_it() =>
        Prop.ForAll(Pointers().Zip(Segment()).ToArbitrary(), input =>
        {
            var (parent, child) = input;
            Comparer.Compare(parent, parent + "/" + child).ShouldBeLessThan(0);
        });

    [Fact]
    public void The_spec_examples_sort_as_numbers()
    {
        string[] pointers = ["/lineItems/10/total", "/lineItems/2/total", "/amount", "/lineItems/1/description", "/lineItems/1"];

        pointers.Order(Comparer).ShouldBe(["/amount", "/lineItems/1", "/lineItems/1/description", "/lineItems/2/total", "/lineItems/10/total"]);
    }

    [Fact]
    public void Keys_that_mix_digits_and_letters_still_sort_in_one_order()
    {
        // Found by the WP14 review: the old comparer gave "/2" < "/10" < "/1a" < "/2".
        string[] pointers = ["/1a", "/10", "/2", "/007", "/-x", "/7"];

        pointers.Order(Comparer).ShouldBe(["/2", "/007", "/7", "/10", "/-x", "/1a"]);
        Comparer.Compare("/2", "/1a").ShouldBeLessThan(0);
        Comparer.Compare("/10", "/1a").ShouldBeLessThan(0);
    }

    /// <summary>A pointer of 0 to 5 segments: "" is the whole document.</summary>
    private static Gen<string> Pointers() =>
        Gen.Choose(0, 5).SelectMany(count => Gen.ListOf(Segment(), count)).Select(s => string.Concat(s.Select(segment => "/" + segment)));

    private static Gen<string> Segment() => Gen.Frequency(
        (2, Gen.Choose(0, 12).Select(i => i.ToString(CultureInfo.InvariantCulture))),
        (1, Gen.Choose(0, int.MaxValue).Select(i => i.ToString(CultureInfo.InvariantCulture))),
        (3, Gen.Elements("amount", "lineItems", "total", "description", "tax", "amount_details", "line_items", "Total", "unitCost", "enhancedSchemeData.itemDetailLine1.description")),
        (2, Name()),
        (1, Gen.Elements("1a", "2b", "10x", "007", "00", "-x", " a", "~0", "")));

    private static Gen<string> Name() =>
        from first in Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray())
        from length in Gen.Choose(0, 8)
        from rest in Gen.ListOf(Gen.Elements("abcxyzAZ019_.".ToCharArray()), length)
        select first + new string([.. rest]);
}
