"""Module for ExampleSpell Spell"""

from characters._example import ExampleHeroSpell
from characters._example.talent import ExampleTalents, ExampleBonusDamageTalent


# Here is where we define the spells. In this case an example spell, using the
# TemplateHeroSpell we created earlier.
class ExampleSpell(ExampleHeroSpell):
    """Defines an example spell."""

    # In this case, we define the spell name, cast time, and how much damage it
    # will do. However we also include the winter_orb_cost we defined in
    # the TemplateHeroSpell.
    def __init__(self):
        super().__init__(
            "Example Spell",
            cast_time=1.5,
            damage_percent=145,
            winter_orb_cost=1,
        )

    # Here we can override the damage of the spell. In this example,
    # We can apply a damage modifier based on a talent that we may have.
    def damage_modifiers(self, damage):
        # We first check to see if the talent is selected.
        if self.character.has_talent(ExampleTalents.EXAMPLEBONUSDAMAGE):
            # Then we modify the damage based on the static variable assigned.
            damage *= ExampleBonusDamageTalent.bonus_damage_for_example_spell

        # Then lastly, we return the damage.
        return damage
