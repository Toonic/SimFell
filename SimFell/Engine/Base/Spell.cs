using System.Runtime.InteropServices;
using SimFell.Logging;
using SimFell.Engine.Base;

namespace SimFell;

public class Spell
{
    public string ID { get; private set; }
    public string Name { get; set; }
    private Stat Cooldown { get; set; }

    public double OffCooldown;
    public Stat CastTime { get; set; }
    public bool Channel { get; set; } //When the spell is a channeled spell.
    public Stat ChannelTime { get; set; } = new Stat(0);
    public Stat TickRate { get; set; }
    public bool HasGCD { get; set; }
    public bool CanCastWhileCasting { get; set; }
    public bool HasAntiSpam { get; set; }
    public bool HasteEffectsCoolodwn { get; set; }
    public int Charges { get; private set; }
    public int MaxCharges { get; set; }
    public Action<Unit, Spell, List<Unit>>? OnCast { get; set; }
    public Action<Unit, Spell, List<Unit>>? OnTick { get; set; }
    public Func<Unit, Spell, bool>? CanCast { get; set; }

    //Modifiers, used typically with Talents.
    public Stat DamageModifiers { get; set; } = new Stat(0);
    public Stat CritModifiers { get; set; } = new Stat(0);

    public Stat ResourceCostModifiers { get; set; } = new Stat(0);

    public Spell(string id, string name, double cooldown = 0, double castTime = 0)
    {
        ID = id.Replace("-", "_");
        Name = name;
        Cooldown = new Stat(cooldown);
        CastTime = new Stat(castTime);

        // Default valeus for all spells unless defined otherwise.
        Channel = false;
        HasGCD = true;
        CanCastWhileCasting = false;
        HasAntiSpam = false;
        HasteEffectsCoolodwn = false;

        Charges = 1;
        MaxCharges = 1;
    }

    public Spell WithOnCast(Action<Unit, Spell, List<Unit>> onCast)
    {
        OnCast = onCast;
        return this;
    }

    public Spell WithOnTick(Action<Unit, Spell, List<Unit>> onTick)
    {
        OnTick = onTick;
        return this;
    }

    public Spell WithCanCast(Func<Unit, Spell, bool> canCast)
    {
        CanCast = canCast;
        return this;
    }

    public Spell HasHastedCooldown()
    {
        HasteEffectsCoolodwn = true;
        return this;
    }

    public Spell HasCharges(int chargeCount)
    {
        Charges = chargeCount;
        MaxCharges = chargeCount;
        return this;
    }

    public Spell IsChanneled(double channelTime, double tickRate)
    {
        Channel = true;
        ChannelTime = new Stat(channelTime);
        TickRate = new Stat(tickRate);
        return this;
    }

    public Spell DisableGCD()
    {
        HasGCD = false;
        return this;
    }

    public Spell EnableCanCastWhileCasting()
    {
        CanCastWhileCasting = true;
        return this;
    }

    [Obsolete("Do not use this constructor. Convert to the new system.")]
    public Spell(
        string id, string name, double cooldown, double castTime, bool channel = false, double channelTime = 0,
        double tickRate = 0, bool hasGCD = true, bool canCastWhileCasting = false,
        bool hasAntiSpam = false, bool hasteEffectsCooldown = false, Func<Unit, Spell, bool>? canCast = null,
        Action<Unit, Spell, List<Unit>>? onCast = null, Action<Unit, Spell, List<Unit>>? onTick = null)
    {
        // Used to flag 
        ConsoleLogger.Log(
            SimulationLogLevel.Error,
            $"Deprecated: {name} is using old constructor still. Update to new constructor.",
            emoji: "⚠️"
        );

        ID = id.Replace("-", "_");
        Name = name;
        Cooldown = new Stat(cooldown);
        CastTime = new Stat(castTime);
        Channel = channel;
        ChannelTime = new Stat(channelTime);
        TickRate = new Stat(tickRate);
        HasGCD = hasGCD;
        HasAntiSpam = hasAntiSpam;
        HasteEffectsCoolodwn = hasteEffectsCooldown;
        CanCastWhileCasting = canCastWhileCasting;
        OnCast = onCast;
        OnTick = onTick;
        CanCast = canCast;
        OffCooldown = 0;
    }

    /// <summary>
    /// Call when updating cooldown from other sources. (EG: On hit, reduce cooldown of X spell by Y).
    /// </summary>
    /// <param name="deltaTime"></param>
    public void UpdateCooldown(Unit caster, double deltaTime)
    {
        if (OffCooldown > 0)
            OffCooldown = OffCooldown - deltaTime;
        UpdateCooldownAndCharges(caster);
    }

    public void SetCurrentCharges(Unit caster, int charges)
    {
        Charges = charges;
        UpdateCooldownAndCharges(caster);
    }

    public bool CheckCanCast(Unit caster)
    {
        UpdateCooldownAndCharges(caster);

        return Charges > 0
               && OffCooldown <= caster.SimLoop.GetElapsed()
               && (CanCastWhileCasting || caster.GCD <= caster.SimLoop.GetElapsed())
               && (CanCast?.Invoke(caster, this) ?? true);
    }

    public double GetCastTime(Unit caster)
    {
        return caster.GetHastedValue(CastTime.GetValue());
    }

    public double GetChannelTime(Unit caster)
    {
        return ChannelTime.GetValue();
        ;
    }

    public double GetTickRate(Unit caster)
    {
        return caster.GetHastedValue(TickRate.GetValue());
    }

    public double GetGCD(Unit caster)
    {
        if (!HasGCD)
            if (HasAntiSpam) return 0.6; //Forced 0.6~ oGCD on all spells to stop people from spamming spells.
            else return 0;

        //TODO: Load in Config for Global GCD.
        return caster.GetHastedValue(1.5);
    }

    public void Cast(Unit caster, List<Unit> targets)
    {
        OnCast?.Invoke(caster, this, targets);
        // Sets the Charges
        Charges--;
        // Sets the cooldown.
        SetOffCooldown(caster);
    }

    private void SetOffCooldown(Unit caster)
    {
        if (HasteEffectsCoolodwn)
            OffCooldown =
                caster.GetHastedValue(Cooldown.GetValue()) +
                caster.SimLoop.GetElapsed(); // Reset cooldown after casting
        else
            OffCooldown =
                Cooldown.GetValue() + caster.SimLoop.GetElapsed(); // Reset cooldown after casting
    }

    private void UpdateCooldownAndCharges(Unit caster)
    {
        if (Charges >= MaxCharges) return;
        if (caster.SimLoop.GetElapsed() >= OffCooldown)
        {
            Charges++;
            if (Charges < MaxCharges)
            {
                double cooldownDuration = HasteEffectsCoolodwn
                    ? caster.GetHastedValue(Cooldown.GetValue())
                    : Cooldown.GetValue();
                OffCooldown = OffCooldown + cooldownDuration;
            }
        }
    }

    /// <summary>
    /// No Cooldown Cast.
    /// </summary>
    /// <param name="caster"></param>
    /// <param name="targets"></param>
    public void FreeCast(Unit caster, List<Unit> targets)
    {
        OnCast?.Invoke(caster, this, targets);
    }

    public void Tick(Unit caster, List<Unit> targets)
    {
        OnTick?.Invoke(caster, this, targets);
        //TODO: Tick Rate handling.
    }
}