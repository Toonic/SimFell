"""Module for Example Debuffs"""

from base import BaseDebuff


# We need to define a base class for the Example hero's debuffs. In some cases,
# A debuff for say Rime could gain anima per tick, or orbs per tick so we need
# to be able to support that. In this example we will assume no additional info
# is required for Debuffs for sake of simplicity.
class ExampleHeroDebuff(BaseDebuff):
    """Base class for all Example debuffs."""

    def __init__(
        self,
        *args,
        **kwargs,
    ):
        # As always pass up the base.
        super().__init__(*args, **kwargs)

    # Note: You can define more overrides here if you wish. BaseDebuff come
    # from the BaseSpell class so all overrides are still valid here.
