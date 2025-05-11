using SimFell.Logging;
namespace SimFell;

public class Unit : SimLoopListener
{
    // Base Variables
    public string Name { get; set; }
    public int Health { get; set; }
    public List<Aura> Buffs { get; set; } = [];
    public List<Aura> Debuffs { get; set; } = [];
    public List<Spell> SpellBook { get; set; } = [];
    public Unit? PrimaryTarget { get; private set; }

    // Casting
    public bool IsCasting = false;
    private Spell? _currentSpell;
    private double _castTime;
    private double _tickTime;
    private List<Unit> _targets = new List<Unit>();
    public double GCD { get; private set; }

    // Baseline stats are always flat 100. As point values.
    public Stat MainStat = new Stat(100);
    public Stat CritcalStrikeStat = new Stat(0, true);
    public Stat ExpertiseStat = new Stat(0, true);
    public Stat HasteStat = new Stat(0, true);
    public Stat SpiritStat = new Stat(0, true);

    // Other Stat Buffs
    public Stat DamageBuffs = new Stat(0);
    public Stat DamageTakenDebuffs = new Stat(0);

    //Events
    public Action<Unit, float, object> OnDamageReceived { get; set; } = (unit, damage, source) => { };

    public Unit(string name, int health)
    {
        Name = name;
        Health = health;
        //Add base 5% Crit.
        CritcalStrikeStat.AddModifier(new StatModifier(StatModifier.StatModType.BasePercentage, 5, this));
    }

    public Unit(string name, int health, int mainStat, int critcalStrikeStat, int expertiseStat, int hasteStat,
        int spiritStat) : this(name, health)
    {
        SetPrimaryStats(mainStat, critcalStrikeStat, expertiseStat, hasteStat, spiritStat);
    }

    public void SetPrimaryStats(int mainStat, int criticalStrikeStat, int expertiseStat, int hasteStat, int spiritStat)
    {
        MainStat.BaseValue = mainStat;
        CritcalStrikeStat.BaseValue = criticalStrikeStat;
        ExpertiseStat.BaseValue = expertiseStat;
        HasteStat.BaseValue = hasteStat;
        SpiritStat.BaseValue = spiritStat;
    }

    /// <summary>
    /// Applies a buff to the Unit and invokes OnApply.
    /// </summary>
    /// <param name="buff"></param>
    public void ApplyBuff(Aura buff)
    {
        var existing = Buffs.Where(aura => aura.ID == buff.ID).ToList();
        if (existing.Count >= buff.MaxStacks)
            Console.WriteLine("TODO: Refresh");
        else
        {
            buff.Apply(this);
            Buffs.Add(buff);
        }

        ConsoleLogger.Log(
            SimulationLogLevel.BuffEvents,
            $"\u001b[1;34m{Name}\u001b[0;30m gains buff: \u001b[1;33m{buff.Name}\u001b[0;30m",
            "💪"
        );
    }

    /// <summary>
    /// Applies a debuff to the Unit and invokes OnApply.
    /// </summary>
    /// <param name="debuff"></param>
    public void ApplyDebuff(Aura debuff)
    {
        var existing = Debuffs.Where(aura => aura.ID == debuff.ID).ToList();
        if (existing.Count >= debuff.MaxStacks)
            // existing.MinBy(aura => aura.RemainingTime)?.Refresh();
            Console.WriteLine("TODO: Refresh");
        else
        {
            debuff.Apply(this);
            Debuffs.Add(debuff);
        }

        ConsoleLogger.Log(
            SimulationLogLevel.DebuffEvents,
            $"\u001b[1;34m{Name}\u001b[0;30m gains debuff: \u001b[1;33m{debuff.Name}\u001b[0;30m",
            "💔"
        );
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
        var damage = (damagePercent / 100f) * MainStat.GetValue(); // Adds the Damage as Main Stat.
        damage *= 1 + (ExpertiseStat.GetValue() / 100f); // Modifies the damage based on expertise.
        damage = DamageBuffs.GetValue(damage);

        var isCritical = SimRandom.Roll(CritcalStrikeStat.GetValue());
        isCritical = SimRandom.Deterministic ? false : isCritical; //TODO: Remove this/make it another setting.
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
        var totalDamage = (int)DamageTakenDebuffs.GetValue(amount);
        // Log damage event with coloring for critical hits
        var sourceName = damageSource is Spell spell ? spell.Name
                         : damageSource is Aura aura ? aura.Name
                         : "Unknown";
        var message = $"\u001b[1;34m{sourceName}\u001b[0;30m"
            + $" hits \u001b[1;33m{Name}\u001b[0;30m"
            + $" for \u001b[1;35m{totalDamage}\u001b[0;30m "
            + $"{(isCritical ? " (Critical Strike)" : "")}";
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, message, isCritical ? "💥" : null);

        OnDamageReceived?.Invoke(this, totalDamage, damageSource);
        Health -= totalDamage;
    }

    protected override void Update()
    {
        // Update buffs
        for (int i = Buffs.Count - 1; i >= 0; i--)
        {
            Buffs[i].Update(SimLoop.Instance.GetElapsed(), this);
            if (Buffs[i].IsExpired)
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.BuffEvents,
                    $"\u001b[1;34m{Name}\u001b[0;30m loses buff: \u001b[1;33m{Buffs[i].Name}\u001b[0;30m",
                    "💪🛑"
                );
                Buffs[i].OnRemove?.Invoke(this);
                Buffs.RemoveAt(i);
            }
        }

        // Update debuffs
        for (int i = Debuffs.Count - 1; i >= 0; i--)
        {
            Debuffs[i].Update(SimLoop.Instance.GetElapsed(), this);
            if (Debuffs[i].IsExpired)
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.DebuffEvents,
                    $"\u001b[1;34m{Name}\u001b[0;30m loses debuff: \u001b[1;33m{Debuffs[i].Name}\u001b[0;30m",
                    "💔🛑"
                );
                Debuffs[i].OnRemove?.Invoke(this);
                Debuffs.RemoveAt(i);
            }
        }

        //Update the GCD for the Unit.
        // GCD = Math.Max(0, GCD - deltaTime);
        // if (GCD > 0) ConsoleLogger.Log(SimulationLogLevel.Debug, $"GCD in Update: {GCD}");

        // Updates Casting.
        if (IsCasting && _currentSpell != null)
        {
            //If the casting is done.
            if (SimLoop.Instance.GetElapsed() >= _castTime)
            {
                _currentSpell.Cast(this, _targets);
                StopCasting();
            }
            //TODO: Handle Tick Events.
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
        ConsoleLogger.Log(
            SimulationLogLevel.DamageEvents,
            $"\u001b[1;34m{Name}\u001b[0;30m is dead.",
            "💀"
        );

        //TODO: Future cleanup.
        Stop();
    }

    public void SetGCD(double gcd)
    {
        if (gcd != 0) ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"\u001b[1;34mGCD\u001b[0;30m: \u001b[1;36m{gcd}\u001b[0;30m"
        );
        GCD = gcd + SimLoop.Instance.GetElapsed();
    }

    public void StartCasting(Spell spell, List<Unit> targets)
    {
        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Casting \u001b[1;34m{spell.Name}\u001b[0;30m"
        );

        _currentSpell = spell;
        _targets = targets;
        _castTime = SimLoop.Instance.GetElapsed() + spell.GetCastTime(this);
        //TODO: Channel.
        IsCasting = true;
        if (spell.HasGCD) SetGCD(spell.GetGCD(this));
    }

    public void StopCasting()
    {
        IsCasting = false;
        _currentSpell = null;
    }
}