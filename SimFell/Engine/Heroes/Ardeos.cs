using System.Diagnostics;
using SimFell.Logging;

namespace SimFell.Engine.Heroes;

public class Ardeos : Unit
{
    private double Cinders { get; set; }
    private int BurningEmbers { get; set; }

    private const int MaxCinders = 100;
    private const int MaxBurningEmbers = 4;

    private Spell _apocalypse;
    private Spell _detonate;

    private Spell _engulfingFlames;
    private Aura engulfingFlamesDebuff;

    private Spell _fireball;

    private Spell _fireFrogs;
    private Aura fireFrogs;

    private Spell _infernalWave;
    private Spell _pyromania;
    private Spell _searingBlaze;
    private Spell _wildfire;

    //Custom Ardeos Events.
    private Action<double> OnCinderUpdate { get; set; }
    private Action<int> OnBurningEmberUpdate { get; set; }
    private Dictionary<Unit, List<Aura>> ActiveDotsByTarget { get; set; } = new Dictionary<Unit, List<Aura>>();

    private struct FireFrogsReference
    {
        public Aura aura;
        public double damage;
    }

    public Ardeos() : base("Ardeos")
    {
        ConfigureSpellBook();
        ConfigureTalents();
    }

    #region Ardeos Specifics

    /// <summary>
    /// Updates the Cinders for Ardeos based on the given delta.
    /// </summary>
    /// <param name="delta"></param>
    public void UpdateCinders(double delta)
    {
        Cinders += delta;
        if (Cinders > MaxCinders)
        {
            Cinders = Cinders - MaxCinders;
            UpdateBurningEmbers(1);
        }

        OnCinderUpdate?.Invoke(delta);
    }

    /// <summary>
    /// Updates the Burning Embers for Ardeos based on the given delta.
    /// </summary>
    /// <param name="delta"></param>
    public void UpdateBurningEmbers(int delta)
    {
        BurningEmbers += delta;
        OnBurningEmberUpdate?.Invoke(delta);
        if (delta < 0 && SimRandom.Roll(SpiritStat.GetValue() / 2))
        {
            BurningEmbers += -delta;
        }

        if (BurningEmbers > MaxBurningEmbers)
            ConsoleLogger.Log(SimulationLogLevel.DamageEvents, "[bold red]Over Capped Burning Embers[/]");
        BurningEmbers = Math.Clamp(BurningEmbers, 0, MaxBurningEmbers);
    }

    private void ApplyWithDotTracker(Unit inTarget, Aura inAura)
    {
        inAura.WithOnRemove((caster, target) => OnRemove(target, inAura));
        inTarget.ApplyDebuff(this, inTarget, inAura);
        // Ensure the list is there.
        if (!ActiveDotsByTarget.ContainsKey(inTarget))
        {
            ActiveDotsByTarget[inTarget] = new List<Aura>();
        }

        ActiveDotsByTarget[inTarget].Add(inAura);
    }

    private int ActiveDotsCount()
    {
        return ActiveDotsByTarget.Values.Sum(auraList => auraList.Count);
    }

    private void OnRemove(Unit target, Aura inAura)
    {
        ActiveDotsByTarget[target].Remove(inAura);
    }

    #endregion

    private void ConfigureSpellBook()
    {
        // Apocalypse
        _apocalypse = new Spell(
                "apocalypse",
                "Apocalypse",
                60,
                3
            )
            .WithOnCast((unit, spell, targets) =>
            {
                // TODO: Damage falloff/Target Cap
                DealAOEDamage(SimRandom.Next(6957, 8503), 5, spell);
                Unit currentPrimaryTarget = PrimaryTarget;
                foreach (var target in targets)
                {
                    //Hacky work around.
                    SetPrimaryTarget(target);
                    _searingBlaze.FreeCast(this, targets);
                }

                SetPrimaryTarget(currentPrimaryTarget);
            });

        // Detonate
        _detonate = new Spell(
                "detonate",
                "Detonate"
            )
            .WithCanCast(((unit, spell) => BurningEmbers >= 1 && ActiveDotsCount() > 0)) // Never snap with 0 Dots.
            .WithOnCast((caster, spell, targets) =>
            {
                UpdateBurningEmbers(-1);
                foreach (var target in ActiveDotsByTarget.Keys)
                {
                    double totalDamage = 0;
                    foreach (Aura aura in ActiveDotsByTarget[target])
                    {
                        totalDamage += aura.GetSimulatedDamage(2, false, true, false);
                    }

                    double damage = totalDamage / 3;
                    // Three instances of "Fourth of July Fireworks"
                    DealDamage(target, damage, spell, true, false, true);
                    DealDamage(target, damage, spell, true, false, true);
                    DealDamage(target, damage, spell, true, false, true);
                }
            });

        // Engulfing Flames.
        _engulfingFlames = new Spell(
                "engulfing-flames",
                "Engulfing Flames",
                20,
                1.5
            )
            .WithOnCast((
                (unit, spell, targets) =>
                {
                    // Applies Debuff to Primary Target.
                    ApplyWithDotTracker(PrimaryTarget, engulfingFlamesDebuff);
                }));

        engulfingFlamesDebuff = new Aura(
                "engulfing-flames",
                "Engulfing Flames",
                9, 1.5, 1
            )
            .WithDamageOnTick(_engulfingFlames, 1638, 2002)
            .WithOnTick(((_, _, aura) =>
            {
                double cinderGainPerTick = 30 / (aura.Duration / aura.GetTickInterval());
                UpdateCinders(cinderGainPerTick);
            }));

        // Fire Ball
        // TODO: Target Cap
        Spell fireballDot = new Spell("fire-ball", "Fire Ball: Dot");

        _fireball = new Spell(
            "fire-ball",
            "Fire Ball",
            30).WithOnCast((unit, spell, targets) =>
        {
            Action<Unit, Unit, double, Spell> onDamageHandler = (caster, target, damage, spell) =>
            {
                if (spell == _fireball)
                {
                    ApplyWithDotTracker(target, new Aura(
                            "fire-ball",
                            "Fire Ball: Dot",
                            12,
                            2
                        )
                        .WithOnTick((caster, target, aura) => { UpdateCinders(1); })
                        .WithDamageOnTick(fireballDot, damage * 0.7, damage * 0.7,
                            true, false, false, true));
                }
            };
            OnDamageDealt += onDamageHandler;
            DealAOEDamage(SimRandom.Next(2529, 3091), 5, spell);
            OnDamageDealt -= onDamageHandler;
        });

        // Fire Frogs.
        Spell fireFrogsDot = new Spell(
            "fire-frogs",
            "Fire Frogs: Dot",
            45
        );
        _fireFrogs = new Spell(
                "fire-frogs",
                "Fire Frogs",
                45
            )
            .WithOnCast((caster, spell, targets) =>
            {
                Dictionary<Unit, FireFrogsReference> frogAuras = new Dictionary<Unit, FireFrogsReference>();
                int frogCount = 5;
                int jumpCount = 3;
                for (int i = 0; i < frogCount; i++)
                {
                    for (int x = 0; x < jumpCount; x++)
                    {
                        Unit target = Targets[SimRandom.Next(0, Targets.Count)];
                        double damage = DealDamage(target, SimRandom.Next(693, 847), _fireFrogs);
                        // Create or get the FireFrog Reference.
                        FireFrogsReference reference;
                        if (frogAuras.ContainsKey(target)) reference = frogAuras[target];
                        else
                        {
                            reference = new FireFrogsReference();

                            // TODO: Get the Tick Rate of Fire Frogs.
                            reference.aura = new Aura(
                                "fire-frogs",
                                "Fire Frogs",
                                12,
                                3
                            );
                            ApplyWithDotTracker(target, reference.aura);
                        }

                        // Update the stored damage + update what the tick does.
                        reference.damage += damage;
                        reference.aura.ClearOnTick();
                        reference.aura.WithDamageOnTick(fireFrogsDot, reference.damage, reference.damage, true,
                            false, false, true);

                        frogAuras[target] = reference;
                    }
                }
            });

        // Infernal Wave
        _infernalWave = new Spell(
                "infernal-wave",
                "Infernal Wave",
                0, 1.5
            )
            .WithOnCast((unit, spell, targets) =>
            {
                UpdateCinders(40);
                DealDamage(PrimaryTarget, SimRandom.Next(1395, 1705), spell);
            });

        // TODO: DONT DO THE COOLDOWN HACKY WORK AROUND TO TEST
        _searingBlaze = new Spell(
                "searing-blaze",
                "Searing Blaze"
            )
            .WithOnCast((unit, spell, targets) =>
            {
                ApplyWithDotTracker(PrimaryTarget,
                    new Aura(
                            "searing-blaze",
                            "Searing Blaze",
                            24, 2)
                        .WithOnTick((_, _, aura) =>
                        {
                            double cinderGainPerTick = 12 / (aura.Duration / aura.GetTickInterval());
                            UpdateCinders(cinderGainPerTick);
                        })
                        .WithDamageOnTick(_searingBlaze, 603, 737)
                );
            });

        // Pyromania
        _pyromania = new Spell(
                "pyromania",
                "Pyromania",
                90,
                0
            )
            .WithOnCast((caster, spell, targets) =>
                {
                    List<Unit> orderedTargets = new List<Unit>(targets);
                    orderedTargets.Remove(PrimaryTarget); //Remove the Primary Target because it is always the Primary.

                    //Order the List based on Highest HP No Debuff > Duration Left on Debuff
                    orderedTargets.Sort((a, b) =>
                    {
                        bool aHasDebuff = a.HasDebuff(engulfingFlamesDebuff);
                        bool bHasDebuff = b.HasDebuff(engulfingFlamesDebuff);

                        // Sort by no Debuff first.
                        if (aHasDebuff && !bHasDebuff) return 1;
                        if (!aHasDebuff && bHasDebuff) return -1;

                        // Then by highest HP.
                        if (!aHasDebuff && !bHasDebuff)
                        {
                            return b.Health.BaseValue.CompareTo(a.Health.BaseValue);
                        }

                        // If they have the buff then it is sorted by the duration left.
                        var aDuration = a.GetDebuff(engulfingFlamesDebuff)?.GetDuration() ?? 0;
                        var bDuration = b.GetDebuff(engulfingFlamesDebuff)?.GetDuration() ?? 0;

                        return bDuration.CompareTo(aDuration);
                    });

                    //Apply EngulfingFlames on the targets.
                    PrimaryTarget.ApplyDebuff(caster, PrimaryTarget, engulfingFlamesDebuff);
                    foreach (var target in orderedTargets.Take(2))
                    {
                        target.ApplyDebuff(caster, PrimaryTarget, engulfingFlamesDebuff);
                    }
                }
            );

        // Wildfire
        // TODO: All of it.
        _wildfire = new Spell(
                "wildfire",
                "Wildfire",
                45)
            .WithOnCast((unit, spell, targets) => { });

        SpellBook.Add(_apocalypse);
        SpellBook.Add(_detonate);
        SpellBook.Add(_fireball);
        SpellBook.Add(_engulfingFlames);
        SpellBook.Add(_searingBlaze);
        SpellBook.Add(_fireFrogs);
        SpellBook.Add(_infernalWave);
        SpellBook.Add(_pyromania);
    }

    private void ConfigureTalents()
    {
    }
}