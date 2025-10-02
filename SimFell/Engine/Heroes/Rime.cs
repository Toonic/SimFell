using SimFell.Logging;

namespace SimFell.Engine.Heroes;

public class Rime : Unit
{
    private int Anima { get; set; }
    private int WinterOrbs { get; set; }

    private const int MaxAnima = 9;
    private const int MaxWinterOrbs = 5;

    //Spells for easy reference for Talents.
    private Spell _frostBolt;
    private Spell _coldSnap;
    private Spell _freezingTorrent;
    private Spell _burstingIce;
    private Spell _flightOfTheNavir;
    private Spell _glacialBlast;
    private Spell _iceBlitz;
    private Spell _iceComet;
    private Spell _wintersBlessing;
    private Spell _wrathOfWinter;

    //Auras for easy refrence for Talents.
    private Aura _burstingIceAura;
    private Aura _wintersEmbraceBuff;

    //Talents for easy reference for Spells.
    //Row 1
    private Talent _chillingFinesse;
    private Talent _wintersEmbrace;
    private Talent _glacialAssault;

    //Row 2
    private Talent _burstBolter;
    private Talent _talonsEdict;
    private Talent _navirsKeeper;

    //Row 3
    private Talent _icyFlow;
    private Talent _avalanche;
    private Talent _coalescingFrost;

    //Row 4
    private Talent _greaterGlacialBlast;

    //Row 5
    private Talent _cascadingBlitz;
    private Talent _frostweaversWrath;
    private Talent _soulfrostTorrent;

    //Row 6
    private Talent _bitingCold;
    private Talent _wisdomOfTheNorth;

    //Custom Rime Stats.
    private Stat _spiritResetChance = new Stat(0);

    //Custom Rime Events.
    private Action<int> OnWinterOrbUpdate { get; set; }
    private Action<int> OnAnimaUpdate { get; set; }

    public Rime() : base("Rime")
    {
        ConfigureSpellBook();
        ConfigureTalents();
    }

    #region Rime Specifics

    /// <summary>
    /// Updates the Anima for Rime based on the given delta.
    /// </summary>
    /// <param name="animaDelta"></param>
    public void UpdateAnima(int animaDelta)
    {
        Anima += animaDelta;
        if (Anima > MaxAnima)
        {
            Anima = 0;
            UpdateWinterOrbs(1);
        }

        OnAnimaUpdate?.Invoke(animaDelta);
    }

    /// <summary>
    /// Updates the Winter Orbs for Rime based on the given delta.
    /// </summary>
    /// <param name="winterOrbsDelta"></param>
    public void UpdateWinterOrbs(int winterOrbsDelta)
    {
        WinterOrbs += winterOrbsDelta;
        OnWinterOrbUpdate?.Invoke(winterOrbsDelta);
        if (winterOrbsDelta < 0 && SimRandom.Roll(SpiritStat.GetValue() / 2))
        {
            WinterOrbs += -winterOrbsDelta;
        }

        if (WinterOrbs > MaxWinterOrbs)
            ConsoleLogger.Log(SimulationLogLevel.DamageEvents, "[bold red]Over Capped Winter Orbs[/]");
        WinterOrbs = Math.Clamp(WinterOrbs, 0, MaxWinterOrbs);
    }

    private Spell frostSwallowsFracture = new Spell(id: "frost-swallows-fracture", name: "Frost Swallows: Fracture");

    public void DoSwallowDamage()
    {
        int dam = SimRandom.Next(567, 693);
        DealDamage(PrimaryTarget, dam, _flightOfTheNavir);
        if (SimRandom.Roll(35))
        {
            //TODO: Figure out what the Target Cap is.
            DealAOEDamage(dam * 0.7f, 5, frostSwallowsFracture, false);
        }
    }

    #endregion

    private void ConfigureSpellBook()
    {
        _spiritResetChance.AddModifier(new DynamicModifier(Modifier.StatModType.AdditivePercent,
            () => { return SpiritStat.GetValue() / 2; }));
        SpiritStat.OnInvalidate += (Stat) => { _spiritResetChance.InvalidateCache(); };

        // Frost Bolt
        _frostBolt = new Spell(id: "frost-bolt", name: "Frost Bolt", cooldown: 0,
                castTime: 1.5f)
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(2106, 2574), spell);
                UpdateAnima(1);
            });

        // Cold Snap
        _coldSnap = new Spell("cold-snap", "Cold Snap", 12, 0)
            .HasHastedCooldown()
            .HasCharges(2)
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(2736, 3344), spell);
                UpdateWinterOrbs(1);
            });

        // Freezing Torrent
        _freezingTorrent = new Spell("freezing-torrent", "Freezing Torrent", 15, 0)
            .IsChanneled(2, 0.4f)
            .WithOnTick((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(1278, 1562), spell);
                UpdateAnima(1);
            });

        // Bursting Ice.
        _burstingIce = new Spell("bursting-ice", "Bursting Ice", 10, 2.0)
            .WithOnCast((unit, spell, targets) =>
            {
                var primaryTarget = targets.FirstOrDefault();
                primaryTarget?.ApplyDebuff(unit, primaryTarget, _burstingIceAura);
            });

        _burstingIceAura = new Aura(
                id: "bursting-ice",
                name: "Bursting Ice",
                duration: 3,
                tickInterval: 0.5
            )
            .WithOnTick((_, _, _) => UpdateAnima(1))
            .WithDamageOnTick(_burstingIce, 495, 605);

        // Flight of the Navir
        // OnDamageReceived event for Flight of the Navir.
        Action<Unit, double, Spell, bool> flightOfTheNavirDamageEvent = (unit, damage, spellSource, isCritical) =>
        {
            int swallowTriggers = spellSource == _coldSnap ? 5
                : spellSource == _freezingTorrent ? 1
                : 0;

            for (int i = 0; i < swallowTriggers; i++)
            {
                DoSwallowDamage();
            }
        };

        _flightOfTheNavir = new Spell("flight-of-the-navir", "Flight of the Navir", 60, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                var primaryTarget = targets.FirstOrDefault();

                // Applies the Debuff to the Primary target.
                primaryTarget!.ApplyDebuff(unit, primaryTarget, new Aura(
                    id: "flight-of-the-navir",
                    name: "Flight of the Navir",
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) =>
                    {
                        //Subscribes to the Units OnDamageReceived event.
                        target.OnDamageReceived += flightOfTheNavirDamageEvent;
                    },
                    onRemove: (caster, target) =>
                    {
                        //UnSubscribes to the Units OnDamageReceived event.
                        target.OnDamageReceived -= flightOfTheNavirDamageEvent;
                    }
                ));
            });

        // Glacial Blast
        _glacialBlast = new Spell("glacial-blast", "Glacial Blast", 0, 2)
            .WithCanCast(((unit, spell) => WinterOrbs >= spell.ResourceCostModifiers.GetValue(2)))
            .WithOnCast((unit, spell, targets) =>
            {
                UpdateWinterOrbs(-(int)(spell.ResourceCostModifiers.GetValue(2)));
                DealDamage(SimRandom.Next(8910, 10890), spell);
            });

        // Ice Comet
        _iceComet = new Spell("ice-comet", "Ice Comet", 0, 0)
            .WithCanCast(((unit, spell) => WinterOrbs >= 2))
            .WithOnCast((unit, spell, targets) =>
            {
                UpdateWinterOrbs(-2);
                //TODO: Figure out what the Target Cap is.
                DealAOEDamage(SimRandom.Next(4059, 4961), 5, spell);
            });

        // Ice Blitz
        Modifier iceBlitzBonusDamage = new Modifier(Modifier.StatModType.MultiplicativePercent, 20);
        _iceBlitz = new Spell("ice-blitz", "Ice Blitz", 120, 0)
            .EnableCanCastWhileCasting()
            .DisableGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "ice-blitz",
                    name: "Ice Blitz",
                    maxStacks: 1,
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) => { target.DamageBuffs.AddModifier(iceBlitzBonusDamage); },
                    onRemove: (caster, target) => { unit.DamageBuffs.RemoveModifier(iceBlitzBonusDamage); }
                ));
            });

        // Winters Blessing.
        Modifier wintersBlessingMod = new Modifier(Modifier.StatModType.AdditivePercent, 20);
        _wintersBlessing = new Spell("winters-blessing", "Winters Blessing", 120, 0)
            .DisableGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "winters-blessing",
                    name: "Winters Blessing",
                    duration: 20,
                    tickInterval: 0,
                    onApply: (caster, target) => { caster.SpiritStat.AddModifier(wintersBlessingMod); },
                    onRemove: (caster, target) => { caster.SpiritStat.RemoveModifier(wintersBlessingMod); }
                ));
            });

        // Wrath of Winter - Spirit Ability.
        Modifier wrathOfWinterDamageMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 25);
        Modifier wrathOfWinterCastTimeMod = new Modifier(Modifier.StatModType.Multiplicative, 0);
        //TODO: Wrath of Winter has a Cast Time now, and I don't know what it is. Tooltip says Instant which is wrong.
        _wrathOfWinter = new Spell("wrath-of-winter", "Wrath of Winter", 0, 1.5)
            .WithCanCast(((unit, spell) => Spirit >= 100))
            .WithOnCast((unit, spell, targets) =>
            {
                Spirit = 0;

                //Wrath of Winter buff.
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "wrath-of-winter",
                    name: "Wrath of Winter",
                    duration: 20,
                    tickInterval: 4,
                    onTick: (caster, target, aura) => { UpdateWinterOrbs(1); },
                    onApply: (caster, target) =>
                    {
                        caster.DamageBuffs.AddModifier(wrathOfWinterDamageMod);
                        _glacialBlast.CastTime.AddModifier(wrathOfWinterCastTimeMod);
                    },
                    onRemove: (caster, target) =>
                    {
                        caster.DamageBuffs.RemoveModifier(wrathOfWinterDamageMod);
                        _glacialBlast.CastTime.RemoveModifier(wrathOfWinterCastTimeMod);
                    }
                ));

                //Spirit of Heroism Buff.
                unit.ApplyBuff(unit, unit, SpiritOfHeroism);
            });

        SpellBook.Add(_iceBlitz);
        SpellBook.Add(_flightOfTheNavir);
        SpellBook.Add(_coldSnap);
        SpellBook.Add(_burstingIce);
        SpellBook.Add(_freezingTorrent);
        SpellBook.Add(_glacialBlast);
        SpellBook.Add(_frostBolt);
        SpellBook.Add(_iceComet);
        SpellBook.Add(_wintersBlessing);
        SpellBook.Add(_wrathOfWinter);

        //Aditional Tracking.
        SpellBook.Add(frostSwallowsFracture);
    }

    public void ConfigureTalents()
    {
        Talents = new List<Talent>();

        #region Row 1

        // Chilling Finesse
        _chillingFinesse = new Talent(
                id: "chilling-finesse",
                name: "Chilling Finesse",
                gridPos: "1.1")
            .WithOnActivate(unit =>
                {
                    _freezingTorrent.OnTick += (unit1, spell, arg3) => { _burstingIce.UpdateCooldown(unit1, 0.3); };
                    _coldSnap.OnCast += (unit1, spell, arg3) => { _freezingTorrent.UpdateCooldown(unit1, 1.5); };
                }
            );

        // Winters Embrace
        Modifier wintersEmbraceDamageBuff = new Modifier(Modifier.StatModType.MultiplicativePercent, 20);
        Modifier wintersEmbraceNegateBursting = new Modifier(Modifier.StatModType.InverseMultiplicativePercent, 20);
        _wintersEmbraceBuff = new Aura(
            id: "winters-embrace",
            name: "Winters Embrace",
            duration: 99999,
            tickInterval: 0,
            onApply: (unit, unit1) =>
            {
                _burstingIce.DamageModifiers.AddModifier(wintersEmbraceNegateBursting);
                unit.DamageBuffs.AddModifier(wintersEmbraceDamageBuff);
            },
            onRemove: (unit, spell) =>
            {
                _burstingIce.DamageModifiers.RemoveModifier(wintersEmbraceNegateBursting);
                unit.DamageBuffs.RemoveModifier(wintersEmbraceDamageBuff);
            }
        );

        _wintersEmbrace = new Talent(
                id: "winters-embrace",
                name: "Winters Embrace",
                gridPos: "1.2")
            .WithOnActivate(unit =>
            {
                _burstingIceAura.OnApply += (unit1, unit2) => { unit1.ApplyBuff(unit1, unit1, _wintersEmbraceBuff); };
                _burstingIceAura.OnRemove += (unit1, unit2) => { unit1.RemoveBuff(_wintersEmbraceBuff); };
            });

        // Glacial Assault
        _glacialAssault = new Talent(
                id: "glacial-assault",
                name: "Glacial Assault",
                gridPos: "1.3")
            .WithOnActivate(unit =>
                {
                    int glacialAssaultStacks = 0;
                    int glacialAssaultMaxStacks = 4;
                    Modifier instantCastMod =
                        new Modifier(Modifier.StatModType.Multiplicative,
                            0); //Multiplies cast time by 0 for instance cast.
                    Modifier damageMod =
                        new Modifier(Modifier.StatModType.MultiplicativePercent, 40); //Multiplies damage by 40$.
                    Modifier resourceCostMod =
                        new Modifier(Modifier.StatModType.Multiplicative,
                            0); //Multiplies resource by 0 for instance cast.

                    Aura glacialAssaultAura = new Aura(
                        id: "glacial-assault",
                        name: "Glacial Assault",
                        duration: 99999,
                        tickInterval: 0,
                        onApply: (unit1, unit2) =>
                        {
                            _glacialBlast.CastTime.AddModifier(instantCastMod);
                            _glacialBlast.DamageModifiers.AddModifier(damageMod);
                            _glacialBlast.ResourceCostModifiers.AddModifier(resourceCostMod);
                        },
                        onRemove: (unit1, unit2) =>
                        {
                            _glacialBlast.CastTime.RemoveModifier(instantCastMod);
                            _glacialBlast.DamageModifiers.RemoveModifier(damageMod);
                            _glacialBlast.ResourceCostModifiers.RemoveModifier(resourceCostMod);
                            glacialAssaultStacks = 0;
                        }
                    );

                    unit.OnDamageDealt += (caster, target, damage, spell) =>
                    {
                        if (spell == _coldSnap)
                        {
                            glacialAssaultStacks++;
                            if (glacialAssaultStacks == glacialAssaultMaxStacks)
                            {
                                caster.ApplyBuff(caster, caster, glacialAssaultAura);
                            }
                        }

                        if (spell == _glacialBlast && caster.HasBuff(glacialAssaultAura))
                        {
                            caster.RemoveBuff(glacialAssaultAura);
                        }
                    };
                }
            );

        Talents.Add(_chillingFinesse);
        Talents.Add(_wintersEmbrace);
        Talents.Add(_glacialAssault);

        #endregion

        #region Row 2

        // BurstBolter
        _burstBolter = new Talent(
                id: "burst-bolter",
                name: "Burst Bolter",
                gridPos: "2.1"
            )
            .WithOnActivate(unit =>
            {
                unit.OnDamageDealt += (caster, target, damage, spell) =>
                {
                    if (spell == _frostBolt)
                    {
                        UpdateAnima(2); // Bonus + 2 Anima.
                        //Same Damage/AOE as Bursting Ice.
                        DealAOEDamage(SimRandom.Next(495, 605), 5, _burstingIce);
                    }
                };
            });

        // Talon's Edict
        _talonsEdict = new Talent(
                id: "talons-edict",
                name: "Talon Edict",
                gridPos: "2.2"
            )
            .WithOnActivate(unit =>
            {
                // TODO: Spawn Swallows.
                unit.OnCrit += (caster, damage, spell) =>
                {
                    if (spell == _coldSnap)
                    {
                        DoSwallowDamage();
                        DoSwallowDamage();
                        DoSwallowDamage();
                    }
                };

                unit.OnDamageDealt += (caster, target, damage, spell) =>
                {
                    if ((spell == _coldSnap || spell == _freezingTorrent) && SimRandom.Roll(15))
                    {
                        DoSwallowDamage();
                    }
                };
            });

        // Navir's Keeper
        _navirsKeeper = new Talent(
                id: "navirs-keeper",
                name: "Navirs Keeper",
                gridPos: "2.3")
            .WithOnActivate(unit =>
                _flightOfTheNavir.OnCast += (unit1, spell, arg3) => _coldSnap.SetCurrentCharges(unit1, 2));

        Talents.Add(_burstBolter);
        Talents.Add(_talonsEdict);
        Talents.Add(_navirsKeeper);

        #endregion

        #region Row 3

        // Icy Flow
        Modifier icyFlowReduceCastTimeModifier = new Modifier(Modifier.StatModType.Flat, -0.5);
        Modifier icyFlowCritModifier = new Modifier(Modifier.StatModType.AdditivePercent, 25);
        Aura icyFlowBuff = new Aura(id: "icy-flow",
            name: "Icy Flow",
            duration: 9999,
            tickInterval: 0,
            onApply: (unit1, unit2) =>
            {
                _glacialBlast.CastTime.AddModifier(icyFlowReduceCastTimeModifier);
                _glacialBlast.CritModifiers.AddModifier(icyFlowCritModifier);
                _iceComet.CritModifiers.AddModifier(icyFlowCritModifier);
            },
            onRemove: (unit1, unit2) =>
            {
                _glacialBlast.CastTime.RemoveModifier(icyFlowReduceCastTimeModifier);
                _glacialBlast.CritModifiers.RemoveModifier(icyFlowCritModifier);
                _iceComet.CritModifiers.RemoveModifier(icyFlowCritModifier);
            });
        _icyFlow = new Talent(
                id: "icy-flow",
                name: "Icy Flow",
                gridPos: "3.1")
            .WithOnActivate(unit =>
            {
                int icyFlowStacks = 0;
                int maxIcyFlowStacks = 2;
                unit.OnDamageDealt += (caster, target, damage, spell) =>
                {
                    if (spell == _coldSnap)
                    {
                        icyFlowStacks++;
                        icyFlowStacks = Math.Min(icyFlowStacks, maxIcyFlowStacks);
                        if (!caster.HasBuff(icyFlowBuff)) caster.ApplyBuff(caster, caster, icyFlowBuff);
                    }

                    if (spell == _glacialBlast || spell == _iceComet)
                    {
                        icyFlowStacks--;
                        icyFlowStacks = Math.Max(icyFlowStacks, 0);
                        if (icyFlowStacks == 0 && caster.HasBuff(icyFlowBuff)) caster.RemoveBuff(icyFlowBuff);
                    }
                };
            });

        // Avalanche
        _avalanche = new Talent(
                id: "avalanche",
                name: "Avalanche",
                gridPos: "3.2")
            .WithOnActivate(unit =>
            {
                _iceComet.OnCast += (unit1, spell, unit2) =>
                {
                    double rollChance = SimRandom.NextDouble();
                    if (rollChance < 0.07)
                    {
                        DealAOEDamage(SimRandom.Next(4059, 4961), 5, spell);
                        DealAOEDamage(SimRandom.Next(4059, 4961), 5, spell);
                    }
                    else if (rollChance < 0.15)
                    {
                        DealAOEDamage(SimRandom.Next(4059, 4961), 5, spell);
                    }
                };
            });

        // Coalescing Frost
        //Used for Damage Tracking - Future better handling needed.
        Spell coalescingFrost = new Spell(id: "coalescing-frost", name: "Coalescing Frost", 0, 0);
        Aura coalescingFrostAura = new Aura(
            id: "coalescing-frost",
            name: "Coalescing Frost",
            duration: 3,
            tickInterval: 0);
        coalescingFrostAura.WithOnRemove((unit1, unit2) =>
        {
            double damage = coalescingFrostAura.CurrentStacks * 53;
            DealAOEDamage(damage, 5, coalescingFrost);
        });
        coalescingFrostAura.WithIncreaseStacks((unit, unit1) => { coalescingFrostAura.ResetDuration(); });


        _coalescingFrost = new Talent(
                id: "coalescing-frost",
                name: "Coalescing Frost",
                gridPos: "3.3")
            .WithOnActivate(unit =>
            {
                //Adds it to the Spellbook for tracking.
                SpellBook.Add(coalescingFrost);

                _freezingTorrent.OnTick += (unit1, spell, unit2) =>
                {
                    if (unit.PrimaryTarget != null)
                    {
                        if (unit.PrimaryTarget.HasDebuff(coalescingFrostAura))
                        {
                            coalescingFrostAura.IncreaseStack();
                        }
                        else
                        {
                            unit.PrimaryTarget.ApplyDebuff(unit, unit.PrimaryTarget, coalescingFrostAura);
                        }
                    }
                };
            });

        Talents.Add(_icyFlow);
        Talents.Add(_avalanche);
        Talents.Add(_coalescingFrost);

        #endregion

        # region Row 4

        // Greater Glacial Blast
        _greaterGlacialBlast = new Talent(
                id: "greater-glacial-blast",
                name: "Greater Glacial Blast",
                gridPos: "4.2")
            .WithOnActivate((unit) =>
            {
                _glacialBlast.DamageModifiers.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 40));
                _glacialBlast.CastTime.AddModifier(new Modifier(Modifier.StatModType.Flat, 0.5));
            });

        Talents.Add(_greaterGlacialBlast);

        #endregion

        # region Row 5

        // Cascading Blitz
        // TODO: 
        _cascadingBlitz = new Talent(
                id: "cascading-blitz",
                name: "Cascading Blitz",
                gridPos: "5.1")
            .WithOnActivate((unit) =>
            {
                if (HasBuff(icyFlowBuff))
                {
                    OnAnimaUpdate += (delta) => { DoSwallowDamage(); };
                    OnDamageDealt += (unit1, target, d, spell) =>
                    {
                        if (spell == _flightOfTheNavir)
                        {
                            icyFlowBuff.UpdateDuration(0.1);
                        }
                    };
                }
            });

        // Frostweavers Wrath
        Modifier frostWeaversWrathCritModifier = new Modifier(Modifier.StatModType.AdditivePercent, 100);
        Aura frostweaversWrath =
            new Aura(id: "frostwavers-wrath", name: "Frostweavers Wrath", duration: 12, tickInterval: 0)
                .WithOnApply((unit, unit1) =>
                {
                    _iceComet.CritModifiers.AddModifier(frostWeaversWrathCritModifier);
                    _glacialBlast.CritModifiers.AddModifier(frostWeaversWrathCritModifier);
                })
                .WithOnRemove((unit1, unit2) =>
                {
                    _iceComet.CritModifiers.RemoveModifier(frostWeaversWrathCritModifier);
                    _glacialBlast.CritModifiers.RemoveModifier(frostWeaversWrathCritModifier);
                });

        _frostweaversWrath = new Talent(
                id: "frostweavers-wrath",
                name: "Frostweavers Wrat",
                gridPos: "5.2")
            .WithOnActivate((unit) =>
            {
                OnWinterOrbUpdate += (delta) =>
                {
                    if (!HasBuff(frostweaversWrath) && SimRandom.Roll(17))
                    {
                        ApplyBuff(this, this, frostweaversWrath);
                    }
                };

                OnDamageDealt += (unit1, target, d, spell) =>
                {
                    if (spell == _iceComet || spell == _glacialBlast)
                    {
                        RemoveBuff(frostweaversWrath);
                    }
                };
            });

        // Soulfrost Torrent
        // TODO: Soulfrost shouldn't be applying the soulFrostTickSpeed and soulFrostCrit buff unless the spell is casted.
        Modifier soulFrostTickSpeed = new Modifier(Modifier.StatModType.InverseMultiplicativePercent, 40);
        Modifier soulFrostCrit = new Modifier(Modifier.StatModType.AdditivePercent, 100);
        Aura soulFrostTorrent = new Aura(id: "soulfrost-torrent", name: "Soul Frost", duration: 18, tickInterval: 0,
            maxStacks: 2);
        soulFrostTorrent.WithOnApply((unit, unit1) =>
        {
            _freezingTorrent.CritModifiers.AddModifier(soulFrostCrit);
            _freezingTorrent.TickRate.AddModifier(soulFrostTickSpeed);
        });
        soulFrostTorrent.WithOnRemove((unit1, unit2) =>
        {
            _freezingTorrent.CritModifiers.RemoveModifier(soulFrostCrit);
            _freezingTorrent.TickRate.RemoveModifier(soulFrostTickSpeed);
        });
        soulFrostTorrent.WithIncreaseStacks(((unit, unit1) => soulFrostTorrent.ResetDuration()));

        _soulfrostTorrent = new Talent(
                id: "soulfrost-torrent",
                name: "Soulfrost Torrent",
                gridPos: "5.3")
            .WithOnActivate(unit =>
            {
                RPPM proc = new RPPM(1.5);
                OnCast += (unit1, unit2, spell) =>
                {
                    if (proc.TryProc(this))
                    {
                        // Handle if Soulfrost procs during Soulfrost. Hence 2 Max Stacks.
                        if (!HasBuff(soulFrostTorrent))
                            ApplyBuff(this, this, soulFrostTorrent);
                        else soulFrostTorrent.IncreaseStack();
                    }
                };

                OnCastDone += (unit1, unit2, spell) =>
                {
                    if (HasBuff(soulFrostTorrent)) soulFrostTorrent.DecreaseStack();
                };
            });

        Talents.Add(_cascadingBlitz);
        Talents.Add(_frostweaversWrath);
        Talents.Add(_soulfrostTorrent);

        #endregion

        #region Row 6

        // Biting Cold
        _bitingCold =
            new Talent(
                    id: "biting-cold",
                    name: "Biting Cold",
                    gridPos: "6.1")
                .WithOnActivate(unit =>
                    CriticalStrikePowerStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 10))
                );

        // Wishdom of the North
        _wisdomOfTheNorth = new Talent(
                id: "wisdom-of-the-north",
                name: "Wisdom of the North",
                gridPos: "6.3")
            .WithOnActivate(unit =>
            {
                OnWinterOrbUpdate += (delta) =>
                {
                    _iceBlitz.UpdateCooldown(this, 0.3 * delta);
                    _flightOfTheNavir.UpdateCooldown(this, 0.3 * delta);
                    _wintersBlessing.UpdateCooldown(this, 0.3 * delta);
                };
            });

        #endregion
    }
}