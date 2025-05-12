using SimFell.Logging;
using Spectre.Console;

namespace SimFell;

public class Rime : Unit
{
    private int Anima { get; set; }
    private int WinterOrbs { get; set; }

    private const int MaxAnima = 10;
    private const int MaxWinterOrbs = 5;

    private Spell _animaSpikes;

    //Spells for easy reference for Talents.
    private Spell _frostBolt;
    private Spell _burstingIce;
    private Spell _coldSnap;
    private Spell _freezingTorrent;
    private Spell _danceOfSwallows;
    private Spell _glacialBlast;
    private Spell _iceBlitz;
    private Spell _iceComet;
    private Spell _wintersBlessing;
    private Spell _wrathOfWinter;

    public Rime(int health) : base("Rime", health)
    {
        ConfigureSpellBook();
        ConfigureTalents();
    }

    public void ActivateTalent(string id)
    {
        var talent = Talents.FirstOrDefault(talent => talent.Id == id);
        if (talent != null) talent.Activate(this);
    }

    public void ActivateTalent(int row, int col)
    {
        var talent = Talents.FirstOrDefault(talent => talent.GridPos == $"{row}.{col}");
        if (talent != null)
        {
            talent.Activate(this);
            ConsoleLogger.Log(SimulationLogLevel.Debug, $"Activated talent '{talent.Name}'");
        }
    }

    public void ConfigureTalents()
    {
        Talents = new List<Talent>();

        #region Row 1

        //Chillblain Talent
        var chillBlain = new Talent(
            id: "chillblain",
            name: "Chillblain",
            gridPos: "1.1",
            onActivate: unit =>
            {
                // +20% Damage Buff to Torrent.
                _freezingTorrent.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent,
                    20, unit));
                // 20% AOE Damage to nearby targets.
                _freezingTorrent.OnTick += (caster, spell, targets) =>
                {
                    int maxTargets = 5;
                    int targetsHit = 0;
                    //Bonus damage on all except for the primary one
                    for (int i = targets.Count - 1; i >= 1; i--)
                    {
                        //20% of base damage.
                        DealDamage(targets[i], 65 * 0.2, spell);
                        targetsHit++;
                        if (maxTargets == targetsHit) break;
                    }
                };
            }
        );

        //Coalescing Ice.
        var coalescingIce = new Talent(
            id: "coalescing-ice",
            name: "Coalescing Ice",
            gridPos: "1.2",
            onActivate: unit =>
            {
                // +30% Damage buff to Bursting Ice.
                _burstingIce.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 30,
                    unit));
                // 3 Anima per Tick Instead of +1 if only fighting one target.
                unit.OnDamageDealt += (caster, damage, spell, aura) =>
                {
                    if (spell == _burstingIce && caster.Targets.Count == 1)
                    {
                        UpdateAnima(+2);
                    }
                };
            }
        );

        //Glacial Assault
        var glacialAssault = new Talent(
            id: "glacial-assault",
            name: "Glacial Assault",
            gridPos: "1.3",
            onActivate: unit =>
            {
                //Flat 20% Crit Bonus.
                _glacialBlast.CritModifiers.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 20, unit));

                //Stacking Buff for Instant Cast
                int glacialAssaultStacks = 0;
                int glacialAssaultMaxStacks = 4;
                
                //Mods for Glacial Blast when procs happen.
                Modifier instantCastMod =
                    new Modifier(Modifier.StatModType.Multiplicative, 0, //Multiplies cast time by 0 for instance cast.
                        _glacialBlast);
                Modifier damageMod =
                    new Modifier(Modifier.StatModType.Multiplicative, 2, //Multiplies damage by 2x.
                        _glacialBlast);
                
                //Glacial Assault Aura buff for tracking.
                //TODO: This should use Stack count where it gets applied every cast, and stacks are kept track in the
                // On Apply. Not in the talent itself.
                Aura glacialAssaultAura = new Aura(
                    id: "glacial-assault",
                    name: "Glacial Assault",
                    duration: 99999,
                    tickInterval: 0,
                    onApply: (unit1, unit2) =>
                    {
                        _glacialBlast.CastTime.AddModifier(instantCastMod);
                        _glacialBlast.DamageModifiers.AddModifier(damageMod);
                    },
                    onRemove: (unit1, unit2) =>
                    {
                        _glacialBlast.CastTime.RemoveModifier(instantCastMod);
                        _glacialBlast.DamageModifiers.RemoveModifier(damageMod);
                        glacialAssaultStacks = 0;
                    }
                );

                //On dealing damage, gain aura at maximum stacks.
                unit.OnDamageDealt += (caster, damage, spell, aura) =>
                {
                    if (spell == _coldSnap)
                    {
                        glacialAssaultStacks++;
                        if (glacialAssaultStacks == glacialAssaultMaxStacks)
                        {
                            caster.ApplyBuff(caster, caster, glacialAssaultAura);
                        }
                    }

                    if (spell == _glacialBlast && caster.HasBuff(glacialAssaultAura))
                    {
                        caster.RemoveBuff(glacialAssaultAura);
                    }
                };
            }
        );
        
        Talents.Add(chillBlain);
        Talents.Add(coalescingIce);
        Talents.Add(glacialAssault);

        #endregion
    }

    private void ConfigureSpellBook()
    {
        // Frostboll Spell
        _frostBolt = new Spell(
            id: "frost-bolt",
            name: "Frost Bolt",
            cooldown: 0,
            castTime: 1.5f,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, 73, spell);
                UpdateAnima(3);
            }
        );

        // Bursting Ice
        _burstingIce = new Spell(
            id: "bursting-ice",
            name: "Bursting Ice",
            cooldown: 15,
            castTime: 2.0,
            onCast: (unit, spell, targets) =>
            {
                var primaryTarget = targets.FirstOrDefault();
                primaryTarget?.ApplyDebuff(unit, primaryTarget, new Aura(
                    id: "bursting-ice",
                    name: "Bursting Ice",
                    duration: 3.15,
                    tickInterval: 0.5,
                    onTick: (caster, target) =>
                    {
                        int animaGained = 0;
                        int maxAnimaGainedPerTick = 3;

                        foreach (var targ in targets)
                        {
                            animaGained += 1;
                            DealDamage(targ, 61, spell);
                        }

                        //Maximum of 3 Anima gained per tick.
                        UpdateAnima(Math.Min(maxAnimaGainedPerTick, animaGained));
                    }
                ));
            }
        );

        // Cold Snap
        _coldSnap = new Spell(
            id: "cold-snap",
            name: "Cold Snap",
            cooldown: 8,
            castTime: 0,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.Where(t => t.Health > 0).FirstOrDefault()
                             ?? throw new Exception("No valid targets");
                DealDamage(target, 204, spell);
                UpdateWinterOrbs(1);
            }
        );

        // Freezing Torrent
        _freezingTorrent = new Spell(
            id: "freezing-torrent",
            name: "Freezing Torrent",
            cooldown: 10,
            castTime: 0,
            channel: true,
            channelTime: 2,
            tickRate: 0.4,
            onTick: (unit, spell, targets) =>
            {
                //TODO: Check to see if target is dead instead of getting first.
                var target = targets.FirstOrDefault() ?? throw new Exception("No valid targets");

                DealDamage(target, 65, spell);
                UpdateAnima(1);
            }
        );

        // Dance of Swallows
        _danceOfSwallows = new Spell(
            id: "dance-of-swallows",
            name: "Dance of Swallows",
            cooldown: 60,
            castTime: 0,
            canCast: (_) => WinterOrbs >= 2,
            onCast: (unit, spell, targets) =>
            {
                UpdateWinterOrbs(-2);
                var target = targets.Where(t => t.Health > 0).FirstOrDefault()
                             ?? throw new Exception("No valid targets");

                // Builds the OnDamage Event.
                Action<Unit, double, Spell, Aura>? onDamageEvent = (unit, damage, spellSource, auraSource) =>
                {
                    // If the source is from ColdSnap, deal bonus damage.
                    if (spellSource == _coldSnap)
                    {
                        const int danceOfSwallowsTriggers = 10;
                        for (int i = 0; i < danceOfSwallowsTriggers; i++)
                        {
                            DealDamage(unit, 53, spell);
                        }
                    }

                    if (spellSource == _freezingTorrent)
                    {
                        DealDamage(unit, 53, spell);
                    }
                };

                // Applies the Debuff to the Primary target.
                target.ApplyDebuff(unit, target, new Aura(
                    id: "dance-of-swallows",
                    name: "Dance of Swallows",
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) =>
                    {
                        //Subscribes to the Units OnDamageRecieved event.
                        target.OnDamageReceived += onDamageEvent;
                    },
                    onRemove: (caster, target) =>
                    {
                        //UnSubscribes to the Units OnDamageRecieved event.
                        target.OnDamageReceived -= onDamageEvent;
                    }
                ));
            }
        );

        //Glacial Blast
        _glacialBlast = new Spell(
            id: "glacial-blast",
            name: "Glacial Blast",
            cooldown: 0,
            castTime: 2.0,
            canCast: (_) => WinterOrbs >= 2,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault() ?? throw new Exception("No valid targets");
                UpdateWinterOrbs(-2);
                DealDamage(target, 504, spell);
            }
        );

        //Ice Blitz
        _iceBlitz = new Spell(
            id: "ice-blitz",
            name: "Ice Blitz",
            cooldown: 120,
            castTime: 0,
            hasGCD: false,
            //TODO: Can Cast while Casting.
            onCast: (unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "ice-blitz",
                    name: "Ice Blitz",
                    maxStacks: 1,
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) =>
                    {
                        target.DamageBuffs.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 15,
                            spell));
                    },
                    onRemove: (caster, target) => { unit.DamageBuffs.RemoveModifier(spell); }
                ));
            }
        );

        //Anima Spikes
        _animaSpikes = new Spell(
            id: "anima-spikes",
            name: "Anima Spikes",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault() ?? throw new Exception("No valid targets");
                DealDamage(target, 36, spell);
                DealDamage(target, 36, spell);
                DealDamage(target, 36, spell);
            }
        );

        //Ice Comet
        _iceComet = new Spell(
            id: "ice-comet",
            name: "Ice Comet",
            cooldown: 0,
            castTime: 0,
            hasGCD: false,
            canCast: (_) => WinterOrbs >= 3,
            onCast: (unit, spell, targets) =>
            {
                UpdateWinterOrbs(-2);

                foreach (var target in targets)
                {
                    DealDamage(target, 300, spell);
                }
            }
        );
        
        //Winters Blessing
        _wintersBlessing = new Spell(
            id: "winters-blessing",
            name: "Winters Blessing",
            cooldown: 120,
            castTime: 0,
            hasGCD: false,
            onCast: (caster, spell, targets) =>
            {
                //15% Damage Buff from Wrath of Winter.
                Modifier spiritMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 20, spell);
                
                caster.ApplyBuff(caster, caster, new Aura(
                    id: "winters-blessing",
                    name: "Winters Blessing",
                    duration: 15,
                    tickInterval: 0,
                    onApply: (unit, target) => { unit.SpiritStat.AddModifier(spiritMod); },
                    onRemove: (unit, target) => { unit.SpiritStat.RemoveModifier(spiritMod); }
                ));
            }
        );
        
        //Wrath of Winter - Spirit Ability.
        _wrathOfWinter = new Spell(
            id: "wrath-of-winter",
            name: "Wrath of Winter",
            cooldown: 0,
            castTime: 0,
            canCast: (_) => Spirit >= 100,
            onCast: (caster, spell, targets) =>
            {
                Spirit = 0; //Sets spirit to 0.
                
                //15% Damage Buff from Wrath of Winter.
                Modifier damageMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 15, spell);
                //+30% Haste from Spirit of Heroism.
                Modifier hasteMod = new Modifier(Modifier.StatModType.AdditivePercent, 30, spell);
                
                caster.ApplyBuff(caster, caster, new Aura(
                    id: "wrath-of-winter",
                    name: "Wrath of Winter",
                    duration: 20,
                    tickInterval: 2,
                    onTick: (unit, target) => { UpdateWinterOrbs(1); },
                    onApply: (unit, target) => { unit.DamageBuffs.AddModifier(damageMod); },
                    onRemove: (unit, target) => { unit.DamageBuffs.RemoveModifier(damageMod); }
                ));
                
                caster.ApplyBuff(caster, caster, new Aura(
                    id: "spirit-of-heroism",
                    name: "Spirit of Heroism",
                    duration: 20,
                    tickInterval: 0,
                    onTick: (unit, target) => { UpdateWinterOrbs(1); },
                    onApply: (unit, target) => { unit.DamageBuffs.AddModifier(hasteMod); },
                    onRemove: (unit, target) => { unit.DamageBuffs.RemoveModifier(hasteMod); }
                ));
            }
        );

        SpellBook.Add(_iceBlitz);
        SpellBook.Add(_danceOfSwallows);
        SpellBook.Add(_coldSnap);
        SpellBook.Add(_burstingIce);
        SpellBook.Add(_freezingTorrent);
        SpellBook.Add(_glacialBlast);
        SpellBook.Add(_frostBolt);
        SpellBook.Add(_iceComet);
        SpellBook.Add(_wintersBlessing);
        SpellBook.Add(_wrathOfWinter);
    }

    /// <summary>
    /// Updates the Anima for Rime based on the given delta.
    /// </summary>
    /// <param name="animaDelta"></param>
    public void UpdateAnima(int animaDelta)
    {
        Anima += animaDelta;
        if (Anima > MaxAnima)
        {
            //TODO: Anima Spikes Here? Or Winter Orb? Need to double check.
            Anima = 0;
            UpdateWinterOrbs(1);
        }
    }

    /// <summary>
    /// Updates the Winter Orbs for Rime based on the given delta.
    /// </summary>
    /// <param name="winterOrbsDelta"></param>
    public void UpdateWinterOrbs(int winterOrbsDelta)
    {
        WinterOrbs += winterOrbsDelta;
        WinterOrbs = Math.Clamp(WinterOrbs, 0, MaxWinterOrbs);

        if (winterOrbsDelta > 0 && PrimaryTarget != null)
            _animaSpikes.Cast(this, new List<Unit>() { PrimaryTarget });
    }
}