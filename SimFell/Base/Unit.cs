using SimFell.Logging;
namespace SimFell;

public class Unit : SimLoopListener
{
    // Base Variables
    public string Name { get; set; }
    public AdjustableStat Health { get; set; }
    public List<Aura> Buffs { get; set; } = [];
    public List<Aura> Debuffs { get; set; } = [];
    public List<Spell> SpellBook { get; set; } = [];
    public List<Talent> Talents { get; set; } = [];
    public List<Spell> Rotation { get; set; } = [];
    public Unit? PrimaryTarget { get; private set; }

    // Casting
    public bool IsCasting = false;
    private Spell? _currentSpell;
    private double _castTime;
    private double _channelTime;
    private double _tickTime;
    public List<Unit> Targets = new List<Unit>();
    public double GCD { get; private set; }

    // Baseline Stats.
    public Stat MainStat = new Stat(100);
    public Stat CritcalStrikeStat = new Stat(0, true);
    public Stat ExpertiseStat = new Stat(0, true);
    public Stat HasteStat = new Stat(0, true);
    public Stat SpiritStat = new Stat(0, true);

    //Spirit Value
    public double Spirit = 100; //TODO: Proper Spirit Regen?


    // Other Stat Buffs
    public Stat DamageBuffs = new Stat(0);
    public Stat DamageTakenDebuffs = new Stat(0);

    //Events 
    public Action<Unit, double, Spell?, Aura?>? OnDamageDealt { get; set; }
    public Action<Unit, double, Spell?, Aura?>? OnDamageReceived { get; set; }
    public Action<Unit, double, Spell?, Aura?>? OnCrit { get; set; }
    public Action<Unit, Spell, List<Unit>> OnCast { get; set; } = (unit, spellSource, targets) => { };
    public Action<Unit, Spell, List<Unit>> OnCastDone { get; set; } = (unit, spellSource, targets) => { };

    public Unit(string name, int health)
    {
        Name = name;
        Health = new AdjustableStat(health);
        //Add base 5% Crit.
        CritcalStrikeStat.AddModifier(new Modifier(Modifier.StatModType.BasePercentage, 5));
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
    /// <param name="caster"></param>
    /// /// <param name="target"></param>
    /// <param name="buff"></param>
    public void ApplyBuff(Unit caster, Unit target, Aura buff)
    {
        var existing = Buffs.Where(aura => aura.ID == buff.ID).ToList();
        if (existing.Count >= buff.MaxStacks)
            Console.WriteLine("TODO: Refresh");
        else
        {
            buff.Apply(caster, target);
            Buffs.Add(buff);
        }

        ConsoleLogger.Log(
            SimulationLogLevel.BuffEvents,
            $"[bold blue]{Name}[/] gains buff: [bold yellow]{buff.Name}[/]",
            "💪"
        );
    }

    public bool HasBuff(Aura buff)
    {
        var existing = Buffs.Where(aura => aura.ID == buff.ID).ToList();
        return existing.Count > 0;
    }

    public void RemoveBuff(Aura buff)
    {
        var existing = Buffs.Where(aura => aura.ID == buff.ID).ToList();
        foreach (var aura in existing)
        {
            ConsoleLogger.Log(
                SimulationLogLevel.BuffEvents,
                $"[bold blue]{Name}[/] loses buff: [bold yellow]{buff.Name}[/]",
                "💪🛑"
            );
            aura.Remove();
            Buffs.Remove(aura);
        }
    }

    /// <summary>
    /// Applies a debuff to the Unit and invokes OnApply.
    /// </summary>
    /// /// <param name="caster"></param>
    /// <param name="target"></param>
    /// <param name="debuff"></param>
    public void ApplyDebuff(Unit caster, Unit target, Aura debuff)
    {
        var existing = Debuffs.Where(aura => aura.ID == debuff.ID).ToList();
        if (existing.Count >= debuff.MaxStacks)
            // existing.MinBy(aura => aura.RemainingTime)?.Refresh();
            Console.WriteLine("TODO: Refresh");
        else
        {
            debuff.Apply(caster, target);
            Debuffs.Add(debuff);
        }

        ConsoleLogger.Log(
            SimulationLogLevel.DebuffEvents,
            $"[bold blue]{Name}[/] gains debuff: [bold yellow]{debuff.Name}[/]",
            "💔"
        );
    }

    /// <summary>
    /// Deals damage to the target based on the passed in Damage Percent. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="target">Target for the damage.</param>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="damageSource">Source of the damage. Usually a spell but can also be an Aura.</param>
    public void DealDamage(Unit target, double damagePercent, Spell? spellSource = null, Aura? auraSource = null)
    {
        var critPercent = CritcalStrikeStat.GetValue();

        if (spellSource != null)
        {
            damagePercent = spellSource.DamageModifiers.GetValue(damagePercent);
            critPercent = spellSource.CritModifiers.GetValue(critPercent);
        }

        //Decide if we want to buff the Auras or the Spells casting them.
        // else if (auraSource != null)
        // {
        //     damagePercent = auraSource.DamageModifiers.GetValue(damagePercent);
        //     critPercent = auraSource.CritModifiers.GetValue(critPercent);
        // }


        //Converts the DamagePercent into a Damage Value.
        var damage = (damagePercent / 100f) * MainStat.GetValue(); // Adds the Damage as Main Stat.
        damage *= 1 + (ExpertiseStat.GetValue() / 100f); // Modifies the damage based on expertise.
        damage = DamageBuffs.GetValue(damage);

        var isCritical = SimRandom.Roll(critPercent);
        isCritical = SimRandom.CanCrit ? isCritical : false;
        if (isCritical) OnCrit?.Invoke(this, damage, spellSource, auraSource); //On Crit events called.
        damage *= isCritical ? 2 : 1; //Doubles the damage if there is a Critical Hit. TODO: Crit power.

        OnDamageDealt?.Invoke(this, damage, spellSource, auraSource); //Called when damage is dealt.
        target.TakeDamage(damage, isCritical, spellSource, auraSource);
    }

    /// <summary>
    /// Called when a target takes damage. Takes into consideration any debuffs on the target, along with any extra
    /// modifiers.
    /// </summary>
    /// <param name="amount">Incoming Damage amount.</param>
    /// <param name="isCritical">If the damage was a critical hit.</param>
    /// <param name="damageSource">Source of the Damage. Usually a spell.</param>
    public void TakeDamage(double amount, bool isCritical, Spell? spellSource = null, Aura? auraSource = null)
    {
        var totalDamage = (int)DamageTakenDebuffs.GetValue(amount);
        // Log damage event with coloring for critical hits
        var sourceName = spellSource != null ? spellSource.Name
                         : auraSource != null ? auraSource.Name
                         : "Unknown";
        var message = $"[bold blue]{sourceName}[/]"
            + $" hits [bold yellow]{Name}[/]"
            + $" for [bold magenta]{totalDamage}[/] "
            + $"{(isCritical ? " (Critical Strike)" : "")}";
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, message, isCritical ? "💥" : null);

        OnDamageReceived?.Invoke(this, totalDamage, spellSource, auraSource);
        Health.BaseValue -= totalDamage;
    }

    protected override void Update()
    {
        Spirit = Math.Min(100, Spirit + (SimLoop.Instance.GetStep() * 0.2 * (1 + (SpiritStat.GetValue() / 100.0)))); //Base Spirit Regen is 0.2.
        // Update buffs
        for (int i = Buffs.Count - 1; i >= 0; i--)
        {
            Buffs[i].Update(SimLoop.Instance.GetElapsed());
            if (Buffs[i].IsExpired)
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.BuffEvents,
                    $"[bold blue]{Name}[/] loses buff: [bold yellow]{Buffs[i].Name}[/]",
                    "💪🛑"
                );
                Buffs[i].Remove();
                Buffs.RemoveAt(i);
            }
        }

        // Update debuffs
        for (int i = Debuffs.Count - 1; i >= 0; i--)
        {
            Debuffs[i].Update(SimLoop.Instance.GetElapsed());
            if (Debuffs[i].IsExpired)
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.DebuffEvents,
                    $"[bold blue]{Name}[/] loses debuff: [bold yellow]{Debuffs[i].Name}[/]",
                    "💔🛑"
                );
                Debuffs[i].Remove();
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
            if (!_currentSpell.Channel && SimLoop.Instance.GetElapsed() >= _castTime)
            {
                _currentSpell.Cast(this, Targets);
                OnCast?.Invoke(this, _currentSpell, Targets);
                StopCasting();
            }
            else if (_currentSpell.Channel)
            {
                if (SimLoop.Instance.GetElapsed() >= _tickTime)
                {
                    _currentSpell.Tick(this, Targets);
                    _tickTime = Math.Round(_tickTime + _currentSpell.GetTickRate(this), 2);
                }
                if (SimLoop.Instance.GetElapsed() >= _channelTime)
                {
                    StopCasting();
                }
            }
        }
    }

    public double GetHastedValue(double baseRate)
    {
        if (baseRate == 0) return 0;
        return baseRate / (1 + HasteStat.GetValue() / 100);
    }

    public void SetPrimaryTarget(Unit target)
    {
        PrimaryTarget = target;
    }

    public bool IsDead()
    {
        return Health.BaseValue <= 0;
    }

    public void Died()
    {
        ConsoleLogger.Log(
            SimulationLogLevel.DamageEvents,
            $"[bold blue]{Name}[/] is dead.",
            "💀"
        );

        //TODO: Future cleanup.
        Stop();
    }

    public void SetGCD(double gcd)
    {
        if (gcd != 0) ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $" -> Setting [bold blue]GCD[/] to [bold aqua]{gcd}[/]"
        );
        GCD = gcd + SimLoop.Instance.GetElapsed();
    }

    public void StartCasting(Spell spell, List<Unit> targets)
    {
        ConsoleLogger.Log(SimulationLogLevel.Debug, $"Preparation for [bold blue]{spell.Name}[/]");

        _currentSpell = spell;
        Targets = targets;
        _castTime = SimLoop.Instance.GetElapsed() + spell.GetCastTime(this);

        IsCasting = true;
        if (spell.HasGCD) SetGCD(spell.GetGCD(this));

        //Handle Channel Spells.
        if (spell.Channel)
        {
            //Channeled spells are technically instant cast.
            spell.Cast(this, targets);
            //Channeled spells always tick once at the very start.
            spell.Tick(this, targets);
            _channelTime = SimLoop.Instance.GetElapsed() + spell.GetChannelTime(this);
            _tickTime = SimLoop.Instance.GetElapsed() + spell.GetTickRate(this);
        }

        if (spell.GetCastTime(this) == 0 && spell.GetChannelTime(this) == 0)
        {
            spell.Cast(this, targets);
            OnCast?.Invoke(this, _currentSpell, Targets);
            StopCasting();
        }
    }

    public void StopCasting()
    {
        if (_currentSpell != null) OnCastDone?.Invoke(this, _currentSpell, Targets);
        IsCasting = false;
        _currentSpell = null;
    }

    public void ActivateTalent(int row, int col)
    {
        var talent = Talents.FirstOrDefault(talent => talent.GridPos == $"{row}.{col}");
        if (talent != null)
        {
            talent.Activate(this);
            ConsoleLogger.Log(SimulationLogLevel.Setup, $"Activated talent '{talent.Name}'");
        }
    }
}