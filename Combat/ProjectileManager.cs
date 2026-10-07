using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Combat;

public sealed class ProjectileManager
{
    public const float ArrowSpeed = 330f;
    public const float WebSlowMultiplier = 0.58f;
    public const float WebShotSlowDurationSeconds = 2.5f;
    public const float WebPatchLifetimeSeconds = 4.5f;

    private const float ActiveDistance = 420f;
    private const float PatchRefreshDurationSeconds = 0.35f;

    private readonly List<Projectile> _projectiles = new(32);
    private readonly List<WebPatch> _webPatches = new(16);

    public IReadOnlyList<Projectile> Projectiles => _projectiles;
    public IReadOnlyList<WebPatch> WebPatches => _webPatches;

    public void SpawnArrow(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.Arrow,
            position,
            NormalizeOrDefault(direction) * ArrowSpeed,
            new Vector2(16f, 7f),
            lifetimeSeconds: 2.4f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId));
    }

    public void SpawnPlayerProjectile(
        ProjectileDefinition definition,
        Vector2 position,
        Vector2 direction,
        float speedMultiplier,
        int damage,
        float poiseDamage,
        float knockback,
        int roomId)
    {
        if (definition == null)
            return;

        _projectiles.Add(new Projectile(
            definition.Type,
            position,
            NormalizeOrDefault(direction) * definition.Speed *
                MathF.Max(.1f, speedMultiplier),
            definition.Size,
            definition.LifetimeSeconds,
            damage,
            blockable: false,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            isPlayerOwned: true,
            poiseDamage,
            knockback));
    }

    public void SpawnWebShot(
        Vector2 position,
        Vector2 direction,
        int roomId)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.WebShot,
            position,
            NormalizeOrDefault(direction) * 245f,
            new Vector2(14f, 14f),
            lifetimeSeconds: 2.2f,
            damage: 0,
            blockable: false,
            unblockable: false,
            WebShotSlowDurationSeconds,
            WebSlowMultiplier,
            roomId));
    }

    public bool TryCreateWebPatch(
        Vector2 position,
        int roomId,
        DungeonMap dungeon)
    {
        var patch = new WebPatch(
            position,
            WebPatchLifetimeSeconds,
            roomId);

        if (dungeon.FindRoomContaining(position) == null ||
            !DungeonCollision.IsWalkable(patch.Bounds, dungeon))
            return false;

        _webPatches.Add(patch);
        return true;
    }

    public void Update(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            Projectile projectile = _projectiles[index];
            projectile.LifetimeRemaining -= elapsedSeconds;

            if (projectile.LifetimeRemaining <= 0f)
            {
                _projectiles.RemoveAt(index);
                continue;
            }

            if (!projectile.IsPlayerOwned &&
                !IsNearPlayer(projectile.Position, player.Position))
                continue;

            Vector2 desiredPosition = projectile.Position +
                projectile.Velocity * elapsedSeconds;
            Vector2 resolvedPosition = DungeonCollision.ResolveMovement(
                projectile.Position,
                desiredPosition,
                projectile.Size,
                dungeon);

            if (Vector2.DistanceSquared(resolvedPosition, desiredPosition) > 0.25f)
            {
                _projectiles.RemoveAt(index);
                continue;
            }

            projectile.Position = resolvedPosition;

            if (projectile.IsPlayerOwned)
                continue;

            bool intersectsBody = projectile.Bounds.Intersects(
                player.BodyHurtbox);
            bool intersectsDefense = projectile.Blockable &&
                player.Combat.IsBlocking &&
                projectile.Bounds.Intersects(player.DefenseBounds);

            if (!intersectsBody && !intersectsDefense)
                continue;

            AttackResolution resolution = AttackResolution.Ignored;

            if (projectile.Damage > 0)
            {
                resolution = player.ReceiveProjectileAttack(
                    new AttackContact(
                        projectile.Damage,
                        projectile.SourcePosition,
                        projectile.Blockable,
                        projectile.Unblockable,
                        projectile.Bounds));

                if (resolution == AttackResolution.Ignored &&
                    !intersectsBody)
                {
                    continue;
                }
            }

            if (intersectsBody && projectile.SlowDurationSeconds > 0f)
            {
                player.ApplySlow(
                    projectile.SlowMovementMultiplier,
                    projectile.SlowDurationSeconds);
            }

            _projectiles.RemoveAt(index);
        }

        for (int index = _webPatches.Count - 1; index >= 0; index--)
        {
            WebPatch patch = _webPatches[index];
            patch.LifetimeRemaining -= elapsedSeconds;

            if (patch.LifetimeRemaining <= 0f)
            {
                _webPatches.RemoveAt(index);
                continue;
            }

            if (IsNearPlayer(patch.Position, player.Position) &&
                patch.Bounds.Intersects(player.Bounds))
            {
                player.ApplySlow(
                    WebSlowMultiplier,
                    PatchRefreshDurationSeconds);
            }
        }
    }

    public void Clear()
    {
        _projectiles.Clear();
        _webPatches.Clear();
    }

    private static bool IsNearPlayer(Vector2 position, Vector2 playerPosition)
    {
        return Vector2.DistanceSquared(position, playerPosition) <=
            ActiveDistance * ActiveDistance;
    }

    private static Vector2 NormalizeOrDefault(Vector2 direction)
    {
        if (direction == Vector2.Zero)
            return Vector2.UnitX;

        direction.Normalize();
        return direction;
    }
}
