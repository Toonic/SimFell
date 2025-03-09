"""Module for Fire Frogs Debuff"""

from characters.ardeos.ardeos_debuff import ArdeosDebuff


class FireFrogsDebuff(ArdeosDebuff):
    """Fire Frogs debuff."""

    def __init__(self):
        super().__init__(
            "Fire Frogs",
            base_tick_duration=3,
            duration=12,
            damage_percent=19,  # Tick Based Damage.
            maximum_stacks=9,
        )

    def on_tick(self):
        super().on_tick()
        self.damage()
