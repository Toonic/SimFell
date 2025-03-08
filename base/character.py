"""Module for the Character class."""

from abc import ABC, abstractmethod
from typing import Dict, List, TYPE_CHECKING, Optional

from .talent import CharacterTalentT


if TYPE_CHECKING:
    from base.spells.base_spell import BaseSpell
    from base.spells.base_buff import BaseBuff
    from sim import Simulation


class BaseCharacter(ABC):
    """Abstract base class for all characters."""

    percent_per_point = 0.21

    def __init__(self, main_stat, crit, expertise, haste, spirit):
        # Main Stat Conversion - Points to % including DR.
        self._main_stat = main_stat
        # Crit has a base of 5%.
        self._crit = self.calculate_stat_diminishing_returns(crit, 5)
        self._expertise = self.calculate_stat_diminishing_returns(expertise)
        self._haste = self.calculate_stat_diminishing_returns(haste)
        self._spirit = self.calculate_stat_diminishing_returns(spirit)

        self._crit_power = 1

        # This will hold the character's available spells.
        self.spells: Dict[str, "BaseSpell"] = {}

        # This will hold the character's rotation.
        self.rotation: List[str] = []

        # All the talents.
        self.talents: List[CharacterTalentT] = []
        # Buffs
        self.buffs: Dict[str, "BaseBuff"] = {}
        self.configure_spell_book()
        self.simulation: Optional["Simulation"] = None

        # External Character Stat Buffs - EG: Gems, Gear, Buffs.
        self.damage_multiplier = 0

        self.main_stat_multiplier = 0
        self.main_stat_additional = 0
        self.crit_multiplier = 0
        self.crit_additional = 0
        self.expertise_multiplier = 0
        self.expertise_additional = 0
        self.haste_multiplier = 0
        self.haste_additional = 0
        self.spirit_multiplier = 0
        self.spirit_additional = 0
        self.crit_power_multiplier = 0
        self.crit_power_additional = 0

    def calculate_stat_diminishing_returns(
        self, stat_points: int, base_percent=0
    ) -> float:
        """Calculates total stat effect with diminishing returns
        applied correctly."""

        base_value = 0.21  # Base effectiveness per point (0.21%)
        breakpoints = [10, 15, 20, 25]  # Percent thresholds
        multipliers = [1.0, 0.9, 0.8, 0.7, 0.6]  # Multipliers for each stage

        total_effect = base_percent  # Accumulated percentage
        used_points = 0  # Points already spent

        for i, threshold in enumerate(
            breakpoints + [float("inf")]
        ):  # Include final tier
            if used_points >= stat_points:
                break

            # Remaining percentage needed to reach the next threshold
            required_percentage = threshold - total_effect
            points_to_threshold = (
                required_percentage / base_value
            )  # Convert % to points

            # Points available in this tier
            points_used = min(stat_points - used_points, points_to_threshold)

            # Apply the appropriate multiplier for this tier
            total_effect += points_used * base_value * multipliers[i]
            used_points += points_used  # Update total points used

        return total_effect

    def set_simulation(self, simulation: "Simulation") -> None:
        """Sets the simulation for the character."""
        self.simulation = simulation

    def get_main_stat(self) -> float:
        """Returns the character's main stat."""
        return (self._main_stat + self.main_stat_additional) * (
            1 + self.main_stat_multiplier
        )

    def get_crit(self) -> float:
        """Returns the character's crit as a percentage."""
        return (self._crit + self.crit_additional) * (1 + self.crit_multiplier)

    def get_haste(self) -> float:
        """Returns the character's haste as a percentage."""
        return (self._haste + self.haste_additional) * (
            1 + self.haste_multiplier
        )

    def get_expertise(self) -> float:
        """Returns the character's expertise as a percentage."""
        return (self._expertise + self.expertise_additional) * (
            1 + self.expertise_multiplier
        )

    def get_spirit(self) -> float:
        """Returns the character's spirit as a percentage."""
        return (self._spirit + self.spirit_additional) * (
            1 + self.spirit_multiplier
        )

    def get_damage_multiplier(self) -> float:
        """Returns the character's damage multiplyer."""
        return 1 + self.damage_multiplier

    def has_talent(self, talent: CharacterTalentT) -> bool:
        """Returns true if the talent is present."""
        return talent in self.talents

    def has_buff(self, buff_simfell_name: str) -> bool:
        """Returns true if the buff is present"""
        return buff_simfell_name in self.buffs

    def get_buff(self, buff_simfell_name: str) -> "BaseBuff":
        """Returns the current Buff."""
        if self.has_buff(buff_simfell_name):
            return self.buffs[buff_simfell_name]

        return None

    def get_crit_power(self) -> float:
        """Returns crit power."""
        return (self._crit_power + self.crit_power_additional) * (
            1 + self.crit_multiplier
        )

    @abstractmethod
    def configure_spell_book(self) -> None:
        """Adds a spells to the character's spell book."""

    @abstractmethod
    def add_talent(self, talent_identifier: str) -> None:
        """Adds a talent to the character's available talents. To be overridden"""
