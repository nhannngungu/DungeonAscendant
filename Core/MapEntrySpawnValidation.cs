using System;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class MapEntrySpawnValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        bool originalShowcase = GameSession.DebugWildForestShowcase;
        GameSession.DebugWildForestShowcase = false;
        try
        {
            var session = new GameSession(new Rectangle(0, 0, 1280, 720), 1701);
            session.Update(Frame, new KeyboardState(Keys.Enter), new MouseState());
            ValidateAirborneSpawn(session, "Map 1 initial entry");
            LandUsingNormalPhysics(session, "Map 1 initial entry");

            session.Player.DebugGodMode = false;
            session.Player.ReceiveDamage(int.MaxValue);
            session.Update(Frame, new KeyboardState(), new MouseState());
            Need(session.State == GameState.GameOver, "Map 1 restart setup did not reach Game Over.");
            session.Update(Frame, new KeyboardState(Keys.R), new MouseState());
            Need(session.State == GameState.Playing, "Map 1 restart input failed.");
            ValidateAirborneSpawn(session, "Map 1 restart");
            LandUsingNormalPhysics(session, "Map 1 restart");

            session.Boss.ReceiveDamage(int.MaxValue);
            session.Update(Frame, new KeyboardState(), new MouseState());
            Need(session.IsExitUnlocked, "Map 1 exit did not unlock for transition validation.");
            session.Player.MoveTo(session.ExitPosition);
            session.Update(Frame, new KeyboardState(Keys.E), new MouseState());
            Need(session.DungeonDepth == 2 && session.CurrentDungeon.IsAncientCatacombs,
                "Map 1 to Map 2 transition failed.");
            ValidateAirborneSpawn(session, "Map 2 transition entry");
            Need(MathF.Abs(session.Player.Position.X - 200f) < .1f &&
                MathF.Abs(session.Player.Position.Y - 584f) < .1f,
                $"Map 2 safe entry changed unexpectedly: {session.Player.Position}.");
            Need(session.Player.DebugGodMode,
                "God Mode state was changed by the map transition.");
            LandUsingNormalPhysics(session, "Map 2 transition entry");
            Need(MathF.Abs(session.Player.Position.Y - 624f) <= 1f,
                "Map 2 Player did not land on the Y=652 entrance floor.");

            Vector2 repeated = SideScrollingCollision.ResolveSafeEntrySpawn(
                session.CurrentDungeon.StartRoom.Bounds.Left + 120f,
                session.Player.Size,
                session.CurrentDungeon,
                GameSession.MapEntryDropOffset);
            Need(Vector2.DistanceSquared(repeated, new Vector2(200f, 584f)) < .01f,
                "Reusable Map 2 entry resolution is not deterministic.");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalShowcase;
        }
    }

    private static void ValidateAirborneSpawn(GameSession session, string label)
    {
        Need(session.IsMapEntryFallActive, $"{label} did not begin in fall-in state.");
        Need(!session.Player.IsGrounded, $"{label} incorrectly began grounded.");
        Need(SideScrollingCollision.IsPositionFree(
            session.Player.Position,
            session.Player.Size,
            session.CurrentDungeon),
            $"{label} overlaps terrain.");
        Need(session.CurrentDungeon.WorldBounds.Contains(session.Player.Bounds),
            $"{label} is outside world bounds.");
        foreach (Enemy enemy in session.Enemies.Enemies)
            Need(!session.Player.Bounds.Intersects(enemy.Bounds),
                $"{label} overlaps {enemy.Type}.");
        Need(session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
            $"{label} camera is outside world bounds.");
    }

    private static void LandUsingNormalPhysics(GameSession session, string label)
    {
        float spawnX = session.Player.Position.X;
        float spawnY = session.Player.Position.Y;
        for (int frame = 0; frame < 120 && session.IsMapEntryFallActive; frame++)
        {
            session.Update(
                Frame,
                new KeyboardState(Keys.D, Keys.Space, Keys.LeftShift),
                new MouseState());
        }

        Need(!session.IsMapEntryFallActive && session.Player.IsGrounded,
            $"{label} did not land through normal gravity.");
        Need(session.Player.Position.Y > spawnY,
            $"{label} did not visibly descend.");
        Need(MathF.Abs(session.Player.Position.X - spawnX) < .1f,
            $"{label} entry lock allowed horizontal movement during the fall.");
        Need(SideScrollingCollision.IsSupported(
            session.Player.Position,
            session.Player.Size,
            session.CurrentDungeon),
            $"{label} landed without floor support.");
        Need(SideScrollingCollision.IsPositionFree(
            session.Player.Position,
            session.Player.Size,
            session.CurrentDungeon),
            $"{label} landed embedded in terrain.");
        Need(session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
            $"{label} camera left world bounds during the fall.");
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
