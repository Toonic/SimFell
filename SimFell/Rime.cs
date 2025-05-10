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
        
        SpellBook.Add(burstingIce);
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