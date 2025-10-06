using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Parlot.Fluent;
using SimFell.Logging;

namespace SimFell.Engine.Heroes;

public class Meiko : Unit
{
    //Expose combo state for APL use.
    public int BuilderOne { get; set; }
    public int BuilderTwo { get; set; }
    //Spells for easy reference for Talents.
    private Spell _autoAttack;
    private Spell _spiritPalm;
    private Spell _windKick;
    private Spell _earthFist;
    private Spell _finisher;
    private Spell _shatterEarth;
    private Spell _shatterEarthstat;
    private Spell _twinSoulsBulwark;
    private Spell _twinSoulsArmy;
    private Spell _stoneShield;
    private Spell _stoneStomp;
    private Spell _risingEarth;
    private Spell _doublePalmStrike;
    private Spell _risingStorm;
    private Spell _lashingStormkick;
    private Spell _earthfistBarrage;
    private Spell _spiritedVortex;

    //Auras for easy refrence for Talents.
    private Aura _spiritedStrikes;
    private Aura _spiritedVortexAura;
    private Aura _spiritedStrikesWill;
    private Aura _spiritedVortexWill;
    private Aura _stormfall;
    private Aura _twinSoulsBulwarkAura;
    private Aura _earthfall;
    private Aura _shatterEarthDot;

    //Talents for easy reference for Spells.
    //Row 1
    private Talent _resonanceOfEarth;
    private Talent _perfectStorm;
    private Talent _willOfStone;

    //Row 2
    private Talent _thunderingKicks;
    private Talent _harshWinds;
    private Talent _earthenGuard;

    //Row 3
    private Talent _earthwell;
    private Talent _debilitatingVortex;
    private Talent _unwaveringSpirit;

    //Row 4
    private Talent _slipstream;
    private Talent _magicWard;
    private Talent _spiritedAdvance;

    //Row 5
    private Talent _sereneResilience;
    private Talent _earthbourne;
    private Talent _forbiddenTechniqueTalent;

    //Row 6
    private Talent _peacefield;
    private Talent _superiorBulwark;
    private Talent _conclusiveStrikes;

    //Row 7 - Legenaries/Weapons
    private Talent _neck;
    private Talent _back;
    private Talent _fatedStrike;

    // Combo tracking for Meiko
    private Spell? _lastBuilder;
    private Spell? _currentBuilder;
    private Spell? _pendingFinisher;

    // Bulwark HP
    // until we have player health implemented.
    private int bulwarkHP = 79431;
    public Meiko() : base("Meiko")
    {
        ConfigureSpellBook();
        ConfigureTalents();
        this.BaseGCD = 1.0;
        this.HastedGCD = false;
    }

    #region Meiko Specifics

    private void ResetCombo()
    {
        _lastBuilder = null;
        _currentBuilder = null;
        _pendingFinisher = null;
        BuilderOne = 0;
        BuilderTwo = 0;
    }

    private void UpdateComboState(Spell builder)
    {
        if (_lastBuilder == null)
        {
            if (builder == _spiritPalm)
            {
                BuilderOne = 1;
            }
            if (builder == _windKick)
            {
                BuilderOne = 2;
            }
            if (builder == _earthFist)
            {
                BuilderOne = 3;
            }
        }
        else if (_lastBuilder != null && _lastBuilder != builder)
        {
            if (builder == _spiritPalm)
            {
                BuilderTwo = 1;
            }
            if (builder == _windKick)
            {
                BuilderTwo = 2;
            }
            if (builder == _earthFist)
            {
                BuilderTwo = 3;
            }
        }

        _lastBuilder = _currentBuilder;

        _currentBuilder = builder;

        if (_lastBuilder != null && _currentBuilder != null && _lastBuilder != _currentBuilder)
        {
            _pendingFinisher = GetFinisherForCombo(_lastBuilder, _currentBuilder);
        }
    }

    private Spell? GetFinisherForCombo(Spell first, Spell second)
    {
        if (first == _spiritPalm && second == _windKick) return _doublePalmStrike;
        if (first == _spiritPalm && second == _earthFist) return _spiritedVortex;
        if (first == _windKick && second == _spiritPalm) return _risingStorm;
        if (first == _windKick && second == _earthFist) return _lashingStormkick;
        if (first == _earthFist && second == _spiritPalm) return _risingEarth;
        if (first == _earthFist && second == _windKick) return _earthfistBarrage;
        return null;
    }

    #endregion
    private void ConfigureSpellBook()
    {
        // Auto Attack
        _autoAttack = new Spell("auto-attack", "Auto Attack", 1.9, 0)
            .HasHastedCooldown()
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                // No tooltip to base it off of, so I used a quickplay log I had.
                DealDamage(targets.FirstOrDefault(), SimRandom.Next(343, 378), spell, includeExpertise: false, isFlatDamage: true);
            });


        // Spirit Palm
        _spiritPalm = new Spell("spirit-palm", "Spirit Palm", 0, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(360, 440), spell);
                UpdateComboState(_spiritPalm);
            });

        // Wind Kick
        _windKick = new Spell("wind-kick", "Wind Kick", 3, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                DealAOEDamage(180, 220, 3, spell);
                UpdateComboState(_windKick);
            });

        // Earth Fist
        _earthFist = new Spell("earth-fist", "Earth Fist", 0, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(360, 440), spell);
                UpdateComboState(_earthFist);
            });

        // Double Palm Strike
        _spiritedStrikes = new Aura("spirited-strikes", "Spirited Strikes", 15, 2);
        _doublePalmStrike = new Spell("double-palm-strike", "Double Palm Strike", 2, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(1170, 1430), spell);
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "spirited-strikes",
                    name: "Spirited Strikes",
                    duration: 15,
                    tickInterval: 0
                ));
            });

        // Spirited Vortex
        _spiritedVortexAura = new Aura("spirited-vortex", "Spirited Vortex", 15, 2);
        _spiritedVortex = new Spell("spirited-vortex", "Spirited Vortex", 2, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                ApplyBuff(unit, unit, new Aura(
                    id: "spirited-vortex",
                    name: "Spirited Vortex",
                    duration: 18,
                    tickInterval: 2)
                    .WithoutHastedTicks()
                    .WithAOEDamageOnTick(_spiritedVortex, 189, 231, 3));
            });

        // Rising Storm
        _risingStorm = new Spell("rising-storm", "Rising Storm", 2, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                DealAOEDamage(540, 660, 3, spell);
                Modifier damageMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 100);
                var stormfall = new Aura(
                    id: "stormfall",
                    name: "Stormfall",
                    duration: 999,
                    tickInterval: 0,
                    maxStacks: 2,
                    onApply: (caster, target) =>
                    {
                        _stormfall.CurrentStacks = 2;
                        _lashingStormkick.DamageModifiers.AddModifier(damageMod);
                    },
                    onRemove: (caster, target) =>
                    {
                        _stormfall.CurrentStacks = 0;
                        _lashingStormkick.DamageModifiers.RemoveModifier(damageMod);
                    }
                );

                _stormfall = stormfall;
                unit.ApplyBuff(unit, unit, stormfall);

                if (_earthfall != null && unit.HasBuff(_earthfall))
                {
                    unit.RemoveBuff(_earthfall);
                }
            });

        // Lashing Stormkick
        _lashingStormkick = new Spell("lashing-stormkick", "Lashing Stormkick", 2, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                DealAOEDamage(450, 550, 3, spell);
                if (_stormfall != null && HasBuff(_stormfall))
                {
                    _stormfall.DecreaseStack();
                    if (SimRandom.Roll(SpiritStat.GetValue() / 2)) _stormfall.IncreaseStack();
                }
            });

        // Rising Earth
        Modifier damageMod = new Modifier(Modifier.StatModType.MultiplicativePercent, 100);
        var earthfall = new Aura(
            id: "earthfall",
            name: "Earthfall",
            duration: 999,
            tickInterval: 0,
            maxStacks: 2,
            onApply: (caster, target) =>
            {
                _earthfall.CurrentStacks = 2;
                _earthfistBarrage.DamageModifiers.AddModifier(damageMod);
            },
            onRemove: (caster, target) =>
            {
                _earthfistBarrage.DamageModifiers.RemoveModifier(damageMod);
            }
        );earthfall.WithDecreaseStacks((unit, unit1) =>
        {
            ConsoleLogger.Log(
                    SimulationLogLevel.CastEvents,
                    $"Earthfall lost a stack, current stacks: {earthfall.CurrentStacks}"
                );
            if (SimRandom.Roll(SpiritStat.GetValue() / 2))
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.CastEvents,
                    $"Spirit refunded Earthfall, current stacks: {earthfall.CurrentStacks}"
                );
                earthfall.IncreaseStack();
            }
        });

        _risingEarth = new Spell("rising-earth", "Rising Earth", 2, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                DealDamage(SimRandom.Next(1134, 1386), spell);
                


                _earthfall = earthfall;
                unit.ApplyBuff(unit, unit, earthfall);

                if (_stormfall != null && unit.HasBuff(_stormfall))
                {
                    unit.RemoveBuff(_stormfall);
                }
            });

        // Earthfist Barrage
        int earthfistBarrageTickCount = 0;
        _earthfistBarrage = new Spell("earthfist-barrage", "Earthfist Barrage", 2, 0)
            .IsChanneled(1.5, 0.3f)
            .WithHastedChannel()
            .WithoutPartialTicks()
            .WithOnTick((unit, spell, targets) =>
            {
                earthfistBarrageTickCount++;
                if (earthfistBarrageTickCount <= 5)
                {
                    DealDamage(SimRandom.Next(207, 253), spell);
                }
                else
                {
                    DealDamage(SimRandom.Next(828, 1012), spell);
                    if (_earthfall != null && HasBuff(_earthfall))
                    {
                        _earthfall.DecreaseStack();
                    }
                    earthfistBarrageTickCount = 0;
                }

            });

        // Finisher
        _finisher = new Spell("finisher", "Finisher", 2, 0)
            .WithCanCast((unit, spell) => _pendingFinisher != null)
            .OffGCD()
            
            .WithOnCast((unit, spell, targets) =>
            {
                if (_pendingFinisher != null)
                {
                    unit.StartCasting(_pendingFinisher, targets);
                    ResetCombo();
                }
            });

        // Shatter Earth
        // Shatter Earth has a travel time that would dissallow ogcd casting, but until I can confirm the duration of that its instant.
        _shatterEarthstat = new Spell("shatter-earth-dot", "Shatter Earth Dot", 0, 0);
        _shatterEarthDot = new Aura(
            id: "shatter-earth",
            name: "Shatter Earth",
            duration: 6,
            tickInterval: 1,
            onTick: (unit, target, spell) =>
            {
                var t = new List<Unit> { target }; //idk
                unit.StartCasting(_shatterEarthstat, t);
            }
        ).WithoutHastedTicks().WithAOEDamageOnTick(_shatterEarthstat, 504, 616, 999 );

        _shatterEarth = new Spell("shatter-earth", "Shatter Earth", 60, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                DealAOEDamage(2790, 3410, 3, spell);
                var primaryTarget = targets.FirstOrDefault();
                primaryTarget?.ApplyDebuff(unit, primaryTarget, _shatterEarthDot);
            });

        // Twin Souls Bulwark
        _twinSoulsBulwarkAura = new Aura(
            id: "twin-souls-bulwark-aura",
            name: "Twin Souls Bulwark",
            duration: 20,
            tickInterval: 0
        ).WithoutPartialTicks();

        _twinSoulsBulwark = new Spell("twin-souls-bulwark", "Twin Souls Bulwark", 180, 0)
            .WithOnCast((unit, spell, targets) =>
            {
                unit.ApplyBuff(unit, unit, _twinSoulsBulwarkAura);
            });

        // Twin Souls Army of One
        _twinSoulsArmy = new Spell("twin-souls-army", "Twin Souls Army", 0, 0)
            .WithCanCast(((unit, spell) => Spirit >= 100))
            .WithOnCast((unit, spell, targets) =>
            {
                Spirit = 0;
                unit.ApplyBuff(unit, unit, SpiritOfHeroism);
                unit.ApplyBuff(unit, unit, new Aura(
                    id: "twin-souls-army-aura",
                    name: "Twin Souls Army",
                    duration: 10,
                    tickInterval: .25,

                    onTick: (caster, targets, aura) =>
                    {
                        Unit target = Targets[SimRandom.Next(0, Targets.Count)];
                        DealDamage(target, SimRandom.Next(189, 231), _twinSoulsArmy);
                    }
                ).WithoutHastedTicks());

            });

        // Stone Shield
        _stoneShield = new Spell("stone-shield", "Stone Shield", 30, 0)
            .HasHastedCooldown()
            .OffGCD()
            .HasCharges(2)
            .WithOnCast((unit, spell, targets) =>
            {
                // TODO - Spread the damage events out over 1s.
                DealAOEDamage(450, 550, 5, spell);
                DealAOEDamage(450, 550, 5, spell);
                DealAOEDamage(450, 550, 5, spell);
                DealAOEDamage(450, 550, 5, spell);

            });

        // Stone Stomp
        _stoneStomp = new Spell("stone-stomp", "Stone Stomp", 120, 0)
            .OffGCD()
            .WithOnCast((unit, spell, targets) =>
            {
                DealAOEDamage(855, 1045, 999, spell);
            });

        SpellBook.Add(_autoAttack);
        SpellBook.Add(_spiritPalm);
        SpellBook.Add(_windKick);
        SpellBook.Add(_earthFist);
        SpellBook.Add(_doublePalmStrike);
        SpellBook.Add(_spiritedVortex);
        SpellBook.Add(_risingStorm);
        SpellBook.Add(_lashingStormkick);
        SpellBook.Add(_risingEarth);
        SpellBook.Add(_earthfistBarrage);
        SpellBook.Add(_finisher);
        SpellBook.Add(_shatterEarth);
        SpellBook.Add(_shatterEarthstat);
        SpellBook.Add(_twinSoulsBulwark);
        SpellBook.Add(_twinSoulsArmy);
        SpellBook.Add(_stoneShield);
        SpellBook.Add(_stoneStomp);
    }

    public void ConfigureTalents()
    {
        Talents = new List<Talent>();

        #region Row 1

        // Resonance of Earth
        _resonanceOfEarth = new Talent(
                id: "resonance-of-earth",
                name: "Resonance of Earth",
                gridPos: "1.1")
            .WithOnActivate(unit =>
            {
                // NYI
            }
            );

        // Perfect Storm
        _perfectStorm = new Talent(
                id: "perfect-storm",
                name: "Perfect Storm",
                gridPos: "1.2")
            .WithOnActivate(unit =>
            {
                unit.OnDamageDealt += (caster, target, damage, spell) =>
                {
                    if (spell == _lashingStormkick)
                    {
                        if (SimRandom.Roll(0.5))
                        {
                            int bonusDamage = (int)(damage * 0.3);
                            DealAOEDamage(bonusDamage, bonusDamage, 3, spell, isFlatDamage: true);
                        }
                    }
                };
            }

        );

        // Will of Stone
        _spiritedVortexWill = new Aura(
            "spirited-vortex", 
            "Spirited Vortex",
            30,
            2)
            .WithoutHastedTicks()
            .WithAOEDamageOnTick(_spiritedVortex, 189, 231, 3);
        _spiritedStrikesWill = new Aura("spirited-strikes", "Spirited Strikes", 36, 0);
        _willOfStone = new Talent(
                id: "will-of-stone",
                name: "Will of Stone",
                gridPos: "1.3")
            .WithOnActivate(unit =>
            {
                _shatterEarth.OnCast += (caster, spell, targets) =>
                {
                    ApplyBuff(caster, caster, _spiritedStrikesWill);

                    ApplyBuff(caster, caster, _spiritedVortexWill);
                };
            });

        Talents.Add(_resonanceOfEarth);
        Talents.Add(_perfectStorm);
        Talents.Add(_willOfStone);

        #endregion

        #region Row 2

        // Thundering Kicks
        Modifier thunderingKicksCritModifier = new Modifier(Modifier.StatModType.AdditivePercent, 20);
        _thunderingKicks = new Talent(
                id: "thundering-kicks",
                name: "Thundering Kicks",
                gridPos: "2.1"
            )
            .WithOnActivate(unit =>
            {
                _lashingStormkick.CritModifiers.AddModifier(thunderingKicksCritModifier);
            });

        // Harsh Winds
        Modifier harshWindsDamageModifier = new Modifier(Modifier.StatModType.MultiplicativePercent, 20);
        _harshWinds = new Talent(
                id: "harsh-winds",
                name: "Harsh Winds",
                gridPos: "2.2"
            )
            .WithOnActivate(unit =>
            {
                _spiritedVortex.DamageModifiers.AddModifier(harshWindsDamageModifier);
            });

        // Earthen Guard
        Modifier earthenGaurdDamageModifier = new Modifier(Modifier.StatModType.MultiplicativePercent, 20);
        _earthenGuard = new Talent(
                id: "earthen-guard",
                name: "Earthen Guard",
                gridPos: "2.3")
            .WithOnActivate(unit =>
            {
                _shatterEarth.DamageModifiers.AddModifier(earthenGaurdDamageModifier);
                _earthFist.DamageModifiers.AddModifier(earthenGaurdDamageModifier);
                _earthfistBarrage.DamageModifiers.AddModifier(earthenGaurdDamageModifier);
                _stoneShield.DamageModifiers.AddModifier(earthenGaurdDamageModifier);
                _stoneStomp.DamageModifiers.AddModifier(earthenGaurdDamageModifier);
            });

        Talents.Add(_harshWinds);
        Talents.Add(_thunderingKicks);
        Talents.Add(_earthenGuard);

        #endregion

        #region Row 3

        // Earthwell
        _earthwell = new Talent(
                id: "earthwell",
                name: "Earthwell",
                gridPos: "3.1")
            .WithOnActivate(unit =>
            {
                // NYI
            });

        // Debilitating Vortex
        _debilitatingVortex = new Talent(
                id: "debilitating-vortex",
                name: "Debilitating Vortex",
                gridPos: "3.2")
            .WithOnActivate(unit =>
            {
                // NYI
            });

        // Unwavering Spirit

        _unwaveringSpirit = new Talent(
                id: "unwavering-spirit",
                name: "Unwavering Spirit",
                gridPos: "3.3")
            .WithOnActivate(unit =>
            {
                SpiritStat.AddModifier(new Modifier(Modifier.StatModType.AdditivePercent, 3));
            });

        Talents.Add(_earthwell);
        Talents.Add(_debilitatingVortex);
        Talents.Add(_unwaveringSpirit);

        #endregion

        # region Row 4

        // Slipstream
        _slipstream = new Talent(
                id: "slipstream",
                name: "SlipStream",
                gridPos: "4.1")
            .WithOnActivate((unit) =>
            {
                // NYI
            });

        // Magic Ward
        _magicWard = new Talent(
                id: "magic-ward",
                name: "Magic Ward",
                gridPos: "4.2")
            .WithOnActivate((unit) =>
            {
                // NYI
            });

        // Spirited Advance
        _spiritedAdvance = new Talent(
                id: "spirited-advance",
                name: "Spirited Advance",
                gridPos: "4.3")
            .WithOnActivate((unit) =>
            {
                // NYI
            });

        Talents.Add(_slipstream);
        Talents.Add(_magicWard);
        Talents.Add(_spiritedAdvance);

        #endregion

        # region Row 5

        //Serene Resilience
        _sereneResilience = new Talent(
                id: "serene-resilience",
                name: "Serene Resilience",
                gridPos: "5.1")
            .WithOnActivate(unit =>
            {
                // NYI
            });

        // Earthbourne
        Modifier earthbourneCooldownMod = new Modifier(Modifier.StatModType.MultiplicativePercent, -50);
        _earthbourne = new Talent(
                id: "earthbourne",
                name: "Earthbourne",
                gridPos: "5.2")
            .WithOnActivate(unit =>
            {
                _shatterEarth.Cooldown.AddModifier(earthbourneCooldownMod);
                _shatterEarth.OnCast += (caster, spell, targets) =>
                {
                    _twinSoulsBulwark.UpdateCooldown(unit, _twinSoulsBulwark.Cooldown.GetValue() * 0.2);
                };
            });

        // Forbidden Technique
        // Ghetto implementation definitely needs rework.

        Spell forbiddenTechnique = new Spell("forbidden-technique", "Forbidden Technique", 0, 0);
        Aura _forbiddenTechnique = new Aura(
            id: "forbidden-technique",
            name: "Forbidden Technique",
            duration: 15,
            tickInterval: 0,
            onApply: (caster, target) =>
            {
                target.ForbiddenTechniqueAccumulator = 0;
            },
            onRemove: (caster, target) =>
            {
                double dam = target.ForbiddenTechniqueAccumulator * 0.15;
                DealAOEDamage(dam, dam, 999, forbiddenTechnique, 
                    includeCriticalStrike: false, 
                    includeExpertise: false, 
                    isFlatDamage: false);
            });
        _forbiddenTechniqueTalent = new Talent(
            id: "forbidden-technique",
            name: "Forbidden Technique",
            gridPos: "5.3")
            .WithOnActivate(unit =>
            {

                SpellBook.Add(forbiddenTechnique);

                _doublePalmStrike.OnCast += (caster, spell, targets) =>
                {
                    var target = unit.PrimaryTarget;
                    target?.ApplyDebuff(caster, target, _forbiddenTechnique);
                };
            });


        Talents.Add(_sereneResilience);
        Talents.Add(_earthbourne);
        Talents.Add(_forbiddenTechniqueTalent);


        #endregion

        #region Row 6

        // Peacefield
        _peacefield = new Talent(
                id: "peacefield",
                name: "Peacefield",
                gridPos: "6.1")
            .WithOnActivate(unit =>
            {
                // TODO
            });


        // Superior Bulwark
        _superiorBulwark = new Talent(
                id: "superior-bulwark",
                name: "Superior Bulwark",
                gridPos: "6.2")
            .WithOnActivate(unit =>
            {
                bulwarkHP = (int)(bulwarkHP * 1.2);
                _twinSoulsBulwarkAura.Duration = 25;
            });

        // Conclusive Strikes
        Modifier conclusiveStrikesDamageModifier = new Modifier(Modifier.StatModType.MultiplicativePercent, 10);
        _conclusiveStrikes = new Talent(
                id: "conclusive-strikes",
                name: "Conclusive Strikes",
                gridPos: "6.3")
            .WithOnActivate(unit =>
            {
                _earthfistBarrage.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);
                _lashingStormkick.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);
                _doublePalmStrike.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);
                _spiritedVortex.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);
                _risingEarth.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);
                _risingStorm.DamageModifiers.AddModifier(conclusiveStrikesDamageModifier);

            });

        Talents.Add(_peacefield);
        Talents.Add(_superiorBulwark);
        Talents.Add(_conclusiveStrikes);

        #endregion

        #region Row 7

        _neck = new Talent(
                id: "neck",
                name: "Necklace of Tectonic Impact",
                gridPos: "7.1")
            .WithOnActivate(unit =>
            {
                int earthfistBarrageTickCount = 0;
                _earthfistBarrage.WithOnCast((unit, spell, targets) =>
                {
                    earthfistBarrageTickCount = 0;
                })
                .WithOnTick((unit, spell, targets) =>
                {
                    earthfistBarrageTickCount++;
                    if (earthfistBarrageTickCount <= 5)
                    {
                        DealDamage(SimRandom.Next(207, 253), spell);
                    }
                    else
                    {

                        DealAOEDamage(1656, 2024, 1, spell);
                        if (HasBuff(_earthfall))
                        {
                            _earthfall.DecreaseStack();
                            _earthfall.DecreaseStack();
                            if (_earthfall.CurrentStacks == 1) _earthfall.IncreaseStack(); // If one refunds you get both back. I think... :shrug:
                        }
                        earthfistBarrageTickCount = 0;
                    }

                });
            });

        _back = new Talent(
                id: "back",
                name: "Shattering Obelisk Drape",
                gridPos: "7.2")
            .WithOnActivate(unit =>
            {

                bool die = true;
                int dur = (int)_twinSoulsBulwarkAura.Duration;
                int rate = dur / 3;
                if (die)
                {
                    dur = SimRandom.Next(5, dur);
                    rate = dur / 3;
                }
                else
                {
                    //NYI
                }

                _twinSoulsBulwarkAura.Duration = dur;
                _twinSoulsBulwarkAura.TickInterval = new Stat(rate);
                int tickCount = 0;
                _twinSoulsBulwarkAura.OnTick = (caster, target, aura) =>
                {
                    tickCount++;
                    if (tickCount == 1)
                    {
                        double dam = ((bulwarkHP * 0.35) * 0.25);
                        DealAOEDamage(dam, dam, 5, _twinSoulsBulwark, includeCriticalStrike: false, includeExpertise: false, isFlatDamage: true);
                    }
                    if (tickCount == 2)
                    {
                        double dam = (bulwarkHP * 0.7) * 0.25;
                        DealAOEDamage(dam, dam, 5, _twinSoulsBulwark, includeCriticalStrike: false, includeExpertise: false, isFlatDamage: true);
                    }
                };
                _twinSoulsBulwarkAura.OnRemove = (caster, target) =>
                {
                    double dam = bulwarkHP * 0.25;
                    tickCount = 0;
                    DealAOEDamage(dam, dam, 5, _twinSoulsBulwark, includeCriticalStrike: false, includeExpertise: false, isFlatDamage: true);
                };

            });

        _fatedStrike = new Talent(
                id: "fated-strike",
                name: "Fated Strike",
                gridPos: "7.3")
            .WithOnActivate(unit =>
            {
                // NYI
            });

        Talents.Add(_neck);
        Talents.Add(_back);
        Talents.Add(_fatedStrike);

        #endregion
    }
}