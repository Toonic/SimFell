using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using SimFell.SimmyRewrite;

namespace SimFell.Reporting.Builders;

/// <summary>
/// A table that provides the Hero Configuration used in the Sim.
/// </summary>
/// <param name="config">A reference to the SimFellConfig.</param>
/// <returns>The DPS Summary Table.</returns>
public class HeroConfigurationTableBuilder(SimFellConfig config)
{
    public Table BuildConfigTable()
    {
        var configTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);
        configTable.AddColumn(new TableColumn("[steelblue1]Configuration[/]"));
        configTable.AddColumn(new TableColumn("[steelblue1]Value[/]"));

        configTable.AddRow(
            $"[steelblue1]Simulation Type[/]",
            $"[aquamarine3]{config.SimType.ToString()}[/]"
        );

        configTable.AddRow(
            $"[steelblue1]Simulation Mode[/]",
            $"[aquamarine3]{config.SimMode.ToString()}[/]"
        );

        configTable.AddRow(
            $"[steelblue1]Enemies[/]",
            $"[aquamarine3]{config.Enemies}[/]"
        );

        configTable.AddRow(
            $"[steelblue1]Duration[/]",
            $"[aquamarine3]{config.Duration}[/]"
        );

        configTable.AddRow(
            $"[steelblue1]Iterations[/]",
            $"[aquamarine3]{config.RunCount}[/]"
        );


        return configTable;
    }

    public Table BuildStats()
    {
        var heroConfigTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);

        // heroConfigTable.Title("[yellow]Hero Configuration[/]");
        heroConfigTable.AddColumn(new TableColumn("[steelblue1]Stat[/]"));
        heroConfigTable.AddColumn(new TableColumn("[steelblue1]Raw[/]"));
        heroConfigTable.AddColumn(new TableColumn("[steelblue1]Percent[/]"));

        Unit hero = config.GetHero();

        heroConfigTable.AddRow(
            $"[steelblue1]Primary[/]",
            $"[aquamarine3]{hero.MainStat.BaseValue}[/]",
            $"[aquamarine3]-[/]"
        );

        heroConfigTable.AddRow(
            $"[steelblue1]Crit[/]",
            $"[aquamarine3]{hero.CritcalStrikeStat.BaseValue}[/]",
            $"[aquamarine3]{hero.CritcalStrikeStat.GetValue():F2}%[/]"
        );
        heroConfigTable.AddRow(
            $"[steelblue1]Expertise[/]",
            $"[aquamarine3]{hero.ExpertiseStat.BaseValue}[/]",
            $"[aquamarine3]{hero.ExpertiseStat.GetValue():F2}%[/]"
        );
        heroConfigTable.AddRow(
            $"[steelblue1]Haste[/]",
            $"[aquamarine3]{hero.HasteStat.BaseValue}[/]",
            $"[aquamarine3]{hero.HasteStat.GetValue():F2}%[/]"
        );
        heroConfigTable.AddRow(
            $"[steelblue1]Spirit[/]",
            $"[aquamarine3]{hero.SpiritStat.BaseValue}[/]",
            $"[aquamarine3]{hero.SpiritStat.GetValue():F2}%[/]"
        );


        return heroConfigTable;
    }

    public Table BuildTalents()
    {
        var heroConfigTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.SandyBrown);

        heroConfigTable.AddColumn(new TableColumn("[steelblue1]Talents[/]"));

        Unit hero = config.GetHero();
        foreach (var talent in hero.Talents)
        {
            if (talent.IsActive)
                heroConfigTable.AddRow(
                    $"[aquamarine3]{talent.Name}[/]"
                );
        }

        return heroConfigTable;
    }
}