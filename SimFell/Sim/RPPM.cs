namespace SimFell;

public class RPPM
{
    private readonly double _ppm; // Proc per minute
    private double _lastProcAttemptTime;

    public RPPM(double ppm)
    {
        _ppm = ppm;
        _lastProcAttemptTime = 0;
    }

    /// <summary>
    /// Attempts to proc based on time since last check. Returns true if proc occurs.
    /// </summary>
    public bool TryProc(Unit unit)
    {
        double currentTime = unit.Simulator.Now;
        double deltaTime = currentTime - _lastProcAttemptTime;
        _lastProcAttemptTime = currentTime;

        // Axel 2025/07/08 - "Yes, it always accumulates. As long as you are "trying to proc it" at least once every 20-30 seconds (I forgot the time cap where it resets now)."
        if (deltaTime >= 30)
            deltaTime = 0;

        if (deltaTime <= 0)
            return false;

        double procChance = (_ppm * deltaTime) / 60.0 * 100.0 * (1 + unit.HasteStat.GetValue() / 100);

        return SimRandom.Roll(procChance);
    }

    /// <summary>
    /// Resets the internal timer (optional if needed).
    /// </summary>
    public void Reset(Unit unit, bool resetBasedOnElapsedTime = false)
    {
        _lastProcAttemptTime = resetBasedOnElapsedTime ? unit.Simulator.Now : 0;
    }
}