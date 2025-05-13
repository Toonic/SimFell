using System.Runtime.InteropServices;
using SimFell.Logging;

namespace SimFell;

public class Spell
{
    public string ID { get; set; }
    public string Name { get; set; }
    private Stat Cooldown { get; set; }
    public double OffCooldown { get; private set; } //When its off cooldown.
    public Stat CastTime { get; set; }
    public bool Channel { get; set; } //When the spell is a channeled spell.
    public Stat ChannelTime { get; set; }
    public Stat TickRate { get; set; }
    public Boolean HasGCD { get; set; }
    public Action<Unit, Spell, List<Unit>>? OnCast { get; set; }
    public Action<Unit, Spell, List<Unit>>? OnTick { get; set; }
    public Func<Unit, bool>? CanCast { get; set; }

    //Modifiers, used typically with Talents.
    public Stat DamageModifiers { get; set; } = new Stat(0);
    public Stat CritModifiers { get; set; } = new Stat(0);

    public Spell(
        string id, string name, double cooldown, double castTime, bool channel = false, double channelTime = 0, double tickRate = 0, bool hasGCD = true,
        Func<Unit, bool>? canCast = null, Action<Unit, Spell, List<Unit>>? onCast = null, Action<Unit, Spell, List<Unit>>? onTick = null)
    {
        ID = id;
        Name = name;
        Cooldown = new Stat(cooldown);
        CastTime = new Stat(castTime);
        Channel = channel;
        ChannelTime = new Stat(channelTime);
        TickRate = new Stat(tickRate);
        HasGCD = hasGCD;
        OnCast = onCast;
        OnTick = onTick;
        CanCast = canCast;
        OffCooldown = 0;
    }

    /// <summary>
    /// Call when updating cooldown from other sources. (EG: On hit, reduce cooldown of X spell by Y).
    /// </summary>
    /// <param name="deltaTime"></param>
    public void UpdateCooldown(double deltaTime)
    {
        if (OffCooldown > 0)
            OffCooldown = Math.Round(OffCooldown - deltaTime, 2);
    }

    public bool CheckCanCast(Unit caster)
    {
        return (CanCast?.Invoke(caster) ?? true) && OffCooldown <= SimLoop.Instance.GetElapsed() && caster.GCD <= SimLoop.Instance.GetElapsed();
    }

    public double GetCastTime(Unit caster)
    {
        return caster.GetHastedValue(CastTime.GetValue());
    }

    public double GetChannelTime(Unit caster)
    {
        return caster.GetHastedValue(ChannelTime.GetValue()); ;
    }

    public double GetTickRate(Unit caster)
    {
        return caster.GetHastedValue(TickRate.GetValue());
    }

    public double GetGCD(Unit caster)
    {
        if (!HasGCD) return 0;
        //TODO: Load in Config for Global GCD.
        return caster.GetHastedValue(1.5);
    }

    public void Cast(Unit caster, List<Unit> targets)
    {
        OnCast?.Invoke(caster, this, targets);
        //Sets the cooldown.
        OffCooldown = Math.Round(Cooldown.GetValue() + SimLoop.Instance.GetElapsed(), 2);  // Reset cooldown after casting
    }

    public void Tick(Unit caster, List<Unit> targets)
    {
        OnTick?.Invoke(caster, this, targets);
        //TODO: Tick Rate handling.
    }
}