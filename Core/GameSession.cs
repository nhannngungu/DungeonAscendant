using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Progression;
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
    private const float WorldTierTransitionDurationSeconds = 1.4f;

    private readonly DungeonGenerator _dungeonGenerator;
    private readonly List<Enemy> _defeatedEnemies = new();
    private KeyboardState _previousKeyboardState;
    private float _worldTierTransitionTimeRemaining;
    private Vector2 _lastSafePlayerPosition;

    public PlayerCharacter Player { get; private set; }
    public MeleeAttack PlayerAttack { get; private set; }
    public EnemyManager Enemies { get; }
    public ProjectileManager Projectiles { get; }
    public RootHazardManager RootHazards { get; }
    public LootManager Loot { get; }
    public TreasureChest Chest { get; private set; }
    public AncientTreant Boss { get; private set; }
    public DungeonMap CurrentDungeon { get; private set; }
    public Camera2D Camera { get; }
    public int KillCount { get; private set; }
    public int DungeonDepth { get; private set; }
    public int WorldTier { get; private set; }
    public RegionDefinition Region { get; private set; }
    public RegionType CurrentRegion => Region.Type;
    public bool BossDefeated { get; private set; }
    public bool IsExitUnlocked => BossDefeated;
    public Vector2 ExitPosition => SideScrollingCollision.PlaceOnGround(
        CurrentDungeon.ExitRoom.Bounds.Center.X,
        new Vector2(40f, 56f),
        CurrentDungeon.ExitRoom);
    public float WorldTierTransitionProgress =>
        _worldTierTransitionTimeRemaining /
        WorldTierTransitionDurationSeconds;
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
        Projectiles = new ProjectileManager();
        RootHazards = new RootHazardManager();
        Loot = new LootManager(randomSeed);
        Camera = new Camera2D(viewportBounds.Width, viewportBounds.Height);
        Player = new PlayerCharacter(viewportBounds.Center.ToVector2());
        PlayerAttack = new MeleeAttack();
        Region = RegionDefinition.WildForest;
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
            Projectiles.Clear();
            RootHazards.Clear();
            Player.ClearTemporaryStatus();
            State = GameState.GameOver;
            _previousKeyboardState = keyboardState;
            return;
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _worldTierTransitionTimeRemaining = System.MathF.Max(
            0f,
            _worldTierTransitionTimeRemaining - elapsedSeconds);
        Player.UpdateTimers(gameTime);
        bool jumpPressed = keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);
        Player.UpdateSideScrollingMovement(
            gameTime,
            keyboardState,
            jumpPressed,
            CurrentDungeon);

        RecoverPlayerFromFallIfNeeded();

        if (Player.IsGrounded &&
            CurrentDungeon.WorldBounds.Contains(Player.Bounds))
        {
            _lastSafePlayerPosition = Player.Position;
        }

        bool interactPressed = keyboardState.IsKeyDown(Keys.E) &&
            !_previousKeyboardState.IsKeyDown(Keys.E);

        if (interactPressed && HandleWorldInteraction())
        {
            _previousKeyboardState = keyboardState;
            return;
        }

        PlayerAttack.Update(gameTime);
        UpdatePlayerAttackArea();

        bool attackPressed = (keyboardState.IsKeyDown(Keys.J) ||
            keyboardState.IsKeyDown(Keys.LeftControl)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.J) ||
              _previousKeyboardState.IsKeyDown(Keys.LeftControl));

        if (attackPressed && PlayerAttack.TryStart())
        {
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted ||
                    !PlayerAttackArea.Intersects(enemy.MeleeTargetBounds))
                {
                    continue;
                }

                enemy.ReceiveDamage(Player.MeleeDamage);
                enemy.ApplyKnockback(
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

        ProcessDefeatedEnemies();
        ProcessBossDefeat();
        Enemies.UpdateCombatAndAi(
            gameTime,
            Player,
            CurrentDungeon,
            Projectiles,
            RootHazards);
        Projectiles.Update(gameTime, Player, CurrentDungeon);
        RootHazards.Update(gameTime, Player);
        Boss.Update(
            gameTime,
            Player,
            CurrentDungeon,
            CurrentDungeon.BossRoom,
            RootHazards);

        if (!Player.IsAlive)
        {
            PlayerAttack.Cancel();
            Projectiles.Clear();
            RootHazards.Clear();
            Player.ClearTemporaryStatus();
            IsInventoryOpen = false;
            State = GameState.GameOver;
        }

        Camera.Follow(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds,
            elapsedSeconds);
        _previousKeyboardState = keyboardState;
    }

    private void ProcessDefeatedEnemies()
    {
        int defeatedCount = Enemies.RemoveDefeated(
            _defeatedEnemies,
            out int experienceReward);

        if (defeatedCount == 0)
            return;

        Player.GainExperience(experienceReward);

        foreach (Enemy enemy in _defeatedEnemies)
        {
            RootHazards.RemoveOwnedBy(enemy);

            if (enemy.CanDropLoot)
            {
                Loot.TryCreateDrop(
                    enemy,
                    Player.Level,
                    DungeonDepth,
                    WorldTier,
                    CurrentDungeon);
            }
        }

        KillCount += defeatedCount;
    }

    private void ResetRun()
    {
        DungeonDepth = 1;
        WorldTier = 1;
        Region = RegionDefinition.WildForest;
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = _dungeonGenerator.Generate();
        Player = new PlayerCharacter(GetRoomEntranceSpawn(CurrentDungeon.StartRoom));
        _lastSafePlayerPosition = Player.Position;
        KillCount = 0;
        IsInventoryOpen = false;
        SelectedInventoryIndex = 0;
        InitializeDungeonState();
    }

    private void CompleteDungeon()
    {
        int previousWorldTier = WorldTier;
        DungeonDepth++;
        WorldTier = WorldProgression.GetWorldTier(DungeonDepth);

        if (WorldTier > previousWorldTier)
        {
            _worldTierTransitionTimeRemaining =
                WorldTierTransitionDurationSeconds;
        }

        CurrentDungeon = _dungeonGenerator.Generate();
        Player.MoveTo(GetRoomEntranceSpawn(CurrentDungeon.StartRoom));
        _lastSafePlayerPosition = Player.Position;
        IsInventoryOpen = false;
        ClampInventorySelection();
        InitializeDungeonState();
    }

    private void InitializeDungeonState()
    {
        PlayerAttack = new MeleeAttack();
        Player.ClearTemporaryStatus();
        Projectiles.Clear();
        RootHazards.Clear();
        Loot.Reset();
        int enemyLevel = WorldProgression.GetEnemyLevel(
            Player.Level,
            DungeonDepth,
            WorldTier);
        Enemies.Reset(
            CurrentDungeon,
            enemyLevel,
            WorldTier,
            CurrentRegion);
        Chest = new TreasureChest(
            SideScrollingCollision.PlaceOnGround(
                CurrentDungeon.TreasureRoom.Bounds.Center.X,
                new Vector2(56f, 40f),
                CurrentDungeon.TreasureRoom),
            CurrentDungeon.TreasureRoom.Id);
        Boss = CreateRegionBoss();
        BossDefeated = false;
        UpdatePlayerAttackArea();
        Camera.Snap(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds);
    }

    private AncientTreant CreateRegionBoss()
    {
        if (Region.BossType != RegionBossType.AncientTreant)
            throw new System.NotSupportedException("Region Boss is not implemented.");

        return new AncientTreant(
            SideScrollingCollision.PlaceOnGround(
                CurrentDungeon.BossRoom.Bounds.Center.X,
                new Vector2(110f, 126f),
                CurrentDungeon.BossRoom),
            CurrentDungeon.BossRoom.Id,
            Player.Level,
            DungeonDepth,
            WorldTier);
    }

    private bool HandleWorldInteraction()
    {
        if (Chest.TryOpen(
            Player.Position,
            Loot,
            Player.Level,
            DungeonDepth,
            WorldTier,
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
            ExitPosition) <=
            ExitInteractionRadius * ExitInteractionRadius;
    }

    private Vector2 GetRoomEntranceSpawn(DungeonRoom room)
    {
        return SideScrollingCollision.PlaceOnGround(
            room.Bounds.Left + 120f,
            Player?.Size ?? new Vector2(40f, 56f),
            room);
    }

    private void RecoverPlayerFromFallIfNeeded()
    {
        Rectangle world = CurrentDungeon.WorldBounds;

        if (Player.Position.Y <= world.Bottom + Player.Size.Y)
            return;

        Player.ReceiveDamage(System.Math.Max(1, Player.MaxHealth / 5));
        Player.MoveTo(_lastSafePlayerPosition);
    }

    private void ProcessBossDefeat()
    {
        if (BossDefeated || Boss.IsAlive)
            return;

        Player.GainExperience(Boss.ExperienceReward);
        RootHazards.Clear();
        Loot.CreateBossDrop(
            Boss.Position,
            Boss.Level,
            Player.Level,
            DungeonDepth,
            WorldTier,
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
