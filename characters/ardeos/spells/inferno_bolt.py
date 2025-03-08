"""Module for Inferno Bolt Spell"""

from characters.ardeos import ArdeosSpell


class InfernoBolt(ArdeosSpell):
    """Frost Bolt Spell"""

    def __init__(self):
        super().__init__(
            "Inferno Bolt", cast_time=2, damage_percent=145, ember_gain=45
        )
