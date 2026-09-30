using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Owns MonoGame-specific drawing for the active game session.
/// </summary>
public sealed class GameRenderer
{
    private readonly GraphicsDevice _graphicsDevice;

    public GameRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Draw()
    {
        _graphicsDevice.Clear(Color.CornflowerBlue);
    }
}
