using System;
using DungeonAscendant.Progression;
using DungeonAscendant.World;

namespace DungeonAscendant.Items;

public sealed class ItemGenerator
{
    private readonly record struct GradeWeights(
        int C1,
        int C2,
        int C3,
        int C4,
        int C5);

    private static readonly GradeWeights[] NormalGradeWeights =
    {
        new(92, 8, 0, 0, 0), new(65, 30, 5, 0, 0),
        new(38, 42, 17, 3, 0), new(18, 35, 32, 13, 2),
        new(8, 22, 38, 25, 7)
    };

    private static readonly GradeWeights[] EliteGradeWeights =
    {
        new(75, 22, 3, 0, 0), new(45, 40, 13, 2, 0),
        new(24, 38, 28, 9, 1), new(10, 27, 38, 21, 4),
        new(4, 14, 36, 34, 12)
    };

    private static readonly GradeWeights[] TreasureGradeWeights =
    {
        new(68, 27, 5, 0, 0), new(38, 42, 17, 3, 0),
        new(18, 36, 34, 11, 1), new(8, 24, 40, 24, 4),
        new(3, 12, 33, 38, 14)
    };

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
        int worldTier,
        RegionType region = RegionType.WildForest)
    {
        _ = sourceLevel;
        _ = playerLevel;
        int safeWorldTier = WorldProgression.ClampWorldTier(worldTier);
        ArmorDefinition armor = _random.Next(3) switch
        {
            0 => EquipmentCatalog.ScoutArmor,
            1 => EquipmentCatalog.KnightArmor,
            _ => EquipmentCatalog.FortressArmor
        };
        return new EquipmentItem(
            armor,
            RollRarity(source, safeWorldTier),
            RollArmorGrade(source, dungeonDepth, safeWorldTier, region));
    }

    public ArmorGrade RollArmorGrade(
        LootSource source,
        int dungeonDepth,
        int worldTier,
        RegionType region = RegionType.WildForest)
    {
        int depthPressure = (Math.Max(1, dungeonDepth) - 1) / 3;
        int tierPressure = WorldProgression.ClampWorldTier(worldTier) - 1;
        int regionPressure = Math.Max(0, (int)region) / 2;
        int stage = Math.Clamp(
            depthPressure + tierPressure + regionPressure,
            0,
            4);
        GradeWeights weights = source switch
        {
            LootSource.EliteEnemy => EliteGradeWeights[stage],
            LootSource.TreasureChest => TreasureGradeWeights[stage],
            _ => NormalGradeWeights[stage]
        };
        int roll = _random.Next(100);

        if (roll < weights.C1)
            return ArmorGrade.C1;

        int threshold = weights.C1 + weights.C2;
        if (roll < threshold)
            return ArmorGrade.C2;

        threshold += weights.C3;
        if (roll < threshold)
            return ArmorGrade.C3;

        threshold += weights.C4;
        return roll < threshold ? ArmorGrade.C4 : ArmorGrade.C5;
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

}
