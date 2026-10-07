using System;

namespace DungeonAscendant.Items;

public sealed class EquipmentItem : Item
{
    public EquipmentDefinition Definition { get; }
    public WeaponDefinition WeaponDefinition => Definition as WeaponDefinition;
    public ArmorDefinition ArmorDefinition => Definition as ArmorDefinition;
    public EquipmentSlot Slot => Definition.Slot;
    public int DamageBonus => WeaponDefinition?.DamageBonus ?? 0;
    public int HealthBonus => ArmorDefinition?.MaxHealthBonus ?? 0;
    public int Score => Slot == EquipmentSlot.Weapon
        ? DamageBonus + Definition.Tier * 3
        : HealthBonus + Definition.Tier * 4;

    public EquipmentItem(WeaponDefinition definition, ItemRarity rarity)
        : base(definition.Id, definition.Name, definition.Tier, rarity)
    {
        Definition = definition;
    }

    public EquipmentItem(ArmorDefinition definition, ItemRarity rarity)
        : base(definition.Id, definition.Name, definition.Tier, rarity)
    {
        Definition = definition;
    }

    public EquipmentItem(
        int itemLevel,
        ItemRarity rarity,
        EquipmentSlot slot,
        int damageBonus,
        int healthBonus)
        : base(
            $"legacy-{slot.ToString().ToLowerInvariant()}-t{itemLevel}",
            slot == EquipmentSlot.Weapon ? "Generated Weapon" : "Generated Armor",
            itemLevel,
            rarity)
    {
        Definition = slot == EquipmentSlot.Weapon
            ? EquipmentCatalog.CreateGeneratedWeapon(
                WeaponFamily.LongSword,
                itemLevel,
                Math.Max(0, damageBonus))
            : EquipmentCatalog.CreateGeneratedArmor(
                ArmorClass.Medium,
                itemLevel,
                Math.Max(0, healthBonus));
    }
}
