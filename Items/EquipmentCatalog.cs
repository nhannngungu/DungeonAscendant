using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;

namespace DungeonAscendant.Items;

public static class EquipmentCatalog
{
    public const int RewardWeaponSampleCount = 6;
    public const int ArmorSampleCount = 3;
    public const ItemRarity DevelopmentWeaponRarity = ItemRarity.Legendary;

    public static readonly WeaponDefinition KnightLongSword = CreateWeapon(
        "knight-long-sword", "Wayfarer Blade + Vanguard Guard", WeaponFamily.LongSword, 2, 8);
    public static readonly WeaponDefinition DuelistDualSwords = CreateWeapon(
        "duelist-dual-swords", "Astral Twinblades — The Phantom Duelist",
        WeaponFamily.DualSwords, 2, 7);
    public static readonly WeaponDefinition HunterBow = CreateWeapon(
        "hunter-bow", "Bow — Phantom Archer", WeaponFamily.HunterBow, 3, 9);
    public static readonly WeaponDefinition IronVanguardGreatsword = CreateWeapon(
        "iron-vanguard-greatsword", "Greatsword — The Iron Sovereign",
        WeaponFamily.GreatSword, 3, 16);
    public static readonly WeaponDefinition ArcaneWarStaff = CreateWeapon(
        "arcane-war-staff", "Ancient Staff — The Eternal Archmage",
        WeaponFamily.ArcaneWarStaff, 2, 11);
    public static readonly WeaponDefinition ForbiddenSovereignAbyssRing = CreateWeapon(
        "forbidden-sovereign-abyss-ring", "Abyss Ring — The Forbidden Sovereign",
        WeaponFamily.AbyssRing, 5, 18);
    public static readonly WeaponDefinition ShadowReaperScythe = CreateWeapon(
        "shadow-reaper-scythe", "Scythe — The Shadow Reaper",
        WeaponFamily.Scythe, 4, 13);

    private static readonly IReadOnlyList<WeaponDefinition>
        ImplementedWeaponDefinitions = Array.AsReadOnly(new[]
        {
            KnightLongSword,
            DuelistDualSwords,
            IronVanguardGreatsword,
            HunterBow,
            ShadowReaperScythe,
            ArcaneWarStaff,
            ForbiddenSovereignAbyssRing
        });

    public static IReadOnlyList<WeaponDefinition> AllImplementedWeapons =>
        ImplementedWeaponDefinitions;
    public static int WeaponSampleCount => ImplementedWeaponDefinitions.Count;

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

    public static IReadOnlyList<EquipmentItem> CreateDevelopmentArmorInventory()
    {
        return new EquipmentItem[]
        {
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
            "knight-long-sword" or "rusted-long-sword" => KnightLongSword,
            "duelist-dual-swords" or "twin-daggers" => DuelistDualSwords,
            "hunter-bow" or "hunter-spear" => HunterBow,
            "arcane-war-staff" or "apprentice-arcane-staff" => ArcaneWarStaff,
            "iron-vanguard-greatsword" or "iron-great-sword" or
                "executioner-great-sword" => IronVanguardGreatsword,
            "forbidden-sovereign-abyss-ring" or "abyss-ring" =>
                ForbiddenSovereignAbyssRing,
            "shadow-reaper-scythe" or "scythe" => ShadowReaperScythe,
            _ => null
        };
        return definition != null;
    }

    public static bool IsRemovedWeaponId(string id)
    {
        return id is "war-axe" or "raider-axe" or
            "breaker-spiked-mace" or "reckoner-chain-flail";
    }

    public static WeaponDefinition ResolveSavedWeaponId(string id)
    {
        return TryResolveSavedWeaponId(id, out WeaponDefinition definition)
            ? definition
            : KnightLongSword;
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
                WeaponTechniqueCatalog.AstralDuelist),
            WeaponFamily.HunterBow => Definition(id, name, family, tier, damageBonus,
                1.08f, 1f, 1.08f, .78f, .82f, false, 0f,
                "Mobile ranged pressure, evasion, and precision counters", WeaponMoveSets.Bow,
                WeaponTechniqueCatalog.Ranger),
            WeaponFamily.ArcaneWarStaff => Definition(id, name, family, tier, damageBonus,
                .94f, 1f, 1.10f, 1.05f, 1.12f, false, .22f,
                "Rune magic, summoning, space control, and thunder", WeaponMoveSets.ArcaneStaff,
                WeaponTechniqueCatalog.Spellblade),
            WeaponFamily.GreatSword => Definition(id, name, family, tier, damageBonus,
                .84f, 1f, 1.12f, 1.18f, 1.48f, false, .42f,
                "Deliberate blade momentum and frontal control",
                WeaponMoveSets.IronVanguard, WeaponTechniqueCatalog.IronVanguard),
            WeaponFamily.AbyssRing => Definition(id, name, family, tier, damageBonus,
                1.18f, .92f, 1.10f, .82f, 1.06f, false, .78f,
                "Dimensional martial arts and validated portal transit",
                WeaponMoveSets.AbyssRing, WeaponTechniqueCatalog.Sovereign),
            WeaponFamily.Scythe => Definition(id, name, family, tier, damageBonus,
                1.16f, .92f, 1.08f, .72f, .88f, false, .66f,
                "Fast rotational pressure, evasive regrips, and shadow momentum",
                WeaponMoveSets.Scythe, WeaponTechniqueCatalog.Scythe),
            WeaponFamily.LongSword => Definition(id, name,
                WeaponFamily.LongSword, tier, damageBonus,
                1f, 1f, 1f, 1f, 1f, true, .35f,
                "Shield-first discipline and committed sword counterplay",
                WeaponMoveSets.KnightSwordAndShield,
                WeaponTechniqueCatalog.KnightSwordAndShield),
            _ => throw new InvalidOperationException(
                $"Weapon '{id}' has no canonical combat profile for {family}.")
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
