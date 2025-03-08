"""Module for Frost Bolt Spell"""

from characters.rime import RimeSpell


class FrostBolt(RimeSpell):
    """Frost Bolt Spell"""

    def __init__(self):
        super().__init__(
            "Frost Bolt", cast_time=1.5, damage_percent=73, anima_gain=3
        )
