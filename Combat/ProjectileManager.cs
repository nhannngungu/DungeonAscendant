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
    private readonly List<ProjectileImpact> _impacts = new(8);
    private float _pendingCursePressure;

    public IReadOnlyList<Projectile> Projectiles => _projectiles;
    public IReadOnlyList<WebPatch> WebPatches => _webPatches;
    public IReadOnlyList<ProjectileImpact> Impacts => _impacts;

    public void SpawnArrow(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId)
    {
        SpawnEnemyArrow(position, direction, damage, roomId, heavy: false);
    }

    public void SpawnEnemyArrow(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        bool heavy)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.Arrow,
            position,
            NormalizeOrDefault(direction) * ArrowSpeed * (heavy ? 1.16f : 1f),
            heavy ? new Vector2(20f, 8f) : new Vector2(16f, 7f),
            lifetimeSeconds: 2.4f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            empoweredVisual: heavy));
    }

    public void SpawnPlayerProjectile(
        ProjectileDefinition definition,
        Vector2 position,
        Vector2 direction,
        float speedMultiplier,
        int damage,
        float poiseDamage,
        float knockback,
        int roomId,
        WeaponTechniqueEffect techniqueEffect,
        int weaponResourceGain,
        float chargeRatio = 0f,
        float focusRatioAtFire = 0f,
        RangerDrawState drawState = RangerDrawState.Quick,
        int sourceAttackId = -1,
        int techniqueUseId = -1,
        int spreadArrowIndex = -1,
        int additionalPierces = 0,
        float placementDistance = 0f)
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
            knockback,
            techniqueEffect: techniqueEffect,
            weaponResourceGain: weaponResourceGain,
            trajectory: definition.Trajectory,
            gravity: definition.Gravity,
            maximumRicochets: definition.MaximumRicochets,
            remainingPierces: definition.RemainingPierces +
                Math.Max(0, additionalPierces),
            impactRadius: definition.ImpactRadius,
            splitCount: definition.SplitCount,
            penetratesTerrain: definition.PenetratesTerrain,
            chargeRatio: chargeRatio,
            focusRatioAtFire: focusRatioAtFire,
            drawState: drawState,
            sourceAttackId: sourceAttackId,
            techniqueUseId: techniqueUseId,
            spreadArrowIndex: spreadArrowIndex,
            isRangerProjectile: definition.Type == ProjectileType.Arrow,
            placementDistance: placementDistance));
    }

    public void SpawnWebShot(
        Vector2 position,
        Vector2 direction,
        int roomId,
        bool empoweredVisual = false)
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
            roomId,
            empoweredVisual: empoweredVisual));
    }

    public void SpawnSoulBolt(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.SoulBolt,
            position,
            NormalizeOrDefault(direction) * 215f,
            new Vector2(15f, 13f),
            lifetimeSeconds: 2.8f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            cursePressure: 7f));
    }

    public float ConsumeEnemyCursePressure()
    {
        float pressure = _pendingCursePressure;
        _pendingCursePressure = 0f;
        return pressure;
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
        _impacts.Clear();

        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            Projectile projectile = _projectiles[index];
            projectile.PreviousPosition = projectile.Position;
            projectile.LifetimeRemaining -= elapsedSeconds;
            projectile.RicochetVisualTimeRemaining = MathF.Max(
                0f,
                projectile.RicochetVisualTimeRemaining - elapsedSeconds);
            projectile.SurfaceSeparationRemaining = MathF.Max(
                0f,
                projectile.SurfaceSeparationRemaining -
                    projectile.Velocity.Length() * elapsedSeconds);

            if (projectile.LifetimeRemaining <= 0f)
            {
                _projectiles.RemoveAt(index);
                continue;
            }

            if (!projectile.IsPlayerOwned &&
                !IsNearPlayer(projectile.Position, player.Position))
                continue;

            if (!MoveProjectile(projectile, elapsedSeconds, dungeon))
            {
                _projectiles.RemoveAt(index);
                continue;
            }

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

                if (resolution != AttackResolution.Ignored)
                    _pendingCursePressure += projectile.CursePressure;

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
        _impacts.Clear();
        _pendingCursePressure = 0f;
    }

    public void ClearEnemyThreats()
    {
        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            if (!_projectiles[index].IsPlayerOwned)
                _projectiles.RemoveAt(index);
        }

        for (int index = _impacts.Count - 1; index >= 0; index--)
        {
            if (!_impacts[index].Projectile.IsPlayerOwned)
                _impacts.RemoveAt(index);
        }

        _webPatches.Clear();
        _pendingCursePressure = 0f;
    }

    public void SpawnArrowRainAtImpact(Projectile projectile, Vector2 impact)
    {
        if (projectile == null || projectile.HasSplit ||
            projectile.Trajectory != ProjectileTrajectoryType.ArrowRain)
            return;

        SpawnArrowRainChildren(projectile, impact);
        projectile.HasSplit = true;
    }

    private bool MoveProjectile(
        Projectile projectile,
        float elapsedSeconds,
        DungeonMap dungeon)
    {
        float distance = projectile.Velocity.Length() * elapsedSeconds;
        int steps = Math.Max(1, (int)MathF.Ceiling(distance / 6f));
        float stepSeconds = elapsedSeconds / steps;

        for (int step = 0; step < steps; step++)
        {
            float previousVerticalVelocity = projectile.Velocity.Y;
            if (projectile.Gravity > 0f)
            {
                projectile.Velocity += new Vector2(
                    0f,
                    projectile.Gravity * stepSeconds);
            }

            if (projectile.Trajectory == ProjectileTrajectoryType.Split &&
                !projectile.HasSplit && previousVerticalVelocity < 0f &&
                projectile.Velocity.Y >= 0f)
            {
                SpawnSplitChildren(projectile);
                projectile.HasSplit = true;
                return false;
            }

            Vector2 candidate = projectile.Position +
                projectile.Velocity * stepSeconds;

            if (SideScrollingCollision.IsPositionFree(
                candidate,
                projectile.Size,
                dungeon))
            {
                projectile.Position = candidate;
                continue;
            }

            if (projectile.PenetratesTerrain &&
                IsInsideWorld(candidate, projectile.Size, dungeon.WorldBounds))
            {
                projectile.Position = candidate;
                continue;
            }

            if (TryRicochet(projectile, candidate, dungeon))
                continue;

            if (projectile.Trajectory == ProjectileTrajectoryType.ArrowRain &&
                !projectile.HasSplit)
            {
                SpawnArrowRainChildren(projectile, projectile.Position);
                projectile.HasSplit = true;
            }

            if (projectile.ImpactRadius > 0f ||
                (!projectile.IsPlayerOwned &&
                 projectile.Type == ProjectileType.Arrow))
                _impacts.Add(new ProjectileImpact(projectile, projectile.Position));
            return false;
        }

        return true;
    }

    private static bool TryRicochet(
        Projectile projectile,
        Vector2 candidate,
        DungeonMap dungeon)
    {
        if (projectile.RicochetCount >= projectile.MaximumRicochets ||
            projectile.SurfaceSeparationRemaining > 0f)
        {
            return false;
        }

        Vector2 xCandidate = new(candidate.X, projectile.Position.Y);
        Vector2 yCandidate = new(projectile.Position.X, candidate.Y);
        bool hitX = !SideScrollingCollision.IsPositionFree(
            xCandidate,
            projectile.Size,
            dungeon);
        bool hitY = !SideScrollingCollision.IsPositionFree(
            yCandidate,
            projectile.Size,
            dungeon);
        Vector2 normal;

        if (hitX && hitY)
        {
            normal = Vector2.Normalize(new Vector2(
                -MathF.Sign(projectile.Velocity.X),
                -MathF.Sign(projectile.Velocity.Y)));
        }
        else if (hitX)
        {
            normal = new Vector2(-MathF.Sign(projectile.Velocity.X), 0f);
        }
        else
        {
            normal = new Vector2(0f, -MathF.Sign(projectile.Velocity.Y));
        }

        projectile.Velocity = Vector2.Reflect(projectile.Velocity, normal);
        projectile.RicochetCount++;
        projectile.LastRicochetPosition = projectile.Position;
        projectile.RicochetVisualTimeRemaining = .14f;
        projectile.SurfaceSeparationRemaining = 12f;
        Vector2 separation = NormalizeOrDefault(projectile.Velocity) * 3f;
        Vector2 separated = projectile.Position + separation;
        if (SideScrollingCollision.IsPositionFree(
            separated,
            projectile.Size,
            dungeon))
        {
            projectile.Position = separated;
        }
        return true;
    }

    private void SpawnSplitChildren(Projectile parent)
    {
        int count = Math.Clamp(parent.SplitCount, 1, 6);
        float speed = MathF.Max(280f, parent.Velocity.Length() * .82f);
        float facing = MathF.Sign(parent.Velocity.X);
        if (facing == 0f)
            facing = 1f;

        for (int index = 0; index < count; index++)
        {
            float spread = count == 1
                ? 0f
                : MathHelper.Lerp(-.52f, .52f, index / (float)(count - 1));
            Vector2 direction = Vector2.Normalize(new Vector2(
                facing * spread,
                1f));
            _projectiles.Add(new Projectile(
                ProjectileType.Arrow,
                parent.Position,
                direction * speed,
                new Vector2(13f, 5f),
                1.8f,
                Math.Max(1, (int)MathF.Round(parent.Damage * .48f)),
                blockable: false,
                unblockable: false,
                0f,
                1f,
                parent.RoomId,
                isPlayerOwned: true,
                parent.PoiseDamage * .45f,
                parent.Knockback * .45f,
                techniqueEffect: parent.TechniqueEffect,
                trajectory: ProjectileTrajectoryType.Ricochet,
                gravity: 520f,
                maximumRicochets: 1,
                chargeRatio: parent.ChargeRatio,
                focusRatioAtFire: parent.FocusRatioAtFire,
                sourceAttackId: parent.SourceAttackId,
                techniqueUseId: parent.TechniqueUseId,
                isRangerProjectile: true));
        }
    }

    private void SpawnArrowRainChildren(Projectile parent, Vector2 impact)
    {
        int count = Math.Clamp(parent.SplitCount, 1, 7);
        for (int index = 0; index < count; index++)
        {
            float ratio = count == 1 ? .5f : index / (float)(count - 1);
            float horizontalOffset = MathHelper.Lerp(-58f, 58f, ratio);
            Vector2 position = impact + new Vector2(
                horizontalOffset,
                -92f - (index % 2) * 12f);
            Vector2 direction = Vector2.Normalize(new Vector2(
                MathHelper.Lerp(-.18f, .18f, ratio),
                1f));
            _projectiles.Add(new Projectile(
                ProjectileType.Arrow,
                position,
                direction * 390f,
                new Vector2(13f, 5f),
                1.2f,
                Math.Max(1, (int)MathF.Round(parent.Damage * .34f)),
                blockable: false,
                unblockable: false,
                0f,
                1f,
                parent.RoomId,
                isPlayerOwned: true,
                parent.PoiseDamage * .30f,
                parent.Knockback * .25f,
                techniqueEffect: parent.TechniqueEffect,
                trajectory: ProjectileTrajectoryType.Ballistic,
                gravity: 240f,
                focusRatioAtFire: parent.FocusRatioAtFire,
                sourceAttackId: parent.SourceAttackId,
                techniqueUseId: parent.TechniqueUseId,
                spreadArrowIndex: index,
                isRangerProjectile: true));
        }
    }

    private static bool IsInsideWorld(
        Vector2 position,
        Vector2 size,
        Rectangle world)
    {
        Rectangle bounds = DungeonCollision.CreateBounds(position, size);
        return bounds.Left >= world.Left && bounds.Right <= world.Right &&
            bounds.Top >= world.Top && bounds.Bottom <= world.Bottom;
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
