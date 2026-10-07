using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum GiantSpiderWebState
{
    Ready,
    Prepare,
    Shoot,
    Recovery
}

public sealed class GiantSpider : Enemy
{
    public const float WebShotCooldownSeconds = 3.1f;
    public const float WebPatchCooldownSeconds = 6.2f;
    public const float PreferredMinimumRange = 82f;
    public const float PreferredMaximumRange = 155f;
    public const float WebPrepareSeconds = 0.36f;
    public const float WebShootSeconds = 0.07f;
    public const float WebRecoverySeconds = 0.25f;

    private float _webShotCooldownRemaining = 1.1f;
    private float _webPatchCooldownRemaining = 2.8f;
    private float _webPhaseTimeRemaining;
    private bool _webProjectilePending;

    public GiantSpiderWebState WebState { get; private set; } =
        GiantSpiderWebState.Ready;
    public float WebPhaseProgress
    {
        get
        {
            float duration = WebState switch
            {
                GiantSpiderWebState.Prepare => WebPrepareSeconds,
                GiantSpiderWebState.Shoot => WebShootSeconds,
                GiantSpiderWebState.Recovery => WebRecoverySeconds,
                _ => 0f
            };

            return duration <= 0f
                ? 0f
                : Math.Clamp(
                    1f - _webPhaseTimeRemaining / duration,
                    0f,
                    1f);
        }
    }

    public GiantSpider(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.GiantSpider,
            position,
            new Vector2(50f, 36f),
            movementSpeed: 96f,
            detectionRange: 265f,
            GetScaledStat(110, 22, level, worldTier, health: true),
            GetScaledStat(11, 2, level, worldTier, health: false),
            experienceReward: 75,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 48f,
            attackCooldownSeconds: 1.15f)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _webShotCooldownRemaining = MathF.Max(
            0f,
            _webShotCooldownRemaining - elapsedSeconds);
        _webPatchCooldownRemaining = MathF.Max(
            0f,
            _webPatchCooldownRemaining - elapsedSeconds);
        UpdateWebPhase(elapsedSeconds);
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
        float distanceSquared = toPlayer.LengthSquared();
        float distance = MathF.Sqrt(distanceSquared);

        if (_webProjectilePending)
        {
            Vector2 releasePosition = GetWebReleasePosition();
            projectiles.SpawnWebShot(
                releasePosition,
                player.Position - releasePosition,
                RoomId);
            _webProjectilePending = false;
        }

        if (WebState != GiantSpiderWebState.Ready)
            return;

        if (_webShotCooldownRemaining <= 0f)
        {
            WebState = GiantSpiderWebState.Prepare;
            _webPhaseTimeRemaining = WebPrepareSeconds;
            _webShotCooldownRemaining = WebShotCooldownSeconds;
            return;
        }

        if (_webPatchCooldownRemaining <= 0f)
        {
            bool patchCreated = projectiles.TryCreateWebPatch(
                player.Position,
                RoomId,
                dungeon);
            _webPatchCooldownRemaining = patchCreated
                ? WebPatchCooldownSeconds
                : 1f;
        }

        if (distance <= Attack.Range)
        {
            TryMeleeAttack(player);

            return;
        }

        if (distance < PreferredMinimumRange)
            MoveAway(gameTime, player.Position, dungeon);
        else if (distance > PreferredMaximumRange)
            MoveToward(
                gameTime,
                player.Position,
                PreferredMaximumRange,
                dungeon);
    }

    public Vector2 GetWebReleasePosition()
    {
        float direction = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        return Position + new Vector2(direction * 28f, -6f);
    }

    protected override void OnDamaged()
    {
        if (!IsAlive || IsStaggered)
        {
            WebState = GiantSpiderWebState.Ready;
            _webPhaseTimeRemaining = 0f;
            _webProjectilePending = false;
        }
    }

    private void UpdateWebPhase(float elapsedSeconds)
    {
        if (WebState == GiantSpiderWebState.Ready)
            return;

        _webPhaseTimeRemaining -= elapsedSeconds;

        while (_webPhaseTimeRemaining <= 0f &&
               WebState != GiantSpiderWebState.Ready)
        {
            float overflow = -_webPhaseTimeRemaining;

            switch (WebState)
            {
                case GiantSpiderWebState.Prepare:
                    WebState = GiantSpiderWebState.Shoot;
                    _webPhaseTimeRemaining = WebShootSeconds - overflow;
                    _webProjectilePending = true;
                    break;
                case GiantSpiderWebState.Shoot:
                    WebState = GiantSpiderWebState.Recovery;
                    _webPhaseTimeRemaining = WebRecoverySeconds - overflow;
                    break;
                default:
                    WebState = GiantSpiderWebState.Ready;
                    _webPhaseTimeRemaining = 0f;
                    break;
            }
        }
    }

    private static int GetScaledStat(
        int baseValue,
        int perLevel,
        int level,
        int worldTier,
        bool health)
    {
        int value = baseValue + (Math.Max(1, level) - 1) * perLevel;
        int percent = health
            ? WorldProgression.GetHealthMultiplierPercent(worldTier)
            : WorldProgression.GetDamageMultiplierPercent(worldTier);
        return WorldProgression.ApplyPercent(value, percent);
    }
}
