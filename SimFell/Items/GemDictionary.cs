using SimFell.SimFileParser.Enums;

namespace SimFell.Items;

public class GemDictionary
{
    private const int POWER_BASE = 100;

    public List<Gem> GemList { get; private set; } = new()
    {
        new Gem(GemType.RUBY, (unit, gem) => {
            if (gem.Power >= POWER_BASE)
                if (unit.Health / unit.MaximumHealth >= 0.8)
                    unit.MainStat.AddModifier(new Modifier(Modifier.StatModType.MultiplicativePercent, 4));
            // More ifs
        }),
        // More gems
    };
}