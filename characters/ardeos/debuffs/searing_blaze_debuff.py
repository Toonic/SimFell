"""Module for Searing Blaze Debuff"""

import random
from characters.ardeos.ardeos_debuff import ArdeosDebuff


class SearingBlazeDebuff(ArdeosDebuff):
    """Searing Blaze debuff."""

    # Dev notes: It appears that this might have a talent that allows for it to stack in the future.

    chance_to_gain_ember = 10  # As Percent

    def __init__(self):
        super().__init__(
            "Searing Blaze",
            base_tick_duration=2,
            ember_per_tick=5,
            duration=24,
            damage_percent=31,  # Tick Based Damage.
        )

    def on_tick(self):
        super().on_tick()
        self.damage()

        if random.uniform(0, 100) < self.chance_to_gain_ember:
            self.character.gain_ember(self.ember_per_tick)
