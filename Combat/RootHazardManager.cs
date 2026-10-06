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
        DungeonMap dungeon)
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
            isTerrainRoot);

        if (dungeon.FindRoomContaining(position) == null)
            return false;

        _hazards.Add(hazard);
        return true;
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
            }

            if (hazard.SlowDurationSeconds > 0f)
            {
                player.ApplySlow(
                    hazard.SlowMultiplier,
                    hazard.SlowDurationSeconds);
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
