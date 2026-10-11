using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

public static class FallenCityEnemyPolishPass2Validation
{
    public static void ValidateOrThrow(DungeonMap map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        ValidateVisualAndCombatIdentity(map);
        ValidateRabidHound(map);
        ValidatePlagueCarrier(map);
        ValidatePlagueClouds(map);
        ValidateIntroductions(map);
    }

    private static void ValidateVisualAndCombatIdentity(DungeonMap map)
    {
        Need(
            FallenCityHoundPlagueSpriteRenderer.HoundVisualSize.X <
                EnemyVisualProfile.DireWolf.VisualSize.X &&
            FallenCityHoundPlagueSpriteRenderer.HoundVisualSize.Y <=
                EnemyVisualProfile.DireWolf.VisualSize.Y,
            "Rabid Hound must keep a lower, more compact silhouette than Dire Wolf.");
        Need(
            FallenCityHoundPlagueSpriteRenderer.CarrierVisualSize.Y >= 78f &&
            FallenCityHoundPlagueSpriteRenderer.CarrierVisualSize.X < 96f,
            "Plague Carrier visual envelope is no longer human and burdened.");

        FallenCityEnemy hound = Create(EnemyType.RabidHound, map, 480f);
        FallenCityEnemy carrier = Create(EnemyType.PlagueCarrier, map, 720f);
        FallenCityEnemy executioner = Create(
            EnemyType.StreetExecutioner,
            map,
            920f);
        Need(hound.MaxHealth < carrier.MaxHealth &&
            hound.MaxHealth < executioner.MaxHealth,
            "Rabid Hound health must remain below area-denial and heavy enemies.");
        Need(hound.MovementSpeed > carrier.MovementSpeed * 2f,
            "Rabid Hound and Plague Carrier mobility identities have converged.");
    }

    private static void ValidateRabidHound(DungeonMap map)
    {
        FallenCityEnemy hound = Create(EnemyType.RabidHound, map, 500f);
        PlayerCharacter player = CreatePlayer(map, 600f);
        var projectiles = new ProjectileManager();

        Tick(hound, player, map, projectiles, .01f);
        Need(hound.CombatState == FallenCityCombatState.HoundThreaten &&
            hound.IsCombatAttackTelegraphing,
            "Rabid Hound did not pause in a readable threat pose.");
        int healthBefore = player.CurrentHealth;
        Tick(hound, player, map, projectiles,
            FallenCityEnemy.HoundThreatenSeconds +
            FallenCityEnemy.LungeWindupSeconds - .10f);
        Need(player.CurrentHealth == healthBefore,
            "Lunging Bite dealt damage before its crouch and windup completed.");
        Tick(hound, player, map, projectiles, .22f);
        Need(player.CurrentHealth < healthBefore,
            "Lunging Bite active motion did not resolve contact.");

        bool sawReposition = false;
        bool sawSideSnap = false;
        for (int frame = 0; frame < 150; frame++)
        {
            Tick(hound, player, map, projectiles, .05f);
            sawReposition |= hound.CombatState ==
                FallenCityCombatState.HoundReposition;
            sawSideSnap |= hound.RabidHoundAttack ==
                RabidHoundAttackKind.SideSnap;
        }
        Need(sawReposition && sawSideSnap,
            "Rabid Hound did not disengage and return with its flank Side Snap.");

        FallenCityEnemy desperate = Create(
            EnemyType.RabidHound,
            map,
            650f);
        PlayerCharacter durable = CreatePlayer(map, 748f);
        desperate.ReceiveDamage((int)MathF.Ceiling(
            desperate.MaxHealth * .70f));
        bool sawFrenzy = false;
        int greatestStrikeBudget = 0;
        bool frenzyFinished = false;
        for (int frame = 0; frame < 320; frame++)
        {
            Tick(desperate, durable, map, new ProjectileManager(), .05f);
            sawFrenzy |= desperate.RabidHoundAttack ==
                RabidHoundAttackKind.FrenzyBite;
            greatestStrikeBudget = Math.Max(
                greatestStrikeBudget,
                desperate.FrenzyStrikesRemaining);
            frenzyFinished |= sawFrenzy &&
                desperate.CombatState == FallenCityCombatState.HoundReposition;
        }
        Need(sawFrenzy && greatestStrikeBudget == 2 && frenzyFinished,
            "Low-health Frenzy Bite must be a bounded two-strike sequence.");
    }

    private static void ValidatePlagueCarrier(DungeonMap map)
    {
        FallenCityEnemy interrupted = Create(
            EnemyType.PlagueCarrier,
            map,
            500f);
        PlayerCharacter rangedPlayer = CreatePlayer(map, 700f);
        var canceledProjectiles = new ProjectileManager();
        Tick(interrupted, rangedPlayer, map, canceledProjectiles, .01f);
        Need(interrupted.CombatState ==
                FallenCityCombatState.PlagueFlaskWindup &&
            canceledProjectiles.Projectiles.Count == 0,
            "Plague Flask must begin with a visible bottle windup.");
        interrupted.ApplyPoiseDamage(interrupted.MaxPoise);
        Tick(interrupted, rangedPlayer, map, canceledProjectiles, .90f);
        Need(canceledProjectiles.Projectiles.Count == 0,
            "An interrupted Plague Carrier released an invisible flask.");

        FallenCityEnemy thrower = Create(
            EnemyType.PlagueCarrier,
            map,
            500f);
        var projectiles = new ProjectileManager();
        Tick(thrower, rangedPlayer, map, projectiles, .01f);
        Tick(thrower, rangedPlayer, map, projectiles,
            FallenCityEnemy.PlagueFlaskWindupSeconds - .12f);
        Need(projectiles.Projectiles.Count == 0,
            "Plague Flask appeared before the throw motion completed.");
        Tick(thrower, rangedPlayer, map, projectiles, .18f);
        Need(projectiles.Projectiles.Count == 1 &&
            projectiles.Projectiles[0].Type == ProjectileType.PlagueFlask &&
            projectiles.Projectiles[0].HasTelegraphPosition &&
            !projectiles.Projectiles[0].PenetratesTerrain,
            "Plague Flask arc, landing marker, or terrain collision contract failed.");

        FallenCityEnemy cougher = Create(
            EnemyType.PlagueCarrier,
            map,
            820f);
        PlayerCharacter closePlayer = CreatePlayer(map, 872f);
        int healthBefore = closePlayer.CurrentHealth;
        Tick(cougher, closePlayer, map, new ProjectileManager(), .01f);
        Need(cougher.PlagueCarrierAttack ==
                PlagueCarrierAttackKind.BlackCough &&
            cougher.CombatState == FallenCityCombatState.PlagueCoughWindup,
            "Close-range Plague Carrier response must be Black Cough.");
        Tick(cougher, closePlayer, map, new ProjectileManager(),
            FallenCityEnemy.BlackCoughWindupSeconds - .10f);
        Need(closePlayer.CurrentHealth == healthBefore,
            "Black Cough dealt damage before its visible windup.");
        Tick(cougher, closePlayer, map, new ProjectileManager(), .18f);
        Need(closePlayer.CurrentHealth < healthBefore && closePlayer.IsSlowed,
            "Black Cough did not apply its low-damage temporary pressure.");
    }

    private static void ValidatePlagueClouds(DungeonMap map)
    {
        PlayerCharacter player = CreatePlayer(map, 700f);
        FallenCityEnemy thrower = Create(
            EnemyType.PlagueCarrier,
            map,
            500f);
        var projectiles = new ProjectileManager();
        Tick(thrower, player, map, projectiles,
            FallenCityEnemy.PlagueFlaskWindupSeconds + .08f);
        Vector2 telegraph = projectiles.Projectiles[0].TelegraphPosition;

        for (int frame = 0;
             frame < 60 && projectiles.PlagueClouds.Count == 0;
             frame++)
        {
            projectiles.Update(Frame(.05f), player, map);
        }
        Need(projectiles.Projectiles.Count == 0 &&
            projectiles.PlagueClouds.Count == 1 &&
            projectiles.PlagueClouds[0].State == PlagueCloudState.Warning,
            "Plague Flask did not break on terrain into a warned cloud.");
        Need(Vector2.DistanceSquared(
                telegraph,
                projectiles.PlagueClouds[0].Position) <= 18f * 18f,
            "Plague Flask landing marker did not match its terrain impact.");

        PlayerCharacter exposedPlayer = CreatePlayer(
            map,
            projectiles.PlagueClouds[0].Position.X);
        int healthBefore = exposedPlayer.CurrentHealth;
        for (int frame = 0; frame < 14; frame++)
            projectiles.Update(Frame(.05f), exposedPlayer, map);
        Need(projectiles.PlagueClouds[0].State == PlagueCloudState.Active &&
            exposedPlayer.IsSlowed &&
            exposedPlayer.CurrentHealth < healthBefore,
            $"Active plague cloud did not apply bounded slow and minor damage " +
            $"(state={projectiles.PlagueClouds[0].State}, " +
            $"slow={exposedPlayer.IsSlowed}, " +
            $"hp={exposedPlayer.CurrentHealth}/{healthBefore}, " +
            $"cloud={projectiles.PlagueClouds[0].Bounds}, " +
            $"player={exposedPlayer.Bounds}).");
        Need(projectiles.PlagueClouds[0].Bounds.Width <= 108 &&
            projectiles.PlagueClouds[0].ActiveDurationSeconds <= 3.25f,
            "Plague cloud exceeded its readable size or duration cap.");

        var collapseManager = new ProjectileManager();
        Vector2 collapsePosition = new(
            map.StartRoom.Bounds.Center.X,
            map.StartRoom.GroundY - 12f);
        Need(collapseManager.TryCreatePlagueCloud(
                collapsePosition,
                map.StartRoom.Id,
                map,
                collapseCloud: true),
            "Contaminated Collapse could not create its warned death zone.");
        PlagueCloud collapse = collapseManager.PlagueClouds[0];
        Need(collapse.IsCollapseCloud &&
            collapse.WarningDurationSeconds >= .65f &&
            collapse.Size.X < 108f &&
            collapse.ActiveDurationSeconds < PlagueCloud.FlaskActiveSeconds,
            "Contaminated Collapse is not smaller, shorter, and clearly warned.");

        var capped = new ProjectileManager();
        float startX = map.StartRoom.Bounds.Left + 130f;
        for (int index = 0; index < 8; index++)
        {
            capped.TryCreatePlagueCloud(
                new Vector2(startX + index * 76f,
                    map.StartRoom.GroundY - 12f),
                map.StartRoom.Id,
                map);
        }
        Need(capped.PlagueClouds.Count <= 5,
            "Plague cloud accumulation exceeded its hard cap.");
        Need(capped.TryCreatePlagueCloud(
                new Vector2(startX + 650f,
                    map.StartRoom.GroundY - 12f),
                map.StartRoom.Id,
                map,
                collapseCloud: true) &&
            capped.PlagueClouds.Count == 5 &&
            capped.PlagueClouds[4].IsCollapseCloud,
            "A capped field silently discarded the warned death cloud.");
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        FallenCityEnemy hound = Create(EnemyType.RabidHound, map, 500f);
        var houndIntroductions = new EnemyIntroductionManager();
        houndIntroductions.Update(
            Frame(.01f),
            hound.Position,
            new Enemy[] { hound },
            null,
            false);
        Need(houndIntroductions.Active?.Name == "RABID HOUND" &&
            houndIntroductions.Active.Title == "HOUND OF THE FALLEN CITY",
            "Rabid Hound first-encounter introduction changed.");

        FallenCityEnemy carrier = Create(
            EnemyType.PlagueCarrier,
            map,
            720f);
        var carrierIntroductions = new EnemyIntroductionManager();
        carrierIntroductions.Update(
            Frame(.01f),
            carrier.Position,
            new Enemy[] { carrier },
            null,
            false);
        Need(carrierIntroductions.Active?.Name == "PLAGUE CARRIER" &&
            carrierIntroductions.Active.Title ==
                "BEARER OF THE BLACK BREATH",
            "Plague Carrier first-encounter introduction changed.");
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
                $"Fallen City enemy polish pass 2 validation failed: {message}");
    }
}
