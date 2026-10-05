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
    private const float PlayerAttackThickness = 48f;
    private const float PlayerKnockbackDistance = 24f;

    private readonly Vector2 _playerSpawnPosition;
    private readonly Rectangle _arenaBounds;
    private KeyboardState _previousKeyboardState;

    public PlayerCharacter Player { get; private set; }
    public MeleeAttack PlayerAttack { get; private set; }
    public EnemyManager Enemies { get; }
    public int KillCount { get; private set; }
    public Rectangle PlayerAttackArea { get; private set; }
    public GameState State { get; private set; }

    public GameSession(Vector2 playerSpawnPosition, Rectangle arenaBounds)
    {
        _playerSpawnPosition = playerSpawnPosition;
        _arenaBounds = arenaBounds;
        Enemies = new EnemyManager(arenaBounds);
        ResetRun();
        State = GameState.Start;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        bool startPressed = keyboardState.IsKeyDown(Keys.Enter) &&
            !_previousKeyboardState.IsKeyDown(Keys.Enter);
        bool pausePressed = keyboardState.IsKeyDown(Keys.Escape) &&
            !_previousKeyboardState.IsKeyDown(Keys.Escape);
        bool restartPressed = keyboardState.IsKeyDown(Keys.R) &&
            !_previousKeyboardState.IsKeyDown(Keys.R);

        if (State == GameState.Start)
        {
            if (startPressed)
                State = GameState.Playing;

            _previousKeyboardState = keyboardState;
            return;
        }

        if (State == GameState.Paused)
        {
            if (pausePressed)
                State = GameState.Playing;

            _previousKeyboardState = keyboardState;
            return;
        }

        if (State == GameState.GameOver)
        {
            if (restartPressed)
            {
                ResetRun();
                State = GameState.Playing;
            }

            _previousKeyboardState = keyboardState;
            return;
        }

        if (pausePressed)
        {
            State = GameState.Paused;
            _previousKeyboardState = keyboardState;
            return;
        }

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
            State = GameState.GameOver;
            _previousKeyboardState = keyboardState;
            return;
        }

        Player.Update(gameTime, keyboardState, _arenaBounds);
        PlayerAttack.Update(gameTime);
        Enemies.UpdateTimers(gameTime);
        UpdatePlayerAttackArea();

        bool attackPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (attackPressed && PlayerAttack.TryStart())
        {
            foreach (Goblin goblin in Enemies.Goblins)
            {
                if (!goblin.IsAlive || !PlayerAttackArea.Intersects(goblin.Bounds))
                    continue;

                goblin.ReceiveDamage(Player.MeleeDamage);
                goblin.ApplyKnockback(
                    Player.Position,
                    PlayerKnockbackDistance,
                    _arenaBounds);
            }
        }

        ProcessDefeatedGoblins();
        Enemies.UpdateCombatAndAi(gameTime, Player);

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
            State = GameState.GameOver;
        }
        else
        {
            Enemies.UpdateSpawning(
                gameTime,
                Player.Position,
                Player.Level,
                KillCount);
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

    private void ResetRun()
    {
        Player = new PlayerCharacter(_playerSpawnPosition);
        PlayerAttack = new MeleeAttack();
        KillCount = 0;
        Enemies.Reset(Player.Position, Player.Level);
        UpdatePlayerAttackArea();
    }

    private void UpdatePlayerAttackArea()
    {
        PlayerAttackArea = MeleeHitArea.Create(
            Player.Position,
            Player.Size,
            Player.Facing,
            PlayerAttack.Range,
            PlayerAttackThickness);
    }
}
