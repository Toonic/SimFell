"""Module for Ice Blitz Buff"""

from base import BaseBuff
from characters.rime.talent import RimeTalents


class IceBlitzBuff(BaseBuff):
    """Glacial Assault buff."""

    ice_blitz_damage_multiplier = 0.15

    def __init__(self):
        super().__init__("Ice Blitz", duration=20, maximum_stacks=1)

    def on_apply(self):
        damage_multiplier = self.ice_blitz_damage_multiplier
        if self.character.has_talent(RimeTalents.WISDOM_OF_THE_NORTH):
            wisdom_of_the_north = RimeTalents.WISDOM_OF_THE_NORTH.value
            damage_multiplier += (
                wisdom_of_the_north.ice_blitz_bonus_damage / 100
            )
        self.character.damage_multiplier += damage_multiplier

    def on_remove(self):
        damage_multiplier = self.ice_blitz_damage_multiplier
        if self.character.has_talent(RimeTalents.WISDOM_OF_THE_NORTH):
            wisdom_of_the_north = RimeTalents.WISDOM_OF_THE_NORTH.value
            damage_multiplier -= (
                wisdom_of_the_north.ice_blitz_bonus_damage / 100
            )
        self.character.damage_multiplier -= damage_multiplier
