using System;

namespace DungeonAscendant.Items;

public abstract class Item
{
    public int ItemLevel { get; }
    public ItemRarity Rarity { get; }

    protected Item(int itemLevel, ItemRarity rarity)
    {
        ItemLevel = Math.Max(1, itemLevel);
        Rarity = rarity;
    }
}
