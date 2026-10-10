using System;
using DungeonAscendant.Items;
using DungeonAscendant.Lore;
using DungeonAscendant.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class InventorySessionValidation
{
    public static void ValidateOrThrow()
    {
        var session = new GameSession(
            new Rectangle(0, 0, 800, 480),
            randomSeed: 4815);

        Press(session, Keys.Enter);
        Press(session, Keys.I);
        Require(session.IsInventoryOpen, "open inventory");
        Require(session.ActiveInventoryTab == InventoryTab.Weapons,
            "default Weapons tab");
        Require(AllItemsMatchTab(session), "Weapons filtering");

        Vector2 lockedPosition = session.Player.Position;
        Press(session, Keys.D);
        Require(session.SelectedInventoryIndex == 1, "right navigation");
        Require(session.Player.Position == lockedPosition, "gameplay input lock");
        int rememberedWeaponIndex = session.SelectedInventoryIndex;

        Press(session, Keys.E);
        Require(session.ActiveInventoryTab == InventoryTab.Equipment,
            "next tab");
        Require(AllItemsMatchTab(session), "Equipment filtering");
        int equipmentCount = session.ActiveInventoryItems.Count;
        Require(equipmentCount >= ArmorGradeRules.FusionMaterialCount,
            "debug Armor availability");
        Require(session.CanFuseSelectedArmor, "Armor fusion eligibility");

        Press(session, Keys.F);
        Require(session.IsFusionConfirmationPending, "fusion prompt");
        Press(session, Keys.Enter);
        Require(!session.IsFusionConfirmationPending, "fusion completion");
        Require(session.ActiveInventoryItems.Count == equipmentCount - 2,
            "fusion filtered-view refresh");
        Require(session.SelectedInventoryItem?.ArmorGrade == ArmorGrade.C2,
            "fusion result selection");

        EquipmentItem fusionResult = session.SelectedInventoryItem;
        Press(session, Keys.Enter);
        Require(ReferenceEquals(session.Player.EquippedItems.Armor, fusionResult),
            "equip selected filtered reference");
        Require(IsSelectionValid(session), "selection after equip");

        Press(session, Keys.Q);
        Require(session.ActiveInventoryTab == InventoryTab.Weapons,
            "previous tab");
        Require(session.SelectedInventoryIndex == rememberedWeaponIndex,
            "per-tab selection memory");
        Require(AllItemsMatchTab(session), "Weapons filtering after mutation");

        Press(session, Keys.F);
        Require(!session.IsFusionConfirmationPending,
            "fusion disabled in Weapons");

        Require(session.Player.Notes.TryCollect(
            LoreCatalog.StarBornHero,
            out LoreNote collectedNote),
            "collect note");
        Require(!collectedNote.IsRead, "new note starts unread");
        Require(!session.Player.Notes.TryCollect(
            LoreCatalog.StarBornHero,
            out _),
            "duplicate note rejected");

        Press(session, Keys.Q);
        Require(session.ActiveInventoryTab == InventoryTab.Notes,
            "previous tab wraps to Notes");
        Require(ReferenceEquals(session.SelectedLoreNote, collectedNote),
            "selected note reference");
        Require(collectedNote.IsRead, "selected note marked read");
        Require(session.ActiveInventoryItems.Count == 0,
            "notes excluded from equipment inventory");

        Press(session, Keys.E);
        Require(session.ActiveInventoryTab == InventoryTab.Weapons,
            "next tab wraps to Weapons");
        Press(session, Keys.I);
        Require(!session.IsInventoryOpen, "close inventory");
    }

    private static void Press(GameSession session, Keys key)
    {
        var frame = new GameTime(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1d / 60d));
        session.Update(
            frame,
            new KeyboardState(key),
            default);
        session.Update(
            frame,
            new KeyboardState(),
            default);
    }

    private static bool AllItemsMatchTab(GameSession session)
    {
        foreach (EquipmentItem item in session.ActiveInventoryItems)
        {
            if (!InventoryTabRules.Includes(session.ActiveInventoryTab, item))
                return false;
        }

        return true;
    }

    private static bool IsSelectionValid(GameSession session)
    {
        return session.ActiveInventoryItems.Count == 0
            ? session.SelectedInventoryIndex == 0
            : session.SelectedInventoryIndex >= 0 &&
                session.SelectedInventoryIndex < session.ActiveInventoryItems.Count;
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Inventory session validation failed: {scenario}.");
        }
    }
}
