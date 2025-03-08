"""Module for ExampleHeroSpell's spells."""

from base import BaseSpell


# Defines a template hero spell from the BaseSpell class. The reason for the
# per hero spell is due to not all heroes sharing a similar resource.
# EG: Rime uses Winter Orbs and Anima, and Ardeos uses Embers.
class ExampleHeroSpell(BaseSpell):
    """Defines a Hero Spell."""

    # Here is where you would define any additional features the hero spell
    # Might use, in this example, a winter orb cost..
    winter_orb_cost = 0

    # You then define the __init__ ensuring to pass up all args and kwargs.
    # You can also define other arguments, again in this example, a winter orb.
    def __init__(
        self,
        *args,
        winter_orb_cost=0,
        **kwargs,
    ):
        # Ensure you always pass down the arguments.
        super().__init__(*args, **kwargs)

        self.winter_orb_cost = winter_orb_cost

    # In this example we override the is_ready function to ensure we have
    # enough winter orbs to cast a spell, along with the base is_ready().
    # This will be used by the Simulation in order to check if the Hero can
    # Cast this spell or not.
    def is_ready(self):
        return (
            super().is_ready()  # We check both the orb cost and the base.
            and self.character.winter_orbs >= self.winter_orb_cost
        )

    # Lastly, in this example, we override the on_cast_complete in order to
    # remove the winter_orb_cost from the heroes total winter orbs.
    def on_cast_complete(self):
        super().on_cast_complete()  # Once again, ensure you call the base.
        if self.winter_orb_cost > 0:
            self.character.lose_winter_orb(self.winter_orb_cost)
