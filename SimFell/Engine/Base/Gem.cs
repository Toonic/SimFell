using SimFell.SimFileParser.Enums;
using SimFell.Logging;
namespace SimFell;

public class Gem
{
    public GemType Type { get; }
    public int Power { get; private set; } = 0;
    public bool IsApplied { get; private set; } = false;

    public Action<Unit, Gem>? OnApply { get; set; }

    public Gem(GemType type, Action<Unit, Gem>? onApply)
    {
        Type = type;
        OnApply = onApply;
    }

    public void AddPower(int power) => Power += power;

    public void Apply(Unit unit)
    {
        if (IsApplied) return;

        ConsoleLogger.Log(SimulationLogLevel.Setup, $"Applying {Type} to {unit.Name}");
        OnApply?.Invoke(unit, this);
        IsApplied = true;
    }
}
