"""Module for Ice Blitz Spell"""

from characters.rime import RimeSpell
from characters.rime.buffs import IceBlitzBuff


class IceBlitz(RimeSpell):
    """Ice Blitz Spell"""

    ice_blitz_damage_multiplier = 0.15

    def __init__(self):
        super().__init__(
            "Ice Blitz",
            cooldown=120,
            has_gcd=False,
            can_cast_on_gcd=True,
            can_cast_while_casting=True,
            buff=IceBlitzBuff(),
        )
