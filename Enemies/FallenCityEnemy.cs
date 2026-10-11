using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum FallenCityCombatState
{
    Ready,
    MilitiaWindup,
    MilitiaActive,
    MilitiaRecovery,
    MilitiaDefensiveStep,
    CrossbowAim,
    CrossbowFire,
    CrossbowReload,
    CrossbowReposition,
    CrossbowShoveWindup,
    CrossbowShoveActive,
    CrossbowShoveRecovery,
    HoundThreaten,
    HoundWindup,
    HoundActive,
    HoundRecovery,
    HoundReposition,
    PlagueFlaskWindup,
    PlagueFlaskRelease,
    PlagueFlaskRecovery,
    PlagueCoughWindup,
    PlagueCoughActive,
    PlagueCoughRecovery,
    WatcherNotice,
    WatcherAlarmApproach,
    WatcherAlarmWindup,
    WatcherAlarmSignal,
    WatcherAlarmRecovery,
    WatcherAlarmInterrupted,
    WatcherPanicSlashWindup,
    WatcherPanicSlashActive,
    WatcherPanicSlashRecovery,
    WatcherRetreat,
    ExecutionerWindup,
    ExecutionerActive,
    ExecutionerRecovery,
    RoyalApproach,
    RoyalGuardStance,
    RoyalWindup,
    RoyalActive,
    RoyalRecovery,
    RoyalGuardBroken,
    AshWindup,
    AshActive,
    AshRecovery,
    AshSurgeWindup,
    BishopWindup,
    BishopActive,
    BishopRecovery,
    BishopReposition,
    KingTransition,
    KingWindup,
    KingActive,
    KingRecovery,
    KingGuard,
    KingGuardBroken,
    KingCounterTell,
    KingCommandStep,
    KingLastCommandWarning,
    KingLastCommandPause,
    KingLastCommandCharge,
    KingLastCommandRecovery
}

public enum MilitiaAttackKind
{
    None,
    MilitiaSlash,
    ShortThrust,
    DesperateSwing
}

public enum CrossbowAttackKind
{
    None,
    StandardBolt,
    HeavyBolt,
    CrossbowShove
}

public enum RabidHoundAttackKind
{
    None,
    LungingBite,
    SideSnap,
    FrenzyBite
}

public enum PlagueCarrierAttackKind
{
    None,
    PlagueFlask,
    BlackCough
}

public enum BellWatcherActionKind
{
    None,
    AlarmSignal,
    PanicSlash,
    RetreatStep
}

public enum StreetExecutionerAttackKind
{
    None,
    HeavyCleave,
    ExecutionSlam,
    DraggingSweep,
    EnragedExecution
}

public enum RoyalGuardAttackKind
{
    None,
    RoyalThrust,
    ShieldBash,
    GuardedSlash
}

public enum AshKnightAttackKind
{
    None,
    EmberSlash,
    AshSweep,
    CinderStrike,
    EmberSurge
}

public enum WarBishopActionKind
{
    None,
    WarChant,
    CommandingSeal,
    StaffStrike,
    FinalBenediction,
    Reposition
}

public enum BrokenKingAttackKind
{
    None,
    RoyalSlash,
    KingsThrust,
    RoyalGuard,
    CrownCounter,
    CommandStep,
    AshCleave,
    RoyalCharge,
    CollapseStrike,
    LastCommand
}

public sealed class FallenCityEnemy : Enemy
{
    public const float MilitiaSlashWindupSeconds = .46f;
    public const float MilitiaSlashActiveSeconds = .12f;
    public const float MilitiaSlashRecoverySeconds = .42f;
    public const float MilitiaThrustWindupSeconds = .54f;
    public const float MilitiaThrustActiveSeconds = .13f;
    public const float MilitiaThrustRecoverySeconds = .48f;
    public const float DesperateSwingWindupSeconds = .76f;
    public const float DesperateSwingActiveSeconds = .15f;
    public const float DesperateSwingRecoverySeconds = .68f;
    public const float DefensiveStepSeconds = .28f;
    public const float StandardBoltAimSeconds = .82f;
    public const float HeavyBoltAimSeconds = 1.24f;
    public const float CrossbowFireSeconds = .10f;
    public const float StandardReloadSeconds = 1.12f;
    public const float HeavyReloadSeconds = 1.52f;
    public const float CrossbowShoveWindupSeconds = .28f;
    public const float CrossbowShoveActiveSeconds = .11f;
    public const float CrossbowShoveRecoverySeconds = .42f;
    public const float CrossbowRepositionSeconds = .42f;
    public const float HoundThreatenSeconds = .24f;
    public const float LungeWindupSeconds = .30f;
    public const float LungeActiveSeconds = .16f;
    public const float LungeRecoverySeconds = .27f;
    public const float SideSnapWindupSeconds = .18f;
    public const float SideSnapActiveSeconds = .11f;
    public const float SideSnapRecoverySeconds = .24f;
    public const float FrenzyWindupSeconds = .22f;
    public const float FrenzyActiveSeconds = .10f;
    public const float FrenzyLinkSeconds = .15f;
    public const float FrenzyRecoverySeconds = .38f;
    public const float HoundRepositionSeconds = .46f;
    public const float PlagueFlaskWindupSeconds = .72f;
    public const float PlagueFlaskReleaseSeconds = .12f;
    public const float PlagueFlaskRecoverySeconds = .68f;
    public const float BlackCoughWindupSeconds = .58f;
    public const float BlackCoughActiveSeconds = .24f;
    public const float BlackCoughRecoverySeconds = .72f;
    public const float WatcherNoticeSeconds = .36f;
    public const float WatcherRetreatSeconds = .46f;
    public const float WatcherApproachTimeoutSeconds = 2.4f;
    public const float AlarmWindupSeconds = 1.18f;
    public const float AlarmSignalSeconds = .32f;
    public const float AlarmRecoverySeconds = .58f;
    public const float AlarmInterruptedSeconds = .52f;
    public const float PanicSlashWindupSeconds = .34f;
    public const float PanicSlashActiveSeconds = .11f;
    public const float PanicSlashRecoverySeconds = .48f;
    public const float HeavyCleaveWindupSeconds = .82f;
    public const float HeavyCleaveActiveSeconds = .17f;
    public const float HeavyCleaveRecoverySeconds = .78f;
    public const float ExecutionSlamWindupSeconds = 1.08f;
    public const float ExecutionSlamActiveSeconds = .18f;
    public const float ExecutionSlamRecoverySeconds = .96f;
    public const float DraggingSweepWindupSeconds = .94f;
    public const float DraggingSweepActiveSeconds = .15f;
    public const float DraggingSweepRecoverySeconds = .82f;
    public const float EnragedExecutionWindupSeconds = .92f;
    public const float EnragedExecutionRecoverySeconds = .68f;
    public const float RoyalGuardStanceSeconds = .58f;
    public const float RoyalThrustWindupSeconds = .54f;
    public const float RoyalThrustActiveSeconds = .15f;
    public const float RoyalThrustRecoverySeconds = .58f;
    public const float RoyalBashWindupSeconds = .43f;
    public const float RoyalBashActiveSeconds = .13f;
    public const float RoyalBashRecoverySeconds = .66f;
    public const float GuardedSlashWindupSeconds = .50f;
    public const float GuardedSlashActiveSeconds = .16f;
    public const float GuardedSlashRecoverySeconds = .62f;
    public const float RoyalGuardBreakSeconds = 1.12f;
    public const float EmberSlashWindupSeconds = .43f;
    public const float EmberSlashActiveSeconds = .15f;
    public const float EmberSlashRecoverySeconds = .43f;
    public const float AshSweepWindupSeconds = .68f;
    public const float AshSweepActiveSeconds = .18f;
    public const float AshSweepRecoverySeconds = .66f;
    public const float CinderStrikeWindupSeconds = .88f;
    public const float CinderStrikeActiveSeconds = .17f;
    public const float CinderStrikeRecoverySeconds = .78f;
    public const float EmberSurgeWindupSeconds = .76f;
    public const float EmberSurgeDurationSeconds = 4.2f;
    public const float WarChantWindupSeconds = 1.12f;
    public const float WarChantActiveSeconds = .34f;
    public const float WarChantRecoverySeconds = .72f;
    public const float CommandingSealWindupSeconds = .82f;
    public const float CommandingSealActiveSeconds = .22f;
    public const float CommandingSealRecoverySeconds = .68f;
    public const float StaffStrikeWindupSeconds = .38f;
    public const float StaffStrikeActiveSeconds = .13f;
    public const float StaffStrikeRecoverySeconds = .58f;
    public const float FinalBenedictionWindupSeconds = 1.34f;
    public const float FinalBenedictionActiveSeconds = .42f;
    public const float FinalBenedictionRecoverySeconds = .90f;
    public const float BishopRepositionSeconds = .58f;
    public const float KingPhaseTransitionSeconds = .86f;
    public const float KingSlashWindupSeconds = .46f;
    public const float KingSlashActiveSeconds = .15f;
    public const float KingSlashRecoverySeconds = .54f;
    public const float KingsThrustWindupSeconds = .64f;
    public const float KingsThrustActiveSeconds = .17f;
    public const float KingsThrustRecoverySeconds = .68f;
    public const float KingGuardSeconds = .72f;
    public const float CrownCounterTellSeconds = .58f;
    public const float CrownCounterActiveSeconds = .15f;
    public const float CrownCounterRecoverySeconds = .82f;
    public const float CommandStepSeconds = .32f;
    public const float AshCleaveWindupSeconds = .70f;
    public const float AshCleaveActiveSeconds = .18f;
    public const float AshCleaveRecoverySeconds = .72f;
    public const float RoyalChargeWindupSeconds = .76f;
    public const float RoyalChargeActiveSeconds = .48f;
    public const float RoyalChargeRecoverySeconds = 1.02f;
    public const float CollapseStrikeWindupSeconds = .96f;
    public const float CollapseStrikeActiveSeconds = .20f;
    public const float CollapseStrikeRecoverySeconds = 1.08f;
    public const float LastCommandWarningSeconds = .88f;
    public const float LastCommandPauseSeconds = .54f;
    public const float LastCommandChargeSeconds = .62f;
    public const float LastCommandRecoverySeconds = 1.48f;
    public const float KingGuardBreakSeconds = 1.02f;

    private bool _alarmSignaled;
    private bool _pendingAlarmSignal;
    private float _supportCooldown;
    private float _combatCooldownRemaining;
    private float _actionTimeRemaining;
    private float _actionDuration;
    private bool _actionHitConsumed;
    private int _militiaPatternIndex;
    private int _crossbowPatternIndex;
    private int _houndPatternIndex;
    private int _frenzyStrikesRemaining;
    private bool _houndSideSnapReady;
    private int _plaguePatternIndex;
    private bool _watcherHasNoticed;
    private bool _hasAlarmTarget;
    private Vector2 _alarmTargetPosition;
    private int _executionerPatternIndex;
    private bool _executionerEnrageUsed;
    private float _executionerImpactTimeRemaining;
    private int _royalPatternIndex;
    private float _royalGuardIntegrity = 100f;
    private float _royalBlockReactionRemaining;
    private int _ashPatternIndex;
    private bool _emberSurgeUsed;
    private float _emberSurgeRemaining;
    private bool _ashHazardSpawned;
    private int _bishopPatternIndex;
    private bool _finalBenedictionUsed;
    private int _bishopNearbyAllies;
    private bool _bishopActionApplied;
    private int _kingPatternIndex;
    private int _activeKingPhase = 1;
    private bool _lastCommandUsed;
    private bool _kingActionApplied;
    private int _kingChainRemaining;
    private float _kingGuardIntegrity = 100f;
    private float _kingGuardReactionRemaining;
    private float _lockedAttackDirection = 1f;

    public FallenCityCombatState CombatState { get; private set; } =
        FallenCityCombatState.Ready;
    public MilitiaAttackKind MilitiaAttack { get; private set; }
    public CrossbowAttackKind CrossbowAttack { get; private set; }
    public RabidHoundAttackKind RabidHoundAttack { get; private set; }
    public PlagueCarrierAttackKind PlagueCarrierAttack { get; private set; }
    public BellWatcherActionKind BellWatcherAction { get; private set; }
    public StreetExecutionerAttackKind StreetExecutionerAttack
        { get; private set; }
    public RoyalGuardAttackKind RoyalGuardAttack { get; private set; }
    public AshKnightAttackKind AshKnightAttack { get; private set; }
    public WarBishopActionKind WarBishopAction { get; private set; }
    public BrokenKingAttackKind BrokenKingAttack { get; private set; }
    public int FrenzyStrikesRemaining => _frenzyStrikesRemaining;
    public bool IsHeavyBolt => CrossbowAttack == CrossbowAttackKind.HeavyBolt;
    public bool IsReloading =>
        CombatState == FallenCityCombatState.CrossbowReload;
    public bool IsDefensiveStepping => CombatState is
        FallenCityCombatState.MilitiaDefensiveStep or
        FallenCityCombatState.CrossbowReposition or
        FallenCityCombatState.HoundReposition;
    public bool AttackFacesLeft => _lockedAttackDirection < 0f;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(
            1f - _actionTimeRemaining / _actionDuration,
            0f,
            1f);

    public int CombatPhase => Type == EnemyType.BrokenKing
        ? CurrentHealth * 100 > MaxHealth * 60 ? 1
            : CurrentHealth * 100 > MaxHealth * 25 ? 2 : 3
        : 1;
    public bool IsSignalingAlarm => Type == EnemyType.BellWatcher &&
        !_alarmSignaled && CombatState is
            FallenCityCombatState.WatcherAlarmWindup or
            FallenCityCombatState.WatcherAlarmSignal;
    public bool IsAlarmWindup => Type == EnemyType.BellWatcher &&
        CombatState == FallenCityCombatState.WatcherAlarmWindup;
    public bool IsAlarmSignalActive => Type == EnemyType.BellWatcher &&
        CombatState == FallenCityCombatState.WatcherAlarmSignal;
    public bool IsAlarmInterrupted => Type == EnemyType.BellWatcher &&
        CombatState == FallenCityCombatState.WatcherAlarmInterrupted;
    public bool HasAlarmTarget => _hasAlarmTarget;
    public Vector2 AlarmTargetPosition => _alarmTargetPosition;
    public bool HasSignaledAlarm => _alarmSignaled;
    public float ExecutionerImpactProgress => _executionerImpactTimeRemaining <= 0f
        ? 0f
        : _executionerImpactTimeRemaining / .28f;
    public bool IsRoyalGuarding => Type == EnemyType.RoyalGuard &&
        CombatState is FallenCityCombatState.RoyalGuardStance or
            FallenCityCombatState.RoyalWindup;
    public bool IsRoyalGuardBroken => Type == EnemyType.RoyalGuard &&
        CombatState == FallenCityCombatState.RoyalGuardBroken;
    public bool IsRoyalBlockReacting => _royalBlockReactionRemaining > 0f;
    public float RoyalGuardIntegrityRatio =>
        MathHelper.Clamp(_royalGuardIntegrity / 100f, 0f, 1f);
    public bool IsEmberSurging => Type == EnemyType.AshKnight &&
        _emberSurgeRemaining > 0f;
    public float EmberSurgeRatio => MathHelper.Clamp(
        _emberSurgeRemaining / EmberSurgeDurationSeconds,
        0f,
        1f);
    public int BishopNearbyAllies => _bishopNearbyAllies;
    public bool IsBishopIsolated => Type == EnemyType.WarBishop &&
        _bishopNearbyAllies == 0;
    public bool IsKingPhaseTransitioning => Type == EnemyType.BrokenKing &&
        CombatState == FallenCityCombatState.KingTransition;
    public bool IsKingGuarding => Type == EnemyType.BrokenKing &&
        CombatState == FallenCityCombatState.KingGuard;
    public bool IsKingCounterTell => Type == EnemyType.BrokenKing &&
        CombatState == FallenCityCombatState.KingCounterTell;
    public bool IsKingGuardBroken => Type == EnemyType.BrokenKing &&
        CombatState == FallenCityCombatState.KingGuardBroken;
    public bool IsKingGuardReacting => _kingGuardReactionRemaining > 0f;
    public bool IsLastCommandWarning => Type == EnemyType.BrokenKing &&
        CombatState is FallenCityCombatState.KingLastCommandWarning or
            FallenCityCombatState.KingLastCommandPause;
    public bool IsLastCommandCharging => Type == EnemyType.BrokenKing &&
        CombatState == FallenCityCombatState.KingLastCommandCharge;
    public override bool UsesProjectileAttack =>
        Type is EnemyType.RuinedCrossbowman or EnemyType.PlagueCarrier;
    public override bool IsCombatAttackActive => Type switch
    {
        EnemyType.FallenMilitiaman =>
            CombatState == FallenCityCombatState.MilitiaActive,
        EnemyType.RuinedCrossbowman => CombatState is
            FallenCityCombatState.CrossbowFire or
            FallenCityCombatState.CrossbowShoveActive,
        EnemyType.RabidHound =>
            CombatState == FallenCityCombatState.HoundActive,
        EnemyType.PlagueCarrier => CombatState is
            FallenCityCombatState.PlagueFlaskRelease or
            FallenCityCombatState.PlagueCoughActive,
        EnemyType.BellWatcher => CombatState is
            FallenCityCombatState.WatcherAlarmSignal or
            FallenCityCombatState.WatcherPanicSlashActive,
        EnemyType.StreetExecutioner =>
            CombatState == FallenCityCombatState.ExecutionerActive,
        EnemyType.RoyalGuard =>
            CombatState == FallenCityCombatState.RoyalActive,
        EnemyType.AshKnight =>
            CombatState == FallenCityCombatState.AshActive,
        EnemyType.WarBishop =>
            CombatState == FallenCityCombatState.BishopActive &&
            WarBishopAction == WarBishopActionKind.StaffStrike,
        EnemyType.BrokenKing => CombatState is
            FallenCityCombatState.KingActive or
            FallenCityCombatState.KingLastCommandCharge,
        _ => base.IsCombatAttackActive
    };
    public override bool IsCombatAttackTelegraphing => Type switch
    {
        EnemyType.FallenMilitiaman =>
            CombatState == FallenCityCombatState.MilitiaWindup,
        EnemyType.RuinedCrossbowman => CombatState is
            FallenCityCombatState.CrossbowAim or
            FallenCityCombatState.CrossbowShoveWindup,
        EnemyType.RabidHound => CombatState is
            FallenCityCombatState.HoundThreaten or
            FallenCityCombatState.HoundWindup,
        EnemyType.PlagueCarrier => CombatState is
            FallenCityCombatState.PlagueFlaskWindup or
            FallenCityCombatState.PlagueCoughWindup,
        EnemyType.BellWatcher => CombatState is
            FallenCityCombatState.WatcherNotice or
            FallenCityCombatState.WatcherAlarmWindup or
            FallenCityCombatState.WatcherPanicSlashWindup,
        EnemyType.StreetExecutioner =>
            CombatState == FallenCityCombatState.ExecutionerWindup,
        EnemyType.RoyalGuard => CombatState is
            FallenCityCombatState.RoyalGuardStance or
            FallenCityCombatState.RoyalWindup,
        EnemyType.AshKnight => CombatState is
            FallenCityCombatState.AshWindup or
            FallenCityCombatState.AshSurgeWindup,
        EnemyType.WarBishop =>
            CombatState == FallenCityCombatState.BishopWindup,
        EnemyType.BrokenKing => CombatState is
            FallenCityCombatState.KingTransition or
            FallenCityCombatState.KingWindup or
            FallenCityCombatState.KingGuard or
            FallenCityCombatState.KingCounterTell or
            FallenCityCombatState.KingLastCommandWarning or
            FallenCityCombatState.KingLastCommandPause,
        _ => base.IsCombatAttackTelegraphing
    };
    public override Rectangle ActiveAttackArea => Type is
        EnemyType.FallenMilitiaman or EnemyType.RuinedCrossbowman or
        EnemyType.RabidHound or EnemyType.PlagueCarrier
        or EnemyType.BellWatcher or EnemyType.StreetExecutioner or
        EnemyType.RoyalGuard or EnemyType.AshKnight
        or EnemyType.WarBishop or EnemyType.BrokenKing
            ? CreatePolishedAttackArea()
            : base.ActiveAttackArea;
    protected override float DeathPresentationDurationSeconds => Type switch
    {
        EnemyType.FallenMilitiaman => .96f,
        EnemyType.RuinedCrossbowman => 1.08f,
        EnemyType.RabidHound => .88f,
        EnemyType.PlagueCarrier => 1.16f,
        EnemyType.BellWatcher => 1.02f,
        EnemyType.StreetExecutioner => 1.36f,
        EnemyType.RoyalGuard => 1.32f,
        EnemyType.AshKnight => 1.42f,
        EnemyType.WarBishop => 1.48f,
        EnemyType.BrokenKing => 2.45f,
        _ => base.DeathPresentationDurationSeconds
    };

    public FallenCityEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId,
        bool isElite)
        : base(
            type,
            position,
            GetSize(type),
            GetSpeed(type),
            GetDetection(type),
            Scale(GetBaseHealth(type), level, worldTier, true),
            Scale(GetBaseDamage(type), level, worldTier, false),
            GetExperience(type),
            level,
            worldTier,
            isElite,
            roomId,
            GetRange(type),
            GetCooldown(type),
            applyEliteModifiers: type != EnemyType.BrokenKing)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        _executionerImpactTimeRemaining = MathF.Max(
            0f,
            _executionerImpactTimeRemaining - elapsed);
        _royalBlockReactionRemaining = MathF.Max(
            0f,
            _royalBlockReactionRemaining - elapsed);
        _kingGuardReactionRemaining = MathF.Max(
            0f,
            _kingGuardReactionRemaining - elapsed);
        _emberSurgeRemaining = MathF.Max(
            0f,
            _emberSurgeRemaining - elapsed);

        if (CombatState == FallenCityCombatState.RoyalGuardBroken)
        {
            _actionTimeRemaining = MathF.Max(
                0f,
                _actionTimeRemaining - elapsed);
            if (_actionTimeRemaining <= 0f && IsAlive)
            {
                CombatState = FallenCityCombatState.Ready;
                RoyalGuardAttack = RoyalGuardAttackKind.None;
                _royalGuardIntegrity = 74f;
                _combatCooldownRemaining = .56f;
                _actionDuration = 0f;
            }
        }

        if (CombatState == FallenCityCombatState.KingGuardBroken)
        {
            _actionTimeRemaining = MathF.Max(
                0f,
                _actionTimeRemaining - elapsed);
            if (_actionTimeRemaining <= 0f && IsAlive)
            {
                CombatState = FallenCityCombatState.Ready;
                BrokenKingAttack = BrokenKingAttackKind.None;
                _kingGuardIntegrity = 76f;
                _combatCooldownRemaining = .58f;
                _actionDuration = 0f;
            }
        }
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        if (Type == EnemyType.BrokenKing && damage > 0 &&
            IsKingSourceInFront(sourcePosition))
        {
            if (IsKingCounterTell)
            {
                base.ReceiveDamage(
                    Math.Max(1, (int)MathF.Ceiling(damage * .38f)),
                    poiseDamage * .30f);
                BrokenKingAttack = BrokenKingAttackKind.CrownCounter;
                StartAction(
                    FallenCityCombatState.KingActive,
                    CrownCounterActiveSeconds);
                return;
            }

            if (IsKingGuarding)
            {
                bool strongKingHit = poiseDamage >= 48f || damage >= 42;
                base.ReceiveDamage(
                    Math.Max(1, (int)MathF.Ceiling(
                        damage * (strongKingHit ? .84f : .48f))),
                    strongKingHit ? poiseDamage : poiseDamage * .65f);
                _kingGuardReactionRemaining = .20f;
                _kingGuardIntegrity -= poiseDamage + damage * .34f;
                if (strongKingHit || _kingGuardIntegrity <= 0f || IsStaggered)
                    EnterKingGuardBreak();
                return;
            }
        }

        if (Type != EnemyType.RoyalGuard || damage <= 0 ||
            !IsRoyalGuarding || !IsRoyalSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        bool strongHit = poiseDamage >= 45f || damage >= 40;
        int guardedDamage = Math.Max(
            1,
            (int)MathF.Ceiling(damage * (strongHit ? .82f : .50f)));
        float guardedPoise = strongHit ? poiseDamage : poiseDamage * .68f;
        base.ReceiveDamage(guardedDamage, guardedPoise);
        _royalBlockReactionRemaining = .20f;
        _royalGuardIntegrity -= poiseDamage + damage * .36f;

        if (strongHit || _royalGuardIntegrity <= 0f || IsStaggered)
            EnterRoyalGuardBreak();
    }

    public void ConfigureAlarmTarget(Vector2? targetPosition)
    {
        if (Type != EnemyType.BellWatcher)
            return;
        _hasAlarmTarget = targetPosition.HasValue;
        _alarmTargetPosition = targetPosition ?? Position;
    }

    public bool TryConsumeAlarmSignal()
    {
        if (!_pendingAlarmSignal)
            return false;
        _pendingAlarmSignal = false;
        return true;
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (!IsPlayerDetected(player.Position) &&
            !(Type == EnemyType.BellWatcher &&
                _watcherHasNoticed && !_alarmSignaled))
            return;

        float elapsed = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        float distance = MathF.Abs(player.Position.X - Position.X);

        _combatCooldownRemaining = MathF.Max(
            0f,
            _combatCooldownRemaining - elapsed);

        if (Type == EnemyType.FallenMilitiaman)
        {
            UpdateFallenMilitiaman(
                gameTime,
                player,
                dungeon,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.RuinedCrossbowman)
        {
            UpdateRuinedCrossbowman(
                gameTime,
                player,
                dungeon,
                projectiles,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.RabidHound)
        {
            UpdateRabidHound(gameTime, player, dungeon, elapsed, distance);
            return;
        }

        if (Type == EnemyType.PlagueCarrier)
        {
            UpdatePlagueCarrier(
                gameTime,
                player,
                dungeon,
                projectiles,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.BellWatcher)
        {
            UpdateBellWatcher(
                gameTime,
                player,
                dungeon,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.StreetExecutioner)
        {
            UpdateStreetExecutioner(
                gameTime,
                player,
                dungeon,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.RoyalGuard)
        {
            UpdateRoyalGuard(gameTime, player, dungeon, elapsed, distance);
            return;
        }

        if (Type == EnemyType.AshKnight)
        {
            UpdateAshKnight(
                gameTime,
                player,
                dungeon,
                rootHazards,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.WarBishop)
        {
            UpdateWarBishop(
                gameTime,
                player,
                dungeon,
                rootHazards,
                enemies,
                elapsed,
                distance);
            return;
        }

        if (Type == EnemyType.BrokenKing)
        {
            UpdateBrokenKing(
                gameTime,
                player,
                dungeon,
                rootHazards,
                elapsed,
                distance);
            return;
        }

        if (UsesProjectileAttack)
        {
            const float preferred = 220f;
            if (distance < 145f)
                MoveAway(gameTime, player.Position, dungeon);
            else if (distance > preferred)
                MoveToward(gameTime, player.Position, preferred, dungeon);
            if (distance <= GetRange(Type) && Attack.TryStart())
            {
                Vector2 origin = Position + new Vector2(
                    Facing == EnemyFacingDirection.Left ? -20f : 20f,
                    -10f);
                projectiles.SpawnEnemyArrow(
                    origin,
                    player.Position - origin,
                    EffectiveAttackDamage,
                    RoomId,
                    heavy: Type == EnemyType.WarBishop);
            }
            return;
        }

        const float stop = 46f;
        float speed = Type == EnemyType.BrokenKing ?
            CombatPhase == 3 ? 1.35f : CombatPhase == 2 ? 1.15f : 1f : 1f;
        if (distance > stop + 8f)
            MoveToward(gameTime, player.Position, stop, dungeon, speed);
        else
            TryMeleeAttack(player);
    }

    protected override void OnDamaged()
    {
        if (Type == EnemyType.BellWatcher && IsSignalingAlarm)
            InterruptWatcherAlarm();
    }

    protected override void OnCombatInterrupted()
    {
        if (Type == EnemyType.BrokenKing)
        {
            if (IsKingGuarding)
                EnterKingGuardBreak();
            else if (!IsKingPhaseTransitioning)
                CancelPolishedAction();
            return;
        }

        if (Type == EnemyType.RoyalGuard)
        {
            if (IsRoyalGuarding)
                EnterRoyalGuardBreak();
            else
                CancelPolishedAction();
            return;
        }

        if (Type is EnemyType.FallenMilitiaman or
            EnemyType.RuinedCrossbowman or EnemyType.RabidHound or
            EnemyType.PlagueCarrier or EnemyType.BellWatcher or
            EnemyType.StreetExecutioner or EnemyType.AshKnight or
            EnemyType.WarBishop)
        {
            CancelPolishedAction();
        }
    }

    public Vector2 GetCrossbowReleasePosition()
    {
        float direction = AttackFacesLeft ? -1f : 1f;
        return new Vector2(
            Position.X + direction * 37f,
            Bounds.Bottom - 43f);
    }

    private void UpdateFallenMilitiaman(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateMilitiaAction(gameTime, player, dungeon, elapsed);
            return;
        }

        if (distance > 68f)
        {
            MoveToward(gameTime, player.Position, 54f, dungeon, .88f);
            return;
        }

        if (_combatCooldownRemaining > 0f)
            return;

        _militiaPatternIndex++;
        MilitiaAttackKind kind = CurrentHealth * 100 <= MaxHealth * 32 &&
            _militiaPatternIndex % 3 == 0
                ? MilitiaAttackKind.DesperateSwing
                : _militiaPatternIndex % 3 == 2
                    ? MilitiaAttackKind.ShortThrust
                    : MilitiaAttackKind.MilitiaSlash;
        StartMilitiaAttack(kind);
    }

    private void UpdateMilitiaAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.MilitiaDefensiveStep)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(direction * EffectiveMovementSpeed * .52f * elapsed, 0f),
                dungeon);
            FaceToward(player.Position);
        }

        if (CombatState == FallenCityCombatState.MilitiaActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            int damage = MilitiaAttack switch
            {
                MilitiaAttackKind.ShortThrust =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.08f),
                MilitiaAttackKind.DesperateSwing =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.42f),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .55f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.MilitiaWindup:
                StartAction(
                    FallenCityCombatState.MilitiaActive,
                    MilitiaActiveSecondsFor(MilitiaAttack));
                break;
            case FallenCityCombatState.MilitiaActive:
                StartAction(
                    FallenCityCombatState.MilitiaRecovery,
                    MilitiaRecoverySecondsFor(MilitiaAttack));
                break;
            case FallenCityCombatState.MilitiaRecovery:
                if (_militiaPatternIndex % 3 == 0)
                {
                    StartAction(
                        FallenCityCombatState.MilitiaDefensiveStep,
                        DefensiveStepSeconds);
                }
                else
                {
                    FinishMilitiaAction();
                }
                break;
            case FallenCityCombatState.MilitiaDefensiveStep:
                FinishMilitiaAction();
                break;
        }
    }

    private void UpdateRuinedCrossbowman(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateCrossbowAction(
                gameTime,
                player,
                dungeon,
                projectiles,
                elapsed);
            return;
        }

        if (distance < 70f && _combatCooldownRemaining <= 0f)
        {
            CrossbowAttack = CrossbowAttackKind.CrossbowShove;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.CrossbowShoveWindup,
                CrossbowShoveWindupSeconds);
            return;
        }

        if (distance < 126f)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(direction * EffectiveMovementSpeed * .36f * elapsed, 0f),
                dungeon);
            FaceToward(player.Position);
            return;
        }

        if (distance > 430f)
        {
            MoveToward(gameTime, player.Position, 390f, dungeon, .30f);
            return;
        }

        if (_combatCooldownRemaining > 0f)
            return;

        _crossbowPatternIndex++;
        CrossbowAttack = _crossbowPatternIndex % 4 == 0
            ? CrossbowAttackKind.HeavyBolt
            : CrossbowAttackKind.StandardBolt;
        LockAttackDirection(player.Position.X);
        StartAction(
            FallenCityCombatState.CrossbowAim,
            IsHeavyBolt ? HeavyBoltAimSeconds : StandardBoltAimSeconds);
    }

    private void UpdateCrossbowAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.CrossbowReposition)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(direction * EffectiveMovementSpeed * .42f * elapsed, 0f),
                dungeon);
            FaceToward(player.Position);
        }

        if (CombatState == FallenCityCombatState.CrossbowShoveActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    Math.Max(1, EffectiveAttackDamage / 2),
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result is AttackResolution.Damaged or
                AttackResolution.GuardBroken)
            {
                player.PushAwayFrom(Position, 26f, dungeon);
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.CrossbowAim:
            {
                Vector2 release = GetCrossbowReleasePosition();
                Vector2 target = player.Position + new Vector2(0f, -8f);
                int damage = IsHeavyBolt
                    ? (int)MathF.Ceiling(EffectiveAttackDamage * 1.72f)
                    : (int)MathF.Ceiling(EffectiveAttackDamage * 1.18f);
                projectiles.SpawnEnemyBolt(
                    release,
                    target - release,
                    damage,
                    RoomId,
                    IsHeavyBolt);
                StartAction(
                    FallenCityCombatState.CrossbowFire,
                    CrossbowFireSeconds);
                break;
            }
            case FallenCityCombatState.CrossbowFire:
                StartAction(
                    FallenCityCombatState.CrossbowReload,
                    IsHeavyBolt
                        ? HeavyReloadSeconds
                        : StandardReloadSeconds);
                break;
            case FallenCityCombatState.CrossbowReload:
                _combatCooldownRemaining = IsHeavyBolt ? .72f : .52f;
                FinishCrossbowAction();
                break;
            case FallenCityCombatState.CrossbowShoveWindup:
                StartAction(
                    FallenCityCombatState.CrossbowShoveActive,
                    CrossbowShoveActiveSeconds);
                break;
            case FallenCityCombatState.CrossbowShoveActive:
                StartAction(
                    FallenCityCombatState.CrossbowShoveRecovery,
                    CrossbowShoveRecoverySeconds);
                break;
            case FallenCityCombatState.CrossbowShoveRecovery:
                StartAction(
                    FallenCityCombatState.CrossbowReposition,
                    CrossbowRepositionSeconds);
                break;
            case FallenCityCombatState.CrossbowReposition:
                _combatCooldownRemaining = .58f;
                FinishCrossbowAction();
                break;
        }
    }

    private void UpdateRabidHound(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateHoundAction(player, dungeon, elapsed);
            return;
        }

        if (distance > 116f)
        {
            MoveToward(gameTime, player.Position, 82f, dungeon, 1.05f);
            return;
        }

        if (_combatCooldownRemaining > 0f)
            return;

        _houndPatternIndex++;
        RabidHoundAttack = RabidHoundAttackKind.None;
        LockAttackDirection(player.Position.X);
        StartAction(
            FallenCityCombatState.HoundThreaten,
            HoundThreatenSeconds);
    }

    private void UpdateHoundAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.HoundActive)
        {
            float speedScale = RabidHoundAttack switch
            {
                RabidHoundAttackKind.LungingBite => 1.45f,
                RabidHoundAttackKind.FrenzyBite => 1.05f,
                _ => .58f
            };
            MoveBy(
                new Vector2(
                    _lockedAttackDirection * EffectiveMovementSpeed *
                        speedScale * elapsed,
                    0f),
                dungeon);

            if (!_actionHitConsumed && TouchesPlayerDefense(player))
            {
                int damage = RabidHoundAttack switch
                {
                    RabidHoundAttackKind.LungingBite =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.08f),
                    RabidHoundAttackKind.SideSnap =>
                        Math.Max(1, EffectiveAttackDamage * 4 / 5),
                    RabidHoundAttackKind.FrenzyBite =>
                        Math.Max(1, EffectiveAttackDamage * 3 / 4),
                    _ => EffectiveAttackDamage
                };
                AttackResolution result = player.ReceiveMeleeAttack(
                    new AttackContact(
                        damage,
                        Position,
                        blockable: true,
                        unblockable: false,
                        ActiveAttackArea));
                if (result == AttackResolution.PerfectGuard)
                    ApplyPoiseDamage(MaxPoise * .62f);
                _actionHitConsumed = true;
            }
        }
        else if (CombatState == FallenCityCombatState.HoundReposition)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(
                    direction * EffectiveMovementSpeed * .92f * elapsed,
                    0f),
                dungeon);
            FaceToward(player.Position);
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.HoundThreaten:
            {
                RabidHoundAttackKind kind =
                    CurrentHealth * 100 <= MaxHealth * 35 &&
                    _houndPatternIndex % 3 == 0
                        ? RabidHoundAttackKind.FrenzyBite
                        : _houndSideSnapReady
                            ? RabidHoundAttackKind.SideSnap
                            : RabidHoundAttackKind.LungingBite;
                StartHoundAttack(kind);
                break;
            }
            case FallenCityCombatState.HoundWindup:
                StartAction(
                    FallenCityCombatState.HoundActive,
                    HoundActiveSecondsFor(RabidHoundAttack));
                break;
            case FallenCityCombatState.HoundActive:
                if (RabidHoundAttack == RabidHoundAttackKind.FrenzyBite &&
                    _frenzyStrikesRemaining > 1)
                {
                    _frenzyStrikesRemaining--;
                    StartAction(
                        FallenCityCombatState.HoundWindup,
                        FrenzyLinkSeconds);
                }
                else
                {
                    _frenzyStrikesRemaining = 0;
                    StartAction(
                        FallenCityCombatState.HoundRecovery,
                        HoundRecoverySecondsFor(RabidHoundAttack));
                }
                break;
            case FallenCityCombatState.HoundRecovery:
                StartAction(
                    FallenCityCombatState.HoundReposition,
                    HoundRepositionSeconds);
                break;
            case FallenCityCombatState.HoundReposition:
                _houndSideSnapReady =
                    RabidHoundAttack != RabidHoundAttackKind.SideSnap;
                FinishHoundAction();
                break;
        }
    }

    private void UpdatePlagueCarrier(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdatePlagueAction(player, projectiles, dungeon, elapsed);
            return;
        }

        if (distance > 315f)
        {
            MoveToward(gameTime, player.Position, 245f, dungeon, .58f);
            return;
        }

        if (distance < 88f && _combatCooldownRemaining <= 0f)
        {
            _plaguePatternIndex++;
            PlagueCarrierAttack = PlagueCarrierAttackKind.BlackCough;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.PlagueCoughWindup,
                BlackCoughWindupSeconds);
            return;
        }

        if (distance < 112f)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(
                    direction * EffectiveMovementSpeed * .28f * elapsed,
                    0f),
                dungeon);
            FaceToward(player.Position);
            return;
        }

        if (_combatCooldownRemaining > 0f)
            return;

        _plaguePatternIndex++;
        PlagueCarrierAttack = PlagueCarrierAttackKind.PlagueFlask;
        LockAttackDirection(player.Position.X);
        StartAction(
            FallenCityCombatState.PlagueFlaskWindup,
            PlagueFlaskWindupSeconds);
    }

    private void UpdatePlagueAction(
        PlayerCharacter player,
        ProjectileManager projectiles,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.PlagueCoughActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    Math.Max(1, (int)MathF.Ceiling(
                        EffectiveAttackDamage * .42f)),
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result is AttackResolution.Damaged or
                AttackResolution.GuardBroken)
            {
                player.ApplySlow(.74f, 1.35f);
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.PlagueFlaskWindup:
            {
                float direction = AttackFacesLeft ? -1f : 1f;
                Vector2 origin = new(
                    Position.X + direction * 19f,
                    Bounds.Bottom - 51f);
                Vector2 landing = new(
                    player.Position.X,
                    player.Bounds.Bottom - 7f);
                projectiles.SpawnPlagueFlask(
                    origin,
                    landing,
                    RoomId,
                    dungeon);
                StartAction(
                    FallenCityCombatState.PlagueFlaskRelease,
                    PlagueFlaskReleaseSeconds);
                break;
            }
            case FallenCityCombatState.PlagueFlaskRelease:
                StartAction(
                    FallenCityCombatState.PlagueFlaskRecovery,
                    PlagueFlaskRecoverySeconds);
                break;
            case FallenCityCombatState.PlagueFlaskRecovery:
                _combatCooldownRemaining = 1.18f;
                FinishPlagueAction();
                break;
            case FallenCityCombatState.PlagueCoughWindup:
                StartAction(
                    FallenCityCombatState.PlagueCoughActive,
                    BlackCoughActiveSeconds);
                break;
            case FallenCityCombatState.PlagueCoughActive:
                StartAction(
                    FallenCityCombatState.PlagueCoughRecovery,
                    BlackCoughRecoverySeconds);
                break;
            case FallenCityCombatState.PlagueCoughRecovery:
                _combatCooldownRemaining = 1.02f;
                FinishPlagueAction();
                break;
        }
    }

    private void UpdateBellWatcher(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateWatcherAction(gameTime, player, dungeon, elapsed);
            return;
        }

        if (!_watcherHasNoticed)
        {
            BellWatcherAction = BellWatcherActionKind.AlarmSignal;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.WatcherNotice,
                WatcherNoticeSeconds);
            return;
        }

        if (_alarmSignaled)
        {
            if (distance > 62f)
            {
                MoveToward(gameTime, player.Position, 54f, dungeon, .72f);
            }
            else if (_combatCooldownRemaining <= 0f)
            {
                StartWatcherPanicSlash(player.Position.X);
            }
            return;
        }

        if (distance < 62f && _combatCooldownRemaining <= 0f)
        {
            StartWatcherPanicSlash(player.Position.X);
            return;
        }

        BeginWatcherAlarmRoute();
    }

    private void UpdateWatcherAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.WatcherAlarmApproach)
        {
            if (!_hasAlarmTarget)
            {
                StartWatcherAlarmWindup(player.Position.X);
                return;
            }

            float targetDistance = MathF.Abs(
                _alarmTargetPosition.X - Position.X);
            if (targetDistance <= 38f)
            {
                StartWatcherAlarmWindup(player.Position.X);
                return;
            }

            MoveToward(
                gameTime,
                _alarmTargetPosition,
                30f,
                dungeon,
                1.08f);
        }
        else if (CombatState == FallenCityCombatState.WatcherRetreat)
        {
            MoveAway(gameTime, player.Position, dungeon, 1.12f);
        }
        else if (CombatState ==
            FallenCityCombatState.WatcherPanicSlashActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    Math.Max(1, EffectiveAttackDamage * 3 / 4),
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .72f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.WatcherNotice:
                _watcherHasNoticed = true;
                BellWatcherAction = BellWatcherActionKind.RetreatStep;
                StartAction(
                    FallenCityCombatState.WatcherRetreat,
                    WatcherRetreatSeconds);
                break;
            case FallenCityCombatState.WatcherRetreat:
                if (_alarmSignaled)
                    FinishWatcherAction(.72f);
                else if (MathF.Abs(player.Position.X - Position.X) < 78f &&
                    _combatCooldownRemaining <= 0f)
                    StartWatcherPanicSlash(player.Position.X);
                else
                    BeginWatcherAlarmRoute();
                break;
            case FallenCityCombatState.WatcherAlarmApproach:
                // A blocked or distant alarm post falls back to the carried
                // horn, preserving the encounter even on alternate routes.
                StartWatcherAlarmWindup(player.Position.X);
                break;
            case FallenCityCombatState.WatcherAlarmWindup:
                StartAction(
                    FallenCityCombatState.WatcherAlarmSignal,
                    AlarmSignalSeconds);
                break;
            case FallenCityCombatState.WatcherAlarmSignal:
                _alarmSignaled = true;
                _pendingAlarmSignal = true;
                StartAction(
                    FallenCityCombatState.WatcherAlarmRecovery,
                    AlarmRecoverySeconds);
                break;
            case FallenCityCombatState.WatcherAlarmRecovery:
                FinishWatcherAction(.72f);
                break;
            case FallenCityCombatState.WatcherAlarmInterrupted:
                BellWatcherAction = BellWatcherActionKind.RetreatStep;
                StartAction(
                    FallenCityCombatState.WatcherRetreat,
                    WatcherRetreatSeconds);
                break;
            case FallenCityCombatState.WatcherPanicSlashWindup:
                StartAction(
                    FallenCityCombatState.WatcherPanicSlashActive,
                    PanicSlashActiveSeconds);
                break;
            case FallenCityCombatState.WatcherPanicSlashActive:
                StartAction(
                    FallenCityCombatState.WatcherPanicSlashRecovery,
                    PanicSlashRecoverySeconds);
                break;
            case FallenCityCombatState.WatcherPanicSlashRecovery:
                BellWatcherAction = BellWatcherActionKind.RetreatStep;
                StartAction(
                    FallenCityCombatState.WatcherRetreat,
                    WatcherRetreatSeconds);
                break;
        }
    }

    private void BeginWatcherAlarmRoute()
    {
        BellWatcherAction = BellWatcherActionKind.AlarmSignal;
        if (_hasAlarmTarget &&
            MathF.Abs(_alarmTargetPosition.X - Position.X) > 38f)
        {
            LockAttackDirection(_alarmTargetPosition.X);
            StartAction(
                FallenCityCombatState.WatcherAlarmApproach,
                WatcherApproachTimeoutSeconds);
            return;
        }

        StartWatcherAlarmWindup(_hasAlarmTarget
            ? _alarmTargetPosition.X
            : Position.X + (Facing == EnemyFacingDirection.Left ? -1f : 1f));
    }

    private void StartWatcherAlarmWindup(float facingTargetX)
    {
        BellWatcherAction = BellWatcherActionKind.AlarmSignal;
        LockAttackDirection(facingTargetX);
        StartAction(
            FallenCityCombatState.WatcherAlarmWindup,
            AlarmWindupSeconds);
    }

    private void StartWatcherPanicSlash(float playerX)
    {
        BellWatcherAction = BellWatcherActionKind.PanicSlash;
        LockAttackDirection(playerX);
        StartAction(
            FallenCityCombatState.WatcherPanicSlashWindup,
            PanicSlashWindupSeconds);
    }

    private void InterruptWatcherAlarm()
    {
        if (_alarmSignaled || !IsSignalingAlarm)
            return;
        _pendingAlarmSignal = false;
        BellWatcherAction = BellWatcherActionKind.AlarmSignal;
        StartAction(
            FallenCityCombatState.WatcherAlarmInterrupted,
            AlarmInterruptedSeconds);
    }

    private void FinishWatcherAction(float cooldown)
    {
        CombatState = FallenCityCombatState.Ready;
        BellWatcherAction = BellWatcherActionKind.None;
        _combatCooldownRemaining = cooldown;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void UpdateStreetExecutioner(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed,
        float distance)
    {
        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateExecutionerAction(player, dungeon, elapsed);
            return;
        }

        if (distance > 90f)
        {
            MoveToward(gameTime, player.Position, 76f, dungeon, .76f);
            return;
        }

        if (_combatCooldownRemaining > 0f)
            return;

        StreetExecutionerAttackKind kind;
        if (!_executionerEnrageUsed &&
            CurrentHealth * 100 <= MaxHealth * 35)
        {
            kind = StreetExecutionerAttackKind.EnragedExecution;
            _executionerEnrageUsed = true;
        }
        else
        {
            kind = (_executionerPatternIndex++ % 3) switch
            {
                1 => StreetExecutionerAttackKind.ExecutionSlam,
                2 => StreetExecutionerAttackKind.DraggingSweep,
                _ => StreetExecutionerAttackKind.HeavyCleave
            };
        }

        StreetExecutionerAttack = kind;
        LockAttackDirection(player.Position.X);
        StartAction(
            FallenCityCombatState.ExecutionerWindup,
            ExecutionerWindupSecondsFor(kind));
    }

    private void UpdateExecutionerAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.ExecutionerActive &&
            !_actionHitConsumed &&
            TouchesPlayerDefense(player) &&
            HasClearExecutionerPath(player, dungeon))
        {
            int damage = StreetExecutionerAttack switch
            {
                StreetExecutionerAttackKind.ExecutionSlam =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.48f),
                StreetExecutionerAttackKind.EnragedExecution =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.55f),
                StreetExecutionerAttackKind.DraggingSweep =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * .94f),
                _ => (int)MathF.Ceiling(EffectiveAttackDamage * 1.16f)
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .24f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.ExecutionerWindup:
                if (StreetExecutionerAttack is
                    StreetExecutionerAttackKind.ExecutionSlam or
                    StreetExecutionerAttackKind.EnragedExecution)
                {
                    _executionerImpactTimeRemaining = .28f;
                }
                StartAction(
                    FallenCityCombatState.ExecutionerActive,
                    ExecutionerActiveSecondsFor(StreetExecutionerAttack));
                break;
            case FallenCityCombatState.ExecutionerActive:
                StartAction(
                    FallenCityCombatState.ExecutionerRecovery,
                    ExecutionerRecoverySecondsFor(StreetExecutionerAttack));
                break;
            case FallenCityCombatState.ExecutionerRecovery:
                CombatState = FallenCityCombatState.Ready;
                StreetExecutionerAttack =
                    StreetExecutionerAttackKind.None;
                _combatCooldownRemaining = .64f;
                _actionDuration = 0f;
                _actionTimeRemaining = 0f;
                break;
        }
    }

    private void UpdateRoyalGuard(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed,
        float distance)
    {
        if (CombatState == FallenCityCombatState.RoyalGuardBroken)
            return;

        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateRoyalAction(player, dungeon, elapsed);
            return;
        }

        if (distance > 94f)
        {
            MoveToward(gameTime, player.Position, 80f, dungeon, .68f);
            return;
        }

        FaceToward(player.Position);
        if (_combatCooldownRemaining > 0f)
            return;

        _royalPatternIndex++;
        _royalGuardIntegrity = 100f;
        RoyalGuardAttack = distance < 57f && _royalPatternIndex % 3 == 2
            ? RoyalGuardAttackKind.ShieldBash
            : _royalPatternIndex % 3 == 0
                ? RoyalGuardAttackKind.GuardedSlash
                : RoyalGuardAttackKind.RoyalThrust;
        LockAttackDirection(player.Position.X);
        StartAction(
            FallenCityCombatState.RoyalGuardStance,
            RoyalGuardStanceSeconds);
    }

    private void UpdateRoyalAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.RoyalActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            int damage = RoyalGuardAttack switch
            {
                RoyalGuardAttackKind.RoyalThrust =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.18f),
                RoyalGuardAttackKind.ShieldBash =>
                    Math.Max(1, (int)MathF.Ceiling(
                        EffectiveAttackDamage * .70f)),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .28f);
            if (RoyalGuardAttack == RoyalGuardAttackKind.ShieldBash &&
                result is AttackResolution.Damaged or
                    AttackResolution.GuardBroken)
            {
                player.PushAwayFrom(Position, 34f, dungeon);
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.RoyalGuardStance:
                StartAction(
                    FallenCityCombatState.RoyalWindup,
                    RoyalWindupSecondsFor(RoyalGuardAttack));
                break;
            case FallenCityCombatState.RoyalWindup:
                StartAction(
                    FallenCityCombatState.RoyalActive,
                    RoyalActiveSecondsFor(RoyalGuardAttack));
                break;
            case FallenCityCombatState.RoyalActive:
                StartAction(
                    FallenCityCombatState.RoyalRecovery,
                    RoyalRecoverySecondsFor(RoyalGuardAttack));
                break;
            case FallenCityCombatState.RoyalRecovery:
                FinishRoyalAction();
                break;
        }
    }

    private void UpdateAshKnight(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        float elapsed,
        float distance)
    {
        if (_emberSurgeRemaining > 0f)
            ApplyTemporaryBuff(15, 12, .20f);

        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateAshAction(player, dungeon, rootHazards, elapsed);
            return;
        }

        if (!_emberSurgeUsed && CurrentHealth * 100 <= MaxHealth * 35 &&
            _combatCooldownRemaining <= 0f)
        {
            _emberSurgeUsed = true;
            AshKnightAttack = AshKnightAttackKind.EmberSurge;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.AshSurgeWindup,
                EmberSurgeWindupSeconds);
            return;
        }

        if (distance > 104f)
        {
            MoveToward(
                gameTime,
                player.Position,
                78f,
                dungeon,
                _emberSurgeRemaining > 0f ? 1.08f : .88f);
            return;
        }

        FaceToward(player.Position);
        if (_combatCooldownRemaining > 0f)
            return;

        _ashPatternIndex++;
        AshKnightAttackKind kind = (_ashPatternIndex % 3) switch
        {
            1 => AshKnightAttackKind.EmberSlash,
            2 => AshKnightAttackKind.AshSweep,
            _ => AshKnightAttackKind.CinderStrike
        };
        StartAshAttack(kind, player.Position.X);
    }

    private void UpdateAshAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.AshActive &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            int damage = AshKnightAttack switch
            {
                AshKnightAttackKind.CinderStrike =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.28f),
                AshKnightAttackKind.AshSweep =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * .92f),
                _ => (int)MathF.Ceiling(EffectiveAttackDamage * 1.06f)
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .31f);
            _actionHitConsumed = true;
        }

        if (CombatState == FallenCityCombatState.AshActive &&
            !_ashHazardSpawned)
        {
            SpawnAshHazard(rootHazards, dungeon);
            _ashHazardSpawned = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.AshSurgeWindup:
                _emberSurgeRemaining = EmberSurgeDurationSeconds;
                AshKnightAttack = AshKnightAttackKind.None;
                CombatState = FallenCityCombatState.Ready;
                _combatCooldownRemaining = .34f;
                _actionDuration = 0f;
                break;
            case FallenCityCombatState.AshWindup:
                _ashHazardSpawned = false;
                StartAction(
                    FallenCityCombatState.AshActive,
                    AshActiveSecondsFor(AshKnightAttack));
                break;
            case FallenCityCombatState.AshActive:
                StartAction(
                    FallenCityCombatState.AshRecovery,
                    AshRecoverySecondsFor(AshKnightAttack));
                break;
            case FallenCityCombatState.AshRecovery:
                FinishAshAction();
                break;
        }
    }

    private void SpawnAshHazard(
        RootHazardManager rootHazards,
        DungeonMap dungeon)
    {
        if (rootHazards == null || AshKnightAttack ==
            AshKnightAttackKind.EmberSlash)
        {
            return;
        }

        float direction = AttackFacesLeft ? -1f : 1f;
        bool cinder = AshKnightAttack == AshKnightAttackKind.CinderStrike;
        Vector2 size = cinder ? new Vector2(78f, 22f) :
            new Vector2(112f, 16f);
        Vector2 center = new(
            Position.X + direction * (cinder ? 48f : 57f),
            Bounds.Bottom - size.Y / 2f);
        rootHazards.TryAddEmberHazard(
            center,
            size,
            cinder ? .34f : .18f,
            cinder ? 2.15f : 1.35f,
            cinder ? Math.Max(2, EffectiveAttackDamage / 4) :
                Math.Max(1, EffectiveAttackDamage / 6),
            RoomId,
            this,
            isCinderZone: cinder,
            dungeon: dungeon);
    }

    private void StartAshAttack(AshKnightAttackKind kind, float targetX)
    {
        AshKnightAttack = kind;
        _ashHazardSpawned = false;
        LockAttackDirection(targetX);
        StartAction(
            FallenCityCombatState.AshWindup,
            AshWindupSecondsFor(kind));
    }

    private void FinishAshAction()
    {
        CombatState = FallenCityCombatState.Ready;
        AshKnightAttack = AshKnightAttackKind.None;
        _ashHazardSpawned = false;
        _combatCooldownRemaining = _emberSurgeRemaining > 0f ? .30f : .52f;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void FinishRoyalAction()
    {
        CombatState = FallenCityCombatState.Ready;
        RoyalGuardAttack = RoyalGuardAttackKind.None;
        _combatCooldownRemaining = .52f;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void EnterRoyalGuardBreak()
    {
        if (Type != EnemyType.RoyalGuard ||
            CombatState == FallenCityCombatState.RoyalGuardBroken || !IsAlive)
        {
            return;
        }

        RoyalGuardAttack = RoyalGuardAttackKind.None;
        _royalGuardIntegrity = 0f;
        _royalBlockReactionRemaining = 0f;
        StartAction(
            FallenCityCombatState.RoyalGuardBroken,
            RoyalGuardBreakSeconds);
    }

    private bool IsRoyalSourceInFront(Vector2 sourcePosition)
    {
        float direction = AttackFacesLeft ? -1f : 1f;
        return (sourcePosition.X - Position.X) * direction >= -5f;
    }

    private void UpdateWarBishop(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        EnemyManager enemies,
        float elapsed,
        float distance)
    {
        _bishopNearbyAllies = CountBishopAllies(enemies);
        _supportCooldown = MathF.Max(0f, _supportCooldown - elapsed);

        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateBishopAction(
                player,
                dungeon,
                rootHazards,
                enemies,
                elapsed);
            return;
        }

        if (!_finalBenedictionUsed && _bishopNearbyAllies > 0 &&
            CurrentHealth * 100 <= MaxHealth * 30 &&
            _combatCooldownRemaining <= 0f)
        {
            _finalBenedictionUsed = true;
            StartBishopAction(
                WarBishopActionKind.FinalBenediction,
                player.Position.X);
            return;
        }

        if (distance < 64f && _combatCooldownRemaining <= 0f)
        {
            StartBishopAction(
                WarBishopActionKind.StaffStrike,
                player.Position.X);
            return;
        }

        if (_bishopNearbyAllies == 0)
        {
            if (distance < 155f)
            {
                WarBishopAction = WarBishopActionKind.Reposition;
                LockAttackDirection(player.Position.X);
                StartAction(
                    FallenCityCombatState.BishopReposition,
                    BishopRepositionSeconds);
            }
            else if (distance > 235f)
            {
                MoveToward(gameTime, player.Position, 205f, dungeon, .52f);
            }
            else
            {
                FaceToward(player.Position);
            }
            return;
        }

        if (distance < 150f)
        {
            MoveAway(gameTime, player.Position, dungeon);
            return;
        }
        if (distance > 285f)
        {
            MoveToward(gameTime, player.Position, 245f, dungeon, .52f);
            return;
        }
        if (_combatCooldownRemaining > 0f || _supportCooldown > 0f)
            return;

        _bishopPatternIndex++;
        StartBishopAction(
            _bishopPatternIndex % 2 == 0
                ? WarBishopActionKind.CommandingSeal
                : WarBishopActionKind.WarChant,
            player.Position.X);
    }

    private void UpdateBishopAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        EnemyManager enemies,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.BishopReposition)
        {
            float away = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(
                    away * EffectiveMovementSpeed * .72f * elapsed,
                    0f),
                dungeon);
            FaceToward(player.Position);
        }

        if (CombatState == FallenCityCombatState.BishopActive &&
            !_bishopActionApplied)
        {
            ApplyBishopAction(player, rootHazards, enemies, dungeon);
            _bishopActionApplied = true;
        }

        if (CombatState == FallenCityCombatState.BishopActive &&
            WarBishopAction == WarBishopActionKind.StaffStrike &&
            !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    Math.Max(1, (int)MathF.Ceiling(
                        EffectiveAttackDamage * .66f)),
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .45f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.BishopWindup:
                _bishopActionApplied = false;
                StartAction(
                    FallenCityCombatState.BishopActive,
                    BishopActiveSecondsFor(WarBishopAction));
                break;
            case FallenCityCombatState.BishopActive:
                StartAction(
                    FallenCityCombatState.BishopRecovery,
                    BishopRecoverySecondsFor(WarBishopAction));
                break;
            case FallenCityCombatState.BishopRecovery:
            case FallenCityCombatState.BishopReposition:
                FinishBishopAction();
                break;
        }
    }

    private void ApplyBishopAction(
        PlayerCharacter player,
        RootHazardManager rootHazards,
        EnemyManager enemies,
        DungeonMap dungeon)
    {
        if (WarBishopAction is WarBishopActionKind.WarChant or
            WarBishopActionKind.FinalBenediction)
        {
            int damageBuff = WarBishopAction ==
                WarBishopActionKind.FinalBenediction ? 18 : 10;
            int movementBuff = WarBishopAction ==
                WarBishopActionKind.FinalBenediction ? 10 : 6;
            float duration = WarBishopAction ==
                WarBishopActionKind.FinalBenediction ? 4.2f : 3.2f;
            if (enemies != null)
            {
                foreach (Enemy ally in enemies.Enemies)
                {
                    if (!ReferenceEquals(ally, this) && ally.IsAlive &&
                        Vector2.DistanceSquared(ally.Position, Position) <=
                            290f * 290f)
                    {
                        ally.ApplyTemporaryBuff(
                            damageBuff,
                            movementBuff,
                            duration);
                    }
                }
            }
            return;
        }

        if (WarBishopAction == WarBishopActionKind.CommandingSeal &&
            rootHazards != null)
        {
            rootHazards.TryAddCommandSeal(
                new Vector2(player.Position.X, player.Bounds.Bottom - 12f),
                new Vector2(118f, 24f),
                .42f,
                2.65f,
                Math.Max(1, EffectiveAttackDamage / 7),
                .88f,
                .28f,
                RoomId,
                this,
                dungeon);
        }
    }

    private int CountBishopAllies(EnemyManager enemies)
    {
        if (enemies == null)
            return 0;
        int count = 0;
        foreach (Enemy ally in enemies.Enemies)
        {
            if (!ReferenceEquals(ally, this) && ally.IsAlive &&
                Vector2.DistanceSquared(ally.Position, Position) <=
                    290f * 290f)
            {
                count++;
            }
        }
        return count;
    }

    private void StartBishopAction(WarBishopActionKind kind, float targetX)
    {
        WarBishopAction = kind;
        _bishopActionApplied = false;
        LockAttackDirection(targetX);
        StartAction(
            FallenCityCombatState.BishopWindup,
            BishopWindupSecondsFor(kind));
    }

    private void FinishBishopAction()
    {
        float cooldown = WarBishopAction switch
        {
            WarBishopActionKind.WarChant => 2.15f,
            WarBishopActionKind.CommandingSeal => 1.65f,
            WarBishopActionKind.FinalBenediction => 2.65f,
            _ => .68f
        };
        _supportCooldown = WarBishopAction is WarBishopActionKind.WarChant or
            WarBishopActionKind.FinalBenediction ? cooldown : 0f;
        CombatState = FallenCityCombatState.Ready;
        WarBishopAction = WarBishopActionKind.None;
        _combatCooldownRemaining = cooldown;
        _bishopActionApplied = false;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void UpdateBrokenKing(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        float elapsed,
        float distance)
    {
        if (CombatPhase > _activeKingPhase &&
            CombatState != FallenCityCombatState.KingTransition)
        {
            _activeKingPhase = CombatPhase;
            BrokenKingAttack = BrokenKingAttackKind.None;
            _kingChainRemaining = 0;
            _kingActionApplied = false;
            StartAction(
                FallenCityCombatState.KingTransition,
                KingPhaseTransitionSeconds);
            return;
        }

        if (CombatState == FallenCityCombatState.KingGuardBroken)
            return;

        if (CombatState != FallenCityCombatState.Ready)
        {
            UpdateKingAction(player, dungeon, rootHazards, elapsed);
            return;
        }

        if (CombatPhase == 3 && !_lastCommandUsed)
        {
            _lastCommandUsed = true;
            BrokenKingAttack = BrokenKingAttackKind.LastCommand;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.KingLastCommandWarning,
                LastCommandWarningSeconds);
            return;
        }

        float preferred = CombatPhase == 1 ? 70f : 78f;
        if (distance > (CombatPhase >= 2 ? 132f : 96f))
        {
            MoveToward(
                gameTime,
                player.Position,
                preferred,
                dungeon,
                CombatPhase == 3 ? .96f : .82f);
            return;
        }

        FaceToward(player.Position);
        if (_combatCooldownRemaining > 0f)
            return;

        BrokenKingAttackKind kind = SelectKingAttack();
        if (kind == BrokenKingAttackKind.CommandStep)
        {
            BrokenKingAttack = kind;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.KingCommandStep,
                CommandStepSeconds);
            return;
        }
        if (kind == BrokenKingAttackKind.RoyalGuard)
        {
            BrokenKingAttack = kind;
            _kingGuardIntegrity = 100f;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.KingGuard,
                KingGuardSeconds);
            return;
        }
        if (kind == BrokenKingAttackKind.CrownCounter)
        {
            BrokenKingAttack = kind;
            LockAttackDirection(player.Position.X);
            StartAction(
                FallenCityCombatState.KingCounterTell,
                CrownCounterTellSeconds);
            return;
        }

        _kingChainRemaining =
            (CombatPhase == 1 &&
                kind == BrokenKingAttackKind.RoyalSlash) ||
            (CombatPhase == 3 &&
                (kind is BrokenKingAttackKind.RoyalSlash or
                    BrokenKingAttackKind.AshCleave))
                ? 1
                : 0;
        StartKingAttack(kind, player.Position.X);
    }

    private BrokenKingAttackKind SelectKingAttack()
    {
        _kingPatternIndex++;
        if (CombatPhase == 1)
        {
            return (_kingPatternIndex % 5) switch
            {
                1 => BrokenKingAttackKind.RoyalSlash,
                2 => BrokenKingAttackKind.KingsThrust,
                3 => BrokenKingAttackKind.RoyalGuard,
                4 => BrokenKingAttackKind.CrownCounter,
                _ => BrokenKingAttackKind.CommandStep
            };
        }
        if (CombatPhase == 2)
        {
            return (_kingPatternIndex % 5) switch
            {
                1 => BrokenKingAttackKind.AshCleave,
                2 => BrokenKingAttackKind.RoyalCharge,
                3 => BrokenKingAttackKind.CollapseStrike,
                4 => BrokenKingAttackKind.KingsThrust,
                _ => BrokenKingAttackKind.RoyalGuard
            };
        }
        return (_kingPatternIndex % 4) switch
        {
            1 => BrokenKingAttackKind.AshCleave,
            2 => BrokenKingAttackKind.RoyalCharge,
            3 => BrokenKingAttackKind.CollapseStrike,
            _ => BrokenKingAttackKind.RoyalSlash
        };
    }

    private void UpdateKingAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (CombatState == FallenCityCombatState.KingCommandStep)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(direction * EffectiveMovementSpeed * .78f * elapsed, 0f),
                dungeon);
            FaceToward(player.Position);
        }
        else if (CombatState == FallenCityCombatState.KingActive &&
            BrokenKingAttack == BrokenKingAttackKind.RoyalCharge)
        {
            MoveBy(
                new Vector2(
                    _lockedAttackDirection * EffectiveMovementSpeed * 2.35f * elapsed,
                    0f),
                dungeon);
        }
        else if (CombatState == FallenCityCombatState.KingLastCommandCharge)
        {
            MoveBy(
                new Vector2(
                    _lockedAttackDirection * EffectiveMovementSpeed * 2.85f * elapsed,
                    0f),
                dungeon);
        }

        bool damageActive = CombatState is
            FallenCityCombatState.KingActive or
            FallenCityCombatState.KingLastCommandCharge;
        if (damageActive && !_actionHitConsumed && TouchesPlayerDefense(player))
        {
            int damage = BrokenKingAttack switch
            {
                BrokenKingAttackKind.KingsThrust =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.18f),
                BrokenKingAttackKind.CrownCounter =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.30f),
                BrokenKingAttackKind.AshCleave =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.20f),
                BrokenKingAttackKind.RoyalCharge =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.28f),
                BrokenKingAttackKind.CollapseStrike =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.42f),
                BrokenKingAttackKind.LastCommand =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.58f),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .20f);
            _actionHitConsumed = true;
        }

        if (CombatState == FallenCityCombatState.KingActive &&
            !_kingActionApplied)
        {
            SpawnKingImpactHazard(rootHazards, dungeon);
            _kingActionApplied = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case FallenCityCombatState.KingTransition:
                CombatState = FallenCityCombatState.Ready;
                _combatCooldownRemaining = .34f;
                _actionDuration = 0f;
                break;
            case FallenCityCombatState.KingGuard:
                FinishKingAction(.64f);
                break;
            case FallenCityCombatState.KingCounterTell:
                StartAction(
                    FallenCityCombatState.KingRecovery,
                    CrownCounterRecoverySeconds);
                break;
            case FallenCityCombatState.KingCommandStep:
                FinishKingAction(.24f);
                break;
            case FallenCityCombatState.KingWindup:
                _kingActionApplied = false;
                StartAction(
                    FallenCityCombatState.KingActive,
                    KingActiveSecondsFor(BrokenKingAttack));
                break;
            case FallenCityCombatState.KingActive:
                float recovery = KingRecoverySecondsFor(BrokenKingAttack);
                if (CombatPhase == 3 && !_actionHitConsumed)
                    recovery += .24f;
                StartAction(
                    FallenCityCombatState.KingRecovery,
                    recovery);
                break;
            case FallenCityCombatState.KingRecovery:
                if (_kingChainRemaining > 0)
                {
                    _kingChainRemaining--;
                    BrokenKingAttack = BrokenKingAttackKind.RoyalSlash;
                    _kingActionApplied = false;
                    StartAction(
                        FallenCityCombatState.KingWindup,
                        KingSlashWindupSeconds * .88f);
                }
                else
                {
                    FinishKingAction(CombatPhase == 3 ? .22f : .48f);
                }
                break;
            case FallenCityCombatState.KingLastCommandWarning:
                StartAction(
                    FallenCityCombatState.KingLastCommandPause,
                    LastCommandPauseSeconds);
                break;
            case FallenCityCombatState.KingLastCommandPause:
                StartAction(
                    FallenCityCombatState.KingLastCommandCharge,
                    LastCommandChargeSeconds);
                break;
            case FallenCityCombatState.KingLastCommandCharge:
                StartAction(
                    FallenCityCombatState.KingLastCommandRecovery,
                    LastCommandRecoverySeconds);
                break;
            case FallenCityCombatState.KingLastCommandRecovery:
                FinishKingAction(.32f);
                break;
        }
    }

    private void SpawnKingImpactHazard(
        RootHazardManager rootHazards,
        DungeonMap dungeon)
    {
        if (rootHazards == null || BrokenKingAttack is not
            (BrokenKingAttackKind.AshCleave or
             BrokenKingAttackKind.CollapseStrike))
        {
            return;
        }
        bool collapse = BrokenKingAttack == BrokenKingAttackKind.CollapseStrike;
        float direction = AttackFacesLeft ? -1f : 1f;
        Vector2 size = collapse ? new Vector2(94f, 22f) :
            new Vector2(118f, 16f);
        rootHazards.TryAddEmberHazard(
            new Vector2(
                Position.X + direction * (collapse ? 49f : 60f),
                Bounds.Bottom - size.Y / 2f),
            size,
            collapse ? .38f : .20f,
            collapse ? 2.2f : 1.45f,
            collapse ? Math.Max(3, EffectiveAttackDamage / 4) :
                Math.Max(2, EffectiveAttackDamage / 6),
            RoomId,
            this,
            isCinderZone: collapse,
            dungeon: dungeon);
    }

    private void StartKingAttack(BrokenKingAttackKind kind, float targetX)
    {
        BrokenKingAttack = kind;
        _kingActionApplied = false;
        LockAttackDirection(targetX);
        StartAction(
            FallenCityCombatState.KingWindup,
            KingWindupSecondsFor(kind));
    }

    private void FinishKingAction(float cooldown)
    {
        CombatState = FallenCityCombatState.Ready;
        BrokenKingAttack = BrokenKingAttackKind.None;
        _kingActionApplied = false;
        _combatCooldownRemaining = cooldown;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void EnterKingGuardBreak()
    {
        if (Type != EnemyType.BrokenKing ||
            CombatState == FallenCityCombatState.KingGuardBroken || !IsAlive)
        {
            return;
        }
        BrokenKingAttack = BrokenKingAttackKind.None;
        _kingGuardIntegrity = 0f;
        _kingGuardReactionRemaining = 0f;
        StartAction(
            FallenCityCombatState.KingGuardBroken,
            KingGuardBreakSeconds);
    }

    private bool IsKingSourceInFront(Vector2 sourcePosition)
    {
        float direction = AttackFacesLeft ? -1f : 1f;
        return (sourcePosition.X - Position.X) * direction >= -6f;
    }

    private bool HasClearExecutionerPath(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (MathF.Abs(Bounds.Bottom - player.Bounds.Bottom) > 42f)
            return false;

        float from = Position.X;
        float to = player.Position.X;
        float direction = MathF.Sign(to - from);
        if (direction == 0f)
            return true;
        float y = MathF.Min(Bounds.Bottom, player.Bounds.Bottom) - 13f;
        for (float x = from + direction * 18f;
             direction > 0f ? x < to - 12f : x > to + 12f;
             x += direction * 6f)
        {
            if (!SideScrollingCollision.IsPositionFree(
                new Vector2(x, y),
                new Vector2(4f, 8f),
                dungeon))
            {
                return false;
            }
        }
        return true;
    }

    private void StartHoundAttack(RabidHoundAttackKind kind)
    {
        RabidHoundAttack = kind;
        _frenzyStrikesRemaining = kind == RabidHoundAttackKind.FrenzyBite
            ? 2
            : 0;
        StartAction(
            FallenCityCombatState.HoundWindup,
            HoundWindupSecondsFor(kind));
    }

    private void FinishHoundAction()
    {
        CombatState = FallenCityCombatState.Ready;
        RabidHoundAttack = RabidHoundAttackKind.None;
        _frenzyStrikesRemaining = 0;
        _combatCooldownRemaining = .24f;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void FinishPlagueAction()
    {
        CombatState = FallenCityCombatState.Ready;
        PlagueCarrierAttack = PlagueCarrierAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private bool TouchesPlayerDefense(PlayerCharacter player)
    {
        Rectangle area = ActiveAttackArea;
        return area.Intersects(player.BodyHurtbox) ||
            (player.Combat.IsBlocking && area.Intersects(player.DefenseBounds));
    }

    private void StartMilitiaAttack(MilitiaAttackKind kind)
    {
        MilitiaAttack = kind;
        LockAttackDirection(Position.X +
            (Facing == EnemyFacingDirection.Left ? -1f : 1f));
        StartAction(
            FallenCityCombatState.MilitiaWindup,
            MilitiaWindupSecondsFor(kind));
    }

    private void StartAction(FallenCityCombatState state, float duration)
    {
        CombatState = state;
        _actionDuration = duration;
        _actionTimeRemaining = duration;
        _actionHitConsumed = false;
    }

    private void FinishMilitiaAction()
    {
        CombatState = FallenCityCombatState.Ready;
        MilitiaAttack = MilitiaAttackKind.None;
        _combatCooldownRemaining = .40f;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void FinishCrossbowAction()
    {
        CombatState = FallenCityCombatState.Ready;
        CrossbowAttack = CrossbowAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
    }

    private void CancelPolishedAction()
    {
        CombatState = FallenCityCombatState.Ready;
        MilitiaAttack = MilitiaAttackKind.None;
        CrossbowAttack = CrossbowAttackKind.None;
        RabidHoundAttack = RabidHoundAttackKind.None;
        PlagueCarrierAttack = PlagueCarrierAttackKind.None;
        BellWatcherAction = BellWatcherActionKind.None;
        StreetExecutionerAttack = StreetExecutionerAttackKind.None;
        RoyalGuardAttack = RoyalGuardAttackKind.None;
        AshKnightAttack = AshKnightAttackKind.None;
        WarBishopAction = WarBishopActionKind.None;
        BrokenKingAttack = BrokenKingAttackKind.None;
        _pendingAlarmSignal = false;
        _executionerImpactTimeRemaining = 0f;
        _frenzyStrikesRemaining = 0;
        _bishopActionApplied = false;
        _kingActionApplied = false;
        _kingChainRemaining = 0;
        _actionHitConsumed = false;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _combatCooldownRemaining = MathF.Max(
            _combatCooldownRemaining,
            .45f);
    }

    private void LockAttackDirection(float targetX)
    {
        float offset = targetX - Position.X;
        if (MathF.Abs(offset) > 1f)
            _lockedAttackDirection = MathF.Sign(offset);
    }

    private Rectangle CreatePolishedAttackArea()
    {
        bool left = AttackFacesLeft;
        if (Type == EnemyType.RuinedCrossbowman)
        {
            const int reach = 48;
            const int height = 40;
            int y = Bounds.Bottom - height - 5;
            return left
                ? new Rectangle(Bounds.Left - reach + 9, y, reach, height)
                : new Rectangle(Bounds.Right - 9, y, reach, height);
        }

        if (Type == EnemyType.RabidHound)
        {
            int reach = RabidHoundAttack switch
            {
                RabidHoundAttackKind.LungingBite => 54,
                RabidHoundAttackKind.FrenzyBite => 48,
                _ => 43
            };
            const int height = 28;
            int y = Bounds.Bottom - height - 2;
            return left
                ? new Rectangle(Bounds.Left - reach + 10, y, reach, height)
                : new Rectangle(Bounds.Right - 10, y, reach, height);
        }

        if (Type == EnemyType.PlagueCarrier)
        {
            const int reach = 94;
            const int height = 52;
            int y = Bounds.Bottom - height - 4;
            return left
                ? new Rectangle(Bounds.Left - reach + 12, y, reach, height)
                : new Rectangle(Bounds.Right - 12, y, reach, height);
        }

        if (Type == EnemyType.BellWatcher)
        {
            const int reach = 47;
            const int height = 38;
            int y = Bounds.Bottom - height - 7;
            return left
                ? new Rectangle(Bounds.Left - reach + 8, y, reach, height)
                : new Rectangle(Bounds.Right - 8, y, reach, height);
        }

        if (Type == EnemyType.StreetExecutioner)
        {
            int reach = StreetExecutionerAttack switch
            {
                StreetExecutionerAttackKind.ExecutionSlam => 67,
                StreetExecutionerAttackKind.EnragedExecution => 72,
                StreetExecutionerAttackKind.DraggingSweep => 82,
                _ => 98
            };
            int height = StreetExecutionerAttack ==
                StreetExecutionerAttackKind.DraggingSweep ? 27 :
                StreetExecutionerAttack is
                    StreetExecutionerAttackKind.ExecutionSlam or
                    StreetExecutionerAttackKind.EnragedExecution ? 48 : 62;
            int y = StreetExecutionerAttack ==
                StreetExecutionerAttackKind.DraggingSweep
                    ? Bounds.Bottom - 29
                    : Bounds.Bottom - height - 4;
            return left
                ? new Rectangle(Bounds.Left - reach + 13, y, reach, height)
                : new Rectangle(Bounds.Right - 13, y, reach, height);
        }

        if (Type == EnemyType.WarBishop)
        {
            const int reach = 54;
            const int height = 46;
            int y = Bounds.Bottom - height - 5;
            return left
                ? new Rectangle(Bounds.Left - reach + 10, y, reach, height)
                : new Rectangle(Bounds.Right - 10, y, reach, height);
        }

        if (Type == EnemyType.BrokenKing)
        {
            int reach = BrokenKingAttack switch
            {
                BrokenKingAttackKind.KingsThrust => 96,
                BrokenKingAttackKind.AshCleave => 99,
                BrokenKingAttackKind.RoyalCharge => 82,
                BrokenKingAttackKind.CollapseStrike => 79,
                BrokenKingAttackKind.LastCommand => 108,
                BrokenKingAttackKind.CrownCounter => 72,
                _ => 70
            };
            int height = BrokenKingAttack switch
            {
                BrokenKingAttackKind.AshCleave => 48,
                BrokenKingAttackKind.CollapseStrike => 58,
                BrokenKingAttackKind.LastCommand => 64,
                _ => 54
            };
            int y = Bounds.Bottom - height - 4;
            return left
                ? new Rectangle(Bounds.Left - reach + 16, y, reach, height)
                : new Rectangle(Bounds.Right - 16, y, reach, height);
        }

        if (Type == EnemyType.RoyalGuard)
        {
            int reach = RoyalGuardAttack switch
            {
                RoyalGuardAttackKind.RoyalThrust => 82,
                RoyalGuardAttackKind.ShieldBash => 52,
                _ => 68
            };
            int height = RoyalGuardAttack == RoyalGuardAttackKind.ShieldBash
                ? 52
                : 45;
            int y = Bounds.Bottom - height - 5;
            return left
                ? new Rectangle(Bounds.Left - reach + 13, y, reach, height)
                : new Rectangle(Bounds.Right - 13, y, reach, height);
        }

        if (Type == EnemyType.AshKnight)
        {
            int reach = AshKnightAttack switch
            {
                AshKnightAttackKind.AshSweep => 91,
                AshKnightAttackKind.CinderStrike => 73,
                _ => 65
            };
            int height = AshKnightAttack == AshKnightAttackKind.AshSweep
                ? 34
                : 57;
            int y = AshKnightAttack == AshKnightAttackKind.AshSweep
                ? Bounds.Bottom - 36
                : Bounds.Bottom - height - 4;
            return left
                ? new Rectangle(Bounds.Left - reach + 13, y, reach, height)
                : new Rectangle(Bounds.Right - 13, y, reach, height);
        }

        int militiaReach = MilitiaAttack switch
        {
            MilitiaAttackKind.ShortThrust => 67,
            MilitiaAttackKind.DesperateSwing => 63,
            _ => 55
        };
        int militiaHeight = MilitiaAttack == MilitiaAttackKind.ShortThrust
            ? 34
            : 48;
        int militiaY = Bounds.Bottom - militiaHeight - 5;
        return left
            ? new Rectangle(
                Bounds.Left - militiaReach + 8,
                militiaY,
                militiaReach,
                militiaHeight)
            : new Rectangle(
                Bounds.Right - 8,
                militiaY,
                militiaReach,
                militiaHeight);
    }

    private static float MilitiaWindupSecondsFor(
        MilitiaAttackKind kind) => kind switch
        {
            MilitiaAttackKind.ShortThrust => MilitiaThrustWindupSeconds,
            MilitiaAttackKind.DesperateSwing => DesperateSwingWindupSeconds,
            _ => MilitiaSlashWindupSeconds
        };

    private static float MilitiaActiveSecondsFor(
        MilitiaAttackKind kind) => kind switch
        {
            MilitiaAttackKind.ShortThrust => MilitiaThrustActiveSeconds,
            MilitiaAttackKind.DesperateSwing => DesperateSwingActiveSeconds,
            _ => MilitiaSlashActiveSeconds
        };

    private static float MilitiaRecoverySecondsFor(
        MilitiaAttackKind kind) => kind switch
        {
            MilitiaAttackKind.ShortThrust => MilitiaThrustRecoverySeconds,
            MilitiaAttackKind.DesperateSwing => DesperateSwingRecoverySeconds,
            _ => MilitiaSlashRecoverySeconds
        };

    private static float HoundWindupSecondsFor(
        RabidHoundAttackKind kind) => kind switch
        {
            RabidHoundAttackKind.SideSnap => SideSnapWindupSeconds,
            RabidHoundAttackKind.FrenzyBite => FrenzyWindupSeconds,
            _ => LungeWindupSeconds
        };

    private static float HoundActiveSecondsFor(
        RabidHoundAttackKind kind) => kind switch
        {
            RabidHoundAttackKind.SideSnap => SideSnapActiveSeconds,
            RabidHoundAttackKind.FrenzyBite => FrenzyActiveSeconds,
            _ => LungeActiveSeconds
        };

    private static float HoundRecoverySecondsFor(
        RabidHoundAttackKind kind) => kind switch
        {
            RabidHoundAttackKind.SideSnap => SideSnapRecoverySeconds,
            RabidHoundAttackKind.FrenzyBite => FrenzyRecoverySeconds,
            _ => LungeRecoverySeconds
        };

    private static float ExecutionerWindupSecondsFor(
        StreetExecutionerAttackKind kind) => kind switch
        {
            StreetExecutionerAttackKind.ExecutionSlam =>
                ExecutionSlamWindupSeconds,
            StreetExecutionerAttackKind.DraggingSweep =>
                DraggingSweepWindupSeconds,
            StreetExecutionerAttackKind.EnragedExecution =>
                EnragedExecutionWindupSeconds,
            _ => HeavyCleaveWindupSeconds
        };

    private static float ExecutionerActiveSecondsFor(
        StreetExecutionerAttackKind kind) => kind switch
        {
            StreetExecutionerAttackKind.ExecutionSlam or
            StreetExecutionerAttackKind.EnragedExecution =>
                ExecutionSlamActiveSeconds,
            StreetExecutionerAttackKind.DraggingSweep =>
                DraggingSweepActiveSeconds,
            _ => HeavyCleaveActiveSeconds
        };

    private static float ExecutionerRecoverySecondsFor(
        StreetExecutionerAttackKind kind) => kind switch
        {
            StreetExecutionerAttackKind.ExecutionSlam =>
                ExecutionSlamRecoverySeconds,
            StreetExecutionerAttackKind.DraggingSweep =>
                DraggingSweepRecoverySeconds,
            StreetExecutionerAttackKind.EnragedExecution =>
                EnragedExecutionRecoverySeconds,
            _ => HeavyCleaveRecoverySeconds
        };

    private static float RoyalWindupSecondsFor(
        RoyalGuardAttackKind kind) => kind switch
        {
            RoyalGuardAttackKind.ShieldBash => RoyalBashWindupSeconds,
            RoyalGuardAttackKind.GuardedSlash => GuardedSlashWindupSeconds,
            _ => RoyalThrustWindupSeconds
        };

    private static float RoyalActiveSecondsFor(
        RoyalGuardAttackKind kind) => kind switch
        {
            RoyalGuardAttackKind.ShieldBash => RoyalBashActiveSeconds,
            RoyalGuardAttackKind.GuardedSlash => GuardedSlashActiveSeconds,
            _ => RoyalThrustActiveSeconds
        };

    private static float RoyalRecoverySecondsFor(
        RoyalGuardAttackKind kind) => kind switch
        {
            RoyalGuardAttackKind.ShieldBash => RoyalBashRecoverySeconds,
            RoyalGuardAttackKind.GuardedSlash => GuardedSlashRecoverySeconds,
            _ => RoyalThrustRecoverySeconds
        };

    private static float AshWindupSecondsFor(
        AshKnightAttackKind kind) => kind switch
        {
            AshKnightAttackKind.AshSweep => AshSweepWindupSeconds,
            AshKnightAttackKind.CinderStrike => CinderStrikeWindupSeconds,
            _ => EmberSlashWindupSeconds
        };

    private static float AshActiveSecondsFor(
        AshKnightAttackKind kind) => kind switch
        {
            AshKnightAttackKind.AshSweep => AshSweepActiveSeconds,
            AshKnightAttackKind.CinderStrike => CinderStrikeActiveSeconds,
            _ => EmberSlashActiveSeconds
        };

    private static float AshRecoverySecondsFor(
        AshKnightAttackKind kind) => kind switch
        {
            AshKnightAttackKind.AshSweep => AshSweepRecoverySeconds,
            AshKnightAttackKind.CinderStrike => CinderStrikeRecoverySeconds,
            _ => EmberSlashRecoverySeconds
        };

    private static float BishopWindupSecondsFor(
        WarBishopActionKind kind) => kind switch
        {
            WarBishopActionKind.CommandingSeal => CommandingSealWindupSeconds,
            WarBishopActionKind.StaffStrike => StaffStrikeWindupSeconds,
            WarBishopActionKind.FinalBenediction =>
                FinalBenedictionWindupSeconds,
            _ => WarChantWindupSeconds
        };

    private static float BishopActiveSecondsFor(
        WarBishopActionKind kind) => kind switch
        {
            WarBishopActionKind.CommandingSeal => CommandingSealActiveSeconds,
            WarBishopActionKind.StaffStrike => StaffStrikeActiveSeconds,
            WarBishopActionKind.FinalBenediction =>
                FinalBenedictionActiveSeconds,
            _ => WarChantActiveSeconds
        };

    private static float BishopRecoverySecondsFor(
        WarBishopActionKind kind) => kind switch
        {
            WarBishopActionKind.CommandingSeal =>
                CommandingSealRecoverySeconds,
            WarBishopActionKind.StaffStrike => StaffStrikeRecoverySeconds,
            WarBishopActionKind.FinalBenediction =>
                FinalBenedictionRecoverySeconds,
            _ => WarChantRecoverySeconds
        };

    private static float KingWindupSecondsFor(
        BrokenKingAttackKind kind) => kind switch
        {
            BrokenKingAttackKind.KingsThrust => KingsThrustWindupSeconds,
            BrokenKingAttackKind.CrownCounter => CrownCounterTellSeconds,
            BrokenKingAttackKind.AshCleave => AshCleaveWindupSeconds,
            BrokenKingAttackKind.RoyalCharge => RoyalChargeWindupSeconds,
            BrokenKingAttackKind.CollapseStrike =>
                CollapseStrikeWindupSeconds,
            _ => KingSlashWindupSeconds
        };

    private static float KingActiveSecondsFor(
        BrokenKingAttackKind kind) => kind switch
        {
            BrokenKingAttackKind.KingsThrust => KingsThrustActiveSeconds,
            BrokenKingAttackKind.CrownCounter => CrownCounterActiveSeconds,
            BrokenKingAttackKind.AshCleave => AshCleaveActiveSeconds,
            BrokenKingAttackKind.RoyalCharge => RoyalChargeActiveSeconds,
            BrokenKingAttackKind.CollapseStrike =>
                CollapseStrikeActiveSeconds,
            _ => KingSlashActiveSeconds
        };

    private static float KingRecoverySecondsFor(
        BrokenKingAttackKind kind) => kind switch
        {
            BrokenKingAttackKind.KingsThrust => KingsThrustRecoverySeconds,
            BrokenKingAttackKind.CrownCounter => CrownCounterRecoverySeconds,
            BrokenKingAttackKind.AshCleave => AshCleaveRecoverySeconds,
            BrokenKingAttackKind.RoyalCharge => RoyalChargeRecoverySeconds,
            BrokenKingAttackKind.CollapseStrike =>
                CollapseStrikeRecoverySeconds,
            _ => KingSlashRecoverySeconds
        };

    private static Vector2 GetSize(EnemyType type) => type switch
    {
        EnemyType.RabidHound => new Vector2(48f, 32f),
        EnemyType.PlagueCarrier => new Vector2(48f, 60f),
        EnemyType.StreetExecutioner => new Vector2(58f, 72f),
        EnemyType.RoyalGuard => new Vector2(52f, 68f),
        EnemyType.AshKnight => new Vector2(56f, 72f),
        EnemyType.WarBishop => new Vector2(48f, 66f),
        EnemyType.BrokenKing => new Vector2(70f, 90f),
        _ => new Vector2(42f, 56f)
    };

    private static float GetSpeed(EnemyType type) => type switch
    {
        EnemyType.RabidHound => 168f,
        EnemyType.PlagueCarrier => 58f,
        EnemyType.BellWatcher => 126f,
        EnemyType.StreetExecutioner => 72f,
        EnemyType.RoyalGuard => 82f,
        EnemyType.AshKnight => 98f,
        EnemyType.BrokenKing => 106f,
        _ => 96f
    };

    private static float GetDetection(EnemyType type) =>
        type is EnemyType.RuinedCrossbowman or EnemyType.BellWatcher or
            EnemyType.WarBishop ? 460f : type == EnemyType.BrokenKing ? 560f : 350f;

    private static int GetBaseHealth(EnemyType type) => type switch
    {
        EnemyType.FallenMilitiaman => 115,
        EnemyType.RuinedCrossbowman => 92,
        EnemyType.RabidHound => 78,
        EnemyType.PlagueCarrier => 155,
        EnemyType.BellWatcher => 82,
        EnemyType.StreetExecutioner => 290,
        EnemyType.RoyalGuard => 360,
        EnemyType.AshKnight => 420,
        EnemyType.WarBishop => 340,
        EnemyType.BrokenKing => 1850,
        _ => 100
    };

    private static int GetBaseDamage(EnemyType type) => type switch
    {
        EnemyType.RabidHound => 13,
        EnemyType.PlagueCarrier => 18,
        EnemyType.StreetExecutioner => 29,
        EnemyType.RoyalGuard => 25,
        EnemyType.AshKnight => 30,
        EnemyType.WarBishop => 22,
        EnemyType.BrokenKing => 36,
        _ => 16
    };

    private static int GetExperience(EnemyType type) => type switch
    {
        EnemyType.RoyalGuard or EnemyType.AshKnight or EnemyType.WarBishop => 260,
        EnemyType.BrokenKing => 1800,
        EnemyType.StreetExecutioner => 190,
        _ => 95
    };

    private static float GetRange(EnemyType type) => type switch
    {
        EnemyType.RuinedCrossbowman => 430f,
        EnemyType.WarBishop => 390f,
        EnemyType.StreetExecutioner => 78f,
        EnemyType.BrokenKing => 94f,
        _ => 60f
    };

    private static float GetCooldown(EnemyType type) => type switch
    {
        EnemyType.RabidHound => .82f,
        EnemyType.RuinedCrossbowman => 1.55f,
        EnemyType.StreetExecutioner => 1.7f,
        EnemyType.BrokenKing => 1.05f,
        _ => 1.2f
    };

    private static int Scale(int value, int level, int worldTier, bool health)
    {
        int perLevel = health ? Math.Max(8, value / 7) : 2;
        int leveled = value + (Math.Max(1, level) - 1) * perLevel;
        int percent = health
            ? WorldProgression.GetHealthMultiplierPercent(worldTier)
            : WorldProgression.GetDamageMultiplierPercent(worldTier);
        return WorldProgression.ApplyPercent(leveled, percent);
    }
}
