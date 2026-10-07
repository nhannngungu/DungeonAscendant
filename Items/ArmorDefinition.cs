using System;

namespace DungeonAscendant.Items;

public sealed class ArmorDefinition : EquipmentDefinition
{
    public ArmorClass Class { get; }
    public int MaxHealthBonus { get; }
    public float DamageTakenMultiplier { get; }
    public float MoveSpeedModifier { get; }
    public float StaminaRegenModifier { get; }
    public float DodgeCostModifier { get; }
    public float GuardCostModifier { get; }

    public ArmorDefinition(
        string id,
        string name,
        ArmorClass armorClass,
        int tier,
        int maxHealthBonus,
        float damageTakenMultiplier,
        float moveSpeedModifier,
        float staminaRegenModifier,
        float dodgeCostModifier,
        float guardCostModifier)
        : base(id, name, tier, EquipmentSlot.Armor)
    {
        Class = armorClass;
        MaxHealthBonus = Math.Max(0, maxHealthBonus);
        DamageTakenMultiplier = Math.Clamp(damageTakenMultiplier, 0.1f, 2f);
        MoveSpeedModifier = Math.Clamp(moveSpeedModifier, 0.25f, 2f);
        StaminaRegenModifier = Math.Clamp(staminaRegenModifier, 0.1f, 3f);
        DodgeCostModifier = Math.Clamp(dodgeCostModifier, 0.1f, 3f);
        GuardCostModifier = Math.Clamp(guardCostModifier, 0.1f, 3f);
    }
}
