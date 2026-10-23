using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum DemonCityCombatState
{
    Ready,
    EnforcerPressureStep,
    EnforcerCleaveWindup, EnforcerCleaveActive, EnforcerCleaveRecovery,
    EnforcerBackhandWindup, EnforcerBackhandActive, EnforcerBackhandRecovery,
    EnforcerShieldWindup, EnforcerShieldActive, EnforcerShieldRecovery,
    KeeperRetreat,
    KeeperBoltCharge, KeeperBoltRelease, KeeperBoltRecovery,
    KeeperBurstWindup, KeeperBurstActive, KeeperBurstRecovery,
    StalkerHunterStep,
    StalkerAshCutWindup, StalkerAshCutActive, StalkerAshCutRecovery,
    StalkerTwinRipWindup, StalkerTwinRipFirst, StalkerTwinRipTurn,
    StalkerTwinRipSecond, StalkerTwinRipRecovery,
    StalkerCrossingWindup, StalkerCrossingActive, StalkerCrossingRecovery,
    StalkerDisengage,
    CollectorRetreat,
    CollectorSweepGather, CollectorSweepActive, CollectorSweepRecovery,
    CollectorHookGather, CollectorHookAim, CollectorHookExtend,
    CollectorHookResolve, CollectorHookRetract, CollectorHookRecovery,
    CollectorDropWindup, CollectorDropActive, CollectorDropRecovery
}

public enum DemonCityAttackKind
{
    None, CityCleave, EnforcerBackhand, ShieldCheck, SoulBolt, LanternBurst,
    AshCut, TwinRip, CrossingFang, ChainSweep, CollectorsHook, ChainDrop
}

/// <summary>
/// Standard Demon City inhabitants. Every attack owns explicit tell, active,
/// and recovery states so rendering, audio, damage, and interruption agree.
/// </summary>
public sealed class DemonCityEnemy : Enemy
{
    public const float CleaveWindupSeconds = .34f, CleaveActiveSeconds = .13f,
        CleaveRecoverySeconds = .48f, BackhandWindupSeconds = .27f,
        BackhandActiveSeconds = .12f, BackhandRecoverySeconds = .50f,
        ShieldWindupSeconds = .25f, ShieldActiveSeconds = .11f,
        ShieldRecoverySeconds = .46f, BoltChargeSeconds = .66f,
        BoltReleaseSeconds = .10f, BoltRecoverySeconds = .62f,
        BurstWindupSeconds = .40f, BurstActiveSeconds = .14f,
        BurstRecoverySeconds = .58f;
    public const float AshCutWindupSeconds = .25f, AshCutActiveSeconds = .11f,
        AshCutRecoverySeconds = .36f, TwinRipWindupSeconds = .32f,
        TwinRipHitSeconds = .10f, TwinRipTurnSeconds = .16f,
        TwinRipRecoverySeconds = .48f, CrossingWindupSeconds = .47f,
        CrossingActiveSeconds = .18f, CrossingRecoverySeconds = .68f,
        HunterStepSeconds = .25f;
    public const float SweepGatherSeconds = .48f, SweepActiveSeconds = .19f,
        SweepRecoverySeconds = .58f, HookGatherSeconds = .40f,
        HookAimSeconds = .40f, HookExtendSeconds = .26f,
        HookResolveSeconds = .12f, HookRetractSeconds = .36f,
        HookRecoverySeconds = .74f, DropWindupSeconds = .28f,
        DropActiveSeconds = .12f, DropRecoverySeconds = .50f,
        CollectorHookCooldownSeconds = 5.4f, CollectorPullDistance = 30f;

    private float _actionTimeRemaining, _actionDuration;
    private float _combatCooldownRemaining = .35f, _shieldCooldownRemaining,
        _burstCooldownRemaining, _retreatCooldownRemaining,
        _hunterStepCooldownRemaining, _crossingCooldownRemaining,
        _hookCooldownRemaining, _dropCooldownRemaining;
    private float _lockedAttackDirection = 1f;
    private Vector2 _lockedBoltTarget, _lockedHookTarget;
    private int _decisionSerial;
    private bool _actionHitConsumed, _projectileReleased, _hookConnected;

    public DemonCityCombatState CombatState { get; private set; } = DemonCityCombatState.Ready;
    public DemonCityAttackKind AttackKind { get; private set; } = DemonCityAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f ? 0f :
        Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedAttackDirection < 0f;
    public float LanternChargeRatio => CombatState == DemonCityCombatState.KeeperBoltCharge
        ? ActionProgress : CombatState == DemonCityCombatState.KeeperBoltRelease ? 1f : 0f;
    public float HookExtensionRatio => CombatState switch
    {
        DemonCityCombatState.CollectorHookExtend => ActionProgress,
        DemonCityCombatState.CollectorHookResolve => 1f,
        DemonCityCombatState.CollectorHookRetract => 1f - ActionProgress,
        _ => 0f
    };
    public Vector2 HookTargetOffset => new(
        _lockedHookTarget.X - Position.X,
        _lockedHookTarget.Y - Bounds.Bottom);
    public bool HookConnected => _hookConnected;
    public float HookCooldownRemaining => _hookCooldownRemaining;

    public override bool UsesProjectileAttack => Type == EnemyType.SoulLanternKeeper;
    public override bool IsCombatAttackTelegraphing => CombatState is
        DemonCityCombatState.EnforcerCleaveWindup or DemonCityCombatState.EnforcerBackhandWindup or
        DemonCityCombatState.EnforcerShieldWindup or DemonCityCombatState.KeeperBoltCharge or
        DemonCityCombatState.KeeperBurstWindup or DemonCityCombatState.StalkerAshCutWindup or
        DemonCityCombatState.StalkerTwinRipWindup or DemonCityCombatState.StalkerTwinRipTurn or
        DemonCityCombatState.StalkerCrossingWindup or DemonCityCombatState.CollectorSweepGather or
        DemonCityCombatState.CollectorHookGather or DemonCityCombatState.CollectorHookAim or
        DemonCityCombatState.CollectorDropWindup;
    public override bool IsCombatAttackActive => CombatState is
        DemonCityCombatState.EnforcerCleaveActive or DemonCityCombatState.EnforcerBackhandActive or
        DemonCityCombatState.EnforcerShieldActive or DemonCityCombatState.KeeperBoltRelease or
        DemonCityCombatState.KeeperBurstActive or DemonCityCombatState.StalkerAshCutActive or
        DemonCityCombatState.StalkerTwinRipFirst or DemonCityCombatState.StalkerTwinRipSecond or
        DemonCityCombatState.StalkerCrossingActive or DemonCityCombatState.CollectorSweepActive or
        DemonCityCombatState.CollectorHookExtend or DemonCityCombatState.CollectorHookResolve or
        DemonCityCombatState.CollectorDropActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        DemonCityCombatState.EnforcerCleaveActive or DemonCityCombatState.EnforcerBackhandActive => ForwardArea(76, 72, -36),
        DemonCityCombatState.EnforcerShieldActive => ForwardArea(54, 56, -28),
        DemonCityCombatState.KeeperBurstActive => new Rectangle((int)Position.X - 72, (int)Position.Y - 58, 144, 116),
        DemonCityCombatState.StalkerAshCutActive => ForwardArea(72, 68, -38),
        DemonCityCombatState.StalkerTwinRipFirst or DemonCityCombatState.StalkerTwinRipSecond => ForwardArea(76, 66, -37),
        DemonCityCombatState.StalkerCrossingActive => ForwardArea(88, 70, -39),
        DemonCityCombatState.CollectorSweepActive => ForwardArea(142, 74, -43),
        DemonCityCombatState.CollectorDropActive => ForwardArea(68, 82, -62),
        DemonCityCombatState.CollectorHookExtend or DemonCityCombatState.CollectorHookResolve => HookArea(),
        _ => Rectangle.Empty
    };

    public DemonCityEnemy(EnemyType type, Vector2 position, int level, int worldTier, int roomId)
        : base(type, position, SizeFor(type), SpeedFor(type), DetectionFor(type),
            EnemyStatScaling.Health(BaseHealth(type), HealthPerLevel(type), level, worldTier),
            EnemyStatScaling.Damage(BaseDamage(type), 2, level, worldTier), Experience(type),
            level, worldTier, false, roomId, AttackRange(type), 0f)
    {
        if (type is not (EnemyType.DemonCityEnforcer or EnemyType.SoulLanternKeeper or
            EnemyType.AshbladeStalker or EnemyType.ChainCollector))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    protected override void UpdateBehavior(GameTime gameTime, PlayerCharacter player,
        DungeonMap dungeon, ProjectileManager projectiles, RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        TickCooldowns(dt);
        if (CombatState != DemonCityCombatState.Ready)
        {
            UpdateAction(dt, player, dungeon, projectiles);
            return;
        }
        if (!IsPlayerDetected(player.Position)) return;
        switch (Type)
        {
            case EnemyType.DemonCityEnforcer: UpdateEnforcer(gameTime, player, dungeon); break;
            case EnemyType.SoulLanternKeeper: UpdateKeeper(gameTime, player, dungeon); break;
            case EnemyType.AshbladeStalker: UpdateStalker(gameTime, player, dungeon); break;
            case EnemyType.ChainCollector: UpdateCollector(gameTime, player, dungeon); break;
        }
    }

    private void TickCooldowns(float dt)
    {
        _combatCooldownRemaining = MathF.Max(0f, _combatCooldownRemaining - dt);
        _shieldCooldownRemaining = MathF.Max(0f, _shieldCooldownRemaining - dt);
        _burstCooldownRemaining = MathF.Max(0f, _burstCooldownRemaining - dt);
        _retreatCooldownRemaining = MathF.Max(0f, _retreatCooldownRemaining - dt);
        _hunterStepCooldownRemaining = MathF.Max(0f, _hunterStepCooldownRemaining - dt);
        _crossingCooldownRemaining = MathF.Max(0f, _crossingCooldownRemaining - dt);
        _hookCooldownRemaining = MathF.Max(0f, _hookCooldownRemaining - dt);
        _dropCooldownRemaining = MathF.Max(0f, _dropCooldownRemaining - dt);
    }

    private void UpdateEnforcer(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float d = MathF.Abs(player.Position.X - Position.X);
        if (d > 76f)
        {
            if (d < 142f && _combatCooldownRemaining <= 0f && (_decisionSerial++ & 3) == 3)
            { Lock(player.Position); Start(DemonCityCombatState.EnforcerPressureStep, .22f, DemonCityAttackKind.None); return; }
            MoveToward(time, player.Position, 68f, map, .92f); return;
        }
        if (_combatCooldownRemaining > 0f) return;
        Lock(player.Position); int decision = _decisionSerial++;
        if (d <= 52f && _shieldCooldownRemaining <= 0f && decision % 4 == 2)
        { _shieldCooldownRemaining = 3.1f; Start(DemonCityCombatState.EnforcerShieldWindup, ShieldWindupSeconds, DemonCityAttackKind.ShieldCheck); }
        else if (decision % 3 == 1) Start(DemonCityCombatState.EnforcerBackhandWindup, BackhandWindupSeconds, DemonCityAttackKind.EnforcerBackhand);
        else Start(DemonCityCombatState.EnforcerCleaveWindup, CleaveWindupSeconds, DemonCityAttackKind.CityCleave);
    }

    private void UpdateKeeper(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float d = MathF.Abs(player.Position.X - Position.X);
        if (d <= 96f && _burstCooldownRemaining <= 0f && _combatCooldownRemaining <= 0f)
        { Lock(player.Position); _burstCooldownRemaining = 3.3f; Start(DemonCityCombatState.KeeperBurstWindup, BurstWindupSeconds, DemonCityAttackKind.LanternBurst); return; }
        if (d < 180f && _retreatCooldownRemaining <= 0f)
        { Lock(player.Position); _retreatCooldownRemaining = 2.7f; Start(DemonCityCombatState.KeeperRetreat, .44f, DemonCityAttackKind.None); return; }
        if (d > 540f) { MoveToward(time, player.Position, 455f, map, .68f); return; }
        if (_combatCooldownRemaining > 0f) return;
        Vector2 target = player.Position + new Vector2(0f, -8f);
        if (!ClearLine(LanternMouth(), target, map)) { if (d > 205f) MoveToward(time, player.Position, 190f, map, .58f); return; }
        Lock(player.Position); _lockedBoltTarget = target;
        Start(DemonCityCombatState.KeeperBoltCharge, BoltChargeSeconds, DemonCityAttackKind.SoulBolt);
    }

    private void UpdateStalker(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float d = MathF.Abs(player.Position.X - Position.X);
        if (d > 88f)
        {
            if (d < 160f && _hunterStepCooldownRemaining <= 0f && _combatCooldownRemaining <= 0f)
            { Lock(player.Position); _hunterStepCooldownRemaining = 2.2f; Start(DemonCityCombatState.StalkerHunterStep, HunterStepSeconds, DemonCityAttackKind.None); return; }
            MoveToward(time, player.Position, 78f, map, 1.02f); return;
        }
        if (_combatCooldownRemaining > 0f) return;
        Lock(player.Position); int decision = _decisionSerial++;
        if (d >= 54f && _crossingCooldownRemaining <= 0f && decision % 4 == 3)
        { _crossingCooldownRemaining = 4.1f; Start(DemonCityCombatState.StalkerCrossingWindup, CrossingWindupSeconds, DemonCityAttackKind.CrossingFang); }
        else if (decision % 3 == 1) Start(DemonCityCombatState.StalkerTwinRipWindup, TwinRipWindupSeconds, DemonCityAttackKind.TwinRip);
        else Start(DemonCityCombatState.StalkerAshCutWindup, AshCutWindupSeconds, DemonCityAttackKind.AshCut);
    }

    private void UpdateCollector(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float d = MathF.Abs(player.Position.X - Position.X);
        if (d < 86f)
        {
            Lock(player.Position);
            if (_dropCooldownRemaining <= 0f && _combatCooldownRemaining <= 0f)
            { _dropCooldownRemaining = 3f; Start(DemonCityCombatState.CollectorDropWindup, DropWindupSeconds, DemonCityAttackKind.ChainDrop); }
            else if (_retreatCooldownRemaining <= 0f)
            { _retreatCooldownRemaining = 3.2f; Start(DemonCityCombatState.CollectorRetreat, .38f, DemonCityAttackKind.None); }
            return;
        }
        if (d > 350f) { MoveToward(time, player.Position, 275f, map, .66f); return; }
        if (_combatCooldownRemaining > 0f) return;
        Lock(player.Position); Vector2 target = player.Position + new Vector2(0f, -10f);
        if (_hookCooldownRemaining <= 0f && d >= 135f && (_decisionSerial++ % 3 == 2))
        {
            _hookCooldownRemaining = CollectorHookCooldownSeconds;
            _lockedHookTarget = ClipHookTarget(HookHand(), target, map); _hookConnected = false;
            Start(DemonCityCombatState.CollectorHookGather, HookGatherSeconds, DemonCityAttackKind.CollectorsHook); return;
        }
        Start(DemonCityCombatState.CollectorSweepGather, SweepGatherSeconds, DemonCityAttackKind.ChainSweep);
    }

    private void UpdateAction(float dt, PlayerCharacter player, DungeonMap map, ProjectileManager projectiles)
    {
        if (CombatState == DemonCityCombatState.EnforcerPressureStep) MoveLocked(.62f, dt, map);
        else if (CombatState is DemonCityCombatState.KeeperRetreat or DemonCityCombatState.CollectorRetreat or DemonCityCombatState.StalkerDisengage)
            MoveLocked(CombatState == DemonCityCombatState.StalkerDisengage ? -.74f : -.68f, dt, map);
        else if (CombatState == DemonCityCombatState.StalkerHunterStep) MoveLocked((_decisionSerial & 1) == 0 ? .88f : -.62f, dt, map);
        else if (CombatState == DemonCityCombatState.StalkerCrossingActive) MoveLocked(.84f, dt, map);
        if (CombatState == DemonCityCombatState.CollectorHookExtend) ResolveHook(player, map);
        _actionTimeRemaining -= dt;
        if (_actionTimeRemaining <= 0f) Advance(projectiles, map);
        if (CombatState is DemonCityCombatState.EnforcerCleaveActive or DemonCityCombatState.EnforcerBackhandActive or
            DemonCityCombatState.EnforcerShieldActive or DemonCityCombatState.KeeperBurstActive or
            DemonCityCombatState.StalkerAshCutActive or DemonCityCombatState.StalkerTwinRipFirst or
            DemonCityCombatState.StalkerTwinRipSecond or DemonCityCombatState.StalkerCrossingActive or
            DemonCityCombatState.CollectorSweepActive or DemonCityCombatState.CollectorDropActive) ResolveContact(player, map);
    }

    private void Advance(ProjectileManager projectiles, DungeonMap map)
    {
        switch (CombatState)
        {
            case DemonCityCombatState.EnforcerPressureStep: Finish(.18f); break;
            case DemonCityCombatState.EnforcerCleaveWindup: Start(DemonCityCombatState.EnforcerCleaveActive, CleaveActiveSeconds, DemonCityAttackKind.CityCleave); break;
            case DemonCityCombatState.EnforcerCleaveActive: Start(DemonCityCombatState.EnforcerCleaveRecovery, CleaveRecoverySeconds, DemonCityAttackKind.CityCleave); break;
            case DemonCityCombatState.EnforcerCleaveRecovery: Finish(.46f); break;
            case DemonCityCombatState.EnforcerBackhandWindup: Start(DemonCityCombatState.EnforcerBackhandActive, BackhandActiveSeconds, DemonCityAttackKind.EnforcerBackhand); break;
            case DemonCityCombatState.EnforcerBackhandActive: Start(DemonCityCombatState.EnforcerBackhandRecovery, BackhandRecoverySeconds, DemonCityAttackKind.EnforcerBackhand); break;
            case DemonCityCombatState.EnforcerBackhandRecovery: Finish(.58f); break;
            case DemonCityCombatState.EnforcerShieldWindup: Start(DemonCityCombatState.EnforcerShieldActive, ShieldActiveSeconds, DemonCityAttackKind.ShieldCheck); break;
            case DemonCityCombatState.EnforcerShieldActive: Start(DemonCityCombatState.EnforcerShieldRecovery, ShieldRecoverySeconds, DemonCityAttackKind.ShieldCheck); break;
            case DemonCityCombatState.EnforcerShieldRecovery: Finish(.72f); break;
            case DemonCityCombatState.KeeperRetreat: Finish(.26f); break;
            case DemonCityCombatState.KeeperBoltCharge: Start(DemonCityCombatState.KeeperBoltRelease, BoltReleaseSeconds, DemonCityAttackKind.SoulBolt); ReleaseSoulBolt(projectiles, map); break;
            case DemonCityCombatState.KeeperBoltRelease: Start(DemonCityCombatState.KeeperBoltRecovery, BoltRecoverySeconds, DemonCityAttackKind.SoulBolt); break;
            case DemonCityCombatState.KeeperBoltRecovery: Finish(.86f); break;
            case DemonCityCombatState.KeeperBurstWindup: Start(DemonCityCombatState.KeeperBurstActive, BurstActiveSeconds, DemonCityAttackKind.LanternBurst); break;
            case DemonCityCombatState.KeeperBurstActive: Start(DemonCityCombatState.KeeperBurstRecovery, BurstRecoverySeconds, DemonCityAttackKind.LanternBurst); break;
            case DemonCityCombatState.KeeperBurstRecovery: Finish(1.08f); break;
            case DemonCityCombatState.StalkerHunterStep: Finish(.38f); break;
            case DemonCityCombatState.StalkerAshCutWindup: Start(DemonCityCombatState.StalkerAshCutActive, AshCutActiveSeconds, DemonCityAttackKind.AshCut); break;
            case DemonCityCombatState.StalkerAshCutActive: Start(DemonCityCombatState.StalkerAshCutRecovery, AshCutRecoverySeconds, DemonCityAttackKind.AshCut); break;
            case DemonCityCombatState.StalkerAshCutRecovery: BeginDisengage(); break;
            case DemonCityCombatState.StalkerTwinRipWindup: Start(DemonCityCombatState.StalkerTwinRipFirst, TwinRipHitSeconds, DemonCityAttackKind.TwinRip); break;
            case DemonCityCombatState.StalkerTwinRipFirst: Start(DemonCityCombatState.StalkerTwinRipTurn, TwinRipTurnSeconds, DemonCityAttackKind.TwinRip); break;
            case DemonCityCombatState.StalkerTwinRipTurn: Start(DemonCityCombatState.StalkerTwinRipSecond, TwinRipHitSeconds, DemonCityAttackKind.TwinRip); break;
            case DemonCityCombatState.StalkerTwinRipSecond: Start(DemonCityCombatState.StalkerTwinRipRecovery, TwinRipRecoverySeconds, DemonCityAttackKind.TwinRip); break;
            case DemonCityCombatState.StalkerTwinRipRecovery: BeginDisengage(); break;
            case DemonCityCombatState.StalkerCrossingWindup: Start(DemonCityCombatState.StalkerCrossingActive, CrossingActiveSeconds, DemonCityAttackKind.CrossingFang); break;
            case DemonCityCombatState.StalkerCrossingActive: Start(DemonCityCombatState.StalkerCrossingRecovery, CrossingRecoverySeconds, DemonCityAttackKind.CrossingFang); break;
            case DemonCityCombatState.StalkerCrossingRecovery: BeginDisengage(); break;
            case DemonCityCombatState.StalkerDisengage: Finish(.48f); break;
            case DemonCityCombatState.CollectorRetreat: Finish(.42f); break;
            case DemonCityCombatState.CollectorSweepGather: Start(DemonCityCombatState.CollectorSweepActive, SweepActiveSeconds, DemonCityAttackKind.ChainSweep); break;
            case DemonCityCombatState.CollectorSweepActive: Start(DemonCityCombatState.CollectorSweepRecovery, SweepRecoverySeconds, DemonCityAttackKind.ChainSweep); break;
            case DemonCityCombatState.CollectorSweepRecovery: Finish(.78f); break;
            case DemonCityCombatState.CollectorHookGather: Start(DemonCityCombatState.CollectorHookAim, HookAimSeconds, DemonCityAttackKind.CollectorsHook); break;
            case DemonCityCombatState.CollectorHookAim: Start(DemonCityCombatState.CollectorHookExtend, HookExtendSeconds, DemonCityAttackKind.CollectorsHook); break;
            case DemonCityCombatState.CollectorHookExtend: Start(DemonCityCombatState.CollectorHookResolve, HookResolveSeconds, DemonCityAttackKind.CollectorsHook); break;
            case DemonCityCombatState.CollectorHookResolve: Start(DemonCityCombatState.CollectorHookRetract, HookRetractSeconds, DemonCityAttackKind.CollectorsHook); break;
            case DemonCityCombatState.CollectorHookRetract: Start(DemonCityCombatState.CollectorHookRecovery, HookRecoverySeconds, DemonCityAttackKind.CollectorsHook); break;
            case DemonCityCombatState.CollectorHookRecovery: Finish(.92f); break;
            case DemonCityCombatState.CollectorDropWindup: Start(DemonCityCombatState.CollectorDropActive, DropActiveSeconds, DemonCityAttackKind.ChainDrop); break;
            case DemonCityCombatState.CollectorDropActive: Start(DemonCityCombatState.CollectorDropRecovery, DropRecoverySeconds, DemonCityAttackKind.ChainDrop); break;
            case DemonCityCombatState.CollectorDropRecovery: Finish(.72f); break;
        }
    }

    private void BeginDisengage() => Start(DemonCityCombatState.StalkerDisengage, .30f, DemonCityAttackKind.None);
    private void ReleaseSoulBolt(ProjectileManager projectiles, DungeonMap map)
    {
        if (_projectileReleased || projectiles == null || !IsAlive) return;
        Vector2 origin = LanternMouth(); if (!ClearLine(origin, _lockedBoltTarget, map)) return;
        _projectileReleased = true; projectiles.SpawnSoulBolt(origin, _lockedBoltTarget - origin, EffectiveAttackDamage, RoomId, cursePressure: 0f);
    }

    private void ResolveHook(PlayerCharacter player, DungeonMap map)
    {
        if (_actionHitConsumed || !IsAlive) return; Rectangle area = HookArea();
        if (area.IsEmpty || !ClearLine(HookHand(), HookTip(), map) ||
            (!SegmentTouches(HookHand(), HookTip(), player.BodyHurtbox, 9f) &&
             !(player.Combat.IsBlocking && SegmentTouches(HookHand(), HookTip(), player.DefenseBounds, 9f)))) return;
        _actionHitConsumed = true;
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * .70f)), Position, true, false, area));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        { player.PullToward(Position, CollectorPullDistance, map); _hookConnected = true; Start(DemonCityCombatState.CollectorHookResolve, HookResolveSeconds + .08f, DemonCityAttackKind.CollectorsHook); }
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap map)
    {
        Rectangle area = ActiveAttackArea;
        if (_actionHitConsumed || area.IsEmpty || !ClearLine(Position + new Vector2(0f, -Size.Y * .18f), player.Position + new Vector2(0f, -8f), map) ||
            (!area.Intersects(player.BodyHurtbox) && !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds)))) return;
        _actionHitConsumed = true;
        float multiplier = CombatState switch
        {
            DemonCityCombatState.EnforcerShieldActive => .64f,
            DemonCityCombatState.EnforcerBackhandActive => .92f,
            DemonCityCombatState.KeeperBurstActive => .72f,
            DemonCityCombatState.StalkerAshCutActive => .88f,
            DemonCityCombatState.StalkerTwinRipFirst or DemonCityCombatState.StalkerTwinRipSecond => .58f,
            DemonCityCombatState.StalkerCrossingActive => 1.18f,
            DemonCityCombatState.CollectorSweepActive => .86f,
            DemonCityCombatState.CollectorDropActive => .68f,
            _ => 1f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)), Position, true, false, area));
        if ((CombatState is DemonCityCombatState.EnforcerShieldActive or DemonCityCombatState.KeeperBurstActive or DemonCityCombatState.CollectorSweepActive) &&
            result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        {
            float push = CombatState == DemonCityCombatState.EnforcerShieldActive ? 20f : CombatState == DemonCityCombatState.CollectorSweepActive ? 18f : 16f;
            player.PushAwayFrom(Position, push, map);
        }
    }

    private void MoveLocked(float scale, float dt, DungeonMap map) => MoveBy(new Vector2(_lockedAttackDirection * EffectiveMovementSpeed * scale * dt, 0f), map);
    private Rectangle ForwardArea(int reach, int height, int yOffset) => new(_lockedAttackDirection < 0f ? Bounds.Left - reach : Bounds.Right, (int)Position.Y + yOffset, reach, height);
    private Rectangle HookArea()
    {
        Vector2 a = HookHand(), b = HookTip(); const int p = 9;
        int l = (int)MathF.Floor(MathF.Min(a.X, b.X)) - p, t = (int)MathF.Floor(MathF.Min(a.Y, b.Y)) - p;
        int r = (int)MathF.Ceiling(MathF.Max(a.X, b.X)) + p, d = (int)MathF.Ceiling(MathF.Max(a.Y, b.Y)) + p;
        return new Rectangle(l, t, Math.Max(1, r - l), Math.Max(1, d - t));
    }
    private Vector2 HookHand() => Position + new Vector2(_lockedAttackDirection * 23f, -Size.Y * .46f);
    private Vector2 HookTip() => Vector2.Lerp(HookHand(), _lockedHookTarget, HookExtensionRatio);
    private Vector2 LanternMouth() => Position + new Vector2(_lockedAttackDirection * 25f, -Size.Y * .20f);
    private static Vector2 ClipHookTarget(Vector2 origin, Vector2 target, DungeonMap map)
    {
        Vector2 delta = target - origin; float length = delta.Length(); if (length > 315f) delta *= 315f / length;
        int steps = Math.Max(2, (int)MathF.Ceiling(delta.Length() / 7f)); Vector2 prior = origin;
        for (int step = 1; step <= steps; step++)
        { Vector2 point = origin + delta * (step / (float)steps); foreach (Platform platform in map.Platforms) if (platform.Bounds.Contains(point.ToPoint())) return prior; prior = point; }
        return origin + delta;
    }
    private static bool SegmentTouches(Vector2 start, Vector2 end, Rectangle bounds, float padding)
    {
        Vector2 center = bounds.Center.ToVector2(); Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        float t = lengthSquared <= .01f ? 0f : Math.Clamp(Vector2.Dot(center - start, segment) / lengthSquared, 0f, 1f);
        Vector2 nearest = start + segment * t;
        float radius = MathF.Min(bounds.Width, bounds.Height) * .5f + padding;
        return Vector2.DistanceSquared(center, nearest) <= radius * radius;
    }
    public bool HasClearShot(Vector2 target, DungeonMap map) => ClearLine(LanternMouth(), target, map);
    private static bool ClearLine(Vector2 origin, Vector2 target, DungeonMap map)
    {
        if (map == null) return false; Vector2 delta = target - origin; float length = delta.Length(); if (length <= 20f) return true;
        int steps = Math.Max(2, (int)MathF.Ceiling(length / 8f));
        for (int step = 2; step < steps - 2; step++) { Vector2 point = origin + delta * (step / (float)steps); foreach (Platform platform in map.Platforms) if (platform.Bounds.Contains(point.ToPoint())) return false; }
        return true;
    }
    private void Lock(Vector2 target) { FaceToward(target); _lockedAttackDirection = target.X < Position.X ? -1f : 1f; }
    private void Start(DemonCityCombatState state, float duration, DemonCityAttackKind kind)
    { CombatState = state; AttackKind = kind; _actionDuration = MathF.Max(.01f, duration); _actionTimeRemaining = _actionDuration; _actionHitConsumed = false; if (state != DemonCityCombatState.KeeperBoltRelease) _projectileReleased = false; }
    private void Finish(float cooldown)
    { CombatState = DemonCityCombatState.Ready; AttackKind = DemonCityAttackKind.None; _actionDuration = 0f; _actionTimeRemaining = 0f; _actionHitConsumed = false; _projectileReleased = false; _hookConnected = false; _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, cooldown); }
    private void Cancel(float cooldown) { Finish(cooldown); Attack.Cancel(); }
    protected override void OnDamaged() { if (!IsAlive) Cancel(0f); }
    protected override void OnCombatInterrupted() => Cancel(.62f);

    private static Vector2 SizeFor(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => new(54f, 82f), EnemyType.SoulLanternKeeper => new(46f, 88f), EnemyType.AshbladeStalker => new(46f, 78f), EnemyType.ChainCollector => new(50f, 94f), _ => throw new ArgumentOutOfRangeException(nameof(type), type, null) };
    private static float SpeedFor(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 94f, EnemyType.SoulLanternKeeper => 78f, EnemyType.AshbladeStalker => 126f, EnemyType.ChainCollector => 72f, _ => 80f };
    private static float DetectionFor(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 560f, EnemyType.SoulLanternKeeper => 720f, EnemyType.AshbladeStalker => 610f, EnemyType.ChainCollector => 700f, _ => 560f };
    private static int BaseHealth(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 215, EnemyType.SoulLanternKeeper => 165, EnemyType.AshbladeStalker => 172, EnemyType.ChainCollector => 218, _ => 165 };
    private static int HealthPerLevel(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 26, EnemyType.ChainCollector => 25, _ => 21 };
    private static int BaseDamage(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 19, EnemyType.SoulLanternKeeper => 18, EnemyType.AshbladeStalker => 17, EnemyType.ChainCollector => 19, _ => 18 };
    private static int Experience(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 155, EnemyType.SoulLanternKeeper => 170, EnemyType.AshbladeStalker => 168, EnemyType.ChainCollector => 188, _ => 160 };
    private static float AttackRange(EnemyType type) => type switch { EnemyType.DemonCityEnforcer => 70f, EnemyType.SoulLanternKeeper => 76f, EnemyType.AshbladeStalker => 76f, EnemyType.ChainCollector => 142f, _ => 70f };
}
