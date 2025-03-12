"""Module for Wrath of Winter Buff"""

from base import BaseBuff


class IncinerateBuff(BaseBuff):
    """Glacial Assault buff."""

    haste_additional_bonus = 30

    def __init__(self):
        super().__init__(
            "Incinerate",
            duration=20,
            maximum_stacks=1,
        )

    def on_apply(self):
        self.character.haste_additional += self.haste_additional_bonus

    def on_remove(self):
        self.character.haste_additional -= self.haste_additional_bonus
