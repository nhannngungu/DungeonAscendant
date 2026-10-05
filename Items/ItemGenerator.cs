using System;

namespace DungeonAscendant.Items;

public sealed class ItemGenerator
{
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
            isElite ? LootSource.EliteEnemy : LootSource.NormalEnemy);
    }

    public EquipmentItem Generate(
        int sourceLevel,
        int playerLevel,
        LootSource source)
    {
        int itemLevel = Math.Max(1, Math.Max(sourceLevel, playerLevel));

        itemLevel += source switch
        {
            LootSource.EliteEnemy => _random.Next(0, 2),
            LootSource.TreasureChest => _random.Next(0, 2),
            LootSource.Boss => 1 + _random.Next(0, 2),
            _ => 0
        };

        ItemRarity rarity = RollRarity(source);
        EquipmentSlot slot = _random.Next(2) == 0
            ? EquipmentSlot.Weapon
            : EquipmentSlot.Armor;
        int rarityPercent = GetRarityMultiplierPercent(rarity);

        if (slot == EquipmentSlot.Weapon)
        {
            int baseDamageBonus = 4 + itemLevel * 2;
            return new EquipmentItem(
                itemLevel,
                rarity,
                slot,
                ScaleAndRound(baseDamageBonus, rarityPercent),
                healthBonus: 0);
        }

        int baseHealthBonus = 10 + itemLevel * 5;
        return new EquipmentItem(
            itemLevel,
            rarity,
            slot,
            damageBonus: 0,
            ScaleAndRound(baseHealthBonus, rarityPercent));
    }

    public ItemRarity RollRarity(bool isElite)
    {
        return RollRarity(
            isElite ? LootSource.EliteEnemy : LootSource.NormalEnemy);
    }

    public ItemRarity RollRarity(LootSource source)
    {
        int roll = _random.Next(100);

        if (source == LootSource.Boss)
        {
            if (roll < 55)
                return ItemRarity.Rare;
            if (roll < 90)
                return ItemRarity.Epic;
            return ItemRarity.Legendary;
        }

        if (source == LootSource.TreasureChest)
        {
            if (roll < 15)
                return ItemRarity.Common;
            if (roll < 50)
                return ItemRarity.Uncommon;
            if (roll < 80)
                return ItemRarity.Rare;
            if (roll < 95)
                return ItemRarity.Epic;
            return ItemRarity.Legendary;
        }

        if (source == LootSource.EliteEnemy)
        {
            if (roll < 25)
                return ItemRarity.Common;
            if (roll < 55)
                return ItemRarity.Uncommon;
            if (roll < 80)
                return ItemRarity.Rare;
            if (roll < 95)
                return ItemRarity.Epic;
            return ItemRarity.Legendary;
        }

        if (roll < 55)
            return ItemRarity.Common;
        if (roll < 80)
            return ItemRarity.Uncommon;
        if (roll < 93)
            return ItemRarity.Rare;
        if (roll < 99)
            return ItemRarity.Epic;
        return ItemRarity.Legendary;
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
