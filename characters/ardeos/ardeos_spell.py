"""Module for Rime's spells."""

from base import BaseSpell


# Defines the ArdeosSpell class.
# Ardeos gains embers for casting spells and uses burning embers to cast spells.
class ArdeosSpell(BaseSpell):
    """Base information for Rime Spells"""

    anima_gain = 0
    winter_orb_cost = 0
    anima_per_tick = 0

    def __init__(
        self,
        *args,
        ember_gain=0,
        burning_ember_cost=0,
        ember_per_tiick=0,
        **kwargs,
    ):
        self.ember_gain = ember_gain
        self.burning_ember_cost = burning_ember_cost
        self.ember_per_tiick = ember_per_tiick
        super().__init__(*args, **kwargs)

    def is_ready(self):
        return (
            super().is_ready()
            and self.character.burning_embers >= self.burning_ember_cost
        )

    def on_cast_complete(self):
        super().on_cast_complete()
        self.character.gain_ember(self.ember_gain)  # Gain Anima on Complete.
        if self.burning_ember_cost > 0:  # Lose Winter Orbs on Complete.
            self.character.lose_burning_embers(self.burning_ember_cost)
        if self.burning_ember_cost < 0:  # Gain Winter Orbs on Complete.
            self.character.gain_burning_embers(abs(self.burning_ember_cost))
