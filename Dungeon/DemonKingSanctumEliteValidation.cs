using System;
using System.Collections.Generic;
using System.Linq;
using DungeonAscendant.Audio;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Localization;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Dungeon;

/// <summary>Headless combat simulation; never creates Game1 or a graphics device.</summary>
public static class DemonKingSanctumEliteValidation
{
    private static readonly GameTime Frame = new(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));
    private static readonly Combat.ProjectileManager Projectiles = new();

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonKingSanctumMapGenerator().Generate();
        Need(Enum.IsDefined(EnemyType.AbyssOracle) && Enum.IsDefined(EnemyType.RoyalSanctumKnight),
            "Enemy 68/69 registration missing.");
        foreach (EnemyType type in new[] { EnemyType.RoyalSanctumKnight, EnemyType.AbyssOracle })
        {
            var enemy = Spawn(map, type);
            string key = type == EnemyType.RoyalSanctumKnight ? "royal_sanctum_knight" : "abyss_oracle";
            Need(enemy.IsElite && !enemy.IsFlying && enemy.MaxPoise < 200 &&
                AudioProfiles.GetEnemy(type).Id == key,
                $"{type} factory/elite/finite poise/audio registration failed.");
            var introductions = new EnemyIntroductionManager();
            introductions.Update(Frame, enemy.Position, new Enemy[] { enemy }, null, false);
            Need(introductions.IsPresenting && !introductions.BlocksCombat &&
                introductions.Active.Identity == (type == EnemyType.RoyalSanctumKnight ?
                    WildForestEnemyIdentity.RoyalSanctumKnight : WildForestEnemyIdentity.AbyssOracle),
                $"{type} introduction identity missing or blocks combat.");
            foreach (Language language in new[] { Language.English, Language.Vietnamese })
            {
                var strings = Loc.GetStrings(language);
                foreach (string field in new[] { "name", "title", "description" })
                    Need(strings.ContainsKey($"enemy.{key}.{field}"), $"{type} localization missing.");
            }
            enemy.ReceiveDamage(1, 1000f);
            Need(enemy.IsStaggered && enemy.IsAlive, $"{type} has permanent super armor.");
        }
        Need(DemonKingSanctumEliteSpriteRenderer.KnightVisualSize.X > 200 &&
            DemonKingSanctumEliteSpriteRenderer.OracleVisualSize.Y > 140,
            "Elite renderer visual bounds do not resolve.");
        ValidateKnight(map);
        ValidateOracle(map);
        ValidateEncounters(map);
    }

    private static void ValidateKnight(DungeonMap map)
    {
        var knight = Spawn(map, EnemyType.RoyalSanctumKnight);
        PlayerCharacter player = Target(map, knight.Position.X - 140f);
        knight.ReceiveDamageFrom(1, 0, player.Position);
        Need(knight.ValidGuardCount == 0 && knight.RiposteCount == 0,
            "Unguarded damage enabled a riposte.");
        Until(knight, player, map, () => knight.IsSovereignGuarding, 1500);
        Need(knight.GuardCooldownRemaining > 0, "Guard has no cooldown.");
        Vector2 front = knight.Position + new Vector2(knight.AttackFacesLeft ? -80 : 80, 0);
        Vector2 back = knight.Position - (front - knight.Position);
        knight.ReceiveDamageFrom(0, 10, front);
        knight.ReceiveDamageFrom(1, 0, back);
        Need(knight.ValidGuardCount == 0, "Invalid guard contact enabled riposte.");
        int health = knight.CurrentHealth;
        float poise = knight.CurrentPoise;
        knight.ReceiveDamageFrom(20, 15, front);
        Need(knight.ValidGuardCount == 1 && health - knight.CurrentHealth < 20 &&
            poise - knight.CurrentPoise < 15, "Valid frontal weapon guard did not resist/arm counter.");
        Until(knight, player, map, () => knight.AttackKind == SanctumEliteAttack.RoyalRiposte, 100);
        Need(knight.CombatState == SanctumEliteState.Windup && !knight.IsCombatAttackActive &&
            knight.RiposteCount == 1, "Guard counter was instant or repeated.");
        for (int i = 0; i < 20; i++)
        {
            knight.Update(Frame, player, map, Projectiles);
            Need(!knight.IsCombatAttackActive, "Riposte skipped its readable delay.");
        }
        Until(knight, player, map, () => knight.CombatState == SanctumEliteState.Ready, 150);
        Need(!knight.IsSovereignGuarding, "Guard never expired.");

        var unguarded = Spawn(map, EnemyType.RoyalSanctumKnight);
        var seen = new HashSet<SanctumEliteAttack>();
        bool expiredCooldown = false, sawGuard = false;
        int sameStateFrames = 0;
        var priorState = unguarded.CombatState;
        for (int i = 0; i < 2400; i++)
        {
            player = Target(map, unguarded.Position.X - 145f);
            unguarded.Update(Frame, player, map, Projectiles);
            seen.Add(unguarded.AttackKind);
            sawGuard |= unguarded.IsSovereignGuarding;
            expiredCooldown |= sawGuard && unguarded.GuardCooldownRemaining <= 0f;
            sameStateFrames = priorState == unguarded.CombatState &&
                unguarded.CombatState != SanctumEliteState.Ready ? sameStateFrames + 1 : 0;
            priorState = unguarded.CombatState;
            Need(sameStateFrames < 100, "Knight action failed to terminate.");
            if (unguarded.AttackKind == SanctumEliteAttack.ThroneThrust &&
                unguarded.CombatState == SanctumEliteState.Active)
            {
                bool facesLeft = unguarded.AttackFacesLeft;
                unguarded.Update(Frame, Target(map, unguarded.Position.X + 145f), map, Projectiles);
                Need(unguarded.AttackFacesLeft == facesLeft, "Active thrust tracked behind the Knight.");
            }
        }
        Need(seen.Contains(SanctumEliteAttack.RoyalSever) && seen.Contains(SanctumEliteAttack.ThroneThrust) &&
            seen.Contains(SanctumEliteAttack.CrownSweep) && sawGuard && expiredCooldown &&
            unguarded.RiposteCount == 0, "Knight roster/cooldown or valid-guard-only riposte failed.");
        Until(unguarded, player, map, () => unguarded.IsSovereignGuarding, 1500);
        unguarded.ReceiveDamage(unguarded.CurrentHealth + 1, 1000);
        Need(!unguarded.IsSovereignGuarding && unguarded.CombatState == SanctumEliteState.Ready,
            "Knight retained guard after death.");
    }

    private static void ValidateOracle(DungeonMap map)
    {
        var oracle = Spawn(map, EnemyType.AbyssOracle);
        var seen = new HashSet<SanctumEliteAttack>();
        SanctumProphecyMarker[] fixedMarkers = Array.Empty<SanctumProphecyMarker>();
        Vector2[] fixedPositions = Array.Empty<Vector2>();
        Vector2 lockedOrigin = default, lockedDestination = default;
        bool spearLocked = false, sawLance = false, sawShift = false, shiftMoved = false,
            shiftExpired = false;
        int sameStateFrames = 0;
        var priorState = oracle.CombatState;
        for (int frame = 0; frame < 3600; frame++)
        {
            float offset = frame % 80 < 40 ? -285f : -355f;
            PlayerCharacter player = Target(map, oracle.Position.X + offset);
            oracle.Update(Frame, player, map, Projectiles);
            seen.Add(oracle.AttackKind);
            sameStateFrames = priorState == oracle.CombatState &&
                oracle.CombatState != SanctumEliteState.Ready ? sameStateFrames + 1 : 0;
            priorState = oracle.CombatState;
            Need(sameStateFrames < 145, "Oracle action failed to terminate.");
            if (oracle.AttackKind == SanctumEliteAttack.ForeseenSpear)
            {
                if (!spearLocked)
                { lockedOrigin = oracle.SpearOrigin; lockedDestination = oracle.SpearDestination; spearLocked = true; }
                Need(oracle.SpearOrigin == lockedOrigin && oracle.SpearDestination == lockedDestination,
                    "Foreseen Spear changed trajectory after placement.");
                if (oracle.CombatState == SanctumEliteState.Windup)
                    Need(!oracle.HasLance, "Foreseen Spear damaged during its telegraph.");
            }
            else spearLocked = false;
            sawLance |= oracle.HasLance;
            if (oracle.Markers.Count > 0)
            {
                if (fixedMarkers.Length == 0 || !ReferenceEquals(fixedMarkers[0], oracle.Markers[0]))
                {
                    fixedMarkers = oracle.Markers.ToArray();
                    fixedPositions = fixedMarkers.Select(m => m.Position).ToArray();
                }
                for (int i = 0; i < fixedMarkers.Length; i++)
                {
                    var marker = fixedMarkers[i];
                    Need(marker.Position == fixedPositions[i] && marker.ActivateAt - marker.RevealAt >= .85f,
                        "Prophecy tracked after placement or lacks anticipation.");
                    Need(!marker.IsActive(marker.RevealAt), "Marker damages before telegraph.");
                    if (marker.IsFalse)
                        for (float age = 0; age < 3f; age += .05f)
                            Need(!marker.IsActive(age), "Fractured Future fake marker damages.");
                }
                if (oracle.AttackKind == SanctumEliteAttack.ThreeOmens)
                {
                    Need(fixedMarkers.Length == 3, "Three Omens marker count incorrect.");
                    for (int i = 1; i < 3; i++)
                        Need(fixedMarkers[i].RevealAt > fixedMarkers[i - 1].RevealAt &&
                            fixedMarkers[i].ActivateAt > fixedMarkers[i - 1].ActivateAt &&
                            MathF.Abs(fixedPositions[i].X - fixedPositions[i - 1].X) - 68f >=
                                PlayerCharacter.BodyHurtboxWidth + 20f,
                            "Three Omens lost displayed order or fair movement space.");
                }
                if (oracle.AttackKind == SanctumEliteAttack.FracturedFuture)
                    Need(fixedMarkers.Length == 2 && fixedMarkers.Count(m => m.IsFalse) == 1 &&
                        !fixedMarkers[0].IsFalse && fixedMarkers[0].Order == 1,
                        "Fractured Future real/fake cue is not deterministic.");
            }
            if (oracle.AttackKind == SanctumEliteAttack.VeilShift)
            {
                sawShift = true;
                shiftMoved |= Vector2.Distance(oracle.ShiftOrigin, oracle.ShiftDestination) > 24f;
                Need(Vector2.Distance(oracle.ShiftOrigin, oracle.ShiftDestination) <=
                    DemonKingSanctumEliteEnemy.MaximumShiftDistance + .1f &&
                    Encounter(map, EnemyType.AbyssOracle).ActivationBounds.Contains(oracle.ShiftDestination.ToPoint()) &&
                    !oracle.IsCombatAttackActive && oracle.ShiftCooldownRemaining > 0,
                    "Veil Shift is unbounded, damaging or lacks cooldown.");
            }
            shiftExpired |= sawShift && oracle.ShiftCooldownRemaining <= 0;
        }
        Need(seen.Contains(SanctumEliteAttack.ForeseenSpear) && seen.Contains(SanctumEliteAttack.FateCircle) &&
            seen.Contains(SanctumEliteAttack.ThreeOmens) && seen.Contains(SanctumEliteAttack.FracturedFuture) &&
            sawLance && sawShift && shiftMoved && shiftExpired && oracle.ProphecyActivationCount > 0,
            "Oracle did not exercise its complete finite kit.");

        // Exercise the actual damage resolver while standing in the fake future.
        // A property-only check would miss accidental routing through another area.
        var future = Spawn(map, EnemyType.AbyssOracle);
        PlayerCharacter futureTarget = Target(map, future.Position.X - 285f);
        Until(future, futureTarget, map, () => future.AttackKind ==
            SanctumEliteAttack.FracturedFuture && future.Markers.Count == 2, 1800);
        Vector2 fakePosition = future.Markers.Single(m => m.IsFalse).Position;
        futureTarget = Target(map, fakePosition.X);
        int priorHits = future.ProphecyHitCount;
        int priorActivations = future.ProphecyActivationCount;
        Until(future, futureTarget, map, () => future.CombatState == SanctumEliteState.Ready, 300);
        Need(future.ProphecyHitCount == priorHits &&
            future.ProphecyActivationCount == priorActivations + 1 && !future.HasOwnedEffects,
            "Fractured Future fake dealt damage, real activated twice, or effects did not clear.");

        foreach (bool kill in new[] { false, true })
        {
            var cleanup = Spawn(map, EnemyType.AbyssOracle);
            PlayerCharacter player = Target(map, cleanup.Position.X - 290f);
            Until(cleanup, player, map, () => cleanup.HasLance, 300);
            if (kill) cleanup.ReceiveDamage(cleanup.CurrentHealth + 1, 1000);
            else cleanup.ReceiveDamage(1, 1000);
            Need(!cleanup.HasOwnedEffects, "Oracle retained travelling lance after death/stagger.");
            cleanup = Spawn(map, EnemyType.AbyssOracle);
            player = Target(map, cleanup.Position.X - 290f);
            Until(cleanup, player, map, () => cleanup.Markers.Count > 0, 600);
            if (kill) cleanup.ReceiveDamage(cleanup.CurrentHealth + 1, 1000);
            else cleanup.ReceiveDamage(1, 1000);
            Need(!cleanup.HasOwnedEffects, "Oracle retained markers after death/stagger.");
        }
        var offscreen = Spawn(map, EnemyType.AbyssOracle);
        Until(offscreen, Target(map, offscreen.Position.X - 285), map, () => offscreen.HasLance, 300);
        offscreen.Update(Frame, Target(map, offscreen.Position.X - 700), map, Projectiles);
        Need(!offscreen.HasOwnedEffects && !offscreen.IsCombatAttackActive,
            "Oracle continued an offscreen prophecy.");
    }

    private static void ValidateEncounters(DungeonMap map)
    {
        var manager = new EnemyManager(69);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary, suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector(); director.Reset(map);
        foreach (EnemyType type in new[] { EnemyType.RoyalSanctumKnight, EnemyType.AbyssOracle })
        {
            EncounterZone encounter = Encounter(map, type);
            Need(encounter.IsEliteZone && !encounter.IsBossZone && encounter.MaxConcurrentEnemies == 1,
                "Elite socket has incorrect encounter semantics.");
            Vector2 trigger = new(encounter.ActivationBounds.Left + 24, encounter.ActivationBounds.Center.Y);
            for (int i = 0; i < 100; i++) director.Update(Frame, trigger, map, manager, 10, 3);
            Need(manager.Enemies.Count(e => e.Type == type) == 1,
                $"{type} did not spawn exactly once.");
        }
        Need(manager.Enemies.Count == 2 && manager.Enemies.All(e => e.IsAlive),
            "Oracle encounter was kill-gated by the Knight.");
        foreach (Enemy enemy in manager.Enemies)
        {
            EncounterZone encounter = Encounter(map, enemy.Type);
            PlayerCharacter player = Target(map, encounter.ActivationBounds.Right + 160);
            Vector2 before = player.Position;
            for (int i = 0; i < 180; i++)
            {
                enemy.Update(Frame, player, map, Projectiles);
                director.EnforceArenaBounds();
            }
            Need(player.Position == before && enemy.IsReturningToAuthoredHome &&
                enemy.Bounds.Left >= encounter.ActivationBounds.Left &&
                enemy.Bounds.Right <= encounter.ActivationBounds.Right,
                "Elite leash moved the Player or let enemy escape authored bounds.");
        }
        Need(map.SpawnSockets.Single(s => s.SocketId == "map7-enemy-10").SuggestedEnemyTypes
                .SequenceEqual(new[] { EnemyType.ThroneboundSeraph }),
            "Pre-throne socket 10 does not contain only Enemy 70.");
    }

    private static DemonKingSanctumEliteEnemy Spawn(DungeonMap map, EnemyType type)
    {
        SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(type));
        var manager = new EnemyManager(68);
        var enemy = manager.SpawnAuthoredEnemy(type, socket, map.FindRoomContaining(socket.Position), map, 10, 3)
            as DemonKingSanctumEliteEnemy;
        Need(enemy != null, $"{type} factory/socket placement failed.");
        enemy.SetAuthoredEncounterBounds(Encounter(map, type).ActivationBounds);
        return enemy;
    }
    private static EncounterZone Encounter(DungeonMap map, EnemyType type) =>
        map.EncounterZones.Single(e => e.SuggestedEnemyTypes.Contains(type));
    private static PlayerCharacter Target(DungeonMap map, float x) => new(
        SideScrollingCollision.ResolveSafeEntrySpawn(x,
            new Vector2(PlayerCharacter.BodyHurtboxWidth, PlayerCharacter.BodyHurtboxHeight), map, 0f, 96f))
        { DebugGodMode = true };
    private static void Until(DemonKingSanctumEliteEnemy enemy, PlayerCharacter player,
        DungeonMap map, Func<bool> predicate, int frames)
    {
        for (int i = 0; i < frames && !predicate(); i++) enemy.Update(Frame, player, map, Projectiles);
        Need(predicate(), $"{enemy.Type} fixture did not reach requested finite state ({enemy.AttackKind}/{enemy.CombatState}).");
    }
    private static void Need(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
