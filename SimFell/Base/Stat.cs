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
    
    private readonly List<StatModifier> _modifiers = new();
    
    public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);
    public void RemoveModifier(StatModifier modifier) => _modifiers.Remove(modifier);
    public void RemoveModifier(object source) => _modifiers.RemoveAll(mod => mod.Source == source);

    public double GetValue()
    {
        double value = BaseValue;
        if (_hasDiminishingReturns)
        {
            value = GetStatAsPercentage((int)value);
        }
        return GetValue(value);
    }
    public double GetValue(double inValue)
    {
        foreach (var mod in _modifiers)
        {
            if (mod.StatMod == StatModifier.StatModType.Additive) inValue += mod.Value;
            if (mod.StatMod == StatModifier.StatModType.Multiplicative) inValue *= 1f + (mod.Value / 100f);
        }
        
        return inValue;
    }
    
    public double GetStatAsPercentage(int statPoints)
    {
        double statPercentage = _modifiers.FirstOrDefault(m => m.StatMod == StatModifier.StatModType.BasePercentage)?.Value ?? 0.0;
        int breakpointIndex = 0;

        for (int i = 0; i < statPoints; i++)
        {
            double effectiveIncrease = PointEffectiveness;

            if (breakpointIndex < _breakPoints.Length && statPercentage >= _breakPoints[breakpointIndex])
            {
                effectiveIncrease *= _breakPointMultipliers[breakpointIndex];
                breakpointIndex++;
            }

            statPercentage += effectiveIncrease;
        }

        return statPercentage;
    }

}

public class StatModifier
{
    public enum StatModType { Additive, Multiplicative, BasePercentage }

    public StatModType StatMod { get; }
    public float Value { get; }
    public object Source { get; }

    public StatModifier(StatModType statMod, float value, object source)
    {
        StatMod = statMod;
        Value = value;
        Source = source;
    }
}