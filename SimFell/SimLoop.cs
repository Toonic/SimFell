using SimFell.SimFileParser.Models;

namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action<double>? OnUpdate;
    private const double step = 0.1; // Simulate 0.1 th of a second.

    private double elapsed;
    private double damageDealt;

    public enum SimulationMode
    {
        Health,
        Time
    }

    public void Start(Unit player, List<Unit> enemies, SimulationMode mode = SimulationMode.Time, double duration = 60)
    {
        elapsed = 0;
        List<Unit> targets = new List<Unit>();
        foreach (var enemy in enemies)
        {
            targets.Add(enemy);
            enemy.OnDamageReceived += OnDamageReceived;
        }

        while (true)
        {
            // Stop condition: Time mode
            if (mode == SimulationMode.Time && elapsed >= duration)
                break;
            if (mode == SimulationMode.Time)
            {
                foreach (var target in targets) target.Health = 999999; //Hacky temp?
            }

            // Stop condition: Health mode
            if (mode == SimulationMode.Health && targets.Count == 0)
                break;

            player.SetPrimaryTarget(targets[0]); //Used mostly for auto-casting abilities. Like Anima Spikes on Rime.
            OnUpdate?.Invoke(step);
            elapsed += step;

            // Then cast the spell that should cast last.
            foreach (var spell in player.SpellBook)
            {
                if (spell.CheckCanCast(player))
                {
                    spell.Cast(player, targets);
                    break; // Only cast one spell at a time
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
        }

        foreach (var enemy in enemies)
        {
            enemy.OnDamageReceived -= OnDamageReceived;
        }

        Console.WriteLine("--------------");
        Console.WriteLine($"Damage Dealt: {damageDealt}");
        Console.WriteLine($"DPS: {damageDealt / elapsed}");
    }

    private void OnDamageReceived(Unit unit, float damageRecieved, object source)
    {
        //In the future we can keep track of the damage source in a dict and output what each damage was.
        damageDealt += damageRecieved;
    }

    public void Update(double delta)
    {
        int steps = (int)(delta / step);
        double remainder = delta % step;

        for (int i = 0; i < steps; i++)
        {
            OnUpdate?.Invoke(step);
            elapsed += step;
        }

        if (remainder > 0)
        {
            OnUpdate?.Invoke(remainder);
            elapsed += remainder;
        }
    }

    public static void ShowConfig(SimFellConfiguration config)
    {
        Console.WriteLine(config.ToStringFormatted);
    }
}