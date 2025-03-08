"""Module for Cold Snap Spell"""

from characters.rime import RimeSpell
from characters.rime.talent import RimeTalents
from characters.rime.buffs import GlacialAssaultBuff
from characters.rime.utils.enums import SpellSimFellName


class ColdSnap(RimeSpell):
    """Cold Snap Spell"""

    def __init__(self):
        super().__init__(
            "Cold Snap", damage_percent=204, winter_orb_cost=-1, cooldown=8
        )

        self._dance_of_swallows_trigger_count = 10

    def apply_buff(self):
        if self.character.has_talent(RimeTalents.GLACIAL_ASSAULT):
            GlacialAssaultBuff().apply(self.character)

    def on_cast_complete(self):
        super().on_cast_complete()

        # Trigger Dance of Swallows on cast if the buff is there.
        dance_of_swallows = self.character.simulation.get_debuff(
            SpellSimFellName.DANCE_OF_SWALLOWS.value
        )

        if dance_of_swallows is not None:
            # Dance of Swallows is hard coded to trigger 10 times from ColdSnap
            for _ in range(self._dance_of_swallows_trigger_count):
                dance_of_swallows.damage()
