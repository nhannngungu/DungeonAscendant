using System;
using DungeonAscendant.Enemies;
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
    public const int MaximumPlagueClouds = 5;
    private const float PlagueCloudMergeDistance = 68f;

    private readonly List<Projectile> _projectiles = new(32);
    private readonly List<WebPatch> _webPatches = new(16);
    private readonly List<PlagueCloud> _plagueClouds = new(6);
    private readonly List<ProjectileImpact> _impacts = new(8);
    private float _pendingCursePressure;
    private float _plagueDamageCooldownRemaining;

    public IReadOnlyList<Projectile> Projectiles => _projectiles;
    public IReadOnlyList<WebPatch> WebPatches => _webPatches;
    public IReadOnlyList<PlagueCloud> PlagueClouds => _plagueClouds;
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

    public void SpawnEnemyBolt(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        bool heavy)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.CrossbowBolt,
            position,
            NormalizeOrDefault(direction) * (heavy ? 385f : 360f),
            heavy ? new Vector2(27f, 9f) : new Vector2(23f, 8f),
            lifetimeSeconds: 2.5f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            empoweredVisual: heavy));
    }

    public void SpawnHellArrow(
        Vector2 position,
        Vector2 target,
        int damage,
        int roomId,
        bool highArc)
    {
        const float gravity = 535f;
        Vector2 delta = target - position;
        Vector2 velocity;
        ProjectileTrajectoryType trajectory;
        float projectileGravity;

        if (highArc)
        {
            float flightSeconds = Math.Clamp(
                MathF.Abs(delta.X) / 315f,
                .76f,
                1.08f);
            velocity = new Vector2(
                delta.X / flightSeconds,
                delta.Y / flightSeconds - gravity * flightSeconds * .5f);
            trajectory = ProjectileTrajectoryType.Ballistic;
            projectileGravity = gravity;
        }
        else
        {
            velocity = NormalizeOrDefault(delta) * 348f;
            trajectory = ProjectileTrajectoryType.Straight;
            projectileGravity = 0f;
        }

        _projectiles.Add(new Projectile(
            ProjectileType.Arrow,
            position,
            velocity,
            new Vector2(18f, 7f),
            lifetimeSeconds: highArc ? 2.9f : 2.45f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            trajectory: trajectory,
            gravity: projectileGravity,
            isEmberArrow: true));
    }

    public void SpawnPlagueFlask(
        Vector2 position,
        Vector2 landingPosition,
        int roomId,
        DungeonMap dungeon)
    {
        const float flightSeconds = .84f;
        const float gravity = 525f;
        Vector2 delta = landingPosition - position;
        Vector2 velocity = new(
            Math.Clamp(delta.X / flightSeconds, -365f, 365f),
            delta.Y / flightSeconds - gravity * flightSeconds * .5f);
        Vector2 flaskSize = new(13f, 15f);
        Vector2 predictedImpact = PredictTerrainImpact(
            position,
            velocity,
            flaskSize,
            gravity,
            dungeon);

        _projectiles.Add(new Projectile(
            ProjectileType.PlagueFlask,
            position,
            velocity,
            flaskSize,
            lifetimeSeconds: 2.1f,
            damage: 0,
            blockable: false,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            trajectory: ProjectileTrajectoryType.Ballistic,
            gravity: gravity,
            telegraphPosition: predictedImpact + new Vector2(0f, 5f)));
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
        float placementDistance = 0f,
        object lockedTarget = null)
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
            placementDistance: placementDistance,
            lockedTarget: lockedTarget));
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
        int roomId,
        float cursePressure = 7f)
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
            cursePressure: cursePressure));
    }

    public void SpawnSigilLance(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.SigilLance,
            position,
            NormalizeOrDefault(direction) * 285f,
            new Vector2(24f, 9f),
            lifetimeSeconds: 2.35f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId));
    }

    public void SpawnJudgmentBolt(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        bool empoweredVisual = false)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.JudgmentBolt,
            position,
            NormalizeOrDefault(direction) * (empoweredVisual ? 385f : 340f),
            empoweredVisual ? new Vector2(28f, 12f) : new Vector2(22f, 10f),
            lifetimeSeconds: 2.45f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            empoweredVisual: empoweredVisual));
    }

    public void SpawnDominionArc(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        bool empoweredVisual = false)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.DominionArc,
            position,
            NormalizeOrDefault(direction) * (empoweredVisual ? 430f : 390f),
            empoweredVisual ? new Vector2(42f, 15f) : new Vector2(34f, 13f),
            lifetimeSeconds: 2.25f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            empoweredVisual: empoweredVisual));
    }

    public void SpawnVowshard(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        bool small = false,
        Enemy owner = null)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.Vowshard,
            position,
            NormalizeOrDefault(direction) * (small ? 275f : 250f),
            small ? new Vector2(12f, 9f) : new Vector2(17f, 12f),
            lifetimeSeconds: small ? 1.9f : 2.35f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId,
            empoweredVisual: !small) { SourceEnemy = owner });
    }

    public void SpawnAbyssScript(
        Vector2 position,
        Vector2 direction,
        int damage,
        int roomId,
        Enemy owner = null)
    {
        _projectiles.Add(new Projectile(
            ProjectileType.AbyssScript,
            position,
            NormalizeOrDefault(direction) * 205f,
            new Vector2(19f, 15f),
            lifetimeSeconds: 2.7f,
            damage,
            blockable: true,
            unblockable: false,
            slowDurationSeconds: 0f,
            slowMovementMultiplier: 1f,
            roomId) { SourceEnemy = owner });
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

    public bool TryCreatePlagueCloud(
        Vector2 position,
        int roomId,
        DungeonMap dungeon,
        bool collapseCloud = false)
    {
        if (dungeon.FindRoomContaining(position) == null)
            return false;

        float mergeDistanceSquared =
            PlagueCloudMergeDistance * PlagueCloudMergeDistance;
        for (int index = 0; index < _plagueClouds.Count; index++)
        {
            PlagueCloud existing = _plagueClouds[index];
            if (existing.RoomId == roomId &&
                existing.IsCollapseCloud == collapseCloud &&
                Vector2.DistanceSquared(existing.Position, position) <=
                    mergeDistanceSquared)
            {
                existing.Refresh(collapseCloud);
                return true;
            }
        }

        if (_plagueClouds.Count >= MaximumPlagueClouds)
        {
            if (!collapseCloud)
                return false;
            // A death warning is promised gameplay information. Replace the
            // oldest field instead of silently dropping it, while preserving
            // the same hard accumulation cap.
            _plagueClouds.RemoveAt(0);
        }

        _plagueClouds.Add(new PlagueCloud(
            position,
            roomId,
            collapseCloud));
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

            // The flask is a visible, terrain-bound delivery object. Its
            // hazard begins only where the bottle lands, never invisibly on
            // contact with the player.
            if (projectile.Type == ProjectileType.PlagueFlask)
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

            if (projectile.Type is ProjectileType.SoulBolt or
                ProjectileType.SigilLance or ProjectileType.JudgmentBolt or
                ProjectileType.DominionArc or ProjectileType.Vowshard or
                ProjectileType.AbyssScript)
                _impacts.Add(new ProjectileImpact(
                    projectile, projectile.Position));

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

        bool plagueDamageApplied = false;
        _plagueDamageCooldownRemaining = MathF.Max(
            0f,
            _plagueDamageCooldownRemaining - elapsedSeconds);
        for (int index = _plagueClouds.Count - 1; index >= 0; index--)
        {
            PlagueCloud cloud = _plagueClouds[index];
            cloud.TimeRemaining -= elapsedSeconds;

            if (cloud.TimeRemaining <= 0f)
            {
                if (cloud.State == PlagueCloudState.Warning)
                {
                    cloud.State = PlagueCloudState.Active;
                    cloud.TimeRemaining = cloud.ActiveDurationSeconds;
                }
                else
                {
                    _plagueClouds.RemoveAt(index);
                    continue;
                }
            }

            if (cloud.State != PlagueCloudState.Active ||
                !IsNearPlayer(cloud.Position, player.Position) ||
                !cloud.Bounds.Intersects(player.Bounds))
            {
                continue;
            }

            player.ApplySlow(
                PlagueCloud.SlowMultiplier,
                PlagueCloud.SlowRefreshSeconds);

            // Overlapping clouds refresh the same bounded slow and share one
            // damage interval, so overlap cannot multiply plague damage.
            if (!plagueDamageApplied &&
                _plagueDamageCooldownRemaining <= 0f)
            {
                player.ReceiveDamage(PlagueCloud.PeriodicDamage);
                _plagueDamageCooldownRemaining =
                    PlagueCloud.DamageIntervalSeconds;
                plagueDamageApplied = true;
            }
        }
    }

    public void Clear()
    {
        _projectiles.Clear();
        _webPatches.Clear();
        _plagueClouds.Clear();
        _impacts.Clear();
        _pendingCursePressure = 0f;
        _plagueDamageCooldownRemaining = 0f;
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
        _plagueClouds.Clear();
        _pendingCursePressure = 0f;
        _plagueDamageCooldownRemaining = 0f;
    }

    public void RemoveOwnedBy(Enemy owner)
    {
        if (owner == null) return;
        for (int i = _projectiles.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_projectiles[i].SourceEnemy, owner)) _projectiles.RemoveAt(i);
        for (int i = _impacts.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_impacts[i].Projectile.SourceEnemy, owner)) _impacts.RemoveAt(i);
    }

    public void ClearEnemyProjectilesInRoom(int roomId)
    {
        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            Projectile projectile = _projectiles[index];
            if (!projectile.IsPlayerOwned && projectile.RoomId == roomId)
                _projectiles.RemoveAt(index);
        }

        for (int index = _impacts.Count - 1; index >= 0; index--)
        {
            Projectile projectile = _impacts[index].Projectile;
            if (!projectile.IsPlayerOwned && projectile.RoomId == roomId)
                _impacts.RemoveAt(index);
        }
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

            if (!projectile.IsPlayerOwned &&
                projectile.Type == ProjectileType.PlagueFlask)
            {
                TryCreatePlagueCloud(
                    projectile.Position + new Vector2(0f, 5f),
                    projectile.RoomId,
                    dungeon);
                _impacts.Add(new ProjectileImpact(
                    projectile,
                    projectile.Position));
                return false;
            }

            if (projectile.ImpactRadius > 0f ||
                (!projectile.IsPlayerOwned &&
                 projectile.Type is ProjectileType.Arrow or
                     ProjectileType.CrossbowBolt or ProjectileType.SoulBolt or
                     ProjectileType.SigilLance or ProjectileType.JudgmentBolt or
                     ProjectileType.DominionArc or ProjectileType.Vowshard or
                     ProjectileType.AbyssScript))
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

    private static Vector2 PredictTerrainImpact(
        Vector2 position,
        Vector2 velocity,
        Vector2 size,
        float gravity,
        DungeonMap dungeon)
    {
        const float stepSeconds = 1f / 60f;
        const int maximumSteps = 126;
        Vector2 predicted = position;
        Vector2 predictedVelocity = velocity;
        for (int step = 0; step < maximumSteps; step++)
        {
            predictedVelocity.Y += gravity * stepSeconds;
            Vector2 candidate = predicted +
                predictedVelocity * stepSeconds;
            if (!SideScrollingCollision.IsPositionFree(
                candidate,
                size,
                dungeon))
            {
                return predicted;
            }
            predicted = candidate;
        }
        return predicted;
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
