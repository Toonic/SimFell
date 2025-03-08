"""Module for Rime's talents."""

from base import CharacterTalent, Talent


class ChillblainTalent(Talent):
    """Defines static variables for Chillblain"""

    percentage_of_damage = 20  # As a %
    maximum_enemies = 5
    bonus_torrent_damage = 20  # As a %


class CoalescingIceTalent(Talent):
    """Defines static variables for Coalescing Ice"""

    bonus_bursting_damage = 20
    bonus_anima_single_target = 2


class GlacialAssaultTalent(Talent):
    """Defines static variables for the Glacial Assault Talent"""

    maximum_stacks = 4
    bonus_damage = 100  # As a %.
    bonus_critical_strike = 20  # As a %


class UnrelentingIceTalent(Talent):
    """Defines static variables for the Unrelenting Ice Talent"""

    bursting_ice_cdr_from_torrent = 0.5  # Flat value in seconds.


class IcyFlowTalent(Talent):
    """Defines static variables for the Icy Flow Talent"""

    torrent_cdr_from_anima_spikes = 0.2  # Flat value in seconds.


class AvalancheTalent(Talent):
    """Defines static variables for the Avalanche Talent"""

    bonus_crit_power = 0.05  # As Multiplier.
    double_comet_chance = 20  # As Percent
    triple_comet_chance = 4  # As Percent


class WisdomOfTheNorthTalent(Talent):
    """Defines static variables for the Wisdom of the North Talent"""

    cdr_per_orb_spent = 1  # Flat value in seconds.
    ice_blitz_bonus_damage = 10  # Additional Percent.


class SoulfrostTorrentTalent(Talent):
    """Defines static variables for the Soulfrost Torrent Talent"""

    anima_and_swallow_crit_bonus = 20  # As Percent
    torrent_bonus_damage = 1.5  # Multiplier
    torrent_bonus_duration = 2  # Multiplier
    soulfrost_ppm = 1.5  # PPM


class RimeTalents(CharacterTalent):
    """Enum for Rime's talents."""

    CHILLBLAIN = ChillblainTalent("1.1", "Chillblain")
    COALESCING_ICE = CoalescingIceTalent("1.2", "Coalescing Ice")
    GLACIAL_ASSAULT = GlacialAssaultTalent("1.3", "Glacial Assault")
    UNRELENTING_ICE = UnrelentingIceTalent("2.1", "Unrelenting Ice")
    ICY_FLOW = IcyFlowTalent("2.2", "Icy Flow")
    TUNDRA_GUARD = Talent("2.3", "Tundra Guard")
    AVALANCHE = AvalancheTalent("3.1", "Avalanche")
    WISDOM_OF_THE_NORTH = WisdomOfTheNorthTalent("3.2", "Wisdom of the North")
    SOULFROST_TORRENT = SoulfrostTorrentTalent("3.3", "Soulfrost Torrent")
