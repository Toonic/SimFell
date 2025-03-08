"""Models for the SimFell file."""

from typing import Any, List, Optional
from pydantic import BaseModel

from base import BaseCharacter
from simfell_parser.enums import Gem, TierSet, Tier
from simfell_parser.utils import CharacterTypeT, map_character_name_to_class


class Condition(BaseModel):
    """Class for a condition in a SimFell file."""

    left: str
    operator: str
    right: Any

    def __str__(self):
        return (
            f"[cornflower_blue]{self.left}[/cornflower_blue] "
            + f"[grey37]{self.operator}[/grey37] "
            + f"[cyan]{self.right}[/cyan]"
        )


class Action(BaseModel):
    """Class for an action in a SimFell file."""

    name: str
    conditions: List[Condition]

    def __str__(self):
        return f"{self.name} ({', '.join(self.conditions)})"


class GemTier(BaseModel):
    """Class for a gem tier in a SimFell configuration."""

    tier: Tier
    gem: Gem


class Equipment(BaseModel):
    """Class for an equipment in a SimFell configuration."""

    name: str
    ilvl: int
    tier: Tier
    tier_set: Optional[TierSet]
    intellect: int
    stamina: int
    expertise: Optional[int]
    crit: Optional[int]
    haste: Optional[int]
    spirit: Optional[int]
    gem_bonus: Optional[int]
    gem: Optional[GemTier]


class Gear(BaseModel):
    """Class for a gear in a SimFell configuration."""

    helmet: Optional[Equipment] = None
    shoulder: Optional[Equipment] = None


class SimFellConfiguration(BaseModel):
    """Class for a SimFell configuration."""

    name: str
    hero: str
    intellect: int
    crit: float
    expertise: float
    haste: float
    spirit: float
    talents: Optional[str] = None
    trinket1: Optional[str] = None
    trinket2: Optional[str] = None

    duration: int
    enemies: int
    run_count: int

    actions: List[Action]
    gear: Gear

    _character: Optional[CharacterTypeT] = None

    @property
    def parsed_json(self) -> str:
        """Convert the configuration to a JSON string."""

        return self.model_dump_json(indent=2)

    @property
    def character(self) -> BaseCharacter:
        """Return the character for the configuration."""

        if self._character is None:
            character_class = map_character_name_to_class(self.hero)
            self._character = character_class(
                intellect=self.intellect,
                crit=self.crit,
                expertise=self.expertise,
                haste=self.haste,
                spirit=self.spirit,
            )

        return self._character

    @character.setter
    def character(self, value: BaseCharacter):
        self._character = value
