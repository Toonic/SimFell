"""Module for Wildfire Spell"""

from characters.ardeos import ArdeosSpell
from characters.ardeos.buffs import WildfireBuff


class Wildfire(ArdeosSpell):
    """Wildfire Spell"""

    def __init__(self):
        super().__init__(
            "Wildfire",
            cooldown=45,
            has_gcd=False,
            buff=WildfireBuff(),
        )
