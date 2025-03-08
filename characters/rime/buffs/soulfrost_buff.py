"""Module for Soulfrost Buff"""

from base import BaseBuff


class SoulfrostBuff(BaseBuff):
    """Glacial Assault buff."""

    def __init__(self):
        super().__init__(
            "Soul Frost",
            duration=float("inf"),
            maximum_stacks=1,
        )
