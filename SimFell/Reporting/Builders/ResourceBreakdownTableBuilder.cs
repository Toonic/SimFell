using SimFell.Engine.Base;
using Spectre.Console;

namespace SimFell.Reporting.Builders;

/// <summary>
/// A table that breaks down the resources a hero used
/// </summary>
/// <param name="resourceResults">The dictionary for resource results.</param>
/// <param name="totalIterations">The integer of total iterations the sim runs.</param>
/// <param name="resourceResults">The total duration of the running sims.</param>
/// <returns>The resource table.</returns>
public class ResourceBreakdownTableBuilder(
    Dictionary<string, float> resourceResults,
    int totalIterations,
    double totalDuration)
{
    public Table Build()
    {
        var resourceTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);
        
        resourceTable.Title("[yellow]Resource Breakdown[/]");
        resourceTable.AddColumn(new TableColumn("[steelblue1]Resource[/]"));
        resourceTable.AddColumn(new TableColumn("[steelblue1]Avg. Total Gained[/]").RightAligned());
        resourceTable.AddColumn(new TableColumn("[steelblue1]Per Second (RPS)[/]").RightAligned());

        foreach (var resource in resourceResults.OrderBy(r => r.Key))
        {
            var avgTotal = resource.Value / totalIterations; 
            var rps = totalDuration > 0 ? avgTotal / totalDuration : 0;
            resourceTable.AddRow(
                $"[steelblue1]{resource.Key}[/]",
                $"[aquamarine3]{avgTotal:N2}[/]",
                $"[aquamarine3]{rps:N2}[/]"
            );
        }
        
        return resourceTable;
    }
}