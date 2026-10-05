using DungeonAscendant.Combat;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

/// <summary>
/// Owns the platform-independent game state and update flow.
/// </summary>
public sealed class GameSession
{
    private KeyboardState _previousKeyboardState;

    public PlayerCharacter Player { get; }
    public Goblin Goblin { get; }
    public MeleeAttack PlayerAttack { get; }

    public GameSession(Vector2 playerSpawnPosition)
    {
        Player = new PlayerCharacter(playerSpawnPosition);
        Goblin = new Goblin(playerSpawnPosition + new Vector2(280f, 0f));
        PlayerAttack = new MeleeAttack();
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Player.Update(gameTime, keyboardState);
        PlayerAttack.Update(gameTime);

        bool attackPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (attackPressed)
            PlayerAttack.TryHit(Player.Position, Goblin);

        Goblin.Update(gameTime, Player.Position);
        _previousKeyboardState = keyboardState;
    }
}
