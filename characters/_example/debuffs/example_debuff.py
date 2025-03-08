"""Module for Example Buff"""

from characters._example.examplehero_debuff import ExampleHeroDebuff


# This example debuff applies a simple damage over time effect.
class ExampleDebuff(ExampleHeroDebuff):
    """Example buff."""

    def __init__(self):
        # As BaseDebuff and Buff come from BaseSpell you can define similar
        # variables here. In this case, we define the damage per tick.
        super().__init__(
            "Example DOT",
            duration=4,
            base_tick_duration=0.5,
            damage_percent=61,  # Tick Based Damage.
        )

    # On tick is what gets called whenever the base_tick_duration happens.
    # In this example, on_tick will be called 8 times with 0 Haste.
    # We also want to call self.damage() for it to actually deal damage.
    def on_tick(self):
        self.damage()
