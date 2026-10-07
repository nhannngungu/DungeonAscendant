using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class MotherSpider : Enemy
{
    public const int MaximumActiveSpiderlings = 3;
    public const float SummonCooldownSeconds = 6.5f;
    public const float WebPrepareSeconds = 0.42f;
    public const float WebShootSeconds = 0.08f;
    public const float WebRecoverySeconds = 0.30f;
    public const float SummonWindupSeconds = 0.55f;
    public const float SummonActiveSeconds = 0.18f;
    public const float SummonRecoverySeconds = 0.35f;

    private float _webShotCooldownRemaining = 0.9f;
    private float _webPatchCooldownRemaining = 2.2f;
    private float _summonCooldownRemaining = 3f;
    private float _specialStateTimeRemaining;
    private bool _webProjectilePending;
    private bool _summonPending;

    public MotherSpiderSpecialState SpecialState { get; private set; }
    public float SpecialStateProgress
    {
        get
        {
            float duration = SpecialState switch
            {
                MotherSpiderSpecialState.WebPrepare => WebPrepareSeconds,
                MotherSpiderSpecialState.WebShoot => WebShootSeconds,
                MotherSpiderSpecialState.WebRecovery => WebRecoverySeconds,
                MotherSpiderSpecialState.SummonWindup => SummonWindupSeconds,
                MotherSpiderSpecialState.Summon => SummonActiveSeconds,
                MotherSpiderSpecialState.SummonRecovery =>
                    SummonRecoverySeconds,
                _ => 0f
            };
            return duration <= 0f
                ? 0f
                : Math.Clamp(
                    1f - _specialStateTimeRemaining / duration,
                    0f,
                    1f);
        }
    }

    public MotherSpider(
        Vector2 position,
        int level,
        int roomId,
        int worldTier)
        : base(
            EnemyType.MotherSpider,
            position,
            new Vector2(72f, 54f),
            movementSpeed: 82f,
            detectionRange: 285f,
            EnemyStatScaling.Health(260, 45, level, worldTier),
            EnemyStatScaling.Damage(16, 3, level, worldTier),
            experienceReward: 220,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: 60f,
            attackCooldownSeconds: 1.2f,
            applyEliteModifiers: false)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _webShotCooldownRemaining = MathF.Max(0f, _webShotCooldownRemaining - elapsedSeconds);
        _webPatchCooldownRemaining = MathF.Max(0f, _webPatchCooldownRemaining - elapsedSeconds);
        _summonCooldownRemaining = MathF.Max(0f, _summonCooldownRemaining - elapsedSeconds);
        UpdateSpecialState(elapsedSeconds);
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (!IsPlayerDetected(player.Position))
            return;

        Vector2 toPlayer = player.Position - Position;
        float distance = MathF.Sqrt(toPlayer.LengthSquared());

        if (_webProjectilePending)
        {
            Vector2 releasePosition = GetWebReleasePosition();
            projectiles.SpawnWebShot(
                releasePosition,
                player.Position - releasePosition,
                RoomId,
                empoweredVisual: true);
            _webProjectilePending = false;
        }

        if (_summonPending && enemies != null)
        {
            _summonCooldownRemaining = enemies.RequestSpiderlingSummon(this)
                ? SummonCooldownSeconds
                : 1f;
            _summonPending = false;
        }

        if (SpecialState != MotherSpiderSpecialState.Ready)
            return;

        if (_summonCooldownRemaining <= 0f && enemies != null)
        {
            EnterSpecialState(
                MotherSpiderSpecialState.SummonWindup,
                SummonWindupSeconds);
            return;
        }

        if (_webShotCooldownRemaining <= 0f)
        {
            EnterSpecialState(
                MotherSpiderSpecialState.WebPrepare,
                WebPrepareSeconds);
            _webShotCooldownRemaining = 2.8f;
            return;
        }

        if (_webPatchCooldownRemaining <= 0f)
        {
            bool created = projectiles.TryCreateWebPatch(
                player.Position,
                RoomId,
                dungeon);
            _webPatchCooldownRemaining = created ? 5.4f : 1f;
        }

        if (distance <= Attack.Range)
        {
            TryMeleeAttack(player);
        }
        else if (distance < 95f)
        {
            MoveAway(gameTime, player.Position, dungeon);
        }
        else if (distance > 170f)
        {
            MoveToward(gameTime, player.Position, 170f, dungeon);
        }
    }

    public Vector2 GetWebReleasePosition()
    {
        float direction = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        return Position + new Vector2(direction * 39f, -7f);
    }

    public Vector2 GetSummonVisualOrigin()
    {
        float direction = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        return Position + new Vector2(-direction * 30f, 18f);
    }

    protected override void OnDamaged()
    {
        if (!IsAlive || IsStaggered)
        {
            SpecialState = MotherSpiderSpecialState.Ready;
            _specialStateTimeRemaining = 0f;
            _webProjectilePending = false;
            _summonPending = false;
        }
    }

    private void UpdateSpecialState(float elapsedSeconds)
    {
        if (SpecialState == MotherSpiderSpecialState.Ready)
            return;

        _specialStateTimeRemaining -= elapsedSeconds;

        while (_specialStateTimeRemaining <= 0f &&
               SpecialState != MotherSpiderSpecialState.Ready)
        {
            float overflow = -_specialStateTimeRemaining;

            switch (SpecialState)
            {
                case MotherSpiderSpecialState.WebPrepare:
                    EnterSpecialState(
                        MotherSpiderSpecialState.WebShoot,
                        WebShootSeconds - overflow);
                    _webProjectilePending = true;
                    break;
                case MotherSpiderSpecialState.WebShoot:
                    EnterSpecialState(
                        MotherSpiderSpecialState.WebRecovery,
                        WebRecoverySeconds - overflow);
                    break;
                case MotherSpiderSpecialState.SummonWindup:
                    EnterSpecialState(
                        MotherSpiderSpecialState.Summon,
                        SummonActiveSeconds - overflow);
                    _summonPending = true;
                    break;
                case MotherSpiderSpecialState.Summon:
                    EnterSpecialState(
                        MotherSpiderSpecialState.SummonRecovery,
                        SummonRecoverySeconds - overflow);
                    break;
                default:
                    EnterSpecialState(MotherSpiderSpecialState.Ready, 0f);
                    break;
            }
        }
    }

    private void EnterSpecialState(
        MotherSpiderSpecialState state,
        float durationSeconds)
    {
        SpecialState = state;
        _specialStateTimeRemaining = MathF.Max(0f, durationSeconds);
    }
}

public enum MotherSpiderSpecialState
{
    Ready,
    WebPrepare,
    WebShoot,
    WebRecovery,
    SummonWindup,
    Summon,
    SummonRecovery
}
