namespace SimFell.Engine.Base;

/// <summary>
/// A data object to store the simulation results for a single spell.
/// </summary>
public class SpellStats
{
    public string SpellName { get; init; } = string.Empty;
    public double TotalDamage { get; set; }
    public int Casts { get; set; }
    public int Ticks { get; set; } // Represents hits for instant/auto-attacks, ticks for DoTs/channels
    public double LargestHit { get; set; } = 0;
    public double SmallestHit { get; set; } = double.MaxValue;
    public int CritCount { get; set; } = 0;
}