using System.Diagnostics;

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
    private double _removeAt;
    private double _lastTick;
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

    public Aura(string id, string name, double duration, double tickInterval, int maxStacks = 1,
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
        _hasPartialTicks = true;
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
        _removeAt = Duration + _caster.SimLoop.GetElapsed();
        _lastTick = _caster.SimLoop.GetElapsed();
        OnApply?.Invoke(caster, target);
    }

    public void Remove()
    {
        _expired = true;
        if (_hasPartialTicks) DoTick();
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

    public double GetDuration()
    {
        return Math.Min(_removeAt - _caster.SimLoop.GetElapsed(), 0);
    }

    public Aura WithoutPartialTicks()
    {
        _hasPartialTicks = false;
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
        _spellSource = spellSource;
        _damageMin = scaleDamageOnTicks ? minDamage / (Duration / TickInterval.GetValue()) : minDamage;
        _damageMax = scaleDamageOnTicks ? maxDamage / (Duration / TickInterval.GetValue()) : maxDamage;
        _includeCriticalStrike = includeCriticalStrike;
        _includeExpertise = includeExpertise;
        _isFlatDamage = isFlatDamage;

        OnTick += (caster, target, aura) =>
        {
            int dmg = SimRandom.Next((int)_damageMin, (int)_damageMax);

            if (_expired && _hasPartialTicks)
            {
                double hastedInterval = GetTickInterval();
                // Calculate how much time is left relative to the next tick
                double timeUntilNextTick = (GetTickInterval() + _lastTick) - caster.SimLoop.GetElapsed();
                // Fraction of tick completed before expiration
                double partialFraction = Math.Max(0, 1.0 - (timeUntilNextTick / hastedInterval));
                dmg = (int)(dmg * partialFraction);
                if (dmg == 0) return;
            }

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
        _removeAt = Duration + _caster.SimLoop.GetElapsed();
        if (resetLastTick)
            _lastTick = _caster.SimLoop.GetElapsed();
    }

    public void UpdateDuration(double delta)
    {
        _removeAt += delta;
    }

    private void DoTick()
    {
        OnTick?.Invoke(_caster, _target, this);
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
            DoTick();
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

    public void Update(double simTime)
    {
        if (_expired) return;
        if (GetTickInterval() > 0)
        {
            while (simTime >= _lastTick + GetTickInterval())
            {
                _lastTick = _caster.SimLoop.GetElapsed();
                DoTick();
            }
        }

        if (simTime >= _removeAt)
        {
            _expired = true;
        }
    }
}