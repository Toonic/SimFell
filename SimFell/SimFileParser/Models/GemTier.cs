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
    public Gem Gem { get; }
    public Tier Tier { get; }
    public Stat Bonus { get; }

    public GemTier(Gem gem, Tier tier, Stat bonus)
    {
        Gem = gem;
        Tier = tier;
        Bonus = bonus;
    }

    /// <summary>
    /// Custom ToString method for the GemTier class.
    /// </summary>
    public override string ToString()
    {
        return $"[{Tier}] {Gem.Name()} giving {Bonus.GetValue()} bonus";
    }
}

public static class GemBonusMapping
{
    private static readonly Dictionary<string, Stat> gemBonusDictionary = new()
    {
        { $"{Gem.EMERALD}_{Tier.T1}", new Stat(0) }
    };

    public static Stat GetBonus(string gemTier)
    {
        return gemBonusDictionary[gemTier];
    }
}
