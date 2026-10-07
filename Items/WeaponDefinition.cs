using System;
using DungeonAscendant.Combat;

namespace DungeonAscendant.Items;

public sealed class WeaponDefinition : EquipmentDefinition
{
    public WeaponFamily Family { get; }
    public int DamageBonus { get; }
    public float AttackSpeedModifier { get; }
    public float StaminaCostModifier { get; }
    public float RangeModifier { get; }
    public float KnockbackModifier { get; }
    public float PoiseDamageModifier { get; }
    public bool UsesShield { get; }
    public float AttackMovementMultiplier { get; }
    public string HeavyAttackProfile { get; }
    public WeaponMoveSet MoveSet { get; }

    public WeaponDefinition(
        string id,
        string name,
        WeaponFamily family,
        int tier,
        int damageBonus,
        float attackSpeedModifier,
        float staminaCostModifier,
        float rangeModifier,
        float knockbackModifier,
        float poiseDamageModifier,
        bool usesShield,
        float attackMovementMultiplier,
        string heavyAttackProfile,
        WeaponMoveSet moveSet)
        : base(id, name, tier, EquipmentSlot.Weapon)
    {
        Family = family;
        DamageBonus = Math.Max(0, damageBonus);
        AttackSpeedModifier = MathF.Max(0.1f, attackSpeedModifier);
        StaminaCostModifier = MathF.Max(0f, staminaCostModifier);
        RangeModifier = MathF.Max(0.1f, rangeModifier);
        KnockbackModifier = MathF.Max(0f, knockbackModifier);
        PoiseDamageModifier = MathF.Max(0f, poiseDamageModifier);
        UsesShield = usesShield;
        AttackMovementMultiplier = Math.Clamp(attackMovementMultiplier, 0f, 1f);
        HeavyAttackProfile = heavyAttackProfile ?? string.Empty;
        MoveSet = moveSet ?? WeaponMoveSets.LongSword;
    }
}
