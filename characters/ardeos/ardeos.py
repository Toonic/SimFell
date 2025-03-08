"""Module for the Ardeos Character."""

import random

from base import BaseCharacter
from characters.ardeos.spells import InfernoBolt

from .utils.enums import SpellSimFellName


# Defines the Ardeos Character class.
class Ardeos(BaseCharacter):
    """Stat Point DR"""

    ember = 0
    burning_embers = 0

    def __init__(self, intellect, crit, expertise, haste, spirit):
        super().__init__(intellect, crit, expertise, haste, spirit)
        self.ember = 0
        self.burning_embers = 0

    def configure_spell_book(self):
        self.spells = {
            SpellSimFellName.INFERNOBOLT.value: InfernoBolt(),
        }

        # I couldn't find a clean way to handle this. Up for solutions.
        for spell in self.spells.values():
            spell.character = self

    def gain_ember(self, amount):
        """Gain Anima"""
        self.ember += amount
        if self.ember >= 10:
            self.ember = 0
            self.gain_burning_embers(1)

    def gain_burning_embers(self, amount):
        """Gain Winter Orbs"""
        self.burning_embers += amount
        self.burning_embers = min(self.burning_embers, 4)

    def lose_burning_embers(self, amount):
        """Lose Winter Orbs"""
        self.burning_embers -= amount
        self.burning_embers = max(self.burning_embers, 0)
        if random.uniform(0, 100) < self.get_spirit():
            self.burning_embers += amount
            self.burning_embers = min(self.burning_embers, 4)

    def add_talent(self, talent_identifier: str):
        print("Todo")
        # talent = RimeTalents.get_by_identifier(talent_identifier)
        # if talent is not None:
        #     self.talents.append(talent)
        #     if talent == RimeTalents.AVALANCHE:
        #         self.crit_power_multiplier += AvalancheTalent.bonus_crit_power
