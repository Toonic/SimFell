"""Module for Ardeos Debuffs"""

from base import BaseDebuff


class ArdeosDebuff(BaseDebuff):
    """Base class for all Rime debuffs."""

    def __init__(
        self,
        *args,
        ember_per_tick=0,
        **kwargs,
    ):
        self.ember_per_tick = ember_per_tick
        super().__init__(*args, **kwargs)
