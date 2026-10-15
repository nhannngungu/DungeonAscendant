using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Audio;

/// <summary>
/// Converts gameplay state transitions into semantic audio events. Gameplay
/// and enemy AI remain unaware of files, variations, volume, and spatial mix.
/// </summary>
public sealed class CombatAudioDirector
{
    private sealed class EnemySnapshot
    {
        public int Health;
        public bool Staggered;
        public bool Telegraphing;
        public bool Active;
        public bool GuardBlockReacting;
        public CatacombCombatState? CatacombState;
        public FallenCityCombatState? FallenCityState;
        public DemonFrontierCombatState? DemonState;
    }

    private readonly AudioManager _audio;
    private readonly Dictionary<Enemy, EnemySnapshot> _enemies = new();
    private readonly HashSet<Projectile> _knownProjectiles = new();
    private readonly HashSet<Projectile> _currentProjectiles = new();
    private readonly HashSet<Enemy> _deathPlayed = new();
    private Player.Player _player;
    private int _playerAttackId;
    private int _playerHealth;
    private bool _playerDodging;
    private bool _playerBlockFeedback;
    private int _greatswordLandingImpactId;
    private bool _greatswordVerdictArming;
    private bool _rangerUltimateCinematic;
    private bool _sovereignUltimateCinematic;
    private bool _sovereignExecutionCinematic;
    private int _sovereignExecutionImpactId;
    private bool _ancientStaffUltimateCinematic;
    private float _idleClock = 4f;
    private float _footstepClock;
    private float _worldOneShotClock = 7f;
    private bool _uiInitialized;
    private GameState _lastGameState;
    private int _lastMenuSelection;
    private bool _lastInventoryOpen;
    private bool _lastNoteFeedback;
    private AncientScholarTombState _lastScholarState;
    private readonly Dictionary<string, TombState> _tombs = new();
    private bool _bossAlive;
    private bool _bossAttackActive;
    private bool _bossTransitioning;
    private bool _insideShrine;
    private bool _map4AudioInitialized;
    private bool _map4AnchorDestroyedFeedback;
    private DemonicCorruptionState _map4CorruptionState;
    private float _map4RiftClock = 3f;
    private int _map4AmbientSerial;

    public CombatAudioDirector(AudioManager audio)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
    }

    public void Update(GameSession session, GameTime gameTime)
    {
        _audio.Update(gameTime);
        ObserveUi(session);
        UpdateScene(session);
        if (session?.State != GameState.Playing ||
            session.Player == null ||
            session.CurrentDungeon == null)
        {
            Reset();
            return;
        }

        if (!ReferenceEquals(_player, session.Player))
            BeginObserving(session);

        ObservePlayer(session);
        ObserveProjectiles(session);
        ObserveEnemies(session, gameTime);
        ObserveBoss(session);
        ObserveWorld(session, gameTime);
    }

    private void UpdateScene(GameSession session)
    {
        if (session?.CurrentDungeon == null || session.State is GameState.Start or GameState.CharacterSelect or GameState.CharacterCreation)
        {
            _audio.SetMusic(MusicState.None);
            _audio.SetAmbience(AmbienceState.None);
            return;
        }
        if (session.State != GameState.Playing && session.State != GameState.Paused) return;
        if (session.CurrentDungeon.IsFallenCity ||
            session.CurrentDungeon.IsDemonLands)
        {
            // Maps 3 and 4 have semantic hooks, but no dedicated music or
            // ambience packs yet. Silence avoids borrowing another map's identity.
            _audio.SetMusic(MusicState.None);
            _audio.SetAmbience(AmbienceState.None);
            return;
        }
        bool map2 = session.CurrentDungeon.IsAncientCatacombs;
        bool map1Boss = !map2 && session.Boss?.IsActivated == true && session.Boss.IsAlive;
        bool map2Boss = map2 && HasActiveFallenKnight(session);
        bool combat = false;
        if (session.Player != null)
            foreach (Enemy enemy in session.Enemies.Enemies)
                if (enemy.IsAlive && Vector2.DistanceSquared(enemy.Position, session.Player.Position) < 650f*650f) { combat=true; break; }
        _audio.SetMusic(map1Boss ? MusicState.Map1Boss : map2Boss ? MusicState.Map2Boss : map2 ? (combat ? MusicState.Map2Combat : MusicState.Map2Explore) : (combat ? MusicState.Map1Combat : MusicState.Map1Explore));
        _audio.SetAmbience(map2 ? AmbienceState.Map2Catacomb : AmbienceState.Map1Forest);
    }

    private static bool HasActiveFallenKnight(GameSession session)
    {
        foreach (Enemy enemy in session.Enemies.Enemies)
            if (enemy.Type == EnemyType.FallenKnight && enemy.IsAlive &&
                (enemy is not CatacombEnemy catacomb || catacomb.CombatState != CatacombCombatState.KnightDormant)) return true;
        return false;
    }

    private void ObserveUi(GameSession session)
    {
        if (session == null) return;
        int selection = session.State switch
        {
            GameState.Start => (int)session.MainMenu.SelectedEntry,
            GameState.CharacterSelect => session.CharacterSelect.SelectedSlotIndex,
            GameState.Paused => (int)session.PauseMenu.SelectedEntry,
            _ when session.LanguageSettings.IsOpen => (int)session.LanguageSettings.SelectedRow,
            _ => 0
        };
        if (!_uiInitialized)
        {
            _uiInitialized=true; _lastGameState=session.State; _lastMenuSelection=selection;
            _lastInventoryOpen=session.IsInventoryOpen; _lastNoteFeedback=session.IsLoreNoteFeedbackVisible;
            return;
        }
        if (selection != _lastMenuSelection) _audio.PlayUi(UiSoundEvent.Select);
        if (session.State != _lastGameState) _audio.PlayUi(session.State == GameState.Playing ? UiSoundEvent.Back : UiSoundEvent.Confirm);
        if (session.IsInventoryOpen && !_lastInventoryOpen) _audio.PlayUi(UiSoundEvent.InventoryOpen);
        if (session.IsLoreNoteFeedbackVisible && !_lastNoteFeedback) _audio.PlayUi(UiSoundEvent.NotePickup);
        _lastGameState=session.State; _lastMenuSelection=selection; _lastInventoryOpen=session.IsInventoryOpen; _lastNoteFeedback=session.IsLoreNoteFeedbackVisible;
    }

    private void ObserveWorld(GameSession session, GameTime gameTime)
    {
        if (session.CurrentDungeon.IsDemonLands)
        {
            ObserveDemonLandsWorld(session, gameTime);
            return;
        }
        if (session.CurrentDungeon.IsFallenCity)
            return;
        float dt=MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds,.1f);
        _footstepClock-=dt; _worldOneShotClock-=dt;
        bool map2=session.CurrentDungeon.IsAncientCatacombs;
        bool insideShrine=false;
        if (map2)
            foreach (var shrine in session.CurrentDungeon.SafeShrines)
                if (Vector2.DistanceSquared(session.Player.Position,shrine.Position)<=shrine.Radius*shrine.Radius) { insideShrine=true; break; }
        if (insideShrine && !_insideShrine)
            _audio.PlayWorld(WorldSoundEvent.Map2Shrine,session.Player.Position,session.Player.Position);
        _insideShrine=insideShrine;
        if (session.Player.IsGrounded && MathF.Abs(session.Player.Velocity.X)>30f && _footstepClock<=0f)
        {
            bool map1Stone = !map2 && session.CurrentDungeon.BossRoom.Bounds.Contains(session.Player.Position.ToPoint());
            _audio.PlayWorld(map2 ? WorldSoundEvent.Map2StoneStep : map1Stone ? WorldSoundEvent.Map1StoneStep : WorldSoundEvent.Map1GrassStep,session.Player.Position,session.Player.Position);
            _footstepClock=.34f;
        }
        if (_worldOneShotClock<=0f)
        {
            Vector2 source=session.Player.Position+new Vector2(session.Player.Facing==FacingDirection.Left?-240f:240f,0f);
            _audio.PlayWorld(map2?WorldSoundEvent.Map2ChainRattle:WorldSoundEvent.Map1BranchSnap,source,session.Player.Position);
            _worldOneShotClock=9f;
        }
        foreach (KeyValuePair<string,TombRuntime> pair in session.Tombs.States)
        {
            if (_tombs.TryGetValue(pair.Key,out TombState previous) && previous!=pair.Value.State)
            {
                Vector2 source=pair.Value.Definition.Bounds.Center.ToVector2();
                if (pair.Value.State==TombState.Disturbed) _audio.PlayWorld(WorldSoundEvent.Map2TombRumble,source,session.Player.Position);
                else if (pair.Value.State is TombState.Opened or TombState.Empty) _audio.PlayWorld(WorldSoundEvent.Map2CoffinOpen,source,session.Player.Position);
            }
            _tombs[pair.Key]=pair.Value.State;
        }
        if (session.ScholarTomb.State==AncientScholarTombState.Opening && _lastScholarState!=AncientScholarTombState.Opening)
            _audio.PlayWorld(WorldSoundEvent.Map2ScholarCoffin,session.ScholarTomb.Position,session.Player.Position);
        _lastScholarState=session.ScholarTomb.State;
    }

    private void ObserveDemonLandsWorld(GameSession session, GameTime gameTime)
    {
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .1f);
        _worldOneShotClock -= dt;
        _map4RiftClock -= dt;

        if (!_map4AudioInitialized)
        {
            _map4AudioInitialized = true;
            _map4CorruptionState = session.Corruption.State;
            _map4AnchorDestroyedFeedback =
                session.CorruptionAnchors.DestroyedFeedbackTimeRemaining > 0f;
        }

        if (session.Corruption.State != _map4CorruptionState)
        {
            _audio.PlayWorld(WorldSoundEvent.Map4Corruption,
                session.Player.Position, session.Player.Position);
            _map4CorruptionState = session.Corruption.State;
        }

        bool destroyedFeedback =
            session.CorruptionAnchors.DestroyedFeedbackTimeRemaining > 0f;
        if (destroyedFeedback && !_map4AnchorDestroyedFeedback)
        {
            Vector2 source = session.Player.Position;
            string destroyedId = session.CorruptionAnchors.LastDestroyedAnchorId;
            if (destroyedId.Length > 0 &&
                session.CorruptionAnchors.States.TryGetValue(
                    destroyedId, out CorruptionAnchorRuntime destroyed))
                source = destroyed.Definition.Position;
            _audio.PlayWorld(WorldSoundEvent.Map4AnchorDestroyed,
                source, session.Player.Position);
        }
        _map4AnchorDestroyedFeedback = destroyedFeedback;

        DemonLandsZoneDefinition currentZone = null;
        foreach (DemonLandsZoneDefinition zone in
            session.CurrentDungeon.DemonLandsZones)
        {
            if (zone.Bounds.Contains(session.Player.Position.ToPoint()))
            {
                currentZone = zone;
                break;
            }
        }

        if (_worldOneShotClock <= 0f)
        {
            WorldSoundEvent ambient = GetDemonLandsAmbientEvent(
                currentZone?.Kind ?? DemonLandsZoneKind.BlackenedFrontier,
                _map4AmbientSerial++);
            foreach (CorruptionAnchorRuntime anchor in
                session.CorruptionAnchors.States.Values)
            {
                if (anchor.IsActive && Vector2.DistanceSquared(
                    anchor.Definition.Position, session.Player.Position) <=
                    210f * 210f)
                {
                    ambient = WorldSoundEvent.Map4AnchorActive;
                    break;
                }
            }
            float direction = session.Player.Facing == FacingDirection.Left ? -1f : 1f;
            Vector2 source = session.Player.Position +
                new Vector2(direction * 250f, -70f);
            _audio.PlayWorld(ambient, source, session.Player.Position);
            _worldOneShotClock = currentZone?.Kind is
                DemonLandsZoneKind.AshWastes or DemonLandsZoneKind.RiftFields
                    ? 7f
                    : 10f;
        }

        bool nearRift = false;
        foreach (CorruptionFieldDefinition field in
            session.CurrentDungeon.CorruptionFields)
        {
            if (!field.IsRift ||
                (field.AnchorId.Length > 0 &&
                 !session.CorruptionAnchors.IsActive(field.AnchorId)))
                continue;
            Rectangle proximity = field.Bounds;
            proximity.Inflate(130, 100);
            if (proximity.Contains(session.Player.Position.ToPoint()))
            {
                nearRift = true;
                if (_map4RiftClock <= 0f)
                    _audio.PlayWorld(WorldSoundEvent.Map4Rift,
                        field.Bounds.Center.ToVector2(), session.Player.Position);
                break;
            }
        }
        if (nearRift && _map4RiftClock <= 0f)
            _map4RiftClock = 6f;
        else if (!nearRift)
            _map4RiftClock = MathF.Min(_map4RiftClock, 2f);
    }

    private static WorldSoundEvent GetDemonLandsAmbientEvent(
        DemonLandsZoneKind zone,
        int serial) => zone switch
    {
        DemonLandsZoneKind.BlackenedFrontier =>
            (serial & 1) == 0
                ? WorldSoundEvent.Map4DemonicAmbience
                : WorldSoundEvent.Map4DistantDemonCry,
        DemonLandsZoneKind.AshWastes => WorldSoundEvent.Map4AshWind,
        DemonLandsZoneKind.FleshRavine => WorldSoundEvent.Map4Corruption,
        DemonLandsZoneKind.RiftFields => WorldSoundEvent.Map4Rift,
        DemonLandsZoneKind.BoneForest => WorldSoundEvent.Map4BoneCreak,
        DemonLandsZoneKind.InfernalQuarry => (serial & 1) == 0
            ? WorldSoundEvent.Map4QuarryChains
            : WorldSoundEvent.Map4LavaFire,
        DemonLandsZoneKind.BloodShrine => WorldSoundEvent.Map4RitualChant,
        DemonLandsZoneKind.LegionCamp => (serial & 1) == 0
            ? WorldSoundEvent.Map4Campfire
            : WorldSoundEvent.Map4DistantWar,
        DemonLandsZoneKind.GateOfHorns => (serial & 1) == 0
            ? WorldSoundEvent.Map4GateChains
            : WorldSoundEvent.Map4LavaFire,
        DemonLandsZoneKind.InfernalHeart => (serial & 1) == 0
            ? WorldSoundEvent.Map4InfernalHeart
            : WorldSoundEvent.Map4LavaFire,
        _ => WorldSoundEvent.Map4DemonicAmbience
    };

    private void ObserveBoss(GameSession session)
    {
        if (session.Boss == null) return;
        var boss=session.Boss;
        if (boss.IsActivated && boss.IsAlive && !_bossAlive) _audio.PlayBoss(BossSoundEvent.Idle,boss.Position,session.Player.Position);
        if (boss.Attack.IsActive && !_bossAttackActive) _audio.PlayBoss(BossSoundEvent.Attack,boss.Position,session.Player.Position);
        if (boss.IsPhaseTransitioning && !_bossTransitioning) _audio.PlayBoss(BossSoundEvent.Special,boss.Position,session.Player.Position);
        if (!boss.IsAlive && _bossAlive) { _audio.PlayBoss(BossSoundEvent.Death,boss.Position,session.Player.Position); _audio.PlayImpact(ImpactMaterial.Wood,boss.Position,session.Player.Position); }
        _bossAlive=boss.IsAlive; _bossAttackActive=boss.Attack.IsActive; _bossTransitioning=boss.IsPhaseTransitioning;
    }

    private void BeginObserving(GameSession session)
    {
        Reset();
        _player = session.Player;
        _playerAttackId = _player.Combat.AttackId;
        _playerHealth = _player.CurrentHealth;
        _playerDodging = _player.Combat.IsDodging;
        _playerBlockFeedback = _player.Combat.IsBlockFeedbackActive;
        _greatswordLandingImpactId =
            _player.Combat.GreatswordLandingImpactId;
        _greatswordVerdictArming =
            _player.WeaponFamily == WeaponFamily.GreatSword &&
            _player.Combat.GreatswordUltimateCinematicRatio >= .72f;
        _rangerUltimateCinematic =
            _player.Combat.RangerUltimateCinematicRatio >= .72f;
        _sovereignUltimateCinematic =
            _player.Combat.SovereignUltimateCinematicRatio >= .70f;
        _sovereignExecutionCinematic =
            session.IsSovereignExecutionActive;
        _sovereignExecutionImpactId = session.SovereignExecutionImpactId;
        _ancientStaffUltimateCinematic =
            _player.Combat.AncientStaffUltimateCinematicRatio >= .70f;
        foreach (Projectile projectile in session.Projectiles.Projectiles)
            _knownProjectiles.Add(projectile);
    }

    private void ObservePlayer(GameSession session)
    {
        var player = session.Player;
        if (player.Combat.AttackId != _playerAttackId &&
            player.Combat.CurrentAttack != null)
        {
            bool sovereignUltimate = player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm;
            bool ancientStaffUltimate = player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate;
            int stage = player.Combat.TechniqueStageIndex;
            if (!sovereignUltimate && !ancientStaffUltimate)
            {
                PlayerSoundEvent soundEvent =
                    player.Combat.State == CombatState.HeavyAttack
                        ? PlayerSoundEvent.HeavyAttack
                        : PlayerSoundEvent.Swing;
                _audio.PlayPlayer(soundEvent, player.WeaponFamily);
            }
            else if (ancientStaffUltimate)
            {
                if (stage is >= 3 and <= 7)
                {
                    _audio.PlayPlayer(PlayerSoundEvent.AttackRelease,
                        player.WeaponFamily);
                }
                else if (stage is 1 or 8)
                {
                    _audio.PlayPlayer(PlayerSoundEvent.HeavyAttack,
                        player.WeaponFamily);
                }
                else if (stage is 0 or 2)
                {
                    _audio.PlayPlayer(PlayerSoundEvent.Special,
                        player.WeaponFamily);
                }
            }
            else if (stage == AbyssRingTuning.UltimateStrikeCount + 1)
            {
                _audio.PlayPlayer(PlayerSoundEvent.HeavyAttack,
                    player.WeaponFamily);
            }
            else if (stage is 1 or 4 or 8 or 12 or 16)
            {
                _audio.PlayPlayer(PlayerSoundEvent.AttackRelease,
                    player.WeaponFamily);
            }
        }
        if (player.Combat.IsDodging && !_playerDodging)
            _audio.PlayPlayer(PlayerSoundEvent.Dash, player.WeaponFamily);
        if (player.Combat.IsBlockFeedbackActive && !_playerBlockFeedback)
            _audio.PlayPlayer(PlayerSoundEvent.BlockImpact, player.WeaponFamily);
        if (player.Combat.GreatswordLandingImpactId !=
            _greatswordLandingImpactId)
        {
            _audio.PlayPlayer(PlayerSoundEvent.HeavyAttack, player.WeaponFamily);
            _audio.PlayImpact(
                ImpactMaterial.Stone,
                player.Position,
                player.Position);
        }
        bool verdictArming = player.WeaponFamily == WeaponFamily.GreatSword &&
            player.Combat.GreatswordUltimateCinematicRatio >= .72f;
        if (verdictArming && !_greatswordVerdictArming)
            _audio.PlayPlayer(PlayerSoundEvent.Special, player.WeaponFamily);
        bool rangerCinematic =
            player.Combat.RangerUltimateCinematicRatio >= .72f;
        if (rangerCinematic && !_rangerUltimateCinematic)
            _audio.PlayPlayer(PlayerSoundEvent.Special, player.WeaponFamily);
        bool sovereignCinematic =
            player.Combat.SovereignUltimateCinematicRatio >= .70f;
        if (sovereignCinematic && !_sovereignUltimateCinematic)
            _audio.PlayPlayer(PlayerSoundEvent.Special, player.WeaponFamily);
        bool ancientStaffCinematic =
            player.Combat.AncientStaffUltimateCinematicRatio >= .70f;
        if (ancientStaffCinematic && !_ancientStaffUltimateCinematic)
            _audio.PlayPlayer(PlayerSoundEvent.Special, player.WeaponFamily);
        bool sovereignExecution =
            session.IsSovereignExecutionActive;
        if (sovereignExecution && !_sovereignExecutionCinematic)
            _audio.PlayPlayer(PlayerSoundEvent.Special, player.WeaponFamily);
        if (session.SovereignExecutionImpactId !=
            _sovereignExecutionImpactId)
        {
            Rectangle target = session.SovereignExecutionTargetBounds;
            Vector2 impactPosition = target.IsEmpty
                ? player.Position
                : target.Center.ToVector2();
            _audio.PlayPlayer(PlayerSoundEvent.HeavyAttack,
                player.WeaponFamily);
            _audio.PlayImpact(
                ImpactMaterial.Stone,
                impactPosition,
                player.Position);
        }
        if (player.CurrentHealth < _playerHealth && player.IsAlive)
        {
            _audio.PlayPlayer(PlayerSoundEvent.Hit, player.WeaponFamily);
            _audio.PlayImpact(ImpactMaterial.Flesh, player.Position, player.Position);
        }

        _playerAttackId = player.Combat.AttackId;
        _playerHealth = player.CurrentHealth;
        _playerDodging = player.Combat.IsDodging;
        _playerBlockFeedback = player.Combat.IsBlockFeedbackActive;
        _greatswordLandingImpactId =
            player.Combat.GreatswordLandingImpactId;
        _greatswordVerdictArming = verdictArming;
        _rangerUltimateCinematic = rangerCinematic;
        _sovereignUltimateCinematic = sovereignCinematic;
        _sovereignExecutionCinematic = sovereignExecution;
        _sovereignExecutionImpactId = session.SovereignExecutionImpactId;
        _ancientStaffUltimateCinematic = ancientStaffCinematic;
    }

    private void ObserveProjectiles(GameSession session)
    {
        _currentProjectiles.Clear();
        foreach (ProjectileImpact impact in session.Projectiles.Impacts)
        {
            if (impact.Projectile.Type == ProjectileType.PlagueFlask)
            {
                _audio.PlayImpact(
                    ImpactMaterial.Wood,
                    impact.Position,
                    session.Player.Position);
                continue;
            }
            if (impact.Projectile.Type != ProjectileType.CrossbowBolt)
                continue;
            _audio.PlayImpact(
                impact.Projectile.EmpoweredVisual
                    ? ImpactMaterial.MetalArmor
                    : ImpactMaterial.Wood,
                impact.Position,
                session.Player.Position);
        }
        foreach (Projectile projectile in session.Projectiles.Projectiles)
        {
            _currentProjectiles.Add(projectile);
            if (!_knownProjectiles.Contains(projectile) && projectile.IsPlayerOwned)
            {
                _audio.PlayPlayer(
                    PlayerSoundEvent.AttackRelease,
                    session.Player.WeaponFamily);
            }
        }
        _knownProjectiles.RemoveWhere(projectile => !_currentProjectiles.Contains(projectile));
        foreach (Projectile projectile in _currentProjectiles)
            _knownProjectiles.Add(projectile);
    }

    private void ObserveEnemies(GameSession session, GameTime gameTime)
    {
        Vector2 listener = session.Player.Position;
        foreach (Enemy enemy in session.DefeatedEnemiesThisFrame)
        {
            if (_deathPlayed.Add(enemy))
                PlayEnemyDeath(enemy, listener);
            _enemies.Remove(enemy);
            _deathPlayed.Remove(enemy);
        }

        var present = new HashSet<Enemy>();
        Enemy nearestIdleCandidate = null;
        float nearestDistanceSquared = 650f * 650f;
        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            present.Add(enemy);
            float distanceSquared = Vector2.DistanceSquared(enemy.Position, listener);
            if (enemy.IsAlive && distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestIdleCandidate = enemy;
            }

            if (!_enemies.TryGetValue(enemy, out EnemySnapshot previous))
            {
                _enemies[enemy] = Capture(enemy);
                continue;
            }

            if (enemy.CurrentHealth < previous.Health)
            {
                EnemySoundProfile profile = AudioProfiles.GetEnemy(enemy.Type);
                _audio.PlayPlayer(PlayerSoundEvent.Hit, session.Player.WeaponFamily);
                if (enemy.IsAlive)
                    _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Hit, enemy.Position, listener);
                else if (_deathPlayed.Add(enemy))
                    _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Death, enemy.Position, listener);
                _audio.PlayImpact(profile.ImpactMaterial, enemy.Position, listener);
            }
            if (enemy.IsStaggered && !previous.Staggered)
                _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Stagger, enemy.Position, listener);

            if (enemy is CatacombEnemy catacomb)
            {
                ObserveCatacombAttack(catacomb, previous, listener);
                if (catacomb.IsGuardBlockReacting && !previous.GuardBlockReacting)
                    _audio.PlayEnemy(
                        catacomb.Type,
                        EnemySoundEvent.Special,
                        catacomb.Position,
                        listener);
            }
            else if (enemy is FallenCityEnemy city)
                ObserveFallenCityAttack(city, previous, listener);
            else
                ObserveStandardAttack(enemy, previous, listener);

            _enemies[enemy] = Capture(enemy);
        }

        var removed = new List<Enemy>();
        foreach (Enemy enemy in _enemies.Keys)
            if (!present.Contains(enemy)) removed.Add(enemy);
        foreach (Enemy enemy in removed)
            _enemies.Remove(enemy);

        _idleClock -= MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .1f);
        if (_idleClock <= 0f && nearestIdleCandidate != null)
        {
            _audio.PlayEnemy(
                nearestIdleCandidate.Type,
                EnemySoundEvent.Idle,
                nearestIdleCandidate.Position,
                listener);
            _idleClock = 5f + (nearestIdleCandidate.GetHashCode() & 3);
        }
    }

    private void ObserveStandardAttack(
        Enemy enemy,
        EnemySnapshot previous,
        Vector2 listener)
    {
        if (enemy.IsCombatAttackTelegraphing && !previous.Telegraphing)
            _audio.PlayEnemy(enemy.Type, EnemySoundEvent.AttackWindup, enemy.Position, listener);
        if (enemy.IsCombatAttackActive && !previous.Active)
            _audio.PlayEnemy(enemy.Type, EnemySoundEvent.AttackRelease, enemy.Position, listener);
        if (enemy is DemonLandsEnemy demon &&
            previous.DemonState != demon.FrontierCombatState &&
            IsDemonSpecialState(demon.FrontierCombatState))
        {
            _audio.PlayEnemy(
                enemy.Type,
                EnemySoundEvent.Special,
                enemy.Position,
                listener);
        }
    }

    private static bool IsDemonSpecialState(DemonFrontierCombatState state) =>
        state is DemonFrontierCombatState.LegionCommandGuard or
            DemonFrontierCombatState.LegionGuardBroken or
            DemonFrontierCombatState.GeneralPhaseTransition or
            DemonFrontierCombatState.GeneralGuard or
            DemonFrontierCombatState.GeneralGuardBroken or
            DemonFrontierCombatState.GeneralPulseActive or
            DemonFrontierCombatState.GeneralAnchorActive or
            DemonFrontierCombatState.GeneralChargeActive or
            DemonFrontierCombatState.GeneralLegionsEndRaise or
            DemonFrontierCombatState.GeneralLegionsEndCharge or
            DemonFrontierCombatState.GeneralLegionsEndCleave;

    private void ObserveFallenCityAttack(
        FallenCityEnemy enemy,
        EnemySnapshot previous,
        Vector2 listener)
    {
        if (previous.FallenCityState != enemy.CombatState)
        {
            bool bishopSupportRelease = enemy.Type == EnemyType.WarBishop &&
                enemy.CombatState == FallenCityCombatState.BishopActive &&
                (enemy.WarBishopAction is WarBishopActionKind.WarChant or
                    WarBishopActionKind.CommandingSeal or
                    WarBishopActionKind.FinalBenediction);
            bool kingSpecial = enemy.Type == EnemyType.BrokenKing &&
                (enemy.CombatState is FallenCityCombatState.KingTransition or
                    FallenCityCombatState.KingGuard or
                    FallenCityCombatState.KingCounterTell or
                    FallenCityCombatState.KingLastCommandPause);
            if (bishopSupportRelease || kingSpecial)
            {
                _audio.PlayEnemy(
                    enemy.Type,
                    EnemySoundEvent.Special,
                    enemy.Position,
                    listener);
            }
        }

        if (enemy.IsCombatAttackTelegraphing && !previous.Telegraphing)
        {
            _audio.PlayEnemy(
                enemy.Type,
                EnemySoundEvent.AttackWindup,
                enemy.Position,
                listener);
        }
        if (!enemy.IsCombatAttackActive || previous.Active)
            return;

        _audio.PlayEnemy(
            enemy.Type,
            enemy.Type == EnemyType.BellWatcher &&
                enemy.IsAlarmSignalActive
                    ? EnemySoundEvent.Special
                    : EnemySoundEvent.AttackRelease,
            enemy.Position,
            listener);

        if (enemy.Type == EnemyType.BrokenKing &&
            (enemy.BrokenKingAttack is BrokenKingAttackKind.AshCleave or
                BrokenKingAttackKind.CollapseStrike or
                BrokenKingAttackKind.LastCommand))
        {
            _audio.PlayEnemy(
                enemy.Type,
                EnemySoundEvent.Special,
                enemy.Position,
                listener);
        }
    }

    private void ObserveCatacombAttack(
        CatacombEnemy enemy,
        EnemySnapshot previous,
        Vector2 listener)
    {
        if (previous.CatacombState == enemy.CombatState)
            return;
        if (IsWindup(enemy.CombatState))
            _audio.PlayEnemy(enemy.Type, EnemySoundEvent.AttackWindup, enemy.Position, listener);
        if (IsRelease(enemy.CombatState))
        {
            _audio.PlayEnemy(enemy.Type, EnemySoundEvent.AttackRelease, enemy.Position, listener);
            if (IsSpecialAttack(enemy))
                _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Special, enemy.Position, listener);
        }
        if (IsSpecial(enemy.CombatState))
            _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Special, enemy.Position, listener);
    }

    private static bool IsWindup(CatacombCombatState state) => state is
        CatacombCombatState.MeleeWindup or
        CatacombCombatState.ArcherNock or
        CatacombCombatState.ArcherDraw or
        CatacombCombatState.BowShoveWindup or
        CatacombCombatState.CorpseWindup or
        CatacombCombatState.WraithWindup or
        CatacombCombatState.GuardWindup or
        CatacombCombatState.GraveWindup or
        CatacombCombatState.KnightWindup or
        CatacombCombatState.SoulWindup;

    private static bool IsRelease(CatacombCombatState state) => state is
        CatacombCombatState.MeleeActive or
        CatacombCombatState.ArcherRelease or
        CatacombCombatState.BowShoveActive or
        CatacombCombatState.CorpseActive or
        CatacombCombatState.WraithActive or
        CatacombCombatState.GuardActive or
        CatacombCombatState.GraveActive or
        CatacombCombatState.KnightActive or
        CatacombCombatState.SoulActive;

    private static bool IsSpecial(CatacombCombatState state) => state is
        CatacombCombatState.WraithEthereal or
        CatacombCombatState.WraithMaterialize or
        CatacombCombatState.KnightTransition;

    private static bool IsSpecialAttack(CatacombEnemy enemy) => enemy.Type switch
    {
        EnemyType.RottenCorpse =>
            enemy.RottenCorpseAttack == RottenCorpseAttackKind.RotVomit,
        EnemyType.Wraith => enemy.WraithAttack is
            WraithAttackKind.SpectralDash or WraithAttackKind.CurseWave,
        EnemyType.UndeadGuard =>
            enemy.UndeadGuardAttack == UndeadGuardAttackKind.ShieldBash,
        EnemyType.GraveBat =>
            enemy.GraveBatAttack == GraveBatAttackKind.GraveScreech,
        EnemyType.CursedKnight =>
            enemy.CursedKnightAttack == CursedKnightAttackKind.ExecutionStrike,
        EnemyType.SoulCollector =>
            enemy.SoulCollectorAttack != SoulCollectorAttackKind.SoulBolt,
        EnemyType.DeathKnight => enemy.DeathKnightAttack is
            DeathKnightAttackKind.DreadCharge or
            DeathKnightAttackKind.TombbreakerSlam,
        EnemyType.FallenKnight => enemy.FallenKnightAttack is
            FallenKnightAttackKind.CursedExtensionSlash or
            FallenKnightAttackKind.GraveStep or
            FallenKnightAttackKind.CursedCounter or
            FallenKnightAttackKind.FinalOathCleave or
            FallenKnightAttackKind.OathbreakerRush or
            FallenKnightAttackKind.LastJudgment,
        _ => false
    };

    private void PlayEnemyDeath(Enemy enemy, Vector2 listener)
    {
        EnemySoundProfile profile = AudioProfiles.GetEnemy(enemy.Type);
        _audio.PlayEnemy(enemy.Type, EnemySoundEvent.Death, enemy.Position, listener);
        _audio.PlayImpact(profile.ImpactMaterial, enemy.Position, listener);
    }

    private static EnemySnapshot Capture(Enemy enemy) => new()
    {
        Health = enemy.CurrentHealth,
        Staggered = enemy.IsStaggered,
        Telegraphing = enemy.IsCombatAttackTelegraphing,
        Active = enemy.IsCombatAttackActive,
        GuardBlockReacting = enemy is CatacombEnemy guard &&
            guard.IsGuardBlockReacting,
        CatacombState = enemy is CatacombEnemy catacomb
            ? catacomb.CombatState
            : null,
        FallenCityState = enemy is FallenCityEnemy fallenCity
            ? fallenCity.CombatState
            : null,
        DemonState = enemy is DemonLandsEnemy demon
            ? demon.FrontierCombatState
            : null
    };

    private void Reset()
    {
        _enemies.Clear();
        _knownProjectiles.Clear();
        _currentProjectiles.Clear();
        _deathPlayed.Clear();
        _player = null;
        _idleClock = 4f;
        _tombs.Clear();
        _bossAlive=false;
        _bossAttackActive=false;
        _bossTransitioning=false;
        _insideShrine=false;
        _map4AudioInitialized=false;
        _map4AnchorDestroyedFeedback=false;
        _map4CorruptionState=DemonicCorruptionState.Stable;
        _map4RiftClock=3f;
        _map4AmbientSerial=0;
        _rangerUltimateCinematic=false;
        _sovereignUltimateCinematic=false;
        _sovereignExecutionCinematic=false;
        _ancientStaffUltimateCinematic=false;
    }
}
