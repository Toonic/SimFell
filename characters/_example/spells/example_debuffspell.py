"""Module for ExampleSpell Spell"""

from characters._example import ExampleHeroSpell
from characters._example.debuffs import ExampleDebuff


# In this example we will define a spell that will apply a debuff on our target
class ExampleBuffSpell(ExampleHeroSpell):
    """Defines an example spell."""

    # The main difference here is that we apply a debuff on cast instead of
    # dealing damage directly. In this case, the ExampleDebuff we created.
    def __init__(self):
        super().__init__(
            "Example Debuff Spell",
            cast_time=0,
            has_gcd=False,  # Example of no GCD on a cst time of 0.
            debuff=ExampleDebuff(),
        )
