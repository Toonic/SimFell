# On application of the buff. Set a new variable which effects tick rate in the player spells/buffs?    b"""Module for Ice Blitz Buff"""

from base import BaseBuff


class WildfireBuff(BaseBuff):
    """Glacial Assault buff."""

    tick_rate_increase = 0.3

    def __init__(self):
        super().__init__("Wildfire", duration=9, maximum_stacks=1)
