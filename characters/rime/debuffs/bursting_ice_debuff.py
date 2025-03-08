"""Module for Bursting Ice Debuff"""

from characters.rime import RimeDebuff
from characters.rime.talent import RimeTalents


class BurstingIceDebuff(RimeDebuff):
    """Glacial Assault buff."""

    maximum_possible_anima = 3

    def __init__(self):
        super().__init__(
            "Bursting Ice",
            anima_per_tick=1,
            base_tick_duration=0.5,
            duration=3.15,
            damage_percent=61,  # Tick Based Damage.
        )

    def damage_modifiers(self, damage):
        if self.character.has_talent(RimeTalents.COALESCING_ICE):
            coalescing_ice = RimeTalents.COALESCING_ICE.value
            return damage * (1 + (coalescing_ice.bonus_bursting_damage / 100))

        return damage

    def on_tick(self):
        super().on_tick()
        self.damage()
        anima_gain = self.anima_per_tick

        # TODO: Check to see if this is 1 target only.
        if self.character.has_talent(RimeTalents.COALESCING_ICE):
            coalescing_ice = RimeTalents.COALESCING_ICE.value
            anima_gain += coalescing_ice.bonus_anima_single_target

        self.character.gain_anima(min(anima_gain, self.maximum_possible_anima))
