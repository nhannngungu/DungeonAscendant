using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Combat;

public sealed class RootHazardManager
{
    public const int MaximumHazards = 24;
    public const int MaximumEmberHazardsPerOwner = 3;
    public const int MaximumCommandSealsPerOwner = 2;
    public const int MaximumBindingSigilsPerOwner = 1;
    public const int MaximumJudicialSealsPerOwner = 4;
    public const int MaximumMalacharHazardsPerOwner = 7;

    private const float ActiveInteractionDistance = 520f;
    private readonly List<RootHazard> _hazards = new(MaximumHazards);

    public IReadOnlyList<RootHazard> Hazards => _hazards;

    public bool TryAdd(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        float slowMultiplier,
        float slowDurationSeconds,
        int roomId,
        object owner,
        bool isTerrainRoot,
        DungeonMap dungeon,
        RootHazardVisualKind visualKind = RootHazardVisualKind.Roots)
    {
        if (_hazards.Count >= MaximumHazards)
            return false;

        var hazard = new RootHazard(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            damage,
            slowMultiplier,
            slowDurationSeconds,
            roomId,
            owner,
            isTerrainRoot,
            visualKind);

        if (dungeon.FindRoomContaining(position) == null)
            return false;

        _hazards.Add(hazard);
        return true;
    }

    public bool TryAddEmberHazard(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        int roomId,
        object owner,
        bool isCinderZone,
        DungeonMap dungeon)
    {
        int ownerEmberCount = 0;
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard existing = _hazards[index];
            if (ReferenceEquals(existing.Owner, owner) &&
                (existing.VisualKind is RootHazardVisualKind.AshTrail or
                    RootHazardVisualKind.CinderZone))
            {
                ownerEmberCount++;
            }
        }

        if (ownerEmberCount >= MaximumEmberHazardsPerOwner)
            return false;

        return TryAdd(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            damage,
            1f,
            0f,
            roomId,
            owner,
            isTerrainRoot: false,
            dungeon: dungeon,
            visualKind: isCinderZone
                ? RootHazardVisualKind.CinderZone
                : RootHazardVisualKind.AshTrail);
    }

    public bool TryAddCommandSeal(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        float slowMultiplier,
        float slowDurationSeconds,
        int roomId,
        object owner,
        DungeonMap dungeon)
    {
        int ownerSealCount = 0;
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard existing = _hazards[index];
            if (ReferenceEquals(existing.Owner, owner) &&
                existing.VisualKind == RootHazardVisualKind.CommandSeal)
            {
                ownerSealCount++;
            }
        }
        if (ownerSealCount >= MaximumCommandSealsPerOwner)
            return false;

        return TryAdd(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            damage,
            Math.Clamp(slowMultiplier, .75f, 1f),
            Math.Clamp(slowDurationSeconds, 0f, .45f),
            roomId,
            owner,
            isTerrainRoot: false,
            dungeon: dungeon,
            visualKind: RootHazardVisualKind.CommandSeal);
    }

    public bool TryAddBindingSigil(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        float slowMultiplier,
        float slowDurationSeconds,
        int roomId,
        object owner,
        DungeonMap dungeon)
    {
        int ownerSigilCount = 0;
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard existing = _hazards[index];
            if (ReferenceEquals(existing.Owner, owner) &&
                existing.VisualKind == RootHazardVisualKind.BindingSigil)
            {
                ownerSigilCount++;
            }
        }
        if (ownerSigilCount >= MaximumBindingSigilsPerOwner)
            return false;

        return TryAdd(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            damage: 0,
            Math.Clamp(slowMultiplier, .68f, .88f),
            Math.Clamp(slowDurationSeconds, .18f, .38f),
            roomId,
            owner,
            isTerrainRoot: false,
            dungeon,
            visualKind: RootHazardVisualKind.BindingSigil);
    }

    public bool TryAddJudicialSeal(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        float slowMultiplier,
        float slowDurationSeconds,
        int roomId,
        object owner,
        RootHazardVisualKind visualKind,
        DungeonMap dungeon)
    {
        if (visualKind is not (RootHazardVisualKind.DecreeZone or
            RootHazardVisualKind.BindingWrit or
            RootHazardVisualKind.FinalVerdictSeal))
        {
            return false;
        }

        int ownerSealCount = 0;
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard existing = _hazards[index];
            if (ReferenceEquals(existing.Owner, owner) &&
                existing.VisualKind is (RootHazardVisualKind.DecreeZone or
                    RootHazardVisualKind.BindingWrit or
                    RootHazardVisualKind.FinalVerdictSeal))
            {
                ownerSealCount++;
            }
        }
        if (ownerSealCount >= MaximumJudicialSealsPerOwner)
            return false;

        return TryAdd(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            Math.Max(0, damage),
            Math.Clamp(slowMultiplier, .55f, 1f),
            Math.Clamp(slowDurationSeconds, 0f, .45f),
            roomId,
            owner,
            isTerrainRoot: false,
            dungeon,
            visualKind);
    }

    public bool TryAddMalacharHazard(
        Vector2 position,
        Vector2 size,
        float telegraphSeconds,
        float activeSeconds,
        int damage,
        int roomId,
        object owner,
        RootHazardVisualKind visualKind,
        DungeonMap dungeon)
    {
        if (visualKind is not (RootHazardVisualKind.AbyssalPillar or
            RootHazardVisualKind.DominionLane or
            RootHazardVisualKind.EdictSeal))
        {
            return false;
        }

        int ownedCount = 0;
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard existing = _hazards[index];
            if (ReferenceEquals(existing.Owner, owner) &&
                existing.VisualKind is (RootHazardVisualKind.AbyssalPillar or
                    RootHazardVisualKind.DominionLane or
                    RootHazardVisualKind.EdictSeal))
            {
                ownedCount++;
            }
        }
        if (ownedCount >= MaximumMalacharHazardsPerOwner)
            return false;

        return TryAdd(
            position,
            size,
            telegraphSeconds,
            activeSeconds,
            Math.Max(0, damage),
            1f,
            0f,
            roomId,
            owner,
            isTerrainRoot: false,
            dungeon,
            visualKind);
    }

    public void Update(
        GameTime gameTime,
        PlayerCharacter player)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int index = _hazards.Count - 1; index >= 0; index--)
        {
            RootHazard hazard = _hazards[index];
            hazard.TimeRemaining -= elapsedSeconds;

            if (hazard.TimeRemaining <= 0f)
            {
                if (hazard.State == RootHazardState.Telegraph)
                {
                    hazard.State = RootHazardState.Active;
                    hazard.TimeRemaining = hazard.ActiveDurationSeconds;
                }
                else
                {
                    _hazards.RemoveAt(index);
                    continue;
                }
            }

            if (hazard.State != RootHazardState.Active ||
                Vector2.DistanceSquared(hazard.Position, player.Position) >
                ActiveInteractionDistance * ActiveInteractionDistance ||
                !hazard.Bounds.Intersects(player.Bounds))
            {
                continue;
            }

            if (!hazard.HasDamagedPlayer && hazard.Damage > 0)
            {
                player.ReceiveDamage(hazard.Damage);
                hazard.HasDamagedPlayer = true;
                if (hazard.VisualKind is RootHazardVisualKind.AshTrail or
                    RootHazardVisualKind.CinderZone)
                {
                    MarkIntersectingEmberHazardsConsumed(player.Bounds);
                }
            }

            if (hazard.SlowDurationSeconds > 0f)
            {
                player.ApplySlow(
                    hazard.SlowMultiplier,
                    hazard.SlowDurationSeconds);
            }
        }
    }

    private void MarkIntersectingEmberHazardsConsumed(Rectangle playerBounds)
    {
        for (int index = 0; index < _hazards.Count; index++)
        {
            RootHazard hazard = _hazards[index];
            if (hazard.State == RootHazardState.Active &&
                (hazard.VisualKind is RootHazardVisualKind.AshTrail or
                    RootHazardVisualKind.CinderZone) &&
                hazard.Bounds.Intersects(playerBounds))
            {
                hazard.HasDamagedPlayer = true;
            }
        }
    }

    public void RemoveOwnedBy(object owner)
    {
        for (int index = _hazards.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(_hazards[index].Owner, owner))
                _hazards.RemoveAt(index);
        }
    }

    public void Clear()
    {
        _hazards.Clear();
    }
}
