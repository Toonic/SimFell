"""Module for Incinerate Spell"""

from characters.ardeos import ArdeosSpell
from characters.ardeos.debuffs import IncinerateDebuff
from characters.ardeos.buffs import IncinerateBuff


class Incinerate(ArdeosSpell):
    """Incinerate Spell"""

    incinerate_debuff = IncinerateDebuff()

    def __init__(self):
        super().__init__(
            "Incinerate",
            cast_time=4.5,
            cooldown=500,  # TODO: Spirit
            damage_percent=73,  # Tick Based.
            ember_per_tick=40,
            channeled=True,
            base_tick_duration=0.5,
        )

    def cast(self, do_damage=True):
        IncinerateBuff().apply(self.character)
        super().cast(do_damage)

    def on_tick(self):
        self.incinerate_debuff.apply(self.character)
        self.character.gain_ember(self.ember_per_tick)
