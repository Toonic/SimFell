from characters.ardeos import ArdeosSpell
from characters.ardeos.debuffs import SearingBlazeDebuff


class SearingBlaze(ArdeosSpell):
    """Engulfing FlameSpell"""

    def __init__(self):
        super().__init__(
            "Searing Blaze",
            cast_time=0,
            debuff=SearingBlazeDebuff(),
        )
