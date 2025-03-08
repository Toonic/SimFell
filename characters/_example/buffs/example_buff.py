"""Module for Example Buff"""

from characters._example.examplehero_buff import ExampleHeroBuff


# This example buff is a copy of Rimes Wrath of Winter buff.
class ExampleBuff(ExampleHeroBuff):
    """Example buff."""

    # Here you can see that we define bonus haste, and damage multiplier
    # buffs that get applied on the cast.
    haste_additional_bonus = 30
    damage_multiplier_bonus = 0.15

    def __init__(self):
        # This is where we define all the stats for the spell.
        # In this case we are defining the tick duration, so how often on_tick
        # is called, along with the winter_orb_per_tick value defined by
        # ExampleHeroBuff.
        super().__init__(
            "ExampleBuff",
            duration=20,
            maximum_stacks=1,
            winter_orb_per_tick=1,
            base_tick_duration=2,
        )

    # On tick is what gets called whenever the base_tick_duration happens.
    # In this example, on_tick will be called 10 times with 0 Haste.
    def on_tick(self):
        self.character.gain_winter_orbs(self.winter_orb_per_tick)

    # Overrides the on_apply.
    # Gets called when the buff or debuff gets applied to its target.
    # In this example, we apply a damage multiplier to the character
    # along with a haste modifier.
    def on_apply(self):
        self.character.damage_multiplier += self.damage_multiplier_bonus
        self.character.haste_additional += self.haste_additional_bonus

    # Overrides the on_remove.
    # This gets called whenever the buff or debuff gets removed from the
    # the target. In this case, we need to remove the stat bonuses.
    def on_remove(self):
        self.character.damage_multiplier -= self.damage_multiplier_bonus
        self.character.haste_additional -= self.haste_additional_bonus
