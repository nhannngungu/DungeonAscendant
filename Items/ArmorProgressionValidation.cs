using System;
using DungeonAscendant.Player;
using DungeonAscendant.World;

namespace DungeonAscendant.Items;

public static class ArmorProgressionValidation
{
    public static void ValidateOrThrow()
    {
        ValidateSuccessfulFusion();
        ValidateEligibilityBoundaries();
        ValidateGenericLootIsArmorOnly();
    }

    private static void ValidateSuccessfulFusion()
    {
        var inventory = new Inventory();

        for (int index = 0; index < ArmorGradeRules.FusionMaterialCount; index++)
        {
            inventory.TryAdd(new EquipmentItem(
                EquipmentCatalog.KnightArmor,
                ItemRarity.Uncommon,
                ArmorGrade.C1));
        }

        EquipmentItem source = inventory.GetItem(0);

        if (!inventory.TryFuseArmor(source, out EquipmentItem result) ||
            inventory.Count != 1 ||
            result.ArmorDefinition.Id != EquipmentCatalog.KnightArmor.Id ||
            result.ArmorGrade != ArmorGrade.C2 ||
            result.Rarity != ItemRarity.Uncommon)
        {
            throw new InvalidOperationException(
                "Armor fusion must consume three identical unequipped pieces " +
                "and create the next Grade with the same definition and rarity.");
        }
    }

    private static void ValidateEligibilityBoundaries()
    {
        var mixed = new Inventory();
        mixed.TryAdd(new EquipmentItem(
            EquipmentCatalog.KnightArmor,
            ItemRarity.Common,
            ArmorGrade.C1));
        mixed.TryAdd(new EquipmentItem(
            EquipmentCatalog.KnightArmor,
            ItemRarity.Common,
            ArmorGrade.C2));
        mixed.TryAdd(new EquipmentItem(
            EquipmentCatalog.ScoutArmor,
            ItemRarity.Common,
            ArmorGrade.C1));

        if (mixed.CanFuseArmor(mixed.GetItem(0)))
            throw new InvalidOperationException("Mixed armor cannot be fused.");

        var maximum = new Inventory();
        for (int index = 0; index < ArmorGradeRules.FusionMaterialCount; index++)
        {
            maximum.TryAdd(new EquipmentItem(
                EquipmentCatalog.FortressArmor,
                ItemRarity.Rare,
                ArmorGrade.C5));
        }

        if (maximum.CanFuseArmor(maximum.GetItem(0)))
            throw new InvalidOperationException("C5 armor cannot be fused.");

        var equippedSafety = new Equipment();
        equippedSafety.SetStartingItems(
            EquipmentCatalog.CreateStartingWeapon(),
            new EquipmentItem(
                EquipmentCatalog.KnightArmor,
                ItemRarity.Uncommon,
                ArmorGrade.C1));
        var unequipped = new Inventory();
        unequipped.TryAdd(new EquipmentItem(
            EquipmentCatalog.KnightArmor,
            ItemRarity.Uncommon,
            ArmorGrade.C1));
        unequipped.TryAdd(new EquipmentItem(
            EquipmentCatalog.KnightArmor,
            ItemRarity.Uncommon,
            ArmorGrade.C1));

        if (unequipped.CanFuseArmor(unequipped.GetItem(0)))
        {
            throw new InvalidOperationException(
                "Equipped armor must not count as fusion material.");
        }
    }

    private static void ValidateGenericLootIsArmorOnly()
    {
        var generator = new ItemGenerator(randomSeed: 7349);

        foreach (LootSource source in Enum.GetValues<LootSource>())
        {
            for (int index = 0; index < 64; index++)
            {
                EquipmentItem item = generator.Generate(
                    sourceLevel: 8,
                    playerLevel: 8,
                    source,
                    dungeonDepth: 9,
                    worldTier: 5,
                    RegionType.WildForest);

                if (item.ArmorDefinition == null || item.WeaponDefinition != null)
                {
                    throw new InvalidOperationException(
                        "Generic loot generation must never create a weapon.");
                }
            }
        }
    }
}
