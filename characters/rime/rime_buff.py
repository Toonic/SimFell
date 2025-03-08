"""Module for Rime Buffs"""

from base import BaseBuff


class RimeBuff(BaseBuff):
    """Base class for all Rime buffs."""

    anima_gain = 0
    winter_orb_cost = 0
    anima_per_tick = 0

    def __init__(
        self,
        *args,
        anima_gain=0,
        winter_orb_cost=0,
        anima_per_tick=0,
        **kwargs,
    ):
        self.anima_gain = anima_gain
        self.winter_orb_cost = winter_orb_cost
        self.anima_per_tick = anima_per_tick
        super().__init__(*args, **kwargs)

    def is_ready(self):
        return (
            super().is_ready()
            and self.character.winter_orbs >= self.winter_orb_cost
        )

    def on_cast_complete(self):
        super().on_cast_complete()
        self.character.gain_anima(self.anima_gain)  # Gain Anima on Complete.
        if self.winter_orb_cost > 0:  # Lose Winter Orbs on Complete.
            self.character.lose_winter_orbs(self.winter_orb_cost)
        if self.winter_orb_cost < 0:  # Gain Winter Orbs on Complete.
            self.character.gain_winter_orbs(abs(self.winter_orb_cost))
