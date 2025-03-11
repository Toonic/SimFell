"""Base class for all debuffs."""

from typing import final
from rich import print  # pylint: disable=redefined-builtin

from base import BaseSpell
from base import BaseCharacter


class BaseDebuff(BaseSpell):
    """Abstract base class for all debuffs."""

    remaining_time = 0

    def __init__(
        self,
        *args,
        duration=0,
        maximum_stacks=1,
        percent_of_damage_per_tick_per_stack=1,
        **kwargs,
    ):
        super().__init__(*args, **kwargs)
        self.duration = duration
        self.tick_rate = 0
        self.time_to_next_tick = 0
        self.current_stacks = 0
        self.maximum_stacks = maximum_stacks
        self.percent_of_damage_per_tick_per_stack = (
            percent_of_damage_per_tick_per_stack
        )
        self._is_active = True

    def cast(self, do_damage=False):
        super().cast(do_damage)

    def on_cast_complete(self):
        super().on_cast_complete()
        self.apply_debuff()

    def apply(self, character: "BaseCharacter") -> None:
        """Applies the debuff to the target."""
        self.character = character

        # Checks to see if the debuff is already on the target and refreshes
        # and/or increases the stack count.
        if self.simfell_id in self.character.simulation.debuffs:
            self.character.simulation.debuffs[self.simfell_id].reapply()
            return

        self.current_stacks = 1
        self.set_values()

        self.character.simulation.debuffs[self.simfell_id] = self
        self._is_active = True

        if self.character.simulation.do_debug:
            print(
                f"Time {self.character.simulation.time:.2f}: "
                + f"✔️ Applied [deep_pink4]{self.name} "
                + "(Debuff)[/deep_pink4] to enemy."
            )

    @final
    def set_values(self):
        """Calculates the tick duration and sets the current duration."""
        self.remaining_time = self.duration

        if self.base_tick_duration > 0:
            self.tick_rate = self.base_tick_duration / (
                1 + (self.character.get_haste() / 100)
            )

            self.time_to_next_tick = self.get_tick_rate_modifier(
                self.tick_rate
            )
        else:
            self.time_to_next_tick = self.duration

    def reapply(self):
        """Re-Applies the Debuff or Increases its Stack Count."""
        if self.current_stacks < self.maximum_stacks:
            self.current_stacks += 1

        self.set_values()

        if self.character.simulation.do_debug:
            print(
                f"Time {self.character.simulation.time:.2f}: "
                + f"🔄 Re-Applied [dark_green]{self.name} "
                + "(Buff)[/dark_green] to character."
            )

    def update_remaining_duration(self, delta_time: int) -> None:
        """Decreases the remaining buff/debuff duration by the delta time."""

        if self._is_active:
            while delta_time > 0 and self.remaining_time > 0:
                if delta_time >= self.time_to_next_tick:
                    delta_time -= self.time_to_next_tick
                    self.remaining_time -= self.time_to_next_tick
                    self.time_to_next_tick = self.get_tick_rate_modifier(
                        self.tick_rate
                    )
                    self.on_tick()
                else:
                    self.time_to_next_tick -= delta_time
                    self.remaining_time -= delta_time
                    delta_time = 0

            if self.remaining_time <= 0 and self._is_active:
                self.remove()

    def get_tick_rate_modifier(self, tick_rate):
        """Allows for overriding the base tickrate from buffs."""
        return tick_rate

    def remove(self) -> None:
        """Removes the debuff from the target."""

        self.remaining_time = 0
        self.character.simulation.debuffs.pop(self.simfell_id, None)
        self._is_active = False

        if self.character.simulation.do_debug:
            print(
                f"Time {self.character.simulation.time:.2f}: "
                + f"❌ Removed [deep_pink4]{self.name} "
                + "(Debuff)[/deep_pink4] from enemy."
            )
