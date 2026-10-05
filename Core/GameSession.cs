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
    private readonly Vector2 _playerSpawnPosition;
    private readonly Rectangle _arenaBounds;
    private KeyboardState _previousKeyboardState;

    public PlayerCharacter Player { get; private set; }
    public MeleeAttack PlayerAttack { get; private set; }
    public EnemyManager Enemies { get; }
    public int KillCount { get; private set; }

    public GameSession(Vector2 playerSpawnPosition, Rectangle arenaBounds)
    {
        _playerSpawnPosition = playerSpawnPosition;
        _arenaBounds = arenaBounds;
        Enemies = new EnemyManager(arenaBounds);
        RestartRun();
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        bool restartPressed = keyboardState.IsKeyDown(Keys.R) &&
            !_previousKeyboardState.IsKeyDown(Keys.R);

        if (!Player.IsAlive)
        {
            if (restartPressed)
                RestartRun();

            _previousKeyboardState = keyboardState;
            return;
        }

        Player.Update(gameTime, keyboardState, _arenaBounds);
        PlayerAttack.Update(gameTime);

        bool attackPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (attackPressed && PlayerAttack.TryStart())
        {
            Goblin target = Enemies.FindNearestTarget(
                Player.Position,
                PlayerAttack.Range);

            if (target != null)
                target.ReceiveDamage(Player.MeleeDamage);
        }

        ProcessDefeatedGoblins();
        Enemies.UpdateCombatAndAi(gameTime, Player);

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
        }
        else
        {
            Enemies.UpdateSpawning(gameTime, Player.Position, Player.Level);
        }

        _previousKeyboardState = keyboardState;
    }

    private void ProcessDefeatedGoblins()
    {
        int defeatedCount = Enemies.RemoveDefeated(out int experienceReward);

        if (defeatedCount == 0)
            return;

        Player.GainExperience(experienceReward);
        KillCount += defeatedCount;
    }

    private void RestartRun()
    {
        Player = new PlayerCharacter(_playerSpawnPosition);
        PlayerAttack = new MeleeAttack();
        KillCount = 0;
        Enemies.Reset(Player.Position, Player.Level);
    }
}
