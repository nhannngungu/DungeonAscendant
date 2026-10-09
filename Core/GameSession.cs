using System;
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
#if DEBUG
    public const bool DeveloperModeEnabled = true;
#else
    public const bool DeveloperModeEnabled = false;
#endif
    public const bool DebugCombatHitboxes = false;
    public static bool DebugWildForestShowcase { get; set; } = false;

    private const float ExitInteractionRadius = 72f;
    private const float WorldTierTransitionDurationSeconds = 1.4f;
    public const float MapEntryDropOffset =
        SideScrollingCollision.DefaultEntryDropOffset;
    public const float WildForestShowcasePlayerStartDistance = 400f;
    public const float WildForestShowcaseBloodBatHeight = 180f;

    private static readonly EnemyType[] WildForestShowcaseEnemyTypes =
    {
        EnemyType.Goblin,
        EnemyType.GoblinHunter,
        EnemyType.DireWolf,
        EnemyType.GiantSpider,
        EnemyType.BloodBat,
        EnemyType.ThornCrawler,
        EnemyType.CorruptedTreant,
        EnemyType.GoblinChief,
        EnemyType.MotherSpider
    };

    private static readonly string[] WildForestShowcaseEnemyNames =
    {
        "GOBLIN",
        "GOBLIN HUNTER",
        "DIRE WOLF",
        "GIANT SPIDER",
        "BLOOD BAT",
        "THORN CRAWLER",
        "CORRUPTED TREANT",
        "GOBLIN CHIEF",
        "MOTHER SPIDER",
        "ANCIENT TREANT"
    };

    private static readonly float[] WildForestShowcaseOffsets =
    {
        400f,
        1200f,
        2000f,
        2850f,
        3700f,
        4550f,
        5550f,
        6550f,
        7600f
    };

    private const float WildForestShowcaseBossOffset = 8900f;

    private readonly DungeonGenerator _dungeonGenerator;
    private readonly CatacombMapGenerator _catacombGenerator;
    private readonly List<Enemy> _defeatedEnemies = new();
    private readonly List<RottenCorpseBurst> _rottenCorpseBursts = new();
    private readonly HashSet<Enemy> _playerAttackHits = new();
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    private float _worldTierTransitionTimeRemaining;
    private Vector2 _lastSafePlayerPosition;
    private bool _mapEntryFallActive;
    private int _trackedPlayerAttackId = -1;
    private int _spawnedPlayerProjectileAttackId = -1;
    private int _duelistMotionAttackId = -1;
    private int _rangerHopAttackId = -1;
    private int _predatorFanTechniqueUseId = -1;
    private readonly Dictionary<Enemy, int> _predatorFanHits = new();
    private int _predatorBossFanHits;
    private readonly List<ProjectileImpact> _pendingArrowRain = new();
    private int _raiderDisplacementTechniqueUseId = -1;
    private readonly HashSet<Enemy> _raiderWallImpactedEnemies = new();
    private int _spellbladeWorldEffectAttackId = -1;
    private int _spellbladeTechniqueUseId = -1;
    private object _runicStrikeChainTarget;
    private int _runicStrikeChainHits;
    private int _arcaneDominionRuneSnapshot;
    private int _breakerWorldEffectAttackId = -1;
    private int _breakerImpactFeedbackAttackId = -1;
    private readonly List<BreakerShockwave> _breakerShockwaves = new();
    private readonly List<BreakerImpactVisual> _breakerImpactVisuals = new();
    private Rectangle _recentDuelistDangerZone;
    private float _recentDuelistDangerTimeRemaining;
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
    private DungeonRoom _sequentialTestRoom;
    private Enemy _sequentialTestEnemy;
    private int _sequentialTestIndex;
    private bool _sequentialBossActive;
    private bool _sequentialTestComplete;
    private readonly List<Enemy> _wildForestShowcaseEnemies = new();

    public PlayerCharacter Player { get; private set; }
    public EnemyManager Enemies { get; }
    public WildForestEncounterDirector WildForestEncounters { get; }
    public EnemyIntroductionManager EnemyIntroductions { get; }
    public CurseSystem Curse { get; }
    public TombInteractionManager Tombs { get; }
    public DeveloperPanel DeveloperPanel { get; } = new();
    public IReadOnlyList<RottenCorpseBurst> RottenCorpseBursts => _rottenCorpseBursts;
    public bool Map01Cleared { get; private set; }
    public bool Map02Cleared { get; private set; }
    public ProjectileManager Projectiles { get; }
    public GroundRuneManager GroundRunes { get; }
    public IReadOnlyList<BreakerShockwave> BreakerShockwaves =>
        _breakerShockwaves;
    public IReadOnlyList<BreakerImpactVisual> BreakerImpactVisuals =>
        _breakerImpactVisuals;
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
    public Vector2 ExitPosition => CurrentDungeon.AuthoredExitPosition ??
        SideScrollingCollision.PlaceOnGround(
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
    public bool ShowMapDebug { get; private set; }
    public bool DebugGodMode => Player.DebugGodMode;
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
    public bool IsWildForestShowcaseMode =>
        DebugWildForestShowcase &&
        CurrentRegion == RegionType.WildForest &&
        DungeonDepth == 1;
    public bool IsSequentialEnemyTestMode => IsWildForestShowcaseMode;
    public bool IsMapEntryFallActive => _mapEntryFallActive;
    public string CurrentMapName => CurrentDungeon?.IsAncientCatacombs == true
        ? "ANCIENT CATACOMBS"
        : "CORRUPTED WILDERNESS";
    public string CurrentZoneName => GetCurrentZoneName();
    public int ActiveEnemyCount => CountActiveEnemies();
    public bool ShouldRenderBoss => CurrentDungeon?.IsAncientCatacombs != true;
    public IReadOnlyList<Enemy> WildForestShowcaseEnemies =>
        _wildForestShowcaseEnemies;
    public bool IsSequentialEnemyTestComplete => _sequentialTestComplete;
    public Enemy SequentialTestEnemy => _sequentialTestEnemy;
    public int SequentialTestIndex => _sequentialTestIndex;
    public string SequentialTestEnemyName => _sequentialTestComplete
        ? "WILD FOREST TEST COMPLETE"
        : WildForestShowcaseEnemyNames[
            System.Math.Clamp(
                _sequentialTestIndex,
                0,
                WildForestShowcaseEnemyNames.Length - 1)];

    public static string GetWildForestShowcaseLabel(EnemyType type)
    {
        for (int index = 0; index < WildForestShowcaseEnemyTypes.Length; index++)
        {
            if (WildForestShowcaseEnemyTypes[index] == type)
                return WildForestShowcaseEnemyNames[index];
        }

        return type == EnemyType.Spiderling ? null : type.ToString().ToUpperInvariant();
    }

    public GameSession(Rectangle viewportBounds, int? randomSeed = null)
    {
        WeaponArchetypeValidation.ValidateOrThrow();
        EquipmentRebuildValidation.ValidateOrThrow();
        ArmorProgressionValidation.ValidateOrThrow();
        InventoryGridNavigation.ValidateOrThrow();
        InventoryTabRules.ValidateOrThrow();
        WildForestMapValidation.ValidateOrThrow();
        _dungeonGenerator = new DungeonGenerator(randomSeed);
        _catacombGenerator = new CatacombMapGenerator();
        Enemies = new EnemyManager(randomSeed);
        WildForestEncounters = new WildForestEncounterDirector();
        EnemyIntroductions = new EnemyIntroductionManager();
        Curse = new CurseSystem();
        Tombs = new TombInteractionManager();
        Projectiles = new ProjectileManager();
        GroundRunes = new GroundRuneManager();
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
        if (DeveloperModeEnabled && DeveloperPanel.Update(this, keyboardState))
        {
            StoreInputStates(keyboardState, mouseState);
            return;
        }

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
        bool restartWildForestShowcasePressed = WasKeyPressed(
            keyboardState,
            Keys.F6);
        bool debugArmorPressed = WasKeyPressed(keyboardState, Keys.F7);
        bool environmentDebugPressed = WasKeyPressed(keyboardState, Keys.F8);
        bool godModePressed = WasKeyPressed(keyboardState, Keys.F9);
        bool fusePressed = WasKeyPressed(keyboardState, Keys.F);
        bool previousTabPressed = WasKeyPressed(keyboardState, Keys.Q);
        bool nextTabPressed = WasKeyPressed(keyboardState, Keys.E);
        _fusionFeedbackTimeRemaining = System.MathF.Max(
            0f,
            _fusionFeedbackTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (debugPressed)
            ShowCombatDebug = !ShowCombatDebug;

        if (environmentDebugPressed)
            ShowMapDebug = !ShowMapDebug;

        if (godModePressed)
            Player.DebugGodMode = !Player.DebugGodMode;

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
            ClearBreakerEffects();
            RootHazards.Clear();
            Player.ClearTemporaryStatus();
            State = GameState.GameOver;
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (IsWildForestShowcaseMode && restartWildForestShowcasePressed)
            RestartWildForestShowcase();

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
        UpdateRecentDuelistDangerZone(elapsedSeconds);
        GroundRunes.Update(elapsedSeconds);
        UpdateSpellbladeRuneNetwork();
        UpdateBreakerEffects(elapsedSeconds);
        _worldTierTransitionTimeRemaining = System.MathF.Max(
            0f,
            _worldTierTransitionTimeRemaining - elapsedSeconds);
        Player.UpdateTimers(gameTime);
        Player.Combat.SetSpellbladeRuneCount(GroundRunes.CountWithin(
            Player.Position,
            SpellbladeTuning.ConvergenceMaximumRange));
        bool entryFallWasActive = _mapEntryFallActive;
        Player.UpdateCombat(
            gameTime,
            entryFallWasActive
                ? default
                : CreateCombatInput(keyboardState, mouseState));
        UpdateDuelistTechniqueMovement();
        UpdateRangerTechniqueMovement();
        UpdateRangerFocusPressure(elapsedSeconds);
        if (Player.Combat.IsHitstopActive)
        {
            StoreInputStates(keyboardState, mouseState);
            return;
        }
        KeyboardState movementInput = entryFallWasActive
            ? new KeyboardState()
            : keyboardState;
        bool jumpPressed = !entryFallWasActive &&
            keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);
        Player.UpdateSideScrollingMovement(
            gameTime,
            movementInput,
            jumpPressed,
            CurrentDungeon);

        if (_mapEntryFallActive && Player.IsGrounded)
            _mapEntryFallActive = false;

        RecoverPlayerFromFallIfNeeded();

        if (Player.IsGrounded &&
            CurrentDungeon.WorldBounds.Contains(Player.Bounds))
        {
            _lastSafePlayerPosition = Player.Position;
        }

        if (entryFallWasActive)
        {
            FollowPlayerWithCamera(elapsedSeconds);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        bool interactPressed = keyboardState.IsKeyDown(Keys.E) &&
            !_previousKeyboardState.IsKeyDown(Keys.E);

        if (interactPressed && HandleWorldInteraction())
        {
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        int activeEnemyLevel = WorldProgression.GetEnemyLevel(
            Player.Level, DungeonDepth, WorldTier);
        WildForestEncounters.Update(
            gameTime,
            Player.Position,
            CurrentDungeon,
            Enemies,
            activeEnemyLevel,
            WorldTier);
        Tombs.Update(gameTime,Player,CurrentDungeon,Enemies,activeEnemyLevel,WorldTier);
        if (Tombs.LootOpenedPosition.HasValue)
            Loot.CreateTreasureChestDrops(Tombs.LootOpenedPosition.Value,
                Player.Level,DungeonDepth,WorldTier,CurrentRegion,CurrentDungeon);
        Curse.Update(gameTime,Player,CurrentDungeon);
        UpdateRottenCorpseBursts(gameTime);
        if (CurrentDungeon.IsAncientCatacombs)
        {
            float curseDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                float distance = Vector2.Distance(enemy.Position, Player.Position);
                if (enemy.Type == EnemyType.GraveBat && distance < 260f)
                    Player.Combat.ExternalStaminaRegenMultiplier *= .72f;
                if (enemy.Type is EnemyType.Wraith or EnemyType.SoulCollector && distance < 135f)
                    Curse.Add(5f * curseDelta);
            }
        }
        if (CurrentDungeon.IsAuthoredWildForest || CurrentDungeon.IsAncientCatacombs)
        {
            EnemyIntroductions.Update(
                gameTime,
                Player.Position,
                Enemies.Enemies,
                Boss,
                allowBossIntroduction: CurrentDungeon.IsAuthoredWildForest);
        }
        if (EnemyIntroductions.JustTriggered)
        {
            Projectiles.Clear();
            RootHazards.Clear();
        }

        UpdatePlayerAttackArea();
        ProcessDefeatedEnemies();
        ProcessBossDefeat();
        if (!EnemyIntroductions.IsPresenting)
        {
            ProcessPlayerAttack();
            Enemies.UpdateCombatAndAi(
                gameTime,
                Player,
                CurrentDungeon,
                Projectiles,
                RootHazards);
            WildForestEncounters.EnforceArenaBounds();
            Projectiles.Update(gameTime, Player, CurrentDungeon);
            ProcessPlayerProjectileImpacts();
            ProcessPlayerProjectileHits();
            ProcessSpellbladeRunePlacement();
            RootHazards.Update(gameTime, Player);
            if (ShouldRenderBoss)
            {
                Boss.Update(
                    gameTime,
                    Player,
                    CurrentDungeon,
                    IsWildForestShowcaseMode
                        ? _sequentialTestRoom
                        : CurrentDungeon.BossRoom,
                    RootHazards,
                    IsWildForestShowcaseMode
                        ? EnemyManager.WildForestShowcaseActivationDistance
                        : null);
            }
        }

        if (!Player.IsAlive)
        {
            Projectiles.Clear();
            GroundRunes.Clear();
            ClearBreakerEffects();
            RootHazards.Clear();
            Player.ClearTemporaryStatus();
            IsInventoryOpen = false;
            State = GameState.GameOver;
        }

        FollowPlayerWithCamera(elapsedSeconds);
        StoreInputStates(keyboardState, mouseState);
    }

    private void UpdateRottenCorpseBursts(GameTime gameTime)
    {
        float elapsed = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        for (int index = _rottenCorpseBursts.Count - 1; index >= 0; index--)
        {
            RottenCorpseBurst burst = _rottenCorpseBursts[index];
            burst.TimeRemaining -= elapsed;
            if (burst.TimeRemaining > 0f)
                continue;
            if (Vector2.DistanceSquared(Player.Position, burst.Position) < 190f * 190f)
                Curse.Add(12f);
            foreach (Enemy nearby in Enemies.Enemies)
                if (Vector2.DistanceSquared(nearby.Position, burst.Position) < 150f * 150f)
                    nearby.ReceiveDamage(18, 32f);
            _rottenCorpseBursts.RemoveAt(index);
        }
    }

    private void ProcessDefeatedEnemies()
    {
        int defeatedCount = Enemies.RemoveDefeated(
            _defeatedEnemies,
            out int experienceReward);

        if (defeatedCount == 0)
            return;

        if (IsWildForestShowcaseMode)
        {
            foreach (Enemy enemy in _defeatedEnemies)
                RootHazards.RemoveOwnedBy(enemy);

            return;
        }

        Player.GainExperience(experienceReward);

        foreach (Enemy enemy in _defeatedEnemies)
        {
            RootHazards.RemoveOwnedBy(enemy);

            if (CurrentDungeon.IsAncientCatacombs)
            {
                Curse.Reduce(enemy.Type switch
                {
                    EnemyType.SoulCollector => 32f,
                    EnemyType.CursedKnight => 18f,
                    EnemyType.DeathKnight => 24f,
                    EnemyType.Wraith => 8f,
                    _ => 3f
                });
                if (enemy.Type == EnemyType.RottenCorpse)
                    _rottenCorpseBursts.Add(new RottenCorpseBurst(enemy.Position));
                if (enemy.Type == EnemyType.FallenKnight)
                {
                    Map02Cleared = true;
                    BossDefeated = true;
                    Curse.Reduce(100f);
                }
            }

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
        Map01Cleared = false;
        Map02Cleared = false;
        WorldTier = 1;
        Region = RegionDefinition.WildForest;
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = IsWildForestShowcaseMode
            ? _dungeonGenerator.GenerateWildForestShowcase()
            : _dungeonGenerator.GenerateWildForest();
        Player = new PlayerCharacter(GetRoomEntranceSpawn(CurrentDungeon.StartRoom));
        _lastSafePlayerPosition = Player.Position;
        KillCount = 0;
        IsInventoryOpen = false;
        ShowCombatDebug = DebugCombatHitboxes;
        ShowMapDebug = false;
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

        CurrentDungeon = DungeonDepth == 2
            ? _catacombGenerator.Generate()
            : _dungeonGenerator.Generate();
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom);
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
        _duelistMotionAttackId = -1;
        _recentDuelistDangerZone = Rectangle.Empty;
        _recentDuelistDangerTimeRemaining = 0f;
        _bossHitByPlayerAttack = false;
        Projectiles.Clear();
        GroundRunes.Clear();
        ClearBreakerEffects();
        _spellbladeWorldEffectAttackId = -1;
        _spellbladeTechniqueUseId = -1;
        _runicStrikeChainTarget = null;
        _runicStrikeChainHits = 0;
        _arcaneDominionRuneSnapshot = 0;
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
            isFirstDungeonOfRun: DungeonDepth == 1,
            suppressProceduralSpawns: IsWildForestShowcaseMode ||
                CurrentDungeon.IsAuthoredWildForest ||
                CurrentDungeon.IsAncientCatacombs);
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom, avoidEnemies: true);
        WildForestEncounters.Reset(CurrentDungeon);
        EnemyIntroductions.Reset();
        Curse.Reset();
        Tombs.Reset(CurrentDungeon);
        _rottenCorpseBursts.Clear();
        Chest = new TreasureChest(
            SideScrollingCollision.PlaceOnGround(
                CurrentDungeon.TreasureRoom.Bounds.Center.X,
                new Vector2(56f, 40f),
                CurrentDungeon.TreasureRoom),
            CurrentDungeon.TreasureRoom.Id);
        Boss = CreateRegionBoss();
        BossDefeated = false;

        if (IsWildForestShowcaseMode)
            RestartWildForestShowcase();
        else
            ClearSequentialEnemyTestState();

        UpdatePlayerAttackArea();
        Camera.Snap(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds,
            CurrentDungeon.IsAuthoredWildForest ? .54f :
                CurrentDungeon.IsAncientCatacombs ? .55f : .57f);
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

    private void RestartWildForestShowcase()
    {
        if (!IsWildForestShowcaseMode || CurrentDungeon == null)
            return;

        Enemies.ClearSequentialTestEnemies();
        Projectiles.Clear();
        GroundRunes.Clear();
        ClearBreakerEffects();
        RootHazards.Clear();
        Loot.Reset();
        _wildForestShowcaseEnemies.Clear();
        _sequentialTestRoom = CurrentDungeon.StartRoom;
        _sequentialTestIndex = 0;
        _sequentialBossActive = true;
        _sequentialTestComplete = false;
        BossDefeated = false;
        Player.ClearTemporaryStatus();
        PlacePlayerAtMapEntry(_sequentialTestRoom, avoidEnemies: true);

        int enemyLevel = WorldProgression.GetEnemyLevel(
            Player.Level,
            DungeonDepth,
            WorldTier);

        for (int index = 0;
             index < WildForestShowcaseEnemyTypes.Length;
             index++)
        {
            Enemy enemy = Enemies.SpawnWildForestShowcaseEnemy(
                WildForestShowcaseEnemyTypes[index],
                _sequentialTestRoom,
                Player.Position.X + WildForestShowcaseOffsets[index],
                enemyLevel,
                WorldTier);
            _wildForestShowcaseEnemies.Add(enemy);
        }

        _sequentialTestEnemy = _wildForestShowcaseEnemies[0];
        Boss = new AncientTreant(
            SideScrollingCollision.PlaceOnGround(
                Player.Position.X + WildForestShowcaseBossOffset,
                new Vector2(110f, 126f),
                _sequentialTestRoom),
            _sequentialTestRoom.Id,
            Player.Level,
            DungeonDepth,
            WorldTier);
        Camera.Snap(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds);
    }

    private void ClearSequentialEnemyTestState()
    {
        _sequentialTestRoom = null;
        _sequentialTestEnemy = null;
        _sequentialTestIndex = 0;
        _sequentialBossActive = false;
        _sequentialTestComplete = false;
        _wildForestShowcaseEnemies.Clear();
    }

    private DungeonRoom FindSequentialTestRoom()
    {
        DungeonRoom result = null;

        foreach (DungeonRoom room in CurrentDungeon.Rooms)
        {
            if (room.Type == RoomType.Enemy &&
                (result == null || room.Bounds.Left < result.Bounds.Left))
            {
                result = room;
            }
        }

        return result ?? CurrentDungeon.StartRoom;
    }

    private Vector2 FindSequentialBossPosition()
    {
        Vector2 bossSize = new(110f, 126f);
        float minimumX = _sequentialTestRoom.Bounds.Left + 190f;
        float maximumX = _sequentialTestRoom.Bounds.Right - 190f;
        float preferredDirection = maximumX - Player.Position.X >= 600f
            ? 1f
            : -1f;

        for (int step = 0; step <= 8; step++)
        {
            float distance = 600f - step * 50f;
            float direction = step % 2 == 0
                ? preferredDirection
                : -preferredDirection;
            float x = MathHelper.Clamp(
                Player.Position.X + direction * distance,
                minimumX,
                maximumX);
            Vector2 candidate = SideScrollingCollision.PlaceOnGround(
                x,
                bossSize,
                _sequentialTestRoom);

            if (SideScrollingCollision.IsPositionFree(
                candidate,
                bossSize,
                CurrentDungeon))
            {
                return candidate;
            }
        }

        return SideScrollingCollision.PlaceOnGround(
            _sequentialTestRoom.Bounds.Center.X,
            bossSize,
            _sequentialTestRoom);
    }

    private bool HandleWorldInteraction()
    {
        if (IsWildForestShowcaseMode)
            return false;

        bool canOpenChest = !CurrentDungeon.IsAuthoredWildForest ||
            WildForestEncounters.WarCampRewardUnlocked;
        if (canOpenChest && Chest.TryOpen(
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

    private Vector2 GetRoomEntranceSpawn(
        DungeonRoom room,
        bool avoidEnemies = false)
    {
        List<Rectangle> blockedBounds = null;
        if (avoidEnemies && Enemies.Enemies.Count > 0)
        {
            blockedBounds = new List<Rectangle>(Enemies.Enemies.Count);
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (enemy.IsAlive)
                    blockedBounds.Add(enemy.Bounds);
            }
        }

        return SideScrollingCollision.ResolveSafeEntrySpawn(
            room.Bounds.Left + 120f,
            Player?.Size ?? new Vector2(40f, 56f),
            CurrentDungeon,
            MapEntryDropOffset,
            blockedBounds: blockedBounds);
    }

    private void PlacePlayerAtMapEntry(
        DungeonRoom room,
        bool avoidEnemies = false)
    {
        Player.MoveTo(GetRoomEntranceSpawn(room, avoidEnemies));
        _lastSafePlayerPosition = Player.Position;
        _mapEntryFallActive = true;
    }

    private void FollowPlayerWithCamera(float elapsedSeconds)
    {
        Camera.Follow(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds,
            elapsedSeconds,
            CurrentDungeon.IsAuthoredWildForest ? .54f :
                CurrentDungeon.IsAncientCatacombs ? .55f : .57f);
    }

    internal void ExecuteDeveloperCommand(
        DeveloperCommand command,
        int targetMap,
        int targetZone)
    {
        if (!DeveloperModeEnabled || State != GameState.Playing)
            return;

        switch (command)
        {
            case DeveloperCommand.ToggleGodMode:
                Player.DebugGodMode = !Player.DebugGodMode;
                break;
            case DeveloperCommand.HealPlayer:
                Player.RestoreHealth();
                break;
            case DeveloperCommand.RefillStamina:
                Player.Combat.Stamina.Restore();
                break;
            case DeveloperCommand.ClearCurse:
                Curse.Reset();
                break;
            case DeveloperCommand.ResetPlayerCombat:
                Player.ClearTemporaryStatus();
                break;
            case DeveloperCommand.TeleportMap:
                DeveloperLoadMap(targetMap);
                break;
            case DeveloperCommand.TeleportZone:
                if (DungeonDepth != targetMap)
                    DeveloperLoadMap(targetMap);
                DeveloperTeleportToZone(targetZone);
                break;
            case DeveloperCommand.ResetCurrentEncounter:
                DeveloperResetCurrentEncounter();
                break;
            case DeveloperCommand.KillActiveEnemies:
                DeveloperKillActiveEnemies();
                break;
            case DeveloperCommand.ToggleMap01Cleared:
                Map01Cleared = !Map01Cleared;
                break;
            case DeveloperCommand.ToggleMap02Cleared:
                Map02Cleared = !Map02Cleared;
                break;
            case DeveloperCommand.ResetCurrentBoss:
                DeveloperResetCurrentBoss();
                break;
            case DeveloperCommand.ResetEnemyIntroductions:
                EnemyIntroductions.Reset();
                break;
            case DeveloperCommand.ToggleMapDebug:
                ShowMapDebug = !ShowMapDebug;
                break;
            case DeveloperCommand.ToggleCombatDebug:
                ShowCombatDebug = !ShowCombatDebug;
                break;
        }
    }

    public int GetCurrentZoneIndex()
    {
        if (CurrentDungeon == null)
            return -1;

        if (CurrentDungeon.IsAncientCatacombs)
        {
            for (int index = 0; index < CurrentDungeon.CatacombZones.Count; index++)
                if (CurrentDungeon.CatacombZones[index].Bounds.Contains(Player.Position.ToPoint()))
                    return index;
            return FindNearestCatacombZone();
        }

        for (int index = 0; index < CurrentDungeon.WildForestSections.Count; index++)
            if (CurrentDungeon.WildForestSections[index].Bounds.Contains(Player.Position.ToPoint()))
                return index;
        return FindNearestWildForestSection();
    }

    private void DeveloperLoadMap(int mapNumber)
    {
        mapNumber = System.Math.Clamp(mapNumber, 1, 2);
        DungeonDepth = mapNumber;
        WorldTier = WorldProgression.GetWorldTier(DungeonDepth);
        Region = RegionDefinition.WildForest;
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = mapNumber == 2
            ? _catacombGenerator.Generate()
            : _dungeonGenerator.GenerateWildForest();
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom);
        IsInventoryOpen = false;
        _pendingFusionItem = null;
        InitializeDungeonState();
        Player.RestoreHealth();
        Player.Combat.Stamina.Restore();
    }

    private void DeveloperTeleportToZone(int zoneIndex)
    {
        int count = CurrentDungeon.IsAncientCatacombs
            ? CurrentDungeon.CatacombZones.Count
            : CurrentDungeon.WildForestSections.Count;
        if (count == 0)
            return;

        zoneIndex = System.Math.Clamp(zoneIndex, 0, count - 1);
        Rectangle bounds = CurrentDungeon.IsAncientCatacombs
            ? CurrentDungeon.CatacombZones[zoneIndex].Bounds
            : CurrentDungeon.WildForestSections[zoneIndex].Bounds;
        ClearDeveloperTransientState();
        List<Rectangle> blockers = GetAliveEnemyBounds();
        Vector2 spawn = SideScrollingCollision.ResolveSafeEntrySpawn(
            bounds.Left + 80f,
            Player.Size,
            CurrentDungeon,
            MapEntryDropOffset,
            blockedBounds: blockers);
        Player.MoveTo(spawn);
        Player.RestoreHealth();
        Player.Combat.Stamina.Restore();
        _lastSafePlayerPosition = spawn;
        _mapEntryFallActive = true;
        Camera.Snap(
            Player.Position,
            Player.Facing,
            CurrentDungeon.WorldBounds,
            CurrentDungeon.IsAuthoredWildForest ? .54f : .55f);
    }

    private void DeveloperResetCurrentEncounter()
    {
        string zoneId = FindCurrentEncounterZoneId(requireBoss: false);
        if (string.IsNullOrEmpty(zoneId))
            return;
        WildForestEncounters.ResetZone(zoneId, Enemies);
        ClearDeveloperTransientState();
    }

    private void DeveloperKillActiveEnemies()
    {
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.IsAlive && Enemies.IsActive(
                enemy, Player.Position, CurrentDungeon))
            {
                enemy.ReceiveDamage(int.MaxValue);
            }
        }

        if (ShouldRenderBoss && Boss.IsAlive && Boss.IsActivated)
            Boss.ReceiveDamage(int.MaxValue);
        ProcessDefeatedEnemies();
        ProcessBossDefeat();
    }

    private void DeveloperResetCurrentBoss()
    {
        int zoneIndex = GetCurrentZoneIndex();
        if (CurrentDungeon.IsAuthoredWildForest &&
            zoneIndex == CurrentDungeon.WildForestSections.Count - 1)
        {
            Boss = CreateRegionBoss();
            BossDefeated = false;
            Map01Cleared = false;
            ClearDeveloperTransientState();
            return;
        }

        string zoneId = FindCurrentEncounterZoneId(requireBoss: true);
        if (string.IsNullOrEmpty(zoneId))
            return;
        WildForestEncounters.ResetZone(zoneId, Enemies);
        if (zoneId == "fallen-knight")
        {
            Map02Cleared = false;
            BossDefeated = false;
        }
        ClearDeveloperTransientState();
    }

    private string FindCurrentEncounterZoneId(bool requireBoss)
    {
        if (!requireBoss)
        {
            if (!string.IsNullOrEmpty(WildForestEncounters.ActiveZoneId))
                return WildForestEncounters.ActiveZoneId;
            if (!string.IsNullOrEmpty(WildForestEncounters.WarningZoneId))
                return WildForestEncounters.WarningZoneId;
        }

        int index = GetCurrentZoneIndex();
        if (index < 0)
            return string.Empty;
        string sectionId = CurrentDungeon.IsAncientCatacombs
            ? CurrentDungeon.CatacombZones[index].ZoneId
            : CurrentDungeon.WildForestSections[index].SectionId;
        foreach (EncounterZone zone in CurrentDungeon.EncounterZones)
        {
            if (zone.SectionId == sectionId &&
                (!requireBoss || zone.IsEliteZone || zone.IsBossZone))
            {
                return zone.ZoneId;
            }
        }
        return string.Empty;
    }

    private void ClearDeveloperTransientState()
    {
        Projectiles.Clear();
        GroundRunes.Clear();
        RootHazards.Clear();
        ClearBreakerEffects();
        _rottenCorpseBursts.Clear();
        Player.ClearTemporaryStatus();
    }

    private List<Rectangle> GetAliveEnemyBounds()
    {
        var result = new List<Rectangle>(Enemies.Enemies.Count);
        foreach (Enemy enemy in Enemies.Enemies)
            if (enemy.IsAlive)
                result.Add(enemy.Bounds);
        return result;
    }

    private string GetCurrentZoneName()
    {
        int index = GetCurrentZoneIndex();
        if (index < 0)
            return "UNKNOWN";
        return CurrentDungeon.IsAncientCatacombs
            ? CurrentDungeon.CatacombZones[index].Name
            : CurrentDungeon.WildForestSections[index].Name;
    }

    private int CountActiveEnemies()
    {
        int count = 0;
        if (CurrentDungeon == null)
            return count;
        foreach (Enemy enemy in Enemies.Enemies)
            if (enemy.IsAlive && Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                count++;
        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated)
            count++;
        return count;
    }

    private int FindNearestCatacombZone()
    {
        int result = 0;
        float distance = float.MaxValue;
        for (int index = 0; index < CurrentDungeon.CatacombZones.Count; index++)
        {
            float candidate = MathF.Abs(
                CurrentDungeon.CatacombZones[index].Bounds.Center.X - Player.Position.X);
            if (candidate < distance){distance = candidate;result = index;}
        }
        return result;
    }

    private int FindNearestWildForestSection()
    {
        int result = 0;
        float distance = float.MaxValue;
        for (int index = 0; index < CurrentDungeon.WildForestSections.Count; index++)
        {
            float candidate = MathF.Abs(
                CurrentDungeon.WildForestSections[index].Bounds.Center.X - Player.Position.X);
            if (candidate < distance){distance = candidate;result = index;}
        }
        return result;
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
        if (IsWildForestShowcaseMode)
        {
            if (!_sequentialBossActive || Boss.IsAlive)
                return;

            _sequentialBossActive = false;
            _sequentialTestComplete = true;
            Projectiles.Clear();
            ClearBreakerEffects();
            RootHazards.Clear();
            return;
        }

        if (BossDefeated || Boss.IsAlive)
            return;

        Player.GainExperience(Boss.ExperienceReward);
        RootHazards.Clear();
        Loot.CreateBossWeaponDrop(
            Boss.Position,
            Region.BossWeaponReward,
            CurrentDungeon);
        BossDefeated = true;
        if (CurrentDungeon.IsAuthoredWildForest)
            Map01Cleared = true;
        KillCount++;
    }

    private void UpdatePlayerAttackArea()
    {
        AttackDefinition attack = Player.Combat.CurrentAttack ??
            Player.Combat.MoveSet.GetLight(0);
        if (Player.WeaponFamily == WeaponFamily.ChainFlail)
        {
            WeaponTechniqueEffect effect =
                Player.Combat.CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None;
            Vector2 localOffset = ChainFlailMotion.GetHeadOffset(
                effect,
                Player.Combat.TechniqueStageIndex,
                attack,
                Player.Combat.StateElapsed,
                Player.Combat.CalculateRange(attack),
                effect == WeaponTechniqueEffect.ReckonerChainsOfJudgment
                    ? Player.Combat.ReckonerMomentumSnapshotRatio
                    : Player.Combat.Resources.ChainMomentumRatio,
                Player.Combat.ReckonerOrbitHoldSeconds,
                Player.Combat.ReckonerCurveSide);
            PlayerAttackArea = ChainFlailMotion.CreateHeadHitbox(
                Player.Position,
                Player.Facing == DungeonAscendant.Player.FacingDirection.Left
                    ? -1f
                    : 1f,
                localOffset,
                attack,
                CurrentDungeon,
                out bool terrainClipped);
            if (terrainClipped && effect ==
                    WeaponTechniqueEffect.ReckonerChainHarpoon &&
                Player.Combat.TechniqueStageIndex == 0)
            {
                Player.Combat.RequestReckonerHarpoonReturn();
            }
            return;
        }

        PlayerAttackArea = MeleeHitArea.Create(
            Player.Position,
            Player.Size,
            Player.Facing,
            Player.Combat.CalculateRange(attack),
            attack.Thickness,
            attack.HitboxShape);
    }

    private void UpdateDuelistTechniqueMovement()
    {
        if (Player.WeaponFamily != WeaponFamily.DualSwords ||
            Player.Combat.CurrentTechnique == null ||
            Player.Combat.CurrentAttack == null ||
            _duelistMotionAttackId == Player.Combat.AttackId)
        {
            return;
        }

        _duelistMotionAttackId = Player.Combat.AttackId;
        WeaponTechniqueEffect effect =
            Player.Combat.CurrentTechnique.Effect;
        int stage = Player.Combat.TechniqueStageIndex;
        bool shouldReposition = effect switch
        {
            WeaponTechniqueEffect.DuelistPhantomStep => stage == 0,
            WeaponTechniqueEffect.DuelistAfterimageExecution => stage == 1,
            WeaponTechniqueEffect.DuelistMirageCyclone => true,
            WeaponTechniqueEffect.DuelistFinalWaltz => stage > 0,
            _ => false
        };

        if (!shouldReposition)
            return;

        Vector2 start = Player.Position;
        bool hasTarget = TryFindDuelistTarget(
            out Rectangle targetBounds,
            out Vector2 targetPosition);
        float facingSign = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
        Vector2 desired;

        if (!hasTarget)
        {
            float fallbackDistance = effect ==
                WeaponTechniqueEffect.DuelistPhantomStep ? 92f : 64f;
            desired = start + new Vector2(
                facingSign * fallbackDistance,
                0f);
        }
        else
        {
            int sequence = GetDuelistMovementSequence(effect, stage);
            float currentSide = start.X <= targetPosition.X ? -1f : 1f;
            float desiredSide = effect ==
                WeaponTechniqueEffect.DuelistPhantomStep
                    ? -currentSide
                    : sequence % 2 == 0 ? -1f : 1f;

            // Large targets use bounded side-crosses rather than a full-body
            // traversal, keeping the player outside their hurtbox.
            if (targetBounds.Width >= 96 &&
                effect == WeaponTechniqueEffect.DuelistPhantomStep)
            {
                desiredSide = currentSide;
            }

            float safeOffset = Player.Size.X / 2f + 14f;
            float desiredX = desiredSide < 0f
                ? targetBounds.Left - safeOffset
                : targetBounds.Right + safeOffset;
            float verticalOffset = effect is
                WeaponTechniqueEffect.DuelistMirageCyclone or
                WeaponTechniqueEffect.DuelistFinalWaltz
                    ? (sequence % 3 - 1) * 6f
                    : 0f;
            desired = new Vector2(desiredX, start.Y + verticalOffset);
        }

        Rectangle sweptBounds = CreateSweptPlayerBounds(start, desired);
        bool crossedActiveAttack = effect ==
            WeaponTechniqueEffect.DuelistPhantomStep &&
            IntersectsActiveEnemyAttack(sweptBounds);
        Vector2 displacement = Player.MoveThroughCombatTechnique(
            desired,
            CurrentDungeon);

        if (hasTarget && Player.Bounds.Intersects(targetBounds))
        {
            Player.MoveThroughCombatTechnique(start, CurrentDungeon);
            displacement = Player.Position - start;
        }


        if (hasTarget)
            Player.FaceToward(targetPosition.X);

        if (crossedActiveAttack && displacement.LengthSquared() >= 24f * 24f)
            Player.Combat.RegisterPerfectPhantomStep();

        if (hasTarget && displacement.LengthSquared() >= 36f * 36f)
            Player.Combat.RegisterAggressiveReposition();
    }

    private void UpdateRangerTechniqueMovement()
    {
        if (Player.WeaponFamily != WeaponFamily.HunterBow ||
            Player.Combat.CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.RangerPredatorsHorizon ||
            Player.Combat.TechniqueStageIndex != 0 ||
            Player.Combat.CurrentAttack == null ||
            _rangerHopAttackId == Player.Combat.AttackId)
            return;

        _rangerHopAttackId = Player.Combat.AttackId;
        Player.BeginRangerHop(-365f);
    }

    private void UpdateRangerFocusPressure(float elapsedSeconds)
    {
        if (Player.WeaponFamily != WeaponFamily.HunterBow)
            return;

        float pressureSquared = RangerTuning.ClosePressureDistance *
            RangerTuning.ClosePressureDistance;
        bool pressured = false;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.CanBeTargeted &&
                Vector2.DistanceSquared(Player.Position, enemy.Position) <=
                    pressureSquared)
            {
                pressured = true;
                break;
            }
        }

        if (!pressured && ShouldRenderBoss && Boss.IsAlive)
        {
            pressured = Vector2.DistanceSquared(
                Player.Position,
                Boss.Position) <= pressureSquared;
        }

        Player.Combat.UpdateRangerClosePressure(elapsedSeconds, pressured);
    }

    private bool TryFindDuelistTarget(
        out Rectangle targetBounds,
        out Vector2 targetPosition)
    {
        targetBounds = Rectangle.Empty;
        targetPosition = Vector2.Zero;
        float bestScore = float.MaxValue;
        float facingSign = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted)
                continue;

            float horizontal = enemy.Position.X - Player.Position.X;
            float score = Vector2.DistanceSquared(
                Player.Position,
                enemy.Position);
            if (MathF.Sign(horizontal) != facingSign)
                score += 180f * 180f;

            if (score < bestScore)
            {
                bestScore = score;
                targetBounds = enemy.MeleeTargetBounds;
                targetPosition = enemy.Position;
            }
        }

        if (ShouldRenderBoss && Boss.IsAlive)
        {
            float horizontal = Boss.Position.X - Player.Position.X;
            float score = Vector2.DistanceSquared(
                Player.Position,
                Boss.Position);
            if (MathF.Sign(horizontal) != facingSign)
                score += 180f * 180f;

            if (score < bestScore)
            {
                bestScore = score;
                targetBounds = Boss.Bounds;
                targetPosition = Boss.Position;
            }
        }

        return bestScore <= 520f * 520f;
    }

    private bool IntersectsActiveEnemyAttack(Rectangle sweptBounds)
    {
        if (_recentDuelistDangerTimeRemaining > 0f &&
            sweptBounds.Intersects(_recentDuelistDangerZone))
        {
            return true;
        }

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.CanBeTargeted && enemy.Attack.IsActive &&
                sweptBounds.Intersects(enemy.AttackArea))
            {
                return true;
            }
        }

        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (!projectile.IsPlayerOwned &&
                projectile.LifetimeRemaining > 0f &&
                sweptBounds.Intersects(projectile.Bounds))
            {
                return true;
            }
        }

        return ShouldRenderBoss && Boss.IsAlive && Boss.Attack.IsActive &&
            sweptBounds.Intersects(Boss.AttackArea);
    }

    private void UpdateRecentDuelistDangerZone(float elapsedSeconds)
    {
        _recentDuelistDangerTimeRemaining = MathF.Max(
            0f,
            _recentDuelistDangerTimeRemaining - MathF.Max(0f, elapsedSeconds));
        Rectangle activeZone = Rectangle.Empty;
        float bestDistance = float.MaxValue;

        void Consider(Rectangle zone)
        {
            float distance = Vector2.DistanceSquared(
                Player.Position,
                zone.Center.ToVector2());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                activeZone = zone;
            }
        }

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted || !enemy.Attack.IsActive)
                continue;

            Consider(enemy.AttackArea);
        }

        if (ShouldRenderBoss && Boss.IsAlive && Boss.Attack.IsActive)
        {
            Consider(Boss.AttackArea);
        }

        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (projectile.IsPlayerOwned || projectile.LifetimeRemaining <= 0f)
                continue;

            Consider(projectile.Bounds);
        }

        if (!activeZone.IsEmpty)
        {
            _recentDuelistDangerZone = activeZone;
            // Covers the single-input chord wait plus a small timing grace.
            _recentDuelistDangerTimeRemaining = .38f;
        }
        else if (_recentDuelistDangerTimeRemaining <= 0f)
        {
            _recentDuelistDangerZone = Rectangle.Empty;
        }
    }

    private Rectangle CreateSweptPlayerBounds(Vector2 start, Vector2 end)
    {
        float halfWidth = Player.Size.X / 2f;
        float halfHeight = Player.Size.Y / 2f;
        int left = (int)MathF.Floor(MathF.Min(start.X, end.X) - halfWidth);
        int right = (int)MathF.Ceiling(MathF.Max(start.X, end.X) + halfWidth);
        int top = (int)MathF.Floor(MathF.Min(start.Y, end.Y) - halfHeight);
        int bottom = (int)MathF.Ceiling(MathF.Max(start.Y, end.Y) + halfHeight);
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static int GetDuelistMovementSequence(
        WeaponTechniqueEffect effect,
        int stage)
    {
        if (effect == WeaponTechniqueEffect.DuelistAfterimageExecution)
            return 1;
        return stage;
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
        WeaponTechnique technique = Player.Combat.CurrentTechnique;
        int processingAttackId = Player.Combat.AttackId;

        PrepareSpellbladeTechnique(technique);
        ProcessSpellbladeWorldEffect(technique);

        if (Player.WeaponFamily == WeaponFamily.WarAxe &&
            Player.Combat.CurrentTechniqueUseId !=
                _raiderDisplacementTechniqueUseId)
        {
            _raiderDisplacementTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
            _raiderWallImpactedEnemies.Clear();
        }

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
            bool wasStaggered = enemy.IsStaggered;
            int imprintStacks = enemy.ArcaneImprintCount;
            float damageMultiplier = enemy.PhysicalDamageTakenMultiplier;
            damageMultiplier *= GetReckonerDamageMultiplier(
                technique,
                enemy.Position);

            if (technique?.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz &&
                !IsPrimaryFinalWaltzTarget(enemy.Position))
            {
                damageMultiplier *= .55f;
            }

            if (technique?.Effect == WeaponTechniqueEffect.ConsumeHunterMark &&
                enemy.IsHunterMarked)
                damageMultiplier *= 1.45f;

            bool wasUnstable = wasStaggered ||
                enemy.CurrentPoise <=
                    enemy.MaxPoise * RaiderTuning.UnstablePoiseRatio;
            damageMultiplier *= GetRaiderDamageMultiplier(
                technique,
                wasUnstable,
                _raiderWallImpactedEnemies.Contains(enemy));

            if (wasStaggered && technique?.Id is "iron-reversal" or
                "skullsplitter")
                damageMultiplier *= 1.35f;
            if (wasStaggered && technique?.Effect ==
                WeaponTechniqueEffect.BreakerAnvilFall)
            {
                damageMultiplier *= 1f +
                    BreakerTuning.AnvilStaggeredDamageBonus;
            }

            float targetPoiseDamage = poiseDamage;
            targetPoiseDamage *= GetRaiderPoiseMultiplier(
                technique,
                wasUnstable);
            targetPoiseDamage *= GetBreakerDirectPoiseMultiplier(
                technique,
                enemy.Position,
                enemy.CurrentPoise,
                enemy.MaxPoise);
            if (technique?.Id == "knights-edge" &&
                Player.Combat.TechniqueStageIndex == 2 &&
                !enemy.IsStaggered &&
                enemy.CurrentPoise <= enemy.MaxPoise * .40f)
            {
                targetPoiseDamage += enemy.MaxPoise * .25f;
            }

            int targetDamage = Math.Max(
                0,
                (int)MathF.Round(damage * damageMultiplier));
            ApplySpellbladeDamageScaling(
                technique,
                imprintStacks,
                isBoss: false,
                ref targetDamage,
                ref targetPoiseDamage);
            enemy.ReceiveDamage(
                targetDamage,
                targetPoiseDamage);
            ApplyTechniqueEffect(enemy, technique?.Effect ?? WeaponTechniqueEffect.None);
            ResolveSpellbladeEnemyHit(
                enemy,
                technique,
                imprintStacks,
                targetDamage);
            ResolveBreakerEnemyHit(enemy, technique, wasUnstable);

            if (IsRaiderTechnique(technique?.Effect ?? WeaponTechniqueEffect.None))
            {
                ResolveRaiderEnemyHit(enemy, technique, wasUnstable);
                bool heavyImpact =
                    technique.GetStage(Player.Combat.TechniqueStageIndex).IsHeavy ||
                    targetPoiseDamage >= 65f;
                Player.Combat.RegisterRaiderImpactFeedback(
                    heavyImpact);
                if (heavyImpact)
                    Camera.AddImpactFeedback(1.8f);
            }
            else if (ResolveReckonerEnemyHit(enemy, technique))
            {
            }
            else if (technique?.Effect == WeaponTechniqueEffect.HookPull)
            {
                if (enemy.IsHeavyPullAnchor)
                    Player.PullToward(enemy.Position, 24f, CurrentDungeon);
                else
                    enemy.PullToward(Player.Position, knockback, CurrentDungeon);
            }
            else
                enemy.ApplyKnockback(Player.Position, knockback, CurrentDungeon);

            Player.Combat.RegisterSuccessfulHit();
            if (technique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainHarpoon &&
                Player.Combat.TechniqueStageIndex == 0 &&
                Player.Combat.RegisterReckonerHarpoonOutboundTarget(
                    enemy.WeightClass == EnemyWeightClass.Heavy ||
                    enemy.IsHeavyPullAnchor))
            {
                break;
            }
        }

        if (Player.Combat.AttackId != processingAttackId)
            return;

        if (!_bossHitByPlayerAttack && ShouldRenderBoss && Boss.IsAlive &&
            PlayerAttackArea.Intersects(Boss.Bounds))
        {
            _bossHitByPlayerAttack = true;
            RegisterPlayerHitEffect(Boss.Bounds);
            int bossImprintStacks = Boss.ArcaneImprintCount;
            float bossDamageMultiplier = technique?.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz &&
                !IsPrimaryFinalWaltzTarget(Boss.Position)
                    ? .55f
                    : 1f;
            bossDamageMultiplier *= GetReckonerDamageMultiplier(
                technique,
                Boss.Position);
            bool bossWasUnstable = Boss.IsStaggered ||
                Boss.CurrentPoise <=
                    Boss.MaxPoise * RaiderTuning.UnstablePoiseRatio;
            bossDamageMultiplier *= GetRaiderDamageMultiplier(
                technique,
                bossWasUnstable,
                wallImpacted: false);
            float bossPoiseMultiplier = GetRaiderPoiseMultiplier(
                technique,
                bossWasUnstable);
            if (Boss.IsStaggered && technique?.Effect ==
                WeaponTechniqueEffect.BreakerAnvilFall)
            {
                bossDamageMultiplier *= 1f +
                    BreakerTuning.AnvilStaggeredDamageBonus;
            }
            bossPoiseMultiplier *= GetBreakerDirectPoiseMultiplier(
                technique,
                Boss.Position,
                Boss.CurrentPoise,
                Boss.MaxPoise);
            int bossDamage = Math.Max(0, (int)MathF.Round(
                damage * bossDamageMultiplier));
            float bossPoiseDamage =
                poiseDamage * bossDamageMultiplier * bossPoiseMultiplier;
            ApplySpellbladeDamageScaling(
                technique,
                bossImprintStacks,
                isBoss: true,
                ref bossDamage,
                ref bossPoiseDamage);
            Boss.ReceiveDamage(bossDamage, bossPoiseDamage);
            ResolveSpellbladeBossHit(
                technique,
                bossImprintStacks,
                bossDamage);
            ResolveBreakerBossHit(technique);
            ResolveReckonerBossHit(technique);
            bool suppressBossDisplacement = technique?.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz &&
                Player.Combat.TechniqueStageIndex < technique.StageCount - 1;
            suppressBossDisplacement |= technique?.Effect is
                WeaponTechniqueEffect.ReckonerChainsOfJudgment or
                WeaponTechniqueEffect.ReckonerVortexSnare;
            bool raiderTechnique = IsRaiderTechnique(
                technique?.Effect ?? WeaponTechniqueEffect.None);
            if (!raiderTechnique &&
                technique?.Effect != WeaponTechniqueEffect.IronJuggernaut &&
                !suppressBossDisplacement)
            {
                Boss.ApplyKnockback(
                    Player.Position,
                    knockback,
                    CurrentDungeon);
            }
            if (technique?.Effect == WeaponTechniqueEffect.HookPull)
                Player.PullToward(Boss.Position, 20f, CurrentDungeon);
            if (raiderTechnique)
            {
                if (technique.Effect == WeaponTechniqueEffect.RaiderHookingAxe)
                {
                    Player.PullToward(
                        Boss.Position,
                        RaiderTuning.BossHookPlayerAdvance,
                        CurrentDungeon);
                }
                bool heavyImpact =
                    technique.GetStage(Player.Combat.TechniqueStageIndex).IsHeavy ||
                    poiseDamage >= 65f;
                Player.Combat.RegisterRaiderImpactFeedback(heavyImpact);
                if (heavyImpact)
                    Camera.AddImpactFeedback(2.2f);
            }
            Player.Combat.RegisterSuccessfulHit();
            if (technique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainHarpoon &&
                Player.Combat.TechniqueStageIndex == 0)
            {
                Player.Combat.RegisterReckonerHarpoonOutboundTarget(
                    heavyOrBoss: true);
            }
        }

        ProcessBreakerWorldEffect(technique);
    }

    private float GetReckonerDamageMultiplier(
        WeaponTechnique technique,
        Vector2 targetPosition)
    {
        if (Player.WeaponFamily != WeaponFamily.ChainFlail ||
            technique?.Effect !=
                WeaponTechniqueEffect.ReckonerCrescentRequiem ||
            Player.Combat.TechniqueStageIndex != 1)
        {
            return 1f;
        }

        float range = Player.Combat.CalculateRange(Player.Combat.CurrentAttack);
        float distance = Vector2.Distance(Player.Position, targetPosition);
        if (distance < range * ReckonerTuning.CrescentOuterEdgeStartRatio)
            return 1f;

        return 1f + ReckonerTuning.CrescentOuterEdgeDamageBonus +
            Player.Combat.Resources.ChainMomentumRatio *
                ReckonerTuning.CrescentMomentumDamageBonus;
    }

    private bool ResolveReckonerEnemyHit(
        Enemy enemy,
        WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.ChainFlail ||
            enemy == null || technique == null)
        {
            return false;
        }

        int stage = Player.Combat.TechniqueStageIndex;
        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.ReckonerChainHarpoon when stage == 1:
                enemy.PullTowardSafe(
                    Player.Position,
                    24f,
                    34f,
                    CurrentDungeon);
                return true;

            case WeaponTechniqueEffect.ReckonerVortexSnare when stage is >= 1 and <= 5:
                float slow = enemy.WeightClass == EnemyWeightClass.Heavy
                    ? ReckonerTuning.VortexHeavySlowMultiplier
                    : ReckonerTuning.VortexSlowMultiplier -
                        Player.Combat.ReckonerMomentumSnapshotRatio * .07f;
                enemy.ApplyControl(
                    slow,
                    ReckonerTuning.VortexSlowDurationSeconds +
                        Player.Combat.ReckonerMomentumSnapshotRatio * .12f);
                return true;

            case WeaponTechniqueEffect.ReckonerVortexSnare when stage == 6:
                enemy.PullTowardSafe(
                    Player.Position,
                    36f + Player.Combat.ReckonerMomentumSnapshotRatio * 12f,
                    30f,
                    CurrentDungeon);
                return true;

            case WeaponTechniqueEffect.ReckonerChainsOfJudgment when stage is >= 1 and <= 5:
                return true;

            case WeaponTechniqueEffect.ReckonerChainsOfJudgment when stage == 6:
                float facing = Player.Facing ==
                    DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
                Vector2 controlCenter = Player.Position +
                    new Vector2(facing * 92f, 0f);
                enemy.PullTowardSafe(
                    controlCenter,
                    42f * (1f + Player.Combat.ReckonerMomentumSnapshotRatio *
                        ReckonerTuning.UltimateMaximumControlBonus),
                    20f,
                    CurrentDungeon);
                return true;

            case WeaponTechniqueEffect.ReckonerChainsOfJudgment when stage == 7:
                Camera.AddImpactFeedback(2.6f);
                return false;
        }

        return false;
    }

    private void ResolveReckonerBossHit(WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.ChainFlail ||
            technique == null)
        {
            return;
        }

        int stage = Player.Combat.TechniqueStageIndex;
        if (technique.Effect == WeaponTechniqueEffect.ReckonerVortexSnare &&
            stage is >= 1 and <= 5)
        {
            Boss.ApplyArcaneSlow(.86f, .24f);
        }
        else if (technique.Effect ==
                WeaponTechniqueEffect.ReckonerChainsOfJudgment &&
            stage == 7)
        {
            Camera.AddImpactFeedback(3.2f);
        }
    }

    private void ProcessBreakerWorldEffect(WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.SpikedMace ||
            technique == null ||
            _breakerWorldEffectAttackId == Player.Combat.AttackId)
            return;

        WeaponTechniqueEffect effect = technique.Effect;
        if (effect is < WeaponTechniqueEffect.BreakerIronCrush or
            > WeaponTechniqueEffect.BreakerWorldbreaker)
            return;

        _breakerWorldEffectAttackId = Player.Combat.AttackId;
        float facing = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
        Vector2 impact = new(
            Player.Position.X + facing * 38f,
            Player.Bounds.Bottom - 3f);
        float ratio = Player.Combat.BreakerInertiaSnapshotRatio;

        if (effect == WeaponTechniqueEffect.BreakerEarthbreaker)
        {
            float radius = BreakerTuning.EarthbreakerShockwaveRadius *
                (1f + ratio * .12f);
            _breakerImpactVisuals.Add(new BreakerImpactVisual(
                impact,
                radius,
                .45f + ratio * .40f));
            ApplyBreakerRadialShockwave(
                impact,
                radius,
                BreakerTuning.EarthbreakerShockwaveDamage,
                BreakerTuning.EarthbreakerShockwavePoise * (1f + ratio * .35f),
                ratio,
                worldbreaker: false);
            Camera.AddImpactFeedback(2.6f + ratio * 1.4f);
        }
        else if (effect == WeaponTechniqueEffect.BreakerCataclysmWheel)
        {
            _breakerImpactVisuals.Add(new BreakerImpactVisual(
                impact,
                66f,
                .65f + ratio * .25f));
            _breakerShockwaves.Add(new BreakerShockwave(
                impact,
                facing,
                ratio));
            Camera.AddImpactFeedback(3.4f);
        }
        else if (effect == WeaponTechniqueEffect.BreakerWorldbreaker)
        {
            float radius = BreakerTuning.WorldbreakerBaseShockwaveRadius +
                BreakerTuning.WorldbreakerMaximumShockwaveRadiusBonus * ratio;
            _breakerImpactVisuals.Add(new BreakerImpactVisual(
                impact,
                radius,
                .85f + ratio * .15f));
            ApplyBreakerRadialShockwave(
                impact,
                radius,
                BreakerTuning.WorldbreakerShockwaveDamage,
                BreakerTuning.WorldbreakerShockwavePoise * (1f + ratio * .55f),
                ratio,
                worldbreaker: true);
            Player.Combat.RegisterBreakerImpactFeedback(worldbreaker: true);
            Camera.AddImpactFeedback(5.2f + ratio * 2f);
        }
    }

    private void ApplyBreakerRadialShockwave(
        Vector2 impact,
        float radius,
        int baseDamage,
        float basePoise,
        float inertiaRatio,
        bool worldbreaker)
    {
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted)
                continue;
            float distanceSquared = Vector2.DistanceSquared(
                impact,
                enemy.Position);
            if (distanceSquared > radiusSquared)
                continue;
            float distanceRatio = MathF.Sqrt(distanceSquared) / radius;
            float falloff = MathHelper.Lerp(1f, .45f, distanceRatio);
            enemy.ReceiveDamage(
                Math.Max(1, (int)MathF.Round(baseDamage * falloff)),
                basePoise * falloff);
            enemy.ApplyKnockback(
                Player.Position,
                (worldbreaker ? 56f : 24f) * falloff,
                CurrentDungeon);
            if ((worldbreaker || inertiaRatio >= .75f) &&
                enemy.WeightClass != EnemyWeightClass.Heavy)
            {
                enemy.ApplyRaiderLaunch(worldbreaker
                    ? BreakerTuning.WorldbreakerNormalLaunchVelocity
                    : -125f);
            }
        }

        if (ShouldRenderBoss && Boss.IsAlive &&
            Vector2.DistanceSquared(impact, Boss.Position) <= radiusSquared)
        {
            float distanceRatio = MathF.Min(
                1f,
                Vector2.Distance(impact, Boss.Position) / radius);
            float falloff = MathHelper.Lerp(1f, .55f, distanceRatio);
            Boss.ReceiveDamage(
                Math.Max(1, (int)MathF.Round(baseDamage * falloff)),
                basePoise * falloff);
        }
    }

    private float GetBreakerDirectPoiseMultiplier(
        WeaponTechnique technique,
        Vector2 targetPosition,
        float currentPoise,
        float maximumPoise)
    {
        if (technique == null ||
            Player.WeaponFamily != WeaponFamily.SpikedMace)
            return 1f;

        if (technique.Effect ==
                WeaponTechniqueEffect.BreakerTitansBackhand &&
            MathF.Abs(targetPosition.X - Player.Position.X) >=
                Player.Combat.CalculateRange(Player.Combat.CurrentAttack) * .68f)
            return 1.32f;

        if (technique.Effect == WeaponTechniqueEffect.BreakerIronCrush &&
            Player.Combat.TechniqueStageIndex == 2 && maximumPoise > 0f &&
            currentPoise <= maximumPoise * .42f)
            return 1.25f;

        return 1f;
    }

    private void ResolveBreakerEnemyHit(
        Enemy enemy,
        WeaponTechnique technique,
        bool wasUnstable)
    {
        if (enemy == null || technique == null ||
            Player.WeaponFamily != WeaponFamily.SpikedMace)
            return;

        if (technique.Effect ==
                WeaponTechniqueEffect.BreakerBatteringRush &&
            wasUnstable && enemy.WeightClass != EnemyWeightClass.Heavy)
        {
            enemy.ApplyRaiderLaunch(BreakerTuning.BatteringRushLaunchVelocity);
        }
        else if (technique.Effect ==
                WeaponTechniqueEffect.BreakerWorldbreaker &&
            enemy.WeightClass != EnemyWeightClass.Heavy)
        {
            enemy.ApplyRaiderLaunch(
                BreakerTuning.WorldbreakerNormalLaunchVelocity);
        }

        RegisterBreakerImpactFeedback(technique);
    }

    private void ResolveBreakerBossHit(WeaponTechnique technique)
    {
        if (technique == null ||
            Player.WeaponFamily != WeaponFamily.SpikedMace)
            return;
        RegisterBreakerImpactFeedback(technique);
    }

    private void RegisterBreakerImpactFeedback(WeaponTechnique technique)
    {
        if (_breakerImpactFeedbackAttackId == Player.Combat.AttackId)
            return;
        _breakerImpactFeedbackAttackId = Player.Combat.AttackId;
        bool worldbreaker = technique.Effect ==
            WeaponTechniqueEffect.BreakerWorldbreaker;
        Player.Combat.RegisterBreakerImpactFeedback(worldbreaker);
        Camera.AddImpactFeedback(worldbreaker ? 5.2f : 2.4f);
    }

    private void UpdateBreakerEffects(float elapsedSeconds)
    {
        for (int index = _breakerImpactVisuals.Count - 1; index >= 0; index--)
        {
            BreakerImpactVisual visual = _breakerImpactVisuals[index];
            visual.TimeRemaining -= elapsedSeconds;
            if (visual.TimeRemaining <= 0f)
                _breakerImpactVisuals.RemoveAt(index);
        }

        for (int index = _breakerShockwaves.Count - 1; index >= 0; index--)
        {
            BreakerShockwave wave = _breakerShockwaves[index];
            float movement = MathF.Min(
                wave.Speed * elapsedSeconds,
                wave.MaximumDistance - wave.DistanceTraveled);
            Vector2 candidate = wave.Position +
                new Vector2(wave.Direction * movement, 0f);
            Vector2 collisionCenter = candidate - new Vector2(0f, 11f);
            if (!SideScrollingCollision.IsPositionFree(
                    collisionCenter,
                    new Vector2(18f, 18f),
                    CurrentDungeon))
            {
                wave.IsExpired = true;
            }
            else
            {
                wave.Position = candidate;
                wave.DistanceTraveled += movement;
                ResolveBreakerShockwaveHits(wave);
            }

            if (wave.DistanceTraveled >= wave.MaximumDistance)
                wave.IsExpired = true;
            if (wave.IsExpired)
                _breakerShockwaves.RemoveAt(index);
        }
    }

    private void ResolveBreakerShockwaveHits(BreakerShockwave wave)
    {
        int damage = Math.Max(1, (int)MathF.Round(MathHelper.Lerp(
            wave.NearDamage,
            wave.FarDamage,
            wave.Progress)));
        float poise = MathHelper.Lerp(
            wave.NearPoise,
            wave.FarPoise,
            wave.Progress);

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted || wave.HitEnemies.Contains(enemy) ||
                !wave.Bounds.Intersects(enemy.MeleeTargetBounds))
                continue;
            wave.HitEnemies.Add(enemy);
            bool unstable = enemy.IsStaggered ||
                enemy.CurrentPoise <= enemy.MaxPoise *
                    BreakerTuning.UnstablePoiseRatio;
            enemy.ReceiveDamage(damage, poise);
            enemy.ApplyKnockback(Player.Position, 44f, CurrentDungeon);
            if (unstable && enemy.WeightClass != EnemyWeightClass.Heavy)
                enemy.ApplyRaiderLaunch(-145f);
        }

        if (!wave.HitBoss && ShouldRenderBoss && Boss.IsAlive &&
            wave.Bounds.Intersects(Boss.Bounds))
        {
            wave.HitBoss = true;
            Boss.ReceiveDamage(damage, poise);
        }
    }

    private void ClearBreakerEffects()
    {
        _breakerShockwaves.Clear();
        _breakerImpactVisuals.Clear();
        _breakerWorldEffectAttackId = -1;
        _breakerImpactFeedbackAttackId = -1;
    }

    private void PrepareSpellbladeTechnique(WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            technique == null ||
            technique.Effect is < WeaponTechniqueEffect.SpellbladeRunicStrikes or
                > WeaponTechniqueEffect.SpellbladeArcaneDominion)
            return;

        if (_spellbladeTechniqueUseId == Player.Combat.CurrentTechniqueUseId)
            return;

        _spellbladeTechniqueUseId = Player.Combat.CurrentTechniqueUseId;
        _runicStrikeChainTarget = null;
        _runicStrikeChainHits = 0;
        _arcaneDominionRuneSnapshot = technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneDominion
            ? GroundRunes.CountWithin(
                Player.Position,
                SpellbladeTuning.DominionRadius)
            : 0;
    }

    private void ProcessSpellbladeWorldEffect(WeaponTechnique technique)
    {
        if (technique == null ||
            _spellbladeWorldEffectAttackId == Player.Combat.AttackId)
            return;
        WeaponTechniqueEffect effect = technique.Effect;
        int stage = Player.Combat.TechniqueStageIndex;
        if (effect is < WeaponTechniqueEffect.SpellbladeRunicStrikes or
            > WeaponTechniqueEffect.SpellbladeArcaneDominion)
            return;

        _spellbladeWorldEffectAttackId = Player.Combat.AttackId;
        if (effect == WeaponTechniqueEffect.SpellbladeArcaneDetonation)
        {
            ApplyRuneBlasts(GroundRunes.DetonateWithin(
                Player.Position,
                SpellbladeTuning.ArcaneDetonationRadius));
        }
        else if (effect ==
            WeaponTechniqueEffect.SpellbladeArcaneConvergence)
        {
            GroundRunes.StartNetwork(Player.Position);
        }
        else if (effect == WeaponTechniqueEffect.SpellbladeArcaneDominion)
        {
            if (stage == 1)
            {
                GroundRunes.AwakenAll(
                    Player.Position,
                    SpellbladeTuning.DominionRadius);
            }
            else if (stage == 2)
            {
                ApplyDominionBinding();
            }
            else if (stage == 3)
            {
                ApplyRuneBlasts(GroundRunes.DetonateWithin(
                    Player.Position,
                    SpellbladeTuning.DominionRadius));
            }
        }
    }

    private void ApplySpellbladeDamageScaling(
        WeaponTechnique technique,
        int imprintStacks,
        bool isBoss,
        ref int damage,
        ref float poiseDamage)
    {
        if (technique == null)
            return;
        int stage = Player.Combat.TechniqueStageIndex;
        if (technique.Effect is
            WeaponTechniqueEffect.SpellbladeArcaneDetonation or
            WeaponTechniqueEffect.SpellbladeResonanceBreaker)
        {
            damage += SpellbladeTuning.GetImprintDamage(
                imprintStacks,
                isBoss);
            poiseDamage += imprintStacks * (isBoss
                ? SpellbladeTuning.BossPoisePerStack
                : SpellbladeTuning.ImprintPoisePerStack);
        }
        else if (technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneDominion &&
            stage == 3)
        {
            float setupBonus = MathF.Min(
                SpellbladeTuning.DominionMaximumSetupBonus,
                _arcaneDominionRuneSnapshot *
                    SpellbladeTuning.DominionRuneBonusPerRune +
                imprintStacks *
                    SpellbladeTuning.DominionImprintBonusPerStack);
            damage = Math.Max(1, (int)MathF.Round(damage * (1f + setupBonus)));
            poiseDamage *= 1f + setupBonus * .65f;
        }
    }

    private void ResolveSpellbladeEnemyHit(
        Enemy enemy,
        WeaponTechnique technique,
        int imprintStacksBeforeHit,
        int directDamage)
    {
        if (enemy == null || technique == null)
            return;
        int stage = Player.Combat.TechniqueStageIndex;
        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.SpellbladeRunicStrikes:
                RegisterRunicStrikeHit(enemy, stage);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDetonation:
                enemy.ConsumeArcaneImprints();
                ApplyImprintControl(enemy, imprintStacksBeforeHit);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneLunge:
                enemy.RefreshArcaneImprints();
                break;
            case WeaponTechniqueEffect.SpellbladeResonanceBreaker:
                enemy.ConsumeArcaneImprints();
                ResolveResonanceRearCone(
                    enemy,
                    enemy.Position,
                    directDamage);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDominion when stage == 3:
                enemy.ConsumeArcaneImprints();
                break;
        }
    }

    private void ResolveSpellbladeBossHit(
        WeaponTechnique technique,
        int imprintStacksBeforeHit,
        int directDamage)
    {
        if (technique == null)
            return;
        int stage = Player.Combat.TechniqueStageIndex;
        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.SpellbladeRunicStrikes:
                RegisterRunicStrikeHit(Boss, stage);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDetonation:
                Boss.ConsumeArcaneImprints();
                if (imprintStacksBeforeHit >= 2)
                {
                    Boss.ApplyArcaneSlow(
                        SpellbladeTuning.BossSlowMultiplier,
                        SpellbladeTuning.BossSlowDurationSeconds);
                }
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneLunge:
                Boss.RefreshArcaneImprints();
                break;
            case WeaponTechniqueEffect.SpellbladeResonanceBreaker:
                Boss.ConsumeArcaneImprints();
                ResolveResonanceRearCone(null, Boss.Position, directDamage);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDominion when stage == 3:
                Boss.ConsumeArcaneImprints();
                break;
        }
    }

    private void RegisterRunicStrikeHit(object target, int stage)
    {
        if (stage == 0 && _runicStrikeChainTarget == null)
        {
            _runicStrikeChainTarget = target;
            _runicStrikeChainHits = 1;
            return;
        }
        if (!ReferenceEquals(_runicStrikeChainTarget, target))
            return;
        if (stage == 1 && _runicStrikeChainHits == 1)
        {
            _runicStrikeChainHits = 2;
            return;
        }
        if (stage != 2 || _runicStrikeChainHits != 2)
            return;

        if (target is Enemy enemy)
            enemy.AddArcaneImprint();
        else if (target is AncientTreant boss)
            boss.AddArcaneImprint();
        _runicStrikeChainHits = 3;
    }

    private static void ApplyImprintControl(Enemy enemy, int stacks)
    {
        if (enemy == null || !enemy.CanBeTargeted)
            return;
        if (stacks >= 2)
        {
            enemy.ApplyControl(
                SpellbladeTuning.TwoStackSlowMultiplier,
                SpellbladeTuning.TwoStackSlowDurationSeconds);
        }
        if (stacks >= 3)
        {
            enemy.ApplyArcaneStun(
                SpellbladeTuning.ThreeStackStunDurationSeconds);
        }
    }

    private void ResolveResonanceRearCone(
        Enemy directTarget,
        Vector2 targetPosition,
        int directDamage)
    {
        float facing = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
        int width = (int)SpellbladeTuning.ResonanceRearConeRange;
        int height = (int)SpellbladeTuning.ResonanceRearConeHeight;
        Rectangle cone = facing < 0f
            ? new Rectangle((int)targetPosition.X - width,
                (int)targetPosition.Y - height / 2, width, height)
            : new Rectangle((int)targetPosition.X,
                (int)targetPosition.Y - height / 2, width, height);
        int damage = Math.Max(1, (int)MathF.Round(
            directDamage * SpellbladeTuning.ResonanceRearConeDamageMultiplier));

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (ReferenceEquals(enemy, directTarget) || !enemy.CanBeTargeted ||
                !cone.Intersects(enemy.MeleeTargetBounds) ||
                !IsInsideResonanceRearCone(
                    targetPosition,
                    facing,
                    enemy.MeleeTargetBounds.Center.ToVector2(),
                    enemy.MeleeTargetBounds.Height))
                continue;
            _playerAttackHits.Add(enemy);
            enemy.ReceiveDamage(damage, 18f);
            RegisterPlayerHitEffect(cone, enemy.MeleeTargetBounds);
        }
        if (directTarget != null && ShouldRenderBoss && Boss.IsAlive &&
            cone.Intersects(Boss.Bounds) &&
            IsInsideResonanceRearCone(
                targetPosition,
                facing,
                Boss.Bounds.Center.ToVector2(),
                Boss.Bounds.Height))
        {
            _bossHitByPlayerAttack = true;
            Boss.ReceiveDamage(damage, 18f);
            RegisterPlayerHitEffect(cone, Boss.Bounds);
        }
    }

    private static bool IsInsideResonanceRearCone(
        Vector2 origin,
        float facing,
        Vector2 target,
        float targetHeight)
    {
        float forward = (target.X - origin.X) * facing;
        if (forward < 0f || forward > SpellbladeTuning.ResonanceRearConeRange)
            return false;
        float expansion = forward / SpellbladeTuning.ResonanceRearConeRange;
        float halfHeight = 10f +
            SpellbladeTuning.ResonanceRearConeHeight * .5f * expansion +
            targetHeight * .25f;
        return MathF.Abs(target.Y - origin.Y) <= halfHeight;
    }

    private void ApplyDominionBinding()
    {
        float radiusSquared = SpellbladeTuning.DominionRadius *
            SpellbladeTuning.DominionRadius;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.CanBeTargeted &&
                Vector2.DistanceSquared(Player.Position, enemy.Position) <=
                    radiusSquared)
            {
                enemy.ApplyControl(
                    SpellbladeTuning.DominionNormalSlowMultiplier,
                    SpellbladeTuning.DominionBindingDurationSeconds);
            }
        }
        if (ShouldRenderBoss && Boss.IsAlive &&
            Vector2.DistanceSquared(Player.Position, Boss.Position) <=
                radiusSquared)
        {
            Boss.ApplyArcaneSlow(
                SpellbladeTuning.DominionBossSlowMultiplier,
                SpellbladeTuning.DominionBindingDurationSeconds);
        }
    }

    private void UpdateSpellbladeRuneNetwork()
    {
        if (GroundRunes.NetworkTickPending)
            ApplyRuneNetworkTick();
        if (GroundRunes.NetworkDetonationPending)
            ApplyRuneBlasts(GroundRunes.ConsumeNetworkRunes());
    }

    private void ApplyRuneNetworkTick()
    {
        var runes = new List<GroundRune>();
        foreach (GroundRune rune in GroundRunes.Runes)
        {
            if (rune.State == GroundRuneState.Network)
                runes.Add(rune);
        }
        runes.Sort((left, right) => left.Position.X.CompareTo(right.Position.X));
        if (runes.Count < 2)
            return;

        var hitEnemies = new HashSet<Enemy>();
        bool hitBoss = false;
        for (int index = 0; index < runes.Count - 1; index++)
        {
            Rectangle line = CreateRuneLineBounds(
                runes[index].Position,
                runes[index + 1].Position);
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted || hitEnemies.Contains(enemy) ||
                    !line.Intersects(enemy.MeleeTargetBounds))
                    continue;
                hitEnemies.Add(enemy);
                int bonus = (int)MathF.Round(enemy.ArcaneImprintCount *
                    SpellbladeTuning.ConvergenceImprintDamagePerStack);
                enemy.ReceiveDamage(
                    SpellbladeTuning.ConvergenceTickDamage + bonus,
                    SpellbladeTuning.ConvergenceTickPoise);
            }
            if (!hitBoss && ShouldRenderBoss && Boss.IsAlive &&
                line.Intersects(Boss.Bounds))
            {
                hitBoss = true;
                int bonus = (int)MathF.Round(Boss.ArcaneImprintCount *
                    SpellbladeTuning.ConvergenceImprintDamagePerStack);
                Boss.ReceiveDamage(
                    SpellbladeTuning.ConvergenceTickDamage + bonus,
                    SpellbladeTuning.ConvergenceTickPoise);
            }
        }
    }

    private static Rectangle CreateRuneLineBounds(Vector2 start, Vector2 end)
    {
        float half = SpellbladeTuning.ConvergenceLineThickness / 2f;
        int left = (int)MathF.Floor(MathF.Min(start.X, end.X));
        int right = (int)MathF.Ceiling(MathF.Max(start.X, end.X));
        int top = (int)MathF.Floor(MathF.Min(start.Y, end.Y) - half);
        int bottom = (int)MathF.Ceiling(MathF.Max(start.Y, end.Y) + half);
        return new Rectangle(left, top, Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private void ApplyRuneBlasts(List<Vector2> positions)
    {
        for (int index = 0; index < positions.Count; index++)
        {
            float multiplier = index switch
            {
                0 => 1f,
                1 => SpellbladeTuning.SecondRuneDamageMultiplier,
                _ => SpellbladeTuning.ThirdRuneDamageMultiplier
            };
            float radiusSquared = SpellbladeTuning.RuneBlastRadius *
                SpellbladeTuning.RuneBlastRadius;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted ||
                    Vector2.DistanceSquared(positions[index], enemy.Position) >
                        radiusSquared)
                    continue;
                enemy.ReceiveDamage(
                    Math.Max(1, (int)MathF.Round(
                        SpellbladeTuning.RuneBlastDamage * multiplier)),
                    SpellbladeTuning.RuneBlastPoise * multiplier);
            }
            if (ShouldRenderBoss && Boss.IsAlive &&
                Vector2.DistanceSquared(positions[index], Boss.Position) <=
                    radiusSquared)
            {
                Boss.ReceiveDamage(
                    Math.Max(1, (int)MathF.Round(
                        SpellbladeTuning.RuneBlastDamage * multiplier)),
                    SpellbladeTuning.RuneBlastPoise * multiplier);
            }
        }
    }

    private static bool IsRaiderTechnique(WeaponTechniqueEffect effect) =>
        effect is WeaponTechniqueEffect.RaiderSavageCleave or
            WeaponTechniqueEffect.RaiderHookingAxe or
            WeaponTechniqueEffect.RaiderBucklerRam or
            WeaponTechniqueEffect.RaiderRavagersRush or
            WeaponTechniqueEffect.RaiderExecutionersGrip or
            WeaponTechniqueEffect.RaiderSkullbreaker or
            WeaponTechniqueEffect.RaiderCrowdCrusher or
            WeaponTechniqueEffect.RaiderWarbringersDominion;

    private float GetRaiderDamageMultiplier(
        WeaponTechnique technique,
        bool unstable,
        bool wallImpacted)
    {
        if (technique == null || !unstable)
        {
            return technique?.Effect ==
                    WeaponTechniqueEffect.RaiderWarbringersDominion &&
                Player.Combat.TechniqueStageIndex == 3 && wallImpacted
                    ? 1f + RaiderTuning.DominionWallDamageBonus
                    : 1f;
        }

        int stage = Player.Combat.TechniqueStageIndex;
        float multiplier = technique.Effect switch
        {
            WeaponTechniqueEffect.RaiderExecutionersGrip when stage == 2 =>
                1f + RaiderTuning.ExecutionerUnstableDamageBonus,
            WeaponTechniqueEffect.RaiderSkullbreaker when stage == 1 =>
                1f + RaiderTuning.SkullbreakerUnstableDamageBonus,
            WeaponTechniqueEffect.RaiderWarbringersDominion when stage == 3 =>
                1f + RaiderTuning.DominionFinalUnstableDamageBonus,
            _ => 1f
        };
        if (technique.Effect ==
                WeaponTechniqueEffect.RaiderWarbringersDominion &&
            stage == 3 && wallImpacted)
        {
            multiplier += RaiderTuning.DominionWallDamageBonus;
        }
        return multiplier;
    }

    private float GetRaiderPoiseMultiplier(
        WeaponTechnique technique,
        bool unstable)
    {
        if (technique == null)
            return 1f;

        int stage = Player.Combat.TechniqueStageIndex;
        if (technique.Effect == WeaponTechniqueEffect.RaiderSavageCleave &&
            stage == 2 && unstable)
            return 1.25f;
        if (technique.Effect ==
                WeaponTechniqueEffect.RaiderExecutionersGrip &&
            stage == 2 && unstable)
            return 1f + RaiderTuning.ExecutionerUnstablePoiseBonus;
        if (technique.Effect == WeaponTechniqueEffect.RaiderSkullbreaker &&
            stage == 1 && unstable)
            return 1.25f;
        if (technique.Effect ==
                WeaponTechniqueEffect.RaiderWarbringersDominion &&
            stage == 3 && unstable)
            return 1.20f;
        return 1f;
    }

    private void ResolveRaiderEnemyHit(
        Enemy enemy,
        WeaponTechnique technique,
        bool wasUnstable)
    {
        if (enemy == null || technique == null || !enemy.CanBeTargeted)
            return;

        int stage = Player.Combat.TechniqueStageIndex;
        float facing = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
        EnemyDisplacementResult displacement = default;
        bool createsCollisionImpact = false;

        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.RaiderSavageCleave:
                displacement = enemy.DisplaceHorizontally(
                    facing,
                    stage == 2 ? 32f : 10f + stage * 5f,
                    CurrentDungeon);
                createsCollisionImpact = stage == 2;
                break;

            case WeaponTechniqueEffect.RaiderHookingAxe:
                if (enemy.WeightClass == EnemyWeightClass.Heavy)
                {
                    displacement = enemy.DisplaceHorizontally(
                        -facing,
                        16f,
                        CurrentDungeon);
                    Player.PullToward(
                        enemy.Position,
                        RaiderTuning.HeavyHookPlayerAdvance,
                        CurrentDungeon);
                }
                else
                {
                    displacement = enemy.PullTowardSafe(
                        Player.Position,
                        RaiderTuning.HookPullDistance,
                        RaiderTuning.HookSafeDistance,
                        CurrentDungeon);
                }
                break;

            case WeaponTechniqueEffect.RaiderBucklerRam:
                displacement = enemy.DisplaceHorizontally(
                    facing,
                    RaiderTuning.BucklerRamPushDistance,
                    CurrentDungeon);
                createsCollisionImpact = true;
                break;

            case WeaponTechniqueEffect.RaiderRavagersRush:
                displacement = enemy.DisplaceHorizontally(
                    facing,
                    RaiderTuning.RavagersRushPushDistance,
                    CurrentDungeon);
                if (wasUnstable)
                    enemy.ApplyRaiderLaunch(-235f);
                createsCollisionImpact = true;
                break;

            case WeaponTechniqueEffect.RaiderExecutionersGrip:
                if (stage == 1)
                {
                    displacement = enemy.PullTowardSafe(
                        Player.Position,
                        RaiderTuning.ExecutionerRepositionDistance,
                        RaiderTuning.HookSafeDistance,
                        CurrentDungeon);
                }
                else if (stage == 2)
                {
                    displacement = enemy.DisplaceHorizontally(
                        facing,
                        28f,
                        CurrentDungeon);
                }
                break;

            case WeaponTechniqueEffect.RaiderSkullbreaker:
                displacement = enemy.DisplaceHorizontally(
                    facing,
                    stage == 0 ? 24f : 46f,
                    CurrentDungeon);
                createsCollisionImpact = stage == 1;
                if (stage == 1 && wasUnstable)
                    enemy.ApplyRaiderLaunch(-180f);
                break;

            case WeaponTechniqueEffect.RaiderCrowdCrusher:
                if (enemy.WeightClass == EnemyWeightClass.Heavy)
                {
                    displacement = enemy.DisplaceHorizontally(
                        Player.Combat.RaiderControlDirection,
                        stage == 2 ? 28f : 8f,
                        CurrentDungeon);
                }
                else if (stage == 0)
                {
                    displacement = enemy.PullTowardSafe(
                        Player.Position,
                        48f,
                        RaiderTuning.HookSafeDistance,
                        CurrentDungeon);
                }
                else if (stage == 2)
                {
                    displacement = enemy.DisplaceHorizontally(
                        Player.Combat.RaiderControlDirection,
                        RaiderTuning.CrowdCrusherThrowDistance,
                        CurrentDungeon);
                    createsCollisionImpact = true;
                }
                break;

            case WeaponTechniqueEffect.RaiderWarbringersDominion:
                if (stage == 0)
                {
                    float movementMultiplier = enemy.WeightClass ==
                        EnemyWeightClass.Heavy
                            ? .85f
                            : RaiderTuning.WarCryMovementMultiplier;
                    enemy.ApplyControl(
                        movementMultiplier,
                        RaiderTuning.WarCryHesitationSeconds);
                }
                else if (stage == 1)
                {
                    Vector2 gatherPoint = Player.Position +
                        new Vector2(
                            facing * RaiderTuning.DominionGatherZoneDistance,
                            0f);
                    displacement = enemy.PullTowardSafe(
                        gatherPoint,
                        RaiderTuning.GatheringPullDistance,
                        18f,
                        CurrentDungeon);
                }
                else if (stage == 2)
                {
                    displacement = enemy.DisplaceHorizontally(
                        facing,
                        RaiderTuning.DominionPushDistance,
                        CurrentDungeon);
                    createsCollisionImpact = true;
                }
                else
                {
                    displacement = enemy.DisplaceHorizontally(
                        facing,
                        38f,
                        CurrentDungeon);
                    createsCollisionImpact = true;
                }
                break;
        }

        if (createsCollisionImpact)
        {
            ResolveRaiderDisplacementCollision(
                enemy,
                displacement,
                displacement.EndPosition.X - displacement.StartPosition.X);
        }
    }

    private void ResolveRaiderDisplacementCollision(
        Enemy displaced,
        EnemyDisplacementResult displacement,
        float direction)
    {
        if (displaced == null || displacement.RequestedDistance <= 0f)
            return;

        if (displacement.HitWall && displaced.ApplyDisplacementImpact(
                RaiderTuning.WallCollisionPoise,
                RaiderTuning.WallCollisionDamage))
        {
            _raiderWallImpactedEnemies.Add(displaced);
            RegisterPlayerHitEffect(displaced.MeleeTargetBounds);
            Camera.AddImpactFeedback(2.5f);
        }

        Rectangle swept = CreateEnemyDisplacementBounds(
            displaced,
            displacement.StartPosition,
            displacement.EndPosition);
        float resolvedDirection = MathF.Abs(direction) > .1f
            ? MathF.Sign(direction)
            : Player.Combat.RaiderControlDirection;

        foreach (Enemy other in Enemies.Enemies)
        {
            if (ReferenceEquals(other, displaced) || !other.CanBeTargeted ||
                !swept.Intersects(other.MeleeTargetBounds))
                continue;

            bool inTravelDirection = resolvedDirection > 0f
                ? other.Position.X >= displacement.StartPosition.X
                : other.Position.X <= displacement.StartPosition.X;
            if (!inTravelDirection)
                continue;

            displaced.ApplyDisplacementImpact(
                RaiderTuning.EnemyCollisionPoise);
            other.ApplyDisplacementImpact(
                RaiderTuning.EnemyCollisionPoise);
            other.DisplaceHorizontally(
                resolvedDirection,
                RaiderTuning.EnemyCollisionNudge,
                CurrentDungeon);
            displaced.DisplaceHorizontally(
                -resolvedDirection,
                6f,
                CurrentDungeon);
            RegisterPlayerHitEffect(other.MeleeTargetBounds);
            break;
        }
    }

    private static Rectangle CreateEnemyDisplacementBounds(
        Enemy enemy,
        Vector2 start,
        Vector2 end)
    {
        float halfWidth = enemy.Size.X / 2f + 5f;
        float halfHeight = enemy.Size.Y / 2f;
        int left = (int)MathF.Floor(MathF.Min(start.X, end.X) - halfWidth);
        int right = (int)MathF.Ceiling(MathF.Max(start.X, end.X) + halfWidth);
        int top = (int)MathF.Floor(MathF.Min(start.Y, end.Y) - halfHeight);
        int bottom = (int)MathF.Ceiling(MathF.Max(start.Y, end.Y) + halfHeight);
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private bool IsPrimaryFinalWaltzTarget(Vector2 candidatePosition)
    {
        float candidateDistance = Vector2.DistanceSquared(
            Player.Position,
            candidatePosition);

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted || enemy.Position == candidatePosition)
                continue;

            if (Vector2.DistanceSquared(Player.Position, enemy.Position) <
                candidateDistance)
            {
                return false;
            }
        }

        return !ShouldRenderBoss || !Boss.IsAlive ||
            Boss.Position == candidatePosition ||
            Vector2.DistanceSquared(Player.Position, Boss.Position) >=
                candidateDistance;
    }

    private void SpawnPlayerProjectile(AttackDefinition attack)
    {
        if (_spawnedPlayerProjectileAttackId == Player.Combat.AttackId ||
            attack.Projectile == null)
            return;

        _spawnedPlayerProjectileAttackId = Player.Combat.AttackId;
        float facing = Player.Facing == DungeonAscendant.Player.FacingDirection.Left
            ? -1f
            : 1f;
        WeaponTechniqueEffect effect = Player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (effect == WeaponTechniqueEffect.RangerHeavensFury)
            facing = Player.Combat.RangerLockedDirection;
        Vector2 direction = new(facing, 0f);

        direction = effect switch
        {
            WeaponTechniqueEffect.RangerSkyfallMarker =>
                GetSkyfallDirection(Player.Combat.RangerSkyfallDistance, facing),
            WeaponTechniqueEffect.RangerFallingStar =>
                Vector2.Normalize(new Vector2(facing * .48f, -.88f)),
            _ => direction
        };

        Vector2 spawnPosition = Player.Position + direction * 30f +
            new Vector2(0f, -10f);
        int roomId = CurrentDungeon.FindRoomContaining(Player.Position)?.Id ?? -1;

        if (effect is WeaponTechniqueEffect.RangerThreefoldHunt or
            WeaponTechniqueEffect.RangerPredatorsHorizon)
        {
            SpawnRangerSpread(attack, effect, spawnPosition, facing, roomId);
            return;
        }

        float focusRatio = effect == WeaponTechniqueEffect.RangerHeavensFury
            ? Player.Combat.RangerUltimateFocusRatio
            : Player.Combat.Resources.HunterFocusRatio;
        int damage = Player.Combat.CalculateDamage(Player.MeleeDamage);
        float poiseDamage = Player.Combat.CalculatePoiseDamage();
        if (effect == WeaponTechniqueEffect.RangerFallingStar)
        {
            damage = Math.Max(1, (int)MathF.Round(damage *
                (1f + focusRatio * RangerTuning.FallingStarMaximumDamageBonus)));
            poiseDamage *= 1f + focusRatio *
                RangerTuning.FallingStarMaximumPoiseBonus;
        }

        Projectiles.SpawnPlayerProjectile(
            attack.Projectile,
            spawnPosition,
            direction,
            Player.Combat.ProjectileSpeedMultiplier,
            damage,
            poiseDamage,
            Player.Combat.CalculateKnockback(),
            roomId,
            effect,
            Player.Combat.CurrentTechnique?.ResourceGainOnHit ?? 0,
            Player.Combat.RangerChargeRatio,
            focusRatio,
            Player.Combat.RangerDrawState,
            Player.Combat.AttackId,
            Player.Combat.CurrentTechniqueUseId,
            additionalPierces: effect == WeaponTechniqueEffect.RangerHuntersDraw &&
                Player.Combat.RangerDrawState == RangerDrawState.Perfect ? 1 : 0,
            placementDistance: effect == WeaponTechniqueEffect.SpellbladeRuneBrand
                ? SpellbladeTuning.GetPlacementDistance(
                    Player.Combat.RunePlacementDistance)
                : 0f);
    }

    private void SpawnRangerSpread(
        AttackDefinition attack,
        WeaponTechniqueEffect effect,
        Vector2 spawnPosition,
        float facing,
        int roomId)
    {
        float focusRatio = Player.Combat.Resources.HunterFocusRatio;
        int count = effect == WeaponTechniqueEffect.RangerThreefoldHunt
            ? 3
            : RangerTuning.PredatorFanArrowCount;
        float maximumAngle = effect == WeaponTechniqueEffect.RangerThreefoldHunt
            ? MathHelper.Lerp(
                RangerTuning.ThreefoldBaseAngleRadians,
                RangerTuning.ThreefoldFocusedAngleRadians,
                focusRatio)
            : .27f;
        int centerIndex = count / 2;
        int baseDamage = Player.Combat.CalculateDamage(Player.MeleeDamage);

        if (effect == WeaponTechniqueEffect.RangerPredatorsHorizon &&
            _predatorFanTechniqueUseId != Player.Combat.CurrentTechniqueUseId)
        {
            _predatorFanTechniqueUseId = Player.Combat.CurrentTechniqueUseId;
            _predatorFanHits.Clear();
            _predatorBossFanHits = 0;
        }

        for (int index = 0; index < count; index++)
        {
            float spread = count == 1
                ? 0f
                : MathHelper.Lerp(-maximumAngle, maximumAngle,
                    index / (float)(count - 1));
            Vector2 direction = new(
                MathF.Cos(spread) * facing,
                MathF.Sin(spread));
            int damage = baseDamage;
            if (effect == WeaponTechniqueEffect.RangerThreefoldHunt &&
                index == centerIndex)
            {
                damage = Math.Max(1, (int)MathF.Round(damage *
                    (1f + focusRatio *
                        RangerTuning.ThreefoldFocusedCenterDamageBonus)));
            }

            Projectiles.SpawnPlayerProjectile(
                attack.Projectile,
                spawnPosition + new Vector2(0f, (index - centerIndex) * 3f),
                direction,
                Player.Combat.ProjectileSpeedMultiplier,
                damage,
                Player.Combat.CalculatePoiseDamage(),
                Player.Combat.CalculateKnockback(),
                roomId,
                effect,
                Player.Combat.CurrentTechnique?.ResourceGainOnHit ?? 0,
                focusRatioAtFire: focusRatio,
                sourceAttackId: Player.Combat.AttackId,
                techniqueUseId: Player.Combat.CurrentTechniqueUseId,
                spreadArrowIndex: index);
        }
    }

    private static Vector2 GetSkyfallDirection(
        SkyfallDistance distance,
        float facing)
    {
        return distance switch
        {
            SkyfallDistance.Near => Vector2.Normalize(
                new Vector2(facing * .21f, -.978f)),
            SkyfallDistance.Far => Vector2.Normalize(
                new Vector2(facing * .555f, -.832f)),
            _ => Vector2.Normalize(new Vector2(facing * .355f, -.935f))
        };
    }

    private void ProcessPlayerProjectileHits()
    {
        _pendingArrowRain.Clear();
        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (!projectile.IsPlayerOwned || projectile.LifetimeRemaining <= 0f)
                continue;

            bool consumed = false;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted ||
                    projectile.HitEnemies.Contains(enemy) ||
                    !projectile.SweptBounds.Intersects(enemy.MeleeTargetBounds))
                    continue;

                if (projectile.IsRangerProjectile)
                {
                    if (projectile.ImpactRadius > 0f)
                    {
                        if (projectile.Trajectory ==
                                ProjectileTrajectoryType.ArrowRain &&
                            !projectile.HasSplit)
                        {
                            _pendingArrowRain.Add(new ProjectileImpact(
                                projectile,
                                projectile.Position));
                        }
                        ResolveRangerImpact(projectile, projectile.Position);
                        consumed = true;
                        break;
                    }

                    projectile.HitEnemies.Add(enemy);
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        enemy.MeleeTargetBounds);
                    int rangerDamage = CalculateRangerProjectileDamage(
                        projectile,
                        enemy.PhysicalDamageTakenMultiplier,
                        enemy.MeleeTargetBounds,
                        enemy.IsHeavyPullAnchor,
                        isBoss: false);
                    float poiseDamage = CalculateRangerProjectilePoise(
                        projectile,
                        enemy,
                        isBoss: false);
                    enemy.ReceiveDamage(
                        rangerDamage,
                        poiseDamage);
                    enemy.ApplyKnockback(
                        projectile.SourcePosition,
                        projectile.Knockback,
                        CurrentDungeon);
                    RecordPredatorFanHit(projectile, enemy);
                    RegisterRangerHit(projectile, enemy.Position);
                    consumed = !AdvancePenetratingProjectile(projectile);
                    if (consumed)
                        break;
                    continue;
                }

                if (projectile.IsSpellbladeProjectile)
                {
                    projectile.HitEnemies.Add(enemy);
                    projectile.HasHitTarget = true;
                    bool alreadyMarked = enemy.ArcaneImprintCount > 0;
                    float spellbladeMultiplier = projectile.TechniqueEffect ==
                            WeaponTechniqueEffect.SpellbladeRunicSpear &&
                        alreadyMarked
                            ? 1f + SpellbladeTuning.RunicSpearMarkedDamageBonus
                            : 1f;
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        enemy.MeleeTargetBounds);
                    enemy.ReceiveDamage(
                        Math.Max(1, (int)MathF.Round(
                            projectile.Damage * spellbladeMultiplier)),
                        projectile.PoiseDamage);
                    enemy.AddArcaneImprint();
                    Player.Combat.RegisterExternalHit(
                        projectile.WeaponResourceGain);
                    consumed = projectile.TechniqueEffect !=
                            WeaponTechniqueEffect.SpellbladeRunicSpear ||
                        !AdvancePenetratingProjectile(projectile);
                    if (consumed)
                        break;
                    continue;
                }

                RegisterPlayerHitEffect(projectile.Bounds, enemy.MeleeTargetBounds);
                float damageMultiplier = projectile.Type == ProjectileType.Arrow
                    ? enemy.PhysicalDamageTakenMultiplier
                    : 1f;
                if (projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.ConsumeHunterMark && enemy.IsHunterMarked)
                    damageMultiplier *= 1.45f;
                enemy.ReceiveDamage(
                    System.Math.Max(0, (int)System.MathF.Round(
                        projectile.Damage * damageMultiplier)),
                    projectile.PoiseDamage);
                ApplyTechniqueEffect(enemy, projectile.TechniqueEffect);
                enemy.ApplyKnockback(
                    projectile.SourcePosition,
                    projectile.Knockback,
                    CurrentDungeon);
                Player.Combat.RegisterExternalHit(projectile.WeaponResourceGain);
                consumed = true;
                break;
            }

            if (!consumed && ShouldRenderBoss && Boss.IsAlive &&
                !projectile.HasHitBoss &&
                projectile.SweptBounds.Intersects(Boss.Bounds))
            {
                if (projectile.IsRangerProjectile && projectile.ImpactRadius > 0f)
                {
                    if (projectile.Trajectory ==
                            ProjectileTrajectoryType.ArrowRain &&
                        !projectile.HasSplit)
                    {
                        _pendingArrowRain.Add(new ProjectileImpact(
                            projectile,
                            projectile.Position));
                    }
                    ResolveRangerImpact(projectile, projectile.Position);
                    consumed = true;
                }
                else if (projectile.IsRangerProjectile)
                {
                    projectile.HasHitBoss = true;
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        Boss.Bounds);
                    int rangerDamage = CalculateRangerProjectileDamage(
                        projectile,
                        1f,
                        Boss.Bounds,
                        isLarge: true,
                        isBoss: true);
                    Boss.ReceiveDamage(
                        rangerDamage,
                        CalculateRangerProjectilePoise(
                            projectile,
                            enemy: null,
                            isBoss: true));
                    Boss.ApplyKnockback(
                        projectile.SourcePosition,
                        projectile.Knockback,
                        CurrentDungeon);
                    RecordPredatorBossFanHit(projectile);
                    RegisterRangerHit(projectile, Boss.Position);
                    consumed = !AdvancePenetratingProjectile(projectile);
                }
                else if (projectile.IsSpellbladeProjectile)
                {
                    projectile.HasHitBoss = true;
                    projectile.HasHitTarget = true;
                    bool alreadyMarked = Boss.ArcaneImprintCount > 0;
                    float spellbladeMultiplier = projectile.TechniqueEffect ==
                            WeaponTechniqueEffect.SpellbladeRunicSpear &&
                        alreadyMarked
                            ? 1f + SpellbladeTuning.RunicSpearMarkedDamageBonus
                            : 1f;
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        Boss.Bounds);
                    Boss.ReceiveDamage(
                        Math.Max(1, (int)MathF.Round(
                            projectile.Damage * spellbladeMultiplier)),
                        projectile.PoiseDamage);
                    Boss.AddArcaneImprint();
                    Player.Combat.RegisterExternalHit(
                        projectile.WeaponResourceGain);
                    consumed = projectile.TechniqueEffect !=
                            WeaponTechniqueEffect.SpellbladeRunicSpear ||
                        !AdvancePenetratingProjectile(projectile);
                }
                else
                {
                RegisterPlayerHitEffect(projectile.Bounds, Boss.Bounds);
                Boss.ReceiveDamage(projectile.Damage, projectile.PoiseDamage);
                Boss.ApplyKnockback(
                    projectile.SourcePosition,
                    projectile.Knockback,
                    CurrentDungeon);
                Player.Combat.RegisterExternalHit(projectile.WeaponResourceGain);
                consumed = true;
                }
            }

            if (consumed)
                projectile.LifetimeRemaining = 0f;
        }

        foreach (ProjectileImpact rain in _pendingArrowRain)
            Projectiles.SpawnArrowRainAtImpact(rain.Projectile, rain.Position);
    }

    private void ProcessPlayerProjectileImpacts()
    {
        foreach (ProjectileImpact impact in Projectiles.Impacts)
        {
            if (!impact.Projectile.IsPlayerOwned)
                continue;

            if (impact.Projectile.TechniqueEffect ==
                WeaponTechniqueEffect.SpellbladeRuneBrand)
            {
                GroundRunes.TryPlace(
                    impact.Position,
                    impact.Projectile.RoomId,
                    CurrentDungeon);
                continue;
            }

            if (!impact.Projectile.IsRangerProjectile)
                continue;

            ResolveRangerImpact(impact.Projectile, impact.Position);
        }
    }

    private void ProcessSpellbladeRunePlacement()
    {
        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (projectile.LifetimeRemaining <= 0f ||
                projectile.HasHitTarget ||
                projectile.TechniqueEffect !=
                    WeaponTechniqueEffect.SpellbladeRuneBrand ||
                projectile.PlacementDistance <= 0f)
                continue;

            if (MathF.Abs(projectile.Position.X - projectile.SourcePosition.X) +
                    .01f < projectile.PlacementDistance)
                continue;

            if (GroundRunes.TryPlace(
                    projectile.Position,
                    projectile.RoomId,
                    CurrentDungeon))
            {
                projectile.LifetimeRemaining = 0f;
            }
        }
    }

    private void ResolveRangerImpact(Projectile projectile, Vector2 position)
    {
        float radius = projectile.ImpactRadius;
        if (radius <= 0f)
            return;

        Rectangle impactBounds = new(
            (int)MathF.Floor(position.X - radius),
            (int)MathF.Floor(position.Y - radius),
            Math.Max(1, (int)MathF.Ceiling(radius * 2f)),
            Math.Max(1, (int)MathF.Ceiling(radius * 2f)));

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted || projectile.HitEnemies.Contains(enemy) ||
                !impactBounds.Intersects(enemy.MeleeTargetBounds))
                continue;

            projectile.HitEnemies.Add(enemy);
            float distance = Vector2.Distance(position, enemy.Position);
            float blastMultiplier = distance <= radius * .45f ? 1f : .58f;
            int damage = Math.Max(1, (int)MathF.Round(
                CalculateRangerProjectileDamage(
                    projectile,
                    enemy.PhysicalDamageTakenMultiplier,
                    enemy.MeleeTargetBounds,
                    enemy.IsHeavyPullAnchor,
                    isBoss: false) * blastMultiplier));
            enemy.ReceiveDamage(
                damage,
                CalculateRangerProjectilePoise(
                    projectile,
                    enemy,
                    isBoss: false) * blastMultiplier);
            enemy.ApplyKnockback(position, projectile.Knockback, CurrentDungeon);
            RegisterPlayerHitEffect(impactBounds, enemy.MeleeTargetBounds);
            RegisterRangerHit(projectile, enemy.Position);
        }

        if (!projectile.HasHitBoss && ShouldRenderBoss && Boss.IsAlive &&
            impactBounds.Intersects(Boss.Bounds))
        {
            projectile.HasHitBoss = true;
            float distance = Vector2.Distance(position, Boss.Position);
            float blastMultiplier = distance <= radius * .45f ? 1f : .58f;
            int damage = Math.Max(1, (int)MathF.Round(
                CalculateRangerProjectileDamage(
                    projectile,
                    1f,
                    Boss.Bounds,
                    isLarge: true,
                    isBoss: true) * blastMultiplier));
            Boss.ReceiveDamage(
                damage,
                CalculateRangerProjectilePoise(
                    projectile,
                    enemy: null,
                    isBoss: true) * blastMultiplier);
            Boss.ApplyKnockback(position, projectile.Knockback, CurrentDungeon);
            RegisterPlayerHitEffect(impactBounds, Boss.Bounds);
            RegisterRangerHit(projectile, Boss.Position);
        }
    }

    private int CalculateRangerProjectileDamage(
        Projectile projectile,
        float physicalDamageMultiplier,
        Rectangle targetBounds,
        bool isLarge,
        bool isBoss)
    {
        float multiplier = physicalDamageMultiplier;

        if (projectile.TechniqueEffect ==
            WeaponTechniqueEffect.RangerDragonPiercer &&
            projectile.TargetsHit > 0)
        {
            float loss = RangerTuning.DragonPiercerDamageLossPerTarget -
                projectile.FocusRatioAtFire *
                    RangerTuning.DragonPiercerMaximumFocusFalloffReduction;
            multiplier *= MathF.Max(
                RangerTuning.DragonPiercerMinimumDamageMultiplier,
                1f - loss * projectile.TargetsHit);
        }

        if (projectile.TechniqueEffect ==
            WeaponTechniqueEffect.RangerThreefoldHunt &&
            projectile.SpreadArrowIndex >= 0 &&
            projectile.SpreadArrowIndex != 1 &&
            (isLarge || isBoss))
        {
            multiplier *= RangerTuning.ThreefoldSecondaryLargeTargetMultiplier;
        }

        if (projectile.Trajectory ==
            ProjectileTrajectoryType.MassivePenetrating &&
            !IsMassiveCenterLine(projectile, targetBounds))
        {
            multiplier *= .68f;
        }

        return Math.Max(1, (int)MathF.Round(projectile.Damage * multiplier));
    }

    private float CalculateRangerProjectilePoise(
        Projectile projectile,
        Enemy enemy,
        bool isBoss)
    {
        float multiplier = 1f;
        if (projectile.TechniqueEffect ==
            WeaponTechniqueEffect.RangerPredatorsHorizon &&
            projectile.SpreadArrowIndex < 0 &&
            projectile.TechniqueUseId == _predatorFanTechniqueUseId)
        {
            int hits = isBoss
                ? _predatorBossFanHits
                : enemy != null && _predatorFanHits.TryGetValue(enemy, out int count)
                    ? count
                    : 0;
            multiplier += Math.Min(
                RangerTuning.PredatorFanHitBonusCap,
                hits) * RangerTuning.PredatorPoiseBonusPerFanHit;
        }

        if (projectile.TechniqueEffect ==
            WeaponTechniqueEffect.RangerThreefoldHunt &&
            projectile.SpreadArrowIndex >= 0 &&
            projectile.SpreadArrowIndex != 1 &&
            (isBoss || enemy?.IsHeavyPullAnchor == true))
        {
            multiplier *= RangerTuning.ThreefoldSecondaryLargeTargetMultiplier;
        }

        return projectile.PoiseDamage * multiplier;
    }

    private void RegisterRangerHit(Projectile projectile, Vector2 targetPosition)
    {
        projectile.HasHitTarget = true;
        projectile.TargetsHit++;
        Player.Combat.RegisterRangerProjectileHit(
            projectile.SourceAttackId,
            projectile.TechniqueEffect,
            projectile.DrawState,
            Vector2.Distance(projectile.SourcePosition, targetPosition) >=
                RangerTuning.SafeSpacingDistance);
        bool ultimate = projectile.TechniqueEffect ==
            WeaponTechniqueEffect.RangerHeavensFury;
        Player.Combat.RegisterRangerImpactFeedback(
            strongHit: ultimate || projectile.DrawState == RangerDrawState.Perfect ||
                projectile.TechniqueEffect is
                    WeaponTechniqueEffect.RangerDragonPiercer or
                    WeaponTechniqueEffect.RangerFallingStar,
            ultimate);
        if (ultimate || projectile.DrawState == RangerDrawState.Perfect)
        {
            Camera.AddImpactFeedback(ultimate ? 4.8f : 1.5f);
        }
        Player.Combat.RegisterExternalHit(projectile.WeaponResourceGain);
    }

    private void RecordPredatorFanHit(Projectile projectile, Enemy enemy)
    {
        if (projectile.TechniqueEffect !=
                WeaponTechniqueEffect.RangerPredatorsHorizon ||
            projectile.SpreadArrowIndex < 0 ||
            projectile.TechniqueUseId != _predatorFanTechniqueUseId)
            return;

        _predatorFanHits.TryGetValue(enemy, out int hits);
        _predatorFanHits[enemy] = Math.Min(
            RangerTuning.PredatorFanHitBonusCap,
            hits + 1);
    }

    private void RecordPredatorBossFanHit(Projectile projectile)
    {
        if (projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerPredatorsHorizon &&
            projectile.SpreadArrowIndex >= 0 &&
            projectile.TechniqueUseId == _predatorFanTechniqueUseId)
        {
            _predatorBossFanHits = Math.Min(
                RangerTuning.PredatorFanHitBonusCap,
                _predatorBossFanHits + 1);
        }
    }

    private static bool IsMassiveCenterLine(
        Projectile projectile,
        Rectangle targetBounds)
    {
        float lineY = (projectile.PreviousPosition.Y + projectile.Position.Y) * .5f;
        float halfBand = MathF.Max(5f, targetBounds.Height * .22f);
        return MathF.Abs(lineY - targetBounds.Center.Y) <= halfBand;
    }

    private static bool AdvancePenetratingProjectile(Projectile projectile)
    {
        if (projectile.RemainingPierces <= 0)
            return false;

        projectile.RemainingPierces--;
        return true;
    }

    private void ApplyTechniqueEffect(
        Enemy enemy,
        WeaponTechniqueEffect effect)
    {
        if (enemy == null)
            return;

        switch (effect)
        {
            case WeaponTechniqueEffect.HunterMark:
                enemy.ApplyHunterMark();
                break;
            case WeaponTechniqueEffect.ConsumeHunterMark:
                enemy.ConsumeHunterMark();
                break;
            case WeaponTechniqueEffect.BrokenArmor:
                enemy.ApplyBrokenArmor();
                break;
            case WeaponTechniqueEffect.DefenseWeaken:
                enemy.ApplyDefenseWeakening(1.10f, 4f);
                break;
            case WeaponTechniqueEffect.GravityControl:
                enemy.ApplyControl(.58f, 1.25f);
                enemy.PullToward(Player.Position, 18f, CurrentDungeon);
                break;
        }
    }

    private CombatInput CreateCombatInput(
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        bool skill1Held = keyboardState.IsKeyDown(Keys.J) ||
            mouseState.LeftButton == ButtonState.Pressed;
        bool skill1WasHeld = _previousKeyboardState.IsKeyDown(Keys.J) ||
            _previousMouseState.LeftButton == ButtonState.Pressed;
        bool skill1Pressed = skill1Held && !skill1WasHeld;
        bool skill1Released = !skill1Held && skill1WasHeld;
        bool skill2Held = keyboardState.IsKeyDown(Keys.K) ||
            mouseState.RightButton == ButtonState.Pressed;
        bool skill2WasHeld = _previousKeyboardState.IsKeyDown(Keys.K) ||
            _previousMouseState.RightButton == ButtonState.Pressed;
        bool skill2Pressed = skill2Held && !skill2WasHeld;
        bool skill2Released = !skill2Held && skill2WasHeld;
        bool skill3Held = keyboardState.IsKeyDown(Keys.L);
        bool skill3WasHeld = _previousKeyboardState.IsKeyDown(Keys.L);
        bool skill3Pressed = skill3Held && !skill3WasHeld;
        bool skill3Released = !skill3Held && skill3WasHeld;
        bool dodgePressed = WasKeyPressed(keyboardState, Keys.LeftShift) ||
            WasKeyPressed(keyboardState, Keys.RightShift);
        bool dashHeld = keyboardState.IsKeyDown(Keys.LeftShift) ||
            keyboardState.IsKeyDown(Keys.RightShift);
        bool blockHeld = keyboardState.IsKeyDown(Keys.LeftControl);
        float horizontalDirection = 0f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            horizontalDirection -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            horizontalDirection += 1f;

        return new CombatInput(
            skill1Pressed,
            skill2Pressed,
            skill3Pressed,
            dodgePressed,
            dashHeld,
            blockHeld,
            horizontalDirection,
            HasImmediateDuelistThreat(),
            skill1Held,
            skill1Released,
            skill3Held,
            skill3Released,
            skill2Held,
            skill2Released);
    }

    private bool HasImmediateDuelistThreat()
    {
        if (Player.WeaponFamily != WeaponFamily.DualSwords)
            return false;

        Rectangle opportunity = Player.Bounds;
        opportunity.Inflate(54, 18);

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.CanBeTargeted && enemy.Attack.IsActive &&
                opportunity.Intersects(enemy.AttackArea))
            {
                return true;
            }
        }

        if (ShouldRenderBoss && Boss.IsAlive && Boss.Attack.IsActive &&
            opportunity.Intersects(Boss.AttackArea))
        {
            return true;
        }

        foreach (Projectile projectile in Projectiles.Projectiles)
        {
            if (!projectile.IsPlayerOwned &&
                projectile.LifetimeRemaining > 0f &&
                opportunity.Intersects(projectile.Bounds))
            {
                return true;
            }
        }

        return false;
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
