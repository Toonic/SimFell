using SimFell.SimFileParser.Models;
using SimFell.Logging;
using Spectre.Console;
using System.Runtime.CompilerServices;

namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action? OnUpdate;
    // Simulate 0.1 th of a second. Or 100 Ticks a Second.
    // For reference, WoW servers run at around a 20 Tickrate.
    private const double step = 0.01;

    private double _ticks;
    private double damageDealt;

    public enum SimulationMode
    {
        Health,
        Time
    }

    public void Start(Unit player, List<Unit> enemies, SimulationMode mode = SimulationMode.Time, double duration = 60)
    {
        _ticks = 0;
        List<Unit> targets = new List<Unit>();
        foreach (var enemy in enemies)
        {
            targets.Add(enemy);
            enemy.OnDamageReceived += OnDamageReceived;
        }

        while (true)
        {
            // Stop condition: Time mode
            if (mode == SimulationMode.Time && GetElapsed() >= duration)
                break;
            if (mode == SimulationMode.Time)
            {
                foreach (var target in targets)
                {
                    target.MaximumHealth = 999999;
                    target.Health = 999999; //Hacky temp?
                }
            }

            // Stop condition: Health mode
            if (mode == SimulationMode.Health && targets.Count == 0)
                break;

            player.SetPrimaryTarget(targets[0]); //Used mostly for auto-casting abilities. Like Anima Spikes on Rime.
            OnUpdate?.Invoke(); //Update all Spells/Buffs to be removed first.
            // Then cast the spell that should cast last.
            if (!player.IsCasting)
            {
                foreach (var spell in player.Rotation)
                {
                    if (spell.CheckCanCast(player))
                    {
                        player.StartCasting(spell, targets);
                        if (player.IsCasting) break; // Only cast one spell at a time
                    }
                }
            }
            else if (player.IsCasting)
            {
                foreach (var spell in player.Rotation)
                {
                    if (spell.CheckCanCast(player) && spell.CanCastWhileCasting)
                    {
                        player.StartCasting(spell, targets);
                    }
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (targets[i].Health <= 0)
                {
                    targets[i].Died();
                    targets.RemoveAt(i);
                }
            }

            _ticks++;
        }

        foreach (var enemy in enemies)
        {
            enemy.OnDamageReceived -= OnDamageReceived;
        }

        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, "--------------");
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"Damage Dealt: {damageDealt}");
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"DPS: {damageDealt / GetElapsed()}");
    }

    private void OnDamageReceived(Unit unit, double damageReceived, Spell? spellSource, Aura? auraSource)
    {
        //In the future we can keep track of the damage source in a dict and output what each damage was.
        damageDealt += damageReceived;
    }

    public double GetElapsed()
    {
        return Math.Round(_ticks * step, 2);
    }

    public double GetStep()
    {
        return step;
    }

    public static void ShowConfig(SimFellConfiguration config)
    {
        ConsoleLogger.Log(SimulationLogLevel.Debug, config.ToStringFormatted);
    }

    public static void ShowPrettyConfig(SimFellConfiguration config)
    {
        var table = new Table();
        table.Title = new TableTitle(
            $"{config.Hero} DPS Simulation\n",
            new Style(Color.Grey, decoration: Decoration.Italic)
        );
        table.Border = TableBorder.None;
        table.Width = 50;

        table.AddColumn(new TableColumn(new Text("Attribute", new Style(Color.MediumPurple4, decoration: Decoration.Bold)).Centered()).Width(20));
        table.AddColumn(new TableColumn(new Text("Value", new Style(Color.MediumPurple4, decoration: Decoration.Bold)).Centered()));
        table.AddRow(new Rule(), new Rule());

        table.AddRow(
            new Text("Enemy Count", "blue").Centered(),
            new Text($"{config.Enemies}", "yellow").Centered()
        );
        table.AddRow(
            new Text("Duration", "blue").Centered(),
            new Text($"{config.Duration}s", "yellow").Centered()
        );
        table.AddEmptyRow();

        var activeTalents = config.Player.Talents.Where(t => t.IsActive).ToList();
        table.AddRow(
            new Text($"{(activeTalents.Count > 0 ? "\n" : "")}Talent Tree", "blue").Centered(),
            new Text(
                $"{(activeTalents.Count > 0 ? string.Join("\n", activeTalents.Select(t => t.Name)) : "N/A")}",
                "yellow"
            ).Centered()
        );

        table.AddEmptyRow();
        table.AddRow(
            new Text("\n\nCharacter", "blue").Centered(),
            // new Text(
            //     $"main: {config.Player.MainStat.GetValue()}\n"
            //     + $"crit: {Math.Round(config.Player.CritcalStrikeStat.GetValue(), 2)}% ({config.Player.CritcalStrikeStat.BaseValue})\n"
            //     + $"exp: {Math.Round(config.Player.ExpertiseStat.GetValue(), 2)}% ({config.Player.ExpertiseStat.BaseValue})\n"
            //     + $"haste: {Math.Round(config.Player.HasteStat.GetValue(), 2)}% ({config.Player.HasteStat.BaseValue})\n"
            //     + $"spirit: {Math.Round(config.Player.SpiritStat.GetValue(), 2)}% ({config.Player.SpiritStat.BaseValue})",
            //     "yellow"
            // ).Centered()
            Align.Center(
                new Grid().AddColumn().AddColumn().AddColumn()
                    .AddRow(new Text[]{
                        new ("main:", "yellow"),
                        new ($"{config.Player.MainStat.GetValue()}%", "yellow"),
                        new ($"({config.Player.MainStat.BaseValue})", "yellow"),
                    })
                    .AddRow(new Text[]{
                        new ("crit:", "yellow"),
                        new ($"{Math.Round(config.Player.CritcalStrikeStat.GetValue(), 2)}%", "yellow"),
                        new ($"({config.Player.CritcalStrikeStat.BaseValue})", "yellow"),
                    })
                    .AddRow(new Text[]{
                        new ("exp:", "yellow"),
                        new ($"{Math.Round(config.Player.ExpertiseStat.GetValue(), 2)}%", "yellow"),
                        new ($"({config.Player.ExpertiseStat.BaseValue})", "yellow"),
                    })
                    .AddRow(new Text[]{
                        new ("haste:", "yellow"),
                        new ($"{Math.Round(config.Player.HasteStat.GetValue(), 2)}%", "yellow"),
                        new ($"({config.Player.HasteStat.BaseValue})", "yellow"),
                    })
                    .AddRow(new Text[]{
                        new ("spirit:", "yellow"),
                        new ($"{Math.Round(config.Player.SpiritStat.GetValue(), 2)}%", "yellow"),
                        new ($"({config.Player.SpiritStat.BaseValue})", "yellow"),
                    })
            )
        );

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine(); // Empty line
    }
}