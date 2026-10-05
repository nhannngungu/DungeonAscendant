using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using DungeonAscendant.Enemies;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

/// <summary>
/// Owns the platform-independent game state and update flow.
/// </summary>
public sealed class GameSession
{
    public PlayerCharacter Player { get; }
    public Goblin Goblin { get; }

    public GameSession(Vector2 playerSpawnPosition)
    {
        Player = new PlayerCharacter(playerSpawnPosition);
        Goblin = new Goblin(playerSpawnPosition + new Vector2(280f, 0f));
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Player.Update(gameTime, keyboardState);
        Goblin.Update(gameTime, Player.Position);
    }
}
