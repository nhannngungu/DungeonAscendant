namespace DungeonAscendant.Items;

/// <summary>
/// An explicitly designed Region boss weapon reward. Generic loot generation
/// cannot create this reward.
/// </summary>
public sealed class BossWeaponReward
{
    public WeaponDefinition Definition { get; }
    public ItemRarity Rarity { get; }

    public BossWeaponReward(
        WeaponDefinition definition,
        ItemRarity rarity)
    {
        Definition = definition;
        Rarity = rarity;
    }

    public EquipmentItem CreateItem()
    {
        return Definition == null
            ? null
            : new EquipmentItem(Definition, Rarity);
    }
}
