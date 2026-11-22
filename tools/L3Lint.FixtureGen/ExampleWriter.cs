namespace L3Lint.FixtureGen;

/// <summary>Writes each rule's pass and fail example into its pack's examples folder.</summary>
internal static class ExampleWriter
{
    public static void Write(FileSetWriter writer)
    {
        foreach (var definition in RuleExampleCatalog.All)
        {
            var folder = $"src/L3Lint.Rules.{definition.Pack}/Pack/examples/";
            writer.WriteJson(folder + definition.RuleId + ".pass.json", definition.Pass());
            writer.WriteJson(folder + definition.RuleId + ".fail.json", definition.Fail());
        }

        writer.WriteText("src/L3Lint.Rules.Visa/Pack/examples/VISA-LT-001.rates.yaml", RuleExampleCatalog.LargeTicketRates + "\n");
    }
}
