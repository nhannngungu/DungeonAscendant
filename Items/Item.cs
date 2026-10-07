using System;

namespace DungeonAscendant.Items;

public abstract class Item
{
    public string Id { get; }
    public string Name { get; }
    public int Tier { get; }
    public int ItemLevel => Tier;
    public ItemRarity Rarity { get; }

    protected Item(string id, string name, int tier, ItemRarity rarity)
    {
        Id = string.IsNullOrWhiteSpace(id) ? "item" : id;
        Name = string.IsNullOrWhiteSpace(name) ? "Item" : name;
        Tier = Math.Max(1, tier);
        Rarity = rarity;
    }
}
