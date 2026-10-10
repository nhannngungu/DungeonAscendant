using System;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class AncientScholarTombValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(.05f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new CatacombMapGenerator().Generate();
        var tomb = new AncientScholarTombSystem();
        tomb.ResetForNewRun();
        tomb.Configure(map);

        Need(tomb.State == AncientScholarTombState.Unopened,
            "Scholar Tomb did not initialize as unopened on Map 2.");
        CatacombZoneDefinition barracks =
            map.CatacombZones[AncientScholarTombSystem.GuardBarracksZoneIndex];
        Need(tomb.Position.X > barracks.Bounds.Left &&
            tomb.Position.X < barracks.Bounds.Left + 120,
            "Scholar Tomb is not in the pre-encounter lip of Guard Barracks.");
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            Need(Vector2.Distance(tomb.Position, socket.Position) >
                AncientScholarTombSystem.SafeRadius,
                $"Spawn socket {socket.SocketId} intrudes on the Scholar safe zone.");
        }

        var director = new WildForestEncounterDirector();
        var enemies = new EnemyManager(921);
        director.Reset(map);
        enemies.Reset(
            map,
            enemyLevel: 3,
            worldTier: 1,
            RegionType.WildForest,
            isFirstDungeonOfRun: false,
            suppressProceduralSpawns: true);
        for (int frame = 0; frame < 20; frame++)
        {
            director.Update(
                Frame,
                new Vector2(barracks.Bounds.Left + 140, barracks.GroundY - 28),
                map,
                enemies,
                enemyLevel: 3,
                worldTier: 1,
                suppressActivation: true);
        }
        Need(!director.IsTriggered("guard-barracks") &&
            enemies.Enemies.Count == 0,
            "Scholar safe-zone suppression activated the Barracks encounter.");

        Need(tomb.TryInspect(tomb.Position),
            "Scholar coffin could not be inspected in range.");
        for (int frame = 0; frame < 24; frame++)
            tomb.Update(Frame, new KeyboardState(), new KeyboardState());
        Need(tomb.State == AncientScholarTombState.Question &&
            tomb.CurrentQuestion.Title == "THE LOOP OF TIME" &&
            tomb.CurrentQuestion.Answers[1] == "B. 0 1 2",
            "Scholar question 1 content or opening transition changed.");

        Press(tomb, Keys.S);
        Press(tomb, Keys.Enter);
        Need(tomb.CorrectAnswerCount == 1 &&
            tomb.ReactionText == "Hm... your mind has not yet decayed.",
            "Scholar question 1 scoring failed.");
        Press(tomb, Keys.Enter);
        Need(tomb.CurrentQuestion.Title == "THE DAY YOU ENTERED THIS WORLD" &&
            tomb.CurrentQuestion.Answers[1] == "B. 01/01/2004",
            "Scholar question 2 content changed.");

        Press(tomb, Keys.Down);
        Press(tomb, Keys.Enter);
        Need(tomb.CorrectAnswerCount == 2,
            "Scholar question 2 scoring failed.");
        Press(tomb, Keys.Enter);
        Need(tomb.CurrentQuestion.Title == "BEASTS OF TWO AND FOUR LEGS" &&
            tomb.CurrentQuestion.Lines[0] == "Vừa gà vừa chó," &&
            tomb.CurrentQuestion.Answers[0] == "A. 22 gà — 14 chó",
            "Scholar question 3 Vietnamese content changed.");

        Press(tomb, Keys.Enter);
        Need(tomb.CorrectAnswerCount == 3,
            "Scholar question 3 scoring failed.");
        Press(tomb, Keys.Enter);
        Need(tomb.State == AncientScholarTombState.RewardChoice,
            "Perfect score did not open the reward category choice.");
        Press(tomb, Keys.Enter);
        Need(tomb.TryConsumeRewardRequest(out int score, out var category) &&
            score == 3 && category == AncientScholarRewardCategory.Weapon,
            "Perfect-score reward request was incorrect.");
        tomb.SetCompletionText("Reward granted.");
        Press(tomb, Keys.Enter);
        Need(tomb.State == AncientScholarTombState.Completed &&
            !tomb.TryInspect(tomb.Position),
            "Scholar challenge can be reopened after completion.");

        tomb.ResetChallengeForDebug();
        Need(tomb.State == AncientScholarTombState.Unopened,
            "Scholar debug reset failed.");
    }

    private static void Press(AncientScholarTombSystem tomb, Keys key)
    {
        tomb.Update(Frame, new KeyboardState(key), new KeyboardState());
        tomb.Update(Frame, new KeyboardState(), new KeyboardState(key));
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
