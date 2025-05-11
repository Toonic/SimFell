using SimFell.Logging;

namespace SimFell;

public class Spell
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Cooldown { get; set; }
    public double RemainingCooldown { get; private set; }
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
        RemainingCooldown = 0;
    }

    public void UpdateCooldown(double deltaTime)
    {
        if (RemainingCooldown > 0)
            RemainingCooldown -= deltaTime;
    }

    public bool CheckCanCast(Unit caster)
    {
        return (CanCast?.Invoke() ?? true) && RemainingCooldown <= 0 && caster.GCD <= 0;
    }

    public double GetCastTime(Unit caster)
    {
        return GetFaster(caster, CastTime);
    }

    public double GetChannelTime(Unit caster)
    {
        return GetFaster(caster, ChannelTime);
    }

    public double GetGCD(Unit caster)
    {
        if (!HasGCD) return 0;
        //TODO: Load in Config for Global GCD.
        return GetFaster(caster, 1.5);
    }

    public double GetTickRate(Unit caster, double baseRate)
    {
        return GetFaster(caster, baseRate);
    }

    private double GetFaster(Unit caster, double baseRate)
    {
        if (baseRate == 0) return 0;
        return baseRate / (1 + caster.HasteStat.GetValue() / 100);
    }

    public void Cast(Unit caster, List<Unit> targets)
    {
        //Handle the GCD. 90% of the time we assume we're full channeling.
        //Until someone tells me clipping is better. In which case I'll hate myself.
        ConsoleLogger.Log(
            SimulationLogLevel.CastEvents,
            $"Casting \u001b[1;34m{Name}\u001b[0;30m"
        );
        SimLoop.Instance.Update(GetCastTime(caster));
        OnCast?.Invoke(caster, this, targets);

        //Sets the GCD on the Unit.
        double calculatedGCD = Math.Max(0, GetGCD(caster) - GetCastTime(caster) - GetChannelTime(caster));
        caster.SetGCD(calculatedGCD);

        //Sets the cooldown.
        RemainingCooldown = Cooldown;  // Reset cooldown after casting
    }
}