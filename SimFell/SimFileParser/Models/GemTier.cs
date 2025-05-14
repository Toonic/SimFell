using SimFell.SimFileParser.Enums;

namespace SimFell.SimFileParser.Models;

/// <summary>
/// Represents a gem tier.
/// </summary>
/// <remarks>
/// For example usage see <see cref="Equipment.Gem"/>
/// and how to access the <see cref="EnumExtensions.Name"/> extension method.
/// </remarks>
public class GemTier
{
    public GemType Gem { get; set; }
    public Tier Tier { get; set; }
    public int Power { get; set; }

    /// <summary>
    /// Custom ToString method for the GemTier class.
    /// </summary>
    public override string ToString() => $"{Gem.Name()} {Tier} ({Power} power)";
}
