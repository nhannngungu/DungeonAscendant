using System;
using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using DungeonAscendant.UI;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Owns MonoGame-specific drawing for the active game session.
/// </summary>
public sealed class GameRenderer : IDisposable
{
    private static readonly IReadOnlyDictionary<char, byte[]> DebugGlyphs =
        new Dictionary<char, byte[]>
        {
            ['A'] = new byte[] { 14, 17, 17, 31, 17, 17, 17 },
            ['B'] = new byte[] { 30, 17, 17, 30, 17, 17, 30 },
            ['C'] = new byte[] { 15, 16, 16, 16, 16, 16, 15 },
            ['D'] = new byte[] { 30, 17, 17, 17, 17, 17, 30 },
            ['E'] = new byte[] { 31, 16, 16, 30, 16, 16, 31 },
            ['F'] = new byte[] { 31, 16, 16, 30, 16, 16, 16 },
            ['G'] = new byte[] { 14, 17, 16, 23, 17, 17, 14 },
            ['H'] = new byte[] { 17, 17, 17, 31, 17, 17, 17 },
            ['I'] = new byte[] { 31, 4, 4, 4, 4, 4, 31 },
            ['J'] = new byte[] { 7, 2, 2, 2, 18, 18, 12 },
            ['K'] = new byte[] { 17, 18, 20, 24, 20, 18, 17 },
            ['L'] = new byte[] { 16, 16, 16, 16, 16, 16, 31 },
            ['M'] = new byte[] { 17, 27, 21, 21, 17, 17, 17 },
            ['N'] = new byte[] { 17, 25, 21, 19, 17, 17, 17 },
            ['O'] = new byte[] { 14, 17, 17, 17, 17, 17, 14 },
            ['P'] = new byte[] { 30, 17, 17, 30, 16, 16, 16 },
            ['Q'] = new byte[] { 14, 17, 17, 17, 21, 18, 13 },
            ['R'] = new byte[] { 30, 17, 17, 30, 20, 18, 17 },
            ['S'] = new byte[] { 15, 16, 16, 14, 1, 1, 30 },
            ['T'] = new byte[] { 31, 4, 4, 4, 4, 4, 4 },
            ['U'] = new byte[] { 17, 17, 17, 17, 17, 17, 14 },
            ['V'] = new byte[] { 17, 17, 17, 17, 17, 10, 4 },
            ['W'] = new byte[] { 17, 17, 17, 21, 21, 21, 10 },
            ['X'] = new byte[] { 17, 17, 10, 4, 10, 17, 17 },
            ['Y'] = new byte[] { 17, 17, 10, 4, 4, 4, 4 },
            ['Z'] = new byte[] { 31, 1, 2, 4, 8, 16, 31 },
            ['0'] = new byte[] { 14, 17, 19, 21, 25, 17, 14 },
            ['1'] = new byte[] { 4, 12, 4, 4, 4, 4, 14 },
            ['2'] = new byte[] { 14, 17, 1, 2, 4, 8, 31 },
            ['3'] = new byte[] { 30, 1, 1, 14, 1, 1, 30 },
            ['4'] = new byte[] { 2, 6, 10, 18, 31, 2, 2 },
            ['5'] = new byte[] { 31, 16, 16, 30, 1, 1, 30 },
            ['6'] = new byte[] { 14, 16, 16, 30, 17, 17, 14 },
            ['7'] = new byte[] { 31, 1, 2, 4, 8, 8, 8 },
            ['8'] = new byte[] { 14, 17, 17, 14, 17, 17, 14 },
            ['9'] = new byte[] { 14, 17, 17, 15, 1, 1, 14 },
            [':'] = new byte[] { 0, 4, 4, 0, 4, 4, 0 },
            ['/'] = new byte[] { 1, 2, 2, 4, 8, 8, 16 },
            ['-'] = new byte[] { 0, 0, 0, 31, 0, 0, 0 },
            ['!'] = new byte[] { 4, 4, 4, 4, 4, 0, 4 },
            ['+'] = new byte[] { 0, 4, 4, 31, 4, 4, 0 },
            ['%'] = new byte[] { 17, 2, 4, 4, 8, 16, 17 }
        };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly PlayerSpriteRenderer _playerSpriteRenderer;
    private readonly GoblinFamilySpriteRenderer _goblinFamilySpriteRenderer;
    private readonly CorruptedBeastSpriteRenderer _corruptedBeastSpriteRenderer;
    private readonly WildForestAmbushSpriteRenderer _wildForestAmbushSpriteRenderer;
    private readonly WildForestHeavySpriteRenderer _wildForestHeavySpriteRenderer;
    private readonly WildForestApexSpriteRenderer _wildForestApexSpriteRenderer;

    public GameRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _playerSpriteRenderer = new PlayerSpriteRenderer(_spriteBatch, _pixel);
        _goblinFamilySpriteRenderer = new GoblinFamilySpriteRenderer(
            _spriteBatch,
            _pixel);
        _corruptedBeastSpriteRenderer = new CorruptedBeastSpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestAmbushSpriteRenderer = new WildForestAmbushSpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestHeavySpriteRenderer = new WildForestHeavySpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestApexSpriteRenderer = new WildForestApexSpriteRenderer(
            _spriteBatch,
            _pixel);
    }

    public void Draw(GameSession gameSession, GameTime gameTime)
    {
        RegionTheme theme = RegionTheme.For(gameSession.CurrentRegion);
        _graphicsDevice.Clear(theme.Background);

        if (gameSession.State == GameState.Start)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawArena();
            DrawStartScreen();
            _spriteBatch.End();
            return;
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: gameSession.Camera.Transform);
        DrawDungeon(gameSession, gameSession.Camera.ViewBounds);

        foreach (WebPatch patch in gameSession.Projectiles.WebPatches)
        {
            if (patch.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawWebPatch(patch);
        }

        foreach (RootHazard hazard in gameSession.RootHazards.Hazards)
        {
            if (hazard.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawRootHazard(hazard);
        }

        if (gameSession.Chest.Bounds.Intersects(gameSession.Camera.ViewBounds))
            DrawTreasureChest(gameSession.Chest);

        foreach (WorldLoot loot in gameSession.Loot.Drops)
        {
            if (loot.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawWorldLoot(loot);
        }

        if (gameSession.ShowCombatDebug &&
            gameSession.Player.Combat.IsAttackActive &&
            gameSession.Player.Combat.CurrentAttack?.Delivery == AttackDelivery.Melee)
            DrawAttackArea(gameSession.PlayerAttackArea);

        DrawRangerTrajectoryGuide(gameSession);
        DrawSpellbladeWorld(gameSession);
        DrawBreakerWorld(gameSession);

        _playerSpriteRenderer.Draw(
            gameSession.Player,
            gameTime,
            gameSession.Player.WeaponFamily == WeaponFamily.ChainFlail
                ? gameSession.PlayerAttackArea.Center.ToVector2()
                : null);

        foreach (Enemy enemy in gameSession.Enemies.DefeatPresentations)
        {
            if (enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawEnemy(enemy, gameTime);
                DrawArcaneImprints(
                    GetEnemyVisualBounds(enemy),
                    enemy.ArcaneImprintCount,
                    bound: enemy.IsArcaneStunned ||
                        IsDominionBindingVisible(
                            gameSession,
                            enemy.Position));
        }

        foreach (Enemy enemy in gameSession.Enemies.Enemies)
        {
            if (enemy.IsAlive &&
                enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
            {
                DrawEnemy(enemy, gameTime);

                if (gameSession.IsWildForestShowcaseMode &&
                    enemy.Type != EnemyType.Spiderling)
                {
                    DrawWildForestShowcaseLabel(
                        GameSession.GetWildForestShowcaseLabel(enemy.Type),
                        GetEnemyVisualBounds(enemy));
                }
            }
        }

        foreach (Projectile projectile in gameSession.Projectiles.Projectiles)
        {
            if (projectile.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawProjectile(projectile);
        }

        foreach (ProjectileImpact impact in gameSession.Projectiles.Impacts)
            DrawRangerImpact(impact);

        if (gameSession.ShouldRenderBoss &&
            (gameSession.Boss.IsAlive ||
             gameSession.Boss.DeathPresentationProgress < 1f) &&
            gameSession.Boss.Bounds.Intersects(gameSession.Camera.ViewBounds))
        {
            DrawAncientTreant(gameSession.Boss, gameTime);
            DrawArcaneImprints(
                gameSession.Boss.Bounds,
                gameSession.Boss.ArcaneImprintCount,
                bound: IsDominionBindingVisible(
                    gameSession,
                    gameSession.Boss.Position));

            if (gameSession.IsWildForestShowcaseMode)
            {
                DrawWildForestShowcaseLabel(
                    "ANCIENT TREANT",
                    new Rectangle(
                        (int)MathF.Round(gameSession.Boss.Position.X -
                            WildForestApexSpriteRenderer.AncientTreantVisualSize.X / 2f),
                        gameSession.Boss.Bounds.Bottom -
                            (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.Y,
                        (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.X,
                        (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.Y));
            }
        }

        _playerSpriteRenderer.DrawHitImpact(
            gameSession.Player,
            gameTime,
            gameSession.PlayerHitEffectId,
            gameSession.PlayerHitEffectPosition);

        if (gameSession.ShowCombatDebug)
            DrawCombatDebug(gameSession);

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHudPanel();
        DrawPlayerHealth(gameSession.Player);
        DrawPlayerStamina(gameSession.Player);
        DrawPlayerProgression(gameSession.Player, gameSession.KillCount);
        DrawWeaponResource(gameSession.Player);
        DrawTechniqueFeedback(gameSession.Player);
        DrawDungeonStatus(gameSession);

        if (gameSession.IsWildForestShowcaseMode)
            DrawSequentialEnemyTestStatus(gameSession);

        if (gameSession.ShowCombatDebug)
            DrawCombatStateDebug(gameSession.Player);

        if (gameSession.WorldTierTransitionProgress > 0f)
            DrawWorldTierTransition(gameSession);

        if (gameSession.IsInventoryOpen)
            DrawInventoryOverlay(gameSession);
        else if (gameSession.State == GameState.Paused)
            DrawPauseOverlay();
        else if (gameSession.State == GameState.GameOver)
            DrawGameOverOverlay();

        if (gameSession.IsFusionFeedbackVisible)
            DrawFusionFeedback(gameSession);

        _spriteBatch.End();
    }

    private void DrawDungeon(GameSession gameSession, Rectangle cameraBounds)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        RegionTheme theme = RegionTheme.For(gameSession.CurrentRegion);
        Rectangle visibleBounds = cameraBounds;
        visibleBounds.Inflate(80, 80);

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            _spriteBatch.Draw(_pixel, room.Bounds, theme.GetRoomFloor(room.Type));
            DrawRectangleOutline(room.Bounds, 5, theme.BoundaryAccent);
        }

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (!corridor.Intersects(visibleBounds))
                continue;

            _spriteBatch.Draw(_pixel, corridor, theme.CorridorFloor);
            DrawRectangleOutline(corridor, 3, theme.BoundaryAccent);
        }

        foreach (Platform platform in dungeon.Platforms)
        {
            if (!platform.Bounds.Intersects(visibleBounds))
                continue;

            Color color = platform.Kind switch
            {
                PlatformKind.Raised => theme.BoundaryAccent,
                PlatformKind.Obstacle => theme.Root,
                PlatformKind.Transition => theme.CorridorFloor,
                _ => theme.Boundary
            };
            _spriteBatch.Draw(_pixel, platform.Bounds, color);
            DrawRectangleOutline(platform.Bounds, 3, theme.BoundaryAccent);

            if (platform.Kind == PlatformKind.Ground ||
                platform.Kind == PlatformKind.Transition)
            {
                DrawForestGroundDetails(platform.Bounds, visibleBounds, theme);
            }
        }

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            if (room.Type == RoomType.Start)
                DrawStartMarker(new Vector2(room.Bounds.Left + 110f, room.GroundY - 35f));
            else if (room.Type == RoomType.Treasure)
                DrawTreasureRoomMarker(gameSession.Chest.Position);
            else if (room.Type == RoomType.Boss)
                DrawBossRoomMarker(new Vector2(room.Center.X, room.GroundY - 70f));
            else if (room.Type == RoomType.Exit)
                DrawExitMarker(gameSession.ExitPosition, gameSession.IsExitUnlocked);
        }
    }

    private void DrawFloorGrid(
        Rectangle area,
        Rectangle visibleBounds,
        Color color)
    {
        const int tileSize = 48;
        int firstX = Math.Max(area.Left, visibleBounds.Left);
        int firstY = Math.Max(area.Top, visibleBounds.Top);
        firstX = area.Left + ((firstX - area.Left) / tileSize) * tileSize;
        firstY = area.Top + ((firstY - area.Top) / tileSize) * tileSize;
        int right = Math.Min(area.Right, visibleBounds.Right);
        int bottom = Math.Min(area.Bottom, visibleBounds.Bottom);

        for (int x = firstX; x < right; x += tileSize)
            _spriteBatch.Draw(_pixel, new Rectangle(x, area.Top, 1, area.Height), color);

        for (int y = firstY; y < bottom; y += tileSize)
            _spriteBatch.Draw(_pixel, new Rectangle(area.Left, y, area.Width, 1), color);
    }

    private void DrawForestGroundDetails(
        Rectangle area,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        const int cellSize = 64;
        int firstX = Math.Max(area.Left, visibleBounds.Left);
        int firstY = Math.Max(area.Top, visibleBounds.Top);
        firstX = area.Left + ((firstX - area.Left) / cellSize) * cellSize;
        firstY = area.Top + ((firstY - area.Top) / cellSize) * cellSize;
        int right = Math.Min(area.Right, visibleBounds.Right);
        int bottom = Math.Min(area.Bottom, visibleBounds.Bottom);

        for (int y = firstY; y < bottom; y += cellSize)
        {
            for (int x = firstX; x < right; x += cellSize)
            {
                int hash = unchecked(x * 73856093 ^ y * 19349663);
                int offsetX = 8 + Math.Abs(hash % 29);
                int offsetY = 8 + Math.Abs(hash / 31 % 29);
                int detailX = Math.Min(x + offsetX, area.Right - 13);
                int detailY = Math.Min(y + offsetY, area.Bottom - 8);

                if ((hash & 3) == 0)
                {
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX, detailY, 13, 6),
                        theme.Corruption);
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX + 4, detailY - 4, 5, 14),
                        theme.Corruption);
                }
                else
                {
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX, detailY, 15, 3),
                        theme.GroundMarking);
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX + 2, detailY - 3, 3, 8),
                        theme.Root);
                }
            }
        }
    }

    private void DrawForestBoundaryDetails(
        Rectangle wall,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        if (!wall.Intersects(visibleBounds))
            return;

        for (int x = wall.Left + 18; x < wall.Right - 18; x += 72)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wall.Top - 4, 5, 12),
                theme.Root);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x + 5, wall.Top - 7, 7, 4),
                theme.BoundaryAccent);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wall.Bottom - 8, 5, 12),
                theme.Root);
        }

        for (int y = wall.Top + 22; y < wall.Bottom - 18; y += 72)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wall.Left - 4, y, 12, 5),
                theme.Root);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wall.Right - 8, y, 12, 5),
                theme.Root);
        }
    }

    private void DrawStartMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 25, (int)center.Y - 25, 50, 50);
        var inner = new Rectangle((int)center.X - 17, (int)center.Y - 17, 34, 34);
        DrawRectangleOutline(outer, 3, new Color(76, 169, 126, 190));
        DrawRectangleOutline(inner, 2, new Color(111, 205, 157, 150));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 3, (int)center.Y - 13, 6, 26), new Color(128, 218, 169, 165));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 13, (int)center.Y - 3, 26, 6), new Color(128, 218, 169, 165));
    }

    private void DrawTreasureRoomMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 44, (int)center.Y - 38, 88, 76);
        DrawRectangleOutline(outer, 3, new Color(191, 161, 72, 160));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(outer.X + 8, outer.Y + 8, 8, 8),
            new Color(226, 196, 91, 145));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(outer.Right - 16, outer.Bottom - 16, 8, 8),
            new Color(226, 196, 91, 145));
    }

    private void DrawBossRoomMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 72, (int)center.Y - 62, 144, 124);
        DrawRectangleOutline(outer, 5, new Color(139, 45, 48, 175));
        DrawRectangleOutline(
            new Rectangle(outer.X + 12, outer.Y + 12, outer.Width - 24, outer.Height - 24),
            2,
            new Color(188, 68, 57, 120));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle((int)center.X - 30, (int)center.Y - 3, 60, 6),
            new Color(155, 54, 48, 115));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle((int)center.X - 3, (int)center.Y - 30, 6, 60),
            new Color(155, 54, 48, 115));
    }

    private void DrawExitMarker(Vector2 center, bool isUnlocked)
    {
        var frame = new Rectangle((int)center.X - 28, (int)center.Y - 34, 56, 68);
        var doorway = new Rectangle(frame.X + 9, frame.Y + 10, frame.Width - 18, frame.Height - 10);
        Color frameColor = isUnlocked
            ? new Color(108, 155, 84, 220)
            : new Color(136, 79, 52, 220);
        Color outlineColor = isUnlocked
            ? new Color(159, 226, 121)
            : new Color(218, 99, 73);
        _spriteBatch.Draw(_pixel, frame, frameColor);
        _spriteBatch.Draw(_pixel, doorway, new Color(30, 21, 43));
        DrawRectangleOutline(frame, 3, outlineColor);

        if (isUnlocked)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(doorway.Center.X - 3, doorway.Center.Y - 3, 6, 6),
                new Color(177, 235, 126));
            return;
        }

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.X + 7, doorway.Y + 4, 5, doorway.Height - 8),
            new Color(177, 79, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.Right - 12, doorway.Y + 4, 5, doorway.Height - 8),
            new Color(177, 79, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.X + 5, doorway.Center.Y - 3, doorway.Width - 10, 6),
            new Color(207, 94, 68));
    }

    private void DrawArena()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var arena = new Rectangle(0, 0, viewport.Width, viewport.Height);
        const int wallThickness = 16;
        const int tileSize = 48;

        _spriteBatch.Draw(_pixel, arena, new Color(37, 43, 46));

        for (int x = wallThickness; x < viewport.Width - wallThickness; x += tileSize)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wallThickness, 1, viewport.Height - wallThickness * 2),
                new Color(67, 75, 77, 80));
        }

        for (int y = wallThickness; y < viewport.Height - wallThickness; y += tileSize)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wallThickness, y, viewport.Width - wallThickness * 2, 1),
                new Color(67, 75, 77, 80));
        }

        Color wallColor = new(66, 61, 62);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, wallThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, viewport.Height - wallThickness, viewport.Width, wallThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, wallThickness, viewport.Height), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(viewport.Width - wallThickness, 0, wallThickness, viewport.Height), wallColor);

        for (int x = 0; x < viewport.Width; x += 64)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x, 0, 2, wallThickness), new Color(92, 84, 82));
            _spriteBatch.Draw(_pixel, new Rectangle(x + 32, viewport.Height - wallThickness, 2, wallThickness), new Color(92, 84, 82));
        }

        for (int y = 0; y < viewport.Height; y += 64)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, y, wallThickness, 2), new Color(92, 84, 82));
            _spriteBatch.Draw(_pixel, new Rectangle(viewport.Width - wallThickness, y + 32, wallThickness, 2), new Color(92, 84, 82));
        }

        DrawCornerBrazier(22, 22);
        DrawCornerBrazier(viewport.Width - 34, 22);
        DrawCornerBrazier(22, viewport.Height - 34);
        DrawCornerBrazier(viewport.Width - 34, viewport.Height - 34);
    }

    private void DrawCornerBrazier(int x, int y)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y + 8, 12, 4), new Color(108, 75, 47));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 3, y + 3, 6, 7), new Color(226, 108, 37));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 5, y, 3, 6), new Color(250, 190, 54));
    }

    private void DrawAttackArea(Rectangle attackArea)
    {
        _spriteBatch.Draw(_pixel, attackArea, new Color(245, 210, 90, 55));
        DrawRectangleOutline(attackArea, 2, new Color(250, 225, 125, 150));
    }

    private void DrawCombatDebug(GameSession gameSession)
    {
        DrawDebugZone(
            gameSession.Player.Bounds,
            new Color(86, 220, 112, 35),
            new Color(86, 220, 112, 220));
        DrawDebugZone(
            gameSession.Player.DefenseBounds,
            new Color(76, 145, 230, 30),
            new Color(76, 170, 245, 220));

        foreach (Enemy enemy in gameSession.Enemies.Enemies)
        {
            if (!enemy.IsAlive ||
                !enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
                continue;

            DrawDebugZone(
                enemy.Bounds,
                new Color(225, 77, 181, 25),
                new Color(225, 77, 181, 190));

            if (enemy.Type != EnemyType.GoblinHunter &&
                (enemy.Attack.IsTelegraphing || enemy.Attack.IsActive))
            {
                Color attackColor = enemy.Attack.IsActive
                    ? new Color(237, 73, 64, 230)
                    : new Color(225, 151, 61, 190);
                DrawDebugZone(
                    enemy.AttackArea,
                    new Color(
                        attackColor.R,
                        attackColor.G,
                        attackColor.B,
                        (byte)(enemy.Attack.IsActive ? 42 : 20)),
                    attackColor);
            }
        }

        foreach (Projectile projectile in gameSession.Projectiles.Projectiles)
        {
            if (!projectile.Bounds.Intersects(gameSession.Camera.ViewBounds))
                continue;

            DrawDebugZone(
                projectile.Bounds,
                new Color(252, 224, 82, 45),
                new Color(252, 224, 82, 235));
        }

        if (gameSession.ShouldRenderBoss && gameSession.Boss.IsAlive)
        {
            DrawDebugZone(
                gameSession.Boss.Bounds,
                new Color(225, 77, 181, 25),
                new Color(225, 77, 181, 190));

            if (gameSession.Boss.Attack.IsTelegraphing ||
                gameSession.Boss.Attack.IsActive)
            {
                Color attackColor = gameSession.Boss.Attack.IsActive
                    ? new Color(237, 73, 64, 230)
                    : new Color(225, 151, 61, 190);
                DrawDebugZone(
                    gameSession.Boss.AttackArea,
                    new Color(
                        attackColor.R,
                        attackColor.G,
                        attackColor.B,
                        (byte)(gameSession.Boss.Attack.IsActive ? 42 : 20)),
                    attackColor);
            }
        }
    }

    private void DrawDebugZone(
        Rectangle bounds,
        Color fillColor,
        Color outlineColor)
    {
        _spriteBatch.Draw(_pixel, bounds, fillColor);
        DrawRectangleOutline(bounds, 2, outlineColor);
    }

    private void DrawLegacyPlayer(PlayerCharacter player)
    {
        Vector2 topLeft = player.Position - player.Size / 2f;
        int centerY = (int)player.Position.Y;
        bool isAttacking = player.Combat.CurrentAttack != null &&
            (player.Combat.State == CombatState.LightAttack ||
             player.Combat.State == CombatState.HeavyAttack);
        int swordLength = player.Combat.CurrentAttack?.Kind switch
        {
            AttackKind.LightTwo => 55,
            AttackKind.LightThree => 64,
            AttackKind.Heavy => 76,
            AttackKind.LightOne => 49,
            _ => 28
        };
        int swordOffsetY = player.Combat.CurrentAttack?.Kind switch
        {
            AttackKind.LightTwo => -9,
            AttackKind.LightThree => 8,
            AttackKind.Heavy => 11,
            _ => 3
        };
        const int swordThickness = 6;

        Rectangle shadow = new(
            (int)topLeft.X + 4,
            (int)(topLeft.Y + player.Size.Y) - 5,
            (int)player.Size.X - 8,
            8);
        Rectangle body = new(
            (int)topLeft.X + 5,
            (int)topLeft.Y + 22,
            (int)player.Size.X - 10,
            (int)player.Size.Y - 22);
        Rectangle head = new(
            (int)topLeft.X + 10,
            (int)topLeft.Y,
            (int)player.Size.X - 20,
            24);
        Rectangle leftLeg = new(body.X + 4, body.Bottom - 5, 8, 10);
        Rectangle rightLeg = new(body.Right - 12, body.Bottom - 5, 8, 10);
        Rectangle leftArm = new(body.X - 4, body.Y + 5, 6, 19);
        Rectangle rightArm = new(body.Right - 2, body.Y + 5, 6, 19);
        Rectangle belt = new(body.X, body.Y + 17, body.Width, 5);

        Rectangle swordBlade;
        Rectangle swordGuard;
        Rectangle swordHandle;

        if (player.Facing == FacingDirection.Left)
        {
            swordBlade = new Rectangle(
                (int)topLeft.X - swordLength + 3,
                centerY + swordOffsetY,
                swordLength,
                swordThickness);
            swordGuard = new Rectangle(
                swordBlade.Right - 3,
                swordBlade.Y - 4,
                6,
                14);
            swordHandle = new Rectangle(
                swordGuard.Right - 1,
                swordBlade.Y,
                9,
                swordThickness);
        }
        else
        {
            swordBlade = new Rectangle(
                (int)(topLeft.X + player.Size.X) - 3,
                centerY + swordOffsetY,
                swordLength,
                swordThickness);
            swordGuard = new Rectangle(
                swordBlade.X - 3,
                swordBlade.Y - 4,
                6,
                14);
            swordHandle = new Rectangle(
                swordGuard.X - 8,
                swordBlade.Y,
                9,
                swordThickness);
        }

        Rectangle eye = CreatePlayerEye(head, player.Facing);
        Color bodyColor = player.IsHitFlashing
            ? new Color(220, 75, 76)
            : player.Combat.IsGuardBroken
                ? new Color(201, 103, 58)
                : player.Combat.IsBlockFeedbackActive
                    ? new Color(105, 174, 211)
                    : player.Combat.IsDodgeInvulnerable
                        ? new Color(105, 157, 196)
                        : new Color(48, 112, 168);
        Color headColor = player.IsHitFlashing
            ? new Color(255, 220, 205)
            : player.Combat.IsGuardBroken
                ? new Color(237, 167, 112)
                : new Color(232, 185, 137);
        Color bladeColor = player.Combat.IsAttackWindingUp
            ? new Color(224, 151, 69)
            : isAttacking
                ? new Color(245, 238, 185)
                : new Color(205, 215, 226);

        if (player.IsGrounded)
            _spriteBatch.Draw(_pixel, shadow, new Color(16, 19, 22, 150));
        _spriteBatch.Draw(_pixel, swordHandle, new Color(91, 57, 38));
        _spriteBatch.Draw(_pixel, swordBlade, bladeColor);
        _spriteBatch.Draw(_pixel, swordGuard, new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, leftLeg, new Color(34, 55, 79));
        _spriteBatch.Draw(_pixel, rightLeg, new Color(34, 55, 79));
        _spriteBatch.Draw(_pixel, body, bodyColor);
        _spriteBatch.Draw(_pixel, leftArm, bodyColor);
        _spriteBatch.Draw(_pixel, rightArm, bodyColor);
        _spriteBatch.Draw(_pixel, belt, new Color(86, 58, 42));
        _spriteBatch.Draw(_pixel, head, headColor);
        _spriteBatch.Draw(_pixel, eye, new Color(30, 25, 27));

        if (player.Combat.IsBlocking)
        {
            int shieldX = player.Facing == FacingDirection.Left
                ? (int)topLeft.X - 10
                : (int)(topLeft.X + player.Size.X) + 2;
            var shield = new Rectangle(shieldX, centerY - 17, 8, 34);
            Color shieldFill = player.Combat.IsBlockFeedbackActive
                ? new Color(178, 218, 235)
                : new Color(92, 129, 155);
            Color shieldEdge = player.Combat.IsBlockFeedbackActive
                ? new Color(244, 249, 238)
                : new Color(190, 204, 212);
            _spriteBatch.Draw(_pixel, shield, shieldFill);
            DrawRectangleOutline(shield, 2, shieldEdge);

            if (player.Combat.IsBlockFeedbackActive)
            {
                int impactX = player.Facing == FacingDirection.Left
                    ? shield.Left - 8
                    : shield.Right + 3;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX, centerY - 3, 6, 6),
                    new Color(244, 237, 172));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX + 2, centerY - 11, 3, 5),
                    new Color(232, 247, 250));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX + 2, centerY + 7, 3, 5),
                    new Color(232, 247, 250));
            }
        }

        if (player.IsSlowed)
        {
            Rectangle webOutline = player.Bounds;
            webOutline.Inflate(7, 5);
            DrawRectangleOutline(
                webOutline,
                2,
                new Color(205, 220, 211, 155));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(webOutline.X, webOutline.Center.Y, webOutline.Width, 2),
                new Color(180, 204, 194, 120));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(webOutline.Center.X, webOutline.Y, 2, webOutline.Height),
                new Color(180, 204, 194, 120));
        }
    }

    private static Rectangle CreatePlayerEye(
        Rectangle head,
        FacingDirection facingDirection)
    {
        return facingDirection == FacingDirection.Left
            ? new Rectangle(head.X + 3, head.Y + 8, 4, 4)
            : new Rectangle(head.Right - 7, head.Y + 8, 4, 4);
    }

    private void DrawEnemy(Enemy enemy, GameTime gameTime)
    {
        switch (enemy)
        {
            case Goblin goblin:
                _goblinFamilySpriteRenderer.Draw(goblin, gameTime);
                break;
            case DireWolf direWolf:
                _corruptedBeastSpriteRenderer.Draw(direWolf, gameTime);
                break;
            case GiantSpider giantSpider:
                _corruptedBeastSpriteRenderer.Draw(giantSpider, gameTime);
                break;
            case GoblinHunter goblinHunter:
                _goblinFamilySpriteRenderer.Draw(goblinHunter, gameTime);
                break;
            case ThornCrawler thornCrawler:
                _wildForestAmbushSpriteRenderer.Draw(thornCrawler, gameTime);
                break;
            case CorruptedTreant corruptedTreant:
                _wildForestHeavySpriteRenderer.Draw(corruptedTreant, gameTime);
                break;
            case BloodBat bloodBat:
                _wildForestAmbushSpriteRenderer.Draw(bloodBat, gameTime);
                break;
            case GoblinChief goblinChief:
                _wildForestHeavySpriteRenderer.Draw(goblinChief, gameTime);
                break;
            case MotherSpider motherSpider:
                _wildForestApexSpriteRenderer.Draw(motherSpider, gameTime);
                break;
            case Spiderling spiderling:
                _wildForestApexSpriteRenderer.Draw(spiderling, gameTime);
                break;
        }

        if (enemy.IsBuffed)
            DrawEnemyBuff(enemy);

        if (enemy.IsStaggered)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(enemy.Bounds.Center.X - 8, enemy.Bounds.Y - 9, 16, 4),
                new Color(225, 183, 244, 220));
        }
    }

    private void DrawDireWolf(DireWolf wolf)
    {
        Vector2 topLeft = wolf.Position - wolf.Size / 2f;
        Color fur = wolf.IsHitFlashing
            ? new Color(232, 230, 211)
            : wolf.IsRetreating
                ? new Color(116, 96, 92)
                : new Color(83, 89, 76);
        Color darkFur = wolf.IsRetreating
            ? new Color(68, 56, 58)
            : new Color(48, 55, 47);
        Rectangle body = new(
            (int)topLeft.X + 9,
            (int)topLeft.Y + 9,
            (int)wolf.Size.X - 19,
            17);
        int muzzleExtension = wolf.Attack.IsActive ? 8 : 3;
        bool facesLeft = wolf.Facing == EnemyFacingDirection.Left;
        Rectangle head = facesLeft
            ? new Rectangle(body.X - 13, body.Y - 4, 17, 17)
            : new Rectangle(body.Right - 4, body.Y - 4, 17, 17);
        Rectangle muzzle = facesLeft
            ? new Rectangle(head.X - 5 - muzzleExtension, head.Y + 7, 7 + muzzleExtension, 7)
            : new Rectangle(head.Right - 2, head.Y + 7, 7 + muzzleExtension, 7);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.X + 2, body.Bottom - 2, body.Width - 3, 7),
            new Color(12, 17, 14, 145));
        DrawEliteOutline(wolf, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(body.X + 4, body.Bottom - 2, 6, 10), darkFur);
        _spriteBatch.Draw(_pixel, new Rectangle(body.Right - 11, body.Bottom - 2, 6, 10), darkFur);
        Rectangle tail = facesLeft
            ? new Rectangle(body.Right - 4, body.Y + 2, 14, 5)
            : new Rectangle(body.X - 10, body.Y + 2, 14, 5);
        _spriteBatch.Draw(_pixel, tail, darkFur);
        _spriteBatch.Draw(_pixel, body, fur);
        _spriteBatch.Draw(_pixel, head, fur);
        _spriteBatch.Draw(_pixel, new Rectangle(head.X + 2, head.Y - 7, 6, 9), darkFur);
        _spriteBatch.Draw(_pixel, new Rectangle(head.Right - 7, head.Y - 6, 6, 8), darkFur);
        _spriteBatch.Draw(_pixel, muzzle, darkFur);
        Rectangle eye = facesLeft
            ? new Rectangle(head.X + 1, head.Y + 4, 4, 4)
            : new Rectangle(head.Right - 5, head.Y + 4, 4, 4);
        _spriteBatch.Draw(_pixel, eye, new Color(229, 66, 51));
    }

    private void DrawGiantSpider(GiantSpider spider)
    {
        Vector2 topLeft = spider.Position - spider.Size / 2f;
        Color bodyColor = spider.IsHitFlashing
            ? new Color(235, 225, 211)
            : new Color(66, 44, 70);
        Color legColor = spider.IsHitFlashing
            ? new Color(210, 207, 196)
            : new Color(41, 32, 44);
        Rectangle abdomen = new(
            (int)topLeft.X + 7,
            (int)topLeft.Y + 8,
            25,
            22);
        bool facesLeft = spider.Facing == EnemyFacingDirection.Left;
        Rectangle head = facesLeft
            ? new Rectangle(abdomen.X - 14, abdomen.Y + 5, 17, 15)
            : new Rectangle(abdomen.Right - 3, abdomen.Y + 5, 17, 15);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(abdomen.X - 2, abdomen.Bottom - 2, 41, 7),
            new Color(12, 15, 14, 145));
        DrawEliteOutline(spider, new Color(237, 190, 59));

        for (int index = 0; index < 4; index++)
        {
            int legY = abdomen.Y + index * 5;
            int reach = index == 0 || index == 3 ? 12 : 9;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(abdomen.X - reach, legY, reach + 3, 3),
                legColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(head.Right - 2, legY, reach + 2, 3),
                legColor);
        }

        _spriteBatch.Draw(_pixel, abdomen, bodyColor);
        _spriteBatch.Draw(_pixel, head, new Color(51, 37, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(abdomen.X + 8, abdomen.Y + 7, 9, 7), new Color(104, 49, 93));
        _spriteBatch.Draw(_pixel, new Rectangle(head.X + 4, head.Y + 4, 3, 3), new Color(229, 58, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(head.Right - 6, head.Y + 4, 3, 3), new Color(229, 58, 54));
        Rectangle fang = facesLeft
            ? new Rectangle(head.X - 3, head.Bottom - 5, 5, 7)
            : new Rectangle(head.Right - 2, head.Bottom - 5, 5, 7);
        _spriteBatch.Draw(_pixel, fang, new Color(204, 194, 174));
    }

    private void DrawThornCrawler(ThornCrawler crawler)
    {
        int x = (int)crawler.Position.X;
        int y = (int)crawler.Position.Y;

        if (crawler.IsUnderground)
        {
            Color soil = crawler.State == ThornCrawlerState.Warning
                ? new Color(177, 71, 115, 205)
                : new Color(92, 68, 43, 150);
            int width = crawler.State == ThornCrawlerState.Warning ? 54 : 34;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x - width / 2, y - 5, width, 10),
                soil);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x - 3, y - 12, 6, 18),
                new Color(92, 113, 57, 175));

            if (crawler.State == ThornCrawlerState.Warning)
            {
                DrawRectangleOutline(
                    new Rectangle(x - 31, y - 18, 62, 36),
                    3,
                    new Color(222, 91, 143, 190));
            }

            return;
        }

        Rectangle bounds = crawler.Bounds;
        Color body = crawler.IsHitFlashing
            ? new Color(234, 228, 200)
            : new Color(73, 94, 49);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 5, bounds.Bottom - 5, bounds.Width - 10, 8),
            new Color(12, 16, 12, 145));
        DrawEliteOutline(crawler, new Color(237, 190, 59));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 10, bounds.Y + 12, bounds.Width - 20, bounds.Height - 12),
            body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Center.X - 4, bounds.Y, 8, 18), new Color(111, 61, 96));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 2, bounds.Y + 13, 12, 5), new Color(112, 122, 62));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 14, bounds.Y + 13, 12, 5), new Color(112, 122, 62));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 5, bounds.Y + 5, 6, 12), new Color(143, 68, 111));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 11, bounds.Y + 5, 6, 12), new Color(143, 68, 111));
    }

    private void DrawCorruptedTreant(CorruptedTreant treant)
    {
        Rectangle bounds = treant.Bounds;
        Color bark = treant.IsHitFlashing
            ? new Color(229, 220, 189)
            : new Color(84, 64, 42);
        Color corruption = new(103, 48, 93);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 7, bounds.Bottom - 7, bounds.Width - 14, 11),
            new Color(11, 15, 11, 155));
        DrawEliteOutline(treant, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 16, bounds.Y + 24, bounds.Width - 32, bounds.Height - 24), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 10, bounds.Y + 7, bounds.Width - 20, 30), new Color(64, 78, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 2, bounds.Y + 30, 18, 9), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 20, bounds.Y + 30, 18, 9), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 13, bounds.Bottom - 12, 12, 18), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 25, bounds.Bottom - 12, 12, 18), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Center.X - 5, bounds.Y + 33, 10, 24), corruption);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 20, bounds.Y + 17, 5, 5), new Color(223, 62, 65));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 25, bounds.Y + 17, 5, 5), new Color(223, 62, 65));
    }

    private void DrawBloodBat(BloodBat bat)
    {
        int centerX = (int)bat.Position.X;
        int centerY = (int)(bat.Position.Y - bat.FlightVisualOffset);
        int wingReach = bat.State == BloodBatState.Diving ? 13 : 20;
        bool facesLeft = bat.Facing == EnemyFacingDirection.Left;
        int leftWingReach = wingReach + (facesLeft ? 3 : -3);
        int rightWingReach = wingReach + (facesLeft ? -3 : 3);
        Color wing = bat.IsHitFlashing
            ? new Color(235, 220, 211)
            : new Color(91, 35, 47);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(centerX - 15, (int)bat.Position.Y + 7, 30, 6),
            new Color(11, 14, 12, 120));
        DrawEliteOutline(bat, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - leftWingReach - 8, centerY - 4, leftWingReach, 10), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 8, centerY - 4, rightWingReach, 10), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 9, centerY - 7, 18, 18), new Color(62, 37, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 6, centerY - 13, 5, 8), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 2, centerY - 13, 5, 8), wing);
        Rectangle eye = facesLeft
            ? new Rectangle(centerX - 6, centerY - 2, 3, 3)
            : new Rectangle(centerX + 3, centerY - 2, 3, 3);
        Rectangle snout = facesLeft
            ? new Rectangle(centerX - 13, centerY + 2, 6, 4)
            : new Rectangle(centerX + 7, centerY + 2, 6, 4);
        _spriteBatch.Draw(_pixel, snout, new Color(113, 47, 50));
        _spriteBatch.Draw(_pixel, eye, new Color(236, 53, 53));
    }

    private void DrawGoblinChief(GoblinChief chief)
    {
        Rectangle bounds = chief.Bounds;
        Color skin = chief.IsHitFlashing
            ? new Color(241, 237, 208)
            : new Color(83, 137, 61);
        DrawEliteOutline(chief, new Color(244, 187, 46));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Bottom - 5, bounds.Width - 14, 9), new Color(12, 15, 13, 150));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 9, bounds.Y + 23, bounds.Width - 18, bounds.Height - 23), new Color(116, 52, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 5, bounds.Y + 4, bounds.Width - 10, 28), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 2, bounds.Y + 12, 10, 12), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 8, bounds.Y + 12, 10, 12), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 13, bounds.Y + 14, 5, 5), new Color(242, 63, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 18, bounds.Y + 14, 5, 5), new Color(242, 63, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Y - 4, bounds.Width - 14, 8), new Color(224, 165, 43));
        int clubLength = chief.Attack.IsActive ? 48 : 33;
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right + 2, bounds.Y + 22, 7, clubLength), new Color(94, 58, 34));

        if (chief.IsWarCryActive)
        {
            Rectangle cry = bounds;
            cry.Inflate(13, 12);
            DrawRectangleOutline(cry, 4, new Color(239, 168, 54, 170));
        }
    }

    private void DrawMotherSpider(MotherSpider mother)
    {
        Rectangle bounds = mother.Bounds;
        Color body = mother.IsHitFlashing
            ? new Color(238, 228, 214)
            : new Color(77, 39, 76);
        DrawEliteOutline(mother, new Color(237, 190, 59));

        for (int index = 0; index < 4; index++)
        {
            int y = bounds.Y + 13 + index * 9;
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 13, y, 24, 4), new Color(42, 27, 43));
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 11, y, 24, 4), new Color(42, 27, 43));
        }

        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Y + 8, 42, bounds.Height - 15), body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 29, bounds.Y + 17, 23, 24), new Color(53, 30, 55));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 20, bounds.Y + 20, 16, 13), new Color(133, 51, 103));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 23, bounds.Y + 23, 5, 5), new Color(239, 57, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 14, bounds.Y + 23, 5, 5), new Color(239, 57, 54));
    }

    private void DrawSpiderling(Spiderling spiderling)
    {
        Rectangle bounds = spiderling.Bounds;
        Color body = spiderling.IsHitFlashing
            ? new Color(230, 226, 211)
            : new Color(58, 39, 61);

        for (int index = 0; index < 3; index++)
        {
            int y = bounds.Y + 3 + index * 5;
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 4, y, 8, 2), body);
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 4, y, 8, 2), body);
        }

        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 3, bounds.Y + 2, bounds.Width - 6, bounds.Height - 4), body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 8, bounds.Y + 5, 3, 3), new Color(226, 66, 57));
    }

    private void DrawEnemyBuff(Enemy enemy)
    {
        Rectangle bounds = GetEnemyVisualBounds(enemy);
        Color color = new(186, 65, 42, 145);
        Rectangle aura = bounds;
        aura.Inflate(4, 3);
        DrawRectangleOutline(aura, 2, new Color(151, 48, 38, 90));
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X - 3, bounds.Y + 7, 3, 10), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Center.X - 1, bounds.Y - 7, 3, 12), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Right, bounds.Y + 7, 3, 10), color);
    }

    private void DrawEliteOutline(Enemy enemy, Color color)
    {
        if (!enemy.IsElite)
            return;

        Rectangle outline = enemy.Bounds;
        outline.Inflate(4, 5);
        DrawRectangleOutline(outline, 3, color);
    }

    private void DrawProjectile(Projectile projectile)
    {
        if (projectile.Type == ProjectileType.WebShot)
        {
            Rectangle bounds = projectile.Bounds;
            Vector2 center = bounds.Center.ToVector2();
            int coreSize = projectile.EmpoweredVisual ? 12 : 8;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - coreSize / 2,
                    bounds.Center.Y - coreSize / 2, coreSize, coreSize),
                projectile.EmpoweredVisual
                    ? new Color(174, 170, 181, 235)
                    : new Color(205, 216, 207, 225));
            DrawWorldLine(center + new Vector2(-10f, -3f),
                center + new Vector2(10f, 4f), 2f,
                new Color(235, 240, 233, 205));
            DrawWorldLine(center + new Vector2(-8f, 7f),
                center + new Vector2(7f, -9f), 2f,
                new Color(225, 233, 226, 190));
            DrawWorldLine(center + new Vector2(-11f, 2f),
                center + new Vector2(8f, -1f), 1f,
                new Color(178, 195, 184, 190));
            if (projectile.EmpoweredVisual)
            {
                DrawWorldLine(center + new Vector2(-13f, -7f),
                    center + new Vector2(13f, 7f), 2f,
                    new Color(155, 135, 159, 205));
                DrawWorldLine(center + new Vector2(-13f, 7f),
                    center + new Vector2(13f, -7f), 2f,
                    new Color(205, 197, 203, 210));
            }
            return;
        }

        if (projectile.Type == ProjectileType.ArcaneBolt)
        {
            if (projectile.TechniqueEffect ==
                WeaponTechniqueEffect.SpellbladeRunicSpear)
            {
                DrawRunicSpearProjectile(projectile);
                return;
            }

            Rectangle glow = projectile.Bounds;
            glow.Inflate(7, 7);
            _spriteBatch.Draw(_pixel, glow, new Color(126, 72, 177, 50));
            Vector2 center = projectile.Position;
            float half = MathF.Max(7f, projectile.Size.X * .45f);
            Color edge = new(135, 105, 188, 225);
            Color core = new(218, 208, 235, 235);
            DrawWorldLine(center + new Vector2(0f, -half),
                center + new Vector2(half, 0f), 2f, edge);
            DrawWorldLine(center + new Vector2(half, 0f),
                center + new Vector2(0f, half), 2f, edge);
            DrawWorldLine(center + new Vector2(0f, half),
                center + new Vector2(-half, 0f), 2f, edge);
            DrawWorldLine(center + new Vector2(-half, 0f),
                center + new Vector2(0f, -half), 2f, edge);
            DrawWorldLine(center - new Vector2(half * .55f, 0f),
                center + new Vector2(half * .55f, 0f), 1f, core);
            DrawWorldLine(center - new Vector2(0f, half * .55f),
                center + new Vector2(0f, half * .55f), 1f, core);
            return;
        }

        Vector2 direction = projectile.Velocity;
        direction.Normalize();
        float rotation = MathF.Atan2(direction.Y, direction.X);
        float shaftLength = MathF.Max(projectile.Size.X, projectile.Size.Y);
        Vector2 shaftStart = projectile.Position - direction * (shaftLength / 2f);
        if (projectile.IsRangerProjectile)
        {
            Color trail = projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerHeavensFury
                    ? new Color(239, 215, 122, 180)
                    : projectile.RicochetCount > 0
                        ? new Color(132, 214, 196, 145)
                        : new Color(211, 191, 135, 105);
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.RangerHeavensFury ? 4f : 2f,
                trail);

            if (projectile.RicochetVisualTimeRemaining > 0f)
            {
                float spark = 7f + projectile.RicochetCount * 2f;
                DrawWorldLine(
                    projectile.LastRicochetPosition + new Vector2(-spark, -spark),
                    projectile.LastRicochetPosition + new Vector2(spark, spark),
                    2f,
                    new Color(245, 218, 126, 220));
                DrawWorldLine(
                    projectile.LastRicochetPosition + new Vector2(-spark, spark),
                    projectile.LastRicochetPosition + new Vector2(spark, -spark),
                    2f,
                    new Color(170, 231, 215, 215));
            }
        }
        _spriteBatch.Draw(
            _pixel,
            shaftStart,
            sourceRectangle: null,
            new Color(105, 74, 42),
            rotation,
            new Vector2(0f, 0.5f),
            new Vector2(shaftLength, 3f),
            SpriteEffects.None,
            layerDepth: 0f);
        _spriteBatch.Draw(
            _pixel,
            projectile.Position + direction * (shaftLength / 2f),
            sourceRectangle: null,
            new Color(67, 73, 68),
            rotation,
            new Vector2(0.5f, 0.5f),
            new Vector2(6f, 7f),
            SpriteEffects.None,
            layerDepth: 0f);
        _spriteBatch.Draw(
            _pixel,
            shaftStart + direction * 2f,
            sourceRectangle: null,
            new Color(91, 63, 47),
            rotation + 0.55f,
            new Vector2(0f, 0.5f),
            new Vector2(6f, 2f),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawRunicSpearProjectile(Projectile projectile)
    {
        Vector2 direction = projectile.Velocity;
        if (direction.LengthSquared() <= .01f)
            direction = Vector2.UnitX;
        else
            direction.Normalize();
        Vector2 perpendicular = new(-direction.Y, direction.X);
        float halfLength = MathF.Max(26f, projectile.Size.X / 2f);
        Vector2 tail = projectile.Position - direction * halfLength;
        Vector2 neck = projectile.Position + direction * (halfLength - 10f);
        Vector2 tip = projectile.Position + direction * (halfLength + 8f);
        Color glow = new(118, 92, 173, 80);
        Color edge = new(145, 119, 202, 220);
        Color core = new(224, 218, 237, 235);
        DrawWorldLine(tail, neck, 8f, glow);
        DrawWorldLine(tail, neck, 3f, edge);
        DrawWorldLine(tail + direction * 7f, neck, 1f, core);
        DrawWorldLine(neck + perpendicular * 8f, tip, 3f, edge);
        DrawWorldLine(neck - perpendicular * 8f, tip, 3f, edge);
        DrawWorldLine(neck, tip, 1f, core);
    }

    private void DrawSpellbladeWorld(GameSession gameSession)
    {
        var networkRunes = new List<GroundRune>();
        foreach (GroundRune rune in gameSession.GroundRunes.Runes)
        {
            if (rune.State == GroundRuneState.Network)
                networkRunes.Add(rune);
        }
        networkRunes.Sort((left, right) =>
            left.Position.X.CompareTo(right.Position.X));

        for (int index = 0; index < networkRunes.Count - 1; index++)
            DrawRuneNetworkLink(
                networkRunes[index].Position,
                networkRunes[index + 1].Position);
        if (networkRunes.Count == 3)
            DrawRuneNetworkArc(
                networkRunes[0].Position,
                networkRunes[2].Position);

        foreach (GroundRune rune in gameSession.GroundRunes.Runes)
            DrawGroundRune(rune);

        foreach (RuneDetonationVisual visual in
            gameSession.GroundRunes.DetonationVisuals)
        {
            float radius = 9f + SpellbladeTuning.RuneBlastRadius *
                visual.Progress;
            int alpha = Math.Clamp(
                (int)MathF.Round(205f * (1f - visual.Progress)), 0, 255);
            DrawArcaneRing(
                visual.Position - new Vector2(0f, 8f),
                radius,
                new Color(166, 132, 213, alpha),
                12,
                2f);
        }

        WeaponTechnique technique = gameSession.Player.Combat.CurrentTechnique;
        if (gameSession.Player.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            technique == null)
            return;

        if (technique.Effect == WeaponTechniqueEffect.SpellbladeArcaneDominion)
        {
            float phase = (gameSession.Player.Combat.TechniqueStageIndex + 1) /
                4f;
            DrawArcaneRing(
                gameSession.Player.Position,
                SpellbladeTuning.DominionRadius * (.72f + phase * .28f),
                new Color(131, 105, 179, 105),
                24,
                2f);
        }
        else if (technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneDetonation &&
            gameSession.Player.Combat.IsAttackActive)
        {
            DrawArcaneRing(
                gameSession.Player.Position,
                SpellbladeTuning.ArcaneDetonationRadius,
                new Color(176, 150, 213, 115),
                18,
                2f);
        }
    }

    private void DrawBreakerWorld(GameSession gameSession)
    {
        foreach (BreakerShockwave wave in gameSession.BreakerShockwaves)
        {
            Vector2 ground = wave.Position;
            float direction = wave.Direction;
            float fade = 1f - wave.Progress * .55f;
            Color crack = new(
                (int)(96f * fade),
                (int)(82f * fade),
                (int)(67f * fade),
                220);
            Color dust = new(122, 105, 82, (int)(145f * fade));
            DrawWorldLine(
                ground - new Vector2(direction * 25f, 2f),
                ground + new Vector2(direction * 22f, -1f),
                4f,
                crack);
            DrawWorldLine(
                ground - new Vector2(direction * 4f, 1f),
                ground + new Vector2(direction * 13f, -12f),
                2f,
                crack);
            DrawWorldLine(
                ground + new Vector2(direction * 5f, 0f),
                ground + new Vector2(direction * 17f, -7f),
                2f,
                crack);
            for (int debris = 0; debris < 3; debris++)
            {
                int size = 3 + debris;
                Vector2 position = ground + new Vector2(
                    -direction * (8f + debris * 8f),
                    -9f - debris * 5f);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)position.X, (int)position.Y, size, size),
                    dust);
            }
        }

        foreach (BreakerImpactVisual visual in gameSession.BreakerImpactVisuals)
        {
            float progress = visual.Progress;
            float radius = visual.Radius * (.22f + progress * .78f);
            float fade = 1f - progress;
            Color crack = new(91, 76, 61, (int)(220f * fade));
            Color dust = new(135, 112, 84, (int)(165f * fade));
            Vector2 left = visual.Position - new Vector2(radius, 0f);
            Vector2 right = visual.Position + new Vector2(radius, 0f);
            DrawWorldLine(left, right, 3f + visual.Strength * 2f, crack);
            for (int index = 0; index < 6; index++)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                float distance = radius * (.20f + index * .12f);
                Vector2 root = visual.Position + new Vector2(
                    side * distance,
                    -1f);
                Vector2 tip = root + new Vector2(
                    side * (7f + index * 2f),
                    -5f - index % 3 * 4f);
                DrawWorldLine(root, tip, index < 2 ? 3f : 2f, crack);
                int fragment = 3 + index % 3;
                Vector2 fragmentPosition = tip - new Vector2(
                    0f,
                    (8f + index * 3f) * fade);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        (int)fragmentPosition.X,
                        (int)fragmentPosition.Y,
                        fragment,
                        fragment),
                    dust);
            }
        }
    }

    private void DrawGroundRune(GroundRune rune)
    {
        Vector2 center = rune.Position - new Vector2(0f, 6f);
        float half = SpellbladeTuning.RuneWidth / 2f;
        Color edge = rune.State switch
        {
            GroundRuneState.Network => new Color(173, 151, 220, 235),
            GroundRuneState.Awakened => new Color(211, 202, 231, 240),
            _ => new Color(119, 91, 161, 210)
        };
        Color core = rune.State == GroundRuneState.Active
            ? new Color(151, 125, 190, 175)
            : new Color(225, 220, 237, 215);
        Vector2 top = center + new Vector2(0f, -11f);
        Vector2 right = center + new Vector2(half, 0f);
        Vector2 bottom = center + new Vector2(0f, 5f);
        Vector2 left = center + new Vector2(-half, 0f);
        DrawWorldLine(top, right, 2f, edge);
        DrawWorldLine(right, bottom, 2f, edge);
        DrawWorldLine(bottom, left, 2f, edge);
        DrawWorldLine(left, top, 2f, edge);
        DrawWorldLine(center - new Vector2(7f, 0f),
            center + new Vector2(7f, 0f), 1f, core);
        DrawWorldLine(center - new Vector2(0f, 7f),
            center + new Vector2(0f, 4f), 1f, core);
    }

    private void DrawRuneNetworkLink(Vector2 start, Vector2 end)
    {
        start -= new Vector2(0f, 8f);
        end -= new Vector2(0f, 8f);
        DrawWorldLine(start, end, 7f, new Color(92, 70, 139, 65));
        DrawWorldLine(start, end, 2f, new Color(177, 154, 220, 205));
        Vector2 midpoint = Vector2.Lerp(start, end, .5f);
        DrawWorldLine(midpoint - new Vector2(5f, 5f),
            midpoint + new Vector2(5f, 5f), 1f,
            new Color(229, 222, 239, 210));
    }

    private void DrawRuneNetworkArc(Vector2 start, Vector2 end)
    {
        start -= new Vector2(0f, 9f);
        end -= new Vector2(0f, 9f);
        Vector2 previous = start;
        for (int segment = 1; segment <= 8; segment++)
        {
            float amount = segment / 8f;
            Vector2 point = Vector2.Lerp(start, end, amount);
            point.Y -= MathF.Sin(amount * MathF.PI) * 24f;
            DrawWorldLine(previous, point, 1f,
                new Color(145, 126, 196, 145));
            previous = point;
        }
    }

    private void DrawArcaneImprints(Rectangle bounds, int count, bool bound)
    {
        count = Math.Clamp(count, 0, SpellbladeTuning.MaximumImprints);
        int spacing = 11;
        int startX = bounds.Center.X - (count - 1) * spacing / 2;
        int y = bounds.Top - 12;
        for (int index = 0; index < count; index++)
        {
            Vector2 center = new(startX + index * spacing, y);
            Color color = new(185, 159, 220, 230);
            DrawWorldLine(center + new Vector2(0f, -4f),
                center + new Vector2(4f, 0f), 2f, color);
            DrawWorldLine(center + new Vector2(4f, 0f),
                center + new Vector2(0f, 4f), 2f, color);
            DrawWorldLine(center + new Vector2(0f, 4f),
                center + new Vector2(-4f, 0f), 2f, color);
            DrawWorldLine(center + new Vector2(-4f, 0f),
                center + new Vector2(0f, -4f), 2f, color);
        }
        if (bound)
        {
            DrawArcaneRing(bounds.Center.ToVector2(),
                MathF.Max(bounds.Width, bounds.Height) * .56f,
                new Color(137, 111, 184, 125), 12, 2f);
        }
    }

    private static bool IsDominionBindingVisible(
        GameSession gameSession,
        Vector2 target) =>
        gameSession.Player.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.SpellbladeArcaneDominion &&
        gameSession.Player.Combat.TechniqueStageIndex >= 2 &&
        Vector2.DistanceSquared(gameSession.Player.Position, target) <=
            SpellbladeTuning.DominionRadius * SpellbladeTuning.DominionRadius;

    private void DrawArcaneRing(
        Vector2 center,
        float radius,
        Color color,
        int segments,
        float thickness)
    {
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int index = 1; index <= segments; index++)
        {
            float angle = MathHelper.TwoPi * index / segments;
            Vector2 next = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * .34f);
            DrawWorldLine(previous, next, thickness, color);
            previous = next;
        }
    }

    private void DrawRangerTrajectoryGuide(GameSession gameSession)
    {
        PlayerCharacter player = gameSession.Player;
        if (player.WeaponFamily != WeaponFamily.HunterBow ||
            !player.Combat.IsRangerTrajectoryAiming ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        if (effect is not WeaponTechniqueEffect.RangerSkyfallMarker and
            not WeaponTechniqueEffect.RangerFallingStar)
            return;

        Vector2 origin = player.Position + new Vector2(0f, -10f);
        float facing = player.Facing == FacingDirection.Left ? -1f : 1f;
        Vector2 direction;
        float speed;
        float gravity;
        float duration;

        if (effect == WeaponTechniqueEffect.RangerSkyfallMarker)
        {
            direction = player.Combat.RangerSkyfallDistance switch
            {
                SkyfallDistance.Near => Vector2.Normalize(
                    new Vector2(facing * .21f, -.978f)),
                SkyfallDistance.Far => Vector2.Normalize(
                    new Vector2(facing * .555f, -.832f)),
                _ => Vector2.Normalize(new Vector2(facing * .355f, -.935f))
            };
            speed = 650f;
            gravity = 780f;
            duration = 1.55f;
        }
        else
        {
            direction = Vector2.Normalize(new Vector2(
                facing * .48f,
                -.88f));
            speed = 690f;
            gravity = 940f;
            duration = 1.30f;
        }

        Vector2 velocity = direction * speed;
        Vector2 previous = origin;
        Vector2 final = origin;
        const int segments = 14;
        for (int index = 1; index <= segments; index++)
        {
            float time = duration * index / segments;
            Vector2 point = origin + velocity * time +
                new Vector2(0f, gravity * time * time * .5f);
            if (!SideScrollingCollision.IsPositionFree(
                    point,
                    new Vector2(10f, 6f),
                    gameSession.CurrentDungeon))
            {
                final = previous;
                break;
            }
            if (index % 2 == 1)
            {
                DrawWorldLine(
                    previous,
                    point,
                    1.5f,
                    new Color(221, 205, 148, 125));
            }
            previous = point;
            final = point;
        }

        Rectangle marker = new((int)final.X - 7, (int)final.Y - 3, 14, 6);
        DrawRectangleOutline(marker, 1, new Color(234, 213, 139, 175));
    }

    private void DrawRangerImpact(ProjectileImpact impact)
    {
        float radius = MathF.Max(12f, impact.Projectile.ImpactRadius);
        Rectangle outer = new(
            (int)(impact.Position.X - radius),
            (int)(impact.Position.Y - radius * .42f),
            (int)(radius * 2f),
            (int)(radius * .84f));
        DrawRectangleOutline(outer, 2, new Color(229, 186, 94, 170));
        DrawWorldLine(
            impact.Position + new Vector2(-radius * .55f, 0f),
            impact.Position + new Vector2(radius * .55f, 0f),
            3f,
            new Color(236, 218, 155, 185));
        DrawWorldLine(
            impact.Position + new Vector2(0f, -radius * .45f),
            impact.Position + new Vector2(0f, radius * .25f),
            2f,
            new Color(164, 222, 187, 165));
    }

    private void DrawWebPatch(WebPatch patch)
    {
        Rectangle bounds = patch.Bounds;
        Vector2 center = bounds.Center.ToVector2();
        Color faint = new(190, 207, 192, 30);
        Color strand = new(218, 228, 220, 115);
        _spriteBatch.Draw(_pixel, bounds, faint);
        DrawWorldLine(new Vector2(bounds.Left + 3, bounds.Bottom - 3),
            new Vector2(bounds.Right - 3, bounds.Top + 4), 1.5f, strand);
        DrawWorldLine(new Vector2(bounds.Left + 3, bounds.Top + 5),
            new Vector2(bounds.Right - 3, bounds.Bottom - 3), 1.5f, strand);
        DrawWorldLine(new Vector2(bounds.Left + 2, center.Y),
            new Vector2(bounds.Right - 2, center.Y), 2f, strand);
        DrawWorldLine(center,
            new Vector2(bounds.Left + 12, bounds.Top + 2), 1f, strand);
        DrawWorldLine(center,
            new Vector2(bounds.Right - 12, bounds.Top + 2), 1f, strand);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Left + 12, bounds.Top + 4,
                bounds.Width - 24, 2),
            new Color(230, 236, 229, 90));
    }

    private void DrawWorldLine(
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            from,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawRootHazard(RootHazard hazard)
    {
        Rectangle bounds = hazard.Bounds;

        if (hazard.Owner is AncientTreant)
        {
            DrawAncientTreantRootHazard(hazard, bounds);
            return;
        }

        if (hazard.Owner is CorruptedTreant && !hazard.IsTerrainRoot)
        {
            DrawCorruptedTreantRootHazard(hazard, bounds);
            return;
        }

        if (hazard.State == RootHazardState.Telegraph)
        {
            Color warning = hazard.IsTerrainRoot
                ? new Color(194, 92, 76, 75)
                : new Color(188, 66, 94, 82);
            _spriteBatch.Draw(_pixel, bounds, warning);
            DrawRectangleOutline(
                bounds,
                3,
                hazard.IsTerrainRoot
                    ? new Color(225, 126, 76, 185)
                    : new Color(222, 86, 124, 185));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 2, bounds.Y, 4, bounds.Height),
                new Color(238, 168, 112, 130));
            return;
        }

        Color rootColor = hazard.IsTerrainRoot
            ? new Color(91, 55, 35)
            : new Color(103, 58, 42);
        _spriteBatch.Draw(_pixel, bounds, new Color(72, 38, 31, 105));

        if (bounds.Width >= bounds.Height)
        {
            for (int x = bounds.X + 5; x < bounds.Right - 3; x += 13)
                _spriteBatch.Draw(_pixel, new Rectangle(x, bounds.Y - 6, 6, bounds.Height + 12), rootColor);
        }
        else
        {
            for (int y = bounds.Y + 5; y < bounds.Bottom - 3; y += 13)
                _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 6, y, bounds.Width + 12, 6), rootColor);
        }

        DrawRectangleOutline(bounds, 2, new Color(143, 72, 67, 190));
    }

    private void DrawAncientTreantRootHazard(
        RootHazard hazard,
        Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        Color glow = new(83, 154, 91, 155);

        if (hazard.State == RootHazardState.Telegraph)
        {
            if (hazard.IsTerrainRoot && bounds.Height > bounds.Width)
            {
                byte verticalAlpha = (byte)MathHelper.Lerp(
                    30f,
                    105f,
                    progress);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(bounds.Center.X - 5, bounds.Y + 2,
                        10, bounds.Height - 4),
                    new Color((byte)63, (byte)105, (byte)65,
                        verticalAlpha));
                DrawWorldLine(
                    new Vector2(bounds.Center.X, bounds.Bottom - 3),
                    new Vector2(bounds.Center.X - 6, bounds.Center.Y),
                    3f,
                    glow);
                DrawWorldLine(
                    new Vector2(bounds.Center.X - 6, bounds.Center.Y),
                    new Vector2(bounds.Center.X + 4, bounds.Y + 3),
                    3f,
                    new Color(112, 177, 101, 175));

                for (int index = 0; index < 5; index++)
                {
                    int y = bounds.Y + 8 + index *
                        Math.Max(8, (bounds.Height - 16) / 5);
                    int width = (int)MathF.Round(3f + progress * 8f);
                    int x = index % 2 == 0
                        ? bounds.Center.X - width
                        : bounds.Center.X;
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, y, width, 5),
                        new Color(62, 50, 31, 220));
                }
                return;
            }

            int groundY = bounds.Bottom - 4;
            byte alpha = (byte)MathHelper.Lerp(30f, 105f, progress);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 2, groundY - 7,
                    bounds.Width - 4, 10),
                new Color((byte)63, (byte)105, (byte)65, alpha));
            DrawWorldLine(
                new Vector2(bounds.X + 4, groundY),
                new Vector2(bounds.Center.X - 6, groundY - 8),
                3f,
                glow);
            DrawWorldLine(
                new Vector2(bounds.Center.X - 6, groundY - 8),
                new Vector2(bounds.Right - 4, groundY - 3),
                3f,
                new Color(112, 177, 101, 175));

            int tips = hazard.IsTerrainRoot ? 4 : 5;
            for (int index = 0; index < tips; index++)
            {
                int x = bounds.X + 5 + index *
                    Math.Max(7, (bounds.Width - 10) / tips);
                int height = (int)MathF.Round(3f + progress *
                    (hazard.IsTerrainRoot ? 8f : 15f));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, groundY - height, 5, height),
                    new Color(62, 50, 31, 220));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x - 2, groundY - height - 3, 8, 3),
                    new Color(64, 94, 48, 175));
            }
            return;
        }

        float eruption = progress < 0.18f
            ? progress / 0.18f
            : progress > 0.72f
                ? 1f - (progress - 0.72f) / 0.28f
                : 1f;
        eruption = MathHelper.Clamp(eruption, 0f, 1f);
        _spriteBatch.Draw(_pixel, bounds,
            new Color(44, 61, 39, 80));

        if (hazard.IsTerrainRoot)
        {
            bool horizontal = bounds.Width >= bounds.Height;
            int count = horizontal ? 6 : 5;
            for (int index = 0; index < count; index++)
            {
                float offset = (index + 0.5f) / count;
                Vector2 root = horizontal
                    ? new Vector2(bounds.X + bounds.Width * offset,
                        bounds.Bottom)
                    : new Vector2(bounds.Center.X,
                        bounds.Y + bounds.Height * offset);
                Vector2 tip = horizontal
                    ? root + new Vector2((index % 2 == 0 ? -7f : 7f),
                        -bounds.Height * eruption)
                    : root + new Vector2(
                        (index % 2 == 0 ? -1f : 1f) *
                            bounds.Width * eruption,
                        -8f);
                DrawWorldLine(root, tip, 8f, new Color(62, 49, 31));
                DrawWorldLine(root, tip, 3f, new Color(68, 104, 54));
            }
            return;
        }

        int ground = bounds.Bottom - 3;
        for (int index = 0; index < 5; index++)
        {
            float x = bounds.X + 6f + index * (bounds.Width - 12f) / 4f;
            float height = (31f + (index % 2) * 17f) * eruption;
            Vector2 root = new(x, ground);
            Vector2 tip = new(x + (index % 2 == 0 ? -8f : 8f),
                ground - height);
            DrawWorldLine(root, tip, 9f, new Color(58, 45, 30));
            DrawWorldLine(root, tip, 4f, new Color(68, 102, 52));
        }
    }

    private void DrawCorruptedTreantRootHazard(
        RootHazard hazard,
        Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        int groundY = bounds.Bottom - 5;

        if (hazard.State == RootHazardState.Telegraph)
        {
            byte glowAlpha = (byte)MathHelper.Lerp(30f, 105f, progress);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 3, groundY - 5,
                    bounds.Width - 6, 9),
                new Color((byte)91, (byte)37, (byte)63, glowAlpha));
            DrawRootLine(
                new Vector2(bounds.X + 6, groundY),
                new Vector2(bounds.Center.X - 7, groundY - 6),
                new Color(124, 52, 74, 175),
                3f);
            DrawRootLine(
                new Vector2(bounds.Center.X - 7, groundY - 6),
                new Vector2(bounds.Center.X + 5, groundY - 2),
                new Color(188, 63, 91, 190),
                2f);
            DrawRootLine(
                new Vector2(bounds.Center.X + 5, groundY - 2),
                new Vector2(bounds.Right - 5, groundY - 8),
                new Color(117, 48, 69, 170),
                3f);

            int tipHeight = (int)MathF.Round(3f + progress * 12f);
            for (int index = 0; index < 3; index++)
            {
                int x = bounds.X + 13 + index * 18;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, groundY - tipHeight,
                        4, tipHeight),
                    new Color(78, 48, 34, 210));
            }

            for (int index = 0; index < 4; index++)
            {
                int x = bounds.X + 7 + index * 15;
                int y = groundY - 3 - ((index + (int)(progress * 5f)) % 3) * 3;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, y, 3, 3),
                    new Color(123, 82, 48, 145));
            }
            return;
        }

        float eruption = progress < 0.2f
            ? progress / 0.2f
            : progress > 0.68f
                ? 1f - (progress - 0.68f) / 0.32f
                : 1f;
        eruption = MathHelper.Clamp(eruption, 0f, 1f);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 2, groundY - 6,
                bounds.Width - 4, 10),
            new Color(72, 39, 31, 125));

        for (int index = 0; index < 4; index++)
        {
            int x = bounds.X + 8 + index * 15;
            int height = (int)MathF.Round((29f + index % 2 * 13f) * eruption);
            Vector2 root = new(x, groundY);
            Vector2 tip = new(x + (index % 2 == 0 ? -6f : 7f),
                groundY - height);
            DrawRootLine(root, tip, new Color(87, 51, 34), 7f);
            DrawRootLine(
                tip,
                tip + new Vector2(index % 2 == 0 ? -8f : 8f, 8f),
                new Color(113, 60, 46),
                4f);
        }
    }

    private void DrawRootLine(
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            from,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawTreasureChest(TreasureChest chest)
    {
        Rectangle bounds = chest.Bounds;
        Rectangle shadow = new(bounds.X + 3, bounds.Bottom - 3, bounds.Width - 6, 8);
        Rectangle body = new(bounds.X, bounds.Y + 13, bounds.Width, bounds.Height - 13);
        Color woodColor = chest.IsOpen
            ? new Color(112, 70, 38)
            : new Color(138, 83, 39);

        _spriteBatch.Draw(_pixel, shadow, new Color(14, 16, 18, 145));
        _spriteBatch.Draw(_pixel, body, woodColor);
        DrawRectangleOutline(body, 3, new Color(198, 155, 67));

        if (chest.IsOpen)
        {
            Rectangle openLid = new(bounds.X + 2, bounds.Y - 2, bounds.Width - 4, 12);
            _spriteBatch.Draw(_pixel, openLid, new Color(101, 62, 34));
            DrawRectangleOutline(openLid, 2, new Color(185, 143, 61));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 11, body.Y + 5, 22, 6),
                new Color(232, 202, 101, 175));
        }
        else
        {
            Rectangle lid = new(bounds.X, bounds.Y + 3, bounds.Width, 16);
            _spriteBatch.Draw(_pixel, lid, new Color(157, 95, 43));
            DrawRectangleOutline(lid, 3, new Color(205, 161, 69));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 5, bounds.Y + 16, 10, 12),
                new Color(225, 183, 69));
        }

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.X + 6, body.Y + 3, 4, body.Height - 6),
            new Color(190, 145, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.Right - 10, body.Y + 3, 4, body.Height - 6),
            new Color(190, 145, 62));
    }

    private void DrawAncientTreant(AncientTreant boss, GameTime gameTime)
    {
        Rectangle bounds = boss.Bounds;
        _wildForestApexSpriteRenderer.Draw(boss, gameTime);

        if (!boss.IsAlive)
            return;

        int barWidth = 126;
        int healthWidth = (int)((barWidth - 4) *
            (boss.CurrentHealth / (float)boss.MaxHealth));
        Rectangle healthBorder = new(
            (int)boss.Position.X - barWidth / 2,
            bounds.Y - 25,
            barWidth,
            11);
        _spriteBatch.Draw(_pixel, healthBorder, new Color(28, 18, 20));
        DrawRectangleOutline(healthBorder, 2, new Color(151, 112, 69));

        if (healthWidth > 0)
        {
            Color healthColor = boss.Phase switch
            {
                AncientTreantPhase.PhaseThree => new Color(220, 55, 63),
                AncientTreantPhase.PhaseTwo => new Color(181, 73, 57),
                _ => new Color(126, 93, 49)
            };
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(healthBorder.X + 2, healthBorder.Y + 2, healthWidth, 7),
                healthColor);
        }

        int poiseWidth = (int)((barWidth - 4) *
            (boss.CurrentPoise / boss.MaxPoise));
        Rectangle poiseBorder = new(
            healthBorder.X,
            healthBorder.Bottom + 2,
            barWidth,
            7);
        _spriteBatch.Draw(_pixel, poiseBorder, new Color(25, 20, 31));

        if (poiseWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(poiseBorder.X + 2, poiseBorder.Y + 2, poiseWidth, 3),
                new Color(166, 111, 196));
        }
    }

    private void DrawWorldLoot(WorldLoot loot)
    {
        Color rarityColor = GetRarityColor(loot.Item.Rarity);
        Rectangle glow = loot.Bounds;
        glow.Inflate(7, 7);
        Rectangle core = loot.Bounds;

        _spriteBatch.Draw(_pixel, glow, new Color(
            rarityColor.R,
            rarityColor.G,
            rarityColor.B,
            (byte)55));
        _spriteBatch.Draw(_pixel, core, new Color(22, 25, 29, 235));
        DrawRectangleOutline(core, 2, rarityColor);
        DrawEquipmentSymbol(loot.Item, core, rarityColor);
        DrawRarityMarkers(
            loot.Item.Rarity,
            core.Center.X,
            core.Bottom + 4,
            rarityColor,
            markerSize: 3);
    }

    private void DrawInventoryOverlay(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 310,
            viewport.Height / 2 - 220,
            620,
            440);

        _spriteBatch.Draw(_pixel, screen, new Color(7, 9, 13, 190));
        _spriteBatch.Draw(_pixel, panel, new Color(22, 27, 34, 248));
        DrawRectangleOutline(panel, 4, new Color(122, 111, 91));
        DrawInventoryTab(
            new Rectangle(panel.X + 28, panel.Y + 17, 120, 30),
            "Q WEAPONS",
            gameSession.ActiveInventoryTab == InventoryTab.Weapons);
        DrawInventoryTab(
            new Rectangle(panel.X + 156, panel.Y + 17, 138, 30),
            "E EQUIPMENT",
            gameSession.ActiveInventoryTab == InventoryTab.Equipment);

        const int slotSize = 50;
        const int slotGap = 8;
        const int columns = InventoryGridNavigation.ColumnCount;
        int inventoryX = panel.X + 28;
        int inventoryY = panel.Y + 57;

        int visibleSlotCount = Math.Max(
            Inventory.DefaultCapacity,
            gameSession.ActiveInventoryItems.Count);

        for (int index = 0; index < visibleSlotCount; index++)
        {
            int column = index % columns;
            int row = index / columns;
            var slotBounds = new Rectangle(
                inventoryX + column * (slotSize + slotGap),
                inventoryY + row * (slotSize + slotGap),
                slotSize,
                slotSize);
            EquipmentItem item = index < gameSession.ActiveInventoryItems.Count
                ? gameSession.ActiveInventoryItems[index]
                : null;
            bool isSelected = item != null &&
                index == gameSession.SelectedInventoryIndex;
            DrawInventorySlot(slotBounds, item, isSelected);
        }

        if (gameSession.ActiveInventoryItems.Count == 0)
        {
            DrawDebugText(
                gameSession.ActiveInventoryEmptyMessage,
                inventoryX + 12,
                inventoryY + 20,
                new Color(132, 144, 153),
                scale: 1);
        }

        int equipmentX = panel.Right - 168;
        EquipmentSlot activeSlot = gameSession.ActiveInventoryTab ==
            InventoryTab.Weapons
                ? EquipmentSlot.Weapon
                : EquipmentSlot.Armor;
        DrawEquipmentSlot(
            new Rectangle(equipmentX, panel.Y + 57, 128, 118),
            activeSlot,
            gameSession.Player.EquippedItems.GetEquipped(activeSlot));

        DrawComparisonIndicator(
            gameSession.SelectedInventoryItem,
            gameSession.Player,
            panel.X + 270,
            panel.Y + 190);

        DrawSelectedItemIdentity(
            gameSession,
            panel.X + 300,
            panel.Y + 195);

        var controls = new Rectangle(panel.X + 28, panel.Bottom - 66, 267, 36);
        _spriteBatch.Draw(_pixel, controls, new Color(31, 38, 47));
        DrawRectangleOutline(controls, 2, new Color(77, 88, 98));
        DrawControlHints(
            controls,
            gameSession.ActiveInventoryTab == InventoryTab.Equipment);
    }

    private void DrawInventoryTab(
        Rectangle bounds,
        string label,
        bool isActive)
    {
        Color borderColor = isActive
            ? new Color(235, 207, 119)
            : new Color(75, 84, 94);
        Color textColor = isActive
            ? new Color(247, 232, 166)
            : new Color(128, 139, 148);
        _spriteBatch.Draw(
            _pixel,
            bounds,
            isActive ? new Color(54, 52, 42) : new Color(27, 32, 39));
        DrawRectangleOutline(bounds, isActive ? 3 : 2, borderColor);
        DrawDebugText(
            label,
            bounds.X + 9,
            bounds.Y + 10,
            textColor,
            scale: 1);

        if (isActive)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 5, bounds.Bottom - 5, bounds.Width - 10, 3),
                new Color(235, 207, 119));
        }
    }

    private void DrawInventorySlot(
        Rectangle bounds,
        EquipmentItem item,
        bool isSelected)
    {
        Color borderColor = isSelected
            ? new Color(245, 230, 151)
            : new Color(83, 91, 99);
        _spriteBatch.Draw(
            _pixel,
            bounds,
            isSelected ? new Color(52, 55, 55) : new Color(30, 35, 42));
        DrawRectangleOutline(bounds, isSelected ? 4 : 2, borderColor);

        if (item == null)
            return;

        Color rarityColor = GetRarityColor(item.Rarity);
        var symbolBounds = new Rectangle(
            bounds.X + 12,
            bounds.Y + 8,
            bounds.Width - 24,
            bounds.Height - 21);
        DrawEquipmentSymbol(item, symbolBounds, rarityColor);
        DrawRarityMarkers(
            item.Rarity,
            bounds.Center.X,
            bounds.Bottom - 9,
            rarityColor,
            markerSize: 4);

        if (item.ArmorDefinition != null)
        {
            DrawDebugText(
                item.ArmorGradeLabel,
                bounds.X + 3,
                bounds.Y + 3,
                new Color(245, 230, 151),
                scale: 1);
        }
    }

    private void DrawSelectedItemIdentity(
        GameSession gameSession,
        int x,
        int y)
    {
        EquipmentItem item = gameSession.SelectedInventoryItem;

        if (item == null)
            return;

        DrawDebugText(
            item.Name.ToUpperInvariant(),
            x,
            y,
            GetRarityColor(item.Rarity),
            scale: 1);

        string identity = item.WeaponDefinition != null
            ? $"WEAPON FAMILY: {item.WeaponDefinition.Family.ToDisplayName()}"
            : item.ArmorDefinition != null
                ? $"CLASS: {item.ArmorDefinition.Class} ARMOR"
                : $"EQUIPMENT SLOT: {item.Slot}";
        DrawDebugText(
            identity.ToUpperInvariant(),
            x,
            y + 13,
            new Color(187, 194, 199),
            scale: 1);

        DrawDebugText(
            item.ArmorDefinition == null
                ? $"RARITY: {item.Rarity}"
                : $"GRADE: {item.ArmorGradeLabel} RARITY: {item.Rarity}",
            x,
            y + 26,
            new Color(187, 194, 199),
            scale: 1);

        string stats = item.WeaponDefinition != null
            ? $"DAMAGE +{item.DamageBonus}"
            : item.ArmorDefinition != null
                ? $"HEALTH +{item.HealthBonus} DAMAGE TAKEN " +
                    $"{(int)MathF.Round(item.DamageTakenMultiplier * 100f)}%"
                : $"SLOT: {item.Slot}";
        DrawDebugText(
            stats,
            x,
            y + 39,
            new Color(161, 190, 174),
            scale: 1);
        DrawDebugText(
            "STATUS: UNEQUIPPED",
            x,
            y + 52,
            new Color(137, 149, 158),
            scale: 1);

        if (item.ArmorDefinition == null)
            return;

        string fusionStatus = gameSession.IsFusionConfirmationPending
            ? $"FUSE 3X {item.ArmorGradeLabel} TO " +
                ArmorGradeRules.ToDisplayName(
                    ArmorGradeRules.Next(item.ArmorGrade))
            : item.ArmorGrade == ArmorGrade.C5
            ? "MAX GRADE"
            : gameSession.CanFuseSelectedArmor
                ? $"FUSE AVAILABLE: 3 TO " +
                    ArmorGradeRules.ToDisplayName(
                        ArmorGradeRules.Next(item.ArmorGrade))
                : $"FUSION: {gameSession.SelectedFusionMaterialCount}/3";
        DrawDebugText(
            fusionStatus,
            x,
            y + 65,
            gameSession.CanFuseSelectedArmor
                ? new Color(245, 205, 93)
                : new Color(137, 149, 158),
            scale: 1);

        if (gameSession.IsFusionConfirmationPending)
        {
            DrawDebugText(
                "ENTER CONFIRM  ESC CANCEL",
                x,
                y + 78,
                new Color(224, 229, 232),
                scale: 1);
        }
    }

    private void DrawEquipmentSlot(
        Rectangle bounds,
        EquipmentSlot slot,
        EquipmentItem item)
    {
        _spriteBatch.Draw(_pixel, bounds, new Color(27, 34, 40));
        DrawRectangleOutline(bounds, 3, new Color(78, 155, 123));

        var slotBadge = new Rectangle(bounds.X + 8, bounds.Y + 8, 30, 30);
        _spriteBatch.Draw(_pixel, slotBadge, new Color(19, 24, 29));
        DrawRectangleOutline(slotBadge, 2, new Color(91, 111, 107));
        DrawEquipmentSymbol(slot, slotBadge, new Color(125, 163, 153));

        if (item == null)
        {
            var empty = new Rectangle(
                bounds.X + 40,
                bounds.Y + 46,
                48,
                48);
            DrawRectangleOutline(empty, 2, new Color(59, 68, 74));
            return;
        }

        Color rarityColor = GetRarityColor(item.Rarity);
        var itemBounds = new Rectangle(
            bounds.Center.X - 25,
            bounds.Y + 40,
            50,
            53);
        DrawEquipmentSymbol(item, itemBounds, rarityColor);
        DrawRarityMarkers(
            item.Rarity,
            bounds.Center.X,
            bounds.Bottom - 16,
            rarityColor,
            markerSize: 5);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Right - 19, bounds.Y + 10, 9, 9),
            new Color(80, 199, 126));
    }

    private void DrawEquipmentSymbol(
        EquipmentItem item,
        Rectangle bounds,
        Color color)
    {
        if (item?.WeaponDefinition != null)
        {
            int centerX = bounds.Center.X;
            int top = bounds.Y + 3;
            int height = Math.Max(8, bounds.Height - 14);

            if (item.WeaponDefinition.Family == WeaponFamily.BattleAxe)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 2, top + 4, 5, height + 4),
                    new Color(105, 68, 45));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 10, top + 1, 21, 9),
                    color);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 13, top + 4, 5, 10),
                    color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.Spear)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top, 4, height + 10), new Color(117, 78, 49));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 5, top - 1, 10, 9), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top - 5, 4, 8), color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.DualDaggers)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 10, top + 2, 4, height - 2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX + 6, top + 2, 4, height - 2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 14, top + height - 4, 12, 4), new Color(173, 130, 70));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX + 2, top + height - 4, 12, 4), new Color(173, 130, 70));
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.Bow)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 10, top + 3, 4, height - 1), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top, 7, 4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top + height, 7, 4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX, top + 2, 2, height + 1), new Color(211, 205, 177));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 4, top + height / 2, 18, 2), new Color(173, 130, 70));
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.ArcaneStaff)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top + 6, 5, height + 4), new Color(105, 68, 45));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top, 15, 12), new Color(45, 35, 57));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 4, top + 3, 9, 6), color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.ChainFlail)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, top + 14, 5, height - 3),
                    new Color(105, 68, 45));
                for (int link = 0; link < 4; link++)
                {
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(centerX + 3 + link * 4, top + 12 - link * 3, 4, 3),
                        new Color(112, 119, 126));
                }
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX + 17, top - 4, 13, 13), color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.GreatSword)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, top + 10, 5, height + 3),
                    new Color(105, 68, 45));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 9, top - 2, 19, 19), color);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 13, top + 5, 27, 5), color);
                return;
            }

            int bladeWidth = item.WeaponDefinition.Family == WeaponFamily.GreatSword
                ? 9
                : 5;
            int guardWidth = item.WeaponDefinition.Family == WeaponFamily.GreatSword
                ? 23
                : 17;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - bladeWidth / 2, top, bladeWidth, height),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - guardWidth / 2, top + height - 4, guardWidth, 4),
                new Color(173, 130, 70));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, top + height, 5, 9),
                new Color(105, 68, 45));
            return;
        }

        if (item?.ArmorDefinition != null)
        {
            int inset = item.ArmorDefinition.Class switch
            {
                ArmorClass.Light => 10,
                ArmorClass.Heavy => 3,
                _ => 7
            };
            int shoulder = item.ArmorDefinition.Class switch
            {
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 8,
                _ => 5
            };
            var chest = new Rectangle(
                bounds.X + inset,
                bounds.Y + 9,
                Math.Max(8, bounds.Width - inset * 2),
                Math.Max(10, bounds.Height - 13));
            _spriteBatch.Draw(_pixel, chest, color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.X - shoulder, chest.Y + 2, shoulder + 1, 11),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.Right - 1, chest.Y + 2, shoulder + 1, 11),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.Center.X - 2, chest.Y + 4, 4, chest.Height - 8),
                new Color(35, 40, 45, 150));
            return;
        }

        DrawEquipmentSymbol(item?.Slot ?? EquipmentSlot.Armor, bounds, color);
    }

    private void DrawEquipmentSymbol(
        EquipmentSlot slot,
        Rectangle bounds,
        Color color)
    {
        if (slot == EquipmentSlot.Weapon)
        {
            int centerX = bounds.Center.X;
            int bladeTop = bounds.Y + 3;
            int bladeHeight = Math.Max(8, bounds.Height - 14);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, bladeTop, 5, bladeHeight),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 8, bladeTop + bladeHeight - 4, 17, 4),
                new Color(173, 130, 70));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, bladeTop + bladeHeight, 5, 9),
                new Color(105, 68, 45));
            return;
        }

        int chestWidth = Math.Max(10, bounds.Width - 14);
        int chestHeight = Math.Max(10, bounds.Height - 13);
        var chest = new Rectangle(
            bounds.Center.X - chestWidth / 2,
            bounds.Y + 9,
            chestWidth,
            chestHeight);
        _spriteBatch.Draw(_pixel, chest, color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.X - 5, chest.Y + 3, 6, 10),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.Right - 1, chest.Y + 3, 6, 10),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.Center.X - 2, chest.Y + 4, 4, chest.Height - 8),
            new Color(35, 40, 45, 150));
    }

    private void DrawRarityMarkers(
        ItemRarity rarity,
        int centerX,
        int y,
        Color color,
        int markerSize)
    {
        int markerCount = (int)rarity + 1;
        int spacing = markerSize + 2;
        int startX = centerX - (markerCount * spacing - 2) / 2;

        for (int index = 0; index < markerCount; index++)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(startX + index * spacing, y, markerSize, markerSize),
                color);
        }
    }

    private void DrawComparisonIndicator(
        EquipmentItem selectedItem,
        PlayerCharacter player,
        int x,
        int y)
    {
        if (selectedItem == null)
            return;

        EquipmentItem equippedItem = player.EquippedItems.GetEquipped(
            selectedItem.Slot);
        int equippedScore = equippedItem?.Score ?? 0;

        if (selectedItem.Score > equippedScore)
        {
            Color color = new(91, 205, 128);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 8, y + 8, 6, 20), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4, y + 4, 14, 6), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y + 8, 22, 5), color);
        }
        else if (selectedItem.Score < equippedScore)
        {
            Color color = new(218, 88, 82);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 8, y, 6, 20), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4, y + 18, 14, 6), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y + 23, 22, 5), color);
        }
        else
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 11, 22, 6),
                new Color(189, 184, 151));
        }
    }

    private void DrawControlHints(Rectangle bounds, bool showFusion)
    {
        DrawDebugText(
            "WASD/ARROWS MOVE",
            bounds.X + 8,
            bounds.Y + 3,
            new Color(156, 170, 181),
            scale: 1);
        DrawDebugText(
            "Q/E TABS  ENTER EQUIP",
            bounds.X + 8,
            bounds.Y + 14,
            new Color(156, 170, 181),
            scale: 1);
        DrawDebugText(
            showFusion ? "F FUSE  I/ESC CLOSE" : "I/ESC CLOSE",
            bounds.X + 8,
            bounds.Y + 25,
            new Color(156, 170, 181),
            scale: 1);
    }

    private void DrawFusionFeedback(GameSession gameSession)
    {
        EquipmentItem source = gameSession.LastFusionSource;
        EquipmentItem result = gameSession.LastFusionResult;

        if (source == null || result == null)
            return;

        Viewport viewport = _graphicsDevice.Viewport;
        var panel = new Rectangle(viewport.Width / 2 - 170, 92, 340, 60);
        _spriteBatch.Draw(_pixel, panel, new Color(25, 39, 35, 242));
        DrawRectangleOutline(panel, 3, new Color(93, 210, 137));
        DrawDebugText(
            "FUSED!",
            panel.Center.X - 21,
            panel.Y + 10,
            new Color(112, 239, 157),
            scale: 1);
        DrawDebugText(
            $"{source.Name} {source.ArmorGradeLabel} X3 TO " +
                $"{result.Name} {result.ArmorGradeLabel}",
            panel.X + 12,
            panel.Y + 33,
            new Color(222, 231, 225),
            scale: 1);
    }

    private void DrawSequentialEnemyTestStatus(GameSession gameSession)
    {
        var panel = new Rectangle(12, 82, 250, 46);
        _spriteBatch.Draw(_pixel, panel, new Color(21, 28, 33, 226));
        DrawRectangleOutline(panel, 2, new Color(189, 151, 79));

        DrawDebugText(
            "WILD FOREST SHOWCASE",
            panel.X + 8,
            panel.Y + 7,
            new Color(241, 205, 111),
            scale: 1);
        DrawDebugText(
            "PROXIMITY AI 650 PX",
            panel.X + 8,
            panel.Y + 20,
            new Color(167, 177, 185),
            scale: 1);

        DrawDebugText(
            "F6 RESET",
            panel.Right - 58,
            panel.Bottom - 10,
            new Color(113, 126, 135),
            scale: 1);
    }

    private void DrawWildForestShowcaseLabel(string text, Rectangle bounds)
    {
        if (string.IsNullOrEmpty(text))
            return;

        const int scale = 1;
        int width = text.Length * 6 * scale;
        int x = bounds.Center.X - width / 2;
        int y = bounds.Top - 18;
        var background = new Rectangle(x - 3, y - 3, width + 6, 12);
        _spriteBatch.Draw(_pixel, background, new Color(12, 17, 25, 185));
        DrawDebugText(text, x, y, new Color(230, 220, 179), scale);
    }

    private static Rectangle GetEnemyVisualBounds(Enemy enemy)
    {
        if (enemy is BloodBat bat)
        {
            Vector2 batSize = EnemyVisualProfile.BloodBat.VisualSize;
            return new Rectangle(
                (int)MathF.Round(bat.Position.X - batSize.X / 2f),
                (int)MathF.Round(
                    bat.Position.Y - bat.FlightVisualOffset - batSize.Y / 2f),
                (int)MathF.Ceiling(batSize.X),
                (int)MathF.Ceiling(batSize.Y));
        }

        Vector2 visualSize = enemy switch
        {
            DireWolf => EnemyVisualProfile.DireWolf.VisualSize,
            GiantSpider => EnemyVisualProfile.GiantSpider.VisualSize,
            ThornCrawler => EnemyVisualProfile.ThornCrawler.VisualSize,
            CorruptedTreant => EnemyVisualProfile.CorruptedTreant.VisualSize,
            GoblinChief => EnemyVisualProfile.GoblinChief.VisualSize,
            MotherSpider => EnemyVisualProfile.MotherSpider.VisualSize,
            Spiderling => EnemyVisualProfile.Spiderling.VisualSize,
            _ => enemy.Size
        };
        return new Rectangle(
            (int)MathF.Round(enemy.Position.X - visualSize.X / 2f),
            enemy.Bounds.Bottom - (int)MathF.Ceiling(visualSize.Y),
            (int)MathF.Ceiling(visualSize.X),
            (int)MathF.Ceiling(visualSize.Y));
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => new Color(86, 193, 103),
            ItemRarity.Rare => new Color(72, 132, 232),
            ItemRarity.Epic => new Color(170, 89, 224),
            ItemRarity.Legendary => new Color(236, 159, 52),
            _ => new Color(202, 207, 210)
        };
    }

    private void DrawDungeonStatus(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var panel = new Rectangle(viewport.Width - 244, 12, 232, 70);
        _spriteBatch.Draw(_pixel, panel, new Color(17, 21, 28, 220));
        DrawRectangleOutline(panel, 2, new Color(112, 119, 126, 230));

        Color depthColor = new(191, 164, 78);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(panel.X + 10, panel.Y + 11, 17, 6),
            depthColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(panel.X + 14, panel.Y + 5, 9, 18),
            depthColor);
        int visibleDepth = Math.Min(gameSession.DungeonDepth, 12);

        for (int index = 0; index < visibleDepth; index++)
        {
            int markerHeight = 7 + index % 3 * 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    panel.X + 36 + index * 10,
                    panel.Y + 23 - markerHeight,
                    7,
                    markerHeight),
                depthColor);
        }

        if (gameSession.DungeonDepth > visibleDepth)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(panel.Right - 28, panel.Y + 8, 16, 12),
                new Color(231, 198, 88));
        }

        Color bossColor = gameSession.BossDefeated
            ? new Color(86, 190, 112)
            : new Color(207, 72, 63);
        int bossX = panel.X + 18;
        int statusY = panel.Y + 42;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX, statusY + 7, 28, 10),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 3, statusY, 6, 9),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 11, statusY - 4, 6, 13),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 19, statusY, 6, 9),
            bossColor);

        Color tierColor = new(151, 104, 221);
        int tierStartX = panel.Center.X -
            (gameSession.WorldTier * 11 - 3) / 2;

        for (int index = 0; index < gameSession.WorldTier; index++)
        {
            int markerX = tierStartX + index * 11;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, statusY + 2, 8, 17),
                tierColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX + 2, statusY - 2, 4, 4),
                new Color(202, 163, 244));
        }

        int exitX = panel.Right - 53;
        Color exitColor = gameSession.IsExitUnlocked
            ? new Color(111, 202, 113)
            : new Color(206, 91, 67);
        var exitFrame = new Rectangle(exitX, statusY - 4, 31, 27);
        DrawRectangleOutline(exitFrame, 3, exitColor);

        if (gameSession.IsExitUnlocked)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.Center.X - 2, exitFrame.Center.Y - 2, 5, 5),
                exitColor);
        }
        else
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.X + 7, exitFrame.Y + 6, 4, 16),
                exitColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.Right - 11, exitFrame.Y + 6, 4, 16),
                exitColor);
        }
    }

    private void DrawWorldTierTransition(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        float progress = MathHelper.Clamp(
            gameSession.WorldTierTransitionProgress,
            0f,
            1f);
        float pulse = 0.55f +
            0.45f * MathF.Abs(MathF.Sin(progress * MathHelper.Pi * 4f));
        byte alpha = (byte)(190f * progress * pulse);
        Color borderColor = new(171, 116, 235, (int)alpha);
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        DrawRectangleOutline(screen, 8, borderColor);

        int markerCount = gameSession.WorldTier;
        int spacing = 22;
        int centerX = viewport.Width / 2;
        int startX = centerX - (markerCount * spacing - 6) / 2;
        int y = viewport.Height / 2 - 25;

        for (int index = 0; index < markerCount; index++)
        {
            int x = startX + index * spacing;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 8, 16, 34),
                new Color(151, 91, 222, (int)alpha));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x + 4, y, 8, 50),
                new Color(211, 169, 247, (int)alpha));
        }
    }

    private void DrawHudPanel()
    {
        var panel = new Rectangle(12, 12, 238, 94);
        _spriteBatch.Draw(_pixel, panel, new Color(17, 21, 28, 220));
        DrawRectangleOutline(panel, 2, new Color(112, 119, 126, 230));
    }

    private void DrawCombatStateDebug(PlayerCharacter player)
    {
        const int x = 270;
        int y = 16;

        if (player.Combat.IsBlockInputHeld)
        {
            DrawDebugLabel("CTRL HELD", x, y, new Color(208, 220, 235));
            y += 22;
        }

        if (player.Combat.IsBlocking)
        {
            DrawDebugLabel("BLOCK ACTIVE", x, y, new Color(99, 191, 247));
        }
    }

    private void DrawDebugLabel(
        string text,
        int x,
        int y,
        Color color)
    {
        const int scale = 2;
        int width = text.Length * 6 * scale + 8;
        var background = new Rectangle(x - 4, y - 4, width, 22);
        _spriteBatch.Draw(_pixel, background, new Color(12, 17, 25, 215));
        DrawRectangleOutline(
            background,
            1,
            new Color(color.R, color.G, color.B, (byte)155));
        DrawDebugText(text, x, y, color, scale);
    }

    private void DrawDebugText(
        string text,
        int x,
        int y,
        Color color,
        int scale)
    {
        int cursorX = x;

        foreach (char character in text)
        {
            if (DebugGlyphs.TryGetValue(character, out byte[] rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if ((rows[row] & (1 << (4 - column))) == 0)
                            continue;

                        _spriteBatch.Draw(
                            _pixel,
                            new Rectangle(
                                cursorX + column * scale,
                                y + row * scale,
                                scale,
                                scale),
                            color);
                    }
                }
            }

            cursorX += 6 * scale;
        }
    }

    private void DrawPlayerStamina(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 44;
        const int barWidth = 198;
        const int barHeight = 10;
        const int borderWidth = 2;
        int fillWidth = (int)((barWidth - borderWidth * 2) *
            (player.CurrentStamina / player.MaxStamina));
        var border = new Rectangle(barX, barY, barWidth, barHeight);
        var background = new Rectangle(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(21, barY + 1, 11, 7),
            new Color(94, 192, 118));
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(24, 54, 35));

        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                new Color(83, 190, 112));
        }
    }

    private void DrawWeaponResource(PlayerCharacter player)
    {
        int maximum = player.Combat.Resources.ActiveMaximum;
        if (maximum <= 0)
            return;

        if (player.WeaponFamily == WeaponFamily.DualSwords)
        {
            DrawDuelistMomentum(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.HunterBow)
        {
            DrawHunterFocus(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.SpikedMace)
        {
            DrawBreakerInertia(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.ChainFlail)
        {
            DrawChainMomentum(player);
            return;
        }

        int value = player.Combat.Resources.ActiveValue;
        Color color = player.WeaponFamily switch
        {
            WeaponFamily.DualSwords => new Color(214, 113, 90),
            WeaponFamily.ArcaneWarStaff => new Color(151, 104, 214),
            WeaponFamily.ChainFlail => new Color(190, 154, 86),
            _ => new Color(154, 158, 164)
        };

        for (int index = 0; index < maximum; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                index < value ? color : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
        }
    }

    private void DrawBreakerInertia(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 91;
        const int barWidth = 198;
        const int barHeight = 7;
        float ratio = player.Combat.Resources.BreakerInertiaRatio;
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + 1,
            barY + 1,
            barWidth - 2,
            barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(118, 109, 99));
        _spriteBatch.Draw(_pixel, background, new Color(38, 31, 29));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            Color fill = Color.Lerp(
                new Color(126, 84, 61),
                new Color(174, 61, 45),
                ratio);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    background.X,
                    background.Y,
                    fillWidth,
                    background.Height),
                fill);
        }
        for (int segment = 1; segment < 10; segment++)
        {
            int x = background.X + background.Width * segment / 10;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, background.Y, 1, background.Height),
                new Color(28, 24, 23, 150));
        }
    }

    private void DrawChainMomentum(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.ChainMomentumRatio;
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + 1,
            barY + 1,
            barWidth - 2,
            barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(135, 126, 112));
        _spriteBatch.Draw(_pixel, background, new Color(34, 31, 29));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            Color fill = Color.Lerp(
                new Color(132, 116, 83),
                new Color(203, 155, 78),
                ratio);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    background.X,
                    background.Y,
                    fillWidth,
                    background.Height),
                fill);
        }

        for (int marker = 1; marker < 4; marker++)
        {
            int x = background.X + background.Width * marker / 4;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, background.Y, 1, background.Height),
                new Color(218, 201, 168, 150));
        }

        int activeSegments = player.Combat.Resources.ActiveValue;
        for (int segment = 0; segment < 10; segment++)
        {
            Rectangle pip = new(42 + segment * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                segment < activeSegments
                    ? new Color(190, 154, 86)
                    : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(164, 157, 143));
        }
    }

    private void DrawDuelistMomentum(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.DuelistMomentumRatio;
        Color momentum = Color.Lerp(
            new Color(164, 77, 72),
            new Color(245, 174, 104),
            ratio);
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(barX + 1, barY + 1, barWidth - 2, barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(48, 29, 32));

        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                momentum);
        }

        for (int index = 1; index < 3; index++)
        {
            int markerX = background.X + background.Width * index / 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(231, 219, 191, 175));
        }

        int tier = player.Combat.Resources.DuelistMomentumTier;
        for (int index = 0; index < 3; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            bool active = ratio > 0f && index <= tier;
            _spriteBatch.Draw(
                _pixel,
                pip,
                active ? momentum : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
        }
    }

    private void DrawHunterFocus(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.HunterFocusRatio;
        Color focus = Color.Lerp(
            new Color(99, 139, 113),
            new Color(225, 205, 112),
            ratio);
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(barX + 1, barY + 1, barWidth - 2, barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(30, 47, 37));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                focus);
        }

        for (int index = 1; index < 4; index++)
        {
            int markerX = background.X + background.Width * index / 4;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(226, 219, 177, 155));
        }

        int segments = player.Combat.Resources.ActiveValue;
        for (int index = 0; index < 4; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                index < segments ? focus : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
        }

        if (player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.RangerSkyfallMarker &&
            player.Combat.State == CombatState.ChargingHeavy)
        {
            int active = player.Combat.RangerSkyfallDistance switch
            {
                SkyfallDistance.Near => 0,
                SkyfallDistance.Medium => 1,
                _ => 2
            };
            for (int index = 0; index < 3; index++)
            {
                Rectangle distance = new(112 + index * 14, 91, 9, 6);
                _spriteBatch.Draw(
                    _pixel,
                    distance,
                    index == active
                        ? new Color(239, 205, 104)
                        : new Color(79, 77, 65));
                DrawRectangleOutline(distance, 1, new Color(213, 201, 163));
            }
        }
    }

    private void DrawTechniqueFeedback(PlayerCharacter player)
    {
        if (!player.Combat.IsTechniqueFeedbackVisible ||
            string.IsNullOrWhiteSpace(player.Combat.TechniqueFeedback))
            return;

        DrawDebugLabel(
            player.Combat.TechniqueFeedback,
            270,
            64,
            new Color(229, 207, 139));
    }

    private void DrawPlayerHealth(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 22;
        const int barWidth = 198;
        const int barHeight = 16;
        const int borderWidth = 2;

        float healthRatio = player.CurrentHealth / (float)player.MaxHealth;
        int fillWidth = (int)((barWidth - borderWidth * 2) * healthRatio);

        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);
        Rectangle health = new(
            background.X,
            background.Y,
            fillWidth,
            background.Height);

        _spriteBatch.Draw(_pixel, new Rectangle(20, 24, 14, 10), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, new Rectangle(23, 21, 4, 16), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, new Rectangle(29, 21, 4, 16), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, border, new Color(185, 190, 194));
        _spriteBatch.Draw(_pixel, background, new Color(62, 25, 30));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, health, new Color(196, 48, 58));
    }

    private void DrawPlayerProgression(PlayerCharacter player, int killCount)
    {
        const int barX = 42;
        const int barY = 59;
        const int barWidth = 198;
        const int barHeight = 10;
        const int borderWidth = 2;
        const int levelIndicatorY = 75;
        const int levelIndicatorSize = 8;
        const int levelIndicatorSpacing = 4;
        const int killIndicatorY = 89;
        const int killIndicatorSize = 6;
        const int killIndicatorSpacing = 3;

        float experienceRatio = player.CurrentExperience /
            (float)player.ExperienceToNextLevel;
        int fillWidth = (int)((barWidth - borderWidth * 2) * experienceRatio);

        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);
        Rectangle experience = new(
            background.X,
            background.Y,
            fillWidth,
            background.Height);

        _spriteBatch.Draw(_pixel, new Rectangle(22, barY + 1, 10, 8), new Color(87, 132, 222));
        _spriteBatch.Draw(_pixel, border, new Color(185, 190, 194));
        _spriteBatch.Draw(_pixel, background, new Color(28, 35, 66));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, experience, new Color(87, 132, 222));

        _spriteBatch.Draw(_pixel, new Rectangle(21, levelIndicatorY, 11, 8), new Color(235, 190, 62));
        int visibleLevel = Math.Min(player.Level, 17);

        for (int level = 0; level < visibleLevel; level++)
        {
            Rectangle indicator = new(
                barX + level * (levelIndicatorSize + levelIndicatorSpacing),
                levelIndicatorY,
                levelIndicatorSize,
                levelIndicatorSize);
            _spriteBatch.Draw(_pixel, indicator, new Color(235, 190, 62));
        }

        _spriteBatch.Draw(_pixel, new Rectangle(22, killIndicatorY, 9, 6), new Color(181, 72, 66));
        int visibleKills = Math.Min(killCount, 22);

        for (int kill = 0; kill < visibleKills; kill++)
        {
            Rectangle indicator = new(
                barX + kill * (killIndicatorSize + killIndicatorSpacing),
                killIndicatorY,
                killIndicatorSize,
                killIndicatorSize);
            _spriteBatch.Draw(_pixel, indicator, new Color(181, 72, 66));
        }
    }

    private void DrawStartScreen()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 180,
            viewport.Height / 2 - 110,
            360,
            220);

        _spriteBatch.Draw(_pixel, screen, new Color(8, 10, 16, 125));
        _spriteBatch.Draw(_pixel, panel, new Color(20, 25, 34, 240));
        DrawRectangleOutline(panel, 4, new Color(155, 121, 56));

        int centerX = panel.Center.X;
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 100, panel.Y + 34, 200, 6), new Color(223, 183, 76));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 72, panel.Y + 48, 144, 4), new Color(124, 101, 58));

        var shield = new Rectangle(centerX - 30, panel.Y + 67, 60, 54);
        _spriteBatch.Draw(_pixel, shield, new Color(49, 89, 126));
        DrawRectangleOutline(shield, 3, new Color(188, 196, 202));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 3, shield.Y + 7, 6, 40), new Color(214, 220, 224));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 18, shield.Y + 17, 36, 6), new Color(214, 220, 224));

        var startButton = new Rectangle(centerX - 78, panel.Bottom - 65, 156, 38);
        _spriteBatch.Draw(_pixel, startButton, new Color(38, 50, 64));
        DrawRectangleOutline(startButton, 2, new Color(105, 176, 120));
        _spriteBatch.Draw(_pixel, new Rectangle(startButton.X + 42, startButton.Center.Y - 3, 62, 6), new Color(128, 216, 145));
        _spriteBatch.Draw(_pixel, new Rectangle(startButton.Right - 53, startButton.Center.Y - 13, 6, 16), new Color(128, 216, 145));
        _spriteBatch.Draw(_pixel, new Rectangle(startButton.Right - 62, startButton.Center.Y + 3, 15, 6), new Color(128, 216, 145));
    }

    private void DrawPauseOverlay()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 105,
            viewport.Height / 2 - 70,
            210,
            140);

        _spriteBatch.Draw(_pixel, screen, new Color(8, 10, 15, 155));
        _spriteBatch.Draw(_pixel, panel, new Color(25, 30, 39, 235));
        DrawRectangleOutline(panel, 3, new Color(142, 151, 160));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.Center.X - 30, panel.Y + 34, 20, 58), new Color(215, 220, 224));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.Center.X + 10, panel.Y + 34, 20, 58), new Color(215, 220, 224));
        _spriteBatch.Draw(_pixel, new Rectangle(panel.Center.X - 48, panel.Bottom - 26, 96, 5), new Color(91, 111, 132));
    }

    private void DrawGameOverOverlay()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 130,
            viewport.Height / 2 - 90,
            260,
            180);

        _spriteBatch.Draw(_pixel, screen, new Color(48, 8, 12, 150));
        _spriteBatch.Draw(_pixel, panel, new Color(35, 20, 25, 240));
        DrawRectangleOutline(panel, 3, new Color(165, 62, 67));

        var skull = new Rectangle(panel.Center.X - 34, panel.Y + 25, 68, 58);
        _spriteBatch.Draw(_pixel, skull, new Color(205, 200, 184));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.X + 13, skull.Y + 18, 13, 14), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Right - 26, skull.Y + 18, 13, 14), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Center.X - 5, skull.Y + 35, 10, 9), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.X + 12, skull.Bottom, 44, 12), new Color(205, 200, 184));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Center.X - 2, skull.Bottom, 4, 12), new Color(71, 54, 54));

        var restartButton = new Rectangle(panel.Center.X - 65, panel.Bottom - 45, 130, 28);
        _spriteBatch.Draw(_pixel, restartButton, new Color(57, 31, 35));
        DrawRectangleOutline(restartButton, 2, new Color(210, 90, 91));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 35, restartButton.Center.Y - 3, 54, 6), new Color(226, 112, 109));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 35, restartButton.Center.Y - 11, 6, 14), new Color(226, 112, 109));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 29, restartButton.Center.Y - 11, 12, 6), new Color(226, 112, 109));
    }

    private void DrawRectangleOutline(Rectangle rectangle, int thickness, Color color)
    {
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height),
            color);
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}
