using SimFell.Logging;
namespace SimFell;

public class Unit(
    string name,
    int health = 100
)
    : SimLoopListener
{
    // Base Variables
    public string Name { get; set; } = name;
    public int Health { get; set; } = health;
    public List<Aura> Buffs { get; set; } = [];
    public List<Aura> Debuffs { get; set; } = [];
    public List<Spell> SpellBook { get; set; } = [];
    public Unit? PrimaryTarget { get; set; }

    // Baseline stats are always flat 100. As point values.
    private int _mainStat = 100;
    private int _critcalStrikeStat = 100;
    private int _expertiseStat = 100;
    private int _hasteStat = 100;
    private int _spiritStat = 100;

    // Multipliers
    public double DamageReceivedMultiplier { get; set; } = 1.0f;

    //Const.
    private const double PointEffectiveness = 0.21; //Base effectivenes per point. (0.21%).
    private readonly double[] _breakPoints = [10.0, 15.0, 20.0, 25.0]; //Percent Threasholds for Break Points.
    private readonly double[] _breakPointMultipliers = [1, 0.9, 0.8, 0.7, 0.6];

    //Events
    public Action<Unit, float, object> OnDamageReceived { get; set; } = (unit, damage, source) => { };

    public void SetPrimaryStats(int mainStat, int criticalStrikeStat, int expertiseStat, int hasteStat, int spiritStat)
    {
        _mainStat = mainStat;
        _critcalStrikeStat = criticalStrikeStat;
        _expertiseStat = expertiseStat;
        _hasteStat = hasteStat;
        _spiritStat = spiritStat;
    }

    /// <summary>
    /// Returns the Main Stat with modifiers.
    /// </summary>
    /// <returns></returns>
    private float GetMainStat()
    {
        return _mainStat;
    }

    private float GetStatAsPercentage(int statPoints)
    {
        double statPercentage = 0;
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

        return (float)statPercentage;
    }

    /// <summary>
    /// Returns the current Critical Strike Stat, including modifiers.
    /// </summary>
    /// <returns>As percentage.</returns>
    private float GetCriticalStrikeStat()
    {
        float stat = GetStatAsPercentage(_critcalStrikeStat);
        stat += 5; //Base 5% Critical Strike chance for everyone.
        return stat;
    }

    /// <summary>
    /// Returns the current Expertise Stat, including modifiers.
    /// </summary>
    /// <returns>As percentage.</returns>
    private float GetExpertiseStat()
    {
        float stat = GetStatAsPercentage(_expertiseStat);
        return stat;
    }

    /// <summary>
    /// Applies a buff to the Unit and invokes OnApply.
    /// </summary>
    /// <param name="buff"></param>
    public void ApplyBuff(Aura buff)
    {
        Buffs.Add(buff);
        Logger.SimulationEvent($"{Name} gains buff: {buff.Name}", "💪");
        buff.OnApply?.Invoke(this);
    }

    /// <summary>
    /// Applies a debuff to the Unit and invokes OnApply.
    /// </summary>
    /// <param name="debuff"></param>
    public void ApplyDebuff(Aura debuff)
    {
        Debuffs.Add(debuff);
        Logger.SimulationEvent($"{Name} gains debuff: {debuff.Name}", "💔");
        debuff.OnApply?.Invoke(this);
    }

    /// <summary>
    /// Deals damage to the target based on the passed in Damage Percent. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="target">Target for the damage.</param>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="damageSource">Source of the damage. (EG: Spell, Aura, etc.) Used mostly for debugging.</param>
    public void DealDamage(Unit target, float damagePercent, object damageSource)
    {
        var damage = (damagePercent / 100f) * GetMainStat(); // Adds the Damage as Main Stat.
        damage *= 1 + (GetExpertiseStat() / 100f); // Modifies the damage based on expertise.
        var isCritical = SimRandom.Roll(GetCriticalStrikeStat());
        damage *= isCritical ? 2 : 1; //Doubles the damage if there is a Critical Hit.

        target.TakeDamage(damage, isCritical, damageSource);
    }

    /// <summary>
    /// Called when a target takes damage. Takes into consideration any debuffs on the target, along with any extra
    /// modifiers.
    /// </summary>
    /// <param name="amount">Incoming Damage amount.</param>
    /// <param name="isCritical">If the damage was a critical hit.</param>
    /// <param name="damageSource">Source of the Damage.</param>
    public void TakeDamage(float amount, bool isCritical, object damageSource)
    {
        var totalDamage = (int)(amount * DamageReceivedMultiplier);

        // Log damage event with coloring for critical hits
        var sourceName = damageSource is Spell spell ? spell.Name
                         : damageSource is Aura aura ? aura.Name
                         : "Unknown";
        var message = $"{sourceName} hits {Name} for {totalDamage}.{(isCritical ? " (Critical Strike)" : "")}";
        Logger.SimulationEvent(message, isCritical ? "💥" : null);

        OnDamageReceived?.Invoke(this, totalDamage, damageSource);
        Health -= totalDamage;
    }

    protected override void Update(double deltaTime)
    {
        // Update buffs
        for (int i = Buffs.Count - 1; i >= 0; i--)
        {
            Buffs[i].Update(deltaTime, this);
            if (Buffs[i].IsExpired)
            {
                Logger.SimulationEvent($"{Name} loses buff: {Buffs[i].Name}", "💪🛑");
                Buffs[i].OnRemove?.Invoke(this);
                Buffs.RemoveAt(i);
            }
        }

        // Update debuffs
        for (int i = Debuffs.Count - 1; i >= 0; i--)
        {
            Debuffs[i].Update(deltaTime, this);
            if (Debuffs[i].IsExpired)
            {
                Logger.SimulationEvent($"{Name} loses debuff: {Debuffs[i].Name}", "💔🛑");
                Debuffs[i].OnRemove?.Invoke(this);
                Debuffs.RemoveAt(i);
            }
        }

        foreach (var spell in SpellBook)
        {
            spell.UpdateCooldown(deltaTime);
        }
    }

    public void SetPrimaryTarget(Unit target)
    {
        PrimaryTarget = target;
    }

    public bool IsDead()
    {
        return Health <= 0;
    }

    public void Died()
    {
        Logger.SimulationEvent($"{Name} is dead.", "💀");
        //TODO: Future cleanup.
        Stop();
    }
}