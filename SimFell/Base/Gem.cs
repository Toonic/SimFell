using SimFell.SimFileParser.Enums;

namespace SimFell;

public class Gem
{
    public GemType Type { get; private set; }
    public int Power { get; private set; } = 0;
    public bool IsApplied { get; private set; } = false;

    public Action<Unit>? OnApply { get; set; }

    public Gem(GemType type, int power, Action<Unit>? onApply)
    {
        Type = type;
        Power = power;
        OnApply = onApply;
    }

    public void IncreasePower(int power) => Power += power;

    public void Apply(Unit unit) => OnApply?.Invoke(unit);
}
