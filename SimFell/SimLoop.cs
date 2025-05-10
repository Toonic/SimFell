namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action<double>? OnUpdate;
    private const double step = 0.1; // Simulate 0.1 th of a second.

    public void Start(Unit player, List<Unit> enemies)
    {
        while (enemies.Count > 0)
        {
            OnUpdate?.Invoke(step);

            // Then cast the spell that should cast last.
            foreach (var spell in player.SpellBook)
            {
                if (spell.CheckCanCast(player))
                {
                    spell.Cast(player, enemies);
                    break; // Only cast one spell at a time
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].Health <= 0)
                {
                    enemies[i].Died();
                    enemies.RemoveAt(i);
                }
            }
        }
    }

    public void Update(double delta)
    {
        int steps = (int)(delta / step);
        double remainder = delta % step;

        for (int i = 0; i < steps; i++)
        {
            OnUpdate?.Invoke(step);
        }

        if (remainder > 0)
        {
            OnUpdate?.Invoke(remainder);
        }
    }
}