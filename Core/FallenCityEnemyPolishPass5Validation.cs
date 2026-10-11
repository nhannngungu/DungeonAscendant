using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Core;

public static class FallenCityEnemyPolishPass5Validation
{
    public static void ValidateOrThrow(DungeonMap map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        ValidateIdentityEnvelope(map);
        ValidateWarBishop(map);
        ValidateBrokenKing(map);
        ValidateIntroductions(map);
    }

    private static void ValidateIdentityEnvelope(DungeonMap map)
    {
        Need(
            FallenCityBishopKingSpriteRenderer.WarBishopVisualSize.Y >= 100f &&
            FallenCityBishopKingSpriteRenderer.WarBishopVisualSize.X <
                FallenCityBishopKingSpriteRenderer.BrokenKingVisualSize.X,
            "War Bishop must remain tall, ceremonial, and support-oriented.");
        Need(
            FallenCityBishopKingSpriteRenderer.BrokenKingVisualSize.X <= 140f &&
            FallenCityBishopKingSpriteRenderer.BrokenKingVisualSize.Y <= 120f,
            "Broken King must retain human-scale boss proportions.");

        FallenCityEnemy bishop = Create(EnemyType.WarBishop, map, 500f);
        FallenCityEnemy king = Create(EnemyType.BrokenKing, map, 760f);
        Need(!bishop.UsesProjectileAttack,
            "War Bishop reverted to ranged projectile spam.");
        Need(king.MaxHealth > bishop.MaxHealth && king.MaxPoise > bishop.MaxPoise,
            "Broken King lost boss durability and Poise.");
    }

    private static void ValidateWarBishop(DungeonMap map)
    {
        SpawnSocket bishopSocket = null;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            if (socket.ZoneId != "palace-vanguard")
                continue;
            foreach (EnemyType type in socket.SuggestedEnemyTypes)
            {
                if (type == EnemyType.WarBishop)
                    bishopSocket = socket;
            }
        }
        Need(bishopSocket != null,
            "War Bishop support validation socket is missing.");

        var manager = new EnemyManager(7715);
        manager.Reset(map, 4, 1, RegionType.RuinedCity,
            suppressProceduralSpawns: true);
        DungeonRoom bishopRoom = map.FindRoomContaining(bishopSocket.Position);
        var bishop = (FallenCityEnemy)manager.SpawnAuthoredEnemy(
            EnemyType.WarBishop, bishopSocket, bishopRoom, map, 4, 1);
        Vector2 allyPosition = new(
            bishopSocket.Position.X - 180f,
            bishopRoom.GroundY - 1f);
        var supportSocket = new SpawnSocket(
            "pass5-support-ally",
            bishopSocket.ZoneId,
            SpawnSocketRole.Elite,
            allyPosition,
            new Rectangle(
                (int)allyPosition.X - 100,
                (int)allyPosition.Y - 130,
                200,
                160),
            new[] { EnemyType.RoyalGuard });
        Enemy ally = manager.SpawnAuthoredEnemy(
            EnemyType.RoyalGuard,
            supportSocket,
            bishopRoom,
            map,
            4,
            1);
        Need(bishop != null,
            "War Bishop support encounter could not spawn its Bishop.");
        Need(ally != null,
            "War Bishop support encounter could not spawn its ally.");

        PlayerCharacter player = CreatePlayer(map, bishop.Position.X + 205f,
            bishopRoom.GroundY);
        var roots = new RootHazardManager();
        var projectiles = new ProjectileManager();
        bool sawChant = false;
        bool sawSeal = false;
        bool sawAllyBuff = false;
        bool sawSealHazard = false;
        int maximumOwnedSeals = 0;
        for (int frame = 0; frame < 260; frame++)
        {
            manager.UpdateCombatAndAi(
                Frame(.05f), player, map, projectiles, roots);
            roots.Update(Frame(.05f), player);
            sawChant |= bishop.WarBishopAction ==
                WarBishopActionKind.WarChant;
            sawSeal |= bishop.WarBishopAction ==
                WarBishopActionKind.CommandingSeal;
            sawAllyBuff |= ally.IsBuffed;
            int activeSeals = 0;
            foreach (RootHazard hazard in roots.Hazards)
            {
                if (hazard.VisualKind == RootHazardVisualKind.CommandSeal)
                {
                    sawSealHazard = true;
                    activeSeals++;
                }
            }
            maximumOwnedSeals = Math.Max(maximumOwnedSeals, activeSeals);
        }
        Need(sawChant && sawAllyBuff,
            "War Chant did not apply a bounded existing ally buff.");
        Need(sawSeal,
            "Commanding Seal did not enter its readable cast cycle.");

        Need(sawSealHazard && maximumOwnedSeals <=
                RootHazardManager.MaximumCommandSealsPerOwner,
            "Commanding Seal is missing or exceeded its active cap.");

        bishop.ReceiveDamage((int)MathF.Ceiling(bishop.MaxHealth * .72f));
        bool sawBenediction = false;
        for (int frame = 0; frame < 200; frame++)
        {
            manager.UpdateCombatAndAi(
                Frame(.05f), player, map, projectiles, roots);
            sawBenediction |= bishop.WarBishopAction ==
                WarBishopActionKind.FinalBenediction;
        }
        Need(sawBenediction,
            "Final Benediction did not trigger as a bounded low-health support action.");

        FallenCityEnemy isolated = Create(EnemyType.WarBishop, map, 980f);
        PlayerCharacter isolatedTarget = CreatePlayer(map, 1030f);
        Tick(isolated, isolatedTarget, map, new RootHazardManager(), .01f);
        Need(isolated.IsBishopIsolated &&
            isolated.WarBishopAction == WarBishopActionKind.StaffStrike,
            "Isolated War Bishop did not fall back to weak close self-defense.");
    }

    private static void ValidateBrokenKing(DungeonMap map)
    {
        FallenCityEnemy king = Create(EnemyType.BrokenKing, map, 500f);
        PlayerCharacter player = CreatePlayer(map, 575f);
        var roots = new RootHazardManager();
        Tick(king, player, map, roots, .01f);
        Need(king.BrokenKingAttack == BrokenKingAttackKind.RoyalSlash &&
            king.CombatState == FallenCityCombatState.KingWindup,
            "King of Steel did not open with a deliberate Royal Slash.");

        bool sawThrust = false;
        bool sawGuard = false;
        bool sawCounter = false;
        for (int frame = 0; frame < 340; frame++)
        {
            Tick(king, player, map, roots, .05f);
            sawThrust |= king.BrokenKingAttack ==
                BrokenKingAttackKind.KingsThrust;
            sawGuard |= king.IsKingGuarding;
            sawCounter |= king.IsKingCounterTell;
            if (king.IsKingGuarding)
            {
                king.ReceiveDamageFrom(45, 52f, player.Position);
                Need(king.IsKingGuardBroken,
                    "Heavy pressure did not break Royal Guard.");
            }
        }
        Need(sawThrust && sawGuard && sawCounter,
            "King of Steel lost thrust, finite guard, or readable counter tell.");

        king.ReceiveDamage((int)MathF.Ceiling(king.MaxHealth * .41f));
        Tick(king, player, map, roots, .01f);
        Need(king.CombatPhase == 2 && king.IsKingPhaseTransitioning &&
            !king.IsCombatAttackActive,
            "King of Ash transition is missing or damaging.");
        Tick(king, player, map, roots,
            FallenCityEnemy.KingPhaseTransitionSeconds + .08f);

        bool sawCleave = false;
        bool sawCharge = false;
        bool sawCollapse = false;
        for (int frame = 0; frame < 360; frame++)
        {
            Tick(king, player, map, roots, .05f);
            sawCleave |= king.BrokenKingAttack ==
                BrokenKingAttackKind.AshCleave;
            sawCharge |= king.BrokenKingAttack ==
                BrokenKingAttackKind.RoyalCharge;
            sawCollapse |= king.BrokenKingAttack ==
                BrokenKingAttackKind.CollapseStrike;
        }
        Need(sawCleave && sawCharge && sawCollapse,
            "King of Ash lost cleave, committed charge, or Collapse Strike.");
        bool foundBurningGround = false;
        foreach (RootHazard hazard in roots.Hazards)
        {
            foundBurningGround |= hazard.VisualKind is
                RootHazardVisualKind.AshTrail or
                RootHazardVisualKind.CinderZone;
        }
        Need(foundBurningGround,
            "King of Ash impacts did not create bounded burning ground.");

        king.ReceiveDamage((int)MathF.Ceiling(king.MaxHealth * .35f));
        Tick(king, player, map, roots, .01f);
        Need(king.CombatPhase == 3 && king.IsKingPhaseTransitioning,
            "The Last Command phase transition did not begin near 25% health.");
        Tick(king, player, map, roots,
            FallenCityEnemy.KingPhaseTransitionSeconds + .08f);
        Tick(king, player, map, roots, .05f);
        Need(king.BrokenKingAttack == BrokenKingAttackKind.LastCommand &&
            king.IsLastCommandWarning,
            "Last Command skipped its sword-plant warning.");

        bool sawPause = false;
        bool sawCommittedCharge = false;
        bool sawLongRecovery = false;
        bool repeated = false;
        bool completed = false;
        for (int frame = 0; frame < 180; frame++)
        {
            Tick(king, player, map, roots, .05f);
            sawPause |= king.CombatState ==
                FallenCityCombatState.KingLastCommandPause;
            sawCommittedCharge |= king.IsLastCommandCharging;
            sawLongRecovery |= king.CombatState ==
                FallenCityCombatState.KingLastCommandRecovery;
            if (sawLongRecovery && king.BrokenKingAttack ==
                BrokenKingAttackKind.None)
            {
                completed = true;
            }
            if (completed && king.BrokenKingAttack ==
                BrokenKingAttackKind.LastCommand)
            {
                repeated = true;
            }
        }
        Need(sawPause && sawCommittedCharge && sawLongRecovery && !repeated,
            "Last Command is incomplete, unreadable, or repeated without bounds.");

        king.ReceiveDamage(int.MaxValue);
        king.UpdateDeathPresentation(Frame(1.2f));
        Need(!king.IsDeathPresentationComplete,
            "Broken King death skipped its kneel and sword-lowering beat.");
        king.UpdateDeathPresentation(Frame(1.3f));
        Need(king.IsDeathPresentationComplete,
            "Broken King final collapse did not settle.");
    }

    private static void ValidateIntroductions(DungeonMap map)
    {
        FallenCityEnemy bishop = Create(EnemyType.WarBishop, map, 500f);
        var bishopIntro = new EnemyIntroductionManager();
        bishopIntro.Update(
            Frame(.01f), bishop.Position, new Enemy[] { bishop }, null, false);
        Need(bishopIntro.Active?.Name == "WAR BISHOP" &&
            bishopIntro.Active.Title == "PREACHER OF THE FINAL WAR",
            "War Bishop first-encounter introduction changed.");

        FallenCityEnemy king = Create(EnemyType.BrokenKing, map, 760f);
        var kingIntro = new EnemyIntroductionManager();
        kingIntro.Update(
            Frame(.01f), king.Position, new Enemy[] { king }, null, false);
        Need(kingIntro.Active?.Name == "THE BROKEN KING" &&
            kingIntro.Active.Title == "LAST SOVEREIGN OF THE FALLEN CITY",
            "Broken King boss presentation changed.");
    }

    private static FallenCityEnemy Create(
        EnemyType type,
        DungeonMap map,
        float x)
    {
        var enemy = new FallenCityEnemy(
            type, Vector2.Zero, 1, 1, map.StartRoom.Id,
            type is EnemyType.WarBishop or EnemyType.BrokenKing);
        enemy.PlaceAtAuthoredSocket(
            new Vector2(x, map.StartRoom.GroundY),
            aerial: false);
        return enemy;
    }

    private static PlayerCharacter CreatePlayer(
        DungeonMap map,
        float x,
        float? groundY = null)
    {
        return new PlayerCharacter(new Vector2(
            x,
            (groundY ?? map.StartRoom.GroundY) -
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
                $"Fallen City enemy polish pass 5 validation failed: {message}");
    }
}
