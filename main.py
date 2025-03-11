"""Main file for the rework sim."""

import argparse
from typing import Optional
from rich import box
from rich.console import Console
from rich.table import Table
from rich.progress import (
    Progress,
    TextColumn,
    BarColumn,
    MofNCompleteColumn,
    TimeElapsedColumn,
    TimeRemainingColumn,
)

from simfell_parser.simfile_parser import SimFileParser, SimFellConfiguration
from simfell_parser.utils import character_classes, default_simfell_files
from sim import Simulation


def handle_configuration(
    arguments: argparse.Namespace,
) -> SimFellConfiguration:
    """Handles the configuration based on the arguments."""
    if arguments.simfile:
        simfile_parser = SimFileParser(arguments.simfile)
        configuration = simfile_parser.parse()
    elif arguments.character_hero:
        simfile_parser = SimFileParser(
            default_simfell_files[arguments.character_hero]
        )
        configuration = simfile_parser.parse()
    else:
        raise ValueError(
            "Either a Simfell File needs to be defined {-f} "
            + "or a Hero needs to be defined {-ch}."
        )

    if arguments.enemy_count:
        configuration.enemies = arguments.enemy_count
    if arguments.talent_tree:
        configuration.talents = arguments.talent_tree
    if arguments.custom_character:
        try:
            stats = [
                int(stat) for stat in arguments.custom_character.split("-")
            ]
        except ValueError as e:
            raise ValueError(
                "Custom character must be formatted as "
                + "intellect-crit-expertise-haste-spirit"
            ) from e

        if len(stats) != 5:
            raise ValueError(
                "Custom character must be formatted as "
                + "intellect-crit-expertise-haste-spirit"
            )
        for stat in stats:
            if stat < 0:
                raise ValueError(
                    "All stats must be positive integers. "
                    + f"Invalid stat: {stat}"
                )

        hero = (
            arguments.character_hero
            if arguments.character_hero is not None
            else configuration.hero
        )

        configuration.character = character_classes[hero](
            intellect=stats[0],
            crit=stats[1],
            expertise=stats[2],
            haste=stats[3],
            spirit=stats[4],
        )

    if arguments.duration:
        configuration.duration = arguments.duration
    if arguments.run_count:
        configuration.run_count = arguments.run_count

    return configuration


def main(arguments: argparse.Namespace):
    """Main function."""

    configuration = handle_configuration(arguments)

    print()

    console = Console()
    table = Table(title="DPS Simulation", box=box.SIMPLE)
    table.add_column(
        "Attribute", style="blue", justify="center", vertical="middle"
    )
    table.add_column("Value", style="yellow", justify="center")

    table.add_row("Simulation Type", arguments.simulation_type)
    table.add_row("Enemy Count", str(configuration.enemies))
    table.add_row("Duration", str(configuration.duration))
    table.add_row("Run Count", str(configuration.run_count))
    if arguments.simulation_type == "stat_weights":
        table.add_row("Stat Weights Gain", str(arguments.stat_weights_gain))

    # Parse the talent tree argument.
    # e.g. Combination of "2-12-3" means Talent 1.2, 2.1, 2.2, 3.3
    # = Coalescing Ice, Unrelenting Ice, Icy Flow, Soulfrost Torrent
    if configuration.talents:
        talents = configuration.talents.split("-")
        for index, talent in enumerate(talents):
            for i in talent:
                configuration.character.add_talent(f"{index+1}.{i}")
    table.add_section()
    table.add_row("Hero", configuration.hero)
    table.add_row(
        "Talent Tree",
        (
            "\n".join(
                [
                    talent.value.name
                    for talent in configuration.character.talents
                ]
            )
            if configuration.character.talents
            else "N/A"
        ),
        end_section=True,
    )
    table.add_row(
        "Character",
        (
            "\n".join(
                f"{key}: {value}"
                for key, value in {
                    "int": configuration.character.get_main_stat(),
                    "crit": (
                        f"{round(configuration.character.get_crit(), 2)}%"
                    ),
                    "exp": (
                        f"{round(configuration.character.get_expertise(), 2)}%"
                    ),
                    "haste": (
                        f"{round(configuration.character.get_haste(), 2)}%"
                    ),
                    "spirit": (
                        f"{round(configuration.character.get_spirit(), 2)}%"
                    ),
                }.items()
            )
        ),
        end_section=True,
    )

    # Sim Options - Uncomment one to run.
    match arguments.simulation_type:
        case "average_dps":
            average_dps(
                table,
                configuration,
                arguments.experimental_feature,
            )
        case "stat_weights":
            raise NotImplementedError("Stat Weights not implemented yet.")
            # stat_weights(
            #     table,
            #     character,
            #     configuration.duration,
            #     configuration.run_count,
            #     arguments.stat_weights_gain,
            #     arguments.experimental_feature,
            #     configuration.enemies,
            # )
        case "debug_sim":
            debug_sim(
                table,
                configuration,
            )

    # Print the final results
    console.print("\n")
    console.print(table)


def debug_sim(table: Table, configuration: SimFellConfiguration) -> None:
    """Runs a debug simulation.
    Creates a deterministic simulation with 0 crit and spirit.
    """

    sim = Simulation(
        configuration,
        do_debug=True,
        is_deterministic=True,
    )
    dps = sim.run(detailed_debug=False)

    table.add_row("Total DPS", f"[bold magenta]{dps:.2f}", end_section=True)


def average_dps(
    table: Table,
    configuration: SimFellConfiguration,
    use_experimental: bool,
    stat_name: Optional[str] = None,
) -> float:
    """Runs a simulation and returns the average DPS."""

    with Progress(
        TextColumn(
            f"[bold]{stat_name if stat_name else 'Calculating DPS'}[/bold] "
            + "[progress.percentage]{task.percentage:>3.0f}%"
        ),
        BarColumn(),
        MofNCompleteColumn(),
        TextColumn("•"),
        TimeElapsedColumn(),
        TextColumn("•"),
        TimeRemainingColumn(),
    ) as progress:
        dps_running_total = 0
        dps_lowest = float("inf")
        dps_highest = float("-inf")

        task = progress.add_task(f"{stat_name}", total=configuration.run_count)

        for _ in range(configuration.run_count):
            sim = Simulation(
                configuration,
                do_debug=False,
                is_deterministic=False,
            )
            dps = sim.run()

            progress.update(task, advance=1)

            dps_lowest = min(dps, dps_lowest)
            dps_highest = max(dps, dps_highest)

            dps_running_total += dps
        avg_dps = dps_running_total / configuration.run_count

    table.add_row(
        "Average DPS" if not stat_name else f"Average DPS ({stat_name})",
        f"[bold magenta]{avg_dps:.2f}",
    )
    table.add_row(
        "Lowest DPS" if not stat_name else f"Lowest DPS ({stat_name})",
        f"[bold magenta]{dps_lowest:.2f}",
    )
    table.add_row(
        "Highest DPS" if not stat_name else f"Highest DPS ({stat_name})",
        f"[bold magenta]{dps_highest:.2f}",
        end_section=True,
    )

    # Experimental: Damage Table
    # ---------------------------
    if not stat_name and use_experimental:
        damage_sum = sum(damage for _, damage in sim.damage_table.items())

        # Sort sim.damage_table by damage dealt from highest to lowest.
        # Remove rows with 0 values
        sorted_damage_table = {
            k: v
            for k, v in sorted(
                sim.damage_table.items(),
                key=lambda item: item[1],
                reverse=True,
            )
            if v > 0
        }

        table.add_row(
            "[bold yellow]-------- Experimental!",
            "[bold yellow]Do not trust! --------",
        )

        # make first 3 rows bold
        for i, (spell, damage) in enumerate(sorted_damage_table.items()):
            spell_name = f"[bold]{spell}" if i < 3 else spell
            damage = (
                f"[bold dark_red]{round(damage, 3)} ({damage/damage_sum:.2%})"
                if i < 3
                else f"[magenta]{round(damage, 3)} ({damage/damage_sum:.2%})"
            )

            table.add_row(spell_name, damage)

    return avg_dps


if __name__ == "__main__":
    # Create parser for command line arguments.
    parser = argparse.ArgumentParser(description="Simulate DPS.")

    parser.add_argument(
        "-s",
        "--simulation-type",
        type=str,
        default="average_dps",
        help="Type of simulation to run.",
        choices=["average_dps", "stat_weights", "debug_sim"],
        required=True,
    )
    parser.add_argument(
        "-e",
        "--enemy-count",
        type=int,
        help="Number of enemies to simulate.",
    )
    parser.add_argument(
        "-t",
        "--talent-tree",
        type=str,
        default="",
        help="Talent tree to use. Format: (row1-row2-row3), "
        + "e.g., 13-1-2 means Talent 1.1, Talent 1.3, Talent 2.1, Talent 3.2",
    )
    parser.add_argument(
        "-c",
        "--custom-character",
        type=str,
        default="",
        help="Custom character to use. "
        + "Format: intellect-crit-expertise-haste-spirit",
    )
    parser.add_argument(
        "-ch",
        "--character-hero",
        type=str,
        choices=character_classes.keys(),
        help="Character hero to use. "
        + f"Choices: {', '.join(character_classes.keys())}",
    )
    parser.add_argument(
        "-d",
        "--duration",
        type=int,
        help="Duration of the simulation.",
    )
    parser.add_argument(
        "-r",
        "--run-count",
        type=int,
        help="Number of runs to average DPS.",
    )
    parser.add_argument(
        "-g",
        "--stat-weights-gain",
        type=float,
        default=20,
        help="Gain of stat weights for the simulation.",
    )
    parser.add_argument(
        "-x",
        "--experimental-feature",
        action="store_true",
        help="Enable experimental features such as the damage table.",
    )
    parser.add_argument(
        "-f",
        "--simfile",
        type=str,
        help="Path to the SimFell file.",
    )

    # Parse arguments.
    args = parser.parse_args()

    # Run the simulation.
    main(args)
