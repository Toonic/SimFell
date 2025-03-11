"""Module for Fire Frogs Spell"""

from characters.ardeos import ArdeosSpell
from characters.ardeos.debuffs import FireFrogsDebuff


class FireFrogs(ArdeosSpell):
    """Fire Frogs Spell"""

    number_of_frogs = 3
    number_of_attacks = 3

    def __init__(self):
        super().__init__(
            "Fire Frogs",
            cooldown=45,
            damage_percent=89,
            debuff=FireFrogsDebuff(),
        )

    def cast(self, do_damage=True):
        super().cast(False)
        for _ in range(self.number_of_frogs):
            for _ in range(self.number_of_attacks):
                self.damage()
                self.apply_debuff()
