using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonAscendant.Audio;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Graphics;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Dungeon;

/// <summary>Final integration simulations only. Never constructs Game1 or a graphics device.</summary>
public static class DemonKingSanctumIntegrationValidation
{
    private static readonly GameTime Frame = new(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60f));

    public static void ValidateOrThrow()
    {
        DungeonMap map = new DemonKingSanctumMapGenerator().Generate();
        ValidateRosterAndLeashes(map);
        ValidateOwnedProjectiles(map);
        ValidateBossSwitch(map);
        ValidateEndingSaveAndReload(map);
    }

    private static void ValidateRosterAndLeashes(DungeonMap map)
    {
        var director = new WildForestEncounterDirector();
        var manager = new EnemyManager(707);
        director.Reset(map);
        manager.Reset(map, 10, 3, RegionType.DemonKingSanctuary, suppressProceduralSpawns: true);
        // Activate every encounter without killing anything: later encounters
        // must not depend on an earlier clear, including the throne encounters.
        foreach (EncounterZone zone in map.EncounterZones)
        {
            Vector2 trigger = Ground(map, zone.ActivationBounds.Left + 24);
            for (int i = 0; i < 50; i++) director.Update(Frame, trigger, map, manager, 10, 3);
            Need(director.IsTriggered(zone.ZoneId), $"{zone.ZoneId} was gated by an earlier encounter.");
        }
        Need(manager.Enemies.Count == 10 && manager.Enemies.All(e => e.IsAlive),
            "The complete 62–71 roster did not spawn exactly once with earlier enemies alive.");
        foreach (EncounterZone zone in map.EncounterZones)
            for (int i = 0; i < 50; i++) director.Update(Frame, Ground(map, zone.ActivationBounds.Left + 24), map, manager, 10, 3);
        Need(manager.Enemies.Count == 10, "Map 7 repeated an encounter setup/spawn.");
        var projectiles = new ProjectileManager();
        var remote = new PlayerCharacter(Ground(map, map.ExitRoom.Bounds.Right - 155)) { DebugGodMode = true };
        Vector2 remoteBefore = remote.Position;
        foreach (Enemy enemy in manager.Enemies)
        {
            Need(enemy.IsSanctumEnemy && Enum.IsDefined(enemy.Type) && manager.IsActive(enemy, remote.Position, map),
                $"{enemy.Type} registration or remote leash scheduling failed.");
            string id = AudioProfiles.GetEnemy(enemy.Type).Id;
            foreach (EnemySoundEvent sound in Enum.GetValues<EnemySoundEvent>())
                Need(AudioProfiles.GetEnemy(enemy.Type).GetClips(sound).Count > 0, $"{enemy.Type}/{sound} audio missing.");
            foreach (Language language in new[] { Language.English, Language.Vietnamese })
                foreach (string field in new[] { "name", "title", "description" })
                    Need(Loc.GetStrings(language).ContainsKey($"enemy.{id}.{field}"), $"{enemy.Type} localization missing.");
            SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(enemy.Type));
            EncounterZone zone = map.EncounterZones.Single(z => z.ZoneId == socket.ZoneId);
            Vector2 home = enemy.Position;
            var target = new PlayerCharacter(Ground(map, enemy.Position.X - 95)) { DebugGodMode = true };
            Vector2 before = target.Position;
            for (int i = 0; i < 1000; i++)
            {
                enemy.Update(Frame, target, map, projectiles);
                projectiles.Update(Frame, target, map);
                director.EnforceArenaBounds();
                Rectangle bounds = enemy is VaeltharEnemy king ? king.AuthoredArenaBounds : zone.ActivationBounds;
                Need(enemy.Bounds.Left >= bounds.Left && enemy.Bounds.Right <= bounds.Right,
                    $"{enemy.Type} escaped its enemy-only leash.");
            }
            Need(target.Position == before, $"{enemy.Type} forcibly moved Player during combat.");
            for (int i = 0; i < 1800; i++) enemy.Update(Frame, remote, map, projectiles);
            Need(enemy.IsReturningToAuthoredHome && MathF.Abs(enemy.Position.X - home.X) < 10 &&
                !HasEffects(enemy) && remote.Position == remoteBefore,
                $"{enemy.Type} froze hazards, pursued outside its leash, or moved Player on disengagement.");
        }
        Need(DemonKingSanctumEnemySpriteRenderer.SentinelVisualSize.X > 0 &&
            DemonKingSanctumEnemySpriteRenderer.AcolyteVisualSize.X > 0 &&
            DemonKingSanctumPenitentSpriteRenderer.HuskVisualSize.X > 0 &&
            DemonKingSanctumPenitentSpriteRenderer.ScribeVisualSize.X > 0 &&
            DemonKingSanctumReliquarySpriteRenderer.ReliquaryVisualSize.X > 0 &&
            DemonKingSanctumReliquarySpriteRenderer.WraithVisualSize.X > 0 &&
            DemonKingSanctumEliteSpriteRenderer.KnightVisualSize.X > 0 &&
            DemonKingSanctumEliteSpriteRenderer.OracleVisualSize.X > 0 &&
            ThroneboundSeraphSpriteRenderer.VisualSize.X > 0 && VaeltharSpriteRenderer.VisualSize.X > 0,
            "A Map 7 dedicated renderer registration is missing.");
        var defeated = new List<Enemy>();
        foreach (Enemy enemy in manager.Enemies) enemy.ReceiveDamage(int.MaxValue, 3000);
        Need(manager.RemoveDefeated(defeated, out _) == 10 && manager.DefeatPresentations.Count == 10 &&
            defeated.All(e => !HasEffects(e)), "Map 7 death lost a presentation or retained effects.");
        Need(manager.RemoveDefeated(defeated, out _) == 0, "Map 7 defeat processing repeated.");
        for (int i = 0; i < 600; i++) manager.UpdateDeathPresentationsForEnding(Frame);
        Need(manager.DefeatPresentations.Count == 0, "Map 7 death presentation did not expire.");
    }

    private static bool HasEffects(Enemy enemy) => enemy switch
    {
        DemonKingSanctumEnemy e => e.HasFracturedOathMarker,
        DemonKingSanctumPenitentEnemy e => e.HasCondemnedMark || e.HasForbiddenLine,
        DemonKingSanctumReliquaryEnemy e => e.HasCloisterGrasp || e.HasEcho || e.IsReliquaryBurstActive,
        DemonKingSanctumEliteEnemy e => e.HasOwnedEffects,
        ThroneboundSeraphEnemy e => e.HasOwnedEffects,
        VaeltharEnemy e => e.HasOwnedEffects,
        _ => false
    };

    private static void ValidateOwnedProjectiles(DungeonMap map)
    {
        foreach (EnemyType type in new[] { EnemyType.VowshardAcolyte, EnemyType.AbyssScribe })
        {
            var manager = new EnemyManager(63);
            SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(type));
            Enemy owner = manager.SpawnAuthoredEnemy(type, socket, map.FindRoomContaining(socket.Position), map, 10, 3);
            var target = new PlayerCharacter(Ground(map, owner.Position.X - 280)) { DebugGodMode = true };
            var projectiles = new ProjectileManager();
            for (int i = 0; i < 1000 && projectiles.Projectiles.Count == 0; i++) owner.Update(Frame, target, map, projectiles);
            Need(projectiles.Projectiles.Count > 0 && projectiles.Projectiles.All(p => p.SourceEnemy == owner),
                $"{type} projectile ownership not assigned.");
            Enemy other = EnemyFactory.Create(EnemyType.OathboundSentinel,
                new EnemySpawnContext(owner.Position, 10, 3, owner.RoomId, false, GoblinVariant.Normal));
            projectiles.SpawnVowshard(owner.Position, Vector2.UnitX, 1, owner.RoomId, owner: other);
            owner.ReceiveDamage(int.MaxValue);
            Need(projectiles.Projectiles.Count == 1 && projectiles.Projectiles[0].SourceEnemy == other,
                $"{type} death did not clear only its owned projectiles.");
        }
    }

    private static void ValidateBossSwitch(DungeonMap map)
    {
        var manager = new EnemyManager(71);
        Enemy Spawn(EnemyType type)
        {
            SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(type));
            return manager.SpawnAuthoredEnemy(type, socket, map.FindRoomContaining(socket.Position), map, 10, 3);
        }
        var seraph = (ThroneboundSeraphEnemy)Spawn(EnemyType.ThroneboundSeraph);
        var king = (VaeltharEnemy)Spawn(EnemyType.Vaelthar);
        king.ConfigureArena(map);
        var projectiles = new ProjectileManager();
        var target = new PlayerCharacter(Ground(map, seraph.Position.X - 200)) { DebugGodMode = true };
        for (int i = 0; i < 220; i++) seraph.Update(Frame, target, map, projectiles);
        Need(GameRenderer.SelectSanctumBossBarTarget(manager.Enemies, target.Position) == seraph,
            "Seraph bar did not resolve before Vaelthar activation.");
        target.MoveTo(Ground(map, king.Position.X - 180));
        for (int i = 0; i < 220; i++) king.Update(Frame, target, map, projectiles);
        // Give both bosses an eligible position to verify priority independently
        // of the normal authored spacing: the draw path selects only one target.
        Vector2 overlap = (seraph.Position + king.Position) * .5f;
        Need(GameRenderer.SelectVaeltharBossBarTarget(manager.Enemies, overlap) == king &&
            GameRenderer.SelectSanctumBossBarTarget(manager.Enemies, overlap) == king,
            "Seraph/Vaelthar overlap did not select one unique final-boss bar.");
        king.ReceiveDamage(int.MaxValue);
        Need(GameRenderer.SelectSanctumBossBarTarget(manager.Enemies, king.Position) != king,
            "Dead Vaelthar retained his boss bar.");
    }

    private static void ValidateEndingSaveAndReload(DungeonMap map)
    {
        string path = Path.Combine(Path.GetTempPath(), "Map7-final-" + Guid.NewGuid().ToString("N"), "slots.json");
        var slots = new SaveSlotManager(path);
        Need(slots.Store(new SaveSlotData { SlotIndex = 0, Occupied = true, CharacterName = "FINAL INTEGRATION",
            Level = 10, DungeonDepth = 7, CurrentMapId = "map-07", WorldTier = 3,
            HasCheckpoint = true, CheckpointX = map.AuthoredExitPosition.Value.X,
            CheckpointY = map.AuthoredExitPosition.Value.Y }), "Ending fixture save failed.");
        GameSession session = Load(path);
        Press(session, Keys.E);
        Need(session.State == GameState.Playing && !session.Map07EndingUnlocked && session.SanctumEnding.TriggerCount == 0,
            "Ending triggered before genuine defeat.");
        // Exercise the real session path, not just terrain collision: keep all
        // ten enemies alive while walking through every encounter to the exit.
        session.Player.MoveTo(Ground(map, map.StartRoom.Bounds.Left + 120));
        session.Player.DebugGodMode = true;
        float priorX = session.Player.Position.X;
        int stillFrames = 0;
        for (int frame = 0; frame < 18000 && session.Player.Position.X < session.ExitPosition.X - 10; frame++)
        {
            bool jump = stillFrames >= 8 && session.Player.IsGrounded && frame % 2 == 0;
            session.Update(Frame, jump ? new KeyboardState(Keys.D, Keys.Space) : new KeyboardState(Keys.D), new MouseState());
            stillFrames = session.Player.Position.X <= priorX + .05f ? stillFrames + 1 : 0;
            priorX = session.Player.Position.X;
        }
        Need(session.Player.Position.X >= session.ExitPosition.X - 10 && session.Enemies.Enemies.Count == 10 &&
            session.Enemies.Enemies.All(e => e.IsAlive) && !session.Map07VaeltharDefeated && session.KillCount == 0,
            "Real Map 7 session traversal was clamped/gated, failed a later spawn, or required a kill.");
        // Defeat both real authored boss instances, not synthetic save flags.
        foreach (EnemyType type in new[] { EnemyType.ThroneboundSeraph, EnemyType.Vaelthar })
        {
            SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(type));
            EncounterZone zone = map.EncounterZones.Single(z => z.ZoneId == socket.ZoneId);
            session.Player.MoveTo(Ground(map, zone.ActivationBounds.Left + 24));
            session.Player.DebugGodMode = true;
            for (int i = 0; i < 60; i++) session.Update(Frame, new KeyboardState(), new MouseState());
            Enemy boss = session.Enemies.Enemies.Single(e => e.Type == type);
            boss.ReceiveDamage(int.MaxValue, 3000);
            session.Update(Frame, new KeyboardState(), new MouseState());
        }
        Need(session.Map07SeraphDefeated && session.Map07VaeltharDefeated && session.Map07Cleared &&
            session.Map07EndingUnlocked && !session.Map07EndingSeen, "Genuine completion failed to unlock/save the ending.");
        session.Player.MoveTo(session.ExitPosition);
        Press(session, Keys.E);
        Need(session.State == GameState.Ending && session.SanctumEnding.TriggerCount == 1,
            "Endpoint did not start exactly one ending.");
        session.FlushActiveSave();
        GameSession resumed = Load(path);
        Need(resumed.State == GameState.Playing && !resumed.Map07EndingSeen && resumed.Map07EndingUnlocked,
            "Interrupted ending was incorrectly marked seen or failed to unlock on reload.");
        foreach (EnemyType type in new[] { EnemyType.ThroneboundSeraph, EnemyType.Vaelthar })
        {
            SpawnSocket socket = map.SpawnSockets.Single(s => s.SuggestedEnemyTypes.Contains(type));
            EncounterZone zone = map.EncounterZones.Single(z => z.ZoneId == socket.ZoneId);
            resumed.Player.MoveTo(Ground(map, zone.ActivationBounds.Left + 24));
            for (int i = 0; i < 90; i++) resumed.Update(Frame, new KeyboardState(), new MouseState());
            Need(!resumed.Enemies.Enemies.Any(e => e.Type == type), $"Completed {type} respawned on reload.");
        }
        resumed.Player.MoveTo(resumed.ExitPosition);
        int tier = resumed.WorldTier, kills = resumed.KillCount;
        Press(resumed, Keys.E);
        Vector2 before = resumed.Player.Position;
        Need(resumed.State == GameState.Ending, "Reloaded endpoint did not start the ending.");
        using var audio = AudioManager.CreateDisabled();
        audio.SetMusic(MusicState.Map1Boss); audio.SetAmbience(AmbienceState.Map1Forest);
        var director = new CombatAudioDirector(audio);
        var lines = new HashSet<string>();
        for (int frame = 0; frame < 600 && resumed.State == GameState.Ending; frame++)
        {
            lines.Add(resumed.SanctumEnding.TextKey);
            resumed.Update(Frame, new KeyboardState(Keys.E, Keys.D, Keys.F10), new MouseState());
            director.Update(resumed, Frame);
            Need(resumed.Player.Position == before && resumed.CurrentDungeon.WorldBounds.Contains(resumed.Camera.ViewBounds),
                $"Ending moved Player ({before} -> {resumed.Player.Position}) or placed camera " +
                $"{resumed.Camera.ViewBounds} outside {resumed.CurrentDungeon.WorldBounds}.");
            Need(resumed.SanctumEnding.ElapsedSeconds <= SanctumEndingFlow.DurationSeconds, "Ending failed to terminate.");
        }
        Need(lines.Count == 3 && resumed.Map07EndingSeen && resumed.State == GameState.Playing &&
            resumed.DungeonDepth == 7 && resumed.WorldTier == tier && resumed.KillCount == kills &&
            resumed.SanctumEnding.TriggerCount == 1 && resumed.Projectiles.Projectiles.Count == 0 &&
            resumed.Enemies.Enemies.All(e => !HasEffects(e)) && !resumed.EnemyIntroductions.IsPresenting &&
            audio.CurrentMusicState == MusicState.None && audio.CurrentAmbienceState == AmbienceState.None,
            "Ending left stale combat/audio, failed its three lines, repeated completion, or changed progression.");
        foreach (Language language in new[] { Language.English, Language.Vietnamese })
            foreach (string key in lines.Concat(new[] { "sanctum.ending.title", "sanctum.ending.interact",
                "sanctum.ending.seen", "sanctum.ending.return" }))
                Need(Loc.GetStrings(language).TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value),
                    $"Ending {language}/{key} localization missing.");
        Press(resumed, Keys.E); Press(resumed, Keys.E);
        Need(resumed.State == GameState.Playing && resumed.SanctumEnding.TriggerCount == 1, "Seen ending retriggered.");
        SaveSlotData saved = new SaveSlotManager(path).GetSlot(0);
        Need(saved.Map07SeraphDefeated && saved.Map07VaeltharDefeated && saved.Map07Cleared &&
            saved.Map07EndpointPrepared && saved.Map07EndingSeen, "Final state was not persisted.");
        GameSession finished = Load(path);
        finished.Player.MoveTo(finished.ExitPosition);
        Press(finished, Keys.E);
        Need(finished.Map07EndingSeen && finished.Map07EndingUnlocked && finished.State == GameState.Playing &&
            finished.SanctumEnding.TriggerCount == 0, "Finished ending replayed after reload.");
        finished.Update(Frame, new KeyboardState(Keys.A), new MouseState());
        Need(finished.Player.Position.X < finished.ExitPosition.X, "Ending did not return movement control.");
        var invalid = new SaveSlotData { Occupied = true, Map07EndingSeen = true, Map07Cleared = true,
            Map07EndpointPrepared = true, BossDefeated = true };
        invalid.Normalize(0);
        Need(!invalid.Map07EndingSeen && !invalid.Map07Cleared && !invalid.Map07EndpointPrepared,
            "Unauthoritative legacy flags unlocked the ending.");
    }

    private static Vector2 Ground(DungeonMap map, float x) => SideScrollingCollision.ResolveSafeEntrySpawn(
        x, new Vector2(40, 56), map, 0, 96);
    private static GameSession Load(string path)
    {
        var session = new GameSession(new Rectangle(0, 0, 1280, 720), randomSeed: 7071, savePath: path);
        Press(session, Keys.Enter); Press(session, Keys.Enter); return session;
    }
    private static void Press(GameSession session, Keys key)
    { session.Update(Frame, new KeyboardState(), new MouseState()); session.Update(Frame, new KeyboardState(key), new MouseState()); session.Update(Frame, new KeyboardState(), new MouseState()); }
    private static void Need(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
