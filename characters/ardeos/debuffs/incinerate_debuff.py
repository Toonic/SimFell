"""Module for Incinerate Debuff"""

from characters.ardeos.ardeos_debuff import ArdeosDebuff


class IncinerateDebuff(ArdeosDebuff):
    """Incinerate debuff."""

    def __init__(self):
        super().__init__(
            "Incinerate",
            base_tick_duration=3,
            duration=12,
            damage_percent=75,  # Tick Based Damage.
            maximum_stacks=9999,
            percent_of_damage_per_tick_per_stack=0.1,  # Each stack is 10%
        )

    def on_tick(self):
        super().on_tick()
        self.damage()

    # Dev Note: Something I'm unsure of is if each stack is calculated individually or not.
    def damage_modifiers(self, damage):
        total_damage = damage
        for _ in range(1, self.current_stacks):
            total_damage += damage * self.percent_of_damage_per_tick_per_stack
        return total_damage
