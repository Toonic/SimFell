"""Module for ExampleSpell Spell"""

from characters._example import ExampleHeroSpell
from characters._example.buffs import ExampleBuff


# In this example we will define a spell that will apply a buff on our Hero.
class ExampleBuffSpell(ExampleHeroSpell):
    """Defines an example spell."""

    # The main difference here is that we apply a buff on cast instead of
    # dealing damage directly. In this case, the ExampleBuff we created.
    def __init__(self):
        super().__init__(
            "Example Buff Spell", cast_time=1.5, buff=ExampleBuff()
        )
