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
    private bool _goblinExperienceAwarded;

    public PlayerCharacter Player { get; }
    public Goblin Goblin { get; }
    public MeleeAttack PlayerAttack { get; }
    public MeleeAttack GoblinAttack { get; }

    public GameSession(Vector2 playerSpawnPosition)
    {
        Player = new PlayerCharacter(playerSpawnPosition);
        Goblin = new Goblin(
            playerSpawnPosition + new Vector2(280f, 0f),
            Player.Level);
        PlayerAttack = new MeleeAttack();
        GoblinAttack = new MeleeAttack(
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
            Goblin.ReceiveDamage(Player.MeleeDamage);
        }

        AwardGoblinExperienceIfDefeated();

        if (Goblin.IsAlive && Player.IsAlive)
        {
            float distanceSquared = Vector2.DistanceSquared(
                Goblin.Position,
                Player.Position);

            if (distanceSquared <= GoblinAttack.Range * GoblinAttack.Range)
            {
                if (GoblinAttack.TryPerform(Goblin.Position, Player.Position))
                {
                    Player.ReceiveDamage(Goblin.AttackDamage);

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

    private void AwardGoblinExperienceIfDefeated()
    {
        if (Goblin.IsAlive || _goblinExperienceAwarded)
            return;

        _goblinExperienceAwarded = true;
        Player.GainExperience(Goblin.ExperienceReward);
    }
}
