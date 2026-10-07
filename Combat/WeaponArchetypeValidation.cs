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
            EquipmentCatalog.IronGreatSword,
            EquipmentCatalog.WarAxe,
            EquipmentCatalog.HunterSpear,
            EquipmentCatalog.TwinDaggers,
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.ApprenticeArcaneStaff
        };
        int[] expectedComboLengths = { 3, 3, 3, 3, 5, 1, 1 };
        var families = new HashSet<WeaponFamily>();

        for (int index = 0; index < weapons.Length; index++)
        {
            WeaponDefinition weapon = weapons[index];
            Require(families.Add(weapon.Family), "Every test weapon must use a unique family.");
            Require(weapon.MoveSet.ComboLength == expectedComboLengths[index],
                $"{weapon.Name} has the wrong combo length.");
            Require(weapon.UsesShield == (weapon.Family == WeaponFamily.LongSword),
                $"{weapon.Name} violates the shield rule.");
        }

        Require(families.Count == Enum.GetValues<WeaponFamily>().Length,
            "The test catalog must cover every weapon family.");

        Require(IsRanged(EquipmentCatalog.HunterBow, AttackDelivery.Arrow),
            "Bow attacks must deliver arrows.");
        Require(IsRanged(EquipmentCatalog.ApprenticeArcaneStaff, AttackDelivery.ArcaneProjectile),
            "Arcane Staff attacks must deliver arcane projectiles.");

        foreach (WeaponDefinition weapon in weapons)
        {
            if (weapon.Family is WeaponFamily.Bow or WeaponFamily.ArcaneStaff)
                continue;

            for (int index = 0; index < weapon.MoveSet.ComboLength; index++)
                Require(weapon.MoveSet.GetLight(index).Delivery == AttackDelivery.Melee,
                    $"{weapon.Name} light attacks must be melee.");
        }

        float spearReach = EffectiveReach(EquipmentCatalog.HunterSpear);
        float greatSwordReach = EffectiveReach(EquipmentCatalog.IronGreatSword);
        float longSwordReach = EffectiveReach(EquipmentCatalog.KnightLongSword);
        float daggerReach = EffectiveReach(EquipmentCatalog.TwinDaggers);
        Require(spearReach > greatSwordReach && greatSwordReach > longSwordReach &&
            longSwordReach > daggerReach,
            "Melee reach ordering must remain Spear > Great Sword > Long Sword > Daggers.");

        float greatSwordCost = EffectiveCost(EquipmentCatalog.IronGreatSword);
        float longSwordCost = EffectiveCost(EquipmentCatalog.KnightLongSword);
        float daggerCost = EffectiveCost(EquipmentCatalog.TwinDaggers);
        Require(greatSwordCost > longSwordCost && longSwordCost > daggerCost,
            "Stamina identity must remain Great Sword > Long Sword > Daggers.");

        float axePoise = EquipmentCatalog.WarAxe.MoveSet.Heavy.PoiseDamage *
            EquipmentCatalog.WarAxe.PoiseDamageModifier;
        float greatSwordPoise = EquipmentCatalog.IronGreatSword.MoveSet.Heavy.PoiseDamage *
            EquipmentCatalog.IronGreatSword.PoiseDamageModifier;
        Require(axePoise > greatSwordPoise,
            "Battle Axe must retain the strongest heavy poise identity.");
    }

    private static bool IsRanged(
        WeaponDefinition weapon,
        AttackDelivery delivery)
    {
        if (weapon.MoveSet.Heavy.Delivery != delivery ||
            weapon.MoveSet.Heavy.Projectile == null)
            return false;

        for (int index = 0; index < weapon.MoveSet.ComboLength; index++)
        {
            AttackDefinition attack = weapon.MoveSet.GetLight(index);
            if (attack.Delivery != delivery || attack.Projectile == null)
                return false;
        }

        return true;
    }

    private static float EffectiveReach(WeaponDefinition weapon) =>
        weapon.MoveSet.GetLight(0).Range * weapon.RangeModifier;

    private static float EffectiveCost(WeaponDefinition weapon) =>
        weapon.MoveSet.GetLight(0).StaminaCost * weapon.StaminaCostModifier;

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
