"""Module for Fireball Debuff"""

import random
from characters.ardeos.ardeos_debuff import ArdeosDebuff


class FireBallDebuff(ArdeosDebuff):
    """Fireball debuff."""

    chance_to_gain_ember = 25

    def __init__(self):
        super().__init__(
            "Fire Ball",
            ember_per_tick=15,
            base_tick_duration=1.5,
            duration=9,
            damage_percent=22,  # Tick Based Damage.
        )

    def on_tick(self):
        super().on_tick()
        self.damage()

        if random.uniform(0, 100) < self.chance_to_gain_ember:
            self.character.gain_ember(self.ember_per_tick)
