using System;

namespace DungeonAscendant.Items;

public sealed class EquipmentItem : Item
{
    public EquipmentDefinition Definition { get; }
    public WeaponDefinition WeaponDefinition => Definition as WeaponDefinition;
    public ArmorDefinition ArmorDefinition => Definition as ArmorDefinition;
    public ArmorGrade ArmorGrade { get; }
    public string ArmorGradeLabel => ArmorGradeRules.ToDisplayName(ArmorGrade);
    public EquipmentSlot Slot => Definition.Slot;
    public int DamageBonus => WeaponDefinition?.DamageBonus ?? 0;
    public int HealthBonus => ArmorDefinition == null
        ? 0
        : ArmorGradeRules.ScaleBonus(
            ArmorDefinition.MaxHealthBonus,
            ArmorGrade);
    public float DamageTakenMultiplier => ArmorDefinition == null
        ? 1f
        : ArmorGradeRules.ScaleDamageTaken(
            ArmorDefinition.DamageTakenMultiplier,
            ArmorGrade);
    public float MoveSpeedModifier => ArmorDefinition == null
        ? 1f
        : ArmorGradeRules.ScaleStrength(
            ArmorDefinition.MoveSpeedModifier,
            ArmorGrade);
    public float StaminaRegenModifier => ArmorDefinition == null
        ? 1f
        : ArmorGradeRules.ScaleStrength(
            ArmorDefinition.StaminaRegenModifier,
            ArmorGrade);
    public float DodgeCostModifier => ArmorDefinition == null
        ? 1f
        : ArmorGradeRules.ScaleCostReduction(
            ArmorDefinition.DodgeCostModifier,
            ArmorGrade);
    public float GuardCostModifier => ArmorDefinition == null
        ? 1f
        : ArmorGradeRules.ScaleCostReduction(
            ArmorDefinition.GuardCostModifier,
            ArmorGrade);
    public int Score => Slot == EquipmentSlot.Weapon
        ? DamageBonus + Definition.Tier * 3
        : HealthBonus + (int)ArmorGrade * 4;

    public EquipmentItem(WeaponDefinition definition, ItemRarity rarity)
        : base(definition.Id, definition.Name, definition.Tier, rarity)
    {
        Definition = definition;
        ArmorGrade = ArmorGrade.C1;
    }

    public EquipmentItem(
        ArmorDefinition definition,
        ItemRarity rarity,
        ArmorGrade armorGrade = ArmorGrade.C1)
        : base(definition.Id, definition.Name, definition.Tier, rarity)
    {
        Definition = definition;
        ArmorGrade = ArmorGradeRules.Clamp(armorGrade);
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
        ArmorGrade = ArmorGrade.C1;
    }
}
