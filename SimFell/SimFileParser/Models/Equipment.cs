using SimFell.SimFileParser.Enums;

namespace SimFell.SimFileParser.Models;

/// <summary>
/// Represents a piece of equipment.
/// </summary>
public class Equipment
{
    public string Name { get; set; } = string.Empty;
    public int Ilvl { get; set; }
    public Tier Tier { get; set; }
    public TierSet? TierSet { get; set; }
    public int Intellect { get; set; }
    public int Stamina { get; set; }
    public int? Expertise { get; set; }
    public int? Crit { get; set; }
    public int? Haste { get; set; }
    public int? Spirit { get; set; }
    public int? GemBonus { get; set; }
    public GemTier? Gem { get; set; }

    /// <summary>
    /// Custom ToString method for the Equipment class.
    /// </summary>
    public override string ToString()
    {
        return $@"
            Name: {Name}
            Ilvl: {Ilvl}
            Tier: {Tier}
            TierSet: {TierSet?.Name() ?? "-"}
            Intellect: {Intellect}
            Stamina: {Stamina}
            Expertise: {Expertise ?? 0}
            Crit: {Crit ?? 0}
            Haste: {Haste ?? 0}
            Spirit: {Spirit ?? 0}
            GemBonus: {GemBonus ?? 0}
            Gem: {Gem?.ToString() ?? "-"}
        ";
    }
}

/// <summary>
/// Represents a set of gear.
/// </summary>
public class Gear
{
    public Equipment? Helmet { get; set; }
    public Equipment? Shoulder { get; set; }

    // TODO: Add more gear slots

    /// <summary>
    /// Custom ToString method for the Gear class.
    /// </summary>
    public override string ToString()
    {
        return $@"
        -> Helmet: {Helmet?.ToString()}
        -> Shoulder: {Shoulder?.ToString()}
        ";
    }
}


