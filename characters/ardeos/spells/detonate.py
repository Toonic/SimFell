"""Module for Detonate Spell"""

from characters.ardeos import ArdeosSpell


class Detonate(ArdeosSpell):
    """Apocalypse Spell"""

    dot_sample_duration = 4  # 4 Seconds of the Dot Duration.
    global_cooldown_override = 1  # Forced 1 Second GCD.

    def __init__(self):
        super().__init__("Detonate", cast_time=1.5, burning_ember_cost=1)

    def get_gcd(self):
        return self.global_cooldown_override

    def on_cast_complete(self):
        super().on_cast_complete()
        for debuff in self.character.simulation.debuffs.values():
            # Get the tick rate.
            debuff_tick_rate = debuff.tick_rate
            for _ in range(int(self.dot_sample_duration / debuff_tick_rate)):
                debuff.damage()
