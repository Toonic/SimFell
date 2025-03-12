"""Module for Ardeos Debuffs"""

from base import BaseDebuff
from characters.ardeos.buffs import WildfireBuff


class ArdeosDebuff(BaseDebuff):
    """Base class for all Rime debuffs."""

    wild_fire_buff = WildfireBuff()

    def __init__(
        self,
        *args,
        ember_per_tick=0,
        **kwargs,
    ):
        self.ember_per_tick = ember_per_tick
        super().__init__(*args, **kwargs)

    def get_tick_rate_modifier(self, tick_rate):
        new_tick_rate = tick_rate
        if self.wild_fire_buff.simfell_id in self.character.buffs:
            new_tick_rate = 1 / (1 + (30 / 100))
        return new_tick_rate
