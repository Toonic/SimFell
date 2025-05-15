using SimFell.Logging;

namespace SimFell;

public class Tariq : Unit
{
    private double Fury { get; set; } //As Percentage in Decimal

    //TODO: You lose 0.01 Fury per Tick (1 second) out of combat.
    private double MaximumFury { get; set; }

    private Spell _autoAttack; //Tariq has an auto attack. Just make it a spell.
    private Spell _skullCrusher;
    private Spell _hammerStorm;
    private Spell _heavyStrike;
    private Spell _thunderCall;
    private Spell _faceBreaker;
    private Spell _wildSwing;
    private Spell _chainLightning;
    private Spell _ragingTempest;
    private Spell _leapSmash;
    private Spell _focusedWrath;
    private Spell _cullingStrike;

    private Aura _thunderCallAura; // A lot of spells reference this so just easier to track it global for Tariq.
    private Aura _focusedWrathAura;
    private Stat _swingTimer;
    public double NextSwingTime;

    private Stat focusedWrathDamageBuff = new Stat(0);
    private Stat focusedWrathCostBuff = new Stat(0);

    public Tariq(int health) : base("Tariq", health)
    {
        _swingTimer = new Stat(4.8);
        OnDamageDealt += GainFury;
        //ConfigureHeavyStrikeReset();
        ConfigureSpellBook();
        //ConfigureTalents();
    }

    public override void SetPrimaryStats(int mainStat, int criticalStrikeStat, int expertiseStat, int hasteStat,
        int spiritStat)
    {
        base.SetPrimaryStats(mainStat, criticalStrikeStat, expertiseStat, hasteStat, spiritStat);
        MaximumFury = mainStat * 12; //Tariqs maximum rage is Str * MaxRageMultiplier.
    }

    // Used to configure when Heavy Strike resets.
    private void ConfigureHeavyStrikeReset()
    {
        OnCast += (unit, spell, targets) =>
        {
            if (spell != _autoAttack)
            {
                //TODO: Remove Heavy Strike Aura.
            }
        };
    }

    private void ResetSwingTimer()
    {
        NextSwingTime = Math.Round(SimLoop.Instance.GetElapsed() + _swingTimer.GetValue(), 2);
    }

    private void GainFury(Unit caster, double damageDelt, Spell? spellSource, Aura? auraSource)
    {
        //If spell is NOT Heavy Strike, Face Breaker, Wild Swing, Chain Lightning, Leap Smash then return.
        if (spellSource == null ||
            (spellSource != _heavyStrike &&
             spellSource != _faceBreaker &&
             spellSource != _wildSwing &&
             spellSource != _chainLightning
            // spellSource != _leapSmash))
            ))
        {
            return;
        }

        var furyToGen = (26.0 * (damageDelt / (1 * 100)));
        var furyToGenAsPercent = furyToGen / MaximumFury;
        GainFury(furyToGenAsPercent);
    }

    private void GainFury(double furyToGen)
    {
        Fury = Math.Min(Math.Round(Fury + furyToGen, 3), 1);
    }

    private void SpendFury(double percentageToSpend)
    {
        Fury -= percentageToSpend;
    }

    private void ConfigureSpellBook()
    {
        // Thunder Call
        _thunderCallAura = new Aura(
            id: "thunder-call",
            name: "Thunder Call",
            duration: 25,
            tickInterval: 0
        );

        _thunderCall = new Spell(
            id: "thunder-call",
            name: "Thunder Call",
            cooldown: 45,
            castTime: 0,
            hasGCD: false,
            canCastWhileCasting: true,
            onCast: (unit, spell, targets) => { unit.ApplyBuff(unit, unit, _thunderCallAura); }
        );

        //TODO: Use stacks on this instead and subtract stacks.
        int focusedWrathUsage = 0;
        Modifier focusedWrathDamageMod = new Modifier(Modifier.StatModType.Multiplicative, 0.5f);
        Modifier focusedWrathCostMod = new Modifier(Modifier.StatModType.Multiplicative, 0.5f);
        _focusedWrathAura = new Aura(
            id: "focused-wrath",
            name: "Focused Wrath",
            duration: 9999,
            tickInterval: 0,
            onApply: (caster, owner) =>
            {
                focusedWrathUsage = 0;
                focusedWrathCostBuff.AddModifier(focusedWrathCostMod);
                focusedWrathDamageBuff.AddModifier(focusedWrathDamageMod);
            },
            onRemove: (caster, owner) =>
            {
                focusedWrathUsage = 0;
                focusedWrathCostBuff.RemoveModifier(focusedWrathCostMod);
                focusedWrathDamageBuff.RemoveModifier(focusedWrathDamageMod);
            }
        );

        // To be honest, Tariq doesn't need this coded, and it probably won't be used.
        // Instead, we will probably just call Heavy Strike when Auto Attacks should happen.
        // Good players will never miss a _heavyStrike.
        _autoAttack = new Spell(
            id: "auto-attack",
            name: "Auto Attack",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            canCastWhileCasting: true, // I'm not 100% sure on this? I'll look later.
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, 90, spell);
            }
        );

        //Heavy Strike.
        _heavyStrike = new Spell(
            id: "heavy-strike",
            name: "Heavy Strike",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            canCastWhileCasting: true,
            canCast: unit => NextSwingTime <= SimLoop.Instance.GetElapsed(),
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, 90 * 2.4, spell);
                ResetSwingTimer(); //Reset the swing timer.

                //Note: Unsure if this effects the Target you hit, or only AOE targets.
                if (unit.HasBuff(_thunderCallAura))
                {
                    foreach (var tar in targets)
                    {
                        DealDamage(tar, 173, _thunderCall);
                    }
                }
            }
        );

        //Skull Crusher
        _skullCrusher = new Spell(
            id: "skull-crusher",
            name: "Skull Crusher",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            hasAntiSpam: true, //Used when the spell can be spammed. (Multi Stacks etc.)
            canCast: unit => Fury >= focusedWrathCostBuff.GetValue(0.5),
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, focusedWrathDamageBuff.GetValue(340), spell);
                ResetSwingTimer();
                SpendFury(0.5);

                if (unit.HasBuff(_thunderCallAura))
                {
                    DealDamage(target, 273, _thunderCall);
                }

                if (unit.HasBuff(_focusedWrathAura))
                {
                    focusedWrathUsage++;
                    if (focusedWrathUsage == 2) unit.RemoveBuff(_focusedWrathAura);
                }
            }
        );

        //Hammer Storm
        double hammerStormRageSpent = 0;

        _hammerStorm = new Spell(
            id: "hammer-storm",
            name: "Hammer Storm",
            cooldown: 1.5,
            castTime: 0,
            channel: true,
            channelTime: 9999, //Channel time is long because it stops when 0.5 Fury is spent.
            tickRate: 0.7,
            canCast: unit => Fury >= focusedWrathCostBuff.GetValue(0.26),
            onTick: (unit, spell, targets) =>
            {
                //First tick includes the base cast amount. Otherwise +0.08 per tick.
                if (hammerStormRageSpent == 0)
                {
                    hammerStormRageSpent = focusedWrathCostBuff.GetValue(0.26);
                    SpendFury(focusedWrathCostBuff.GetValue(0.26));
                }
                else
                {
                    hammerStormRageSpent += focusedWrathCostBuff.GetValue(0.08);
                    SpendFury(focusedWrathCostBuff.GetValue(0.08));
                }

                //Reset the swing timer every tick.
                ResetSwingTimer();

                //Deal damage to everything around you.
                foreach (var target in targets)
                {
                    DealDamage(target, focusedWrathDamageBuff.GetValue(56), spell);
                    if (unit.HasBuff(_thunderCallAura))
                    {
                        DealDamage(target, 44.8, _thunderCall);
                    }
                }

                if (unit.HasBuff(_focusedWrathAura))
                {
                    focusedWrathUsage++;
                    if (focusedWrathUsage == 2) unit.RemoveBuff(_focusedWrathAura);
                }

                //Stop it if the hammerStormRageSpent is 0.5
                if (hammerStormRageSpent >= 0.5) StopCasting(); //Stop channeling at 0.5 fury spent.
            }
        );

        // Face Breaker
        bool faceBreakerActive = false;
        _faceBreaker = new Spell(
            id: "face-breaker",
            name: "Face Breaker",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            canCastWhileCasting: true,
            canCast: unit => faceBreakerActive,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                faceBreakerActive = false;
                DealDamage(target, 275, spell);
            }
        );

        // Wild Swing
        // Wild Swing can't crit instead increases damage.
        Modifier noCritMod = new Modifier(Modifier.StatModType.Multiplicative, 0);
        Modifier critToDamageMod;

        _wildSwing = new Spell(
            id: "wild-swing",
            name: "Wild Swing",
            cooldown: 0,
            castTime: 0,
            onCast: (unit, spell, targets) =>
            {
                float dodgeChanceAsPercent = 14;
                dodgeChanceAsPercent = Math.Max(dodgeChanceAsPercent - (7 * (targets.Count - 1)), 0);
                dodgeChanceAsPercent += 5; //Base 5% on everyone and everything.

                //Dynamically get the Crit chance and apply it to the damage.
                critToDamageMod = new Modifier(Modifier.StatModType.MultiplicativePercent,
                    (float)unit.CritcalStrikeStat.GetValue());
                _wildSwing.DamageModifiers.AddModifier(critToDamageMod);

                foreach (var target in targets)
                {
                    if (SimRandom.Roll(dodgeChanceAsPercent))
                    {
                        ConsoleLogger.Log(SimulationLogLevel.CastEvents,
                            "[bold blue]Wild Swing[/] [bold red]missed![/]");
                        faceBreakerActive = true;
                    }
                    else DealDamage(target, 62, spell);
                }

                //Remove it once done.
                _wildSwing.DamageModifiers.RemoveModifier(critToDamageMod);
            }
        );
        _wildSwing.CritModifiers.AddModifier(noCritMod);

        _chainLightning = new Spell(
            id: "chain-lightning",
            name: "Chain Lightning",
            cooldown: 7.5,
            castTime: 0,
            canCast: (unit) => unit.HasBuff(_thunderCallAura),
            onCast: (unit, spell, targets) =>
            {
                int maxBounces = 5;
                int currentBounces = 0;
                while (currentBounces != maxBounces)
                {
                    foreach (var target in targets)
                    {
                        DealDamage(target, 85, spell);
                        currentBounces++;
                        if (currentBounces == maxBounces) break;
                    }
                }
            }
        );

        _ragingTempest = new Spell(
            id: "raging-tempest",
            name: "Raging Tempest",
            cooldown: 0,
            castTime: 0,
            canCast: (unit) => Spirit >= 100,
            onCast: (unit, spell, targets) =>
            {
                Spirit = 0;
                int ticks = 0;
                unit.ApplyBuff(unit, unit, SpiritOfHeroism);
                unit.ApplyBuff(unit, unit, _thunderCallAura); //Hacky way to handle it.
                Modifier critMod = new Modifier(Modifier.StatModType.AdditivePercent, 1);
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "raging-tempest",
                    name: "Raging Tempest",
                    duration: 20,
                    tickInterval: 0.5,
                    onTick: (caster, owner) =>
                    {
                        var target = Targets[SimRandom.Next(0, Targets.Count)];
                        DealDamage(target, 99, spell);
                        owner.CritcalStrikeStat.RemoveModifier(critMod);
                        ticks++;
                        critMod = new Modifier(Modifier.StatModType.AdditivePercent, ticks);
                    },
                    onRemove: (caster, owner) =>
                    {
                        owner.CritcalStrikeStat.RemoveModifier(critMod);
                    }
                ));
            }
        );

        _leapSmash = new Spell(
            id: "leap-smash",
            name: "Leap Smash",
            cooldown: 20,
            castTime: 0,
            hasGCD: false,
            onCast: (unit, spell, targets) =>
            {
                foreach (var target in targets)
                {
                    DealDamage(target, 347, spell);
                }

                GainFury(0.25);
            }
        );

        _cullingStrike = new Spell(
            id: "culling-strike",
            name: "Culling Strike",
            cooldown: 5,
            castTime: 0,
            canCast: (unit) =>
            {
                var target = unit.Targets
                    .Where(u => u.Health <= 0.3 * u.MaximumHealth).OrderBy(u => u.Health).FirstOrDefault();
                return target != null && Fury >= 0;
            },
            onCast: (unit, spell, targets) =>
            {
                var target = unit.Targets
                    .Where(u => u.Health < 0.3 * u.MaximumHealth).OrderBy(u => u.Health).FirstOrDefault();
                int maxStacks = 20;
                int currentStacks = 0;

                while (currentStacks < maxStacks && Fury >= 0.01)
                {
                    SpendFury(0.01);
                    currentStacks++;
                }

                DealDamage(target, (currentStacks * 20) + 200, _cullingStrike);

            }
        );

        _focusedWrath = new Spell(
            id: "focused-wrath",
            name: "Focused Wrath",
            cooldown: 90,
            castTime: 0,
            hasGCD: false,
            onCast: (unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, _focusedWrathAura);
            }
        );

        SpellBook.Add(_autoAttack);
        SpellBook.Add(_skullCrusher);
        SpellBook.Add(_hammerStorm);
        SpellBook.Add(_heavyStrike);
        SpellBook.Add(_faceBreaker);
        SpellBook.Add(_wildSwing);
        SpellBook.Add(_thunderCall);
        SpellBook.Add(_chainLightning);
        SpellBook.Add(_ragingTempest);
        SpellBook.Add(_leapSmash);
        //SpellBook.ADd(_unbreakableWill); //Not Coded due to it being a defensive.
        SpellBook.Add(_focusedWrath);
        //SpellBook.Add(_intimidate); //Not coded due to being a taunt.
        SpellBook.Add(_cullingStrike);
    }

    private void ConfigureTalents()
    {
    }
}