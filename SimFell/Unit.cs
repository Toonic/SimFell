namespace SimFell;

public class Unit : SimLoopListener
{
    public string Name { get; set; }
    public int Health { get; set; }
    public List<Aura> Buffs { get; set; }
    public List<Aura> Debuffs { get; set; }
    public List<Spell> SpellBook { get; set; }

    // Multipliers
    public double DamageReceivedMultiplier { get; set; } = 1.0f;
    
    public Unit(string name, int health)
    {
        Name = name;
        Health = health;
        Buffs = [];
        Debuffs = [];
        SpellBook = [];
    }

    public void ApplyBuff(Aura buff)
    {
        Buffs.Add(buff);
        Console.WriteLine($"{Name} gains buff: {buff.Name}");
        buff.OnApply?.Invoke(this);
    }

    public void ApplyDebuff(Aura debuff)
    {
        Debuffs.Add(debuff);
        Console.WriteLine($"{Name} gains debuff: {debuff.Name}");
        debuff.OnApply?.Invoke(this);
    }

    public void DealDamage(Unit target, int amount)
    {
        target.TakeDamage(amount);
        Console.WriteLine($"{Name} deals {amount} damage to {target.Name}.");
    }

    public void TakeDamage(int amount)
    {
        var totalDamage = (int)(amount * DamageReceivedMultiplier);
        Health -= totalDamage;
        Console.WriteLine(
            $"{Name} takes {totalDamage} ({Math.Round((DamageReceivedMultiplier - 1) * 100, 2)}% increase)"
            + $" damage. Remaining health: {Health}"
        );
    }

    protected override void Update(double deltaTime)
    {
        // Update buffs
        for (int i = Buffs.Count - 1; i >= 0; i--)
        {
            Buffs[i].Update(deltaTime, this);
            if (Buffs[i].IsExpired)
            {
                Console.WriteLine($"{Name} loses buff: {Buffs[i].Name}");
                Buffs[i].OnRemove?.Invoke(this);
                Buffs.RemoveAt(i);
            }
        }

        // Update debuffs
        for (int i = Debuffs.Count - 1; i >= 0; i--)
        {
            Debuffs[i].Update(deltaTime, this);
            if (Debuffs[i].IsExpired)
            {
                Console.WriteLine($"{Name} loses debuff: {Debuffs[i].Name}");
                Debuffs[i].OnRemove?.Invoke(this);
                Debuffs.RemoveAt(i);
            }
        }

        foreach (var spell in SpellBook)
        {
            spell.UpdateCooldown(deltaTime);
        }
    }
}