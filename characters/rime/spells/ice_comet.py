"""Module for Ice Comet Spell"""

import random
from characters.rime import RimeSpell
from characters.rime.talent import AvalancheTalent, RimeTalents


class IceComet(RimeSpell):
    """Ice Comet Spell"""

    def __init__(self):
        super().__init__("Ice Comet", damage_percent=300, winter_orb_cost=3)

    def on_cast_complete(self):
        if self.character.has_talent(RimeTalents.AVALANCHE):
            if random.uniform(0, 100) < AvalancheTalent.double_comet_chance:
                self.damage()
                if (
                    random.uniform(0, 100)
                    < AvalancheTalent.triple_comet_chance
                ):
                    self.damage()
