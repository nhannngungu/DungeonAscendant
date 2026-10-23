using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum DemonCityBossState
{
    Intro,
    Ready,
    PhaseTransition,
    Reposition,
    CleaveWindup,
    CleaveActive,
    CleaveRecovery,
    ChopRaise,
    ChopHold,
    ChopImpact,
    ChopRecovery,
    DragLower,
    DragAdvance,
    DragRise,
    DragRecovery,
    StompWindup,
    StompImpact,
    StompRecovery,
    FollowupTell,
    FollowupActive,
    FollowupRecovery,
    BoltCharge,
    BoltRelease,
    BoltRecovery,
    DecreeGesture,
    DecreeCommit,
    DecreeRecovery,
    ScepterWindup,
    ScepterActive,
    ScepterRecovery,
    WritGesture,
    WritCommit,
    WritRecovery,
    StepPlant,
    StepDistort,
    StepRecovery,
    VerdictPlant,
    VerdictSequence,
    VerdictRelease,
    VerdictRecovery
}

public enum DemonCityBossAttackKind
{
    None,
    SlaughterCleave,
    AltarChop,
    ButchersDrag,
    RitualStomp,
    DelayedCleave,
    JudgmentBolt,
    DecreeZone,
    ScepterJudgment,
    WritOfBinding,
    MagistrateStep,
    FinalVerdict
}

/// <summary>
/// The two authored Map 6 boss-tier encounters. Each owns a finite, readable
/// state machine and cleans up every room projectile and owned seal on death.
/// </summary>
public sealed class DemonCityBossEnemy : Enemy
{
    private float _actionRemaining;
    private float _actionDuration;
    private float _combatCooldown = .45f;
    private float _stepCooldown;
    private float _verdictCooldown;
    private float _lockedDirection = 1f;
    private int _decisionSerial;
    private bool _hitConsumed;
    private bool _followupQueued;
    private bool _phaseTwoPlayed;
    private bool _phaseThreePlayed;
    private bool _deathRaised;
    private bool _shotReleased;
    private Vector2 _lockedTarget;
    private ProjectileManager _projectiles;
    private RootHazardManager _hazards;
    private DungeonMap _dungeon;

    public DemonCityBossState CombatState { get; private set; } =
        DemonCityBossState.Intro;
    public DemonCityBossAttackKind AttackKind { get; private set; } =
        DemonCityBossAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool BossIntroCompleted { get; private set; }
    public int PhaseTransitionCount { get; private set; }
    public int BossTierCompletionCount { get; private set; }
    public int BossCombatPhase => Type switch
    {
        EnemyType.CathedralButcher => CurrentHealth * 100 < MaxHealth * 55 ? 2 : 1,
        EnemyType.AbyssalMagistrate when CurrentHealth * 10 < MaxHealth * 3 => 3,
        EnemyType.AbyssalMagistrate when CurrentHealth * 100 <= MaxHealth * 65 => 2,
        EnemyType.AbyssalMagistrate => 1,
        _ => 0
    };
    public Vector2 StepDestination { get; private set; }
    public bool IsBossTierEncounter => true;
    public event Action<DemonCityBossEnemy> BossTierDefeated;

    public override bool CanDropLoot => false;
    public override bool UsesProjectileAttack => Type == EnemyType.AbyssalMagistrate;
    public override bool CanBeTargeted => base.CanBeTargeted &&
        CombatState != DemonCityBossState.Intro;
    protected override float DeathPresentationDurationSeconds =>
        Type == EnemyType.CathedralButcher ? 1.42f : 1.26f;

    public override bool IsCombatAttackTelegraphing => CombatState is
        DemonCityBossState.CleaveWindup or DemonCityBossState.ChopRaise or
        DemonCityBossState.ChopHold or DemonCityBossState.DragLower or
        DemonCityBossState.StompWindup or DemonCityBossState.FollowupTell or
        DemonCityBossState.BoltCharge or DemonCityBossState.DecreeGesture or
        DemonCityBossState.ScepterWindup or DemonCityBossState.WritGesture or
        DemonCityBossState.StepPlant or DemonCityBossState.StepDistort or
        DemonCityBossState.VerdictPlant or DemonCityBossState.VerdictSequence;

    public override bool IsCombatAttackActive => CombatState is
        DemonCityBossState.CleaveActive or DemonCityBossState.ChopImpact or
        DemonCityBossState.DragAdvance or DemonCityBossState.DragRise or
        DemonCityBossState.StompImpact or DemonCityBossState.FollowupActive or
        DemonCityBossState.ScepterActive;

    public override Rectangle ActiveAttackArea => CombatState switch
    {
        DemonCityBossState.CleaveActive => ForwardArea(148, 104, -74),
        DemonCityBossState.ChopImpact => ForwardArea(104, 126, -92),
        DemonCityBossState.DragAdvance => ForwardArea(112, 62, -26),
        DemonCityBossState.DragRise => ForwardArea(122, 116, -88),
        DemonCityBossState.StompImpact => new Rectangle(
            (int)Position.X - 116, Bounds.Bottom - 40, 232, 62),
        DemonCityBossState.FollowupActive => ForwardArea(128, 92, -66),
        DemonCityBossState.ScepterActive => ForwardArea(116, 82, -60),
        _ => Rectangle.Empty
    };

    public DemonCityBossEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId)
        : base(
            type,
            position,
            type == EnemyType.CathedralButcher
                ? new Vector2(92f, 132f)
                : new Vector2(60f, 118f),
            type == EnemyType.CathedralButcher ? 56f : 78f,
            type == EnemyType.CathedralButcher ? 780f : 840f,
            EnemyStatScaling.Health(
                type == EnemyType.CathedralButcher ? 330 : 405,
                type == EnemyType.CathedralButcher ? 34 : 38,
                level,
                worldTier),
            EnemyStatScaling.Damage(
                type == EnemyType.CathedralButcher ? 25 : 23,
                3,
                level,
                worldTier),
            type == EnemyType.CathedralButcher ? 520 : 680,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: type == EnemyType.CathedralButcher ? 142f : 126f,
            attackCooldownSeconds: 0f)
    {
        if (type is not (EnemyType.CathedralButcher or
            EnemyType.AbyssalMagistrate))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        Start(DemonCityBossState.Intro,
            type == EnemyType.CathedralButcher ? .72f : 1.02f,
            DemonCityBossAttackKind.None);
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
        _projectiles = projectiles;
        _hazards = rootHazards;
        _dungeon = dungeon;
        _combatCooldown = MathF.Max(0f, _combatCooldown - elapsed);
        _stepCooldown = MathF.Max(0f, _stepCooldown - elapsed);
        _verdictCooldown = MathF.Max(0f, _verdictCooldown - elapsed);

        if (CombatState != DemonCityBossState.Ready)
        {
            UpdateCommittedMovement(player, dungeon, elapsed);
            _actionRemaining -= elapsed;
            if (IsCombatAttackActive)
                ResolveContact(player, dungeon);
            if (_actionRemaining <= 0f)
                Advance(player, dungeon);
            return;
        }

        if (!IsPlayerDetected(player.Position))
            return;

        if (BeginRequiredPhaseTransition())
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (_combatCooldown > 0f)
        {
            if (distance > Attack.Range + 26f)
                MoveToward(gameTime, player.Position, Attack.Range, dungeon, .46f);
            return;
        }

        LockDirection(player.Position);
        if (Type == EnemyType.CathedralButcher)
            ChooseButcherAttack(distance);
        else
            ChooseMagistrateAttack(player, dungeon, distance);
    }

    private bool BeginRequiredPhaseTransition()
    {
        if (BossCombatPhase >= 2 && !_phaseTwoPlayed)
        {
            _phaseTwoPlayed = true;
            PhaseTransitionCount++;
            _hazards?.RemoveOwnedBy(this);
            Start(DemonCityBossState.PhaseTransition,
                Type == EnemyType.CathedralButcher ? .82f : .90f,
                DemonCityBossAttackKind.None);
            return true;
        }
        if (BossCombatPhase >= 3 && !_phaseThreePlayed)
        {
            _phaseThreePlayed = true;
            PhaseTransitionCount++;
            _hazards?.RemoveOwnedBy(this);
            Start(DemonCityBossState.PhaseTransition, .96f,
                DemonCityBossAttackKind.None);
            return true;
        }
        return false;
    }

    private void ChooseButcherAttack(float distance)
    {
        int decision = _decisionSerial++;
        bool phaseTwo = BossCombatPhase == 2;
        _followupQueued = phaseTwo && decision % 3 == 1;
        bool useDrag = distance > 178f ||
            (phaseTwo ? decision % 3 == 0 : decision % 5 == 2);
        if (useDrag)
        {
            Start(DemonCityBossState.DragLower, .52f,
                DemonCityBossAttackKind.ButchersDrag);
        }
        else if (decision % 3 == 1)
        {
            Start(DemonCityBossState.ChopRaise, .62f,
                DemonCityBossAttackKind.AltarChop);
        }
        else if (decision % 3 == 2)
        {
            Start(DemonCityBossState.StompWindup, .58f,
                DemonCityBossAttackKind.RitualStomp);
        }
        else
        {
            Start(DemonCityBossState.CleaveWindup, .48f,
                DemonCityBossAttackKind.SlaughterCleave);
        }
    }

    private void ChooseMagistrateAttack(
        PlayerCharacter player,
        DungeonMap dungeon,
        float distance)
    {
        int decision = _decisionSerial++;
        if (BossCombatPhase >= 2 && _verdictCooldown <= 0f && decision % 6 == 4)
        {
            BeginFinalVerdict(player, dungeon);
            return;
        }
        if (_stepCooldown <= 0f && distance < 150f && decision % 4 == 1)
        {
            BeginMagistrateStep(player, dungeon);
            return;
        }
        if (BossCombatPhase >= 2 && decision % 5 == 2)
        {
            _lockedTarget = player.Position;
            Start(DemonCityBossState.WritGesture, .68f,
                DemonCityBossAttackKind.WritOfBinding);
        }
        else if (decision % 4 == 0)
        {
            _lockedTarget = player.Position;
            Start(DemonCityBossState.DecreeGesture, .66f,
                DemonCityBossAttackKind.DecreeZone);
        }
        else if (distance <= 134f && decision % 3 == 1)
        {
            Start(DemonCityBossState.ScepterWindup, .42f,
                DemonCityBossAttackKind.ScepterJudgment);
        }
        else
        {
            _lockedTarget = player.Position;
            Start(DemonCityBossState.BoltCharge, .56f,
                DemonCityBossAttackKind.JudgmentBolt);
        }
    }

    private void UpdateCommittedMovement(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState == DemonCityBossState.DragAdvance)
            MoveLocked(.72f, elapsed, dungeon);
        else if (CombatState == DemonCityBossState.CleaveActive)
            MoveLocked(.22f, elapsed, dungeon);
        else if (CombatState == DemonCityBossState.StepDistort)
        {
            Vector2 offset = StepDestination - Position;
            float length = offset.Length();
            if (length > .5f)
            {
                MoveBy(offset / length * MathF.Min(
                    length, EffectiveMovementSpeed * 4.2f * elapsed), dungeon);
            }
        }
    }

    private void Advance(PlayerCharacter player, DungeonMap dungeon)
    {
        switch (CombatState)
        {
            case DemonCityBossState.Intro:
                BossIntroCompleted = true;
                Finish(.35f);
                break;
            case DemonCityBossState.PhaseTransition:
                Finish(.48f);
                break;
            case DemonCityBossState.Reposition:
                Finish(.32f);
                break;
            case DemonCityBossState.CleaveWindup:
                Start(DemonCityBossState.CleaveActive, .18f, AttackKind);
                break;
            case DemonCityBossState.CleaveActive:
                Start(DemonCityBossState.CleaveRecovery,
                    BossCombatPhase == 2 ? .54f : .68f, AttackKind);
                break;
            case DemonCityBossState.CleaveRecovery:
                FinishOrFollowup();
                break;
            case DemonCityBossState.ChopRaise:
                Start(DemonCityBossState.ChopHold, .24f, AttackKind);
                break;
            case DemonCityBossState.ChopHold:
                Start(DemonCityBossState.ChopImpact, .16f, AttackKind);
                break;
            case DemonCityBossState.ChopImpact:
                Start(DemonCityBossState.ChopRecovery,
                    BossCombatPhase == 2 ? .70f : .82f, AttackKind);
                break;
            case DemonCityBossState.ChopRecovery:
                FinishOrFollowup();
                break;
            case DemonCityBossState.DragLower:
                Start(DemonCityBossState.DragAdvance,
                    BossCombatPhase == 2 ? .62f : .54f, AttackKind);
                break;
            case DemonCityBossState.DragAdvance:
                Start(DemonCityBossState.DragRise, .16f, AttackKind);
                break;
            case DemonCityBossState.DragRise:
                Start(DemonCityBossState.DragRecovery, .72f, AttackKind);
                break;
            case DemonCityBossState.DragRecovery:
                FinishOrFollowup();
                break;
            case DemonCityBossState.StompWindup:
                Start(DemonCityBossState.StompImpact, .18f, AttackKind);
                break;
            case DemonCityBossState.StompImpact:
                Start(DemonCityBossState.StompRecovery, .72f, AttackKind);
                break;
            case DemonCityBossState.StompRecovery:
                FinishOrFollowup();
                break;
            case DemonCityBossState.FollowupTell:
                Start(DemonCityBossState.FollowupActive, .14f, AttackKind);
                break;
            case DemonCityBossState.FollowupActive:
                Start(DemonCityBossState.FollowupRecovery, .72f, AttackKind);
                break;
            case DemonCityBossState.FollowupRecovery:
                Finish(.42f);
                break;
            case DemonCityBossState.BoltCharge:
                ReleaseJudgmentBolt(empowered: false);
                Start(DemonCityBossState.BoltRelease, .10f, AttackKind);
                break;
            case DemonCityBossState.BoltRelease:
                Start(DemonCityBossState.BoltRecovery, .54f, AttackKind);
                break;
            case DemonCityBossState.BoltRecovery:
                Finish(.38f);
                break;
            case DemonCityBossState.DecreeGesture:
                CreateDecree(dungeon);
                Start(DemonCityBossState.DecreeCommit, .18f, AttackKind);
                break;
            case DemonCityBossState.DecreeCommit:
                Start(DemonCityBossState.DecreeRecovery, .62f, AttackKind);
                break;
            case DemonCityBossState.DecreeRecovery:
                Finish(.42f);
                break;
            case DemonCityBossState.ScepterWindup:
                Start(DemonCityBossState.ScepterActive, .14f, AttackKind);
                break;
            case DemonCityBossState.ScepterActive:
                Start(DemonCityBossState.ScepterRecovery, .62f, AttackKind);
                break;
            case DemonCityBossState.ScepterRecovery:
                Finish(.38f);
                break;
            case DemonCityBossState.WritGesture:
                CreateBindingWrit(dungeon);
                Start(DemonCityBossState.WritCommit, .16f, AttackKind);
                break;
            case DemonCityBossState.WritCommit:
                Start(DemonCityBossState.WritRecovery, .62f, AttackKind);
                break;
            case DemonCityBossState.WritRecovery:
                Finish(.44f);
                break;
            case DemonCityBossState.StepPlant:
                Start(DemonCityBossState.StepDistort, .34f, AttackKind);
                break;
            case DemonCityBossState.StepDistort:
                Position = StepDestination;
                Start(DemonCityBossState.StepRecovery, .64f, AttackKind);
                break;
            case DemonCityBossState.StepRecovery:
                Finish(.55f);
                break;
            case DemonCityBossState.VerdictPlant:
                CreateVerdictSeals(dungeon);
                Start(DemonCityBossState.VerdictSequence, 1.48f, AttackKind);
                break;
            case DemonCityBossState.VerdictSequence:
                ReleaseJudgmentBolt(empowered: true);
                Start(DemonCityBossState.VerdictRelease, .14f, AttackKind);
                break;
            case DemonCityBossState.VerdictRelease:
                Start(DemonCityBossState.VerdictRecovery, 1.05f, AttackKind);
                break;
            case DemonCityBossState.VerdictRecovery:
                Finish(.62f);
                break;
        }
    }

    private void FinishOrFollowup()
    {
        if (_followupQueued)
        {
            _followupQueued = false;
            Start(DemonCityBossState.FollowupTell, .38f,
                DemonCityBossAttackKind.DelayedCleave);
        }
        else
        {
            Start(DemonCityBossState.Reposition, .24f,
                DemonCityBossAttackKind.None);
        }
    }

    private void BeginMagistrateStep(PlayerCharacter player, DungeonMap dungeon)
    {
        _stepCooldown = BossCombatPhase switch
        {
            3 => 4.2f,
            2 => 4.8f,
            _ => 5.2f
        };
        float away = MathF.Sign(Position.X - player.Position.X);
        if (away == 0f)
            away = -_lockedDirection;
        float targetX = Position.X + away * 210f;
        DungeonRoom room = dungeon.FindRoomContaining(Position);
        Rectangle bounds = room?.Bounds ?? dungeon.WorldBounds;
        targetX = MathHelper.Clamp(targetX,
            bounds.Left + Size.X / 2f + 46f,
            bounds.Right - Size.X / 2f - 46f);
        StepDestination = SideScrollingCollision.ResolveSafeEntrySpawn(
            targetX, Size, dungeon, 0f, 0f);
        Start(DemonCityBossState.StepPlant, .44f,
            DemonCityBossAttackKind.MagistrateStep);
    }

    private void BeginFinalVerdict(PlayerCharacter player, DungeonMap dungeon)
    {
        _verdictCooldown = BossCombatPhase == 3 ? 7.2f : 8.8f;
        _lockedTarget = player.Position;
        _hazards?.RemoveOwnedBy(this);
        Start(DemonCityBossState.VerdictPlant, .84f,
            DemonCityBossAttackKind.FinalVerdict);
    }

    private void CreateDecree(DungeonMap dungeon)
    {
        Vector2 ground = GroundAt(_lockedTarget.X, dungeon);
        _hazards?.TryAddJudicialSeal(
            ground + new Vector2(0f, 7f),
            new Vector2(BossCombatPhase >= 2 ? 142f : 122f, 42f),
            .86f, .62f, Math.Max(1, EffectiveAttackDamage * 3 / 4),
            .78f, .28f, RoomId, this,
            RootHazardVisualKind.DecreeZone, dungeon);
        if (BossCombatPhase >= 2)
        {
            Vector2 chainedGround = GroundAt(
                _lockedTarget.X + _lockedDirection * 168f, dungeon);
            _hazards?.TryAddJudicialSeal(
                chainedGround + new Vector2(0f, 7f),
                new Vector2(112f, 40f),
                1.08f, .54f,
                Math.Max(1, EffectiveAttackDamage * 2 / 3),
                .82f, .24f, RoomId, this,
                RootHazardVisualKind.DecreeZone, dungeon);
        }
    }

    private void CreateBindingWrit(DungeonMap dungeon)
    {
        Vector2 ground = GroundAt(_lockedTarget.X, dungeon);
        _hazards?.TryAddJudicialSeal(
            ground + new Vector2(0f, 5f), new Vector2(104f, 34f),
            .78f, .50f, 0, .58f, .34f, RoomId, this,
            RootHazardVisualKind.BindingWrit, dungeon);
    }

    private void CreateVerdictSeals(DungeonMap dungeon)
    {
        Vector2 center = GroundAt(_lockedTarget.X, dungeon);
        float width = BossCombatPhase == 3 ? 112f : 96f;
        for (int index = 0; index < 3; index++)
        {
            float x = center.X + (index - 1) * 220f;
            Vector2 ground = GroundAt(x, dungeon);
            _hazards?.TryAddJudicialSeal(
                ground + new Vector2(0f, 6f), new Vector2(width, 44f),
                .82f + index * .20f, BossCombatPhase == 3 ? .38f : .32f,
                Math.Max(1, EffectiveAttackDamage), .72f, .26f,
                RoomId, this, RootHazardVisualKind.FinalVerdictSeal, dungeon);
        }
    }

    private void ReleaseJudgmentBolt(bool empowered)
    {
        if (_shotReleased || _projectiles == null)
            return;
        Vector2 origin = Position + new Vector2(
            _lockedDirection * 28f, -Size.Y * .30f);
        Vector2 direction = _lockedTarget - origin;
        _projectiles.SpawnJudgmentBolt(
            origin,
            direction,
            empowered
                ? Math.Max(1, EffectiveAttackDamage * 5 / 4)
                : Math.Max(1, EffectiveAttackDamage * 4 / 5),
            RoomId,
            empowered);
        _shotReleased = true;
    }

    private Vector2 GroundAt(float x, DungeonMap dungeon)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);
        Rectangle bounds = room?.Bounds ?? dungeon.WorldBounds;
        float safeX = MathHelper.Clamp(x,
            bounds.Left + 70f, bounds.Right - 70f);
        return SideScrollingCollision.ResolveSafeEntrySpawn(
            safeX, new Vector2(40f, 20f), dungeon, 0f, 0f);
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap dungeon)
    {
        if (_hitConsumed || !ActiveAttackArea.Intersects(player.Bounds))
            return;
        int damage = EffectiveAttackDamage;
        bool unblockable = false;
        float push = 0f;
        if (CombatState == DemonCityBossState.ChopImpact)
            damage = (damage * 5 + 3) / 4;
        else if (CombatState == DemonCityBossState.StompImpact)
        {
            damage = damage * 3 / 4;
            push = 34f;
        }
        else if (CombatState == DemonCityBossState.DragAdvance)
        {
            damage = damage * 3 / 5;
            push = 18f;
        }

        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, damage), Position, blockable: !unblockable,
            unblockable, ActiveAttackArea));
        if (push > 0f && result is (AttackResolution.Damaged or
            AttackResolution.GuardBroken))
        {
            player.PushAwayFrom(Position, push, dungeon);
        }
        if (result == AttackResolution.PerfectGuard)
            ApplyPoiseDamage(MaxPoise * .24f);
        _hitConsumed = true;
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset)
    {
        int y = (int)Position.Y + yOffset;
        return _lockedDirection < 0f
            ? new Rectangle(Bounds.Left - reach + 18, y, reach, height)
            : new Rectangle(Bounds.Right - 18, y, reach, height);
    }

    private void MoveLocked(float scale, float elapsed, DungeonMap dungeon)
    {
        Vector2 desired = new(
            _lockedDirection * EffectiveMovementSpeed * scale * elapsed, 0f);
        Vector2 resolved = SideScrollingCollision.ResolveDisplacement(
            Position, desired, Size, dungeon);
        if (SideScrollingCollision.IsSupported(resolved, Size, dungeon))
            MoveBy(resolved - Position, dungeon);
    }

    private void LockDirection(Vector2 target)
    {
        float offset = target.X - Position.X;
        if (MathF.Abs(offset) > 2f)
            _lockedDirection = MathF.Sign(offset);
        FaceToward(target);
    }

    private void Start(
        DemonCityBossState state,
        float duration,
        DemonCityBossAttackKind attack)
    {
        CombatState = state;
        AttackKind = attack;
        _actionDuration = duration;
        _actionRemaining = duration;
        _hitConsumed = false;
        _shotReleased = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = DemonCityBossState.Ready;
        AttackKind = DemonCityBossAttackKind.None;
        _actionDuration = 0f;
        _actionRemaining = 0f;
        _hitConsumed = false;
        _shotReleased = false;
        float cadence = Type == EnemyType.AbyssalMagistrate
            ? BossCombatPhase switch
            {
                3 => .78f,
                2 => .88f,
                _ => 1f
            }
            : 1f;
        _combatCooldown = MathF.Max(_combatCooldown, cooldown * cadence);
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
        {
            if (!_deathRaised)
            {
                _deathRaised = true;
                _hazards?.RemoveOwnedBy(this);
                _projectiles?.ClearEnemyProjectilesInRoom(RoomId);
                BossTierCompletionCount++;
                BossTierDefeated?.Invoke(this);
            }
            CancelAction(0f);
            return;
        }
        if (IsStaggered && CombatState != DemonCityBossState.PhaseTransition)
            CancelAction(.64f);
    }

    protected override void OnCombatInterrupted()
    {
        if (CombatState != DemonCityBossState.PhaseTransition)
            CancelAction(.64f);
    }

    private void CancelAction(float cooldown)
    {
        _hazards?.RemoveOwnedBy(this);
        _followupQueued = false;
        Finish(cooldown);
    }
}
