using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

public static class FallenCityEnemyPolishValidation
{
    public static void ValidateOrThrow(DungeonMap map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        ValidateVisualEnvelope(map);
        ValidateMilitiaman(map);
        ValidateCrossbowman(map);
        ValidateIntroductions(map);
    }

    private static void ValidateVisualEnvelope(DungeonMap map)
    {
        Need(
            FallenCityMilitiaSpriteRenderer.MilitiamanVisualSize ==
                new Vector2(82f, 72f),
            "Fallen Militiaman human-sized visual envelope changed.");
        Need(
            FallenCityMilitiaSpriteRenderer.CrossbowmanVisualSize.X >
                FallenCityMilitiaSpriteRenderer.MilitiamanVisualSize.X,
            "Ruined Crossbowman must keep its stockier crossbow silhouette.");

        var militia = Create(EnemyType.FallenMilitiaman, map, 440f);
        var crossbowman = Create(EnemyType.RuinedCrossbowman, map, 760f);
        var skeleton = new CatacombEnemy(
            EnemyType.Skeleton,
            Vector2.Zero,
            1,
            1,
            map.StartRoom.Id,
            false);
        var royal = Create(EnemyType.RoyalGuard, map, 980f);
        Need(militia.MaxPoise > skeleton.MaxPoise &&
            militia.MaxPoise < royal.MaxPoise,
            "Militiaman Poise no longer sits between Skeleton and Royal Guard.");
        Need(crossbowman.MaxPoise < militia.MaxPoise,
            "Crossbowman must remain easier to interrupt than the Militiaman.");
    }

    private static void ValidateMilitiaman(DungeonMap map)
    {
        FallenCityEnemy militia = Create(
            EnemyType.FallenMilitiaman,
            map,
            500f);
        PlayerCharacter player = CreatePlayer(map, 552f);
        var projectiles = new ProjectileManager();

        Tick(militia, player, map, projectiles, .01f);
        Need(militia.CombatState == FallenCityCombatState.MilitiaWindup &&
            militia.MilitiaAttack == MilitiaAttackKind.MilitiaSlash &&
            militia.IsCombatAttackTelegraphing,
            "Militia Slash did not begin with its authored windup.");
        int healthBefore = player.CurrentHealth;
        Tick(militia, player, map, projectiles,
            FallenCityEnemy.MilitiaSlashWindupSeconds - .12f);
        Need(player.CurrentHealth == healthBefore,
            "Militiaman damage landed before the visible slash motion.");
        Tick(militia, player, map, projectiles, .18f);
        Need(player.CurrentHealth < healthBefore,
            "Militia Slash active motion did not resolve its contact.");

        FallenCityEnemy desperate = Create(
            EnemyType.FallenMilitiaman,
            map,
            620f);
        PlayerCharacter durablePlayer = CreatePlayer(map, 672f);
        desperate.ReceiveDamage((int)MathF.Ceiling(desperate.MaxHealth * .70f));
        bool sawThrust = false;
        bool sawDesperate = false;
        bool sawDefensiveStep = false;
        for (int frame = 0; frame < 220; frame++)
        {
            Tick(desperate, durablePlayer, map, projectiles, .05f);
            sawThrust |= desperate.MilitiaAttack ==
                MilitiaAttackKind.ShortThrust;
            sawDesperate |= desperate.MilitiaAttack ==
                MilitiaAttackKind.DesperateSwing;
            sawDefensiveStep |= desperate.CombatState ==
                FallenCityCombatState.MilitiaDefensiveStep;
        }
        Need(sawThrust && sawDesperate && sawDefensiveStep,
            "Militiaman thrust, low-health swing, or defensive step is missing.");

        desperate.ReceiveDamage(int.MaxValue);
        desperate.UpdateDeathPresentation(Frame(.50f));
        Need(!desperate.IsDeathPresentationComplete,
            "Militiaman collapse disappeared before it could read.");
        desperate.UpdateDeathPresentation(Frame(.50f));
        Need(desperate.IsDeathPresentationComplete,
            "Militiaman collapse did not finish cleanly.");
    }

    private static void ValidateCrossbowman(DungeonMap map)
    {
        FallenCityEnemy interrupted = Create(
            EnemyType.RuinedCrossbowman,
            map,
            760f);
        PlayerCharacter player = CreatePlayer(map, 1030f);
        var interruptedProjectiles = new ProjectileManager();
        Tick(interrupted, player, map, interruptedProjectiles, .01f);
        Need(interrupted.CombatState == FallenCityCombatState.CrossbowAim &&
            interruptedProjectiles.Projectiles.Count == 0,
            "Crossbowman must visibly aim before creating a bolt.");
        interrupted.ApplyPoiseDamage(interrupted.MaxPoise);
        Tick(interrupted, player, map, interruptedProjectiles, .60f);
        Need(interrupted.IsStaggered &&
            interruptedProjectiles.Projectiles.Count == 0,
            "An interrupted Crossbowman fired an invisible delayed bolt.");

        FallenCityEnemy firing = Create(
            EnemyType.RuinedCrossbowman,
            map,
            760f);
        var projectiles = new ProjectileManager();
        Tick(firing, player, map, projectiles, .01f);
        Tick(firing, player, map, projectiles,
            FallenCityEnemy.StandardBoltAimSeconds + .06f);
        Need(projectiles.Projectiles.Count == 1 &&
            projectiles.Projectiles[0].Type == ProjectileType.CrossbowBolt &&
            !projectiles.Projectiles[0].PenetratesTerrain,
            "Standard Bolt release or terrain collision contract failed.");
        Need(projectiles.Projectiles[0].Damage > new CatacombEnemy(
                EnemyType.SkeletonArcher,
                Vector2.Zero,
                1,
                1,
                map.StartRoom.Id,
                false).AttackDamage,
            "Crossbow shot damage must remain above Skeleton Archer damage.");
        Tick(firing, player, map, projectiles,
            FallenCityEnemy.CrossbowFireSeconds + .06f);
        Need(firing.IsReloading && !firing.IsCombatAttackTelegraphing,
            "Crossbow reload is not exposed as a readable punish window.");
        float reloadProgress = firing.ActionProgress;
        Tick(firing, player, map, projectiles, .28f);
        Need(firing.IsReloading && firing.ActionProgress > reloadProgress,
            "Crossbow reload is not visibly time-progressive.");

        for (int frame = 0;
             frame < 360 && projectiles.Projectiles.Count < 4;
             frame++)
        {
            Tick(firing, player, map, projectiles, .05f);
        }
        Need(projectiles.Projectiles.Count >= 4 &&
            projectiles.Projectiles[3].EmpoweredVisual &&
            projectiles.Projectiles[3].Damage > projectiles.Projectiles[0].Damage,
            "Heavy Bolt cadence, damage, or physical weight is missing.");

        FallenCityEnemy close = Create(
            EnemyType.RuinedCrossbowman,
            map,
            860f);
        PlayerCharacter closePlayer = CreatePlayer(map, 916f);
        Tick(close, closePlayer, map, new ProjectileManager(), .01f);
        Need(close.CrossbowAttack == CrossbowAttackKind.CrossbowShove &&
            close.CombatState ==
                FallenCityCombatState.CrossbowShoveWindup,
            "Point-blank Crossbowman response must be a shove, not a shot.");
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        var introductions = new EnemyIntroductionManager();
        FallenCityEnemy militia = Create(
            EnemyType.FallenMilitiaman,
            map,
            500f);
        introductions.Update(
            Frame(.01f),
            militia.Position,
            new Enemy[] { militia },
            null,
            false);
        Need(introductions.Active?.Title ==
                "THE SOLDIER WHO NEVER RETURNED",
            "Fallen Militiaman first-encounter title changed.");

        for (int frame = 0; frame < 50; frame++)
        {
            introductions.Update(
                Frame(.05f),
                militia.Position,
                new Enemy[] { militia },
                null,
                false);
        }

        FallenCityEnemy crossbowman = Create(
            EnemyType.RuinedCrossbowman,
            map,
            760f);
        introductions.Update(
            Frame(.01f),
            crossbowman.Position,
            new Enemy[] { crossbowman },
            null,
            false);
        Need(introductions.Active?.Title == "WATCHER OF EMPTY STREETS",
            "Ruined Crossbowman first-encounter title changed.");
    }

    private static FallenCityEnemy Create(
        EnemyType type,
        DungeonMap map,
        float x)
    {
        var enemy = new FallenCityEnemy(
            type,
            Vector2.Zero,
            1,
            1,
            map.StartRoom.Id,
            false);
        enemy.PlaceAtAuthoredSocket(
            new Vector2(x, map.StartRoom.GroundY),
            aerial: false);
        return enemy;
    }

    private static PlayerCharacter CreatePlayer(DungeonMap map, float x)
    {
        return new PlayerCharacter(new Vector2(
            x,
            map.StartRoom.GroundY - PlayerCharacter.BodyHurtboxHeight / 2f))
        {
            DebugGodMode = true
        };
    }

    private static void Tick(
        FallenCityEnemy enemy,
        PlayerCharacter player,
        DungeonMap map,
        ProjectileManager projectiles,
        float seconds)
    {
        var roots = new RootHazardManager();
        float remaining = seconds;
        while (remaining > 0f)
        {
            float step = MathF.Min(.05f, remaining);
            enemy.Update(Frame(step), player, map, projectiles, roots);
            remaining -= step;
        }
    }

    private static GameTime Frame(float seconds) => new(
        TimeSpan.Zero,
        TimeSpan.FromSeconds(seconds));

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(
                $"Fallen City enemy polish validation failed: {message}");
    }
}
