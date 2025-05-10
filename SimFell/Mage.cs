namespace SimFell;

public class Mage : Unit
{
    public int Mana { get; set; }
    public int Intellect { get; set; }

    public Mage(string name, int health) : base(name, health)
    {
        Mana = 100;
        Intellect = 300;

        SpellBook.Add(
            //Compact added spell. Showing you don't need to use id: etc.
            new Spell("01", "Ignite Mind", 12f,
                //Confirms if we can cast the spell or not.
                canCast: () => Mana >= 30,
                //Defines the OnCast event.
                onCast: (caster, targets) =>
                {
                    //Reduce Mana.
                    // Mana -= 30;
                    //Gets the first target only in this example. Useful for AOE spells where first target is primary.
                    var firstTarget = targets.FirstOrDefault();
                    //Ensure the first target isn't Null. Safety first!
                    if (firstTarget == null) return;
                    caster.DealDamage(firstTarget, (int)(Intellect * 0.25f));
                    // This is an example just showing you can use id: etc.
                    var burning = new Aura(
                        id: "02",
                        name: "Burning",
                        duration: 10,
                        tickInterval: 1,
                        onTick: (target) => caster.DealDamage(target, (int)(Intellect * 0.1f))
                    );
                    //Applies the debuff to the first target.
                    firstTarget.ApplyDebuff(burning);
                }
            )
        );
        SpellBook.Add(
            new("03", "Bloodlust", 300f,
                shouldCastFirst: true,
                canCast: () => Mana >= 60,
                onCast: (caster, targets) =>
                {
                    // var mage_caster = caster as Mage ?? throw new Exception("Caster is not a Mage");
                    // var originalIntellect = mage_caster.Intellect;
                    // var mage_caster = caster as Mage ?? throw new Exception("Caster is not a Mage");
                    // var originalIntellect = mage_caster.Intellect;
                    
                    var bonusIntellect = (int)(Intellect * 1.2f);
                    
                    var bloodlustBuff = new Aura(
                        id: "04",
                        name: "Bloodlust Buff",
                        duration: 25,
                        tickInterval: 1,
                        // Increase intellect by 20%
                        onApply: unit =>
                        {
                            Intellect += bonusIntellect;
                        },
                        // Revert the Intellect to original value.
                        onRemove: unit =>
                        {
                            Intellect -= bonusIntellect;
                        }
                    );
                    caster.ApplyBuff(bloodlustBuff);

                    var fearedDebuff = new Aura(
                        id: "05",
                        name: "Feared",
                        duration: 10,
                        tickInterval: 1,
                        onApply: unit => unit.DamageReceivedMultiplier = 1.2f,
                        onRemove: unit => unit.DamageReceivedMultiplier = 1.0f
                    );
                    foreach (var target in targets)
                    {
                        target.ApplyDebuff(fearedDebuff);
                    }
                }
            )
        );
    }
}