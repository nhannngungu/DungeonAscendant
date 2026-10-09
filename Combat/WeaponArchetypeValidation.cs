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
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.RaiderWarAxe,
            EquipmentCatalog.ArcaneWarStaff,
            EquipmentCatalog.BreakerSpikedMace,
            EquipmentCatalog.ReckonerChainFlail
        };
        var families = new HashSet<WeaponFamily>();

        foreach (WeaponDefinition weapon in weapons)
        {
            Require(families.Add(weapon.Family),
                "Every test weapon must use a unique family.");

            foreach (WeaponTechniqueInput input in Enum.GetValues<WeaponTechniqueInput>())
            {
                WeaponTechnique technique = weapon.CombatProfile.Get(input);
                Require(technique != null,
                    $"{weapon.Name} is missing {input}.");
                Require(technique.StageCount > 0,
                    $"{weapon.Name} {technique.Name} needs at least one stage.");
            }

            WeaponTechnique ultimate = weapon.CombatProfile.Get(
                WeaponTechniqueInput.Ultimate);
            Require(ultimate.RequiresFullStamina && ultimate.StaminaCost == 100f,
                $"{weapon.Name} ultimate must consume full stamina.");
        }

        Require(families.Count == EquipmentCatalog.WeaponSampleCount,
            "The debug catalog must cover all seven official weapon families.");
        Require(EquipmentCatalog.BreakerSpikedMace.CombatProfile
                .Get(WeaponTechniqueInput.Ultimate).StageCount == 1,
            "Worldbreaker must remain one enormous hit.");
        Require(EquipmentCatalog.KnightLongSword.UsesShield,
            "Knight requires its large shield.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
