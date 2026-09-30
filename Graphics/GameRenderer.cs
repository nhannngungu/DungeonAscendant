using System;
using DungeonAscendant.Core;
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
        DrawPlayer(gameSession.Player);
        _spriteBatch.End();
    }

    private void DrawPlayer(PlayerCharacter player)
    {
        Vector2 topLeft = player.Position - player.Size / 2f;

        Rectangle swordBlade = new(
            (int)(topLeft.X + player.Size.X - 2f),
            (int)(topLeft.Y + 19f),
            30,
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

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}
