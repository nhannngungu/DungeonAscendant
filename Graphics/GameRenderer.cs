using System;
using DungeonAscendant.Core;
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
        _graphicsDevice.Clear(new Color(28, 34, 48));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (gameSession.PlayerAttack.IsActive)
            DrawAttackArea(gameSession.PlayerAttackArea);

        DrawPlayer(gameSession.Player, gameSession.PlayerAttack.IsActive);

        foreach (Goblin goblin in gameSession.Enemies.Goblins)
        {
            if (goblin.IsAlive)
                DrawGoblin(goblin);
        }

        DrawPlayerHealth(gameSession.Player);
        DrawPlayerProgression(gameSession.Player, gameSession.KillCount);

        _spriteBatch.End();
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

        Rectangle swordBlade;
        Rectangle swordGuard;

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

        _spriteBatch.Draw(_pixel, swordBlade, bladeColor);
        _spriteBatch.Draw(_pixel, swordGuard, new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, body, bodyColor);
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

        GetGoblinColors(
            goblin,
            out Color skinColor,
            out Color bodyColor,
            out Color eyeColor);

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
        _spriteBatch.Draw(_pixel, clubHandle, new Color(103, 68, 42));
        _spriteBatch.Draw(
            _pixel,
            clubHead,
            goblin.IsElite ? new Color(214, 160, 47) : new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, body, bodyColor);
        _spriteBatch.Draw(_pixel, head, skinColor);
        _spriteBatch.Draw(_pixel, leftEye, eyeColor);
        _spriteBatch.Draw(_pixel, rightEye, eyeColor);

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

    private void DrawPlayerHealth(PlayerCharacter player)
    {
        const int barX = 20;
        const int barY = 20;
        const int barWidth = 220;
        const int barHeight = 18;
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

        _spriteBatch.Draw(_pixel, border, new Color(225, 225, 225));
        _spriteBatch.Draw(_pixel, background, new Color(62, 25, 30));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, health, new Color(196, 48, 58));
    }

    private void DrawPlayerProgression(PlayerCharacter player, int killCount)
    {
        const int barX = 20;
        const int barY = 44;
        const int barWidth = 220;
        const int barHeight = 12;
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

        _spriteBatch.Draw(_pixel, border, new Color(225, 225, 225));
        _spriteBatch.Draw(_pixel, background, new Color(28, 35, 66));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, experience, new Color(87, 132, 222));

        int visibleLevel = Math.Min(player.Level, 18);

        for (int level = 0; level < visibleLevel; level++)
        {
            Rectangle indicator = new(
                barX + level * (levelIndicatorSize + levelIndicatorSpacing),
                levelIndicatorY,
                levelIndicatorSize,
                levelIndicatorSize);
            _spriteBatch.Draw(_pixel, indicator, new Color(235, 190, 62));
        }

        int visibleKills = Math.Min(killCount, 24);

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
