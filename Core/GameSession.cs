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
    public MeleeAttack GoblinAttack { get; }

    public GameSession(Vector2 playerSpawnPosition)
    {
        Player = new PlayerCharacter(playerSpawnPosition);
        Goblin = new Goblin(playerSpawnPosition + new Vector2(280f, 0f));
        PlayerAttack = new MeleeAttack();
        GoblinAttack = new MeleeAttack(
            damage: 10,
            range: 50f,
            cooldownSeconds: 1f);
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Player.Update(gameTime, keyboardState);
        PlayerAttack.Update(gameTime);
        GoblinAttack.Update(gameTime);

        bool attackPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (Player.IsAlive &&
            attackPressed &&
            PlayerAttack.TryPerform(Player.Position, Goblin.Position) &&
            Goblin.IsAlive)
        {
            Goblin.ReceiveDamage(PlayerAttack.Damage);
        }

        if (Goblin.IsAlive && Player.IsAlive)
        {
            float distanceSquared = Vector2.DistanceSquared(
                Goblin.Position,
                Player.Position);

            if (distanceSquared <= GoblinAttack.Range * GoblinAttack.Range)
            {
                if (GoblinAttack.TryPerform(Goblin.Position, Player.Position))
                {
                    Player.ReceiveDamage(GoblinAttack.Damage);

                    if (!Player.IsAlive)
                        PlayerAttack.Cancel();
                }
            }
            else
            {
                Goblin.Update(gameTime, Player.Position, GoblinAttack.Range);
            }
        }

        if (!Player.IsAlive)
            PlayerAttack.Cancel();

        _previousKeyboardState = keyboardState;
    }
}
