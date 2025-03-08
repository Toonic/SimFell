"""Module for utility enums"""

from enum import Enum


# For each spell we create. We need to define what the ID will be when calling
# it from the SimFell File. For standard we use snake_case.
class SpellSimFellName(Enum):
    """Enum for spell simfell names"""

    EXAMPLESPELL = "example_spell"
    EXAMPLESPELLBUFF = "example_spell_buff"
    EXAMPLESPELLDEBUFF = "example_spell_debuff"
