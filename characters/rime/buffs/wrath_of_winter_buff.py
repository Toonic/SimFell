"""Module for Wrath of Winter Buff"""

from characters.rime.rime_buff import RimeBuff


class WrathOfWinterBuff(RimeBuff):
    """Glacial Assault buff."""

    haste_additional_bonus = 30
    damage_multiplier_bonus = 0.15

    def __init__(self):
        super().__init__(
            "Wrath Of Winter",
            duration=20,
            maximum_stacks=1,
            base_tick_duration=2,
        )

    def on_tick(self):
        self.character.gain_winter_orbs(1)

    def on_apply(self):
        self.character.damage_multiplier += self.damage_multiplier_bonus
        self.character.haste_additional += self.haste_additional_bonus

    def on_remove(self):
        self.character.damage_multiplier -= self.damage_multiplier_bonus
        self.character.haste_additional -= self.haste_additional_bonus
