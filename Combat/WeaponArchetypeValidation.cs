using System;
using System.Collections.Generic;
using DungeonAscendant.Items;

namespace DungeonAscendant.Combat;

/// <summary>
/// Fast fail checks for the playable archetype contract. These run when a
/// session is created, so data edits cannot silently collapse families back
/// into a shared sword profile.
/// </summary>
public static class WeaponArchetypeValidation
{
    public static void ValidateOrThrow()
    {
        WeaponDefinition[] weapons =
        {
            EquipmentCatalog.KnightLongSword,
            EquipmentCatalog.DuelistDualSwords,
            EquipmentCatalog.IronVanguardGreatsword,
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.ReckonerChainFlail,
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.ForbiddenSovereignAbyssRing
        };
        var families = new HashSet<WeaponFamily>();

        foreach (WeaponDefinition weapon in weapons)
        {
            Require(families.Add(weapon.Family),
                "Every test weapon must use a unique family.");

            foreach (WeaponTechniqueInput input in Enum.GetValues<WeaponTechniqueInput>())
            {
                if (input == WeaponTechniqueInput.DashSkill2 &&
                    weapon.Family != WeaponFamily.ArcaneWarStaff)
                {
                    continue;
                }

                WeaponTechnique technique = weapon.CombatProfile.Get(input);
                Require(technique != null,
                    $"{weapon.Name} is missing {input}.");
                Require(technique.StageCount > 0,
                    $"{weapon.Name} {technique.Name} needs at least one stage.");
            }

            WeaponTechnique ultimate = weapon.CombatProfile.Get(
                WeaponTechniqueInput.Ultimate);
            if (weapon.Family == WeaponFamily.HunterBow)
            {
                Require(ultimate.Effect ==
                        WeaponTechniqueEffect.RangerPhantomHunt,
                    "Bow ultimate must remain Phantom Hunt.");
                Require(ultimate.RequiresFullStamina &&
                        ultimate.StaminaCost == 100f,
                    "Phantom Hunt must use and require full stamina.");
                Require(ultimate.ResourceCost == 0,
                    "Phantom Hunt must not depend on Flow Focus or the legacy Bow Ultimate Meter.");
            }
            else
            {
                Require(ultimate.RequiresFullStamina && ultimate.StaminaCost == 100f,
                    $"{weapon.Name} ultimate must consume full stamina.");
            }
        }

        Require(families.Count == EquipmentCatalog.WeaponSampleCount,
            "The debug catalog must cover all implemented weapon families.");
        Require(EquipmentCatalog.BreakerSpikedMace.CombatProfile
                .Get(WeaponTechniqueInput.Ultimate).StageCount == 1,
            "Worldbreaker must remain one enormous hit.");
        Require(EquipmentCatalog.KnightLongSword.UsesShield,
            "Knight requires its large shield.");
        Require(!EquipmentCatalog.IronVanguardGreatsword.UsesShield,
            "Iron Sovereign must guard with the blade, not a shield.");

        WeaponCombatProfile sovereign =
            EquipmentCatalog.ForbiddenSovereignAbyssRing.CombatProfile;
        Require(sovereign.Get(WeaponTechniqueInput.Skill1).StageCount == 8,
            "Sovereign Martial Arts must retain its eight-hit chain.");
        Require(sovereign.Get(WeaponTechniqueInput.Skill1Skill2).StageCount == 6 &&
                sovereign.Get(WeaponTechniqueInput.Skill2Skill3).StageCount == 5,
            "Abyss Ring portal combinations have lost their bounded stages.");
        WeaponTechnique cataclysm = sovereign.Get(
            WeaponTechniqueInput.Ultimate);
        Require(cataclysm.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                cataclysm.RequiresFullStamina &&
                cataclysm.StaminaCost == 100f &&
                cataclysm.StageCount == 19,
            "Golden Sovereign Cataclysm must use full Stamina and its bounded sequence.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
