"""Module for Rime's talents."""

from base import CharacterTalent, Talent


# This is where we define each talent as a class. We use the class for static
# variables. EG: How much bonus damage it does, PPM, etc. That way, if a patch
# comes out and they change one of these values. We only need to change it
# in one location.
class ExampleBonusDamageTalent(Talent):
    """Defines static variables for the Example Talent"""

    bonus_damage_for_example_spell = 1.5  # Multiplier


# We also need to define the characters talents mapped to where they are located
# on the talent chart in game. Where top row is 1.1, 1.2, 1.3. Second row is
# 2.1, 2.2, 2.3. Etc.
class ExampleTalents(CharacterTalent):
    """Enum for Template's talents."""

    # We also define a readable name here for output.
    EXAMPLEBONUSDAMAGE = ExampleBonusDamageTalent(
        "1.1", "Example Bonus Damage Talent"
    )
