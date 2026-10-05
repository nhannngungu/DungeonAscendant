using System;
using System.Collections.Generic;

namespace DungeonAscendant.Enemies;

public static class EnemyFactory
{
    private static readonly IReadOnlyDictionary<
        EnemyType,
        Func<EnemySpawnContext, Enemy>> Creators =
        new Dictionary<EnemyType, Func<EnemySpawnContext, Enemy>>
        {
            [EnemyType.Goblin] = context => new Goblin(
                context.Position,
                context.Level,
                context.GoblinVariant,
                context.IsElite,
                roomId: context.RoomId,
                worldTier: context.WorldTier),
            [EnemyType.DireWolf] = context => new DireWolf(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier),
            [EnemyType.GiantSpider] = context => new GiantSpider(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier),
            [EnemyType.GoblinHunter] = context => new GoblinHunter(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier)
        };

    public static Enemy Create(EnemyType type, EnemySpawnContext context)
    {
        if (!Creators.TryGetValue(type, out Func<EnemySpawnContext, Enemy> creator))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);

        return creator(context);
    }
}
