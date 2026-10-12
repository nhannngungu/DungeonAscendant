using System;
using System.IO;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Localization;
using DungeonAscendant.Lore;
using DungeonAscendant.Persistence;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class PigPhotoEasterEggValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        ValidateLocalization();
        ValidateAspectFit();
        ValidateDropCollectionPersistenceAndAdmin();
    }

    private static void ValidateLocalization()
    {
        var english = Loc.GetStrings(Language.English);
        var vietnamese = Loc.GetStrings(Language.Vietnamese);
        Need(english["notes.pig_photo.title"] == "Mysterious Photograph" &&
            english["notes.pig_photo.content"] == "Just a pig.",
            "English photograph localization is incorrect.");
        Need(vietnamese["notes.pig_photo.title"] == "Bức Ảnh Kỳ Lạ" &&
            vietnamese["notes.pig_photo.content"] ==
                "Chỉ là một con heo mà thôi.",
            "Vietnamese photograph localization is incorrect.");
        Need(LoreCatalog.MysteriousPhotograph.ImageAssetKey ==
                LoreCatalog.PigPhotoImageAssetKey &&
            LoreCatalog.StarBornHero.ImageAssetKey.Length == 0,
            "Lore note image metadata is incorrect.");
    }

    private static void ValidateAspectFit()
    {
        (int width, int height)[] viewports =
        {
            (1280, 720),
            (1600, 900),
            (1920, 1080)
        };
        foreach ((int width, int height) in viewports)
        {
            int panelWidth = Math.Min(width - 48, 980);
            int panelHeight = Math.Min(height - 48, 920);
            var area = new Rectangle(
                0,
                0,
                panelWidth - 44,
                panelHeight - 112);
            Rectangle fitted = GameRenderer.CalculateAspectFitBounds(
                1024,
                1536,
                area);
            Need(area.Contains(fitted) && fitted.Width > 0 && fitted.Height > 0,
                $"Photograph does not fit safely at {width}x{height}.");
            float sourceAspect = 1024f / 1536f;
            float fittedAspect = fitted.Width / (float)fitted.Height;
            Need(MathF.Abs(sourceAspect - fittedAspect) < .005f,
                $"Photograph aspect ratio changed at {width}x{height}.");

            var notesImageArea = new Rectangle(0, 0, 334, 212);
            Rectangle notesFitted = GameRenderer.CalculateAspectFitBounds(
                1024,
                1536,
                notesImageArea);
            Need(notesImageArea.Contains(notesFitted) &&
                notesFitted.Width > 0 && notesFitted.Height > 0,
                $"Notes photograph does not fit safely at {width}x{height}.");
            float notesAspect = notesFitted.Width / (float)notesFitted.Height;
            Need(MathF.Abs(sourceAspect - notesAspect) < .005f,
                $"Notes photograph aspect ratio changed at {width}x{height}.");
        }
    }

    private static void ValidateDropCollectionPersistenceAndAdmin()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendant-pig-photo-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");
        try
        {
            var manager = new SaveSlotManager(path);
            Need(manager.Store(CreateMapTwoSlot(0, "PHOTO TEST")) &&
                manager.Store(CreateMapTwoSlot(1, "CLEAN SLOT")),
                "Photograph validation could not create save fixtures.");

            var session = new GameSession(
                new Rectangle(0, 0, 1280, 720),
                randomSeed: 2442,
                savePath: path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            SettleEntry(session);
            Need(session.CurrentDungeon.IsAncientCatacombs &&
                !session.Map2PigPhotoCollected,
                "Map 2 photograph fixture did not load cleanly.");

            Enemy skeleton = CreateEnemy(session, EnemyType.Skeleton);
            Need(!session.TrySpawnFallenKnightPigPhotoDrop(skeleton),
                "A normal Map 2 enemy triggered the photograph.");
            Enemy cursedKnight = CreateEnemy(session, EnemyType.CursedKnight);
            Need(!session.TrySpawnFallenKnightPigPhotoDrop(cursedKnight),
                "Cursed Knight still triggered the photograph.");

            Enemy fallenKnight = DefeatFallenKnight(session);
            EncounterZone fallenArena = session.CurrentDungeon.EncounterZones[^1];
            LoreNotePickup spawnedPickup = session.LoreNotePickup;
            Need(session.Map02Cleared && session.BossDefeated &&
                session.LoreNotePickup?.Definition.Id ==
                    LoreCatalog.MysteriousPhotograph.Id &&
                fallenArena.ActivationBounds.Contains(
                    session.LoreNotePickup.Bounds) &&
                SideScrollingCollision.IsPositionFree(
                    session.LoreNotePickup.Position,
                    LoreNotePickup.Size,
                    session.CurrentDungeon) &&
                SideScrollingCollision.IsSupported(
                    session.LoreNotePickup.Position,
                    LoreNotePickup.Size,
                    session.CurrentDungeon),
                "Fallen Knight did not create a safe guaranteed photograph.");
            Need(!session.TrySpawnFallenKnightPigPhotoDrop(fallenKnight) &&
                ReferenceEquals(spawnedPickup, session.LoreNotePickup),
                "Repeated Fallen Knight death handling duplicated the pickup.");

            MovePlayerToPhotograph(session);
            Press(session, Keys.E);
            Need(session.Map2PigPhotoCollected &&
                session.IsPigPhotoPopupOpen &&
                session.Player.Notes.Contains(
                    LoreCatalog.MysteriousPhotograph.Id) &&
                CountPhotographNotes(session) == 1,
                "Photograph collection did not open the modal or add one note.");
            Need(new SaveSlotManager(path).GetSlot(0).Map2PigPhotoCollected,
                "Photograph collection was not saved immediately.");
            Need(!session.TrySpawnFallenKnightPigPhotoDrop(fallenKnight),
                "A collected character could trigger a duplicate photograph.");
            Press(session, Keys.Escape);
            Need(!session.IsPigPhotoPopupOpen,
                "Escape did not close the photograph popup.");
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ForcePigPhotoDrop, 2, 0);
            Need(session.LoreNotePickup.IsCollected &&
                CountPhotographNotes(session) == 1,
                "Admin force-drop bypassed the one-copy rule.");
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ResetCurrentBoss, 2, 0);
            Need(!session.Map02Cleared && !session.BossDefeated &&
                !session.WildForestEncounters.IsTriggered("fallen-knight"),
                "Admin Fallen Knight reset did not reopen the encounter.");

            ReturnThroughPauseMenu(session);
            Press(session, Keys.D);
            Press(session, Keys.Enter);
            SettleEntry(session);
            Need(!session.Map2PigPhotoCollected &&
                !session.Player.Notes.Contains(
                    LoreCatalog.MysteriousPhotograph.Id),
                "Photograph collection leaked into another character slot.");
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ForcePigPhotoDrop, 2, 0);
            Need(session.LoreNotePickup?.Definition.Id ==
                    LoreCatalog.MysteriousPhotograph.Id,
                "Admin force-drop did not create the photograph on Map 2.");
            ReturnThroughPauseMenu(session);
            Press(session, Keys.Enter);
            SettleEntry(session);
            Need(!session.Map2PigPhotoCollected &&
                session.LoreNotePickup == null,
                "An uncollected photograph survived a save reload.");
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ForcePigPhotoDrop, 2, 0);
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ResetPigPhotoCollected, 2, 0);
            Need(!session.Map2PigPhotoCollected &&
                session.LoreNotePickup == null &&
                !session.Player.Notes.Contains(
                    LoreCatalog.MysteriousPhotograph.Id),
                "Admin reset did not clear photograph state.");

            ReturnThroughPauseMenu(session);
            Press(session, Keys.A);
            Press(session, Keys.Enter);
            SettleEntry(session);
            Need(session.Map2PigPhotoCollected &&
                CountPhotographNotes(session) == 1 &&
                session.LoreNotePickup == null,
                "Collected photograph did not restore without duplicating.");
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ResetPigPhotoCollected, 2, 0);
            session.ExecuteDeveloperCommand(
                DeveloperCommand.ForcePigPhotoDrop, 2, 0);
            MovePlayerToPhotograph(session);
            Press(session, Keys.E);
            Need(session.IsPigPhotoPopupOpen &&
                CountPhotographNotes(session) == 1,
                "Admin reset/force-drop could not retest collection.");
            Press(session, Keys.Enter);
            Need(!session.IsPigPhotoPopupOpen,
                "Enter did not close the photograph popup.");

            for (int frame = 0;
                frame < 120 && session.Player.IsInvulnerable;
                frame++)
            {
                session.Update(Frame, new KeyboardState(), new MouseState());
            }
            session.Player.DebugGodMode = false;
            session.Player.ReceiveDamage(int.MaxValue);
            session.Update(Frame, new KeyboardState(), new MouseState());
            Need(session.State == GameState.GameOver,
                "Photograph death-restart fixture did not reach Game Over.");
            Press(session, Keys.R);
            Need(session.State == GameState.Playing &&
                session.Map2PigPhotoCollected &&
                CountPhotographNotes(session) == 1,
                "Collected photograph was lost or duplicated on death restart.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static SaveSlotData CreateMapTwoSlot(int slotIndex, string name) =>
        new()
        {
            SlotIndex = slotIndex,
            Occupied = true,
            CharacterName = name,
            Level = 3,
            DungeonDepth = 2,
            CurrentMapId = "map-02",
            CurrentArea = "TOMB ENTRANCE",
            Map01Cleared = true,
            LastPlayedUtc = DateTimeOffset.UtcNow
        };

    private static Enemy CreateEnemy(GameSession session, EnemyType type)
    {
        Dungeon.DungeonRoom room = session.CurrentDungeon.StartRoom;
        Enemy enemy = EnemyFactory.Create(type, new EnemySpawnContext(
            session.Player.Position,
            3,
            1,
            room.Id,
            type == EnemyType.CursedKnight,
            GoblinVariant.Normal));
        enemy.SnapToGround(room);
        return enemy;
    }

    private static Enemy DefeatFallenKnight(GameSession session)
    {
        EncounterZone final = session.CurrentDungeon.EncounterZones[^1];
        session.Player.MoveTo(new Vector2(
            final.ActivationBounds.Left + 4f,
            session.CurrentDungeon.BossRoom.GroundY - 28f));
        for (int frame = 0;
            frame < 60 &&
            !session.WildForestEncounters.IsTriggered("fallen-knight");
            frame++)
        {
            session.Update(Frame, new KeyboardState(), new MouseState());
        }

        Enemy fallenKnight = null;
        foreach (Enemy enemy in session.Enemies.Enemies)
            if (enemy.Type == EnemyType.FallenKnight)
                fallenKnight = enemy;
        Need(fallenKnight != null,
            "Fallen Knight did not spawn for photograph validation.");
        fallenKnight.ReceiveDamage(int.MaxValue);
        session.Update(Frame, new KeyboardState(), new MouseState());
        return fallenKnight;
    }

    private static void MovePlayerToPhotograph(GameSession session)
    {
        Need(session.LoreNotePickup != null,
            "Photograph pickup was unavailable for interaction.");
        session.Player.MoveTo(session.LoreNotePickup.Position);
    }

    private static int CountPhotographNotes(GameSession session)
    {
        int count = 0;
        foreach (var note in session.Player.Notes.Notes)
            if (note.Id == LoreCatalog.MysteriousPhotograph.Id)
                count++;
        return count;
    }

    private static void SettleEntry(GameSession session)
    {
        for (int frame = 0; frame < 180; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
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
            "Photograph validation could not return to Character Select.");
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
