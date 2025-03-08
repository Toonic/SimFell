"""Module for Engulfing Flames Debuff"""

import random
from characters.ardeos.ardeos_debuff import ArdeosDebuff


class EngulfingFlamesDebuff(ArdeosDebuff):
    """Engulfing Flames debuff."""

    chance_to_gain_ember = 20

    def __init__(self):
        super().__init__(
            "Engulfing Flames",
            ember_per_tick=10,
            base_tick_duration=3,
            duration=24,
            damage_percent=62,  # Tick Based Damage.
        )

    def on_tick(self):
        super().on_tick()
        self.damage()

        if random.uniform(0, 100) < self.chance_to_gain_ember:
            self.character.gain_ember(self.ember_per_tick)
