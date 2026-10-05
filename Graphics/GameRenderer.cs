using System;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
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
        DrawPlayer(gameSession.Player, gameSession.PlayerAttack.IsActive);

        if (gameSession.Goblin.IsAlive)
            DrawGoblin(gameSession.Goblin, gameSession.GoblinAttack.IsActive);

        DrawPlayerHealth(gameSession.Player);

        _spriteBatch.End();
    }

    private void DrawPlayer(PlayerCharacter player, bool isAttacking)
    {
        Vector2 topLeft = player.Position - player.Size / 2f;
        int swordLength = isAttacking ? 48 : 30;

        Rectangle swordBlade = new(
            (int)(topLeft.X + player.Size.X - 2f),
            (int)(topLeft.Y + 19f),
            swordLength,
            7);
        Rectangle swordGuard = new(
            swordBlade.X - 5,
            swordBlade.Y - 4,
            6,
            15);
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
        Rectangle eye = new(head.Right - 7, head.Y + 8, 4, 4);

        _spriteBatch.Draw(_pixel, swordBlade, new Color(205, 215, 226));
        _spriteBatch.Draw(_pixel, swordGuard, new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, body, new Color(48, 112, 168));
        _spriteBatch.Draw(_pixel, head, new Color(232, 185, 137));
        _spriteBatch.Draw(_pixel, eye, new Color(30, 25, 27));
    }

    private void DrawGoblin(Goblin goblin, bool isAttacking)
    {
        Vector2 topLeft = goblin.Position - goblin.Size / 2f;
        int clubLength = isAttacking ? 39 : 25;

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

        Color goblinGreen = new(91, 156, 75);
        _spriteBatch.Draw(_pixel, leftEar, goblinGreen);
        _spriteBatch.Draw(_pixel, rightEar, goblinGreen);
        _spriteBatch.Draw(_pixel, clubHandle, new Color(103, 68, 42));
        _spriteBatch.Draw(_pixel, clubHead, new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, body, new Color(104, 69, 47));
        _spriteBatch.Draw(_pixel, head, goblinGreen);
        _spriteBatch.Draw(_pixel, leftEye, new Color(225, 50, 45));
        _spriteBatch.Draw(_pixel, rightEye, new Color(225, 50, 45));
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

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}
