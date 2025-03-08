"""Module for Example Buffs"""

from base import BaseBuff


# We need to define a base class for the Example hero's buffs. In some cases,
# A buff for say Rime could gain anima per tick, or orbs per tick so we need
# to be able to support that. In this example, we will assume all or most buffs
# have a winter_orb_per_tick attached to them.
class ExampleHeroBuff(BaseBuff):
    """Base class for all Example buffs."""

    winter_orb_per_tick = 0

    def __init__(
        self,
        *args,
        winter_orb_per_tick=0,
        **kwargs,
    ):
        # As always pass up the base.
        super().__init__(*args, **kwargs)
        self.winter_orb_cost = winter_orb_per_tick

    # Note: You can define more overrides here if you wish. BaseBuffs come
    # from the BaseSpell class so all overrides are still valid here.
