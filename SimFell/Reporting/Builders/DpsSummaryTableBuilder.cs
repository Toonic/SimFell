using Spectre.Console;
using System.Collections.Generic;
using System.Linq;

namespace SimFell.Reporting.Builders;

/// <summary>
/// A table that provides the dps summary for low, avg, and high.
/// </summary>
/// <param name="iterationDpsValues">A list of all the dps values from each iteration.</param>
/// <param name="totalIterations">The integer of total iterations the sim runs.</param>
/// <returns>The DPS Summary Table.</returns>
public class DpsSummaryTableBuilder(
    List<double> iterationDpsValues,
    int totalIterations)
{
    public Table Build()
    {
        var dpsSummaryTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);
        dpsSummaryTable.Title("[yellow]Overall DPS Summary[/]");

        if (totalIterations > 1)
        {
            var avgDps = iterationDpsValues.Average();
            var minDps = iterationDpsValues.Min();
            var maxDps = iterationDpsValues.Max();

            dpsSummaryTable.AddColumn(new TableColumn("[red]Lowest DPS[/]").Centered());
            dpsSummaryTable.AddColumn(new TableColumn("[steelblue1]Average DPS[/]").Centered());
            dpsSummaryTable.AddColumn(new TableColumn("[green]Highest DPS[/]").Centered());
            dpsSummaryTable.AddRow(
                $"[bold red]{minDps:N2}[/]",
                $"[bold aquamarine3]{avgDps:N2}[/]",
                $"[bold green]{maxDps:N2}[/]"
            );
        }
        else
        {
            var singleDps = iterationDpsValues.FirstOrDefault();
            dpsSummaryTable.AddColumn(new TableColumn("[steelblue1]DPS[/]").Centered());
            dpsSummaryTable.AddRow(
                $"[bold aquamarine3]{singleDps:F2}[/]"
            );
        }

        return dpsSummaryTable;
    }
}