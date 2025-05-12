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

    public Rime(string name, int health) : base(name, health)
    {
        // Frostboll Spell
        var frostBolt = new Spell(
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
        var burstingIce = new Spell(
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
                    onTick: (caster,target) =>
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
        var coldSnap = new Spell(
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
        var freezingTorrent = new Spell(
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
        var danceOfSwallows = new Spell(
            id: "dance-of-swallows",
            name: "Dance of Swallows",
            cooldown: 60,
            castTime: 0,
            canCast: () => WinterOrbs >= 2,
            onCast: (unit, spell, targets) =>
            {
                UpdateWinterOrbs(-2);
                var target = targets.Where(t => t.Health > 0).FirstOrDefault()
                             ?? throw new Exception("No valid targets");

                // Builds the OnDamage Event.
                Action<Unit, double, object>? onDamageEvent = (unit, damage, source) =>
                {
                    // If the source is from ColdSnap, deal bonus damage.
                    if (source == coldSnap)
                    {
                        const int danceOfSwallowsTriggers = 10;
                        for (int i = 0; i < danceOfSwallowsTriggers; i++)
                        {
                            DealDamage(unit, 53, spell);
                        }
                    }

                    if (source == freezingTorrent)
                    {
                        DealDamage(unit, 53, spell);
                    }
                };

                // Applies the Debuff to the Primary target.
                target.ApplyDebuff(unit, target,new Aura(
                    id: "dance-of-swallows",
                    name: "Dance of Swallows",
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster,target) =>
                    {
                        //Subscribes to the Units OnDamageRecieved event.
                        target.OnDamageReceived += onDamageEvent;
                    },
                    onRemove: (caster,target) =>
                    {
                        //UnSubscribes to the Units OnDamageRecieved event.
                        target.OnDamageReceived -= onDamageEvent;
                    }
                ));
            }
        );

        //Glacial Blast
        var glacialBlast = new Spell(
            id: "glacial-blast",
            name: "Glacial Blast",
            cooldown: 0,
            castTime: 2.0,
            canCast: () => WinterOrbs >= 2,
            onCast: (unit, spell, targets) =>
            {
                var target = targets.FirstOrDefault() ?? throw new Exception("No valid targets");
                UpdateWinterOrbs(-2);
                DealDamage(target, 504, spell);
            }
        );

        //Icy Blitz
        var icyBlitz = new Spell(
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
                    onApply: (caster,target) =>
                    {
                        target.DamageBuffs.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 15,
                            spell));
                    },
                    onRemove: (caster,target) =>
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

        //Spell Priority Order because why not?
        //SpellBook.Add(icyBlitz);
        //SpellBook.Add(danceOfSwallows);
        //SpellBook.Add(coldSnap);
        //SpellBook.Add(burstingIce);
        SpellBook.Add(freezingTorrent);
        //SpellBook.Add(glacialBlast);
        //SpellBook.Add(frostBolt);
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