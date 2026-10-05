using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Owns MonoGame-specific drawing for the active game session.
/// </summary>
public sealed class GameRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public GameRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(GameSession gameSession)
    {
        _graphicsDevice.Clear(new Color(9, 11, 16));

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
        DrawDungeon(gameSession.CurrentDungeon, gameSession.Camera.ViewBounds);

        if (gameSession.PlayerAttack.IsActive)
            DrawAttackArea(gameSession.PlayerAttackArea);

        DrawPlayer(gameSession.Player, gameSession.PlayerAttack.IsActive);

        foreach (Goblin goblin in gameSession.Enemies.Goblins)
        {
            if (goblin.IsAlive &&
                goblin.Bounds.Intersects(gameSession.Camera.ViewBounds))
            {
                DrawGoblin(goblin);
            }
        }

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHudPanel();
        DrawPlayerHealth(gameSession.Player);
        DrawPlayerProgression(gameSession.Player, gameSession.KillCount);

        if (gameSession.State == GameState.Paused)
            DrawPauseOverlay();
        else if (gameSession.State == GameState.GameOver)
            DrawGameOverOverlay();

        _spriteBatch.End();
    }

    private void DrawDungeon(DungeonMap dungeon, Rectangle cameraBounds)
    {
        Rectangle visibleBounds = cameraBounds;
        visibleBounds.Inflate(80, 80);
        Color wallColor = new(48, 43, 46);
        Color corridorFloor = new(45, 50, 52);

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (!corridor.Intersects(visibleBounds))
                continue;

            Rectangle wall = corridor;
            wall.Inflate(8, 8);
            _spriteBatch.Draw(_pixel, wall, wallColor);
        }

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            Rectangle wall = room.Bounds;
            wall.Inflate(10, 10);
            _spriteBatch.Draw(_pixel, wall, wallColor);
            DrawRectangleOutline(wall, 3, new Color(82, 71, 71));
        }

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (!corridor.Intersects(visibleBounds))
                continue;

            _spriteBatch.Draw(_pixel, corridor, corridorFloor);
            DrawFloorGrid(corridor, visibleBounds, new Color(72, 77, 78, 75));
        }

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            Color floorColor = room.Type switch
            {
                RoomType.Start => new Color(43, 55, 53),
                RoomType.Enemy => new Color(51, 45, 46),
                RoomType.Exit => new Color(54, 49, 42),
                _ => new Color(47, 51, 52)
            };

            _spriteBatch.Draw(_pixel, room.Bounds, floorColor);
            DrawFloorGrid(room.Bounds, visibleBounds, new Color(76, 82, 82, 80));

            if (room.Type == RoomType.Start)
                DrawStartMarker(room.Center);
            else if (room.Type == RoomType.Exit)
                DrawExitMarker(room.Center);
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

    private void DrawStartMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 25, (int)center.Y - 25, 50, 50);
        var inner = new Rectangle((int)center.X - 17, (int)center.Y - 17, 34, 34);
        DrawRectangleOutline(outer, 3, new Color(76, 169, 126, 190));
        DrawRectangleOutline(inner, 2, new Color(111, 205, 157, 150));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 3, (int)center.Y - 13, 6, 26), new Color(128, 218, 169, 165));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 13, (int)center.Y - 3, 26, 6), new Color(128, 218, 169, 165));
    }

    private void DrawExitMarker(Vector2 center)
    {
        var frame = new Rectangle((int)center.X - 28, (int)center.Y - 34, 56, 68);
        var doorway = new Rectangle(frame.X + 9, frame.Y + 10, frame.Width - 18, frame.Height - 10);
        _spriteBatch.Draw(_pixel, frame, new Color(136, 104, 48, 205));
        _spriteBatch.Draw(_pixel, doorway, new Color(30, 21, 43));
        DrawRectangleOutline(frame, 3, new Color(226, 190, 85));
        _spriteBatch.Draw(_pixel, new Rectangle(doorway.Center.X - 3, doorway.Center.Y - 3, 6, 6), new Color(224, 185, 72));
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

    private void DrawPlayer(PlayerCharacter player, bool isAttacking)
    {
        Vector2 topLeft = player.Position - player.Size / 2f;
        int centerX = (int)player.Position.X;
        int centerY = (int)player.Position.Y;
        int swordLength = isAttacking ? 52 : 28;
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

        switch (player.Facing)
        {
            case FacingDirection.Up:
                swordBlade = new Rectangle(
                    centerX + 7,
                    (int)topLeft.Y - swordLength + 3,
                    swordThickness,
                    swordLength);
                swordGuard = new Rectangle(
                    swordBlade.X - 4,
                    swordBlade.Bottom - 3,
                    14,
                    6);
                swordHandle = new Rectangle(
                    swordBlade.X,
                    swordGuard.Bottom - 1,
                    swordThickness,
                    9);
                break;
            case FacingDirection.Down:
                swordBlade = new Rectangle(
                    centerX + 7,
                    (int)(topLeft.Y + player.Size.Y) - 3,
                    swordThickness,
                    swordLength);
                swordGuard = new Rectangle(
                    swordBlade.X - 4,
                    swordBlade.Y - 3,
                    14,
                    6);
                swordHandle = new Rectangle(
                    swordBlade.X,
                    swordGuard.Y - 8,
                    swordThickness,
                    9);
                break;
            case FacingDirection.Left:
                swordBlade = new Rectangle(
                    (int)topLeft.X - swordLength + 3,
                    centerY + 3,
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
                break;
            default:
                swordBlade = new Rectangle(
                    (int)(topLeft.X + player.Size.X) - 3,
                    centerY + 3,
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
                break;
        }

        Rectangle eye = CreatePlayerEye(head, player.Facing);
        Color bodyColor = player.IsHitFlashing
            ? new Color(220, 75, 76)
            : new Color(48, 112, 168);
        Color headColor = player.IsHitFlashing
            ? new Color(255, 220, 205)
            : new Color(232, 185, 137);
        Color bladeColor = isAttacking
            ? new Color(245, 238, 185)
            : new Color(205, 215, 226);

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

        if (player.IsInvulnerable)
        {
            Rectangle outline = new(
                (int)topLeft.X - 2,
                (int)topLeft.Y - 2,
                (int)player.Size.X + 4,
                (int)player.Size.Y + 4);
            DrawRectangleOutline(outline, 2, new Color(245, 115, 115, 150));
        }
    }

    private static Rectangle CreatePlayerEye(
        Rectangle head,
        FacingDirection facingDirection)
    {
        return facingDirection switch
        {
            FacingDirection.Up => new Rectangle(head.Center.X - 2, head.Y + 3, 4, 4),
            FacingDirection.Down => new Rectangle(head.Center.X - 2, head.Bottom - 7, 4, 4),
            FacingDirection.Left => new Rectangle(head.X + 3, head.Y + 8, 4, 4),
            _ => new Rectangle(head.Right - 7, head.Y + 8, 4, 4)
        };
    }

    private void DrawGoblin(Goblin goblin)
    {
        Vector2 topLeft = goblin.Position - goblin.Size / 2f;
        int clubLength = goblin.Attack.IsActive ? 39 : 25;

        Rectangle shadow = new(
            (int)topLeft.X + 3,
            (int)(topLeft.Y + goblin.Size.Y) - 5,
            (int)goblin.Size.X - 6,
            7);
        Rectangle leftEar = new(
            (int)topLeft.X,
            (int)topLeft.Y + 7,
            8,
            10);
        Rectangle rightEar = new(
            (int)(topLeft.X + goblin.Size.X) - 8,
            (int)topLeft.Y + 7,
            8,
            10);
        Rectangle body = new(
            (int)topLeft.X + 7,
            (int)topLeft.Y + 20,
            (int)goblin.Size.X - 14,
            (int)goblin.Size.Y - 20);
        Rectangle head = new(
            (int)topLeft.X + 5,
            (int)topLeft.Y + 3,
            (int)goblin.Size.X - 10,
            22);
        Rectangle leftEye = new(head.X + 5, head.Y + 7, 4, 4);
        Rectangle rightEye = new(head.Right - 9, head.Y + 7, 4, 4);
        Rectangle clubHandle = new(body.Right + 1, body.Y + 3, 5, clubLength);
        Rectangle clubHead = new(
            clubHandle.X - 3,
            clubHandle.Bottom - 8,
            11,
            10);
        Rectangle leftLeg = new(body.X + 2, body.Bottom - 3, 7, 8);
        Rectangle rightLeg = new(body.Right - 9, body.Bottom - 3, 7, 8);

        GetGoblinColors(
            goblin,
            out Color skinColor,
            out Color bodyColor,
            out Color eyeColor);

        _spriteBatch.Draw(_pixel, shadow, new Color(16, 19, 22, 145));

        if (goblin.IsElite)
        {
            Rectangle eliteOutline = new(
                (int)topLeft.X - 3,
                (int)topLeft.Y - 5,
                (int)MathF.Ceiling(goblin.Size.X) + 6,
                (int)MathF.Ceiling(goblin.Size.Y) + 8);
            DrawRectangleOutline(eliteOutline, 3, new Color(242, 190, 45));
        }

        _spriteBatch.Draw(_pixel, leftEar, skinColor);
        _spriteBatch.Draw(_pixel, rightEar, skinColor);
        _spriteBatch.Draw(_pixel, leftLeg, bodyColor);
        _spriteBatch.Draw(_pixel, rightLeg, bodyColor);
        _spriteBatch.Draw(_pixel, clubHandle, new Color(103, 68, 42));
        _spriteBatch.Draw(
            _pixel,
            clubHead,
            goblin.IsElite ? new Color(214, 160, 47) : new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, body, bodyColor);
        _spriteBatch.Draw(_pixel, head, skinColor);
        _spriteBatch.Draw(_pixel, leftEye, eyeColor);
        _spriteBatch.Draw(_pixel, rightEye, eyeColor);

        if (goblin.Variant == GoblinVariant.Fast)
        {
            Rectangle sash = new(body.X + 2, body.Y + 7, body.Width - 4, 4);
            _spriteBatch.Draw(_pixel, sash, new Color(76, 180, 184));
        }
        else if (goblin.Variant == GoblinVariant.Brute)
        {
            Rectangle leftShoulder = new(body.X - 4, body.Y + 2, 7, 9);
            Rectangle rightShoulder = new(body.Right - 3, body.Y + 2, 7, 9);
            _spriteBatch.Draw(_pixel, leftShoulder, new Color(82, 64, 62));
            _spriteBatch.Draw(_pixel, rightShoulder, new Color(82, 64, 62));
        }

        if (goblin.IsElite)
        {
            Rectangle crownBase = new(head.X + 3, head.Y - 6, head.Width - 6, 5);
            Rectangle crownPoint = new(head.Center.X - 3, head.Y - 10, 6, 5);
            _spriteBatch.Draw(_pixel, crownBase, new Color(242, 190, 45));
            _spriteBatch.Draw(_pixel, crownPoint, new Color(255, 220, 78));
        }
    }

    private static void GetGoblinColors(
        Goblin goblin,
        out Color skinColor,
        out Color bodyColor,
        out Color eyeColor)
    {
        if (goblin.IsHitFlashing)
        {
            skinColor = new Color(245, 245, 225);
            bodyColor = new Color(205, 205, 195);
            eyeColor = new Color(255, 190, 45);
            return;
        }

        switch (goblin.Variant)
        {
            case GoblinVariant.Fast:
                skinColor = new Color(126, 190, 76);
                bodyColor = new Color(47, 92, 86);
                eyeColor = new Color(242, 126, 45);
                break;
            case GoblinVariant.Brute:
                skinColor = new Color(72, 123, 58);
                bodyColor = new Color(112, 55, 45);
                eyeColor = new Color(245, 55, 45);
                break;
            default:
                skinColor = new Color(91, 156, 75);
                bodyColor = new Color(104, 69, 47);
                eyeColor = new Color(225, 50, 45);
                break;
        }
    }

    private void DrawHudPanel()
    {
        var panel = new Rectangle(12, 12, 238, 78);
        _spriteBatch.Draw(_pixel, panel, new Color(17, 21, 28, 220));
        DrawRectangleOutline(panel, 2, new Color(112, 119, 126, 230));
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
        const int barY = 46;
        const int barWidth = 198;
        const int barHeight = 10;
        const int borderWidth = 2;
        const int levelIndicatorY = 62;
        const int levelIndicatorSize = 8;
        const int levelIndicatorSpacing = 4;
        const int killIndicatorY = 76;
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

        _spriteBatch.Draw(_pixel, new Rectangle(22, 47, 10, 8), new Color(87, 132, 222));
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
