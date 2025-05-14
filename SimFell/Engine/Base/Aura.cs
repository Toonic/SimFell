namespace SimFell;

public class Aura
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public double TickInterval { get; set; }
    public int MaxStacks { get; set; }

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

    public bool IsExpired => _expired;

    public Aura(string id, string name, double duration, double tickInterval, int maxStacks = 1000,
        Action<Unit, Unit>? onTick = null,
        Action<Unit, Unit>? onApply = null,
        Action<Unit, Unit>? onRemove = null)
    {
        ID = id;
        Name = name;
        Duration = duration;
        TickInterval = tickInterval;
        MaxStacks = maxStacks;
        OnTick = onTick;
        OnApply = onApply;
        OnRemove = onRemove;

        _expired = false;
    }

    public void Refresh()
    {
        //TODO: Pandemic for dots/buffs??
        _removeAt = Duration + SimLoop.Instance.GetElapsed();
    }

    public void Apply(Unit caster, Unit target)
    {
        _expired = false;
        _caster = caster;
        _target = target;
        _removeAt = Duration + SimLoop.Instance.GetElapsed();
        _nextTick = Math.Round(_caster.GetHastedValue(TickInterval) + SimLoop.Instance.GetElapsed(), 2);
        OnApply?.Invoke(caster, target);
    }

    public void Remove()
    {
        OnRemove?.Invoke(_caster, _target);
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