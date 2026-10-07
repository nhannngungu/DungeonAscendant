using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;

namespace DungeonAscendant.Items;

public static class EquipmentCatalog
{
    public const int WeaponSampleCount = 7;
    public const int ArmorSampleCount = 3;

    public static readonly WeaponDefinition RustedLongSword = CreateWeapon(
        "rusted-long-sword", "Rusted Long Sword", WeaponFamily.LongSword, 1, 4);
    public static readonly WeaponDefinition KnightLongSword = CreateWeapon(
        "knight-long-sword", "Knight Long Sword", WeaponFamily.LongSword, 2, 8);
    public static readonly WeaponDefinition IronGreatSword = CreateWeapon(
        "iron-great-sword", "Iron Great Sword", WeaponFamily.GreatSword, 2, 12);
    public static readonly WeaponDefinition ExecutionerGreatSword = CreateWeapon(
        "executioner-great-sword", "Executioner Great Sword", WeaponFamily.GreatSword, 3, 18);
    public static readonly WeaponDefinition RaiderAxe = CreateWeapon(
        "raider-axe", "Raider Axe", WeaponFamily.BattleAxe, 1, 9);
    public static readonly WeaponDefinition WarAxe = CreateWeapon(
        "war-axe", "War Axe", WeaponFamily.BattleAxe, 3, 16);
    public static readonly WeaponDefinition HunterSpear = CreateWeapon(
        "hunter-spear", "Hunter Spear", WeaponFamily.Spear, 2, 9);
    public static readonly WeaponDefinition TwinDaggers = CreateWeapon(
        "twin-daggers", "Twin Daggers", WeaponFamily.DualDaggers, 2, 7);
    public static readonly WeaponDefinition HunterBow = CreateWeapon(
        "hunter-bow", "Hunter Bow", WeaponFamily.Bow, 2, 8);
    public static readonly WeaponDefinition ApprenticeArcaneStaff = CreateWeapon(
        "apprentice-arcane-staff", "Apprentice Arcane Staff", WeaponFamily.ArcaneStaff, 2, 11);

    public static readonly ArmorDefinition ScoutArmor = new(
        "scout-armor", "Scout Armor", ArmorClass.Light, 1, 5,
        .96f, 1.08f, 1.20f, .86f, 1.08f);
    public static readonly ArmorDefinition KnightArmor = new(
        "knight-armor", "Knight Armor", ArmorClass.Medium, 2, 20,
        .86f, 1f, 1f, 1f, .90f);
    public static readonly ArmorDefinition FortressArmor = new(
        "fortress-armor", "Fortress Armor", ArmorClass.Heavy, 3, 45,
        .72f, .86f, .80f, 1.18f, .72f);

    public static EquipmentItem CreateStartingWeapon() =>
        new(KnightLongSword, ItemRarity.Uncommon);

    public static EquipmentItem CreateStartingArmor() =>
        new(KnightArmor, ItemRarity.Uncommon);

    public static IReadOnlyList<EquipmentItem> CreateSampleInventory()
    {
        return new EquipmentItem[]
        {
            new(KnightLongSword, ItemRarity.Uncommon),
            new(IronGreatSword, ItemRarity.Common),
            new(WarAxe, ItemRarity.Rare),
            new(HunterSpear, ItemRarity.Uncommon),
            new(TwinDaggers, ItemRarity.Uncommon),
            new(HunterBow, ItemRarity.Uncommon),
            new(ApprenticeArcaneStaff, ItemRarity.Rare),
            new(ScoutArmor, ItemRarity.Common),
            new(KnightArmor, ItemRarity.Uncommon),
            new(FortressArmor, ItemRarity.Rare)
        };
    }

    public static EquipmentItem CreateWeaponSample(int index)
    {
        return index switch
        {
            0 => new EquipmentItem(KnightLongSword, ItemRarity.Uncommon),
            1 => new EquipmentItem(IronGreatSword, ItemRarity.Common),
            2 => new EquipmentItem(WarAxe, ItemRarity.Rare),
            3 => new EquipmentItem(HunterSpear, ItemRarity.Uncommon),
            4 => new EquipmentItem(TwinDaggers, ItemRarity.Uncommon),
            5 => new EquipmentItem(HunterBow, ItemRarity.Uncommon),
            _ => new EquipmentItem(ApprenticeArcaneStaff, ItemRarity.Rare)
        };
    }

    public static EquipmentItem CreateArmorSample(int index)
    {
        return index switch
        {
            0 => new EquipmentItem(ScoutArmor, ItemRarity.Common),
            1 => new EquipmentItem(KnightArmor, ItemRarity.Uncommon),
            _ => new EquipmentItem(FortressArmor, ItemRarity.Rare)
        };
    }

    public static WeaponDefinition CreateGeneratedWeapon(
        WeaponFamily family,
        int tier,
        int damageBonus)
    {
        string familyId = family.ToDisplayName()
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();
        return CreateWeapon(
            $"generated-{familyId}-t{Math.Max(1, tier)}",
            $"Dungeon {family.ToDisplayName()}",
            family,
            tier,
            damageBonus);
    }

    public static ArmorDefinition CreateGeneratedArmor(
        ArmorClass armorClass,
        int tier,
        int healthBonus)
    {
        return armorClass switch
        {
            ArmorClass.Light => new ArmorDefinition(
                $"generated-light-t{tier}", "Dungeon Scout Armor",
                armorClass, tier, healthBonus, .96f, 1.08f, 1.20f, .86f, 1.08f),
            ArmorClass.Heavy => new ArmorDefinition(
                $"generated-heavy-t{tier}", "Dungeon Fortress Armor",
                armorClass, tier, healthBonus, .72f, .86f, .80f, 1.18f, .72f),
            _ => new ArmorDefinition(
                $"generated-medium-t{tier}", "Dungeon Knight Armor",
                ArmorClass.Medium, tier, healthBonus, .86f, 1f, 1f, 1f, .90f)
        };
    }

    private static WeaponDefinition CreateWeapon(
        string id,
        string name,
        WeaponFamily family,
        int tier,
        int damageBonus)
    {
        return family switch
        {
            WeaponFamily.GreatSword => Definition(id, name, family, tier, damageBonus,
                .72f, 1.10f, 1.05f, 1.30f, 1.35f, false, .12f,
                "Massive overhead smash", WeaponMoveSets.GreatSword),
            WeaponFamily.BattleAxe => Definition(id, name, family, tier, damageBonus,
                .84f, 1.05f, .96f, 1.35f, 1.45f, false, .22f,
                "Execution downward chop", WeaponMoveSets.BattleAxe),
            WeaponFamily.Spear => Definition(id, name, family, tier, damageBonus,
                1.05f, .95f, 1.12f, .85f, 1.05f, false, .28f,
                "Committed long lunge", WeaponMoveSets.Spear),
            WeaponFamily.DualDaggers => Definition(id, name, family, tier, damageBonus,
                1.30f, .80f, .90f, .60f, .70f, false, .58f,
                "Rapid crossing burst", WeaponMoveSets.DualDaggers),
            WeaponFamily.Bow => Definition(id, name, family, tier, damageBonus,
                1f, .90f, 1f, .80f, 1f, false, 0f,
                "Charged piercing arrow", WeaponMoveSets.Bow),
            WeaponFamily.ArcaneStaff => Definition(id, name, family, tier, damageBonus,
                .82f, 1.10f, 1f, 1.10f, 1.25f, false, .08f,
                "Charged arcane bolt", WeaponMoveSets.ArcaneStaff),
            _ => Definition(id, name, WeaponFamily.LongSword, tier, damageBonus,
                1f, 1f, 1f, 1f, 1f, true, .35f,
                "Precise committed thrust", WeaponMoveSets.LongSword)
        };
    }

    private static WeaponDefinition Definition(
        string id, string name, WeaponFamily family, int tier, int damageBonus,
        float speed, float stamina, float range, float knockback, float poise,
        bool shield, float movement, string heavy, WeaponMoveSet moveSet)
    {
        return new WeaponDefinition(
            id, name, family, tier, damageBonus, speed, stamina, range,
            knockback, poise, shield, movement, heavy, moveSet);
    }
}
