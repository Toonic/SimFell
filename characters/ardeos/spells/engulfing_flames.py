"""Module for Engulfing Flame Spell"""

from characters.ardeos import ArdeosSpell
from characters.ardeos.debuffs import EngulfingFlamesDebuff


class EngulfingFlames(ArdeosSpell):
    """Engulfing FlameSpell"""

    def __init__(self):
        super().__init__(
            "Engulfing Flames",
            cast_time=1.5,
            debuff=EngulfingFlamesDebuff(),
        )
