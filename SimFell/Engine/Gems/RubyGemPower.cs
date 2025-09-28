namespace SimFell.Engine.Base.Gems;

public class RubyGemPower : GemPower
{
    public RubyGemPower()
    {
        GemType = GemType.Ruby;
    }

    protected override void ConfigureGemEffects()
    {
        var rubyT1 = new GemEffect("unyielding-vitality", "Unyielding Vitality", 240)
            .WithOnActivate(unit =>
            {
                // TODO: Code Unyielding Vitality and what it does when you activate it.
            })
            .WithOnDeactivate(unit =>
            {
                // TODO: Code Unyielding Vitality and what it does when you deactivate it.
                // This is used specifically for when you get Unyielding Vitality - II as the first one gets removed.
                // Useful for we ever get a weird Legendary that reads "Chance on Crit to gain 30% more Gem Power" or
                // Something weird like that.
            });

        gemEffects.Add(rubyT1);
    }
}