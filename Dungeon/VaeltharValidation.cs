using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonAscendant.Audio;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Items;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.Player;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Dungeon;

/// <summary>Deterministic simulation only: never constructs Game1 or a graphics device.</summary>
public static class VaeltharValidation
{
    private static readonly GameTime Frame = new(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));
    private static readonly ProjectileManager Projectiles = new();
    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonKingSanctumMapGenerator().Generate();
        Need(Enum.GetValues<EnemyType>()[^1] == EnemyType.Vaelthar, "Enemy pass exceeded 71.");
        VaeltharEnemy king = Spawn(map);
        Need(king.IsElite && !king.IsFlying && king.MaxPoise > 430 && king.MaxPoise < 520 &&
            VaeltharSpriteRenderer.VisualSize.Y > ThroneboundSeraphSpriteRenderer.VisualSize.Y,
            "Final boss stature, finite poise or renderer failed.");
        Enemy malachar = EnemyFactory.Create(EnemyType.Malachar,
            new EnemySpawnContext(king.Position, 10, 3, king.RoomId, true, GoblinVariant.Normal));
        Need(king.MaxHealth > malachar.MaxHealth && king.AttackDamage > malachar.AttackDamage,
            "Vaelthar does not exceed Malachar's final-ruler tier.");
        Need(AudioProfiles.GetEnemy(EnemyType.Vaelthar).Id == "vaelthar", "Vaelthar audio missing.");
        foreach (EnemySoundEvent sound in Enum.GetValues<EnemySoundEvent>())
            Need(AudioProfiles.GetEnemy(EnemyType.Vaelthar).GetClips(sound).Count > 0, $"Vaelthar {sound} missing.");
        foreach (Language language in new[] { Language.English, Language.Vietnamese })
            foreach (string field in new[] { "name", "title", "description", "phase1", "phase2", "phase3", "ultimate" })
                Need(Loc.GetStrings(language).ContainsKey("enemy.vaelthar." + field), "Vaelthar localization missing.");
        Need(Loc.GetStrings(Language.Vietnamese)["enemy.vaelthar.name"] == "VAELTHAR — MA VƯƠNG",
            "Vaelthar Vietnamese canonical name failed.");
        var introduction = new EnemyIntroductionManager();
        introduction.Update(Frame, king.Position, new Enemy[] { king }, null, false);
        Need(introduction.IsPresenting && !introduction.BlocksCombat && introduction.Duration < 3 &&
            introduction.Active.Identity == WildForestEnemyIdentity.Vaelthar, "Final boss intro locked combat.");
        for (int i = 0; i < 240; i++) introduction.Update(Frame, king.Position, new Enemy[] { king }, null, false);
        Need(introduction.DiscoveryCount == 1 && !introduction.IsPresenting, "Final boss title repeated.");
        ValidateCombat(map);
        ValidateReversal(map);
        ValidateUltimate(map);
        ValidateCleanupAndLeash(map);
        ValidateMovingEntryAndRemoteLeash(map);
        ValidateJklImmunity(map);
        ValidateSaveAndEndpoint(map);
    }

    private static void ValidateCombat(DungeonMap map)
    {
        VaeltharEnemy king = Spawn(map);
        var attacks = new HashSet<VaeltharAttack>();
        var cutLengths = new HashSet<int>();
        VaeltharLane[] lockedLanes = Array.Empty<VaeltharLane>();
        Vector2[] lockedPositions = Array.Empty<Vector2>();
        bool cooldownExpired = false, sawGuard = false, sawWave = false, sawWaveExpire = false;
        int lastUltimate = 0, lastUltimateFrame = -10000, globalFrame = 0;
        for (int phase = 1; phase <= 3; phase++)
        {
            if (phase == 2) king.ReceiveDamage(king.CurrentHealth - (int)(king.MaxHealth * .70f), 0);
            if (phase == 3)
            {
                king.ReceiveDamage(king.CurrentHealth - (int)MathF.Ceiling(king.MaxHealth * .35f), 0);
                for (int i = 0; i < 120; i++) king.Update(Frame, Follow(map, king), map, Projectiles);
                Need(king.CombatPhase == 2, "Phase three began at/above 35 percent.");
                king.ReceiveDamage(king.CurrentHealth - (int)(king.MaxHealth * .34f), 0);
            }
            VaeltharState prior = king.CombatState;
            int sameFrames = 0;
            int frames = phase == 1 ? 4200 : phase == 2 ? 6800 : 9400;
            for (int frame = 0; frame < frames; frame++, globalFrame++)
            {
                PlayerCharacter player = Follow(map, king);
                king.Update(Frame, player, map, Projectiles);
                attacks.Add(king.AttackKind);
                sameFrames = king.CombatState == prior && king.CombatState != VaeltharState.Ready ? sameFrames + 1 : 0;
                prior = king.CombatState;
                Need(sameFrames < 180, "Vaelthar state did not terminate.");
                Need(king.ComboLength <= 4 && king.ComboIndex <= king.ComboLength, "Vaelthar combo became infinite.");
                if (king.AttackKind == VaeltharAttack.KingsCut) cutLengths.Add(king.ComboLength);
                sawGuard |= king.IsReversing;
                cooldownExpired |= sawGuard && king.ReversalCooldownRemaining <= 0;
                sawWave |= king.HasWave;
                sawWaveExpire |= sawWave && !king.HasWave;
                if (king.CombatState == VaeltharState.Pursuit)
                    Need(Vector2.Distance(king.MovementOrigin, king.MovementDestination) <=
                        VaeltharEnemy.MaximumPursuitDistance + .1f, "Pursuit exceeded its bound.");
                if (king.Lanes.Count > 0)
                {
                    if (lockedLanes.Length == 0 || !ReferenceEquals(lockedLanes[0], king.Lanes[0]))
                    { lockedLanes = king.Lanes.ToArray(); lockedPositions = lockedLanes.Select(l => l.Position).ToArray(); }
                    for (int i = 0; i < lockedLanes.Length; i++)
                    {
                        VaeltharLane lane = lockedLanes[i];
                        Need(lane.Position == lockedPositions[i] && lane.ActivateAt - lane.RevealAt >= .8f &&
                            !lane.IsActive(lane.RevealAt), "Lane tracked or damaged before warning.");
                        Need(king.AuthoredArenaBounds.Contains(lane.Position.ToPoint()), "Lane escaped throne arena.");
                    }
                    var sorted = lockedLanes.OrderBy(l => l.Position.X).ToArray();
                    for (int i = 1; i < sorted.Length; i++)
                        Need(sorted[i].Area.Left - sorted[i - 1].Area.Right >= PlayerCharacter.BodyHurtboxWidth + 20,
                            "Prophecy pattern removed full-player safe lanes.");
                    if (king.AttackKind == VaeltharAttack.RoyalDecree)
                        for (int i = 1; i < lockedLanes.Length; i++)
                            Need(lockedLanes[i].RevealAt > lockedLanes[i - 1].RevealAt &&
                                lockedLanes[i].ActivateAt > lockedLanes[i - 1].ActivateAt, "Royal Decree order changed.");
                }
                if (king.UltimateCount > lastUltimate)
                {
                    Need(phase == 3 && globalFrame - lastUltimateFrame >=
                        (int)(VaeltharEnemy.UltimateCooldownSeconds * 60) - 2, "End of Kings repeated too frequently.");
                    lastUltimate = king.UltimateCount; lastUltimateFrame = globalFrame;
                }
                if (king.CombatState == VaeltharState.Active)
                {
                    bool left = king.AttackFacesLeft;
                    float opposite = king.Position.X + (left ? 170 : -170);
                    king.Update(Frame, Target(map, opposite), map, Projectiles);
                    globalFrame++;
                    Need(king.AttackFacesLeft == left, "Active attack snapped 180 degrees.");
                }
            }
            Need(king.CombatPhase == phase && king.PhaseTwoTransitionCount == (phase >= 2 ? 1 : 0) &&
                king.PhaseThreeTransitionCount == (phase == 3 ? 1 : 0) && king.IntroCompletionCount == 1,
                "Boss intro or phase thresholds fired more than once.");
        }
        foreach (VaeltharAttack attack in Enum.GetValues<VaeltharAttack>())
            Need(attacks.Contains(attack), $"Vaelthar never exercised {attack}.");
        Need(cutLengths.SetEquals(new[] { 2, 3 }) && sawGuard && cooldownExpired &&
            sawWave && sawWaveExpire && king.CounterCount == 0,
            "Melee length, finite guard/wave or valid-hit-only reversal failed.");
        king.ReceiveDamage(1, 3000);
        Need(king.IsStaggered && king.IsAlive && !king.HasOwnedEffects, "Vaelthar has permanent super armor.");
    }

    private static void ValidateReversal(DungeonMap map)
    {
        VaeltharEnemy king = Spawn(map);
        PlayerCharacter player = Follow(map, king);
        king.ReceiveDamageFrom(1, 0, player.Position);
        Need(king.ValidReversalCount == 0 && king.CounterCount == 0, "Ordinary damage armed reversal.");
        Until(king, player, map, () => king.IsReversing, 2300);
        Vector2 front = king.Position + new Vector2(king.AttackFacesLeft ? -80 : 80, 0);
        king.ReceiveDamageFrom(0, 10, front);
        king.ReceiveDamageFrom(1, 0, king.Position - (front - king.Position));
        Need(king.ValidReversalCount == 0, "Invalid reversal contact armed a counter.");
        int health = king.CurrentHealth;
        king.ReceiveDamageFrom(20, 20, front);
        Need(king.ValidReversalCount == 1 && health - king.CurrentHealth < 20 &&
            king.ReversalCooldownRemaining > 0, "Frontal reversal did not defend with finite cooldown.");
        Until(king, player, map, () => king.CombatState == VaeltharState.CounterTell, 70);
        for (int i = 0; i < 22; i++)
        { king.Update(Frame, player, map, Projectiles); Need(!king.IsCombatAttackActive, "Reversal counter was instant."); }
        Until(king, player, map, () => king.CombatState == VaeltharState.Ready, 180);
        Need(king.CounterCount == 1 && !king.IsReversing, "Reversal counter repeated or guard stayed active.");
    }

    private static void ValidateUltimate(DungeonMap map)
    {
        VaeltharEnemy king = Spawn(map);
        king.ReceiveDamage((int)(king.MaxHealth * .67f), 0);
        PlayerCharacter player = Follow(map, king);
        Until(king, player, map, () => king.IsEndOfKings, 6000);
        Need(king.CombatPhase == 3 && king.Lanes.Count == 3 && king.Lanes[^1].IsFinisher,
            "End of Kings omitted its three warnings.");
        VaeltharLane[] ordered = king.Lanes.OrderBy(l => l.Position.X).ToArray();
        float safeX = (ordered[0].Area.Right + ordered[1].Area.Left) / 2f;
        player = Target(map, safeX);
        int hits = king.AttackHitCount, laneHits = king.LaneHitCount, activations = king.LaneActivationCount;
        Vector2 before = player.Position;
        Until(king, player, map, () => king.CombatState == VaeltharState.Recovery, 700);
        Need(king.AttackHitCount == hits && king.LaneHitCount == laneHits &&
            king.LaneActivationCount == activations + 2 && player.Position == before && !king.HasOwnedEffects,
            "End of Kings has no safe lane, displaced Player, or activated unmarked damage.");
        Need(king.UltimateCooldownRemaining > 20 && king.UltimateDimRatio > 0,
            "End of Kings lacks rarity or recovery presentation.");
        for (int i = 0; i < 90; i++)
        { king.Update(Frame, player, map, Projectiles); Need(king.CombatState == VaeltharState.Recovery, "Ultimate recovery too short."); }
    }

    private static void ValidateCleanupAndLeash(DungeonMap map)
    {
        foreach (bool wave in new[] { true, false })
        {
            VaeltharEnemy king = Spawn(map);
            king.ReceiveDamage((int)(king.MaxHealth * .35f), 0);
            PlayerCharacter player = Follow(map, king);
            Until(king, player, map, () => wave ? king.HasWave : king.Lanes.Count > 0, 5000);
            int fired = 0; king.FinalBossDefeated += _ => fired++;
            king.ReceiveDamage(king.CurrentHealth + 1, 3000); king.ReceiveDamage(1);
            Need(!king.HasOwnedEffects && king.CompletionCount == 1 && fired == 1 &&
                !king.IsDeathPresentationComplete, "Death hazards or exact-once completion failed.");
            Need(GameRenderer.SelectVaeltharBossBarTarget(new Enemy[] { king }, player.Position) == null,
                "Dead king retained UI.");
            for (int i = 0; i < 100; i++) king.UpdateDeathPresentation(Frame);
            Need(!king.IsDeathPresentationComplete, "Final death did not preserve its standing/kneeling presentation.");
            for (int i = 0; i < 110; i++) king.UpdateDeathPresentation(Frame);
            Need(king.IsDeathPresentationComplete && king.CompletionCount == 1, "Final death failed to terminate.");
        }
        var living = Spawn(map);
        living.ReceiveDamage((int)(living.MaxHealth * .35f), 0);
        Until(living, Follow(map, living), map, () => living.HasOwnedEffects, 4000);
        PlayerCharacter outside = Target(map, living.AuthoredArenaBounds.Right + 160);
        Vector2 outsideBefore = outside.Position;
        for (int i = 0; i < 1000; i++)
        { living.Update(Frame, outside, map, Projectiles); living.ConstrainToAuthoredArena(living.AuthoredArenaBounds); }
        Need(living.IsReturningToAuthoredHome && !living.HasOwnedEffects && outside.Position == outsideBefore &&
            MathF.Abs(living.Position.X - Socket(map).Position.X) < 10,
            "Final boss leash did not disengage/home safely or moved Player.");
        Need(GameRenderer.SelectVaeltharBossBarTarget(new Enemy[] { living }, outside.Position) == null,
            "Leashed boss retained UI.");
        PlayerCharacter traversal = Target(map, living.AuthoredArenaBounds.Right - 100);
        for (int i = 0; i < 160; i++) traversal.UpdateSideScrollingMovement(Frame,
            new KeyboardState(Keys.D), false, map);
        Need(traversal.Position.X > living.AuthoredArenaBounds.Right + 100 && living.IsAlive,
            "Throne arena clamps Player or requires a kill for traversal.");
    }

    private static void ValidateMovingEntryAndRemoteLeash(DungeonMap map)
    {
        var manager = new EnemyManager(7100);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary, suppressProceduralSpawns: true);
        var director = new WildForestEncounterDirector(); director.Reset(map);
        EncounterZone encounter = map.EncounterZones.Single(e => e.ZoneId == VaeltharEnemy.EncounterId);
        PlayerCharacter player = Target(map, encounter.ActivationBounds.Left - 24);
        bool spawnedAhead = false;
        for (int i = 0; i < 150; i++)
        {
            player.UpdateSideScrollingMovement(Frame, new KeyboardState(Keys.D), false, map);
            director.Update(Frame, player.Position, map, manager, 10, 3);
            if (manager.Enemies.Any(e => e.Type == EnemyType.Vaelthar))
            { spawnedAhead = player.Position.X < Socket(map).Position.X - 160; break; }
        }
        Need(spawnedAhead, "Walking entry deferred the boss spawn until after passing the throne.");
        var king = (VaeltharEnemy)manager.Enemies.Single(e => e.Type == EnemyType.Vaelthar);
        king.ReceiveDamage((int)(king.MaxHealth * .35f), 0);
        Until(king, Follow(map, king), map, () => king.HasOwnedEffects, 5000);
        PlayerCharacter remote = Target(map, map.StartRoom.Center.X);
        Vector2 before = remote.Position;
        for (int i = 0; i < 1000; i++)
            manager.UpdateCombatAndAi(Frame, remote, map, Projectiles, null);
        Need(!king.HasOwnedEffects && king.IsReturningToAuthoredHome && remote.Position == before &&
            MathF.Abs(king.Position.X - Socket(map).Position.X) < 10,
            "Remote room deactivation froze final-boss hazards or prevented safe home return.");
    }

    private static void ValidateJklImmunity(DungeonMap map)
    {
        VaeltharEnemy king = Spawn(map);
        PlayerCharacter player = Follow(map, king);
        Until(king, player, map, () => king.CombatState == VaeltharState.Windup &&
            king.AttackKind == VaeltharAttack.KingsCut, 350);
        player.MoveTo(new Vector2(king.Position.X - 95, player.Position.Y));
        player.EquipDebugItem(new EquipmentItem(EquipmentCatalog.KnightLongSword, ItemRarity.Legendary));
        player.Combat.SetKnightUltimateTargetAvailable(true);
        player.Combat.Update(Frame, new CombatInput(true, true, true, false, false, false, 1),
            true, FacingDirection.Right);
        for (int i = 0; i < 8 && !player.Combat.IsUltimateControlImmune; i++)
            player.Combat.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.05)),
                new CombatInput(false, false, false, false, false, false, 0), true, FacingDirection.Right);
        Need(player.Combat.IsUltimateControlImmune, "JKL fixture did not activate.");
        var technique = player.Combat.CurrentTechnique;
        Vector2 before = player.Position;
        int health = player.CurrentHealth;
        Until(king, player, map, () => king.AttackHitCount > 0, 120);
        Need(player.CurrentHealth < health && player.Combat.IsUltimateControlImmune &&
            ReferenceEquals(player.Combat.CurrentTechnique, technique) && player.Position == before,
            "Vaelthar damage interrupted or displaced a JKL ultimate.");
    }

    private static void ValidateSaveAndEndpoint(DungeonMap map)
    {
        string directory = Path.Combine(Path.GetTempPath(), "DungeonAscendant-Vaelthar-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "save-slots.json");
        var slots = new SaveSlotManager(path);
        EncounterZone encounter = map.EncounterZones.Single(e => e.ZoneId == VaeltharEnemy.EncounterId);
        Vector2 checkpoint = SideScrollingCollision.ResolveSafeEntrySpawn(encounter.ActivationBounds.Left + 24,
            new Vector2(40, 56), map, 0, 96);
        Need(slots.Store(new SaveSlotData
        {
            SlotIndex = 0, Occupied = true, CharacterName = "VAELTHAR TEST", Level = 10,
            DungeonDepth = 7, CurrentMapId = "map-07", WorldTier = 3,
            HasCheckpoint = true, CheckpointX = checkpoint.X, CheckpointY = checkpoint.Y,
            BossDefeated = true, Map06Cleared = true, Map06ExitPrepared = true
        }), "Final boss save fixture could not be stored.");
        var session = Load(path, 7171);
        Need(!session.Map07VaeltharDefeated && !session.Map07EndpointPrepared && !session.IsExitUnlocked &&
            !session.BossDefeated, "Legacy generic defeat incorrectly unlocked the final endpoint.");
        for (int i = 0; i < 50; i++) session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Enemies.Enemies.Count(e => e.Type == EnemyType.Vaelthar) == 1,
            "Final boss failed to spawn exactly once.");
        for (int i = 0; i < 60; i++) session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Enemies.Enemies.Count(e => e.Type == EnemyType.Vaelthar) == 1,
            "Final boss duplicated on continued activation.");
        var king = (VaeltharEnemy)session.Enemies.Enemies.Single(e => e.Type == EnemyType.Vaelthar);
        Need(GameRenderer.SelectVaeltharBossBarTarget(session.Enemies.Enemies, session.Player.Position) == king,
            "Final boss UI selector did not resolve the throne ruler.");
        king.ReceiveDamage(king.CurrentHealth + 1, 3000);
        session.Update(Frame, new KeyboardState(), new MouseState());
        Need(session.Map07VaeltharDefeated && session.Map07EndpointPrepared && session.IsExitUnlocked &&
            session.BossDefeated && king.CompletionCount == 1 && session.Enemies.DefeatPresentations.Contains(king),
            "Genuine final defeat did not prepare the endpoint/presentation.");
        session.FlushActiveSave();
        SaveSlotData saved = new SaveSlotManager(path).GetSlot(0);
        Need(saved.Map07VaeltharDefeated && saved.Map07EndpointPrepared && saved.KillCount > 0,
            "Final defeat/reward state was not persisted.");
        var restored = Load(path, 7172);
        for (int i = 0; i < 150; i++) restored.Update(Frame, new KeyboardState(), new MouseState());
        Need(restored.Map07VaeltharDefeated && restored.Map07EndpointPrepared && restored.IsExitUnlocked &&
            !restored.Enemies.Enemies.Any(e => e.Type == EnemyType.Vaelthar) &&
            restored.WildForestEncounters.IsCleared(VaeltharEnemy.EncounterId), "Save/reload respawned Vaelthar.");
        restored.Player.MoveTo(restored.ExitPosition);
        int tier = restored.WorldTier;
        Press(restored, Keys.E);
        Need(restored.DungeonDepth == 7 && restored.WorldTier == tier && restored.State == GameState.Ending &&
            restored.Map07EndpointPrepared, "Prepared endpoint failed to start the short ending or changed map.");
        for (int i = 0; i < 450; i++) restored.Update(Frame, new KeyboardState(), new MouseState());
        Need(restored.Map07EndingSeen && restored.State == GameState.Playing,
            "Short ending did not complete and return control.");
        restored.ExecuteDeveloperCommand(DeveloperCommand.TeleportMap, 7, 8);
        restored.Player.MoveTo(checkpoint);
        for (int i = 0; i < 100; i++) restored.Update(Frame, new KeyboardState(), new MouseState());
        Need(!restored.Enemies.Enemies.Any(e => e.Type == EnemyType.Vaelthar), "Map regeneration respawned the defeated king.");
    }

    private static VaeltharEnemy Spawn(DungeonMap map)
    {
        var manager = new EnemyManager(71);
        SpawnSocket socket = Socket(map);
        var king = manager.SpawnAuthoredEnemy(EnemyType.Vaelthar, socket,
            map.FindRoomContaining(socket.Position), map, 10, 3) as VaeltharEnemy;
        Need(king != null, "Vaelthar factory/socket placement failed.");
        king.ConfigureArena(map); return king;
    }
    private static SpawnSocket Socket(DungeonMap map) => map.SpawnSockets.Single(s => s.SocketId == "map7-boss-vaelthar");
    private static PlayerCharacter Target(DungeonMap map, float x) => new(
        SideScrollingCollision.ResolveSafeEntrySpawn(x, new Vector2(40, 56), map, 0, 96)) { DebugGodMode = true };
    private static PlayerCharacter Follow(DungeonMap map, VaeltharEnemy king)
    {
        float x = king.Position.X - 180;
        if (x < king.AuthoredArenaBounds.Left + 50) x = king.Position.X + 180;
        return Target(map, x);
    }
    private static void Until(VaeltharEnemy king, PlayerCharacter player, DungeonMap map, Func<bool> condition, int frames)
    {
        for (int i = 0; i < frames && !condition(); i++) king.Update(Frame, player, map, Projectiles);
        Need(condition(), $"Vaelthar fixture did not reach state ({king.AttackKind}/{king.CombatState}).");
    }
    private static GameSession Load(string path, int seed)
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), randomSeed: seed, savePath: path);
        Press(session, Keys.Enter); Press(session, Keys.Enter); return session;
    }
    private static void Press(GameSession session, Keys key)
    { session.Update(Frame, new KeyboardState(key), new MouseState()); session.Update(Frame, new KeyboardState(), new MouseState()); }
    private static void Need(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
