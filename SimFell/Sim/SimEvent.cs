namespace SimFell.Sim;

public class SimEvent : IComparable<SimEvent>
{
    public double Time { get; private set; }
    public Action Action { get; private set; }
    public double Duration { get; private set; }
    public double StartTime { get; private set; }

    private bool _hastedEvent;
    private Unit? _caster;
    private Simulator _simulator;

    public SimEvent()
    {
    }

    public SimEvent(Simulator sim, Unit caster, double duration, Action action, bool hastedEvent = true)
    {
        Config(sim, caster, duration, action, hastedEvent);
    }

    public void Config(Simulator sim, Unit caster, double duration, Action action, bool hastedEvent = true)
    {
        _simulator = sim;
        _caster = caster;
        Action = action;
        _hastedEvent = hastedEvent;
        StartTime = sim.Now;
        Duration = duration;

        Time = StartTime + GetAdjustedDuration();

        if (_hastedEvent && _caster != null)
        {
            _caster.HasteStat.OnModifierAdded += OnHasteChanged;
            _caster.HasteStat.OnModifierRemoved += OnHasteChanged;
        }
    }

    private double GetAdjustedDuration()
    {
        if (!_hastedEvent || _caster == null) return Duration;
        return Math.Round(_caster.GetHastedValue(Duration), 2);
    }

    private void OnHasteChanged()
    {
        if (!_hastedEvent || _caster == null) return;

        double now = _simulator.Now;
        double elapsed = Math.Max(0.0, now - StartTime); // protect against StartTime in the future
        double adjustedTotalDuration = GetAdjustedDuration(); // duration measured from StartTime

        // remaining time = total duration (from StartTime) minus elapsed time already spent
        double remaining = adjustedTotalDuration - elapsed;

        // if remaining <= 0, fire as soon as possible (now); otherwise schedule at now + remaining
        Time = (remaining <= 0.0) ? now : (now + remaining);

        _simulator.RescheduleEvent(this);
    }

    public void ResetTime()
    {
        Time = _simulator.Now + GetAdjustedDuration();
    }

    public void UpdateTime(double delta)
    {
        Time += delta;
        Time = Math.Max(_simulator.Now, Time);
        _simulator.RescheduleEvent(this);
    }

    public void Unsubscribe()
    {
        if (_caster != null)
        {
            _caster.HasteStat.OnModifierAdded -= OnHasteChanged;
            _caster.HasteStat.OnModifierRemoved -= OnHasteChanged;
        }
    }

    public int CompareTo(SimEvent other) => Time.CompareTo(other.Time);
}