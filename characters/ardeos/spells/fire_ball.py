"""Module for FireBall Spell"""

from characters.ardeos import ArdeosSpell

from characters.ardeos.debuffs import FireBallDebuff


class FireBall(ArdeosSpell):
    """FireBall Spell"""

    def __init__(self):
        super().__init__(
            "Fire Ball",
            cast_time=20,
            damage_percent=430,
            debuff=FireBallDebuff(),
        )
