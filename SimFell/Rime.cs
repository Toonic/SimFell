namespace SimFell;

public class Rime : Unit
{
    private int Anima { get; set; }
    private int WinterOrbs { get; set; }

    private const int MaxAnima = 10;
    private const int MaxWinterOrbs = 5;

    public Rime(string name, int health) : base(name, health)
    {
        var frostBolt = new Spell(
            id: "frost-bolt",
            name:"Frost Bolt",
            cooldown: 0,
            castTime: 1.5f,
            onCast: (unit, spell, targets) =>
            {
                DealDamage(targets.FirstOrDefault(), 73, spell);
                UpdateAnima(3);
            }
        );

        var burstingIce = new Spell(
            id: "bursting-ice",
            name:"Bursting Ice",
            cooldown: 15,
            castTime: 2.0f,
            onCast: (unit, spell, targets) =>
            {
                var primaryTarget = targets.FirstOrDefault();
                primaryTarget?.ApplyDebuff(new Aura(
                    id: "bursting-ice",
                    name:"Bursting Ice",
                    duration:3.15,
                    tickInterval:0.5,
                    onTick: (target) =>
                    {
                        int animaGained = 0;
                        int maxAnimaGainedPerTick = 3;
                        
                        foreach (var unit in targets)
                        {
                            animaGained += 1;
                            DealDamage(unit, 61, spell);
                        }
                        
                        //Maximum of 3 Anima gained per tick.
                        UpdateAnima(Math.Min(maxAnimaGainedPerTick, animaGained));
                    }
                ));
            }
        );

        var coldSnap = new Spell(
            id: "cold-snap",
            name:"Cold Snap",
            cooldown: 8,
            castTime: 0,
            onCast: (unit, spell, targets) =>
            {
                DealDamage(targets.FirstOrDefault(), 73, spell);
                UpdateWinterOrbs(1);
            }
        );

        var danceOfSwallows = new Spell(
            id: "dance-of-swallows",
            name:"Dance of Swallows",
            cooldown: 60,
            castTime: 0,
            canCast: () => WinterOrbs >= 2,
            onCast: (unit, spell, targets) =>
            {
                UpdateWinterOrbs(-2);
                var target = targets.FirstOrDefault();
                
                // Builds the OnDamage Event.
                Action<Unit, float, object> onDamageEvent = (unit, damage, source) =>
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
                    
                    //TODO: Check to see if the damage source is from Soulfrost/Freezing Torrent.
                };
                
                // Applies the Debuff to the Primary target.
                target.ApplyDebuff(new Aura(
                    id: "dance-of-swallows",
                    name:"Dance of Swallows",
                    duration:20,
                    tickInterval:0,
                    onApply: (unit) =>
                    {
                        //Subscribes to the Units OnDamageRecieved event.
                        unit.OnDamageReceived += onDamageEvent;
                    },
                    onRemove: (unit) =>
                    {
                        //UnSubscribes to the Units OnDamageRecieved event.
                        unit.OnDamageReceived -= onDamageEvent;
                    }
                ));
            }
        );
        
        //Spell Priority Order because why not?
        SpellBook.Add(danceOfSwallows);
        SpellBook.Add(burstingIce);
        SpellBook.Add(coldSnap);
        SpellBook.Add(frostBolt);
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
    }
}