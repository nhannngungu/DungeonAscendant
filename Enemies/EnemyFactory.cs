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
            [EnemyType.FallenKnight] = context => Catacomb(context,EnemyType.FallenKnight),
            [EnemyType.FallenMilitiaman] = context => City(context, EnemyType.FallenMilitiaman),
            [EnemyType.RuinedCrossbowman] = context => City(context, EnemyType.RuinedCrossbowman),
            [EnemyType.RabidHound] = context => City(context, EnemyType.RabidHound),
            [EnemyType.PlagueCarrier] = context => City(context, EnemyType.PlagueCarrier),
            [EnemyType.BellWatcher] = context => City(context, EnemyType.BellWatcher),
            [EnemyType.StreetExecutioner] = context => City(context, EnemyType.StreetExecutioner),
            [EnemyType.RoyalGuard] = context => City(context, EnemyType.RoyalGuard),
            [EnemyType.AshKnight] = context => City(context, EnemyType.AshKnight),
            [EnemyType.WarBishop] = context => City(context, EnemyType.WarBishop),
            [EnemyType.BrokenKing] = context => City(context, EnemyType.BrokenKing),
            [EnemyType.ImpRaider] = context => Demon(context, EnemyType.ImpRaider),
            [EnemyType.HellArcher] = context => Demon(context, EnemyType.HellArcher),
            [EnemyType.SpineHound] = context => Demon(context, EnemyType.SpineHound),
            [EnemyType.RiftStalker] = context => Demon(context, EnemyType.RiftStalker),
            [EnemyType.BoneHarvester] = context => Demon(context, EnemyType.BoneHarvester),
            [EnemyType.Hellbrute] = context => Demon(context, EnemyType.Hellbrute),
            [EnemyType.CorruptionPriest] = context => Demon(context, EnemyType.CorruptionPriest),
            [EnemyType.RiftWarden] = context => Demon(context, EnemyType.RiftWarden),
            [EnemyType.DemonLegionnaire] = context => Demon(context, EnemyType.DemonLegionnaire),
            [EnemyType.HornedGeneral] = context => Demon(context, EnemyType.HornedGeneral)
        };

    private static Enemy Catacomb(EnemySpawnContext context,EnemyType type)=>
        new CatacombEnemy(type,context.Position,context.Level,context.WorldTier,context.RoomId,
            context.IsElite||type is EnemyType.CursedKnight or EnemyType.DeathKnight or EnemyType.SoulCollector or EnemyType.FallenKnight);

    private static Enemy City(EnemySpawnContext context, EnemyType type) =>
        new FallenCityEnemy(
            type,
            context.Position,
            context.Level,
            context.WorldTier,
            context.RoomId,
            context.IsElite || type is EnemyType.StreetExecutioner or
                EnemyType.RoyalGuard or EnemyType.AshKnight or
                EnemyType.WarBishop or EnemyType.BrokenKing);

    private static Enemy Demon(EnemySpawnContext context, EnemyType type) =>
        new DemonLandsEnemy(
            type,
            context.Position,
            context.Level,
            context.WorldTier,
            context.RoomId,
            context.IsElite || type is EnemyType.RiftWarden or
                EnemyType.DemonLegionnaire or EnemyType.HornedGeneral);

    public static Enemy Create(EnemyType type, EnemySpawnContext context)
    {
        if (!Creators.TryGetValue(type, out Func<EnemySpawnContext, Enemy> creator))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);

        return creator(context);
    }
}
