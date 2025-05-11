using SimFell.SimFileParser.Models;
using SimFell.Logging;
using Spectre.Console;
using System.Threading.Tasks.Dataflow;

namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action<double>? OnUpdate;
    private const double step = 0.01; // Simulate 0.1 th of a second.

    private double p_Elapsed;
    private double damageDealt;

    public enum SimulationMode
    {
        Health,
        Time
    }

    public void Start(Unit player, List<Unit> enemies, SimulationMode mode = SimulationMode.Time, double duration = 60)
    {
        p_Elapsed = 0;
        List<Unit> targets = new List<Unit>();
        foreach (var enemy in enemies)
        {
            targets.Add(enemy);
            enemy.OnDamageReceived += OnDamageReceived;
        }

        while (true)
        {
            // Stop condition: Time mode
            if (mode == SimulationMode.Time && p_Elapsed >= duration)
                break;
            if (mode == SimulationMode.Time)
            {
                foreach (var target in targets) target.Health = 999999; //Hacky temp?
            }

            // Stop condition: Health mode
            if (mode == SimulationMode.Health && targets.Count == 0)
                break;

            player.SetPrimaryTarget(targets[0]); //Used mostly for auto-casting abilities. Like Anima Spikes on Rime.

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
            
            p_Elapsed += step;
            OnUpdate?.Invoke(step);
        }

        foreach (var enemy in enemies)
        {
            enemy.OnDamageReceived -= OnDamageReceived;
        }

        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, "--------------");
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"Damage Dealt: {damageDealt}");
        ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"DPS: {damageDealt / p_Elapsed}");
    }

    private void OnDamageReceived(Unit unit, float damageReceived, object source)
    {
        //In the future we can keep track of the damage source in a dict and output what each damage was.
        damageDealt += damageReceived;
    }

    public void Update(double delta)
    {
        if (delta == 0) return;
        ConsoleLogger.Log(
            SimulationLogLevel.Debug,
            $"Time Delta: \u001b[1;36m{delta}\u001b[0;30m"
        );
        int steps = (int)(delta / step);
        double remainder = delta % step;

        for (int i = 0; i < steps; i++)
        {
            OnUpdate?.Invoke(step);
            p_Elapsed += step;
        }

        if (remainder >= step)
        {
            OnUpdate?.Invoke(remainder);
            p_Elapsed += remainder;
        }
    }

    public double GetElapsed()
    {
        return p_Elapsed;
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