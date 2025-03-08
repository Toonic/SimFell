"""Module for the Rime Character."""

import random

from base import BaseCharacter
from base.character import CharacterTalentT
from characters.rime.spells import (
    AnimaSpikes,
    BurstingIce,
    ColdSnap,
    DanceOfSwallows,
    FreezingTorrent,
    FrostBolt,
    GlacialBlast,
    IceBlitz,
    IceComet,
    WrathOfWinter,
)
from characters.rime.talent import RimeTalents, AvalancheTalent

from .utils.enums import SpellSimFellName


# Defines the Rime Character class.
class Rime(BaseCharacter):
    """Stat Point DR"""

    anima_spikes = None
    anima = 0
    winter_orbs = 0

    def __init__(self, intellect, crit, expertise, haste, spirit):
        super().__init__(intellect, crit, expertise, haste, spirit)
        self.anima = 0
        self.winter_orbs = 0

    def configure_spell_book(self):
        self.spells = {
            SpellSimFellName.WRATH_OF_WINTER.value: WrathOfWinter(),
            SpellSimFellName.FROST_BOLT.value: FrostBolt(),
            SpellSimFellName.COLD_SNAP.value: ColdSnap(),
            SpellSimFellName.FREEZING_TORRENT.value: FreezingTorrent(),
            SpellSimFellName.BURSTING_ICE.value: BurstingIce(),
            SpellSimFellName.GLACIAL_BLAST.value: GlacialBlast(),
            SpellSimFellName.ICE_COMET.value: IceComet(),
            SpellSimFellName.DANCE_OF_SWALLOWS.value: DanceOfSwallows(),
            SpellSimFellName.ICE_BLITZ.value: IceBlitz(),
        }

        self.anima_spikes = AnimaSpikes()
        self.anima_spikes.character = self

        self.dance_of_swallows = DanceOfSwallows()
        self.dance_of_swallows.character = self

        # I couldn't find a clean way to handle this. Up for solutions.
        for spell in self.spells.values():
            spell.character = self

    def gain_anima(self, amount):
        """Gain Anima"""
        self.anima += amount
        if self.anima >= 10:
            self.anima = 0
            self.gain_winter_orbs(1)

        if SpellSimFellName.ICE_BLITZ.value in self.buffs:
            for _ in range(amount):
                self.anima_spikes.cast()

    def gain_winter_orbs(self, amount):
        """Gain Winter Orbs"""
        self.winter_orbs += amount
        for _ in range(3):
            self.anima_spikes.cast()
        if self.winter_orbs > 5:
            self.winter_orbs = 5

    def lose_winter_orbs(self, amount):
        """Lose Winter Orbs"""
        self.winter_orbs -= amount
        self.winter_orbs = max(self.winter_orbs, 0)
        if random.uniform(0, 100) < self.get_spirit():
            self.winter_orbs += amount
            self.winter_orbs = min(self.winter_orbs, 5)

    def add_talent(self, talent_identifier: str):
        talent = RimeTalents.get_by_identifier(talent_identifier)
        if talent is not None:
            self.talents.append(talent)
            if talent == RimeTalents.AVALANCHE:
                self.crit_power_multiplier += AvalancheTalent.bonus_crit_power
