"""Utility functions for the simfell_parser module."""

from typing import TypeVar, TYPE_CHECKING, Optional, Dict

from characters.rime.rime import Rime
from characters.ardeos.ardeos import Ardeos

if TYPE_CHECKING:
    from base import BaseSpell, BaseCharacter

SpellTypeT = TypeVar("SpellTypeT", bound="BaseSpell")
CharacterTypeT = TypeVar("CharacterTypeT", bound="BaseCharacter")

# Dictionary to map class names to their corresponding classes
character_classes: Dict[str, CharacterTypeT] = {"Rime": Rime, "Ardeos": Ardeos}

default_simfell_files: Dict[str, str] = {
    "Rime": "simfell_parser/defaults/rime_default.simfell",
    "Ardeos": "simfell_parser/defaults/ardeos_default.simfell",
}


def map_character_name_to_class(character_name: str) -> "BaseCharacter":
    """Map a character name to a class."""

    class_name = character_name.capitalize()

    character_class: Optional[CharacterTypeT] = character_classes.get(
        class_name, None
    )
    if character_class is None:
        raise ValueError(f"Character class '{class_name}' not found.")

    return character_class
