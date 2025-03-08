"""Module for the Ardeos Character."""

import random

from base import BaseCharacter

from characters._example.spells import ExampleSpell, ExampleDebuff, ExampleBuff
from characters._example.talent import ExampleTalents
from .utils.enums import SpellSimFellName


# Here is where we define the Hero class from BaseCharacter.
# This is where you will define any custom features the character may have.
# Eg. Resources, References to Spells that all other spells can reference
# like Anima Spikes, and more.
class ExampleHero(BaseCharacter):
    """Defines the Example Character."""

    winter_orbs = 0

    # Ensure you pass down the args and kwargs and reset any default values.
    def __init__(
        self,
        *args,
        **kwargs,
    ):
        super().__init__(*args, **kwargs)
        self.winter_orbs = 0

    # Here we override the configure_spell_book which each hero has. We need to
    # add our list of spells the Hero can cast to the spells using the defined
    # simefell_name from the enums along with a new instance of the spell.
    def configure_spell_book(self):
        self.spells = {
            SpellSimFellName.EXAMPLESPELL.value: ExampleSpell(),
            SpellSimFellName.EXAMPLESPELLBUFF.value: ExampleBuff(),
            SpellSimFellName.EXAMPLESPELLDEBUFF.value: ExampleDebuff(),
        }

        # We then need to go through and add a reference to the character to
        # each spell. This is just for ease of access when casing the spells.
        for spell in self.spells.values():
            spell.character = self

    # We also need to override the add_talent, as not all heroes have the same
    # talents!
    def add_talent(self, talent_identifier: str):
        # We need to get the talent from our list of talents.
        talent = ExampleTalents.get_by_identifier(talent_identifier)
        # And then apply it to the list of talents.
        if talent is not None:
            self.talents.append(talent)
            # Note: If a talent were to provide a global passive to the hero.
            # EG. Rimes Avalanche giving 5% Crit Power, you would define/apply
            # That value to here.

    # We can then define any custom functions needed for the hero to function.
    # In this example we're just using lose_winter_orb. However in Rime's case
    # We also define gain_winter_orb and gain_anima
    def lose_winter_orb(self, amount):
        """Lose Winter Orbs"""
        self.winter_orbs -= amount
        self.winter_orbs = max(self.winter_orbs, 0)

        # In the case of Rime, she can refund Winter Orbs based on a random
        # chance defined by her spirit % so we include that here.
        if random.uniform(0, 100) < self.get_spirit():
            self.winter_orbs += amount
            self.winter_orbs = min(self.winter_orbs, 5)

    # Defining what happens for gaining winter orbs.
    def gain_winter_orbs(self, amount):
        """Gain Winter Orbs"""
        self.winter_orbs += amount
        self.winter_orbs = min(self.winter_orbs, 5)
