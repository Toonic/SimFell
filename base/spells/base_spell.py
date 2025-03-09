"""Module for the Spell class."""

import random
from abc import ABC
from typing import TYPE_CHECKING, final
from rich import print  # pylint: disable=redefined-builtin

if TYPE_CHECKING:
    from base.character import BaseCharacter


class BaseSpell(ABC):
    """Abstract base class for all spells."""

    def __init__(
        self,
        name="",
        cast_time=0,  # The casttime of the spell.
        cooldown=0,  # The cooldown of the spell.
        # The Damage the spell does based on the players main stat.
        damage_percent=0,
        channeled=False,  # If the spell is channeled or not.
        base_tick_duration=-1,  # How many ticks of damage over the duration.
        has_gcd=True,  # If the spell has a GCD or not.
        # If the spell can be cast while GCD  is happening or not.
        can_cast_on_gcd=False,
        # If the spell can be cast while casting or not.
        can_cast_while_casting=False,
        buff=None,
        debuff=None,
    ):
        self.name = name
        self.cast_time = cast_time
        self.cooldown = cooldown
        self.damage_percent = damage_percent
        self.channeled = channeled
        self.base_tick_duration = base_tick_duration
        self.has_gcd = has_gcd
        self.can_cast_on_gcd = can_cast_on_gcd
        self.can_cast_while_casting = can_cast_while_casting
        self.remaining_cooldown = 0
        self.character = None
        self.buff = buff
        self.debuff = debuff
        self.ticks = 0

    @final
    def set_character(self, character: "BaseCharacter") -> None:
        """Sets the character for the spell."""
        self.character = character

    @property
    def simfell_id(self) -> str:
        """Returns the name of the spell in the simfell file."""
        return self.name.lower().replace(" ", "_")

    def is_ready(self) -> bool:
        """Returns True if the spell is ready to be cast."""
        return self.remaining_cooldown <= 0

    def effective_cast_time(self) -> float:
        """Returns the effective cast time of the spell.
        Including any modifiers."""
        return self.cast_time * (1 - self.character.get_haste() / 100)

    def cast(self, do_damage=True) -> None:
        """Casts the spell."""
        if self.channeled:
            self.character.simulation.gcd = self.get_gcd()
            self.set_cooldown()  # Channeled spells cooldown starts on cast.
            self.ticks = int(self.cast_time / self.base_tick_duration)
            if do_damage:
                self.damage()
            for _ in range(self.ticks):
                self.character.simulation.update_time(self.base_tick_duration)
                if do_damage:
                    self.damage()
                self.on_tick()
        else:
            self.character.simulation.gcd = self.get_gcd()
            self.character.simulation.update_time(self.effective_cast_time())
            if do_damage:
                self.damage()
            self.on_cast_complete()

    # NOTE: Public because Tariq has some spells with a static GCD
    # so this will future support that.
    def get_gcd(self) -> float:
        """Returns the GCD of the spell."""
        return (
            1.5 / (1 + self.character.get_haste() / 100) if self.has_gcd else 0
        )

    def damage(self) -> float:
        """Returns the damage of the spell. Including any modifiers."""
        # TODO: Handle AOE.
        damage = self.damage_percent  # The base damage of the spell.
        damage = self.damage_modifiers(
            damage  # Damage modifiers that modify the base %.
        )
        damage = self.damage_modified_player_stats(
            damage  # The damage after being modified by player stats.
        )
        damage *= 1 + self.character.damage_multiplier

        # Roll for Crit Damage.
        if random.uniform(0, 100) < self.get_crit_chance():
            self.on_crit()
            damage *= 2 * self.character.get_crit_power()

        if damage > 0:
            if self.character.simulation.do_debug:
                print(
                    f"Time {self.character.simulation.time:.2f}: "
                    + f"💥 [cornflower_blue]{self.name}[/cornflower_blue] "
                    + f"deals [bold red]{damage:.2f}[/bold red] damage"
                )

        self.character.simulation.damage += damage

    # Used as an override for damage modifiers from Talents and other sources.
    def damage_modifiers(self, damage) -> float:
        """Returns the damage of the spell. Including any modifiers."""
        return damage

    @final
    def damage_modified_player_stats(self, damage) -> float:
        """Returns the damage of the spell after being modified
        by player stats"""
        return (
            (damage / 100)
            * self.character.get_main_stat()
            * (1 + self.character.get_expertise() / 100)
        )

    @final
    def get_crit_chance(self) -> float:
        """Returns the crit chance of the spell."""
        return self.crit_chance_modifiers(self.character.get_crit())

    def crit_chance_modifiers(self, crit_chance) -> float:
        """Returns the crit chance of the spell. Including any modifiers."""

        # NOTE: This method will be used to override the crit chance
        # of the spell.

        return crit_chance

    def on_crit(self) -> None:
        """Called when a spell crits."""

    def on_tick(self) -> None:
        """The effect of the spell on each tick."""

    def on_cast_complete(self) -> None:
        """The effect of the spell when the cast is complete."""
        self.apply_buff()
        self.apply_debuff()
        self.set_cooldown()

    def set_cooldown(self) -> None:
        """Sets the cooldown of the spell."""
        self.remaining_cooldown = self.cooldown

    def reset_cooldown(self) -> None:
        """Resets the cooldown of the spell."""
        self.remaining_cooldown = 0

    def update_cooldown(self, delta_time: int) -> None:
        """Decreases the remaining cooldown by the delta time."""
        self.remaining_cooldown -= delta_time

    def apply_buff(self):
        """Applies the associated buff to the character."""
        if self.buff is not None:
            self.buff.apply(self.character)

    def apply_debuff(self):
        """Applies the associated debuff to the enemy."""
        if self.debuff is not None:
            self.debuff.apply(self.character)
