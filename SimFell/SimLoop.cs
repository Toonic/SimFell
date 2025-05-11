using SimFell.SimFileParser.Models;
using SimFell.Logging;
using Spectre.Console;
using System.Threading.Tasks.Dataflow;

namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action? OnUpdate;
    // Simulate 0.1 th of a second. Or 100 Ticks a Second.
    // For reference, WoW servers run at around a 20 Tickrate.
    private const double step = 0.01;

    private long _ticks;
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
                foreach (var target in targets) target.Health = 999999; //Hacky temp?
            }

            // Stop condition: Health mode
            if (mode == SimulationMode.Health && targets.Count == 0)
                break;

            player.SetPrimaryTarget(targets[0]); //Used mostly for auto-casting abilities. Like Anima Spikes on Rime.
            OnUpdate?.Invoke();
            // Then cast the spell that should cast last.
            if (!player.IsCasting)
            {
                foreach (var spell in player.SpellBook)
                {
                    if (spell.CheckCanCast(player))
                    {
                        player.StartCasting(spell, targets);
                        break; // Only cast one spell at a time
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

    private void OnDamageReceived(Unit unit, float damageReceived, object source)
    {
        //In the future we can keep track of the damage source in a dict and output what each damage was.
        damageDealt += damageReceived;
    }

    public double GetElapsed()
    {
        return _ticks * step;
    }

    public static void ShowConfig(SimFellConfiguration config)
    {
        ConsoleLogger.Log(SimulationLogLevel.Debug, config.ToStringFormatted);
    }

    public static void ShowPrettyConfig(SimFellConfiguration config)
    {
        var grid = new Grid();

        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            new Text("\u001b[1;34mHero\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Hero}\u001b[0m").Centered()
        );
        grid.AddEmptyRow();
        grid.AddRow(
            new Text("\u001b[1;34mIntellect\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Intellect}\u001b[0m").Centered()
        );
        grid.AddRow(
            new Text("\u001b[1;34mCrit\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Crit}\u001b[0m").Centered()
        );
        grid.AddRow(
            new Text("\u001b[1;34mExpertise\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Expertise}\u001b[0m").Centered()
        );
        grid.AddRow(
            new Text("\u001b[1;34mHaste\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Haste}\u001b[0m").Centered()
        );
        grid.AddRow(
            new Text("\u001b[1;34mSpirit\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Spirit}\u001b[0m").Centered()
        );
        grid.AddEmptyRow();
        grid.AddRow(
            new Text("\u001b[1;34mDuration\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Duration}\u001b[0m").Centered()
        );
        grid.AddRow(
            new Text("\u001b[1;34mEnemies\u001b[0m").Centered(),
            new Text($"\u001b[1;33m{config.Enemies}\u001b[0m").Centered()
        );

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine("\n");
    }
}