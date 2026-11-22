namespace L3Lint.Core;

/// <summary>
/// How strong a rule's source is. Travels with every finding so users can judge it (Card brand rules tab, "Provenance").
/// </summary>
public enum Confidence
{
    /// <summary>l3lint's own safety or input-hygiene rule, not a brand requirement.</summary>
    Policy,

    /// <summary>Stated in a document the card brand itself publishes.</summary>
    PublicBrand,

    /// <summary>Stated in public documentation from a gateway, processor or acquirer.</summary>
    ProcessorDoc,

    /// <summary>l3lint's own heuristic, consistent with public statements but not stated as a rule anywhere.</summary>
    Inferred,
}
