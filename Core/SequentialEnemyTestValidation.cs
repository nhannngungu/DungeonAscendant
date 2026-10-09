using System;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class SequentialEnemyTestValidation
{
    private static readonly EnemyType[] ExpectedEnemyTypes =
    {
        EnemyType.Goblin,
        EnemyType.GoblinHunter,
        EnemyType.DireWolf,
        EnemyType.GiantSpider,
        EnemyType.BloodBat,
        EnemyType.ThornCrawler,
        EnemyType.CorruptedTreant,
        EnemyType.GoblinChief,
        EnemyType.MotherSpider
    };

    private static readonly float[] ExpectedGaps =
    {
        800f, 800f, 850f, 850f, 850f, 1000f, 1000f, 1050f, 1300f
    };

    public static void ValidateOrThrow()
    {
        bool originalMode = GameSession.DebugWildForestShowcase;

        try
        {
            GameSession.DebugWildForestShowcase = true;
            ValidateSequence();
            GameSession.DebugWildForestShowcase = false;
            ValidateAuthoredMapSpawnDeferral();
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalMode;
        }
    }

    private static void ValidateSequence()
    {
        var session = StartSession(randomSeed: 90210);
        int startingInventoryCount = session.Player.Inventory.Count;
        int startingExperience = session.Player.CurrentExperience;

        Require(session.IsWildForestShowcaseMode, "showcase enabled");
        Require(session.WildForestShowcaseEnemies.Count == ExpectedEnemyTypes.Length,
            "all nine regular enemies exist at test start");
        Require(session.Boss.IsAlive && session.ShouldRenderBoss,
            "Ancient Treant exists at test start");
        Require(session.CurrentDungeon.StartRoom.Bounds.Width ==
            Dungeon.DungeonGenerator.WildForestShowcaseRoomWidth,
            "wide deterministic showcase room");

        float previousX = session.Player.Position.X;

        for (int index = 0; index < ExpectedEnemyTypes.Length; index++)
        {
            Enemy enemy = session.WildForestShowcaseEnemies[index];
            Require(enemy != null && enemy.IsAlive, $"enemy {index} alive");
            Require(enemy.Type == ExpectedEnemyTypes[index], $"order {index}");
            Require(enemy.Position.X > previousX, $"left-to-right {index}");
            float expectedGap = index == 0
                ? GameSession.WildForestShowcasePlayerStartDistance
                : ExpectedGaps[index - 1];
            Require(MathF.Abs(enemy.Position.X - previousX - expectedGap) < 0.1f,
                $"spacing {index}");
            Require(SideScrollingCollision.IsPositionFree(
                    enemy.Position,
                    enemy.Size,
                    session.CurrentDungeon),
                $"collision-free spawn {index}");
            previousX = enemy.Position.X;
        }

        Enemy bat = session.WildForestShowcaseEnemies[4];
        Require(MathF.Abs(
                session.CurrentDungeon.StartRoom.GroundY - bat.Position.Y -
                GameSession.WildForestShowcaseBloodBatHeight) < 0.1f,
            "Blood Bat flight height");
        Require(MathF.Abs(session.Boss.Position.X - previousX - ExpectedGaps[^1]) < 0.1f,
            "Ancient Treant spacing");

        Enemy first = session.WildForestShowcaseEnemies[0];
        Enemy second = session.WildForestShowcaseEnemies[1];
        Require(session.Enemies.IsActive(
                first, session.Player.Position, session.CurrentDungeon),
            "nearby first enemy active");
        Require(!session.Enemies.IsActive(
                second, session.Player.Position, session.CurrentDungeon),
            "distant enemy inactive");
        Tick(session, .1f);
        Require(!session.Boss.IsActivated,
            "Ancient Treant does not activate early");

        session.Player.MoveTo(session.Boss.Position + new Vector2(-300f, 0f));
        Tick(session, .1f);
        Require(session.Boss.IsActivated, "Ancient Treant activates nearby");

        Enemy mother = session.WildForestShowcaseEnemies[8];
        session.Player.MoveTo(mother.Position + new Vector2(-200f, 0f));
        Tick(session, 3.1f);
        Tick(session, MotherSpider.SummonWindupSeconds + 0.01f);
        Enemy spiderling = null;

        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy.Type == EnemyType.Spiderling)
            {
                spiderling = enemy;
                break;
            }
        }

        Require(spiderling != null &&
            MathF.Abs(spiderling.Position.X - mother.Position.X) <=
                EnemyManager.WildForestShowcaseSummonRadius,
            "Mother Spider summons stay local");

        first.ReceiveDamage(int.MaxValue);
        Tick(session, .1f);
        Require(!first.IsAlive && second.IsAlive,
            "death does not replace or clear lineup");
        Require(session.Player.CurrentExperience == startingExperience,
            "experience suppressed");
        Require(session.Loot.Drops.Count == 0, "loot suppressed");
        Require(session.Player.Inventory.Count == startingInventoryCount,
            "inventory not flooded");
        Require(session.KillCount == 0 && session.DungeonDepth == 1,
            "enemy progression suppressed");

        session.Boss.ReceiveDamage(int.MaxValue);
        Tick(session, .1f);
        Require(!session.BossDefeated, "normal boss progression suppressed");
        Require(session.DungeonDepth == 1, "Dungeon Depth unchanged");

        Press(session, Keys.F6);
        Require(session.WildForestShowcaseEnemies.Count == ExpectedEnemyTypes.Length,
            "F6 restores full lineup");
        Require(session.WildForestShowcaseEnemies[0].IsAlive && session.Boss.IsAlive,
            "F6 restores defeated enemies");
        Require(MathF.Abs(
                session.WildForestShowcaseEnemies[0].Position.X -
                session.Player.Position.X -
                GameSession.WildForestShowcasePlayerStartDistance) < 0.1f,
            "F6 returns player to test start");
    }

    private static void ValidateAuthoredMapSpawnDeferral()
    {
        GameSession session = StartSession(randomSeed: 90210);
        Require(!session.IsWildForestShowcaseMode, "debug mode disabled");
        Require(session.CurrentDungeon.IsAuthoredWildForest,
            "authored Map 1 active");
        Require(session.Enemies.Enemies.Count == 0,
            "legacy random population deferred");
        Require(session.CurrentDungeon.EncounterZones.Count > 0 &&
            session.CurrentDungeon.SpawnSockets.Count > 0,
            "future encounter metadata retained");
        Require(session.ShouldRenderBoss, "authored boss retained");
        Require(!session.ShowMapDebug,
            "environment debug overlay disabled at startup");
        Press(session, Keys.F3);
        Require(session.ShowCombatDebug && !session.ShowMapDebug,
            "combat debug does not enable environment guides");
        Press(session, Keys.F3);
        Press(session, Keys.F8);
        Require(session.ShowMapDebug,
            "F8 enables environment debug overlay");
        Press(session, Keys.F8);
        Require(!session.ShowMapDebug,
            "F8 restores clean gameplay view");
    }

    private static GameSession StartSession(int randomSeed)
    {
        var session = new GameSession(
            new Rectangle(0, 0, 800, 480),
            randomSeed);
        Press(session, Keys.Enter);
        return session;
    }

    private static void Press(GameSession session, Keys key)
    {
        Update(session, 1f / 60f, new KeyboardState(key));
        Update(session, 1f / 60f, new KeyboardState());
    }

    private static void Tick(GameSession session, float seconds)
    {
        Update(session, seconds, new KeyboardState());
    }

    private static void Update(
        GameSession session,
        float seconds,
        KeyboardState keyboardState)
    {
        session.Update(
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds)),
            keyboardState,
            default);
    }

    private static int CountMainEnemies(GameSession session)
    {
        int count = 0;

        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy.IsAlive && enemy.Type != EnemyType.Spiderling)
                count++;
        }

        return count;
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Sequential enemy test validation failed: {scenario}.");
        }
    }
}
