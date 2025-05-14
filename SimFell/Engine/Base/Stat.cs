namespace SimFell;

public class Stat
{
    //Base Value as points.
    public double BaseValue { get; set; }

    private bool _hasDiminishingReturns;
    
    private const double PointEffectiveness = 0.21; //Base effectivenes per point. (0.21%).
    private readonly double[] _breakPoints = [10.0, 15.0, 20.0, 25.0]; //Percent Threasholds for Break Points.
    private readonly double[] _breakPointMultipliers = [1, 0.9, 0.8, 0.7, 0.6];

    public Stat(double baseStat, bool hasDiminishingReturns = false)
    {
        BaseValue = baseStat;
        _hasDiminishingReturns = hasDiminishingReturns;
    }
    
    private readonly List<Modifier> _modifiers = new();
    
    public void AddModifier(Modifier modifier) => _modifiers.Add(modifier);
    public void RemoveModifier(Modifier modifier) => _modifiers.Remove(modifier);

    public double GetValue()
    {
        return GetValue(BaseValue);
    }
    public double GetValue(double inBaseValue)
    {
        //Adds the Flat Modifiers. EG: For gear.
        double raw = inBaseValue + _modifiers
            .Where(m => m.StatMod == Modifier.StatModType.Flat)
            .Sum(m => m.Value);
        
        //If it has Diminishing Returns. Calculate the Diminishing Returns.
        double value = _hasDiminishingReturns ? GetStatAsPercentage((int)raw) : raw;
        
        //Adds any Additive Percentages to the Value.
        value += _modifiers
            .Where(m => m.StatMod == Modifier.StatModType.AdditivePercent)
            .Sum(m => m.Value);
        
        //Any extra Multiplicative Percentages.
        foreach (var mod in _modifiers.Where(m => m.StatMod == Modifier.StatModType.MultiplicativePercent))
            value *= 1 + (mod.Value / 100.0);
        
        //Flat multipliers. Eg 2.0
        foreach (var mod in _modifiers.Where(m => m.StatMod == Modifier.StatModType.Multiplicative))
            value *= mod.Value;

        return value;
    }
    
    public double GetStatAsPercentage(int statPoints)
    {
        //Adds Base Percentage. EG: 5% Base Crit.
        double statPercentage = _modifiers
            .Where(m => m.StatMod == Modifier.StatModType.BasePercentage)
            .Sum(m => m.Value);

        int breakpointIndex = 0;

        for (int i = 0; i < statPoints; i++)
        {
            double effective = PointEffectiveness;

            if (breakpointIndex < _breakPoints.Length && statPercentage >= _breakPoints[breakpointIndex])
            {
                effective *= _breakPointMultipliers[breakpointIndex];
                breakpointIndex++;
            }

            statPercentage += effective;
        }

        return statPercentage;
    }

}

public class Modifier
{
    public enum StatModType { Flat, BasePercentage, AdditivePercent, MultiplicativePercent, Multiplicative }

    public StatModType StatMod { get; }
    public float Value { get; }

    public Modifier(StatModType statMod, float value)
    {
        StatMod = statMod;
        Value = value;
    }
}