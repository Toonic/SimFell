using System.Diagnostics;
using SimFell.Sim;

namespace SimFell;

public class Aura
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public Stat TickInterval { get; set; }
    public int MaxStacks { get; set; }
    public int CurrentStacks { get; set; }

    // Runtime Data
    private Unit _caster;
    private Unit _target;
    private bool _expired;
    private bool _hasPartialTicks;

    // Events
    public Action<Unit, Unit, Aura>? OnTick;
    public Action<Unit, Unit>? OnApply;
    public Action<Unit, Unit>? OnRemove;
    public Action<Unit, Unit>? OnIncreaseStack;

    // Damage Values
    private Spell _spellSource;
    private double _damageMin;
    private double _damageMax;
    private bool _includeCriticalStrike;
    private bool _includeExpertise;
    private bool _isFlatDamage;

    public bool IsExpired => _expired;

    private SimEvent _removeEvent;
    private SimEvent _tickEvent;

    public Aura(string id, string name, double duration, double tickInterval,
        int maxStacks = 1,
        Action<Unit, Unit, Aura>? onTick = null,
        Action<Unit, Unit>? onApply = null,
        Action<Unit, Unit>? onRemove = null)
    {
        ID = id.Replace("-", "_");
        Name = name;
        Duration = duration;
        TickInterval = new Stat(tickInterval);
        CurrentStacks = 1;
        MaxStacks = maxStacks;
        _hasPartialTicks = false;
        OnTick = onTick;
        OnApply = onApply;
        OnRemove = onRemove;

        _expired = false;
    }

    public Unit GetTarget()
    {
        return _target;
    }

    public double GetTickInterval()
    {
        return _caster.GetHastedValue(TickInterval.GetValue());
    }

    public void Apply(Unit caster, Unit target)
    {
        _expired = false;
        _caster = caster;
        _target = target;
        //_lastTick = _caster.Simulator.Now;
        OnApply?.Invoke(caster, target);

        _removeEvent = new SimEvent(caster.Simulator, caster, Duration, () =>
        {
            target.RemoveBuff(this);
            target.RemoveDebuff(this);
        }, false);
        caster.Simulator.Schedule(_removeEvent);

        if (TickInterval.GetValue() > 0)
        {
            _tickEvent = new SimEvent(caster.Simulator, caster, TickInterval.GetValue(), () => DoTick());
            caster.Simulator.Schedule(_tickEvent);
        }
    }

    public void Remove()
    {
        _expired = true;
        if (TickInterval.GetValue() > 0)
        {
            if (_hasPartialTicks)
            {
                double partialTickPercentage =
                    (_caster.Simulator.Now - _tickEvent.StartTime) / (_tickEvent.Time - _tickEvent.StartTime);

                if (partialTickPercentage < 1 && partialTickPercentage > 0)
                {
                    double oldMinDamage = _damageMin;
                    double oldMaxDamage = _damageMax;
                    _damageMin *= partialTickPercentage;
                    _damageMax *= partialTickPercentage;
                    DoTick(false);
                    _damageMin = oldMinDamage;
                    _damageMax = oldMaxDamage;
                }

                Console.WriteLine(partialTickPercentage);
            }

            _caster.Simulator.UnSchedule(_tickEvent);
        }

        OnRemove?.Invoke(_caster, _target);
    }

    public void IncreaseStack()
    {
        CurrentStacks++;
        CurrentStacks = Math.Min(CurrentStacks, MaxStacks);
        OnIncreaseStack?.Invoke(_caster, _target);
    }

    public void DecreaseStack()
    {
        CurrentStacks--;
        CurrentStacks = Math.Max(CurrentStacks, 0);
        if (CurrentStacks == 0) Remove();
    }

    public double GetRemainingDuration()
    {
        return Math.Min(_removeEvent.Time - _caster.Simulator.Now, 0);
    }

    public Aura WithoutPartialTicks()
    {
        _hasPartialTicks = false;
        return this;
    }

    public Aura WithPartialTicks()
    {
        _hasPartialTicks = true;
        return this;
    }

    public Aura WithOnApply(Action<Unit, Unit>? onApply)
    {
        OnApply += onApply;
        return this;
    }

    public Aura WithOnRemove(Action<Unit, Unit>? onRemove)
    {
        OnRemove += onRemove;
        return this;
    }

    public Aura WithOnTick(Action<Unit, Unit, Aura>? onTick)
    {
        OnTick += onTick;
        return this;
    }

    public void ClearOnTick()
    {
        OnTick = null;
    }

    public Aura WithDamageOnTick(Spell spellSource, double minDamage, double maxDamage, bool scaleDamageOnTicks = false,
        bool includeCriticalStrike = true, bool includeExpertise = true, bool isFlatDamage = false)
    {
        _hasPartialTicks = true;
        _spellSource = spellSource;
        _damageMin = scaleDamageOnTicks ? minDamage / (Duration / TickInterval.GetValue()) : minDamage;
        _damageMax = scaleDamageOnTicks ? maxDamage / (Duration / TickInterval.GetValue()) : maxDamage;
        _includeCriticalStrike = includeCriticalStrike;
        _includeExpertise = includeExpertise;
        _isFlatDamage = isFlatDamage;

        OnTick += (caster, target, aura) =>
        {
            int dmg = SimRandom.Next((int)_damageMin, (int)_damageMax);
            caster.DealDamage(target, dmg, _spellSource, _includeCriticalStrike, _includeExpertise, _isFlatDamage);
        };

        return this;
    }

    public Aura WithIncreaseStacks(Action<Unit, Unit>? onIncreaseStack)
    {
        OnIncreaseStack += onIncreaseStack;
        return this;
    }

    public void ResetDuration(bool resetLastTick = false)
    {
        _removeEvent.ResetTime();
        if (resetLastTick) _tickEvent.ResetTime();
    }

    public void UpdateDuration(double delta)
    {
        _removeEvent.UpdateTime(delta);
    }

    private void DoTick(bool scheduleNextTick = true)
    {
        OnTick?.Invoke(_caster, _target, this);

        if (scheduleNextTick)
        {
            _tickEvent = new SimEvent(_caster.Simulator, _caster, TickInterval.GetValue(), () => DoTick());
            _caster.Simulator.Schedule(_tickEvent);
        }
    }

    public void DoBonusInstantTicks(double durationInSeconds)
    {
        if (_expired || GetTickInterval() <= 0 || OnTick == null) return;

        // Calculate how many ticks would occur in the given duration
        double hastedInterval = GetTickInterval();
        int bonusTicks = (int)Math.Floor(durationInSeconds / hastedInterval);

        // Apply the effect for that many ticks
        for (int i = 0; i < bonusTicks; i++)
        {
            DoTick(false);
        }
    }

    public double GetSimulatedDamage(double durationInSeconds)
    {
        return GetSimulatedDamage(durationInSeconds, _includeCriticalStrike, _includeExpertise, _isFlatDamage);
    }

    public double GetSimulatedDamage(double durationInSeconds, bool includeCriticalStrike,
        bool includeExpertise, bool isFlatDamage)
    {
        if (_expired || GetTickInterval() <= 0 || OnTick == null) return 0;

        // Calculate how many ticks would occur in the given duration
        double hastedInterval = GetTickInterval();
        double bonusTicks = durationInSeconds / hastedInterval;
        int bonusFullTicks = (int)Math.Floor(bonusTicks);
        double partialTick = bonusTicks - bonusFullTicks;
        double totalDamage = 0;

        // Apply the effect for that many ticks
        int dmg;
        for (int i = 0; i < bonusFullTicks; i++)
        {
            dmg = SimRandom.Next((int)_damageMin, (int)_damageMax);
            totalDamage += _caster.GetDamage(_target, dmg, _spellSource, includeCriticalStrike, includeExpertise,
                isFlatDamage).damage;
        }

        if (_hasPartialTicks)
        {
            dmg = SimRandom.Next((int)_damageMin, (int)_damageMax);
            totalDamage += partialTick * _caster.GetDamage(_target, dmg, _spellSource, includeCriticalStrike,
                includeExpertise,
                isFlatDamage).damage;
        }

        return totalDamage;
    }
}