"""Module for the Simulation class."""

from typing import Dict
from copy import deepcopy
from rich import print  # pylint: disable=redefined-builtin

from base.spells.base_debuff import BaseDebuff
from simfell_parser.model import SimFellConfiguration
from simfell_parser.condition_parser import SimFileConditionParser
from simfell_parser.utils import SpellTypeT


class Simulation:
    """Class for the Simulation."""

    def __init__(
        self,
        configuration: SimFellConfiguration,
        do_debug=False,
        is_deterministic=False,
    ):
        """Initialize the Simulation."""

        self.character = deepcopy(configuration.character)
        self.character.set_simulation(self)
        self.duration = configuration.duration
        self.enemy_count = configuration.enemies
        self.do_debug = do_debug
        self.detailed_debug = False
        self.time = 0
        self.gcd = 0
        self.ability_queue = []
        self.debuffs: Dict[str, BaseDebuff] = {}
        self.damage = 0
        self.damage_table = {}
        self.is_deterministic = is_deterministic

        self.configuration = configuration

        if self.is_deterministic:
            self.character._crit = 0
            self.character._spirit = 0

    def get_debuff(self, debuff_simfell_name: str) -> BaseDebuff:
        """Returns the Debuff."""
        if debuff_simfell_name in self.debuffs:
            return self.debuffs[debuff_simfell_name]

        return None

    def update_time(self, delta_time: float):
        """Update the time of the simulation."""
        delta_time = round(delta_time, 2)
        self.time += delta_time
        self.gcd -= delta_time

        # Update spell cooldowns
        for spell in self.character.spells.values():
            spell.update_cooldown(delta_time)

        # Update Players Buffs
        for buff in list(self.character.buffs.values()):
            if self.do_debug:
                print(
                    f"Time {self.time:.2f}: 🔄 Updating "
                    + f"[dark_green]{buff.name} (Buff)[/dark_green] "
                    + "remaining duration"
                )

            buff.update_remaining_duration(delta_time)

        # Update Debuffs on an Enemy.
        # TODO: In the future we will want to keep track of each enemy
        # and each debuff. This will be important for Multi-Dotting
        # in the future.

        for debuff in list(self.debuffs.values()):
            if self.do_debug:
                print(
                    f"Time {self.time:.2f}: 🔄 Updating "
                    + f"[deep_pink4]{debuff.name} (Debuff)[/deep_pink4] "
                    + "remaining duration"
                )

            debuff.update_remaining_duration(delta_time)

    def run(self, detailed_debug=False):
        """Run the simulation."""
        self.detailed_debug = detailed_debug

        while self.time <= self.duration:
            if self.gcd > 0:
                if self.do_debug:
                    print(
                        f"Time {self.time:.2f}: GCD: {self.gcd:.2f}"
                        + " | [grey37]Updating time by GCD"
                    )
                self.update_time(self.gcd)

            for action in self.configuration.actions:
                if self.do_debug and self.detailed_debug:
                    print(
                        "[grey37]--------------------------[/grey37]"
                        + f"\nAction: [dark_magenta]'{action.name}'"
                        + "[/dark_magenta], Conditions: "
                        + f"{len(action.conditions)}"
                    )

                spell: SpellTypeT = self.character.spells.get(
                    action.name.split("/")[1], None
                )
                if not spell:
                    print(
                        f"Spell [cornflower_blue]'{action.name}'"
                        + "[/cornflower_blue] not found in character spells"
                    )
                    continue

                evaluate_conditions = (
                    SimFileConditionParser.evaluate_conditions(
                        action.conditions, self
                    )
                )

                is_spell_ready = spell.is_ready()

                if self.do_debug and self.detailed_debug:
                    print(f"\tCondition Results: {evaluate_conditions}")
                    print(f"\tSpell Ready: {is_spell_ready}")
                    print("\t=====================\n")

                if all([is_spell_ready, evaluate_conditions]):
                    if self.do_debug:
                        print(
                            f"Time {self.time:.2f}: "
                            + f"🔮 Casting [cornflower_blue]{spell.name}"
                            + "[/cornflower_blue]"
                        )
                    spell.cast()
                    break
            else:
                if self.do_debug:
                    print(
                        f"Time {self.time:.2f}: No Spell Ready"
                        + " | [grey37]Updating time by 0.1"
                    )
                self.update_time(0.1)

        return self.damage / self.duration
