using System;
using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Lore;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.Player;
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
#if DEBUG || PLAYTEST_BUILD
    public const bool DeveloperModeEnabled = true;
#else
    public const bool DeveloperModeEnabled = false;
#endif
    public const bool DebugCombatHitboxes = false;
    public static bool DebugWildForestShowcase { get; set; } = false;

    private const float ExitInteractionRadius = 72f;
    private const float WorldTierTransitionDurationSeconds = 1.4f;
    private const float AutosaveIntervalSeconds = 12f;
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

    private static readonly ArmorDefinition AncientScholarMedallion = new(
        "ancient-scholar-medallion",
        "Ancient Scholar Medallion",
        ArmorClass.Light,
        3,
        18,
        .91f,
        1.04f,
        1.12f,
        .88f,
        .92f);

    private readonly DungeonGenerator _dungeonGenerator;
    private readonly CatacombMapGenerator _catacombGenerator;
    private readonly FallenCityMapGenerator _fallenCityGenerator;
    private readonly DemonLandsMapGenerator _demonLandsGenerator;
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
    private object _rangerUltimateCandidate;
    private object _rangerUltimateLockedTarget;
    private int _rangerUltimateTechniqueUseId = -1;
    private bool _rangerUltimateLandingRequested;
    private object _greatswordUltimateCandidate;
    private object _greatswordUltimateLockedTarget;
    private Vector2 _greatswordUltimateApproachPosition;
    private Vector2 _greatswordUltimateLandingPosition;
    private int _greatswordUltimateTechniqueUseId = -1;
    private int _greatswordUltimateMotionAttackId = -1;
    private bool _greatswordUltimateLandingRequested;
    private int _greatswordWorldEffectAttackId = -1;
    private object _sovereignUltimateCandidate;
    private object _sovereignUltimateLockedTarget;
    private object _sovereignActionTarget;
    private int _sovereignUltimateTechniqueUseId = -1;
    private int _sovereignActionTechniqueUseId = -1;
    private int _sovereignMotionAttackId = -1;
    private bool _sovereignSafeExitRequested;
    private int _sovereignExecutionTechniqueUseId = -1;
    private Vector2 _sovereignSafeExitPosition;
    private float _sovereignExecutionFrameRemaining;
    private bool _sovereignExecutionSuppressed;
    private bool _sovereignExecutionImpactApplied;
    private object _sovereignExecutionTarget;
    private int _sovereignExecutionImpactId;
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
    private object _ancientStaffCandidate;
    private object _ancientStaffUltimateCandidate;
    private object _ancientStaffLockedTarget;
    private object _scytheUltimateCandidate;
    private object _scytheUltimateLockedTarget;
    private object _scytheShadowSeverTarget;
    private int _scytheUltimateTechniqueUseId = -1;
    private int _scytheUltimateMotionAttackId = -1;
    private bool _scytheSafeReturnRequested;
    private Vector2 _scytheSafeReturnPosition;
    private Vector2 _scytheUltimateVisualStartPosition;
    private int _scytheCrossTechniqueUseId = -1;
    private readonly Dictionary<Enemy, int> _scytheCrossEnemyHits = new();
    private int _scytheCrossBossHits;
    private int _scytheEnhancedMarkTechniqueUseId = -1;
    private Vector2 _ancientStaffLockedCenter;
    private int _ancientStaffTechniqueUseId = -1;
    private int _ancientStaffWorldEffectAttackId = -1;
    private int _ancientStaffMotionAttackId = -1;
    private float _thunderSpiritTimeRemaining;
    private float _thunderSpiritAttackCooldown;
    private ThunderSpiritState _thunderSpiritState;
    private Vector2 _thunderSpiritLastTarget;
    private float _thunderSpiritSummonVisualRemaining;
    private float _thunderSpiritDismissVisualRemaining;
    private float _ancientStaffEffectTimeRemaining;
    private float _ancientStaffEffectDuration;
    private Vector2 _ancientStaffBlinkOrigin;
    private float _ancientStaffBlinkTrailRemaining;
    private int _breakerWorldEffectAttackId = -1;
    private int _breakerImpactFeedbackAttackId = -1;
    private int _greatswordLandingImpactId = -1;
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
    private int _selectedNoteIndex;
    private float _loreNoteFeedbackTimeRemaining;
    private LoreNote _lastAcquiredLoreNote;
    private DungeonRoom _sequentialTestRoom;
    private Enemy _sequentialTestEnemy;
    private int _sequentialTestIndex;
    private bool _sequentialBossActive;
    private bool _sequentialTestComplete;
    private readonly List<Enemy> _wildForestShowcaseEnemies = new();
    private readonly SaveSlotManager _saveSlotManager;
    private int _pendingSlotIndex = -1;
    private int _activeSlotIndex = -1;
    private double _activePlayTimeSeconds;
    private float _autosaveTimeRemaining = AutosaveIntervalSeconds;

    public PlayerCharacter Player { get; private set; }
    public EnemyManager Enemies { get; }
    public WildForestEncounterDirector WildForestEncounters { get; }
    public EnemyIntroductionManager EnemyIntroductions { get; }
    public CurseSystem Curse { get; }
    public TombInteractionManager Tombs { get; }
    public AncientScholarTombSystem ScholarTomb { get; }
    public LastDiceTableSystem LastDiceTable { get; }
    public DeveloperPanel DeveloperPanel { get; } = new();
    public PauseMenuState PauseMenu { get; } = new();
    public MainMenuState MainMenu { get; } = new();
    public LanguageSettingsState LanguageSettings { get; } = new();
    public CharacterSelectState CharacterSelect { get; } = new();
    public CharacterCreatorState CharacterCreator { get; } = new();
    public CharacterAppearance RunAppearance { get; private set; } =
        CharacterAppearance.CreateDefault();
    public string CharacterName { get; private set; } = "ASCENDANT";
    public IReadOnlyList<SaveSlotData> SaveSlots => _saveSlotManager.Slots;
    public string SaveError => _saveSlotManager.LastError;
    public IReadOnlyList<RottenCorpseBurst> RottenCorpseBursts => _rottenCorpseBursts;
    public IReadOnlyList<Enemy> DefeatedEnemiesThisFrame => _defeatedEnemies;
    public bool Map01Cleared { get; private set; }
    public bool Map02Cleared { get; private set; }
    public bool Map03Cleared { get; private set; }
    public bool Map03ExitPrepared { get; private set; }
    public bool Map04Cleared { get; private set; }
    public bool Map04ExitPrepared { get; private set; }
    public bool Map2PigPhotoCollected { get; private set; }
    public bool IsPigPhotoPopupOpen { get; private set; }
    public CityAlarmSystem CityAlarm { get; }
    public CollapsingCitySystem CollapsingCity { get; }
    public DemonicCorruptionSystem Corruption { get; }
    public CorruptionAnchorSystem CorruptionAnchors { get; }
    public ProjectileManager Projectiles { get; }
    public GroundRuneManager GroundRunes { get; }
    public SovereignPortalSystem SovereignPortals { get; } = new();
    public IReadOnlyList<BreakerShockwave> BreakerShockwaves =>
        _breakerShockwaves;
    public IReadOnlyList<BreakerImpactVisual> BreakerImpactVisuals =>
        _breakerImpactVisuals;
    public RootHazardManager RootHazards { get; }
    public LootManager Loot { get; }
    public LoreNotePickup LoreNotePickup { get; private set; }
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
    public bool IsSelectedInventoryItemEquipped =>
        ReferenceEquals(
            SelectedInventoryItem,
            Player?.EquippedItems.GetEquipped(
                SelectedInventoryItem?.Slot ?? EquipmentSlot.Weapon));
    public IReadOnlyList<LoreNote> CollectedNotes => Player.Notes.Notes;
    public LoreNote SelectedLoreNote =>
        ActiveInventoryTab == InventoryTab.Notes &&
        SelectedInventoryIndex >= 0 &&
        SelectedInventoryIndex < Player.Notes.Count
            ? Player.Notes.Notes[SelectedInventoryIndex]
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
    public bool IsLoreNoteFeedbackVisible =>
        _loreNoteFeedbackTimeRemaining > 0f && _lastAcquiredLoreNote != null;
    public LoreNote LastAcquiredLoreNote => _lastAcquiredLoreNote;
    public bool IsWildForestShowcaseMode =>
        DebugWildForestShowcase &&
        CurrentRegion == RegionType.WildForest &&
        DungeonDepth == 1;
    public bool IsSequentialEnemyTestMode => IsWildForestShowcaseMode;
    public bool IsMapEntryFallActive => _mapEntryFallActive;
    public bool IsRangerUltimateTargetLocked =>
        _rangerUltimateLockedTarget != null;
    public Rectangle RangerUltimateTargetBounds =>
        GetRangerTargetBounds(_rangerUltimateLockedTarget);
    public bool IsGreatswordUltimateTargetLocked =>
        _greatswordUltimateLockedTarget != null;
    public Rectangle GreatswordUltimateTargetBounds =>
        GetCombatTargetBounds(_greatswordUltimateLockedTarget);
    public bool IsSovereignUltimateTargetLocked =>
        _sovereignUltimateLockedTarget != null;
    public Rectangle SovereignUltimateTargetBounds =>
        GetCombatTargetBounds(_sovereignUltimateLockedTarget);
    public bool IsScytheUltimateTargetLocked =>
        _scytheUltimateLockedTarget != null;
    public Rectangle ScytheUltimateTargetBounds =>
        GetCombatTargetBounds(_scytheUltimateLockedTarget);
    public bool IsScytheUltimateActive =>
        Player?.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.ScytheUmbraDevourer;
    public int ScytheUltimateStage => IsScytheUltimateActive
        ? Player.Combat.TechniqueStageIndex
        : -1;
    public float ScytheUltimateStageRatio =>
        IsScytheUltimateActive && Player.Combat.CurrentAttack != null
            ? Math.Clamp(
                Player.Combat.StateElapsed /
                    Math.Max(.01f, Player.Combat.CurrentAttack.TotalTime),
                0f,
                1f)
            : 0f;
    public Vector2 ScytheUltimateVisualStartPosition =>
        _scytheUltimateVisualStartPosition;
    public bool IsSovereignUltimateActive =>
        Player?.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.SovereignGoldenCataclysm;
    public int SovereignUltimateStage => IsSovereignUltimateActive
        ? Player.Combat.TechniqueStageIndex
        : -1;
    public float SovereignUltimateStageRatio =>
        IsSovereignUltimateActive && Player.Combat.CurrentAttack != null
            ? Math.Clamp(
                Player.Combat.StateElapsed /
                    Math.Max(.01f, Player.Combat.CurrentAttack.TotalTime),
                0f,
                1f)
            : 0f;
    public float SovereignExecutionFrameProgress =>
        _sovereignExecutionFrameRemaining > 0f
            ? 1f - _sovereignExecutionFrameRemaining /
                AbyssRingTuning.ExecutionDurationSeconds
            : 0f;
    public bool IsSovereignExecutionActive =>
        _sovereignExecutionFrameRemaining > 0f;
    public float SovereignExecutionElapsedSeconds =>
        IsSovereignExecutionActive
            ? AbyssRingTuning.ExecutionDurationSeconds -
                _sovereignExecutionFrameRemaining
            : 0f;
    public bool IsSovereignExecutionImpactActive =>
        IsSovereignExecutionActive &&
        SovereignExecutionElapsedSeconds >=
            AbyssRingTuning.ExecutionImpactSeconds &&
        SovereignExecutionElapsedSeconds <
            AbyssRingTuning.ExecutionHitStopEndSeconds;
    public int SovereignExecutionImpactId => _sovereignExecutionImpactId;
    public Rectangle SovereignExecutionTargetBounds =>
        GetCombatTargetBounds(_sovereignExecutionTarget);
    public bool IsSovereignExecutionSuppressed =>
        _sovereignExecutionSuppressed;
    public ThunderSpiritState ThunderSpiritState => _thunderSpiritState;
    public float ThunderSpiritTimeRemaining => _thunderSpiritTimeRemaining;
    public Vector2 ThunderSpiritPosition => Player == null
        ? Vector2.Zero
        : Player.Position + new Vector2(
            Player.Facing == FacingDirection.Left ? 38f : -38f,
            -54f);
    public Vector2 ThunderSpiritLastTarget => _thunderSpiritLastTarget;
    public bool IsThunderSpiritVisualVisible =>
        _thunderSpiritState != ThunderSpiritState.Dormant ||
        _thunderSpiritDismissVisualRemaining > 0f;
    public float ThunderSpiritSummonVisualRatio => Math.Clamp(
        1f - _thunderSpiritSummonVisualRemaining / .62f,
        0f,
        1f);
    public float ThunderSpiritDismissVisualRatio => Math.Clamp(
        _thunderSpiritDismissVisualRemaining / .38f,
        0f,
        1f);
    public bool IsAncientStaffUltimateActive =>
        Player?.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate;
    public int AncientStaffUltimateStage => IsAncientStaffUltimateActive
        ? Player.Combat.TechniqueStageIndex
        : -1;
    public float AncientStaffUltimateStageRatio =>
        IsAncientStaffUltimateActive && Player.Combat.CurrentAttack != null
            ? Math.Clamp(
                Player.Combat.StateElapsed /
                    Math.Max(.01f, Player.Combat.CurrentAttack.TotalTime),
                0f,
                1f)
            : 0f;
    public Vector2 AncientStaffEffectCenter => _ancientStaffLockedCenter;
    public float AncientStaffEffectRatio => _ancientStaffEffectDuration > 0f
        ? Math.Clamp(
            _ancientStaffEffectTimeRemaining / _ancientStaffEffectDuration,
            0f,
            1f)
        : 0f;
    public Vector2 AncientStaffBlinkOrigin => _ancientStaffBlinkOrigin;
    public float AncientStaffBlinkTrailRatio => Math.Clamp(
        _ancientStaffBlinkTrailRemaining / .30f,
        0f,
        1f);
    public float AncientStaffRestraintVisualStrength =>
        _ancientStaffLockedTarget switch
        {
            AncientTreant => .30f,
            Enemy enemy when enemy.IsElite => .58f,
            _ => 1f
        };
    public WeaponTechniqueEffect PlayerHitTechniqueEffect { get; private set; }
    public int PlayerHitTechniqueStage { get; private set; } = -1;
    public string CurrentMapName => CurrentDungeon?.IsDemonLands == true
        ? Loc.Get("hud.map_demon_lands")
        : CurrentDungeon?.IsFallenCity == true
            ? Loc.Get("hud.map_fallen_city")
        : CurrentDungeon?.IsAncientCatacombs == true
            ? Loc.Get("hud.map_catacombs")
            : Loc.Get("hud.map_wilderness");
    public string CurrentZoneName => GetCurrentZoneName();
    public int ActiveEnemyCount => CountActiveEnemies();
    public bool ShouldRenderBoss => CurrentDungeon != null &&
        !CurrentDungeon.IsAncientCatacombs && !CurrentDungeon.IsFallenCity &&
        !CurrentDungeon.IsDemonLands;
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

    /// <summary>
    /// Deterministic constructor retained for the existing headless gameplay
    /// validations, which intentionally bypass front-end screens.
    /// </summary>
    public GameSession(Rectangle viewportBounds, int randomSeed)
        : this(viewportBounds, (int?)randomSeed, null)
    {
        ResetRun();
        State = GameState.Playing;
    }

    public GameSession(
        Rectangle viewportBounds,
        int? randomSeed = null,
        string savePath = null)
    {
        WeaponArchetypeValidation.ValidateOrThrow();
        EquipmentRebuildValidation.ValidateOrThrow();
        ArmorProgressionValidation.ValidateOrThrow();
        InventoryGridNavigation.ValidateOrThrow();
        InventoryTabRules.ValidateOrThrow();
        WildForestMapValidation.ValidateOrThrow();
        _saveSlotManager = new SaveSlotManager(savePath);
        _dungeonGenerator = new DungeonGenerator(randomSeed);
        _catacombGenerator = new CatacombMapGenerator();
        _fallenCityGenerator = new FallenCityMapGenerator();
        _demonLandsGenerator = new DemonLandsMapGenerator();
        Enemies = new EnemyManager(randomSeed);
        WildForestEncounters = new WildForestEncounterDirector();
        EnemyIntroductions = new EnemyIntroductionManager();
        Curse = new CurseSystem();
        Tombs = new TombInteractionManager();
        ScholarTomb = new AncientScholarTombSystem();
        LastDiceTable = new LastDiceTableSystem(randomSeed);
        CityAlarm = new CityAlarmSystem();
        CollapsingCity = new CollapsingCitySystem();
        Corruption = new DemonicCorruptionSystem();
        CorruptionAnchors = new CorruptionAnchorSystem();
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

    public void ResizeViewport(Rectangle viewportBounds)
    {
        Camera.ResizeViewport(viewportBounds.Width, viewportBounds.Height);
    }

    public void Update(
        GameTime gameTime,
        KeyboardState keyboardState,
        MouseState mouseState)
    {
        bool startPressed = keyboardState.IsKeyDown(Keys.Enter) &&
            !keyboardState.IsKeyDown(Keys.LeftAlt) &&
            !keyboardState.IsKeyDown(Keys.RightAlt) &&
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
        bool deletePressed = WasKeyPressed(keyboardState, Keys.Delete);
        bool createPressed = WasKeyPressed(keyboardState, Keys.C);
        _fusionFeedbackTimeRemaining = System.MathF.Max(
            0f,
            _fusionFeedbackTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        _loreNoteFeedbackTimeRemaining = System.MathF.Max(
            0f,
            _loreNoteFeedbackTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        UpdateSovereignExecution(
            (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (State == GameState.Paused)
        {
            UpdatePauseMenu(keyboardState, startPressed, pausePressed);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.Playing && LastDiceTable.IsModalActive)
        {
            UpdateLastDiceTableModal(gameTime, keyboardState);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.Playing && IsPigPhotoPopupOpen)
        {
            if (startPressed || pausePressed)
                IsPigPhotoPopupOpen = false;
            Player.Combat.CancelActions(restoreStamina: false);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (DeveloperModeEnabled && DeveloperPanel.Update(this, keyboardState))
        {
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (debugPressed)
            ShowCombatDebug = !ShowCombatDebug;

        if (environmentDebugPressed)
            ShowMapDebug = !ShowMapDebug;

        if (godModePressed)
            Player.DebugGodMode = !Player.DebugGodMode;

        if (State == GameState.Start)
        {
            if (LanguageSettings.IsOpen)
            {
                UpdateLanguageSettings(keyboardState, startPressed, pausePressed);
            }
            else
            {
                if (WasKeyPressed(keyboardState, Keys.W) ||
                    WasKeyPressed(keyboardState, Keys.Up))
                {
                    MainMenu.Move(-1);
                }
                else if (WasKeyPressed(keyboardState, Keys.S) ||
                    WasKeyPressed(keyboardState, Keys.Down))
                {
                    MainMenu.Move(1);
                }

                if (startPressed && MainMenu.SelectedEntry == MainMenuEntry.Settings)
                {
                    LanguageSettings.Open();
                }
                else if (startPressed)
                {
                    _saveSlotManager.Reload();
                    CharacterSelect.Begin(_saveSlotManager.Slots);
                    State = GameState.CharacterSelect;
                }
            }

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.CharacterSelect)
        {
            UpdateCharacterSelect(
                keyboardState,
                startPressed,
                pausePressed,
                deletePressed,
                createPressed);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.CharacterCreation)
        {
            UpdateCharacterCreation(keyboardState, startPressed, pausePressed);
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (State == GameState.GameOver)
        {
            if (restartPressed)
            {
                ResetRunPreservingEquipment();
                SaveActiveSlot();
                State = GameState.Playing;
            }

            StoreInputStates(keyboardState, mouseState);
            return;
        }

        float elapsedForSave = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .25f);
        _activePlayTimeSeconds += elapsedForSave;
        _autosaveTimeRemaining -= elapsedForSave;
        if (_autosaveTimeRemaining <= 0f)
        {
            SaveActiveSlot();
            _autosaveTimeRemaining = AutosaveIntervalSeconds;
        }

        if (ScholarTomb.IsModalActive)
        {
            UpdateScholarTombModal(gameTime, keyboardState);
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
            ClearRangerUltimateLock();
            ClearGreatswordUltimateLock();
            ClearSovereignState();
            ClearAncientStaffState();
            RefreshInventoryView();
            RestoreActiveTabSelection();
            ClampInventorySelection();
            MarkSelectedNoteRead();
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (pausePressed)
        {
            SaveActiveSlot();
            Player.Combat.CancelActions(restoreStamina: false);
            ClearRangerUltimateLock();
            ClearGreatswordUltimateLock();
            ClearSovereignState();
            ClearAncientStaffState();
            PauseMenu.Begin();
            State = GameState.Paused;
            StoreInputStates(keyboardState, mouseState);
            return;
        }

        if (!Player.IsAlive)
        {
            ClearRangerUltimateLock();
            ClearGreatswordUltimateLock();
            ClearSovereignState();
            ClearAncientStaffState();
            Projectiles.Clear();
            ClearBreakerEffects();
            RootHazards.Clear();
            ClearDemonRitualEffects();
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
        UpdateAncientStaffCandidates();
        UpdateBreakerEffects(elapsedSeconds);
        _worldTierTransitionTimeRemaining = System.MathF.Max(
            0f,
            _worldTierTransitionTimeRemaining - elapsedSeconds);
        Player.UpdateTimers(gameTime);
        Player.Combat.SetSpellbladeRuneCount(GroundRunes.CountWithin(
            Player.Position,
            SpellbladeTuning.ConvergenceMaximumRange));
        UpdateRangerUltimateCandidate();
        Player.Combat.SetRangerUltimateTargetAvailable(
            _rangerUltimateCandidate != null);
        UpdateGreatswordUltimateCandidate();
        Player.Combat.SetGreatswordUltimateTargetAvailable(
            _greatswordUltimateCandidate != null);
        UpdateSovereignUltimateCandidate();
        Player.Combat.SetSovereignUltimateTargetAvailable(
            _sovereignUltimateCandidate != null);
        Player.Combat.SetAncientStaffUltimateTargetAvailable(
            _ancientStaffUltimateCandidate != null);
        UpdateScytheUltimateCandidate();
        Player.Combat.SetScytheUltimateTargetAvailable(
            _scytheUltimateCandidate != null);
        bool entryFallWasActive = _mapEntryFallActive;
        GameTime playerCombatTime = IsSovereignExecutionActive
            ? ScaleGameTime(
                gameTime,
                AbyssRingTuning.ExecutionWorldTimeScale)
            : gameTime;
        Player.UpdateCombat(
            playerCombatTime,
            entryFallWasActive || IsSovereignExecutionActive
                ? default
                : CreateCombatInput(keyboardState, mouseState));
        UpdateRangerUltimateLock();
        UpdateGreatswordUltimateLock();
        UpdateSovereignTargetLock();
        UpdateAncientStaffTargetLock();
        UpdateScytheUltimateLock();
        GameTime cinematicWorldTime = ScaleGameTime(
            gameTime,
            MathF.Min(
                MathF.Min(
                    Player.Combat.RangerUltimateWorldTimeScale,
                    Player.Combat.GreatswordUltimateWorldTimeScale),
                MathF.Min(
                    MathF.Min(
                        Player.Combat.SovereignUltimateWorldTimeScale,
                        MathF.Min(
                            Player.Combat.AncientStaffUltimateWorldTimeScale,
                            Player.Combat.ScytheUltimateWorldTimeScale)),
                    IsSovereignExecutionActive
                        ? AbyssRingTuning.ExecutionWorldTimeScale
                        : 1f)));
        if (_greatswordLandingImpactId !=
            Player.Combat.GreatswordLandingImpactId)
        {
            _greatswordLandingImpactId =
                Player.Combat.GreatswordLandingImpactId;
            Camera.AddImpactFeedback(1.65f);
        }
        UpdateDuelistTechniqueMovement();
        UpdateRangerTechniqueMovement();
        UpdateGreatswordUltimateMovement(elapsedSeconds);
        UpdateSovereignTechniqueMovement();
        UpdateAncientStaffTechniqueMovement();
        UpdateScytheTechniqueMovement();
        ResolvePerfectRiftCounter();
        SovereignPortals.Update(elapsedSeconds);
        UpdateRangerFocusPressure(elapsedSeconds);
        UpdateAncientStaffRuntime(elapsedSeconds);
        if (Player.Combat.IsHitstopActive)
        {
            StoreInputStates(keyboardState, mouseState);
            return;
        }
        KeyboardState movementInput =
            entryFallWasActive || IsSovereignExecutionActive
            ? new KeyboardState()
            : keyboardState;
        bool jumpPressed = !entryFallWasActive &&
            !IsSovereignExecutionActive &&
            keyboardState.IsKeyDown(Keys.Space) &&
            !_previousKeyboardState.IsKeyDown(Keys.Space);
        Player.UpdateSideScrollingMovement(
            IsSovereignExecutionActive ? playerCombatTime : gameTime,
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
        bool insideScholarSafeZone =
            ScholarTomb.IsPlayerInsideSafeZone(Player.Position);
        WildForestEncounters.Update(
            gameTime,
            Player.Position,
            CurrentDungeon,
            Enemies,
            activeEnemyLevel,
            WorldTier,
            suppressActivation: insideScholarSafeZone);
        CorruptionAnchors.Update(
            gameTime,
            CurrentDungeon,
            Player,
            Enemies,
            Corruption,
            activeEnemyLevel,
            WorldTier);
        Corruption.Update(
            gameTime,
            Player,
            CurrentDungeon,
            CorruptionAnchors);
        CityAlarm.Update(
            gameTime,
            CurrentDungeon,
            Player.Position,
            Enemies,
            WildForestEncounters,
            activeEnemyLevel,
            WorldTier);
        CollapsingCity.Update(gameTime);
        Tombs.Update(gameTime,Player,CurrentDungeon,Enemies,activeEnemyLevel,WorldTier);
        if (Tombs.LootOpenedPosition.HasValue)
            Loot.CreateTreasureChestDrops(Tombs.LootOpenedPosition.Value,
                Player.Level,DungeonDepth,WorldTier,CurrentRegion,CurrentDungeon);
        if (insideScholarSafeZone)
        {
            Curse.Reduce(6f * elapsedSeconds);
            Projectiles.ClearEnemyThreats();
            RootHazards.Clear();
            _rottenCorpseBursts.Clear();
        }
        else
        {
            Curse.Update(gameTime,Player,CurrentDungeon);
        }
        UpdateRottenCorpseBursts(gameTime);
        if (CurrentDungeon.IsAncientCatacombs)
        {
            float curseDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                float distance = Vector2.Distance(enemy.Position, Player.Position);
                if (enemy is CatacombEnemy graveBat &&
                    graveBat.Type == EnemyType.GraveBat)
                {
                    Player.Combat.ExternalStaminaRegenMultiplier *=
                        graveBat.GraveBatStaminaPressureMultiplier;
                }
                if (enemy.Type == EnemyType.Wraith && distance < 135f)
                    Curse.Add(5f * curseDelta);
                if (enemy is CatacombEnemy collector &&
                    collector.Type == EnemyType.SoulCollector &&
                    collector.HasActiveSoulField &&
                    collector.SoulFieldBounds.Intersects(Player.Bounds))
                {
                    Curse.Add(10f * curseDelta);
                }
            }
        }
        if (CurrentDungeon.IsAuthoredWildForest ||
            CurrentDungeon.IsAncientCatacombs || CurrentDungeon.IsFallenCity ||
            CurrentDungeon.IsDemonLands)
        {
            EnemyIntroductions.Update(
                gameTime,
                Player.Position,
                Enemies.Enemies,
                Boss,
                allowBossIntroduction: CurrentDungeon.IsAuthoredWildForest);
        }
        if (EnemyIntroductions.JustTriggered &&
            EnemyIntroductions.BlocksCombat)
        {
            Projectiles.Clear();
            RootHazards.Clear();
        }

        UpdatePlayerAttackArea();
        ProcessDefeatedEnemies();
        ProcessBossDefeat();
        if (!EnemyIntroductions.BlocksCombat)
        {
            ProcessPlayerAttack();
            if (CurrentDungeon.IsDemonLands && Player.Combat.IsAttackActive)
            {
                CorruptionAnchors.TryDamage(
                    PlayerAttackArea,
                    Player.Combat.AttackId,
                    70,
                    Corruption);
            }
            if (!insideScholarSafeZone)
            {
                Enemies.UpdateCombatAndAi(
                    cinematicWorldTime,
                    Player,
                    CurrentDungeon,
                    Projectiles,
                    RootHazards);
                ApplyDemonCorruptionPressure();
                CollapsingCity.UpdateEliteAttacks(
                    CurrentDungeon,
                    Enemies.Enemies);
                ApplyCatacombEnemyCursePressure();
                WildForestEncounters.EnforceArenaBounds();
                Projectiles.Update(cinematicWorldTime, Player, CurrentDungeon);
                Curse.Add(Projectiles.ConsumeEnemyCursePressure());
                ProcessPlayerProjectileImpacts();
                ProcessPlayerProjectileHits();
                ProcessSpellbladeRunePlacement();
                RootHazards.Update(cinematicWorldTime, Player);
            }
            if (ShouldRenderBoss)
            {
                Boss.Update(
                    cinematicWorldTime,
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
            ClearRangerUltimateLock();
            ClearGreatswordUltimateLock();
            ClearSovereignState();
            ClearAncientStaffState();
            Projectiles.Clear();
            GroundRunes.Clear();
            ClearBreakerEffects();
            RootHazards.Clear();
            ClearDemonRitualEffects();
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
            if (burst.HasBurst)
            {
                burst.AftermathTimeRemaining -= elapsed;
                if (burst.IsComplete)
                    _rottenCorpseBursts.RemoveAt(index);
                continue;
            }

            burst.TimeUntilBurst -= elapsed;
            if (burst.TimeUntilBurst > 0f)
                continue;

            burst.HasBurst = true;
            if (Vector2.DistanceSquared(Player.Position, burst.Position) < 190f * 190f)
                Curse.Add(12f);
            foreach (Enemy nearby in Enemies.Enemies)
                if (Vector2.DistanceSquared(nearby.Position, burst.Position) < 150f * 150f)
                    nearby.ReceiveDamage(14, 38f);
        }
    }

    private void ApplyCatacombEnemyCursePressure()
    {
        if (CurrentDungeon?.IsAncientCatacombs != true)
            return;

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy is CatacombEnemy catacomb)
                Curse.Add(catacomb.ConsumeCursePressure());
        }
    }

    private void ApplyDemonCorruptionPressure()
    {
        if (CurrentDungeon?.IsDemonLands != true)
            return;

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy demon)
                continue;
            Corruption.Add(demon.ConsumeCorruptionPressure());
            if (demon.ConsumeAnchorBlessingRequest() &&
                CorruptionAnchors.TryBlessNearestActive(
                    demon,
                    demon.Position,
                    370f,
                    out string anchorId,
                    out Vector2 anchorPosition))
            {
                demon.ConfirmAnchorBlessing(anchorId, anchorPosition);
            }
            if (demon.HasAnchorTether)
            {
                demon.SynchronizeAnchorBlessing(
                    CorruptionAnchors.IsBlessed(demon.BlessedAnchorId));
            }
            if (demon.ConsumeAnchorPulseRequest())
            {
                CorruptionAnchors.TrySummonBossAnchor(
                    demon,
                    demon.RequestedBossAnchorPosition,
                    CurrentDungeon,
                    Player,
                    out _);
            }
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
            bool spawnFallenKnightPhotograph = false;
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
                    spawnFallenKnightPhotograph = true;
                }
            }

            if (CurrentDungeon.IsFallenCity &&
                enemy.Type == EnemyType.PlagueCarrier)
            {
                Projectiles.TryCreatePlagueCloud(
                    new Vector2(
                        enemy.Position.X,
                        enemy.Bounds.Bottom - 12f),
                    enemy.RoomId,
                    CurrentDungeon,
                    collapseCloud: true);
            }

            if (CurrentDungeon.IsFallenCity &&
                enemy.Type == EnemyType.BrokenKing)
            {
                Map03Cleared = true;
                BossDefeated = true;
            }

            if (CurrentDungeon.IsDemonLands &&
                enemy.Type == EnemyType.HornedGeneral)
            {
                Map04Cleared = true;
                BossDefeated = true;
                CorruptionAnchors.ClearBossAnchors(
                    enemy as DemonLandsEnemy,
                    includeAuthoredBossAnchors: true);
                Corruption.Reduce(100f);
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

            if (spawnFallenKnightPhotograph)
                TrySpawnFallenKnightPigPhotoDrop(enemy);
        }

        KillCount += defeatedCount;
    }

    internal bool TrySpawnFallenKnightPigPhotoDrop(Enemy enemy)
    {
        if (enemy == null || enemy.Type != EnemyType.FallenKnight ||
            CurrentDungeon?.IsAncientCatacombs != true ||
            Map2PigPhotoCollected || LoreNotePickup != null)
        {
            return false;
        }

        return SpawnPigPhotoPickup(new Vector2(
            enemy.Position.X,
            enemy.Bounds.Bottom),
            GetFallenKnightArenaBounds());
    }

    private bool SpawnPigPhotoPickup(
        Vector2 footPosition,
        Rectangle allowedBounds)
    {
        if (CurrentDungeon?.IsAncientCatacombs != true ||
            Map2PigPhotoCollected || LoreNotePickup != null)
        {
            return false;
        }

        Vector2 size = DungeonAscendant.World.LoreNotePickup.Size;
        if (!TryResolveSafePigPhotoPosition(
                footPosition,
                size,
                allowedBounds,
                out Vector2 position))
        {
            return false;
        }

        LoreNotePickup = new LoreNotePickup(
            LoreCatalog.MysteriousPhotograph,
            position);
        return true;
    }

    private bool TryResolveSafePigPhotoPosition(
        Vector2 preferredFootPosition,
        Vector2 size,
        Rectangle allowedBounds,
        out Vector2 position)
    {
        position = Vector2.Zero;
        float halfWidth = size.X / 2f;
        float minimumX = allowedBounds.Left + halfWidth + 2f;
        float maximumX = allowedBounds.Right - halfWidth - 2f;
        if (minimumX > maximumX)
            return false;

        float preferredX = MathHelper.Clamp(
            preferredFootPosition.X,
            minimumX,
            maximumX);
        float maximumSearchRadius = MathF.Max(
            120f,
            MathF.Max(preferredX - minimumX, maximumX - preferredX));
        int searchSteps = (int)MathF.Ceiling(maximumSearchRadius / 16f);
        for (int step = 0; step <= searchSteps; step++)
        {
            float distance = MathF.Min(maximumSearchRadius, step * 16f);
            if (TryResolveSafePigPhotoAtX(
                    MathF.Min(maximumX, preferredX + distance),
                    preferredFootPosition.Y,
                    size,
                    allowedBounds,
                    out position))
            {
                return true;
            }

            if (step > 0 && TryResolveSafePigPhotoAtX(
                    MathF.Max(minimumX, preferredX - distance),
                    preferredFootPosition.Y,
                    size,
                    allowedBounds,
                    out position))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveSafePigPhotoAtX(
        float x,
        float preferredFootY,
        Vector2 size,
        Rectangle allowedBounds,
        out Vector2 position)
    {
        position = Vector2.Zero;
        float halfWidth = size.X / 2f;
        float bestDistance = float.MaxValue;
        foreach (Platform platform in CurrentDungeon.Platforms)
        {
            if (x - halfWidth < platform.Bounds.Left ||
                x + halfWidth > platform.Bounds.Right)
            {
                continue;
            }

            Vector2 candidate = new(
                x,
                platform.Bounds.Top - size.Y / 2f);
            Rectangle candidateBounds = DungeonCollision.CreateBounds(
                candidate,
                size);
            if (!allowedBounds.Contains(candidateBounds) ||
                !SideScrollingCollision.IsPositionFree(
                    candidate, size, CurrentDungeon) ||
                !SideScrollingCollision.IsSupported(
                    candidate, size, CurrentDungeon))
            {
                continue;
            }

            float distance = MathF.Abs(platform.Bounds.Top - preferredFootY);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            position = candidate;
        }

        return bestDistance < float.MaxValue;
    }

    private Rectangle GetFallenKnightArenaBounds()
    {
        foreach (EncounterZone zone in CurrentDungeon.EncounterZones)
            if (zone.ZoneId == "fallen-knight")
                return zone.ActivationBounds;
        return CurrentDungeon.BossRoom.Bounds;
    }

    private void UpdatePauseMenu(
        KeyboardState keyboardState,
        bool confirmPressed,
        bool backPressed)
    {
        if (LanguageSettings.IsOpen)
        {
            UpdateLanguageSettings(keyboardState, confirmPressed, backPressed);
            return;
        }

        if (PauseMenu.IsReturnConfirmationOpen)
        {
            if (backPressed)
            {
                PauseMenu.CancelConfirmation();
                return;
            }

            if (WasKeyPressed(keyboardState, Keys.W) ||
                WasKeyPressed(keyboardState, Keys.Up) ||
                WasKeyPressed(keyboardState, Keys.S) ||
                WasKeyPressed(keyboardState, Keys.Down) ||
                WasKeyPressed(keyboardState, Keys.A) ||
                WasKeyPressed(keyboardState, Keys.Left) ||
                WasKeyPressed(keyboardState, Keys.D) ||
                WasKeyPressed(keyboardState, Keys.Right))
            {
                PauseMenu.ToggleConfirmation();
            }

            if (!confirmPressed)
                return;

            if (PauseMenu.ConfirmReturn)
                ReturnToCharacterSelect();
            else
                PauseMenu.CancelConfirmation();
            return;
        }

        if (backPressed)
        {
            State = GameState.Playing;
            return;
        }

        if (WasKeyPressed(keyboardState, Keys.W) ||
            WasKeyPressed(keyboardState, Keys.Up))
        {
            PauseMenu.Move(-1);
        }
        else if (WasKeyPressed(keyboardState, Keys.S) ||
            WasKeyPressed(keyboardState, Keys.Down))
        {
            PauseMenu.Move(1);
        }

        if (!confirmPressed)
            return;

        switch (PauseMenu.SelectedEntry)
        {
            case PauseMenuEntry.Resume:
                State = GameState.Playing;
                break;
            case PauseMenuEntry.Settings:
                LanguageSettings.Open();
                break;
            case PauseMenuEntry.ReturnToCharacterSelect:
                PauseMenu.OpenReturnConfirmation();
                break;
        }
    }

    private void UpdateLanguageSettings(
        KeyboardState keyboardState,
        bool confirmPressed,
        bool backPressed)
    {
        if (backPressed || confirmPressed)
        {
            LanguageSettings.Close();
            return;
        }

        if (WasKeyPressed(keyboardState, Keys.W) ||
            WasKeyPressed(keyboardState, Keys.Up))
        {
            LanguageSettings.MoveRow(-1);
        }
        else if (WasKeyPressed(keyboardState, Keys.S) ||
            WasKeyPressed(keyboardState, Keys.Down))
        {
            LanguageSettings.MoveRow(1);
        }
        else if (WasKeyPressed(keyboardState, Keys.A) ||
            WasKeyPressed(keyboardState, Keys.Left))
        {
            LanguageSettings.Adjust(-1);
        }
        else if (WasKeyPressed(keyboardState, Keys.D) ||
            WasKeyPressed(keyboardState, Keys.Right))
        {
            LanguageSettings.Adjust(1);
        }
    }

    private void ReturnToCharacterSelect()
    {
        int returningSlotIndex = _activeSlotIndex;
        SaveActiveSlot();
        CleanupGameplayRuntime();
        _saveSlotManager.Reload();
        CharacterSelect.Begin(
            _saveSlotManager.Slots,
            returningSlotIndex >= 0 ? returningSlotIndex : null);
        _activeSlotIndex = -1;
        _pendingSlotIndex = -1;
        State = GameState.CharacterSelect;
    }

    private void CleanupGameplayRuntime()
    {
        ClearRangerUltimateLock();
        ClearGreatswordUltimateLock();
        ClearSovereignState();
        ClearAncientStaffState();
        Projectiles.Clear();
        GroundRunes.Clear();
        RootHazards.Clear();
        ClearDemonRitualEffects();
        Corruption.Reset();
        CorruptionAnchors.Reset(null);
        ClearBreakerEffects();
        Loot.Reset();
        Enemies.Reset(
            null,
            1,
            1,
            RegionType.WildForest,
            suppressProceduralSpawns: true);
        WildForestEncounters.Reset(null);
        EnemyIntroductions.Reset();
        Curse.Reset();
        CityAlarm.Reset();
        CollapsingCity.Reset(CurrentDungeon);
        Tombs.Reset(null);
        ScholarTomb.ResetForNewRun();
        LastDiceTable.ResetForNewRun();
        Camera.ResetRuntime();
        _defeatedEnemies.Clear();
        _rottenCorpseBursts.Clear();
        _playerAttackHits.Clear();
        _pendingArrowRain.Clear();
        _predatorFanHits.Clear();
        _raiderWallImpactedEnemies.Clear();
        _wildForestShowcaseEnemies.Clear();
        _activeInventoryItems.Clear();
        ClearFusionState();
        ClearSequentialEnemyTestState();
        LoreNotePickup = null;
        Map2PigPhotoCollected = false;
        IsPigPhotoPopupOpen = false;
        Chest = null;
        Boss = null;
        CurrentDungeon = null;
        IsInventoryOpen = false;
        _lastAcquiredLoreNote = null;
        _loreNoteFeedbackTimeRemaining = 0f;
        _worldTierTransitionTimeRemaining = 0f;
        Player.ClearTemporaryStatus();
        Player.Combat.CancelActions(restoreStamina: false);
        Player = new PlayerCharacter(Vector2.Zero);
        RunAppearance = CharacterAppearance.CreateDefault();
        CharacterName = "ASCENDANT";
    }

    private void UpdateCharacterSelect(
        KeyboardState keyboardState,
        bool confirmPressed,
        bool backPressed,
        bool deletePressed,
        bool createPressed)
    {
        SaveSlotData selected = _saveSlotManager.GetSlot(
            CharacterSelect.SelectedSlotIndex);

        if (CharacterSelect.IsDeleteConfirmationOpen)
        {
            if (backPressed)
            {
                CharacterSelect.CancelDelete();
            }
            else if (confirmPressed || deletePressed)
            {
                _saveSlotManager.Delete(CharacterSelect.SelectedSlotIndex);
                CharacterSelect.CancelDelete();
                CharacterSelect.RefreshPreviews(_saveSlotManager.Slots);
            }
            return;
        }

        if (backPressed)
        {
            State = GameState.Start;
            return;
        }

        if (WasKeyPressed(keyboardState, Keys.A) ||
            WasKeyPressed(keyboardState, Keys.Left))
        {
            CharacterSelect.Move(-1);
        }
        else if (WasKeyPressed(keyboardState, Keys.D) ||
            WasKeyPressed(keyboardState, Keys.Right))
        {
            CharacterSelect.Move(1);
        }

        selected = _saveSlotManager.GetSlot(CharacterSelect.SelectedSlotIndex);
        if (deletePressed)
        {
            CharacterSelect.BeginDelete(selected);
            return;
        }

        if (!confirmPressed && !(createPressed && !selected.Occupied))
            return;

        if (selected.Occupied)
        {
            LoadRunFromSlot(selected);
            State = GameState.Playing;
            return;
        }

        _pendingSlotIndex = selected.SlotIndex;
        CharacterCreator.Begin(_pendingSlotIndex);
        Player.SetAppearance(CharacterCreator.Draft);
        State = GameState.CharacterCreation;
    }

    private void ResetRun()
    {
        bool preservePigPhoto = Map2PigPhotoCollected;
        ScholarTomb.ResetForNewRun();
        LastDiceTable.ResetForNewRun();
        DungeonDepth = 1;
        Map01Cleared = false;
        Map02Cleared = false;
        Map03Cleared = false;
        Map03ExitPrepared = false;
        Map04Cleared = false;
        Map04ExitPrepared = false;
        IsPigPhotoPopupOpen = false;
        WorldTier = 1;
        Region = RegionDefinition.WildForest;
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = IsWildForestShowcaseMode
            ? _dungeonGenerator.GenerateWildForestShowcase()
            : _dungeonGenerator.GenerateWildForest();
        Player = new PlayerCharacter(
            GetRoomEntranceSpawn(CurrentDungeon.StartRoom),
            appearance: RunAppearance);
        Map2PigPhotoCollected = preservePigPhoto;
        if (preservePigPhoto && Player.Notes.TryCollect(
                LoreCatalog.MysteriousPhotograph,
                out LoreNote preservedPhotograph))
        {
            preservedPhotograph.MarkRead();
        }
        _lastSafePlayerPosition = Player.Position;
        KillCount = 0;
        IsInventoryOpen = false;
        ShowCombatDebug = DebugCombatHitboxes;
        ShowMapDebug = false;
        SelectedInventoryIndex = 0;
        ActiveInventoryTab = InventoryTab.Weapons;
        _selectedWeaponIndex = 0;
        _selectedEquipmentIndex = 0;
        _selectedNoteIndex = 0;
        _lastAcquiredLoreNote = null;
        _loreNoteFeedbackTimeRemaining = 0f;
        RefreshInventoryView();
        _debugWeaponIndex = 1;
        _debugArmorIndex = 1;
        ClearFusionState();
        InitializeDungeonState();
    }

    private void ResetRunPreservingEquipment()
    {
        EquipmentItem weapon = Player?.EquippedItems.Weapon;
        EquipmentItem armor = Player?.EquippedItems.Armor;
        int level = Player?.Level ?? 1;
        int experience = Player?.CurrentExperience ?? 0;
        var inventory = Player == null
            ? new List<EquipmentItem>()
            : new List<EquipmentItem>(Player.Inventory.Items);

        ResetRun();
        Player.RestoreSaveState(level, experience, weapon, armor);
        Player.Inventory.Restore(inventory);
        RefreshInventoryView();
        ClampInventorySelection();
        UpdatePlayerAttackArea();
    }

    private void UpdateCharacterCreation(
        KeyboardState keyboardState,
        bool confirmPressed,
        bool backPressed)
    {
        if (backPressed)
        {
            Player.SetAppearance(CharacterAppearance.CreateDefault());
            CharacterSelect.Begin(_saveSlotManager.Slots);
            State = GameState.CharacterSelect;
            return;
        }

        if (WasKeyPressed(keyboardState, Keys.W) ||
            WasKeyPressed(keyboardState, Keys.Up))
        {
            CharacterCreator.MoveSelection(-1);
        }
        else if (WasKeyPressed(keyboardState, Keys.S) ||
            WasKeyPressed(keyboardState, Keys.Down))
        {
            CharacterCreator.MoveSelection(1);
        }

        int adjustment = 0;
        if (WasKeyPressed(keyboardState, Keys.A) ||
            WasKeyPressed(keyboardState, Keys.Left))
        {
            adjustment = -1;
        }
        else if (WasKeyPressed(keyboardState, Keys.D) ||
            WasKeyPressed(keyboardState, Keys.Right))
        {
            adjustment = 1;
        }

        if (adjustment != 0)
        {
            CharacterCreator.Adjust(adjustment);
            Player.SetAppearance(CharacterCreator.Draft);
        }

        if (!confirmPressed)
            return;

        switch (CharacterCreator.SelectedRow)
        {
            case CharacterCreatorRow.Randomize:
                CharacterCreator.Randomize();
                Player.SetAppearance(CharacterCreator.Draft);
                break;
            case CharacterCreatorRow.ResetDefault:
                CharacterCreator.ResetToDefault();
                Player.SetAppearance(CharacterCreator.Draft);
                break;
            case CharacterCreatorRow.Confirm:
                RunAppearance = CharacterCreator.Draft.Clone();
                RunAppearance.Normalize();
                CharacterName = SaveSlotData.SanitizeName(
                    CharacterCreator.CharacterName,
                    Math.Max(0, _pendingSlotIndex));
                _activeSlotIndex = Math.Clamp(
                    _pendingSlotIndex,
                    0,
                    SaveSlotManager.SlotCount - 1);
                _activePlayTimeSeconds = 0d;
                _autosaveTimeRemaining = AutosaveIntervalSeconds;
                ResetRun();
                SaveActiveSlot();
                State = GameState.Playing;
                break;
            default:
                CharacterCreator.Adjust(1);
                Player.SetAppearance(CharacterCreator.Draft);
                break;
        }
    }

    private void LoadRunFromSlot(SaveSlotData slot)
    {
        slot.Normalize(slot.SlotIndex);
        _activeSlotIndex = slot.SlotIndex;
        _pendingSlotIndex = -1;
        CharacterName = slot.CharacterName;
        RunAppearance = slot.Appearance.Clone();
        _activePlayTimeSeconds = slot.PlayTimeSeconds;
        _autosaveTimeRemaining = AutosaveIntervalSeconds;

        ScholarTomb.ResetForNewRun();
        LastDiceTable.ResetForNewRun();
        DungeonDepth = Math.Clamp(slot.DungeonDepth, 1, 4);
        WorldTier = WorldProgression.GetWorldTier(DungeonDepth);
        Region = GetRegionForDepth(DungeonDepth);
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = DungeonDepth switch
        {
            2 => _catacombGenerator.Generate(),
            3 => _fallenCityGenerator.Generate(),
            4 => _demonLandsGenerator.Generate(),
            _ => _dungeonGenerator.GenerateWildForest()
        };
        Player = new PlayerCharacter(
            GetRoomEntranceSpawn(CurrentDungeon.StartRoom),
            appearance: RunAppearance);
        Player.RestoreSaveState(
            slot.Level,
            slot.CurrentExperience,
            CharacterSelectState.CreateWeapon(slot),
            CharacterSelectState.CreateArmor(slot));
        if (slot.HasInventorySnapshot)
        {
            var restoredInventory = new List<EquipmentItem>(slot.InventoryItems.Count);
            foreach (SavedEquipmentItemData savedItem in slot.InventoryItems)
                restoredInventory.Add(CharacterSelectState.CreateInventoryItem(savedItem));
            Player.Inventory.Restore(restoredInventory);
            Player.NormalizeDevelopmentTestWeapons();
        }
        foreach (SavedLoreNoteData savedNote in slot.Notes)
        {
            LoreNoteDefinition definition = LoreCatalog.Resolve(savedNote.Id);
            if (definition == null ||
                !Player.Notes.TryCollect(definition, out LoreNote note))
            {
                continue;
            }

            if (savedNote.IsRead)
                note.MarkRead();
        }
        Map2PigPhotoCollected = slot.Map2PigPhotoCollected ||
            Player.Notes.Contains(LoreCatalog.MysteriousPhotograph.Id);
        if (Map2PigPhotoCollected &&
            !Player.Notes.Contains(LoreCatalog.MysteriousPhotograph.Id) &&
            Player.Notes.TryCollect(
                LoreCatalog.MysteriousPhotograph,
                out LoreNote restoredPhotograph))
        {
            restoredPhotograph.MarkRead();
        }
        IsPigPhotoPopupOpen = false;
        KillCount = slot.KillCount;
        IsInventoryOpen = false;
        ShowCombatDebug = DebugCombatHitboxes;
        ShowMapDebug = false;
        SelectedInventoryIndex = 0;
        ActiveInventoryTab = InventoryTab.Weapons;
        _selectedWeaponIndex = 0;
        _selectedEquipmentIndex = 0;
        _selectedNoteIndex = 0;
        _lastAcquiredLoreNote = null;
        _loreNoteFeedbackTimeRemaining = 0f;
        RefreshInventoryView();
        _debugWeaponIndex = 1;
        _debugArmorIndex = 1;
        ClearFusionState();
        InitializeDungeonState();

        LastDiceTable.Restore(
            slot.LastDiceStarted,
            slot.LastDiceCompleted,
            slot.LastDiceRoundIndex,
            slot.LastDiceWins,
            slot.LastDiceRoundCommitted,
            slot.LastDiceRoundResolved,
            slot.LastDiceCompletionReached,
            slot.LastDiceChoice,
            slot.LastDiceDie1,
            slot.LastDiceDie2,
            slot.LastDiceDie3,
            slot.LastDiceRevealProgress,
            slot.LastDiceRewardGranted,
            slot.LastDiceRewardWeaponIndex);

        Map01Cleared = slot.Map01Cleared;
        Map02Cleared = slot.Map02Cleared;
        Map03Cleared = slot.Map03Cleared;
        Map04Cleared = slot.Map04Cleared;
        if (CurrentDungeon.IsAncientCatacombs && Map02Cleared)
            WildForestEncounters.MarkCleared("fallen-knight");
        if (CurrentDungeon.IsFallenCity && Map03Cleared)
            WildForestEncounters.MarkCleared("broken-king");
        if (CurrentDungeon.IsDemonLands && Map04Cleared)
            WildForestEncounters.MarkCleared("horned-general");
        if (slot.BossDefeated)
        {
            BossDefeated = true;
            if (ShouldRenderBoss && Boss?.IsAlive == true)
                Boss.ReceiveDamage(int.MaxValue);
        }

        var checkpoint = new Vector2(slot.CheckpointX, slot.CheckpointY);
        if (slot.HasCheckpoint &&
            CurrentDungeon.WorldBounds.Contains(checkpoint.ToPoint()) &&
            CurrentDungeon.FindRoomContaining(checkpoint) != null)
        {
            Player.MoveTo(checkpoint);
            _lastSafePlayerPosition = checkpoint;
            Camera.Snap(
                Player.Position,
                Player.Facing,
                CurrentDungeon.WorldBounds,
                CurrentDungeon.IsAuthoredWildForest ? .54f :
                    CurrentDungeon.IsAncientCatacombs ? .55f :
                    CurrentDungeon.IsFallenCity ? .56f :
                    CurrentDungeon.IsDemonLands ? .57f : .58f);
        }
    }

    private void SaveActiveSlot()
    {
        if (_activeSlotIndex < 0 ||
            _activeSlotIndex >= SaveSlotManager.SlotCount ||
            CurrentDungeon == null ||
            Player == null)
        {
            return;
        }

        EquipmentItem weapon = Player.EquippedItems.Weapon;
        EquipmentItem armor = Player.EquippedItems.Armor;
        WeaponDefinition weaponDefinition = Player.EquippedWeapon;
        ArmorDefinition armorDefinition = Player.EquippedArmor;
        var slot = new SaveSlotData
        {
            SlotIndex = _activeSlotIndex,
            Occupied = true,
            CharacterName = CharacterName,
            Appearance = Player.Appearance.Clone(),
            Level = Player.Level,
            CurrentExperience = Player.CurrentExperience,
            DungeonDepth = DungeonDepth,
            WorldTier = WorldTier,
            CurrentMapId = $"map-{DungeonDepth:00}",
            CurrentArea = CurrentZoneName,
            CheckpointX = Player.Position.X,
            CheckpointY = Player.Position.Y,
            HasCheckpoint = true,
            EquippedWeaponId = weaponDefinition.Id,
            EquippedWeaponName = weaponDefinition.Name,
            EquippedWeaponFamily = weaponDefinition.Family,
            EquippedWeaponTier = weaponDefinition.Tier,
            EquippedWeaponDamageBonus = weaponDefinition.DamageBonus,
            EquippedWeaponRarity = weapon?.Rarity ?? ItemRarity.Uncommon,
            EquippedArmorId = armorDefinition.Id,
            EquippedArmorName = armorDefinition.Name,
            EquippedArmorClass = armorDefinition.Class,
            EquippedArmorTier = armorDefinition.Tier,
            EquippedArmorHealthBonus = armorDefinition.MaxHealthBonus,
            EquippedArmorRarity = armor?.Rarity ?? ItemRarity.Uncommon,
            EquippedArmorGrade = armor?.ArmorGrade ?? ArmorGrade.C1,
            HasInventorySnapshot = true,
            InventoryItems = CaptureInventory(Player),
            Notes = CaptureNotes(Player),
            Map01Cleared = Map01Cleared,
            Map02Cleared = Map02Cleared,
            Map03Cleared = Map03Cleared,
            Map04Cleared = Map04Cleared,
            Map2PigPhotoCollected = Map2PigPhotoCollected,
            BossDefeated = BossDefeated,
            LastDiceStarted = LastDiceTable.Started,
            LastDiceCompleted = LastDiceTable.IsCompleted,
            LastDiceRoundIndex = LastDiceTable.RoundIndex,
            LastDiceWins = LastDiceTable.Wins,
            LastDiceRoundCommitted = LastDiceTable.RoundCommitted,
            LastDiceRoundResolved = LastDiceTable.RoundResolved,
            LastDiceCompletionReached = LastDiceTable.CompletionReached,
            LastDiceChoice = LastDiceTable.Choice,
            LastDiceDie1 = LastDiceTable.Dice[0],
            LastDiceDie2 = LastDiceTable.Dice[1],
            LastDiceDie3 = LastDiceTable.Dice[2],
            LastDiceRevealProgress = LastDiceTable.RevealProgress,
            LastDiceRewardGranted = LastDiceTable.RewardGranted,
            LastDiceRewardWeaponIndex = LastDiceTable.RewardWeaponIndex,
            KillCount = KillCount,
            PlayTimeSeconds = _activePlayTimeSeconds,
            LastPlayedUtc = DateTimeOffset.UtcNow
        };
        _saveSlotManager.Store(slot);
    }

    private static List<SavedEquipmentItemData> CaptureInventory(
        PlayerCharacter player)
    {
        var result = new List<SavedEquipmentItemData>(player.Inventory.Count);
        foreach (EquipmentItem item in player.Inventory.Items)
            result.Add(SavedEquipmentItemData.FromItem(item));
        return result;
    }

    private static List<SavedLoreNoteData> CaptureNotes(PlayerCharacter player)
    {
        var result = new List<SavedLoreNoteData>(player.Notes.Count);
        foreach (LoreNote note in player.Notes.Notes)
        {
            result.Add(new SavedLoreNoteData
            {
                Id = note.Id,
                IsRead = note.IsRead
            });
        }
        return result;
    }

    public void FlushActiveSave()
    {
        SaveActiveSlot();
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

        CurrentDungeon = DungeonDepth switch
        {
            2 => _catacombGenerator.Generate(),
            3 => _fallenCityGenerator.Generate(),
            4 => _demonLandsGenerator.Generate(),
            _ => throw new InvalidOperationException(
                $"Map {DungeonDepth} is not implemented.")
        };
        Region = GetRegionForDepth(DungeonDepth);
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom);
        IsInventoryOpen = false;
        _pendingFusionItem = null;
        RefreshInventoryView();
        ClampInventorySelection();
        InitializeDungeonState();
        SaveActiveSlot();
    }

    private void InitializeDungeonState()
    {
        Player.Combat.CancelActions(restoreStamina: false);
        ClearRangerUltimateLock();
        ClearGreatswordUltimateLock();
        ClearSovereignState();
        ClearAncientStaffState();
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
        LoreNotePickup = null;
        IsPigPhotoPopupOpen = false;
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
                CurrentDungeon.IsAncientCatacombs ||
                CurrentDungeon.IsFallenCity ||
                CurrentDungeon.IsDemonLands);
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom, avoidEnemies: true);
        WildForestEncounters.Reset(CurrentDungeon);
        EnemyIntroductions.Reset();
        Curse.Reset();
        Tombs.Reset(CurrentDungeon);
        ScholarTomb.Configure(CurrentDungeon);
        LastDiceTable.Configure(CurrentDungeon);
        CityAlarm.Reset();
        CollapsingCity.Reset(CurrentDungeon);
        Map03ExitPrepared = false;
        Corruption.Reset();
        CorruptionAnchors.Reset(CurrentDungeon);
        Map04ExitPrepared = false;
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
                CurrentDungeon.IsAncientCatacombs ? .55f :
                CurrentDungeon.IsFallenCity ? .56f :
                CurrentDungeon.IsDemonLands ? .57f : .58f);
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

        if (CityAlarm.TryDisableNearest(Player.Position, CurrentDungeon))
            return true;

        if (ScholarTomb.TryInspect(Player.Position))
        {
            Player.Combat.CancelActions(restoreStamina: false);
            Projectiles.Clear();
            RootHazards.Clear();
            _rottenCorpseBursts.Clear();
            return true;
        }

        if (LastDiceTable.TryOpen(Player.Position))
        {
            Player.Combat.CancelActions(restoreStamina: false);
            Projectiles.Clear();
            RootHazards.Clear();
            _rottenCorpseBursts.Clear();
            SaveActiveSlot();
            return true;
        }

        if (LoreNotePickup?.TryCollect(
            Player.Position,
            Player.Notes,
            out LoreNote collectedNote) == true)
        {
            if (collectedNote.Id == LoreCatalog.MysteriousPhotograph.Id)
                OpenPigPhoto(collectedNote);
            else
                OpenLoreNote(collectedNote);
            return true;
        }

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
            if (CurrentDungeon.IsDemonLands)
            {
                Map04ExitPrepared = true;
                return true;
            }
            if (CurrentDungeon.IsFallenCity)
            {
                Map03ExitPrepared = true;
                CompleteDungeon();
                return true;
            }
            CompleteDungeon();
            return true;
        }

        Loot.TryCollectNearest(Player.Position, Player.Inventory);
        return false;
    }

    private void OpenPigPhoto(LoreNote note)
    {
        if (note == null)
            return;

        Map2PigPhotoCollected = true;
        IsPigPhotoPopupOpen = true;
        note.MarkRead();
        _lastAcquiredLoreNote = note;
        _loreNoteFeedbackTimeRemaining = 0f;
        Player.Combat.CancelActions(restoreStamina: false);
        SaveActiveSlot();
    }

    private void OpenLoreNote(LoreNote note)
    {
        if (note == null)
            return;

        ActiveInventoryTab = InventoryTab.Notes;
        SelectedInventoryIndex = 0;
        for (int index = 0; index < Player.Notes.Count; index++)
        {
            if (ReferenceEquals(Player.Notes.Notes[index], note))
            {
                SelectedInventoryIndex = index;
                break;
            }
        }

        _selectedNoteIndex = SelectedInventoryIndex;
        _pendingFusionItem = null;
        IsInventoryOpen = true;
        note.MarkRead();
        _lastAcquiredLoreNote = note;
        _loreNoteFeedbackTimeRemaining = 2.8f;
        RefreshInventoryView();
        Player.Combat.CancelActions(restoreStamina: false);
    }

    private void UpdateScholarTombModal(
        GameTime gameTime,
        KeyboardState keyboardState)
    {
        ScholarTomb.Update(
            gameTime,
            keyboardState,
            _previousKeyboardState);
        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        Curse.Reduce(6f * elapsedSeconds);
        Player.Combat.CancelActions(restoreStamina: false);
        Projectiles.Clear();
        RootHazards.Clear();
        _rottenCorpseBursts.Clear();

        if (ScholarTomb.TryConsumeRewardRequest(
            out int score,
            out AncientScholarRewardCategory category))
        {
            GrantScholarReward(score, category);
        }

        FollowPlayerWithCamera(elapsedSeconds);
    }

    private void UpdateLastDiceTableModal(
        GameTime gameTime,
        KeyboardState keyboardState)
    {
        LastDiceTable.Update(
            gameTime,
            keyboardState,
            _previousKeyboardState);
        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        Player.Combat.CancelActions(restoreStamina: false);
        Projectiles.Clear();
        RootHazards.Clear();
        _rottenCorpseBursts.Clear();

        if (LastDiceTable.TryConsumeRewardRequest(
            out int wins,
            out int weaponIndex))
        {
            GrantLastDiceReward(wins, weaponIndex);
        }

        if (LastDiceTable.ConsumeSaveRequest())
            SaveActiveSlot();
        FollowPlayerWithCamera(elapsedSeconds);
    }

    private void GrantLastDiceReward(int wins, int weaponIndex)
    {
        EquipmentItem sample = EquipmentCatalog.CreateWeaponSample(
            Math.Clamp(
                weaponIndex,
                0,
                EquipmentCatalog.RewardWeaponSampleCount - 1));
        var reward = new EquipmentItem(
            sample.WeaponDefinition,
            LastDiceTableSystem.GetRewardRarity(wins) ?? ItemRarity.Epic);
        if (!Player.Inventory.TryAdd(reward))
            Loot.CreateRewardDrop(reward, LastDiceTable.Position, CurrentDungeon);
        RefreshInventoryView();
        SaveActiveSlot();
    }

    private void GrantScholarReward(
        int score,
        AncientScholarRewardCategory category)
    {
        EquipmentItem reward = score switch
        {
            1 => new EquipmentItem(
                EquipmentCatalog.ScoutArmor,
                ItemRarity.Uncommon,
                ArmorGrade.C2),
            2 => new EquipmentItem(
                EquipmentCatalog.KnightArmor,
                ItemRarity.Rare,
                ArmorGrade.C3),
            _ => category switch
            {
                AncientScholarRewardCategory.Weapon => new EquipmentItem(
                    EquipmentCatalog.ReckonerChainFlail,
                    ItemRarity.Legendary),
                AncientScholarRewardCategory.Relic => new EquipmentItem(
                    AncientScholarMedallion,
                    ItemRarity.Legendary,
                    ArmorGrade.C5),
                _ => new EquipmentItem(
                    EquipmentCatalog.FortressArmor,
                    ItemRarity.Legendary,
                    ArmorGrade.C5)
            }
        };

        bool stored = Player.Inventory.TryAdd(reward);
        if (!stored)
        {
            Loot.CreateRewardDrop(
                reward,
                ScholarTomb.Position,
                CurrentDungeon);
        }

        RefreshInventoryView();
        ScholarTomb.SetCompletionText(
            stored
                ? Loc.Format("scholar.reward.granted", reward.Name)
                : Loc.Format("scholar.reward.inventory_full", reward.Name));
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
        if (IsSovereignExecutionActive)
        {
            float executionTime = SovereignExecutionElapsedSeconds;
            float eyeEase = MathHelper.SmoothStep(0f, 1f, Math.Clamp(
                (executionTime - AbyssRingTuning.ExecutionSilenceEndSeconds) /
                (AbyssRingTuning.ExecutionEyeEndSeconds -
                    AbyssRingTuning.ExecutionSilenceEndSeconds),
                0f,
                1f));
            float returnEase = MathHelper.SmoothStep(0f, 1f, Math.Clamp(
                (executionTime - AbyssRingTuning.ExecutionShatterEndSeconds) /
                (AbyssRingTuning.ExecutionDurationSeconds -
                    AbyssRingTuning.ExecutionShatterEndSeconds),
                0f,
                1f));
            float executionZoomTarget = MathHelper.Lerp(
                MathHelper.Lerp(
                    1f,
                    AbyssRingTuning.ExecutionCameraZoom,
                    eyeEase),
                1f,
                returnEase);
            Vector2 executionCameraTarget = Player.Position;
            Rectangle executionTarget = SovereignExecutionTargetBounds;
            if (!executionTarget.IsEmpty)
            {
                Vector2 targetCenter = executionTarget.Center.ToVector2();
                executionCameraTarget = Vector2.Lerp(
                    Player.Position,
                    targetCenter,
                    .52f * eyeEase * (1f - returnEase));
            }
            Camera.SetCinematicZoom(executionZoomTarget, elapsedSeconds);
            Camera.Follow(
                executionCameraTarget,
                Player.Facing,
                CurrentDungeon.WorldBounds,
                elapsedSeconds,
                .42f);
            return;
        }

        if (IsAncientStaffUltimateActive)
        {
            float intro = Player.Combat.AncientStaffUltimateCinematicRatio;
            int stage = AncientStaffUltimateStage;
            float returnEase = stage == 9
                ? MathHelper.SmoothStep(
                    0f,
                    1f,
                    AncientStaffUltimateStageRatio)
                : 0f;
            float focus = intro > 0f
                ? MathHelper.SmoothStep(0f, 1f, intro)
                : 1f;
            float phaseZoom = stage switch
            {
                0 => 1.12f,
                1 => 1.08f,
                2 => 1.10f,
                >= 3 and <= 7 => 1.11f,
                8 => MathHelper.Lerp(1.12f, 1.28f,
                    MathHelper.SmoothStep(0f, 1f,
                        AncientStaffUltimateStageRatio)),
                9 => 1f,
                _ => AncientStaffTuning.UltimateCameraZoom
            };
            float staffZoom = intro > 0f
                ? MathHelper.Lerp(1f,
                    AncientStaffTuning.UltimateCameraZoom,
                    focus)
                : MathHelper.Lerp(phaseZoom, 1f, returnEase);
            float centerWeight = stage switch
            {
                0 => .54f,
                1 => .62f,
                2 => .69f,
                >= 3 and <= 7 => .64f,
                8 => .68f,
                9 => .58f * (1f - returnEase),
                _ => .48f
            };
            Vector2 staffTarget = Vector2.Lerp(
                Player.Position,
                _ancientStaffLockedCenter,
                centerWeight * focus);
            float verticalFraming = stage switch
            {
                1 => -22f,
                2 => -58f,
                >= 3 and <= 7 => -50f,
                8 => -72f,
                _ => 0f
            };
            staffTarget.Y += verticalFraming * (1f - returnEase);
            Camera.SetCinematicZoom(staffZoom, elapsedSeconds);
            Camera.Follow(
                staffTarget,
                Player.Facing,
                CurrentDungeon.WorldBounds,
                elapsedSeconds,
                stage is >= 2 and <= 8 ? .50f : .46f);
            return;
        }

        float rangerCinematic =
            Player.Combat.RangerUltimateCinematicRatio;
        float greatswordCinematic =
            Player.Combat.GreatswordUltimateCinematicRatio;
        float sovereignCinematic =
            Player.Combat.SovereignUltimateCinematicRatio;
        float scytheCinematic =
            Player.Combat.ScytheUltimateCinematicRatio;
        float cinematic = MathF.Max(
            MathF.Max(sovereignCinematic, scytheCinematic),
            MathF.Max(rangerCinematic, greatswordCinematic));
        float cinematicZoom = sovereignCinematic >=
                MathF.Max(scytheCinematic,
                    MathF.Max(rangerCinematic, greatswordCinematic))
            ? AbyssRingTuning.UltimateCameraZoom
            : scytheCinematic >= MathF.Max(rangerCinematic, greatswordCinematic)
                ? ScytheTuning.UltimateCameraZoom
            : greatswordCinematic > rangerCinematic
                ? GreatswordTuning.FinalVerdictCameraZoom
                : RangerTuning.PhantomHuntCameraZoom;
        float zoomTarget = cinematic <= 0f
            ? 1f
            : MathHelper.Lerp(
                1f,
                cinematicZoom,
                MathF.Sin(cinematic * MathHelper.PiOver2));
        Vector2 cameraTarget = Player.Position;
        Rectangle scytheTarget = ScytheUltimateTargetBounds;
        if (IsScytheUltimateActive && !scytheTarget.IsEmpty)
        {
            Vector2 targetCenter = scytheTarget.Center.ToVector2();
            int stage = ScytheUltimateStage;
            float stageRatio = ScytheUltimateStageRatio;
            if (Player.Combat.State == CombatState.ChargingHeavy)
            {
                float anticipation = MathHelper.SmoothStep(0f, 1f,
                    scytheCinematic);
                cameraTarget = Vector2.Lerp(Player.Position, targetCenter,
                    .26f + anticipation * .16f);
                zoomTarget = MathHelper.Lerp(1f,
                    ScytheTuning.UltimateCameraZoom, anticipation);
            }
            else if (stage == 0)
            {
                cameraTarget = Vector2.Lerp(Player.Position, targetCenter,
                    .46f);
                zoomTarget = 1.13f;
            }
            else if (stage is >= 1 and <= 3)
            {
                cameraTarget = targetCenter + new Vector2(0f, -12f);
                zoomTarget = stage == 3 ? 1.24f : 1.21f;
            }
            else if (stage is 4 or 5)
            {
                cameraTarget = Vector2.Lerp(targetCenter, Player.Position,
                    .22f);
                zoomTarget = 1.12f;
            }
            else if (stage == 6)
            {
                float apex = MathF.Sin(stageRatio * MathHelper.Pi);
                cameraTarget = Vector2.Lerp(targetCenter, Player.Position,
                    .30f) + new Vector2(0f, -34f * apex);
                zoomTarget = MathHelper.Lerp(1.16f, 1.27f, apex);
            }
            else
            {
                float returnEase = MathHelper.SmoothStep(0f, 1f,
                    stageRatio);
                cameraTarget = Vector2.Lerp(targetCenter, Player.Position,
                    .40f + returnEase * .60f);
                zoomTarget = MathHelper.Lerp(1.10f, 1f, returnEase);
            }
        }
        Rectangle sovereignTarget = SovereignUltimateTargetBounds;
        if (IsSovereignUltimateActive && !sovereignTarget.IsEmpty)
        {
            Vector2 targetCenter = sovereignTarget.Center.ToVector2();
            int stage = Player.Combat.TechniqueStageIndex;
            if (Player.Combat.State == CombatState.ChargingHeavy)
            {
                cameraTarget = Vector2.Lerp(
                    Player.Position,
                    targetCenter,
                    sovereignCinematic * .34f);
            }
            else if (stage == 0)
            {
                cameraTarget = Vector2.Lerp(Player.Position, targetCenter, .42f);
                zoomTarget = 1.18f;
            }
            else if (stage <= AbyssRingTuning.UltimateStrikeCount)
            {
                cameraTarget = Vector2.Lerp(targetCenter, Player.Position, .16f);
                zoomTarget = MathHelper.Lerp(1.16f, 1.08f,
                    stage / (float)AbyssRingTuning.UltimateStrikeCount);
            }
            else if (stage == AbyssRingTuning.UltimateStrikeCount + 1)
            {
                cameraTarget = targetCenter + new Vector2(0f, -42f);
                zoomTarget = 1.28f;
            }
            else
            {
                cameraTarget = Vector2.Lerp(targetCenter, Player.Position, .35f);
                zoomTarget = 1.10f;
            }
        }
        Camera.SetCinematicZoom(zoomTarget, elapsedSeconds);
        Camera.Follow(
            cameraTarget,
            Player.Facing,
            CurrentDungeon.WorldBounds,
            elapsedSeconds,
            CurrentDungeon.IsAuthoredWildForest ? .54f :
                CurrentDungeon.IsAncientCatacombs ? .55f :
                CurrentDungeon.IsFallenCity ? .56f :
                CurrentDungeon.IsDemonLands ? .57f : .58f);
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
            case DeveloperCommand.ClearCorruption:
                Corruption.Reset();
                break;
            case DeveloperCommand.ResetPlayerCombat:
                Player.ClearTemporaryStatus();
                break;
            case DeveloperCommand.GrantAllWeapons:
                Player.GrantAllWeapons();
                RefreshInventoryView();
                ClampInventorySelection();
                SaveActiveSlot();
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
            case DeveloperCommand.ResetScholarChallenge:
                ScholarTomb.ResetChallengeForDebug();
                break;
            case DeveloperCommand.ResetLastDiceChallenge:
                LastDiceTable.ResetChallengeForDebug();
                SaveActiveSlot();
                break;
            case DeveloperCommand.KillActiveEnemies:
                DeveloperKillActiveEnemies();
                break;
            case DeveloperCommand.ForcePigPhotoDrop:
                DeveloperForcePigPhotoDrop();
                break;
            case DeveloperCommand.ResetPigPhotoCollected:
                DeveloperResetPigPhoto();
                break;
            case DeveloperCommand.ToggleMap01Cleared:
                Map01Cleared = !Map01Cleared;
                break;
            case DeveloperCommand.ToggleMap02Cleared:
                Map02Cleared = !Map02Cleared;
                break;
            case DeveloperCommand.ToggleMap03Cleared:
                Map03Cleared = !Map03Cleared;
                break;
            case DeveloperCommand.ResetCitySystems:
                CityAlarm.Reset();
                CollapsingCity.Reset(CurrentDungeon);
                break;
            case DeveloperCommand.ResetCorruptionAnchors:
                CorruptionAnchors.Reset(CurrentDungeon);
                Corruption.Reset();
                break;
            case DeveloperCommand.ResetCurrentBoss:
                DeveloperResetCurrentBoss();
                break;
            case DeveloperCommand.ResetEnemyIntroductions:
                EnemyIntroductions.Reset();
                break;
            case DeveloperCommand.CycleBowFocus:
                Player.Combat.DebugCycleRangerFocus();
                break;
            case DeveloperCommand.ToggleBowUltimate:
                Player.Combat.DebugToggleRangerUltimate();
                break;
            case DeveloperCommand.ForceBowPerfectDodge:
                Player.Combat.DebugForceRangerPerfectDodge();
                break;
            case DeveloperCommand.ResetBowRuntime:
                Player.Combat.DebugResetRanger();
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

        if (CurrentDungeon.IsFallenCity)
        {
            for (int index = 0; index < CurrentDungeon.FallenCityZones.Count; index++)
                if (CurrentDungeon.FallenCityZones[index].Bounds.Contains(
                    Player.Position.ToPoint()))
                    return index;
            return FindNearestFallenCityZone();
        }

        if (CurrentDungeon.IsDemonLands)
        {
            for (int index = 0; index < CurrentDungeon.DemonLandsZones.Count; index++)
                if (CurrentDungeon.DemonLandsZones[index].Bounds.Contains(
                    Player.Position.ToPoint()))
                    return index;
            return FindNearestDemonLandsZone();
        }

        for (int index = 0; index < CurrentDungeon.WildForestSections.Count; index++)
            if (CurrentDungeon.WildForestSections[index].Bounds.Contains(Player.Position.ToPoint()))
                return index;
        return FindNearestWildForestSection();
    }

    private void DeveloperLoadMap(int mapNumber)
    {
        mapNumber = System.Math.Clamp(mapNumber, 1, 4);
        DungeonDepth = mapNumber;
        WorldTier = WorldProgression.GetWorldTier(DungeonDepth);
        Region = GetRegionForDepth(DungeonDepth);
        _worldTierTransitionTimeRemaining = 0f;
        CurrentDungeon = mapNumber switch
        {
            2 => _catacombGenerator.Generate(),
            3 => _fallenCityGenerator.Generate(),
            4 => _demonLandsGenerator.Generate(),
            _ => _dungeonGenerator.GenerateWildForest()
        };
        PlacePlayerAtMapEntry(CurrentDungeon.StartRoom);
        IsInventoryOpen = false;
        _pendingFusionItem = null;
        InitializeDungeonState();
        Player.RestoreHealth();
        Player.Combat.Stamina.Restore();
    }

    private void DeveloperTeleportToZone(int zoneIndex)
    {
        if (CurrentDungeon.IsAncientCatacombs &&
            zoneIndex == CatacombMapGenerator.ZoneCount &&
            ScholarTomb.IsAvailable)
        {
            ClearDeveloperTransientState();
            Vector2 scholarSpawn = SideScrollingCollision.ResolveSafeEntrySpawn(
                ScholarTomb.Position.X - 82f,
                Player.Size,
                CurrentDungeon,
                MapEntryDropOffset);
            Player.MoveTo(scholarSpawn);
            Player.RestoreHealth();
            Player.Combat.Stamina.Restore();
            _lastSafePlayerPosition = scholarSpawn;
            _mapEntryFallActive = true;
            Camera.Snap(
                Player.Position,
                Player.Facing,
                CurrentDungeon.WorldBounds,
                .55f);
            return;
        }

        int count = CurrentDungeon.IsDemonLands
            ? CurrentDungeon.DemonLandsZones.Count
            : CurrentDungeon.IsFallenCity
                ? CurrentDungeon.FallenCityZones.Count
            : CurrentDungeon.IsAncientCatacombs
                ? CurrentDungeon.CatacombZones.Count
                : CurrentDungeon.WildForestSections.Count;
        if (count == 0)
            return;

        zoneIndex = System.Math.Clamp(zoneIndex, 0, count - 1);
        Rectangle bounds = CurrentDungeon.IsDemonLands
            ? CurrentDungeon.DemonLandsZones[zoneIndex].Bounds
            : CurrentDungeon.IsFallenCity
                ? CurrentDungeon.FallenCityZones[zoneIndex].Bounds
            : CurrentDungeon.IsAncientCatacombs
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
            CurrentDungeon.IsAuthoredWildForest ? .54f :
            CurrentDungeon.IsDemonLands ? .57f : .55f);
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

    private void DeveloperForcePigPhotoDrop()
    {
        if (CurrentDungeon?.IsAncientCatacombs != true ||
            Map2PigPhotoCollected || LoreNotePickup != null)
        {
            return;
        }

        Rectangle arenaBounds = GetFallenKnightArenaBounds();
        Vector2 footPosition = arenaBounds.Contains(Player.Bounds)
            ? new Vector2(Player.Position.X, Player.Bounds.Bottom)
            : new Vector2(
                arenaBounds.Center.X,
                CurrentDungeon.BossRoom.GroundY);
        SpawnPigPhotoPickup(footPosition, arenaBounds);
    }

    private void DeveloperResetPigPhoto()
    {
        Map2PigPhotoCollected = false;
        IsPigPhotoPopupOpen = false;
        if (LoreNotePickup?.Definition.Id ==
            LoreCatalog.MysteriousPhotograph.Id)
        {
            LoreNotePickup = null;
        }
        Player.Notes.Remove(LoreCatalog.MysteriousPhotograph.Id);
        _lastAcquiredLoreNote = null;
        _loreNoteFeedbackTimeRemaining = 0f;
        RefreshInventoryView();
        SaveActiveSlot();
    }

    private void DeveloperResetFallenKnightEncounter()
    {
        if (CurrentDungeon?.IsAncientCatacombs != true ||
            !WildForestEncounters.ResetZone("fallen-knight", Enemies))
        {
            return;
        }

        if (LoreNotePickup?.Definition.Id ==
            LoreCatalog.MysteriousPhotograph.Id)
        {
            LoreNotePickup = null;
        }
        Map02Cleared = false;
        BossDefeated = false;
        ClearDeveloperTransientState();
        SaveActiveSlot();
    }

    private void DeveloperResetCurrentBoss()
    {
        if (CurrentDungeon.IsAncientCatacombs)
        {
            DeveloperResetFallenKnightEncounter();
            return;
        }

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
        if (zoneId == "broken-king")
        {
            Map03Cleared = false;
            BossDefeated = false;
            Map03ExitPrepared = false;
        }
        else if (zoneId == "horned-general")
        {
            Map04Cleared = false;
            BossDefeated = false;
            Map04ExitPrepared = false;
            CorruptionAnchors.Reset(CurrentDungeon);
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
        string sectionId = CurrentDungeon.IsDemonLands
            ? CurrentDungeon.DemonLandsZones[index].ZoneId
            : CurrentDungeon.IsFallenCity
                ? CurrentDungeon.FallenCityZones[index].ZoneId
            : CurrentDungeon.IsAncientCatacombs
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
        Player.Combat.CancelActions(restoreStamina: false);
        ClearRangerUltimateLock();
        ClearGreatswordUltimateLock();
        ClearSovereignState();
        ClearAncientStaffState();
        Projectiles.Clear();
        GroundRunes.Clear();
        RootHazards.Clear();
        ClearDemonRitualEffects();
        ClearBreakerEffects();
        _rottenCorpseBursts.Clear();
        Enemies.ClearTemporaryBuffs();
        CityAlarm.Reset();
        CollapsingCity.Reset(CurrentDungeon);
        Player.ClearTemporaryStatus();
    }

    private void ClearDemonRitualEffects()
    {
        CorruptionAnchors.ClearPriestBlessings();
        CorruptionAnchors.ClearBossAnchors();
        foreach (Enemy enemy in Enemies.Enemies)
            if (enemy is DemonLandsEnemy demon)
                demon.ClearEncounterTransientEffects();
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
        return CurrentDungeon.IsDemonLands
            ? Loc.Get(CurrentDungeon.DemonLandsZones[index].NameKey)
            : CurrentDungeon.IsFallenCity
                ? Loc.Get(CurrentDungeon.FallenCityZones[index].NameKey)
            : CurrentDungeon.IsAncientCatacombs
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

    private int FindNearestFallenCityZone()
    {
        int result = 0;
        float distance = float.MaxValue;
        for (int index = 0; index < CurrentDungeon.FallenCityZones.Count; index++)
        {
            float candidate = MathF.Abs(
                CurrentDungeon.FallenCityZones[index].Bounds.Center.X -
                Player.Position.X);
            if (candidate < distance)
            {
                distance = candidate;
                result = index;
            }
        }
        return result;
    }

    private int FindNearestDemonLandsZone()
    {
        int result = 0;
        float distance = float.MaxValue;
        for (int index = 0; index < CurrentDungeon.DemonLandsZones.Count; index++)
        {
            float candidate = MathF.Abs(
                CurrentDungeon.DemonLandsZones[index].Bounds.Center.X -
                Player.Position.X);
            if (candidate < distance)
            {
                distance = candidate;
                result = index;
            }
        }
        return result;
    }

    private static RegionDefinition GetRegionForDepth(int dungeonDepth) =>
        dungeonDepth switch
        {
            2 => RegionDefinition.AncientCatacombs,
            3 => RegionDefinition.FallenCity,
            >= 4 => RegionDefinition.DemonLands,
            _ => RegionDefinition.WildForest
        };

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
        if (CurrentDungeon.IsAuthoredWildForest &&
            !Player.Notes.Contains(LoreCatalog.StarBornHero.Id) &&
            LoreNotePickup == null)
        {
            LoreNotePickup = new LoreNotePickup(
                LoreCatalog.StarBornHero,
                SideScrollingCollision.PlaceOnGround(
                    Boss.Position.X,
                    DungeonAscendant.World.LoreNotePickup.Size,
                    CurrentDungeon.BossRoom));
        }
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

        if (Player.WeaponFamily == WeaponFamily.GreatSword &&
            Player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordWhirlingJudgment &&
            Player.Combat.TechniqueStageIndex < 6)
        {
            var arcFacing = Player.Combat.TechniqueStageIndex % 2 == 0
                ? Player.Facing
                : Player.Facing ==
                    DungeonAscendant.Player.FacingDirection.Left
                    ? DungeonAscendant.Player.FacingDirection.Right
                    : DungeonAscendant.Player.FacingDirection.Left;
            PlayerAttackArea = MeleeHitArea.Create(
                Player.Position,
                Player.Size,
                arcFacing,
                Player.Combat.CalculateRange(attack),
                attack.Thickness,
                attack.HitboxShape);
            return;
        }

        if (Player.WeaponFamily == WeaponFamily.GreatSword &&
            Player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordKingsRupture &&
            Player.Combat.TechniqueStageIndex == 2)
        {
            PlayerAttackArea = CreateGreatswordGroundFractureBounds();
            return;
        }

        if (Player.WeaponFamily == WeaponFamily.GreatSword &&
            Player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFinalVerdict &&
            Player.Combat.TechniqueStageIndex is >= 1 and <= 3)
        {
            Rectangle lockedBounds = GetCombatTargetBounds(
                _greatswordUltimateLockedTarget);
            float maximumContactDistance =
                Player.Combat.CalculateRange(attack) +
                lockedBounds.Width * .5f + Player.Size.X * .5f;
            if (!lockedBounds.IsEmpty &&
                Vector2.DistanceSquared(
                    Player.Position,
                    lockedBounds.Center.ToVector2()) <=
                    maximumContactDistance * maximumContactDistance)
            {
                lockedBounds.Inflate(18, 14);
                PlayerAttackArea = lockedBounds;
                return;
            }
        }

        if (Player.WeaponFamily == WeaponFamily.AbyssRing &&
            Player.Combat.CurrentTechnique != null)
        {
            WeaponTechniqueEffect effect =
                Player.Combat.CurrentTechnique.Effect;
            int stage = Player.Combat.TechniqueStageIndex;
            bool portalContact = effect ==
                    WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                    stage <= 17 ||
                effect == WeaponTechniqueEffect.SovereignMartialArts &&
                    stage is 2 or 4 or 6 or 7 ||
                effect == WeaponTechniqueEffect.SovereignParadoxImpact &&
                    stage is >= 1 and <= 3;
            object targetObject = effect ==
                    WeaponTechniqueEffect.SovereignGoldenCataclysm
                ? _sovereignUltimateLockedTarget
                : _sovereignActionTarget;
            Rectangle portalTarget = GetCombatTargetBounds(targetObject);
            if (portalContact && !portalTarget.IsEmpty)
            {
                portalTarget.Inflate(14, 10);
                PlayerAttackArea = portalTarget;
                return;
            }
        }

        if (Player.WeaponFamily == WeaponFamily.Scythe &&
            Player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ScytheUmbraDevourer)
        {
            Rectangle lockedBounds = GetCombatTargetBounds(
                _scytheUltimateLockedTarget);
            if (!lockedBounds.IsEmpty)
            {
                lockedBounds.Inflate(16, 12);
                PlayerAttackArea = lockedBounds;
                return;
            }
        }

        PlayerAttackArea = MeleeHitArea.Create(
            Player.Position,
            Player.Size,
            Player.Facing,
            Player.Combat.CalculateRange(attack),
            attack.Thickness,
            attack.HitboxShape);
    }

    private Rectangle CreateGreatswordGroundFractureBounds()
    {
        float direction = Player.Facing ==
            DungeonAscendant.Player.FacingDirection.Left ? -1f : 1f;
        float reached = 0f;
        float feetY = Player.Bounds.Bottom;
        for (float distance = GreatswordTuning.GroundFractureProbeStep;
             distance <= GreatswordTuning.GroundFractureRange;
             distance += GreatswordTuning.GroundFractureProbeStep)
        {
            Vector2 probe = new(
                Player.Position.X + direction * distance,
                feetY - 5f);
            Vector2 probeSize = new(8f, 8f);
            if (!SideScrollingCollision.IsPositionFree(
                    probe,
                    probeSize,
                    CurrentDungeon) ||
                !SideScrollingCollision.IsSupported(
                    probe,
                    probeSize,
                    CurrentDungeon))
                break;
            reached = distance;
        }

        int width = Math.Max(1, (int)MathF.Ceiling(reached));
        int height = 34;
        int y = (int)MathF.Round(feetY) - height;
        return direction < 0f
            ? new Rectangle(
                (int)MathF.Round(Player.Position.X) - width,
                y,
                width,
                height)
            : new Rectangle(
                (int)MathF.Round(Player.Position.X),
                y,
                width,
                height);
    }

    private void ProcessGreatswordWorldEffect(WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.GreatSword ||
            technique == null ||
            _greatswordWorldEffectAttackId == Player.Combat.AttackId)
            return;

        WeaponTechniqueEffect effect = technique.Effect;
        int stage = Player.Combat.TechniqueStageIndex;
        if (effect == WeaponTechniqueEffect.GreatswordKingsRupture &&
            stage == 2)
        {
            _greatswordWorldEffectAttackId = Player.Combat.AttackId;
            Player.Combat.RegisterGreatswordImpactFeedback(ultimate: false);
            Camera.AddImpactFeedback(2.1f);
            return;
        }

        if (effect != WeaponTechniqueEffect.GreatswordFinalVerdict ||
            stage != 4)
            return;

        _greatswordWorldEffectAttackId = Player.Combat.AttackId;
        Player.Combat.RegisterGreatswordImpactFeedback(ultimate: true);
        Camera.AddImpactFeedback(4.2f);
        const float radius = 88f;
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted ||
                enemy.WeightClass == EnemyWeightClass.Heavy ||
                Vector2.DistanceSquared(Player.Position, enemy.Position) >
                    radiusSquared)
                continue;
            enemy.ApplyKnockback(Player.Position, 24f, CurrentDungeon);
        }
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
            Player.Combat.CurrentAttack == null ||
            _rangerHopAttackId == Player.Combat.AttackId)
            return;

        WeaponTechniqueEffect effect =
            Player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        bool shouldHop = effect ==
                WeaponTechniqueEffect.RangerPredatorsHorizon &&
                Player.Combat.TechniqueStageIndex == 0 ||
            effect == WeaponTechniqueEffect.RangerPhantomHunt &&
                Player.Combat.TechniqueStageIndex == 0;
        if (!shouldHop)
            return;

        _rangerHopAttackId = Player.Combat.AttackId;
        Player.BeginRangerHop(effect == WeaponTechniqueEffect.RangerPhantomHunt
            ? -420f
            : -365f);
    }

    private void UpdateRangerUltimateCandidate()
    {
        _rangerUltimateCandidate = null;
        if (Player?.WeaponFamily != WeaponFamily.HunterBow ||
            CurrentDungeon == null)
            return;

        float maximumDistanceSquared =
            RangerTuning.PhantomHuntTargetRange *
            RangerTuning.PhantomHuntTargetRange;
        int bestMaximumHealth = -1;
        float bestDistanceSquared = float.MaxValue;
        int bestStableId = int.MaxValue;

        for (int index = 0; index < Enemies.Enemies.Count; index++)
        {
            Enemy enemy = Enemies.Enemies[index];
            if (!CanDamageAncientStaffEnemy(enemy) ||
                !Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                continue;

            float distanceSquared = Vector2.DistanceSquared(
                Player.Position,
                enemy.Position);
            if (distanceSquared > maximumDistanceSquared ||
                !HasClearRangerUltimatePath(enemy.MeleeTargetBounds))
                continue;

            if (IsBetterRangerUltimateTarget(
                enemy.MaxHealth,
                distanceSquared,
                index,
                bestMaximumHealth,
                bestDistanceSquared,
                bestStableId))
            {
                _rangerUltimateCandidate = enemy;
                bestMaximumHealth = enemy.MaxHealth;
                bestDistanceSquared = distanceSquared;
                bestStableId = index;
            }
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated &&
            !Boss.IsPhaseTransitioning)
        {
            float distanceSquared = Vector2.DistanceSquared(
                Player.Position,
                Boss.Position);
            int stableId = Enemies.Enemies.Count;
            if (distanceSquared <= maximumDistanceSquared &&
                HasClearRangerUltimatePath(Boss.Bounds) &&
                IsBetterRangerUltimateTarget(
                    Boss.MaxHealth,
                    distanceSquared,
                    stableId,
                    bestMaximumHealth,
                    bestDistanceSquared,
                    bestStableId))
            {
                _rangerUltimateCandidate = Boss;
            }
        }
    }

    private void UpdateScytheUltimateCandidate()
    {
        _scytheUltimateCandidate = null;
        if (Player?.WeaponFamily != WeaponFamily.Scythe || CurrentDungeon == null)
            return;

        float maximumDistanceSquared = ScytheTuning.UltimateTargetRange *
            ScytheTuning.UltimateTargetRange;
        int bestMaximumHealth = -1;
        float bestDistanceSquared = float.MaxValue;
        int bestStableId = int.MaxValue;
        for (int index = 0; index < Enemies.Enemies.Count; index++)
        {
            Enemy enemy = Enemies.Enemies[index];
            if (!enemy.CanBeTargeted ||
                !Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                continue;
            float distanceSquared = Vector2.DistanceSquared(
                Player.Position, enemy.Position);
            if (distanceSquared > maximumDistanceSquared ||
                !HasClearRangerUltimatePath(enemy.MeleeTargetBounds))
                continue;
            if (!IsBetterRangerUltimateTarget(
                enemy.MaxHealth, distanceSquared, index,
                bestMaximumHealth, bestDistanceSquared, bestStableId))
                continue;
            _scytheUltimateCandidate = enemy;
            bestMaximumHealth = enemy.MaxHealth;
            bestDistanceSquared = distanceSquared;
            bestStableId = index;
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated &&
            !Boss.IsPhaseTransitioning)
        {
            float distanceSquared = Vector2.DistanceSquared(
                Player.Position, Boss.Position);
            if (distanceSquared <= maximumDistanceSquared &&
                HasClearRangerUltimatePath(Boss.Bounds) &&
                IsBetterRangerUltimateTarget(
                    Boss.MaxHealth, distanceSquared, Enemies.Enemies.Count,
                    bestMaximumHealth, bestDistanceSquared, bestStableId))
                _scytheUltimateCandidate = Boss;
        }
    }

    private void UpdateScytheUltimateLock()
    {
        bool active = Player.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.ScytheUmbraDevourer;
        if (!active)
        {
            ClearScytheUltimateLock();
            return;
        }

        if (_scytheUltimateTechniqueUseId !=
            Player.Combat.CurrentTechniqueUseId)
        {
            _scytheUltimateTechniqueUseId = Player.Combat.CurrentTechniqueUseId;
            _scytheUltimateLockedTarget = _scytheUltimateCandidate;
            _scytheSafeReturnPosition = Player.Position;
            _scytheUltimateVisualStartPosition = Player.Position;
            _scytheSafeReturnRequested = false;
            _scytheUltimateMotionAttackId = -1;
        }

        bool valid = IsScytheUltimateTargetValid(_scytheUltimateLockedTarget);
        if (!valid && !_scytheSafeReturnRequested)
        {
            _scytheSafeReturnRequested = true;
            Player.Combat.RequestScytheSafeReturn();
        }
    }

    private bool IsScytheUltimateTargetValid(object target)
    {
        if (target is Enemy enemy)
            return enemy.CanBeTargeted && IsActiveEnemy(enemy);
        return target is AncientTreant boss && ReferenceEquals(boss, Boss) &&
            ShouldRenderBoss && boss.IsAlive && !boss.IsPhaseTransitioning;
    }

    private void UpdateScytheTechniqueMovement()
    {
        if (Player?.Combat.CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ScytheUmbraDevourer ||
            CurrentDungeon == null ||
            _scytheUltimateMotionAttackId == Player.Combat.AttackId)
            return;

        _scytheUltimateMotionAttackId = Player.Combat.AttackId;
        int stage = Player.Combat.TechniqueStageIndex;
        _scytheUltimateVisualStartPosition = Player.Position;
        if (stage == 7)
        {
            Player.MoveThroughCombatTechnique(
                _scytheSafeReturnPosition, CurrentDungeon);
            return;
        }

        Rectangle target = GetCombatTargetBounds(_scytheUltimateLockedTarget);
        if (target.IsEmpty || stage is >= 1 and <= 3)
            return;

        float side = Player.Position.X <= target.Center.X ? -1f : 1f;
        if (stage is 4 or 5)
            side = -side;
        Vector2 desired = new(
            target.Center.X + side *
                (target.Width * .5f + ScytheTuning.UltimateSafeOffset),
            Player.Position.Y);
        Player.MoveThroughCombatTechnique(desired, CurrentDungeon);
        Player.FaceToward(target.Center.X);
    }

    private void ClearScytheUltimateLock()
    {
        _scytheUltimateCandidate = null;
        _scytheUltimateLockedTarget = null;
        _scytheUltimateTechniqueUseId = -1;
        _scytheUltimateMotionAttackId = -1;
        _scytheSafeReturnRequested = false;
        _scytheUltimateVisualStartPosition = Vector2.Zero;
        Player?.Combat.SetScytheUltimateTargetAvailable(false);
        Camera?.ResetCinematicZoom();
    }

    private void UpdateRangerUltimateLock()
    {
        bool active = Player.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.RangerPhantomHunt;
        if (!active)
        {
            ClearRangerUltimateLock();
            return;
        }

        if (_rangerUltimateTechniqueUseId !=
            Player.Combat.CurrentTechniqueUseId)
        {
            _rangerUltimateTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
            _rangerUltimateLockedTarget = _rangerUltimateCandidate;
            _rangerUltimateLandingRequested = false;
        }

        if (!_rangerUltimateLandingRequested &&
            !IsRangerUltimateTargetValid(_rangerUltimateLockedTarget))
        {
            _rangerUltimateLandingRequested = true;
            Player.Combat.RequestRangerUltimateLanding();
        }
    }

    private bool IsRangerUltimateTargetValid(object target)
    {
        if (target is Enemy enemy)
        {
            for (int index = 0; index < Enemies.Enemies.Count; index++)
                if (ReferenceEquals(Enemies.Enemies[index], enemy))
                    return enemy.CanBeTargeted;
            return false;
        }

        return target is AncientTreant boss &&
            ReferenceEquals(boss, Boss) && ShouldRenderBoss && boss.IsAlive &&
            !boss.IsPhaseTransitioning;
    }

    private bool HasClearRangerUltimatePath(Rectangle targetBounds)
    {
        Vector2 start = Player.Position + new Vector2(0f, -10f);
        Vector2 end = targetBounds.Center.ToVector2();
        float distance = Vector2.Distance(start, end);
        int steps = Math.Max(2, (int)MathF.Ceiling(distance / 12f));
        for (int step = 1; step < steps; step++)
        {
            Vector2 sample = Vector2.Lerp(start, end, step / (float)steps);
            if (!SideScrollingCollision.IsPositionFree(
                sample,
                new Vector2(4f, 4f),
                CurrentDungeon))
                return false;
        }
        return true;
    }

    private static bool IsBetterRangerUltimateTarget(
        int maximumHealth,
        float distanceSquared,
        int stableId,
        int bestMaximumHealth,
        float bestDistanceSquared,
        int bestStableId)
    {
        if (maximumHealth != bestMaximumHealth)
            return maximumHealth > bestMaximumHealth;
        if (MathF.Abs(distanceSquared - bestDistanceSquared) > .01f)
            return distanceSquared < bestDistanceSquared;
        return stableId < bestStableId;
    }

    private static Rectangle GetRangerTargetBounds(object target)
    {
        return target switch
        {
            Enemy enemy => enemy.MeleeTargetBounds,
            AncientTreant boss => boss.Bounds,
            _ => Rectangle.Empty
        };
    }

    private static GameTime ScaleGameTime(GameTime gameTime, float scale)
    {
        scale = Math.Clamp(scale, .05f, 1f);
        if (scale >= .999f)
            return gameTime;
        return new GameTime(
            gameTime.TotalGameTime,
            TimeSpan.FromTicks((long)(gameTime.ElapsedGameTime.Ticks * scale)));
    }

    private void ClearRangerUltimateLock()
    {
        _rangerUltimateCandidate = null;
        _rangerUltimateLockedTarget = null;
        _rangerUltimateTechniqueUseId = -1;
        _rangerUltimateLandingRequested = false;
        Player?.Combat.SetRangerUltimateTargetAvailable(false);
        Camera?.ResetCinematicZoom();
    }

    private void UpdateGreatswordUltimateCandidate()
    {
        if (Player?.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.GreatswordFinalVerdict)
            return;

        _greatswordUltimateCandidate = null;
        if (Player?.WeaponFamily != WeaponFamily.GreatSword ||
            CurrentDungeon == null)
            return;

        float maximumDistanceSquared =
            GreatswordTuning.FinalVerdictTargetRange *
            GreatswordTuning.FinalVerdictTargetRange;
        int bestMaximumHealth = -1;
        float bestDistanceSquared = float.MaxValue;
        int bestStableId = int.MaxValue;
        Vector2 bestApproach = Vector2.Zero;
        Vector2 bestLanding = Vector2.Zero;

        for (int index = 0; index < Enemies.Enemies.Count; index++)
        {
            Enemy enemy = Enemies.Enemies[index];
            if (!enemy.CanBeTargeted ||
                !Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                continue;

            float distanceSquared = Vector2.DistanceSquared(
                Player.Position,
                enemy.Position);
            if (distanceSquared > maximumDistanceSquared ||
                !TryPlanGreatswordUltimate(
                    enemy.MeleeTargetBounds,
                    out Vector2 approach,
                    out Vector2 landing))
                continue;

            if (IsBetterRangerUltimateTarget(
                enemy.MaxHealth,
                distanceSquared,
                index,
                bestMaximumHealth,
                bestDistanceSquared,
                bestStableId))
            {
                _greatswordUltimateCandidate = enemy;
                bestMaximumHealth = enemy.MaxHealth;
                bestDistanceSquared = distanceSquared;
                bestStableId = index;
                bestApproach = approach;
                bestLanding = landing;
            }
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated &&
            !Boss.IsPhaseTransitioning)
        {
            float distanceSquared = Vector2.DistanceSquared(
                Player.Position,
                Boss.Position);
            int stableId = Enemies.Enemies.Count;
            if (distanceSquared <= maximumDistanceSquared &&
                TryPlanGreatswordUltimate(
                    Boss.Bounds,
                    out Vector2 approach,
                    out Vector2 landing) &&
                IsBetterRangerUltimateTarget(
                    Boss.MaxHealth,
                    distanceSquared,
                    stableId,
                    bestMaximumHealth,
                    bestDistanceSquared,
                    bestStableId))
            {
                _greatswordUltimateCandidate = Boss;
                bestApproach = approach;
                bestLanding = landing;
            }
        }

        if (_greatswordUltimateCandidate != null)
        {
            _greatswordUltimateApproachPosition = bestApproach;
            _greatswordUltimateLandingPosition = bestLanding;
        }
    }

    private void UpdateGreatswordUltimateLock()
    {
        bool active = Player.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.GreatswordFinalVerdict;
        if (!active)
        {
            ClearGreatswordUltimateLock();
            return;
        }

        if (_greatswordUltimateTechniqueUseId !=
            Player.Combat.CurrentTechniqueUseId)
        {
            _greatswordUltimateTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
            _greatswordUltimateLockedTarget =
                _greatswordUltimateCandidate;
            _greatswordUltimateLandingRequested = false;
            _greatswordUltimateMotionAttackId = -1;
        }

        if (!_greatswordUltimateLandingRequested &&
            !IsGreatswordUltimateTargetValid(
                _greatswordUltimateLockedTarget))
        {
            _greatswordUltimateLandingRequested = true;
            Player.Combat.RequestGreatswordUltimateLanding();
        }
    }

    private bool IsGreatswordUltimateTargetValid(object target)
    {
        if (target is Enemy enemy)
        {
            for (int index = 0; index < Enemies.Enemies.Count; index++)
            {
                if (ReferenceEquals(Enemies.Enemies[index], enemy))
                {
                    return enemy.CanBeTargeted &&
                        Enemies.IsActive(
                            enemy,
                            Player.Position,
                            CurrentDungeon);
                }
            }
            return false;
        }

        return target is AncientTreant boss &&
            ReferenceEquals(boss, Boss) && ShouldRenderBoss && boss.IsAlive &&
            boss.IsActivated && !boss.IsPhaseTransitioning;
    }

    private bool TryPlanGreatswordUltimate(
        Rectangle targetBounds,
        out Vector2 approach,
        out Vector2 landing)
    {
        approach = Vector2.Zero;
        landing = Vector2.Zero;
        float preferredSide = Player.Position.X <= targetBounds.Center.X
            ? -1f
            : 1f;
        float halfWidth = Player.Size.X * .5f;
        float baseOffset = targetBounds.Width * .5f + halfWidth + 18f;
        float startingY = targetBounds.Bottom - Player.Size.Y * .5f;
        float maximumY = MathF.Min(
            CurrentDungeon.WorldBounds.Bottom - Player.Size.Y * .5f,
            startingY + 144f);

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            float side = sideIndex == 0 ? preferredSide : -preferredSide;
            for (int extra = 0; extra <= 48; extra += 16)
            {
                float x = targetBounds.Center.X +
                    side * (baseOffset + extra);
                for (float y = startingY; y <= maximumY; y += 4f)
                {
                    Vector2 candidate = new(x, y);
                    Rectangle candidateBounds =
                        DungeonCollision.CreateBounds(candidate, Player.Size);
                    if (candidateBounds.Intersects(targetBounds) ||
                        IsGreatswordLandingBlocked(
                            candidateBounds,
                            targetBounds) ||
                        !SideScrollingCollision.IsPositionFree(
                            candidate,
                            Player.Size,
                            CurrentDungeon) ||
                        !SideScrollingCollision.IsSupported(
                            candidate,
                            Player.Size,
                            CurrentDungeon) ||
                        !HasClearGreatswordUltimatePath(candidate))
                        continue;

                    approach = candidate;
                    landing = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsGreatswordLandingBlocked(
        Rectangle candidateBounds,
        Rectangle targetBounds)
    {
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted ||
                enemy.MeleeTargetBounds == targetBounds)
                continue;
            if (candidateBounds.Intersects(enemy.MeleeTargetBounds))
                return true;
        }

        return ShouldRenderBoss && Boss?.IsAlive == true &&
            Boss.Bounds != targetBounds &&
            candidateBounds.Intersects(Boss.Bounds);
    }

    private bool HasClearGreatswordUltimatePath(Vector2 destination)
    {
        Vector2 start = Player.Position - new Vector2(0f, 20f);
        Vector2 end = destination - new Vector2(0f, 20f);
        float distance = Vector2.Distance(start, end);
        int steps = Math.Max(2, (int)MathF.Ceiling(distance / 10f));
        Vector2 probeSize = new(
            MathF.Max(12f, Player.Size.X * .72f),
            MathF.Max(20f, Player.Size.Y * .62f));
        for (int step = 1; step < steps; step++)
        {
            Vector2 sample = Vector2.Lerp(start, end, step / (float)steps);
            if (!SideScrollingCollision.IsPositionFree(
                sample,
                probeSize,
                CurrentDungeon))
                return false;
        }
        return true;
    }

    private void UpdateGreatswordUltimateMovement(float elapsedSeconds)
    {
        if (Player.Combat.CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.GreatswordFinalVerdict ||
            Player.Combat.CurrentAttack == null)
            return;

        int stage = Player.Combat.TechniqueStageIndex;
        if (_greatswordUltimateLockedTarget != null)
        {
            Rectangle target = GetCombatTargetBounds(
                _greatswordUltimateLockedTarget);
            if (!target.IsEmpty)
                Player.FaceToward(target.Center.X);
        }

        if (stage == 0 && _greatswordUltimateMotionAttackId !=
                Player.Combat.AttackId)
        {
            _greatswordUltimateMotionAttackId = Player.Combat.AttackId;
            Player.BeginGreatswordUltimateLeap(-365f);
        }

        if (stage != 0 && stage !=
                Player.Combat.EffectiveTechniqueStageCount - 1)
            return;

        Vector2 destination = stage == 0
            ? _greatswordUltimateApproachPosition
            : ResolveGreatswordLandingFallback();
        float horizontal = destination.X - Player.Position.X;
        float movement = MathF.Min(
            MathF.Abs(horizontal),
            GreatswordTuning.FinalVerdictApproachSpeed *
                MathF.Max(0f, elapsedSeconds));
        if (movement > .01f)
        {
            Player.MoveThroughCombatTechnique(
                Player.Position + new Vector2(
                    MathF.Sign(horizontal) * movement,
                    0f),
                CurrentDungeon);
        }
    }

    private Vector2 ResolveGreatswordLandingFallback()
    {
        Rectangle targetBounds = GetCombatTargetBounds(
            _greatswordUltimateLockedTarget);
        bool avoidsTarget = targetBounds.IsEmpty ||
            !DungeonCollision.CreateBounds(
                _greatswordUltimateLandingPosition,
                Player.Size).Intersects(targetBounds);
        bool plannedSafe = SideScrollingCollision.IsPositionFree(
                _greatswordUltimateLandingPosition,
                Player.Size,
                CurrentDungeon) &&
            SideScrollingCollision.IsSupported(
                _greatswordUltimateLandingPosition,
                Player.Size,
                CurrentDungeon) && avoidsTarget;
        if (plannedSafe)
            return _greatswordUltimateLandingPosition;

        bool lastAvoidsTarget = targetBounds.IsEmpty ||
            !DungeonCollision.CreateBounds(
                _lastSafePlayerPosition,
                Player.Size).Intersects(targetBounds);
        bool lastSafe = SideScrollingCollision.IsPositionFree(
                _lastSafePlayerPosition,
                Player.Size,
                CurrentDungeon) &&
            SideScrollingCollision.IsSupported(
                _lastSafePlayerPosition,
                Player.Size,
                CurrentDungeon) && lastAvoidsTarget;
        return lastSafe ? _lastSafePlayerPosition : Player.Position;
    }

    private void ClearGreatswordUltimateLock()
    {
        _greatswordUltimateCandidate = null;
        _greatswordUltimateLockedTarget = null;
        _greatswordUltimateApproachPosition = Vector2.Zero;
        _greatswordUltimateLandingPosition = Vector2.Zero;
        _greatswordUltimateTechniqueUseId = -1;
        _greatswordUltimateMotionAttackId = -1;
        _greatswordUltimateLandingRequested = false;
        Player?.Combat.SetGreatswordUltimateTargetAvailable(false);
        Camera?.ResetCinematicZoom();
    }

    private void UpdateSovereignUltimateCandidate()
    {
        if (Player?.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm)
            return;

        _sovereignUltimateCandidate = FindSovereignTarget(
            AbyssRingTuning.UltimateTargetRange,
            preferHighestHealth: true);
    }

    private object FindSovereignTarget(
        float maximumRange,
        bool preferHighestHealth)
    {
        if (Player?.WeaponFamily != WeaponFamily.AbyssRing ||
            CurrentDungeon == null)
            return null;

        float maximumDistanceSquared = maximumRange * maximumRange;
        object best = null;
        int bestHealth = -1;
        float bestDistance = float.MaxValue;
        int bestStableId = int.MaxValue;
        for (int index = 0; index < Enemies.Enemies.Count; index++)
        {
            Enemy enemy = Enemies.Enemies[index];
            if (!enemy.CanBeTargeted ||
                !Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                continue;
            float distance = Vector2.DistanceSquared(
                Player.Position, enemy.Position);
            if (distance > maximumDistanceSquared ||
                !HasClearRangerUltimatePath(enemy.MeleeTargetBounds) ||
                preferHighestHealth &&
                    !HasSovereignSafeTransitPlan(enemy.MeleeTargetBounds))
                continue;
            bool better = preferHighestHealth
                ? IsBetterRangerUltimateTarget(
                    enemy.MaxHealth, distance, index,
                    bestHealth, bestDistance, bestStableId)
                : distance < bestDistance;
            if (!better)
                continue;
            best = enemy;
            bestHealth = enemy.MaxHealth;
            bestDistance = distance;
            bestStableId = index;
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated &&
            !Boss.IsPhaseTransitioning)
        {
            float distance = Vector2.DistanceSquared(
                Player.Position, Boss.Position);
            int stableId = Enemies.Enemies.Count;
            bool better = preferHighestHealth
                ? IsBetterRangerUltimateTarget(
                    Boss.MaxHealth, distance, stableId,
                    bestHealth, bestDistance, bestStableId)
                : distance < bestDistance;
            if (distance <= maximumDistanceSquared && better &&
                HasClearRangerUltimatePath(Boss.Bounds) &&
                (!preferHighestHealth ||
                    HasSovereignSafeTransitPlan(Boss.Bounds)))
                best = Boss;
        }
        return best;
    }

    private bool HasSovereignSafeTransitPlan(Rectangle targetBounds)
    {
        if (targetBounds.IsEmpty || CurrentDungeon == null ||
            !SideScrollingCollision.IsPositionFree(
                Player.Position,
                Player.Size,
                CurrentDungeon))
        {
            return false;
        }

        Vector2 center = targetBounds.Center.ToVector2();
        for (int index = 0; index < 8; index++)
        {
            float angle = MathHelper.TwoPi * index / 8f;
            Vector2 desired = center + new Vector2(
                MathF.Cos(angle) * 116f,
                MathF.Sin(angle) * 64f);
            if (TryResolveSovereignTransit(
                desired,
                targetBounds,
                out _))
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateSovereignTargetLock()
    {
        if (Player?.WeaponFamily != WeaponFamily.AbyssRing)
        {
            ClearSovereignState();
            return;
        }

        WeaponTechnique technique = Player.Combat.CurrentTechnique;
        bool ultimate = technique?.Effect ==
            WeaponTechniqueEffect.SovereignGoldenCataclysm;
        if (ultimate)
        {
            if (_sovereignExecutionTechniqueUseId ==
                Player.Combat.CurrentTechniqueUseId)
                return;

            if (_sovereignUltimateTechniqueUseId !=
                Player.Combat.CurrentTechniqueUseId)
            {
                _sovereignUltimateTechniqueUseId =
                    Player.Combat.CurrentTechniqueUseId;
                _sovereignUltimateLockedTarget = _sovereignUltimateCandidate;
                _sovereignSafeExitPosition = Player.Position;
                _sovereignSafeExitRequested = false;
                Rectangle target = GetCombatTargetBounds(
                    _sovereignUltimateLockedTarget);
                if (!target.IsEmpty)
                {
                    SovereignPortals.Clear();
                    SovereignPortals.OpenGoldenGateArray(
                        target.Center.ToVector2());
                }
            }
            if (!_sovereignSafeExitRequested &&
                !IsSovereignTargetValid(_sovereignUltimateLockedTarget))
            {
                _sovereignSafeExitRequested = true;
                Player.Combat.RequestSovereignSafeExit();
            }
            else if (!SovereignPortals.HasUltimateConstellation)
            {
                Rectangle target = GetCombatTargetBounds(
                    _sovereignUltimateLockedTarget);
                if (!target.IsEmpty)
                    SovereignPortals.OpenGoldenGateArray(
                        target.Center.ToVector2());
            }
            return;
        }

        if (_sovereignUltimateTechniqueUseId >= 0)
        {
            SovereignPortals.Clear();
            Camera.ResetCinematicZoom();
        }
        _sovereignUltimateLockedTarget = null;
        _sovereignUltimateTechniqueUseId = -1;
        if (technique == null)
        {
            _sovereignActionTarget = null;
            _sovereignActionTechniqueUseId = -1;
            return;
        }
        if (_sovereignActionTechniqueUseId !=
            Player.Combat.CurrentTechniqueUseId)
        {
            _sovereignActionTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
            _sovereignActionTarget = FindSovereignTarget(
                AbyssRingTuning.StrikePortalRange *
                    (Player.Combat.IsSovereignThroneActive
                        ? AbyssRingTuning.ThronePortalRangeMultiplier
                        : 1f),
                preferHighestHealth: false);
        }
        if (_sovereignActionTarget != null &&
            !IsSovereignTargetValid(_sovereignActionTarget))
        {
            _sovereignActionTarget = null;
            Player.Combat.CancelSovereignTargetedSequence();
            _sovereignActionTechniqueUseId = -1;
        }
    }

    private bool IsSovereignTargetValid(object target)
    {
        if (target is Enemy enemy)
        {
            foreach (Enemy active in Enemies.Enemies)
                if (ReferenceEquals(active, enemy))
                    return enemy.CanBeTargeted && enemy switch
                    {
                        CatacombEnemy catacomb =>
                            !catacomb.IsPhaseTransitioning,
                        FallenCityEnemy city =>
                            !city.IsKingPhaseTransitioning,
                        DemonLandsEnemy demon =>
                            demon.FrontierCombatState !=
                                DemonFrontierCombatState.GeneralPhaseTransition,
                        _ => true
                    };
            return false;
        }
        return target is AncientTreant boss && ReferenceEquals(boss, Boss) &&
            ShouldRenderBoss && boss.IsAlive && !boss.IsPhaseTransitioning;
    }

    private void UpdateSovereignTechniqueMovement()
    {
        if (Player?.WeaponFamily != WeaponFamily.AbyssRing ||
            Player.Combat.CurrentAttack == null ||
            _sovereignMotionAttackId == Player.Combat.AttackId)
            return;

        _sovereignMotionAttackId = Player.Combat.AttackId;
        WeaponTechniqueEffect effect =
            Player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        int stage = Player.Combat.TechniqueStageIndex;
        object targetObject = effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm
            ? _sovereignUltimateLockedTarget
            : _sovereignActionTarget;
        Rectangle target = GetCombatTargetBounds(targetObject);

        if (effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre &&
            stage == 0 && !target.IsEmpty)
        {
            SovereignPortals.OpenTheatreArray(target.Center.ToVector2());
        }

        bool shouldTransit = effect is
            WeaponTechniqueEffect.SovereignOmnipresentStep or
            WeaponTechniqueEffect.SovereignHeavenReversal or
            WeaponTechniqueEffect.SovereignZeroDistanceStrike ||
            effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage == 4 ||
            effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre && stage < 4 ||
            effect == WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                stage is >= 1 and <= 18;
        bool strikePortal = effect is
            WeaponTechniqueEffect.SovereignMartialArts &&
                stage is 2 or 4 or 6 or 7 ||
            effect == WeaponTechniqueEffect.SovereignParadoxImpact &&
                stage is >= 1 and <= 3 ||
            effect == WeaponTechniqueEffect.SovereignGoldenCataclysm;

        if (strikePortal && !target.IsEmpty)
        {
            Vector2 strikeOrigin = Player.Bounds.Center.ToVector2();
            Vector2 strikeExit = target.Center.ToVector2();
            SovereignPortals.OpenPair(
                SovereignPortalType.Strike,
                strikeOrigin,
                strikeExit,
                Player.Combat.IsSovereignThroneActive ? 1.15f : .92f,
                portalStep: effect ==
                        WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                    stage >= 1,
                presentationSeconds: effect ==
                        WeaponTechniqueEffect.SovereignGoldenCataclysm
                    ? Player.Combat.CurrentAttack.TotalTime
                    : 0f);
            if (effect == WeaponTechniqueEffect.SovereignMartialArts &&
                stage == 6)
            {
                Vector2 strikeDirection = strikeExit - strikeOrigin;
                if (strikeDirection.LengthSquared() > .001f)
                    strikeDirection.Normalize();
                SovereignPortals.Open(
                    SovereignPortalType.Strike,
                    strikeExit + new Vector2(24f, -10f),
                    MathF.Atan2(strikeDirection.Y, strikeDirection.X),
                    Player.Combat.IsSovereignThroneActive ? 1.05f : .84f,
                    logical: false,
                    SovereignPortalFlow.Exit,
                    strikeDirection);
            }
        }
        if (!shouldTransit)
            return;

        Vector2 desired = Player.Position;
        if (effect == WeaponTechniqueEffect.SovereignOmnipresentStep)
        {
            Vector2 direction = Player.Combat.SovereignTransitDirection;
            if (direction.LengthSquared() < .01f)
                direction = new Vector2(Player.Facing ==
                    FacingDirection.Left ? 1f : -1f, 0f);
            direction.Normalize();
            desired += new Vector2(
                direction.X * AbyssRingTuning.TransitRange,
                direction.Y * AbyssRingTuning.ElevatedTransit);
        }
        else if (effect == WeaponTechniqueEffect.SovereignHeavenReversal)
        {
            float side = Player.Facing == FacingDirection.Left ? -1f : 1f;
            desired += new Vector2(side * 74f, -112f);
        }
        else if (effect == WeaponTechniqueEffect.SovereignGoldenCataclysm &&
            stage == 18)
        {
            desired = _sovereignSafeExitPosition;
        }
        else if (!target.IsEmpty)
        {
            int sequence = Math.Max(0, stage - 1);
            float angle = MathHelper.TwoPi * sequence /
                Math.Max(4, AbyssRingTuning.UltimateStrikeCount);
            float radius = effect ==
                    WeaponTechniqueEffect.SovereignGoldenCataclysm
                ? 72f
                : 56f;
            desired = target.Center.ToVector2() + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * .35f);
        }

        if (!TryResolveSovereignTransit(desired, target, out Vector2 safe))
            return;
        Vector2 start = Player.Position;
        SovereignPortals.OpenPair(
            SovereignPortalType.Transit,
            start,
            safe,
            .9f,
            portalStep: effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm,
            presentationSeconds: effect ==
                    WeaponTechniqueEffect.SovereignGoldenCataclysm
                ? Player.Combat.CurrentAttack.TotalTime
                : 0f);
        Player.MoveTo(safe);
        if (!target.IsEmpty)
            Player.FaceToward(target.Center.X);
    }

    private bool TryResolveSovereignTransit(
        Vector2 desired,
        Rectangle target,
        out Vector2 safe)
    {
        safe = Player.Position;
        Rectangle world = CurrentDungeon.WorldBounds;
        Rectangle destination = DungeonCollision.CreateBounds(
            desired, Player.Size);
        if (!world.Contains(destination) ||
            !SideScrollingCollision.IsPositionFree(
                desired, Player.Size, CurrentDungeon) ||
            (!target.IsEmpty && destination.Intersects(target)))
            return false;

        if (ShouldRenderBoss && Boss?.IsActivated == true &&
            CurrentDungeon.BossRoom != null &&
            CurrentDungeon.BossRoom.Bounds.Contains(Player.Bounds) &&
            !CurrentDungeon.BossRoom.Bounds.Contains(destination))
            return false;

        foreach (Enemy enemy in Enemies.Enemies)
            if (enemy.CanBeTargeted && destination.Intersects(enemy.MeleeTargetBounds))
                return false;
        if (ShouldRenderBoss && Boss?.IsAlive == true &&
            destination.Intersects(Boss.Bounds))
            return false;

        Vector2 start = Player.Position;
        float distance = Vector2.Distance(start, desired);
        int steps = Math.Max(2, (int)MathF.Ceiling(distance / 18f));
        for (int index = 1; index < steps; index++)
        {
            Vector2 sample = Vector2.Lerp(start, desired, index / (float)steps);
            if (!SideScrollingCollision.IsPositionFree(
                sample, new Vector2(6f, 6f), CurrentDungeon))
                return false;
        }
        safe = desired;
        return true;
    }

    private void ResolvePerfectRiftCounter()
    {
        if (!Player.Combat.TryConsumePerfectRiftCounter(out Vector2 source))
            return;
        Enemy best = null;
        float bestDistance = 132f * 132f;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted)
                continue;
            float distance = Vector2.DistanceSquared(enemy.Position, source);
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }
        int damage = Math.Max(1, (int)MathF.Round(
            Player.MeleeDamage * AbyssRingTuning.PerfectRiftDamageMultiplier));
        if (best != null)
        {
            SovereignPortals.OpenPair(
                SovereignPortalType.Strike,
                source,
                best.MeleeTargetBounds.Center.ToVector2(),
                .8f);
            best.ReceiveDamageFrom(damage, 26f, Player.Position);
        }
        else if (ShouldRenderBoss && Boss?.IsAlive == true &&
            Vector2.DistanceSquared(Boss.Position, source) < 132f * 132f &&
            !Boss.IsPhaseTransitioning)
        {
            SovereignPortals.OpenPair(
                SovereignPortalType.Strike,
                source,
                Boss.Bounds.Center.ToVector2(),
                .8f);
            Boss.ReceiveDamage(damage, 26f);
        }
        else
        {
            SovereignPortals.Open(
                SovereignPortalType.Strike,
                source,
                scale: .8f,
                flow: SovereignPortalFlow.Entry);
        }
    }

    private void ClearSovereignState()
    {
        _sovereignUltimateCandidate = null;
        _sovereignUltimateLockedTarget = null;
        _sovereignActionTarget = null;
        _sovereignUltimateTechniqueUseId = -1;
        _sovereignActionTechniqueUseId = -1;
        _sovereignMotionAttackId = -1;
        _sovereignSafeExitRequested = false;
        _sovereignExecutionTechniqueUseId = -1;
        _sovereignExecutionFrameRemaining = 0f;
        _sovereignExecutionSuppressed = false;
        _sovereignExecutionImpactApplied = false;
        _sovereignExecutionTarget = null;
        SovereignPortals.Clear();
        Player?.Combat.SetSovereignUltimateTargetAvailable(false);
        Camera?.ResetCinematicZoom();
    }

    private static Rectangle GetCombatTargetBounds(object target)
    {
        return target switch
        {
            Enemy enemy => enemy.MeleeTargetBounds,
            AncientTreant boss => boss.Bounds,
            _ => Rectangle.Empty
        };
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
            if (enemy.CanBeTargeted && enemy.IsCombatAttackActive &&
                sweptBounds.Intersects(enemy.ActiveAttackArea))
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
            if (!enemy.CanBeTargeted || !enemy.IsCombatAttackActive)
                continue;

            Consider(enemy.ActiveAttackArea);
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
        RegisterPlayerHitEffect(
            PlayerAttackArea,
            targetBounds,
            Player.Combat.CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None,
            Player.Combat.TechniqueStageIndex);
    }

    private void RegisterPlayerHitEffect(
        Rectangle targetBounds,
        WeaponTechniqueEffect effect,
        int stage)
    {
        RegisterPlayerHitEffect(PlayerAttackArea, targetBounds, effect, stage);
    }

    private void RegisterPlayerHitEffect(
        Rectangle hitArea,
        Rectangle targetBounds)
    {
        RegisterPlayerHitEffect(
            hitArea,
            targetBounds,
            Player.Combat.CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None,
            Player.Combat.TechniqueStageIndex);
    }

    private void RegisterPlayerHitEffect(
        Rectangle hitArea,
        Rectangle targetBounds,
        WeaponTechniqueEffect effect,
        int stage)
    {
        Rectangle overlap = Rectangle.Intersect(
            hitArea,
            targetBounds);
        PlayerHitEffectPosition = overlap.Width > 0 && overlap.Height > 0
            ? overlap.Center.ToVector2()
            : targetBounds.Center.ToVector2();
        PlayerHitTechniqueEffect = effect;
        PlayerHitTechniqueStage = stage;
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

        if (attack.IsHeavy)
            CollapsingCity.TryBreakWithHeavyAttack(
                PlayerAttackArea,
                CurrentDungeon);

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
        ProcessGreatswordWorldEffect(technique);
        if (ProcessAncientStaffWorldEffect(technique))
            return;

        if (Player.WeaponFamily == WeaponFamily.GreatSword &&
            technique?.Effect ==
                WeaponTechniqueEffect.GreatswordFinalVerdict &&
            damage <= 0 && poiseDamage <= 0f)
        {
            return;
        }
        if (Player.WeaponFamily == WeaponFamily.Scythe &&
            damage <= 0 && poiseDamage <= 0f)
            return;

        if (technique?.Effect == WeaponTechniqueEffect.ScythePhantomCross &&
            _scytheCrossTechniqueUseId != Player.Combat.CurrentTechniqueUseId)
        {
            _scytheCrossTechniqueUseId = Player.Combat.CurrentTechniqueUseId;
            _scytheCrossEnemyHits.Clear();
            _scytheCrossBossHits = 0;
        }
        if (technique?.Effect == WeaponTechniqueEffect.ScytheAbyssalRequiem &&
            Player.Combat.TechniqueStageIndex == 0)
            _scytheShadowSeverTarget = null;

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
                technique?.Effect ==
                    WeaponTechniqueEffect.GreatswordFinalVerdict &&
                    !ReferenceEquals(
                        _greatswordUltimateLockedTarget,
                        enemy) ||
                technique?.Effect ==
                    WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                    !ReferenceEquals(
                        _sovereignUltimateLockedTarget,
                        enemy) ||
                technique?.Effect ==
                    WeaponTechniqueEffect.ScytheUmbraDevourer &&
                    !ReferenceEquals(_scytheUltimateLockedTarget, enemy) ||
                technique?.Effect ==
                    WeaponTechniqueEffect.ScytheAbyssalRequiem &&
                    Player.Combat.TechniqueStageIndex == 5 &&
                    (!Player.Combat.ScytheShadowSeverEnabled ||
                     !ReferenceEquals(_scytheShadowSeverTarget, enemy)) ||
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
            damageMultiplier *= Player.Combat.GetTargetDamageMultiplier(
                enemy.MaxHealth <= 0
                    ? 1f
                    : enemy.CurrentHealth / (float)enemy.MaxHealth);
            damageMultiplier *= GetScytheDamageMultiplier(
                technique, enemy.ReapMarkCount);

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
            enemy.ReceiveDamageFrom(
                targetDamage,
                targetPoiseDamage,
                Player.Position);
            TryResolveSovereignExecution(enemy, technique);
            ApplyTechniqueEffect(enemy, technique?.Effect ?? WeaponTechniqueEffect.None);
            ResolveSpellbladeEnemyHit(
                enemy,
                technique,
                imprintStacks,
                targetDamage);
            ResolveBreakerEnemyHit(enemy, technique, wasUnstable);
            bool scytheHandled = ResolveScytheEnemyHit(enemy, technique);

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
            else if (scytheHandled)
            {
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
            else if (technique?.Effect ==
                WeaponTechniqueEffect.SovereignEventHorizonPalm)
            {
                if (enemy.WeightClass != EnemyWeightClass.Heavy &&
                    !enemy.IsHeavyPullAnchor)
                {
                    enemy.PullToward(
                        Player.Position,
                        MathF.Max(18f, knockback),
                        CurrentDungeon);
                }
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
            (technique?.Effect !=
                WeaponTechniqueEffect.GreatswordFinalVerdict ||
             ReferenceEquals(_greatswordUltimateLockedTarget, Boss)) &&
            (technique?.Effect !=
                WeaponTechniqueEffect.SovereignGoldenCataclysm ||
             ReferenceEquals(_sovereignUltimateLockedTarget, Boss)) &&
            (technique?.Effect !=
                WeaponTechniqueEffect.ScytheUmbraDevourer ||
             ReferenceEquals(_scytheUltimateLockedTarget, Boss)) &&
            (technique?.Effect !=
                WeaponTechniqueEffect.ScytheAbyssalRequiem ||
             Player.Combat.TechniqueStageIndex != 5 ||
             Player.Combat.ScytheShadowSeverEnabled &&
                ReferenceEquals(_scytheShadowSeverTarget, Boss)) &&
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
            bossDamageMultiplier *= Player.Combat.GetTargetDamageMultiplier(
                Boss.MaxHealth <= 0
                    ? 1f
                    : Boss.CurrentHealth / (float)Boss.MaxHealth);
            bossDamageMultiplier *= GetScytheDamageMultiplier(
                technique, Boss.ReapMarkCount);
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
            TryResolveSovereignBossExecution(technique);
            ResolveSpellbladeBossHit(
                technique,
                bossImprintStacks,
                bossDamage);
            ResolveBreakerBossHit(technique);
            ResolveReckonerBossHit(technique);
            ResolveScytheBossHit(technique);
            bool suppressBossDisplacement = technique?.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz &&
                Player.Combat.TechniqueStageIndex < technique.StageCount - 1;
            suppressBossDisplacement |= technique?.Effect is
                WeaponTechniqueEffect.ReckonerChainsOfJudgment or
                WeaponTechniqueEffect.ReckonerVortexSnare or
                WeaponTechniqueEffect.SovereignEventHorizonPalm or
                WeaponTechniqueEffect.ScytheUmbraDevourer or
                WeaponTechniqueEffect.ScytheAbyssalRequiem or
                WeaponTechniqueEffect.ScythePhantomCross or
                WeaponTechniqueEffect.ScytheEclipseWaltz;
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

    private void TryResolveSovereignExecution(
        Enemy enemy,
        WeaponTechnique technique)
    {
        if (enemy == null || technique?.Effect !=
                WeaponTechniqueEffect.SovereignGoldenCataclysm ||
            Player.Combat.TechniqueStageIndex != 17 ||
            _sovereignExecutionTechniqueUseId ==
                Player.Combat.CurrentTechniqueUseId ||
            !enemy.IsAlive || enemy.CurrentHealth * 10 >= enemy.MaxHealth)
            return;

        bool protectedPhase = enemy switch
        {
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.FallenKnight =>
                    catacomb.CombatPhase < 3 || catacomb.IsPhaseTransitioning,
            FallenCityEnemy city when
                city.Type == EnemyType.BrokenKing => city.CombatPhase < 3,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.HornedGeneral => demon.CombatPhase < 3,
            _ => false
        };
        _sovereignExecutionTechniqueUseId =
            Player.Combat.CurrentTechniqueUseId;
        BeginSovereignExecutionFrame(enemy, protectedPhase);
        SovereignPortals.Open(
            SovereignPortalType.Strike,
            enemy.MeleeTargetBounds.Center.ToVector2(),
            0f,
            1.25f);
    }

    private void TryResolveSovereignBossExecution(WeaponTechnique technique)
    {
        if (technique?.Effect !=
                WeaponTechniqueEffect.SovereignGoldenCataclysm ||
            Player.Combat.TechniqueStageIndex != 17 ||
            _sovereignExecutionTechniqueUseId ==
                Player.Combat.CurrentTechniqueUseId ||
            Boss?.IsAlive != true || Boss.CurrentHealth * 10 >= Boss.MaxHealth)
            return;

        _sovereignExecutionTechniqueUseId =
            Player.Combat.CurrentTechniqueUseId;
        bool protectedPhase = Boss.Phase != AncientTreantPhase.PhaseThree ||
            Boss.IsPhaseTransitioning;
        BeginSovereignExecutionFrame(Boss, protectedPhase);
        SovereignPortals.Open(
            SovereignPortalType.Strike,
            Boss.Bounds.Center.ToVector2(),
            0f,
            1.4f);
    }

    private void BeginSovereignExecutionFrame(object target, bool suppressed)
    {
        SovereignPortals.Clear();
        _sovereignExecutionFrameRemaining =
            AbyssRingTuning.ExecutionDurationSeconds;
        _sovereignExecutionSuppressed = suppressed;
        _sovereignExecutionImpactApplied = false;
        _sovereignExecutionTarget = target;
    }

    private void UpdateSovereignExecution(float elapsedSeconds)
    {
        if (_sovereignExecutionFrameRemaining <= 0f)
            return;

        if (!_sovereignExecutionImpactApplied &&
            !IsSovereignExecutionTargetPresent())
        {
            _sovereignSafeExitRequested = true;
            Player?.Combat.RequestSovereignSafeExit();
            EndSovereignExecution();
            return;
        }

        _sovereignExecutionFrameRemaining = MathF.Max(
            0f,
            _sovereignExecutionFrameRemaining - elapsedSeconds);
        float executionTime = AbyssRingTuning.ExecutionDurationSeconds -
            _sovereignExecutionFrameRemaining;
        if (!_sovereignExecutionImpactApplied &&
            executionTime >= AbyssRingTuning.ExecutionImpactSeconds)
        {
            _sovereignExecutionImpactApplied = true;
            _sovereignExecutionImpactId++;
            if (!_sovereignExecutionSuppressed)
            {
                if (_sovereignExecutionTarget is Enemy enemy &&
                    enemy.IsAlive && IsActiveEnemy(enemy))
                {
                    enemy.ReceiveDamageFrom(
                        enemy.CurrentHealth,
                        0f,
                        Player.Position);
                }
                else if (_sovereignExecutionTarget is AncientTreant boss &&
                    ReferenceEquals(boss, Boss) && boss.IsAlive)
                {
                    boss.ReceiveDamage(boss.CurrentHealth, 0f);
                }
            }
            Camera.AddImpactFeedback(
                _sovereignExecutionSuppressed ? 2.4f :
                _sovereignExecutionTarget is AncientTreant ? 4.8f : 4.4f);
        }

        if (_sovereignExecutionFrameRemaining > 0f)
            return;

        EndSovereignExecution();
    }

    private bool IsSovereignExecutionTargetPresent()
    {
        if (_sovereignExecutionTarget is Enemy enemy)
            return enemy.IsAlive && IsActiveEnemy(enemy);
        return _sovereignExecutionTarget is AncientTreant boss &&
            ReferenceEquals(boss, Boss) && boss.IsAlive && ShouldRenderBoss;
    }

    private void EndSovereignExecution()
    {
        _sovereignExecutionFrameRemaining = 0f;
        _sovereignExecutionTarget = null;
        _sovereignExecutionSuppressed = false;
        _sovereignExecutionImpactApplied = false;
        SovereignPortals.Clear();
        Camera.ResetCinematicZoom();
    }

    private bool IsActiveEnemy(Enemy enemy)
    {
        foreach (Enemy active in Enemies.Enemies)
            if (ReferenceEquals(active, enemy))
                return true;
        return false;
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

    private float GetScytheDamageMultiplier(
        WeaponTechnique technique,
        int reapMarks)
    {
        if (Player.WeaponFamily != WeaponFamily.Scythe || technique == null)
            return 1f;
        if (technique.Effect == WeaponTechniqueEffect.ScytheReapersDance &&
            Player.Combat.TechniqueStageIndex == 5)
        {
            return 1f + Math.Min(
                ScytheTuning.J6MaximumMarkBonus,
                Math.Clamp(reapMarks, 0, ScytheTuning.MaximumReapMarks) * .05f);
        }
        return 1f;
    }

    private bool ResolveScytheEnemyHit(Enemy enemy, WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.Scythe ||
            enemy == null || technique == null)
            return false;

        int stage = Player.Combat.TechniqueStageIndex;
        bool enhanced = Player.Combat.IsScytheEnhancedAttack;
        if (enhanced && _scytheEnhancedMarkTechniqueUseId !=
                Player.Combat.CurrentTechniqueUseId)
        {
            enemy.AddReapMark();
            _scytheEnhancedMarkTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
        }
        if (technique.Effect == WeaponTechniqueEffect.ScytheUmbraDevourer)
            AddScytheUltimateImpactFeedback(stage);
        if (!enemy.IsAlive)
            return true;

        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.ScytheReapersDance when stage is 2 or 4:
                enemy.AddReapMark();
                if (stage == 4 && !enemy.IsElite && !enemy.IsHeavyPullAnchor &&
                    enemy.WeightClass is EnemyWeightClass.Light or
                        EnemyWeightClass.Normal)
                {
                    enemy.PullTowardSafe(
                        Player.Position, 22f, 28f, CurrentDungeon);
                    return true;
                }
                return false;
            case WeaponTechniqueEffect.ScytheReapersDance when stage == 5:
                enemy.ConsumeReapMarks();
                return false;
            case WeaponTechniqueEffect.ScytheWraithCut:
                enemy.AddReapMark();
                return false;
            case WeaponTechniqueEffect.ScytheShadowPass when stage == 1:
                enemy.AddReapMark();
                return false;
            case WeaponTechniqueEffect.ScythePhantomCross:
                int mask = _scytheCrossEnemyHits.TryGetValue(enemy, out int value)
                    ? value
                    : 0;
                mask |= stage == 0 ? 1 : 2;
                _scytheCrossEnemyHits[enemy] = mask;
                if (mask == 3)
                    enemy.AddReapMark();
                return true;
            case WeaponTechniqueEffect.ScytheAbyssalRequiem when stage == 0:
                if (!enemy.IsElite && !enemy.IsHeavyPullAnchor &&
                    enemy.WeightClass is EnemyWeightClass.Light or
                        EnemyWeightClass.Normal)
                {
                    enemy.PullTowardSafe(
                        Player.Position, 34f, 26f, CurrentDungeon);
                }
                else
                {
                    float side = Player.Position.X <= enemy.Position.X ? -1f : 1f;
                    Vector2 desired = enemy.Position + new Vector2(
                        side * (enemy.Size.X * .5f + Player.Size.X * .5f + 18f),
                        Player.Position.Y - enemy.Position.Y);
                    Player.MoveThroughCombatTechnique(desired, CurrentDungeon);
                    Player.FaceToward(enemy.Position.X);
                }
                return true;
            case WeaponTechniqueEffect.ScytheAbyssalRequiem when stage == 4:
                if (enemy.ReapMarkCount >= ScytheTuning.MaximumReapMarks)
                {
                    enemy.ConsumeReapMarks();
                    _scytheShadowSeverTarget = enemy;
                    Player.Combat.EnableScytheShadowSever();
                }
                return true;
            case WeaponTechniqueEffect.ScytheAbyssalRequiem:
            case WeaponTechniqueEffect.ScytheEclipseWaltz:
            case WeaponTechniqueEffect.ScytheUmbraDevourer:
                return true;
        }

        return false;
    }

    private void ResolveScytheBossHit(WeaponTechnique technique)
    {
        if (Player.WeaponFamily != WeaponFamily.Scythe || technique == null ||
            Boss == null || !Boss.IsAlive)
            return;

        int stage = Player.Combat.TechniqueStageIndex;
        if (technique.Effect == WeaponTechniqueEffect.ScytheUmbraDevourer)
            AddScytheUltimateImpactFeedback(stage);
        if (Player.Combat.IsScytheEnhancedAttack &&
            _scytheEnhancedMarkTechniqueUseId !=
                Player.Combat.CurrentTechniqueUseId)
        {
            Boss.AddReapMark();
            _scytheEnhancedMarkTechniqueUseId =
                Player.Combat.CurrentTechniqueUseId;
        }

        if (technique.Effect == WeaponTechniqueEffect.ScytheReapersDance)
        {
            if (stage is 2 or 4)
                Boss.AddReapMark();
            else if (stage == 5)
                Boss.ConsumeReapMarks();
        }
        else if (technique.Effect == WeaponTechniqueEffect.ScytheWraithCut ||
            technique.Effect == WeaponTechniqueEffect.ScytheShadowPass && stage == 1)
        {
            Boss.AddReapMark();
        }
        else if (technique.Effect == WeaponTechniqueEffect.ScythePhantomCross)
        {
            _scytheCrossBossHits |= stage == 0 ? 1 : 2;
            if (_scytheCrossBossHits == 3)
                Boss.AddReapMark();
        }
        else if (technique.Effect == WeaponTechniqueEffect.ScytheAbyssalRequiem)
        {
            if (stage == 0)
            {
                float side = Player.Position.X <= Boss.Position.X ? -1f : 1f;
                Vector2 desired = Boss.Position + new Vector2(
                    side * (Boss.Size.X * .5f + Player.Size.X * .5f + 20f),
                    Player.Position.Y - Boss.Position.Y);
                Player.MoveThroughCombatTechnique(desired, CurrentDungeon);
                Player.FaceToward(Boss.Position.X);
            }
            else if (stage == 4 &&
                Boss.ReapMarkCount >= ScytheTuning.MaximumReapMarks)
            {
                Boss.ConsumeReapMarks();
                _scytheShadowSeverTarget = Boss;
                Player.Combat.EnableScytheShadowSever();
            }
        }
    }

    private void AddScytheUltimateImpactFeedback(int stage)
    {
        float strength = stage switch
        {
            0 => 1.25f,
            1 => 1.05f,
            2 => 1.25f,
            3 => 1.65f,
            4 or 5 => 2.35f,
            6 => 4.8f,
            _ => 0f
        };
        if (strength > 0f)
            Camera.AddImpactFeedback(strength);
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

    private void UpdateAncientStaffCandidates()
    {
        if (Player?.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            CurrentDungeon == null || Player.Combat.IsFourthVeilActive)
        {
            _ancientStaffCandidate = null;
            _ancientStaffUltimateCandidate = null;
            return;
        }

        if (Player.Combat.CurrentTechnique != null)
            return;

        _ancientStaffCandidate = FindAncientStaffTarget(
            AncientStaffTuning.RunePrisonRange,
            preferHighestHealth: false);
        _ancientStaffUltimateCandidate = FindAncientStaffTarget(
            AncientStaffTuning.UltimateRange,
            preferHighestHealth: true);
    }

    private object FindAncientStaffTarget(
        float maximumRange,
        bool preferHighestHealth)
    {
        float maximumDistance = maximumRange * maximumRange;
        object best = null;
        int bestHealth = -1;
        float bestDistance = float.MaxValue;
        int bestStableId = int.MaxValue;
        for (int index = 0; index < Enemies.Enemies.Count; index++)
        {
            Enemy enemy = Enemies.Enemies[index];
            if (!enemy.CanBeTargeted ||
                !Enemies.IsActive(enemy, Player.Position, CurrentDungeon))
                continue;
            float distance = Vector2.DistanceSquared(
                Player.Position,
                enemy.Position);
            if (distance > maximumDistance)
                continue;
            bool better = preferHighestHealth
                ? IsBetterRangerUltimateTarget(
                    enemy.MaxHealth,
                    distance,
                    index,
                    bestHealth,
                    bestDistance,
                    bestStableId)
                : distance < bestDistance;
            if (!better)
                continue;
            best = enemy;
            bestHealth = enemy.MaxHealth;
            bestDistance = distance;
            bestStableId = index;
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true && Boss.IsActivated &&
            !Boss.IsPhaseTransitioning)
        {
            float distance = Vector2.DistanceSquared(
                Player.Position,
                Boss.Position);
            int stableId = Enemies.Enemies.Count;
            bool better = distance <= maximumDistance &&
                (preferHighestHealth
                    ? IsBetterRangerUltimateTarget(
                        Boss.MaxHealth,
                        distance,
                        stableId,
                        bestHealth,
                        bestDistance,
                        bestStableId)
                    : distance < bestDistance);
            if (better)
                best = Boss;
        }
        return best;
    }

    private void UpdateAncientStaffTargetLock()
    {
        if (Player?.WeaponFamily != WeaponFamily.ArcaneWarStaff)
        {
            ClearAncientStaffState();
            return;
        }

        WeaponTechnique technique = Player.Combat.CurrentTechnique;
        if (technique == null)
        {
            bool hadStaffLock = _ancientStaffTechniqueUseId >= 0 ||
                _ancientStaffLockedTarget != null;
            _ancientStaffLockedTarget = null;
            _ancientStaffTechniqueUseId = -1;
            if (hadStaffLock)
                Camera.ResetCinematicZoom();
            return;
        }
        if (_ancientStaffTechniqueUseId ==
            Player.Combat.CurrentTechniqueUseId)
        {
            if (technique.Effect ==
                    WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
                (IsAncientStaffTargetTransitioning(_ancientStaffLockedTarget) ||
                    IsBossTransitioningInAncientStaffArea()))
            {
                Player.Combat.CancelActions();
                ClearAncientStaffState();
            }
            else if (technique.Effect ==
                    WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
                Player.Combat.State != CombatState.ChargingHeavy &&
                Player.Combat.TechniqueStageIndex < 9 &&
                !HasAncientStaffTargetsInArea())
            {
                Player.Combat.RequestAncientStaffUltimateReturn();
            }
            return;
        }

        _ancientStaffTechniqueUseId = Player.Combat.CurrentTechniqueUseId;
        _ancientStaffLockedTarget = technique.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate
            ? _ancientStaffUltimateCandidate
            : _ancientStaffCandidate;
        Rectangle bounds = GetCombatTargetBounds(_ancientStaffLockedTarget);
        _ancientStaffLockedCenter = bounds.IsEmpty
            ? Player.Position
            : bounds.Center.ToVector2();
    }

    private void UpdateAncientStaffTechniqueMovement()
    {
        if (Player?.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            Player.Combat.CurrentAttack == null ||
            _ancientStaffMotionAttackId == Player.Combat.AttackId)
            return;
        _ancientStaffMotionAttackId = Player.Combat.AttackId;
        if (Player.Combat.CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.AncientStaffBlinkCast)
            return;

        float direction = Player.Facing == FacingDirection.Left ? -1f : 1f;
        _ancientStaffBlinkOrigin = Player.Position;
        _ancientStaffBlinkTrailRemaining = .30f;
        Player.MoveThroughCombatTechnique(
            Player.Position + new Vector2(
                direction * AncientStaffTuning.BlinkDistance,
                0f),
            CurrentDungeon);
    }

    private bool ProcessAncientStaffWorldEffect(WeaponTechnique technique)
    {
        WeaponTechniqueEffect effect = technique?.Effect ??
            WeaponTechniqueEffect.None;
        bool staffEffect = effect is
            WeaponTechniqueEffect.AncientStaffAegisWard or
            WeaponTechniqueEffect.AncientStaffThunderSpirit or
            WeaponTechniqueEffect.AncientStaffRunePrison or
            WeaponTechniqueEffect.AncientStaffSpiritBastion or
            WeaponTechniqueEffect.AncientStaffElderInvocation or
            WeaponTechniqueEffect.AncientStaffAstralDescent or
            WeaponTechniqueEffect.AncientStaffFourthVeil or
            WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate;
        if (!staffEffect)
            return false;
        if (_ancientStaffWorldEffectAttackId == Player.Combat.AttackId)
            return true;

        _ancientStaffWorldEffectAttackId = Player.Combat.AttackId;
        int stage = Player.Combat.TechniqueStageIndex;
        int damage = Player.Combat.CalculateDamage(Player.MeleeDamage);
        float poise = Player.Combat.CalculatePoiseDamage();
        float radius = 0f;
        Vector2 center = Player.Position;

        if (effect == WeaponTechniqueEffect.AncientStaffThunderSpirit)
        {
            _thunderSpiritTimeRemaining =
                AncientStaffTuning.ThunderSpiritDurationSeconds;
            _thunderSpiritAttackCooldown = .25f;
            _thunderSpiritState = ThunderSpiritState.Active;
            _thunderSpiritSummonVisualRemaining = .62f;
            _thunderSpiritDismissVisualRemaining = 0f;
            return true;
        }
        if (effect is WeaponTechniqueEffect.AncientStaffAegisWard or
            WeaponTechniqueEffect.AncientStaffFourthVeil)
            return true;

        if (effect == WeaponTechniqueEffect.AncientStaffRunePrison)
        {
            center = _ancientStaffLockedCenter;
            radius = AncientStaffTuning.RunePrisonRadius +
                Player.Combat.ElderSigils * 8f;
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffSpiritBastion)
        {
            center = Player.Position;
            radius = AncientStaffTuning.SpiritBastionRadius;
            if (_thunderSpiritState == ThunderSpiritState.Dormant)
                _thunderSpiritSummonVisualRemaining = .34f;
            _thunderSpiritTimeRemaining = MathF.Max(
                _thunderSpiritTimeRemaining,
                AncientStaffTuning.ThunderSpiritDurationSeconds * .5f);
            _thunderSpiritState = ThunderSpiritState.Empowered;
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffElderInvocation)
        {
            center = _ancientStaffLockedCenter;
            radius = AncientStaffTuning.ElderInvocationRadius;
            if (stage == 3 && Player.Combat.ElderSigils ==
                    AncientStaffTuning.MaximumElderSigils)
                damage = Math.Max(1, (int)MathF.Round(damage * 1.10f));
            if (stage == 3 && _thunderSpiritState != ThunderSpiritState.Dormant)
            {
                _thunderSpiritState = ThunderSpiritState.Discharged;
                _thunderSpiritTimeRemaining = MathF.Min(
                    _thunderSpiritTimeRemaining,
                    .45f);
            }
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffAstralDescent)
        {
            center = Player.Position;
            radius = stage == 0 ? 76f : 108f;
        }
        else if (effect ==
            WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate)
        {
            center = _ancientStaffLockedCenter;
            radius = AncientStaffTuning.UltimateRadius;
        }

        _ancientStaffEffectTimeRemaining = stage == 8 ? .58f : .34f;
        _ancientStaffEffectDuration = _ancientStaffEffectTimeRemaining;
        bool hit = ApplyAncientStaffAreaDamage(
            center,
            radius,
            damage,
            poise,
            effect,
            stage);
        if (hit)
            Player.Combat.RegisterSuccessfulHit();
        if (effect ==
            WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate)
        {
            float impactFeedback = stage switch
            {
                1 => 2.0f,
                3 => 1.0f,
                5 => 1.3f,
                7 => 1.6f,
                8 => 5.0f,
                _ => 0f
            };
            if (impactFeedback > 0f)
                Camera.AddImpactFeedback(impactFeedback);
        }
        return true;
    }

    private bool ApplyAncientStaffAreaDamage(
        Vector2 center,
        float radius,
        int damage,
        float poise,
        WeaponTechniqueEffect effect,
        int stage)
    {
        if (damage <= 0 && poise <= 0f)
            return false;
        bool hit = false;
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (!CanDamageAncientStaffEnemy(enemy) ||
                Vector2.DistanceSquared(center, enemy.Position) >
                    radiusSquared)
                continue;
            enemy.ReceiveDamageFrom(damage, poise, Player.Position);
            ApplyAncientStaffControl(enemy, effect, stage);
            RegisterPlayerHitEffect(enemy.MeleeTargetBounds, effect, stage);
            hit = true;
        }

        if (ShouldRenderBoss && Boss?.IsAlive == true &&
            !Boss.IsPhaseTransitioning &&
            Vector2.DistanceSquared(center, Boss.Position) <= radiusSquared)
        {
            Boss.ReceiveDamage(damage, poise * .65f);
            if (effect == WeaponTechniqueEffect.AncientStaffRunePrison)
                Boss.ApplyArcaneSlow(.82f, .55f);
            else if (effect ==
                    WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
                stage is 1 or 2)
                Boss.ApplyArcaneSlow(.88f, .40f);
            RegisterPlayerHitEffect(Boss.Bounds, effect, stage);
            hit = true;
        }
        return hit;
    }

    private static bool CanDamageAncientStaffEnemy(Enemy enemy)
    {
        if (enemy?.CanBeTargeted != true)
            return false;
        return enemy switch
        {
            CatacombEnemy catacomb => !catacomb.IsPhaseTransitioning,
            FallenCityEnemy city => !city.IsKingPhaseTransitioning,
            DemonLandsEnemy demon => demon.FrontierCombatState !=
                DemonFrontierCombatState.GeneralPhaseTransition,
            _ => true
        };
    }

    private static bool IsAncientStaffTargetTransitioning(object target)
    {
        return target switch
        {
            AncientTreant boss => boss.IsPhaseTransitioning,
            CatacombEnemy catacomb => catacomb.IsPhaseTransitioning,
            FallenCityEnemy city => city.IsKingPhaseTransitioning,
            DemonLandsEnemy demon => demon.FrontierCombatState ==
                DemonFrontierCombatState.GeneralPhaseTransition,
            _ => false
        };
    }

    private bool IsBossTransitioningInAncientStaffArea()
    {
        return ShouldRenderBoss && Boss?.IsPhaseTransitioning == true &&
            Vector2.DistanceSquared(
                _ancientStaffLockedCenter,
                Boss.Position) <= AncientStaffTuning.UltimateRadius *
                    AncientStaffTuning.UltimateRadius;
    }

    private bool HasAncientStaffTargetsInArea()
    {
        float radiusSquared = AncientStaffTuning.UltimateRadius *
            AncientStaffTuning.UltimateRadius;
        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (CanDamageAncientStaffEnemy(enemy) &&
                Vector2.DistanceSquared(
                    _ancientStaffLockedCenter,
                    enemy.Position) <= radiusSquared)
                return true;
        }
        return ShouldRenderBoss && Boss?.IsAlive == true &&
            !Boss.IsPhaseTransitioning &&
            Vector2.DistanceSquared(
                _ancientStaffLockedCenter,
                Boss.Position) <= radiusSquared;
    }

    private static void ApplyAncientStaffControl(
        Enemy enemy,
        WeaponTechniqueEffect effect,
        int stage)
    {
        if (effect == WeaponTechniqueEffect.AncientStaffRunePrison &&
            stage == 0)
        {
            enemy.ApplyControl(
                enemy.IsElite ? .62f : .25f,
                enemy.IsElite ? .65f : 1.25f);
            if (!enemy.IsElite)
                enemy.ApplyArcaneStun(.38f);
        }
        else if (effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
            stage == 1)
        {
            if (!enemy.IsElite && enemy.WeightClass != EnemyWeightClass.Heavy)
                enemy.ApplyRaiderLaunch(-220f);
            else
                enemy.ApplyControl(.72f, .42f);
        }
        else if (effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
            stage == 2)
        {
            enemy.ApplyControl(enemy.IsElite ? .72f : .38f, .48f);
        }
    }

    private void UpdateAncientStaffRuntime(float elapsedSeconds)
    {
        _ancientStaffEffectTimeRemaining = MathF.Max(
            0f,
            _ancientStaffEffectTimeRemaining - elapsedSeconds);
        _ancientStaffBlinkTrailRemaining = MathF.Max(
            0f,
            _ancientStaffBlinkTrailRemaining - elapsedSeconds);
        _thunderSpiritSummonVisualRemaining = MathF.Max(
            0f,
            _thunderSpiritSummonVisualRemaining - elapsedSeconds);
        _thunderSpiritDismissVisualRemaining = MathF.Max(
            0f,
            _thunderSpiritDismissVisualRemaining - elapsedSeconds);
        if (Player?.WeaponFamily != WeaponFamily.ArcaneWarStaff)
            return;
        if (!Player.IsAlive)
        {
            ClearAncientStaffState();
            return;
        }
        if (_thunderSpiritTimeRemaining <= 0f)
        {
            _thunderSpiritState = ThunderSpiritState.Dormant;
            return;
        }

        _thunderSpiritTimeRemaining = MathF.Max(
            0f,
            _thunderSpiritTimeRemaining - elapsedSeconds);
        _thunderSpiritAttackCooldown -= elapsedSeconds;
        if (_thunderSpiritTimeRemaining <= 0f)
        {
            _thunderSpiritState = ThunderSpiritState.Dormant;
            _thunderSpiritDismissVisualRemaining = .38f;
            return;
        }
        if (_thunderSpiritState == ThunderSpiritState.Discharged ||
            _thunderSpiritAttackCooldown > 0f ||
            Player.Combat.IsFourthVeilActive)
            return;

        object target = FindAncientStaffTarget(
            AncientStaffTuning.ThunderSpiritRange,
            preferHighestHealth: false);
        Rectangle bounds = GetCombatTargetBounds(target);
        if (bounds.IsEmpty)
        {
            _thunderSpiritAttackCooldown = .25f;
            return;
        }

        _thunderSpiritLastTarget = bounds.Center.ToVector2();
        int damage = Math.Max(1, (int)MathF.Round(
            Player.MeleeDamage * (_thunderSpiritState ==
                ThunderSpiritState.Empowered ? .34f : .24f)));
        if (target is Enemy enemy && CanDamageAncientStaffEnemy(enemy))
            enemy.ReceiveDamageFrom(damage, 10f, ThunderSpiritPosition);
        else if (target is AncientTreant boss && boss.IsAlive &&
            !boss.IsPhaseTransitioning)
            boss.ReceiveDamage(damage, 6f);
        else
            return;
        Player.Combat.Resources.AddElderSigil();
        _ancientStaffEffectTimeRemaining = .18f;
        _ancientStaffEffectDuration = .18f;
        _thunderSpiritAttackCooldown =
            AncientStaffTuning.ThunderSpiritAttackIntervalSeconds;
    }

    private void ClearAncientStaffState()
    {
        bool hadCameraState = _ancientStaffLockedTarget != null ||
            _ancientStaffTechniqueUseId >= 0 ||
            _ancientStaffEffectTimeRemaining > 0f;
        _ancientStaffCandidate = null;
        _ancientStaffUltimateCandidate = null;
        _ancientStaffLockedTarget = null;
        _ancientStaffLockedCenter = Vector2.Zero;
        _ancientStaffTechniqueUseId = -1;
        _ancientStaffWorldEffectAttackId = -1;
        _ancientStaffMotionAttackId = -1;
        _thunderSpiritTimeRemaining = 0f;
        _thunderSpiritAttackCooldown = 0f;
        _thunderSpiritState = ThunderSpiritState.Dormant;
        _thunderSpiritLastTarget = Vector2.Zero;
        _thunderSpiritSummonVisualRemaining = 0f;
        _thunderSpiritDismissVisualRemaining = 0f;
        _ancientStaffEffectTimeRemaining = 0f;
        _ancientStaffEffectDuration = 0f;
        _ancientStaffBlinkOrigin = Vector2.Zero;
        _ancientStaffBlinkTrailRemaining = 0f;
        Player?.Combat.SetAncientStaffUltimateTargetAvailable(false);
        if (hadCameraState)
            Camera?.ResetCinematicZoom();
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
        if (effect is WeaponTechniqueEffect.RangerHeavensFury or
            WeaponTechniqueEffect.RangerPhantomHunt)
            facing = Player.Combat.RangerLockedDirection;
        Vector2 direction = new(facing, 0f);
        object lockedTarget = effect == WeaponTechniqueEffect.RangerPhantomHunt
            ? _rangerUltimateLockedTarget
            : null;

        direction = effect switch
        {
            WeaponTechniqueEffect.RangerSkyfallMarker =>
                GetSkyfallDirection(Player.Combat.RangerSkyfallDistance, facing),
            WeaponTechniqueEffect.RangerFallingStar =>
                Vector2.Normalize(new Vector2(facing * .48f, -.88f)),
            WeaponTechniqueEffect.RangerArrowStorm =>
                Vector2.Normalize(new Vector2(facing * .36f, -.93f)),
            WeaponTechniqueEffect.RangerSkyfallVolley =>
                Vector2.Normalize(new Vector2(facing * .34f, .94f)),
            WeaponTechniqueEffect.RangerSkyfallShot =>
                Vector2.Normalize(new Vector2(facing, 1f)),
            _ => direction
        };

        Vector2 spawnPosition = Player.Position + direction * 30f +
            new Vector2(0f, -10f);
        if (lockedTarget != null)
        {
            Vector2 aim = GetRangerTargetBounds(lockedTarget).Center.ToVector2();
            Vector2 aimDelta = aim - spawnPosition;
            if (aimDelta.LengthSquared() > 1f)
                direction = Vector2.Normalize(aimDelta);
        }
        int roomId = CurrentDungeon.FindRoomContaining(Player.Position)?.Id ?? -1;

        if (effect is WeaponTechniqueEffect.RangerThreefoldHunt or
            WeaponTechniqueEffect.RangerPredatorsHorizon)
        {
            SpawnRangerSpread(attack, effect, spawnPosition, facing, roomId);
            return;
        }

        float focusRatio = effect is WeaponTechniqueEffect.RangerHeavensFury or
                WeaponTechniqueEffect.RangerPhantomHunt
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

        if (effect == WeaponTechniqueEffect.AncientStaffArcaneBoltChain &&
            Player.Combat.TechniqueStageIndex == 2)
        {
            for (int glyph = -1; glyph <= 1; glyph += 2)
            {
                Vector2 glyphDirection = Vector2.Normalize(new Vector2(
                    direction.X,
                    glyph * .075f));
                Projectiles.SpawnPlayerProjectile(
                    attack.Projectile,
                    spawnPosition + new Vector2(0f, glyph * 7f),
                    glyphDirection,
                    Player.Combat.ProjectileSpeedMultiplier,
                    damage,
                    poiseDamage,
                    Player.Combat.CalculateKnockback(),
                    roomId,
                    effect,
                    Player.Combat.CurrentTechnique?.ResourceGainOnHit ?? 0,
                    sourceAttackId: Player.Combat.AttackId,
                    techniqueUseId: Player.Combat.CurrentTechniqueUseId,
                    spreadArrowIndex: Player.Combat.TechniqueStageIndex);
            }
            return;
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
            additionalPierces: effect is WeaponTechniqueEffect.RangerHuntersDraw or
                    WeaponTechniqueEffect.RangerPiercingDraw &&
                Player.Combat.RangerDrawState == RangerDrawState.Perfect ? 1 : 0,
            placementDistance: effect == WeaponTechniqueEffect.SpellbladeRuneBrand
                ? SpellbladeTuning.GetPlacementDistance(
                    Player.Combat.RunePlacementDistance)
                : 0f,
            spreadArrowIndex: effect is
                    WeaponTechniqueEffect.AncientStaffArcaneBoltChain or
                    WeaponTechniqueEffect.AncientStaffBlinkCast
                ? Player.Combat.TechniqueStageIndex
                : -1,
            lockedTarget: lockedTarget);
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

            if (CurrentDungeon.IsDemonLands &&
                CorruptionAnchors.TryDamage(
                    projectile.SweptBounds,
                    projectile.SourceAttackId >= 0
                        ? projectile.SourceAttackId
                        : Player.Combat.AttackId,
                    Math.Max(35, projectile.Damage),
                    Corruption))
            {
                projectile.LifetimeRemaining = 0f;
                continue;
            }

            bool consumed = false;
            foreach (Enemy enemy in Enemies.Enemies)
            {
                if (!enemy.CanBeTargeted ||
                    projectile.IsAncientStaffProjectile &&
                        !CanDamageAncientStaffEnemy(enemy) ||
                    projectile.LockedTarget != null &&
                        !ReferenceEquals(projectile.LockedTarget, enemy) ||
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
                        enemy.MeleeTargetBounds,
                        projectile.TechniqueEffect,
                        projectile.SpreadArrowIndex);
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
                    bool weakPoint = IsRangerWeakPointHit(projectile, enemy);
                    if (weakPoint)
                    {
                        rangerDamage = Math.Max(1, (int)MathF.Round(
                            rangerDamage * 1.12f));
                        poiseDamage *= 1.45f;
                    }
                    enemy.ReceiveDamageFrom(
                        rangerDamage,
                        poiseDamage,
                        projectile.SourcePosition);
                    enemy.ApplyKnockback(
                        projectile.SourcePosition,
                        projectile.Knockback,
                        CurrentDungeon);
                    RecordPredatorFanHit(projectile, enemy);
                    RegisterRangerHit(projectile, enemy.Position, weakPoint);
                    consumed = !AdvancePenetratingProjectile(projectile);
                    if (consumed)
                        break;
                    continue;
                }

                if (projectile.IsAncientStaffProjectile)
                {
                    projectile.HitEnemies.Add(enemy);
                    projectile.HasHitTarget = true;
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        enemy.MeleeTargetBounds,
                        projectile.TechniqueEffect,
                        projectile.SpreadArrowIndex);
                    enemy.ReceiveDamageFrom(
                        projectile.Damage,
                        projectile.PoiseDamage,
                        projectile.SourcePosition);
                    Player.Combat.RegisterExternalHit(
                        projectile.WeaponResourceGain);
                    if (projectile.TechniqueEffect ==
                        WeaponTechniqueEffect.AncientStaffBlinkCast)
                    {
                        ApplyAncientStaffAreaDamage(
                            enemy.Position,
                            42f,
                            Math.Max(1, (int)MathF.Round(
                                Player.MeleeDamage * .35f)),
                            14f,
                            projectile.TechniqueEffect,
                            1);
                    }
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
                    enemy.ReceiveDamageFrom(
                        Math.Max(1, (int)MathF.Round(
                            projectile.Damage * spellbladeMultiplier)),
                        projectile.PoiseDamage,
                        projectile.SourcePosition);
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
                enemy.ReceiveDamageFrom(
                    System.Math.Max(0, (int)System.MathF.Round(
                        projectile.Damage * damageMultiplier)),
                    projectile.PoiseDamage,
                    projectile.SourcePosition);
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
                (!projectile.IsAncientStaffProjectile ||
                    !Boss.IsPhaseTransitioning) &&
                (projectile.LockedTarget == null ||
                    ReferenceEquals(projectile.LockedTarget, Boss)) &&
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
                        Boss.Bounds,
                        projectile.TechniqueEffect,
                        projectile.SpreadArrowIndex);
                    int rangerDamage = CalculateRangerProjectileDamage(
                        projectile,
                        1f,
                        Boss.Bounds,
                        isLarge: true,
                        isBoss: true);
                    bool weakPoint = Boss.IsStaggered &&
                        projectile.SweptBounds.Intersects(Boss.WeakPointBounds);
                    if (weakPoint)
                        rangerDamage = Math.Max(1, (int)MathF.Round(rangerDamage * 1.10f));
                    float bossPoise = CalculateRangerProjectilePoise(
                        projectile,
                        enemy: null,
                        isBoss: true) * (weakPoint ? 1.35f : 1f);
                    Boss.ReceiveDamage(rangerDamage, bossPoise);
                    Boss.ApplyKnockback(
                        projectile.SourcePosition,
                        projectile.Knockback,
                        CurrentDungeon);
                    RecordPredatorBossFanHit(projectile);
                    RegisterRangerHit(projectile, Boss.Position, weakPoint);
                    consumed = !AdvancePenetratingProjectile(projectile);
                }
                else if (projectile.IsAncientStaffProjectile)
                {
                    projectile.HasHitBoss = true;
                    projectile.HasHitTarget = true;
                    RegisterPlayerHitEffect(
                        projectile.SweptBounds,
                        Boss.Bounds,
                        projectile.TechniqueEffect,
                        projectile.SpreadArrowIndex);
                    Boss.ReceiveDamage(
                        projectile.Damage,
                        projectile.PoiseDamage);
                    Player.Combat.RegisterExternalHit(
                        projectile.WeaponResourceGain);
                    if (projectile.TechniqueEffect ==
                        WeaponTechniqueEffect.AncientStaffBlinkCast)
                    {
                        ApplyAncientStaffAreaDamage(
                            Boss.Position,
                            42f,
                            Math.Max(1, (int)MathF.Round(
                                Player.MeleeDamage * .35f)),
                            9f,
                            projectile.TechniqueEffect,
                            1);
                    }
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

            if (impact.Projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.AncientStaffBlinkCast &&
                !impact.Projectile.HasHitTarget)
            {
                ApplyAncientStaffAreaDamage(
                    impact.Position,
                    42f,
                    Math.Max(1, (int)MathF.Round(
                        Player.MeleeDamage * .35f)),
                    14f,
                    impact.Projectile.TechniqueEffect,
                    1);
                continue;
            }

            if (!impact.Projectile.IsRangerProjectile)
                continue;

            if (!impact.Projectile.HasHitTarget)
                Player.Combat.RegisterRangerProjectileMiss(
                    impact.Projectile.SourceAttackId);
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

    private void RegisterRangerHit(
        Projectile projectile,
        Vector2 targetPosition,
        bool weakPoint = false)
    {
        projectile.HasHitTarget = true;
        projectile.TargetsHit++;
        Player.Combat.RegisterRangerProjectileHit(
            projectile.SourceAttackId,
            projectile.TechniqueEffect,
            projectile.DrawState,
            Vector2.Distance(projectile.SourcePosition, targetPosition) >=
                RangerTuning.SafeSpacingDistance);
        if (weakPoint)
            Player.Combat.RegisterRangerWeakPointHit();
        bool execution = projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerHeavensFury ||
            projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerPhantomHunt &&
            projectile.Size.X >= 28f;
        Player.Combat.RegisterRangerImpactFeedback(
            strongHit: execution ||
                projectile.DrawState == RangerDrawState.Perfect ||
                projectile.TechniqueEffect is
                    WeaponTechniqueEffect.RangerDragonPiercer or
                    WeaponTechniqueEffect.RangerFallingStar,
            execution);
        if (execution || projectile.DrawState == RangerDrawState.Perfect)
        {
            Camera.AddImpactFeedback(execution ? 3.4f : 1.5f);
        }
        Player.Combat.RegisterExternalHit(projectile.WeaponResourceGain);
    }

    private static bool IsRangerWeakPointHit(
        Projectile projectile,
        Enemy enemy)
    {
        return (enemy.IsCombatAttackTelegraphing || enemy.IsStaggered) &&
            projectile.SweptBounds.Intersects(enemy.WeakPointBounds);
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
        bool movementCancelPressed =
            (keyboardState.IsKeyDown(Keys.A) &&
                !_previousKeyboardState.IsKeyDown(Keys.A)) ||
            (keyboardState.IsKeyDown(Keys.D) &&
                !_previousKeyboardState.IsKeyDown(Keys.D)) ||
            (keyboardState.IsKeyDown(Keys.Left) &&
                !_previousKeyboardState.IsKeyDown(Keys.Left)) ||
            (keyboardState.IsKeyDown(Keys.Right) &&
                !_previousKeyboardState.IsKeyDown(Keys.Right));
        float horizontalDirection = 0f;
        float verticalDirection = 0f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            horizontalDirection -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            horizontalDirection += 1f;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            verticalDirection -= 1f;
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            verticalDirection += 1f;

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
            skill2Released,
            movementCancelPressed,
            verticalDirection);
    }

    private bool HasImmediateDuelistThreat()
    {
        if (Player.WeaponFamily != WeaponFamily.DualSwords)
            return false;

        Rectangle opportunity = Player.Bounds;
        opportunity.Inflate(54, 18);

        foreach (Enemy enemy in Enemies.Enemies)
        {
            if (enemy.CanBeTargeted && enemy.IsCombatAttackActive &&
                opportunity.Intersects(enemy.ActiveAttackArea))
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

        if (ActiveInventoryTab == InventoryTab.Notes)
        {
            int previousNoteIndex = SelectedInventoryIndex;
            if (upPressed)
                SelectedInventoryIndex--;
            else if (downPressed)
                SelectedInventoryIndex++;

            ClampInventorySelection();
            if (SelectedInventoryIndex != previousNoteIndex || equipPressed)
                MarkSelectedNoteRead();
            return;
        }

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

            if (selectedItem != null &&
                !ReferenceEquals(
                    selectedItem,
                    Player.EquippedItems.GetEquipped(selectedItem.Slot)) &&
                Player.EquipItem(selectedItem))
            {
                RefreshInventoryView();
                SelectedInventoryIndex = _activeInventoryItems.IndexOf(
                    Player.EquippedItems.GetEquipped(selectedItem.Slot));
            }

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
        MarkSelectedNoteRead();
    }

    private void RefreshInventoryView()
    {
        InventoryTabRules.PopulateView(
            Player.Inventory.Items,
            ActiveInventoryTab,
            _activeInventoryItems);

        if (ActiveInventoryTab == InventoryTab.Weapons &&
            Player.EquippedItems.Weapon != null &&
            !_activeInventoryItems.Contains(Player.EquippedItems.Weapon))
        {
            _activeInventoryItems.Add(Player.EquippedItems.Weapon);
        }

        if (ActiveInventoryTab == InventoryTab.Weapons)
        {
            _activeInventoryItems.Sort((left, right) =>
            {
                int leftOrder = EquipmentCatalog.GetImplementedWeaponOrder(
                    left.WeaponId);
                int rightOrder = EquipmentCatalog.GetImplementedWeaponOrder(
                    right.WeaponId);
                int order = leftOrder.CompareTo(rightOrder);
                return order != 0
                    ? order
                    : string.CompareOrdinal(left.WeaponId, right.WeaponId);
            });
        }
    }

    private void RememberActiveTabSelection()
    {
        if (ActiveInventoryTab == InventoryTab.Weapons)
            _selectedWeaponIndex = SelectedInventoryIndex;
        else if (ActiveInventoryTab == InventoryTab.Equipment)
            _selectedEquipmentIndex = SelectedInventoryIndex;
        else
            _selectedNoteIndex = SelectedInventoryIndex;
    }

    private void RestoreActiveTabSelection()
    {
        SelectedInventoryIndex = ActiveInventoryTab switch
        {
            InventoryTab.Weapons => _selectedWeaponIndex,
            InventoryTab.Equipment => _selectedEquipmentIndex,
            _ => _selectedNoteIndex
        };
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
        int itemCount = ActiveInventoryTab == InventoryTab.Notes
            ? Player.Notes.Count
            : _activeInventoryItems.Count;
        int maximumIndex = itemCount - 1;
        SelectedInventoryIndex = maximumIndex < 0
            ? 0
            : System.Math.Clamp(SelectedInventoryIndex, 0, maximumIndex);
        RememberActiveTabSelection();
    }

    private void MarkSelectedNoteRead()
    {
        SelectedLoreNote?.MarkRead();
    }
}
