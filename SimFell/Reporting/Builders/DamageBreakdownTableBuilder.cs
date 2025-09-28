using SimFell.Engine.Base;
using Spectre.Console;

namespace SimFell.Reporting.Builders;

/// <summary>
/// A table that breaks down the results for each spell/buff cast
/// </summary>
/// <param name="spellResults">The dictionary for all spell results.</param>
/// <param name="totalIterations">The integer of total iterations the sim runs.</param>
/// <returns>The damage breakdown table.</returns>
public class DamageBreakdownTableBuilder(
    Dictionary<string, SpellStats> spellResults,
    int totalIterations,
    double duration
)
{
    public Table Build()
    {
        var overallDamage = spellResults.Values.Sum(s => s.TotalDamage);
        var breakdownTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);

        breakdownTable.Title("[yellow]Damage Breakdown[/]");
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Spell Name[/]"));
        breakdownTable.AddColumn(new TableColumn("[steelblue1]% of Total[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Damage[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Casts/Attacks[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Avg Cast Dmg[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Ticks / Hits[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Smallest Hit[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Average Hit[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Largest Hit[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]Crit Count[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]% Crit[/]").RightAligned());
        breakdownTable.AddColumn(new TableColumn("[steelblue1]DPS[/]").RightAligned());

        double totalAggregateDamage = 0;
        double totalAggregateCasts = 0;
        double totalAggregateTicks = 0;

        foreach (var spellStat in spellResults.Values.OrderByDescending(s => s.TotalDamage))
        {
            var avgDamage = spellStat.TotalDamage / totalIterations;
            totalAggregateDamage += avgDamage;
            var percentage = overallDamage > 0 ? (spellStat.TotalDamage / overallDamage * 100) : 0;
            var avgCasts = (double)spellStat.Casts / totalIterations;
            totalAggregateCasts += avgCasts;
            var avgTicks = (double)spellStat.Ticks / totalIterations;
            totalAggregateTicks += avgTicks;
            var avgCastDamage = avgCasts > 0 ? avgDamage / avgCasts : 0;
            var largestHit = spellStat.LargestHit;
            var smallestHit = spellStat.SmallestHit.Equals(double.MaxValue) ? 0 : spellStat.SmallestHit;
            var critCount = (double)spellStat.CritCount / totalIterations;
            var critPercentage = avgTicks > 0 ? (critCount / avgTicks * 100) : 0;
            var dps = avgDamage / duration;
            var averageHit = avgTicks > 0 ? avgDamage / avgTicks : 0;

            breakdownTable.AddRow(
                $"[steelblue1]{spellStat.SpellName}[/]",
                $"[aquamarine3]{percentage:F2}%[/]",
                $"[aquamarine3]{avgDamage:N0}[/]",
                $"[aquamarine3]{avgCasts:N0}[/]",
                $"[aquamarine3]{avgCastDamage:N0}[/]",
                $"[aquamarine3]{avgTicks:N0}[/]",
                $"[aquamarine3]{smallestHit:N0}[/]",
                $"[aquamarine3]{averageHit:N0}[/]",
                $"[aquamarine3]{largestHit:N0}[/]",
                $"[aquamarine3]{critCount:F2}[/]",
                $"[aquamarine3]{critPercentage:F2}%[/]",
                $"[aquamarine3]{dps:N0}[/]"
            );
        }

        breakdownTable.Columns[0].Footer = new Markup("[bold steelblue1]Total[/]");
        breakdownTable.Columns[2].Footer = new Markup($"[bold aquamarine3]{totalAggregateDamage:N0}[/]");
        breakdownTable.Columns[3].Footer = new Markup($"[bold aquamarine3]{totalAggregateCasts:F2}[/]");
        breakdownTable.Columns[5].Footer = new Markup($"[bold aquamarine3]{totalAggregateTicks:F2}[/]");
        breakdownTable.ShowFooters();

        return breakdownTable;
    }
}