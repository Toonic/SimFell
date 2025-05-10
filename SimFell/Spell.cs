namespace SimFell;

public class Spell
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Cooldown { get; set; }
    public double RemainingCooldown { get; private set; }
    public double CastTime { get; set; }
    public Action<Unit,Spell,List<Unit>>? OnCast { get; set; }
    public Func<bool>? CanCast { get; set; }

    public Spell(
        string id, string name, double cooldown, double castTime,
        Func<bool>? canCast = null, Action<Unit, Spell, List<Unit>>? onCast = null)
    {
        ID = id;
        Name = name;
        Cooldown = cooldown;
        CastTime = castTime;
        OnCast = onCast;
        CanCast = canCast;
        RemainingCooldown = 0;
    }

    public void UpdateCooldown(double deltaTime)
    {
        if (RemainingCooldown > 0)
            RemainingCooldown -= deltaTime;
    }

    public bool CheckCanCast(Unit caster)
    {
        return (CanCast?.Invoke() ?? true) && RemainingCooldown <= 0;
    }

    public void Cast(Unit caster, List<Unit> targets)
    {
        OnCast?.Invoke(caster, this, targets);
        RemainingCooldown = Cooldown;  // Reset cooldown after casting
    }
}