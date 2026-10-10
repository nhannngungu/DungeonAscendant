using System;
using System.IO;
using DungeonAscendant.Core;
using DungeonAscendant.Lore;
using DungeonAscendant.Player;
using DungeonAscendant.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Persistence;

public static class SaveSlotValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendant-save-validation-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");

        try
        {
            ValidateManagerRoundTrip(path);
            ValidateFrontendFlow(path);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static void ValidateManagerRoundTrip(string path)
    {
        var manager = new SaveSlotManager(path);
        Need(manager.Slots.Count == SaveSlotManager.SlotCount,
            "Save manager did not expose exactly three slots.");
        Need(!manager.GetSlot(0).Occupied,
            "Fresh save manager did not start empty.");

        var appearance = CharacterAppearance.CreateDefault();
        appearance.HairStyle = HairStyle.TiedBack;
        appearance.SkinTone = SkinTone.Deep;
        var saved = new SaveSlotData
        {
            SlotIndex = 1,
            Occupied = true,
            CharacterName = "SEREN",
            Appearance = appearance,
            Level = 7,
            DungeonDepth = 2,
            CurrentMapId = "map-02",
            CurrentArea = "ANCIENT CATACOMBS",
            PlayTimeSeconds = 3661d,
            LastPlayedUtc = DateTimeOffset.UtcNow
        };
        Need(manager.Store(saved), "Save manager could not write a slot.");

        var reloaded = new SaveSlotManager(path);
        SaveSlotData slot = reloaded.GetSlot(1);
        Need(slot.Occupied && slot.CharacterName == "SEREN" && slot.Level == 7,
            "Saved identity/progression did not survive reload.");
        Need(slot.Appearance.HairStyle == HairStyle.TiedBack &&
            slot.Appearance.SkinTone == SkinTone.Deep,
            "Saved CharacterAppearance did not survive reload.");
        Need(Math.Abs(slot.PlayTimeSeconds - 3661d) < .01d,
            "Saved play time did not survive reload.");

        Need(reloaded.Delete(1), "Save manager could not delete a slot.");
        var afterDelete = new SaveSlotManager(path);
        Need(!afterDelete.GetSlot(1).Occupied,
            "Deleted slot returned after reload.");
    }

    private static void ValidateFrontendFlow(string path)
    {
        var session = new GameSession(
            new Rectangle(0, 0, 1280, 720),
            randomSeed: null,
            savePath: path);
        Press(session, Keys.Enter);
        Need(session.State == GameState.CharacterSelect,
            "Title did not open Character Select.");
        Press(session, Keys.Enter);
        Need(session.State == GameState.CharacterCreation,
            "Empty slot did not open Character Creation.");

        session.CharacterCreator.MoveSelection(
            (int)CharacterCreatorRow.Confirm -
            (int)session.CharacterCreator.SelectedRow);
        Press(session, Keys.Enter);
        Need(session.State == GameState.Playing,
            "Confirmed character did not enter gameplay.");
        Need(new SaveSlotManager(path).GetSlot(0).Occupied,
            "Confirmed character was not persisted.");

        session.Player.GainExperience(100);
        Need(session.Player.Notes.TryCollect(
            LoreCatalog.StarBornHero,
            out LoreNote collectedNote),
            "Pause-return setup could not collect a note.");
        collectedNote.MarkRead();
        int originalInventoryCount = session.Player.Inventory.Count;
        session.Player.Inventory.Remove(session.Player.Inventory.GetItem(0));
        int slotZeroInventoryCount = session.Player.Inventory.Count;
        Need(slotZeroInventoryCount == originalInventoryCount - 1,
            "Pause-return setup could not modify inventory.");

        Vector2 pausedPosition = session.Player.Position;
        Press(session, Keys.Escape);
        Need(session.State == GameState.Paused,
            "Escape did not open the pause menu.");
        Press(session, Keys.S);
        Press(session, Keys.S);
        Need(session.PauseMenu.SelectedEntry ==
            PauseMenuEntry.ReturnToCharacterSelect,
            "Pause menu could not select Return to Character Select.");
        Press(session, Keys.Enter);
        Need(session.PauseMenu.IsReturnConfirmationOpen &&
            !session.PauseMenu.ConfirmReturn,
            "Return confirmation did not open safely on No.");
        Press(session, Keys.D);
        Need(session.Player.Position == pausedPosition,
            "Player moved while return confirmation was open.");
        Need(session.PauseMenu.ConfirmReturn,
            "Return confirmation could not select Yes.");
        Press(session, Keys.Enter);
        Need(session.State == GameState.CharacterSelect &&
            session.CharacterSelect.SelectedSlotIndex == 0,
            "Confirmed return did not preserve the active slot selection.");
        Need(session.CurrentDungeon == null &&
            session.Projectiles.Projectiles.Count == 0 &&
            session.RootHazards.Hazards.Count == 0 &&
            session.GroundRunes.Count == 0,
            "Return to Character Select retained gameplay runtime state.");

        SaveSlotData returnedSlot = new SaveSlotManager(path).GetSlot(0);
        Need(returnedSlot.Level == 2 && returnedSlot.Notes.Count == 1 &&
            returnedSlot.Notes[0].IsRead &&
            returnedSlot.InventoryItems.Count == slotZeroInventoryCount,
            "Return did not persist progression, notes, and inventory.");

        Press(session, Keys.D);
        Press(session, Keys.Enter);
        Need(session.State == GameState.CharacterCreation,
            "Second empty slot did not open Character Creation.");
        session.CharacterCreator.Draft.BodyType = CharacterBodyType.Female;
        session.CharacterCreator.Draft.HairStyle = HairStyle.Long;
        session.CharacterCreator.MoveSelection(
            (int)CharacterCreatorRow.Confirm -
            (int)session.CharacterCreator.SelectedRow);
        Press(session, Keys.Enter);
        Need(session.State == GameState.Playing && session.Player.Level == 1 &&
            session.Player.Notes.Count == 0,
            "New second slot inherited first-slot progression or notes.");
        int slotOneInventoryCount = session.Player.Inventory.Count;

        ReturnThroughPauseMenu(session);
        Need(session.CharacterSelect.SelectedSlotIndex == 1,
            "Second slot was not kept selected after return.");
        Press(session, Keys.A);
        Press(session, Keys.Enter);
        Need(session.State == GameState.Playing && session.Player.Level == 2 &&
            session.Player.Notes.Count == 1 &&
            session.Player.Inventory.Count == slotZeroInventoryCount &&
            session.Player.Appearance.BodyType == CharacterBodyType.Male,
            "Loading slot zero did not restore its independent player state.");

        ReturnThroughPauseMenu(session);
        Press(session, Keys.D);
        Press(session, Keys.Enter);
        Need(session.State == GameState.Playing && session.Player.Level == 1 &&
            session.Player.Notes.Count == 0 &&
            session.Player.Inventory.Count == slotOneInventoryCount &&
            session.Player.Appearance.BodyType == CharacterBodyType.Female &&
            session.Player.Appearance.HairStyle == HairStyle.Long,
            "Loading slot one leaked state from slot zero.");

        var continued = new GameSession(
            new Rectangle(0, 0, 1280, 720),
            randomSeed: null,
            savePath: path);
        Press(continued, Keys.Enter);
        Press(continued, Keys.Enter);
        Need(continued.State == GameState.Playing,
            "Occupied slot did not continue into gameplay.");

        var deleteSession = new GameSession(
            new Rectangle(0, 0, 1280, 720),
            randomSeed: null,
            savePath: path);
        Press(deleteSession, Keys.Enter);
        Press(deleteSession, Keys.Delete);
        Need(deleteSession.CharacterSelect.IsDeleteConfirmationOpen,
            "Delete did not require confirmation.");
        Press(deleteSession, Keys.Enter);
        Need(!new SaveSlotManager(path).GetSlot(0).Occupied,
            "Confirmed delete was not persisted.");
    }

    private static void ReturnThroughPauseMenu(GameSession session)
    {
        Press(session, Keys.Escape);
        Press(session, Keys.S);
        Press(session, Keys.S);
        Press(session, Keys.Enter);
        Press(session, Keys.D);
        Press(session, Keys.Enter);
        Need(session.State == GameState.CharacterSelect,
            "Pause confirmation did not return to Character Select.");
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame, new KeyboardState(key), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
