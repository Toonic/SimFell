using System.Collections.Concurrent;
using Spectre.Console;
using SimFell.Engine.Base;
using SimFell.Reporting.Builders;
using System.Collections.Generic;
using System.Linq;
using SimFell.SimmyRewrite;

namespace SimFell.Reporting;

public class ResultsReporter
{
    //private Dictionary<string, SpellStats> spellResults;
    ConcurrentDictionary<string, SpellStats> spellResults = new ConcurrentDictionary<string, SpellStats>();
    Dictionary<string, float> resourceResults = new Dictionary<string, float>();
    private List<double> iterationDpsValues = new List<double>();
    private int totalIterations;
    private double totalDuration;
    private SimFellConfig config;

    public void StoreResults(Simulator simulator, SimFellConfig config)
    {
        this.config = config;
        totalIterations = config.RunCount;
        totalDuration = config.Duration;

        iterationDpsValues.Add(simulator.GetDPS());

        BuildSpellResults(simulator);
    }

    private void BuildSpellResults(Simulator simulator)
    {
        foreach (var spellStat in simulator.GetSpellStats())
        {
            spellResults.AddOrUpdate(
                spellStat.Key,
                _ => new SpellStats
                {
                    SpellName = spellStat.Value.SpellName,
                    TotalDamage = spellStat.Value.TotalDamage,
                    Casts = spellStat.Value.Casts,
                    Ticks = spellStat.Value.Ticks,
                    LargestHit = spellStat.Value.LargestHit,
                    SmallestHit = spellStat.Value.SmallestHit,
                    CritCount = spellStat.Value.CritCount,
                },
                (key, existingStat) =>
                {
                    lock (existingStat)
                    {
                        existingStat.Casts += spellStat.Value.Casts;
                        existingStat.Ticks += spellStat.Value.Ticks;
                        existingStat.TotalDamage += spellStat.Value.TotalDamage;
                        if (spellStat.Value.LargestHit > existingStat.LargestHit)
                        {
                            existingStat.LargestHit = spellStat.Value.LargestHit;
                        }

                        if (spellStat.Value.SmallestHit < existingStat.SmallestHit)
                        {
                            existingStat.SmallestHit = spellStat.Value.SmallestHit;
                        }

                        existingStat.CritCount += spellStat.Value.CritCount;
                    }

                    return existingStat;
                });
        }
    }

    public void Display()
    {
        DisplayHeroConfiguration();
        DisplayDpsSummary();
        DisplayDamageBreakdown();
        DisplayResourceBreakdown();
    }

    private void DisplayHeroConfiguration()
    {
        var simConfigurationTable = new HeroConfigurationTableBuilder(config);
        var simConfig = simConfigurationTable.BuildConfigTable();
        var heroStats = simConfigurationTable.BuildStats();
        var heroTalents = simConfigurationTable.BuildTalents();

        var outerTable = new Table().Border(TableBorder.None);
        outerTable.Title($"[yellow]{config.ConfigName}[/]");
        outerTable.AddColumn(new TableColumn(""));
        outerTable.AddColumn(new TableColumn(""));
        outerTable.AddColumn(new TableColumn(""));

        outerTable.AddRow(simConfig, heroStats, heroTalents);

        AnsiConsole.Write(new Align(outerTable, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    private void DisplayDpsSummary()
    {
        var table = new DpsSummaryTableBuilder(iterationDpsValues, totalIterations).Build();
        AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    private void DisplayDamageBreakdown()
    {
        var table =
            new DamageBreakdownTableBuilder(spellResults.ToDictionary(), totalIterations, config.Duration).Build();
        AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    private void DisplayResourceBreakdown()
    {
        if (resourceResults.Count == 0)
        {
            return;
        }

        var table = new ResourceBreakdownTableBuilder(resourceResults, totalIterations, totalDuration).Build();
        AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
    }
}