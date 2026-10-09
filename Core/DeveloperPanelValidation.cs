using System;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public static class DeveloperPanelValidation
{
    private static readonly GameTime Frame = new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        Need(GameSession.DeveloperModeEnabled, "Developer mode is unavailable in this build.");
        bool originalShowcase = GameSession.DebugWildForestShowcase;
        GameSession.DebugWildForestShowcase = false;
        try
        {
            var session = new GameSession(new Rectangle(0, 0, 1280, 720), 4401);
            Press(session, Keys.Enter);
            SettleEntry(session);
            int inventoryCount = session.Player.Inventory.Count;
            var weapon = session.Player.WeaponFamily;

            Press(session, Keys.F10);
            Need(session.DeveloperPanel.IsOpen, "F10 did not open the panel.");
            Vector2 pausedPosition = session.Player.Position;
            Tick(session, 1f);
            Need(session.Player.Position == pausedPosition, "Gameplay moved while panel was open.");
            int initialSelection = session.DeveloperPanel.SelectedIndex;
            Press(session, Keys.S);
            Need(session.DeveloperPanel.SelectedIndex == initialSelection + 1,
                "S did not move panel selection.");
            Press(session, Keys.W);
            Need(session.DeveloperPanel.SelectedIndex == initialSelection,
                "W did not move panel selection.");

            bool godMode = session.DebugGodMode;
            Press(session, Keys.Enter);
            Need(session.DebugGodMode != godMode, "God Mode panel toggle failed.");
            Press(session, Keys.Enter);
            Need(session.DebugGodMode == godMode, "God Mode panel restore failed.");

            session.Player.DebugGodMode = false;
            session.Player.ReceiveDamage(25);
            session.ExecuteDeveloperCommand(DeveloperCommand.HealPlayer, 1, 0);
            Need(session.Player.CurrentHealth == session.Player.MaxHealth, "Heal command failed.");
            session.Player.Combat.Stamina.SpendUpTo(40f);
            session.ExecuteDeveloperCommand(DeveloperCommand.RefillStamina, 1, 0);
            Need(session.Player.Combat.Stamina.IsFull, "Stamina refill failed.");
            session.Curse.Add(70f);
            session.ExecuteDeveloperCommand(DeveloperCommand.ClearCurse, 1, 0);
            Need(session.Curse.Value == 0f, "Clear Curse failed.");
            session.Player.DebugGodMode = godMode;

            session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 2, 0);
            ValidateSafeDebugArrival(session, 2, "Map 2 teleport");
            Press(session, Keys.Escape);
            Need(!session.DeveloperPanel.IsOpen, "Escape did not close the panel.");
            SettleEntry(session);
            Press(session, Keys.F10);

            for (int map = 1; map <= 2; map++)
            {
                session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, map, 0);
                int zoneCount = map == 1
                    ? session.CurrentDungeon.WildForestSections.Count
                    : session.CurrentDungeon.CatacombZones.Count;
                for (int zone = 0; zone < zoneCount; zone++)
                {
                    session.ExecuteDeveloperCommand(
                        DeveloperCommand.TeleportZone, map, zone);
                    Need(session.GetCurrentZoneIndex() == zone,
                        $"Map {map} zone {zone} teleport selected the wrong zone.");
                    ValidateSafeDebugArrival(session, map, $"Map {map} zone {zone}");
                }
            }

            ValidateEncounterCommands(session);

            for (int switchIndex = 0; switchIndex < 10; switchIndex++)
            {
                int map = switchIndex % 2 == 0 ? 1 : 2;
                session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, map, 0);
                ValidateSafeDebugArrival(session, map, $"stress switch {switchIndex + 1}");
                Need(session.Enemies.Enemies.Count == 0,
                    "Map switch retained or duplicated enemies.");
                Need(session.Projectiles.Projectiles.Count == 0 &&
                    session.RootHazards.Hazards.Count == 0 &&
                    session.GroundRunes.Count == 0,
                    "Map switch retained transient combat state.");
                if (map == 1)
                    Need(session.Curse.Value == 0f, "Map 1 retained Catacomb Curse.");
            }

            Need(session.Player.Inventory.Count == inventoryCount &&
                session.Player.WeaponFamily == weapon,
                "Map teleport changed inventory or equipped weapon.");

            session.ExecuteDeveloperCommand(DeveloperCommand.ToggleMap01Cleared, 1, 0);
            Need(session.Map01Cleared, "Map 1 flag toggle failed.");
            session.ExecuteDeveloperCommand(DeveloperCommand.ToggleMap02Cleared, 1, 0);
            Need(session.Map02Cleared, "Map 2 flag toggle failed.");
            session.EnemyIntroductions.Reset();
            session.ExecuteDeveloperCommand(DeveloperCommand.ResetEnemyIntroductions, 1, 0);
            Need(session.EnemyIntroductions.DiscoveryCount == 0,
                "Enemy introduction reset failed.");
            bool mapDebug = session.ShowMapDebug;
            session.ExecuteDeveloperCommand(DeveloperCommand.ToggleMapDebug, 1, 0);
            Need(session.ShowMapDebug != mapDebug, "Map Debug command failed.");
            session.ExecuteDeveloperCommand(DeveloperCommand.ToggleMapDebug, 1, 0);

            Press(session, Keys.Escape);
            Need(!session.DeveloperPanel.IsOpen, "Panel did not close after stress test.");
            Need(session.State == GameState.Playing, "Panel changed normal pause state.");
        }
        finally
        {
            GameSession.DebugWildForestShowcase = originalShowcase;
        }
    }

    private static void ValidateEncounterCommands(GameSession session)
    {
        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 1, 0);
        Press(session, Keys.Escape);
        SettleEntry(session);
        var zone = session.CurrentDungeon.EncounterZones[0];
        Vector2 trigger = SideScrollingCollision.ResolveSafeEntrySpawn(
            zone.ActivationBounds.Left + 3f,
            session.Player.Size,
            session.CurrentDungeon,
            dropOffset: 0f);
        session.Player.MoveTo(trigger);
        for (int frame = 0; frame < 40; frame++)
        {
            session.Update(
                new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05f)),
                new KeyboardState(),
                new MouseState());
        }
        Need(session.WildForestEncounters.IsTriggered(zone.ZoneId) &&
            session.Enemies.Enemies.Count > 0,
            "Encounter fixture did not trigger.");

        Press(session, Keys.F10);
        session.ExecuteDeveloperCommand(
            DeveloperCommand.ResetCurrentEncounter, 1, 0);
        Need(!session.WildForestEncounters.IsTriggered(zone.ZoneId) &&
            session.Enemies.Enemies.Count == 0,
            "Reset Current Encounter did not restore the zone.");

        Press(session, Keys.Escape);
        for (int frame = 0; frame < 40; frame++)
        {
            session.Update(
                new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05f)),
                new KeyboardState(),
                new MouseState());
        }
        Need(session.WildForestEncounters.IsTriggered(zone.ZoneId) &&
            session.Enemies.Enemies.Count > 0,
            "Reset encounter could not trigger again.");
        Press(session, Keys.F10);
        session.ExecuteDeveloperCommand(
            DeveloperCommand.KillActiveEnemies, 1, 0);
        foreach (var enemy in session.Enemies.Enemies)
            Need(!enemy.IsAlive, "Kill Active Enemies left an active enemy alive.");

        session.ExecuteDeveloperCommand(DeveloperCommand.TeleportZone, 1, 7);
        session.Boss.ReceiveDamage(int.MaxValue);
        Need(!session.Boss.IsAlive, "Boss reset fixture did not die.");
        session.ExecuteDeveloperCommand(DeveloperCommand.ResetCurrentBoss, 1, 7);
        Need(session.Boss.IsAlive && !session.BossDefeated,
            "Reset Current Boss did not restore Ancient Treant.");
    }

    private static void ValidateSafeDebugArrival(
        GameSession session,
        int expectedMap,
        string label)
    {
        Need(session.DungeonDepth == expectedMap, $"{label}: wrong map.");
        Need(session.IsMapEntryFallActive && !session.Player.IsGrounded,
            $"{label}: arrival is not airborne.");
        Need(SideScrollingCollision.IsPositionFree(
            session.Player.Position, session.Player.Size, session.CurrentDungeon),
            $"{label}: Player overlaps terrain.");
        Need(session.CurrentDungeon.WorldBounds.Contains(session.Player.Bounds),
            $"{label}: Player is outside world bounds.");
        Need(session.CurrentDungeon.WorldBounds.Contains(session.Camera.ViewBounds),
            $"{label}: camera is outside world bounds.");
    }

    private static void SettleEntry(GameSession session)
    {
        for (int frame = 0; frame < 90 && session.IsMapEntryFallActive; frame++)
            session.Update(Frame, new KeyboardState(), new MouseState());
        Need(!session.IsMapEntryFallActive && session.Player.IsGrounded,
            "Safe debug arrival did not land through gravity.");
    }

    private static void Press(GameSession session, Keys key)
    {
        session.Update(Frame, new KeyboardState(key), new MouseState());
        session.Update(Frame, new KeyboardState(), new MouseState());
    }

    private static void Tick(GameSession session, float seconds)
    {
        session.Update(
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds)),
            new KeyboardState(),
            new MouseState());
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
