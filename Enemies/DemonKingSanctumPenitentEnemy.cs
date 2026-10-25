using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum PenitentCombatState
{
    Ready, Reposition,
    PenitentSwingWindup, PenitentSwingActive, PenitentSwingRecovery,
    BurdenDropWindup, BurdenDropActive, BurdenDropRecovery,
    ChainLungeWindup, ChainLungeActive, ChainLungeRecovery,
    RepentanceBraceStart, RepentanceBraceActive, RepentanceBraceRecovery,
    AbyssScriptWrite, AbyssScriptRelease, AbyssScriptRecovery,
    CondemnedMarkWrite, CondemnedMarkRecovery,
    ForbiddenLineWrite, ForbiddenLineRecovery,
    EraseWindup, EraseActive, EraseRecovery
}

public enum PenitentAttackKind
{
    None, PenitentSwing, BurdenDrop, ChainLunge, RepentanceStagger,
    AbyssScript, CondemnedMark, ForbiddenLine, Erase
}

/// <summary>
/// Dedicated Map 7 pair for Pilgrim's Descent and the Altar approach.
/// Damage zones are explicit, local, finite, and owned by the Scribe.
/// </summary>
public sealed class DemonKingSanctumPenitentEnemy : Enemy
{
    public const float MarkTelegraphSeconds = .70f;
    public const float MarkActiveSeconds = .17f;
    public const float LineTelegraphSeconds = .82f;
    public const float LineActiveSeconds = .22f;
    public const float ForbiddenLineLength = 224f;

    private float _actionRemaining, _actionDuration;
    private float _combatCooldown = .45f, _specialCooldown,
        _defenseCooldown, _repositionCooldown;
    private float _lockedDirection = 1f;
    private Vector2 _lockedTarget;
    private bool _hitConsumed, _projectileReleased;
    private int _decisionSerial;
    private float _markTelegraphRemaining, _markActiveRemaining;
    private float _lineTelegraphRemaining, _lineActiveRemaining;
    private bool _markHitConsumed, _lineHitConsumed;
    private ProjectileManager _ownedProjectiles;

    public PenitentCombatState CombatState { get; private set; } =
        PenitentCombatState.Ready;
    public PenitentAttackKind AttackKind { get; private set; } =
        PenitentAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f ? 0f :
        Math.Clamp(1f - _actionRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool IsRepentanceBraced =>
        CombatState == PenitentCombatState.RepentanceBraceActive;
    public bool HasCondemnedMark =>
        _markTelegraphRemaining > 0f || _markActiveRemaining > 0f;
    public bool IsCondemnedMarkActive => _markActiveRemaining > 0f;
    public Vector2 CondemnedMarkPosition { get; private set; }
    public float MarkTelegraphRatio => Math.Clamp(
        1f - _markTelegraphRemaining / MarkTelegraphSeconds, 0f, 1f);
    public bool HasForbiddenLine =>
        _lineTelegraphRemaining > 0f || _lineActiveRemaining > 0f;
    public bool IsForbiddenLineActive => _lineActiveRemaining > 0f;
    public Vector2 ForbiddenLinePosition { get; private set; }
    public float LineTelegraphRatio => Math.Clamp(
        1f - _lineTelegraphRemaining / LineTelegraphSeconds, 0f, 1f);
    public float SpecialCooldownRemaining => _specialCooldown;

    public override bool UsesProjectileAttack => Type == EnemyType.AbyssScribe;
    public override bool IsCombatAttackTelegraphing => CombatState is
        PenitentCombatState.PenitentSwingWindup or
        PenitentCombatState.BurdenDropWindup or
        PenitentCombatState.ChainLungeWindup or
        PenitentCombatState.RepentanceBraceStart or
        PenitentCombatState.AbyssScriptWrite or
        PenitentCombatState.CondemnedMarkWrite or
        PenitentCombatState.ForbiddenLineWrite or
        PenitentCombatState.EraseWindup;
    public override bool IsCombatAttackActive => CombatState is
        PenitentCombatState.PenitentSwingActive or
        PenitentCombatState.BurdenDropActive or
        PenitentCombatState.ChainLungeActive or
        PenitentCombatState.AbyssScriptRelease or
        PenitentCombatState.EraseActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        PenitentCombatState.PenitentSwingActive => ForwardArea(102, 76, -66),
        PenitentCombatState.BurdenDropActive => new Rectangle(
            (int)Position.X - 55, Bounds.Bottom - 35, 110, 40),
        PenitentCombatState.ChainLungeActive => ForwardArea(92, 67, -61),
        PenitentCombatState.EraseActive => new Rectangle(
            (int)Position.X - 54, (int)Position.Y - 53, 108, 106),
        _ => Rectangle.Empty
    };

    protected override float DeathPresentationDurationSeconds => .88f;

    public DemonKingSanctumPenitentEnemy(
        EnemyType type, Vector2 position, int level, int worldTier, int roomId)
        : base(type, position, SizeFor(type), SpeedFor(type),
            type == EnemyType.PenitentHusk ? 620f : 770f,
            EnemyStatScaling.Health(BaseHealth(type),
                type == EnemyType.PenitentHusk ? 30 : 22, level, worldTier),
            EnemyStatScaling.Damage(BaseDamage(type), 2, level, worldTier),
            type == EnemyType.PenitentHusk ? 225 : 205,
            level, worldTier, false, roomId,
            type == EnemyType.PenitentHusk ? 104f : 56f, 0f)
    {
        if (type is not (EnemyType.PenitentHusk or EnemyType.AbyssScribe))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    public override void ReceiveDamage(int damage, float poiseDamage)
    {
        // The brace is deliberately temporary and reduces rather than ignores
        // poise damage, so heavy player actions can still break it.
        base.ReceiveDamage(damage,
            IsRepentanceBraced ? poiseDamage * .38f : poiseDamage);
    }

    protected override void UpdateBehavior(
        GameTime gameTime, PlayerCharacter player, DungeonMap dungeon,
        ProjectileManager projectiles, RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        _ownedProjectiles = projectiles;
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _combatCooldown = MathF.Max(0f, _combatCooldown - dt);
        _specialCooldown = MathF.Max(0f, _specialCooldown - dt);
        _defenseCooldown = MathF.Max(0f, _defenseCooldown - dt);
        _repositionCooldown = MathF.Max(0f, _repositionCooldown - dt);
        UpdateOwnedGlyphs(dt, player);

        if (CombatState != PenitentCombatState.Ready)
        {
            UpdateAction(dt, player, dungeon, projectiles);
            return;
        }
        if (Type == EnemyType.PenitentHusk)
            UpdateHusk(gameTime, player, dungeon);
        else
            UpdateScribe(gameTime, player, dungeon);
    }

    private void UpdateHusk(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 112f)
        {
            MoveToward(time, player.Position, 92f, map, .57f);
            return;
        }
        if (_combatCooldown > 0f)
            return;
        Lock(player.Position);
        int choice = _decisionSerial++;
        if (_specialCooldown <= 0f && choice % 5 == 4)
        {
            _specialCooldown = 5.8f;
            Start(PenitentCombatState.RepentanceBraceStart, .32f,
                PenitentAttackKind.RepentanceStagger);
        }
        else if (distance >= 72f && choice % 3 == 1)
            Start(PenitentCombatState.ChainLungeWindup, .48f,
                PenitentAttackKind.ChainLunge);
        else if (choice % 3 == 2)
            Start(PenitentCombatState.BurdenDropWindup, .68f,
                PenitentAttackKind.BurdenDrop);
        else
            Start(PenitentCombatState.PenitentSwingWindup, .54f,
                PenitentAttackKind.PenitentSwing);
    }

    private void UpdateScribe(GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance < 78f)
        {
            if (_defenseCooldown <= 0f)
            {
                Lock(player.Position);
                _defenseCooldown = 3.5f;
                Start(PenitentCombatState.EraseWindup, .28f,
                    PenitentAttackKind.Erase);
            }
            else if (_repositionCooldown <= 0f)
            {
                Lock(player.Position);
                _repositionCooldown = 2.8f;
                Start(PenitentCombatState.Reposition, .27f,
                    PenitentAttackKind.None);
            }
            return;
        }
        if (distance > 480f)
        {
            MoveToward(time, player.Position, 365f, map, .58f);
            return;
        }
        if (_combatCooldown > 0f || HasCondemnedMark || HasForbiddenLine)
            return;

        Lock(player.Position);
        _lockedTarget = player.Position + new Vector2(0f,
            player.Bounds.Bottom - player.Position.Y);
        int choice = _decisionSerial++;
        if (_specialCooldown <= 0f && choice % 5 == 4)
        {
            _specialCooldown = 5.4f;
            Start(PenitentCombatState.ForbiddenLineWrite, .76f,
                PenitentAttackKind.ForbiddenLine);
        }
        else if (choice % 3 == 1)
            Start(PenitentCombatState.CondemnedMarkWrite, .62f,
                PenitentAttackKind.CondemnedMark);
        else
            Start(PenitentCombatState.AbyssScriptWrite, .65f,
                PenitentAttackKind.AbyssScript);
    }

    private void UpdateAction(
        float dt, PlayerCharacter player, DungeonMap map,
        ProjectileManager projectiles)
    {
        if (CombatState == PenitentCombatState.Reposition)
            MoveBy(new Vector2(-_lockedDirection * EffectiveMovementSpeed *
                .64f * dt, 0f), map);
        else if (CombatState == PenitentCombatState.ChainLungeActive)
            MoveBy(new Vector2(_lockedDirection * EffectiveMovementSpeed *
                1.65f * dt, 0f), map);

        _actionRemaining -= dt;
        if (_actionRemaining <= 0f)
            Advance(projectiles, map);
        if (CombatState is PenitentCombatState.PenitentSwingActive or
            PenitentCombatState.BurdenDropActive or
            PenitentCombatState.ChainLungeActive or
            PenitentCombatState.EraseActive)
            ResolveContact(player, map);
    }

    private void Advance(ProjectileManager projectiles, DungeonMap map)
    {
        switch (CombatState)
        {
            case PenitentCombatState.Reposition: Finish(.62f); break;
            case PenitentCombatState.PenitentSwingWindup:
                Start(PenitentCombatState.PenitentSwingActive, .18f,
                    PenitentAttackKind.PenitentSwing); break;
            case PenitentCombatState.PenitentSwingActive:
                Start(PenitentCombatState.PenitentSwingRecovery, .72f,
                    PenitentAttackKind.PenitentSwing); break;
            case PenitentCombatState.PenitentSwingRecovery: Finish(.78f); break;
            case PenitentCombatState.BurdenDropWindup:
                Start(PenitentCombatState.BurdenDropActive, .15f,
                    PenitentAttackKind.BurdenDrop); break;
            case PenitentCombatState.BurdenDropActive:
                Start(PenitentCombatState.BurdenDropRecovery, .94f,
                    PenitentAttackKind.BurdenDrop); break;
            case PenitentCombatState.BurdenDropRecovery: Finish(.92f); break;
            case PenitentCombatState.ChainLungeWindup:
                Start(PenitentCombatState.ChainLungeActive, .24f,
                    PenitentAttackKind.ChainLunge); break;
            case PenitentCombatState.ChainLungeActive:
                Start(PenitentCombatState.ChainLungeRecovery, .68f,
                    PenitentAttackKind.ChainLunge); break;
            case PenitentCombatState.ChainLungeRecovery: Finish(.84f); break;
            case PenitentCombatState.RepentanceBraceStart:
                Start(PenitentCombatState.RepentanceBraceActive, .76f,
                    PenitentAttackKind.RepentanceStagger); break;
            case PenitentCombatState.RepentanceBraceActive:
                Start(PenitentCombatState.RepentanceBraceRecovery, .42f,
                    PenitentAttackKind.RepentanceStagger); break;
            case PenitentCombatState.RepentanceBraceRecovery: Finish(.66f); break;
            case PenitentCombatState.AbyssScriptWrite:
                Start(PenitentCombatState.AbyssScriptRelease, .11f,
                    PenitentAttackKind.AbyssScript);
                ReleaseScript(projectiles, map); break;
            case PenitentCombatState.AbyssScriptRelease:
                Start(PenitentCombatState.AbyssScriptRecovery, .66f,
                    PenitentAttackKind.AbyssScript); break;
            case PenitentCombatState.AbyssScriptRecovery: Finish(.96f); break;
            case PenitentCombatState.CondemnedMarkWrite:
                CreateCondemnedMark();
                Start(PenitentCombatState.CondemnedMarkRecovery, .68f,
                    PenitentAttackKind.CondemnedMark); break;
            case PenitentCombatState.CondemnedMarkRecovery: Finish(.92f); break;
            case PenitentCombatState.ForbiddenLineWrite:
                CreateForbiddenLine();
                Start(PenitentCombatState.ForbiddenLineRecovery, .76f,
                    PenitentAttackKind.ForbiddenLine); break;
            case PenitentCombatState.ForbiddenLineRecovery: Finish(1.02f); break;
            case PenitentCombatState.EraseWindup:
                Start(PenitentCombatState.EraseActive, .14f,
                    PenitentAttackKind.Erase); break;
            case PenitentCombatState.EraseActive:
                Start(PenitentCombatState.EraseRecovery, .52f,
                    PenitentAttackKind.Erase); break;
            case PenitentCombatState.EraseRecovery: Finish(.82f); break;
        }
    }

    private void ReleaseScript(ProjectileManager projectiles, DungeonMap map)
    {
        if (_projectileReleased || projectiles == null || !IsAlive)
            return;
        Vector2 origin = Position + new Vector2(_lockedDirection * 25f,
            -Size.Y * .42f);
        Vector2 target = _lockedTarget + new Vector2(0f, -34f);
        if (!ClearLine(origin, target, map))
            return;
        _projectileReleased = true;
        projectiles.SpawnAbyssScript(origin, target - origin,
            EffectiveAttackDamage, RoomId, owner: this);
    }

    private void CreateCondemnedMark()
    {
        CondemnedMarkPosition = _lockedTarget;
        _markTelegraphRemaining = MarkTelegraphSeconds;
        _markActiveRemaining = 0f;
        _markHitConsumed = false;
    }

    private void CreateForbiddenLine()
    {
        ForbiddenLinePosition = _lockedTarget;
        _lineTelegraphRemaining = LineTelegraphSeconds;
        _lineActiveRemaining = 0f;
        _lineHitConsumed = false;
    }

    private void UpdateOwnedGlyphs(float dt, PlayerCharacter player)
    {
        if (_markTelegraphRemaining > 0f)
        {
            _markTelegraphRemaining = MathF.Max(0f,
                _markTelegraphRemaining - dt);
            if (_markTelegraphRemaining <= 0f && IsAlive)
                _markActiveRemaining = MarkActiveSeconds;
        }
        else if (_markActiveRemaining > 0f)
        {
            _markActiveRemaining = MathF.Max(0f, _markActiveRemaining - dt);
            Rectangle area = new((int)CondemnedMarkPosition.X - 22,
                (int)CondemnedMarkPosition.Y - 118, 44, 122);
            ResolveGlyphContact(player, area, .82f, ref _markHitConsumed);
        }

        if (_lineTelegraphRemaining > 0f)
        {
            _lineTelegraphRemaining = MathF.Max(0f,
                _lineTelegraphRemaining - dt);
            if (_lineTelegraphRemaining <= 0f && IsAlive)
                _lineActiveRemaining = LineActiveSeconds;
        }
        else if (_lineActiveRemaining > 0f)
        {
            _lineActiveRemaining = MathF.Max(0f, _lineActiveRemaining - dt);
            Rectangle area = new(
                (int)(ForbiddenLinePosition.X - ForbiddenLineLength * .5f),
                (int)ForbiddenLinePosition.Y - 26,
                (int)ForbiddenLineLength, 30);
            ResolveGlyphContact(player, area, .72f, ref _lineHitConsumed);
        }
    }

    private void ResolveGlyphContact(
        PlayerCharacter player, Rectangle area, float multiplier,
        ref bool consumed)
    {
        if (consumed || (!area.Intersects(player.BodyHurtbox) &&
            !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))))
            return;
        consumed = true;
        player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap map)
    {
        Rectangle area = ActiveAttackArea;
        if (_hitConsumed || area.IsEmpty ||
            (!area.Intersects(player.BodyHurtbox) &&
             !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))))
            return;
        _hitConsumed = true;
        float multiplier = CombatState switch
        {
            PenitentCombatState.BurdenDropActive => 1.30f,
            PenitentCombatState.ChainLungeActive => 1.08f,
            PenitentCombatState.EraseActive => .46f,
            _ => 1f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset) => new(
        _lockedDirection < 0f ? Bounds.Left - reach : Bounds.Right,
        (int)Position.Y + yOffset, reach, height);

    private void Lock(Vector2 target)
    {
        FaceToward(target);
        _lockedDirection = target.X < Position.X ? -1f : 1f;
    }

    private void Start(
        PenitentCombatState state, float duration, PenitentAttackKind kind)
    {
        CombatState = state;
        AttackKind = kind;
        _actionDuration = MathF.Max(.01f, duration);
        _actionRemaining = _actionDuration;
        _hitConsumed = false;
        if (state != PenitentCombatState.AbyssScriptRelease)
            _projectileReleased = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = PenitentCombatState.Ready;
        AttackKind = PenitentAttackKind.None;
        _actionRemaining = _actionDuration = 0f;
        _hitConsumed = _projectileReleased = false;
        _combatCooldown = MathF.Max(_combatCooldown, cooldown);
    }

    private void Cancel(float cooldown)
    {
        Finish(cooldown);
        _markTelegraphRemaining = _markActiveRemaining = 0f;
        _lineTelegraphRemaining = _lineActiveRemaining = 0f;
        _markHitConsumed = _lineHitConsumed = true;
        _ownedProjectiles?.RemoveOwnedBy(this);
        Attack.Cancel();
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
            Cancel(0f);
    }

    protected override void OnCombatInterrupted() => Cancel(.72f);

    private static bool ClearLine(Vector2 origin, Vector2 target, DungeonMap map)
    {
        if (map == null) return false;
        Vector2 delta = target - origin;
        int steps = Math.Max(2, (int)MathF.Ceiling(delta.Length() / 8f));
        for (int step = 2; step < steps - 2; step++)
        {
            Vector2 point = origin + delta * (step / (float)steps);
            foreach (World.Platform platform in map.Platforms)
                if (platform.Bounds.Contains(point.ToPoint())) return false;
        }
        return true;
    }

    private static Vector2 SizeFor(EnemyType type) =>
        type == EnemyType.PenitentHusk
            ? new Vector2(62f, 98f)
            : new Vector2(45f, 91f);
    private static float SpeedFor(EnemyType type) =>
        type == EnemyType.PenitentHusk ? 67f : 73f;
    private static int BaseHealth(EnemyType type) =>
        type == EnemyType.PenitentHusk ? 292 : 184;
    private static int BaseDamage(EnemyType type) =>
        type == EnemyType.PenitentHusk ? 24 : 21;
}
