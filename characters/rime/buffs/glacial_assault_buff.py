"""Module for Glacial Assault Buff"""

from base import BaseBuff
from characters.rime.talent import GlacialAssaultTalent


class GlacialAssaultBuff(BaseBuff):
    """Glacial Assault buff."""

    def __init__(self):
        super().__init__(
            "Glacial Assault",
            duration=float("inf"),
            maximum_stacks=GlacialAssaultTalent.maximum_stacks,
        )
