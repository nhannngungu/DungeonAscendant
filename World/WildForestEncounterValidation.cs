using System;
using DungeonAscendant.Bosses;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public static class WildForestEncounterValidation
{
    public static void ValidateOrThrow()
    {
        DungeonMap map = new DungeonGenerator(1701).GenerateWildForest();
        ValidateEncounterSpawns(map);
        ValidateIntroductions(map);
    }

    private static void ValidateEncounterSpawns(DungeonMap map)
    {
        foreach (EncounterZone zone in map.EncounterZones)
        {
            if (zone.IsBossZone)
                continue;

            var enemies = new EnemyManager(1701);
            enemies.Reset(
                map, 1, 1, RegionType.WildForest,
                suppressProceduralSpawns: true);
            var director = new WildForestEncounterDirector();
            director.Reset(map);
            Vector2 player = new(
                zone.ActivationBounds.Left + 4f,
                zone.ActivationBounds.Center.Y);
            for (int frame = 0; frame < 20; frame++)
            {
                director.Update(
                    Frame(.05f), player, map, enemies, 1, 1);
            }

            Require(director.IsTriggered(zone.ZoneId),
                $"Encounter {zone.ZoneId} could not find a safe socket.");
            Require(enemies.Enemies.Count > 0 &&
                enemies.Enemies.Count <= zone.MaxConcurrentEnemies,
                $"Encounter {zone.ZoneId} violated its population cap.");
            if (zone.ZoneId == "outskirts-first-wolf")
            {
                Require(enemies.Enemies.Count == 1 &&
                    enemies.Enemies[0].Type == EnemyType.DireWolf,
                    "The first Wild Forest encounter must be exactly one Dire Wolf.");
            }

            foreach (Enemy enemy in enemies.Enemies)
            {
                Require(float.IsFinite(enemy.Position.X) &&
                    float.IsFinite(enemy.Position.Y),
                    $"Encounter {zone.ZoneId} created an invalid coordinate.");
                Require(SideScrollingCollision.IsPositionFree(
                        enemy.Position, enemy.Size, map),
                    $"Encounter {zone.ZoneId} spawned {enemy.Type} inside collision.");
                Require(!enemy.Bounds.Contains(player.ToPoint()),
                    $"Encounter {zone.ZoneId} spawned on the Player.");
                if (!enemy.IsFlying)
                {
                    Require(SideScrollingCollision.IsSupported(
                            enemy.Position, enemy.Size, map),
                        $"Encounter {zone.ZoneId} left {enemy.Type} unsupported.");
                }
            }
        }
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        var introductions = new EnemyIntroductionManager();
        int events = 0;
        introductions.EnemyDiscovered += _ => events++;
        EnemyType[] types =
        {
            EnemyType.DireWolf,
            EnemyType.GiantSpider,
            EnemyType.Goblin,
            EnemyType.GoblinHunter,
            EnemyType.ThornCrawler,
            EnemyType.CorruptedTreant,
            EnemyType.BloodBat,
            EnemyType.GoblinChief,
            EnemyType.MotherSpider
        };

        foreach (EnemyType type in types)
        {
            var context = new EnemySpawnContext(
                new Vector2(300f, 300f), 1, 1, 0,
                type is EnemyType.GoblinChief or EnemyType.MotherSpider,
                GoblinVariant.Normal);
            Enemy enemy = EnemyFactory.Create(type, context);
            introductions.Update(
                Frame(.05f), Vector2.Zero, new[] { enemy }, null, false);
            Require(introductions.IsPresenting,
                $"First {type} sighting did not trigger an introduction.");
            Expire(introductions, enemy, null);
            introductions.Update(
                Frame(.05f), Vector2.Zero, new[] { enemy }, null, false);
            Require(!introductions.IsPresenting,
                $"{type} introduction repeated in the same run.");
        }

        DungeonRoom arena = map.BossRoom;
        var boss = new AncientTreant(
            new Vector2(300f, 300f), arena.Id, 1, 1, 1);
        introductions.Update(
            Frame(.05f), Vector2.Zero, Array.Empty<Enemy>(), boss, true);
        Require(introductions.IsPresenting &&
            introductions.Active.Identity ==
                WildForestEnemyIdentity.AncientTreant,
            "Ancient Treant introduction did not trigger.");
        Require(events == 10 && introductions.DiscoveryCount == 10,
            "Introduction discovery hook did not report all ten identities.");
    }

    private static void Expire(
        EnemyIntroductionManager introductions,
        Enemy enemy,
        AncientTreant boss)
    {
        for (int frame = 0; frame < 80; frame++)
        {
            introductions.Update(
                Frame(.05f), Vector2.Zero,
                enemy == null ? Array.Empty<Enemy>() : new[] { enemy },
                boss,
                boss != null);
        }
    }

    private static GameTime Frame(float seconds) => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(seconds));

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
