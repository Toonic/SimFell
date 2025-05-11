namespace SimFell;

public class Aura
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public double TickInterval { get; set; }
    public int MaxStacks { get; set; }

    // Runtime Data
    private double _removeAt;
    private double _nextTick;
    private bool _expired;

    //Owner its on.
    public Action<Unit>? OnTick;
    public Action<Unit>? OnApply;
    public Action<Unit>? OnRemove;

    public bool IsExpired => _expired;

    public Aura(string id, string name, double duration, double tickInterval, int maxStacks = 1000,
        Action<Unit>? onTick = null,
        Action<Unit>? onApply = null,
        Action<Unit>? onRemove = null)
    {
        ID = id;
        Name = name;
        Duration = duration;
        TickInterval = tickInterval;
        MaxStacks = maxStacks;
        OnTick = onTick;
        OnApply = onApply;
        OnRemove = onRemove;

        _removeAt = 0;
        _expired = false;
    }

    public void Refresh()
    {
        //TODO: Pandemic for dots/buffs??
        _removeAt = Duration + SimLoop.Instance.GetElapsed();
    }

    public void Apply(Unit unit)
    {
        _removeAt = Duration + SimLoop.Instance.GetElapsed();
        //TODO: TickInterval should take into consideration haste.
        _nextTick = TickInterval + SimLoop.Instance.GetElapsed();
        OnApply?.Invoke(unit);
    }

    public void Update(double simTime, Unit owner)
    {
        if (_expired) return;

        while (simTime >= _nextTick)
        {
            _nextTick += TickInterval;
            OnTick?.Invoke(owner);
        }
        
        if (simTime >= _removeAt)
        {
            _expired = true;
        }
    }
}