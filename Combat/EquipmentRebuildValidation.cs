using System;
using DungeonAscendant.Bosses;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>Deterministic regression checks for input priority and migration.</summary>
public static class EquipmentRebuildValidation
{
    public static void ValidateOrThrow()
    {
        var single = new CombatInputBuffer();
        Require(single.Update(0f, true, false, false) == null,
            "Single input resolved before its combo window.");
        Require(single.Update(.31f, false, false, false) ==
            WeaponTechniqueInput.Skill1,
            "J did not resolve as Skill 1.");

        var dual = new CombatInputBuffer();
        dual.Update(0f, true, false, false);
        dual.Update(.10f, false, true, false);
        Require(dual.Update(.21f, false, false, false) ==
            WeaponTechniqueInput.Skill1Skill2,
            "J+K did not win over its single inputs.");

        var triple = new CombatInputBuffer();
        triple.Update(0f, true, false, false);
        triple.Update(.10f, false, true, false);
        Require(triple.Update(.10f, false, false, true) ==
            WeaponTechniqueInput.Ultimate,
            "J+K+L did not resolve immediately at highest priority.");
        Require(!triple.HasPendingInput,
            "Ultimate input was not consumed atomically.");

        Require(EquipmentCatalog.ResolveSavedWeaponId("iron-great-sword") ==
            EquipmentCatalog.IronVanguardGreatsword,
            "Great Sword migration fallback failed.");
        Require(EquipmentCatalog.ResolveSavedWeaponId("hunter-spear") ==
            EquipmentCatalog.HunterBow,
            "Spear migration fallback failed.");
        Require(EquipmentCatalog.ResolveSavedWeaponId("twin-daggers") ==
            EquipmentCatalog.DuelistDualSwords,
            "Dual Daggers migration fallback failed.");

        var resources = new WeaponResourceState();
        resources.SetFamily(WeaponFamily.DualSwords);
        resources.AddDuelistMomentum(75f);
        resources.OnOwnerDamaged();
        Require(MathF.Abs(resources.DuelistMomentum - 47f) < .01f,
            "Duelist damage loss must be meaningful without clearing Momentum.");
        resources.SetFamily(WeaponFamily.SpikedMace);
        resources.AddBreakerInertia(140f);
        Require(resources.BreakerInertia == BreakerTuning.MaximumInertia &&
            resources.SpendAllBreakerInertia() ==
                BreakerTuning.MaximumInertia,
            "Breaker Inertia must cap at 100 and be consumable.");

        ValidateEveryTechniqueStarts();
        ValidateKnightCombat();
        ValidateDuelistCombat();
        ValidateGreatswordCombat();
        ValidateRangerCombat();
        ValidateRaiderCombat();
        ValidateSpellbladeCombat();
        ValidateBreakerCombat();
        ValidateReckonerCombat();
        ValidateSovereignCombat();
        ValidateCommittedActionFlow();
    }

    private static void ValidateSovereignCombat()
    {
        WeaponDefinition weapon =
            EquipmentCatalog.ForbiddenSovereignAbyssRing;
        WeaponCombatProfile profile = weapon.CombatProfile;
        Require(weapon.Family == WeaponFamily.AbyssRing &&
                profile.DashInput == WeaponTechniqueInput.Skill1,
            "The Forbidden Sovereign must retain the Abyss Ring identity and Shift+J mapping.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id ==
                    "sovereign-martial-arts" &&
                profile.Get(WeaponTechniqueInput.Skill2).Id ==
                    "omnipresent-step" &&
                profile.Get(WeaponTechniqueInput.Skill3).Id ==
                    "throne-manifestation" &&
                profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                    "paradox-impact" &&
                profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                    "infinite-rift-theatre" &&
                profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                    "event-horizon-palm" &&
                profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                    "golden-sovereign-cataclysm",
            "Abyss Ring J/K/L chord mapping is incomplete.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).StageCount == 8 &&
                profile.Get(WeaponTechniqueInput.Ultimate).StageCount == 19,
            "Abyss Ring bounded combo or Cataclysm stages changed unexpectedly.");
    }

    private static void ValidateCommittedActionFlow()
    {
        WeaponDefinition[] weapons =
        {
            EquipmentCatalog.KnightLongSword,
            EquipmentCatalog.DuelistDualSwords,
            EquipmentCatalog.IronVanguardGreatsword,
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.ForbiddenSovereignAbyssRing
        };
        WeaponTechniqueInput[] actions =
        {
            WeaponTechniqueInput.Skill1,
            WeaponTechniqueInput.Skill2,
            WeaponTechniqueInput.Skill3
        };

        foreach (WeaponDefinition weapon in weapons)
        {
            foreach (WeaponTechniqueInput action in actions)
            {
                var combat = new PlayerCombat();
                combat.ApplyLoadout(
                    weapon,
                    EquipmentCatalog.CreateStartingArmor());
                if (weapon.Family == WeaponFamily.ArcaneWarStaff)
                    combat.SetSpellbladeRuneCount(2);
                StartInput(combat, weapon, action);
                Require(combat.HasCommittedAction,
                    $"{weapon.Name} {action} did not enter a committed action.");

                AttackDefinition attack = combat.CurrentAttack;
                WeaponTechnique technique = combat.CurrentTechnique;
                CombatState state = combat.State;
                int attackId = combat.AttackId;
                combat.OnDamaged(isAlive: true, damage: 10);
                Require(combat.State == state &&
                    ReferenceEquals(combat.CurrentAttack, attack) &&
                    ReferenceEquals(combat.CurrentTechnique, technique) &&
                    combat.AttackId == attackId,
                    $"{weapon.Name} {action} was cancelled by normal damage.");

                combat.Update(
                    Frame(.016f),
                    new CombatInput(
                        false, false, false, false, false, false, -1f,
                        movementCancelPressed: true),
                    isGrounded: true,
                    FacingDirection.Right);
                if (weapon.Family == WeaponFamily.GreatSword &&
                    action == WeaponTechniqueInput.Skill3)
                {
                    Require(combat.IsGreatswordWhirlStopping &&
                        combat.CurrentAttack?.DamageMultiplier == 0f,
                        "Whirling Judgment did not enter its clean spin-stop.");
                    Advance(combat, 10);
                }
                else if (weapon.Family == WeaponFamily.GreatSword)
                {
                    Require(combat.HasCommittedAction &&
                        ReferenceEquals(combat.CurrentTechnique, technique),
                        $"{weapon.Name} {action} lost its committed recovery to movement.");
                    continue;
                }
                else
                {
                    Require(combat.State == CombatState.Moving &&
                        !combat.HasCommittedAction &&
                        combat.CurrentAttack == null &&
                        combat.CurrentTechnique == null &&
                        !combat.IsAttackActive &&
                        combat.AttackId == attackId,
                        $"{weapon.Name} {action} did not cancel cleanly into movement.");
                }
                combat.Update(
                    Frame(.05f),
                    EmptyInput(),
                    isGrounded: true,
                    FacingDirection.Left);
                Require(combat.CurrentAttack == null &&
                    combat.CurrentTechnique == null,
                    $"{weapon.Name} {action} resumed after movement cancel.");
            }

            var hardInterrupt = new PlayerCombat();
            hardInterrupt.ApplyLoadout(
                weapon,
                EquipmentCatalog.CreateStartingArmor());
            StartInput(
                hardInterrupt,
                weapon,
                WeaponTechniqueInput.Skill1);
            hardInterrupt.OnDamaged(
                isAlive: true,
                damage: 30,
                hardInterrupt: true);
            Require(hardInterrupt.State == CombatState.Hurt &&
                !hardInterrupt.HasCommittedAction,
                $"{weapon.Name} ignored a hard interrupt.");

            var death = new PlayerCombat();
            death.ApplyLoadout(
                weapon,
                EquipmentCatalog.CreateStartingArmor());
            StartInput(death, weapon, WeaponTechniqueInput.Skill1);
            death.OnDamaged(isAlive: false, damage: int.MaxValue);
            Require(death.State == CombatState.Dead &&
                !death.HasCommittedAction,
                $"{weapon.Name} attack overrode death.");
        }
    }

    private static void ValidateReckonerCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.ReckonerChainFlail
            .CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Reckoner dash technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id == "chain-lash" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id ==
                "orbiting-maelstrom" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "chain-harpoon" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id ==
                "reapers-passage" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "crescent-requiem" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "serpents-fang" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "vortex-snare" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                "chains-of-judgment",
            "Reckoner J/K/L chord mapping is incomplete.");

        WeaponTechnique lash = profile.Get(WeaponTechniqueInput.Skill1);
        WeaponTechnique orbit = profile.Get(WeaponTechniqueInput.Skill2);
        WeaponTechnique harpoon = profile.Get(WeaponTechniqueInput.Skill3);
        WeaponTechnique vortex = profile.Get(
            WeaponTechniqueInput.Skill2Skill3);
        WeaponTechnique ultimate = profile.Get(WeaponTechniqueInput.Ultimate);
        Require(lash.StageCount == 3 && lash.RequiresRepeatedInput &&
            lash.AdditionalStageStaminaCost == 6f,
            "Chain Lash must remain a forgiving paid three-input rhythm chain.");
        Require(orbit.StageCount == 2 &&
            orbit.StaminaCost == ReckonerTuning.OrbitActivationStamina,
            "Orbiting Maelstrom must have activation and release stages.");
        Require(harpoon.StageCount == 2 &&
            harpoon.GetStage(0).Range == ReckonerTuning.HarpoonMaximumDistance,
            "Chain Harpoon must have capped outbound and return stages.");
        Require(vortex.StageCount == 7 &&
            vortex.GetStage(6).HitboxShape == AttackHitboxShape.Thrust,
            "Vortex Snare must preserve launch, bounded passes, and retract.");
        Require(ultimate.RequiresFullStamina && ultimate.StageCount == 8 &&
            ultimate.GetStage(6).PoiseDamage >= 80f &&
            ultimate.GetStage(7).PoiseDamage >= 120f,
            "Chains of Judgment must preserve convergence and final judgment.");

        var resource = new WeaponResourceState();
        resource.SetFamily(WeaponFamily.ChainFlail);
        resource.AddChainMomentum(500f);
        Require(resource.ChainMomentum == ReckonerTuning.MomentumMaximum,
            "Chain Momentum exceeded its 100 point cap.");
        resource.OnOwnerDamaged();
        Require(resource.ChainMomentum ==
                ReckonerTuning.MomentumMaximum -
                    ReckonerTuning.HeavyDamageMomentumLoss,
            "Heavy damage did not remove the bounded Chain Momentum amount.");
        resource.Update(ReckonerTuning.MomentumDecayDelaySeconds - .05f);
        float beforeDecay = resource.ChainMomentum;
        resource.Update(.10f);
        Require(resource.ChainMomentum < beforeDecay,
            "Chain Momentum did not decay after its inactivity grace period.");
        Require(resource.SpendAllChainMomentum() > 0f &&
            resource.ChainMomentum == 0f,
            "Chain Momentum could not be consumed independently of Stamina.");

        AttackDefinition firstLash = lash.GetStage(0);
        Vector2 waitingHead = ChainFlailMotion.GetHeadOffset(
            lash.Effect, 0, firstLash, firstLash.StartupTime * .5f,
            firstLash.Range, 0f, 0f, 1);
        Vector2 caughtHead = ChainFlailMotion.GetHeadOffset(
            lash.Effect, 0, firstLash,
            firstLash.StartupTime + firstLash.ActiveTime * .8f,
            firstLash.Range, 0f, 0f, 1);
        Require(Vector2.Distance(waitingHead, caughtHead) > 45f,
            "Chain head no longer visibly lags then catches the handle.");

        AttackDefinition serpent = profile.Get(
            WeaponTechniqueInput.Skill1Skill3).GetStage(0);
        Vector2 leftCurve = ChainFlailMotion.GetHeadOffset(
            WeaponTechniqueEffect.ReckonerSerpentsFang,
            0, serpent, serpent.StartupTime + serpent.ActiveTime * .5f,
            serpent.Range, 0f, 0f, -1);
        Vector2 rightCurve = ChainFlailMotion.GetHeadOffset(
            WeaponTechniqueEffect.ReckonerSerpentsFang,
            0, serpent, serpent.StartupTime + serpent.ActiveTime * .5f,
            serpent.Range, 0f, 0f, 1);
        Require(MathF.Sign(leftCurve.Y + 31f) !=
                MathF.Sign(rightCurve.Y + 31f),
            "Serpent's Fang A/D paths must curve to opposite sides.");

        var heldOrbit = new PlayerCombat();
        heldOrbit.ApplyLoadout(
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.CreateStartingArmor());
        heldOrbit.Update(
            Frame(.016f),
            ReckonerOrbitInput(pressed: true, held: true),
            true,
            FacingDirection.Right);
        for (int frame = 0; frame < 55; frame++)
        {
            heldOrbit.Update(
                Frame(.05f),
                ReckonerOrbitInput(held: true),
                true,
                FacingDirection.Right);
        }
        Require(heldOrbit.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ReckonerOrbitingMaelstrom &&
            heldOrbit.ReckonerOrbitStage == 3 &&
            heldOrbit.Resources.ChainMomentum > 0f &&
            heldOrbit.Stamina.Current <
                heldOrbit.Stamina.Maximum - orbit.StaminaCost,
            "Held Orbit did not stage up, build Momentum, and drain Stamina.");
        heldOrbit.Update(
            Frame(.05f),
            ReckonerOrbitInput(held: false, released: true),
            true,
            FacingDirection.Right);
        Require(heldOrbit.TechniqueStageIndex == 1,
            "Orbit release did not enter its controlled recovery arc.");

        var lowHarpoon = new PlayerCombat();
        lowHarpoon.ApplyLoadout(
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.CreateStartingArmor());
        StartInput(lowHarpoon, EquipmentCatalog.ReckonerChainFlail,
            WeaponTechniqueInput.Skill3);
        Require(lowHarpoon.RegisterReckonerHarpoonOutboundTarget(false) &&
            lowHarpoon.TechniqueStageIndex == 1,
            "Low-Momentum Harpoon did not return after its first target.");

        var highUltimate = new PlayerCombat();
        highUltimate.ApplyLoadout(
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.CreateStartingArmor());
        highUltimate.Resources.AddChainMomentum(85f);
        StartInput(highUltimate, EquipmentCatalog.ReckonerChainFlail,
            WeaponTechniqueInput.Ultimate);
        Require(highUltimate.Stamina.Current == 0f &&
            highUltimate.Resources.ChainMomentum == 0f &&
            MathF.Abs(highUltimate.ReckonerMomentumSnapshot - 85f) < .01f &&
            highUltimate.EffectiveTechniqueStageCount == 8,
            "High-Momentum Judgment did not snapshot/consume both resources with capped passes.");

        var lowUltimate = new PlayerCombat();
        lowUltimate.ApplyLoadout(
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.CreateStartingArmor());
        StartInput(lowUltimate, EquipmentCatalog.ReckonerChainFlail,
            WeaponTechniqueInput.Ultimate);
        Require(lowUltimate.EffectiveTechniqueStageCount == 6,
            "Low-Momentum Judgment must remain useful with three circle passes.");
    }

    private static void ValidateSpellbladeCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.ArcaneWarStaff
            .CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Ancient Staff blink technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id ==
                "arcane-bolt-chain" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "aegis-ward" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "thunder-spirit" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id ==
                "blink-cast" &&
            profile.Get(WeaponTechniqueInput.DashSkill2).Id ==
                "fourth-veil" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "rune-prison" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "elder-invocation" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "spirit-bastion" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                "heavenfall-thunder-gate",
            "Ancient Staff J/K/L chord mapping is incomplete.");

        WeaponTechnique bolts = profile.Get(WeaponTechniqueInput.Skill1);
        WeaponTechnique prison = profile.Get(
            WeaponTechniqueInput.Skill1Skill2);
        WeaponTechnique bastion = profile.Get(
            WeaponTechniqueInput.Skill2Skill3);
        WeaponTechnique elder = profile.Get(
            WeaponTechniqueInput.Skill1Skill3);
        WeaponTechnique ultimate = profile.Get(WeaponTechniqueInput.Ultimate);
        Require(bolts.StageCount == 5 && bolts.RequiresRepeatedInput &&
            bolts.GetStage(0).DamageMultiplier == .60f &&
            bolts.GetStage(4).DamageMultiplier == 1.25f,
            "Arcane Bolt Chain must preserve five escalating ranged attacks.");
        Require(prison.StageCount == 2 && bastion.StageCount == 5 &&
            elder.StageCount == 4,
            "Ancient Staff dual-skill sequences have incorrect stage counts.");
        Require(ultimate.RequiresFullStamina && ultimate.StageCount == 10 &&
            ultimate.GetStage(1).DamageMultiplier == .60f &&
            ultimate.GetStage(8).DamageMultiplier == 1.80f,
            "Heavenfall must preserve setup, barrage, and final judgment.");

        var sigils = new WeaponResourceState();
        sigils.SetFamily(WeaponFamily.ArcaneWarStaff);
        sigils.AddElderSigil(20);
        Require(sigils.ElderSigils == AncientStaffTuning.MaximumElderSigils,
            "Elder Sigils exceeded their bounded three-stack cap.");
        Require(AncientStaffTuning.ChordWindowSeconds >= .10f &&
                AncientStaffTuning.ChordWindowSeconds <= .15f &&
                AncientStaffTuning.FourthVeilDurationSeconds == 12f,
            "Ancient Staff chord or Fourth Veil timing changed unexpectedly.");

        var caster = new PlayerCombat();
        caster.ApplyLoadout(
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.CreateStartingArmor());
        caster.SetAncientStaffUltimateTargetAvailable(true);
        StartInput(
            caster,
            EquipmentCatalog.ArcaneWarStaff,
            WeaponTechniqueInput.Ultimate);
        Require(caster.CurrentTechnique?.Id == "heavenfall-thunder-gate" &&
            caster.Stamina.Current == 0f,
            "Heavenfall must validate a target then consume full stamina once.");
    }

    private static void ValidateBreakerCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.BreakerSpikedMace
            .CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Breaker dash technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id == "iron-crush" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "pendulum-swing" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "earthbreaker" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id ==
                "battering-rush" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "titans-backhand" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "anvil-fall" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "cataclysm-wheel" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id == "worldbreaker",
            "Breaker J/K/L chord mapping is incomplete.");

        WeaponTechnique crush = profile.Get(WeaponTechniqueInput.Skill1);
        WeaponTechnique pendulum = profile.Get(WeaponTechniqueInput.Skill2);
        WeaponTechnique worldbreaker = profile.Get(
            WeaponTechniqueInput.Ultimate);
        Require(crush.StageCount == 3 && crush.RequiresRepeatedInput &&
            crush.AdditionalStageStaminaCost == 7f &&
            crush.GetStage(2).PoiseDamage > crush.GetStage(1).PoiseDamage,
            "Iron Crush must be a paid three-hit escalating Poise chain.");
        Require(pendulum.StageCount == 1 &&
            pendulum.Effect == WeaponTechniqueEffect.BreakerPendulumSwing,
            "Pendulum must release one bounded heavy sweep.");
        Require(worldbreaker.RequiresFullStamina &&
            worldbreaker.StageCount == 1 &&
            worldbreaker.GetStage(0).StartupTime >= 1.5f &&
            worldbreaker.GetStage(0).RecoveryTime >= 1f,
            "Worldbreaker must remain one high-commitment main hit.");

        var inertia = new WeaponResourceState();
        inertia.SetFamily(WeaponFamily.SpikedMace);
        inertia.AddBreakerInertia(75f);
        inertia.Update(BreakerTuning.InertiaDecayDelaySeconds + .05f);
        inertia.Update(1f);
        Require(inertia.BreakerInertia < 75f &&
            inertia.BreakerInertia > 0f,
            "Breaker Inertia did not decay gradually after inactivity.");
        inertia.OnOwnerDamaged();
        Require(inertia.BreakerInertia <=
                75f - BreakerTuning.InterruptedAttackLoss,
            "Interrupted Breaker attacks did not lose meaningful Inertia.");

        var pendulumCombat = new PlayerCombat();
        pendulumCombat.ApplyLoadout(
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.CreateStartingArmor());
        pendulumCombat.Update(
            Frame(.016f),
            new CombatInput(
                false, true, false, false, false, false, 0f,
                skill2Held: true),
            true,
            FacingDirection.Right);
        AdvanceHeldSkill2(pendulumCombat, 7);
        Require(pendulumCombat.IsPendulumCharging &&
            pendulumCombat.BreakerPendulumStage ==
                BreakerPendulumStage.StageOne,
            "Pendulum failed to enter its first held stage.");
        AdvanceHeldSkill2(pendulumCombat, 8);
        Require(pendulumCombat.BreakerPendulumStage ==
                BreakerPendulumStage.StageTwo,
            "Pendulum failed to enter its second held stage.");
        AdvanceHeldSkill2(pendulumCombat, 10);
        Require(pendulumCombat.BreakerPendulumStage ==
                BreakerPendulumStage.StageThree,
            "Pendulum failed to enter its third held stage.");
        AdvanceHeldSkill2(pendulumCombat, 13);
        Require(!pendulumCombat.IsPendulumCharging &&
            pendulumCombat.CurrentTechnique?.Id == "pendulum-swing",
            "Pendulum exceeded its maximum hold instead of auto-releasing.");

        var earthbreaker = new PlayerCombat();
        earthbreaker.ApplyLoadout(
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.CreateStartingArmor());
        earthbreaker.Resources.AddBreakerInertia(100f);
        StartInput(
            earthbreaker,
            EquipmentCatalog.BreakerSpikedMace,
            WeaponTechniqueInput.Skill3);
        Require(earthbreaker.CurrentTechnique?.Id == "earthbreaker" &&
            earthbreaker.BreakerInertiaSnapshot == 100f &&
            earthbreaker.Resources.BreakerInertia ==
                100f - BreakerTuning.EarthbreakerConsumption,
            "Earthbreaker did not snapshot and consume bounded Inertia.");

        var cataclysm = new PlayerCombat();
        cataclysm.ApplyLoadout(
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.CreateStartingArmor());
        cataclysm.Resources.AddBreakerInertia(100f);
        StartInput(
            cataclysm,
            EquipmentCatalog.BreakerSpikedMace,
            WeaponTechniqueInput.Skill2Skill3);
        Require(cataclysm.CurrentTechnique?.Id == "cataclysm-wheel" &&
            MathF.Abs(cataclysm.Resources.BreakerInertia - 28f) < .01f,
            "Cataclysm Wheel did not consume 72% of current Inertia.");

        var ultimate = new PlayerCombat();
        ultimate.ApplyLoadout(
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.CreateStartingArmor());
        ultimate.Resources.AddBreakerInertia(100f);
        StartInput(
            ultimate,
            EquipmentCatalog.BreakerSpikedMace,
            WeaponTechniqueInput.Ultimate);
        Require(ultimate.CurrentTechnique?.Id == "worldbreaker" &&
            ultimate.EffectiveTechniqueStageCount == 1 &&
            ultimate.BreakerInertiaSnapshot == 100f &&
            ultimate.Resources.BreakerInertia == 0f &&
            ultimate.Stamina.Current == 0f,
            "Worldbreaker must snapshot Inertia and consume all setup/stamina.");
    }

    private static void AdvanceHeldSkill2(PlayerCombat combat, int frames)
    {
        for (int index = 0; index < frames; index++)
        {
            combat.Update(
                Frame(.05f),
                new CombatInput(
                    false, false, false, false, false, false, 0f,
                    skill2Held: true),
                true,
                FacingDirection.Right);
        }
    }

    private static void ValidateRaiderCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.RaiderWarAxe
            .CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Raider dash technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id ==
                "savage-cleave" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "hooking-axe" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "buckler-ram" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id ==
                "ravagers-rush" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "executioners-grip" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "skullbreaker" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "crowd-crusher" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                "warbringers-dominion",
            "Raider J/K/L chord mapping is incomplete.");

        WeaponTechnique savage = profile.Get(WeaponTechniqueInput.Skill1);
        WeaponTechnique executioner = profile.Get(
            WeaponTechniqueInput.Skill1Skill2);
        WeaponTechnique skullbreaker = profile.Get(
            WeaponTechniqueInput.Skill1Skill3);
        WeaponTechnique crowdCrusher = profile.Get(
            WeaponTechniqueInput.Skill2Skill3);
        WeaponTechnique dominion = profile.Get(
            WeaponTechniqueInput.Ultimate);
        Require(savage.StageCount == 3 && savage.RequiresRepeatedInput &&
            savage.AdditionalStageStaminaCost == 6f &&
            savage.GetStage(2).PoiseDamage > savage.GetStage(1).PoiseDamage,
            "Savage Cleave must be a paid three-input chain with a high-Poise finisher.");
        Require(executioner.StageCount == 3 &&
            executioner.Effect ==
                WeaponTechniqueEffect.RaiderExecutionersGrip,
            "Executioner's Grip must remain one authored strike-hook-reverse sequence.");
        Require(skullbreaker.StageCount == 2 &&
            skullbreaker.GetStage(0).HitboxShape == AttackHitboxShape.Thrust &&
            skullbreaker.GetStage(1).HitboxShape == AttackHitboxShape.Chop,
            "Skullbreaker must open with buckler impact before its axe finisher.");
        Require(crowdCrusher.StageCount == 3 &&
            crowdCrusher.Effect == WeaponTechniqueEffect.RaiderCrowdCrusher,
            "Crowd Crusher must keep its hook-turn-throw sequence.");
        Require(dominion.RequiresFullStamina && dominion.StageCount == 4 &&
            dominion.GetStage(0).HitboxShape == AttackHitboxShape.Circular &&
            dominion.GetStage(3).HitboxShape == AttackHitboxShape.FrontalFan,
            "Warbringer's Dominion must contain war cry, gather, push, and sundering phases.");

        Require(RaiderTuning.ClassifyWeight(30f, false) ==
                EnemyWeightClass.Light &&
            RaiderTuning.ClassifyWeight(70f, false) ==
                EnemyWeightClass.Normal &&
            RaiderTuning.ClassifyWeight(130f, false) ==
                EnemyWeightClass.Heavy &&
            RaiderTuning.GetDisplacementMultiplier(
                EnemyWeightClass.Boss) == 0f,
            "Raider weight classification or boss displacement immunity failed.");

        var room = new DungeonRoom(
            1,
            new Rectangle(0, 0, 400, 240),
            RoomType.Start);
        var dungeon = new DungeonMap(
            new[] { room },
            Array.Empty<Rectangle>(),
            room.Bounds);
        var normalTarget = new Goblin(
            new Vector2(100f, 100f),
            roomId: room.Id);
        var heavyTarget = new CorruptedTreant(
            new Vector2(100f, 100f),
            1,
            false,
            room.Id,
            1);
        EnemyDisplacementResult normalMove = normalTarget.DisplaceHorizontally(
            1f,
            80f,
            dungeon);
        EnemyDisplacementResult heavyMove = heavyTarget.DisplaceHorizontally(
            1f,
            80f,
            dungeon);
        Require(normalMove.DistanceMoved > heavyMove.DistanceMoved * 2f,
            "Heavy targets must resist Raider displacement without becoming immovable.");

        var lightTarget = new BloodBat(
            new Vector2(250f, 100f),
            1,
            false,
            room.Id,
            1);
        lightTarget.PullTowardSafe(
            new Vector2(100f, 100f),
            140f,
            RaiderTuning.HookSafeDistance,
            dungeon);
        Require(MathF.Abs(lightTarget.Position.X - 100f) + .01f >=
                RaiderTuning.HookSafeDistance,
            "Light-target pull scaling crossed the Raider safe-contact distance.");

        var wallTarget = new Goblin(
            new Vector2(370f, 100f),
            roomId: room.Id);
        EnemyDisplacementResult wallMove = wallTarget.DisplaceHorizontally(
            1f,
            RaiderTuning.BucklerRamPushDistance,
            dungeon);
        float poiseBeforeWall = wallTarget.CurrentPoise;
        bool firstWallImpact = wallTarget.ApplyDisplacementImpact(
            RaiderTuning.WallCollisionPoise,
            RaiderTuning.WallCollisionDamage);
        bool repeatedWallImpact = wallTarget.ApplyDisplacementImpact(
            RaiderTuning.WallCollisionPoise,
            RaiderTuning.WallCollisionDamage);
        Require(wallMove.HitWall &&
            wallTarget.Bounds.Right <= dungeon.WorldBounds.Right &&
            firstWallImpact && !repeatedWallImpact &&
            wallTarget.CurrentPoise < poiseBeforeWall,
            "Wall displacement must clamp safely and rate-limit collision Stagger.");

        var savageCombat = new PlayerCombat();
        savageCombat.ApplyLoadout(
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.CreateStartingArmor());
        StartInput(
            savageCombat,
            EquipmentCatalog.RaiderWarAxe,
            WeaponTechniqueInput.Skill1);
        savageCombat.Update(
            Frame(.05f),
            EmptyInput(),
            true,
            FacingDirection.Right);
        savageCombat.Update(
            Frame(.05f),
            EmptyInput(),
            true,
            FacingDirection.Right);
        savageCombat.Update(
            Frame(.016f),
            Skill1Input(),
            true,
            FacingDirection.Right);
        AdvanceUntilStage(savageCombat, 1);
        savageCombat.Update(
            Frame(.05f),
            EmptyInput(),
            true,
            FacingDirection.Right);
        savageCombat.Update(
            Frame(.05f),
            EmptyInput(),
            true,
            FacingDirection.Right);
        savageCombat.Update(
            Frame(.016f),
            Skill1Input(),
            true,
            FacingDirection.Right);
        AdvanceUntilStage(savageCombat, 2);
        Require(savageCombat.Stamina.Current == 82f,
            "Savage Cleave must spend 6 stamina per committed stage.");

        var leftThrow = new PlayerCombat();
        leftThrow.ApplyLoadout(
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.CreateStartingArmor());
        leftThrow.Update(
            Frame(.016f),
            new CombatInput(
                false, true, true, false, false, false, -1f),
            true,
            FacingDirection.Right);
        for (int frame = 0; frame < 7 &&
            leftThrow.CurrentTechnique == null; frame++)
        {
            leftThrow.Update(
                Frame(.05f),
                new CombatInput(
                    false, false, false, false, false, false, -1f),
                true,
                FacingDirection.Right);
        }
        Require(leftThrow.CurrentTechnique?.Id == "crowd-crusher" &&
            leftThrow.RaiderControlDirection < 0f,
            "Crowd Crusher did not capture A as its throw direction.");

        var ultimate = new PlayerCombat();
        ultimate.ApplyLoadout(
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.CreateStartingArmor());
        StartInput(
            ultimate,
            EquipmentCatalog.RaiderWarAxe,
            WeaponTechniqueInput.Ultimate);
        Require(ultimate.CurrentTechnique?.Id ==
                "warbringers-dominion" &&
            ultimate.EffectiveTechniqueStageCount == 4 &&
            ultimate.Stamina.Current == 0f,
            "Warbringer's Dominion must consume full stamina and begin four phases.");
    }

    private static void ValidateRangerCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.HunterBow.CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Bow dash technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id == "quick-shot-combo" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "evasive-shot" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "phantom-barrage" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id == "backstep-shot" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "twin-fang" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "piercing-volley" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "phantom-retreat" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id == "phantom-hunt",
            "Phantom Archer J/K/L mapping is incomplete.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).StageCount == 6,
            "Mobile Shot must contain five inputs and staggered Twin Release.");
        Require(profile.Get(WeaponTechniqueInput.Ultimate).StageCount ==
                RangerTuning.PhantomHuntArrowCount + 3 &&
            profile.Get(WeaponTechniqueInput.Ultimate).RequiresFullStamina,
            "Phantom Hunt must use rise, twelve timed arrows, execution, and landing.");
        Require(WeaponTechniqueCatalog.RangerSkyfallShot.GetStage(0)
                .Projectile.Trajectory == ProjectileTrajectoryType.Ballistic,
            "Skyfall Shot must use a terrain-aware aerial projectile.");

        var resources = new WeaponResourceState();
        resources.SetFamily(WeaponFamily.HunterBow);
        resources.AddHunterFocus(500f);
        Require(resources.HunterFocus == 100f,
            "Bow Flow Focus must remain bounded.");
        resources.OnOwnerDamaged();
        Require(resources.HunterFocus == 76f,
            "Damage must reduce Flow Focus without affecting stamina activation.");

        var evasive = new PlayerCombat();
        evasive.ApplyLoadout(
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.CreateStartingArmor());
        evasive.Update(Frame(.016f), new CombatInput(
            false, true, false, false, false, false, 0f,
            skill2Held: true), true, FacingDirection.Right);
        Advance(evasive, 3);
        Require(evasive.CurrentTechnique?.Id == "evasive-shot" &&
            evasive.CurrentAttack != null,
            "Ground K did not resolve reliably into Evasive Shot.");

        var ultimate = new PlayerCombat();
        ultimate.ApplyLoadout(
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.CreateStartingArmor());
        Require(ultimate.Resources.HunterFocus == 0f &&
            ultimate.Resources.HunterUltimate == 0f,
            "Phantom Hunt validation must begin without a Bow resource meter.");
        ultimate.SetRangerUltimateTargetAvailable(true);
        ultimate.Update(Frame(.016f), new CombatInput(
            true, true, true, false, false, false, 1f,
            skill1Held: true, skill3Held: true, skill2Held: true),
            true, FacingDirection.Right);
        Require(ultimate.IsRangerUltimateCharging &&
            ultimate.Stamina.Current == 0f &&
            ultimate.Resources.HunterUltimate == 0f,
            "J+K+L must start without a Bow meter and consume full stamina once.");
        Advance(ultimate, 44);
        Require(ultimate.CurrentAttack != null &&
            !ultimate.IsRangerUltimateCharging,
            "Phantom Hunt did not leave the bounded cinematic intro.");

        var rejectedUltimate = new PlayerCombat();
        rejectedUltimate.ApplyLoadout(
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.CreateStartingArmor());
        rejectedUltimate.Update(Frame(.016f), new CombatInput(
            true, true, true, false, false, false, 1f),
            true, FacingDirection.Right);
        Require(rejectedUltimate.CurrentTechnique == null &&
            rejectedUltimate.Stamina.IsFull,
            "Targetless Phantom Hunt consumed stamina or entered cinematic state.");
    }

    private static void ValidateDuelistCombat()
    {
        WeaponCombatProfile profile = EquipmentCatalog.DuelistDualSwords
            .CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1,
            "Duelist dash technique must be Shift+J.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id == "twin-fang" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "phantom-step" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "blade-tempest" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "afterimage-execution" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "hundred-fangs" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "mirage-cyclone" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id == "final-waltz",
            "Duelist J/K/L chord mapping is incomplete.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).StageCount == 5,
            "Twin Fang must contain five authored hits.");

        var decay = new WeaponResourceState();
        decay.SetFamily(WeaponFamily.DualSwords);
        decay.AddDuelistMomentum(50f);
        decay.Update(DuelistTuning.DecayDelaySeconds - .1f);
        Require(MathF.Abs(decay.DuelistMomentum - 50f) < .01f,
            "Duelist Momentum decayed before its inactivity delay.");
        decay.Update(.2f);
        Require(decay.DuelistMomentum < 50f,
            "Duelist Momentum did not decay after inactivity.");
        decay.AddDuelistMomentum(500f);
        Require(decay.DuelistMomentum == DuelistTuning.MomentumMaximum,
            "Duelist Momentum exceeded its 100 point cap.");

        Require(GetDuelistStageCount(
                WeaponTechniqueInput.Skill3, 0f) == 4 &&
            GetDuelistStageCount(
                WeaponTechniqueInput.Skill3, 50f) == 5 &&
            GetDuelistStageCount(
                WeaponTechniqueInput.Skill3, 100f) == 6,
            "Blade Tempest must scale to 4/5/6 hits by Momentum tier.");
        Require(GetDuelistStageCount(
                WeaponTechniqueInput.Skill1Skill3, 0f) == 6 &&
            GetDuelistStageCount(
                WeaponTechniqueInput.Skill1Skill3, 50f) == 7 &&
            GetDuelistStageCount(
                WeaponTechniqueInput.Skill1Skill3, 100f) == 8,
            "Hundred Fangs must preserve its bounded 6/7/8 hit scaling.");

        ValidateFinalWaltzTier(0f, 6);
        ValidateFinalWaltzTier(50f, 8);
        ValidateFinalWaltzTier(100f, 10);

        var phantom = CreateDuelistCombat(20f);
        StartInput(
            phantom,
            EquipmentCatalog.DuelistDualSwords,
            WeaponTechniqueInput.Skill2);
        float beforePerfect = phantom.Resources.DuelistMomentum;
        Require(phantom.IsPhantomStepEvading,
            "Phantom Step did not expose its forgiving evade window.");
        phantom.RegisterPerfectPhantomStep();
        Require(phantom.Resources.DuelistMomentum > beforePerfect &&
            phantom.IsPerfectPhantomFeedbackActive,
            "Perfect Phantom Step did not award Momentum and feedback.");

        var reactivePhantom = CreateDuelistCombat(0f);
        reactivePhantom.Update(
            Frame(.016f),
            new CombatInput(
                false, true, false, false, false, false, 0f,
                perfectEvadeOpportunity: true),
            true,
            FacingDirection.Right);
        Require(reactivePhantom.CurrentTechnique?.Id == "phantom-step",
            "A threatened Duelist must begin Phantom Step without chord latency.");

        var priority = CreateDuelistCombat(0f);
        priority.Update(
            Frame(.016f),
            new CombatInput(
                true, true, false, false, false, false, 0f,
                perfectEvadeOpportunity: true),
            true,
            FacingDirection.Right);
        Advance(priority, 7);
        Require(priority.CurrentTechnique?.Id == "afterimage-execution",
            "Perfect-evade acceleration must not steal J+K priority.");
    }

    private static int GetDuelistStageCount(
        WeaponTechniqueInput input,
        float momentum)
    {
        PlayerCombat combat = CreateDuelistCombat(momentum);
        StartInput(combat, EquipmentCatalog.DuelistDualSwords, input);
        return combat.EffectiveTechniqueStageCount;
    }

    private static void ValidateFinalWaltzTier(
        float momentum,
        int expectedHits)
    {
        PlayerCombat combat = CreateDuelistCombat(momentum);
        StartInput(
            combat,
            EquipmentCatalog.DuelistDualSwords,
            WeaponTechniqueInput.Ultimate);
        Require(combat.CurrentTechnique?.Id == "final-waltz" &&
            combat.EffectiveTechniqueStageCount == expectedHits,
            $"Final Waltz at {momentum:0} Momentum must use {expectedHits} hits.");
        Require(combat.Stamina.Current == 0f &&
            combat.Resources.DuelistMomentum == 0f &&
            MathF.Abs(combat.UltimateMomentumSnapshot - momentum) < .01f,
            "Final Waltz must consume full Stamina and all snapshotted Momentum.");
    }

    private static PlayerCombat CreateDuelistCombat(float momentum)
    {
        var combat = new PlayerCombat();
        combat.ApplyLoadout(
            EquipmentCatalog.DuelistDualSwords,
            EquipmentCatalog.CreateStartingArmor());
        combat.Resources.AddDuelistMomentum(momentum);
        return combat;
    }

    private static void ValidateGreatswordCombat()
    {
        WeaponDefinition weapon = EquipmentCatalog.IronVanguardGreatsword;
        Require(EquipmentCatalog.CreateWeaponSample(2).WeaponDefinition == weapon &&
            weapon.Family == WeaponFamily.GreatSword &&
            weapon.CombatProfile == WeaponTechniqueCatalog.IronVanguard &&
            weapon.MoveSet.ComboLength == 4,
            "Official Weapon 3 is not the Iron Sovereign Greatsword.");

        WeaponCombatProfile profile = weapon.CombatProfile;
        Require(profile.DashInput == WeaponTechniqueInput.Skill1 &&
            profile.Get(WeaponTechniqueInput.Skill1).Id == "iron-sweep" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "iron-resolve" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id ==
                "whirling-judgment" &&
            profile.Get(WeaponTechniqueInput.DashSkill).Id ==
                "vanguard-assault" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "titans-reckoning" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "iron-dominion" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "kings-rupture" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                "final-verdict",
            "Iron Sovereign J/K/L mapping is incomplete.");

        WeaponTechnique combo = profile.Get(
            WeaponTechniqueInput.Skill1);
        float[] expectedDamage = { 1.10f, 1.20f, 1.35f, 1.80f };
        float[] expectedPoise = { 34f, 46f, 72f, 108f };
        Require(combo.StageCount == 4 && combo.RequiresRepeatedInput,
            "Sovereign Sword Combo must contain four deliberate inputs.");
        for (int stage = 0; stage < combo.StageCount; stage++)
        {
            Require(MathF.Abs(combo.GetStage(stage).DamageMultiplier -
                    expectedDamage[stage]) < .001f &&
                MathF.Abs(combo.GetStage(stage).PoiseDamage -
                    expectedPoise[stage]) < .001f,
                $"Greatsword J{stage + 1} damage/Poise tuning regressed.");
        }

        var deflect = CreateGreatswordCombat();
        StartGreatswordGuard(deflect);
        Require(deflect.ResolveIncomingAttack(
                FrontContact(24), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.PerfectGuard,
            "Steel Deflect did not accept an allowed frontal melee contact.");
        var guard = CreateGreatswordCombat();
        StartGreatswordGuard(guard);
        for (int frame = 0; frame < 5; frame++)
            HoldGreatswordGuard(guard);
        Require(guard.ResolveIncomingAttack(
                FrontContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.BladeGuarded,
            "Iron Resolve did not become a partial blade guard.");
        Require(guard.ResolveIncomingAttack(
                RearContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.Damaged,
            "Iron Resolve incorrectly protected the rear.");
        Require(guard.ResolveIncomingAttack(
                FrontContact(20), Vector2.Zero, FacingDirection.Right,
                isProjectile: true) == AttackResolution.BladeGuarded,
            "Iron Resolve projectile contact incorrectly triggered Deflect.");
        Require(guard.ResolveIncomingAttack(
                new AttackContact(20, new Vector2(100f, 0f), true, true,
                    new Rectangle(8, -40, 48, 80)),
                Vector2.Zero,
                FacingDirection.Right) == AttackResolution.Damaged,
            "Iron Resolve blocked an unblockable contact.");

        WeaponTechnique whirlData = profile.Get(
            WeaponTechniqueInput.Skill3);
        float whirlDamage = 0f;
        for (int stage = 0; stage < whirlData.StageCount; stage++)
            whirlDamage += whirlData.GetStage(stage).DamageMultiplier;
        Require(whirlData.StageCount == 7 &&
            MathF.Abs(whirlDamage - 2.85f) < .001f,
            "Whirling Judgment must contain six ticks and one 75% sweep.");

        WeaponTechnique dominion = profile.Get(
            WeaponTechniqueInput.Skill2Skill3);
        float dominionDamage = 0f;
        for (int stage = 0; stage < dominion.StageCount; stage++)
            dominionDamage += dominion.GetStage(stage).DamageMultiplier;
        Require(dominion.StageCount == 4 &&
            MathF.Abs(dominionDamage - 3.05f) < .001f,
            "Iron Dominion damage events must remain bounded to 305%.");

        WeaponTechnique rupture = profile.Get(
            WeaponTechniqueInput.Skill1Skill3);
        float ruptureBaseDamage = 0f;
        for (int stage = 0; stage < rupture.StageCount; stage++)
            ruptureBaseDamage += rupture.GetStage(stage).DamageMultiplier;
        Require(rupture.StageCount == 3 &&
            MathF.Abs(ruptureBaseDamage - 2.20f) < .001f,
            "King's Rupture level-one total must remain 220%.");

        WeaponTechnique finalVerdict = profile.Get(
            WeaponTechniqueInput.Ultimate);
        float verdictDamage = 0f;
        for (int stage = 0; stage < finalVerdict.StageCount; stage++)
            verdictDamage += finalVerdict.GetStage(stage).DamageMultiplier;
        Require(finalVerdict.StageCount == 6 &&
            finalVerdict.RequiresFullStamina &&
            finalVerdict.ResourceCost == 0 &&
            MathF.Abs(verdictDamage - 5.20f) < .001f,
            "Final Verdict must remain a full-stamina 100/160/260 sequence.");

        var pair = CreateGreatswordCombat();
        pair.Update(
            Frame(.016f),
            new CombatInput(true, true, false, false, false, false, 0f,
                skill1Held: true, skill2Held: true),
            true,
            FacingDirection.Right);
        Require(pair.CurrentTechnique == null,
            "JK components fired before the Greatsword chord window resolved.");
        for (int frame = 0; frame < 4 && pair.CurrentTechnique == null; frame++)
        {
            pair.Update(
                Frame(.05f),
                new CombatInput(false, false, false, false, false, false, 0f,
                    skill1Held: true, skill2Held: true),
                true,
                FacingDirection.Right);
        }
        Require(pair.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordTitanReckoning &&
            pair.State == CombatState.ChargingHeavy,
            "JK did not atomically enter Titan's Reckoning charge.");

        var falling = CreateGreatswordCombat();
        falling.Update(
            Frame(.016f),
            new CombatInput(false, true, false, false, false, false, 0f,
                skill2Held: true),
            false,
            FacingDirection.Right);
        for (int frame = 0; frame < 4 && falling.CurrentTechnique == null; frame++)
            falling.Update(Frame(.05f), EmptyInput(), false,
                FacingDirection.Right);
        Require(falling.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion,
            "Air K did not select Falling Bastion.");

        var rejectedVerdict = CreateGreatswordCombat();
        StartInput(rejectedVerdict, weapon, WeaponTechniqueInput.Ultimate);
        Require(rejectedVerdict.CurrentTechnique == null &&
            rejectedVerdict.Stamina.IsFull,
            "Targetless Final Verdict consumed stamina or entered cinematic state.");

        var verdict = CreateGreatswordCombat();
        verdict.SetGreatswordUltimateTargetAvailable(true);
        StartInput(verdict, weapon, WeaponTechniqueInput.Ultimate);
        Require(verdict.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFinalVerdict &&
            verdict.State == CombatState.ChargingHeavy &&
            verdict.Stamina.Current == 0f &&
            profile.Get(WeaponTechniqueInput.Ultimate).ResourceCost == 0,
            "Final Verdict must use full stamina once without another meter.");
    }

    private static PlayerCombat CreateGreatswordCombat()
    {
        var combat = new PlayerCombat();
        combat.ApplyLoadout(
            EquipmentCatalog.IronVanguardGreatsword,
            EquipmentCatalog.CreateStartingArmor());
        return combat;
    }

    private static void StartGreatswordGuard(PlayerCombat combat)
    {
        combat.Update(
            Frame(.016f),
            new CombatInput(false, true, false, false, false, false, 0f,
                skill2Held: true),
            true,
            FacingDirection.Right);
        for (int frame = 0; frame < 4 && combat.CurrentTechnique == null; frame++)
            HoldGreatswordGuard(combat);
    }

    private static void HoldGreatswordGuard(PlayerCombat combat)
    {
        combat.Update(
            Frame(.05f),
            new CombatInput(false, false, false, false, false, false, 0f,
                skill2Held: true),
            true,
            FacingDirection.Right);
    }

    private static void ValidateKnightCombat()
    {
        EquipmentItem armor = EquipmentCatalog.CreateStartingArmor();

        var chain = new PlayerCombat();
        chain.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(chain, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill1);
        chain.Update(Frame(.04f), EmptyInput(), true, FacingDirection.Right);
        chain.Update(Frame(.04f), Skill1Input(), true, FacingDirection.Right);
        AdvanceUntilStage(chain, 1);
        chain.Update(Frame(.04f), EmptyInput(), true, FacingDirection.Right);
        chain.Update(Frame(.04f), Skill1Input(), true, FacingDirection.Right);
        AdvanceUntilStage(chain, 2);
        Require(chain.TechniqueStageIndex == 2 &&
            MathF.Abs(chain.Stamina.Current - 79f) < .01f,
            "Knight's Edge must require three J presses and spend 7 stamina per hit.");

        var bastion = new PlayerCombat();
        bastion.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(bastion, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill2);
        AttackResolution perfect = bastion.ResolveIncomingAttack(
            FrontContact(20), Vector2.Zero, FacingDirection.Right);
        Require(perfect == AttackResolution.PerfectGuard &&
            bastion.IsPerfectGuardFeedbackActive,
            "Iron Bastion failed its Perfect Guard window.");
        Advance(bastion, 2);
        bastion.Update(Frame(.05f), Skill1Input(), true, FacingDirection.Right);
        Advance(bastion, 6);
        Require(bastion.CurrentTechnique?.Id == "knights-edge" &&
            bastion.AttackPowerMultiplier >= 1.15f,
            "Iron Bastion Perfect Guard did not transition cleanly into its rewarded follow-up.");

        var normalGuard = new PlayerCombat();
        normalGuard.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(normalGuard, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill2);
        Advance(normalGuard, 5);
        Require(normalGuard.ResolveIncomingAttack(
                FrontContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.Blocked,
            "Iron Bastion must become a normal stamina block after Perfect Guard.");
        Require(normalGuard.ResolveIncomingAttack(
                RearContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.Damaged,
            "Iron Bastion incorrectly protected the rear.");

        var reversal = new PlayerCombat();
        reversal.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(reversal, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill1Skill2);
        Require(reversal.ResolveIncomingAttack(
                FrontContact(24), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.PerfectGuard &&
            reversal.AttackPowerMultiplier >= 1.45f,
            "Guardian's Reversal did not reward a timed interception.");

        var oath = new PlayerCombat();
        oath.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(oath, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Ultimate);
        oath.ResolveIncomingAttack(
            FrontContact(50), Vector2.Zero, FacingDirection.Right);
        oath.ResolveIncomingAttack(
            FrontContact(200), Vector2.Zero, FacingDirection.Right);
        Require(oath.OathPreventedDamage == PlayerCombat.OathPreventedDamageCap &&
            oath.OathStoredForce == PlayerCombat.OathStoredForceCap,
            "Oath prevented damage or stored force exceeded its cap.");
        AdvanceUntilStage(oath, 2, 45);
        Require(oath.CalculateDamage(25) == 115,
            "Oath final damage must be base 55 plus capped 60 stored force.");

        var emptyOath = new PlayerCombat();
        emptyOath.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(emptyOath, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Ultimate);
        AdvanceUntilStage(emptyOath, 2, 45);
        Require(emptyOath.CalculateDamage(25) == 55,
            "Oath without prevented damage must retain only its base final damage.");
    }

    private static CombatInput Skill1Input() => new(
        true, false, false, false, false, false, 0f);

    private static CombatInput EmptyInput() => new(
        false, false, false, false, false, false, 0f);

    private static CombatInput ReckonerOrbitInput(
        bool pressed = false,
        bool held = false,
        bool released = false) => new(
            skill1Pressed: false,
            skill2Pressed: pressed,
            skill3Pressed: false,
            dodgePressed: false,
            dashHeld: false,
            blockHeld: false,
            horizontalDirection: 0f,
            skill2Held: held,
            skill2Released: released);

    private static AttackContact FrontContact(int damage) => new(
        damage,
        new Vector2(100f, 0f),
        blockable: true,
        unblockable: false,
        new Rectangle(8, -40, 48, 80));

    private static AttackContact RearContact(int damage) => new(
        damage,
        new Vector2(-100f, 0f),
        blockable: true,
        unblockable: false,
        new Rectangle(-20, -40, 40, 80));

    private static void Advance(PlayerCombat combat, int frames)
    {
        for (int index = 0; index < frames; index++)
            combat.Update(
                Frame(.05f), EmptyInput(), true, FacingDirection.Right);
    }

    private static void AdvanceUntilStage(
        PlayerCombat combat,
        int stage,
        int maximumFrames = 16)
    {
        for (int frame = 0;
             frame < maximumFrames && combat.CurrentTechnique != null &&
                combat.TechniqueStageIndex < stage;
             frame++)
        {
            combat.Update(
                Frame(.05f), EmptyInput(), true, FacingDirection.Right);
        }
    }

    private static void ValidateEveryTechniqueStarts()
    {
        WeaponDefinition[] weapons =
        {
            EquipmentCatalog.KnightLongSword,
            EquipmentCatalog.DuelistDualSwords,
            EquipmentCatalog.IronVanguardGreatsword,
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.ForbiddenSovereignAbyssRing
        };

        foreach (WeaponDefinition weapon in weapons)
        {
            foreach (WeaponTechniqueInput input in Enum.GetValues<WeaponTechniqueInput>())
            {
                if (weapon.CombatProfile.Get(input) == null)
                    continue;
                var combat = new PlayerCombat();
                combat.ApplyLoadout(weapon, EquipmentCatalog.CreateStartingArmor());
                if (weapon.Family == WeaponFamily.ArcaneWarStaff)
                    combat.SetSpellbladeRuneCount(2);
                if (weapon.Family == WeaponFamily.GreatSword &&
                    input == WeaponTechniqueInput.Ultimate)
                {
                    combat.SetGreatswordUltimateTargetAvailable(true);
                }
                if (weapon.Family == WeaponFamily.AbyssRing &&
                    input == WeaponTechniqueInput.Ultimate)
                {
                    combat.SetSovereignUltimateTargetAvailable(true);
                }
                if (weapon.Family == WeaponFamily.ArcaneWarStaff &&
                    input == WeaponTechniqueInput.Ultimate)
                {
                    combat.SetAncientStaffUltimateTargetAvailable(true);
                }
                StartInput(combat, weapon, input);
                Require(combat.CurrentTechnique?.Input == input,
                    $"{weapon.Name} failed to start {input}.");

                if (input == WeaponTechniqueInput.Ultimate)
                    Require(combat.Stamina.Current == 0f,
                        $"{weapon.Name} ultimate did not consume all stamina.");
            }

            var insufficient = new PlayerCombat();
            insufficient.ApplyLoadout(weapon, EquipmentCatalog.CreateStartingArmor());
            if (weapon.Family == WeaponFamily.GreatSword)
                insufficient.SetGreatswordUltimateTargetAvailable(true);
            if (weapon.Family == WeaponFamily.AbyssRing)
                insufficient.SetSovereignUltimateTargetAvailable(true);
            if (weapon.Family == WeaponFamily.ArcaneWarStaff)
                insufficient.SetAncientStaffUltimateTargetAvailable(true);
            insufficient.Stamina.SpendUpTo(1f);
            StartInput(insufficient, weapon, WeaponTechniqueInput.Ultimate);
            Require(insufficient.CurrentTechnique == null &&
                insufficient.Stamina.Current == insufficient.Stamina.Maximum - 1f,
                $"{weapon.Name} ultimate activated without full stamina.");
        }
    }

    private static void StartInput(
        PlayerCombat combat,
        WeaponDefinition weapon,
        WeaponTechniqueInput input)
    {
        bool skill1 = input is WeaponTechniqueInput.Skill1 or
            WeaponTechniqueInput.Skill1Skill2 or
            WeaponTechniqueInput.Skill1Skill3 or
            WeaponTechniqueInput.Ultimate;
        bool skill2 = input is WeaponTechniqueInput.Skill2 or
            WeaponTechniqueInput.Skill1Skill2 or
            WeaponTechniqueInput.Skill2Skill3 or
            WeaponTechniqueInput.Ultimate;
        bool skill3 = input is WeaponTechniqueInput.Skill3 or
            WeaponTechniqueInput.Skill1Skill3 or
            WeaponTechniqueInput.Skill2Skill3 or
            WeaponTechniqueInput.Ultimate;
        bool dash = input is WeaponTechniqueInput.DashSkill or
            WeaponTechniqueInput.DashSkill2;

        if (dash)
        {
            WeaponTechniqueInput dashInput = input ==
                    WeaponTechniqueInput.DashSkill2
                ? WeaponTechniqueInput.Skill2
                : weapon.CombatProfile.DashInput;
            skill1 = dashInput == WeaponTechniqueInput.Skill1;
            skill2 = dashInput == WeaponTechniqueInput.Skill2;
            skill3 = dashInput == WeaponTechniqueInput.Skill3;
        }

        bool greatsword = weapon.Family == WeaponFamily.GreatSword;
        bool grounded = true;
        bool skill1Held = greatsword && skill1;
        bool skill2Held = greatsword && skill2;
        bool skill3Held = greatsword && skill3;

        combat.Update(
            Frame(.016f),
            new CombatInput(
                skill1, skill2, skill3,
                dodgePressed: dash,
                dashHeld: dash,
                blockHeld: false,
                horizontalDirection: 1f,
                skill1Held: skill1Held,
                skill3Held: skill3Held,
                skill2Held: skill2Held),
            isGrounded: grounded,
            FacingDirection.Right);

        for (int frame = 0;
             frame < 7 && combat.CurrentTechnique == null;
             frame++)
        {
            combat.Update(
                Frame(.05f),
                new CombatInput(false, false, false, false, false, false, 0f),
                isGrounded: grounded,
                FacingDirection.Right);
        }
    }

    private static GameTime Frame(float seconds) => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(seconds));

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
