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

    private Aura _thunderCallAura; // A lot of spells reference this so just easier to track it global for Tariq.
    private Stat _swingTimer;
    public double NextSwingTime;
    
    public Tariq(int health) : base("Tariq", health)
    {
        _swingTimer = new Stat(4.8);
        OnDamageDealt += GainFury;
        //ConfigureHeavyStrikeReset();
        ConfigureSpellBook();
        //ConfigureTalents();
    }

    public override void SetPrimaryStats(int mainStat, int criticalStrikeStat, int expertiseStat, int hasteStat, int spiritStat)
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
            (spellSource != _heavyStrike
             // spellSource != _faceBreaker &&
             // spellSource != _wildSwing &&
             // spellSource != _chainLightning &&
             // spellSource != _leapSmash))
             ))
             {
                 return;
             }
        var furyToGen = (26.0 * (damageDelt / (1 * 100)));
        var furyToGenAsPercent = furyToGen / MaximumFury;
        Fury = Math.Min(Math.Round(Fury + furyToGenAsPercent,3), 1);
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
            onCast: (unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, _thunderCallAura);
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
                DealDamage(target, 240, spell);
                ResetSwingTimer(); //Reset the swing timer.

                // TODO: Thunder Call Bonus
                // Unsure if Heavy Strike deals bonus damage single target.
            }
        );
        
        //Skull Crusher
        _skullCrusher = new Spell(
            id: "skull-crusher",
            name: "Skull Crusher",
            cooldown: 1.5,
            castTime: 0,
            hasGCD: true,
            canCast: unit => Fury >= 0.5, 
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, 240, spell);
                ResetSwingTimer();
                SpendFury(0.5);
                
                //TODO: Thunder Call Bonus
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
            channelTime: 2.1, // We stop it manually in the cast.
            tickRate: 0.7,
            canCast: unit => Fury >= 0.5, 
            onCast: (unit, spell, targets) =>
            {
                hammerStormRageSpent = 0.26;
            },
            onTick: (unit, spell, targets) =>
            {
                //First tick includes the base amount. Otherwise +0.08 per tick.
                if (hammerStormRageSpent == 0) { hammerStormRageSpent = 0.26; SpendFury(0.26); }
                else { hammerStormRageSpent += 0.08; SpendFury(0.08);}
                
                //Reset the swing timer every tick.
                ResetSwingTimer();
                
                //Deal damage to everything around you.
                foreach (var target in targets)
                {
                    DealDamage(target, 56, spell);
                }
                
                //TODO: Thunder Call Bonus
                
                //Stop it if the hammerStormRageSpent is 0.5
                // if (hammerStormRageSpent >= 0.5) StopCasting(); //Stop channeling at 0.5 fury spent.
            }
            
        );
        
        SpellBook.Add(_autoAttack);
        SpellBook.Add(_heavyStrike);
        SpellBook.Add(_thunderCall);
        SpellBook.Add(_skullCrusher);
        SpellBook.Add(_hammerStorm);
    }

    private void ConfigureTalents()
    {
        
    }
}