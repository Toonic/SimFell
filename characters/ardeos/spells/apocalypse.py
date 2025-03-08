"""Module for Apocalypse Spell"""

from characters.ardeos import ArdeosSpell


class Apocalypse(ArdeosSpell):
    """Apocalypse Spell"""

    cdr_from_detonate = 10

    def __init__(self):
        super().__init__(
            "Apocalypse",
            cast_time=3,
            cooldown=180,
            damage_percent=1573,
        )
