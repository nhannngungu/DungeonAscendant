using System;
using System.IO;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Items;
using DungeonAscendant.Persistence;
using DungeonAscendant.Player;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class LastDiceTableValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(.05f));

    public static void ValidateOrThrow()
    {
        ValidateHeldRevealAndLockedDice();
        ValidateRulesAndRewards();
        ValidateSaveRoundTrip();
        ValidateSessionReload();
    }

    private static void ValidateHeldRevealAndLockedDice()
    {
        DungeonMap map = new FallenCityMapGenerator().Generate();
        var table = new LastDiceTableSystem(3471);
        table.Configure(map);
        Need(table.State == LastDiceTableState.Ready &&
            table.CanInteract(table.Position),
            "Last Dice Table was not available in Market Ruins.");
        Need(table.TryOpen(table.Position),
            "Last Dice Table could not begin its first round.");

        for (int frame = 0; frame < 30; frame++)
            table.Update(Frame, new KeyboardState(), new KeyboardState());
        Need(table.State == LastDiceTableState.Selecting,
            "Dealer preparation did not reach bet selection.");

        table.Update(Frame,
            new KeyboardState(Keys.D), new KeyboardState());
        Need(table.Choice == LastDiceChoice.Tai,
            "TAI could not be selected.");
        table.Update(Frame,
            new KeyboardState(Keys.Enter), new KeyboardState());
        Need(table.State == LastDiceTableState.Revealing &&
            table.IsChoiceLocked,
            "Confirming a bet did not lock the round before reveal.");
        int die1 = table.Dice[0];
        int die2 = table.Dice[1];
        int die3 = table.Dice[2];
        Need(die1 is >= 1 and <= 6 && die2 is >= 1 and <= 6 &&
            die3 is >= 1 and <= 6,
            "Committed dice were not generated before reveal.");

        float closed = table.RevealProgress;
        for (int frame = 0; frame < 20; frame++)
            table.Update(Frame, new KeyboardState(), new KeyboardState());
        Need(table.RevealProgress == closed,
            "Reveal advanced without held input.");

        for (int frame = 0; frame < 12; frame++)
            table.Update(Frame,
                new KeyboardState(Keys.E), new KeyboardState(Keys.E));
        float partial = table.RevealProgress;
        Need(partial > 0f && partial < 1f,
            "Held reveal input did not produce a partial opening.");
        table.Update(Frame, new KeyboardState(), new KeyboardState(Keys.E));
        float paused = table.RevealProgress;
        for (int frame = 0; frame < 40; frame++)
            table.Update(Frame, new KeyboardState(), new KeyboardState());
        Need(table.RevealProgress == paused,
            "Releasing reveal input did not freeze the bowl exactly.");

        table.Update(Frame,
            new KeyboardState(Keys.A), new KeyboardState());
        Need(table.Choice == LastDiceChoice.Tai,
            "Committed choice changed during a partial reveal.");
        Need(table.Dice[0] == die1 && table.Dice[1] == die2 &&
            table.Dice[2] == die3,
            "Stopping the reveal rerolled committed dice.");

        var resumed = new LastDiceTableSystem(9999);
        resumed.Configure(map);
        resumed.Restore(
            table.Started, table.IsCompleted, table.RoundIndex, table.Wins,
            table.RoundCommitted, table.RoundResolved,
            table.CompletionReached, table.Choice,
            die1, die2, die3, table.RevealProgress,
            table.RewardGranted, table.RewardWeaponIndex);
        Need(resumed.State == LastDiceTableState.Revealing &&
            resumed.RevealProgress == paused &&
            resumed.Dice[0] == die1 && resumed.Dice[1] == die2 &&
            resumed.Dice[2] == die3,
            "Restoring a committed round changed its partial reveal or dice.");

        for (int frame = 0; frame < 180 &&
             resumed.State == LastDiceTableState.Revealing; frame++)
        {
            resumed.Update(Frame,
                new KeyboardState(Keys.Enter),
                new KeyboardState(Keys.Enter));
        }
        Need(resumed.State == LastDiceTableState.RoundResult &&
            resumed.RevealProgress == 1f,
            "Resumed held reveal did not finalize at exactly one.");
        Need(resumed.Dice[0] == die1 && resumed.Dice[1] == die2 &&
            resumed.Dice[2] == die3,
            "Completing the reveal rerolled committed dice.");
    }

    private static void ValidateRulesAndRewards()
    {
        Need(LastDiceTableSystem.Evaluate(LastDiceChoice.Xiu, 1, 3, 6),
            "XIU did not win on a non-triple total of 10.");
        Need(!LastDiceTableSystem.Evaluate(LastDiceChoice.Xiu, 4, 4, 4),
            "A triple incorrectly paid XIU.");
        Need(LastDiceTableSystem.Evaluate(LastDiceChoice.Tai, 6, 5, 6),
            "TAI did not win on a non-triple total of 17.");
        Need(!LastDiceTableSystem.Evaluate(LastDiceChoice.Tai, 6, 6, 6),
            "A triple incorrectly paid TAI.");
        Need(LastDiceTableSystem.GetRewardRarity(0) == null &&
            LastDiceTableSystem.GetRewardRarity(1) == null &&
            LastDiceTableSystem.GetRewardRarity(2) == ItemRarity.Epic &&
            LastDiceTableSystem.GetRewardRarity(3) == ItemRarity.Legendary,
            "Last Dice reward tiers no longer match the 2/3 and 3/3 rules.");

        var reward = new LastDiceTableSystem(71);
        reward.Configure(new FallenCityMapGenerator().Generate());
        reward.Restore(
            started: true,
            completed: false,
            roundIndex: 2,
            wins: 2,
            roundCommitted: true,
            roundResolved: true,
            completionReached: true,
            choice: LastDiceChoice.Tai,
            die1: 5,
            die2: 4,
            die3: 3,
            revealProgress: 1f,
            rewardGranted: false,
            rewardWeaponIndex: 4);
        Need(reward.TryConsumeRewardRequest(out int wins, out int index) &&
            wins == 2 && index == 4,
            "A completed 2/3 challenge did not request its locked weapon reward.");
        Need(!reward.TryConsumeRewardRequest(out _, out _),
            "Last Dice reward request could be consumed twice.");
    }

    private static void ValidateSaveRoundTrip()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendant-dice-validation-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "slots.json");
        try
        {
            var manager = new SaveSlotManager(path);
            Need(manager.Store(new SaveSlotData
            {
                SlotIndex = 0,
                Occupied = true,
                CharacterName = "DICEKEEPER",
                Appearance = CharacterAppearance.CreateDefault(),
                DungeonDepth = 3,
                LastDiceStarted = true,
                LastDiceRoundIndex = 1,
                LastDiceWins = 1,
                LastDiceRoundCommitted = true,
                LastDiceChoice = LastDiceChoice.Xiu,
                LastDiceDie1 = 2,
                LastDiceDie2 = 3,
                LastDiceDie3 = 4,
                LastDiceRevealProgress = .4375f,
                LastDiceRewardWeaponIndex = -1
            }), "Committed Last Dice fixture could not be saved.");

            SaveSlotData loaded = new SaveSlotManager(path).GetSlot(0);
            Need(loaded.LastDiceRoundCommitted &&
                loaded.LastDiceChoice == LastDiceChoice.Xiu &&
                loaded.LastDiceDie1 == 2 && loaded.LastDiceDie2 == 3 &&
                loaded.LastDiceDie3 == 4 &&
                MathF.Abs(loaded.LastDiceRevealProgress - .4375f) < .0001f,
                "Save/load changed the committed dice or partial reveal.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static void ValidateSessionReload()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendant-dice-session-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "slots.json");
        try
        {
            var manager = new SaveSlotManager(path);
            Need(manager.Store(new SaveSlotData
            {
                SlotIndex = 0,
                Occupied = true,
                CharacterName = "LOCKEDROUND",
                Appearance = CharacterAppearance.CreateDefault(),
                DungeonDepth = 3,
                CurrentMapId = "map-03",
                CurrentArea = "MARKET RUINS",
                Map01Cleared = true,
                Map02Cleared = true,
                LastDiceStarted = true,
                LastDiceRoundIndex = 1,
                LastDiceWins = 1,
                LastDiceRoundCommitted = true,
                LastDiceChoice = LastDiceChoice.Tai,
                LastDiceDie1 = 6,
                LastDiceDie2 = 2,
                LastDiceDie3 = 5,
                LastDiceRevealProgress = .31f,
                LastDiceRewardWeaponIndex = -1
            }), "Session reload fixture could not be saved.");

            var session = new GameSession(
                new Rectangle(0, 0, 1280, 720), 8841, path);
            Press(session, Keys.Enter);
            Press(session, Keys.Enter);
            Need(session.State == GameState.Playing &&
                session.LastDiceTable.State == LastDiceTableState.Revealing &&
                session.LastDiceTable.Dice[0] == 6 &&
                session.LastDiceTable.Dice[1] == 2 &&
                session.LastDiceTable.Dice[2] == 5 &&
                MathF.Abs(session.LastDiceTable.RevealProgress - .31f) < .0001f,
                "GameSession did not restore the committed Last Dice round.");

            for (int frame = 0; frame < 10; frame++)
            {
                session.Update(Frame,
                    new KeyboardState(Keys.E),
                    new MouseState());
            }
            float savedProgress = session.LastDiceTable.RevealProgress;
            session.FlushActiveSave();

            var reloaded = new GameSession(
                new Rectangle(0, 0, 1280, 720), 123, path);
            Press(reloaded, Keys.Enter);
            Press(reloaded, Keys.Enter);
            Need(reloaded.LastDiceTable.State == LastDiceTableState.Revealing &&
                reloaded.LastDiceTable.Choice == LastDiceChoice.Tai &&
                reloaded.LastDiceTable.Dice[0] == 6 &&
                reloaded.LastDiceTable.Dice[1] == 2 &&
                reloaded.LastDiceTable.Dice[2] == 5 &&
                MathF.Abs(reloaded.LastDiceTable.RevealProgress - savedProgress) < .0001f,
                "Closing and loading the same save rerolled or reset the committed round.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
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
