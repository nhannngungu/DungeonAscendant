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
        int itemLevel = Math.Max(1, Math.Max(enemyLevel, playerLevel));

        if (isElite)
            itemLevel += _random.Next(0, 2);

        ItemRarity rarity = RollRarity(isElite);
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
        int roll = _random.Next(100);

        if (isElite)
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
