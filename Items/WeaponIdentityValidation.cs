using System;
using System.Collections.Generic;
using System.IO;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Persistence;
using DungeonAscendant.Player;
using DungeonAscendant.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Items;

public static class WeaponIdentityValidation
{
    public static void ValidateOrThrow()
    {
        ValidateCatalogAndGrant();
        ValidateEquipAndOrdering();
        ValidateRuntimeSeparation();
        ValidatePersistenceAndSlotIsolation();
        ValidateRewardFactories();
        ValidateDeathRestartAndMapChanges();
    }

    private static void ValidateCatalogAndGrant()
    {
        string[] expectedIds =
        {
            "knight-long-sword",
            "duelist-dual-swords",
            "iron-vanguard-greatsword",
            "hunter-bow",
            "shadow-reaper-scythe",
            "arcane-war-staff",
            "forbidden-sovereign-abyss-ring"
        };
        Require(EquipmentCatalog.AllImplementedWeapons.Count ==
            EquipmentCatalog.WeaponSampleCount &&
            EquipmentCatalog.WeaponSampleCount == expectedIds.Length,
            "catalog count does not match sample count");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var types = new HashSet<WeaponFamily>();
        for (int index = 0; index < expectedIds.Length; index++)
        {
            WeaponDefinition definition =
                EquipmentCatalog.AllImplementedWeapons[index];
            Require(definition.WeaponId == expectedIds[index],
                $"canonical roster mismatch at index {index}");
            Require(ids.Add(definition.WeaponId),
                $"duplicate canonical WeaponId {definition.WeaponId}");
            Require(types.Add(definition.WeaponType),
                $"duplicate canonical WeaponType {definition.WeaponType}");
        }

        var player = new PlayerCharacter(Vector2.Zero);
        Require(player.CountOwnedImplementedWeapons() ==
            EquipmentCatalog.WeaponSampleCount,
            "test loadout does not own every implemented weapon");
        foreach (WeaponDefinition definition in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            EquipmentItem owned = FindOwnedWeapon(player, definition.WeaponId);
            ItemRarity expected = definition.Family == WeaponFamily.AbyssRing
                ? ItemRarity.SecretMythic
                : EquipmentCatalog.DevelopmentWeaponRarity;
            Require(owned?.Rarity == expected,
                $"development weapon {definition.WeaponId} is not orange grade");
        }
        Require(CountOwnedWeaponId(player, EquipmentCatalog.KnightLongSword.WeaponId) == 1,
            "starting weapon was duplicated by the test inventory");
        int before = player.Inventory.Count;
        Require(player.GrantAllWeapons() == 0 &&
            player.GrantAllWeapons() == 0 &&
            player.Inventory.Count == before,
            "Grant All Weapons is not idempotent");
        Require(EquipmentCatalog.ResolveSavedWeaponId("rusted-long-sword") ==
            EquipmentCatalog.KnightLongSword,
            "obsolete starting sword did not migrate to Knight");
		WeaponDefinition knight = EquipmentCatalog.KnightLongSword;
		Require(knight.WeaponId == "knight-long-sword" &&
			knight.Family == WeaponFamily.LongSword &&
			ReferenceEquals(
				knight.CombatProfile,
				WeaponTechniqueCatalog.KnightSwordAndShield) &&
			ReferenceEquals(
				knight.MoveSet,
				WeaponMoveSets.KnightSwordAndShield),
			"Knight identity is not bound to the current Sword & Shield profile");
		var legacyKnightSlot = new SaveSlotData
		{
			EquippedWeaponId = "rusted-long-sword"
		};
		EquipmentItem migratedKnight =
			CharacterSelectState.CreateWeapon(legacyKnightSlot);
		Require(migratedKnight.WeaponId == knight.WeaponId &&
			ReferenceEquals(migratedKnight.WeaponDefinition, knight),
			"legacy Long Sword save did not resolve to the canonical Knight definition");

        EquipmentItem removed = FindInventoryWeapon(
            player,
            EquipmentCatalog.HunterBow.WeaponId);
        Require(removed != null && player.Inventory.Remove(removed),
            "could not create Grant All Weapons fixture");
        Require(player.GrantAllWeapons() == 1 &&
            player.CountOwnedImplementedWeapons() ==
                EquipmentCatalog.WeaponSampleCount &&
            player.GrantAllWeapons() == 0,
            "Grant All Weapons did not restore exactly one missing identity");

        var legacyPlayer = new PlayerCharacter(Vector2.Zero);
        legacyPlayer.RestoreSaveState(
            1,
            0,
            EquipmentCatalog.CreateDevelopmentWeapon(0),
            EquipmentCatalog.CreateStartingArmor());
        var legacyInventory = new List<EquipmentItem>();
        for (int index = 1;
             index < EquipmentCatalog.AllImplementedWeapons.Count;
             index++)
        {
            if (EquipmentCatalog.AllImplementedWeapons[index].WeaponId !=
                EquipmentCatalog.ShadowReaperScythe.WeaponId)
            {
                legacyInventory.Add(
                    EquipmentCatalog.CreateDevelopmentWeapon(index));
            }
        }
        legacyPlayer.Inventory.Restore(legacyInventory);
        Require(legacyPlayer.NormalizeDevelopmentTestWeapons(),
            "legacy development loadout was not recognized");
        foreach (WeaponDefinition definition in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            EquipmentItem owned = FindOwnedWeapon(
                legacyPlayer,
                definition.WeaponId);
            ItemRarity expected = definition.Family == WeaponFamily.AbyssRing
                ? ItemRarity.SecretMythic
                : EquipmentCatalog.DevelopmentWeaponRarity;
            Require(owned?.Rarity == expected &&
                CountOwnedWeaponId(legacyPlayer, definition.WeaponId) == 1,
                $"legacy loadout did not normalize {definition.WeaponId}");
        }
    }

    private static void ValidateEquipAndOrdering()
    {
        var player = new PlayerCharacter(Vector2.Zero);
        var reordered = new List<EquipmentItem>();
        foreach (EquipmentItem item in player.Inventory.Items)
        {
            if (item.WeaponDefinition != null)
                reordered.Add(item);
        }
        reordered.Sort((left, right) =>
            string.CompareOrdinal(right.WeaponId, left.WeaponId));
        player.Inventory.Restore(reordered);

        foreach (EquipmentItem item in reordered)
        {
            string weaponId = item.WeaponId;
            WeaponFamily weaponType = item.WeaponType.Value;
            Require(player.EquipItem(item), $"could not equip {weaponId}");
            Require(ReferenceEquals(player.EquippedItems.Weapon, item) &&
                player.EquippedWeapon.WeaponId == weaponId &&
                player.WeaponFamily == weaponType,
                $"equip mutated {weaponId} after inventory reordering");
        }

        foreach (WeaponDefinition definition in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            Require(CountOwnedWeaponId(player, definition.WeaponId) == 1,
                $"equip swapping duplicated or lost {definition.WeaponId}");
        }

        for (int cycle = 0; cycle < 5; cycle++)
        {
            foreach (WeaponDefinition definition in
                     EquipmentCatalog.AllImplementedWeapons)
            {
                EquipmentItem item = FindOwnedWeapon(player, definition.WeaponId);
                Require(item != null, $"swap cycle lost {definition.WeaponId}");
                if (!ReferenceEquals(player.EquippedItems.Weapon, item))
                    Require(player.EquipItem(item),
                        $"swap cycle could not equip {definition.WeaponId}");
                Require(player.EquippedWeapon.WeaponId == definition.WeaponId &&
                    player.EquippedWeapon.WeaponType == definition.WeaponType &&
                    ReferenceEquals(
                        player.EquippedWeapon.CombatProfile,
                        definition.CombatProfile) &&
                    ReferenceEquals(player.EquippedWeapon.MoveSet, definition.MoveSet),
                    $"swap cycle crossed identity or skills for {definition.WeaponId}");
            }
        }

    }

    private static void ValidateRuntimeSeparation()
    {
        var combat = new PlayerCombat();
        combat.ApplyLoadout(
            EquipmentCatalog.HunterBow,
            EquipmentCatalog.CreateStartingArmor());
        combat.Resources.AddHunterFocus(80f);

        WeaponDefinition alternateBow = new(
            "validation-bow",
            "Validation Bow",
            WeaponFamily.HunterBow,
            4,
            12,
            EquipmentCatalog.HunterBow.AttackSpeedModifier,
            EquipmentCatalog.HunterBow.StaminaCostModifier,
            EquipmentCatalog.HunterBow.RangeModifier,
            EquipmentCatalog.HunterBow.KnockbackModifier,
            EquipmentCatalog.HunterBow.PoiseDamageModifier,
            EquipmentCatalog.HunterBow.UsesShield,
            EquipmentCatalog.HunterBow.AttackMovementMultiplier,
            EquipmentCatalog.HunterBow.HeavyAttackProfile,
            EquipmentCatalog.HunterBow.MoveSet,
            EquipmentCatalog.HunterBow.CombatProfile);
        combat.ApplyLoadout(
            alternateBow,
            EquipmentCatalog.CreateStartingArmor());
        Require(combat.Resources.Family == WeaponFamily.HunterBow &&
            combat.Resources.HunterFocus == 0f,
            "same-family swap retained runtime combat resources");
        Require(alternateBow.WeaponId == "validation-bow" &&
            alternateBow.WeaponType == WeaponFamily.HunterBow,
            "runtime state mutated persistent weapon identity");
    }

    private static void ValidatePersistenceAndSlotIsolation()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendant-weapon-identity-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "slots.json");
        try
        {
            var manager = new SaveSlotManager(path);
            var obsoleteInventoryItem = new SavedEquipmentItemData
            {
                DefinitionId = "hunter-spear",
                Name = "Hunter Spear",
                Slot = EquipmentSlot.Weapon,
                Rarity = ItemRarity.Legendary,
                WeaponFamily = WeaponFamily.Spear,
                Tier = 3,
                DamageBonus = 9
            };

            SaveSlotData[] slots =
            {
                CreateSlot(
                    0,
                    "BOW",
                    EquipmentCatalog.HunterBow,
                    obsoleteInventoryItem),
                CreateSlot(1, "GREAT", EquipmentCatalog.IronVanguardGreatsword),
                CreateSlot(2, "STAFF", EquipmentCatalog.ArcaneWarStaff)
            };
            foreach (SaveSlotData slot in slots)
                Require(manager.Store(slot), $"could not store slot {slot.SlotIndex}");

            var reloaded = new SaveSlotManager(path);
            string[] expectedIds =
            {
                EquipmentCatalog.HunterBow.WeaponId,
                EquipmentCatalog.IronVanguardGreatsword.WeaponId,
                EquipmentCatalog.ArcaneWarStaff.WeaponId
            };
            for (int index = 0; index < expectedIds.Length; index++)
            {
                EquipmentItem equipped = CharacterSelectState.CreateWeapon(
                    reloaded.GetSlot(index));
                Require(equipped.WeaponId == expectedIds[index],
                    $"slot {index} loaded another slot's weapon identity");
            }

            SavedEquipmentItemData loadedObsolete =
                reloaded.GetSlot(0).InventoryItems[0];
            Require(CharacterSelectState.TryCreateInventoryItem(
                    loadedObsolete,
                    out EquipmentItem migrated) &&
                migrated.WeaponId == EquipmentCatalog.HunterBow.WeaponId,
                "legacy inventory weapon did not migrate to canonical identity");

            loadedObsolete.DefinitionId = "removed-prototype-weapon";
            Require(!CharacterSelectState.TryCreateInventoryItem(
                    loadedObsolete,
                    out EquipmentItem skipped) &&
                skipped == null,
                "unknown saved weapon was exposed as a playable item");

            SaveSlotData unknownEquipped = CreateSlot(
                0,
                "UNKNOWN",
                EquipmentCatalog.HunterBow);
            unknownEquipped.EquippedWeaponId = "removed-prototype-weapon";
            Require(CharacterSelectState.CreateWeapon(unknownEquipped).WeaponId ==
                EquipmentCatalog.KnightLongSword.WeaponId,
                "unknown equipped weapon did not fall back safely");

            string[] removedIds =
            {
                "war-axe",
                "raider-axe",
                "breaker-spiked-mace",
                "reckoner-chain-flail"
            };
            foreach (string removedId in removedIds)
            {
                loadedObsolete.DefinitionId = removedId;
                Require(!CharacterSelectState.TryCreateInventoryItem(
                        loadedObsolete,
                        out EquipmentItem removedItem) &&
                    removedItem == null,
                    $"removed inventory weapon {removedId} was exposed");

                unknownEquipped.EquippedWeaponId = removedId;
                Require(CharacterSelectState.CreateWeapon(unknownEquipped).WeaponId ==
                    EquipmentCatalog.KnightLongSword.WeaponId,
                    $"removed equipped weapon {removedId} did not fall back safely");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static void ValidateRewardFactories()
    {
        for (int index = 0; index < EquipmentCatalog.WeaponSampleCount; index++)
        {
            EquipmentItem reward = EquipmentCatalog.CreateWeaponSample(index);
            Require(reward.WeaponDefinition != null &&
                reward.WeaponId == EquipmentCatalog.AllImplementedWeapons[index].WeaponId &&
                reward.WeaponType ==
                    EquipmentCatalog.AllImplementedWeapons[index].WeaponType,
                $"Last Dice weapon index {index} produced an invalid identity");
        }

        var bossReward = new BossWeaponReward(
            EquipmentCatalog.IronVanguardGreatsword,
            ItemRarity.Legendary).CreateItem();
        Require(bossReward?.WeaponId ==
                EquipmentCatalog.IronVanguardGreatsword.WeaponId &&
            bossReward.WeaponType == WeaponFamily.GreatSword,
            "boss reward factory changed weapon identity");
    }

    private static void ValidateDeathRestartAndMapChanges()
    {
        var session = new GameSession(
            new Rectangle(0, 0, 1280, 720),
            randomSeed: 9127);
        EquipmentItem bow = FindOwnedWeapon(
            session.Player,
            EquipmentCatalog.HunterBow.WeaponId);
        Require(bow != null && session.Player.EquipItem(bow),
            "death/restart fixture could not equip Bow");
        string weaponId = session.Player.EquippedWeapon.WeaponId;
        WeaponFamily weaponType = session.Player.WeaponFamily;

        for (int map = 1; map <= 4; map++)
        {
            session.ExecuteDeveloperCommand(
                DeveloperCommand.TeleportMap,
                map,
                0);
            Require(session.Player.EquippedWeapon.WeaponId == weaponId &&
                session.Player.WeaponFamily == weaponType,
                $"map/admin transition {map} mutated equipped weapon");
        }

        session.Player.DebugGodMode = false;
        session.Player.ReceiveDamage(10000);
        var frame = new GameTime(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1f / 60f));
        session.Update(frame, new KeyboardState(), new MouseState());
        Require(session.State == GameState.GameOver,
            "death/restart fixture did not enter Game Over");
        session.Update(frame, new KeyboardState(Keys.R), new MouseState());
        Require(session.State == GameState.Playing &&
            session.Player.EquippedWeapon.WeaponId == weaponId &&
            session.Player.WeaponFamily == weaponType &&
            session.Player.Combat.State == CombatState.Idle &&
            session.Player.Combat.Resources.HunterFocus == 0f,
            "death/restart mutated identity or retained weapon runtime state");
    }

    private static SaveSlotData CreateSlot(
        int index,
        string name,
        WeaponDefinition weapon,
        SavedEquipmentItemData inventoryItem = null)
    {
        var slot = new SaveSlotData
        {
            SlotIndex = index,
            Occupied = true,
            CharacterName = name,
            EquippedWeaponId = weapon.WeaponId,
            EquippedWeaponName = weapon.Name,
            EquippedWeaponFamily = weapon.WeaponType,
            EquippedWeaponTier = weapon.Tier,
            EquippedWeaponDamageBonus = weapon.DamageBonus,
            HasInventorySnapshot = inventoryItem != null
        };
        if (inventoryItem != null)
            slot.InventoryItems.Add(inventoryItem);
        return slot;
    }

    private static EquipmentItem FindInventoryWeapon(
        PlayerCharacter player,
        string weaponId)
    {
        foreach (EquipmentItem item in player.Inventory.Items)
        {
            if (item.WeaponId == weaponId)
                return item;
        }
        return null;
    }

    private static EquipmentItem FindOwnedWeapon(
        PlayerCharacter player,
        string weaponId)
    {
        if (player.EquippedItems.Weapon?.WeaponId == weaponId)
            return player.EquippedItems.Weapon;
        return FindInventoryWeapon(player, weaponId);
    }

    private static int CountOwnedWeaponId(
        PlayerCharacter player,
        string weaponId)
    {
        int count = player.EquippedItems.Weapon?.WeaponId == weaponId ? 1 : 0;
        foreach (EquipmentItem item in player.Inventory.Items)
        {
            if (item.WeaponId == weaponId)
                count++;
        }
        return count;
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
            throw new InvalidOperationException(
                $"Weapon identity validation failed: {scenario}.");
    }
}
