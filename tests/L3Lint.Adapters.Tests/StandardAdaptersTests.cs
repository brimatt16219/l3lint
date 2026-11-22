using L3Lint.Adapters.Json;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Tests;

/// <summary>The shipped adapter list and shared tables.</summary>
public sealed class StandardAdaptersTests
{
    [Fact]
    public void All_lists_the_seven_adapters_in_order()
    {
        StandardAdapters.All.Select(a => a.Name).ShouldBe(["stripe", "adyen", "braintree", "cybersource", "authorizenet", "checkoutcom", "csv"]);
        StandardAdapters.All.Select(a => a.Kind).ShouldBe([InputKind.Json, InputKind.Json, InputKind.Json, InputKind.Json, InputKind.Json, InputKind.Json, InputKind.Csv]);
        StandardAdapters.All.ShouldAllBe(a => a.GetType().IsSealed && a.GetType().IsPublic);
    }

    [Fact]
    public void Adapters_hold_no_instance_state()
    {
        foreach (var adapter in StandardAdapters.All)
        {
            adapter.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .ShouldBeEmpty(adapter.Name);
        }
    }

    [Theory]
    [InlineData("USA", "US")]
    [InlineData("CAN", "CA")]
    [InlineData("GBR", "GB")]
    [InlineData("DEU", "DE")]
    [InlineData("JPN", "JP")]
    [InlineData("BHR", "BH")]
    [InlineData("MEX", "MX")]
    public void Country_table_converts_alpha3_to_alpha2_and_back(string alpha3, string alpha2)
    {
        CountryCodes.TryGetAlpha2(alpha3, out var two).ShouldBeTrue();
        two.ShouldBe(alpha2);
        CountryCodes.TryGetAlpha3(alpha2, out var three).ShouldBeTrue();
        three.ShouldBe(alpha3);
    }

    [Fact]
    public void Country_table_holds_the_249_ISO_3166_codes() => CountryCodes.Count.ShouldBe(249);

    [Fact]
    public void Country_table_is_case_sensitive() => CountryCodes.TryGetAlpha2("usa", out _).ShouldBeFalse();
}
