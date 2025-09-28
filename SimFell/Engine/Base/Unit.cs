using SimFell.Logging;
using SimFell.SimmyRewrite;

namespace SimFell;

public class Unit : SimLoopListener
{
    // Base Variables
    public string Name { get; set; }
    public HealthStat Health { get; set; }
    private bool HasInfiniteHp { get; set; }
    public Stat Stamina = new Stat(0);
    public List<Aura> Buffs { get; set; } = [];
    public List<Aura> Debuffs { get; set; } = [];
    public List<Spell> SpellBook { get; set; } = [];
    public List<Talent> Talents { get; set; } = [];
    public List<Spell> Rotation { get; set; } = [];
    public Unit? PrimaryTarget { get; private set; }

    public List<SimAction> SimActions { get; set; } = new List<SimAction>();

    // Casting
    public bool IsCasting = false;
    private Spell? _currentSpell;
    private double _castTime;
    private double _channelTime;
    private double _tickTime;
    public List<Unit> Targets = new List<Unit>();
    public double GCD { get; private set; }

    // Baseline Stats.
    public Stat MainStat = new Stat(1000);
    public Stat CritcalStrikeStat = new Stat(0, true);
    public Stat ExpertiseStat = new Stat(0, true);
    public Stat HasteStat = new Stat(0, true);
    public Stat SpiritStat = new Stat(0, true);

    //Critical Strike Power
    public Stat CriticalStrikePowerStat = new Stat(0);

    //Spirit Value
    public double Spirit = 100; //TODO: Proper Spirit Regen?

    static Modifier spiritOfHeroismMod = new Modifier(Modifier.StatModType.AdditivePercent, 30);

    public Aura SpiritOfHeroism = new Aura(
        id: "spirit-of-heroism",
        name: "Spirit of Heroism",
        duration: 20,
        tickInterval: 0,
        onApply: (unit, target) => { unit.HasteStat.AddModifier(spiritOfHeroismMod); },
        onRemove: (unit, target) => { unit.HasteStat.RemoveModifier(spiritOfHeroismMod); }
    );


    // Other Stat Buffs
    public Stat DamageBuffs = new Stat(0);
    public Stat DamageTakenDebuffs = new Stat(0);

    //Events 
    public Action<Unit, double, Spell?>? OnDamageDealt { get; set; }
    public Action<Unit, double, Spell?, bool>? OnDamageReceived { get; set; }
    public Action<Unit, double, Spell?>? OnCrit { get; set; }
    public Action<Unit, Spell, List<Unit>> OnCast { get; set; } = (unit, spellSource, targets) => { };
    public Action<Unit, Spell, List<Unit>> OnCastDone { get; set; } = (unit, spellSource, targets) => { };

    // On Health Updated event
    public event Action? OnHealthUpdated;

    public Unit ShallowCopy()
    {
        return (Unit)this.MemberwiseClone();
    }

    public Unit(string name, bool hasInfiniteHp = false)
    {
        Name = name;
        Stamina = new Stat(999999);
        Health = new HealthStat(Stamina.GetValue());
        HasInfiniteHp = hasInfiniteHp;

        //Add base 5% Crit.
        CritcalStrikeStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 5));

        // Subscribe to Stat Modifiers.
        Stamina.OnModifierAdded += UpdateHealthFromStamina;
        Stamina.OnModifierRemoved += UpdateHealthFromStamina;
    }

    private void UpdateHealthFromStamina()
    {
        ConsoleLogger.Log(SimulationLogLevel.Debug, $"Updating health from stamina");

        double oldMax = Health.GetMaxValue();
        double newMax = Stamina.GetValue();

        // Adjust current health proportionally
        if (oldMax > 0)
        {
            Health.BaseValue = Health.GetValue() / oldMax * newMax;
            Health.MaximumValue = Health.GetMaxValue() / oldMax * newMax;
        }
        else
        {
            Health.BaseValue = newMax;
            Health.MaximumValue = newMax;
        }

        OnHealthUpdated?.Invoke();

        ConsoleLogger.Log(SimulationLogLevel.Debug,
            $"New health: {Health.GetValue()} | Max Health: {Health.GetMaxValue()}");
    }

    public Unit(string name, int health, int mainStat, int critcalStrikeStat, int expertiseStat, int hasteStat,
        int spiritStat) : this(name)
    {
        SetPrimaryStats(mainStat, critcalStrikeStat, expertiseStat, hasteStat, spiritStat);
    }

    public virtual void SetPrimaryStats(int mainStat, int criticalStrikeStat, int expertiseStat, int hasteStat,
        int spiritStat, bool isPercentile = false)
    {
        MainStat.BaseValue = mainStat;
        if (!isPercentile)
        {
            CritcalStrikeStat.BaseValue = criticalStrikeStat;
            ExpertiseStat.BaseValue = expertiseStat;
            HasteStat.BaseValue = hasteStat;
            SpiritStat.BaseValue = spiritStat;
        }
        else
        {
            CritcalStrikeStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, criticalStrikeStat - 5));
            ExpertiseStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, expertiseStat));
            HasteStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, hasteStat));
            SpiritStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, spiritStat));
        }
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
            Console.WriteLine("TODO: Refresh?");
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

    public bool HasDebuff(Aura buff)
    {
        var existing = Debuffs.Where(aura => aura.ID == buff.ID).ToList();
        return existing.Count > 0;
    }

    /// <summary>
    /// Deals damage to the primary/first target based on the passed in Damage Percent. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="damageSource">Source of the damage. Usually a spell but can also be an Aura.</param>
    public void DealDamage(double damagePercent, Spell? spellSource = null)
    {
        var target = Targets.FirstOrDefault()
                     ?? throw new Exception("No valid targets");
        DealDamage(target, damagePercent, spellSource);
    }

    /// <summary>
    /// Deals damage to the target based on the passed in Damage Percent. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="target">Target for the damage.</param>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="damageSource">Source of the damage. Usually a spell but can also be an Aura.</param>
    public void DealDamage(Unit target, double damagePercent, Spell? spellSource = null)
    {
        // TODO: Remove this when I start to use values from the game (EG: Proper percentages)
        damagePercent = damagePercent / 1000.0;

        var critPercent = CritcalStrikeStat.GetValue();

        if (spellSource != null)
        {
            damagePercent = spellSource.DamageModifiers.GetValue(damagePercent);
            critPercent = spellSource.CritModifiers.GetValue(critPercent);
        }

        Modifier grievousCritsModifier = new Modifier(Modifier.StatModType.AdditivePercent, 0);
        if (critPercent > 100.0)
        {
            CriticalStrikePowerStat.AddModifier(grievousCritsModifier);
        }

        //Converts the DamagePercent into a Damage Value.
        var damage = damagePercent * MainStat.GetValue(); // Adds the Damage as Main Stat.
        damage *= 1 + (ExpertiseStat.GetValue() / 100f); // Modifies the damage based on expertise.
        damage = DamageBuffs.GetValue(damage);

        var isCritical = SimRandom.Roll(critPercent);
        isCritical = SimRandom.CanCrit ? isCritical : false;
        //Handle Crit Power.
        if (isCritical) damage = CriticalStrikePowerStat.GetValue(damage);
        CriticalStrikePowerStat.RemoveModifier(grievousCritsModifier);
        //Handle general On Crit.
        if (isCritical) OnCrit?.Invoke(this, damage, spellSource); //On Crit events called.
        damage *= isCritical ? 2 : 1; //Doubles the damage if there is a Critical Hit. TODO: Crit power.

        var damageDealtAfterMods = target.TakeDamage(damage, isCritical, spellSource);
        OnDamageDealt?.Invoke(this, damageDealtAfterMods, spellSource); //Called when damage is dealt.
    }

    /// <summary>
    /// Deals damage to all targets. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="targetCap">Target cap before AOE Damage Scaling happens.</param>
    /// <param name="includePrimaryTarget">Includes the Primary Target when dealing AOE. Disable when doing splash damage.</param>
    /// <param name="damageSource">Source of the damage. Usually a spell but can also be an Aura.</param>
    public void DealAOEDamage(double damagePercent, double targetCap, Spell? spellSource = null,
        bool includePrimaryTarget = true)
    {
        //Gets the list of targets, skips the first one if includePrimaryTarget is False.
        var affectedTargets = includePrimaryTarget ? Targets : Targets.Skip(1).ToList();
        int targetCount = affectedTargets.Count;

        // Calculate damage per target
        double damagePerTarget = damagePercent * (targetCount > targetCap ? Math.Sqrt(targetCap / targetCount) : 1.0);

        // Deal damage to each affected target
        foreach (var target in affectedTargets)
        {
            DealDamage(target, damagePerTarget, spellSource);
        }
    }

    /// <summary>
    /// Deals damage to a specific number of targets. Takes into consideration current MainStat,
    /// Expertise, Critical Hit Chance, and Critical Hit Power.
    /// </summary>
    /// <param name="damagePercent">Damage percentage as full XX.X%</param>
    /// <param name="targetCap">Target cap before AOE stops.</param>
    /// <param name="includePrimaryTarget">Includes the Primary Target when dealing AOE. Disable when doing splash damage.</param>
    /// <param name="damageSource">Source of the damage. Usually a spell but can also be an Aura.</param>
    public void DealCappedAOEDamage(double damagePercent, double targetCap, Spell? spellSource = null,
        bool includePrimaryTarget = true)
    {
        //Gets the list of targets, skips the first one if includePrimaryTarget is False.
        var affectedTargets = includePrimaryTarget ? Targets : Targets.Skip(1).ToList();
        int targetCount = affectedTargets.Count;

        var enemies_stuck = 0;
        // Deal damage to each affected target
        foreach (var target in affectedTargets)
        {
            DealDamage(target, damagePercent, spellSource);
            enemies_stuck++;
            if (enemies_stuck >= targetCap)
                break;
        }
    }

    /// <summary>
    /// Called when a target takes damage. Takes into consideration any debuffs on the target, along with any extra
    /// modifiers.
    /// </summary>
    /// <returns>Damage taken after modifiers.</returns>
    /// <param name="amount">Incoming Damage amount.</param>
    /// <param name="isCritical">If the damage was a critical hit.</param>
    public double TakeDamage(double amount, bool isCritical, Spell? spellSource = null)
    {
        var totalDamage = (int)DamageTakenDebuffs.GetValue(amount);

        // Log damage event with coloring for critical hits
        var sourceName = spellSource != null
            ? spellSource.Name
            : "Unknown";
        var message = $"[bold blue]{sourceName}[/]"
                      + $" hits [bold yellow]{Name}[/]"
                      + $" for [bold magenta]{totalDamage}[/] "
                      + $"{(isCritical ? " (Critical Strike)" : "")}";
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, message, isCritical ? "💥" : null);

        OnDamageReceived?.Invoke(this, totalDamage, spellSource, isCritical);

        if (!HasInfiniteHp) Health.BaseValue -= totalDamage;
        if (Health.GetValue() < 0) Health.BaseValue = 0;
        OnHealthUpdated?.Invoke();

        return totalDamage;
    }

    protected override void Update()
    {
        Spirit = Math.Min(100,
            Spirit + (SimLoop.GetStep() * 0.2 * (1 + (SpiritStat.GetValue() / 100.0)))); //Base Spirit Regen is 0.2.
        // Update buffs
        for (int i = Buffs.Count - 1; i >= 0; i--)
        {
            Buffs[i].Update(SimLoop.GetElapsed());
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
            Debuffs[i].Update(SimLoop.GetElapsed());
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
            if (!_currentSpell.Channel && SimLoop.GetElapsed() >= _castTime)
            {
                _currentSpell.Cast(this, Targets);
                OnCast?.Invoke(this, _currentSpell, Targets);
                StopCasting();
            }
            else if (_currentSpell.Channel)
            {
                if (SimLoop.GetElapsed() >= _tickTime)
                {
                    _tickTime = Math.Round(_tickTime + _currentSpell.GetTickRate(this), 2);
                    _currentSpell.Tick(this, Targets);
                }

                if (SimLoop.GetElapsed() >= _channelTime)
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

    public bool IsDead() => Health.GetValue() <= 0;

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
        if (gcd != 0)
            ConsoleLogger.Log(
                SimulationLogLevel.CastEvents,
                $" -> Setting [bold blue]GCD[/] to [bold aqua]{gcd}[/]"
            );
        GCD = gcd + SimLoop.GetElapsed();
    }

    public void StartCasting(Spell spell, List<Unit> targets)
    {
        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Casting [bold blue]{spell.Name}[/]"
        );

        if (!spell.CanCastWhileCasting)
        {
            _currentSpell = spell;
            Targets = targets;
            _castTime = Math.Round(SimLoop.GetElapsed() + spell.GetCastTime(this), 2);
            IsCasting = true;
            SetGCD(spell.GetGCD(this));

            //Handle Channel Spells.
            if (spell.Channel)
            {
                //Channeled spells are technically instant cast.
                spell.Cast(this, targets);
                OnCast?.Invoke(this, _currentSpell, Targets);
                //Channeled spells always tick once at the very start.
                spell.Tick(this, targets);
                _channelTime = Math.Round(SimLoop.GetElapsed() + spell.GetChannelTime(this), 2);
                _tickTime = Math.Round(SimLoop.GetElapsed() + spell.GetTickRate(this), 2);
            }

            if (spell.GetCastTime(this) == 0 && spell.GetChannelTime(this) == 0)
            {
                spell.Cast(this, targets);
                OnCast?.Invoke(this, _currentSpell, Targets);
                StopCasting();
            }
        }
        else if (spell.CanCastWhileCasting)
        {
            spell.Cast(this, targets);
            OnCast?.Invoke(this, spell, Targets);
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