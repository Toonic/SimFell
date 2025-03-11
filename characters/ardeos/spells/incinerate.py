"""Module for Incinerate Spell"""

from characters.ardeos import ArdeosSpell
from characters.ardeos.debuffs import IncinerateDebuff


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

    def on_tick(self):
        self.incinerate_debuff.apply(self.character)
        self.character.gain_ember(self.ember_per_tick)
