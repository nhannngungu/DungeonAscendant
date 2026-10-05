using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
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
    private const float ExitInteractionRadius = 72f;

    private readonly DungeonGenerator _dungeonGenerator;
    private readonly List<Goblin> _defeatedGoblins = new();
    private KeyboardState _previousKeyboardState;

    public PlayerCharacter Player { get; private set; }
    public MeleeAttack PlayerAttack { get; private set; }
    public EnemyManager Enemies { get; }
    public LootManager Loot { get; }
    public TreasureChest Chest { get; private set; }
    public GoblinWarlord Boss { get; private set; }
    public DungeonMap CurrentDungeon { get; private set; }
    public Camera2D Camera { get; }
    public int KillCount { get; private set; }
    public int DungeonDepth { get; private set; }
    public bool BossDefeated { get; private set; }
    public bool IsExitUnlocked => BossDefeated;
    public Rectangle PlayerAttackArea { get; private set; }
    public GameState State { get; private set; }
    public bool IsInventoryOpen { get; private set; }
    public int SelectedInventoryIndex { get; private set; }
    public EquipmentItem SelectedInventoryItem =>
        Player.Inventory.GetItem(SelectedInventoryIndex);

    public GameSession(Rectangle viewportBounds, int? randomSeed = null)
    {
        _dungeonGenerator = new DungeonGenerator(randomSeed);
        Enemies = new EnemyManager(randomSeed);
        Loot = new LootManager(randomSeed);
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
        bool inventoryPressed = keyboardState.IsKeyDown(Keys.I) &&
            !_previousKeyboardState.IsKeyDown(Keys.I);

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

        if (IsInventoryOpen)
        {
            if (inventoryPressed)
                IsInventoryOpen = false;
            else
                UpdateInventoryInput(keyboardState, startPressed);

            _previousKeyboardState = keyboardState;
            return;
        }

        if (inventoryPressed)
        {
            IsInventoryOpen = true;
            PlayerAttack.Cancel();
            ClampInventorySelection();
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

        bool interactPressed = keyboardState.IsKeyDown(Keys.E) &&
            !_previousKeyboardState.IsKeyDown(Keys.E);

        if (interactPressed && HandleWorldInteraction())
        {
            _previousKeyboardState = keyboardState;
            return;
        }

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

            if (Boss.IsAlive && PlayerAttackArea.Intersects(Boss.Bounds))
            {
                Boss.ReceiveDamage(Player.MeleeDamage);
                Boss.ApplyKnockback(
                    Player.Position,
                    PlayerKnockbackDistance,
                    CurrentDungeon);
            }
        }

        ProcessDefeatedGoblins();
        ProcessBossDefeat();
        Enemies.UpdateCombatAndAi(gameTime, Player, CurrentDungeon);
        Boss.Update(
            gameTime,
            Player,
            CurrentDungeon,
            CurrentDungeon.BossRoom);

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
            IsInventoryOpen = false;
            State = GameState.GameOver;
        }

        Camera.Follow(Player.Position, CurrentDungeon.WorldBounds);
        _previousKeyboardState = keyboardState;
    }

    private void ProcessDefeatedGoblins()
    {
        int defeatedCount = Enemies.RemoveDefeated(
            _defeatedGoblins,
            out int experienceReward);

        if (defeatedCount == 0)
            return;

        Player.GainExperience(experienceReward);

        foreach (Goblin goblin in _defeatedGoblins)
        {
            Loot.TryCreateDrop(
                goblin,
                Player.Level,
                CurrentDungeon);
        }

        KillCount += defeatedCount;
    }

    private void ResetRun()
    {
        DungeonDepth = 1;
        CurrentDungeon = _dungeonGenerator.Generate();
        Player = new PlayerCharacter(CurrentDungeon.StartRoom.Center);
        KillCount = 0;
        IsInventoryOpen = false;
        SelectedInventoryIndex = 0;
        InitializeDungeonState();
    }

    private void CompleteDungeon()
    {
        DungeonDepth++;
        CurrentDungeon = _dungeonGenerator.Generate();
        Player.MoveTo(CurrentDungeon.StartRoom.Center);
        IsInventoryOpen = false;
        ClampInventorySelection();
        InitializeDungeonState();
    }

    private void InitializeDungeonState()
    {
        PlayerAttack = new MeleeAttack();
        Loot.Reset();
        int enemyLevel = DungeonProgression.GetEnemyLevel(
            Player.Level,
            DungeonDepth);
        Enemies.Reset(CurrentDungeon, enemyLevel);
        Chest = new TreasureChest(
            CurrentDungeon.TreasureRoom.Center,
            CurrentDungeon.TreasureRoom.Id);
        Boss = new GoblinWarlord(
            CurrentDungeon.BossRoom.Center,
            CurrentDungeon.BossRoom.Id,
            Player.Level,
            DungeonDepth);
        BossDefeated = false;
        UpdatePlayerAttackArea();
        Camera.Follow(Player.Position, CurrentDungeon.WorldBounds);
    }

    private bool HandleWorldInteraction()
    {
        if (Chest.TryOpen(
            Player.Position,
            Loot,
            Player.Level,
            DungeonDepth,
            CurrentDungeon))
        {
            return false;
        }

        if (IsExitUnlocked && IsPlayerNearExit())
        {
            CompleteDungeon();
            return true;
        }

        Loot.TryCollectNearest(Player.Position, Player.Inventory);
        return false;
    }

    private bool IsPlayerNearExit()
    {
        return Vector2.DistanceSquared(
            Player.Position,
            CurrentDungeon.ExitRoom.Center) <=
            ExitInteractionRadius * ExitInteractionRadius;
    }

    private void ProcessBossDefeat()
    {
        if (BossDefeated || Boss.IsAlive)
            return;

        Player.GainExperience(Boss.ExperienceReward);
        Loot.CreateBossDrop(
            Boss.Position,
            Boss.Level,
            Player.Level,
            CurrentDungeon);
        BossDefeated = true;
        KillCount++;
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

    private void UpdateInventoryInput(
        KeyboardState keyboardState,
        bool equipPressed)
    {
        bool previousPressed =
            (keyboardState.IsKeyDown(Keys.Up) ||
             keyboardState.IsKeyDown(Keys.W)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.Up) ||
              _previousKeyboardState.IsKeyDown(Keys.W));
        bool nextPressed =
            (keyboardState.IsKeyDown(Keys.Down) ||
             keyboardState.IsKeyDown(Keys.S)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.Down) ||
              _previousKeyboardState.IsKeyDown(Keys.S));
        int itemCount = Player.Inventory.Count;

        if (itemCount == 0)
        {
            SelectedInventoryIndex = 0;
            return;
        }

        if (previousPressed)
        {
            SelectedInventoryIndex =
                (SelectedInventoryIndex - 1 + itemCount) % itemCount;
        }
        else if (nextPressed)
        {
            SelectedInventoryIndex =
                (SelectedInventoryIndex + 1) % itemCount;
        }

        if (equipPressed)
        {
            EquipmentItem selectedItem = SelectedInventoryItem;

            if (selectedItem != null)
                Player.EquipItem(selectedItem);

            ClampInventorySelection();
        }
    }

    private void ClampInventorySelection()
    {
        int maximumIndex = Player.Inventory.Count - 1;
        SelectedInventoryIndex = maximumIndex < 0
            ? 0
            : System.Math.Min(SelectedInventoryIndex, maximumIndex);
    }
}
