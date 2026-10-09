using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;

namespace DungeonAscendant.Items;

public static class EquipmentCatalog
{
    public const int WeaponSampleCount = 7;
    public const int ArmorSampleCount = 3;

    public static readonly WeaponDefinition RustedLongSword = CreateWeapon(
        "rusted-long-sword", "Rusted Long Sword + Shield", WeaponFamily.LongSword, 1, 4);
    public static readonly WeaponDefinition KnightLongSword = CreateWeapon(
        "knight-long-sword", "Knight Long Sword + Shield", WeaponFamily.LongSword, 2, 8);
    public static readonly WeaponDefinition DuelistDualSwords = CreateWeapon(
        "duelist-dual-swords", "Duelist Dual Swords", WeaponFamily.DualSwords, 2, 7);
    public static readonly WeaponDefinition HunterBow = CreateWeapon(
        "hunter-bow", "Hunter Bow", WeaponFamily.HunterBow, 2, 8);
    public static readonly WeaponDefinition RaiderWarAxe = CreateWeapon(
        "war-axe", "War Axe + Buckler", WeaponFamily.WarAxe, 3, 16);
    public static readonly WeaponDefinition ArcaneWarStaff = CreateWeapon(
        "arcane-war-staff", "Arcane War Staff", WeaponFamily.ArcaneWarStaff, 2, 11);
    public static readonly WeaponDefinition BreakerSpikedMace = CreateWeapon(
        "breaker-spiked-mace", "Breaker Spiked Mace", WeaponFamily.SpikedMace, 3, 17);
    public static readonly WeaponDefinition ReckonerChainFlail = CreateWeapon(
        "reckoner-chain-flail", "Reckoner Chain Flail", WeaponFamily.ChainFlail, 3, 14);

    [Obsolete("Use BreakerSpikedMace.")]
    public static WeaponDefinition IronGreatSword => BreakerSpikedMace;
    [Obsolete("Use BreakerSpikedMace.")]
    public static WeaponDefinition ExecutionerGreatSword => BreakerSpikedMace;
    [Obsolete("Use RaiderWarAxe.")]
    public static WeaponDefinition RaiderAxe => RaiderWarAxe;
    [Obsolete("Use RaiderWarAxe.")]
    public static WeaponDefinition WarAxe => RaiderWarAxe;
    [Obsolete("Use HunterBow.")]
    public static WeaponDefinition HunterSpear => HunterBow;
    [Obsolete("Use DuelistDualSwords.")]
    public static WeaponDefinition TwinDaggers => DuelistDualSwords;
    [Obsolete("Use ArcaneWarStaff.")]
    public static WeaponDefinition ApprenticeArcaneStaff => ArcaneWarStaff;

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
            new(DuelistDualSwords, ItemRarity.Uncommon),
            new(HunterBow, ItemRarity.Uncommon),
            new(RaiderWarAxe, ItemRarity.Rare),
            new(ArcaneWarStaff, ItemRarity.Rare),
            new(BreakerSpikedMace, ItemRarity.Rare),
            new(ReckonerChainFlail, ItemRarity.Rare),
            new(ScoutArmor, ItemRarity.Common),
            new(ScoutArmor, ItemRarity.Common),
            new(ScoutArmor, ItemRarity.Common),
            new(KnightArmor, ItemRarity.Uncommon),
            new(KnightArmor, ItemRarity.Uncommon),
            new(KnightArmor, ItemRarity.Uncommon),
            new(FortressArmor, ItemRarity.Rare),
            new(FortressArmor, ItemRarity.Rare),
            new(FortressArmor, ItemRarity.Rare)
        };
    }

    public static EquipmentItem CreateWeaponSample(int index)
    {
        return index switch
        {
            0 => new EquipmentItem(KnightLongSword, ItemRarity.Uncommon),
            1 => new EquipmentItem(DuelistDualSwords, ItemRarity.Uncommon),
            2 => new EquipmentItem(HunterBow, ItemRarity.Uncommon),
            3 => new EquipmentItem(RaiderWarAxe, ItemRarity.Rare),
            4 => new EquipmentItem(ArcaneWarStaff, ItemRarity.Rare),
            5 => new EquipmentItem(BreakerSpikedMace, ItemRarity.Rare),
            _ => new EquipmentItem(ReckonerChainFlail, ItemRarity.Rare)
        };
    }

    public static WeaponDefinition ResolveSavedWeaponId(string id)
    {
        return id switch
        {
            "rusted-long-sword" => RustedLongSword,
            "knight-long-sword" => KnightLongSword,
            "duelist-dual-swords" or "twin-daggers" => DuelistDualSwords,
            "hunter-bow" or "hunter-spear" => HunterBow,
            "war-axe" or "raider-axe" => RaiderWarAxe,
            "arcane-war-staff" or "apprentice-arcane-staff" => ArcaneWarStaff,
            "breaker-spiked-mace" or "iron-great-sword" or
                "executioner-great-sword" => BreakerSpikedMace,
            "reckoner-chain-flail" => ReckonerChainFlail,
            _ => KnightLongSword
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
        family = family.ToOfficialFamily();
        string familyId = family.ToDisplayName()
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace("+", "", StringComparison.Ordinal)
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
        family = family.ToOfficialFamily();
        return family switch
        {
            WeaponFamily.DualSwords => Definition(id, name, family, tier, damageBonus,
                1.24f, 1f, .88f, .65f, .72f, false, .62f,
                "Continuous asymmetric blade pressure", WeaponMoveSets.DualDaggers,
                WeaponTechniqueCatalog.Duelist),
            WeaponFamily.HunterBow => Definition(id, name, family, tier, damageBonus,
                1f, 1f, 1f, .82f, 1f, false, 0f,
                "Patient precision shooting", WeaponMoveSets.Bow,
                WeaponTechniqueCatalog.Ranger),
            WeaponFamily.WarAxe => Definition(id, name, family, tier, damageBonus,
                .88f, 1f, .98f, 1.34f, 1.35f, true, .25f,
                "Aggressive guard breaking", WeaponMoveSets.BattleAxe,
                WeaponTechniqueCatalog.Raider),
            WeaponFamily.ArcaneWarStaff => Definition(id, name, family, tier, damageBonus,
                .94f, 1f, 1.10f, 1.05f, 1.12f, false, .22f,
                "Physical strikes into arcane release", WeaponMoveSets.ArcaneStaff,
                WeaponTechniqueCatalog.Spellblade),
            WeaponFamily.SpikedMace => Definition(id, name, family, tier, damageBonus,
                .72f, 1f, 1f, 1.55f, 1.72f, false, .12f,
                "Committed poise destruction", WeaponMoveSets.GreatSword,
                WeaponTechniqueCatalog.Breaker),
            WeaponFamily.ChainFlail => Definition(id, name, family, tier, damageBonus,
                .92f, 1f, 1.25f, 1.18f, 1.14f, false, .30f,
                "Procedural mid-range chain control", WeaponMoveSets.Spear,
                WeaponTechniqueCatalog.Reckoner),
            _ => Definition(id, name, WeaponFamily.LongSword, tier, damageBonus,
                1f, 1f, 1f, 1f, 1f, true, .35f,
                "Balanced defense and counterplay", WeaponMoveSets.LongSword,
                WeaponTechniqueCatalog.Knight)
        };
    }

    private static WeaponDefinition Definition(
        string id, string name, WeaponFamily family, int tier, int damageBonus,
        float speed, float stamina, float range, float knockback, float poise,
        bool shield, float movement, string heavy, WeaponMoveSet moveSet,
        WeaponCombatProfile combatProfile)
    {
        return new WeaponDefinition(
            id, name, family, tier, damageBonus, speed, stamina, range,
            knockback, poise, shield, movement, heavy, moveSet, combatProfile);
    }
}
