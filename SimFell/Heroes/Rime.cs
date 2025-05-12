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
    private Spell _icyBlitz;

    public Rime(int health) : base("Rime", health)
    {
        ConfigureSpellBook();
        ConfigureTalents();
    }

    //TODO: Make this so it can also take in the gridpos/Different override?
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

        //Chillblain Talent
        var chillBlain = new Talent(
            id: "chillblain",
            name: "Chillblain",
            gridPos: "1.1",
            onActivate: unit =>
            {
                _freezingTorrent.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 20, unit));
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

        Talents.Add(chillBlain);
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

        //Icy Blitz
        _icyBlitz = new Spell(
            id: "icy-blitz",
            name: "Icy Blitz",
            cooldown: 120,
            castTime: 0,
            //TODO: No GCD
            //TODO: Can Cast while Casting.
            onCast: (unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "icy-blitz",
                    name: "Icy Blitz",
                    maxStacks: 1,
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) =>
                    {
                        target.DamageBuffs.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 15,
                            spell));
                    },
                    onRemove: (caster, target) =>
                    {
                        unit.DamageBuffs.RemoveModifier(spell);
                    }
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

        SpellBook.Add(_icyBlitz);
        SpellBook.Add(_danceOfSwallows);
        SpellBook.Add(_coldSnap);
        SpellBook.Add(_burstingIce);
        SpellBook.Add(_freezingTorrent);
        SpellBook.Add(_glacialBlast);
        SpellBook.Add(_frostBolt);
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

        if (PrimaryTarget != null)
            _animaSpikes.Cast(this, new List<Unit>() { PrimaryTarget });
    }
}