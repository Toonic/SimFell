"""Base class for all debuffs."""

from rich import print  # pylint: disable=redefined-builtin

from base import BaseSpell
from base import BaseCharacter


class BaseDebuff(BaseSpell):
    """Abstract base class for all debuffs."""

    remaining_time = 0

    def __init__(self, *args, duration=0, maximum_stacks=1, **kwargs):
        super().__init__(*args, **kwargs)
        self.duration = duration
        self.tick_rate = 0
        self.time_to_next_tick = 0
        self.maximum_stacks = maximum_stacks
        self._is_active = True

    def cast(self, do_damage=False):
        super().cast(do_damage)

    def on_cast_complete(self):
        super().on_cast_complete()
        self.apply_debuff()

    def apply(self, character: "BaseCharacter") -> None:
        """Applies the debuff to the target."""
        self.character = character
        self.remaining_time = self.duration
        if self.base_tick_duration > 0:
            self.tick_rate = self.base_tick_duration / (
                1 + (self.character.get_haste() / 100)
            )

            self.time_to_next_tick = self.tick_rate
        else:
            self.time_to_next_tick = self.duration
        self.character.simulation.debuffs[self.simfell_id] = self
        self._is_active = True

        if self.character.simulation.do_debug:
            print(
                f"Time {self.character.simulation.time:.2f}: "
                + f"✔️ Applied [deep_pink4]{self.name} "
                + "(Debuff)[/deep_pink4] to enemy."
            )

    def update_remaining_duration(self, delta_time: int) -> None:
        """Decreases the remaining buff/debuff duration by the delta time."""

        if self._is_active:
            while delta_time > 0 and self.remaining_time > 0:
                if delta_time >= self.time_to_next_tick:
                    delta_time -= self.time_to_next_tick
                    self.remaining_time -= self.time_to_next_tick
                    self.time_to_next_tick = self.tick_rate
                    self.on_tick()
                else:
                    self.time_to_next_tick -= delta_time
                    self.remaining_time -= delta_time
                    delta_time = 0

            if self.remaining_time <= 0 and self._is_active:
                self.remove()

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
