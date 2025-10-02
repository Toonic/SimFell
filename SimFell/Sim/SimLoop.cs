using SimFell.Logging;
using Spectre.Console;
using SimFell.Engine.Base;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SimFell;

public class SimLoop
{
    public event Action<double, double>? OnUpdate;

    // Simulate 0.1 th of a second. Or 100 Ticks a Second.
    // For reference, WoW servers run at around a 10 Tickrate.
    private const double step = 0.033;

    private double _ticks;
    private double totalDamage;
    private double totalTime;

    private ConcurrentDictionary<string, SpellStats> _spellStats = new ConcurrentDictionary<string, SpellStats>();
    private ConcurrentDictionary<string, float> _resourcesGenerated = new ConcurrentDictionary<string, float>();

    public void Start(Unit player, List<Unit> enemies, double duration = 60)
    {
        _ticks = 0;

        _spellStats.Clear();
        _resourcesGenerated.Clear();

        player.SetSimLoop(this);
        List<Unit> targets = new List<Unit>();
        foreach (var enemy in enemies)
        {
            targets.Add(enemy);
            enemy.SetSimLoop(this);
            enemy.OnDamageReceived += OnDamageReceived;
        }

        player.Targets = targets;
        player.OnCast += OnCast;

        while (true)
        {
            double elapsedTime = GetElapsed();
            if (elapsedTime >= duration)
                break;

            player.SetPrimaryTarget(targets[0]); //Used mostly for auto-casting abilities. Like Anima Spikes on Rime.
            OnUpdate?.Invoke(elapsedTime, _ticks); //Update all Spells/Buffs to be removed first.

            // Then cast the spell that should cast last.
            if (!player.IsCasting)
            {
                foreach (var action in player.SimActions)
                {
                    if (action.CanExecute(player))
                    {
                        player.StartCasting(action.Spell, targets);
                        if (player.IsCasting) break; // Only cast one spell at a time
                    }
                }
            }
            else if (player.IsCasting)
            {
                foreach (var action in player.SimActions)
                {
                    if (action.Spell.CanCastWhileCasting && action.CanExecute(player))
                    {
                        player.StartCasting(action.Spell, targets);
                        if (player.IsCasting) break; // Only cast one spell at a time
                    }
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (targets[i].Health.GetValue() <= 0)
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
            enemy.Stop();
        }

        player.OnCast -= OnCast;
        player.Stop();

        totalDamage = _spellStats.Values.Sum(s => s.TotalDamage);
        totalTime = GetElapsed();
    }

    private void OnCast(Unit arg1, Spell spellSource, List<Unit> arg3)
    {
        string spellName = spellSource?.Name ?? "Unknown";
        _spellStats.AddOrUpdate(
            spellName,
            _ => new SpellStats
            {
                SpellName = spellName,
                TotalDamage = 0,
                Ticks = 0,
                LargestHit = 0,
                SmallestHit = 0,
                CritCount = 0,
                Casts = 1
            },
            (key, existingStats) =>
            {
                existingStats.Casts++;
                return existingStats;
            });
    }

    public double GetDPS()
    {
        return totalDamage / totalTime;
    }

    private void OnDamageReceived(Unit unit, double damageReceived, Spell? spellSource, bool isCritical)
    {
        string spellName = spellSource?.Name ?? "Unknown";
        _spellStats.AddOrUpdate(
            spellName,
            _ => new SpellStats
            {
                SpellName = spellName,
                TotalDamage = damageReceived,
                Ticks = 1,
                LargestHit = damageReceived,
                SmallestHit = damageReceived,
                CritCount = isCritical ? 1 : 0,
            },
            (key, existingStats) =>
            {
                existingStats.TotalDamage += damageReceived;
                existingStats.Ticks++;
                if (damageReceived > existingStats.LargestHit)
                {
                    existingStats.LargestHit = damageReceived;
                }

                if (damageReceived < existingStats.SmallestHit || existingStats.SmallestHit == 0)
                {
                    existingStats.SmallestHit = damageReceived;
                }

                if (isCritical)
                {
                    existingStats.CritCount++;
                }

                return existingStats;
            });
    }

    public double GetElapsed()
    {
        return _ticks * step;
    }

    public double GetStep()
    {
        return step;
    }

    public ConcurrentDictionary<string, SpellStats> GetSpellStats()
    {
        return _spellStats;
    }
}