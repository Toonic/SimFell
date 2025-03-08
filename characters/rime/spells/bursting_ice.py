"""Module for Bursting Ice Spell"""

from characters.rime import RimeSpell
from characters.rime.debuffs import BurstingIceDebuff


class BurstingIce(RimeSpell):
    """Bursting Ice Spell"""

    def __init__(self):
        super().__init__(
            "Bursting Ice",
            cast_time=2.0,
            cooldown=15,
            debuff=BurstingIceDebuff(),
        )
