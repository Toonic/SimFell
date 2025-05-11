namespace SimFell;

public class Aura
{
    public string ID { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public double TickInterval { get; set; }
    public int MaxStacks { get; set; }

    // Runtime Data
    private double _timeRemaining;
    private double _tickTimer;
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

        _timeRemaining = duration;
        _tickTimer = 0;
        _expired = false;
    }
    
    public double RemainingTime => _timeRemaining;

    public void Refresh()
    {
        //TODO: Pandemic for dots/buffs??
        _timeRemaining = Duration;
        _tickTimer = 0;
    }

    public void Update(double deltaTime, Unit owner)
    {
        if (_expired) return;

        _timeRemaining -= deltaTime;
        _tickTimer += deltaTime;

        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0;
            OnTick?.Invoke(owner);
        }

        if (_timeRemaining <= 0)
        {
            _expired = true;
        }
    }
}