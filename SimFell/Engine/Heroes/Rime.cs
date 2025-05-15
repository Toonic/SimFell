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

    // Modifiers for Talents.
    private Modifier _iceBlitzBonusDamage;

    //Custom Rime Events.
    private Action<int> OnWinterOrbUpdate { get; set; }
    private Action<int> OnAnimaUpdate { get; set; }

    public Rime(int health) : base("Rime", health)
    {
        ConfigureSpellBook();
        ConfigureTalents();
    }

    public void ConfigureTalents()
    {
        Talents = new List<Talent>();

        #region Talents Row 1

        //Chillblain Talent
        var chillBlain = new Talent(
            id: "chillblain",
            name: "Chillblain",
            gridPos: "1.1",
            onActivate: unit =>
            {
                // +20% Damage Buff to Torrent.
                _freezingTorrent.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent,
                    20));
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
                _burstingIce.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 30));
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
                _glacialBlast.CritModifiers.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 20));

                //Stacking Buff for Instant Cast
                int glacialAssaultStacks = 0;
                int glacialAssaultMaxStacks = 4;

                //Mods for Glacial Blast when procs happen.
                Modifier instantCastMod =
                    new Modifier(Modifier.StatModType.Multiplicative, 0); //Multiplies cast time by 0 for instance cast.
                Modifier damageMod =
                    new Modifier(Modifier.StatModType.Multiplicative, 2); //Multiplies damage by 2x.

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
        #region Talents Row 2

        // Unrelenting Ice
        var unrelentingIce = new Talent(
            id: "unrelenting-ice",
            name: "Unrelenting Ice",
            gridPos: "2.1",
            onActivate: unit =>
            {
                // Reduce bursting ice cooldown by 0.5 every tick.
                _freezingTorrent.OnTick += (caster, spell, targets) =>
                {
                    _burstingIce.UpdateCooldown(0.5);
                };
            }
        );

        // Icy Flow
        var icyFlow = new Talent(
            id: "icy-flow",
            name: "Ice Flow",
            gridPos: "2.2",
            onActivate: unit =>
            {
                unit.OnDamageDealt += (caster, damage, spell, aura) =>
                {
                    // Reduce freezing torrent cooldown by 0.2 every time a shard hits.
                    if (spell == _animaSpikes)
                    {
                        _freezingTorrent.UpdateCooldown(0.2);
                    }
                };
            }
        );

        //Tundra Guard
        var tundraGuard = new Talent(
            id: "tundra-guard",
            name: "Tundra Guard",
            gridPos: "2.3"
        );

        Talents.Add(unrelentingIce);
        Talents.Add(icyFlow);
        Talents.Add(tundraGuard);
        #endregion
        #region Talents Row 3

        // Avalanche
        var avalanche = new Talent(
            id: "avalanche",
            name: "Avalanche",
            gridPos: "3.1",
            onActivate: unit =>
            {
                // Passive 5%
                unit.CritcalStrikeStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 5));

                _iceComet.OnCast += (caster, spell, targets) =>
                {
                    //20% Chance to fire off a second one.
                    if (SimRandom.Roll(20))
                    {
                        foreach (var target in targets)
                        {
                            DealDamage(target, 300, spell);
                        }

                        //4% Chance to firee off a third one.
                        if (SimRandom.Roll(4))
                        {
                            foreach (var target in targets)
                            {
                                DealDamage(target, 300, spell);
                            }
                        }
                    }
                };
            }
        );

        //Wisdom of the North
        var wisdomOfTheNorth = new Talent(
            id: "wisdom-of-the-north",
            name: "Wisdom of The North",
            gridPos: "3.2",
            onActivate: unit =>
            {
                //Bonus Ice Blitz damage if this is active by 10%.
                _iceBlitzBonusDamage = new Modifier(Modifier.StatModType.MultiplicativePercent, 15 + 10);

                double cdr = 1;
                OnWinterOrbUpdate += (delta) =>
                {
                    //For every winter orb spent reduce cooldowns.
                    if (delta < 0)
                    {
                        _iceBlitz.UpdateCooldown(cdr);
                        _danceOfSwallows.UpdateCooldown(cdr);
                        _wintersBlessing.UpdateCooldown(cdr);
                    }
                };
            }
        );

        // Soulfrost Torrent.
        var soulfrostTorrent = new Talent(
            id: "soulfrost-torrent",
            name: "Soulfrost Torrent",
            gridPos: "3.3",
            onActivate: unit =>
            {
                //Mods for Soulfrost Torrent.
                var freezingTorrentChannelTimeMod = new Modifier(Modifier.StatModType.Multiplicative, 2.0f);
                var freezingTorrentDamageMod = new Modifier(Modifier.StatModType.Multiplicative, 2.0f);

                Aura soulFrostAura = new Aura(
                    id: "soulfrost-torrent",
                    name: "Soulfrost Torrent",
                    duration: 9999,
                    tickInterval: 0
                );

                var soulFrostRPPM = new RPPM(1.5);
                var hasUsed = false;
                unit.OnCrit += (caster, damage, spell, targets) =>
                {
                    if (soulFrostRPPM.TryProc())
                    {
                        caster.ApplyBuff(caster, caster, soulFrostAura);
                    }
                };

                _freezingTorrent.OnCast += (caster, spell, targets) =>
                {
                    if (caster.HasBuff(soulFrostAura))
                    {
                        hasUsed = true;
                        _freezingTorrent.ChannelTime.AddModifier(freezingTorrentChannelTimeMod);
                        _freezingTorrent.DamageModifiers.AddModifier(freezingTorrentDamageMod);
                    }
                    else
                    {
                        foreach (var buffs in caster.Buffs)
                        {
                            Console.WriteLine(buffs.Name);
                        }
                    }
                };

                unit.OnCastDone += (caster, spell, targets) =>
                {
                    if (caster.HasBuff(soulFrostAura) && spell == _freezingTorrent)
                    {
                        hasUsed = false;
                        unit.RemoveBuff(soulFrostAura);
                        _freezingTorrent.ChannelTime.RemoveModifier(freezingTorrentChannelTimeMod);
                        _freezingTorrent.DamageModifiers.RemoveModifier(freezingTorrentDamageMod);
                    }
                };
            }
        );

        Talents.Add(avalanche);
        Talents.Add(wisdomOfTheNorth);
        Talents.Add(soulfrostTorrent);

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
                var target = targets.Where(t => t.Health.GetValue() > 0).FirstOrDefault()
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
                var target = targets.Where(t => t.Health.GetValue() > 0).FirstOrDefault()
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
        _iceBlitzBonusDamage = new Modifier(Modifier.StatModType.MultiplicativePercent, 15);

        _iceBlitz = new Spell(
            id: "ice-blitz",
            name: "Ice Blitz",
            cooldown: 120,
            castTime: 0,
            hasGCD: false,
            canCastWhileCasting: true,
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
                        target.DamageBuffs.AddModifier(_iceBlitzBonusDamage);
                    },
                    onRemove: (caster, target) => { unit.DamageBuffs.RemoveModifier(_iceBlitzBonusDamage); }
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
                Modifier spiritMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 20);

                caster.ApplyBuff(caster, caster, new Aura(
                    id: "winters-blessing",
                    name: "Winters Blessing",
                    duration: 15,
                    tickInterval: 0,
                    onApply: (unit, target) => { unit.ExpertiseStat.AddModifier(spiritMod); },
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
                Modifier damageMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 15);
                
                //Wrath of Winter buff.
                caster.ApplyBuff(caster, caster, new Aura(
                    id: "wrath-of-winter",
                    name: "Wrath of Winter",
                    duration: 20,
                    tickInterval: 2,
                    onTick: (unit, target) => { UpdateWinterOrbs(1); },
                    onApply: (unit, target) => { unit.DamageBuffs.AddModifier(damageMod); },
                    onRemove: (unit, target) => { unit.DamageBuffs.RemoveModifier(damageMod); }
                ));
                
                //Spirit of Heroism Buff.
                caster.ApplyBuff(caster, caster, SpiritOfHeroism);
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
            Anima = 0;
            UpdateWinterOrbs(1);
        }
        OnAnimaUpdate?.Invoke(animaDelta);
    }

    /// <summary>
    /// Updates the Winter Orbs for Rime based on the given delta.
    /// </summary>
    /// <param name="winterOrbsDelta"></param>
    public void UpdateWinterOrbs(int winterOrbsDelta)
    {
        WinterOrbs += winterOrbsDelta;
        if (winterOrbsDelta < 0 && SimRandom.Roll(SpiritStat.GetValue())) WinterOrbs += winterOrbsDelta;
        if(WinterOrbs > MaxWinterOrbs) ConsoleLogger.Log(SimulationLogLevel.Debug, "[bold red]Over Capped Winter Orbs[/b]");
        WinterOrbs = Math.Clamp(WinterOrbs, 0, MaxWinterOrbs);

        OnWinterOrbUpdate?.Invoke(winterOrbsDelta);

        if (winterOrbsDelta > 0 && PrimaryTarget != null)
            _animaSpikes.Cast(this, new List<Unit>() { PrimaryTarget });
    }
}