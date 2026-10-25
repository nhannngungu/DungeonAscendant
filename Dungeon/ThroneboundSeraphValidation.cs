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

/// <summary>Headless simulation: no Game1, graphics device, or gameplay launch.</summary>
public static class ThroneboundSeraphValidation
{
    private static readonly GameTime Frame = new(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));
    private static readonly Combat.ProjectileManager Projectiles = new();

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonKingSanctumMapGenerator().Generate();
        Need(Enum.IsDefined(EnemyType.ThroneboundSeraph), "Enemy 70 registration missing.");
        var enemy = Spawn(map);
        Need(enemy.Type == EnemyType.ThroneboundSeraph && enemy.IsElite &&
            enemy.MaxPoise > 200 && enemy.MaxPoise < 260 && !enemy.IsFlying &&
            ThroneboundSeraphSpriteRenderer.VisualSize.Y >= 200,
            "Seraph registration, finite sub-boss poise or renderer failed.");
        var profile = AudioProfiles.GetEnemy(enemy.Type);
        Need(profile.Id == "thronebound_seraph", "Seraph audio profile missing.");
        foreach (EnemySoundEvent sound in Enum.GetValues<EnemySoundEvent>())
            Need(profile.GetClips(sound).Count > 0, $"Seraph {sound} audio missing.");
        foreach (Language language in new[] { Language.English, Language.Vietnamese })
        {
            var strings = Loc.GetStrings(language);
            foreach (string field in new[] { "name", "title", "description" })
                Need(strings.ContainsKey("enemy.thronebound_seraph." + field), "Seraph localization missing.");
        }
        Need(Loc.GetStrings(Language.Vietnamese)["enemy.thronebound_seraph.name"] == "THIÊN SỨ TRÓI NGAI",
            "Seraph Vietnamese canonical name incorrect.");
        var intro = new EnemyIntroductionManager();
        intro.Update(Frame, enemy.Position, new Enemy[] { enemy }, null, false);
        Need(intro.IsPresenting && !intro.BlocksCombat && intro.Duration <= 2.2f &&
            intro.Active.Identity == WildForestEnemyIdentity.ThroneboundSeraph,
            "Seraph introduction is missing, too long, or locks combat.");
        for (int i = 0; i < 200; i++) intro.Update(Frame, enemy.Position, new Enemy[] { enemy }, null, false);
        Need(intro.DiscoveryCount == 1 && !intro.IsPresenting, "Seraph introduction repeated.");
        ValidateCombat(map);
        ValidateChains(map);
        ValidateCleanupAndUi(map);
        ValidateEncounterAndTraversal(map);
    }

    private static void ValidateCombat(DungeonMap map)
    {
        var enemy = Spawn(map);
        var observed = new HashSet<SeraphState>();
        bool sawAir = false;
        Vector2 chainEnd = default, landing = default;
        bool trackingChain = false, trackingLanding = false;
        for (int phase = 1; phase <= 2; phase++)
        {
            if (phase == 2)
                enemy.ReceiveDamage(enemy.CurrentHealth - (int)(enemy.MaxHealth * .54f), 0);
            SeraphState prior = enemy.CombatState;
            int sameFrames = 0;
            for (int frame = 0; frame < 2600; frame++)
            {
                PlayerCharacter player = Target(map, enemy.Position.X - 170);
                enemy.Update(Frame, player, map, Projectiles);
                observed.Add(enemy.CombatState);
                sameFrames = enemy.CombatState == prior && enemy.CombatState != SeraphState.Ready ? sameFrames + 1 : 0;
                prior = enemy.CombatState;
                Need(sameFrames < 90, "Seraph attack state failed to terminate.");
                if (enemy.HasChain)
                {
                    if (!trackingChain) { chainEnd = enemy.ChainDestination; trackingChain = true; }
                    Need(chainEnd == enemy.ChainDestination && Vector2.Distance(enemy.ChainOrigin, chainEnd) <=
                        ThroneboundSeraphEnemy.MaximumChainReach + .1f, "Chain tracked or exceeded bounded reach.");
                    Need(enemy.ChainCooldownRemaining > 0, "Chain can lock repeatedly without cooldown.");
                }
                else trackingChain = false;
                if (enemy.HasLandingMarker)
                {
                    if (!trackingLanding) { landing = enemy.LandingPosition; trackingLanding = true; }
                    Need(enemy.LandingPosition == landing && MathF.Abs(landing.X - enemy.AscentOrigin.X) <=
                        ThroneboundSeraphEnemy.MaximumDescentOffset, "Descent homed after placement.");
                }
                else trackingLanding = false;
                if (enemy.IsFlying)
                {
                    sawAir = true;
                    Need(enemy.AscentOrigin.Y - enemy.Position.Y <= ThroneboundSeraphEnemy.MaximumAscentHeight + 1 &&
                        enemy.AscentOrigin.Y - enemy.Position.Y >= -2 &&
                        Vector2.Distance(enemy.Position, player.Position) < 360,
                        "Ascent left its short, camera-readable height.");
                }
                if (enemy.HaloLinesVisible)
                {
                    Need(enemy.HaloReach <= ThroneboundSeraphEnemy.MaximumHaloReach,
                        "Halo Break reach grew unbounded.");
                    // A grounded player-sized hurtbox fits between the center
                    // ray and the diagonal ray, not merely a point-sized gap.
                    var gap = new Rectangle((int)enemy.HaloOrigin.X + 33,
                        enemy.Bounds.Bottom - (int)PlayerCharacter.BodyHurtboxHeight,
                        (int)PlayerCharacter.BodyHurtboxWidth, (int)PlayerCharacter.BodyHurtboxHeight);
                    for (int i = 0; i < 3; i++)
                        Need(!ThroneboundSeraphEnemy.SegmentTouches(enemy.HaloOrigin,
                            enemy.HaloRayEnd(i), gap, phase == 2 ? 10 : 8), "Halo Break lost a safe gap.");
                    if (enemy.CombatState == SeraphState.HaloCharge)
                        Need(!enemy.IsCombatAttackActive, "Halo rays damage during telegraph.");
                }
                if (enemy.CombatState == SeraphState.ThrustActive)
                {
                    bool left = enemy.AttackFacesLeft;
                    enemy.Update(Frame, Target(map, enemy.Position.X + 170), map, Projectiles);
                    Need(enemy.AttackFacesLeft == left, "Committed thrust snapped 180 degrees.");
                }
            }
            Need(enemy.IntroCompletionCount == 1 && enemy.CombatPhase == phase &&
                enemy.PhaseTransitionCount == phase - 1, "Seraph intro or phase fired more than once.");
        }
        foreach (SeraphState required in new[] { SeraphState.ThrustActive, SeraphState.WingActive,
            SeraphState.ChainExtend, SeraphState.AscentRise, SeraphState.AscentHover,
            SeraphState.AscentDescent, SeraphState.AscentStrike, SeraphState.HaloCharge,
            SeraphState.HaloActive, SeraphState.PhaseShift })
            Need(observed.Contains(required), $"Seraph never exercised {required}.");
        Need(sawAir, "Seraph ascent did not rise.");
        enemy.ReceiveDamage(1, 2000);
        Need(enemy.IsStaggered && enemy.IsAlive && !enemy.HasOwnedEffects,
            "Seraph has permanent super armor or retains staggered hazards.");
    }

    private static void ValidateChains(DungeonMap map)
    {
        var enemy = Spawn(map);
        PlayerCharacter player = Target(map, enemy.Position.X - 285, godMode: false);
        Until(enemy, player, map, () => enemy.CombatState == SeraphState.ChainExtend, 300);
        Vector2 before = player.Position;
        for (int i = 0; i < 160; i++) enemy.Update(Frame, player, map, Projectiles);
        Need(enemy.ChainHitCount == 1 && enemy.ChainPullCount == 0 && player.Position == before,
            "Throne Chain failed to hit once without moving Player.");

        var room = new DungeonRoom(0, new Rectangle(100, 100, 1200, 600), RoomType.Enemy) { GroundY = 620 };
        room.AddPlatform(new Platform(new Rectangle(100, 620, 1200, 80), PlatformKind.Ground, 0));
        room.AddPlatform(new Platform(new Rectangle(600, 220, 20, 400), PlatformKind.Obstacle, 0));
        var wallMap = new DungeonMap(new[] { room }, Array.Empty<Rectangle>(), room.Bounds);
        var blocked = new ThroneboundSeraphEnemy(new Vector2(400, 552), 10, 3, 0);
        blocked.SetAuthoredEncounterBounds(room.Bounds);
        var wallPlayer = new PlayerCharacter(new Vector2(710, 620 - PlayerCharacter.BodyHurtboxHeight / 2));
        Vector2 wallBefore = wallPlayer.Position;
        Until(blocked, wallPlayer, wallMap, () => blocked.CombatState == SeraphState.ChainExtend, 300);
        Need(blocked.ChainDestination.X < 600 && !ThroneboundSeraphEnemy.HasClearLine(
            blocked.Position, wallPlayer.Position, wallMap), "Chain warning passed through a wall.");
        for (int i = 0; i < 90; i++) blocked.Update(Frame, wallPlayer, wallMap, Projectiles);
        Need(blocked.ChainHitCount == 0 && blocked.ChainPullCount == 0 && wallPlayer.Position == wallBefore,
            "Chain damaged or pulled through a wall.");
    }

    private static void ValidateCleanupAndUi(DungeonMap map)
    {
        foreach (SeraphState state in new[] { SeraphState.ChainExtend, SeraphState.AscentHover, SeraphState.HaloActive })
        {
            var enemy = Spawn(map);
            PlayerCharacter player = Target(map, enemy.Position.X - 170);
            Until(enemy, player, map, () => enemy.CombatState == state, 2200);
            int notifications = 0; enemy.SubBossDefeated += _ => notifications++;
            enemy.ReceiveDamage(enemy.CurrentHealth + 1, 2000);
            enemy.ReceiveDamage(1, 2000);
            Need(!enemy.HasOwnedEffects && !enemy.IsFlying && enemy.CompletionCount == 1 && notifications == 1,
                "Seraph death did not clear effects or completed repeatedly.");
            Need(GameRenderer.SelectSeraphSubBossBarTarget(new Enemy[] { enemy }, player.Position) == null,
                "Dead Seraph retained sub-boss UI.");
        }
        var primary = Spawn(map);
        var secondary = Spawn(map);
        PlayerCharacter target = Target(map, primary.Position.X - 170);
        for (int i = 0; i < 45; i++)
        { primary.Update(Frame, target, map, Projectiles); secondary.Update(Frame, target, map, Projectiles); }
        Need(GameRenderer.SelectSeraphSubBossBarTarget(new Enemy[] { primary, secondary }, target.Position) == primary,
            "Sub-boss UI did not choose one deterministic relevant target.");
        Need(GameRenderer.SelectSeraphSubBossBarTarget(new Enemy[] { primary },
            target.Position + new Vector2(1600, 0)) == null, "Distant Seraph retained UI.");
    }

    private static void ValidateEncounterAndTraversal(DungeonMap map)
    {
        var manager = new EnemyManager(70);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary, suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector(); director.Reset(map);
        EncounterZone encounter = Encounter(map);
        Need(encounter.IsEliteZone && !encounter.IsBossZone && encounter.MaxConcurrentEnemies == 1 &&
            map.SpawnSockets.Single(s => s.SocketId == "map7-enemy-10").SuggestedEnemyTypes
                .SequenceEqual(new[] { EnemyType.ThroneboundSeraph }) &&
            !map.SpawnSockets.Any(s => s.SocketId == "map7-neutral-10"),
            "Socket 10 still has an active placeholder or final-boss semantics.");
        Vector2 trigger = new(encounter.ActivationBounds.Left + 24, encounter.ActivationBounds.Center.Y);
        for (int i = 0; i < 100; i++) director.Update(Frame, trigger, map, manager, 10, 3);
        Need(manager.Enemies.Count == 1, "Seraph did not spawn exactly once.");
        var enemy = (ThroneboundSeraphEnemy)manager.Enemies[0];
        PlayerCharacter player = Target(map, enemy.Position.X - 170);
        Until(enemy, player, map, () => enemy.IsFlying, 2200);
        player = Target(map, encounter.ActivationBounds.Right + 180);
        Vector2 before = player.Position;
        for (int i = 0; i < 500; i++)
        {
            enemy.Update(Frame, player, map, Projectiles);
            director.EnforceArenaBounds();
        }
        Need(enemy.IsReturningToAuthoredHome && !enemy.IsFlying && !enemy.HasOwnedEffects &&
            SideScrollingCollision.IsSupported(enemy.Position, enemy.Size, map) &&
            player.Position == before && MathF.Abs(enemy.Position.X - Socket(map).Position.X) < 10,
            "Airborne disengagement did not land/return safely or moved Player.");
        Need(GameRenderer.SelectSeraphSubBossBarTarget(new Enemy[] { enemy }, player.Position) == null,
            "Leashed Seraph retained UI.");
        // Manual movement simulation runs beyond the dedicated bounds with a
        // living guardian, proving this encounter does not clamp the Player.
        player = Target(map, encounter.ActivationBounds.Right - 80);
        for (int i = 0; i < 180; i++) player.UpdateSideScrollingMovement(Frame,
            new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.D), false, map);
        Need(player.Position.X > encounter.ActivationBounds.Right + 100 && enemy.IsAlive && !director.IsCleared(encounter.ZoneId),
            "Pre-throne traversal required the Seraph's death.");
        enemy.ReceiveDamage(enemy.CurrentHealth + 1, 2000);
        var defeated = new List<Enemy>();
        Need(manager.RemoveDefeated(defeated, out int experience) == 1 && experience > 0 &&
            manager.DefeatPresentations.Contains(enemy), "Seraph death/reward/presentation was not collected.");
        Need(manager.RemoveDefeated(defeated, out _) == 0, "Seraph reward collected twice.");
        for (int i = 0; i < 100; i++) director.Update(Frame, trigger, map, manager, 10, 3);
        Need(manager.Enemies.Count == 0 && director.IsCleared(encounter.ZoneId), "Seraph respawned after completion.");
    }

    private static ThroneboundSeraphEnemy Spawn(DungeonMap map)
    {
        SpawnSocket socket = Socket(map);
        var manager = new EnemyManager(70);
        var enemy = manager.SpawnAuthoredEnemy(EnemyType.ThroneboundSeraph, socket,
            map.FindRoomContaining(socket.Position), map, 10, 3) as ThroneboundSeraphEnemy;
        Need(enemy != null, "Seraph factory/socket placement failed.");
        enemy.SetAuthoredEncounterBounds(Encounter(map).ActivationBounds);
        return enemy;
    }
    private static SpawnSocket Socket(DungeonMap map) => map.SpawnSockets.Single(s => s.SocketId == "map7-enemy-10");
    private static EncounterZone Encounter(DungeonMap map) => map.EncounterZones.Single(e => e.ZoneId == "sanctum-thronebound-seraph");
    private static PlayerCharacter Target(DungeonMap map, float x, bool godMode = true) => new(
        SideScrollingCollision.ResolveSafeEntrySpawn(x,
            new Vector2(PlayerCharacter.BodyHurtboxWidth, PlayerCharacter.BodyHurtboxHeight), map, 0, 96))
        { DebugGodMode = godMode };
    private static void Until(ThroneboundSeraphEnemy enemy, PlayerCharacter player, DungeonMap map, Func<bool> predicate, int frames)
    {
        for (int i = 0; i < frames && !predicate(); i++) enemy.Update(Frame, player, map, Projectiles);
        Need(predicate(), $"Seraph did not reach requested state ({enemy.CombatState}).");
    }
    private static void Need(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
