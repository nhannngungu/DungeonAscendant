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
        Require(MathF.Abs(resources.DuelistMomentum - 60f) < .01f,
            "Duelist damage loss must be meaningful without clearing Momentum.");
        ValidateEveryTechniqueStarts();
        ValidateUltimateControlImmunity();
        ValidateKnightCombat();
        ValidateDuelistCombat();
        ValidateGreatswordCombat();
        ValidateRangerCombat();
        ValidateSpellbladeCombat();
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
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.ShadowReaperScythe,
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
        Require(profile.Get(WeaponTechniqueInput.Skill1).Id ==
                "starburst-sword-art" &&
            profile.Get(WeaponTechniqueInput.Skill2).Id == "phantom-parry" &&
            profile.Get(WeaponTechniqueInput.Skill3).Id == "blade-overdrive" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2).Id ==
                "cross-requiem" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3).Id ==
                "heaven-splitter" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3).Id ==
                "thousand-edge-waltz" &&
            profile.Get(WeaponTechniqueInput.Ultimate).Id ==
                "infinite-starburst-ascension",
            "Astral Twinblades J/K/L chord mapping is incomplete.");
        Require(profile.Get(WeaponTechniqueInput.Skill1).StageCount == 8,
            "Starburst Sword Art must contain eight authored attacks.");
        Require(profile.Get(WeaponTechniqueInput.Ultimate).StageCount ==
                DuelistTuning.UltimateStrikeCount + 1,
            "Infinite Starburst must contain exactly 32 strikes and Final X.");

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

        var priority = CreateDuelistCombat(0f);
        priority.Update(
            Frame(.016f),
            new CombatInput(
                true, true, false, false, false, false, 0f),
            true,
            FacingDirection.Right);
        Advance(priority, 7);
        Require(priority.CurrentTechnique?.Id == "cross-requiem",
            "J+K must resolve atomically as Cross Requiem.");

        float totalUltimateDamage = 0f;
        WeaponTechnique ultimate = profile.Get(WeaponTechniqueInput.Ultimate);
        for (int index = 0; index < ultimate.StageCount; index++)
            totalUltimateDamage += ultimate.GetStage(index).DamageMultiplier;
        Require(MathF.Abs(totalUltimateDamage - 6.80f) < .001f,
            "Infinite Starburst aggregate damage must total 680%.");
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
        WeaponCombatProfile profile =
            EquipmentCatalog.KnightLongSword.CombatProfile;
		Require(ReferenceEquals(
				profile,
				WeaponTechniqueCatalog.KnightSwordAndShield) &&
			profile.Id == "knight-sword-and-shield" &&
			ReferenceEquals(
				EquipmentCatalog.KnightLongSword.MoveSet,
				WeaponMoveSets.KnightSwordAndShield) &&
			profile.DashInput == WeaponTechniqueInput.Skill1 &&
            profile.Get(WeaponTechniqueInput.Skill1)?.Id == "knights-combo" &&
            profile.Get(WeaponTechniqueInput.Skill2)?.Id == "shield-guard" &&
            profile.Get(WeaponTechniqueInput.Skill3)?.Id == "shield-bash" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill2)?.Id ==
                "guard-counter" &&
            profile.Get(WeaponTechniqueInput.Skill2Skill3)?.Id ==
                "shield-wall" &&
            profile.Get(WeaponTechniqueInput.Skill1Skill3)?.Id ==
                "blade-breaker" &&
            profile.Get(WeaponTechniqueInput.DashSkill)?.Id ==
                "vanguard-slash" &&
            profile.Get(WeaponTechniqueInput.Ultimate)?.Id ==
                "knights-final-oath",
            "Knight V4.1 J/K/L, Shift+J, and JKL mapping is incomplete.");
        Require(WeaponTechniqueCatalog.KnightGuardRiposte.Id ==
                "guard-riposte" &&
            WeaponTechniqueCatalog.KnightFallingShield.Id ==
                "falling-shield",
            "Knight V4.1 contextual Guard Riposte or Air+K mapping is stale.");

        RequireStageDamage(profile.Get(WeaponTechniqueInput.Skill1),
            1f, 1.05f, 1.15f, 1.45f);
        RequireStageDamage(profile.Get(WeaponTechniqueInput.Skill1Skill2),
            .35f, .75f, .95f);
        RequireStageDamage(profile.Get(WeaponTechniqueInput.Skill2Skill3),
            0f, .55f, .45f);
        RequireStageDamage(profile.Get(WeaponTechniqueInput.Skill1Skill3),
            .85f, .50f, 1.25f);

        var chain = new PlayerCombat();
        chain.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(chain, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill1);
        for (int stage = 1; stage < 4; stage++)
        {
            chain.Update(Frame(.04f), EmptyInput(), true,
                FacingDirection.Right);
            chain.Update(Frame(.04f), Skill1Input(), true,
                FacingDirection.Right);
            AdvanceUntilStage(chain, stage);
        }
        Require(chain.TechniqueStageIndex == 3 &&
            chain.CurrentTechnique?.Id == "knights-combo",
            "Knight's Combo must expose four manually linked J attacks.");

        var guard = new PlayerCombat();
        guard.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(guard, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill2);
        AttackResolution perfect = guard.ResolveIncomingAttack(
            FrontContact(20), Vector2.Zero, FacingDirection.Right);
        Require(perfect == AttackResolution.PerfectGuard &&
            guard.IsPerfectGuardFeedbackActive,
            "Shield Guard failed its Perfect Guard window.");
        guard.Update(Frame(.05f), Skill1Input(), true, FacingDirection.Right);
        Advance(guard, 5);
        Require(guard.CurrentTechnique?.Id == "guard-riposte",
            "Perfect Guard did not transition into Guard Riposte.");

        var normalGuard = new PlayerCombat();
        normalGuard.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(normalGuard, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill2);
        Advance(normalGuard, 5);
        Require(normalGuard.ResolveIncomingAttack(
                FrontContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.Blocked,
            "Shield Guard must become a normal stamina block after its timing window.");
        Require(normalGuard.ResolveIncomingAttack(
                RearContact(20), Vector2.Zero, FacingDirection.Right) ==
                AttackResolution.Damaged,
            "Shield Guard incorrectly protected the rear.");

        var counter = new PlayerCombat();
        counter.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(counter, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Skill1Skill2);
        Require(counter.CurrentTechnique?.Id == "guard-counter" &&
            counter.CurrentTechnique.StageCount == 3,
            "Guard Counter must run its shield-check and two-slash sequence.");

        var vanguard = new PlayerCombat();
        vanguard.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        StartInput(vanguard, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.DashSkill);
        Require(vanguard.CurrentTechnique?.Id == "vanguard-slash" &&
            vanguard.CurrentTechnique.Effect ==
                WeaponTechniqueEffect.KnightVanguardSlash,
            "Shift+J must start Vanguard Slash through the shared dash input path.");

        var sixtyFpsVanguard = new PlayerCombat();
        sixtyFpsVanguard.ApplyLoadout(
            EquipmentCatalog.KnightLongSword,
            armor);
        for (int frame = 0;
             frame < 20 && sixtyFpsVanguard.CurrentTechnique == null;
             frame++)
        {
            sixtyFpsVanguard.Update(
                Frame(1f / 60f),
                new CombatInput(
                    skill1Pressed: frame == 0,
                    skill2Pressed: false,
                    skill3Pressed: false,
                    dodgePressed: frame == 0,
                    dashHeld: true,
                    blockHeld: false,
                    horizontalDirection: 1f,
                    skill1Held: true),
                isGrounded: true,
                FacingDirection.Right);
        }
        Require(sixtyFpsVanguard.CurrentTechnique?.Id == "vanguard-slash" &&
                sixtyFpsVanguard.State != CombatState.Dodging,
            "At 60 FPS the shared Shift input dodged before Vanguard Slash resolved.");

        var falling = new PlayerCombat();
        falling.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        falling.Update(Frame(.016f),
            new CombatInput(false, true, false, false, false, false, 0f),
            isGrounded: false, FacingDirection.Right);
        for (int frame = 0;
             frame < 7 && falling.CurrentTechnique == null;
             frame++)
        {
            falling.Update(Frame(.05f), EmptyInput(),
                isGrounded: false, FacingDirection.Right);
        }
        Require(falling.CurrentTechnique?.Id == "falling-shield" &&
            falling.CurrentTechnique.Effect ==
                WeaponTechniqueEffect.KnightFallingShield,
            "Air+K must start Falling Shield.");

        var landingEdge = new PlayerCombat();
        landingEdge.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        landingEdge.Update(Frame(1f / 60f),
            new CombatInput(false, true, false, false, false, false, 0f),
            isGrounded: false, FacingDirection.Right);
        for (int frame = 0;
             frame < 20 && landingEdge.CurrentTechnique == null;
             frame++)
        {
            landingEdge.Update(Frame(1f / 60f), EmptyInput(),
                isGrounded: true, FacingDirection.Right);
        }
        Require(landingEdge.CurrentTechnique?.Id == "falling-shield",
            "Air+K lost its aerial context before the Knight chord resolved.");

		var switchReset = new PlayerCombat();
		switchReset.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
		StartInput(switchReset, EquipmentCatalog.KnightLongSword,
			WeaponTechniqueInput.Skill2);
		Require(switchReset.ResolveIncomingAttack(
				FrontContact(20), Vector2.Zero, FacingDirection.Right) ==
			AttackResolution.PerfectGuard,
			"Knight switch-reset fixture did not earn Guard Riposte.");
		switchReset.ApplyLoadout(EquipmentCatalog.DuelistDualSwords, armor);
		switchReset.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
		StartInput(switchReset, EquipmentCatalog.KnightLongSword,
			WeaponTechniqueInput.Skill1);
		Require(switchReset.CurrentTechnique?.Id == "knights-combo",
			"Knight -> Twin Blades -> Knight leaked stale Guard Riposte state.");

        var oath = new PlayerCombat();
        oath.ApplyLoadout(EquipmentCatalog.KnightLongSword, armor);
        oath.SetKnightUltimateTargetAvailable(true);
        StartInput(oath, EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Ultimate);
        Require(oath.IsKnightFinalOathActive && oath.Stamina.Current == 0f &&
            oath.IsUltimateControlImmune,
            "Knight's Final Oath must validate its target, spend full stamina, and enter shared immunity.");
    }

    private static CombatInput Skill1Input() => new(
        true, false, false, false, false, false, 0f);

    private static CombatInput EmptyInput() => new(
        false, false, false, false, false, false, 0f);

    private static void RequireStageDamage(
        WeaponTechnique technique,
        params float[] expected)
    {
        Require(technique != null && technique.StageCount == expected.Length,
            $"Knight technique stage count mismatch for {technique?.Id ?? "<null>"}.");
        for (int stage = 0; stage < expected.Length; stage++)
        {
            Require(MathF.Abs(
                    technique.GetStage(stage).DamageMultiplier - expected[stage]) <
                    .0001f,
                $"Knight technique {technique.Id} stage {stage + 1} damage mismatch.");
        }
    }

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
        foreach (WeaponDefinition weapon in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            foreach (WeaponTechniqueInput input in Enum.GetValues<WeaponTechniqueInput>())
            {
				WeaponTechnique expected = weapon.CombatProfile.Get(input);
				if (expected == null)
                    continue;
                var combat = new PlayerCombat();
                combat.ApplyLoadout(weapon, EquipmentCatalog.CreateStartingArmor());
                if (weapon.Family == WeaponFamily.ArcaneWarStaff)
                    combat.SetSpellbladeRuneCount(2);
                if (input == WeaponTechniqueInput.Ultimate)
                    PrepareUltimateActivation(combat, weapon);
                StartInput(combat, weapon, input);
				Require(ReferenceEquals(combat.CurrentTechnique, expected),
					$"{weapon.Name} failed to start current technique " +
					$"'{expected.Name}' ({expected.Id}).");

                if (input == WeaponTechniqueInput.Ultimate)
                    Require(combat.Stamina.Current == 0f,
                        $"{weapon.Name} ultimate did not consume all stamina.");
            }

            var insufficient = new PlayerCombat();
            insufficient.ApplyLoadout(weapon, EquipmentCatalog.CreateStartingArmor());
            PrepareUltimateActivation(insufficient, weapon);
            insufficient.Stamina.SpendUpTo(1f);
            StartInput(insufficient, weapon, WeaponTechniqueInput.Ultimate);
            Require(insufficient.CurrentTechnique == null &&
                insufficient.Stamina.Current == insufficient.Stamina.Maximum - 1f,
                $"{weapon.Name} ultimate activated without full stamina.");
        }
    }

    private static void ValidateUltimateControlImmunity()
    {
        foreach (WeaponDefinition weapon in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            var combat = new PlayerCombat();
            combat.ApplyLoadout(
                weapon,
                EquipmentCatalog.CreateStartingArmor());
            PrepareUltimateActivation(combat, weapon);
            StartInput(combat, weapon, WeaponTechniqueInput.Ultimate);
            Require(combat.IsUltimateControlImmune,
                $"{weapon.Name} ultimate did not enter shared control immunity.");

            WeaponTechnique technique = combat.CurrentTechnique;
            CombatState state = combat.State;
            AttackDefinition attack = combat.CurrentAttack;
            combat.OnDamaged(isAlive: true, damage: 30, hardInterrupt: true);
            combat.HardInterrupt();
            Require(combat.IsUltimateControlImmune &&
                ReferenceEquals(combat.CurrentTechnique, technique) &&
                ReferenceEquals(combat.CurrentAttack, attack) &&
                combat.State == state,
                $"{weapon.Name} ultimate was interrupted by enemy control.");

            AttackResolution resolution = combat.ResolveIncomingAttack(
                new AttackContact(
                    30,
                    new Vector2(100f, 0f),
                    blockable: true,
                    unblockable: true,
                    new Rectangle(-20, -40, 40, 80)),
                Vector2.Zero,
                FacingDirection.Right);
            Require(resolution == AttackResolution.Damaged &&
                combat.IsUltimateControlImmune,
                $"{weapon.Name} ultimate did not preserve damage contact.");

            combat.CancelActions();
            Require(!combat.IsUltimateControlImmune &&
                combat.CurrentTechnique == null,
                $"{weapon.Name} ultimate immunity survived lifecycle reset.");

            var death = new PlayerCombat();
            death.ApplyLoadout(
                weapon,
                EquipmentCatalog.CreateStartingArmor());
            PrepareUltimateActivation(death, weapon);
            StartInput(death, weapon, WeaponTechniqueInput.Ultimate);
            death.OnDamaged(isAlive: false, damage: int.MaxValue,
                hardInterrupt: true);
            Require(death.State == CombatState.Dead &&
                !death.IsUltimateControlImmune &&
                death.CurrentTechnique == null,
                $"{weapon.Name} ultimate overrode player death.");
        }

        var player = new DungeonAscendant.Player.Player(Vector2.Zero)
        {
            DebugGodMode = false
        };
        player.EquipDebugItem(new EquipmentItem(
            EquipmentCatalog.KnightLongSword,
            ItemRarity.Legendary));
        player.Combat.SetKnightUltimateTargetAvailable(true);
        StartInput(
            player.Combat,
            EquipmentCatalog.KnightLongSword,
            WeaponTechniqueInput.Ultimate);
        int healthBeforeHit = player.CurrentHealth;
        Require(player.ReceiveDamage(10) &&
            player.CurrentHealth < healthBeforeHit &&
            player.Combat.IsUltimateControlImmune,
            "Ultimate immunity blocked damage or lost the active action.");
        player.ApplySlow(.2f, 3f);
        Require(!player.IsSlowed,
            "Ultimate-controlled movement was reduced by enemy slow.");
    }

    private static void PrepareUltimateActivation(
        PlayerCombat combat,
        WeaponDefinition weapon)
    {
        switch (weapon.Family)
        {
            case WeaponFamily.DualSwords:
                combat.SetDuelistUltimateTargetAvailable(true);
                break;
            case WeaponFamily.HunterBow:
                combat.SetRangerUltimateTargetAvailable(true);
                break;
            case WeaponFamily.GreatSword:
                combat.SetGreatswordUltimateTargetAvailable(true);
                break;
            case WeaponFamily.LongSword:
                combat.SetKnightUltimateTargetAvailable(true);
                break;
            case WeaponFamily.ArcaneWarStaff:
                combat.SetAncientStaffUltimateTargetAvailable(true);
                break;
            case WeaponFamily.Scythe:
                combat.SetScytheUltimateTargetAvailable(true);
                break;
            case WeaponFamily.AbyssRing:
                combat.SetSovereignUltimateTargetAvailable(true);
                break;
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
                new CombatInput(false, false, false, false, dash, false, 0f),
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
