using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Progression;
using DungeonAscendant.UI;
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
    public const bool DebugCombatHitboxes = false;

    private const float ExitInteractionRadius = 72f;
    private const float WorldTierTransitionDurationSeconds = 1.4f;

    private readonly DungeonGenerator _dungeonGenerator;
    private readonly List<Enemy> _defeatedEnemies = new();
    private readonly HashSet<Enemy> _playerAttackHits = new();
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    private float _worldTierTransitionTimeRemaining;
    private Vector2 _lastSafePlayerPosition;
    private int _trackedPlayerAttackId = -1;
    private int _spawnedPlayerProjectileAttackId = -1;
    private int _debugWeaponIndex = 1;
    private int _debugArmorIndex = 1;
    private bool _bossHitByPlayerAttack;
    private EquipmentItem _pendingFusionItem;
    private EquipmentItem _lastFusionSource;
    private EquipmentItem _lastFusionResult;
    private float _fusionFeedbackTimeRemaining;
    private readonly List<EquipmentItem> _activeInventoryItems = new();
    private int _selectedWeaponIndex;
    private int _selectedEquipmentIndex;

    public PlayerCharacter Player { get; private set; }
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
    public int PlayerHitEffectId { get; private set; }
    public Vector2 PlayerHitEffectPosition { get; private set; }
    public GameState State { get; private set; }
    public bool IsInventoryOpen { get; private set; }
    public bool ShowCombatDebug { get; private set; } = DebugCombatHitboxes;
    public InventoryTab ActiveInventoryTab { get; private set; } =
        InventoryTab.Weapons;
    public IReadOnlyList<EquipmentItem> ActiveInventoryItems =>
        _activeInventoryItems;
    public int SelectedInventoryIndex { get; private set; }
    public EquipmentItem SelectedInventoryItem =>
        SelectedInventoryIndex >= 0 &&
        SelectedInventoryIndex < _activeInventoryItems.Count
            ? _activeInventoryItems[SelectedInventoryIndex]
            : null;
    public string ActiveInventoryEmptyMessage =>
        InventoryTabRules.GetEmptyMessage(ActiveInventoryTab);
    public bool IsFusionConfirmationPending => _pendingFusionItem != null;
    public bool CanFuseSelectedArmor =>
        ActiveInventoryTab == InventoryTab.Equipment &&
        Player.Inventory.CanFuseArmor(SelectedInventoryItem);
    public int SelectedFusionMaterialCount =>
        Player.Inventory.CountMatchingFusionMaterials(SelectedInventoryItem);
    public EquipmentItem PendingFusionItem => _pendingFusionItem;
    public EquipmentItem LastFusionSource => _lastFusionSource;
    public EquipmentItem LastFusionResult => _lastFusionResult;
    public bool IsFusionFeedbackVisible => _fusionFeedbackTimeRemaining > 0f;

    public GameSession(Rectangle viewportBounds, int? randomSeed = null)
    {
        WeaponArchetypeValidation.ValidateOrThrow();
        ArmorProgressionValidation.ValidateOrThrow();
        InventoryGridNavigation.ValidateOrThrow();
        InventoryTabRules.ValidateOrThrow();
        _dungeonGenerator = new DungeonGenerator(randomSeed);
        Enemies = new EnemyManager(randomSeed);
        Projectiles = new ProjectileManager();
        RootHazards = new RootHazardManager();
        Loot = new LootManager(randomSeed);
        Camera = new Camera2D(viewportBounds.Width, viewportBounds.Height);
        Player = new PlayerCharacter(viewportBounds.Center.ToVector2());
        RefreshInventoryView();
        Region = RegionDefinition.WildForest;
        UpdatePlayerAttackArea();
        State = GameState.Start;
    }

    public void Update(
        GameTime gameTime,
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        bool startPressed = keyboardState.IsKeyDown(Keys.Enter) &&
            !_previousKeyboardState.IsKeyDown(Keys.Enter);
        bool pausePressed = keyboardState.IsKeyDown(Keys.Escape) &&
            !_previousKeyboardState.IsKeyDown(Keys.Escape);
        bool restartPressed = keyboardState.IsKeyDown(Keys.R) &&
            !_previousKeyboardState.IsKeyDown(Keys.R);
        bool inventoryPressed = keyboardState.IsKeyDown(Keys.I) &&
            !_previousKeyboardState.IsKeyDown(Keys.I);
        bool debugPressed = WasKeyPressed(keyboardState, Keys.F3);
        bool debugWeaponPressed = WasKeyPressed(keyboardState, Keys.F5);
        bool debugArmorPressed = WasKeyPressed(keyboardState, Keys.F6);
        bool fusePressed = WasKeyPressed(keyboardState, Keys.F);
        bool previousTabPressed = WasKeyPressed(keyboardState, Keys.Q);
        bool nextTabPressed = WasKeyPressed(keyboardState, Keys.E);
        _fusionFeedbackTimeRemaining = System.MathF.Max(
            0f,
            _fusionFeedbackTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (debugPressed)
            ShowCombatDebug = !ShowCombatDebug;

        if (State == GameState.Start)
        {
            if (startPressed)
            {
                ResetRun();
                State = GameState.Playing;
            }

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.Paused)
        {
            if (pausePressed)
                State = GameState.Playing;

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.GameOver)
        {
            if (restartPressed)
            {
                ResetRun();
                State = GameState.Playing;
            }

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (IsInventoryOpen)
        {
            if (inventoryPressed)
            {
                IsInventoryOpen = false;
                _pendingFusionItem = null;
            }
            else if (pausePressed)
            {
                if (_pendingFusionItem != null)
                    _pendingFusionItem = null;
                else
                    IsInventoryOpen = false;
            }
            else
                UpdateInventoryInput(
                    keyboardState,
                    startPressed,
                    fusePressed,
                    previousTabPressed,
                    nextTabPressed);

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (inventoryPressed)
        {
            IsInventoryOpen = true;
            Player.Combat.CancelActions();
            RefreshInventoryView();
            RestoreActiveTabSelection();
            ClampInventorySelection();
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (pausePressed)
        {
            State = GameState.Paused;
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (!Player.IsAlive)
        {
            Projectiles.Clear();
            RootHazards.Clear();
            Player.ClearTemporaryStatus();
            State = GameState.GameOver;
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (debugWeaponPressed)
        {
            _debugWeaponIndex =
                (_debugWeaponIndex + 1) % EquipmentCatalog.WeaponSampleCount;
            Player.EquipDebugItem(
                EquipmentCatalog.CreateWeaponSample(_debugWeaponIndex));
        }

        if (debugArmorPressed)
        {
            _debugArmorIndex =
                (_debugArmorIndex + 1) % EquipmentCatalog.ArmorSampleCount;
            Player.EquipDebugItem(
                EquipmentCatalog.CreateArmorSample(_debugArmorIndex));
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _worldTierTransitionTimeRemaining = System.MathF.Max(
            0f,
            _worldTierTransitionTimeRemaining - elapsedSeconds);
        Player.UpdateTimers(gameTime);
        Player.UpdateCombat(
            gameTime,
            CreateCombatInput(keyboardState, mouseState));
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
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        UpdatePlayerAttackArea();
        ProcessPlayerAttack();

        ProcessDefeatedEnemies();
        ProcessBossDefeat();
        Enemies.UpdateCombatAndAi(
            gameTime,
            Player,
            CurrentDungeon,
            Projectiles,
            RootHazards);
        Projectiles.Update(gameTime, Player, CurrentDungeon);
        ProcessPlayerProjectileHits();
        RootHazards.Update(gameTime, Player);
        Boss.Update(
            gameTime,
            Player,
            CurrentDungeon,
            CurrentDungeon.BossRoom,
            RootHazards);

        if (!Player.IsAlive)
        {
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
        StoreInputStates(keyboardState, mouseState);
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
                    CurrentRegion,
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
        ShowCombatDebug = DebugCombatHitboxes;
        SelectedInventoryIndex = 0;
        ActiveInventoryTab = InventoryTab.Weapons;
        _selectedWeaponIndex = 0;
        _selectedEquipmentIndex = 0;
        RefreshInventoryView();
        _debugWeaponIndex = 1;
        _debugArmorIndex = 1;
        ClearFusionState();
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
        _pendingFusionItem = null;
        RefreshInventoryView();
        ClampInventorySelection();
        InitializeDungeonState();
    }

    private void InitializeDungeonState()
    {
        Player.ClearTemporaryStatus();
        _playerAttackHits.Clear();
        _trackedPlayerAttackId = -1;
        _spawnedPlayerProjectileAttackId = -1;
        _bossHitByPlayerAttack = false;
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
            CurrentRegion,
            isFirstDungeonOfRun: DungeonDepth == 1);
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
            CurrentRegion,
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
        Loot.CreateBossWeaponDrop(
            Boss.Position,
            Region.BossWeaponReward,
            CurrentDungeon);
        BossDefeated = true;
        KillCount++;
    }

    private void UpdatePlayerAttackArea()
    {
        AttackDefinition attack = Player.Combat.CurrentAttack ??
            Player.Combat.MoveSet.GetLight(0);
        PlayerAttackArea = MeleeHitArea.Create(
            Player.Position,
            Player.Size,
            Player.Facing,
            Player.Combat.CalculateRange(attack),
            attack.Thickness,
            attack.HitboxShape);
    }

    private void RegisterPlayerHitEffect(Rectangle targetBounds)
    {
        RegisterPlayerHitEffect(PlayerAttackArea, targetBounds);
    }

    private void RegisterPlayerHitEffect(
        Rectangle hitArea,
        Rectangle targetBounds)
    {
        Rectangle overlap = Rectangle.Intersect(
            hitArea,
            targetBounds);
        PlayerHitEffectPosition = overlap.Width > 0 && overlap.Height > 0
            ? overlap.Center.ToVector2()
            : targetBounds.Center.ToVector2();
        PlayerHitEffectId++;
    }

    private void ProcessPlayerAttack()
    {
        if (_trackedPlayerAttackId != Player.Combat.AttackId)
        {
            _trackedPlayerAttackId = Player.Combat.AttackId;
            _playerAttackHits.Clear();
            _bossHitByPlayerAttack = false;
        }

        if (!Player.Combat.IsAttackActive ||
            Player.Combat.CurrentAttack == null)
        {
            return;
        }

        AttackDefinition attack = Player.Combat.CurrentAttack;

        if (attack.Delivery != AttackDelivery.Melee)
        {
            SpawnPlayerProjectile(attack);
            return;
        }

        int damage = Player.Combat.CalculateDamage(Player.MeleeDamage);
        float poiseDamage = Player.Combat.CalculatePoiseDamage();
        float knockback = Player.Combat.CalculateKnockback();

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted ||
                _playerAttackHits.Contains(enemy) ||
                !PlayerAttackArea.Intersects(enemy.MeleeTargetBounds))
            {
                continue;
            }

            _playerAttackHits.Add(enemy);
            RegisterPlayerHitEffect(enemy.MeleeTargetBounds);
            enemy.ReceiveDamage(damage, poiseDamage);
            enemy.ApplyKnockback(
                Player.Position,
                knockback,
                CurrentDungeon);
        }

        if (!_bossHitByPlayerAttack && Boss.IsAlive &&
            PlayerAttackArea.Intersects(Boss.Bounds))
        {
            _bossHitByPlayerAttack = true;
            RegisterPlayerHitEffect(Boss.Bounds);
            Boss.ReceiveDamage(damage, poiseDamage);
            Boss.ApplyKnockback(
                Player.Position,
                knockback,
                CurrentDungeon);
        }
    }

    private void SpawnPlayerProjectile(AttackDefinition attack)
    {
        if (_spawnedPlayerProjectileAttackId == Player.Combat.AttackId ||
            attack.Projectile == null)
            return;

        _spawnedPlayerProjectileAttackId = Player.Combat.AttackId;
        float direction = Player.Facing == DungeonAscendant.Player.FacingDirection.Left
            ? -1f
            : 1f;
        Vector2 spawnPosition = Player.Position + new Vector2(direction * 30f, -10f);
        int roomId = CurrentDungeon.FindRoomContaining(Player.Position)?.Id ?? -1;
        Projectiles.SpawnPlayerProjectile(
            attack.Projectile,
            spawnPosition,
            new Vector2(direction, 0f),
            Player.Combat.ProjectileSpeedMultiplier,
            Player.Combat.CalculateDamage(Player.MeleeDamage),
            Player.Combat.CalculatePoiseDamage(),
            Player.Combat.CalculateKnockback(),
            roomId);
    }

    private void ProcessPlayerProjectileHits()
    {
        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (!projectile.IsPlayerOwned || projectile.LifetimeRemaining <= 0f)
                continue;

            bool consumed = false;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted ||
                    !projectile.Bounds.Intersects(enemy.MeleeTargetBounds))
                    continue;

                RegisterPlayerHitEffect(projectile.Bounds, enemy.MeleeTargetBounds);
                enemy.ReceiveDamage(projectile.Damage, projectile.PoiseDamage);
                enemy.ApplyKnockback(
                    projectile.SourcePosition,
                    projectile.Knockback,
                    CurrentDungeon);
                consumed = true;
                break;
            }

            if (!consumed && Boss.IsAlive &&
                projectile.Bounds.Intersects(Boss.Bounds))
            {
                RegisterPlayerHitEffect(projectile.Bounds, Boss.Bounds);
                Boss.ReceiveDamage(projectile.Damage, projectile.PoiseDamage);
                Boss.ApplyKnockback(
                    projectile.SourcePosition,
                    projectile.Knockback,
                    CurrentDungeon);
                consumed = true;
            }

            if (consumed)
                projectile.LifetimeRemaining = 0f;
        }
    }

    private CombatInput CreateCombatInput(
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        bool lightPressed = WasKeyPressed(keyboardState, Keys.J) ||
            (mouseState.LeftButton == ButtonState.Pressed &&
             _previousMouseState.LeftButton == ButtonState.Released);
        bool heavyHeld = keyboardState.IsKeyDown(Keys.K) ||
            mouseState.RightButton == ButtonState.Pressed;
        bool heavyWasHeld = _previousKeyboardState.IsKeyDown(Keys.K) ||
            _previousMouseState.RightButton == ButtonState.Pressed;
        bool heavyPressed = heavyHeld && !heavyWasHeld;
        bool heavyReleased = !heavyHeld && heavyWasHeld;
        bool dodgePressed = WasKeyPressed(keyboardState, Keys.LeftShift) ||
            WasKeyPressed(keyboardState, Keys.RightShift);
        bool blockHeld = keyboardState.IsKeyDown(Keys.LeftControl);
        float horizontalDirection = 0f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            horizontalDirection -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            horizontalDirection += 1f;

        return new CombatInput(
            lightPressed,
            heavyPressed,
            heavyHeld,
            heavyReleased,
            dodgePressed,
            blockHeld,
            horizontalDirection);
    }

    private bool WasKeyPressed(KeyboardState keyboardState, Keys key)
    {
        return keyboardState.IsKeyDown(key) &&
            !_previousKeyboardState.IsKeyDown(key);
    }

    private void StoreInputStates(
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void UpdateInventoryInput(
        KeyboardState keyboardState,
        bool equipPressed,
        bool fusePressed,
        bool previousTabPressed,
        bool nextTabPressed)
    {
        if (previousTabPressed)
            SwitchInventoryTab(previous: true);
        else if (nextTabPressed)
            SwitchInventoryTab(previous: false);

        bool upPressed = WasEitherKeyPressed(
            keyboardState,
            Keys.W,
            Keys.Up);
        bool downPressed = WasEitherKeyPressed(
            keyboardState,
            Keys.S,
            Keys.Down);
        bool leftPressed = WasEitherKeyPressed(
            keyboardState,
            Keys.A,
            Keys.Left);
        bool rightPressed = WasEitherKeyPressed(
            keyboardState,
            Keys.D,
            Keys.Right);
        int itemCount = _activeInventoryItems.Count;

        if (itemCount == 0)
        {
            SelectedInventoryIndex = 0;
            _pendingFusionItem = null;
            return;
        }

        int previousIndex = SelectedInventoryIndex;

        if (upPressed)
            MoveInventorySelection(rowDelta: -1, columnDelta: 0);
        else if (downPressed)
            MoveInventorySelection(rowDelta: 1, columnDelta: 0);
        else if (leftPressed)
            MoveInventorySelection(rowDelta: 0, columnDelta: -1);
        else if (rightPressed)
            MoveInventorySelection(rowDelta: 0, columnDelta: 1);

        if (SelectedInventoryIndex != previousIndex)
            _pendingFusionItem = null;

        if (equipPressed && _pendingFusionItem != null)
        {
            EquipmentItem source = _pendingFusionItem;

            if (Player.Inventory.TryFuseArmor(source, out EquipmentItem result))
            {
                _lastFusionSource = source;
                _lastFusionResult = result;
                _fusionFeedbackTimeRemaining = 2.5f;
                RefreshInventoryView();
                SelectedInventoryIndex = _activeInventoryItems.IndexOf(result);
            }

            _pendingFusionItem = null;
            ClampInventorySelection();
        }
        else if (fusePressed && ActiveInventoryTab == InventoryTab.Equipment)
        {
            EquipmentItem selectedItem = SelectedInventoryItem;

            if (Player.Inventory.CanFuseArmor(selectedItem))
                _pendingFusionItem = selectedItem;
        }
        else if (equipPressed)
        {
            EquipmentItem selectedItem = SelectedInventoryItem;

            if (selectedItem != null && Player.EquipItem(selectedItem))
                RefreshInventoryView();

            ClampInventorySelection();
        }
    }

    private bool WasEitherKeyPressed(
        KeyboardState keyboardState,
        Keys first,
        Keys second)
    {
        return WasKeyPressed(keyboardState, first) ||
            WasKeyPressed(keyboardState, second);
    }

    private void MoveInventorySelection(int rowDelta, int columnDelta)
    {
        SelectedInventoryIndex = InventoryGridNavigation.Move(
            SelectedInventoryIndex,
            _activeInventoryItems.Count,
            rowDelta,
            columnDelta);
        RememberActiveTabSelection();
    }

    private void SwitchInventoryTab(bool previous)
    {
        RememberActiveTabSelection();
        InventoryTab[] tabs = System.Enum.GetValues<InventoryTab>();
        int direction = previous ? -1 : 1;
        int nextIndex = ((int)ActiveInventoryTab + direction + tabs.Length) %
            tabs.Length;
        ActiveInventoryTab = tabs[nextIndex];
        _pendingFusionItem = null;
        RefreshInventoryView();
        RestoreActiveTabSelection();
        ClampInventorySelection();
    }

    private void RefreshInventoryView()
    {
        InventoryTabRules.PopulateView(
            Player.Inventory.Items,
            ActiveInventoryTab,
            _activeInventoryItems);
    }

    private void RememberActiveTabSelection()
    {
        if (ActiveInventoryTab == InventoryTab.Weapons)
            _selectedWeaponIndex = SelectedInventoryIndex;
        else
            _selectedEquipmentIndex = SelectedInventoryIndex;
    }

    private void RestoreActiveTabSelection()
    {
        SelectedInventoryIndex = ActiveInventoryTab == InventoryTab.Weapons
            ? _selectedWeaponIndex
            : _selectedEquipmentIndex;
    }

    private void ClearFusionState()
    {
        _pendingFusionItem = null;
        _lastFusionSource = null;
        _lastFusionResult = null;
        _fusionFeedbackTimeRemaining = 0f;
    }

    private void ClampInventorySelection()
    {
        int maximumIndex = _activeInventoryItems.Count - 1;
        SelectedInventoryIndex = maximumIndex < 0
            ? 0
            : System.Math.Clamp(SelectedInventoryIndex, 0, maximumIndex);
        RememberActiveTabSelection();
    }
}
