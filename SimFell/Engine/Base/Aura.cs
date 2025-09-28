namespace SimFell;

public class Aura
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public double TickInterval { get; set; }
    public int MaxStacks { get; set; }
    public int CurrentStacks { get; set; }

    // Runtime Data
    private Unit _caster;
    private Unit _target;
    private double _removeAt;
    private double _nextTick;
    private bool _expired;

    //Owner its on.
    public Action<Unit, Unit>? OnTick;
    public Action<Unit, Unit>? OnApply;
    public Action<Unit, Unit>? OnRemove;
    public Action<Unit, Unit>? OnIncreaseStack;

    public bool IsExpired => _expired;

    public Aura(string id, string name, double duration, double tickInterval, int maxStacks = 1000,
        Action<Unit, Unit>? onTick = null,
        Action<Unit, Unit>? onApply = null,
        Action<Unit, Unit>? onRemove = null)
    {
        ID = id.Replace("-", "_");
        Name = name;
        Duration = duration;
        TickInterval = tickInterval;
        CurrentStacks = 1;
        MaxStacks = maxStacks;
        OnTick = onTick;
        OnApply = onApply;
        OnRemove = onRemove;

        _expired = false;
    }

    public void Apply(Unit caster, Unit target)
    {
        _expired = false;
        _caster = caster;
        _target = target;
        _removeAt = Duration + caster.SimLoop.GetElapsed();
        _nextTick = Math.Round(_caster.GetHastedValue(TickInterval) + caster.SimLoop.GetElapsed(), 2);
        OnApply?.Invoke(caster, target);
    }

    public void Remove()
    {
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

    public Aura WithOnApply(Action<Unit, Unit>? onApply)
    {
        OnApply += onApply;
        return this;
    }

    public Aura WithOnRemove(Action<Unit, Unit>? onRemove)
    {
        onRemove += onRemove;
        return this;
    }

    public Aura WithOnTick(Action<Unit, Unit>? onTick)
    {
        OnTick += onTick;
        return this;
    }

    public Aura WithIncreaseStacks(Action<Unit, Unit>? onIncreaseStack)
    {
        OnIncreaseStack += onIncreaseStack;
        return this;
    }

    public void ResetDuration()
    {
        _removeAt = Duration + _caster.SimLoop.GetElapsed();
    }

    public void UpdateDuration(double delta)
    {
        _removeAt += delta;
    }

    public void Update(double simTime)
    {
        if (_expired) return;
        if (TickInterval > 0)
        {
            while (simTime >= _nextTick)
            {
                _nextTick = Math.Round(_nextTick + _caster.GetHastedValue(TickInterval), 2);
                OnTick?.Invoke(_caster, _target);
            }
        }

        if (simTime >= _removeAt)
        {
            _expired = true;
        }
    }
}