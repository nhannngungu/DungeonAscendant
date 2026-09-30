using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

/// <summary>
/// Owns the platform-independent game state and update flow.
/// </summary>
public sealed class GameSession
{
    public PlayerCharacter Player { get; }

    public GameSession(Vector2 playerSpawnPosition)
    {
        Player = new PlayerCharacter(playerSpawnPosition);
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Player.Update(gameTime, keyboardState);
    }
}
