using SimFell.Logging;
using SimFell.Sim;
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
    List<Aura> _expiredBuffs = new List<Aura>();
    public List<Aura> Debuffs { get; set; } = [];
    List<Aura> _expiredDebuffs = new List<Aura>();
    public List<Spell> SpellBook { get; set; } = [];
    public List<Talent> Talents { get; set; } = [];
    public List<Spell> Rotation { get; set; } = [];
    public Unit? PrimaryTarget { get; private set; }

    public List<SimAction> SimActions { get; set; } = new List<SimAction>();

    // Casting
    public bool IsCasting = false;
    private Spell? _currentSpell;
    public List<Unit> Targets = new List<Unit>();

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
    public Action<Unit, Unit, double, Spell?>? OnDamageDealt { get; set; }
    public Action<Unit, double, Spell?, bool>? OnDamageReceived { get; set; }
    public Action<Unit, double, Spell?>? OnCrit { get; set; }
    public Action<Unit, Spell, List<Unit>> OnCastStarted { get; set; } = (unit, spellSource, targets) => { };
    public Action<Unit, Spell, List<Unit>> OnCastDone { get; set; } = (unit, spellSource, targets) => { };

    public Action<Unit, Spell, List<Unit>> OnChannelStarted { get; set; } = (unit, spellSource, targets) => { };
    public Action<Unit, Spell, List<Unit>> OnChannelEnd { get; set; } = (unit, spellSource, targets) => { };

    // On Health Updated event
    public event Action? OnHealthUpdated;

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
        {
            // Console.WriteLine("TODO: Refresh?");
        }
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

    public void RemoveDebuff(Aura debuff)
    {
        var existing = Debuffs.Where(aura => aura.ID == debuff.ID).ToList();
        foreach (var aura in existing)
        {
            ConsoleLogger.Log(
                SimulationLogLevel.BuffEvents,
                $"[bold blue]{Name}[/] loses Debuff: [bold yellow]{debuff.Name}[/]",
                "💪🛑"
            );
            aura.Remove();
            Debuffs.Remove(aura);
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

    public List<Aura> GetDebuffs(Aura buff)
    {
        var existing = Debuffs.Where(aura => aura.ID == buff.ID).ToList();
        return existing;
    }

    public Aura GetDebuff(Aura buff)
    {
        var existing = Debuffs.Where(aura => aura.ID == buff.ID).ToList();
        return existing.FirstOrDefault();
    }

    /// <summary>
    /// Calculates how much damage something will do, excluding the 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="damagePercent"></param>
    /// <param name="spellSource"></param>
    /// <param name="includeCriticalStrike"></param>
    /// <param name="includeExpertise"></param>
    /// <param name="isFlatDamage"></param>
    /// <returns>Damage value as Float. Bool if Critical Strike happened.</returns>
    public (double damage, bool isCritical) GetDamage(Unit target, double damagePercent, Spell? spellSource = null,
        bool includeCriticalStrike = true, bool includeExpertise = true, bool isFlatDamage = false)
    {
        // TODO: Remove this when I start to use values from the game (EG: Proper percentages)
        if (!isFlatDamage) damagePercent = damagePercent / 1000.0;

        var critPercent = CritcalStrikeStat.GetValue();
        critPercent = includeCriticalStrike ? critPercent : 0;

        if (spellSource != null)
        {
            damagePercent = spellSource.DamageModifiers.GetValue(damagePercent);
            critPercent = spellSource.CritModifiers.GetValue(critPercent);
        }

        Modifier grievousCritsModifier = new Modifier(Modifier.StatModType.AdditivePercent, 0);
        if (critPercent > 100.0)
        {
            grievousCritsModifier.Value = critPercent - 100.0;
            CriticalStrikePowerStat.AddModifier(grievousCritsModifier);
        }

        //Converts the DamagePercent into a Damage Value.
        var damage = damagePercent * MainStat.GetValue(); // Adds the Damage as Main Stat.
        if (isFlatDamage) damage = damagePercent;
        if (includeExpertise)
            damage *= 1 + (ExpertiseStat.GetValue() / 100f); // Modifies the damage based on expertise.
        damage = DamageBuffs.GetValue(damage);

        var isCritical = SimRandom.Roll(critPercent);
        isCritical = SimRandom.CanCrit ? isCritical : false;
        //Handle general On Crit.
        damage *= isCritical ? 2 : 1; //Doubles the damage if there is a Critical Hit.
        //Handle Crit Power.
        if (isCritical) damage = CriticalStrikePowerStat.GetValue(damage);
        CriticalStrikePowerStat.RemoveModifier(grievousCritsModifier);

        //Any additional mods on the target.
        damage = GetDamageTakenWithDebuffs(damage);

        return (damage, isCritical);
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
    /// <param name="includeCriticalStrike">If the damage can crit.</param>
    /// <param name="includeExpertise">If the damage will include Expertise in its calculations.</param>
    /// <param name="isFlatDamage">If the damage is flat, does not include damagePercent + MainStat </param>
    public double DealDamage(Unit target, double damagePercent, Spell? spellSource = null,
        bool includeCriticalStrike = true, bool includeExpertise = true, bool isFlatDamage = false)
    {
        var (damage, isCritical) =
            GetDamage(target, damagePercent, spellSource, includeCriticalStrike, includeExpertise, isFlatDamage);

        var totalDamageTaken = target.TakeDamage(damage, isCritical, spellSource);
        if (isCritical) OnCrit?.Invoke(this, totalDamageTaken, spellSource); //On Crit events called.
        OnDamageDealt?.Invoke(this, target, totalDamageTaken, spellSource); //Called when damage is dealt.

        return damage;
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
        double damagePerTarget =
            damagePercent * (targetCount > targetCap ? Math.Sqrt(targetCap / targetCount) : 1.0);

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

    public double GetDamageTakenWithDebuffs(double amount)
    {
        return DamageTakenDebuffs.GetValue(amount);
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
        var totalDamage = (int)amount;

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
    }

    private List<SimEvent> _castEvents = new List<SimEvent>();
    private double _castStartTime;

    public void StartCasting(Spell spell, List<Unit> targets)
    {
        _currentSpell = spell;

        // Handle Non-Channeled Spells.
        if (!spell.Channel)
        {
            ConsoleLogger.Log(
                SimulationLogLevel.CastEvents,
                $"Casting [bold blue]{spell.Name}[/]"
            );

            // Fire off the Cast Started Event.
            OnCastStarted?.Invoke(this, spell, Targets);

            // Schedule the actual cast finish event.
            double castTime = spell.CastTime.GetValue();
            SimEvent castFinishEvent = new SimEvent(Simulator, this, castTime, () => FinishCasting(spell));
            _castStartTime = Simulator.Now;
            _castEvents.Add(castFinishEvent);
            Simulator.Schedule(castFinishEvent);
        }

        if (spell.Channel)
        {
            ConsoleLogger.Log(
                SimulationLogLevel.CastEvents,
                $"Channeling [bold blue]{spell.Name}[/]"
            );

            // Trigger Cast Started Event.
            OnChannelStarted?.Invoke(this, spell, Targets);

            // Channel Spells set their cooldown and casting cost at the start of the channel.
            spell.CastingCost(this);
            spell.CastFinished(this);

            _castStartTime = Simulator.Now;

            // Channeled Spells always have a single hit at the start of the channel.
            TriggerSpellEvent(spell);
            // Queue the channel ending.
            Simulator.Schedule(new SimEvent(Simulator, this, spell.ChannelTime.GetValue(),
                () => FinishChanneling(spell), spell.HasteEffectsChannel));

            // Schedule the next tick event.
            spell.OnTick += OnTickFromChanneledSpell;
            SimEvent tickEvent = new SimEvent(Simulator, this, spell.GetTickRate(this),
                () => TriggerSpellEvent(spell));
            _castEvents.Add(tickEvent);
            Simulator.Schedule(tickEvent);
        }
    }

    // Used to handle on tick events for channeled spells.
    private void OnTickFromChanneledSpell(Unit caster, Spell spell, List<Unit> targets)
    {
        _castEvents.Clear();
        SimEvent tickEvent = new SimEvent(Simulator, this, spell.GetTickRate(this),
            () => TriggerSpellEvent(spell));
        _castEvents.Add(tickEvent);
        Simulator.Schedule(tickEvent);
    }

    public void InteruptCasting()
    {
        Console.WriteLine("!! Not fully implemented yet. !! ");
        foreach (var evt in _castEvents)
        {
            Simulator.UnSchedule(evt);
        }

        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Cancel Casting [bold blue]{_currentSpell.Name}[/]"
        );
    }

    private void FinishCasting(Spell spell)
    {
        _castEvents.Clear();
        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Finished Casting [bold blue]{spell.Name}[/]"
        );

        // Handle the Casting Cost + Cast Finished.
        spell.CastingCost(this);
        spell.CastFinished(this);
        if (spell != null) OnCastDone?.Invoke(this, spell, Targets);

        // Schedule the Spells actual effect taking into consideration travel time.
        double travelTime = spell.TravelTime.GetValue();
        Simulator.Schedule(new SimEvent(Simulator, this, travelTime,
            () => TriggerSpellEvent(spell), false));

        ScheduleNextCast();
    }

    private void FinishChanneling(Spell spell)
    {
        spell.OnTick -= OnTickFromChanneledSpell;

        foreach (var evt in _castEvents)
        {
            if (evt.Time > Simulator.Now)
            {
                //Handles Partial Ticks for channeled spells.
                double partialTickPercentage = (Simulator.Now - evt.StartTime) / (evt.Time - evt.StartTime);
                Modifier partialTickMod = new Modifier(Modifier.StatModType.Multiplicative, partialTickPercentage);
                spell.DamageModifiers.AddModifier(partialTickMod);
                TriggerSpellEvent(spell);
                spell.DamageModifiers.RemoveModifier(partialTickMod);
                Simulator.UnSchedule(evt);
            }
        }

        _castEvents.Clear();
        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Finished Channeling [bold blue]{spell.Name}[/]"
        );

        ScheduleNextCast();
    }

    private void ScheduleNextCast()
    {
        double gcd = _currentSpell.GetGCD(this);
        double nextActionDelay = Math.Max(0,
            gcd - (_currentSpell.Channel ? _currentSpell.ChannelTime.GetValue() : _currentSpell.CastTime.GetValue()));
        if (nextActionDelay > 0)
            ConsoleLogger.Log(
                SimulationLogLevel.CastEvents,
                $" -> Waiting on [bold blue]GCD[/]."
            );
        Simulator.Schedule(new SimEvent(Simulator, this, nextActionDelay,
            () => Simulator.QueuePlayerAction(this)));
    }

    private void TriggerSpellEvent(Spell spell)
    {
        if (spell.Channel) spell.Tick(this, Targets);
        else spell.Cast(this, Targets);
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

    public void CleanUp()
    {
        OnDamageDealt = null;
        OnDamageReceived = null;
        OnCrit = null;
        OnCastStarted = null;
        OnCastDone = null;
    }
}