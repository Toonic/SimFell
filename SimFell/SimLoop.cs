namespace SimFell;

public class SimLoop
{
    private static SimLoop? _instance;
    public static SimLoop Instance => _instance ??= new();
    public event Action<double>? OnUpdate;

    public void Start(Unit player, List<Unit> enemies)
    {
        const double step = 0.5; // simulate half-second per iteration
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
                if (enemies[i].Health <= 0) enemies.RemoveAt(i);
            }
        }
    }
}