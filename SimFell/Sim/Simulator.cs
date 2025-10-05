using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using SimFell;
using SimFell.Engine.Base;
using SimFell.Sim;

public class Simulator
{
    private const double frameTime = 0.033;
    //private const double frameTime = 0.0000001;

    private double totalDamage;
    private ConcurrentDictionary<string, SpellStats> _spellStats = new ConcurrentDictionary<string, SpellStats>();
    private ConcurrentDictionary<string, float> _resourcesGenerated = new ConcurrentDictionary<string, float>();
    public double Now { get; private set; } = 0;

    private readonly PriorityQueue<SimEvent, double> queue = new();
    private List<SimEvent> _rescheduleEvents = new();
    private bool _doReschedule = false;

    private double _duration;
    private Unit _caster;
    private List<Unit> _targets;

    public Simulator(Unit caster, List<Unit> enemies, double duration = 60)
    {
        _caster = caster;
        _duration = duration;
        _caster.SetSimulator(this);
        _targets = new List<Unit>(enemies);
        foreach (var enemy in _targets)
        {
            enemy.SetSimulator(this);
        }

        _caster.SetPrimaryTarget(_targets[0]);

        Configure();
    }

    public void Schedule(SimEvent evt)
    {
        queue.Enqueue(evt, evt.Time);
    }

    public void UnSchedule(SimEvent evt)
    {
        evt.Unsubscribe();
        if (queue.Count == 0) return;

        var temp = new List<(SimEvent Event, double Priority)>();

        // Dequeue everything and store except the one to remove
        while (queue.Count > 0)
        {
            var e = queue.Dequeue();
            if (e != evt)
            {
                temp.Add((e, e.Time));
            }
        }

        // Rebuild the queue
        foreach (var (e, priority) in temp)
        {
            queue.Enqueue(e, priority);
        }
    }

    public void RescheduleEvent(SimEvent evt)
    {
        _rescheduleEvents.Add(evt);
        _doReschedule = true;
    }

    public void RescheduleEvents()
    {
        if (_rescheduleEvents.Count == 0)
            return;

        var rescheduleSet = new HashSet<SimEvent>(_rescheduleEvents);
        var temp = new List<SimEvent>();

        while (queue.Count > 0)
        {
            var e = queue.Dequeue();
            if (!rescheduleSet.Contains(e))
                temp.Add(e);
        }

        queue.Clear();

        foreach (var e in temp)
            queue.Enqueue(e, e.Time);

        foreach (var e in _rescheduleEvents)
            queue.Enqueue(e, e.Time);

        _doReschedule = false;
        _rescheduleEvents.Clear();
    }

    public void Configure()
    {
        Now = 0;
        _spellStats.Clear();
        _resourcesGenerated.Clear();

        foreach (var enemy in _targets)
        {
            enemy.OnDamageReceived += OnDamageReceived;
        }

        _caster.Targets = _targets;
        _caster.OnCastDone += OnCastDone;
        _caster.OnChannelStarted += OnCastDone;
    }

    public void Run()
    {
        // Schedule the player's first decision event at time 0
        Schedule(new SimEvent(this, _caster, 0, () => TryPlayerAction(_caster), hastedEvent: false));

        while (queue.Count > 0)
        {
            var evt = queue.Dequeue();
            if (evt.Time > _duration) break;

            Now = evt.Time;
            if (evt.Action != null) evt.Action.Invoke();
            evt.Unsubscribe(); // cleanup
            if (_doReschedule) RescheduleEvents();
        }

        Finish();
    }

    public void QueuePlayerAction(Unit player)
    {
        Schedule(new SimEvent(this, player, frameTime, () => TryPlayerAction(player), hastedEvent: false));
    }

    private void TryPlayerAction(Unit player)
    {
        foreach (var action in player.SimActions)
        {
            if (action.CanExecute(player))
            {
                player.StartCasting(action.Spell, player.Targets);
                return; // Only one spell at a time
            }
        }

        // Always schedule the next decision tick (e.g., every GCD or decision interval)
        Schedule(new SimEvent(this, player, frameTime, () => TryPlayerAction(player), hastedEvent: false));
    }

    private void OnCastDone(Unit arg1, Spell spellSource, List<Unit> arg3)
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

    public void Finish()
    {
        totalDamage = _spellStats.Values.Sum(s => s.TotalDamage);

        foreach (var enemy in _targets)
        {
            enemy.OnDamageReceived -= OnDamageReceived;
        }

        _caster.OnCastDone -= OnCastDone;
        _caster.OnChannelStarted -= OnCastDone;
    }

    public double GetDPS()
    {
        return totalDamage / _duration;
    }

    public ConcurrentDictionary<string, SpellStats> GetSpellStats()
    {
        return _spellStats;
    }
}