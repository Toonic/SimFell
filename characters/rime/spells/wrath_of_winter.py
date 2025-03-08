"""Module for Wrath of Winter Spell"""

from characters.rime import RimeSpell
from characters.rime.buffs import WrathOfWinterBuff


class WrathOfWinter(RimeSpell):
    """Wrath of Winter Spell"""

    haste_additional_bonus = 30
    damage_multiplier_bonus = 0.15

    def __init__(self):
        super().__init__(
            "Wrath of Winter",
            cast_time=0,
            cooldown=1000,  # TODO: Spirit Gen instead.
            buff=WrathOfWinterBuff(),
        )
