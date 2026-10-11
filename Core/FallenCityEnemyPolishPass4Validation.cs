using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

public static class FallenCityEnemyPolishPass4Validation
{
    public static void ValidateOrThrow(DungeonMap map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        ValidateIdentityEnvelope(map);
        ValidateRoyalGuard(map);
        ValidateAshKnight(map);
        ValidateIntroductions(map);
    }

    private static void ValidateIdentityEnvelope(DungeonMap map)
    {
        Need(
            FallenCityRoyalAshSpriteRenderer.RoyalGuardVisualSize.X >= 110f &&
            FallenCityRoyalAshSpriteRenderer.RoyalGuardVisualSize.Y >= 90f,
            "Royal Guard must retain a shield-led full-plate silhouette.");
        Need(
            FallenCityRoyalAshSpriteRenderer.AshKnightVisualSize.X >= 118f &&
            FallenCityRoyalAshSpriteRenderer.AshKnightVisualSize.Y >= 94f,
            "Ash Knight must retain a broad charred-knight silhouette.");

        FallenCityEnemy royal = Create(EnemyType.RoyalGuard, map, 500f);
        FallenCityEnemy ash = Create(EnemyType.AshKnight, map, 760f);
        FallenCityEnemy executioner = Create(
            EnemyType.StreetExecutioner,
            map,
            1000f);
        Need(royal.MaxHealth > executioner.MaxHealth &&
            royal.MaxPoise > executioner.MaxPoise,
            "Royal Guard lost its elite defensive durability.");
        Need(ash.MaxHealth > royal.MaxHealth &&
            ash.MovementSpeed > royal.MovementSpeed,
            "Ash Knight lost its durable but more aggressive elite identity.");
    }

    private static void ValidateRoyalGuard(DungeonMap map)
    {
        PlayerCharacter player = CreatePlayer(map, 570f);
        FallenCityEnemy frontGuard = Create(EnemyType.RoyalGuard, map, 500f);
        var roots = new RootHazardManager();
        Tick(frontGuard, player, map, roots, .01f);
        Need(frontGuard.IsRoyalGuarding &&
            frontGuard.CombatState == FallenCityCombatState.RoyalGuardStance,
            "Royal Guard did not open a finite readable guard stance.");

        int frontBefore = frontGuard.CurrentHealth;
        frontGuard.ReceiveDamageFrom(20, 8f, player.Position);
        int guardedLoss = frontBefore - frontGuard.CurrentHealth;

        FallenCityEnemy rearGuard = Create(EnemyType.RoyalGuard, map, 760f);
        PlayerCharacter rearTarget = CreatePlayer(map, 830f);
        Tick(rearGuard, rearTarget, map, new RootHazardManager(), .01f);
        int rearBefore = rearGuard.CurrentHealth;
        rearGuard.ReceiveDamageFrom(
            20,
            8f,
            new Vector2(rearGuard.Position.X - 80f, rearGuard.Position.Y));
        Need(guardedLoss < rearBefore - rearGuard.CurrentHealth,
            "Royal Guard frontal reduction also reduced rear damage.");

        frontGuard.ReceiveDamageFrom(45, 50f, player.Position);
        Need(frontGuard.IsRoyalGuardBroken && !frontGuard.IsRoyalGuarding,
            "Heavy/Poise damage did not open the Royal Guard's punish window.");
        Tick(frontGuard, player, map, roots,
            FallenCityEnemy.RoyalGuardBreakSeconds + .08f);
        Need(!frontGuard.IsRoyalGuardBroken,
            "Royal Guard break window did not recover cleanly.");

        bool sawThrust = false;
        bool sawBash = false;
        bool sawSlash = false;
        FallenCityEnemy patternGuard = Create(EnemyType.RoyalGuard, map, 980f);
        PlayerCharacter closePlayer = CreatePlayer(map, 1028f);
        for (int frame = 0; frame < 260; frame++)
        {
            Tick(patternGuard, closePlayer, map, roots, .05f);
            sawThrust |= patternGuard.RoyalGuardAttack ==
                RoyalGuardAttackKind.RoyalThrust;
            sawBash |= patternGuard.RoyalGuardAttack ==
                RoyalGuardAttackKind.ShieldBash;
            sawSlash |= patternGuard.RoyalGuardAttack ==
                RoyalGuardAttackKind.GuardedSlash;
        }
        Need(sawThrust && sawBash && sawSlash,
            "Royal Thrust, Shield Bash, or Guarded Slash is missing from the bounded pattern.");
    }

    private static void ValidateAshKnight(DungeonMap map)
    {
        FallenCityEnemy ash = Create(EnemyType.AshKnight, map, 500f);
        PlayerCharacter player = CreatePlayer(map, 580f);
        var roots = new RootHazardManager();
        bool sawSlash = false;
        bool sawSweep = false;
        bool sawCinder = false;
        bool sawTelegraph = false;
        for (int frame = 0; frame < 260; frame++)
        {
            Tick(ash, player, map, roots, .05f);
            sawSlash |= ash.AshKnightAttack == AshKnightAttackKind.EmberSlash;
            sawSweep |= ash.AshKnightAttack == AshKnightAttackKind.AshSweep;
            sawCinder |= ash.AshKnightAttack == AshKnightAttackKind.CinderStrike;
            sawTelegraph |= ash.IsCombatAttackTelegraphing;
        }
        Need(sawSlash && sawSweep && sawCinder && sawTelegraph,
            "Ash Knight's three readable attack cycles are incomplete.");
        Need(roots.Hazards.Count <= RootHazardManager.MaximumEmberHazardsPerOwner,
            "Ash Knight exceeded its owned ember-zone cap.");
        bool foundAshTrail = false;
        bool foundCinder = false;
        foreach (RootHazard hazard in roots.Hazards)
        {
            foundAshTrail |= hazard.VisualKind == RootHazardVisualKind.AshTrail;
            foundCinder |= hazard.VisualKind == RootHazardVisualKind.CinderZone;
        }
        Need(foundAshTrail && foundCinder,
            "Ash Sweep trail or Cinder Strike zone did not use the bounded hazard system.");

        FallenCityEnemy surging = Create(EnemyType.AshKnight, map, 820f);
        PlayerCharacter surgeTarget = CreatePlayer(map, 900f);
        surging.ReceiveDamage((int)MathF.Ceiling(surging.MaxHealth * .70f));
        Tick(surging, surgeTarget, map, new RootHazardManager(), .01f);
        Need(surging.AshKnightAttack == AshKnightAttackKind.EmberSurge &&
            surging.CombatState == FallenCityCombatState.AshSurgeWindup,
            "Low-health Ember Surge skipped its readable one-time windup.");
        Tick(surging, surgeTarget, map, new RootHazardManager(),
            FallenCityEnemy.EmberSurgeWindupSeconds + .08f);
        Need(surging.IsEmberSurging,
            "Ember Surge did not begin its temporary empowered window.");
        Tick(surging, surgeTarget, map, new RootHazardManager(),
            FallenCityEnemy.EmberSurgeDurationSeconds + .10f);
        Need(!surging.IsEmberSurging,
            "Ember Surge became permanent instead of expiring.");
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        FallenCityEnemy royal = Create(EnemyType.RoyalGuard, map, 500f);
        var royalIntro = new EnemyIntroductionManager();
        royalIntro.Update(
            Frame(.01f), royal.Position, new Enemy[] { royal }, null, false);
        Need(royalIntro.Active?.Name == "ROYAL GUARD" &&
            royalIntro.Active.Title == "SHIELD OF A DEAD KINGDOM",
            "Royal Guard first-encounter introduction changed.");

        FallenCityEnemy ash = Create(EnemyType.AshKnight, map, 760f);
        var ashIntro = new EnemyIntroductionManager();
        ashIntro.Update(
            Frame(.01f), ash.Position, new Enemy[] { ash }, null, false);
        Need(ashIntro.Active?.Name == "ASH KNIGHT" &&
            ashIntro.Active.Title == "KNIGHT OF THE BURNING QUARTER",
            "Ash Knight first-encounter introduction changed.");
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
            map.StartRoom.GroundY -
                PlayerCharacter.BodyHurtboxHeight / 2f))
        {
            DebugGodMode = true
        };
    }

    private static void Tick(
        FallenCityEnemy enemy,
        PlayerCharacter player,
        DungeonMap map,
        RootHazardManager roots,
        float seconds)
    {
        var projectiles = new ProjectileManager();
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
                $"Fallen City enemy polish pass 4 validation failed: {message}");
    }
}
