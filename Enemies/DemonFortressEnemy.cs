using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum DemonFortressCombatState
{
    Ready,
    LegionGuard,
    LegionSlashWindup,
    LegionSlashActive,
    LegionSlashRecovery,
    LegionFollowupWindup,
    LegionFollowupActive,
    LegionFollowupRecovery,
    LegionCounterWindup,
    LegionCounterActive,
    LegionCounterRecovery,
    ArcherAim,
    ArcherDraw,
    ArcherHold,
    ArcherRelease,
    ArcherRecovery,
    ArcherReposition,
    HornRetreat,
    HornReach,
    HornRaise,
    HornInhale,
    HornHold,
    HornBlast,
    HornRecovery,
    HornInterrupted,
    HornJabWindup,
    HornJabActive,
    HornJabRecovery,
    ShieldGuard,
    ShieldPushWindup,
    ShieldPushActive,
    ShieldPushRecovery,
    ShieldStrikeWindup,
    ShieldStrikeActive,
    ShieldStrikeRecovery,
    PikeReposition,
    PikeSettle,
    PikeThrustExtend,
    PikeThrustHold,
    PikeRetract,
    PikeRecovery,
    PikeAdvanceSettle,
    PikeAdvanceExtend,
    PikeAdvanceHold,
    PikeAdvanceRetract,
    PikeAdvanceRecovery,
    HoundTurn,
    HoundFlank,
    HoundLungeWindup,
    HoundLungeActive,
    HoundLanding,
    HoundLungeRecovery,
    HoundBiteWindup,
    HoundBiteActive,
    HoundBiteRecovery,
    SmithSlamRaise,
    SmithSlamFall,
    SmithSlamImpact,
    SmithSlamRecovery,
    SmithSwingWindup,
    SmithSwingActive,
    SmithSwingRecovery,
    OfficerCommandRaise,
    OfficerCommandActive,
    OfficerCommandRecovery,
    OfficerSlashWindup,
    OfficerSlashActive,
    OfficerSlashRecovery,
    OfficerAdvanceWindup,
    OfficerAdvanceActive,
    OfficerAdvanceRecovery,
    BallistaSetup,
    BallistaAcquire,
    BallistaAim,
    BallistaWarning,
    BallistaFire,
    BallistaRecoil,
    BallistaReloadReach,
    BallistaReloadCrank,
    BallistaReloadLoad,
    BallistaReloadTension,
    BallistaReady,
    BallistaShoveWindup,
    BallistaShoveActive,
    BallistaShoveRecovery,
    ExecutionerIntro,
    ExecutionerPhaseTransition,
    ExecutionerCleaveWindup,
    ExecutionerCleaveActive,
    ExecutionerCleaveRecovery,
    ExecutionerArmorBreakWindup,
    ExecutionerArmorBreakActive,
    ExecutionerArmorBreakRecovery,
    ExecutionerCounterEnter,
    ExecutionerCounterWindow,
    ExecutionerCounterRetaliateWindup,
    ExecutionerCounterRetaliateActive,
    ExecutionerCounterRecovery,
    ExecutionerJudgmentRaise,
    ExecutionerJudgmentHold,
    ExecutionerJudgmentFall,
    ExecutionerJudgmentImpact,
    ExecutionerJudgmentRecovery,
    ExecutionerReverseTell,
    ExecutionerReverseActive,
    ExecutionerReverseRecovery
}

/// <summary>
/// Map 5 combat family. Enemies 41-48 own their military combat loops, while
/// 49-50 own isolated boss-tier siege and duel state machines.
/// </summary>
public sealed class DemonFortressEnemy : Enemy
{
    public const float LegionSlashWindupSeconds = .30f;
    public const float LegionSlashActiveSeconds = .11f;
    public const float LegionSlashRecoverySeconds = .34f;
    public const float LegionFollowupWindupSeconds = .22f;
    public const float LegionFollowupActiveSeconds = .10f;
    public const float LegionFollowupRecoverySeconds = .40f;
    public const float LegionGuardMaximumSeconds = .72f;
    public const float LegionCounterWindupSeconds = .20f;
    public const float LegionCounterActiveSeconds = .10f;
    public const float LegionCounterRecoverySeconds = .38f;
    public const float ArcherAimSeconds = .22f;
    public const float ArcherDrawSeconds = .46f;
    public const float ArcherHoldSeconds = .13f;
    public const float ArcherReleaseSeconds = .08f;
    public const float ArcherRecoverySeconds = .52f;
    public const float ArcherRepositionSeconds = .46f;
    public const float HornReachSeconds = .25f;
    public const float HornRaiseSeconds = .35f;
    public const float HornInhaleSeconds = .32f;
    public const float HornHoldSeconds = .65f;
    public const float HornBlastSeconds = .14f;
    public const float HornRecoverySeconds = .58f;
    public const float HornInterruptedSeconds = .52f;
    public const float HornRetreatSeconds = .48f;
    public const float HornJabWindupSeconds = .28f;
    public const float HornJabActiveSeconds = .10f;
    public const float HornJabRecoverySeconds = .44f;
    public const float ShieldGuardSeconds = .88f;
    public const float ShieldPushWindupSeconds = .38f;
    public const float ShieldPushActiveSeconds = .13f;
    public const float ShieldPushRecoverySeconds = .58f;
    public const float ShieldStrikeWindupSeconds = .32f;
    public const float ShieldStrikeActiveSeconds = .11f;
    public const float ShieldStrikeRecoverySeconds = .46f;
    public const float PikeSettleSeconds = .30f;
    public const float PikeExtendSeconds = .18f;
    public const float PikeHoldSeconds = .10f;
    public const float PikeRetractSeconds = .20f;
    public const float PikeRecoverySeconds = .52f;
    public const float PikeRepositionSeconds = .42f;
    public const float HoundTurnSeconds = .18f;
    public const float HoundFlankSeconds = .34f;
    public const float HoundLungeWindupSeconds = .34f;
    public const float HoundLungeActiveSeconds = .30f;
    public const float HoundLandingSeconds = .18f;
    public const float HoundLungeRecoverySeconds = .62f;
    public const float HoundBiteWindupSeconds = .22f;
    public const float HoundBiteActiveSeconds = .09f;
    public const float HoundBiteRecoverySeconds = .40f;
    public const float SmithSlamRaiseSeconds = .72f;
    public const float SmithSlamFallSeconds = .18f;
    public const float SmithSlamImpactSeconds = .16f;
    public const float SmithSlamRecoverySeconds = .78f;
    public const float SmithSwingWindupSeconds = .48f;
    public const float SmithSwingActiveSeconds = .16f;
    public const float SmithSwingRecoverySeconds = .66f;
    public const float OfficerCommandRaiseSeconds = .45f;
    public const float OfficerCommandActiveSeconds = .18f;
    public const float OfficerCommandRecoverySeconds = .42f;
    public const float OfficerSlashWindupSeconds = .24f;
    public const float OfficerSlashActiveSeconds = .11f;
    public const float OfficerSlashRecoverySeconds = .32f;
    public const float OfficerAdvanceWindupSeconds = .30f;
    public const float OfficerAdvanceActiveSeconds = .16f;
    public const float OfficerAdvanceRecoverySeconds = .42f;
    public const float BallistaWarningSeconds = .58f;
    public const float BallistaReloadSeconds = 2.08f;
    public const float ExecutionerCounterWindowSeconds = .62f;
    public const float ExecutionerJudgmentRaiseSeconds = .82f;

    private float _combatCooldownRemaining;
    private float _guardCooldownRemaining;
    private float _repositionCooldownRemaining;
    private float _alarmCooldownRemaining;
    private float _shieldPushCooldownRemaining;
    private float _houndLungeCooldownRemaining;
    private float _houndBiteCooldownRemaining;
    private float _officerCommandCooldownRemaining;
    private float _executionerCounterCooldownRemaining;
    private float _decisionCooldownRemaining;
    private float _actionTimeRemaining;
    private float _actionDuration;
    private float _guardIntegrity = 72f;
    private float _guardBlockReactionRemaining;
    private float _repositionDistanceRemaining;
    private float _lockedAttackDirection = 1f;
    private int _decisionSerial;
    private bool _counterPending;
    private bool _actionHitConsumed;
    private bool _shotReleased;
    private bool _alarmCompletedThisAction;
    private bool _officerCommandApplied;
    private bool _officerDeathRaised;
    private bool _bossTierDeathRaised;
    private bool _bossIntroCompleted;
    private bool _executionerPhaseTransitioned;
    private float _pendingAttackDirection = 1f;
    private Vector2 _lockedArrowTarget;
    private ProjectileManager _activeProjectiles;
    private readonly List<Enemy> _commandedAllies = new(8);

    public DemonFortressCombatState CombatState { get; private set; } =
        DemonFortressCombatState.Ready;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedAttackDirection < 0f;
    public bool IsLegionGuarding => Type == EnemyType.DemonLegionnaireFortress &&
        CombatState == DemonFortressCombatState.LegionGuard;
    public bool IsLegionBlockReacting => _guardBlockReactionRemaining > 0f;
    public bool IsHornAlarmWindup => Type == EnemyType.HornGuard &&
        CombatState is DemonFortressCombatState.HornReach or
            DemonFortressCombatState.HornRaise or
            DemonFortressCombatState.HornInhale or
            DemonFortressCombatState.HornHold;
    public bool IsHornAlarmCompleted => Type == EnemyType.HornGuard &&
        CombatState == DemonFortressCombatState.HornBlast;
    public bool IsHornAlarmInterrupted => Type == EnemyType.HornGuard &&
        CombatState == DemonFortressCombatState.HornInterrupted;
    public int AlarmCompletionCount { get; private set; }
    public bool IsShieldbearerGuarding =>
        Type == EnemyType.DemonShieldbearer &&
        CombatState == DemonFortressCombatState.ShieldGuard;
    public bool IsShieldbearerBlockReacting =>
        Type == EnemyType.DemonShieldbearer &&
        _guardBlockReactionRemaining > 0f;
    public float PikeExtensionRatio => CombatState switch
    {
        DemonFortressCombatState.PikeThrustExtend or
            DemonFortressCombatState.PikeAdvanceExtend => ActionProgress,
        DemonFortressCombatState.PikeThrustHold or
            DemonFortressCombatState.PikeAdvanceHold => 1f,
        DemonFortressCombatState.PikeRetract or
            DemonFortressCombatState.PikeAdvanceRetract => 1f - ActionProgress,
        _ => 0f
    };
    public bool IsHoundCommitted => Type == EnemyType.WarHound &&
        CombatState is DemonFortressCombatState.HoundLungeWindup or
            DemonFortressCombatState.HoundLungeActive or
            DemonFortressCombatState.HoundLanding or
            DemonFortressCombatState.HoundLungeRecovery;
    public bool IsSmithGroundImpact => Type == EnemyType.InfernalSmith &&
        CombatState == DemonFortressCombatState.SmithSlamImpact;
    public bool IsOfficerCommanding => Type == EnemyType.DemonOfficer &&
        CombatState is DemonFortressCombatState.OfficerCommandRaise or
            DemonFortressCombatState.OfficerCommandActive or
            DemonFortressCombatState.OfficerCommandRecovery;
    public int OfficerCommandCount { get; private set; }
    public int OfficerDeathNotificationCount { get; private set; }
    public int BossTierCompletionCount { get; private set; }
    public int BallistaWarningCount { get; private set; }
    public int BallistaShotCount { get; private set; }
    public int BallistaCycleCount { get; private set; }
    public int ExecutionerCounterTriggerCount { get; private set; }
    public int ExecutionerPhaseTransitionCount { get; private set; }
    public int BossCombatPhase => Type switch
    {
        EnemyType.BallistaGunner => CurrentHealth * 5 < MaxHealth * 3 ? 2 : 1,
        EnemyType.BlackGuardExecutioner => CurrentHealth * 5 < MaxHealth * 3 ? 2 : 1,
        _ => 0
    };
    public bool IsBallistaLoaded => Type == EnemyType.BallistaGunner &&
        CombatState is not (DemonFortressCombatState.BallistaFire or
            DemonFortressCombatState.BallistaRecoil or
            DemonFortressCombatState.BallistaReloadReach or
            DemonFortressCombatState.BallistaReloadCrank or
            DemonFortressCombatState.BallistaReloadLoad);
    public bool IsBallistaWarning => Type == EnemyType.BallistaGunner &&
        CombatState == DemonFortressCombatState.BallistaWarning;
    public float BallistaAimVerticalOffset => Type == EnemyType.BallistaGunner
        ? Math.Clamp(
            (_lockedArrowTarget.Y - (Position.Y - Size.Y * .27f)) * .12f,
            -12f,
            12f)
        : 0f;
    public bool IsExecutionerCountering => Type == EnemyType.BlackGuardExecutioner &&
        CombatState is DemonFortressCombatState.ExecutionerCounterEnter or
            DemonFortressCombatState.ExecutionerCounterWindow;
    public int ActiveCommandedAllyCount
    {
        get
        {
            int count = 0;
            foreach (Enemy ally in _commandedAllies)
                if (ally.IsAlive && ally.IsBuffed)
                    count++;
            return count;
        }
    }
    public event Action<DemonFortressEnemy> AlarmCompleted;
    public event Action<DemonFortressEnemy> OfficerDefeated;
    public event Action<DemonFortressEnemy> BossTierDefeated;
    public bool IsBossTierEncounter => Type is
        EnemyType.BallistaGunner or EnemyType.BlackGuardExecutioner;

    public override bool CanDropLoot => !IsBossTierEncounter;
    protected override float DeathPresentationDurationSeconds =>
        IsBossTierEncounter ? 1.05f : base.DeathPresentationDurationSeconds;
    public override bool UsesProjectileAttack => Type is
        EnemyType.FortressArcher or EnemyType.BallistaGunner;
    public override bool IsCombatAttackTelegraphing => CombatState is
        DemonFortressCombatState.LegionSlashWindup or
        DemonFortressCombatState.LegionFollowupWindup or
        DemonFortressCombatState.LegionCounterWindup or
        DemonFortressCombatState.ArcherAim or
        DemonFortressCombatState.ArcherDraw or
        DemonFortressCombatState.ArcherHold or
        DemonFortressCombatState.HornReach or
        DemonFortressCombatState.HornRaise or
        DemonFortressCombatState.HornInhale or
        DemonFortressCombatState.HornHold or
        DemonFortressCombatState.HornJabWindup or
        DemonFortressCombatState.ShieldPushWindup or
        DemonFortressCombatState.ShieldStrikeWindup or
        DemonFortressCombatState.PikeSettle or
        DemonFortressCombatState.PikeAdvanceSettle or
        DemonFortressCombatState.HoundLungeWindup or
        DemonFortressCombatState.HoundBiteWindup or
        DemonFortressCombatState.SmithSlamRaise or
        DemonFortressCombatState.SmithSwingWindup or
        DemonFortressCombatState.OfficerCommandRaise or
        DemonFortressCombatState.OfficerSlashWindup or
        DemonFortressCombatState.OfficerAdvanceWindup or
        DemonFortressCombatState.BallistaAcquire or
        DemonFortressCombatState.BallistaAim or
        DemonFortressCombatState.BallistaWarning or
        DemonFortressCombatState.BallistaShoveWindup or
        DemonFortressCombatState.ExecutionerCleaveWindup or
        DemonFortressCombatState.ExecutionerArmorBreakWindup or
        DemonFortressCombatState.ExecutionerCounterEnter or
        DemonFortressCombatState.ExecutionerCounterRetaliateWindup or
        DemonFortressCombatState.ExecutionerJudgmentRaise or
        DemonFortressCombatState.ExecutionerJudgmentHold or
        DemonFortressCombatState.ExecutionerReverseTell;
    public override bool IsCombatAttackActive => CombatState is
        DemonFortressCombatState.LegionSlashActive or
        DemonFortressCombatState.LegionFollowupActive or
        DemonFortressCombatState.LegionCounterActive or
        DemonFortressCombatState.ArcherRelease or
        DemonFortressCombatState.HornJabActive or
        DemonFortressCombatState.ShieldPushActive or
        DemonFortressCombatState.ShieldStrikeActive or
        DemonFortressCombatState.PikeThrustExtend or
        DemonFortressCombatState.PikeThrustHold or
        DemonFortressCombatState.PikeAdvanceExtend or
        DemonFortressCombatState.PikeAdvanceHold or
        DemonFortressCombatState.HoundLungeActive or
        DemonFortressCombatState.HoundBiteActive or
        DemonFortressCombatState.SmithSlamFall or
        DemonFortressCombatState.SmithSlamImpact or
        DemonFortressCombatState.SmithSwingActive or
        DemonFortressCombatState.OfficerSlashActive or
        DemonFortressCombatState.OfficerAdvanceActive or
        DemonFortressCombatState.BallistaFire or
        DemonFortressCombatState.BallistaShoveActive or
        DemonFortressCombatState.ExecutionerCleaveActive or
        DemonFortressCombatState.ExecutionerArmorBreakActive or
        DemonFortressCombatState.ExecutionerCounterRetaliateActive or
        DemonFortressCombatState.ExecutionerJudgmentFall or
        DemonFortressCombatState.ExecutionerJudgmentImpact or
        DemonFortressCombatState.ExecutionerReverseActive;
    public override Rectangle ActiveAttackArea =>
        Type switch
        {
            EnemyType.DemonLegionnaireFortress => CreateLegionAttackArea(),
            EnemyType.HornGuard => CreateHornAttackArea(),
            EnemyType.DemonShieldbearer => CreateShieldbearerAttackArea(),
            EnemyType.InfernalPikeman => CreatePikeAttackArea(),
            EnemyType.WarHound => CreateHoundAttackArea(),
            EnemyType.InfernalSmith => CreateSmithAttackArea(),
            EnemyType.DemonOfficer => CreateOfficerAttackArea(),
            EnemyType.BallistaGunner => CreateBallistaAttackArea(),
            EnemyType.BlackGuardExecutioner => CreateExecutionerAttackArea(),
            _ => base.ActiveAttackArea
        };

    public DemonFortressEnemy(
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
            GetDetectionRange(type),
            GetHealth(type, level, worldTier),
            GetDamage(type, level, worldTier),
            GetExperience(type),
            level,
            worldTier,
            isElite,
            roomId,
            GetAttackRange(type),
            GetAttackCooldown(type))
    {
        if (!IsFortressType(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        _combatCooldownRemaining = .28f;
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        if (Type == EnemyType.BlackGuardExecutioner &&
            CombatState == DemonFortressCombatState.ExecutionerCounterWindow &&
            damage > 0 && IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(
                Math.Max(1, (int)MathF.Ceiling(damage * .35f)),
                poiseDamage * .25f,
                sourcePosition);
            if (IsAlive && !IsStaggered)
            {
                LockAttackDirection(sourcePosition);
                ExecutionerCounterTriggerCount++;
                _executionerCounterCooldownRemaining = 4.8f;
                StartAction(
                    DemonFortressCombatState.ExecutionerCounterRetaliateWindup,
                    .28f);
            }
            return;
        }

        bool directionalGuard = IsLegionGuarding || IsShieldbearerGuarding;
        if (!directionalGuard || damage <= 0 || !IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        bool shieldbearer = IsShieldbearerGuarding;
        bool guardBreaker = poiseDamage >= (shieldbearer ? 58f : 45f) ||
            damage >= (shieldbearer ? 56 : 42);
        int guardedDamage = Math.Max(
            1,
            (int)MathF.Ceiling(damage * (guardBreaker
                ? (shieldbearer ? .70f : .82f)
                : (shieldbearer ? .28f : .45f))));
        float guardedPoise = guardBreaker
            ? poiseDamage
            : poiseDamage * (shieldbearer ? .40f : .58f);
        base.ReceiveDamage(guardedDamage, guardedPoise);
        _guardBlockReactionRemaining = .20f;
        _guardIntegrity -= poiseDamage + damage * (shieldbearer ? .28f : .38f);

        if (!IsAlive)
            return;
        if (guardBreaker || _guardIntegrity <= 0f || IsStaggered)
        {
            CancelFortressAction(.62f);
            _guardCooldownRemaining = shieldbearer ? 2.8f : 3.1f;
            return;
        }

        if (!shieldbearer)
            _counterPending = (_decisionSerial & 1) == 0;
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
        {
            if (IsBossTierEncounter && !_bossTierDeathRaised)
            {
                _bossTierDeathRaised = true;
                _activeProjectiles?.ClearEnemyProjectilesInRoom(RoomId);
                CancelFortressAction(0f);
                BossTierCompletionCount++;
                BossTierDefeated?.Invoke(this);
                return;
            }
            if (Type == EnemyType.DemonOfficer && !_officerDeathRaised)
            {
                _officerDeathRaised = true;
                ClearOfficerCommandBuffs();
                CancelFortressAction(0f);
                OfficerDeathNotificationCount++;
                OfficerDefeated?.Invoke(this);
                return;
            }
            CancelFortressAction(0f);
            return;
        }
        if (IsHornAlarmWindup)
            InterruptHornAction();
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _combatCooldownRemaining = MathF.Max(0f, _combatCooldownRemaining - elapsed);
        _guardCooldownRemaining = MathF.Max(0f, _guardCooldownRemaining - elapsed);
        _repositionCooldownRemaining = MathF.Max(0f, _repositionCooldownRemaining - elapsed);
        _alarmCooldownRemaining = MathF.Max(0f, _alarmCooldownRemaining - elapsed);
        _shieldPushCooldownRemaining = MathF.Max(
            0f, _shieldPushCooldownRemaining - elapsed);
        _houndLungeCooldownRemaining = MathF.Max(
            0f, _houndLungeCooldownRemaining - elapsed);
        _houndBiteCooldownRemaining = MathF.Max(
            0f, _houndBiteCooldownRemaining - elapsed);
        _officerCommandCooldownRemaining = MathF.Max(
            0f, _officerCommandCooldownRemaining - elapsed);
        _executionerCounterCooldownRemaining = MathF.Max(
            0f, _executionerCounterCooldownRemaining - elapsed);
        _decisionCooldownRemaining = MathF.Max(0f, _decisionCooldownRemaining - elapsed);
        _guardBlockReactionRemaining = MathF.Max(
            0f, _guardBlockReactionRemaining - elapsed);

        if (Type == EnemyType.DemonLegionnaireFortress)
        {
            UpdateLegionnaire(gameTime, player, dungeon, elapsed);
            return;
        }
        if (Type == EnemyType.FortressArcher)
        {
            UpdateFortressArcher(gameTime, player, dungeon, projectiles, elapsed);
            return;
        }
        if (Type == EnemyType.HornGuard)
        {
            UpdateHornGuard(gameTime, player, dungeon, enemies, elapsed);
            return;
        }
        if (Type == EnemyType.DemonShieldbearer)
        {
            UpdateShieldbearer(gameTime, player, dungeon, elapsed);
            return;
        }
        if (Type == EnemyType.InfernalPikeman)
        {
            UpdatePikeman(gameTime, player, dungeon, elapsed);
            return;
        }
        if (Type == EnemyType.WarHound)
        {
            UpdateWarHound(gameTime, player, dungeon, elapsed);
            return;
        }
        if (Type == EnemyType.InfernalSmith)
        {
            UpdateInfernalSmith(gameTime, player, dungeon, elapsed);
            return;
        }
        if (Type == EnemyType.DemonOfficer)
        {
            UpdateDemonOfficer(gameTime, player, dungeon, enemies, elapsed);
            return;
        }
        if (Type == EnemyType.BallistaGunner)
        {
            _activeProjectiles = projectiles;
            UpdateBallistaGunner(player, dungeon, projectiles, elapsed);
            return;
        }
        if (Type == EnemyType.BlackGuardExecutioner)
        {
            UpdateBlackGuardExecutioner(gameTime, player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance <= Attack.Range)
        {
            TryMeleeAttack(player);
            return;
        }
        float aggression = Type switch
        {
            EnemyType.WarHound => 1.22f,
            EnemyType.DemonShieldbearer => .62f,
            EnemyType.InfernalSmith => .68f,
            EnemyType.BlackGuardExecutioner => .72f,
            _ => .86f
        };
        MoveToward(gameTime, player.Position, Attack.Range, dungeon, aggression);
    }

    private void UpdateLegionnaire(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateLegionnaireAction(player, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_combatCooldownRemaining <= 0f && distance <= 82f)
        {
            LockAttackDirection(player.Position);
            StartAction(
                DemonFortressCombatState.LegionSlashWindup,
                LegionSlashWindupSeconds);
            return;
        }
        if (_decisionCooldownRemaining <= 0f)
        {
            _decisionCooldownRemaining = .34f;
            _decisionSerial++;
            if (_guardCooldownRemaining <= 0f && distance is >= 92f and <= 205f &&
                _decisionSerial % 3 == 0)
            {
                _guardIntegrity = 72f;
                _counterPending = false;
                LockAttackDirection(player.Position);
                StartAction(
                    DemonFortressCombatState.LegionGuard,
                    LegionGuardMaximumSeconds);
                return;
            }
        }
        MoveToward(gameTime, player.Position, 70f, dungeon, .82f);
    }

    private void UpdateLegionnaireAction(PlayerCharacter player, float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.LegionGuard)
        {
            if (_counterPending && _guardBlockReactionRemaining <= .12f)
            {
                _counterPending = false;
                LockAttackDirection(player.Position);
                StartAction(
                    DemonFortressCombatState.LegionCounterWindup,
                    LegionCounterWindupSeconds);
                return;
            }
            if (_actionTimeRemaining <= 0f)
            {
                CombatState = DemonFortressCombatState.Ready;
                _guardCooldownRemaining = 2.25f;
                _combatCooldownRemaining = .38f;
                _actionDuration = 0f;
            }
            return;
        }

        if (CombatState is DemonFortressCombatState.LegionSlashActive or
            DemonFortressCombatState.LegionFollowupActive or
            DemonFortressCombatState.LegionCounterActive)
            TryResolveLegionContact(player);
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.LegionSlashWindup:
                StartAction(DemonFortressCombatState.LegionSlashActive,
                    LegionSlashActiveSeconds);
                break;
            case DemonFortressCombatState.LegionSlashActive:
                if ((_decisionSerial & 1) == 0 &&
                    MathF.Abs(player.Position.X - Position.X) <= 108f)
                    StartAction(DemonFortressCombatState.LegionFollowupWindup,
                        LegionFollowupWindupSeconds);
                else
                    StartAction(DemonFortressCombatState.LegionSlashRecovery,
                        LegionSlashRecoverySeconds);
                break;
            case DemonFortressCombatState.LegionFollowupWindup:
                StartAction(DemonFortressCombatState.LegionFollowupActive,
                    LegionFollowupActiveSeconds);
                break;
            case DemonFortressCombatState.LegionFollowupActive:
                StartAction(DemonFortressCombatState.LegionFollowupRecovery,
                    LegionFollowupRecoverySeconds);
                break;
            case DemonFortressCombatState.LegionCounterWindup:
                StartAction(DemonFortressCombatState.LegionCounterActive,
                    LegionCounterActiveSeconds);
                break;
            case DemonFortressCombatState.LegionCounterActive:
                StartAction(DemonFortressCombatState.LegionCounterRecovery,
                    LegionCounterRecoverySeconds);
                break;
            case DemonFortressCombatState.LegionSlashRecovery:
            case DemonFortressCombatState.LegionFollowupRecovery:
            case DemonFortressCombatState.LegionCounterRecovery:
                CombatState = DemonFortressCombatState.Ready;
                _combatCooldownRemaining = .62f;
                _actionDuration = 0f;
                break;
        }
    }

    private void UpdateFortressArcher(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateArcherAction(gameTime, player, dungeon, projectiles, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (distance < 168f && _repositionCooldownRemaining <= 0f)
        {
            _lockedAttackDirection = Position.X < player.Position.X ? -1f : 1f;
            _repositionDistanceRemaining = 105f;
            StartAction(DemonFortressCombatState.ArcherReposition,
                ArcherRepositionSeconds);
            _repositionCooldownRemaining = 2.2f;
            return;
        }
        if (distance is >= 175f and <= 690f &&
            _combatCooldownRemaining <= 0f && HasClearShot(player.Position, dungeon))
        {
            StartAction(DemonFortressCombatState.ArcherAim, ArcherAimSeconds);
            return;
        }
        if (distance > 520f)
            MoveToward(gameTime, player.Position, 430f, dungeon, .38f);
    }

    private void UpdateArcherAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.ArcherReposition)
        {
            float before = Position.X;
            Vector2 threat = Position + new Vector2(-_lockedAttackDirection * 12f, 0f);
            MoveAway(gameTime, threat, dungeon, .72f);
            _repositionDistanceRemaining = MathF.Max(
                0f, _repositionDistanceRemaining - MathF.Abs(Position.X - before));
            if (_actionTimeRemaining <= 0f || _repositionDistanceRemaining <= 0f)
            {
                CombatState = DemonFortressCombatState.Ready;
                _combatCooldownRemaining = .32f;
                _actionDuration = 0f;
            }
            return;
        }

        if (CombatState == DemonFortressCombatState.ArcherAim)
            FaceToward(player.Position);
        if ((CombatState is DemonFortressCombatState.ArcherDraw or
            DemonFortressCombatState.ArcherHold) &&
            !HasClearShot(_lockedArrowTarget, dungeon))
        {
            StartAction(DemonFortressCombatState.ArcherRecovery,
                ArcherRecoverySeconds * .75f);
            return;
        }
        if (CombatState == DemonFortressCombatState.ArcherRelease && !_shotReleased)
        {
            _shotReleased = true;
            Vector2 origin = GetBowOrigin();
            projectiles.SpawnEnemyArrow(
                origin, _lockedArrowTarget - origin, EffectiveAttackDamage,
                RoomId, heavy: false);
        }
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.ArcherAim:
                LockAttackDirection(player.Position);
                _lockedArrowTarget = player.Position;
                StartAction(DemonFortressCombatState.ArcherDraw, ArcherDrawSeconds);
                break;
            case DemonFortressCombatState.ArcherDraw:
                StartAction(DemonFortressCombatState.ArcherHold, ArcherHoldSeconds);
                break;
            case DemonFortressCombatState.ArcherHold:
                StartAction(DemonFortressCombatState.ArcherRelease, ArcherReleaseSeconds);
                break;
            case DemonFortressCombatState.ArcherRelease:
                StartAction(DemonFortressCombatState.ArcherRecovery, ArcherRecoverySeconds);
                break;
            case DemonFortressCombatState.ArcherRecovery:
                CombatState = DemonFortressCombatState.Ready;
                _combatCooldownRemaining = .72f;
                _actionDuration = 0f;
                break;
        }
    }

    private void UpdateHornGuard(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateHornGuardAction(gameTime, player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        bool screened = HasFrontlineScreen(player.Position, enemies);
        if (distance < 145f && _repositionCooldownRemaining <= 0f)
        {
            _lockedAttackDirection = Position.X < player.Position.X ? -1f : 1f;
            _repositionDistanceRemaining = 92f;
            StartAction(DemonFortressCombatState.HornRetreat, HornRetreatSeconds);
            _repositionCooldownRemaining = 2.8f;
            return;
        }
        if (distance <= 78f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.HornJabWindup,
                HornJabWindupSeconds);
            return;
        }
        if (_alarmCooldownRemaining <= 0f && distance is >= 150f and <= 500f &&
            (screened || distance >= 245f) && HasClearSupportLine(player.Position, dungeon))
        {
            LockAttackDirection(player.Position);
            _alarmCompletedThisAction = false;
            StartAction(DemonFortressCombatState.HornReach, HornReachSeconds);
            return;
        }
        if (distance > 430f)
            MoveToward(gameTime, player.Position, 390f, dungeon, .38f);
    }

    private void UpdateHornGuardAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.HornRetreat)
        {
            float before = Position.X;
            Vector2 threat = Position + new Vector2(-_lockedAttackDirection * 12f, 0f);
            MoveAway(gameTime, threat, dungeon, .78f);
            _repositionDistanceRemaining = MathF.Max(
                0f, _repositionDistanceRemaining - MathF.Abs(Position.X - before));
            if (_actionTimeRemaining <= 0f || _repositionDistanceRemaining <= 0f)
                FinishFortressAction(.34f);
            return;
        }

        if (CombatState is DemonFortressCombatState.HornReach or
            DemonFortressCombatState.HornRaise)
            FaceToward(player.Position);
        if (IsHornAlarmWindup &&
            (MathF.Abs(player.Position.X - Position.X) > 520f ||
                !HasClearSupportLine(player.Position, dungeon)))
        {
            InterruptHornAction();
            return;
        }
        if (CombatState == DemonFortressCombatState.HornJabActive)
            TryResolveFortressContact(player, dungeon, pushDistance: 0f,
                damageMultiplier: .72f);
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.HornReach:
                StartAction(DemonFortressCombatState.HornRaise, HornRaiseSeconds);
                break;
            case DemonFortressCombatState.HornRaise:
                StartAction(DemonFortressCombatState.HornInhale, HornInhaleSeconds);
                break;
            case DemonFortressCombatState.HornInhale:
                StartAction(DemonFortressCombatState.HornHold, HornHoldSeconds);
                break;
            case DemonFortressCombatState.HornHold:
                if (!_alarmCompletedThisAction && IsAlive)
                {
                    _alarmCompletedThisAction = true;
                    AlarmCompletionCount++;
                    AlarmCompleted?.Invoke(this);
                }
                StartAction(DemonFortressCombatState.HornBlast, HornBlastSeconds);
                break;
            case DemonFortressCombatState.HornBlast:
                StartAction(DemonFortressCombatState.HornRecovery,
                    HornRecoverySeconds);
                _alarmCooldownRemaining = 8f;
                break;
            case DemonFortressCombatState.HornRecovery:
            case DemonFortressCombatState.HornInterrupted:
                FinishFortressAction(.42f);
                break;
            case DemonFortressCombatState.HornJabWindup:
                StartAction(DemonFortressCombatState.HornJabActive,
                    HornJabActiveSeconds);
                break;
            case DemonFortressCombatState.HornJabActive:
                StartAction(DemonFortressCombatState.HornJabRecovery,
                    HornJabRecoverySeconds);
                break;
            case DemonFortressCombatState.HornJabRecovery:
                FinishFortressAction(.76f);
                break;
        }
    }

    private void UpdateShieldbearer(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateShieldbearerAction(gameTime, player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance <= 72f && _shieldPushCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.ShieldPushWindup,
                ShieldPushWindupSeconds);
            _shieldPushCooldownRemaining = 2.7f;
            return;
        }
        if (distance <= 88f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.ShieldStrikeWindup,
                ShieldStrikeWindupSeconds);
            return;
        }
        if (_decisionCooldownRemaining <= 0f)
        {
            _decisionCooldownRemaining = .42f;
            _decisionSerial++;
            if (_guardCooldownRemaining <= 0f && distance is >= 90f and <= 230f &&
                (_decisionSerial & 1) == 0)
            {
                _guardIntegrity = 128f;
                LockAttackDirection(player.Position);
                StartAction(DemonFortressCombatState.ShieldGuard,
                    ShieldGuardSeconds);
                return;
            }
        }
        MoveToward(gameTime, player.Position, 76f, dungeon, .58f);
    }

    private void UpdateShieldbearerAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.ShieldGuard)
        {
            if (_actionTimeRemaining <= 0f)
            {
                _guardCooldownRemaining = 1.9f;
                FinishFortressAction(.30f);
            }
            return;
        }
        if (CombatState == DemonFortressCombatState.ShieldPushActive)
        {
            MoveBy(new Vector2(
                _lockedAttackDirection * EffectiveMovementSpeed * .52f * elapsed, 0f),
                dungeon);
            TryResolveFortressContact(player, dungeon, pushDistance: 28f,
                damageMultiplier: .70f);
        }
        else if (CombatState == DemonFortressCombatState.ShieldStrikeActive)
        {
            TryResolveFortressContact(player, dungeon, pushDistance: 0f,
                damageMultiplier: .90f);
        }
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.ShieldPushWindup:
                StartAction(DemonFortressCombatState.ShieldPushActive,
                    ShieldPushActiveSeconds);
                break;
            case DemonFortressCombatState.ShieldPushActive:
                StartAction(DemonFortressCombatState.ShieldPushRecovery,
                    ShieldPushRecoverySeconds);
                break;
            case DemonFortressCombatState.ShieldPushRecovery:
                FinishFortressAction(.58f);
                break;
            case DemonFortressCombatState.ShieldStrikeWindup:
                StartAction(DemonFortressCombatState.ShieldStrikeActive,
                    ShieldStrikeActiveSeconds);
                break;
            case DemonFortressCombatState.ShieldStrikeActive:
                StartAction(DemonFortressCombatState.ShieldStrikeRecovery,
                    ShieldStrikeRecoverySeconds);
                break;
            case DemonFortressCombatState.ShieldStrikeRecovery:
                FinishFortressAction(.72f);
                break;
        }
    }

    private void UpdatePikeman(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdatePikemanAction(gameTime, player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance < 88f && _repositionCooldownRemaining <= 0f)
        {
            _lockedAttackDirection = Position.X < player.Position.X ? -1f : 1f;
            _repositionDistanceRemaining = 72f;
            StartAction(DemonFortressCombatState.PikeReposition,
                PikeRepositionSeconds);
            _repositionCooldownRemaining = 2.15f;
            return;
        }
        if (_combatCooldownRemaining <= 0f && distance is >= 102f and <= 168f &&
            HasClearPikeLine(player.Position, dungeon))
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.PikeSettle, PikeSettleSeconds);
            return;
        }
        if (_combatCooldownRemaining <= 0f && distance is > 168f and <= 218f &&
            HasClearPikeLine(player.Position, dungeon))
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.PikeAdvanceSettle,
                PikeSettleSeconds + .08f);
            return;
        }
        if (distance > 174f)
            MoveToward(gameTime, player.Position, 148f, dungeon, .54f);
    }

    private void UpdatePikemanAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.PikeReposition)
        {
            float before = Position.X;
            Vector2 threat = Position + new Vector2(-_lockedAttackDirection * 12f, 0f);
            MoveAway(gameTime, threat, dungeon, .62f);
            _repositionDistanceRemaining = MathF.Max(
                0f, _repositionDistanceRemaining - MathF.Abs(Position.X - before));
            if (_actionTimeRemaining <= 0f || _repositionDistanceRemaining <= 0f)
                FinishFortressAction(.30f);
            return;
        }

        bool pikeActive = CombatState is DemonFortressCombatState.PikeThrustExtend or
            DemonFortressCombatState.PikeThrustHold or
            DemonFortressCombatState.PikeAdvanceExtend or
            DemonFortressCombatState.PikeAdvanceHold;
        if (CombatState == DemonFortressCombatState.PikeAdvanceExtend)
        {
            MoveBy(new Vector2(
                _lockedAttackDirection * EffectiveMovementSpeed * .48f * elapsed, 0f),
                dungeon);
        }
        if (pikeActive)
            TryResolvePikeContact(player, dungeon);
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.PikeSettle:
                StartAction(DemonFortressCombatState.PikeThrustExtend,
                    PikeExtendSeconds);
                break;
            case DemonFortressCombatState.PikeThrustExtend:
                StartAction(DemonFortressCombatState.PikeThrustHold,
                    PikeHoldSeconds);
                break;
            case DemonFortressCombatState.PikeThrustHold:
                StartAction(DemonFortressCombatState.PikeRetract,
                    PikeRetractSeconds);
                break;
            case DemonFortressCombatState.PikeRetract:
                StartAction(DemonFortressCombatState.PikeRecovery,
                    PikeRecoverySeconds);
                break;
            case DemonFortressCombatState.PikeRecovery:
                FinishFortressAction(.78f);
                break;
            case DemonFortressCombatState.PikeAdvanceSettle:
                StartAction(DemonFortressCombatState.PikeAdvanceExtend,
                    PikeExtendSeconds + .04f);
                break;
            case DemonFortressCombatState.PikeAdvanceExtend:
                StartAction(DemonFortressCombatState.PikeAdvanceHold,
                    PikeHoldSeconds);
                break;
            case DemonFortressCombatState.PikeAdvanceHold:
                StartAction(DemonFortressCombatState.PikeAdvanceRetract,
                    PikeRetractSeconds);
                break;
            case DemonFortressCombatState.PikeAdvanceRetract:
                StartAction(DemonFortressCombatState.PikeAdvanceRecovery,
                    PikeRecoverySeconds + .10f);
                break;
            case DemonFortressCombatState.PikeAdvanceRecovery:
                FinishFortressAction(.88f);
                break;
        }
    }

    private void UpdateWarHound(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateWarHoundAction(gameTime, player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        bool wantsLeft = player.Position.X < Position.X;
        bool facesLeft = Facing == EnemyFacingDirection.Left;
        if (wantsLeft != facesLeft && distance > 34f)
        {
            _lockedAttackDirection = wantsLeft ? -1f : 1f;
            StartAction(DemonFortressCombatState.HoundTurn, HoundTurnSeconds);
            return;
        }
        FaceToward(player.Position);
        if (distance <= 66f && _houndBiteCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.HoundBiteWindup,
                HoundBiteWindupSeconds);
            _houndBiteCooldownRemaining = 1.35f;
            return;
        }
        if (_decisionCooldownRemaining <= 0f)
        {
            _decisionCooldownRemaining = .92f;
            _decisionSerial++;
            if (distance is >= 135f and <= 265f && _decisionSerial % 3 == 0)
            {
                _lockedAttackDirection = Position.X < player.Position.X ? -1f : 1f;
                _repositionDistanceRemaining = 48f;
                StartAction(DemonFortressCombatState.HoundFlank, HoundFlankSeconds);
                return;
            }
        }
        if (distance is >= 96f and <= 278f && _houndLungeCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.HoundLungeWindup,
                HoundLungeWindupSeconds);
            _houndLungeCooldownRemaining = 2.25f;
            return;
        }
        if (distance > 72f)
            MoveToward(gameTime, player.Position, 62f, dungeon,
                distance > 250f ? 1.08f : .82f);
        else if (distance < 48f)
            MoveAway(gameTime, player.Position, dungeon, .42f);
    }

    private void UpdateWarHoundAction(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.HoundTurn)
        {
            if (_actionTimeRemaining <= 0f)
            {
                FaceToward(Position + new Vector2(_lockedAttackDirection * 20f, 0f));
                FinishFortressAction(.08f);
            }
            return;
        }
        if (CombatState == DemonFortressCombatState.HoundFlank)
        {
            float before = Position.X;
            MoveBy(new Vector2(-_lockedAttackDirection * EffectiveMovementSpeed *
                .56f * elapsed, 0f), dungeon);
            _repositionDistanceRemaining = MathF.Max(
                0f, _repositionDistanceRemaining - MathF.Abs(Position.X - before));
            if (_actionTimeRemaining <= 0f || _repositionDistanceRemaining <= 0f)
                FinishFortressAction(.12f);
            return;
        }
        if (CombatState == DemonFortressCombatState.HoundLungeActive)
        {
            float before = Position.X;
            MoveBy(new Vector2(_lockedAttackDirection * 410f * elapsed, 0f), dungeon);
            TryResolveHoundContact(player, dungeon, damageMultiplier: 1.18f,
                pushDistance: 18f);
            if (MathF.Abs(Position.X - before) < .2f)
            {
                StartAction(DemonFortressCombatState.HoundLanding,
                    HoundLandingSeconds);
                return;
            }
        }
        else if (CombatState == DemonFortressCombatState.HoundBiteActive)
        {
            TryResolveHoundContact(player, dungeon, damageMultiplier: .86f,
                pushDistance: 0f);
        }
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.HoundLungeWindup:
                StartAction(DemonFortressCombatState.HoundLungeActive,
                    HoundLungeActiveSeconds);
                break;
            case DemonFortressCombatState.HoundLungeActive:
                StartAction(DemonFortressCombatState.HoundLanding,
                    HoundLandingSeconds);
                break;
            case DemonFortressCombatState.HoundLanding:
                StartAction(DemonFortressCombatState.HoundLungeRecovery,
                    HoundLungeRecoverySeconds);
                break;
            case DemonFortressCombatState.HoundLungeRecovery:
                FinishFortressAction(.30f);
                break;
            case DemonFortressCombatState.HoundBiteWindup:
                StartAction(DemonFortressCombatState.HoundBiteActive,
                    HoundBiteActiveSeconds);
                break;
            case DemonFortressCombatState.HoundBiteActive:
                StartAction(DemonFortressCombatState.HoundBiteRecovery,
                    HoundBiteRecoverySeconds);
                break;
            case DemonFortressCombatState.HoundBiteRecovery:
                FinishFortressAction(.36f);
                break;
        }
    }

    private void UpdateInfernalSmith(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateInfernalSmithAction(player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance <= 98f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            _decisionSerial++;
            StartAction(
                (_decisionSerial & 1) == 1
                    ? DemonFortressCombatState.SmithSlamRaise
                    : DemonFortressCombatState.SmithSwingWindup,
                (_decisionSerial & 1) == 1
                    ? SmithSlamRaiseSeconds
                    : SmithSwingWindupSeconds);
            return;
        }
        if (distance is > 98f and <= 390f)
            MoveToward(gameTime, player.Position, 82f, dungeon, .48f);
    }

    private void UpdateInfernalSmithAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState is DemonFortressCombatState.SmithSlamFall or
            DemonFortressCombatState.SmithSlamImpact or
            DemonFortressCombatState.SmithSwingActive)
            TryResolveSmithContact(player, dungeon);
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.SmithSlamRaise:
                StartAction(DemonFortressCombatState.SmithSlamFall,
                    SmithSlamFallSeconds);
                break;
            case DemonFortressCombatState.SmithSlamFall:
            {
                bool consumed = _actionHitConsumed;
                StartAction(DemonFortressCombatState.SmithSlamImpact,
                    SmithSlamImpactSeconds);
                _actionHitConsumed = consumed;
                break;
            }
            case DemonFortressCombatState.SmithSlamImpact:
                StartAction(DemonFortressCombatState.SmithSlamRecovery,
                    SmithSlamRecoverySeconds);
                break;
            case DemonFortressCombatState.SmithSlamRecovery:
                FinishFortressAction(1.02f);
                break;
            case DemonFortressCombatState.SmithSwingWindup:
                StartAction(DemonFortressCombatState.SmithSwingActive,
                    SmithSwingActiveSeconds);
                break;
            case DemonFortressCombatState.SmithSwingActive:
                StartAction(DemonFortressCombatState.SmithSwingRecovery,
                    SmithSwingRecoverySeconds);
                break;
            case DemonFortressCombatState.SmithSwingRecovery:
                FinishFortressAction(.92f);
                break;
        }
    }

    private void UpdateDemonOfficer(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateDemonOfficerAction(player, dungeon, enemies, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        int nearbyAllies = CountCommandableAllies(enemies, 255f);
        if (_officerCommandCooldownRemaining <= 0f && nearbyAllies > 0 &&
            distance <= 460f)
        {
            LockAttackDirection(player.Position);
            _officerCommandApplied = false;
            StartAction(DemonFortressCombatState.OfficerCommandRaise,
                OfficerCommandRaiseSeconds);
            return;
        }
        if (distance <= 82f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.OfficerSlashWindup,
                OfficerSlashWindupSeconds);
            return;
        }
        if (distance is > 82f and <= 148f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.OfficerAdvanceWindup,
                OfficerAdvanceWindupSeconds);
            return;
        }
        float preferred = nearbyAllies > 0 ? 102f : 78f;
        if (distance <= 440f)
            MoveToward(gameTime, player.Position, preferred, dungeon, .72f);
    }

    private void UpdateDemonOfficerAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.OfficerCommandActive &&
            !_officerCommandApplied)
        {
            _officerCommandApplied = true;
            OfficerCommandCount++;
            ApplyOfficerCommand(enemies);
            _officerCommandCooldownRemaining = 7.5f;
        }
        if (CombatState == DemonFortressCombatState.OfficerSlashActive)
            TryResolveOfficerContact(player, dungeon, advancing: false);
        if (CombatState == DemonFortressCombatState.OfficerAdvanceActive)
        {
            MoveBy(new Vector2(
                _lockedAttackDirection * EffectiveMovementSpeed * .58f * elapsed, 0f),
                dungeon);
            TryResolveOfficerContact(player, dungeon, advancing: true);
        }
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.OfficerCommandRaise:
                StartAction(DemonFortressCombatState.OfficerCommandActive,
                    OfficerCommandActiveSeconds);
                break;
            case DemonFortressCombatState.OfficerCommandActive:
                StartAction(DemonFortressCombatState.OfficerCommandRecovery,
                    OfficerCommandRecoverySeconds);
                break;
            case DemonFortressCombatState.OfficerCommandRecovery:
                FinishFortressAction(.34f);
                break;
            case DemonFortressCombatState.OfficerSlashWindup:
                StartAction(DemonFortressCombatState.OfficerSlashActive,
                    OfficerSlashActiveSeconds);
                break;
            case DemonFortressCombatState.OfficerSlashActive:
                StartAction(DemonFortressCombatState.OfficerSlashRecovery,
                    OfficerSlashRecoverySeconds);
                break;
            case DemonFortressCombatState.OfficerSlashRecovery:
                FinishFortressAction(.52f);
                break;
            case DemonFortressCombatState.OfficerAdvanceWindup:
                StartAction(DemonFortressCombatState.OfficerAdvanceActive,
                    OfficerAdvanceActiveSeconds);
                break;
            case DemonFortressCombatState.OfficerAdvanceActive:
                StartAction(DemonFortressCombatState.OfficerAdvanceRecovery,
                    OfficerAdvanceRecoverySeconds);
                break;
            case DemonFortressCombatState.OfficerAdvanceRecovery:
                FinishFortressAction(.62f);
                break;
        }
    }

    private void UpdateBallistaGunner(
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateBallistaAction(player, dungeon, projectiles, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (!_bossIntroCompleted)
        {
            if (distance > 540f)
                return;
            LockAttackDirection(player.Position);
            _lockedArrowTarget = player.Position;
            StartAction(DemonFortressCombatState.BallistaSetup, .72f);
            return;
        }

        if (distance <= 105f && _combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.BallistaShoveWindup, .32f);
            return;
        }
        if (_combatCooldownRemaining <= 0f)
        {
            LockAttackDirection(player.Position);
            _lockedArrowTarget = player.Position;
            StartAction(DemonFortressCombatState.BallistaAcquire,
                BossCombatPhase == 2 ? .30f : .40f);
        }
    }

    private void UpdateBallistaAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState is DemonFortressCombatState.BallistaAcquire or
            DemonFortressCombatState.BallistaAim)
        {
            float maximumTrack = (BossCombatPhase == 2 ? 250f : 205f) * elapsed;
            Vector2 delta = player.Position - _lockedArrowTarget;
            if (delta.LengthSquared() > maximumTrack * maximumTrack && maximumTrack > 0f)
                delta = Vector2.Normalize(delta) * maximumTrack;
            _lockedArrowTarget += delta;
            if ((_lockedArrowTarget.X - Position.X) * _lockedAttackDirection < 80f)
                _lockedArrowTarget.X = Position.X + _lockedAttackDirection * 80f;
        }
        if (CombatState == DemonFortressCombatState.BallistaFire && !_shotReleased)
        {
            _shotReleased = true;
            Vector2 origin = Position + new Vector2(
                _lockedAttackDirection * 76f, -Size.Y * .27f);
            projectiles.SpawnEnemyBolt(
                origin,
                _lockedArrowTarget - origin,
                Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * 1.38f)),
                RoomId,
                heavy: true);
            BallistaShotCount++;
        }
        if (CombatState == DemonFortressCombatState.BallistaShoveActive)
            TryResolveBallistaShove(player, dungeon);
        if (_actionTimeRemaining > 0f)
            return;

        float reloadScale = BossCombatPhase == 2 ? .82f : 1f;
        switch (CombatState)
        {
            case DemonFortressCombatState.BallistaSetup:
                _bossIntroCompleted = true;
                StartAction(DemonFortressCombatState.BallistaAcquire,
                    BossCombatPhase == 2 ? .30f : .40f);
                break;
            case DemonFortressCombatState.BallistaAcquire:
                StartAction(DemonFortressCombatState.BallistaAim,
                    BossCombatPhase == 2 ? .54f : .70f);
                break;
            case DemonFortressCombatState.BallistaAim:
                BallistaWarningCount++;
                StartAction(DemonFortressCombatState.BallistaWarning,
                    BossCombatPhase == 2 ? .47f : BallistaWarningSeconds);
                break;
            case DemonFortressCombatState.BallistaWarning:
                StartAction(DemonFortressCombatState.BallistaFire, .09f);
                break;
            case DemonFortressCombatState.BallistaFire:
                StartAction(DemonFortressCombatState.BallistaRecoil, .34f);
                break;
            case DemonFortressCombatState.BallistaRecoil:
                StartAction(DemonFortressCombatState.BallistaReloadReach,
                    .32f * reloadScale);
                break;
            case DemonFortressCombatState.BallistaReloadReach:
                StartAction(DemonFortressCombatState.BallistaReloadCrank,
                    .65f * reloadScale);
                break;
            case DemonFortressCombatState.BallistaReloadCrank:
                StartAction(DemonFortressCombatState.BallistaReloadLoad,
                    .55f * reloadScale);
                break;
            case DemonFortressCombatState.BallistaReloadLoad:
                StartAction(DemonFortressCombatState.BallistaReloadTension,
                    .56f * reloadScale);
                break;
            case DemonFortressCombatState.BallistaReloadTension:
                StartAction(DemonFortressCombatState.BallistaReady, .24f);
                break;
            case DemonFortressCombatState.BallistaReady:
                BallistaCycleCount++;
                FinishFortressAction(.16f);
                break;
            case DemonFortressCombatState.BallistaShoveWindup:
                StartAction(DemonFortressCombatState.BallistaShoveActive, .12f);
                break;
            case DemonFortressCombatState.BallistaShoveActive:
                StartAction(DemonFortressCombatState.BallistaShoveRecovery, .55f);
                break;
            case DemonFortressCombatState.BallistaShoveRecovery:
                FinishFortressAction(.72f);
                break;
        }
    }

    private void UpdateBlackGuardExecutioner(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonFortressCombatState.Ready)
        {
            UpdateExecutionerAction(player, dungeon, elapsed);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        FaceToward(player.Position);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (!_bossIntroCompleted)
        {
            if (distance > 540f)
                return;
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.ExecutionerIntro, .78f);
            return;
        }
        if (BossCombatPhase == 2 && !_executionerPhaseTransitioned)
        {
            _executionerPhaseTransitioned = true;
            ExecutionerPhaseTransitionCount++;
            LockAttackDirection(player.Position);
            StartAction(DemonFortressCombatState.ExecutionerPhaseTransition, .72f);
            return;
        }

        if (_combatCooldownRemaining <= 0f && distance <= 138f)
        {
            LockAttackDirection(player.Position);
            _decisionSerial++;
            if (BossCombatPhase == 2 && _decisionSerial % 3 == 0)
            {
                StartAction(DemonFortressCombatState.ExecutionerJudgmentRaise,
                    ExecutionerJudgmentRaiseSeconds);
            }
            else if (_executionerCounterCooldownRemaining <= 0f &&
                _decisionSerial % 4 == 0)
            {
                StartAction(DemonFortressCombatState.ExecutionerCounterEnter, .34f);
            }
            else if (_decisionSerial % 3 == 0)
            {
                StartAction(DemonFortressCombatState.ExecutionerArmorBreakWindup,
                    .62f);
            }
            else
            {
                StartAction(DemonFortressCombatState.ExecutionerCleaveWindup, .58f);
            }
            return;
        }

        if (distance <= 440f)
            MoveToward(gameTime, player.Position, 96f, dungeon,
                BossCombatPhase == 2 ? .72f : .58f);
    }

    private void UpdateExecutionerAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - elapsed);
        if (CombatState == DemonFortressCombatState.ExecutionerArmorBreakActive)
            MoveBy(new Vector2(_lockedAttackDirection * 24f * elapsed, 0f), dungeon);
        if (CombatState is DemonFortressCombatState.ExecutionerCleaveActive or
            DemonFortressCombatState.ExecutionerArmorBreakActive or
            DemonFortressCombatState.ExecutionerCounterRetaliateActive or
            DemonFortressCombatState.ExecutionerJudgmentFall or
            DemonFortressCombatState.ExecutionerJudgmentImpact or
            DemonFortressCombatState.ExecutionerReverseActive)
        {
            TryResolveExecutionerContact(player, dungeon);
        }
        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case DemonFortressCombatState.ExecutionerIntro:
                _bossIntroCompleted = true;
                FinishFortressAction(.28f);
                break;
            case DemonFortressCombatState.ExecutionerPhaseTransition:
                FinishFortressAction(.35f);
                break;
            case DemonFortressCombatState.ExecutionerCleaveWindup:
                StartAction(DemonFortressCombatState.ExecutionerCleaveActive, .17f);
                break;
            case DemonFortressCombatState.ExecutionerCleaveActive:
                if (BossCombatPhase == 2 &&
                    (player.Position.X - Position.X) * _lockedAttackDirection < -34f)
                {
                    _pendingAttackDirection = player.Position.X < Position.X ? -1f : 1f;
                    StartAction(DemonFortressCombatState.ExecutionerReverseTell, .36f);
                }
                else
                    StartAction(DemonFortressCombatState.ExecutionerCleaveRecovery, .72f);
                break;
            case DemonFortressCombatState.ExecutionerCleaveRecovery:
                FinishFortressAction(BossCombatPhase == 2 ? .46f : .62f);
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakWindup:
                StartAction(DemonFortressCombatState.ExecutionerArmorBreakActive, .15f);
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakActive:
                StartAction(DemonFortressCombatState.ExecutionerArmorBreakRecovery, .82f);
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakRecovery:
                FinishFortressAction(BossCombatPhase == 2 ? .48f : .68f);
                break;
            case DemonFortressCombatState.ExecutionerCounterEnter:
                StartAction(DemonFortressCombatState.ExecutionerCounterWindow,
                    ExecutionerCounterWindowSeconds);
                break;
            case DemonFortressCombatState.ExecutionerCounterWindow:
                _executionerCounterCooldownRemaining = 4.2f;
                StartAction(DemonFortressCombatState.ExecutionerCounterRecovery, .52f);
                break;
            case DemonFortressCombatState.ExecutionerCounterRetaliateWindup:
                StartAction(DemonFortressCombatState.ExecutionerCounterRetaliateActive,
                    .14f);
                break;
            case DemonFortressCombatState.ExecutionerCounterRetaliateActive:
                StartAction(DemonFortressCombatState.ExecutionerCounterRecovery, .68f);
                break;
            case DemonFortressCombatState.ExecutionerCounterRecovery:
                FinishFortressAction(.64f);
                break;
            case DemonFortressCombatState.ExecutionerJudgmentRaise:
                StartAction(DemonFortressCombatState.ExecutionerJudgmentHold,
                    (_decisionSerial & 1) == 0 ? .42f : .22f);
                break;
            case DemonFortressCombatState.ExecutionerJudgmentHold:
                StartAction(DemonFortressCombatState.ExecutionerJudgmentFall, .18f);
                break;
            case DemonFortressCombatState.ExecutionerJudgmentFall:
            {
                bool consumed = _actionHitConsumed;
                StartAction(DemonFortressCombatState.ExecutionerJudgmentImpact, .16f);
                _actionHitConsumed = consumed;
                break;
            }
            case DemonFortressCombatState.ExecutionerJudgmentImpact:
                StartAction(DemonFortressCombatState.ExecutionerJudgmentRecovery, .96f);
                break;
            case DemonFortressCombatState.ExecutionerJudgmentRecovery:
                FinishFortressAction(.72f);
                break;
            case DemonFortressCombatState.ExecutionerReverseTell:
                _lockedAttackDirection = _pendingAttackDirection;
                StartAction(DemonFortressCombatState.ExecutionerReverseActive, .15f);
                break;
            case DemonFortressCombatState.ExecutionerReverseActive:
                StartAction(DemonFortressCombatState.ExecutionerReverseRecovery, .68f);
                break;
            case DemonFortressCombatState.ExecutionerReverseRecovery:
                FinishFortressAction(.62f);
                break;
        }
    }

    public bool HasClearShot(Vector2 target, DungeonMap dungeon)
    {
        return HasClearLine(GetBowOrigin(), target, dungeon);
    }

    private bool HasClearSupportLine(Vector2 target, DungeonMap dungeon) =>
        HasClearLine(Position + new Vector2(0f, -Size.Y * .25f), target, dungeon);

    private bool HasClearPikeLine(Vector2 target, DungeonMap dungeon)
    {
        float direction = CombatState == DemonFortressCombatState.Ready
            ? Facing == EnemyFacingDirection.Left ? -1f : 1f
            : _lockedAttackDirection;
        return HasClearLine(Position + new Vector2(
            direction * Size.X * .35f, -Size.Y * .05f), target, dungeon);
    }

    private static bool HasClearLine(
        Vector2 origin,
        Vector2 target,
        DungeonMap dungeon)
    {
        if (dungeon == null)
            return false;
        Vector2 delta = target - origin;
        float length = delta.Length();
        if (length <= 24f)
            return true;
        int steps = Math.Max(2, (int)MathF.Ceiling(length / 8f));
        for (int step = 2; step < steps - 2; step++)
        {
            Vector2 point = origin + delta * (step / (float)steps);
            foreach (Platform platform in dungeon.Platforms)
                if (platform.Bounds.Contains(point.ToPoint()))
                    return false;
        }
        return true;
    }

    private bool HasFrontlineScreen(Vector2 playerPosition, EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        float towardPlayer = MathF.Sign(playerPosition.X - Position.X);
        if (towardPlayer == 0f)
            return false;
        foreach (Enemy ally in enemies.Enemies)
        {
            if (ReferenceEquals(ally, this) || !ally.IsAlive || ally.RoomId != RoomId ||
                ally.Type is not (EnemyType.DemonLegionnaireFortress or
                    EnemyType.DemonShieldbearer))
                continue;
            float allyOffset = ally.Position.X - Position.X;
            if (MathF.Sign(allyOffset) == towardPlayer &&
                MathF.Abs(allyOffset) is >= 36f and <= 260f)
                return true;
        }
        return false;
    }

    private Vector2 GetBowOrigin() => Position + new Vector2(
        (AttackFacesLeft ? -1f : 1f) * Size.X * .38f,
        -Size.Y * .18f);

    private void TryResolveLegionContact(PlayerCharacter player)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.Bounds))
            return;
        _actionHitConsumed = true;
        player.ReceiveMeleeAttack(new AttackContact(
            EffectiveAttackDamage, Position, true, false, ActiveAttackArea));
    }

    private void TryResolveFortressContact(
        PlayerCharacter player,
        DungeonMap dungeon,
        float pushDistance,
        float damageMultiplier)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        int damage = Math.Max(1,
            (int)MathF.Round(EffectiveAttackDamage * damageMultiplier));
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            damage, Position, true, false, ActiveAttackArea));
        if (pushDistance > 0f &&
            result is (AttackResolution.Damaged or AttackResolution.GuardBroken))
            player.PushAwayFrom(Position, pushDistance, dungeon);
    }

    private void TryResolvePikeContact(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (_actionHitConsumed || !HasClearPikeLine(player.Position, dungeon) ||
            !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        float multiplier = CombatState is DemonFortressCombatState.PikeAdvanceExtend or
            DemonFortressCombatState.PikeAdvanceHold ? 1.05f : 1f;
        player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position,
            true,
            false,
            ActiveAttackArea));
    }

    private void TryResolveHoundContact(
        PlayerCharacter player,
        DungeonMap dungeon,
        float damageMultiplier,
        float pushDistance)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1,
                (int)MathF.Round(EffectiveAttackDamage * damageMultiplier)),
            Position,
            true,
            false,
            ActiveAttackArea));
        if (pushDistance > 0f &&
            result is (AttackResolution.Damaged or AttackResolution.GuardBroken))
            player.PushAwayFrom(Position, pushDistance, dungeon);
    }

    private void TryResolveSmithContact(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        float multiplier = CombatState switch
        {
            DemonFortressCombatState.SmithSlamFall => 1.42f,
            DemonFortressCombatState.SmithSlamImpact => 1.18f,
            _ => 1.12f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position,
            true,
            false,
            ActiveAttackArea));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        {
            float push = CombatState == DemonFortressCombatState.SmithSlamImpact
                ? 26f : 16f;
            player.PushAwayFrom(Position, push, dungeon);
        }
    }

    private void TryResolveOfficerContact(
        PlayerCharacter player,
        DungeonMap dungeon,
        bool advancing)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(
                EffectiveAttackDamage * (advancing ? 1.08f : 1f))),
            Position,
            true,
            false,
            ActiveAttackArea));
        if (advancing &&
            result is (AttackResolution.Damaged or AttackResolution.GuardBroken))
            player.PushAwayFrom(Position, 12f, dungeon);
    }

    private void TryResolveBallistaShove(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * .52f)),
            Position,
            true,
            false,
            ActiveAttackArea));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
            player.PushAwayFrom(Position, 22f, dungeon);
    }

    private void TryResolveExecutionerContact(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (_actionHitConsumed || !ActiveAttackArea.Intersects(player.BodyHurtbox))
            return;
        _actionHitConsumed = true;
        bool armorBreak = CombatState ==
            DemonFortressCombatState.ExecutionerArmorBreakActive;
        float multiplier = CombatState switch
        {
            DemonFortressCombatState.ExecutionerJudgmentFall => 1.68f,
            DemonFortressCombatState.ExecutionerJudgmentImpact => 1.40f,
            DemonFortressCombatState.ExecutionerCounterRetaliateActive => 1.45f,
            DemonFortressCombatState.ExecutionerCleaveActive => 1.28f,
            DemonFortressCombatState.ExecutionerReverseActive => 1.12f,
            _ => 1.34f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position,
            blockable: true,
            unblockable: armorBreak,
            hitArea: ActiveAttackArea));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        {
            float push = CombatState is
                DemonFortressCombatState.ExecutionerJudgmentFall or
                DemonFortressCombatState.ExecutionerJudgmentImpact
                    ? 34f
                    : armorBreak ? 28f : 18f;
            player.PushAwayFrom(Position, push, dungeon);
        }
    }

    private int CountCommandableAllies(EnemyManager enemies, float radius)
    {
        if (enemies == null)
            return 0;
        float radiusSquared = radius * radius;
        int count = 0;
        foreach (Enemy ally in enemies.Enemies)
        {
            if (!IsCommandableAlly(ally) ||
                Vector2.DistanceSquared(ally.Position, Position) > radiusSquared)
                continue;
            count++;
        }
        return count;
    }

    private void ApplyOfficerCommand(EnemyManager enemies)
    {
        _commandedAllies.Clear();
        if (enemies == null)
            return;
        const float radiusSquared = 255f * 255f;
        foreach (Enemy ally in enemies.Enemies)
        {
            if (!IsCommandableAlly(ally) ||
                Vector2.DistanceSquared(ally.Position, Position) > radiusSquared)
                continue;
            ally.ApplyTemporaryBuff(6, 4, 3.8f);
            _commandedAllies.Add(ally);
        }
    }

    private bool IsCommandableAlly(Enemy ally) =>
        ally != null && ally.IsAlive && !ReferenceEquals(ally, this) &&
        ally.RoomId == RoomId && ally is DemonFortressEnemy &&
        ally.Type is not (EnemyType.DemonOfficer or EnemyType.BallistaGunner or
            EnemyType.BlackGuardExecutioner);

    private void ClearOfficerCommandBuffs()
    {
        foreach (Enemy ally in _commandedAllies)
            if (ally.IsAlive)
                ally.ClearTemporaryBuff();
        _commandedAllies.Clear();
        _officerCommandApplied = false;
    }

    private Rectangle CreateLegionAttackArea()
    {
        int reach = CombatState == DemonFortressCombatState.LegionCounterActive
            ? 68
            : CombatState == DemonFortressCombatState.LegionFollowupActive ? 76 : 72;
        const int height = 64;
        int y = (int)Position.Y - height / 2;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach, y, reach, height)
            : new Rectangle(Bounds.Right, y, reach, height);
    }

    private Rectangle CreateHornAttackArea() =>
        CreateDirectionalAttackArea(reach: 58, height: 48);

    private Rectangle CreateShieldbearerAttackArea() =>
        CreateDirectionalAttackArea(
            reach: CombatState == DemonFortressCombatState.ShieldPushActive ? 64 : 55,
            height: 68);

    private Rectangle CreatePikeAttackArea()
    {
        float tipDistance = 48f + PikeExtensionRatio * 108f;
        const int tipWidth = 34;
        const int height = 30;
        int y = (int)Position.Y - height / 2 - 3;
        int tipX = (int)MathF.Round(Position.X + _lockedAttackDirection * tipDistance);
        return AttackFacesLeft
            ? new Rectangle(tipX, y, tipWidth, height)
            : new Rectangle(tipX - tipWidth, y, tipWidth, height);
    }

    private Rectangle CreateHoundAttackArea()
    {
        int reach = CombatState == DemonFortressCombatState.HoundLungeActive ? 58 : 42;
        const int height = 38;
        int y = Bounds.Top + 2;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach, y, reach + 16, height)
            : new Rectangle(Bounds.Right - 16, y, reach + 16, height);
    }

    private Rectangle CreateSmithAttackArea()
    {
        if (CombatState == DemonFortressCombatState.SmithSlamImpact)
        {
            const int width = 132;
            return new Rectangle(
                (int)Position.X - width / 2,
                Bounds.Bottom - 34,
                width,
                34);
        }
        return CreateDirectionalAttackArea(
            reach: CombatState == DemonFortressCombatState.SmithSwingActive ? 88 : 70,
            height: 78);
    }

    private Rectangle CreateOfficerAttackArea() =>
        CreateDirectionalAttackArea(
            reach: CombatState == DemonFortressCombatState.OfficerAdvanceActive
                ? 82 : 70,
            height: 68);

    private Rectangle CreateBallistaAttackArea() =>
        CreateDirectionalAttackArea(reach: 62, height: 54);

    private Rectangle CreateExecutionerAttackArea()
    {
        if (CombatState is DemonFortressCombatState.ExecutionerJudgmentFall or
            DemonFortressCombatState.ExecutionerJudgmentImpact)
        {
            const int width = 148;
            return new Rectangle(
                (int)Position.X - width / 2,
                Bounds.Bottom - 46,
                width,
                46);
        }
        int reach = CombatState switch
        {
            DemonFortressCombatState.ExecutionerCleaveActive => 116,
            DemonFortressCombatState.ExecutionerCounterRetaliateActive => 104,
            DemonFortressCombatState.ExecutionerReverseActive => 96,
            _ => 88
        };
        return CreateDirectionalAttackArea(reach, 92);
    }

    private Rectangle CreateDirectionalAttackArea(int reach, int height)
    {
        int y = (int)Position.Y - height / 2;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach, y, reach, height)
            : new Rectangle(Bounds.Right, y, reach, height);
    }

    private bool IsSourceInFront(Vector2 sourcePosition)
    {
        float offset = sourcePosition.X - Position.X;
        if (IsLegionGuarding || IsShieldbearerGuarding || IsExecutionerCountering)
            return MathF.Abs(offset) <= 2f || MathF.Sign(offset) == _lockedAttackDirection;
        return MathF.Abs(offset) <= 2f ||
            (offset < 0f && Facing == EnemyFacingDirection.Left) ||
            (offset > 0f && Facing == EnemyFacingDirection.Right);
    }

    private void LockAttackDirection(Vector2 target)
    {
        FaceToward(target);
        _lockedAttackDirection = target.X < Position.X ? -1f : 1f;
    }

    private void StartAction(DemonFortressCombatState state, float duration)
    {
        CombatState = state;
        _actionDuration = duration;
        _actionTimeRemaining = duration;
        _actionHitConsumed = false;
        _shotReleased = false;
    }

    private void FinishFortressAction(float cooldown)
    {
        CombatState = DemonFortressCombatState.Ready;
        _actionTimeRemaining = 0f;
        _actionDuration = 0f;
        _actionHitConsumed = false;
        _shotReleased = false;
        _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, cooldown);
    }

    private void InterruptHornAction()
    {
        if (!IsHornAlarmWindup)
            return;
        _alarmCompletedThisAction = false;
        _alarmCooldownRemaining = MathF.Max(_alarmCooldownRemaining, 5f);
        StartAction(DemonFortressCombatState.HornInterrupted,
            HornInterruptedSeconds);
    }

    private void CancelFortressAction(float cooldown)
    {
        CombatState = DemonFortressCombatState.Ready;
        _actionTimeRemaining = 0f;
        _actionDuration = 0f;
        _counterPending = false;
        _actionHitConsumed = false;
        _shotReleased = false;
        _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, cooldown);
    }

    protected override void OnCombatInterrupted()
    {
        if (IsHornAlarmWindup)
        {
            InterruptHornAction();
            return;
        }
        if (Type == EnemyType.BallistaGunner &&
            CombatState is DemonFortressCombatState.BallistaFire or
                DemonFortressCombatState.BallistaRecoil or
                DemonFortressCombatState.BallistaReloadReach or
                DemonFortressCombatState.BallistaReloadCrank or
                DemonFortressCombatState.BallistaReloadLoad or
                DemonFortressCombatState.BallistaReloadTension)
        {
            StartAction(DemonFortressCombatState.BallistaReloadReach, .42f);
            return;
        }
        CancelFortressAction(.58f);
        if (Type == EnemyType.DemonShieldbearer)
            _guardCooldownRemaining = MathF.Max(_guardCooldownRemaining, 1.8f);
        if (Type == EnemyType.BlackGuardExecutioner)
            _executionerCounterCooldownRemaining = MathF.Max(
                _executionerCounterCooldownRemaining, 1.8f);
    }

    private static bool IsFortressType(EnemyType type) => type is
        EnemyType.DemonLegionnaireFortress or EnemyType.FortressArcher or
        EnemyType.HornGuard or EnemyType.DemonShieldbearer or
        EnemyType.InfernalPikeman or EnemyType.WarHound or
        EnemyType.InfernalSmith or EnemyType.DemonOfficer or
        EnemyType.BallistaGunner or EnemyType.BlackGuardExecutioner;

    private static Vector2 GetSize(EnemyType type) => type switch
    {
        EnemyType.DemonLegionnaireFortress => new Vector2(54f, 80f),
        EnemyType.FortressArcher => new Vector2(47f, 76f),
        EnemyType.HornGuard => new Vector2(48f, 76f),
        EnemyType.WarHound => new Vector2(66f, 42f),
        EnemyType.DemonShieldbearer => new Vector2(64f, 84f),
        EnemyType.InfernalSmith => new Vector2(74f, 92f),
        EnemyType.DemonOfficer => new Vector2(58f, 86f),
        EnemyType.BallistaGunner => new Vector2(94f, 76f),
        EnemyType.BlackGuardExecutioner => new Vector2(76f, 102f),
        _ => new Vector2(52f, 76f)
    };

    private static float GetSpeed(EnemyType type) => type switch
    {
        EnemyType.DemonLegionnaireFortress => 86f,
        EnemyType.WarHound => 172f,
        EnemyType.FortressArcher => 96f,
        EnemyType.DemonShieldbearer => 66f,
        EnemyType.InfernalPikeman => 82f,
        EnemyType.InfernalSmith => 62f,
        EnemyType.DemonOfficer => 88f,
        EnemyType.BallistaGunner => 36f,
        EnemyType.BlackGuardExecutioner => 58f,
        _ => 96f
    };

    private static float GetDetectionRange(EnemyType type) => type switch
    {
        EnemyType.FortressArcher => 720f,
        EnemyType.BallistaGunner => 900f,
        EnemyType.DemonOfficer => 650f,
        _ => 540f
    };

    private static int GetHealth(EnemyType type, int level, int worldTier)
    {
        int baseHealth = type switch
        {
            EnemyType.WarHound => 145,
            EnemyType.FortressArcher => 150,
            EnemyType.HornGuard => 180,
            EnemyType.DemonShieldbearer => 260,
            EnemyType.InfernalPikeman => 190,
            EnemyType.InfernalSmith => 330,
            EnemyType.DemonOfficer => 285,
            EnemyType.BallistaGunner => 720,
            EnemyType.BlackGuardExecutioner => 980,
            _ => 205
        };
        int scaled = baseHealth + (Math.Max(1, level) - 1) * 24;
        return WorldProgression.ApplyPercent(
            scaled, WorldProgression.GetHealthMultiplierPercent(worldTier));
    }

    private static int GetDamage(EnemyType type, int level, int worldTier)
    {
        int baseDamage = type switch
        {
            EnemyType.WarHound => 15,
            EnemyType.FortressArcher => 17,
            EnemyType.DemonShieldbearer => 19,
            EnemyType.InfernalPikeman => 22,
            EnemyType.InfernalSmith => 25,
            EnemyType.DemonOfficer => 23,
            EnemyType.BallistaGunner => 31,
            EnemyType.BlackGuardExecutioner => 36,
            _ => 18
        };
        int scaled = baseDamage + (Math.Max(1, level) - 1) * 2;
        return WorldProgression.ApplyPercent(
            scaled, WorldProgression.GetDamageMultiplierPercent(worldTier));
    }

    private static int GetExperience(EnemyType type) => type switch
    {
        EnemyType.BallistaGunner => 650,
        EnemyType.BlackGuardExecutioner => 900,
        EnemyType.InfernalSmith or EnemyType.DemonOfficer => 230,
        EnemyType.DemonShieldbearer => 190,
        _ => 145
    };

    private static float GetAttackRange(EnemyType type) => type switch
    {
        EnemyType.InfernalPikeman => 104f,
        EnemyType.InfernalSmith => 82f,
        EnemyType.BlackGuardExecutioner => 112f,
        EnemyType.FortressArcher or EnemyType.BallistaGunner => 48f,
        EnemyType.WarHound => 58f,
        _ => 66f
    };

    private static float GetAttackCooldown(EnemyType type) => type switch
    {
        EnemyType.WarHound => .82f,
        EnemyType.DemonShieldbearer => 1.45f,
        EnemyType.InfernalSmith => 1.55f,
        EnemyType.BlackGuardExecutioner => 1.75f,
        _ => 1.1f
    };
}
