using Newtonsoft.Json.Linq;
using System;

namespace SimFell;

public class Stat
{
    //Base Value as points.
    public double BaseValue;
    private bool _hasDiminishingReturns;

    private static readonly double PointEffectiveness = 0.017; //Base effectiveness per point. (0.17%).

    // Percent Thresholdst for Break Points. Please make sure the first entry is 0.
    private static readonly double[] _breakPoints = [0.0, 10.0, 15.0, 20.0, 25.0];

    // Breakpoint Multipliers Please make sure there is an additional 1 entry for the 0->firstBreakpoint.
    private static readonly double[] _breakPointMultipliers = [1, 1, 0.95, 0.9, 0.85, 0.8];

    private static readonly double[] _breakPointRatingsPartial =
    [
        .. _breakPoints.Select((x, index) =>
            (index > 0 ? x - _breakPoints[index - 1] : x) / _breakPointMultipliers[index] / PointEffectiveness)
    ];

    private static readonly double[] _breakPointRatings =
        [.. _breakPointRatingsPartial.Select((x, index) => _breakPointRatingsPartial.Take(index + 1).Sum())];

    // Caching fields
    public Action<Stat> OnInvalidate { get; set; } = (Stat) => { };
    private bool _cachedResultValid = false;
    private double _cachedBaseValue = double.NaN;
    private double _cachedResult = 0.0;
    private bool _dynamicUseResultCache = false;

    class CacheValues
    {
        public double FlatSum { get; set; } = 0.0;
        public double AdditivePercentSum { get; set; } = 0.0;
        public double MultiplicativePercent { get; set; } = 1.0;
        public double Multiplicative { get; set; } = 1.0;
        public bool Valid { get; set; } = false;
        public double InverseMultiplicativePercent { get; set; } = 1.0;
    }

    // Pre-calculated modifier sums for performance.
    private CacheValues _staticCache = new CacheValues();

    // Duplicate for dynamic for convenient data structure and re-use of code.
    private CacheValues _dynamicCache = new CacheValues();

    public Stat(double baseStat, bool hasDiminishingReturns = false)
    {
        BaseValue = baseStat;
        _hasDiminishingReturns = hasDiminishingReturns;
        InvalidateCache();
    }

    protected readonly List<Modifier> _modifiers = new();
    protected readonly List<Modifier> _dynamicModifiers = new();


    public void AddModifier(Modifier modifier)
    {
        if (modifier is DynamicModifier)
        {
            _dynamicModifiers.Add(modifier);
            InvalidateDynamicCache();
        }
        else
        {
            _modifiers.Add(modifier);
            InvalidateStaticCache();
        }

        OnModifierAdded?.Invoke();
    }

    public void RemoveModifier(Modifier modifier)
    {
        if (modifier is DynamicModifier)
        {
            _dynamicModifiers.Remove(modifier);
            InvalidateDynamicCache();
        }
        else
        {
            _modifiers.Remove(modifier);
            InvalidateStaticCache();
        }

        OnModifierRemoved?.Invoke();
    }

    public void InvalidateDynamicCache(bool triggerEvents = true)
    {
        _dynamicCache.Valid = false;
        _cachedResultValid = false;
        if (triggerEvents)
        {
            OnInvalidate?.Invoke(this);
        }
    }

    public void InvalidateStaticCache(bool triggerEvents = true)
    {
        _staticCache.Valid = false;
        _cachedResultValid = false;
        if (triggerEvents)
        {
            OnInvalidate?.Invoke(this);
        }
    }

    public void InvalidateCache()
    {
        InvalidateStaticCache(false);
        InvalidateDynamicCache(false);
        OnInvalidate?.Invoke(this);
    }

    private void RecalculateModifierCache(ref CacheValues cache, List<Modifier> modifiers)
    {
        if (cache.Valid)
            return;

        cache.FlatSum = 0.0;
        cache.AdditivePercentSum = 0.0;
        cache.MultiplicativePercent = 1.0;
        cache.Multiplicative = 1.0;

        foreach (var modifier in modifiers)
        {
            switch (modifier.StatMod)
            {
                case Modifier.StatModType.Flat:
                    cache.FlatSum += modifier.Value;
                    break;
                case Modifier.StatModType.AdditivePercent:
                    cache.AdditivePercentSum += modifier.Value;
                    break;
                case Modifier.StatModType.MultiplicativePercent:
                    cache.MultiplicativePercent *= 1 + modifier.Value / 100.0d;
                    break;
                case Modifier.StatModType.Multiplicative:
                    cache.Multiplicative *= modifier.Value;
                    break;
                case Modifier.StatModType.InverseMultiplicativePercent:
                    cache.InverseMultiplicativePercent /= 1 + modifier.Value / 100.0;
                    break;
            }
        }

        cache.Valid = true;
    }

    private void RecalculateModifierCache(bool force = false)
    {
        if (_dynamicModifiers.Count > 0)
        {
            if (force)
                _dynamicCache.Valid = false;

            RecalculateModifierCache(ref _dynamicCache, _dynamicModifiers);

            // We do not want to cache this unless specifically instructed to.
            if (!_dynamicUseResultCache)
                _dynamicCache.Valid = false;
        }

        if (force)
            _staticCache.Valid = false;

        RecalculateModifierCache(ref _staticCache, _modifiers);
    }

    public event Action? OnModifierAdded;
    public event Action? OnModifierRemoved;

    /// <summary>
    /// Allows Dynamic Modifiers to be cached in the ResultCache.
    /// </summary>
    public Stat SetDynamicUseResultCache()
    {
        _dynamicUseResultCache = true;
        return this;
    }

    public double GetValue()
    {
        return GetValue(BaseValue);
    }

    public double GetValue(double inBaseValue)
    {
        // Check if we can use cached result
        if (_cachedResultValid && _cachedBaseValue == inBaseValue &&
            (_dynamicModifiers.Count == 0 || _dynamicUseResultCache))
        {
            return _cachedResult;
        }

        // Recalculate Modifier Cache if needed. Function will check if it is needed.
        RecalculateModifierCache();

        // Calculate the value
        double raw = inBaseValue + _staticCache.FlatSum + _dynamicCache.FlatSum;

        // If it has Diminishing Returns, calculate the Diminishing Returns.
        double value = _hasDiminishingReturns ? GetStatAsPercentage((int)raw) : raw;

        // Add any Additive Percentages to the Value.
        value += _staticCache.AdditivePercentSum + _dynamicCache.AdditivePercentSum;

        // Any extra Multiplicative Percentages.
        value *= _staticCache.MultiplicativePercent * _dynamicCache.MultiplicativePercent;

        // Flat multipliers. Eg 2.0
        value *= _staticCache.Multiplicative * _dynamicCache.Multiplicative;

        // Apply inverse multiplicative percentages (for faster)
        value *= _staticCache.InverseMultiplicativePercent * _dynamicCache.InverseMultiplicativePercent;

        _cachedResult = value;
        _cachedBaseValue = inBaseValue;
        _cachedResultValid = true;

        return value;
    }

    // Cache for diminishing returns calculation
    private readonly Dictionary<int, double> _percentageCache = new();

    public double GetStatAsPercentage(int statPoints)
    {
        if (_percentageCache.TryGetValue(statPoints, out double cachedPercentage))
        {
            return cachedPercentage;
        }

        for (int i = 1; i < _breakPointRatings.Length; i++)
        {
            if (statPoints > _breakPointRatings[i])
                continue;

            if (statPoints == _breakPointRatings[i])
            {
                _percentageCache[statPoints] = _breakPoints[i];
                return _percentageCache[statPoints];
            }

            // Lerp
            _percentageCache[statPoints] = _breakPoints[i - 1] + (_breakPoints[i] - _breakPoints[i - 1]) *
                (statPoints - _breakPointRatings[i - 1]) / _breakPointRatingsPartial[i];

            return _percentageCache[statPoints];
        }

        // Lerp with Infinity
        _percentageCache[statPoints] = _breakPoints.Last() + (statPoints - _breakPointRatings.Last()) *
            PointEffectiveness * _breakPointMultipliers.Last();
        return _percentageCache[statPoints];
    }

    public bool HasModifier(Modifier modifier) => _modifiers.Contains(modifier);

    // Method to clear caches if needed (useful for memory management in long-running simulations)
    public void ClearCache()
    {
        InvalidateCache();
        _percentageCache.Clear();
    }
}

public class HealthStat : Stat
{
    public double MaximumValue;

    // Cache for max value calculation
    private bool _maxValueCacheValid = false;
    private double _cachedMaxValue = 0.0;

    public HealthStat(double baseStat, bool hasDiminishingReturns = false) : base(baseStat, hasDiminishingReturns)
    {
        MaximumValue = baseStat;
        OnModifierAdded += InvalidateMaxValueCache;
        OnModifierRemoved += InvalidateMaxValueCache;
    }

    private void InvalidateMaxValueCache()
    {
        _maxValueCacheValid = false;
    }

    public double GetMaxValue()
    {
        if (_maxValueCacheValid)
        {
            return _cachedMaxValue;
        }

        // Check for dynamic modifiers
        bool hasDynamicModifiers = _dynamicModifiers.Count > 0;

        _cachedMaxValue = GetValue(MaximumValue);

        // Only cache if no dynamic modifiers
        if (!hasDynamicModifiers)
        {
            _maxValueCacheValid = true;
        }

        return _cachedMaxValue;
    }
}

public class Modifier
{
    public enum StatModType
    {
        /// <summary>
        /// Modifies the value by a flat amount. (EG: Reduce -0.5s Cast Time)
        /// </summary>
        Flat,

        /// <summary>
        /// Modifies the value by a flat percentage. (EG: Additional +25% Crit Chance)
        /// </summary>
        AdditivePercent,

        /// <summary>
        /// Modifies the Value by a percentage. (EG: 20% More Damage.)
        /// </summary>
        MultiplicativePercent,

        /// <summary>
        /// Modifies the final Value via a given Multiplier. (EG: 2x Cast Time)
        /// </summary>
        Multiplicative,

        /// <summary>
        /// Special modifier that decreases the interval for “faster” effects.
        /// Value is the percent increase speed. (e.g., 40 = 40% faster ticks)
        /// </summary>
        InverseMultiplicativePercent
    }

    public StatModType StatMod { get; }
    public virtual double Value { get; set; }

    public Modifier(StatModType statMod, double value)
    {
        StatMod = statMod;
        Value = value;
    }
}

public class DynamicModifier : Modifier
{
    private Func<double> ValueCallback { get; }

    public override double Value
    {
        get => ValueCallback.Invoke();
    }

    public DynamicModifier(StatModType statMod, Func<double> callback) : base(statMod, 0d)
    {
        ValueCallback = callback;
    }
}