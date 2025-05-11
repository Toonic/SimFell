using System.Runtime.InteropServices;
using SimFell.Logging;

namespace SimFell;

public class Spell
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Cooldown { get; set; }
    public double OffCooldown { get; private set; }
    public double CastTime { get; set; }
    public double ChannelTime { get; set; }
    public Boolean HasGCD { get; set; }
    public Action<Unit, Spell, List<Unit>>? OnCast { get; set; }
    public Func<bool>? CanCast { get; set; }

    public Spell(
        string id, string name, double cooldown, double castTime, double channelTime = 0, bool hasGCD = true,
        Func<bool>? canCast = null, Action<Unit, Spell, List<Unit>>? onCast = null)
    {
        ID = id;
        Name = name;
        Cooldown = cooldown;
        CastTime = castTime;
        ChannelTime = channelTime;
        HasGCD = hasGCD;
        OnCast = onCast;
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
            OffCooldown -= deltaTime;
    }

    public bool CheckCanCast(Unit caster)
    {
        return (CanCast?.Invoke() ?? true) && OffCooldown <= SimLoop.Instance.GetElapsed() && caster.GCD <= SimLoop.Instance.GetElapsed();
    }

    public double GetCastTime(Unit caster)
    {
        return caster.GetHastedValue(CastTime);
    }

    public double GetChannelTime(Unit caster)
    {
        return caster.GetHastedValue(ChannelTime);
    }

    public double GetGCD(Unit caster)
    {
        if (!HasGCD) return 0;
        //TODO: Load in Config for Global GCD.
        return caster.GetHastedValue(1.5);
    }

    public double GetTickRate(Unit caster, double baseRate)
    {
        return caster.GetHastedValue(baseRate);
    }

    public void Cast(Unit caster, List<Unit> targets)
    {
        OnCast?.Invoke(caster, this, targets);
        //Sets the cooldown.
        OffCooldown = Cooldown + SimLoop.Instance.GetElapsed();  // Reset cooldown after casting
    }

    public void Tick(Unit caster, List<Unit> targets)
    {
        //TODO: Channeling
    }
}