using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;

namespace DungeonAscendant.Items;

public static class EquipmentCatalog
{
    public const int WeaponSampleCount = 9;
    public const int RewardWeaponSampleCount = 8;
    public const int ArmorSampleCount = 3;
    public const ItemRarity DevelopmentWeaponRarity = ItemRarity.Legendary;

    public static readonly WeaponDefinition RustedLongSword = CreateWeapon(
        "rusted-long-sword", "Rusted Long Sword + Shield", WeaponFamily.LongSword, 1, 4);
    public static readonly WeaponDefinition KnightLongSword = CreateWeapon(
        "knight-long-sword", "Knight Long Sword + Shield", WeaponFamily.LongSword, 2, 8);
    public static readonly WeaponDefinition DuelistDualSwords = CreateWeapon(
        "duelist-dual-swords", "Duelist Dual Swords", WeaponFamily.DualSwords, 2, 7);
    public static readonly WeaponDefinition HunterBow = CreateWeapon(
        "hunter-bow", "Bow — Phantom Archer", WeaponFamily.HunterBow, 3, 9);
    public static readonly WeaponDefinition IronVanguardGreatsword = CreateWeapon(
        "iron-vanguard-greatsword", "Greatsword — The Iron Sovereign",
        WeaponFamily.GreatSword, 3, 16);
    public static readonly WeaponDefinition RaiderWarAxe = CreateWeapon(
        "war-axe", "War Axe + Buckler", WeaponFamily.WarAxe, 3, 16);
    public static readonly WeaponDefinition ArcaneWarStaff = CreateWeapon(
        "arcane-war-staff", "Ancient Staff — The Eternal Archmage",
        WeaponFamily.ArcaneWarStaff, 2, 11);
    public static readonly WeaponDefinition BreakerSpikedMace = CreateWeapon(
        "breaker-spiked-mace", "Breaker Spiked Mace", WeaponFamily.SpikedMace, 3, 17);
    public static readonly WeaponDefinition ReckonerChainFlail = CreateWeapon(
        "reckoner-chain-flail", "Reckoner Chain Flail", WeaponFamily.ChainFlail, 3, 14);
    public static readonly WeaponDefinition ForbiddenSovereignAbyssRing = CreateWeapon(
        "forbidden-sovereign-abyss-ring", "Abyss Ring — The Forbidden Sovereign",
        WeaponFamily.AbyssRing, 5, 18);

    private static readonly IReadOnlyList<WeaponDefinition>
        ImplementedWeaponDefinitions = Array.AsReadOnly(new[]
        {
            KnightLongSword,
            DuelistDualSwords,
            IronVanguardGreatsword,
            RaiderWarAxe,
            HunterBow,
            BreakerSpikedMace,
            ReckonerChainFlail,
            ArcaneWarStaff,
            ForbiddenSovereignAbyssRing
        });

    public static IReadOnlyList<WeaponDefinition> AllImplementedWeapons =>
        ImplementedWeaponDefinitions;

    [Obsolete("Use IronVanguardGreatsword.")]
    public static WeaponDefinition IronGreatSword => IronVanguardGreatsword;
    [Obsolete("Use IronVanguardGreatsword.")]
    public static WeaponDefinition ExecutionerGreatSword => IronVanguardGreatsword;
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
            CreateDevelopmentWeapon(0),
            CreateDevelopmentWeapon(1),
            CreateDevelopmentWeapon(2),
            CreateDevelopmentWeapon(3),
            CreateDevelopmentWeapon(4),
            CreateDevelopmentWeapon(5),
            CreateDevelopmentWeapon(6),
            CreateDevelopmentWeapon(7),
            CreateDevelopmentWeapon(8),
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
        index = Math.Clamp(index, 0, ImplementedWeaponDefinitions.Count - 1);
        WeaponDefinition definition = ImplementedWeaponDefinitions[index];
        ItemRarity rarity = definition.Family == WeaponFamily.AbyssRing
            ? ItemRarity.SecretMythic
            : definition.Tier >= 3
            ? ItemRarity.Rare
            : ItemRarity.Uncommon;
        return new EquipmentItem(definition, rarity);
    }

    public static EquipmentItem CreateDevelopmentWeapon(int index)
    {
        index = Math.Clamp(index, 0, ImplementedWeaponDefinitions.Count - 1);
        WeaponDefinition definition = ImplementedWeaponDefinitions[index];
        return new EquipmentItem(
            definition,
            definition.Family == WeaponFamily.AbyssRing
                ? ItemRarity.SecretMythic
                : DevelopmentWeaponRarity);
    }

    public static int GetImplementedWeaponOrder(string weaponId)
    {
        for (int index = 0; index < ImplementedWeaponDefinitions.Count; index++)
        {
            if (ImplementedWeaponDefinitions[index].WeaponId == weaponId)
                return index;
        }

        return int.MaxValue;
    }

    public static bool TryResolveSavedWeaponId(
        string id,
        out WeaponDefinition definition)
    {
        definition = id switch
        {
            "rusted-long-sword" => RustedLongSword,
            "knight-long-sword" => KnightLongSword,
            "duelist-dual-swords" or "twin-daggers" => DuelistDualSwords,
            "hunter-bow" or "hunter-spear" => HunterBow,
            "war-axe" or "raider-axe" => RaiderWarAxe,
            "arcane-war-staff" or "apprentice-arcane-staff" => ArcaneWarStaff,
            "iron-vanguard-greatsword" or "iron-great-sword" or
                "executioner-great-sword" => IronVanguardGreatsword,
            "breaker-spiked-mace" => BreakerSpikedMace,
            "reckoner-chain-flail" => ReckonerChainFlail,
            "forbidden-sovereign-abyss-ring" or "abyss-ring" =>
                ForbiddenSovereignAbyssRing,
            _ => null
        };
        return definition != null;
    }

    public static WeaponDefinition ResolveSavedWeaponId(string id)
    {
        return TryResolveSavedWeaponId(id, out WeaponDefinition definition)
            ? definition
            : KnightLongSword;
    }

    public static WeaponDefinition RestoreWeaponDefinition(
        string weaponId,
        string name,
        WeaponFamily weaponType,
        int tier,
        int damageBonus)
    {
        if (TryResolveSavedWeaponId(weaponId, out WeaponDefinition canonical))
            return canonical;

        return CreateGeneratedWeapon(
            weaponId,
            name,
            weaponType,
            tier,
            damageBonus);
    }

    public static ArmorDefinition ResolveSavedArmorId(string id)
    {
        return id switch
        {
            "scout-armor" => ScoutArmor,
            "fortress-armor" => FortressArmor,
            _ => KnightArmor
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
        return CreateGeneratedWeapon(
            $"generated-{familyId}-t{Math.Max(1, tier)}",
            $"Dungeon {family.ToDisplayName()}",
            family,
            tier,
            damageBonus);
    }

    public static WeaponDefinition CreateGeneratedWeapon(
        string weaponId,
        string name,
        WeaponFamily family,
        int tier,
        int damageBonus)
    {
        family = family.ToOfficialFamily();
        string stableId = string.IsNullOrWhiteSpace(weaponId)
            ? $"generated-{family.ToString().ToLowerInvariant()}-t{Math.Max(1, tier)}"
            : weaponId;
        string displayName = string.IsNullOrWhiteSpace(name)
            ? $"Dungeon {family.ToDisplayName()}"
            : name;
        return CreateWeapon(stableId, displayName, family, tier, damageBonus);
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
                1.08f, 1f, 1.08f, .78f, .82f, false, 0f,
                "Mobile ranged pressure, evasion, and precision counters", WeaponMoveSets.Bow,
                WeaponTechniqueCatalog.Ranger),
            WeaponFamily.WarAxe => Definition(id, name, family, tier, damageBonus,
                .88f, 1f, .98f, 1.34f, 1.35f, true, .25f,
                "Aggressive guard breaking", WeaponMoveSets.BattleAxe,
                WeaponTechniqueCatalog.Raider),
            WeaponFamily.ArcaneWarStaff => Definition(id, name, family, tier, damageBonus,
                .94f, 1f, 1.10f, 1.05f, 1.12f, false, .22f,
                "Rune magic, summoning, space control, and thunder", WeaponMoveSets.ArcaneStaff,
                WeaponTechniqueCatalog.Spellblade),
            WeaponFamily.SpikedMace => Definition(id, name, family, tier, damageBonus,
                .72f, 1f, 1f, 1.55f, 1.72f, false, .12f,
                "Committed poise destruction", WeaponMoveSets.GreatSword,
                WeaponTechniqueCatalog.Breaker),
            WeaponFamily.ChainFlail => Definition(id, name, family, tier, damageBonus,
                .92f, 1f, 1.25f, 1.18f, 1.14f, false, .30f,
                "Procedural mid-range chain control", WeaponMoveSets.Spear,
                WeaponTechniqueCatalog.Reckoner),
            WeaponFamily.GreatSword => Definition(id, name, family, tier, damageBonus,
                .84f, 1f, 1.12f, 1.18f, 1.48f, false, .42f,
                "Deliberate blade momentum and frontal control",
                WeaponMoveSets.IronVanguard, WeaponTechniqueCatalog.IronVanguard),
            WeaponFamily.AbyssRing => Definition(id, name, family, tier, damageBonus,
                1.18f, .92f, 1.10f, .82f, 1.06f, false, .78f,
                "Dimensional martial arts and validated portal transit",
                WeaponMoveSets.AbyssRing, WeaponTechniqueCatalog.Sovereign),
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
