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
                context.WorldTier),
            [EnemyType.ThornCrawler] = context => new ThornCrawler(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier),
            [EnemyType.CorruptedTreant] = context => new CorruptedTreant(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier),
            [EnemyType.BloodBat] = context => new BloodBat(
                context.Position,
                context.Level,
                context.IsElite,
                context.RoomId,
                context.WorldTier),
            [EnemyType.GoblinChief] = context => new GoblinChief(
                context.Position,
                context.Level,
                context.RoomId,
                context.WorldTier),
            [EnemyType.MotherSpider] = context => new MotherSpider(
                context.Position,
                context.Level,
                context.RoomId,
                context.WorldTier),
            [EnemyType.Skeleton] = context => Catacomb(context,EnemyType.Skeleton),
            [EnemyType.SkeletonArcher] = context => Catacomb(context,EnemyType.SkeletonArcher),
            [EnemyType.RottenCorpse] = context => Catacomb(context,EnemyType.RottenCorpse),
            [EnemyType.Wraith] = context => Catacomb(context,EnemyType.Wraith),
            [EnemyType.UndeadGuard] = context => Catacomb(context,EnemyType.UndeadGuard),
            [EnemyType.CursedKnight] = context => Catacomb(context,EnemyType.CursedKnight),
            [EnemyType.GraveBat] = context => Catacomb(context,EnemyType.GraveBat),
            [EnemyType.DeathKnight] = context => Catacomb(context,EnemyType.DeathKnight),
            [EnemyType.SoulCollector] = context => Catacomb(context,EnemyType.SoulCollector),
            [EnemyType.FallenKnight] = context => Catacomb(context,EnemyType.FallenKnight)
        };

    private static Enemy Catacomb(EnemySpawnContext context,EnemyType type)=>
        new CatacombEnemy(type,context.Position,context.Level,context.WorldTier,context.RoomId,
            context.IsElite||type is EnemyType.CursedKnight or EnemyType.DeathKnight or EnemyType.SoulCollector or EnemyType.FallenKnight);

    public static Enemy Create(EnemyType type, EnemySpawnContext context)
    {
        if (!Creators.TryGetValue(type, out Func<EnemySpawnContext, Enemy> creator))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);

        return creator(context);
    }
}
