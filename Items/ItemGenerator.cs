using System;
using DungeonAscendant.Progression;

namespace DungeonAscendant.Items;

public sealed class ItemGenerator
{
    private static readonly RarityWeights[] NormalEnemyWeights =
    {
        new(55, 25, 13, 6, 1),
        new(49, 27, 15, 8, 1),
        new(43, 28, 18, 9, 2),
        new(37, 29, 20, 11, 3),
        new(31, 30, 22, 13, 4)
    };

    private static readonly RarityWeights[] EliteEnemyWeights =
    {
        new(25, 30, 25, 15, 5),
        new(21, 29, 27, 17, 6),
        new(17, 28, 29, 19, 7),
        new(13, 27, 31, 21, 8),
        new(9, 26, 33, 23, 9)
    };

    private static readonly RarityWeights[] TreasureChestWeights =
    {
        new(15, 35, 30, 15, 5),
        new(12, 32, 32, 18, 6),
        new(9, 29, 34, 21, 7),
        new(6, 26, 36, 24, 8),
        new(3, 23, 38, 27, 9)
    };

    private static readonly RarityWeights[] BossWeights =
    {
        new(0, 0, 55, 35, 10),
        new(0, 0, 50, 38, 12),
        new(0, 0, 45, 41, 14),
        new(0, 0, 40, 44, 16),
        new(0, 0, 35, 47, 18)
    };

    private readonly Random _random;

    public ItemGenerator(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
    }

    public EquipmentItem Generate(
        int enemyLevel,
        int playerLevel,
        bool isElite)
    {
        return Generate(
            enemyLevel,
            playerLevel,
            isElite ? LootSource.EliteEnemy : LootSource.NormalEnemy,
            dungeonDepth: 1,
            worldTier: 1);
    }

    public EquipmentItem Generate(
        int sourceLevel,
        int playerLevel,
        LootSource source)
    {
        return Generate(
            sourceLevel,
            playerLevel,
            source,
            dungeonDepth: 1,
            worldTier: 1);
    }

    public EquipmentItem Generate(
        int sourceLevel,
        int playerLevel,
        LootSource source,
        int dungeonDepth,
        int worldTier)
    {
        int safeDepth = Math.Max(1, dungeonDepth);
        int safeWorldTier = WorldProgression.ClampWorldTier(worldTier);
        int itemLevel = Math.Max(1, Math.Max(sourceLevel, playerLevel));
        itemLevel += (safeDepth - 1) / 3;
        itemLevel += (safeWorldTier - 1) / 2;
        itemLevel += source switch
        {
            LootSource.EliteEnemy => _random.Next(0, 2),
            LootSource.TreasureChest => 1 + _random.Next(0, 2),
            LootSource.Boss => 2 + _random.Next(0, 2),
            _ => 0
        };

        ItemRarity rarity = RollRarity(source, safeWorldTier);
        EquipmentSlot slot = _random.Next(2) == 0
            ? EquipmentSlot.Weapon
            : EquipmentSlot.Armor;
        int rarityPercent = GetRarityMultiplierPercent(rarity);

        if (slot == EquipmentSlot.Weapon)
        {
            int baseDamageBonus = 4 + itemLevel * 2;
            WeaponFamily family = (WeaponFamily)_random.Next(
                Enum.GetValues<WeaponFamily>().Length);
            WeaponDefinition definition = EquipmentCatalog.CreateGeneratedWeapon(
                family,
                itemLevel,
                ScaleAndRound(baseDamageBonus, rarityPercent));
            return new EquipmentItem(definition, rarity);
        }

        int baseHealthBonus = 10 + itemLevel * 5;
        ArmorClass armorClass = (ArmorClass)_random.Next(3);
        ArmorDefinition armor = EquipmentCatalog.CreateGeneratedArmor(
            armorClass,
            itemLevel,
            ScaleAndRound(baseHealthBonus, rarityPercent));
        return new EquipmentItem(armor, rarity);
    }

    public ItemRarity RollRarity(bool isElite)
    {
        return RollRarity(
            isElite ? LootSource.EliteEnemy : LootSource.NormalEnemy,
            worldTier: 1);
    }

    public ItemRarity RollRarity(LootSource source)
    {
        return RollRarity(source, worldTier: 1);
    }

    public ItemRarity RollRarity(LootSource source, int worldTier)
    {
        RarityWeights weights = GetRarityWeights(source, worldTier);
        int roll = _random.Next(100);

        if (roll < weights.Common)
            return ItemRarity.Common;

        int threshold = weights.Common + weights.Uncommon;

        if (roll < threshold)
            return ItemRarity.Uncommon;

        threshold += weights.Rare;

        if (roll < threshold)
            return ItemRarity.Rare;

        threshold += weights.Epic;
        return roll < threshold
            ? ItemRarity.Epic
            : ItemRarity.Legendary;
    }

    public static RarityWeights GetRarityWeights(
        LootSource source,
        int worldTier)
    {
        int index = WorldProgression.ClampWorldTier(worldTier) - 1;
        return source switch
        {
            LootSource.EliteEnemy => EliteEnemyWeights[index],
            LootSource.TreasureChest => TreasureChestWeights[index],
            LootSource.Boss => BossWeights[index],
            _ => NormalEnemyWeights[index]
        };
    }

    public static int GetRarityMultiplierPercent(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => 125,
            ItemRarity.Rare => 150,
            ItemRarity.Epic => 190,
            ItemRarity.Legendary => 250,
            _ => 100
        };
    }

    private static int ScaleAndRound(int value, int percent)
    {
        return (value * percent + 50) / 100;
    }
}
