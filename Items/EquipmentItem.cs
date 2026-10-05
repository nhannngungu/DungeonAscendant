using System;

namespace DungeonAscendant.Items;

public sealed class EquipmentItem : Item
{
    public EquipmentSlot Slot { get; }
    public int DamageBonus { get; }
    public int HealthBonus { get; }
    public int Score => Slot == EquipmentSlot.Weapon
        ? DamageBonus
        : HealthBonus;

    public EquipmentItem(
        int itemLevel,
        ItemRarity rarity,
        EquipmentSlot slot,
        int damageBonus,
        int healthBonus)
        : base(itemLevel, rarity)
    {
        Slot = slot;
        DamageBonus = Math.Max(0, damageBonus);
        HealthBonus = Math.Max(0, healthBonus);
    }
}
