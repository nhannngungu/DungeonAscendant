using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

/// <summary>
/// Owns the platform-independent run state, dungeon, and gameplay update flow.
/// </summary>
public sealed class GameSession
{
    private const float PlayerAttackThickness = 48f;
    private const float PlayerKnockbackDistance = 24f;

    private readonly DungeonGenerator _dungeonGenerator;
    private KeyboardState _previousKeyboardState;

    public PlayerCharacter Player { get; private set; }
    public MeleeAttack PlayerAttack { get; private set; }
    public EnemyManager Enemies { get; }
    public DungeonMap CurrentDungeon { get; private set; }
    public Camera2D Camera { get; }
    public int KillCount { get; private set; }
    public Rectangle PlayerAttackArea { get; private set; }
    public GameState State { get; private set; }

    public GameSession(Rectangle viewportBounds, int? randomSeed = null)
    {
        _dungeonGenerator = new DungeonGenerator(randomSeed);
        Enemies = new EnemyManager(randomSeed);
        Camera = new Camera2D(viewportBounds.Width, viewportBounds.Height);
        Player = new PlayerCharacter(viewportBounds.Center.ToVector2());
        PlayerAttack = new MeleeAttack();
        UpdatePlayerAttackArea();
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
            {
                ResetRun();
                State = GameState.Playing;
            }

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

        Player.UpdateTimers(gameTime);
        Vector2 desiredPosition = Player.GetDesiredPosition(
            gameTime,
            keyboardState);
        Player.MoveTo(DungeonCollision.ResolveMovement(
            Player.Position,
            desiredPosition,
            Player.Size,
            CurrentDungeon));

        PlayerAttack.Update(gameTime);
        Enemies.UpdateTimers(gameTime, Player.Position, CurrentDungeon);
        UpdatePlayerAttackArea();

        bool attackPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (attackPressed && PlayerAttack.TryStart())
        {
            foreach (Goblin goblin in Enemies.Goblins)
            {
                if (!goblin.IsAlive ||
                    !PlayerAttackArea.Intersects(goblin.Bounds))
                {
                    continue;
                }

                goblin.ReceiveDamage(Player.MeleeDamage);
                goblin.ApplyKnockback(
                    Player.Position,
                    PlayerKnockbackDistance,
                    CurrentDungeon);
            }
        }

        ProcessDefeatedGoblins();
        Enemies.UpdateCombatAndAi(gameTime, Player, CurrentDungeon);

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
            State = GameState.GameOver;
        }

        Camera.Follow(Player.Position, CurrentDungeon.WorldBounds);
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
        CurrentDungeon = _dungeonGenerator.Generate();
        Player = new PlayerCharacter(CurrentDungeon.StartRoom.Center);
        PlayerAttack = new MeleeAttack();
        KillCount = 0;
        Enemies.Reset(CurrentDungeon, Player.Level);
        UpdatePlayerAttackArea();
        Camera.Follow(Player.Position, CurrentDungeon.WorldBounds);
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
