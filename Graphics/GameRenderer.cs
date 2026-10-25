using System;
using System.Collections.Generic;
using DungeonAscendant.Bosses;
using DungeonAscendant.Combat;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Lore;
using DungeonAscendant.Localization;
using DungeonAscendant.Persistence;
using DungeonAscendant.Player;
using DungeonAscendant.UI;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Owns MonoGame-specific drawing for the active game session.
/// </summary>
public sealed class GameRenderer : IDisposable
{
    private static readonly float[] VaeltharPhaseThresholds = { .70f, .35f };
    private static readonly string[] ScholarQuestionLabels =
    {
        "QUESTION 1 / 3",
        "QUESTION 2 / 3",
        "QUESTION 3 / 3"
    };
    private static readonly string[] ScholarAnswerLabels =
    {
        "ANSWER 1 / 3",
        "ANSWER 2 / 3",
        "ANSWER 3 / 3"
    };
    private static readonly string[] ScholarTruthLabels =
    {
        "TRUTHS REMEMBERED: 0",
        "TRUTHS REMEMBERED: 1",
        "TRUTHS REMEMBERED: 2",
        "TRUTHS REMEMBERED: 3"
    };
    private static readonly string[] ScholarFinalScoreLabels =
    {
        "FINAL SCORE: 0 / 3",
        "FINAL SCORE: 1 / 3",
        "FINAL SCORE: 2 / 3",
        "FINAL SCORE: 3 / 3"
    };
    private static readonly CharacterCreatorRow[] CharacterCreatorSummaryRows =
    {
        CharacterCreatorRow.CharacterName,
        CharacterCreatorRow.BodyType,
        CharacterCreatorRow.BodyBuild,
        CharacterCreatorRow.Face,
        CharacterCreatorRow.SkinTone,
        CharacterCreatorRow.Hair,
        CharacterCreatorRow.HairColor,
        CharacterCreatorRow.FacialHair,
        CharacterCreatorRow.FaceDetail,
        CharacterCreatorRow.TorsoWidth,
        CharacterCreatorRow.ArmThickness,
        CharacterCreatorRow.LegThickness,
        CharacterCreatorRow.ApparentHeight
    };

    private static readonly IReadOnlyDictionary<char, byte[]> DebugGlyphs =
        new Dictionary<char, byte[]>
        {
            ['A'] = new byte[] { 14, 17, 17, 31, 17, 17, 17 },
            ['B'] = new byte[] { 30, 17, 17, 30, 17, 17, 30 },
            ['C'] = new byte[] { 15, 16, 16, 16, 16, 16, 15 },
            ['D'] = new byte[] { 30, 17, 17, 17, 17, 17, 30 },
            ['E'] = new byte[] { 31, 16, 16, 30, 16, 16, 31 },
            ['F'] = new byte[] { 31, 16, 16, 30, 16, 16, 16 },
            ['G'] = new byte[] { 14, 17, 16, 23, 17, 17, 14 },
            ['H'] = new byte[] { 17, 17, 17, 31, 17, 17, 17 },
            ['I'] = new byte[] { 31, 4, 4, 4, 4, 4, 31 },
            ['J'] = new byte[] { 7, 2, 2, 2, 18, 18, 12 },
            ['K'] = new byte[] { 17, 18, 20, 24, 20, 18, 17 },
            ['L'] = new byte[] { 16, 16, 16, 16, 16, 16, 31 },
            ['M'] = new byte[] { 17, 27, 21, 21, 17, 17, 17 },
            ['N'] = new byte[] { 17, 25, 21, 19, 17, 17, 17 },
            ['O'] = new byte[] { 14, 17, 17, 17, 17, 17, 14 },
            ['P'] = new byte[] { 30, 17, 17, 30, 16, 16, 16 },
            ['Q'] = new byte[] { 14, 17, 17, 17, 21, 18, 13 },
            ['R'] = new byte[] { 30, 17, 17, 30, 20, 18, 17 },
            ['S'] = new byte[] { 15, 16, 16, 14, 1, 1, 30 },
            ['T'] = new byte[] { 31, 4, 4, 4, 4, 4, 4 },
            ['U'] = new byte[] { 17, 17, 17, 17, 17, 17, 14 },
            ['V'] = new byte[] { 17, 17, 17, 17, 17, 10, 4 },
            ['W'] = new byte[] { 17, 17, 17, 21, 21, 21, 10 },
            ['X'] = new byte[] { 17, 17, 10, 4, 10, 17, 17 },
            ['Y'] = new byte[] { 17, 17, 10, 4, 4, 4, 4 },
            ['Z'] = new byte[] { 31, 1, 2, 4, 8, 16, 31 },
            ['0'] = new byte[] { 14, 17, 19, 21, 25, 17, 14 },
            ['1'] = new byte[] { 4, 12, 4, 4, 4, 4, 14 },
            ['2'] = new byte[] { 14, 17, 1, 2, 4, 8, 31 },
            ['3'] = new byte[] { 30, 1, 1, 14, 1, 1, 30 },
            ['4'] = new byte[] { 2, 6, 10, 18, 31, 2, 2 },
            ['5'] = new byte[] { 31, 16, 16, 30, 1, 1, 30 },
            ['6'] = new byte[] { 14, 16, 16, 30, 17, 17, 14 },
            ['7'] = new byte[] { 31, 1, 2, 4, 8, 8, 8 },
            ['8'] = new byte[] { 14, 17, 17, 14, 17, 17, 14 },
            ['9'] = new byte[] { 14, 17, 17, 15, 1, 1, 14 },
            [':'] = new byte[] { 0, 4, 4, 0, 4, 4, 0 },
            ['/'] = new byte[] { 1, 2, 2, 4, 8, 8, 16 },
            ['-'] = new byte[] { 0, 0, 0, 31, 0, 0, 0 },
            ['!'] = new byte[] { 4, 4, 4, 4, 4, 0, 4 },
            ['+'] = new byte[] { 0, 4, 4, 31, 4, 4, 0 },
            ['%'] = new byte[] { 17, 2, 4, 4, 8, 16, 17 },
            ['>'] = new byte[] { 16, 8, 4, 2, 4, 8, 16 },
            ['<'] = new byte[] { 1, 2, 4, 8, 4, 2, 1 },
            ['.'] = new byte[] { 0, 0, 0, 0, 0, 6, 6 },
            [','] = new byte[] { 0, 0, 0, 0, 6, 6, 4 },
            ['?'] = new byte[] { 14, 17, 1, 2, 4, 0, 4 },
            ['#'] = new byte[] { 10, 31, 10, 10, 31, 10, 0 },
            ['='] = new byte[] { 0, 31, 0, 31, 0, 0, 0 },
            ['('] = new byte[] { 2, 4, 8, 8, 8, 4, 2 },
            [')'] = new byte[] { 8, 4, 2, 2, 2, 4, 8 },
            ['{'] = new byte[] { 3, 4, 4, 8, 4, 4, 3 },
            ['}'] = new byte[] { 24, 4, 4, 2, 4, 4, 24 },
            ['\"'] = new byte[] { 10, 10, 10, 0, 0, 0, 0 },
            [';'] = new byte[] { 0, 6, 6, 0, 6, 6, 4 },
            ['['] = new byte[] { 14, 8, 8, 8, 8, 8, 14 },
            [']'] = new byte[] { 14, 2, 2, 2, 2, 2, 14 },
            ['\''] = new byte[] { 4, 4, 8, 0, 0, 0, 0 }
        };
    private static readonly Color[] WildForestSkyBands =
    {
        new Color(13, 21, 17),
        new Color(15, 25, 19),
        new Color(18, 29, 21),
        new Color(20, 31, 21),
        new Color(18, 27, 19)
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _pigPhoto;
    private readonly PlayerSpriteRenderer _playerSpriteRenderer;
    private readonly GoblinFamilySpriteRenderer _goblinFamilySpriteRenderer;
    private readonly CorruptedBeastSpriteRenderer _corruptedBeastSpriteRenderer;
    private readonly WildForestAmbushSpriteRenderer _wildForestAmbushSpriteRenderer;
    private readonly WildForestHeavySpriteRenderer _wildForestHeavySpriteRenderer;
    private readonly WildForestApexSpriteRenderer _wildForestApexSpriteRenderer;
    private readonly CatacombSkeletonSpriteRenderer
        _catacombSkeletonSpriteRenderer;
    private readonly CatacombRotSpriteRenderer _catacombRotSpriteRenderer;
    private readonly CatacombGuardBatSpriteRenderer
        _catacombGuardBatSpriteRenderer;
    private readonly CatacombEliteSpriteRenderer _catacombEliteSpriteRenderer;
    private readonly CatacombKnightSpriteRenderer _catacombKnightSpriteRenderer;
    private readonly FallenCityMilitiaSpriteRenderer
        _fallenCityMilitiaSpriteRenderer;
    private readonly FallenCityHoundPlagueSpriteRenderer
        _fallenCityHoundPlagueSpriteRenderer;
    private readonly FallenCityWatcherExecutionerSpriteRenderer
        _fallenCityWatcherExecutionerSpriteRenderer;
    private readonly FallenCityRoyalAshSpriteRenderer
        _fallenCityRoyalAshSpriteRenderer;
    private readonly FallenCityBishopKingSpriteRenderer
        _fallenCityBishopKingSpriteRenderer;
    private readonly DemonFrontierSpriteRenderer _demonFrontierSpriteRenderer;
    private readonly DemonPredatorSpriteRenderer _demonPredatorSpriteRenderer;
    private readonly DemonHeavySpriteRenderer _demonHeavySpriteRenderer;
    private readonly DemonRitualSpriteRenderer _demonRitualSpriteRenderer;
    private readonly DemonLegionSpriteRenderer _demonLegionSpriteRenderer;
    private readonly DemonFortressEnemySpriteRenderer _demonFortressEnemySpriteRenderer;
    private readonly DemonCityEnemySpriteRenderer _demonCityEnemySpriteRenderer;
    private readonly DemonCityIndustrialEnemySpriteRenderer
        _demonCityIndustrialEnemySpriteRenderer;
    private readonly DemonCityEliteEnemySpriteRenderer
        _demonCityEliteEnemySpriteRenderer;
    private readonly DemonCityBossEnemySpriteRenderer
        _demonCityBossEnemySpriteRenderer;
    private readonly MalacharEnemySpriteRenderer _malacharEnemySpriteRenderer;
    private readonly DemonKingSanctumEnemySpriteRenderer
        _demonKingSanctumEnemySpriteRenderer;
    private readonly DemonKingSanctumPenitentSpriteRenderer
        _demonKingSanctumPenitentSpriteRenderer;
    private readonly DemonKingSanctumReliquarySpriteRenderer
        _demonKingSanctumReliquarySpriteRenderer;
    private readonly DemonKingSanctumEliteSpriteRenderer
        _demonKingSanctumEliteSpriteRenderer;
    private readonly ThroneboundSeraphSpriteRenderer _throneboundSeraphSpriteRenderer;
    private readonly VaeltharSpriteRenderer _vaeltharSpriteRenderer;
    private readonly CatacombEnvironmentRenderer _catacombEnvironmentRenderer;
    private readonly AncientScholarTombRenderer _ancientScholarTombRenderer;
    private readonly FallenCityEnvironmentRenderer _fallenCityEnvironmentRenderer;
    private readonly DemonLandsEnvironmentRenderer _demonLandsEnvironmentRenderer;
    private readonly DemonFortressEnvironmentRenderer _demonFortressEnvironmentRenderer;
    private readonly DemonCityEnvironmentRenderer _demonCityEnvironmentRenderer;
    private readonly DemonKingSanctumEnvironmentRenderer
        _demonKingSanctumEnvironmentRenderer;
    private int _observedElderSigils;
    private float _elderSigilPulseRemaining;
    private int _observedDuelistMomentumTier = -1;
    private float _duelistMomentumTierPulseRemaining;
    private int _duelistUltimateVisualUseId = -1;
    private int _duelistUltimateObservedStage = -1;
    private Vector2 _duelistUltimateLastPlayerPosition;
    private Vector2 _duelistUltimateDeparturePosition;
    private float _duelistUltimateDepartureFade;
    private float _duelistUltimateDepartureDuration;
    private Vector2 _thunderSpiritVisualPosition;
    private bool _thunderSpiritVisualInitialized;

    public GameRenderer(
        GraphicsDevice graphicsDevice,
        Texture2D pigPhoto = null)
    {
        _graphicsDevice = graphicsDevice;
        _pigPhoto = pigPhoto;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _playerSpriteRenderer = new PlayerSpriteRenderer(_spriteBatch, _pixel);
        _goblinFamilySpriteRenderer = new GoblinFamilySpriteRenderer(
            _spriteBatch,
            _pixel);
        _corruptedBeastSpriteRenderer = new CorruptedBeastSpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestAmbushSpriteRenderer = new WildForestAmbushSpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestHeavySpriteRenderer = new WildForestHeavySpriteRenderer(
            _spriteBatch,
            _pixel);
        _wildForestApexSpriteRenderer = new WildForestApexSpriteRenderer(
            _spriteBatch,
            _pixel);
        _catacombSkeletonSpriteRenderer = new CatacombSkeletonSpriteRenderer(
            _spriteBatch,
            _pixel);
        _catacombRotSpriteRenderer = new CatacombRotSpriteRenderer(
            _spriteBatch,
            _pixel);
        _catacombGuardBatSpriteRenderer = new CatacombGuardBatSpriteRenderer(
            _spriteBatch,
            _pixel);
        _catacombEliteSpriteRenderer = new CatacombEliteSpriteRenderer(
            _spriteBatch,
            _pixel);
        _catacombKnightSpriteRenderer = new CatacombKnightSpriteRenderer(
            _spriteBatch,
            _pixel);
        _fallenCityMilitiaSpriteRenderer =
            new FallenCityMilitiaSpriteRenderer(_spriteBatch, _pixel);
        _fallenCityHoundPlagueSpriteRenderer =
            new FallenCityHoundPlagueSpriteRenderer(_spriteBatch, _pixel);
        _fallenCityWatcherExecutionerSpriteRenderer =
            new FallenCityWatcherExecutionerSpriteRenderer(
                _spriteBatch,
                _pixel);
        _fallenCityRoyalAshSpriteRenderer =
            new FallenCityRoyalAshSpriteRenderer(_spriteBatch, _pixel);
        _fallenCityBishopKingSpriteRenderer =
            new FallenCityBishopKingSpriteRenderer(_spriteBatch, _pixel);
        _demonFrontierSpriteRenderer = new DemonFrontierSpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonPredatorSpriteRenderer = new DemonPredatorSpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonHeavySpriteRenderer = new DemonHeavySpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonRitualSpriteRenderer = new DemonRitualSpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonLegionSpriteRenderer = new DemonLegionSpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonFortressEnemySpriteRenderer = new DemonFortressEnemySpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonCityEnemySpriteRenderer = new DemonCityEnemySpriteRenderer(
            _spriteBatch,
            _pixel);
        _demonCityIndustrialEnemySpriteRenderer =
            new DemonCityIndustrialEnemySpriteRenderer(_spriteBatch, _pixel);
        _demonCityEliteEnemySpriteRenderer =
            new DemonCityEliteEnemySpriteRenderer(_spriteBatch, _pixel);
        _demonCityBossEnemySpriteRenderer =
            new DemonCityBossEnemySpriteRenderer(_spriteBatch, _pixel);
        _malacharEnemySpriteRenderer =
            new MalacharEnemySpriteRenderer(_spriteBatch, _pixel);
        _demonKingSanctumEnemySpriteRenderer =
            new DemonKingSanctumEnemySpriteRenderer(_spriteBatch, _pixel);
        _demonKingSanctumPenitentSpriteRenderer =
            new DemonKingSanctumPenitentSpriteRenderer(_spriteBatch, _pixel);
        _demonKingSanctumReliquarySpriteRenderer =
            new DemonKingSanctumReliquarySpriteRenderer(_spriteBatch, _pixel);
        _demonKingSanctumEliteSpriteRenderer =
            new DemonKingSanctumEliteSpriteRenderer(_spriteBatch, _pixel);
        _throneboundSeraphSpriteRenderer = new ThroneboundSeraphSpriteRenderer(_spriteBatch, _pixel);
        _vaeltharSpriteRenderer = new VaeltharSpriteRenderer(_spriteBatch, _pixel);
        _catacombEnvironmentRenderer = new CatacombEnvironmentRenderer(
            _spriteBatch,
            _pixel);
        _ancientScholarTombRenderer = new AncientScholarTombRenderer(
            _spriteBatch,
            _pixel);
        _fallenCityEnvironmentRenderer = new FallenCityEnvironmentRenderer(
            _spriteBatch,
            _pixel);
        _demonLandsEnvironmentRenderer = new DemonLandsEnvironmentRenderer(
            _spriteBatch,
            _pixel);
        _demonFortressEnvironmentRenderer = new DemonFortressEnvironmentRenderer(
            _spriteBatch,
            _pixel);
        _demonCityEnvironmentRenderer = new DemonCityEnvironmentRenderer(
            _spriteBatch,
            _pixel);
        _demonKingSanctumEnvironmentRenderer =
            new DemonKingSanctumEnvironmentRenderer(_spriteBatch, _pixel);
    }

    private void UpdateDuelistUltimateVisualMemory(
        GameSession gameSession,
        GameTime gameTime)
    {
        PlayerCharacter player = gameSession.Player;
        bool active = player?.Combat.IsDuelistUltimateActive == true &&
            player.Combat.State != CombatState.ChargingHeavy;
        if (!active)
        {
            _duelistUltimateVisualUseId = -1;
            _duelistUltimateObservedStage = -1;
            _duelistUltimateDepartureFade = 0f;
            _duelistUltimateDepartureDuration = 0f;
            return;
        }

        int useId = player.Combat.CurrentTechniqueUseId;
        if (_duelistUltimateVisualUseId != useId)
        {
            _duelistUltimateVisualUseId = useId;
            _duelistUltimateObservedStage = player.Combat.TechniqueStageIndex;
            _duelistUltimateLastPlayerPosition = player.Position;
            _duelistUltimateDeparturePosition = player.Position;
            _duelistUltimateDepartureFade = 0f;
        }

        int stage = player.Combat.TechniqueStageIndex;
        if (stage != _duelistUltimateObservedStage)
        {
            _duelistUltimateDeparturePosition =
                _duelistUltimateLastPlayerPosition;
            int group = Math.Clamp(stage / 8, 0, 3);
            _duelistUltimateDepartureDuration = stage >=
                    DuelistTuning.UltimateStrikeCount
                ? .16f
                : group switch
                {
                    0 => .13f,
                    1 => .105f,
                    2 => .082f,
                    _ => .062f
                };
            _duelistUltimateDepartureFade =
                _duelistUltimateDepartureDuration;
            _duelistUltimateObservedStage = stage;
        }
        _duelistUltimateLastPlayerPosition = player.Position;
        _duelistUltimateDepartureFade = MathF.Max(
            0f,
            _duelistUltimateDepartureFade - MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                .05f));
    }

    public void Draw(
        GameSession gameSession,
        GameTime gameTime,
        Action<string> diagnosticLog = null)
    {
        RegionTheme theme = RegionTheme.For(gameSession.CurrentRegion);
        _graphicsDevice.Clear(theme.Background);
        UpdateDuelistUltimateVisualMemory(gameSession, gameTime);
        diagnosticLog?.Invoke("[DRAW] GraphicsDevice.Clear completed");

        if (gameSession.State == GameState.Start)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            diagnosticLog?.Invoke("[DRAW] SpriteBatch.Begin completed");
            DrawArena();
            DrawStartScreen(gameSession);
            diagnosticLog?.Invoke("[DRAW] Frontend render completed");
            _spriteBatch.End();
            diagnosticLog?.Invoke("[DRAW] SpriteBatch.End completed");
            return;
        }

        if (gameSession.State == GameState.CharacterCreation)
        {
            DrawCharacterCreation(gameSession, gameTime);
            return;
        }

        if (gameSession.State == GameState.CharacterSelect)
        {
            DrawCharacterSelect(gameSession, gameTime);
            return;
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: gameSession.Camera.Transform);
        DrawWildForestDepth(gameSession, gameSession.Camera.ViewBounds);
        DrawCatacombDepth(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);
        DrawDungeon(gameSession, gameSession.Camera.ViewBounds, gameTime);
        _ancientScholarTombRenderer.Draw(
            gameSession.ScholarTomb,
            gameSession.Camera.ViewBounds,
            gameTime);
        DrawLastDiceTableWorld(gameSession, gameTime);

        foreach (WebPatch patch in gameSession.Projectiles.WebPatches)
        {
            if (patch.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawWebPatch(patch);
        }

        foreach (PlagueCloud cloud in gameSession.Projectiles.PlagueClouds)
        {
            if (cloud.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawPlagueCloud(cloud, gameTime);
        }

        foreach (RootHazard hazard in gameSession.RootHazards.Hazards)
        {
            if (hazard.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawRootHazard(hazard);
        }

        if (gameSession.Chest.Bounds.Intersects(gameSession.Camera.ViewBounds))
            DrawTreasureChest(gameSession.Chest);

        foreach (WorldLoot loot in gameSession.Loot.Drops)
        {
            if (loot.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawWorldLoot(loot);
        }

        if (gameSession.LoreNotePickup is { IsCollected: false } lorePickup &&
            lorePickup.Bounds.Intersects(gameSession.Camera.ViewBounds))
        {
            DrawLoreNotePickup(lorePickup, gameTime);
        }

        if (gameSession.ShowCombatDebug &&
            gameSession.Player.Combat.IsAttackActive &&
            gameSession.Player.Combat.CurrentAttack?.Delivery == AttackDelivery.Melee)
            DrawAttackArea(gameSession.PlayerAttackArea);

        DrawRangerTrajectoryGuide(gameSession);
        DrawSpellbladeWorld(gameSession);
        DrawAncientStaffWorld(gameSession, gameTime);
        DrawSovereignUltimateWorld(gameSession, gameTime);
        DrawSovereignPortals(gameSession);
        DrawFourthVeilBackground(gameSession, gameTime);

        Vector2? sovereignVisualPosition = null;
        Vector2? sovereignTravelDirection = null;
        float sovereignTransitionStrength = 0f;
        if (gameSession.SovereignPortals.TryGetTransitVisual(
            out Vector2 transitPosition,
            out Vector2 transitDirection,
            out float transitStrength))
        {
            sovereignVisualPosition = transitPosition;
            sovereignTravelDirection = transitDirection;
            sovereignTransitionStrength = transitStrength;
        }
        if (gameSession.Player.Combat.IsFourthVeilVisualActive)
        {
            float veilStrength = gameSession.Player.Combat
                .FourthVeilVisualStrength;
            float drift = MathF.Sin(
                (float)gameTime.TotalGameTime.TotalSeconds * 4.2f);
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f : 1f;
            _playerSpriteRenderer.DrawFourthVeilEcho(
                gameSession.Player,
                gameSession.Player.Position + new Vector2(
                    -facing * (5f + drift * 2f),
                    1f),
                veilStrength * .68f,
                purpleLayer: true);
            _playerSpriteRenderer.DrawFourthVeilEcho(
                gameSession.Player,
                gameSession.Player.Position + new Vector2(
                    facing * (3f - drift),
                    -1f),
                veilStrength * .48f,
                purpleLayer: false);
        }
        Vector2? playerVisualPosition = sovereignVisualPosition;
        if (gameSession.IsScytheUltimateActive &&
            !gameSession.Player.Combat.IsUmbralAttached &&
            gameSession.Player.Combat.State != CombatState.ChargingHeavy)
        {
            int stage = gameSession.ScytheUltimateStage;
            float ratio = gameSession.ScytheUltimateStageRatio;
            float arrival = stage switch
            {
                0 => Math.Clamp(ratio / .36f, 0f, 1f),
                4 or 5 => Math.Clamp(ratio / .42f, 0f, 1f),
                6 => Math.Clamp(ratio / .26f, 0f, 1f),
                7 => Math.Clamp(ratio / .72f, 0f, 1f),
                _ => 1f
            };
            arrival = MathHelper.SmoothStep(0f, 1f, arrival);
            playerVisualPosition = Vector2.Lerp(
                gameSession.ScytheUltimateVisualStartPosition,
                gameSession.Player.Position,
                arrival);
        }
        if (gameSession.IsDuelistUltimateActive &&
            gameSession.Player.Combat.State != CombatState.ChargingHeavy)
        {
            int duelistStage = gameSession.DuelistUltimateStage;
            float duelistRatio = gameSession.DuelistUltimateStageRatio;
            float verticalOffset = 0f;
            if (duelistStage >= DuelistTuning.UltimateStrikeCount)
            {
                if (duelistRatio < .08f)
                    verticalOffset = 0f;
                else if (duelistRatio < .22f)
                    verticalOffset = MathHelper.Lerp(0f, -158f,
                        MathHelper.SmoothStep(0f, 1f,
                            (duelistRatio - .08f) / .14f));
                else if (duelistRatio < .38f)
                    verticalOffset = -158f;
                else if (duelistRatio < .53f)
                    verticalOffset = MathHelper.Lerp(-158f, 0f,
                        MathHelper.SmoothStep(0f, 1f,
                            (duelistRatio - .38f) / .15f));
                else if (duelistRatio < .76f)
                    verticalOffset = -9f * MathF.Sin(
                        (duelistRatio - .53f) / .23f * MathHelper.Pi);
            }
            playerVisualPosition = gameSession.Player.Position +
                new Vector2(0f, verticalOffset);
        }
        DrawScytheUltimateShadowField(gameSession, gameTime);
        DrawDuelistUltimateBlinkDeparture(gameSession);
        if (!(gameSession.IsScytheUltimateActive &&
                gameSession.Player.Combat.IsUmbralAttached))
        {
            _playerSpriteRenderer.Draw(
                gameSession.Player,
                gameTime,
                null,
                playerVisualPosition,
                sovereignTravelDirection,
                sovereignTransitionStrength);
        }
        DrawFourthVeilOverlay(gameSession, gameTime);

        foreach (Enemy enemy in gameSession.Enemies.DefeatPresentations)
        {
            if (enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawEnemy(enemy, gameTime);
                DrawArcaneImprints(
                    GetEnemyVisualBounds(enemy),
                    enemy.ArcaneImprintCount,
                    bound: enemy.IsArcaneStunned ||
                        IsDominionBindingVisible(
                            gameSession,
                            enemy.Position));
        }

        foreach (Enemy enemy in gameSession.Enemies.Enemies)
        {
            if (enemy.IsAlive &&
                enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
            {
                DrawEnemy(enemy, gameTime);

                if (gameSession.IsWildForestShowcaseMode &&
                    enemy.Type != EnemyType.Spiderling)
                {
                    DrawWildForestShowcaseLabel(
                        GameSession.GetWildForestShowcaseLabel(enemy.Type),
                        GetEnemyVisualBounds(enemy));
                }

                DrawEnemyHealthBar(
                    enemy,
                    gameSession.Camera.ViewBounds,
                    gameSession.IsWildForestShowcaseMode);
                DrawReapMarks(enemy.MeleeTargetBounds,
                    gameSession.PlayerHitEffectPosition,
                    enemy.ReapMarkCount,
                    enemy.ReapMarkLifetimeRatio,
                    enemy.ReapMarkGainVisualRatio,
                    enemy.ReapMarkRefreshVisualRatio,
                    enemy.ReapMarkExitVisualRatio,
                    enemy.ReapMarkExitVisualCount,
                    enemy.ReapMarkExitWasConsumed);
            }
        }

        DrawRangerUltimateTargetLock(gameSession, gameTime);
        DrawGreatswordUltimateTargetLock(gameSession, gameTime);
        DrawSovereignUltimateTargetLock(gameSession, gameTime);
        DrawScytheUltimateTargetLock(gameSession, gameTime);
        DrawDuelistUltimateTargetLock(gameSession, gameTime);

        foreach (Projectile projectile in gameSession.Projectiles.Projectiles)
        {
            if (projectile.Bounds.Intersects(gameSession.Camera.ViewBounds))
                DrawProjectile(projectile);
        }

        foreach (ProjectileImpact impact in gameSession.Projectiles.Impacts)
        {
            if (!impact.Projectile.IsPlayerOwned &&
                impact.Projectile.Type is ProjectileType.Arrow or
                    ProjectileType.CrossbowBolt)
            {
                DrawEnemyArrowImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.PlagueFlask)
            {
                DrawPlagueFlaskImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.SoulBolt)
            {
                DrawSoulBoltImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.SigilLance)
            {
                DrawSigilLanceImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.JudgmentBolt)
            {
                DrawJudgmentBoltImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.DominionArc)
            {
                DrawDominionArcImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.Vowshard)
            {
                DrawVowshardImpact(impact);
            }
            else if (impact.Projectile.Type == ProjectileType.AbyssScript)
            {
                DrawAbyssScriptImpact(impact);
            }
            else
            {
                DrawRangerImpact(impact);
            }
        }

        if (gameSession.ShouldRenderBoss &&
            (gameSession.Boss.IsAlive ||
             gameSession.Boss.DeathPresentationProgress < 1f) &&
            gameSession.Boss.Bounds.Intersects(gameSession.Camera.ViewBounds))
        {
            DrawAncientTreant(gameSession.Boss, gameTime);
            DrawReapMarks(gameSession.Boss.Bounds,
                gameSession.PlayerHitEffectPosition,
                gameSession.Boss.ReapMarkCount,
                gameSession.Boss.ReapMarkLifetimeRatio,
                gameSession.Boss.ReapMarkGainVisualRatio,
                gameSession.Boss.ReapMarkRefreshVisualRatio,
                gameSession.Boss.ReapMarkExitVisualRatio,
                gameSession.Boss.ReapMarkExitVisualCount,
                gameSession.Boss.ReapMarkExitWasConsumed);
            DrawArcaneImprints(
                gameSession.Boss.Bounds,
                gameSession.Boss.ArcaneImprintCount,
                bound: IsDominionBindingVisible(
                    gameSession,
                    gameSession.Boss.Position));

            if (gameSession.IsWildForestShowcaseMode)
            {
                DrawWildForestShowcaseLabel(
                    "ANCIENT TREANT",
                    new Rectangle(
                        (int)MathF.Round(gameSession.Boss.Position.X -
                            WildForestApexSpriteRenderer.AncientTreantVisualSize.X / 2f),
                        gameSession.Boss.Bounds.Bottom -
                            (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.Y,
                        (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.X,
                        (int)WildForestApexSpriteRenderer.AncientTreantVisualSize.Y));
            }
        }

        _playerSpriteRenderer.DrawHitImpact(
            gameSession.Player,
            gameTime,
            gameSession.PlayerHitEffectId,
            gameSession.PlayerHitEffectPosition,
            gameSession.PlayerHitTechniqueEffect,
            gameSession.PlayerHitTechniqueStage);

        DrawWildForestForeground(gameSession, gameSession.Camera.ViewBounds);
        _catacombEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);
        _fallenCityEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);
        _demonCityEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);
        _demonKingSanctumEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);

        if (gameSession.ShowMapDebug)
            DrawWildForestMapDebug(gameSession);

        if (gameSession.ShowCombatDebug)
            DrawCombatDebug(gameSession);

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (gameSession.State == GameState.Ending)
        {
            DrawSanctumEnding(gameSession);
            _spriteBatch.End();
            return;
        }
        _demonLandsEnvironmentRenderer.DrawCorruptionOverlay(
            gameSession,
            new Rectangle(0, 0,
                _graphicsDevice.Viewport.Width,
                _graphicsDevice.Viewport.Height),
            gameTime);
        DrawRangerUltimateCinematic(gameSession);
        DrawGreatswordUltimateCinematic(gameSession);
        DrawKnightUltimateCinematic(gameSession);
        DrawSovereignAssaultOverlay(gameSession);
        DrawSovereignUltimateCinematic(gameSession);
        DrawAncientStaffUltimateCinematic(gameSession, gameTime);
        DrawScytheUltimateCinematic(gameSession);
        DrawDuelistUltimateCinematic(gameSession);
        DrawSovereignExecutionFrame(gameSession);
        bool hideSovereignHud =
            gameSession.Player.Combat.SovereignUltimateCinematicRatio > 0f ||
            gameSession.Player.Combat.KnightUltimateCinematicRatio > 0f ||
            gameSession.IsSovereignExecutionActive ||
            gameSession.Player.Combat.AncientStaffUltimateCinematicRatio > 0f ||
            gameSession.IsScytheUltimateActive ||
            gameSession.IsDuelistUltimateActive;
        if (!hideSovereignHud)
        {
            Enemy sanctumBoss = gameSession.CurrentDungeon.IsDemonKingSanctum ?
                SelectSanctumBossBarTarget(gameSession.Enemies.Enemies, gameSession.Player.Position) : null;
            DrawVaeltharArenaDim(sanctumBoss as VaeltharEnemy);
            DrawHudPanel();
            DrawPlayerHealth(gameSession.Player);
            DrawPlayerStamina(gameSession.Player);
            DrawCurseMeter(gameSession);
            DrawCityAlarm(gameSession);
            DrawDemonicCorruptionMeter(gameSession);
            DrawBrokenKingBossBar(gameSession);
            DrawHornedGeneralBossBar(gameSession);
            DrawDemonFortressBossBar(gameSession);
            DrawDemonCityBossBar(gameSession);
            DrawSeraphSubBossBar(sanctumBoss as ThroneboundSeraphEnemy);
            DrawVaeltharBossBar(sanctumBoss as VaeltharEnemy);
            DrawPlayerProgression(gameSession.Player, gameSession.KillCount);
            DrawWeaponResource(gameSession.Player, gameTime);
            DrawTechniqueFeedback(gameSession.Player);
            DrawDungeonStatus(gameSession);
        }

        if (gameSession.ScholarTomb.CanInspect(gameSession.Player.Position))
        {
            DrawDebugLabel(
                Loc.Get("hud.interact_coffin"),
                438,
                650,
                new Color(169, 213, 206));
        }

        if (gameSession.LastDiceTable.CanInteract(gameSession.Player.Position))
        {
            string dicePrompt = Loc.Get("dice.interact");
            DrawCenteredDebugText(
                dicePrompt,
                _graphicsDevice.Viewport.Width / 2,
                _graphicsDevice.Viewport.Height - 70,
                new Color(229, 190, 105),
                FitDebugTextScale(
                    dicePrompt,
                    _graphicsDevice.Viewport.Width - 40,
                    2));
        }

        if (gameSession.LoreNotePickup?.CanInteract(
            gameSession.Player.Position) == true)
        {
            bool photograph = gameSession.LoreNotePickup.Definition.Id ==
                LoreCatalog.MysteriousPhotograph.Id;
            string recordPrompt = Loc.Get(photograph
                ? "hud.pickup_photograph"
                : "hud.read_record");
            int promptWidth = recordPrompt.Length * 12;
            DrawDebugLabel(
                recordPrompt,
                _graphicsDevice.Viewport.Width / 2 - promptWidth / 2,
                _graphicsDevice.Viewport.Height - 70,
                new Color(220, 196, 124));
        }

        if (gameSession.DebugGodMode)
            DrawDebugLabel(
                "GOD MODE  F9",
                270,
                88,
                new Color(231, 200, 103));

        if (gameSession.EnemyIntroductions.IsPresenting)
            DrawEnemyIntroduction(gameSession.EnemyIntroductions);

        if (gameSession.IsWildForestShowcaseMode)
            DrawSequentialEnemyTestStatus(gameSession);

        if (gameSession.ShowCombatDebug)
            DrawCombatStateDebug(gameSession.Player);

        if (gameSession.WorldTierTransitionProgress > 0f)
            DrawWorldTierTransition(gameSession);

        if (gameSession.IsPigPhotoPopupOpen)
            DrawPigPhotoPopup();
        else if (gameSession.LastDiceTable.IsModalActive)
            DrawLastDiceTable(gameSession);
        else if (gameSession.ScholarTomb.IsModalActive)
            DrawAncientScholarQuiz(gameSession);
        else if (gameSession.IsInventoryOpen)
            DrawInventoryOverlay(gameSession);
        else if (gameSession.State == GameState.Paused)
            DrawPauseOverlay(gameSession);
        else if (gameSession.State == GameState.GameOver)
            DrawGameOverOverlay();

        if (gameSession.DeveloperPanel.IsOpen)
            DrawDeveloperPanel(gameSession);

        if (gameSession.IsFusionFeedbackVisible)
            DrawFusionFeedback(gameSession);

        if (gameSession.IsLoreNoteFeedbackVisible)
            DrawLoreNoteAcquiredFeedback(gameSession);

        _spriteBatch.End();
    }

    private void DrawWildForestDepth(
        GameSession gameSession,
        Rectangle cameraBounds)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        if (!dungeon.IsAuthoredWildForest)
            return;

        _spriteBatch.Draw(
            _pixel,
            cameraBounds,
            new Color(9, 15, 12));
        int bandHeight = Math.Max(1, cameraBounds.Height / 5);
        for (int band = 0; band < WildForestSkyBands.Length; band++)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    cameraBounds.Left,
                    cameraBounds.Top + band * bandHeight,
                    cameraBounds.Width,
                    band == WildForestSkyBands.Length - 1
                        ? cameraBounds.Bottom -
                            (cameraBounds.Top + band * bandHeight)
                        : bandHeight),
                WildForestSkyBands[band]);
        }

        Rectangle visible = cameraBounds;
        visible.Inflate(300, 100);
        for (int sectionIndex = 0;
             sectionIndex < dungeon.WildForestSections.Count;
             sectionIndex++)
        {
            WildForestSection section = dungeon.WildForestSections[sectionIndex];
            if (!section.Bounds.Intersects(visible))
                continue;

            int farSpacing = section.Kind is
                WildForestSectionKind.CorruptedGrove or
                WildForestSectionKind.MothersNest or
                WildForestSectionKind.AncientSanctuary ? 128 : 180;
            int farHeight = section.Kind is
                WildForestSectionKind.CorruptedGrove or
                WildForestSectionKind.MothersNest or
                WildForestSectionKind.AncientSanctuary ? 420 : 310;
            float farShift = cameraBounds.X * .68f;
            for (int x = section.Bounds.Left - farSpacing;
                 x < section.Bounds.Right + farSpacing;
                 x += farSpacing)
            {
                int hash = unchecked(x * 73856093 ^ section.Bounds.Left);
                int width = 28 + Math.Abs(hash % 30);
                int height = farHeight + Math.Abs(hash / 31 % 100);
                int drawX = (int)MathF.Round(x + farShift);
                int bottom = section.GroundY - 54;
                Color trunk = new(14, 24, 19, 220);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(drawX - width / 2, bottom - height, width, height),
                    trunk);
                DrawRootLine(
                    new Vector2(drawX, bottom - height + 58),
                    new Vector2(drawX - 68 - Math.Abs(hash % 36), bottom - height + 12),
                    trunk,
                    13f);
                DrawRootLine(
                    new Vector2(drawX, bottom - height + 92),
                    new Vector2(drawX + 74 + Math.Abs(hash / 7 % 42), bottom - height + 38),
                    trunk,
                    11f);
            }

            float midShift = cameraBounds.X * .32f;
            int midSpacing = section.Kind is WildForestSectionKind.Webwood or
                WildForestSectionKind.MothersNest
                ? 126
                : section.Kind is WildForestSectionKind.CorruptedGrove or
                    WildForestSectionKind.AncientSanctuary ? 155 : 225;
            for (int x = section.Bounds.Left; x < section.Bounds.Right;
                 x += midSpacing)
            {
                int hash = unchecked(x * 19349663 ^ section.Bounds.Right);
                int drawX = (int)MathF.Round(x + midShift);
                int width = 20 + Math.Abs(hash % 24);
                int top = section.Bounds.Top + 70 + Math.Abs(hash / 17 % 55);
                int bottom = section.GroundY - 24;
                Color trunk = section.CorruptionLevel > .55f
                    ? new Color(29, 31, 24, 225)
                    : new Color(27, 35, 27, 220);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(drawX - width / 2, top, width, bottom - top),
                    trunk);
                if ((hash & 3) == 0)
                {
                    DrawRootLine(
                        new Vector2(drawX, top + 100),
                        new Vector2(drawX - 58, top + 54),
                        trunk,
                        8f);
                }
            }

            if (section.FogDensity > .05f)
            {
                float fogOpacity =
                    (18f + section.FogDensity * 46f) / 255f;
                Color fog = new Color(112, 124, 105) * fogOpacity;
                int fogY = section.GroundY - 175;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        section.Bounds.Left,
                        fogY,
                        section.Bounds.Width,
                        118),
                    fog);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        section.Bounds.Left + section.Bounds.Width / 5,
                        fogY - 54,
                        section.Bounds.Width * 3 / 5,
                        72),
                    new Color(104, 116, 99) * (fogOpacity * .5f));
            }
        }
    }

    private void DrawAuthoredWildForestGameplay(
        GameSession gameSession,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        for (int sectionIndex = 0;
             sectionIndex < dungeon.WildForestSections.Count;
             sectionIndex++)
        {
            WildForestSection section = dungeon.WildForestSections[sectionIndex];
            if (!section.Bounds.Intersects(visibleBounds))
                continue;

            if (gameSession.ShowMapDebug)
            {
                Color plane = Color.Lerp(
                    new Color(23, 31, 24),
                    new Color(31, 25, 23),
                    section.CorruptionLevel * .55f);
                _spriteBatch.Draw(
                    _pixel,
                    section.Bounds,
                    new Color(plane.R, plane.G, plane.B) * (52f / 255f));
            }
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    section.Bounds.Left,
                    section.Bounds.Top,
                    section.Bounds.Width,
                    18),
                new Color(10, 16, 13));
        }

        for (int corridorIndex = 0;
             corridorIndex < dungeon.Corridors.Count;
             corridorIndex++)
        {
            Rectangle corridor = dungeon.Corridors[corridorIndex];
            if (gameSession.ShowMapDebug && corridor.Intersects(visibleBounds))
            {
                _spriteBatch.Draw(
                    _pixel,
                    corridor,
                    new Color(26, 31, 23) * (48f / 255f));
            }
        }

        for (int platformIndex = 0;
             platformIndex < dungeon.Platforms.Count;
             platformIndex++)
        {
            Platform platform = dungeon.Platforms[platformIndex];
            if (!platform.Bounds.Intersects(visibleBounds))
                continue;
            WildForestSection section = FindSectionAtX(
                dungeon,
                platform.Bounds.Center.X);
            DrawWildForestPlatform(platform, section, visibleBounds, theme);
        }

        for (int sectionIndex = 0;
             sectionIndex < dungeon.WildForestSections.Count;
             sectionIndex++)
        {
            WildForestSection section = dungeon.WildForestSections[sectionIndex];
            if (section.Bounds.Intersects(visibleBounds))
                DrawWildForestSectionStory(section, visibleBounds);
        }

        for (int landmarkIndex = 0;
             landmarkIndex < dungeon.Landmarks.Count;
             landmarkIndex++)
        {
            WildForestLandmark landmark = dungeon.Landmarks[landmarkIndex];
            if (visibleBounds.Contains(landmark.Position.ToPoint()) ||
                Math.Abs(landmark.Position.X - visibleBounds.Center.X) < 520f)
            {
                DrawWildForestLandmark(landmark);
            }
        }

        if (gameSession.Chest.Bounds.Intersects(visibleBounds))
            DrawTreasureRoomMarker(gameSession.Chest.Position);
        if (gameSession.BossDefeated)
            DrawExitMarker(gameSession.ExitPosition, isUnlocked: true);
    }

    private void DrawWildForestPlatform(
        Platform platform,
        WildForestSection section,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        bool ruins = section?.Kind is WildForestSectionKind.WarCamp or
            WildForestSectionKind.AncientSanctuary;
        bool webbed = section?.Kind is WildForestSectionKind.Webwood or
            WildForestSectionKind.MothersNest;
        Color body = platform.Kind switch
        {
            PlatformKind.Raised when ruins => new Color(62, 64, 56),
            PlatformKind.Raised when webbed => new Color(61, 55, 46),
            PlatformKind.Raised => new Color(60, 48, 37),
            PlatformKind.Obstacle when ruins => new Color(68, 69, 61),
            PlatformKind.Obstacle => new Color(65, 49, 35),
            PlatformKind.Transition => new Color(47, 43, 31),
            _ => section?.Kind == WildForestSectionKind.Thornlands
                ? new Color(42, 33, 29)
                : new Color(43, 40, 30)
        };
        _spriteBatch.Draw(_pixel, platform.Bounds, body);

        Color surface = ruins
            ? new Color(94, 97, 79)
            : section?.Kind is WildForestSectionKind.CorruptedGrove or
                WildForestSectionKind.AncientSanctuary
                ? new Color(69, 76, 47)
                : new Color(76, 78, 48);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                platform.Bounds.Left,
                platform.Bounds.Top,
                platform.Bounds.Width,
                Math.Min(6, platform.Bounds.Height)),
            surface);

        if (platform.Kind is PlatformKind.Ground or PlatformKind.Transition)
        {
            int start = Math.Max(platform.Bounds.Left + 18, visibleBounds.Left - 24);
            int end = Math.Min(platform.Bounds.Right - 12, visibleBounds.Right + 24);
            for (int x = start; x < end; x += 54)
            {
                int hash = unchecked(x * 83492791 ^ platform.Bounds.Top);
                int height = 5 + Math.Abs(hash % 11);
                Color detail = (hash & 3) == 0 &&
                    (section?.CorruptionLevel ?? 0f) > .35f
                    ? new Color(77, 40, 39, 135)
                    : new Color(43, 56, 34, 180);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, platform.Bounds.Top - height, 3, height),
                    detail);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x - 3, platform.Bounds.Top - 3, 9, 3),
                    detail);
            }
        }
        else
        {
            DrawRectangleOutline(platform.Bounds, 2, new Color(34, 31, 27));
        }
    }

    private static WildForestSection FindSectionAtX(
        DungeonMap dungeon,
        int x)
    {
        WildForestSection nearest = null;
        int bestDistance = int.MaxValue;
        for (int index = 0; index < dungeon.WildForestSections.Count; index++)
        {
            WildForestSection section = dungeon.WildForestSections[index];
            if (x >= section.Bounds.Left && x < section.Bounds.Right)
                return section;
            int distance = Math.Abs(section.Bounds.Center.X - x);
            if (distance < bestDistance)
            {
                nearest = section;
                bestDistance = distance;
            }
        }
        return nearest;
    }

    private void DrawWildForestSectionStory(
        WildForestSection section,
        Rectangle visibleBounds)
    {
        switch (section.Kind)
        {
            case WildForestSectionKind.ForestOutskirts:
                DrawOutskirtsStory(section);
                break;
            case WildForestSectionKind.GoblinEncampment:
                DrawGoblinCampStory(section);
                break;
            case WildForestSectionKind.Webwood:
                DrawSpiderGroveStory(section);
                break;
            case WildForestSectionKind.Thornlands:
                DrawThornGroundStory(section);
                break;
            case WildForestSectionKind.CorruptedGrove:
                DrawDeepForestStory(section);
                break;
            case WildForestSectionKind.WarCamp:
                DrawWarCampStory(section);
                break;
            case WildForestSectionKind.MothersNest:
                DrawMothersNestStory(section);
                break;
            case WildForestSectionKind.AncientSanctuary:
                DrawBossArenaStory(section);
                break;
        }
    }

    private void DrawOutskirtsStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color wood = new(78, 60, 42);
        for (int index = 0; index < 4; index++)
        {
            int x = left + 145 + index * 72;
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 42, 7, 42), wood);
            if (index < 3)
                DrawRootLine(new Vector2(x, ground - 28), new Vector2(x + 72, ground - 20), wood, 5f);
        }
        Vector2 wheel = new(left + 620, ground - 20);
        DrawRectangleOutline(new Rectangle((int)wheel.X - 22, (int)wheel.Y - 22, 44, 44), 5, new Color(69, 53, 39));
        DrawRootLine(wheel - new Vector2(20, 0), wheel + new Vector2(20, 0), wood, 3f);
        DrawRootLine(wheel - new Vector2(0, 20), wheel + new Vector2(0, 20), wood, 3f);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 735, ground - 18, 52, 18), new Color(74, 60, 43));
        for (int index = 0; index < 4; index++)
        {
            int x = left + 430 + index * 32;
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 4 - index % 2 * 3, 10, 4), new Color(47, 39, 31));
        }
        DrawRootLine(new Vector2(left + 820, ground), new Vector2(left + 850, ground - 25), new Color(55, 76, 44), 7f);
        DrawRootLine(new Vector2(left + 850, ground - 25), new Vector2(left + 875, ground), new Color(55, 76, 44), 7f);
    }

    private void DrawBrokenTrailStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        DrawRootLine(
            new Vector2(left + 565, ground - 18),
            new Vector2(left + 785, ground - 62),
            new Color(72, 53, 37),
            31f);
        DrawRootLine(
            new Vector2(left + 610, ground - 30),
            new Vector2(left + 570, ground - 108),
            new Color(62, 47, 34),
            17f);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 380, ground - 78, 34, 78), new Color(75, 77, 67));
        _spriteBatch.Draw(_pixel, new Rectangle(left + 373, ground - 79, 48, 9), new Color(91, 91, 76));
        for (int x = left + 90; x < section.Bounds.Right - 40; x += 120)
            DrawRootLine(new Vector2(x, ground), new Vector2(x + 30, ground - 20), new Color(54, 45, 33), 6f);
    }

    private void DrawGoblinCampStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color crudeWood = new(78, 54, 36);
        for (int index = 0; index < 5; index++)
        {
            int x = left + 160 + index * 34;
            DrawRootLine(new Vector2(x, ground), new Vector2(x + (index % 2 == 0 ? -4 : 5), ground - 54), crudeWood, 7f);
            DrawRootLine(new Vector2(x - 7, ground - 48), new Vector2(x + 12, ground - 48), new Color(99, 69, 42), 3f);
        }
        _spriteBatch.Draw(_pixel, new Rectangle(left + 500, ground - 8, 78, 8), new Color(46, 39, 31));
        _spriteBatch.Draw(_pixel, new Rectangle(left + 522, ground - 15, 34, 7), new Color(89, 54, 34));
        for (int index = 0; index < 4; index++)
        {
            int x = left + 795 + index * 18;
            DrawRootLine(new Vector2(x, ground), new Vector2(x + 7, ground - 42), crudeWood, 5f);
        }
        _spriteBatch.Draw(_pixel, new Rectangle(left + 1010, ground - 34, 48, 34), new Color(76, 58, 39));
        DrawRectangleOutline(new Rectangle(left + 1010, ground - 34, 48, 34), 3, new Color(48, 39, 31));
        for (int index = 0; index < 3; index++)
            DrawRootLine(new Vector2(left + 620 + index * 16, ground - 4), new Vector2(left + 630 + index * 19, ground - 22), new Color(131, 120, 91), 2f);
        DrawRootLine(new Vector2(left + 390, ground), new Vector2(left + 450, ground - 78), new Color(87, 58, 38), 6f);
        DrawRootLine(new Vector2(left + 450, ground - 78), new Vector2(left + 515, ground), new Color(87, 58, 38), 6f);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 420, ground - 48, 64, 48), new Color(75, 48, 38));
        _spriteBatch.Draw(_pixel, new Rectangle(left + 524, ground - 28, 30, 12), new Color(164, 75, 39, 180));
    }

    private void DrawSpiderGroveStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color web = new(151, 158, 151, 105);
        for (int cluster = 0; cluster < 4; cluster++)
        {
            Vector2 top = new(left + 150 + cluster * 270, section.Bounds.Top + 115 + cluster % 2 * 34);
            Vector2 bottomLeft = new(top.X - 86, ground - 86);
            Vector2 bottomRight = new(top.X + 94, ground - 68);
            DrawRootLine(top, bottomLeft, web, 2f);
            DrawRootLine(top, bottomRight, web, 2f);
            DrawRootLine(bottomLeft, bottomRight, web, 2f);
            DrawRootLine(Vector2.Lerp(top, bottomLeft, .45f), Vector2.Lerp(top, bottomRight, .45f), web, 1f);
            DrawRootLine(Vector2.Lerp(top, bottomLeft, .72f), Vector2.Lerp(top, bottomRight, .72f), web, 1f);
        }
        for (int index = 0; index < 3; index++)
        {
            int x = left + 380 + index * 210;
            DrawRootLine(new Vector2(x, section.Bounds.Top + 70), new Vector2(x, section.Bounds.Top + 190), web, 2f);
            _spriteBatch.Draw(_pixel, new Rectangle(x - 10, section.Bounds.Top + 184, 20, 39), new Color(111, 117, 108, 150));
        }
        _spriteBatch.Draw(_pixel, new Rectangle(left + 52, ground - 90, 74, 90), new Color(34, 29, 25));
        DrawRectangleOutline(new Rectangle(left + 70, ground - 68, 38, 54), 4, new Color(64, 52, 39));
    }

    private void DrawThornGroundStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        for (int patch = 0; patch < 9; patch++)
        {
            int x = left + 85 + patch * 116;
            int hash = unchecked(x * 486187739);
            Color soil = (hash & 1) == 0
                ? new Color(54, 36, 31)
                : new Color(46, 34, 29);
            _spriteBatch.Draw(_pixel, new Rectangle(x - 30, ground - 5, 66, 7), soil);
            for (int thorn = 0; thorn < 3; thorn++)
            {
                DrawRootLine(
                    new Vector2(x + thorn * 12 - 12, ground),
                    new Vector2(x + thorn * 10 - 18, ground - 20 - thorn * 5),
                    new Color(62, 48, 34),
                    3f);
            }
            if (patch % 3 == 1)
                _spriteBatch.Draw(_pixel, new Rectangle(x + 8, ground - 13, 5, 5), new Color(91, 42, 43, 135));
        }
    }

    private void DrawRuinsStory(WildForestSection section)
    {
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color stone = new(72, 75, 67);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 120, ground - 92, 46, 92), stone);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 1010, ground - 68, 54, 68), stone);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 1080, ground - 38, 94, 38), new Color(62, 65, 58));
        for (int x = left + 80; x < section.Bounds.Right - 70; x += 150)
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 8, 108, 8), new Color(82, 82, 69));
        DrawRootLine(new Vector2(left + 90, ground - 25), new Vector2(left + 360, ground - 4), new Color(58, 68, 42), 7f);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 1240, ground - 55, 10, 55), new Color(78, 57, 44));
        _spriteBatch.Draw(_pixel, new Rectangle(left + 1234, ground - 58, 22, 7), new Color(91, 63, 44));
    }

    private void DrawDeepForestStory(WildForestSection section)
    {
        int ground = section.GroundY;
        for (int index = 0; index < 5; index++)
        {
            int x = section.Bounds.Left + 120 + index * 280;
            int width = index % 2 == 0 ? 70 : 52;
            _spriteBatch.Draw(_pixel, new Rectangle(x, section.Bounds.Top + 25, width, ground - section.Bounds.Top - 25), new Color(49, 42, 31));
            DrawRootLine(new Vector2(x + width / 2, ground - 10), new Vector2(x - 64, ground), new Color(60, 46, 32), 16f);
            DrawRootLine(new Vector2(x + width / 2, ground - 8), new Vector2(x + width + 70, ground), new Color(60, 46, 32), 15f);
            if (index % 2 == 0)
                _spriteBatch.Draw(_pixel, new Rectangle(x + width / 2 - 2, ground - 190, 4, 68), new Color(88, 39, 40, 90));
        }
        for (int index = 0; index < 6; index++)
        {
            int x = section.Bounds.Left + 205 + index * 168;
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 9, 12, 6), new Color(86, 142, 83, 145));
            _spriteBatch.Draw(_pixel, new Rectangle(x + 5, ground - 4, 3, 6), new Color(132, 125, 84));
        }
        _spriteBatch.Draw(_pixel, new Rectangle(section.Bounds.Left + 760, ground - 62, 38, 62), new Color(77, 79, 68));
        DrawRootLine(new Vector2(section.Bounds.Left + 765, ground - 42), new Vector2(section.Bounds.Left + 790, ground - 27), new Color(107, 116, 83), 3f);
    }

    private void DrawWarCampStory(WildForestSection section)
    {
        DrawGoblinCampStory(section);
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color wood = new(72, 48, 32);
        for (int index = 0; index < 7; index++)
        {
            int x = left + 70 + index * 32;
            DrawRootLine(new Vector2(x, ground), new Vector2(x + 5, ground - 72), wood, 8f);
        }
        _spriteBatch.Draw(_pixel, new Rectangle(left + 680, ground - 52, 72, 52), new Color(76, 55, 38));
        DrawRectangleOutline(new Rectangle(left + 680, ground - 52, 72, 52), 4, new Color(43, 34, 27));
        _spriteBatch.Draw(_pixel, new Rectangle(left + 885, ground - 42, 58, 42), new Color(91, 58, 38));
        DrawRectangleOutline(new Rectangle(left + 885, ground - 42, 58, 42), 5, new Color(48, 36, 27));
        DrawRootLine(new Vector2(left + 970, ground), new Vector2(left + 970, ground - 106), wood, 7f);
        _spriteBatch.Draw(_pixel, new Rectangle(left + 974, ground - 100, 54, 34), new Color(91, 38, 39));
    }

    private void DrawMothersNestStory(WildForestSection section)
    {
        DrawSpiderGroveStory(section);
        DrawBossApproachStory(section);
        int left = section.Bounds.Left;
        int ground = section.GroundY;
        Color egg = new(126, 132, 115, 185);
        for (int index = 0; index < 5; index++)
        {
            int x = left + 465 + index * 94;
            int y = index % 2 == 0 ? ground - 205 : ground - 166;
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, 24, 36), egg);
            DrawRectangleOutline(new Rectangle(x, y, 24, 36), 2, new Color(76, 72, 61));
        }
    }

    private void DrawBossApproachStory(WildForestSection section)
    {
        int ground = section.GroundY;
        for (int index = 0; index < 4; index++)
        {
            int x = section.Bounds.Left + 70 + index * 250;
            DrawRootLine(new Vector2(x, ground), new Vector2(x + 145, ground - 105 - index * 10), new Color(68, 49, 34), 20f);
        }
        _spriteBatch.Draw(_pixel, new Rectangle(section.Bounds.Left + 420, ground - 45, 38, 45), new Color(69, 70, 61));
        _spriteBatch.Draw(_pixel, new Rectangle(section.Bounds.Left + 750, ground - 28, 72, 28), new Color(62, 63, 55));
        DrawRootLine(new Vector2(section.Bounds.Left + 255, ground - 3), new Vector2(section.Bounds.Left + 274, ground - 15), new Color(126, 116, 91), 3f);
    }

    private void DrawBossArenaStory(WildForestSection section)
    {
        int ground = section.GroundY;
        _spriteBatch.Draw(_pixel, new Rectangle(section.Bounds.Left + 42, section.Bounds.Top + 30, 84, ground - section.Bounds.Top - 30), new Color(43, 37, 29));
        _spriteBatch.Draw(_pixel, new Rectangle(section.Bounds.Right - 126, section.Bounds.Top + 22, 84, ground - section.Bounds.Top - 22), new Color(43, 37, 29));
        for (int index = 0; index < 11; index++)
        {
            int x = section.Bounds.Left + 110 + index * 143;
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 6, 82, 6), new Color(72, 51, 39));
            if (index % 3 == 0)
                DrawRootLine(new Vector2(x + 20, ground), new Vector2(x + 55, ground - 24), new Color(65, 48, 33), 7f);
        }
        Color ancientStone = new(73, 78, 67);
        for (int index = 0; index < 4; index++)
        {
            int x = section.Bounds.Left + 250 + index * 350;
            _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 150, 34, 150), ancientStone);
            _spriteBatch.Draw(_pixel, new Rectangle(x - 10, ground - 158, 54, 12), new Color(91, 96, 79));
        }
        Rectangle rune = new(section.Bounds.Center.X - 155, ground - 15, 310, 13);
        DrawRectangleOutline(rune, 3, new Color(73, 116, 82, 155));
        DrawRectangleOutline(new Rectangle(rune.X + 48, rune.Y + 3, rune.Width - 96, 7), 2, new Color(95, 142, 94, 140));
    }

    private void DrawWildForestLandmark(WildForestLandmark landmark)
    {
        Vector2 p = landmark.Position;
        switch (landmark.Kind)
        {
            case WildForestLandmarkKind.FallenWatchTree:
                DrawRootLine(p + new Vector2(-110, -5), p + new Vector2(105, -82), new Color(71, 51, 35), 34f);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 96, (int)p.Y - 118, 38, 64), new Color(64, 57, 46));
                break;
            case WildForestLandmarkKind.SpiderGroveDeadTree:
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 25, (int)p.Y - 250, 50, 250), new Color(55, 47, 36));
                DrawRootLine(p + new Vector2(0, -205), p + new Vector2(-120, -285), new Color(55, 47, 36), 17f);
                DrawRootLine(p + new Vector2(0, -175), p + new Vector2(132, -252), new Color(55, 47, 36), 15f);
                break;
            case WildForestLandmarkKind.WarCampGate:
                Color stone = new(84, 85, 75);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 125, (int)p.Y - 178, 38, 178), stone);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X + 87, (int)p.Y - 178, 38, 178), stone);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 125, (int)p.Y - 190, 250, 34), stone);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 18, (int)p.Y - 190, 32, 18), new Color(37, 38, 34));
                break;
            case WildForestLandmarkKind.GiganticRootGate:
                DrawRootLine(p + new Vector2(-62, 0), p + new Vector2(-24, -245), new Color(67, 48, 33), 31f);
                DrawRootLine(p + new Vector2(115, 0), p + new Vector2(42, -245), new Color(67, 48, 33), 31f);
                DrawRootLine(p + new Vector2(-24, -245), p + new Vector2(42, -245), new Color(67, 48, 33), 28f);
                break;
            case WildForestLandmarkKind.MothersNestMaw:
                DrawRootLine(p + new Vector2(-105, 0), p + new Vector2(-35, -105), new Color(55, 45, 35), 30f);
                DrawRootLine(p + new Vector2(105, 0), p + new Vector2(35, -105), new Color(55, 45, 35), 30f);
                DrawRootLine(p + new Vector2(-35, -105), p + new Vector2(35, -105), new Color(55, 45, 35), 22f);
                break;
            case WildForestLandmarkKind.TreantArenaHeart:
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 74, (int)p.Y - 58, 148, 58), new Color(61, 45, 32));
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 54, (int)p.Y - 65, 108, 14), new Color(83, 65, 43));
                DrawRootLine(p + new Vector2(-50, -12), p + new Vector2(-145, 0), new Color(67, 48, 33), 15f);
                DrawRootLine(p + new Vector2(50, -12), p + new Vector2(145, 0), new Color(67, 48, 33), 15f);
                _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 4, (int)p.Y - 36, 8, 14), new Color(93, 40, 39, 125));
                break;
        }
    }

    private void DrawWildForestForeground(
        GameSession gameSession,
        Rectangle cameraBounds)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        if (!dungeon.IsAuthoredWildForest)
            return;

        WildForestSection section = dungeon.FindWildForestSection(
            gameSession.Player.Position);
        if (section == null)
            return;
        int firstX = cameraBounds.Left - cameraBounds.Left % 96;
        Color grass = section.CorruptionLevel > .55f
            ? new Color(35, 42, 31, 115)
            : new Color(42, 58, 37, 105);
        for (int x = firstX; x < cameraBounds.Right + 96; x += 96)
        {
            int hash = unchecked(x * 19349663);
            int height = 11 + Math.Abs(hash % 17);
            DrawRootLine(
                new Vector2(x, section.GroundY + 2),
                new Vector2(x - 7, section.GroundY - height),
                grass,
                4f);
            DrawRootLine(
                new Vector2(x + 6, section.GroundY + 2),
                new Vector2(x + 13, section.GroundY - height + 4),
                grass,
                3f);
        }
    }

    private void DrawCatacombDepth(
        GameSession session,
        Rectangle camera,
        GameTime gameTime)
    {
        _catacombEnvironmentRenderer.DrawDepth(session, camera, gameTime);
    }

    private void DrawAncientCatacombs(
        GameSession session,
        Rectangle visible,
        GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        _catacombEnvironmentRenderer.DrawWorld(
            session,
            visible,
            gameTime);
        foreach (RottenCorpseBurst burst in session.RottenCorpseBursts)
            _catacombRotSpriteRenderer.DrawBurst(burst);
        if (session.ShowMapDebug)
        {
            foreach (CurseZoneDefinition zone in map.CurseZones)
                DrawRectangleOutline(
                    zone.Bounds, 2, new Color(155, 85, 180, 190));
            foreach (EncounterZone zone in map.EncounterZones)
                DrawRectangleOutline(
                    zone.ActivationBounds, 2,
                    new Color(105, 190, 165, 190));
        }
    }

    private void DrawCatacombEnemy(CatacombEnemy e)
    {
        Rectangle b=e.Bounds;Color bone=new(184,178,154);Color armor=e.Type switch{EnemyType.CursedKnight=>new Color(82,74,75),EnemyType.DeathKnight=>new Color(69,66,72),EnemyType.FallenKnight=>new Color(83,77,78),EnemyType.UndeadGuard=>new Color(91,89,82),_=>bone};
        if(e.Type==EnemyType.Wraith)armor=e.IsEthereal?new Color(83,150,158,90):new Color(98,164,169,190);
        if(e.Type==EnemyType.GraveBat){_spriteBatch.Draw(_pixel,new Rectangle(b.X-12,b.Y+5,b.Width+24,b.Height/2),new Color(54,50,58));return;}
        if(e.Type==EnemyType.RottenCorpse)armor=new Color(91,94,70);
        _spriteBatch.Draw(_pixel,new Rectangle(b.X+5,b.Y+12,b.Width-10,b.Height-12),armor);
        _spriteBatch.Draw(_pixel,new Rectangle(b.Center.X-10,b.Y,20,18),e.Type is EnemyType.Skeleton or EnemyType.SkeletonArcher?bone:armor);
        if(e.Type is EnemyType.SkeletonArcher or EnemyType.SoulCollector)DrawRootLine(new Vector2(b.Right,b.Y+16),new Vector2(b.Right+18,b.Bottom-8),new Color(119,91,63),3f);
        else DrawRootLine(new Vector2(b.Right-3,b.Y+20),new Vector2(b.Right+25,b.Bottom-12),new Color(135,133,124),4f);
        if(e.Type==EnemyType.FallenKnight)_spriteBatch.Draw(_pixel,new Rectangle(b.X+3,b.Y-5,b.Width-6,5),new Color(133,102,61));
    }

    private void DrawCurseMeter(GameSession session)
    {
        if(session.CurrentDungeon?.IsAncientCatacombs!=true)return;
        Rectangle bar=new(270,58,150,8);_spriteBatch.Draw(_pixel,bar,new Color(35,27,42));int fill=(int)((bar.Width-2)*session.Curse.Ratio);if(fill>0)_spriteBatch.Draw(_pixel,new Rectangle(bar.X+1,bar.Y+1,fill,bar.Height-2),new Color(119,73,145));DrawRectangleOutline(bar,1,new Color(151,139,158));DrawDebugText(Loc.Get("hud.curse"),270,46,new Color(182,170,190),1);
    }

    private void DrawCityAlarm(GameSession session)
    {
        if (session.CurrentDungeon?.IsFallenCity != true)
            return;
        string alarm = string.Format(Loc.Get("hud.city_alarm"), session.CityAlarm.Level);
        Color alarmColor = session.CityAlarm.Level switch
        {
            3 => new Color(230, 73, 61), 2 => new Color(221, 130, 60),
            1 => new Color(209, 174, 77), _ => new Color(137, 148, 145)
        };
        if (session.CityAlarm.WatcherSignalFeedbackTimeRemaining > 0f)
        {
            float pulse = .5f + .5f * MathF.Sin(
                session.CityAlarm.WatcherSignalFeedbackTimeRemaining * 24f);
            alarmColor = Color.Lerp(
                new Color(238, 92, 57),
                new Color(249, 211, 105),
                pulse);
            DrawRectangleOutline(
                new Rectangle(263, 40, 152, 17),
                2,
                new Color(alarmColor, 180));
        }
        DrawDebugText(alarm, 270, 46, alarmColor, 1);
        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy is FallenCityEnemy watcher &&
                watcher.Type == EnemyType.BellWatcher &&
                watcher.IsSignalingAlarm)
            {
                DrawCenteredDebugText(
                    Loc.Get("hud.city_alarm_interrupt"),
                    _graphicsDevice.Viewport.Width / 2,
                    76,
                    new Color(232, 151, 68),
                    1);
                break;
            }
        }
        foreach (CityAlarmObjectDefinition alarmObject in session.CurrentDungeon.CityAlarmObjects)
        {
            if (!session.CityAlarm.IsDisabled(alarmObject.ObjectId) &&
                Vector2.DistanceSquared(session.Player.Position, alarmObject.Position) <=
                    alarmObject.DisableRadius * alarmObject.DisableRadius)
            {
                DrawCenteredDebugText(Loc.Get("hud.city_alarm_disable"),
                    _graphicsDevice.Viewport.Width / 2,
                    _graphicsDevice.Viewport.Height - 70,
                    new Color(221, 188, 112), 1);
                break;
            }
        }
        if (session.Map03ExitPrepared)
            DrawCenteredDebugText(Loc.Get("hud.path_unfinished"),
                _graphicsDevice.Viewport.Width / 2, 112,
                new Color(190, 151, 104), 1);
    }

    private void DrawDemonicCorruptionMeter(GameSession session)
    {
        if (session.CurrentDungeon?.IsDemonLands != true)
            return;
        Rectangle bar = new(270, 58, 170, 8);
        _spriteBatch.Draw(_pixel, bar, new Color(39, 18, 29));
        int fill = (int)MathF.Round((bar.Width - 2) * session.Corruption.Ratio);
        if (fill > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bar.X + 1, bar.Y + 1, fill, bar.Height - 2),
                session.Corruption.State == DemonicCorruptionState.Overrun
                    ? new Color(216, 48, 50)
                    : new Color(157, 48, 112));
        }
        DrawRectangleOutline(bar, 1, new Color(184, 103, 137));
        string stateKey = session.Corruption.State switch
        {
            DemonicCorruptionState.Tainted => "hud.corruption.tainted",
            DemonicCorruptionState.Corrupted => "hud.corruption.corrupted",
            DemonicCorruptionState.Overrun => "hud.corruption.overrun",
            _ => "hud.corruption.stable"
        };
        DrawDebugText(
            Loc.Format(
                "hud.corruption",
                (int)session.Corruption.Value,
                Loc.Get(stateKey)),
            270,
            46,
            new Color(208, 155, 176),
            1);
        if (session.CorruptionAnchors.DestroyedFeedbackTimeRemaining > 0f)
        {
            DrawCenteredDebugText(
                Loc.Get("hud.anchor_destroyed"),
                _graphicsDevice.Viewport.Width / 2,
                88,
                new Color(126, 226, 187),
                1);
        }
        if (session.Map04ExitPrepared)
        {
            DrawCenteredDebugText(
                Loc.Get("hud.future_map_5"),
                _graphicsDevice.Viewport.Width / 2,
                112,
                new Color(213, 151, 91),
                1);
        }
    }

    private void DrawBrokenKingBossBar(GameSession session)
    {
        if (session.CurrentDungeon?.IsFallenCity != true)
            return;
        FallenCityEnemy king = null;
        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy is FallenCityEnemy candidate &&
                candidate.Type == EnemyType.BrokenKing && candidate.IsAlive)
            {
                king = candidate;
                break;
            }
        }
        if (king == null || !king.IsHealthBarVisible)
            return;

        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        int width = Math.Clamp(viewportWidth * 7 / 16, 360, 680);
        int x = (viewportWidth - width) / 2;
        int y = viewportHeight - 104;
        DrawCenteredDebugText(
            Loc.Get("enemy.broken_king.name"),
            viewportWidth / 2,
            y - 24,
            new Color(220, 196, 139),
            1);
        DrawCenteredDebugText(
            Loc.Get("enemy.broken_king.title"),
            viewportWidth / 2,
            y - 11,
            new Color(151, 139, 124),
            1);

        var frame = new Rectangle(x, y + 4, width, 16);
        _spriteBatch.Draw(_pixel, frame, new Color(19, 15, 17, 235));
        DrawRectangleOutline(frame, 2, new Color(143, 111, 66, 235));
        var track = new Rectangle(x + 3, y + 7, width - 6, 10);
        _spriteBatch.Draw(_pixel, track, new Color(56, 23, 27, 240));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            king.CurrentHealth / (float)king.MaxHealth,
            0f,
            1f));
        if (fill > 0)
        {
            Color health = king.CombatPhase switch
            {
                3 => new Color(188, 68, 42),
                2 => new Color(151, 62, 47),
                _ => new Color(126, 48, 53)
            };
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(track.X, track.Y, fill, track.Height),
                health);
        }
        int phaseOne = track.X + track.Width * 3 / 5;
        int phaseTwo = track.X + track.Width / 4;
        _spriteBatch.Draw(_pixel,
            new Rectangle(phaseOne, track.Y - 2, 2, track.Height + 4),
            new Color(218, 188, 126, 190));
        _spriteBatch.Draw(_pixel,
            new Rectangle(phaseTwo, track.Y - 2, 2, track.Height + 4),
            new Color(218, 188, 126, 190));
    }

    private void DrawHornedGeneralBossBar(GameSession session)
    {
        if (session.CurrentDungeon?.IsDemonLands != true)
            return;
        DemonLandsEnemy general = null;
        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy is DemonLandsEnemy candidate &&
                candidate.Type == EnemyType.HornedGeneral && candidate.IsAlive)
            {
                general = candidate;
                break;
            }
        }
        if (general == null || !general.IsHealthBarVisible)
            return;

        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        int width = Math.Clamp(viewportWidth * 7 / 16, 360, 680);
        int x = (viewportWidth - width) / 2;
        int y = viewportHeight - 104;
        DrawCenteredDebugText(
            Loc.Get("enemy.horned_general.name"),
            viewportWidth / 2,
            y - 30,
            new Color(232, 177, 121),
            1);
        DrawCenteredDebugText(
            Loc.Get("enemy.horned_general.title"),
            viewportWidth / 2,
            y - 17,
            new Color(188, 111, 92),
            1);
        var frame = new Rectangle(x, y + 4, width, 16);
        _spriteBatch.Draw(_pixel, frame, new Color(22, 12, 16, 235));
        DrawRectangleOutline(frame, 2, new Color(157, 67, 55, 235));
        var track = new Rectangle(x + 3, y + 7, width - 6, 10);
        _spriteBatch.Draw(_pixel, track, new Color(62, 18, 27, 240));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            general.CurrentHealth / (float)general.MaxHealth,
            0f,
            1f));
        if (fill > 0)
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(track.X, track.Y, fill, track.Height),
                general.CombatPhase == 3
                    ? new Color(224, 60, 33)
                    : general.CombatPhase == 2
                        ? new Color(181, 47, 78)
                        : new Color(142, 54, 50));
        int phaseTwoMarker = track.X +
            (int)MathF.Round(track.Width * .60f);
        int phaseThreeMarker = track.X +
            (int)MathF.Round(track.Width * .25f);
        Color markerColor = new(232, 184, 112, 200);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(phaseTwoMarker, track.Y - 2, 2, track.Height + 4),
            markerColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(phaseThreeMarker, track.Y - 2, 2, track.Height + 4),
            markerColor);
    }

    private void DrawDemonFortressBossBar(GameSession session)
    {
        if (session.CurrentDungeon?.IsDemonFortress != true)
            return;

        DemonFortressEnemy target = null;
        float nearest = float.MaxValue;
        foreach (Enemy enemy in session.Enemies.Enemies)
        {
            if (enemy is not DemonFortressEnemy candidate || !candidate.IsAlive ||
                !candidate.IsBossTierEncounter || !candidate.IsHealthBarVisible)
                continue;
            float distance = Vector2.DistanceSquared(
                candidate.Position, session.Player.Position);
            if (distance >= nearest)
                continue;
            nearest = distance;
            target = candidate;
        }
        if (target == null)
            return;

        bool executioner = target.Type == EnemyType.BlackGuardExecutioner;
        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        int width = Math.Clamp(
            viewportWidth * (executioner ? 7 : 6) / 16, 350, executioner ? 700 : 610);
        int x = (viewportWidth - width) / 2;
        int y = viewportHeight - 104;
        string key = executioner
            ? "enemy.black_guard_executioner"
            : "enemy.ballista_gunner";
        DrawCenteredDebugText(
            Loc.Get(key + ".name"),
            viewportWidth / 2,
            y - 30,
            executioner ? new Color(220, 207, 195) : new Color(210, 170, 112),
            1);
        DrawCenteredDebugText(
            Loc.Get(key + ".title"),
            viewportWidth / 2,
            y - 17,
            executioner ? new Color(151, 63, 66) : new Color(139, 112, 82),
            1);

        var frame = new Rectangle(x, y + 4, width, executioner ? 17 : 14);
        _spriteBatch.Draw(_pixel, frame, new Color(18, 13, 16, 238));
        DrawRectangleOutline(frame, 2,
            executioner ? new Color(132, 52, 57, 240) : new Color(126, 91, 55, 235));
        var track = new Rectangle(x + 3, y + 7, width - 6, frame.Height - 6);
        _spriteBatch.Draw(_pixel, track, new Color(54, 23, 25, 242));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            target.CurrentHealth / (float)target.MaxHealth, 0f, 1f));
        if (fill > 0)
        {
            Color health = executioner
                ? target.BossCombatPhase == 2
                    ? new Color(185, 46, 48)
                    : new Color(126, 48, 55)
                : target.BossCombatPhase == 2
                    ? new Color(194, 82, 41)
                    : new Color(143, 75, 43);
            _spriteBatch.Draw(_pixel,
                new Rectangle(track.X, track.Y, fill, track.Height), health);
        }
        int phaseMarker = track.X + (int)MathF.Round(track.Width * .60f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(phaseMarker, track.Y - 2, 2, track.Height + 4),
            new Color(220, 183, 120, 205));
    }

    private void DrawSanctumEnding(GameSession session)
    {
        int width = _graphicsDevice.Viewport.Width, height = _graphicsDevice.Viewport.Height;
        float fade = session.SanctumEnding.FadeRatio;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height), new Color(5, 3, 12, (int)(72 * fade)));
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, 72), new Color(5, 3, 9, (int)(220 * fade)));
        _spriteBatch.Draw(_pixel, new Rectangle(0, height - 150, width, 150), new Color(5, 3, 9, (int)(230 * fade)));
        DrawCenteredDebugText(Loc.Get("sanctum.ending.title"), width / 2, 28,
            new Color(226, 199, 150) * fade, 2);
        string line = Loc.Get(session.SanctumEnding.TextKey);
        DrawCenteredDebugText(line, width / 2, height - 110, new Color(229, 217, 235) * fade,
            FitDebugTextScale(line, width - 60, 2));
        string returnText = Loc.Get("sanctum.ending.return");
        DrawCenteredDebugText(returnText, width / 2, height - 52, new Color(157, 146, 164) * fade,
            FitDebugTextScale(returnText, width - 60, 1));
    }

    internal static ThroneboundSeraphEnemy SelectSeraphSubBossBarTarget(
        IReadOnlyList<Enemy> enemies, Vector2 playerPosition)
    {
        ThroneboundSeraphEnemy selected = null;
        float nearest = 650f * 650f;
        foreach (Enemy enemy in enemies)
        {
            if (enemy is not ThroneboundSeraphEnemy seraph || !seraph.IsAlive ||
                seraph.IntroCompletionCount == 0 || seraph.IsReturningToAuthoredHome)
                continue;
            float distance = Vector2.DistanceSquared(seraph.Position, playerPosition);
            if (distance >= nearest) continue;
            nearest = distance; selected = seraph;
        }
        return selected;
    }

    internal static Enemy SelectSanctumBossBarTarget(IReadOnlyList<Enemy> enemies, Vector2 player) =>
        (Enemy)SelectVaeltharBossBarTarget(enemies, player) ?? SelectSeraphSubBossBarTarget(enemies, player);

    private void DrawSeraphSubBossBar(ThroneboundSeraphEnemy target)
    {
        if (target == null) return;
        int viewportWidth = _graphicsDevice.Viewport.Width;
        int width = Math.Clamp(viewportWidth * 6 / 16, 370, 640);
        int x = (viewportWidth - width) / 2;
        int y = _graphicsDevice.Viewport.Height - 108;
        DrawCenteredDebugText(Loc.Get("enemy.thronebound_seraph.name"), viewportWidth / 2,
            y - 30, new Color(218, 205, 178), 1);
        DrawCenteredDebugText(Loc.Get("enemy.thronebound_seraph.title"), viewportWidth / 2,
            y - 17, new Color(154, 100, 158), 1);
        var frame = new Rectangle(x, y + 4, width, 16);
        _spriteBatch.Draw(_pixel, frame, new Color(16, 11, 18, 242));
        DrawRectangleOutline(frame, 2, new Color(145, 109, 72, 240));
        var track = new Rectangle(x + 3, y + 7, width - 6, 10);
        _spriteBatch.Draw(_pixel, track, new Color(48, 20, 35, 245));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            target.CurrentHealth / (float)target.MaxHealth, 0f, 1f));
        if (fill > 0) _spriteBatch.Draw(_pixel,
            new Rectangle(track.X, track.Y, fill, track.Height), target.CombatPhase == 2 ?
                new Color(166, 66, 151) : new Color(124, 78, 111));
        _spriteBatch.Draw(_pixel, new Rectangle(track.X + (int)(track.Width * .55f),
            track.Y - 2, 2, track.Height + 4), new Color(201, 168, 110, 190));
    }

    internal static VaeltharEnemy SelectVaeltharBossBarTarget(IReadOnlyList<Enemy> enemies, Vector2 player)
    {
        VaeltharEnemy target = null;
        float nearest = 760f * 760f;
        foreach (Enemy enemy in enemies)
        {
            if (enemy is not VaeltharEnemy king || !king.IsAlive || king.IntroCompletionCount == 0 ||
                king.IsReturningToAuthoredHome) continue;
            float distance = Vector2.DistanceSquared(king.Position, player);
            if (distance >= nearest) continue;
            nearest = distance; target = king;
        }
        return target;
    }
    private void DrawVaeltharArenaDim(VaeltharEnemy king)
    {
        if (king == null || king.UltimateDimRatio <= 0) return;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
            new Color(7, 2, 15, (int)(36 * king.UltimateDimRatio)));
    }
    private void DrawVaeltharBossBar(VaeltharEnemy king)
    {
        if (king == null) return;
        int screen = _graphicsDevice.Viewport.Width;
        int width = Math.Clamp(screen * 3 / 5, 540, 900);
        int x = (screen - width) / 2, y = _graphicsDevice.Viewport.Height - 122;
        DrawCenteredDebugText(Loc.Get("enemy.vaelthar.name"), screen / 2, y - 38,
            new Color(236, 211, 165), 2);
        DrawCenteredDebugText(Loc.Get("enemy.vaelthar.title"), screen / 2, y - 17,
            new Color(170, 106, 193), 1);
        var frame = new Rectangle(x, y + 4, width, 24);
        _spriteBatch.Draw(_pixel, frame, new Color(10, 7, 16, 245));
        DrawRectangleOutline(frame, 3, new Color(170, 121, 64, 240));
        var track = new Rectangle(x + 4, y + 8, width - 8, 16);
        _spriteBatch.Draw(_pixel, track, new Color(43, 17, 38, 245));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(king.CurrentHealth / (float)king.MaxHealth, 0, 1));
        if (fill > 0)
        {
            Color health = king.CombatPhase == 3 ? new Color(180, 61, 151) :
                king.CombatPhase == 2 ? new Color(126, 60, 149) : new Color(117, 42, 65);
            _spriteBatch.Draw(_pixel, new Rectangle(track.X, track.Y, fill, track.Height), health);
            _spriteBatch.Draw(_pixel, new Rectangle(track.X, track.Y, fill, 2), new Color(222, 172, 95));
        }
        foreach (float threshold in VaeltharPhaseThresholds)
            _spriteBatch.Draw(_pixel, new Rectangle(track.X + (int)(track.Width * threshold),
                track.Y - 2, 2, track.Height + 4), new Color(208, 174, 122, 180));
    }

    private void DrawDemonCityBossBar(GameSession session)
    {
        if (session.CurrentDungeon?.IsDemonCity != true)
            return;

        Enemy selected = SelectDemonCityBossBarTarget(
            session.Enemies.Enemies,
            session.Player.Position);
        if (selected is MalacharEnemy malachar)
        {
            DrawMalacharBossBar(malachar);
            return;
        }
        if (selected is not DemonCityBossEnemy target)
            return;

        bool magistrate = target.Type == EnemyType.AbyssalMagistrate;
        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        int width = Math.Clamp(viewportWidth * (magistrate ? 7 : 6) / 16,
            370, magistrate ? 720 : 640);
        int x = (viewportWidth - width) / 2;
        int y = viewportHeight - 108;
        string key = magistrate
            ? "enemy.abyssal_magistrate"
            : "enemy.cathedral_butcher";
        DrawCenteredDebugText(Loc.Get(key + ".name"), viewportWidth / 2,
            y - 30, magistrate ? new Color(220, 198, 233) :
                new Color(218, 194, 169), 1);
        DrawCenteredDebugText(Loc.Get(key + ".title"), viewportWidth / 2,
            y - 17, magistrate ? new Color(145, 94, 176) :
                new Color(142, 59, 55), 1);

        var frame = new Rectangle(x, y + 4, width, magistrate ? 18 : 16);
        _spriteBatch.Draw(_pixel, frame, new Color(16, 11, 18, 242));
        DrawRectangleOutline(frame, 2, magistrate
            ? new Color(119, 75, 146, 240)
            : new Color(125, 61, 52, 240));
        var track = new Rectangle(x + 3, y + 7, width - 6, frame.Height - 6);
        _spriteBatch.Draw(_pixel, track, new Color(48, 20, 27, 245));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            target.CurrentHealth / (float)target.MaxHealth, 0f, 1f));
        if (fill > 0)
        {
            Color health = magistrate
                ? target.BossCombatPhase == 3
                    ? new Color(173, 72, 194)
                    : target.BossCombatPhase == 2
                        ? new Color(133, 63, 164)
                        : new Color(96, 57, 126)
                : target.BossCombatPhase == 2
                    ? new Color(177, 48, 47)
                    : new Color(127, 51, 48);
            _spriteBatch.Draw(_pixel,
                new Rectangle(track.X, track.Y, fill, track.Height), health);
        }

        float[] thresholds = magistrate
            ? new[] { .65f, .30f }
            : new[] { .55f };
        foreach (float threshold in thresholds)
        {
            int marker = track.X + (int)MathF.Round(track.Width * threshold);
            _spriteBatch.Draw(_pixel,
                new Rectangle(marker, track.Y - 2, 2, track.Height + 4),
                new Color(224, 187, 119, 210));
        }
    }

    internal static Enemy SelectDemonCityBossBarTarget(
        IReadOnlyList<Enemy> enemies,
        Vector2 playerPosition)
    {
        Enemy selected = null;
        float nearest = float.MaxValue;
        foreach (Enemy enemy in enemies)
        {
            bool ready = enemy switch
            {
                MalacharEnemy malachar => malachar.BossIntroCompleted,
                DemonCityBossEnemy boss => boss.BossIntroCompleted,
                _ => false
            };
            if (!ready || !enemy.IsAlive || !enemy.IsHealthBarVisible)
                continue;
            float distance = Vector2.DistanceSquared(
                enemy.Position, playerPosition);
            if (distance >= nearest)
                continue;
            nearest = distance;
            selected = enemy;
        }
        return selected;
    }

    private void DrawMalacharBossBar(MalacharEnemy target)
    {
        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        int width = Math.Clamp(viewportWidth * 3 / 5, 520, 840);
        int x = (viewportWidth - width) / 2;
        int y = viewportHeight - 120;
        DrawCenteredDebugText(Loc.Get("enemy.malachar.name"),
            viewportWidth / 2, y - 35, new Color(237, 215, 166), 2);
        DrawCenteredDebugText(Loc.Get("enemy.malachar.title"),
            viewportWidth / 2, y - 17, new Color(164, 103, 181), 1);

        var frame = new Rectangle(x, y + 5, width, 21);
        _spriteBatch.Draw(_pixel, frame, new Color(12, 9, 17, 246));
        DrawRectangleOutline(frame, 3, new Color(164, 119, 71, 245));
        var track = new Rectangle(x + 4, y + 9, width - 8, frame.Height - 8);
        _spriteBatch.Draw(_pixel, track, new Color(39, 19, 38, 248));
        int fill = (int)MathF.Round(track.Width * Math.Clamp(
            target.CurrentHealth / (float)target.MaxHealth, 0f, 1f));
        if (fill > 0)
        {
            Color health = target.CombatPhase switch
            {
                3 => new Color(188, 70, 116),
                2 => new Color(133, 64, 151),
                _ => new Color(91, 58, 112)
            };
            _spriteBatch.Draw(_pixel,
                new Rectangle(track.X, track.Y, fill, track.Height), health);
            _spriteBatch.Draw(_pixel,
                new Rectangle(track.X, track.Y, fill, 2),
                new Color(228, 183, 108, 205));
        }
        foreach (float threshold in new[] { .70f, .35f })
        {
            int marker = track.X + (int)MathF.Round(track.Width * threshold);
            _spriteBatch.Draw(_pixel,
                new Rectangle(marker, track.Y - 3, 3, track.Height + 6),
                new Color(229, 193, 121, 225));
        }
    }

    private void DrawDemonLands(
        GameSession session,
        Rectangle visibleBounds,
        GameTime gameTime)
    {
        _demonLandsEnvironmentRenderer.DrawWorld(
            session,
            visibleBounds,
            gameTime);
    }

    private void DrawDungeon(
        GameSession gameSession,
        Rectangle cameraBounds,
        GameTime gameTime)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        RegionTheme theme = RegionTheme.For(gameSession.CurrentRegion);
        Rectangle visibleBounds = cameraBounds;
        visibleBounds.Inflate(80, 80);

        if (dungeon.IsDemonLands)
        {
            DrawDemonLands(gameSession, visibleBounds, gameTime);
            if (dungeon.BossRoom.Bounds.Intersects(visibleBounds))
                DrawExitMarker(
                    gameSession.ExitPosition,
                    gameSession.IsExitUnlocked);
            return;
        }

        if (dungeon.IsDemonFortress)
        {
            _demonFortressEnvironmentRenderer.DrawWorld(
                gameSession, visibleBounds, gameTime);
            DrawExitMarker(gameSession.ExitPosition, gameSession.IsExitUnlocked);
            return;
        }

        if (dungeon.IsDemonCity)
        {
            _demonCityEnvironmentRenderer.DrawWorld(
                gameSession, visibleBounds, gameTime);
            DrawExitMarker(gameSession.ExitPosition,
                gameSession.IsExitUnlocked);
            return;
        }

        if (dungeon.IsDemonKingSanctum)
        {
            _demonKingSanctumEnvironmentRenderer.DrawWorld(
                gameSession, visibleBounds, gameTime);
            DrawExitMarker(gameSession.ExitPosition, gameSession.Map07EndpointPrepared);
            if (gameSession.Map07EndpointPrepared && visibleBounds.Contains(gameSession.ExitPosition.ToPoint()))
                DrawDebugLabel(Loc.Get(gameSession.Map07EndingSeen ? "sanctum.ending.seen" :
                    gameSession.IsPlayerNearExit() ? "sanctum.ending.interact" : "sanctum.endpoint.prepared"), (int)gameSession.ExitPosition.X - 90,
                    (int)gameSession.ExitPosition.Y - 90, new Color(202, 178, 222));
            return;
        }

        if (dungeon.IsFallenCity)
        {
            _fallenCityEnvironmentRenderer.DrawWorld(
                gameSession,
                visibleBounds,
                gameTime);
            if (dungeon.BossRoom.Bounds.Intersects(visibleBounds))
                DrawExitMarker(
                    gameSession.ExitPosition,
                    gameSession.IsExitUnlocked);
            return;
        }

        if (dungeon.IsAuthoredWildForest)
        {
            DrawAuthoredWildForestGameplay(gameSession, visibleBounds, theme);
            return;
        }

        if (dungeon.IsAncientCatacombs)
        {
            DrawAncientCatacombs(gameSession, visibleBounds, gameTime);
            return;
        }

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            _spriteBatch.Draw(_pixel, room.Bounds, theme.GetRoomFloor(room.Type));
            DrawRectangleOutline(room.Bounds, 5, theme.BoundaryAccent);
        }

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (!corridor.Intersects(visibleBounds))
                continue;

            _spriteBatch.Draw(_pixel, corridor, theme.CorridorFloor);
            DrawRectangleOutline(corridor, 3, theme.BoundaryAccent);
        }

        foreach (Platform platform in dungeon.Platforms)
        {
            if (!platform.Bounds.Intersects(visibleBounds))
                continue;

            Color color = platform.Kind switch
            {
                PlatformKind.Raised => theme.BoundaryAccent,
                PlatformKind.Obstacle => theme.Root,
                PlatformKind.Transition => theme.CorridorFloor,
                _ => theme.Boundary
            };
            _spriteBatch.Draw(_pixel, platform.Bounds, color);
            DrawRectangleOutline(platform.Bounds, 3, theme.BoundaryAccent);

            if (platform.Kind == PlatformKind.Ground ||
                platform.Kind == PlatformKind.Transition)
            {
                DrawForestGroundDetails(platform.Bounds, visibleBounds, theme);
            }
        }

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (!room.Bounds.Intersects(visibleBounds))
                continue;

            if (room.Type == RoomType.Start)
                DrawStartMarker(new Vector2(room.Bounds.Left + 110f, room.GroundY - 35f));
            else if (room.Type == RoomType.Treasure)
                DrawTreasureRoomMarker(gameSession.Chest.Position);
            else if (room.Type == RoomType.Boss)
                DrawBossRoomMarker(new Vector2(room.Center.X, room.GroundY - 70f));
            else if (room.Type == RoomType.Exit)
                DrawExitMarker(gameSession.ExitPosition, gameSession.IsExitUnlocked);
        }
    }

    private void DrawFloorGrid(
        Rectangle area,
        Rectangle visibleBounds,
        Color color)
    {
        const int tileSize = 48;
        int firstX = Math.Max(area.Left, visibleBounds.Left);
        int firstY = Math.Max(area.Top, visibleBounds.Top);
        firstX = area.Left + ((firstX - area.Left) / tileSize) * tileSize;
        firstY = area.Top + ((firstY - area.Top) / tileSize) * tileSize;
        int right = Math.Min(area.Right, visibleBounds.Right);
        int bottom = Math.Min(area.Bottom, visibleBounds.Bottom);

        for (int x = firstX; x < right; x += tileSize)
            _spriteBatch.Draw(_pixel, new Rectangle(x, area.Top, 1, area.Height), color);

        for (int y = firstY; y < bottom; y += tileSize)
            _spriteBatch.Draw(_pixel, new Rectangle(area.Left, y, area.Width, 1), color);
    }

    private void DrawForestGroundDetails(
        Rectangle area,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        const int cellSize = 64;
        int firstX = Math.Max(area.Left, visibleBounds.Left);
        int firstY = Math.Max(area.Top, visibleBounds.Top);
        firstX = area.Left + ((firstX - area.Left) / cellSize) * cellSize;
        firstY = area.Top + ((firstY - area.Top) / cellSize) * cellSize;
        int right = Math.Min(area.Right, visibleBounds.Right);
        int bottom = Math.Min(area.Bottom, visibleBounds.Bottom);

        for (int y = firstY; y < bottom; y += cellSize)
        {
            for (int x = firstX; x < right; x += cellSize)
            {
                int hash = unchecked(x * 73856093 ^ y * 19349663);
                int offsetX = 8 + Math.Abs(hash % 29);
                int offsetY = 8 + Math.Abs(hash / 31 % 29);
                int detailX = Math.Min(x + offsetX, area.Right - 13);
                int detailY = Math.Min(y + offsetY, area.Bottom - 8);

                if ((hash & 3) == 0)
                {
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX, detailY, 13, 6),
                        theme.Corruption);
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX + 4, detailY - 4, 5, 14),
                        theme.Corruption);
                }
                else
                {
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX, detailY, 15, 3),
                        theme.GroundMarking);
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(detailX + 2, detailY - 3, 3, 8),
                        theme.Root);
                }
            }
        }
    }

    private void DrawForestBoundaryDetails(
        Rectangle wall,
        Rectangle visibleBounds,
        RegionTheme theme)
    {
        if (!wall.Intersects(visibleBounds))
            return;

        for (int x = wall.Left + 18; x < wall.Right - 18; x += 72)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wall.Top - 4, 5, 12),
                theme.Root);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x + 5, wall.Top - 7, 7, 4),
                theme.BoundaryAccent);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wall.Bottom - 8, 5, 12),
                theme.Root);
        }

        for (int y = wall.Top + 22; y < wall.Bottom - 18; y += 72)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wall.Left - 4, y, 12, 5),
                theme.Root);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wall.Right - 8, y, 12, 5),
                theme.Root);
        }
    }

    private void DrawStartMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 25, (int)center.Y - 25, 50, 50);
        var inner = new Rectangle((int)center.X - 17, (int)center.Y - 17, 34, 34);
        DrawRectangleOutline(outer, 3, new Color(76, 169, 126, 190));
        DrawRectangleOutline(inner, 2, new Color(111, 205, 157, 150));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 3, (int)center.Y - 13, 6, 26), new Color(128, 218, 169, 165));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 13, (int)center.Y - 3, 26, 6), new Color(128, 218, 169, 165));
    }

    private void DrawTreasureRoomMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 44, (int)center.Y - 38, 88, 76);
        DrawRectangleOutline(outer, 3, new Color(191, 161, 72, 160));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(outer.X + 8, outer.Y + 8, 8, 8),
            new Color(226, 196, 91, 145));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(outer.Right - 16, outer.Bottom - 16, 8, 8),
            new Color(226, 196, 91, 145));
    }

    private void DrawBossRoomMarker(Vector2 center)
    {
        var outer = new Rectangle((int)center.X - 72, (int)center.Y - 62, 144, 124);
        DrawRectangleOutline(outer, 5, new Color(139, 45, 48, 175));
        DrawRectangleOutline(
            new Rectangle(outer.X + 12, outer.Y + 12, outer.Width - 24, outer.Height - 24),
            2,
            new Color(188, 68, 57, 120));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle((int)center.X - 30, (int)center.Y - 3, 60, 6),
            new Color(155, 54, 48, 115));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle((int)center.X - 3, (int)center.Y - 30, 6, 60),
            new Color(155, 54, 48, 115));
    }

    private void DrawExitMarker(Vector2 center, bool isUnlocked)
    {
        var frame = new Rectangle((int)center.X - 28, (int)center.Y - 34, 56, 68);
        var doorway = new Rectangle(frame.X + 9, frame.Y + 10, frame.Width - 18, frame.Height - 10);
        Color frameColor = isUnlocked
            ? new Color(108, 155, 84, 220)
            : new Color(136, 79, 52, 220);
        Color outlineColor = isUnlocked
            ? new Color(159, 226, 121)
            : new Color(218, 99, 73);
        _spriteBatch.Draw(_pixel, frame, frameColor);
        _spriteBatch.Draw(_pixel, doorway, new Color(30, 21, 43));
        DrawRectangleOutline(frame, 3, outlineColor);

        if (isUnlocked)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(doorway.Center.X - 3, doorway.Center.Y - 3, 6, 6),
                new Color(177, 235, 126));
            return;
        }

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.X + 7, doorway.Y + 4, 5, doorway.Height - 8),
            new Color(177, 79, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.Right - 12, doorway.Y + 4, 5, doorway.Height - 8),
            new Color(177, 79, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(doorway.X + 5, doorway.Center.Y - 3, doorway.Width - 10, 6),
            new Color(207, 94, 68));
    }

    private void DrawArena()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var arena = new Rectangle(0, 0, viewport.Width, viewport.Height);
        const int wallThickness = 16;
        const int tileSize = 48;

        _spriteBatch.Draw(_pixel, arena, new Color(37, 43, 46));

        for (int x = wallThickness; x < viewport.Width - wallThickness; x += tileSize)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, wallThickness, 1, viewport.Height - wallThickness * 2),
                new Color(67, 75, 77, 80));
        }

        for (int y = wallThickness; y < viewport.Height - wallThickness; y += tileSize)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(wallThickness, y, viewport.Width - wallThickness * 2, 1),
                new Color(67, 75, 77, 80));
        }

        Color wallColor = new(66, 61, 62);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, wallThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, viewport.Height - wallThickness, viewport.Width, wallThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, wallThickness, viewport.Height), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(viewport.Width - wallThickness, 0, wallThickness, viewport.Height), wallColor);

        for (int x = 0; x < viewport.Width; x += 64)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x, 0, 2, wallThickness), new Color(92, 84, 82));
            _spriteBatch.Draw(_pixel, new Rectangle(x + 32, viewport.Height - wallThickness, 2, wallThickness), new Color(92, 84, 82));
        }

        for (int y = 0; y < viewport.Height; y += 64)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, y, wallThickness, 2), new Color(92, 84, 82));
            _spriteBatch.Draw(_pixel, new Rectangle(viewport.Width - wallThickness, y + 32, wallThickness, 2), new Color(92, 84, 82));
        }

        DrawCornerBrazier(22, 22);
        DrawCornerBrazier(viewport.Width - 34, 22);
        DrawCornerBrazier(22, viewport.Height - 34);
        DrawCornerBrazier(viewport.Width - 34, viewport.Height - 34);
    }

    private void DrawCornerBrazier(int x, int y)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y + 8, 12, 4), new Color(108, 75, 47));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 3, y + 3, 6, 7), new Color(226, 108, 37));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 5, y, 3, 6), new Color(250, 190, 54));
    }

    private void DrawAttackArea(Rectangle attackArea)
    {
        _spriteBatch.Draw(_pixel, attackArea, new Color(245, 210, 90, 55));
        DrawRectangleOutline(attackArea, 2, new Color(250, 225, 125, 150));
    }

    private void DrawCombatDebug(GameSession gameSession)
    {
        DrawDebugZone(
            gameSession.Player.Bounds,
            new Color(86, 220, 112, 35),
            new Color(86, 220, 112, 220));
        DrawDebugZone(
            gameSession.Player.DefenseBounds,
            new Color(76, 145, 230, 30),
            new Color(76, 170, 245, 220));

        foreach (Enemy enemy in gameSession.Enemies.Enemies)
        {
            if (!enemy.IsAlive ||
                !enemy.Bounds.Intersects(gameSession.Camera.ViewBounds))
                continue;

            DrawDebugZone(
                enemy.Bounds,
                new Color(225, 77, 181, 25),
                new Color(225, 77, 181, 190));

            if (gameSession.Player.WeaponFamily == WeaponFamily.HunterBow &&
                (enemy.IsCombatAttackTelegraphing || enemy.IsStaggered))
            {
                DrawDebugZone(
                    enemy.WeakPointBounds,
                    new Color(240, 214, 92, 45),
                    new Color(255, 226, 105, 235));
            }

            if (enemy.Type != EnemyType.GoblinHunter &&
                (enemy.IsCombatAttackTelegraphing ||
                 enemy.IsCombatAttackActive))
            {
                Color attackColor = enemy.IsCombatAttackActive
                    ? new Color(237, 73, 64, 230)
                    : new Color(225, 151, 61, 190);
                DrawDebugZone(
                    enemy.ActiveAttackArea,
                    new Color(
                        attackColor.R,
                        attackColor.G,
                        attackColor.B,
                        (byte)(enemy.IsCombatAttackActive ? 42 : 20)),
                    attackColor);
            }
        }

        foreach (Projectile projectile in gameSession.Projectiles.Projectiles)
        {
            if (!projectile.Bounds.Intersects(gameSession.Camera.ViewBounds))
                continue;

            DrawDebugZone(
                projectile.Bounds,
                new Color(252, 224, 82, 45),
                new Color(252, 224, 82, 235));
        }

        if (gameSession.ShouldRenderBoss && gameSession.Boss.IsAlive)
        {
            DrawDebugZone(
                gameSession.Boss.Bounds,
                new Color(225, 77, 181, 25),
                new Color(225, 77, 181, 190));

            if (gameSession.Player.WeaponFamily == WeaponFamily.HunterBow &&
                gameSession.Boss.IsStaggered)
            {
                DrawDebugZone(
                    gameSession.Boss.WeakPointBounds,
                    new Color(240, 214, 92, 45),
                    new Color(255, 226, 105, 235));
            }

            if (gameSession.Boss.Attack.IsTelegraphing ||
                gameSession.Boss.Attack.IsActive)
            {
                Color attackColor = gameSession.Boss.Attack.IsActive
                    ? new Color(237, 73, 64, 230)
                    : new Color(225, 151, 61, 190);
                DrawDebugZone(
                    gameSession.Boss.AttackArea,
                    new Color(
                        attackColor.R,
                        attackColor.G,
                        attackColor.B,
                        (byte)(gameSession.Boss.Attack.IsActive ? 42 : 20)),
                    attackColor);
            }
        }
    }

    private void DrawWildForestMapDebug(GameSession gameSession)
    {
        DungeonMap dungeon = gameSession.CurrentDungeon;
        Rectangle visibleBounds = gameSession.Camera.ViewBounds;

        for (int sectionIndex = 0;
             sectionIndex < dungeon.WildForestSections.Count;
             sectionIndex++)
        {
            WildForestSection section = dungeon.WildForestSections[sectionIndex];
            if (!section.Bounds.Intersects(visibleBounds))
                continue;

            Color outline = section.Kind == WildForestSectionKind.AncientSanctuary
                ? new Color(235, 92, 72, 230)
                : new Color(115, 210, 178, 175);
            DrawRectangleOutline(section.Bounds, 2, outline);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(section.Bounds.Left, section.Bounds.Top, 3, section.Bounds.Height),
                outline);
        }

        for (int platformIndex = 0;
             platformIndex < dungeon.Platforms.Count;
             platformIndex++)
        {
            Platform platform = dungeon.Platforms[platformIndex];
            if (platform.Bounds.Intersects(visibleBounds))
                DrawRectangleOutline(platform.Bounds, 1, new Color(238, 221, 112, 190));
        }

        for (int zoneIndex = 0;
             zoneIndex < dungeon.EncounterZones.Count;
             zoneIndex++)
        {
            EncounterZone zone = dungeon.EncounterZones[zoneIndex];
            if (!zone.ActivationBounds.Intersects(visibleBounds))
                continue;

            Color outline = zone.IsBossZone
                ? new Color(255, 75, 75, 235)
                : zone.IsEliteZone
                    ? new Color(226, 102, 232, 225)
                    : new Color(76, 165, 245, 205);
            DrawDebugZone(
                zone.ActivationBounds,
                new Color(outline.R, outline.G, outline.B, (byte)15),
                outline);
            DrawRectangleOutline(zone.GroundBounds, 2, new Color(98, 214, 133, 210));
        }

        for (int socketIndex = 0;
             socketIndex < dungeon.SpawnSockets.Count;
             socketIndex++)
        {
            SpawnSocket socket = dungeon.SpawnSockets[socketIndex];
            if (!visibleBounds.Contains(socket.Position.ToPoint()))
                continue;

            Color color = socket.Role switch
            {
                SpawnSocketRole.GroundMelee => new Color(112, 229, 132),
                SpawnSocketRole.Ranged => new Color(92, 179, 255),
                SpawnSocketRole.Flying => new Color(126, 225, 241),
                SpawnSocketRole.Ambush => new Color(229, 180, 72),
                SpawnSocketRole.Heavy => new Color(236, 119, 75),
                SpawnSocketRole.Elite => new Color(224, 101, 235),
                SpawnSocketRole.Boss => new Color(255, 62, 62),
                _ => Color.White
            };
            int x = (int)socket.Position.X;
            int y = (int)socket.Position.Y;
            _spriteBatch.Draw(_pixel, new Rectangle(x - 8, y - 2, 17, 4), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x - 2, y - 8, 4, 17), color);
            DrawRectangleOutline(socket.PlacementBounds, 1, new Color(color.R, color.G, color.B, (byte)145));
        }
    }

    private void DrawDebugZone(
        Rectangle bounds,
        Color fillColor,
        Color outlineColor)
    {
        _spriteBatch.Draw(_pixel, bounds, fillColor);
        DrawRectangleOutline(bounds, 2, outlineColor);
    }

    private void DrawLegacyPlayer(PlayerCharacter player)
    {
        Vector2 topLeft = player.Position - player.Size / 2f;
        int centerY = (int)player.Position.Y;
        bool isAttacking = player.Combat.CurrentAttack != null &&
            (player.Combat.State == CombatState.LightAttack ||
             player.Combat.State == CombatState.HeavyAttack);
        int swordLength = player.Combat.CurrentAttack?.Kind switch
        {
            AttackKind.LightTwo => 55,
            AttackKind.LightThree => 64,
            AttackKind.Heavy => 76,
            AttackKind.LightOne => 49,
            _ => 28
        };
        int swordOffsetY = player.Combat.CurrentAttack?.Kind switch
        {
            AttackKind.LightTwo => -9,
            AttackKind.LightThree => 8,
            AttackKind.Heavy => 11,
            _ => 3
        };
        const int swordThickness = 6;

        Rectangle shadow = new(
            (int)topLeft.X + 4,
            (int)(topLeft.Y + player.Size.Y) - 5,
            (int)player.Size.X - 8,
            8);
        Rectangle body = new(
            (int)topLeft.X + 5,
            (int)topLeft.Y + 22,
            (int)player.Size.X - 10,
            (int)player.Size.Y - 22);
        Rectangle head = new(
            (int)topLeft.X + 10,
            (int)topLeft.Y,
            (int)player.Size.X - 20,
            24);
        Rectangle leftLeg = new(body.X + 4, body.Bottom - 5, 8, 10);
        Rectangle rightLeg = new(body.Right - 12, body.Bottom - 5, 8, 10);
        Rectangle leftArm = new(body.X - 4, body.Y + 5, 6, 19);
        Rectangle rightArm = new(body.Right - 2, body.Y + 5, 6, 19);
        Rectangle belt = new(body.X, body.Y + 17, body.Width, 5);

        Rectangle swordBlade;
        Rectangle swordGuard;
        Rectangle swordHandle;

        if (player.Facing == FacingDirection.Left)
        {
            swordBlade = new Rectangle(
                (int)topLeft.X - swordLength + 3,
                centerY + swordOffsetY,
                swordLength,
                swordThickness);
            swordGuard = new Rectangle(
                swordBlade.Right - 3,
                swordBlade.Y - 4,
                6,
                14);
            swordHandle = new Rectangle(
                swordGuard.Right - 1,
                swordBlade.Y,
                9,
                swordThickness);
        }
        else
        {
            swordBlade = new Rectangle(
                (int)(topLeft.X + player.Size.X) - 3,
                centerY + swordOffsetY,
                swordLength,
                swordThickness);
            swordGuard = new Rectangle(
                swordBlade.X - 3,
                swordBlade.Y - 4,
                6,
                14);
            swordHandle = new Rectangle(
                swordGuard.X - 8,
                swordBlade.Y,
                9,
                swordThickness);
        }

        Rectangle eye = CreatePlayerEye(head, player.Facing);
        Color bodyColor = player.IsHitFlashing
            ? new Color(220, 75, 76)
            : player.Combat.IsGuardBroken
                ? new Color(201, 103, 58)
                : player.Combat.IsBlockFeedbackActive
                    ? new Color(105, 174, 211)
                    : player.Combat.IsDodgeInvulnerable
                        ? new Color(105, 157, 196)
                        : new Color(48, 112, 168);
        Color headColor = player.IsHitFlashing
            ? new Color(255, 220, 205)
            : player.Combat.IsGuardBroken
                ? new Color(237, 167, 112)
                : new Color(232, 185, 137);
        Color bladeColor = player.Combat.IsAttackWindingUp
            ? new Color(224, 151, 69)
            : isAttacking
                ? new Color(245, 238, 185)
                : new Color(205, 215, 226);

        if (player.IsGrounded)
            _spriteBatch.Draw(_pixel, shadow, new Color(16, 19, 22, 150));
        _spriteBatch.Draw(_pixel, swordHandle, new Color(91, 57, 38));
        _spriteBatch.Draw(_pixel, swordBlade, bladeColor);
        _spriteBatch.Draw(_pixel, swordGuard, new Color(126, 85, 50));
        _spriteBatch.Draw(_pixel, leftLeg, new Color(34, 55, 79));
        _spriteBatch.Draw(_pixel, rightLeg, new Color(34, 55, 79));
        _spriteBatch.Draw(_pixel, body, bodyColor);
        _spriteBatch.Draw(_pixel, leftArm, bodyColor);
        _spriteBatch.Draw(_pixel, rightArm, bodyColor);
        _spriteBatch.Draw(_pixel, belt, new Color(86, 58, 42));
        _spriteBatch.Draw(_pixel, head, headColor);
        _spriteBatch.Draw(_pixel, eye, new Color(30, 25, 27));

        if (player.Combat.IsBlocking)
        {
            int shieldX = player.Facing == FacingDirection.Left
                ? (int)topLeft.X - 10
                : (int)(topLeft.X + player.Size.X) + 2;
            var shield = new Rectangle(shieldX, centerY - 17, 8, 34);
            Color shieldFill = player.Combat.IsBlockFeedbackActive
                ? new Color(178, 218, 235)
                : new Color(92, 129, 155);
            Color shieldEdge = player.Combat.IsBlockFeedbackActive
                ? new Color(244, 249, 238)
                : new Color(190, 204, 212);
            _spriteBatch.Draw(_pixel, shield, shieldFill);
            DrawRectangleOutline(shield, 2, shieldEdge);

            if (player.Combat.IsBlockFeedbackActive)
            {
                int impactX = player.Facing == FacingDirection.Left
                    ? shield.Left - 8
                    : shield.Right + 3;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX, centerY - 3, 6, 6),
                    new Color(244, 237, 172));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX + 2, centerY - 11, 3, 5),
                    new Color(232, 247, 250));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(impactX + 2, centerY + 7, 3, 5),
                    new Color(232, 247, 250));
            }
        }

        if (player.IsSlowed)
        {
            Rectangle webOutline = player.Bounds;
            webOutline.Inflate(7, 5);
            DrawRectangleOutline(
                webOutline,
                2,
                new Color(205, 220, 211, 155));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(webOutline.X, webOutline.Center.Y, webOutline.Width, 2),
                new Color(180, 204, 194, 120));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(webOutline.Center.X, webOutline.Y, 2, webOutline.Height),
                new Color(180, 204, 194, 120));
        }
    }

    private static Rectangle CreatePlayerEye(
        Rectangle head,
        FacingDirection facingDirection)
    {
        return facingDirection == FacingDirection.Left
            ? new Rectangle(head.X + 3, head.Y + 8, 4, 4)
            : new Rectangle(head.Right - 7, head.Y + 8, 4, 4);
    }

    private void DrawEnemy(Enemy enemy, GameTime gameTime)
    {
        switch (enemy)
        {
            case Goblin goblin:
                _goblinFamilySpriteRenderer.Draw(goblin, gameTime);
                break;
            case DireWolf direWolf:
                _corruptedBeastSpriteRenderer.Draw(direWolf, gameTime);
                break;
            case GiantSpider giantSpider:
                _corruptedBeastSpriteRenderer.Draw(giantSpider, gameTime);
                break;
            case GoblinHunter goblinHunter:
                _goblinFamilySpriteRenderer.Draw(goblinHunter, gameTime);
                break;
            case ThornCrawler thornCrawler:
                _wildForestAmbushSpriteRenderer.Draw(thornCrawler, gameTime);
                break;
            case CorruptedTreant corruptedTreant:
                _wildForestHeavySpriteRenderer.Draw(corruptedTreant, gameTime);
                break;
            case BloodBat bloodBat:
                _wildForestAmbushSpriteRenderer.Draw(bloodBat, gameTime);
                break;
            case GoblinChief goblinChief:
                _wildForestHeavySpriteRenderer.Draw(goblinChief, gameTime);
                break;
            case MotherSpider motherSpider:
                _wildForestApexSpriteRenderer.Draw(motherSpider, gameTime);
                break;
            case Spiderling spiderling:
                _wildForestApexSpriteRenderer.Draw(spiderling, gameTime);
                break;
            case CatacombEnemy catacomb:
                if (catacomb.Type is EnemyType.Skeleton or
                    EnemyType.SkeletonArcher)
                {
                    _catacombSkeletonSpriteRenderer.Draw(
                        catacomb,
                        gameTime);
                }
                else if (catacomb.Type is EnemyType.RottenCorpse or
                    EnemyType.Wraith)
                {
                    _catacombRotSpriteRenderer.Draw(catacomb, gameTime);
                }
                else if (catacomb.Type is EnemyType.UndeadGuard or
                    EnemyType.GraveBat)
                {
                    _catacombGuardBatSpriteRenderer.Draw(catacomb, gameTime);
                }
                else if (catacomb.Type is EnemyType.CursedKnight or
                    EnemyType.SoulCollector)
                {
                    _catacombEliteSpriteRenderer.Draw(catacomb, gameTime);
                }
                else if (catacomb.Type is EnemyType.DeathKnight or
                    EnemyType.FallenKnight)
                {
                    _catacombKnightSpriteRenderer.Draw(catacomb, gameTime);
                }
                else
                {
                    DrawCatacombEnemy(catacomb);
                }
                break;
            case FallenCityEnemy city:
                if (city.Type is EnemyType.FallenMilitiaman or
                    EnemyType.RuinedCrossbowman)
                {
                    _fallenCityMilitiaSpriteRenderer.Draw(city, gameTime);
                }
                else if (city.Type is EnemyType.RabidHound or
                    EnemyType.PlagueCarrier)
                {
                    _fallenCityHoundPlagueSpriteRenderer.Draw(city, gameTime);
                }
                else if (city.Type is EnemyType.BellWatcher or
                    EnemyType.StreetExecutioner)
                {
                    _fallenCityWatcherExecutionerSpriteRenderer.Draw(
                        city,
                        gameTime);
                }
                else if (city.Type is EnemyType.RoyalGuard or
                    EnemyType.AshKnight)
                {
                    _fallenCityRoyalAshSpriteRenderer.Draw(city, gameTime);
                }
                else if (city.Type is EnemyType.WarBishop or
                    EnemyType.BrokenKing)
                {
                    _fallenCityBishopKingSpriteRenderer.Draw(city, gameTime);
                }
                else
                {
                    DrawFallenCityEnemy(city);
                }
                break;
            case DemonLandsEnemy demon:
                if (demon.Type is EnemyType.ImpRaider or EnemyType.HellArcher)
                    _demonFrontierSpriteRenderer.Draw(demon, gameTime);
                else if (demon.Type is EnemyType.SpineHound or
                    EnemyType.RiftStalker)
                {
                    _demonPredatorSpriteRenderer.Draw(demon, gameTime);
                }
                else if (demon.Type is EnemyType.BoneHarvester or
                    EnemyType.Hellbrute)
                {
                    _demonHeavySpriteRenderer.Draw(demon, gameTime);
                }
                else if (demon.Type is EnemyType.CorruptionPriest or
                    EnemyType.RiftWarden)
                {
                    _demonRitualSpriteRenderer.Draw(demon, gameTime);
                }
                else if (demon.Type is EnemyType.DemonLegionnaire or
                    EnemyType.HornedGeneral)
                {
                    _demonLegionSpriteRenderer.Draw(demon, gameTime);
                }
                else
                    DrawDemonLandsEnemy(demon);
                break;
            case DemonFortressEnemy fortress:
                _demonFortressEnemySpriteRenderer.Draw(fortress, gameTime);
                break;
            case DemonCityEnemy demonCity:
                _demonCityEnemySpriteRenderer.Draw(demonCity, gameTime);
                break;
            case DemonCityIndustrialEnemy industrial:
                _demonCityIndustrialEnemySpriteRenderer.Draw(
                    industrial, gameTime);
                break;
            case DemonCityEliteEnemy eliteCity:
                _demonCityEliteEnemySpriteRenderer.Draw(eliteCity, gameTime);
                break;
            case DemonCityBossEnemy cityBoss:
                _demonCityBossEnemySpriteRenderer.Draw(cityBoss, gameTime);
                break;
            case MalacharEnemy malachar:
                _malacharEnemySpriteRenderer.Draw(malachar, gameTime);
                break;
            case DemonKingSanctumEnemy sanctumEnemy:
                _demonKingSanctumEnemySpriteRenderer.Draw(
                    sanctumEnemy, gameTime);
                break;
            case DemonKingSanctumPenitentEnemy penitentEnemy:
                _demonKingSanctumPenitentSpriteRenderer.Draw(
                    penitentEnemy, gameTime);
                break;
            case DemonKingSanctumReliquaryEnemy reliquaryEnemy:
                _demonKingSanctumReliquarySpriteRenderer.Draw(
                    reliquaryEnemy, gameTime);
                break;
            case DemonKingSanctumEliteEnemy sanctumElite:
                _demonKingSanctumEliteSpriteRenderer.Draw(sanctumElite, gameTime);
                break;
            case ThroneboundSeraphEnemy seraph:
                _throneboundSeraphSpriteRenderer.Draw(seraph, gameTime);
                break;
            case VaeltharEnemy king:
                _vaeltharSpriteRenderer.Draw(king, gameTime);
                break;
        }

        if (enemy.IsBuffed)
            DrawEnemyBuff(enemy);

        if (enemy.IsStaggered)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(enemy.Bounds.Center.X - 8, enemy.Bounds.Y - 9, 16, 4),
                new Color(225, 183, 244, 220));
        }
    }

    private void DrawFallenCityEnemy(FallenCityEnemy enemy)
    {
        Rectangle bounds = enemy.Bounds;
        Color body = enemy.IsHitFlashing ? new Color(231, 222, 204) : enemy.Type switch
        {
            EnemyType.RabidHound => new Color(91, 69, 58),
            EnemyType.PlagueCarrier => new Color(92, 96, 64),
            EnemyType.BellWatcher => new Color(115, 84, 52),
            EnemyType.AshKnight => new Color(76, 70, 69),
            EnemyType.WarBishop => new Color(92, 55, 75),
            EnemyType.BrokenKing => enemy.CombatPhase switch
            {
                3 => new Color(141, 53, 48), 2 => new Color(111, 67, 67),
                _ => new Color(86, 77, 82)
            },
            _ => new Color(83, 79, 76)
        };
        Color metal = enemy.Type is EnemyType.RoyalGuard or EnemyType.AshKnight or
            EnemyType.StreetExecutioner or EnemyType.BrokenKing
            ? new Color(119, 115, 111) : body;
        if (enemy.Type == EnemyType.RabidHound)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.X, bounds.Y + 8, bounds.Width, bounds.Height - 12), body);
            return;
        }
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 5, bounds.Y + 15, bounds.Width - 10,
                bounds.Height - 15), metal);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Center.X - 10, bounds.Y, 20, 19), body);
        if (enemy.Type == EnemyType.BellWatcher && enemy.IsSignalingAlarm)
            DrawRectangleOutline(new Rectangle(bounds.X - 8, bounds.Y - 12,
                bounds.Width + 16, bounds.Height + 20), 3, new Color(218, 115, 52));
        if (enemy.Type == EnemyType.BrokenKing)
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X - 15, bounds.Y - 8, 30, 8),
                new Color(178, 133, 56));
    }

    private void DrawDemonLandsEnemy(DemonLandsEnemy enemy)
    {
        Rectangle bounds = enemy.Bounds;
        Color body = enemy.IsHitFlashing ? new Color(244, 226, 211) : enemy.Type switch
        {
            EnemyType.ImpRaider => new Color(151, 58, 46),
            EnemyType.HellArcher => new Color(112, 48, 55),
            EnemyType.SpineHound => new Color(104, 55, 47),
            EnemyType.RiftStalker => new Color(92, 48, 117),
            EnemyType.BoneHarvester => new Color(165, 151, 126),
            EnemyType.Hellbrute => new Color(119, 46, 34),
            EnemyType.CorruptionPriest => new Color(119, 47, 105),
            EnemyType.RiftWarden => new Color(84, 50, 124),
            EnemyType.DemonLegionnaire => new Color(91, 57, 55),
            EnemyType.HornedGeneral => enemy.CombatPhase switch
            {
                3 => new Color(205, 62, 35),
                2 => new Color(157, 48, 74),
                _ => new Color(105, 57, 58)
            },
            _ => new Color(125, 54, 48)
        };
        _spriteBatch.Draw(_pixel, bounds, body);
        DrawRectangleOutline(bounds, 2, new Color(47, 22, 24));
        int hornWidth = Math.Max(5, bounds.Width / 5);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Left + 4, bounds.Top - 9, hornWidth, 11),
            new Color(191, 166, 126));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Right - hornWidth - 4, bounds.Top - 9, hornWidth, 11),
            new Color(191, 166, 126));
        if (enemy.IsLegionsEndTelegraphing)
        {
            var warning = new Rectangle(
                bounds.Center.X - 125,
                bounds.Bottom - 18,
                250,
                18);
            _spriteBatch.Draw(_pixel, warning, new Color(224, 62, 34, 85));
            DrawRectangleOutline(warning, 2, new Color(246, 105, 53, 210));
        }
    }

    private void DrawDireWolf(DireWolf wolf)
    {
        Vector2 topLeft = wolf.Position - wolf.Size / 2f;
        Color fur = wolf.IsHitFlashing
            ? new Color(232, 230, 211)
            : wolf.IsRetreating
                ? new Color(116, 96, 92)
                : new Color(83, 89, 76);
        Color darkFur = wolf.IsRetreating
            ? new Color(68, 56, 58)
            : new Color(48, 55, 47);
        Rectangle body = new(
            (int)topLeft.X + 9,
            (int)topLeft.Y + 9,
            (int)wolf.Size.X - 19,
            17);
        int muzzleExtension = wolf.Attack.IsActive ? 8 : 3;
        bool facesLeft = wolf.Facing == EnemyFacingDirection.Left;
        Rectangle head = facesLeft
            ? new Rectangle(body.X - 13, body.Y - 4, 17, 17)
            : new Rectangle(body.Right - 4, body.Y - 4, 17, 17);
        Rectangle muzzle = facesLeft
            ? new Rectangle(head.X - 5 - muzzleExtension, head.Y + 7, 7 + muzzleExtension, 7)
            : new Rectangle(head.Right - 2, head.Y + 7, 7 + muzzleExtension, 7);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.X + 2, body.Bottom - 2, body.Width - 3, 7),
            new Color(12, 17, 14, 145));
        DrawEliteOutline(wolf, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(body.X + 4, body.Bottom - 2, 6, 10), darkFur);
        _spriteBatch.Draw(_pixel, new Rectangle(body.Right - 11, body.Bottom - 2, 6, 10), darkFur);
        Rectangle tail = facesLeft
            ? new Rectangle(body.Right - 4, body.Y + 2, 14, 5)
            : new Rectangle(body.X - 10, body.Y + 2, 14, 5);
        _spriteBatch.Draw(_pixel, tail, darkFur);
        _spriteBatch.Draw(_pixel, body, fur);
        _spriteBatch.Draw(_pixel, head, fur);
        _spriteBatch.Draw(_pixel, new Rectangle(head.X + 2, head.Y - 7, 6, 9), darkFur);
        _spriteBatch.Draw(_pixel, new Rectangle(head.Right - 7, head.Y - 6, 6, 8), darkFur);
        _spriteBatch.Draw(_pixel, muzzle, darkFur);
        Rectangle eye = facesLeft
            ? new Rectangle(head.X + 1, head.Y + 4, 4, 4)
            : new Rectangle(head.Right - 5, head.Y + 4, 4, 4);
        _spriteBatch.Draw(_pixel, eye, new Color(229, 66, 51));
    }

    private void DrawGiantSpider(GiantSpider spider)
    {
        Vector2 topLeft = spider.Position - spider.Size / 2f;
        Color bodyColor = spider.IsHitFlashing
            ? new Color(235, 225, 211)
            : new Color(66, 44, 70);
        Color legColor = spider.IsHitFlashing
            ? new Color(210, 207, 196)
            : new Color(41, 32, 44);
        Rectangle abdomen = new(
            (int)topLeft.X + 7,
            (int)topLeft.Y + 8,
            25,
            22);
        bool facesLeft = spider.Facing == EnemyFacingDirection.Left;
        Rectangle head = facesLeft
            ? new Rectangle(abdomen.X - 14, abdomen.Y + 5, 17, 15)
            : new Rectangle(abdomen.Right - 3, abdomen.Y + 5, 17, 15);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(abdomen.X - 2, abdomen.Bottom - 2, 41, 7),
            new Color(12, 15, 14, 145));
        DrawEliteOutline(spider, new Color(237, 190, 59));

        for (int index = 0; index < 4; index++)
        {
            int legY = abdomen.Y + index * 5;
            int reach = index == 0 || index == 3 ? 12 : 9;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(abdomen.X - reach, legY, reach + 3, 3),
                legColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(head.Right - 2, legY, reach + 2, 3),
                legColor);
        }

        _spriteBatch.Draw(_pixel, abdomen, bodyColor);
        _spriteBatch.Draw(_pixel, head, new Color(51, 37, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(abdomen.X + 8, abdomen.Y + 7, 9, 7), new Color(104, 49, 93));
        _spriteBatch.Draw(_pixel, new Rectangle(head.X + 4, head.Y + 4, 3, 3), new Color(229, 58, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(head.Right - 6, head.Y + 4, 3, 3), new Color(229, 58, 54));
        Rectangle fang = facesLeft
            ? new Rectangle(head.X - 3, head.Bottom - 5, 5, 7)
            : new Rectangle(head.Right - 2, head.Bottom - 5, 5, 7);
        _spriteBatch.Draw(_pixel, fang, new Color(204, 194, 174));
    }

    private void DrawThornCrawler(ThornCrawler crawler)
    {
        int x = (int)crawler.Position.X;
        int y = (int)crawler.Position.Y;

        if (crawler.IsUnderground)
        {
            Color soil = crawler.State == ThornCrawlerState.Warning
                ? new Color(177, 71, 115, 205)
                : new Color(92, 68, 43, 150);
            int width = crawler.State == ThornCrawlerState.Warning ? 54 : 34;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x - width / 2, y - 5, width, 10),
                soil);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x - 3, y - 12, 6, 18),
                new Color(92, 113, 57, 175));

            if (crawler.State == ThornCrawlerState.Warning)
            {
                DrawRectangleOutline(
                    new Rectangle(x - 31, y - 18, 62, 36),
                    3,
                    new Color(222, 91, 143, 190));
            }

            return;
        }

        Rectangle bounds = crawler.Bounds;
        Color body = crawler.IsHitFlashing
            ? new Color(234, 228, 200)
            : new Color(73, 94, 49);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 5, bounds.Bottom - 5, bounds.Width - 10, 8),
            new Color(12, 16, 12, 145));
        DrawEliteOutline(crawler, new Color(237, 190, 59));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 10, bounds.Y + 12, bounds.Width - 20, bounds.Height - 12),
            body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Center.X - 4, bounds.Y, 8, 18), new Color(111, 61, 96));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 2, bounds.Y + 13, 12, 5), new Color(112, 122, 62));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 14, bounds.Y + 13, 12, 5), new Color(112, 122, 62));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 5, bounds.Y + 5, 6, 12), new Color(143, 68, 111));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 11, bounds.Y + 5, 6, 12), new Color(143, 68, 111));
    }

    private void DrawCorruptedTreant(CorruptedTreant treant)
    {
        Rectangle bounds = treant.Bounds;
        Color bark = treant.IsHitFlashing
            ? new Color(229, 220, 189)
            : new Color(84, 64, 42);
        Color corruption = new(103, 48, 93);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 7, bounds.Bottom - 7, bounds.Width - 14, 11),
            new Color(11, 15, 11, 155));
        DrawEliteOutline(treant, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 16, bounds.Y + 24, bounds.Width - 32, bounds.Height - 24), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 10, bounds.Y + 7, bounds.Width - 20, 30), new Color(64, 78, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 2, bounds.Y + 30, 18, 9), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 20, bounds.Y + 30, 18, 9), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 13, bounds.Bottom - 12, 12, 18), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 25, bounds.Bottom - 12, 12, 18), bark);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Center.X - 5, bounds.Y + 33, 10, 24), corruption);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 20, bounds.Y + 17, 5, 5), new Color(223, 62, 65));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 25, bounds.Y + 17, 5, 5), new Color(223, 62, 65));
    }

    private void DrawBloodBat(BloodBat bat)
    {
        int centerX = (int)bat.Position.X;
        int centerY = (int)(bat.Position.Y - bat.FlightVisualOffset);
        int wingReach = bat.State == BloodBatState.Diving ? 13 : 20;
        bool facesLeft = bat.Facing == EnemyFacingDirection.Left;
        int leftWingReach = wingReach + (facesLeft ? 3 : -3);
        int rightWingReach = wingReach + (facesLeft ? -3 : 3);
        Color wing = bat.IsHitFlashing
            ? new Color(235, 220, 211)
            : new Color(91, 35, 47);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(centerX - 15, (int)bat.Position.Y + 7, 30, 6),
            new Color(11, 14, 12, 120));
        DrawEliteOutline(bat, new Color(237, 190, 59));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - leftWingReach - 8, centerY - 4, leftWingReach, 10), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 8, centerY - 4, rightWingReach, 10), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 9, centerY - 7, 18, 18), new Color(62, 37, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 6, centerY - 13, 5, 8), wing);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 2, centerY - 13, 5, 8), wing);
        Rectangle eye = facesLeft
            ? new Rectangle(centerX - 6, centerY - 2, 3, 3)
            : new Rectangle(centerX + 3, centerY - 2, 3, 3);
        Rectangle snout = facesLeft
            ? new Rectangle(centerX - 13, centerY + 2, 6, 4)
            : new Rectangle(centerX + 7, centerY + 2, 6, 4);
        _spriteBatch.Draw(_pixel, snout, new Color(113, 47, 50));
        _spriteBatch.Draw(_pixel, eye, new Color(236, 53, 53));
    }

    private void DrawGoblinChief(GoblinChief chief)
    {
        Rectangle bounds = chief.Bounds;
        Color skin = chief.IsHitFlashing
            ? new Color(241, 237, 208)
            : new Color(83, 137, 61);
        DrawEliteOutline(chief, new Color(244, 187, 46));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Bottom - 5, bounds.Width - 14, 9), new Color(12, 15, 13, 150));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 9, bounds.Y + 23, bounds.Width - 18, bounds.Height - 23), new Color(116, 52, 42));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 5, bounds.Y + 4, bounds.Width - 10, 28), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 2, bounds.Y + 12, 10, 12), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 8, bounds.Y + 12, 10, 12), skin);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 13, bounds.Y + 14, 5, 5), new Color(242, 63, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 18, bounds.Y + 14, 5, 5), new Color(242, 63, 43));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Y - 4, bounds.Width - 14, 8), new Color(224, 165, 43));
        int clubLength = chief.Attack.IsActive ? 48 : 33;
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right + 2, bounds.Y + 22, 7, clubLength), new Color(94, 58, 34));

        if (chief.IsWarCryActive)
        {
            Rectangle cry = bounds;
            cry.Inflate(13, 12);
            DrawRectangleOutline(cry, 4, new Color(239, 168, 54, 170));
        }
    }

    private void DrawMotherSpider(MotherSpider mother)
    {
        Rectangle bounds = mother.Bounds;
        Color body = mother.IsHitFlashing
            ? new Color(238, 228, 214)
            : new Color(77, 39, 76);
        DrawEliteOutline(mother, new Color(237, 190, 59));

        for (int index = 0; index < 4; index++)
        {
            int y = bounds.Y + 13 + index * 9;
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 13, y, 24, 4), new Color(42, 27, 43));
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 11, y, 24, 4), new Color(42, 27, 43));
        }

        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 7, bounds.Y + 8, 42, bounds.Height - 15), body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 29, bounds.Y + 17, 23, 24), new Color(53, 30, 55));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 20, bounds.Y + 20, 16, 13), new Color(133, 51, 103));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 23, bounds.Y + 23, 5, 5), new Color(239, 57, 54));
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 14, bounds.Y + 23, 5, 5), new Color(239, 57, 54));
    }

    private void DrawSpiderling(Spiderling spiderling)
    {
        Rectangle bounds = spiderling.Bounds;
        Color body = spiderling.IsHitFlashing
            ? new Color(230, 226, 211)
            : new Color(58, 39, 61);

        for (int index = 0; index < 3; index++)
        {
            int y = bounds.Y + 3 + index * 5;
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 4, y, 8, 2), body);
            _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 4, y, 8, 2), body);
        }

        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 3, bounds.Y + 2, bounds.Width - 6, bounds.Height - 4), body);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 8, bounds.Y + 5, 3, 3), new Color(226, 66, 57));
    }

    private void DrawEnemyBuff(Enemy enemy)
    {
        Rectangle bounds = GetEnemyVisualBounds(enemy);
        Color color = new(186, 65, 42, 145);
        Rectangle aura = bounds;
        aura.Inflate(4, 3);
        DrawRectangleOutline(aura, 2, new Color(151, 48, 38, 90));
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X - 3, bounds.Y + 7, 3, 10), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Center.X - 1, bounds.Y - 7, 3, 12), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Right, bounds.Y + 7, 3, 10), color);
    }

    private void DrawEnemyHealthBar(
        Enemy enemy,
        Rectangle cameraBounds,
        bool leaveRoomForShowcaseLabel)
    {
        if (enemy == null ||
            !enemy.IsAlive ||
            !enemy.IsHealthBarVisible ||
            enemy.Type == EnemyType.FallenKnight ||
            enemy.Type == EnemyType.BrokenKing ||
            enemy.Type == EnemyType.HornedGeneral ||
            enemy.Type == EnemyType.BallistaGunner ||
            enemy.Type == EnemyType.BlackGuardExecutioner ||
            enemy.Type == EnemyType.CathedralButcher ||
            enemy.Type == EnemyType.AbyssalMagistrate ||
            enemy.Type == EnemyType.ThroneboundSeraph ||
            enemy.Type == EnemyType.Vaelthar ||
            enemy.Type == EnemyType.Malachar ||
            enemy.MaxHealth <= 0)
        {
            return;
        }

        Rectangle visualBounds = GetEnemyVisualBounds(enemy);
        bool large = enemy.IsElite || visualBounds.Width >= 88;
        int minimumWidth = large ? 80 : 45;
        int maximumWidth = large ? 110 : 75;
        int width = Math.Clamp(
            (int)MathF.Round(visualBounds.Width * .92f),
            minimumWidth,
            maximumWidth);
        int height = enemy.IsElite ? 10 : 8;
        int headClearance = enemy.IsElite ? 18 : 14;
        if (leaveRoomForShowcaseLabel && enemy.Type != EnemyType.Spiderling)
            headClearance += 18;

        Rectangle frame = new(
            visualBounds.Center.X - width / 2,
            visualBounds.Top - headClearance,
            width,
            height);
        Rectangle occupied = new(
            frame.X - 1,
            frame.Y - 1,
            frame.Width + 3,
            frame.Height + 4);
        if (occupied.Left < cameraBounds.Left ||
            occupied.Right > cameraBounds.Right ||
            occupied.Top < cameraBounds.Top ||
            occupied.Bottom > cameraBounds.Bottom)
        {
            return;
        }

        float opacity = Math.Clamp(enemy.HealthBarOpacity, 0f, 1f);
        int shadowAlpha = (int)MathF.Round(145f * opacity);
        int frameAlpha = (int)MathF.Round(235f * opacity);
        int fillAlpha = (int)MathF.Round(245f * opacity);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(frame.X + 1, frame.Y + 2, frame.Width, frame.Height),
            new Color(3, 4, 6, shadowAlpha));
        _spriteBatch.Draw(
            _pixel,
            frame,
            new Color(18, 15, 18, frameAlpha));
        DrawRectangleOutline(
            frame,
            1,
            new Color(
                enemy.IsElite ? 126 : 91,
                enemy.IsElite ? 96 : 76,
                enemy.IsElite ? 62 : 65,
                frameAlpha));

        Rectangle track = new(
            frame.X + 2,
            frame.Y + 2,
            Math.Max(1, frame.Width - 4),
            Math.Max(1, frame.Height - 4));
        _spriteBatch.Draw(
            _pixel,
            track,
            new Color(51, 25, 28, frameAlpha));

        float healthRatio = Math.Clamp(
            enemy.CurrentHealth / (float)enemy.MaxHealth,
            0f,
            1f);
        int fillWidth = Math.Clamp(
            (int)MathF.Round(track.Width * healthRatio),
            0,
            track.Width);
        if (fillWidth <= 0)
            return;

        Color healthColor = enemy.IsElite
            ? new Color(151, 67, 48, fillAlpha)
            : new Color(126, 52, 49, fillAlpha);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(track.X, track.Y, fillWidth, track.Height),
            healthColor);

        if (track.Height >= 4)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(track.X, track.Y, fillWidth, 1),
                new Color(196, 103, 76, (int)MathF.Round(165f * opacity)));
        }
    }

    private void DrawEliteOutline(Enemy enemy, Color color)
    {
        if (!enemy.IsElite)
            return;

        Rectangle outline = enemy.Bounds;
        outline.Inflate(4, 5);
        DrawRectangleOutline(outline, 3, color);
    }

    private void DrawProjectile(Projectile projectile)
    {
        if (projectile.Type == ProjectileType.PlagueFlask)
        {
            Vector2 flaskDirection = projectile.Velocity;
            if (flaskDirection.LengthSquared() > .01f)
                flaskDirection.Normalize();
            else
                flaskDirection = Vector2.UnitX;
            Vector2 normal = new(-flaskDirection.Y, flaskDirection.X);
            DrawWorldLine(
                projectile.Position - flaskDirection * 7f,
                projectile.Position + flaskDirection * 5f,
                7f,
                new Color(42, 43, 35));
            DrawWorldLine(
                projectile.Position - flaskDirection * 5f,
                projectile.Position + flaskDirection * 4f,
                4f,
                new Color(112, 117, 83));
            DrawWorldLine(
                projectile.Position - normal * 5f,
                projectile.Position + normal * 5f,
                3f,
                new Color(75, 54, 38));

            if (projectile.HasTelegraphPosition)
            {
                Vector2 landing = projectile.TelegraphPosition;
                var marker = new Rectangle(
                    (int)MathF.Round(landing.X - 25f),
                    (int)MathF.Round(landing.Y - 5f),
                    50,
                    10);
                DrawRectangleOutline(
                    marker,
                    2,
                    new Color(133, 128, 82, 145));
                DrawWorldLine(
                    new Vector2(marker.Left + 7, marker.Center.Y),
                    new Vector2(marker.Right - 7, marker.Center.Y),
                    1f,
                    new Color(158, 147, 93, 125));
            }
            return;
        }

        if (projectile.Type == ProjectileType.Vowshard)
        {
            Vector2 shardDirection = projectile.Velocity;
            if (shardDirection.LengthSquared() > .01f) shardDirection.Normalize();
            else shardDirection = Vector2.UnitX;
            Vector2 shardNormal = new(-shardDirection.Y, shardDirection.X);
            DrawWorldLine(projectile.PreviousPosition, projectile.Position,
                2f, new Color(101, 72, 119, 105));
            DrawWorldLine(projectile.Position - shardDirection * 8f,
                projectile.Position + shardDirection * 9f, 6f,
                new Color(91, 86, 82));
            DrawWorldLine(projectile.Position - shardNormal * 5f,
                projectile.Position + shardDirection * 4f, 2f,
                new Color(184, 137, 198));
            return;
        }

        if (projectile.Type == ProjectileType.AbyssScript)
        {
            Vector2 scriptDirection = projectile.Velocity;
            if (scriptDirection.LengthSquared() > .01f)
                scriptDirection.Normalize();
            else
                scriptDirection = Vector2.UnitX;
            Vector2 scriptNormal = new(-scriptDirection.Y, scriptDirection.X);
            DrawWorldLine(projectile.PreviousPosition, projectile.Position,
                2f, new Color(103, 70, 120, 105));
            DrawWorldLine(projectile.Position - scriptDirection * 10f,
                projectile.Position + scriptDirection * 10f, 3f,
                new Color(179, 130, 194, 225));
            DrawWorldLine(projectile.Position - scriptNormal * 8f,
                projectile.Position + scriptNormal * 8f, 2f,
                new Color(119, 88, 134, 215));
            DrawWorldLine(projectile.Position - scriptDirection * 5f -
                    scriptNormal * 5f,
                projectile.Position + scriptDirection * 5f +
                    scriptNormal * 5f, 2f,
                new Color(201, 158, 208, 205));
            return;
        }

        if (projectile.Type == ProjectileType.SoulBolt)
        {
            Vector2 center = projectile.Position;
            Vector2 soulDirection = projectile.Velocity;
            if (soulDirection.LengthSquared() > .01f)
                soulDirection.Normalize();
            else
                soulDirection = Vector2.UnitX;
            Vector2 normal = new(-soulDirection.Y, soulDirection.X);
            DrawWorldLine(center - soulDirection * 17f,
                center + soulDirection * 8f,
                5f, new Color(61, 66, 91, 155));
            DrawWorldLine(center - soulDirection * 8f,
                center + soulDirection * 7f,
                3f, new Color(129, 121, 165, 225));
            DrawWorldLine(center - soulDirection * 4f - normal * 5f,
                center + soulDirection * 5f + normal * 4f,
                2f, new Color(174, 164, 193, 205));
            return;
        }

        if (projectile.Type == ProjectileType.SigilLance)
        {
            Vector2 center = projectile.Position;
            Vector2 lanceDirection = projectile.Velocity;
            if (lanceDirection.LengthSquared() > .01f)
                lanceDirection.Normalize();
            else
                lanceDirection = Vector2.UnitX;
            Vector2 normal = new(-lanceDirection.Y, lanceDirection.X);
            DrawWorldLine(
                projectile.PreviousPosition,
                center,
                3f,
                new Color(104, 69, 143, 115));
            DrawWorldLine(center - lanceDirection * 16f,
                center + lanceDirection * 14f,
                5f, new Color(80, 59, 105, 205));
            DrawWorldLine(center - lanceDirection * 11f,
                center + lanceDirection * 16f,
                2f, new Color(205, 174, 226, 235));
            DrawWorldLine(center - normal * 7f,
                center + lanceDirection * 5f,
                2f, new Color(145, 104, 175, 220));
            DrawWorldLine(center + normal * 7f,
                center + lanceDirection * 5f,
                2f, new Color(145, 104, 175, 220));
            return;
        }

        if (projectile.Type == ProjectileType.JudgmentBolt)
        {
            Vector2 judgmentDirection = projectile.Velocity;
            if (judgmentDirection.LengthSquared() > .01f)
                judgmentDirection.Normalize();
            else
                judgmentDirection = Vector2.UnitX;
            Vector2 normal = new(-judgmentDirection.Y, judgmentDirection.X);
            float length = projectile.EmpoweredVisual ? 25f : 19f;
            DrawWorldLine(projectile.PreviousPosition, projectile.Position,
                projectile.EmpoweredVisual ? 5f : 3f,
                new Color(104, 67, 137, 120));
            DrawWorldLine(projectile.Position - judgmentDirection * length,
                projectile.Position + judgmentDirection * 10f,
                projectile.EmpoweredVisual ? 7f : 5f,
                new Color(213, 180, 112, 235));
            DrawWorldLine(projectile.Position - normal * 8f,
                projectile.Position + normal * 8f,
                3f, new Color(146, 99, 177, 225));
            return;
        }

        if (projectile.Type == ProjectileType.DominionArc)
        {
            Vector2 arcDirection = projectile.Velocity;
            if (arcDirection.LengthSquared() > .01f)
                arcDirection.Normalize();
            else
                arcDirection = Vector2.UnitX;
            Vector2 normal = new(-arcDirection.Y, arcDirection.X);
            float length = projectile.EmpoweredVisual ? 37f : 29f;
            DrawWorldLine(projectile.PreviousPosition, projectile.Position,
                projectile.EmpoweredVisual ? 7f : 5f,
                new Color(89, 48, 116, 125));
            DrawWorldLine(projectile.Position - arcDirection * length,
                projectile.Position + arcDirection * 13f,
                projectile.EmpoweredVisual ? 10f : 8f,
                new Color(111, 67, 139, 230));
            DrawWorldLine(projectile.Position - arcDirection * (length - 4f),
                projectile.Position + arcDirection * 16f,
                3f, new Color(230, 191, 111, 235));
            DrawWorldLine(projectile.Position - normal * 9f,
                projectile.Position + normal * 9f,
                2f, new Color(175, 122, 191, 210));
            return;
        }

        if (projectile.Type == ProjectileType.WebShot)
        {
            Rectangle bounds = projectile.Bounds;
            Vector2 center = bounds.Center.ToVector2();
            int coreSize = projectile.EmpoweredVisual ? 12 : 8;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - coreSize / 2,
                    bounds.Center.Y - coreSize / 2, coreSize, coreSize),
                projectile.EmpoweredVisual
                    ? new Color(174, 170, 181, 235)
                    : new Color(205, 216, 207, 225));
            DrawWorldLine(center + new Vector2(-10f, -3f),
                center + new Vector2(10f, 4f), 2f,
                new Color(235, 240, 233, 205));
            DrawWorldLine(center + new Vector2(-8f, 7f),
                center + new Vector2(7f, -9f), 2f,
                new Color(225, 233, 226, 190));
            DrawWorldLine(center + new Vector2(-11f, 2f),
                center + new Vector2(8f, -1f), 1f,
                new Color(178, 195, 184, 190));
            if (projectile.EmpoweredVisual)
            {
                DrawWorldLine(center + new Vector2(-13f, -7f),
                    center + new Vector2(13f, 7f), 2f,
                    new Color(155, 135, 159, 205));
                DrawWorldLine(center + new Vector2(-13f, 7f),
                    center + new Vector2(13f, -7f), 2f,
                    new Color(205, 197, 203, 210));
            }
            return;
        }

        if (projectile.Type == ProjectileType.ArcaneBolt)
        {
            if (projectile.TechniqueEffect ==
                WeaponTechniqueEffect.DuelistHeavenSplitter)
            {
                DrawDuelistHeavenSplitterProjectile(projectile);
                return;
            }
            if (projectile.IsAncientStaffProjectile)
            {
                DrawAncientStaffProjectile(projectile);
                return;
            }
            if (projectile.TechniqueEffect ==
                WeaponTechniqueEffect.SpellbladeRunicSpear)
            {
                DrawRunicSpearProjectile(projectile);
                return;
            }

            Rectangle glow = projectile.Bounds;
            glow.Inflate(7, 7);
            _spriteBatch.Draw(_pixel, glow, new Color(126, 72, 177, 50));
            Vector2 center = projectile.Position;
            float half = MathF.Max(7f, projectile.Size.X * .45f);
            Color edge = new(135, 105, 188, 225);
            Color core = new(218, 208, 235, 235);
            DrawWorldLine(center + new Vector2(0f, -half),
                center + new Vector2(half, 0f), 2f, edge);
            DrawWorldLine(center + new Vector2(half, 0f),
                center + new Vector2(0f, half), 2f, edge);
            DrawWorldLine(center + new Vector2(0f, half),
                center + new Vector2(-half, 0f), 2f, edge);
            DrawWorldLine(center + new Vector2(-half, 0f),
                center + new Vector2(0f, -half), 2f, edge);
            DrawWorldLine(center - new Vector2(half * .55f, 0f),
                center + new Vector2(half * .55f, 0f), 1f, core);
            DrawWorldLine(center - new Vector2(0f, half * .55f),
                center + new Vector2(0f, half * .55f), 1f, core);
            return;
        }

        Vector2 direction = projectile.Velocity;
        if (direction.LengthSquared() > .01f)
            direction.Normalize();
        else
            direction = Vector2.UnitX;
        float rotation = MathF.Atan2(direction.Y, direction.X);
        float shaftLength = MathF.Max(projectile.Size.X, projectile.Size.Y);
        Vector2 shaftStart = projectile.Position - direction * (shaftLength / 2f);
        if (projectile.IsEmberArrow)
        {
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                4f,
                new Color(154, 48, 28, 75));
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                2f,
                new Color(235, 104, 42, 165));
        }
        else if (!projectile.IsPlayerOwned && projectile.EmpoweredVisual &&
            projectile.Type != ProjectileType.CrossbowBolt)
        {
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                2f,
                new Color(78, 133, 153, 115));
        }
        if (projectile.IsRangerProjectile)
        {
            bool phantomHunt = projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerPhantomHunt;
            bool executionArrow = phantomHunt && projectile.Size.X >= 28f;
            Color trail = phantomHunt
                    ? executionArrow
                        ? new Color(119, 239, 255, 225)
                        : new Color(66, 194, 232, 165)
                    : projectile.TechniqueEffect ==
                        WeaponTechniqueEffect.RangerHeavensFury
                        ? new Color(239, 215, 122, 180)
                    : projectile.RicochetCount > 0
                        ? new Color(132, 214, 196, 145)
                        : new Color(211, 191, 135, 105);
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                executionArrow || projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.RangerHeavensFury ? 4f : 2f,
                trail);

            if (projectile.RicochetVisualTimeRemaining > 0f)
            {
                float spark = 7f + projectile.RicochetCount * 2f;
                DrawWorldLine(
                    projectile.LastRicochetPosition + new Vector2(-spark, -spark),
                    projectile.LastRicochetPosition + new Vector2(spark, spark),
                    2f,
                    new Color(245, 218, 126, 220));
                DrawWorldLine(
                    projectile.LastRicochetPosition + new Vector2(-spark, spark),
                    projectile.LastRicochetPosition + new Vector2(spark, -spark),
                    2f,
                    new Color(170, 231, 215, 215));
            }
        }
        _spriteBatch.Draw(
            _pixel,
            shaftStart,
            sourceRectangle: null,
            projectile.IsEmberArrow
                ? new Color(78, 44, 31)
                : projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.RangerPhantomHunt
                    ? new Color(73, 166, 190)
                : new Color(105, 74, 42),
            rotation,
            new Vector2(0f, 0.5f),
            new Vector2(shaftLength, 3f),
            SpriteEffects.None,
            layerDepth: 0f);
        _spriteBatch.Draw(
            _pixel,
            projectile.Position + direction * (shaftLength / 2f),
            sourceRectangle: null,
            projectile.IsEmberArrow
                ? new Color(239, 116, 49)
                : projectile.TechniqueEffect ==
                    WeaponTechniqueEffect.RangerPhantomHunt
                    ? new Color(153, 241, 255)
                : new Color(67, 73, 68),
            rotation,
            new Vector2(0.5f, 0.5f),
            new Vector2(6f, 7f),
            SpriteEffects.None,
            layerDepth: 0f);
        _spriteBatch.Draw(
            _pixel,
            shaftStart + direction * 2f,
            sourceRectangle: null,
            new Color(91, 63, 47),
            rotation + 0.55f,
            new Vector2(0f, 0.5f),
            new Vector2(6f, 2f),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawDuelistHeavenSplitterProjectile(Projectile projectile)
    {
        Vector2 direction = projectile.Velocity;
        if (direction.LengthSquared() <= .01f)
            direction = Vector2.UnitX;
        else
            direction.Normalize();
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 center = projectile.Position;
        float halfLength = MathF.Max(27f, projectile.Size.X * .46f);
        float halfSpread = MathF.Max(13f, projectile.Size.Y * .25f);
        Vector2 cyanStart = center - direction * halfLength - normal * halfSpread;
        Vector2 cyanEnd = center + direction * halfLength + normal * halfSpread;
        Vector2 violetStart = center - direction * halfLength + normal * halfSpread;
        Vector2 violetEnd = center + direction * halfLength - normal * halfSpread;

        DrawWorldLine(cyanStart, cyanEnd, 6f,
            new Color(80, 181, 207, 70));
        DrawWorldLine(violetStart, violetEnd, 6f,
            new Color(139, 96, 184, 66));
        DrawWorldLine(cyanStart + direction * 4f,
            cyanEnd - direction * 2f, 3f,
            new Color(145, 226, 239, 225));
        DrawWorldLine(violetStart + direction * 4f,
            violetEnd - direction * 2f, 3f,
            new Color(192, 166, 225, 220));
        DrawWorldLine(cyanStart + direction * 10f,
            cyanEnd - direction * 4f, 1f,
            new Color(232, 253, 255, 245));
        DrawWorldLine(violetStart + direction * 10f,
            violetEnd - direction * 4f, 1f,
            new Color(240, 230, 252, 240));

        Vector2 wake = projectile.PreviousPosition - direction * 9f;
        DrawWorldLine(wake - normal * 7f,
            center - direction * 18f - normal * 4f, 1f,
            new Color(121, 211, 230, 125));
        DrawWorldLine(wake + normal * 7f,
            center - direction * 18f + normal * 4f, 1f,
            new Color(183, 151, 218, 118));
        for (int fragment = -1; fragment <= 1; fragment++)
        {
            Vector2 point = center - direction * (22f + (fragment + 1) * 8f) +
                normal * fragment * 7f;
            Color color = fragment == 0
                ? new Color(225, 248, 252, 180)
                : new Color(205, 186, 232, 145);
            DrawWorldLine(point - direction * 2f, point + direction * 2f,
                1f, color);
        }
    }

    private void DrawRunicSpearProjectile(Projectile projectile)
    {
        Vector2 direction = projectile.Velocity;
        if (direction.LengthSquared() <= .01f)
            direction = Vector2.UnitX;
        else
            direction.Normalize();
        Vector2 perpendicular = new(-direction.Y, direction.X);
        float halfLength = MathF.Max(26f, projectile.Size.X / 2f);
        Vector2 tail = projectile.Position - direction * halfLength;
        Vector2 neck = projectile.Position + direction * (halfLength - 10f);
        Vector2 tip = projectile.Position + direction * (halfLength + 8f);
        Color glow = new(118, 92, 173, 80);
        Color edge = new(145, 119, 202, 220);
        Color core = new(224, 218, 237, 235);
        DrawWorldLine(tail, neck, 8f, glow);
        DrawWorldLine(tail, neck, 3f, edge);
        DrawWorldLine(tail + direction * 7f, neck, 1f, core);
        DrawWorldLine(neck + perpendicular * 8f, tip, 3f, edge);
        DrawWorldLine(neck - perpendicular * 8f, tip, 3f, edge);
        DrawWorldLine(neck, tip, 1f, core);
    }

    private void DrawAncientStaffProjectile(Projectile projectile)
    {
        Vector2 direction = projectile.Velocity;
        if (direction.LengthSquared() < .01f)
            direction = Vector2.UnitX;
        else
            direction.Normalize();
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 center = projectile.Position;
        Color blue = new(83, 162, 238, 225);
        Color purple = new(137, 91, 211, 205);
        Color gold = new(232, 191, 82, 235);
        Color white = new(235, 246, 255, 245);
        float travelPhase = center.X * .075f + center.Y * .031f;

        if (projectile.TechniqueEffect ==
            WeaponTechniqueEffect.AncientStaffBlinkCast)
        {
            Vector2 tip = center + direction * 31f;
            DrawWorldLine(center - direction * 35f, tip, 8f,
                new Color(73, 104, 225, 72));
            DrawWorldLine(center - direction * 27f, tip, 3f, purple);
            DrawWorldLine(center - direction * 7f, tip, 1f, white);
            for (int fold = -1; fold <= 1; fold += 2)
            {
                Vector2 wing = center + direction * 9f + normal * fold * 9f;
                DrawWorldLine(wing, tip, 2f, gold);
                DrawWorldLine(center - direction * 18f + normal * fold * 4f,
                    center - direction * 7f + normal * fold * 8f,
                    1f, blue);
            }
            return;
        }

        switch (projectile.SpreadArrowIndex)
        {
            case 0:
                DrawWorldLine(center - direction * 13f,
                    center + direction * 7f, 2f,
                    new Color(72, 145, 226, 145));
                DrawWorldLine(center - direction * 4f,
                    center + direction * 9f, 3f, blue);
                DrawWorldLine(center - normal * 3f,
                    center + normal * 3f, 1f, gold);
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)center.X + (int)(direction.X * 5f) - 1,
                        (int)center.Y + (int)(direction.Y * 5f) - 1, 3, 3),
                    white);
                break;
            case 1:
                DrawWorldLine(center - direction * 20f,
                    center - direction * 5f, 5f,
                    new Color(108, 77, 190, 80));
                DrawWorldLine(center - direction * 13f,
                    center + direction * 10f, 7f, purple);
                DrawWorldLine(center - direction * 8f,
                    center + direction * 12f, 3f, white);
                DrawBrokenWorldRune(center - direction * 2f, 11f,
                    travelPhase, gold, blue, 7, 1f, .70f);
                break;
            case 2:
                float pairLead = projectile.Velocity.Y < 0f ? 5f : -5f;
                center += direction * pairLead;
                Color pairColor = projectile.Velocity.Y < 0f ? gold : blue;
                DrawWorldLine(center - direction * 20f,
                    center - direction * 4f, 1f, pairColor);
                Vector2 pairTip = center + direction * 10f;
                DrawWorldLine(center - direction * 7f + normal * 5f,
                    pairTip, 2f, pairColor);
                DrawWorldLine(center - direction * 7f - normal * 5f,
                    pairTip, 1f, white);
                DrawWorldLine(center - direction * 8f + normal * 5f,
                    center - direction * 8f - normal * 5f,
                    1f, purple);
                break;
            case 3:
                DrawWorldLine(center - direction * 30f,
                    center + direction * 25f, 7f,
                    new Color(101, 62, 178, 90));
                DrawWorldLine(center - direction * 25f,
                    center + direction * 28f, 3f, purple);
                DrawWorldLine(center - direction * 13f,
                    center + direction * 30f, 1f, white);
                Vector2 previousA = center - direction * 27f;
                Vector2 previousB = previousA;
                for (int segment = 1; segment <= 7; segment++)
                {
                    float along = -27f + segment * 7.5f;
                    float wave = MathF.Sin(travelPhase + segment * .95f) * 8f;
                    Vector2 pointA = center + direction * along + normal * wave;
                    Vector2 pointB = center + direction * along - normal * wave;
                    DrawWorldLine(previousA, pointA, 2f, gold);
                    DrawWorldLine(previousB, pointB, 1f, blue);
                    previousA = pointA;
                    previousB = pointB;
                }
                break;
            default:
                DrawWorldLine(center - direction * 29f,
                    center - direction * 10f, 5f,
                    new Color(102, 66, 184, 72));
                DrawWorldLine(center - direction * 14f,
                    center + direction * 15f, 9f, purple);
                DrawWorldLine(center - direction * 9f,
                    center + direction * 17f, 4f, blue);
                DrawBrokenWorldRune(center, 17f, travelPhase,
                    gold, blue, 10, 3f, .72f);
                DrawBrokenWorldRune(center, 10f, -travelPhase * .8f,
                    purple, white, 7, 2f, .62f);
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)center.X - 3, (int)center.Y - 3, 7, 7),
                    white);
                for (int wake = 1; wake <= 3; wake++)
                {
                    Vector2 fragment = center - direction * (17f + wake * 7f) +
                        normal * (wake % 2 == 0 ? 5f : -5f);
                    DrawWorldLine(fragment,
                        fragment + direction * 4f + normal * 2f,
                        1f, wake == 2 ? gold : blue);
                }
                break;
        }
    }

    private void DrawAncientStaffWorld(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (gameSession.Player.WeaponFamily != WeaponFamily.ArcaneWarStaff)
        {
            _thunderSpiritVisualInitialized = false;
            return;
        }

        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 7f);
        float runeTime = (float)gameTime.TotalGameTime.TotalSeconds;
        Color blue = new(75, 151, 229, 205);
        Color purple = new(126, 78, 194, 180);
        Color gold = new(224, 183, 73, 220);
        Color lightning = new(229, 244, 255, 235);

        DrawThunderSpirit(gameSession, gameTime);

        WeaponTechnique technique = gameSession.Player.Combat.CurrentTechnique;
        if (technique == null)
            return;
        WeaponTechniqueEffect effect = technique.Effect;
        int stage = gameSession.Player.Combat.TechniqueStageIndex;
        float stageRatio = gameSession.AncientStaffUltimateStageRatio;
        Vector2 center = gameSession.AncientStaffEffectCenter;

        if (effect == WeaponTechniqueEffect.AncientStaffAegisWard)
        {
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f
                : 1f;
            Vector2 ward = gameSession.Player.Position +
                new Vector2(facing * 43f, -7f);
            float stress = gameSession.Player.Combat.AegisWardStress;
            float stability = stress > .72f
                ? .58f + .42f * MathF.Abs(MathF.Sin(runeTime * 19f))
                : 1f;
            Color wardBlue = new(75, 151, 229,
                (int)(178f * stability));
            Color wardGold = new(224, 183, 73,
                (int)(205f * stability));
            Color wardPurple = new(126, 78, 194,
                (int)(105f * stability));

            for (int plane = -3; plane <= 3; plane++)
            {
                float y = plane * 10f;
                DrawWorldLine(
                    ward + new Vector2(-facing * (4f + Math.Abs(plane)), y),
                    ward + new Vector2(facing * (6f + Math.Abs(plane)),
                        y + facing * 2f),
                    plane == 0 ? 2f : 1f,
                    plane == 0 ? wardGold : wardPurple);
            }
            DrawBrokenWorldRune(ward, 35f + pulse,
                runeTime * .38f, wardBlue, wardGold,
                13, 2f, .68f, 1f, 1.08f);
            DrawBrokenWorldRune(ward, 24f,
                -runeTime * .52f + .3f, wardGold, wardPurple,
                9, 1f, .58f, 1f, 1.12f);
            for (int glyph = -2; glyph <= 2; glyph++)
            {
                Vector2 glyphCenter = ward + new Vector2(
                    facing * (glyph % 2 == 0 ? 3f : -2f),
                    glyph * 12f);
                DrawWorldLine(glyphCenter + new Vector2(-4f, 0f),
                    glyphCenter + new Vector2(0f, -4f), 1f, wardGold);
                DrawWorldLine(glyphCenter + new Vector2(0f, -4f),
                    glyphCenter + new Vector2(4f, 0f), 1f, wardBlue);
            }

            if (gameSession.Player.Combat.IsBlockFeedbackActive ||
                gameSession.Player.Combat.IsPerfectGuardFeedbackActive)
            {
                bool deflect = gameSession.Player.Combat
                    .IsPerfectGuardFeedbackActive;
                Vector2 impact = ward + new Vector2(
                    facing * 2f,
                    gameSession.Player.Combat.AegisImpactVerticalOffset * .72f);
                float ripple = deflect ? 17f + pulse * 5f : 10f + pulse * 4f;
                DrawBrokenWorldRune(impact, ripple,
                    deflect ? -runeTime * 1.8f : runeTime,
                    deflect ? lightning : wardBlue,
                    deflect ? wardGold : wardPurple,
                    deflect ? 10 : 7,
                    deflect ? 3f : 2f,
                    .55f,
                    1f,
                    .85f);
                for (int ray = -2; ray <= 2; ray++)
                {
                    Vector2 rayDirection = Vector2.Normalize(new Vector2(
                        facing,
                        ray * .34f));
                    DrawWorldLine(
                        impact + rayDirection * 5f,
                        impact + rayDirection * (deflect ? 27f : 16f),
                        ray == 0 && deflect ? 3f : 1f,
                        deflect && ray % 2 == 0 ? wardGold : lightning);
                }
            }

            if (stress > .72f)
            {
                DrawWorldLine(ward + new Vector2(-9f, -18f),
                    ward + new Vector2(3f, -7f), 1f, wardPurple);
                DrawWorldLine(ward + new Vector2(3f, -7f),
                    ward + new Vector2(-4f, 6f), 1f, wardGold);
            }
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffRunePrison)
        {
            float radius = AncientStaffTuning.RunePrisonRadius +
                gameSession.Player.Combat.ElderSigils * 8f;
            AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
            float progress = attack == null
                ? 1f
                : Math.Clamp(gameSession.Player.Combat.StateElapsed /
                    MathF.Max(.01f, attack.TotalTime), 0f, 1f);
            float outerFormation = stage == 0
                ? Math.Clamp(progress / .58f, 0f, 1f)
                : 1f;
            float innerFormation = stage == 0
                ? Math.Clamp((progress - .20f) / .58f, 0f, 1f)
                : 1f;
            float restraint = gameSession.AncientStaffRestraintVisualStrength;
            float contraction = stage == 1
                ? MathHelper.Lerp(1f, .79f,
                    MathHelper.SmoothStep(0f, 1f, progress))
                : 1f;
            radius *= contraction;
            DrawBrokenWorldRune(center, radius,
                -.28f + runeTime * .08f, purple, blue,
                20, 3f, .73f, outerFormation, .29f);
            DrawBrokenWorldRune(center, radius * .66f,
                .42f - runeTime * .11f, gold, blue,
                14, 2f, .62f, innerFormation, .29f);
            DrawBrokenWorldRune(center, radius * .33f,
                -.75f + runeTime * .16f, blue, gold,
                9, 1f, .54f, innerFormation, .29f);
            for (int spoke = 0; spoke < 8; spoke++)
            {
                float angle = MathHelper.TwoPi * spoke / 8f + .18f;
                Vector2 inner = center + new Vector2(
                    MathF.Cos(angle) * radius * .34f,
                    MathF.Sin(angle) * radius * .10f);
                Vector2 outer = center + new Vector2(
                    MathF.Cos(angle) * radius * .85f,
                    MathF.Sin(angle) * radius * .25f);
                DrawWorldLine(inner, outer, 1f,
                    spoke % 3 == 0 ? gold : purple);
            }
            for (int pillar = 0; pillar < 6; pillar++)
            {
                float angle = MathHelper.TwoPi * pillar / 6f;
                Vector2 root = center + new Vector2(
                    MathF.Cos(angle) * radius * .72f,
                    MathF.Sin(angle) * radius * .20f);
                float height = (20f + 31f * innerFormation) * restraint;
                Vector2 top = root - new Vector2(0f, height);
                DrawWorldLine(root, top,
                    restraint > .8f ? 3f : 2f,
                    pillar % 2 == 0 ? blue : purple);
                if (restraint > .45f)
                {
                    Vector2 chainEnd = Vector2.Lerp(top,
                        center - new Vector2(0f, 15f), .68f);
                    DrawWorldLine(top, chainEnd, 1f, gold);
                }
            }
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffSpiritBastion)
        {
            AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
            float progress = attack == null ? 1f : Math.Clamp(
                gameSession.Player.Combat.StateElapsed /
                    MathF.Max(.01f, attack.TotalTime), 0f, 1f);
            Vector2 bastion = gameSession.Player.Position;
            float radius = AncientStaffTuning.SpiritBastionRadius;
            DrawBrokenWorldRune(bastion, radius,
                runeTime * .12f, blue, purple,
                24, 3f, .72f, 1f, .34f);
            float pulseRadius = radius *
                (.48f + progress * .34f + stage * .025f);
            DrawBrokenWorldRune(bastion, pulseRadius,
                -runeTime * .38f, gold, lightning,
                15, 2f, .56f, 1f, .34f);
            Vector2 imprint = bastion - new Vector2(0f, 21f);
            DrawWorldLine(imprint - new Vector2(9f, 0f),
                imprint + new Vector2(9f, 0f), 5f,
                new Color(71, 132, 215, 115));
            DrawWorldLine(imprint, imprint + new Vector2(0f, 28f),
                4f, new Color(95, 75, 180, 115));
            DrawWorldLine(imprint - new Vector2(4f, -4f),
                imprint - new Vector2(0f, 11f), 2f, gold);
            DrawWorldLine(imprint + new Vector2(4f, 4f),
                imprint + new Vector2(19f, 12f), 2f, blue);
            for (int vein = 0; vein < 5; vein++)
            {
                float angle = MathHelper.TwoPi * vein / 5f +
                    runeTime * .15f;
                Vector2 inner = bastion + new Vector2(
                    MathF.Cos(angle) * 24f,
                    MathF.Sin(angle) * 9f);
                Vector2 middle = bastion + new Vector2(
                    MathF.Cos(angle + .18f) * radius * .58f,
                    MathF.Sin(angle + .18f) * radius * .20f);
                Vector2 outer = bastion + new Vector2(
                    MathF.Cos(angle) * radius * .92f,
                    MathF.Sin(angle) * radius * .31f);
                DrawWorldLine(inner, middle, 2f,
                    vein % 2 == 0 ? blue : purple);
                DrawWorldLine(middle, outer, 1f,
                    vein == stage % 5 ? lightning : gold);
            }
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffElderInvocation)
        {
            AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
            float progress = attack == null ? 1f : Math.Clamp(
                gameSession.Player.Combat.StateElapsed /
                    MathF.Max(.01f, attack.TotalTime), 0f, 1f);
            float apparition = stage == 0
                ? MathHelper.SmoothStep(0f, 1f, progress)
                : stage == 3
                    ? 1f - progress * .76f
                    : 1f;
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f : 1f;
            Vector2 commandHand = gameSession.Player.Position +
                new Vector2(facing * 25f, -47f);
            DrawArchmagePresence(
                gameSession.Player.Position + new Vector2(-facing * 9f, 18f),
                apparition * .76f,
                1.16f,
                commandHand,
                runeTime);
            for (int rune = 0; rune < 5; rune++)
            {
                float x = (rune - 2) * 15f;
                float rise = (stage + progress) * (12f + rune * 2f);
                Vector2 glyph = gameSession.Player.Position +
                    new Vector2(x, 10f - rise);
                DrawWorldLine(glyph - new Vector2(4f, 0f),
                    glyph + new Vector2(0f, -5f), 1f,
                    rune % 2 == 0 ? gold : blue);
                DrawWorldLine(glyph + new Vector2(0f, -5f),
                    glyph + new Vector2(4f, 0f), 1f, purple);
            }
            Vector2 matrix = center - new Vector2(0f, 72f);
            DrawBrokenWorldRune(matrix, 50f + stage * 5f,
                runeTime * .23f, purple, gold,
                18, 2f, .66f,
                stage == 0 ? progress : 1f, .42f);
            DrawBrokenWorldRune(matrix, 29f,
                -runeTime * .42f, blue, lightning,
                11, 2f, .54f,
                stage == 0 ? progress : 1f, .42f);
            if (stage >= 1)
            {
                Vector2 strikeStart = center - new Vector2(0f, 155f);
                Vector2 bend = Vector2.Lerp(strikeStart, center, .52f) +
                    new Vector2(stage % 2 == 0 ? -7f : 7f, 0f);
                DrawWorldLine(strikeStart, bend,
                    stage == 3 ? 7f : 3f,
                    stage == 3 ? lightning : blue);
                DrawWorldLine(bend, center,
                    stage == 3 ? 9f : 4f,
                    stage == 3 ? lightning : blue);
                DrawWorldLine(bend, bend + new Vector2(12f, 8f),
                    1f, gold);
            }
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffAstralDescent)
        {
            DrawArcaneRing(gameSession.Player.Position + new Vector2(0f, 24f),
                36f + stage * 18f, purple, 14, 2f);
            DrawWorldLine(gameSession.Player.Position - new Vector2(0f, 34f),
                gameSession.Player.Position + new Vector2(0f, 36f),
                4f, lightning);
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffBlinkCast)
        {
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f : 1f;
            float trailRatio = gameSession.AncientStaffBlinkTrailRatio;
            Vector2 origin = trailRatio > 0f
                ? gameSession.AncientStaffBlinkOrigin
                : gameSession.Player.Position -
                    new Vector2(facing * AncientStaffTuning.BlinkDistance, 0f);
            Vector2 destination = gameSession.Player.Position;
            Color blinkPurple = new(117, 83, 201,
                (int)(150f * trailRatio));
            Color blinkBlue = new(75, 151, 229,
                (int)(180f * trailRatio));
            Color blinkGold = new(224, 183, 73,
                (int)(110f * trailRatio));
            DrawWorldLine(origin + new Vector2(0f, -24f),
                origin + new Vector2(0f, 22f), 7f,
                new Color(83, 65, 160, (int)(72f * trailRatio)));
            DrawWorldLine(origin + new Vector2(-8f, -12f),
                origin + new Vector2(8f, -4f), 2f, blinkBlue);
            DrawWorldLine(origin + new Vector2(-7f, 8f),
                origin + new Vector2(6f, 16f), 2f, blinkPurple);
            DrawBrokenWorldRune(origin, 22f,
                runeTime * .9f, blinkPurple, blinkBlue,
                9, 2f, .56f, trailRatio, .82f);
            for (int fracture = 1; fracture <= 4; fracture++)
            {
                float t = fracture / 5f;
                Vector2 point = Vector2.Lerp(origin, destination, t);
                float bend = (fracture % 2 == 0 ? 1f : -1f) *
                    (4f + fracture);
                DrawWorldLine(point - new Vector2(facing * 9f, bend),
                    point + new Vector2(facing * 5f, -bend * .35f),
                    1f,
                    fracture == 3 ? blinkGold : blinkPurple);
            }
            DrawBrokenWorldRune(destination - new Vector2(0f, 5f), 16f,
                -runeTime * 1.2f, blinkBlue, blinkGold,
                8, 2f, .56f, 1f, .82f);
        }
        else if (effect ==
            WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate)
        {
            DrawHeavenfallThunderGateWorld(
                gameSession,
                gameTime,
                stage,
                stageRatio,
                center);
        }
    }

    private void DrawHeavenfallThunderGateWorld(
        GameSession gameSession,
        GameTime gameTime,
        int stage,
        float stageRatio,
        Vector2 center)
    {
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        bool judgment = gameSession.Player.Combat.State ==
            CombatState.ChargingHeavy;
        float intro = judgment
            ? gameSession.Player.Combat.AncientStaffUltimateCinematicRatio
            : 1f;
        Color blue = new(67, 139, 226, 188);
        Color paleBlue = new(150, 209, 255, 220);
        Color purple = new(112, 71, 185, 165);
        Color gold = new(226, 184, 70, 205);
        Color white = new(239, 248, 255, 240);

        if (judgment)
        {
            float mark = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(intro / .68f, 0f, 1f));
            DrawBrokenWorldRune(center,
                24f + 23f * mark,
                -.35f + time * .08f,
                gold,
                blue,
                12,
                2f,
                .54f,
                mark,
                .30f);
            DrawWorldLine(
                gameSession.Player.Position - new Vector2(0f, 12f),
                Vector2.Lerp(gameSession.Player.Position, center, mark),
                1f,
                new Color(222, 184, 75, (int)(112f * mark)));
            for (int glyph = 0; glyph < 4; glyph++)
            {
                float side = glyph % 2 == 0 ? -1f : 1f;
                Vector2 rune = gameSession.Player.Position + new Vector2(
                    side * (14f + glyph * 4f),
                    7f - mark * (22f + glyph * 10f));
                DrawWorldLine(rune - new Vector2(3f, 0f),
                    rune + new Vector2(0f, -5f), 1f,
                    glyph < 2 ? gold : blue);
                DrawWorldLine(rune + new Vector2(0f, -5f),
                    rune + new Vector2(4f, 1f), 1f, purple);
            }
            return;
        }

        float sealFormation = stage == 0
            ? MathHelper.SmoothStep(0f, 1f, stageRatio)
            : 1f;
        float sealIntegrity = stage == 9
            ? 1f - MathHelper.SmoothStep(0f, 1f, stageRatio)
            : 1f;
        float finalContact = stage == 8
            ? GetAncientStaffContactRatio(gameSession)
            : 1f;
        float contraction = stage == 8
            ? MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(stageRatio / MathF.Max(.01f, finalContact), 0f, 1f))
            : 0f;
        float sealRadius = AncientStaffTuning.UltimateRadius *
            MathHelper.Lerp(1f, .72f, contraction);
        DrawHeavenfallGroundSeal(center, sealRadius, time,
            sealFormation, sealIntegrity, stage, stageRatio,
            blue, purple, gold, paleBlue);

        float earthRise = stage == 1
            ? MathHelper.SmoothStep(0f, 1f, stageRatio)
            : stage is >= 2 and <= 9 ? 1f : 0f;
        float aftermath = stage == 9 ? stageRatio : 0f;
        if (stage >= 1)
        {
            DrawHeavenfallDebris(center, time, earthRise, aftermath,
                stage == 8 && stageRatio >= finalContact,
                blue, gold);
        }
        if (stage is 1 or 2)
            DrawHeavenfallSuspension(gameSession, center,
                stage == 1 ? earthRise : 1f, blue, purple, gold);

        float gateFormation = stage switch
        {
            1 => Math.Clamp((stageRatio - .58f) / .42f, 0f, .22f),
            2 => MathHelper.SmoothStep(0f, 1f, stageRatio),
            >= 3 and <= 8 => 1f,
            9 => 1f - MathHelper.SmoothStep(0f, 1f, stageRatio),
            _ => 0f
        };
        Vector2 gate = center - new Vector2(0f, 205f);
        if (gateFormation > 0f)
            DrawHeavenfallSpaceGate(gate, time, gateFormation,
                stage, stageRatio, blue, purple, gold, paleBlue);

        float archmage = stage switch
        {
            1 => MathF.Sin(stageRatio * MathHelper.Pi) * .58f,
            2 => MathF.Sin(Math.Clamp(stageRatio * 1.18f, 0f, 1f) *
                MathHelper.Pi) * .70f,
            8 => MathF.Sin(Math.Clamp(stageRatio / .70f, 0f, 1f) *
                MathHelper.Pi) * .92f,
            9 => (1f - stageRatio) * .35f,
            _ => 0f
        };
        if (archmage > .01f)
        {
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f : 1f;
            Vector2 gesture = stage == 8
                ? gate + new Vector2(facing * 35f, 22f)
                : Vector2.Lerp(gameSession.Player.Position, gate, .54f);
            DrawArchmagePresence(
                gameSession.Player.Position + new Vector2(-facing * 18f, 24f),
                archmage,
                stage == 8 ? 1.72f : 1.34f,
                gesture,
                time);
            DrawWorldLine(
                gameSession.Player.Position - new Vector2(0f, 44f),
                gesture,
                stage == 8 ? 3f : 2f,
                new Color(226, 184, 70, (int)(145f * archmage)));
            DrawWorldLine(gesture, gate,
                1f,
                new Color(130, 91, 204, (int)(155f * archmage)));
        }

        if (stage is >= 3 and <= 7)
        {
            int strikeIndex = stage - 3;
            float contact = GetAncientStaffContactRatio(gameSession);
            float charge = Math.Clamp(stageRatio /
                MathF.Max(.01f, contact), 0f, 1f);
            float afterglow = stageRatio >= contact
                ? 1f - Math.Clamp((stageRatio - contact) /
                    MathF.Max(.01f, 1f - contact), 0f, 1f)
                : 0f;
            float boltOpacity = stageRatio < contact
                ? Math.Clamp((charge - .64f) / .36f, 0f, 1f)
                : afterglow;
            float offset = strikeIndex switch
            {
                0 => -72f,
                1 => 48f,
                2 => -30f,
                3 => 78f,
                _ => 0f
            };
            Vector2 impact = center + new Vector2(offset, 0f);
            DrawHeavenfallGateCharge(gate, charge,
                strikeIndex, blue, gold, white);
            if (boltOpacity > .01f)
            {
                DrawHeavenfallLightning(gate + new Vector2(offset * .10f, 22f),
                    impact,
                    boltOpacity,
                    strikeIndex,
                    final: false,
                    white,
                    blue,
                    purple,
                    gold);
                DrawBrokenWorldRune(impact, 22f + strikeIndex * 3f,
                    time * .7f, gold, blue,
                    9 + strikeIndex, 2f, .50f,
                    boltOpacity, .25f);
            }
        }
        else if (stage == 8)
        {
            float charge = Math.Clamp(stageRatio /
                MathF.Max(.01f, finalContact), 0f, 1f);
            float afterglow = stageRatio >= finalContact
                ? 1f - Math.Clamp((stageRatio - finalContact) /
                    MathF.Max(.01f, 1f - finalContact), 0f, 1f)
                : 0f;
            DrawHeavenfallGateCharge(gate, charge,
                5, blue, gold, white);
            if (stageRatio >= finalContact - .075f)
            {
                float opacity = stageRatio < finalContact
                    ? Math.Clamp((stageRatio - (finalContact - .075f)) /
                        .075f, 0f, 1f)
                    : afterglow;
                DrawHeavenfallLightning(gate + new Vector2(0f, 18f),
                    center,
                    opacity,
                    5,
                    final: true,
                    white,
                    blue,
                    purple,
                    gold);
                float impact = stageRatio >= finalContact
                    ? 1f - Math.Clamp((stageRatio - finalContact) / .30f,
                        0f, 1f)
                    : 0f;
                DrawHeavenfallFinalGroundReaction(center, time,
                    impact, blue, purple, gold, white);
            }
        }

        if (stage == 9 && gameSession.ThunderSpiritState !=
                ThunderSpiritState.Dormant)
        {
            float spiritReturn = 1f - stageRatio;
            DrawWorldLine(gameSession.ThunderSpiritPosition,
                gameSession.Player.Position - new Vector2(0f, 30f),
                1f,
                new Color(111, 170, 233, (int)(92f * spiritReturn)));
            DrawBrokenWorldRune(gameSession.ThunderSpiritPosition,
                13f + spiritReturn * 5f, -time * .44f,
                blue, gold, 8, 1f, .52f, spiritReturn, .72f);
        }
    }

    private static float GetAncientStaffContactRatio(GameSession gameSession)
    {
        AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
        return attack == null
            ? .35f
            : Math.Clamp(attack.StartupTime /
                MathF.Max(.01f, attack.TotalTime), .05f, .90f);
    }

    private void DrawHeavenfallGroundSeal(
        Vector2 center,
        float radius,
        float time,
        float formation,
        float integrity,
        int stage,
        float stageRatio,
        Color blue,
        Color purple,
        Color gold,
        Color paleBlue)
    {
        formation = Math.Clamp(formation, 0f, 1f);
        integrity = Math.Clamp(integrity, 0f, 1f);
        float central = Math.Clamp(formation / .24f, 0f, 1f);
        float radial = Math.Clamp((formation - .14f) / .34f, 0f, 1f);
        float outer = Math.Clamp((formation - .34f) / .42f, 0f, 1f);
        float symbols = Math.Clamp((formation - .62f) / .38f, 0f, 1f);
        float overload = stage == 8
            ? Math.Clamp((stageRatio - .30f) / .20f, 0f, 1f)
            : 0f;
        Color outerBlue = new(blue.R, blue.G, blue.B,
            (int)(blue.A * integrity));
        Color sealGold = new(gold.R, gold.G, gold.B,
            (int)(gold.A * integrity));
        Color sealPurple = new(purple.R, purple.G, purple.B,
            (int)(purple.A * integrity));

        DrawBrokenWorldRune(center, radius,
            -.24f + time * .025f,
            outerBlue,
            sealGold,
            34,
            overload > 0f ? 4f : 3f,
            .76f,
            outer * integrity,
            .27f);
        DrawBrokenWorldRune(center, radius * .70f,
            .31f - time * .038f,
            sealGold,
            sealPurple,
            24,
            2f,
            .67f,
            symbols * integrity,
            .27f);
        DrawBrokenWorldRune(center, radius * .31f,
            -.62f + time * .055f,
            sealPurple,
            paleBlue,
            12,
            2f,
            .56f,
            central * integrity,
            .27f);

        int visibleSpokes = Math.Clamp((int)MathF.Ceiling(12f * radial), 0, 12);
        for (int spoke = 0; spoke < visibleSpokes; spoke++)
        {
            float angle = MathHelper.TwoPi * spoke / 12f;
            Vector2 direction = new(
                MathF.Cos(angle),
                MathF.Sin(angle) * .27f);
            Vector2 start = center + direction * radius * .32f;
            Vector2 end = center + direction * radius *
                (overload > 0f && spoke % 3 == 0 ? 1.10f : .90f);
            DrawWorldLine(start, end,
                spoke % 3 == 0 ? 2f : 1f,
                spoke % 4 == 0 ? sealGold : outerBlue);
            if (symbols > 0f && spoke % 2 == 0)
            {
                Vector2 glyph = center + direction * radius * .80f;
                Vector2 tangent = new(-direction.Y, direction.X);
                DrawWorldLine(glyph - tangent * 5f,
                    glyph + tangent * 5f, 1f, sealPurple);
            }
        }
    }

    private void DrawHeavenfallDebris(
        Vector2 center,
        float time,
        float rise,
        float aftermath,
        bool finalImpact,
        Color blue,
        Color gold)
    {
        float floating = Math.Clamp(rise * (1f - aftermath), 0f, 1f);
        for (int rock = 0; rock < 13; rock++)
        {
            float angle = MathHelper.TwoPi * rock / 13f + .17f;
            float ring = 52f + (rock * 37 % 145);
            Vector2 ground = center + new Vector2(
                MathF.Cos(angle) * ring,
                MathF.Sin(angle) * ring * .25f);
            float lag = Math.Clamp(rise * 1.35f - rock % 4 * .09f, 0f, 1f);
            float lift = (24f + rock * 17f % 68f) * lag;
            float hover = MathF.Sin(time * 1.15f + rock * 1.7f) * 3f * floating;
            float outward = finalImpact ? 10f + rock % 4 * 5f : 0f;
            Vector2 position = ground + new Vector2(
                MathF.Cos(angle) * outward,
                -lift + hover + aftermath * (35f + rock * 3f));
            int width = 5 + rock * 7 % 12;
            int height = 4 + rock * 5 % 8;
            Color stone = rock % 3 == 0
                ? new Color(58, 62, 79, (int)(185f * (1f - aftermath)))
                : new Color(76, 70, 73, (int)(175f * (1f - aftermath)));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)position.X - width / 2,
                    (int)position.Y - height / 2, width, height),
                stone);
            DrawWorldLine(position - new Vector2(width * .45f, height * .35f),
                position + new Vector2(width * .48f, height * .18f),
                1f, rock % 4 == 0 ? gold : blue);
            if (rise < 1f && lag > .05f)
            {
                float dust = 1f - lag;
                DrawWorldLine(ground - new Vector2(width, 0f),
                    ground - new Vector2(width + 7f, 2f + dust * 5f),
                    2f, new Color(105, 96, 88, (int)(90f * dust)));
                DrawWorldLine(ground + new Vector2(width, 0f),
                    ground + new Vector2(width + 8f, -3f - dust * 4f),
                    1f, new Color(116, 106, 91, (int)(75f * dust)));
            }
        }
    }

    private void DrawHeavenfallSuspension(
        GameSession gameSession,
        Vector2 center,
        float strength,
        Color blue,
        Color purple,
        Color gold)
    {
        float radiusSquared = AncientStaffTuning.UltimateRadius *
            AncientStaffTuning.UltimateRadius;
        foreach (Enemy enemy in gameSession.Enemies.Enemies)
        {
            if (!enemy.CanBeTargeted ||
                Vector2.DistanceSquared(center, enemy.Position) > radiusSquared)
                continue;
            DrawHeavenfallSuspensionBands(enemy.Bounds,
                strength, false, blue, purple, gold);
        }
        if (gameSession.ShouldRenderBoss && gameSession.Boss?.IsAlive == true &&
            Vector2.DistanceSquared(center, gameSession.Boss.Position) <=
                radiusSquared)
        {
            DrawHeavenfallSuspensionBands(gameSession.Boss.Bounds,
                strength, true, blue, purple, gold);
        }
    }

    private void DrawHeavenfallSuspensionBands(
        Rectangle bounds,
        float strength,
        bool symbolicOnly,
        Color blue,
        Color purple,
        Color gold)
    {
        Vector2 center = bounds.Center.ToVector2();
        float halfHeight = bounds.Height * .62f + 8f;
        for (int band = -1; band <= 1; band++)
        {
            float x = band * (bounds.Width * .45f + 5f);
            DrawWorldLine(center + new Vector2(x, halfHeight * .62f),
                center + new Vector2(x * .72f, -halfHeight),
                symbolicOnly ? 2f : 1f,
                band == 0 ? gold : band < 0 ? purple : blue);
        }
        DrawBrokenWorldRune(center + new Vector2(0f, halfHeight * .55f),
            bounds.Width * .60f + 8f,
            symbolicOnly ? .35f : -.25f,
            symbolicOnly ? gold : blue,
            purple,
            symbolicOnly ? 13 : 9,
            symbolicOnly ? 2f : 1f,
            .55f,
            strength,
            .25f);
    }

    private void DrawHeavenfallSpaceGate(
        Vector2 gate,
        float time,
        float formation,
        int stage,
        float stageRatio,
        Color blue,
        Color purple,
        Color gold,
        Color paleBlue)
    {
        formation = Math.Clamp(formation, 0f, 1f);
        float fracture = Math.Clamp(formation / .20f, 0f, 1f);
        float arcs = Math.Clamp((formation - .14f) / .34f, 0f, 1f);
        float interior = Math.Clamp((formation - .38f) / .32f, 0f, 1f);
        float stable = Math.Clamp((formation - .68f) / .32f, 0f, 1f);
        float width = 114f * (.18f + .82f * formation);
        float verticalScale = .48f;
        float finalAlignment = stage == 8
            ? MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(stageRatio / .31f, 0f, 1f))
            : 0f;

        DrawWorldLine(gate - new Vector2(width * fracture, 0f),
            gate + new Vector2(width * fracture, 0f),
            2f + 3f * fracture,
            new Color(132, 91, 202, (int)(185f * formation)));
        if (interior > 0f)
        {
            for (int cloud = -3; cloud <= 3; cloud++)
            {
                float y = cloud * 10f * formation;
                float normalized = y / MathF.Max(1f, 55f * formation);
                float span = width * .84f * MathF.Sqrt(MathF.Max(0f,
                    1f - normalized * normalized));
                float drift = MathF.Sin(time * .65f + cloud) * 8f;
                DrawWorldLine(gate + new Vector2(-span + drift, y),
                    gate + new Vector2(span * .72f + drift, y),
                    cloud == 0 ? 12f : 7f,
                    new Color(10 + Math.Abs(cloud) * 3, 15, 38,
                        (int)(175f * interior)));
            }
            for (int storm = 0; storm < 5; storm++)
            {
                float angle = time * (.18f + storm * .012f) +
                    MathHelper.TwoPi * storm / 5f;
                Vector2 start = gate + new Vector2(
                    MathF.Cos(angle) * width * .50f,
                    MathF.Sin(angle) * width * .17f);
                Vector2 end = gate + new Vector2(
                    MathF.Cos(angle + .72f) * width * .24f,
                    MathF.Sin(angle + .72f) * width * .09f);
                DrawWorldLine(start, end, storm == 2 ? 2f : 1f,
                    storm % 2 == 0 ? purple : blue);
            }
        }

        DrawBrokenWorldRune(gate, width,
            -.32f + time * (.055f + finalAlignment * .18f),
            purple, gold, 28, 4f, .70f, arcs, verticalScale);
        DrawBrokenWorldRune(gate, width * .73f,
            .44f - time * (.09f + finalAlignment * .24f),
            blue, paleBlue, 21, 2f, .62f, stable, verticalScale);
        DrawBrokenWorldRune(gate, width * .48f,
            -.80f + time * .14f,
            gold, purple, 15, 2f, .54f, stable, verticalScale);
        for (int depth = 0; depth < 5; depth++)
        {
            float z = (depth + 1) / 5f;
            float angle = time * (.11f + z * .04f) + depth * 1.21f;
            Vector2 shard = gate + new Vector2(
                MathF.Cos(angle) * width * (1f + z * .18f),
                MathF.Sin(angle) * width * verticalScale * (1f + z * .12f));
            DrawWorldLine(shard,
                shard + new Vector2(MathF.Cos(angle) * (4f + z * 7f),
                    MathF.Sin(angle) * (2f + z * 4f)),
                depth % 2 == 0 ? 2f : 1f,
                depth % 3 == 0 ? gold : purple);
        }
    }

    private void DrawHeavenfallGateCharge(
        Vector2 gate,
        float charge,
        int strikeIndex,
        Color blue,
        Color gold,
        Color white)
    {
        charge = Math.Clamp(charge, 0f, 1f);
        float radius = 8f + charge * (strikeIndex >= 5 ? 31f : 16f);
        DrawBrokenWorldRune(gate + new Vector2(0f, 4f), radius,
            -.4f + charge * 1.8f,
            strikeIndex >= 5 ? gold : blue,
            white,
            strikeIndex >= 5 ? 16 : 10,
            strikeIndex >= 5 ? 3f : 2f,
            .58f,
            charge,
            .48f);
        DrawWorldLine(gate - new Vector2(radius * charge, 0f),
            gate + new Vector2(radius * charge, 0f),
            1f + charge * (strikeIndex >= 5 ? 6f : 3f),
            new Color(239, 248, 255, (int)(195f * charge)));
    }

    private void DrawHeavenfallLightning(
        Vector2 origin,
        Vector2 impact,
        float opacity,
        int variant,
        bool final,
        Color white,
        Color blue,
        Color purple,
        Color gold)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        int segments = final ? 8 : 6;
        Vector2 previous = origin;
        for (int segment = 1; segment <= segments; segment++)
        {
            float t = segment / (float)segments;
            float bend = segment == segments
                ? 0f
                : MathF.Sin((segment * 2.41f + variant * 1.73f)) *
                    (final ? 17f : 10f) * (1f - t * .38f);
            Vector2 next = Vector2.Lerp(origin, impact, t) +
                new Vector2(bend, 0f);
            DrawWorldLine(previous, next,
                (final ? 17f : 7f) * opacity,
                new Color(53, 112, 207, (int)(120f * opacity)));
            DrawWorldLine(previous, next,
                (final ? 8f : 3f) * opacity,
                new Color(white.R, white.G, white.B,
                    (int)(white.A * opacity)));
            if (segment is 2 or 4 || final && segment == 6)
            {
                float side = (segment + variant) % 2 == 0 ? -1f : 1f;
                Vector2 branch = next + new Vector2(
                    side * (final ? 34f : 20f),
                    13f + segment * 3f);
                DrawWorldLine(next, branch,
                    final ? 3f : 2f,
                    new Color(purple.R, purple.G, purple.B,
                        (int)(155f * opacity)));
                DrawWorldLine(next, Vector2.Lerp(next, branch, .56f),
                    1f,
                    new Color(gold.R, gold.G, gold.B,
                        (int)(145f * opacity)));
            }
            previous = next;
        }
    }

    private void DrawHeavenfallFinalGroundReaction(
        Vector2 center,
        float time,
        float impact,
        Color blue,
        Color purple,
        Color gold,
        Color white)
    {
        if (impact <= .01f)
            return;
        float expansion = 1f - impact;
        DrawBrokenWorldRune(center, 55f + expansion * 165f,
            time * .10f, white, blue,
            26, 4f, .60f, impact, .26f);
        DrawBrokenWorldRune(center, 36f + expansion * 115f,
            -time * .18f, gold, purple,
            20, 3f, .55f, impact, .26f);
        for (int crack = 0; crack < 12; crack++)
        {
            float angle = MathHelper.TwoPi * crack / 12f + .12f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle) * .27f);
            Vector2 start = center + direction * 21f;
            Vector2 middle = center + direction * (55f + expansion * 38f) +
                new Vector2((crack % 2 == 0 ? -1f : 1f) * 7f, 0f);
            Vector2 end = center + direction * (95f + expansion * 95f);
            DrawWorldLine(start, middle, crack % 3 == 0 ? 3f : 2f,
                crack % 4 == 0 ? gold : blue);
            DrawWorldLine(middle, end, 1f,
                crack % 3 == 0 ? white : purple);
        }
    }

    private void DrawThunderSpirit(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsThunderSpiritVisualVisible)
        {
            _thunderSpiritVisualInitialized = false;
            return;
        }

        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        Vector2 desired = gameSession.ThunderSpiritPosition;
        if (!_thunderSpiritVisualInitialized)
        {
            _thunderSpiritVisualPosition = desired + new Vector2(0f, 18f);
            _thunderSpiritVisualInitialized = true;
        }
        float distance = Vector2.Distance(
            _thunderSpiritVisualPosition,
            desired);
        float followRate = distance > 115f ? 13f : 6.5f;
        float follow = 1f - MathF.Exp(-followRate * elapsed);
        _thunderSpiritVisualPosition = Vector2.Lerp(
            _thunderSpiritVisualPosition,
            desired,
            follow);

        ThunderSpiritState state = gameSession.ThunderSpiritState;
        float summon = gameSession.ThunderSpiritSummonVisualRatio;
        float dismiss = gameSession.ThunderSpiritDismissVisualRatio;
        float form = state == ThunderSpiritState.Dormant
            ? dismiss
            : summon;
        float hover = MathF.Sin(time * 2.7f) * 3.5f * form;
        float sway = MathF.Sin(time * 1.35f) * 1.8f;
        Vector2 spirit = _thunderSpiritVisualPosition +
            new Vector2(sway, hover);

        WeaponTechniqueEffect effect =
            gameSession.Player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
        float stageProgress = attack == null
            ? 0f
            : Math.Clamp(gameSession.Player.Combat.StateElapsed /
                MathF.Max(.01f, attack.TotalTime), 0f, 1f);
        float stretch = Math.Clamp(distance / 150f, 0f, 1f);

        if (effect == WeaponTechniqueEffect.AncientStaffSpiritBastion)
        {
            float merge = gameSession.Player.Combat.TechniqueStageIndex == 0
                ? MathHelper.SmoothStep(0f, 1f, stageProgress)
                : gameSession.Player.Combat.TechniqueStageIndex < 4
                    ? 1f
                    : 1f - stageProgress;
            float facing = gameSession.Player.Facing == FacingDirection.Left
                ? -1f : 1f;
            Vector2 wardCore = gameSession.Player.Position +
                new Vector2(facing * 38f, -8f);
            spirit = Vector2.Lerp(spirit, wardCore, merge);
            stretch = MathF.Max(stretch, merge);
            form *= 1f - merge * .55f;
        }
        else if (effect == WeaponTechniqueEffect.AncientStaffElderInvocation)
        {
            int stage = gameSession.Player.Combat.TechniqueStageIndex;
            Vector2 commandPoint = stage == 0
                ? gameSession.Player.Position + new Vector2(42f, -82f)
                : gameSession.AncientStaffEffectCenter + new Vector2(0f, -92f);
            float command = stage == 0
                ? stageProgress * .72f
                : .72f + stageProgress * .22f;
            spirit = Vector2.Lerp(spirit, commandPoint, command);
            stretch = MathF.Max(stretch, .25f + command * .42f);
        }

        Color blue = new(72, 158, 235, (int)(205f * form));
        Color deep = new(95, 66, 174, (int)(118f * form));
        Color gold = new(226, 185, 75, (int)(205f * form));
        Color white = new(235, 248, 255,
            (int)((state == ThunderSpiritState.Discharged ? 125f : 240f) *
                form));
        if (state == ThunderSpiritState.Discharged)
        {
            blue = new Color(69, 105, 164, (int)(135f * form));
            gold = new Color(156, 126, 71, (int)(115f * form));
        }

        if (summon < 1f && state != ThunderSpiritState.Dormant)
        {
            float convergence = MathHelper.SmoothStep(0f, 1f, summon);
            DrawBrokenWorldRune(desired + new Vector2(0f, 8f),
                30f - convergence * 7f,
                time * .8f, deep, gold, 12, 2f, .58f,
                Math.Clamp(summon * 1.6f, 0f, 1f), 1.12f);
            for (int glyph = 0; glyph < 5; glyph++)
            {
                float angle = MathHelper.TwoPi * glyph / 5f + time * .4f;
                Vector2 outer = desired + new Vector2(
                    MathF.Cos(angle) * 31f,
                    MathF.Sin(angle) * 24f);
                Vector2 inner = Vector2.Lerp(outer, desired,
                    .35f + convergence * .48f);
                DrawWorldLine(outer, inner, 1f,
                    glyph % 2 == 0 ? gold : blue);
            }
        }

        float bodyLength = 23f + stretch * 13f;
        Vector2 head = spirit - new Vector2(0f, 17f);
        Vector2 shoulder = spirit - new Vector2(0f, 7f);
        Vector2 lower = spirit + new Vector2(0f, bodyLength);
        DrawWorldLine(shoulder - new Vector2(8f, 0f),
            shoulder + new Vector2(8f, 0f), 8f, deep);
        DrawWorldLine(shoulder, lower, 10f,
            new Color(71, 102, 178, (int)(86f * form)));
        DrawWorldLine(shoulder, lower - new Vector2(0f, 4f), 4f, blue);
        DrawWorldLine(lower - new Vector2(0f, 7f),
            lower + new Vector2(-5f, 7f), 2f, deep);
        DrawWorldLine(lower - new Vector2(0f, 5f),
            lower + new Vector2(5f, 5f), 1f, blue);

        DrawWorldLine(head + new Vector2(0f, -7f),
            head + new Vector2(6f, 1f), 3f, blue);
        DrawWorldLine(head + new Vector2(6f, 1f),
            head + new Vector2(0f, 7f), 3f, deep);
        DrawWorldLine(head + new Vector2(0f, 7f),
            head + new Vector2(-6f, 1f), 3f, blue);
        DrawWorldLine(head + new Vector2(-6f, 1f),
            head + new Vector2(0f, -7f), 2f, gold);
        DrawWorldLine(head + new Vector2(-7f, -2f),
            head + new Vector2(-3f, -10f), 2f, gold);
        DrawWorldLine(head + new Vector2(7f, -2f),
            head + new Vector2(3f, -10f), 2f, gold);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)head.X - 2, (int)head.Y, 5, 3),
            white);

        bool attacking = gameSession.AncientStaffEffectRatio > 0f &&
            gameSession.ThunderSpiritLastTarget != Vector2.Zero;
        Vector2 targetDirection = attacking
            ? gameSession.ThunderSpiritLastTarget - spirit
            : new Vector2(
                gameSession.Player.Facing == FacingDirection.Left ? -1f : 1f,
                -.12f);
        if (targetDirection.LengthSquared() < .01f)
            targetDirection = Vector2.UnitX;
        else
            targetDirection.Normalize();
        Vector2 perpendicular = new(-targetDirection.Y, targetDirection.X);
        float extension = state == ThunderSpiritState.Empowered ? 22f : 16f;
        Vector2 leftHand = shoulder - perpendicular * 8f +
            targetDirection * extension;
        Vector2 rightHand = shoulder + perpendicular * 8f +
            targetDirection * (extension - 3f);
        DrawWorldLine(shoulder - perpendicular * 7f, leftHand,
            state == ThunderSpiritState.Empowered ? 4f : 3f, blue);
        DrawWorldLine(shoulder + perpendicular * 7f, rightHand,
            3f, white);
        DrawWorldLine(leftHand - perpendicular * 3f,
            leftHand + perpendicular * 4f, 1f, gold);

        float haloRadius = state == ThunderSpiritState.Empowered ? 24f : 18f;
        DrawBrokenWorldRune(spirit - new Vector2(0f, 5f), haloRadius,
            time * (state == ThunderSpiritState.Empowered ? .85f : .42f),
            state == ThunderSpiritState.Discharged ? deep : blue,
            gold,
            state == ThunderSpiritState.Empowered ? 11 : 8,
            state == ThunderSpiritState.Empowered ? 2f : 1f,
            .52f,
            form,
            .78f);

        if (distance > 34f)
        {
            Vector2 trailDirection = desired - _thunderSpiritVisualPosition;
            if (trailDirection.LengthSquared() > .01f)
                trailDirection.Normalize();
            for (int trail = 1; trail <= 3; trail++)
            {
                Vector2 fragment = spirit - trailDirection *
                    (11f + trail * 7f) + perpendicular *
                    (trail % 2 == 0 ? 4f : -4f);
                DrawWorldLine(fragment, fragment + trailDirection * 6f,
                    1f, trail == 2 ? gold : deep);
            }
        }

        if (attacking)
        {
            Vector2 target = gameSession.ThunderSpiritLastTarget;
            Vector2 first = Vector2.Lerp(leftHand, target, .30f) +
                perpendicular * 5f;
            Vector2 second = Vector2.Lerp(leftHand, target, .64f) -
                perpendicular * 7f;
            DrawWorldLine(leftHand, first, 4f,
                new Color(63, 129, 226, (int)(125f * form)));
            DrawWorldLine(first, second, 3f, blue);
            DrawWorldLine(second, target, 2f, white);
            DrawWorldLine(first, first + perpendicular * 10f, 1f, blue);
            DrawWorldLine(second, second - perpendicular * 8f, 1f, gold);
        }

        if (state == ThunderSpiritState.Dormant && dismiss > 0f)
        {
            for (int fragment = 0; fragment < 5; fragment++)
            {
                float side = fragment % 2 == 0 ? -1f : 1f;
                Vector2 start = spirit + new Vector2(
                    side * (4f + fragment * 2f),
                    -fragment * 7f);
                DrawWorldLine(start,
                    start + new Vector2(side * 3f, -7f - fragment),
                    1f,
                    fragment == 4 ? white : fragment % 2 == 0 ? gold : blue);
            }
        }
    }

    private void DrawArchmagePresence(
        Vector2 baseCenter,
        float opacity,
        float scale,
        Vector2 gestureTarget,
        float time)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        if (opacity <= .01f)
            return;
        Color spectral = new(63, 88, 158, (int)(82f * opacity));
        Color edge = new(100, 128, 205, (int)(128f * opacity));
        Color purple = new(116, 76, 190, (int)(105f * opacity));
        Color gold = new(224, 184, 76, (int)(155f * opacity));
        Color eyes = new(231, 244, 255, (int)(205f * opacity));
        float drift = MathF.Sin(time * .72f) * 2f;
        Vector2 head = baseCenter + new Vector2(0f, -104f * scale + drift);
        Vector2 shoulders = baseCenter + new Vector2(0f, -82f * scale);
        Vector2 robe = baseCenter + new Vector2(0f, -18f * scale);

        DrawWorldLine(shoulders - new Vector2(25f * scale, 0f),
            shoulders + new Vector2(25f * scale, 0f),
            15f * scale, spectral);
        DrawWorldLine(shoulders, robe,
            24f * scale, spectral);
        DrawWorldLine(shoulders - new Vector2(18f * scale, 0f),
            robe - new Vector2(27f * scale, -4f),
            8f * scale, purple);
        DrawWorldLine(shoulders + new Vector2(18f * scale, 0f),
            robe + new Vector2(27f * scale, -4f),
            8f * scale, edge);
        DrawWorldLine(robe - new Vector2(12f * scale, 0f),
            baseCenter - new Vector2(28f * scale, 0f),
            5f * scale, spectral);
        DrawWorldLine(robe + new Vector2(12f * scale, 0f),
            baseCenter + new Vector2(28f * scale, 0f),
            4f * scale, spectral);

        DrawWorldLine(head + new Vector2(0f, -13f * scale),
            head + new Vector2(10f * scale, 1f), 5f * scale, edge);
        DrawWorldLine(head + new Vector2(10f * scale, 1f),
            head + new Vector2(0f, 13f * scale), 5f * scale, spectral);
        DrawWorldLine(head + new Vector2(0f, 13f * scale),
            head + new Vector2(-10f * scale, 1f), 5f * scale, purple);
        DrawWorldLine(head + new Vector2(-10f * scale, 1f),
            head + new Vector2(0f, -13f * scale), 4f * scale, edge);
        DrawWorldLine(head - new Vector2(7f * scale, 1f),
            head - new Vector2(2f * scale, 1f), 2f, eyes);
        DrawWorldLine(head + new Vector2(2f * scale, -1f),
            head + new Vector2(7f * scale, -1f), 2f, eyes);

        DrawBrokenWorldRune(head, 28f * scale,
            time * .17f, gold, edge,
            13, 2f, .58f, opacity, 1f);
        DrawBrokenWorldRune(head, 20f * scale,
            -.35f - time * .11f, purple, gold,
            9, 1f, .52f, opacity, 1f);

        Vector2 raisedShoulder = shoulders + new Vector2(15f * scale, 0f);
        Vector2 elbow = Vector2.Lerp(raisedShoulder, gestureTarget, .52f) +
            new Vector2(-5f * scale, -17f * scale);
        DrawWorldLine(raisedShoulder, elbow, 9f * scale, spectral);
        DrawWorldLine(elbow, gestureTarget, 7f * scale, edge);
        Vector2 handDirection = gestureTarget - elbow;
        if (handDirection.LengthSquared() < .01f)
            handDirection = Vector2.UnitX;
        else
            handDirection.Normalize();
        Vector2 handNormal = new(-handDirection.Y, handDirection.X);
        for (int finger = -2; finger <= 2; finger++)
        {
            Vector2 root = gestureTarget + handNormal * finger * 2.2f;
            DrawWorldLine(root,
                root + handDirection * (8f + (2 - Math.Abs(finger)) * 2f),
                1f, finger == 0 ? gold : edge);
        }

        for (int fragment = 0; fragment < 5; fragment++)
        {
            float side = fragment % 2 == 0 ? -1f : 1f;
            Vector2 start = robe + new Vector2(
                side * (11f + fragment * 5f),
                8f + fragment * 8f);
            DrawWorldLine(start,
                start + new Vector2(side * 5f, 6f + fragment),
                1f,
                fragment % 3 == 0 ? gold : spectral);
        }
    }

    private void DrawFourthVeilBackground(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.Player.Combat.IsFourthVeilVisualActive)
            return;
        float strength = gameSession.Player.Combat.FourthVeilVisualStrength;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        Rectangle body = gameSession.Player.Bounds;
        Vector2 center = body.Center.ToVector2();
        for (int layer = 3; layer >= 1; layer--)
        {
            Rectangle separation = body;
            separation.Inflate(9 + layer * 8, 7 + layer * 7);
            _spriteBatch.Draw(_pixel, separation,
                new Color(27, 24, 68,
                    (int)(strength * (8f + layer * 5f))));
        }

        bool entering = gameSession.Player.Combat.IsFourthVeilActive &&
            gameSession.Player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffFourthVeil;
        bool exiting = !gameSession.Player.Combat.IsFourthVeilActive;
        float presence = entering
            ? MathF.Sin(strength * MathF.PI)
            : exiting
                ? MathF.Sin((1f - strength) * MathF.PI)
                : 0f;
        if (presence > .02f)
        {
            DrawArchmagePresence(
                center + new Vector2(0f, 20f),
                presence * .54f,
                .82f,
                center + new Vector2(
                    gameSession.Player.Facing == FacingDirection.Left
                        ? -16f : 16f,
                    -35f),
                time);
        }

        float bend = 7f + strength * 10f;
        for (int fracture = 0; fracture < 5; fracture++)
        {
            float angle = MathHelper.TwoPi * fracture / 5f + time * .08f;
            Vector2 start = center + new Vector2(
                MathF.Cos(angle) * (26f + fracture * 2f),
                MathF.Sin(angle) * (35f + fracture));
            Vector2 middle = start + new Vector2(
                MathF.Cos(angle + .55f) * bend,
                MathF.Sin(angle + .55f) * bend);
            DrawWorldLine(start, middle, 2f,
                new Color(92, 74, 177, (int)(125f * strength)));
            DrawWorldLine(middle,
                middle + new Vector2(
                    MathF.Cos(angle - .35f) * 6f,
                    MathF.Sin(angle - .35f) * 6f),
                1f,
                new Color(91, 147, 222, (int)(145f * strength)));
        }
    }

    private void DrawFourthVeilOverlay(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.Player.Combat.IsFourthVeilVisualActive)
            return;
        float strength = gameSession.Player.Combat.FourthVeilVisualStrength;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        float pulse = .5f + .5f * MathF.Sin(time * 5.5f);
        Rectangle body = gameSession.Player.Bounds;
        Vector2 center = body.Center.ToVector2();
        Color purple = new(116, 82, 203,
            (int)((92f + pulse * 45f) * strength));
        Color blue = new(84, 145, 224,
            (int)((105f + pulse * 38f) * strength));
        Color gold = new(220, 181, 78, (int)(105f * strength));
        Color white = new(232, 244, 255, (int)(125f * strength));

        for (int contour = -1; contour <= 1; contour += 2)
        {
            float x = contour * (body.Width * .58f + pulse * 2f);
            DrawWorldLine(center + new Vector2(x, -body.Height * .42f),
                center + new Vector2(x + contour * 4f, -5f),
                2f, contour < 0 ? purple : blue);
            DrawWorldLine(center + new Vector2(x + contour * 4f, -5f),
                center + new Vector2(x, body.Height * .42f),
                1f, contour < 0 ? blue : purple);
        }

        if (MathF.Abs(gameSession.Player.Velocity.X) > 24f)
        {
            float travel = MathF.Sign(gameSession.Player.Velocity.X);
            for (int echo = 1; echo <= 3; echo++)
            {
                Vector2 foot = center + new Vector2(
                    -travel * (10f + echo * 10f),
                    body.Height * .46f - echo % 2 * 3f);
                DrawWorldLine(foot - new Vector2(5f, 0f), foot,
                    1f, echo == 2 ? gold : purple);
                DrawWorldLine(foot, foot + new Vector2(
                    -travel * 5f, -5f), 1f, blue);
                DrawWorldLine(foot + new Vector2(-travel * 5f, -5f),
                    foot + new Vector2(-travel * 2f, -9f), 1f, purple);
            }
        }

        float remaining = gameSession.Player.Combat.FourthVeilRatio;
        int visibleMarks = gameSession.Player.Combat.IsFourthVeilActive
            ? Math.Clamp((int)MathF.Ceiling(remaining * 12f), 0, 12)
            : 0;
        float timerRadius = MathF.Max(body.Width, body.Height) * .58f;
        for (int mark = 0; mark < 12; mark++)
        {
            float angle = -MathHelper.PiOver2 +
                MathHelper.TwoPi * mark / 12f;
            Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 start = center + radial * timerRadius;
            DrawWorldLine(start, start + radial * (mark % 3 == 0 ? 6f : 3f),
                mark < visibleMarks ? 2f : 1f,
                mark < visibleMarks
                    ? (remaining < .17f && mark % 2 == 0 ? white : gold)
                    : new Color(65, 58, 91, 70));
        }

        if (!gameSession.Player.Combat.IsFourthVeilActive)
        {
            float convergence = 1f - strength;
            DrawBrokenWorldRune(center, 31f - convergence * 12f,
                -time * 1.3f, purple, gold, 10, 2f, .55f,
                strength, 1f);
            DrawWorldLine(center - new Vector2(18f * strength, 0f),
                center + new Vector2(18f * strength, 0f), 2f, white);
        }
    }

    private void DrawAncientStaffUltimateCinematic(
        GameSession gameSession,
        GameTime gameTime)
    {
        float ratio = gameSession.Player.Combat
            .AncientStaffUltimateCinematicRatio;
        if (ratio <= 0f && !gameSession.IsAncientStaffUltimateActive)
            return;
        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        if (ratio > 0f)
        {
            float enter = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(ratio / .18f, 0f, 1f));
            float leave = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((1f - ratio) / .13f, 0f, 1f));
            float visibility = enter * leave;
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(3, 6, 17, (int)(188f * visibility)));

            Vector2 runeCenter = new(width * .62f, height * .45f);
            DrawBrokenWorldRune(runeCenter, 142f,
                -.32f + ratio * .72f,
                new Color(84, 143, 229, (int)(185f * visibility)),
                new Color(226, 184, 70, (int)(220f * visibility)),
                30, 4f, .68f, visibility, .72f);
            DrawBrokenWorldRune(runeCenter, 96f,
                .44f - ratio * 1.05f,
                new Color(124, 79, 198, (int)(175f * visibility)),
                new Color(225, 241, 255, (int)(195f * visibility)),
                21, 2f, .58f, visibility, .72f);
            for (int line = 0; line < 7; line++)
            {
                float y = height * (.23f + line * .075f);
                float offset = MathF.Sin(line * 1.47f) * width * .035f;
                DrawWorldLine(
                    new Vector2(width * .48f + offset, y),
                    new Vector2(width * (.90f - line * .018f),
                        y - height * .10f),
                    line % 3 == 0 ? 2f : 1f,
                    line % 2 == 0
                        ? new Color(73, 125, 210, (int)(105f * visibility))
                        : new Color(129, 82, 190, (int)(90f * visibility)));
            }

            Vector2 archmageBase = new(width * .69f, height * .75f);
            Vector2 gesture = new(width * .52f, height * .40f);
            DrawArchmagePresence(archmageBase,
                visibility * .80f,
                MathF.Max(1.45f, height / 470f),
                gesture,
                time * .32f);

            float portrait = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .12f) / .24f, 0f, 1f)) * leave;
            if (portrait > 0f)
            {
                float portraitScale = MathHelper.Lerp(3.2f, 3.65f, portrait);
                Vector2 portraitAnchor = new(width * .29f, height * .84f);
                _spriteBatch.End();
                _spriteBatch.Begin(
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: Matrix.CreateScale(portraitScale) *
                        Matrix.CreateTranslation(
                            portraitAnchor.X,
                            portraitAnchor.Y,
                            0f));
                _playerSpriteRenderer.DrawCinematicCutIn(
                    gameSession.Player,
                    Vector2.Zero);
                _spriteBatch.End();
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                Vector2 crystal = new(width * .43f, height * .30f);
                DrawBrokenWorldRune(crystal, 20f + ratio * 9f,
                    -ratio * 2.2f,
                    new Color(228, 245, 255, (int)(230f * portrait)),
                    new Color(224, 183, 73, (int)(205f * portrait)),
                    10, 2f, .56f, portrait, .72f);
            }

            if (ratio >= .38f)
                DrawCenteredDebugText(
                    "HEAVENFALL THUNDER GATE",
                    width / 2,
                    (int)(height * .80f),
                    new Color(229, 197, 96, (int)(245f * visibility)),
                    3);
            if (ratio >= .69f)
                DrawCenteredDebugText(
                    "RISE... AND BE JUDGED!",
                    width / 2,
                    (int)(height * .87f),
                    new Color(225, 241, 255, (int)(225f * visibility)),
                    2);
            return;
        }

        int stage = gameSession.AncientStaffUltimateStage;
        float stageRatio = gameSession.AncientStaffUltimateStageRatio;
        if (stage == 8)
        {
            float contact = GetAncientStaffContactRatio(gameSession);
            float impact = 1f - Math.Clamp(
                MathF.Abs(stageRatio - contact) / .085f,
                0f,
                1f);
            if (impact > 0f)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                    new Color(2, 5, 15, (int)(105f * impact)));
                Vector2 impactScreen = Vector2.Transform(
                    gameSession.AncientStaffEffectCenter,
                    gameSession.Camera.Transform);
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)impactScreen.X - 4, 0, 9, height),
                    new Color(225, 244, 255, (int)(190f * impact)));
                DrawWorldLine(
                    new Vector2(impactScreen.X - 28f, height * .12f),
                    new Vector2(impactScreen.X + 18f, height * .90f),
                    2f,
                    new Color(225, 184, 72, (int)(145f * impact)));
            }
        }
        else if (stage == 9)
        {
            float recovery = 1f - stageRatio;
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(13, 16, 35, (int)(28f * recovery)));
        }
    }

    private void DrawSpellbladeWorld(GameSession gameSession)
    {
        var networkRunes = new List<GroundRune>();
        foreach (GroundRune rune in gameSession.GroundRunes.Runes)
        {
            if (rune.State == GroundRuneState.Network)
                networkRunes.Add(rune);
        }
        networkRunes.Sort((left, right) =>
            left.Position.X.CompareTo(right.Position.X));

        for (int index = 0; index < networkRunes.Count - 1; index++)
            DrawRuneNetworkLink(
                networkRunes[index].Position,
                networkRunes[index + 1].Position);
        if (networkRunes.Count == 3)
            DrawRuneNetworkArc(
                networkRunes[0].Position,
                networkRunes[2].Position);

        foreach (GroundRune rune in gameSession.GroundRunes.Runes)
            DrawGroundRune(rune);

        foreach (RuneDetonationVisual visual in
            gameSession.GroundRunes.DetonationVisuals)
        {
            float radius = 9f + SpellbladeTuning.RuneBlastRadius *
                visual.Progress;
            int alpha = Math.Clamp(
                (int)MathF.Round(205f * (1f - visual.Progress)), 0, 255);
            DrawArcaneRing(
                visual.Position - new Vector2(0f, 8f),
                radius,
                new Color(166, 132, 213, alpha),
                12,
                2f);
        }

        WeaponTechnique technique = gameSession.Player.Combat.CurrentTechnique;
        if (gameSession.Player.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            technique == null)
            return;

        if (technique.Effect == WeaponTechniqueEffect.SpellbladeArcaneDominion)
        {
            float phase = (gameSession.Player.Combat.TechniqueStageIndex + 1) /
                4f;
            DrawArcaneRing(
                gameSession.Player.Position,
                SpellbladeTuning.DominionRadius * (.72f + phase * .28f),
                new Color(131, 105, 179, 105),
                24,
                2f);
        }
        else if (technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneDetonation &&
            gameSession.Player.Combat.IsAttackActive)
        {
            DrawArcaneRing(
                gameSession.Player.Position,
                SpellbladeTuning.ArcaneDetonationRadius,
                new Color(176, 150, 213, 115),
                18,
                2f);
        }
    }

    private void DrawGroundRune(GroundRune rune)
    {
        Vector2 center = rune.Position - new Vector2(0f, 6f);
        float half = SpellbladeTuning.RuneWidth / 2f;
        Color edge = rune.State switch
        {
            GroundRuneState.Network => new Color(173, 151, 220, 235),
            GroundRuneState.Awakened => new Color(211, 202, 231, 240),
            _ => new Color(119, 91, 161, 210)
        };
        Color core = rune.State == GroundRuneState.Active
            ? new Color(151, 125, 190, 175)
            : new Color(225, 220, 237, 215);
        Vector2 top = center + new Vector2(0f, -11f);
        Vector2 right = center + new Vector2(half, 0f);
        Vector2 bottom = center + new Vector2(0f, 5f);
        Vector2 left = center + new Vector2(-half, 0f);
        DrawWorldLine(top, right, 2f, edge);
        DrawWorldLine(right, bottom, 2f, edge);
        DrawWorldLine(bottom, left, 2f, edge);
        DrawWorldLine(left, top, 2f, edge);
        DrawWorldLine(center - new Vector2(7f, 0f),
            center + new Vector2(7f, 0f), 1f, core);
        DrawWorldLine(center - new Vector2(0f, 7f),
            center + new Vector2(0f, 4f), 1f, core);
    }

    private void DrawRuneNetworkLink(Vector2 start, Vector2 end)
    {
        start -= new Vector2(0f, 8f);
        end -= new Vector2(0f, 8f);
        DrawWorldLine(start, end, 7f, new Color(92, 70, 139, 65));
        DrawWorldLine(start, end, 2f, new Color(177, 154, 220, 205));
        Vector2 midpoint = Vector2.Lerp(start, end, .5f);
        DrawWorldLine(midpoint - new Vector2(5f, 5f),
            midpoint + new Vector2(5f, 5f), 1f,
            new Color(229, 222, 239, 210));
    }

    private void DrawRuneNetworkArc(Vector2 start, Vector2 end)
    {
        start -= new Vector2(0f, 9f);
        end -= new Vector2(0f, 9f);
        Vector2 previous = start;
        for (int segment = 1; segment <= 8; segment++)
        {
            float amount = segment / 8f;
            Vector2 point = Vector2.Lerp(start, end, amount);
            point.Y -= MathF.Sin(amount * MathF.PI) * 24f;
            DrawWorldLine(previous, point, 1f,
                new Color(145, 126, 196, 145));
            previous = point;
        }
    }

    private void DrawArcaneImprints(Rectangle bounds, int count, bool bound)
    {
        count = Math.Clamp(count, 0, SpellbladeTuning.MaximumImprints);
        int spacing = 11;
        int startX = bounds.Center.X - (count - 1) * spacing / 2;
        int y = bounds.Top - 12;
        for (int index = 0; index < count; index++)
        {
            Vector2 center = new(startX + index * spacing, y);
            Color color = new(185, 159, 220, 230);
            DrawWorldLine(center + new Vector2(0f, -4f),
                center + new Vector2(4f, 0f), 2f, color);
            DrawWorldLine(center + new Vector2(4f, 0f),
                center + new Vector2(0f, 4f), 2f, color);
            DrawWorldLine(center + new Vector2(0f, 4f),
                center + new Vector2(-4f, 0f), 2f, color);
            DrawWorldLine(center + new Vector2(-4f, 0f),
                center + new Vector2(0f, -4f), 2f, color);
        }
        if (bound)
        {
            DrawArcaneRing(bounds.Center.ToVector2(),
                MathF.Max(bounds.Width, bounds.Height) * .56f,
                new Color(137, 111, 184, 125), 12, 2f);
        }
    }

    private static bool IsDominionBindingVisible(
        GameSession gameSession,
        Vector2 target) =>
        gameSession.Player.Combat.CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.SpellbladeArcaneDominion &&
        gameSession.Player.Combat.TechniqueStageIndex >= 2 &&
        Vector2.DistanceSquared(gameSession.Player.Position, target) <=
            SpellbladeTuning.DominionRadius * SpellbladeTuning.DominionRadius;

    private void DrawArcaneRing(
        Vector2 center,
        float radius,
        Color color,
        int segments,
        float thickness)
    {
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int index = 1; index <= segments; index++)
        {
            float angle = MathHelper.TwoPi * index / segments;
            Vector2 next = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * .34f);
            DrawWorldLine(previous, next, thickness, color);
            previous = next;
        }
    }

    private void DrawBrokenWorldRune(
        Vector2 center,
        float radius,
        float rotation,
        Color primary,
        Color accent,
        int segments,
        float thickness,
        float arcFraction,
        float completion = 1f,
        float verticalScale = .72f)
    {
        completion = Math.Clamp(completion, 0f, 1f);
        int visibleSegments = Math.Clamp(
            (int)MathF.Ceiling(segments * completion),
            0,
            segments);
        for (int index = 0; index < visibleSegments; index++)
        {
            float startAngle = rotation + MathHelper.TwoPi *
                index / segments + .035f;
            float endAngle = rotation + MathHelper.TwoPi *
                (index + arcFraction) / segments;
            Vector2 start = center + new Vector2(
                MathF.Cos(startAngle) * radius,
                MathF.Sin(startAngle) * radius * verticalScale);
            Vector2 end = center + new Vector2(
                MathF.Cos(endAngle) * radius,
                MathF.Sin(endAngle) * radius * verticalScale);
            DrawWorldLine(start, end,
                index % 4 == 0 ? thickness + 1f : thickness,
                index % 2 == 0 ? primary : accent);
            if (index % 2 == 0)
            {
                Vector2 mark = center + new Vector2(
                    MathF.Cos(startAngle) * (radius + 3f),
                    MathF.Sin(startAngle) * (radius + 3f) * verticalScale);
                Vector2 direction = Vector2.Normalize(mark - center);
                DrawWorldLine(mark, mark + direction * (3f + index % 3),
                    1f, accent);
            }
        }
    }

    private void DrawRangerTrajectoryGuide(GameSession gameSession)
    {
        PlayerCharacter player = gameSession.Player;
        if (player.WeaponFamily != WeaponFamily.HunterBow ||
            !player.Combat.IsRangerTrajectoryAiming ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        if (effect is not WeaponTechniqueEffect.RangerSkyfallMarker and
            not WeaponTechniqueEffect.RangerFallingStar)
            return;

        Vector2 origin = player.Position + new Vector2(0f, -10f);
        float facing = player.Facing == FacingDirection.Left ? -1f : 1f;
        Vector2 direction;
        float speed;
        float gravity;
        float duration;

        if (effect == WeaponTechniqueEffect.RangerSkyfallMarker)
        {
            direction = player.Combat.RangerSkyfallDistance switch
            {
                SkyfallDistance.Near => Vector2.Normalize(
                    new Vector2(facing * .21f, -.978f)),
                SkyfallDistance.Far => Vector2.Normalize(
                    new Vector2(facing * .555f, -.832f)),
                _ => Vector2.Normalize(new Vector2(facing * .355f, -.935f))
            };
            speed = 650f;
            gravity = 780f;
            duration = 1.55f;
        }
        else
        {
            direction = Vector2.Normalize(new Vector2(
                facing * .48f,
                -.88f));
            speed = 690f;
            gravity = 940f;
            duration = 1.30f;
        }

        Vector2 velocity = direction * speed;
        Vector2 previous = origin;
        Vector2 final = origin;
        const int segments = 14;
        for (int index = 1; index <= segments; index++)
        {
            float time = duration * index / segments;
            Vector2 point = origin + velocity * time +
                new Vector2(0f, gravity * time * time * .5f);
            if (!SideScrollingCollision.IsPositionFree(
                    point,
                    new Vector2(10f, 6f),
                    gameSession.CurrentDungeon))
            {
                final = previous;
                break;
            }
            if (index % 2 == 1)
            {
                DrawWorldLine(
                    previous,
                    point,
                    1.5f,
                    new Color(221, 205, 148, 125));
            }
            previous = point;
            final = point;
        }

        Rectangle marker = new((int)final.X - 7, (int)final.Y - 3, 14, 6);
        DrawRectangleOutline(marker, 1, new Color(234, 213, 139, 175));
    }

    private void DrawRangerImpact(ProjectileImpact impact)
    {
        float radius = MathF.Max(12f, impact.Projectile.ImpactRadius);
        Rectangle outer = new(
            (int)(impact.Position.X - radius),
            (int)(impact.Position.Y - radius * .42f),
            (int)(radius * 2f),
            (int)(radius * .84f));
        DrawRectangleOutline(outer, 2, new Color(229, 186, 94, 170));
        DrawWorldLine(
            impact.Position + new Vector2(-radius * .55f, 0f),
            impact.Position + new Vector2(radius * .55f, 0f),
            3f,
            new Color(236, 218, 155, 185));
        DrawWorldLine(
            impact.Position + new Vector2(0f, -radius * .45f),
            impact.Position + new Vector2(0f, radius * .25f),
            2f,
            new Color(164, 222, 187, 165));
    }

    private void DrawSoulBoltImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        Color outer = new(112, 96, 153, 165);
        Color inner = new(186, 166, 211, 210);
        DrawWorldLine(center + new Vector2(-12f, 0f),
            center + new Vector2(12f, 0f), 2f, outer);
        DrawWorldLine(center + new Vector2(0f, -10f),
            center + new Vector2(0f, 10f), 2f, outer);
        DrawWorldLine(center + new Vector2(-7f, -7f),
            center + new Vector2(7f, 7f), 2f, inner);
        DrawWorldLine(center + new Vector2(-7f, 7f),
            center + new Vector2(7f, -7f), 2f, inner);
    }

    private void DrawSigilLanceImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        Color violet = new(151, 105, 181, 190);
        Color bronze = new(202, 162, 104, 205);
        DrawWorldLine(center + new Vector2(-13f, -8f),
            center + new Vector2(13f, 8f), 2f, violet);
        DrawWorldLine(center + new Vector2(-13f, 8f),
            center + new Vector2(13f, -8f), 2f, violet);
        DrawWorldLine(center + new Vector2(-8f, 0f),
            center + new Vector2(8f, 0f), 3f, bronze);
    }

    private void DrawJudgmentBoltImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        float radius = impact.Projectile.EmpoweredVisual ? 19f : 14f;
        Color decree = new(151, 98, 181, 205);
        Color gold = new(225, 190, 119, 225);
        DrawWorldLine(center + new Vector2(-radius, 0f),
            center + new Vector2(radius, 0f), 3f, decree);
        DrawWorldLine(center + new Vector2(0f, -radius),
            center + new Vector2(0f, radius), 3f, decree);
        DrawWorldLine(center + new Vector2(-radius * .6f, -radius * .6f),
            center + new Vector2(radius * .6f, radius * .6f), 2f, gold);
        DrawWorldLine(center + new Vector2(-radius * .6f, radius * .6f),
            center + new Vector2(radius * .6f, -radius * .6f), 2f, gold);
    }

    private void DrawDominionArcImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        float radius = impact.Projectile.EmpoweredVisual ? 24f : 18f;
        Color violet = new(132, 75, 157, 210);
        Color gold = new(230, 190, 111, 230);
        DrawWorldLine(center + new Vector2(-radius, -7f),
            center + new Vector2(radius, 7f), 4f, violet);
        DrawWorldLine(center + new Vector2(-radius, 7f),
            center + new Vector2(radius, -7f), 3f, gold);
        DrawWorldLine(center + new Vector2(0f, -radius * .7f),
            center + new Vector2(0f, radius * .7f), 2f, violet);
    }

    private void DrawVowshardImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        Color stone = new(112, 105, 96, 210);
        Color mark = new(170, 123, 186, 195);
        for (int index = 0; index < 5; index++)
        {
            float angle = index * MathHelper.TwoPi / 5f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            DrawWorldLine(center + direction * 3f,
                center + direction * (9f + index * 1.5f),
                index % 2 == 0 ? 3f : 2f,
                index == 2 ? mark : stone);
        }
    }

    private void DrawAbyssScriptImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        Color glyph = new(178, 127, 194, 200);
        DrawWorldLine(center + new Vector2(-13, -9),
            center + new Vector2(12, -9), 2f, glyph);
        DrawWorldLine(center + new Vector2(12, -9),
            center + new Vector2(-9, 9), 2f, glyph);
        DrawWorldLine(center + new Vector2(-9, 9),
            center + new Vector2(14, 9), 2f, glyph);
    }

    private void DrawEnemyArrowImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        if (impact.Projectile.IsEmberArrow)
        {
            DrawWorldLine(center + new Vector2(-8f, -5f),
                center + new Vector2(5f, 4f), 3f,
                new Color(230, 91, 39, 210));
            DrawWorldLine(center + new Vector2(-4f, 6f),
                center + new Vector2(6f, -5f), 2f,
                new Color(247, 167, 68, 220));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)center.X - 8, (int)center.Y + 2, 4, 3),
                new Color(115, 72, 53, 150));
            return;
        }
        bool crossbowBolt =
            impact.Projectile.Type == ProjectileType.CrossbowBolt;
        Color spark = crossbowBolt
            ? impact.Projectile.EmpoweredVisual
                ? new Color(190, 164, 112, 220)
                : new Color(157, 139, 105, 205)
            : impact.Projectile.EmpoweredVisual
                ? new Color(104, 160, 177, 205)
                : new Color(151, 128, 91, 195);
        DrawWorldLine(center + new Vector2(-7f, -5f),
            center + new Vector2(4f, 3f), 2f, spark);
        DrawWorldLine(center + new Vector2(-3f, 5f),
            center + new Vector2(5f, -4f), 1.5f,
            new Color(175, 167, 143, 175));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)center.X - 5, (int)center.Y + 2, 3, 2),
            crossbowBolt
                ? new Color(118, 101, 77, 175)
                : new Color(119, 113, 101, 160));
    }

    private void DrawPlagueFlaskImpact(ProjectileImpact impact)
    {
        Vector2 center = impact.Position;
        DrawWorldLine(center + new Vector2(-9f, -3f),
            center + new Vector2(-2f, 4f), 2f,
            new Color(117, 122, 88, 185));
        DrawWorldLine(center + new Vector2(1f, -5f),
            center + new Vector2(7f, 3f), 2f,
            new Color(84, 89, 67, 180));
        DrawWorldLine(center + new Vector2(-3f, 3f),
            center + new Vector2(10f, 1f), 1.5f,
            new Color(142, 126, 84, 165));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)center.X - 12, (int)center.Y + 2, 5, 2),
            new Color(124, 106, 76, 135));
    }

    private void DrawWebPatch(WebPatch patch)
    {
        Rectangle bounds = patch.Bounds;
        Vector2 center = bounds.Center.ToVector2();
        Color faint = new(190, 207, 192, 30);
        Color strand = new(218, 228, 220, 115);
        _spriteBatch.Draw(_pixel, bounds, faint);
        DrawWorldLine(new Vector2(bounds.Left + 3, bounds.Bottom - 3),
            new Vector2(bounds.Right - 3, bounds.Top + 4), 1.5f, strand);
        DrawWorldLine(new Vector2(bounds.Left + 3, bounds.Top + 5),
            new Vector2(bounds.Right - 3, bounds.Bottom - 3), 1.5f, strand);
        DrawWorldLine(new Vector2(bounds.Left + 2, center.Y),
            new Vector2(bounds.Right - 2, center.Y), 2f, strand);
        DrawWorldLine(center,
            new Vector2(bounds.Left + 12, bounds.Top + 2), 1f, strand);
        DrawWorldLine(center,
            new Vector2(bounds.Right - 12, bounds.Top + 2), 1f, strand);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Left + 12, bounds.Top + 4,
                bounds.Width - 24, 2),
            new Color(230, 236, 229, 90));
    }

    private void DrawPlagueCloud(PlagueCloud cloud, GameTime gameTime)
    {
        Rectangle bounds = cloud.Bounds;
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 4.1f +
            cloud.Position.X * .013f);

        if (cloud.State == PlagueCloudState.Warning)
        {
            int inset = (int)MathF.Round((1f - cloud.PhaseProgress) * 8f);
            var warning = new Rectangle(
                bounds.X + inset,
                bounds.Y + inset / 3,
                Math.Max(2, bounds.Width - inset * 2),
                Math.Max(2, bounds.Height - inset * 2 / 3));
            DrawRectangleOutline(
                warning,
                2,
                cloud.IsCollapseCloud
                    ? new Color(151, 122, 75, 185)
                    : new Color(137, 132, 82, 165));
            DrawWorldLine(
                new Vector2(warning.Left + 8, warning.Bottom - 2),
                new Vector2(warning.Right - 8, warning.Bottom - 2),
                2f,
                new Color(106, 103, 72, 130));
            return;
        }

        _spriteBatch.Draw(
            _pixel,
            bounds,
            new Color(78, 82, 58, 42 + (int)(pulse * 13f)));
        DrawRectangleOutline(bounds, 2, new Color(118, 120, 76, 125));
        DrawWorldLine(
            new Vector2(bounds.Left + 5, bounds.Bottom - 3),
            new Vector2(bounds.Right - 5, bounds.Bottom - 3),
            3f,
            new Color(91, 87, 61, 140));

        for (int index = 0; index < 7; index++)
        {
            float phase = (float)gameTime.TotalGameTime.TotalSeconds *
                (.55f + index * .07f) + index * 1.27f;
            float x = bounds.Center.X + MathF.Sin(phase * 1.7f) *
                (bounds.Width * .38f);
            float rise = (phase * 13f) % Math.Max(8f, bounds.Height - 5f);
            float y = bounds.Bottom - 5f - rise;
            int size = index % 3 == 0 ? 4 : 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle((int)x, (int)y, size, size),
                index % 2 == 0
                    ? new Color(124, 128, 82, 100)
                    : new Color(93, 98, 70, 90));
        }
    }

    private void DrawWorldLine(
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            from,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawRootHazard(RootHazard hazard)
    {
        Rectangle bounds = hazard.Bounds;

        if (hazard.VisualKind == RootHazardVisualKind.CommandSeal)
        {
            DrawCommandSeal(hazard, bounds);
            return;
        }

        if (hazard.VisualKind == RootHazardVisualKind.BindingSigil)
        {
            DrawBindingSigil(hazard, bounds);
            return;
        }

        if (hazard.VisualKind is RootHazardVisualKind.DecreeZone or
            RootHazardVisualKind.BindingWrit or
            RootHazardVisualKind.FinalVerdictSeal)
        {
            DrawJudicialSeal(hazard, bounds);
            return;
        }

        if (hazard.VisualKind is RootHazardVisualKind.AbyssalPillar or
            RootHazardVisualKind.DominionLane or
            RootHazardVisualKind.EdictSeal)
        {
            DrawMalacharHazard(hazard, bounds);
            return;
        }

        if (hazard.VisualKind is RootHazardVisualKind.AshTrail or
            RootHazardVisualKind.CinderZone)
        {
            DrawAshHazard(hazard, bounds);
            return;
        }

        if (hazard.Owner is AncientTreant)
        {
            DrawAncientTreantRootHazard(hazard, bounds);
            return;
        }

        if (hazard.Owner is CorruptedTreant && !hazard.IsTerrainRoot)
        {
            DrawCorruptedTreantRootHazard(hazard, bounds);
            return;
        }

        if (hazard.State == RootHazardState.Telegraph)
        {
            Color warning = hazard.IsTerrainRoot
                ? new Color(194, 92, 76, 75)
                : new Color(188, 66, 94, 82);
            _spriteBatch.Draw(_pixel, bounds, warning);
            DrawRectangleOutline(
                bounds,
                3,
                hazard.IsTerrainRoot
                    ? new Color(225, 126, 76, 185)
                    : new Color(222, 86, 124, 185));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 2, bounds.Y, 4, bounds.Height),
                new Color(238, 168, 112, 130));
            return;
        }

        Color rootColor = hazard.IsTerrainRoot
            ? new Color(91, 55, 35)
            : new Color(103, 58, 42);
        _spriteBatch.Draw(_pixel, bounds, new Color(72, 38, 31, 105));

        if (bounds.Width >= bounds.Height)
        {
            for (int x = bounds.X + 5; x < bounds.Right - 3; x += 13)
                _spriteBatch.Draw(_pixel, new Rectangle(x, bounds.Y - 6, 6, bounds.Height + 12), rootColor);
        }
        else
        {
            for (int y = bounds.Y + 5; y < bounds.Bottom - 3; y += 13)
                _spriteBatch.Draw(_pixel, new Rectangle(bounds.X - 6, y, bounds.Width + 12, 6), rootColor);
        }

        DrawRectangleOutline(bounds, 2, new Color(143, 72, 67, 190));
    }

    private void DrawJudicialSeal(RootHazard hazard, Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        bool binding = hazard.VisualKind == RootHazardVisualKind.BindingWrit;
        bool verdict = hazard.VisualKind == RootHazardVisualKind.FinalVerdictSeal;
        Color violet = binding
            ? new Color(107, 80, 151)
            : new Color(130, 78, 153);
        Color gold = verdict
            ? new Color(226, 181, 103)
            : new Color(184, 144, 86);
        byte fillAlpha = hazard.State == RootHazardState.Telegraph
            ? (byte)MathHelper.Lerp(28f, 92f, progress)
            : (byte)118;
        _spriteBatch.Draw(_pixel, bounds,
            new Color(violet.R, violet.G, violet.B, fillAlpha));
        DrawRectangleOutline(bounds, verdict ? 3 : 2,
            new Color(gold, hazard.State == RootHazardState.Telegraph
                ? (int)MathHelper.Lerp(110f, 230f, progress)
                : 220));

        int centerX = bounds.Center.X;
        int centerY = bounds.Center.Y;
        DrawWorldLine(new Vector2(bounds.Left + 8, centerY),
            new Vector2(bounds.Right - 8, centerY), verdict ? 3f : 2f,
            new Color(gold, 195));
        DrawWorldLine(new Vector2(centerX, bounds.Top + 3),
            new Vector2(centerX, bounds.Bottom - 3), 2f,
            new Color(violet, 225));
        if (binding)
        {
            DrawWorldLine(new Vector2(bounds.Left + 12, bounds.Top + 4),
                new Vector2(bounds.Right - 12, bounds.Bottom - 4), 2f,
                new Color(gold, 185));
            DrawWorldLine(new Vector2(bounds.Left + 12, bounds.Bottom - 4),
                new Vector2(bounds.Right - 12, bounds.Top + 4), 2f,
                new Color(gold, 185));
        }
        else
        {
            int spacing = verdict ? 22 : 28;
            for (int x = bounds.Left + 10; x < bounds.Right - 5; x += spacing)
            {
                DrawWorldLine(new Vector2(x, bounds.Top + 5),
                    new Vector2(x + 8, bounds.Bottom - 5), 2f,
                    new Color(gold, 170));
            }
        }
    }

    private void DrawMalacharHazard(RootHazard hazard, Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        bool pillar = hazard.VisualKind == RootHazardVisualKind.AbyssalPillar;
        bool lane = hazard.VisualKind == RootHazardVisualKind.DominionLane;
        Color violet = pillar
            ? new Color(111, 57, 139)
            : new Color(91, 50, 116);
        Color gold = hazard.VisualKind == RootHazardVisualKind.EdictSeal
            ? new Color(229, 187, 105)
            : new Color(190, 139, 79);
        byte alpha = hazard.State == RootHazardState.Telegraph
            ? (byte)MathHelper.Lerp(32f, 102f, progress)
            : (byte)145;
        _spriteBatch.Draw(_pixel, bounds,
            new Color(violet.R, violet.G, violet.B, alpha));
        DrawRectangleOutline(bounds, lane ? 3 : 2,
            new Color(gold, hazard.State == RootHazardState.Telegraph
                ? (int)MathHelper.Lerp(130f, 235f, progress)
                : 230));

        if (pillar && hazard.State == RootHazardState.Active)
        {
            int center = bounds.Center.X;
            DrawWorldLine(new Vector2(center - 12, bounds.Bottom),
                new Vector2(center - 4, bounds.Top), 8f,
                new Color(126, 73, 155, 220));
            DrawWorldLine(new Vector2(center + 12, bounds.Bottom),
                new Vector2(center + 4, bounds.Top), 6f,
                new Color(220, 174, 100, 205));
        }
        else
        {
            DrawWorldLine(new Vector2(bounds.Left + 8, bounds.Center.Y),
                new Vector2(bounds.Right - 8, bounds.Center.Y),
                lane ? 4f : 2f, new Color(gold, 205));
            int spacing = lane ? 38 : 22;
            for (int x = bounds.Left + 10; x < bounds.Right - 5; x += spacing)
            {
                DrawWorldLine(new Vector2(x, bounds.Top + 4),
                    new Vector2(x + 11, bounds.Bottom - 4), 2f,
                    new Color(violet, 225));
            }
        }
    }

    private void DrawAshHazard(RootHazard hazard, Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        bool cinder = hazard.VisualKind == RootHazardVisualKind.CinderZone;
        if (hazard.State == RootHazardState.Telegraph)
        {
            byte alpha = (byte)MathHelper.Lerp(44f, 112f, progress);
            _spriteBatch.Draw(
                _pixel,
                bounds,
                new Color((byte)111, (byte)70, (byte)54, alpha));
            DrawRectangleOutline(
                bounds,
                2,
                new Color(188, 104, 58, 175));
            for (int x = bounds.Left + 6; x < bounds.Right - 2; x += 12)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, bounds.Center.Y - 1, 6, 2),
                    new Color(215, 128, 70, 150));
            }
            return;
        }

        _spriteBatch.Draw(
            _pixel,
            bounds,
            cinder
                ? new Color(70, 51, 45, 125)
                : new Color(65, 55, 51, 104));
        DrawRectangleOutline(
            bounds,
            2,
            new Color(143, 74, 50, 178));
        int spacing = cinder ? 11 : 16;
        for (int x = bounds.Left + 5; x < bounds.Right - 3; x += spacing)
        {
            int height = 4 + Math.Abs(x / spacing) % (cinder ? 8 : 5);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, bounds.Bottom - height, 3, height),
                new Color(198, 78, 37, 176));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x + 1, bounds.Bottom - height - 2, 2, 3),
                new Color(231, 137, 60, 142));
        }
    }

    private void DrawCommandSeal(RootHazard hazard, Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        Color edge = hazard.State == RootHazardState.Telegraph
            ? new Color(
                (byte)181,
                (byte)142,
                (byte)76,
                (byte)MathHelper.Lerp(90f, 205f, progress))
            : new Color(165, 118, 64, 190);
        _spriteBatch.Draw(
            _pixel,
            bounds,
            hazard.State == RootHazardState.Telegraph
                ? new Color(91, 54, 47, 48)
                : new Color(82, 43, 42, 78));
        DrawRectangleOutline(bounds, 2, edge);
        int inset = Math.Max(4, bounds.Height / 4);
        DrawRectangleOutline(
            new Rectangle(
                bounds.X + inset,
                bounds.Y + inset / 2,
                Math.Max(2, bounds.Width - inset * 2),
                Math.Max(2, bounds.Height - inset)),
            2,
            new Color(210, 184, 118, 150));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Center.X - 2, bounds.Y + 3, 4,
                Math.Max(2, bounds.Height - 6)),
            new Color(126, 51, 45, 155));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 7, bounds.Center.Y - 1,
                Math.Max(2, bounds.Width - 14), 3),
            new Color(198, 159, 91, 125));
    }

    private void DrawBindingSigil(RootHazard hazard, Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        bool active = hazard.State == RootHazardState.Active;
        Color edge = active
            ? new Color(181, 116, 199, 210)
            : new Color(145, 91, 169,
                (int)MathHelper.Lerp(80f, 195f, progress));
        _spriteBatch.Draw(
            _pixel,
            bounds,
            active
                ? new Color(77, 43, 91, 92)
                : new Color(65, 42, 76, 45));
        DrawRectangleOutline(bounds, active ? 3 : 2, edge);
        int inset = Math.Max(5, bounds.Height / 3);
        DrawRectangleOutline(
            new Rectangle(
                bounds.X + inset,
                bounds.Y + 3,
                Math.Max(2, bounds.Width - inset * 2),
                Math.Max(2, bounds.Height - 6)),
            2,
            new Color(205, 169, 117, active ? 190 : 125));
        DrawWorldLine(
            new Vector2(bounds.Left + 14f, bounds.Center.Y),
            new Vector2(bounds.Center.X - 7f, bounds.Top + 2f),
            2f, edge);
        DrawWorldLine(
            new Vector2(bounds.Center.X + 7f, bounds.Top + 2f),
            new Vector2(bounds.Right - 14f, bounds.Center.Y),
            2f, edge);
        if (active)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X - 2, bounds.Top - 10, 4, 10),
                new Color(171, 119, 194, 150));
        }
    }

    private void DrawAncientTreantRootHazard(
        RootHazard hazard,
        Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        Color glow = new(83, 154, 91, 155);

        if (hazard.State == RootHazardState.Telegraph)
        {
            if (hazard.IsTerrainRoot && bounds.Height > bounds.Width)
            {
                byte verticalAlpha = (byte)MathHelper.Lerp(
                    30f,
                    105f,
                    progress);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(bounds.Center.X - 5, bounds.Y + 2,
                        10, bounds.Height - 4),
                    new Color((byte)63, (byte)105, (byte)65,
                        verticalAlpha));
                DrawWorldLine(
                    new Vector2(bounds.Center.X, bounds.Bottom - 3),
                    new Vector2(bounds.Center.X - 6, bounds.Center.Y),
                    3f,
                    glow);
                DrawWorldLine(
                    new Vector2(bounds.Center.X - 6, bounds.Center.Y),
                    new Vector2(bounds.Center.X + 4, bounds.Y + 3),
                    3f,
                    new Color(112, 177, 101, 175));

                for (int index = 0; index < 5; index++)
                {
                    int y = bounds.Y + 8 + index *
                        Math.Max(8, (bounds.Height - 16) / 5);
                    int width = (int)MathF.Round(3f + progress * 8f);
                    int x = index % 2 == 0
                        ? bounds.Center.X - width
                        : bounds.Center.X;
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, y, width, 5),
                        new Color(62, 50, 31, 220));
                }
                return;
            }

            int groundY = bounds.Bottom - 4;
            byte alpha = (byte)MathHelper.Lerp(30f, 105f, progress);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 2, groundY - 7,
                    bounds.Width - 4, 10),
                new Color((byte)63, (byte)105, (byte)65, alpha));
            DrawWorldLine(
                new Vector2(bounds.X + 4, groundY),
                new Vector2(bounds.Center.X - 6, groundY - 8),
                3f,
                glow);
            DrawWorldLine(
                new Vector2(bounds.Center.X - 6, groundY - 8),
                new Vector2(bounds.Right - 4, groundY - 3),
                3f,
                new Color(112, 177, 101, 175));

            int tips = hazard.IsTerrainRoot ? 4 : 5;
            for (int index = 0; index < tips; index++)
            {
                int x = bounds.X + 5 + index *
                    Math.Max(7, (bounds.Width - 10) / tips);
                int height = (int)MathF.Round(3f + progress *
                    (hazard.IsTerrainRoot ? 8f : 15f));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, groundY - height, 5, height),
                    new Color(62, 50, 31, 220));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x - 2, groundY - height - 3, 8, 3),
                    new Color(64, 94, 48, 175));
            }
            return;
        }

        float eruption = progress < 0.18f
            ? progress / 0.18f
            : progress > 0.72f
                ? 1f - (progress - 0.72f) / 0.28f
                : 1f;
        eruption = MathHelper.Clamp(eruption, 0f, 1f);
        _spriteBatch.Draw(_pixel, bounds,
            new Color(44, 61, 39, 80));

        if (hazard.IsTerrainRoot)
        {
            bool horizontal = bounds.Width >= bounds.Height;
            int count = horizontal ? 6 : 5;
            for (int index = 0; index < count; index++)
            {
                float offset = (index + 0.5f) / count;
                Vector2 root = horizontal
                    ? new Vector2(bounds.X + bounds.Width * offset,
                        bounds.Bottom)
                    : new Vector2(bounds.Center.X,
                        bounds.Y + bounds.Height * offset);
                Vector2 tip = horizontal
                    ? root + new Vector2((index % 2 == 0 ? -7f : 7f),
                        -bounds.Height * eruption)
                    : root + new Vector2(
                        (index % 2 == 0 ? -1f : 1f) *
                            bounds.Width * eruption,
                        -8f);
                DrawWorldLine(root, tip, 8f, new Color(62, 49, 31));
                DrawWorldLine(root, tip, 3f, new Color(68, 104, 54));
            }
            return;
        }

        int ground = bounds.Bottom - 3;
        for (int index = 0; index < 5; index++)
        {
            float x = bounds.X + 6f + index * (bounds.Width - 12f) / 4f;
            float height = (31f + (index % 2) * 17f) * eruption;
            Vector2 root = new(x, ground);
            Vector2 tip = new(x + (index % 2 == 0 ? -8f : 8f),
                ground - height);
            DrawWorldLine(root, tip, 9f, new Color(58, 45, 30));
            DrawWorldLine(root, tip, 4f, new Color(68, 102, 52));
        }
    }

    private void DrawCorruptedTreantRootHazard(
        RootHazard hazard,
        Rectangle bounds)
    {
        float progress = hazard.PhaseProgress;
        int groundY = bounds.Bottom - 5;

        if (hazard.State == RootHazardState.Telegraph)
        {
            byte glowAlpha = (byte)MathHelper.Lerp(30f, 105f, progress);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 3, groundY - 5,
                    bounds.Width - 6, 9),
                new Color((byte)91, (byte)37, (byte)63, glowAlpha));
            DrawRootLine(
                new Vector2(bounds.X + 6, groundY),
                new Vector2(bounds.Center.X - 7, groundY - 6),
                new Color(124, 52, 74, 175),
                3f);
            DrawRootLine(
                new Vector2(bounds.Center.X - 7, groundY - 6),
                new Vector2(bounds.Center.X + 5, groundY - 2),
                new Color(188, 63, 91, 190),
                2f);
            DrawRootLine(
                new Vector2(bounds.Center.X + 5, groundY - 2),
                new Vector2(bounds.Right - 5, groundY - 8),
                new Color(117, 48, 69, 170),
                3f);

            int tipHeight = (int)MathF.Round(3f + progress * 12f);
            for (int index = 0; index < 3; index++)
            {
                int x = bounds.X + 13 + index * 18;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, groundY - tipHeight,
                        4, tipHeight),
                    new Color(78, 48, 34, 210));
            }

            for (int index = 0; index < 4; index++)
            {
                int x = bounds.X + 7 + index * 15;
                int y = groundY - 3 - ((index + (int)(progress * 5f)) % 3) * 3;
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, y, 3, 3),
                    new Color(123, 82, 48, 145));
            }
            return;
        }

        float eruption = progress < 0.2f
            ? progress / 0.2f
            : progress > 0.68f
                ? 1f - (progress - 0.68f) / 0.32f
                : 1f;
        eruption = MathHelper.Clamp(eruption, 0f, 1f);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 2, groundY - 6,
                bounds.Width - 4, 10),
            new Color(72, 39, 31, 125));

        for (int index = 0; index < 4; index++)
        {
            int x = bounds.X + 8 + index * 15;
            int height = (int)MathF.Round((29f + index % 2 * 13f) * eruption);
            Vector2 root = new(x, groundY);
            Vector2 tip = new(x + (index % 2 == 0 ? -6f : 7f),
                groundY - height);
            DrawRootLine(root, tip, new Color(87, 51, 34), 7f);
            DrawRootLine(
                tip,
                tip + new Vector2(index % 2 == 0 ? -8f : 8f, 8f),
                new Color(113, 60, 46),
                4f);
        }
    }

    private void DrawRootLine(
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            from,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawTreasureChest(TreasureChest chest)
    {
        Rectangle bounds = chest.Bounds;
        Rectangle shadow = new(bounds.X + 3, bounds.Bottom - 3, bounds.Width - 6, 8);
        Rectangle body = new(bounds.X, bounds.Y + 13, bounds.Width, bounds.Height - 13);
        Color woodColor = chest.IsOpen
            ? new Color(112, 70, 38)
            : new Color(138, 83, 39);

        _spriteBatch.Draw(_pixel, shadow, new Color(14, 16, 18, 145));
        _spriteBatch.Draw(_pixel, body, woodColor);
        DrawRectangleOutline(body, 3, new Color(198, 155, 67));

        if (chest.IsOpen)
        {
            Rectangle openLid = new(bounds.X + 2, bounds.Y - 2, bounds.Width - 4, 12);
            _spriteBatch.Draw(_pixel, openLid, new Color(101, 62, 34));
            DrawRectangleOutline(openLid, 2, new Color(185, 143, 61));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 11, body.Y + 5, 22, 6),
                new Color(232, 202, 101, 175));
        }
        else
        {
            Rectangle lid = new(bounds.X, bounds.Y + 3, bounds.Width, 16);
            _spriteBatch.Draw(_pixel, lid, new Color(157, 95, 43));
            DrawRectangleOutline(lid, 3, new Color(205, 161, 69));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Center.X - 5, bounds.Y + 16, 10, 12),
                new Color(225, 183, 69));
        }

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.X + 6, body.Y + 3, 4, body.Height - 6),
            new Color(190, 145, 62));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(body.Right - 10, body.Y + 3, 4, body.Height - 6),
            new Color(190, 145, 62));
    }

    private void DrawAncientTreant(AncientTreant boss, GameTime gameTime)
    {
        Rectangle bounds = boss.Bounds;
        _wildForestApexSpriteRenderer.Draw(boss, gameTime);

        if (!boss.IsAlive)
            return;

        int barWidth = 126;
        int healthWidth = (int)((barWidth - 4) *
            (boss.CurrentHealth / (float)boss.MaxHealth));
        Rectangle healthBorder = new(
            (int)boss.Position.X - barWidth / 2,
            bounds.Y - 25,
            barWidth,
            11);
        _spriteBatch.Draw(_pixel, healthBorder, new Color(28, 18, 20));
        DrawRectangleOutline(healthBorder, 2, new Color(151, 112, 69));

        if (healthWidth > 0)
        {
            Color healthColor = boss.Phase switch
            {
                AncientTreantPhase.PhaseThree => new Color(220, 55, 63),
                AncientTreantPhase.PhaseTwo => new Color(181, 73, 57),
                _ => new Color(126, 93, 49)
            };
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(healthBorder.X + 2, healthBorder.Y + 2, healthWidth, 7),
                healthColor);
        }

        int poiseWidth = (int)((barWidth - 4) *
            (boss.CurrentPoise / boss.MaxPoise));
        Rectangle poiseBorder = new(
            healthBorder.X,
            healthBorder.Bottom + 2,
            barWidth,
            7);
        _spriteBatch.Draw(_pixel, poiseBorder, new Color(25, 20, 31));

        if (poiseWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(poiseBorder.X + 2, poiseBorder.Y + 2, poiseWidth, 3),
                new Color(166, 111, 196));
        }
    }

    private void DrawWorldLoot(WorldLoot loot)
    {
        Color rarityColor = GetRarityColor(loot.Item.Rarity);
        Rectangle glow = loot.Bounds;
        glow.Inflate(7, 7);
        Rectangle core = loot.Bounds;

        _spriteBatch.Draw(_pixel, glow, new Color(
            rarityColor.R,
            rarityColor.G,
            rarityColor.B,
            (byte)55));
        _spriteBatch.Draw(_pixel, core, new Color(22, 25, 29, 235));
        DrawRectangleOutline(core, 2, rarityColor);
        DrawEquipmentSymbol(loot.Item, core, rarityColor);
        DrawRarityMarkers(
            loot.Item.Rarity,
            core.Center.X,
            core.Bottom + 4,
            rarityColor,
            markerSize: 3);
    }

    private void DrawLoreNotePickup(
        LoreNotePickup pickup,
        GameTime gameTime)
    {
        Rectangle bounds = pickup.Bounds;
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 2.4f);
        if (pickup.Definition.Id == LoreCatalog.MysteriousPhotograph.Id)
        {
            DrawPhotographPickup(bounds, pulse);
            return;
        }

        Rectangle glow = bounds;
        glow.Inflate(7 + (int)(pulse * 2f), 5 + (int)(pulse * 2f));
        _spriteBatch.Draw(
            _pixel,
            glow,
            new Color(169, 137, 69, (int)(35 + pulse * 24)));
        _spriteBatch.Draw(_pixel, bounds, new Color(181, 159, 106));
        DrawRectangleOutline(bounds, 2, new Color(91, 66, 42));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, 2),
            new Color(91, 71, 49));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 5, bounds.Y + 10, bounds.Width - 8, 2),
            new Color(105, 79, 51));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 5, bounds.Y + 15, bounds.Width - 13, 2),
            new Color(105, 79, 51));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Right - 7, bounds.Bottom - 7, 5, 5),
            new Color(113, 42, 37));
    }

    private void DrawPhotographPickup(Rectangle bounds, float pulse)
    {
        Rectangle glow = bounds;
        glow.Inflate(8 + (int)(pulse * 3f), 7 + (int)(pulse * 2f));
        _spriteBatch.Draw(
            _pixel,
            glow,
            new Color(174, 207, 203, (int)(38 + pulse * 34)));
        _spriteBatch.Draw(_pixel, bounds, new Color(229, 221, 195));
        DrawRectangleOutline(bounds, 2, new Color(73, 80, 78));
        var image = new Rectangle(
            bounds.X + 4,
            bounds.Y + 4,
            bounds.Width - 8,
            Math.Max(5, bounds.Height - 11));
        _spriteBatch.Draw(_pixel, image, new Color(65, 86, 82));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(image.X + 3, image.Bottom - 5, image.Width - 6, 3),
            new Color(191, 128, 116));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.X + 7, bounds.Bottom - 4, bounds.Width - 14, 2),
            new Color(126, 112, 91));
    }

    private void DrawPigPhotoPopup()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        int panelWidth = Math.Min(viewport.Width - 48, 980);
        int panelHeight = Math.Min(viewport.Height - 48, 920);
        var panel = new Rectangle(
            screen.Center.X - panelWidth / 2,
            screen.Center.Y - panelHeight / 2,
            panelWidth,
            panelHeight);

        _spriteBatch.Draw(_pixel, screen, new Color(4, 5, 7, 224));
        _spriteBatch.Draw(_pixel, panel, new Color(19, 21, 24, 252));
        DrawRectangleOutline(panel, 3, new Color(174, 158, 117));

        string title = Loc.Get("notes.pig_photo.title");
        DrawDebugText(
            title,
            panel.X + 20,
            panel.Y + 18,
            new Color(237, 224, 184),
            FitDebugTextScale(title, panel.Width - 210, 2));
        string close = Loc.Get("photo.close");
        DrawDebugText(
            close,
            panel.Right - 20 - close.Length * 6,
            panel.Y + 20,
            new Color(145, 153, 155),
            1);

        var imageArea = new Rectangle(
            panel.X + 22,
            panel.Y + 54,
            panel.Width - 44,
            panel.Height - 112);
        int sourceWidth = _pigPhoto?.Width ?? 2;
        int sourceHeight = _pigPhoto?.Height ?? 3;
        Rectangle imageBounds = CalculateAspectFitBounds(
            sourceWidth,
            sourceHeight,
            imageArea);
        _spriteBatch.Draw(_pixel, imageBounds, new Color(11, 12, 13));
        if (_pigPhoto != null)
        {
            _spriteBatch.Draw(_pigPhoto, imageBounds, Color.White);
        }
        else
        {
            DrawMissingPhotograph(imageBounds);
        }
        DrawRectangleOutline(imageBounds, 2, new Color(207, 199, 176));

        string caption = Loc.Get("notes.pig_photo.content");
        DrawCenteredDebugText(
            caption,
            panel.Center.X,
            imageBounds.Bottom + 18,
            new Color(231, 226, 207),
            FitDebugTextScale(caption, panel.Width - 40, 2));
    }

    private void DrawMissingPhotograph(Rectangle bounds)
    {
        var paper = new Rectangle(
            bounds.Center.X - Math.Min(90, bounds.Width / 3),
            bounds.Center.Y - Math.Min(70, bounds.Height / 3),
            Math.Min(180, bounds.Width * 2 / 3),
            Math.Min(140, bounds.Height * 2 / 3));
        _spriteBatch.Draw(_pixel, paper, new Color(221, 216, 199));
        DrawRectangleOutline(paper, 3, new Color(89, 91, 86));
        var image = new Rectangle(
            paper.X + 14,
            paper.Y + 14,
            Math.Max(1, paper.Width - 28),
            Math.Max(1, paper.Height - 45));
        _spriteBatch.Draw(_pixel, image, new Color(63, 76, 72));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(image.X + 12, image.Bottom - 18,
                Math.Max(1, image.Width - 24), 8),
            new Color(184, 127, 116));
    }

    internal static Rectangle CalculateAspectFitBounds(
        int sourceWidth,
        int sourceHeight,
        Rectangle destination)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 ||
            destination.Width <= 0 || destination.Height <= 0)
        {
            return Rectangle.Empty;
        }

        float scale = MathF.Min(
            destination.Width / (float)sourceWidth,
            destination.Height / (float)sourceHeight);
        int width = Math.Max(1, (int)MathF.Floor(sourceWidth * scale));
        int height = Math.Max(1, (int)MathF.Floor(sourceHeight * scale));
        return new Rectangle(
            destination.Center.X - width / 2,
            destination.Center.Y - height / 2,
            width,
            height);
    }

    private void DrawInventoryOverlay(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 310,
            viewport.Height / 2 - 220,
            620,
            440);

        _spriteBatch.Draw(_pixel, screen, new Color(7, 9, 13, 190));
        _spriteBatch.Draw(_pixel, panel, new Color(22, 27, 34, 248));
        DrawRectangleOutline(panel, 4, new Color(122, 111, 91));
        DrawInventoryTab(
            new Rectangle(panel.X + 28, panel.Y + 17, 120, 30),
            "Q " + Loc.Get("inventory.weapons"),
            gameSession.ActiveInventoryTab == InventoryTab.Weapons);
        DrawInventoryTab(
            new Rectangle(panel.X + 156, panel.Y + 17, 138, 30),
            "E " + Loc.Get("inventory.equipment"),
            gameSession.ActiveInventoryTab == InventoryTab.Equipment);
        DrawInventoryTab(
            new Rectangle(panel.X + 302, panel.Y + 17, 100, 30),
            Loc.Get("inventory.notes"),
            gameSession.ActiveInventoryTab == InventoryTab.Notes);

        if (gameSession.ActiveInventoryTab == InventoryTab.Notes)
        {
            DrawNotesInventory(gameSession, panel);
            return;
        }

        const int slotSize = 50;
        const int slotGap = 8;
        const int columns = InventoryGridNavigation.ColumnCount;
        int inventoryX = panel.X + 28;
        int inventoryY = panel.Y + 57;

        int visibleSlotCount = Math.Max(
            Inventory.DefaultCapacity,
            gameSession.ActiveInventoryItems.Count);

        for (int index = 0; index < visibleSlotCount; index++)
        {
            int column = index % columns;
            int row = index / columns;
            var slotBounds = new Rectangle(
                inventoryX + column * (slotSize + slotGap),
                inventoryY + row * (slotSize + slotGap),
                slotSize,
                slotSize);
            EquipmentItem item = index < gameSession.ActiveInventoryItems.Count
                ? gameSession.ActiveInventoryItems[index]
                : null;
            bool isSelected = item != null &&
                index == gameSession.SelectedInventoryIndex;
            bool isEquipped = item != null && ReferenceEquals(
                item,
                gameSession.Player.EquippedItems.GetEquipped(item.Slot));
            DrawInventorySlot(slotBounds, item, isSelected, isEquipped);
        }

        if (gameSession.ActiveInventoryItems.Count == 0)
        {
            DrawDebugText(
                gameSession.ActiveInventoryEmptyMessage,
                inventoryX + 12,
                inventoryY + 20,
                new Color(132, 144, 153),
                scale: 1);
        }

        int equipmentX = panel.Right - 168;
        EquipmentSlot activeSlot = gameSession.ActiveInventoryTab ==
            InventoryTab.Weapons
                ? EquipmentSlot.Weapon
                : EquipmentSlot.Armor;
        DrawEquipmentSlot(
            new Rectangle(equipmentX, panel.Y + 57, 128, 118),
            activeSlot,
            gameSession.Player.EquippedItems.GetEquipped(activeSlot));

        DrawComparisonIndicator(
            gameSession.SelectedInventoryItem,
            gameSession.Player,
            panel.X + 270,
            panel.Y + 190);

        DrawSelectedItemIdentity(
            gameSession,
            panel.X + 300,
            panel.Y + 195);

        var controls = new Rectangle(panel.X + 28, panel.Bottom - 66, 267, 36);
        _spriteBatch.Draw(_pixel, controls, new Color(31, 38, 47));
        DrawRectangleOutline(controls, 2, new Color(77, 88, 98));
        DrawControlHints(
            controls,
            gameSession.ActiveInventoryTab == InventoryTab.Equipment);
    }

    private void DrawNotesInventory(GameSession gameSession, Rectangle panel)
    {
        Rectangle listPanel = new(
            panel.X + 28,
            panel.Y + 62,
            190,
            panel.Height - 142);
        Rectangle detailPanel = new(
            panel.X + 230,
            panel.Y + 62,
            panel.Width - 258,
            panel.Height - 142);
        _spriteBatch.Draw(_pixel, listPanel, new Color(27, 32, 39));
        _spriteBatch.Draw(_pixel, detailPanel, new Color(18, 23, 29));
        DrawRectangleOutline(listPanel, 2, new Color(75, 84, 94));
        DrawRectangleOutline(detailPanel, 2, new Color(75, 84, 94));
        DrawDebugText(
            Loc.Get("notes.collected"),
            listPanel.X + 10,
            listPanel.Y + 10,
            new Color(201, 183, 125),
            1);

        const int rowHeight = 23;
        int listTop = listPanel.Y + 31;
        int visibleCount = Math.Max(1, (listPanel.Bottom - listTop - 8) / rowHeight);
        int selectedIndex = gameSession.SelectedInventoryIndex;
        int firstVisible = Math.Max(0, selectedIndex - visibleCount + 1);
        int maximumFirst = Math.Max(0, gameSession.CollectedNotes.Count - visibleCount);
        firstVisible = Math.Min(firstVisible, maximumFirst);

        if (gameSession.CollectedNotes.Count == 0)
        {
            DrawDebugText(
                Loc.Get("inventory.no_notes"),
                listPanel.X + 10,
                listTop + 5,
                new Color(126, 137, 146),
                1);
        }
        else
        {
            int end = Math.Min(
                gameSession.CollectedNotes.Count,
                firstVisible + visibleCount);
            for (int index = firstVisible; index < end; index++)
            {
                LoreNote note = gameSession.CollectedNotes[index];
                Rectangle row = new(
                    listPanel.X + 7,
                    listTop + (index - firstVisible) * rowHeight,
                    listPanel.Width - 14,
                    rowHeight - 3);
                bool selected = index == selectedIndex;
                _spriteBatch.Draw(
                    _pixel,
                    row,
                    selected
                        ? new Color(58, 55, 43)
                        : new Color(23, 28, 34));
                if (selected)
                    DrawRectangleOutline(row, 1, new Color(205, 180, 106));
                DrawDebugText(
                    selected ? ">" : " ",
                    row.X + 4,
                    row.Y + 6,
                    new Color(222, 196, 117),
                    1);
                DrawDebugText(
                    note.Title,
                    row.X + 16,
                    row.Y + 6,
                    selected
                        ? new Color(231, 216, 165)
                        : new Color(159, 164, 158),
                    1);
                if (!note.IsRead)
                {
                    DrawDebugText(
                        Loc.Get("notes.unread"),
                        row.Right - 24,
                        row.Y + 6,
                        new Color(218, 156, 72),
                        1);
                }
            }
        }

        LoreNote selectedNote = gameSession.SelectedLoreNote;
        if (selectedNote != null)
            DrawLoreNoteDetail(selectedNote, detailPanel);

        Rectangle controls = new(
            panel.X + 28,
            panel.Bottom - 66,
            374,
            36);
        _spriteBatch.Draw(_pixel, controls, new Color(31, 38, 47));
        DrawRectangleOutline(controls, 2, new Color(77, 88, 98));
        DrawDebugText(
            Loc.Get("inventory.controls"),
            controls.X + 8,
            controls.Y + 7,
            new Color(156, 170, 181),
            1);
        DrawDebugText(
            string.Empty,
            controls.X + 8,
            controls.Y + 20,
            new Color(156, 170, 181),
            1);
    }

    private void DrawLoreNoteDetail(LoreNote note, Rectangle panel)
    {
        int x = panel.X + 14;
        int y = panel.Y + 12;
        int availableWidth = panel.Width - 28;
        DrawDebugText(
            note.Title,
            x,
            y,
            new Color(232, 213, 151),
            FitDebugTextScale(note.Title, availableWidth, 2));
        bool imageBacked = !string.IsNullOrWhiteSpace(note.ImageAssetKey);
        int contentY;
        if (imageBacked)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 28, availableWidth, 1),
                new Color(76, 72, 59));

            const int captionLineHeight = 14;
            int captionLineCount = Math.Min(note.ContentLines.Count, 5);
            int captionHeight = Math.Max(
                captionLineHeight,
                captionLineCount * captionLineHeight);
            int imageTop = y + 40;
            int imageBottom = panel.Bottom - 10 - captionHeight - 10;
            var imageArea = new Rectangle(
                x,
                imageTop,
                availableWidth,
                Math.Max(1, imageBottom - imageTop));
            Texture2D imageTexture = ResolveLoreNoteImage(note.ImageAssetKey);
            int sourceWidth = imageTexture?.Width ?? 2;
            int sourceHeight = imageTexture?.Height ?? 3;
            Rectangle imageBounds = CalculateAspectFitBounds(
                sourceWidth,
                sourceHeight,
                imageArea);

            _spriteBatch.Draw(_pixel, imageBounds, new Color(11, 12, 13));
            if (imageTexture != null)
                _spriteBatch.Draw(imageTexture, imageBounds, Color.White);
            else
                DrawMissingPhotograph(imageBounds);
            DrawRectangleOutline(imageBounds, 2, new Color(207, 199, 176));

            contentY = imageArea.Bottom + 10;
            for (int index = 0; index < captionLineCount; index++)
            {
                string line = note.ContentLines[index];
                DrawCenteredDebugText(
                    line,
                    panel.Center.X,
                    contentY,
                    new Color(202, 201, 184),
                    FitDebugTextScale(line, availableWidth, 1));
                contentY += captionLineHeight;
            }

            return;
        }
        else
        {
            DrawDebugText(
                Loc.Get("notes.source"),
                x,
                y + 27,
                new Color(124, 141, 147),
                1);
            DrawDebugText(
                note.Source,
                x,
                y + 40,
                new Color(174, 181, 174),
                FitDebugTextScale(note.Source, availableWidth, 1));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 56, availableWidth, 1),
                new Color(76, 72, 59));
            contentY = y + 69;
        }

        const int lineHeight = 14;
        foreach (string line in note.ContentLines)
        {
            if (contentY + 7 > panel.Bottom - 10)
                break;
            DrawDebugText(
                line,
                x,
                contentY,
                new Color(202, 201, 184),
                1);
            contentY += lineHeight;
        }
    }

    private Texture2D ResolveLoreNoteImage(string imageAssetKey)
    {
        return string.Equals(
            imageAssetKey,
            LoreCatalog.PigPhotoImageAssetKey,
            StringComparison.Ordinal)
                ? _pigPhoto
                : null;
    }

    private void DrawInventoryTab(
        Rectangle bounds,
        string label,
        bool isActive)
    {
        Color borderColor = isActive
            ? new Color(235, 207, 119)
            : new Color(75, 84, 94);
        Color textColor = isActive
            ? new Color(247, 232, 166)
            : new Color(128, 139, 148);
        _spriteBatch.Draw(
            _pixel,
            bounds,
            isActive ? new Color(54, 52, 42) : new Color(27, 32, 39));
        DrawRectangleOutline(bounds, isActive ? 3 : 2, borderColor);
        DrawDebugText(
            label,
            bounds.X + 9,
            bounds.Y + 10,
            textColor,
            scale: 1);

        if (isActive)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.X + 5, bounds.Bottom - 5, bounds.Width - 10, 3),
                new Color(235, 207, 119));
        }
    }

    private void DrawInventorySlot(
        Rectangle bounds,
        EquipmentItem item,
        bool isSelected,
        bool isEquipped)
    {
        Color borderColor = isSelected
            ? new Color(245, 230, 151)
            : isEquipped
                ? new Color(78, 182, 133)
                : new Color(83, 91, 99);
        _spriteBatch.Draw(
            _pixel,
            bounds,
            isSelected ? new Color(52, 55, 55) : new Color(30, 35, 42));
        DrawRectangleOutline(bounds, isSelected ? 4 : 2, borderColor);

        if (item == null)
            return;

        Color rarityColor = GetRarityColor(item.Rarity);
        var symbolBounds = new Rectangle(
            bounds.X + 12,
            bounds.Y + 8,
            bounds.Width - 24,
            bounds.Height - 21);
        DrawEquipmentSymbol(item, symbolBounds, rarityColor);
        DrawRarityMarkers(
            item.Rarity,
            bounds.Center.X,
            bounds.Bottom - 9,
            rarityColor,
            markerSize: 4);

        if (item.ArmorDefinition != null)
        {
            DrawDebugText(
                item.ArmorGradeLabel,
                bounds.X + 3,
                bounds.Y + 3,
                new Color(245, 230, 151),
                scale: 1);
        }

        if (isEquipped)
        {
            DrawDebugText(
                "E",
                bounds.Right - 9,
                bounds.Y + 3,
                new Color(111, 230, 166),
                scale: 1);
        }
    }

    private void DrawSelectedItemIdentity(
        GameSession gameSession,
        int x,
        int y)
    {
        EquipmentItem item = gameSession.SelectedInventoryItem;

        if (item == null)
            return;

        DrawDebugText(
            GetLocalizedEquipmentName(item),
            x,
            y,
            GetRarityColor(item.Rarity),
            scale: 1);

        string identity = item.WeaponDefinition != null
            ? $"WEAPON FAMILY: {item.WeaponDefinition.Family.ToDisplayName()}"
            : item.ArmorDefinition != null
                ? $"CLASS: {item.ArmorDefinition.Class} ARMOR"
                : $"EQUIPMENT SLOT: {item.Slot}";
        DrawDebugText(
            identity.ToUpperInvariant(),
            x,
            y + 13,
            new Color(187, 194, 199),
            scale: 1);

        DrawDebugText(
            item.ArmorDefinition == null
                ? $"TIER: {item.Tier}  RARITY: {item.Rarity}"
                : $"GRADE: {item.ArmorGradeLabel} RARITY: {item.Rarity}",
            x,
            y + 26,
            new Color(187, 194, 199),
            scale: 1);

        string stats = item.WeaponDefinition != null
            ? $"DAMAGE +{item.DamageBonus}"
            : item.ArmorDefinition != null
                ? $"HEALTH +{item.HealthBonus} DAMAGE TAKEN " +
                    $"{(int)MathF.Round(item.DamageTakenMultiplier * 100f)}%"
                : $"SLOT: {item.Slot}";
        DrawDebugText(
            stats,
            x,
            y + 39,
            new Color(161, 190, 174),
            scale: 1);
        DrawDebugText(
            gameSession.IsSelectedInventoryItemEquipped
                ? "STATUS: EQUIPPED"
                : "STATUS: UNEQUIPPED",
            x,
            y + 52,
            gameSession.IsSelectedInventoryItemEquipped
                ? new Color(111, 230, 166)
                : new Color(137, 149, 158),
            scale: 1);

        if (item.WeaponDefinition != null)
        {
            if (GameSession.DeveloperModeEnabled)
            {
                DrawDebugText(
                    $"ID: {item.WeaponId}",
                    x,
                    y + 65,
                    new Color(104, 119, 130),
                    scale: 1);
            }
            return;
        }

        if (item.ArmorDefinition == null)
            return;

        string fusionStatus = gameSession.IsFusionConfirmationPending
            ? $"FUSE 3X {item.ArmorGradeLabel} TO " +
                ArmorGradeRules.ToDisplayName(
                    ArmorGradeRules.Next(item.ArmorGrade))
            : item.ArmorGrade == ArmorGrade.C5
            ? "MAX GRADE"
            : gameSession.CanFuseSelectedArmor
                ? $"FUSE AVAILABLE: 3 TO " +
                    ArmorGradeRules.ToDisplayName(
                        ArmorGradeRules.Next(item.ArmorGrade))
                : $"FUSION: {gameSession.SelectedFusionMaterialCount}/3";
        DrawDebugText(
            fusionStatus,
            x,
            y + 65,
            gameSession.CanFuseSelectedArmor
                ? new Color(245, 205, 93)
                : new Color(137, 149, 158),
            scale: 1);

        if (gameSession.IsFusionConfirmationPending)
        {
            DrawDebugText(
                "ENTER CONFIRM  ESC CANCEL",
                x,
                y + 78,
                new Color(224, 229, 232),
                scale: 1);
        }
    }

    private void DrawEquipmentSlot(
        Rectangle bounds,
        EquipmentSlot slot,
        EquipmentItem item)
    {
        _spriteBatch.Draw(_pixel, bounds, new Color(27, 34, 40));
        DrawRectangleOutline(bounds, 3, new Color(78, 155, 123));

        var slotBadge = new Rectangle(bounds.X + 8, bounds.Y + 8, 30, 30);
        _spriteBatch.Draw(_pixel, slotBadge, new Color(19, 24, 29));
        DrawRectangleOutline(slotBadge, 2, new Color(91, 111, 107));
        DrawEquipmentSymbol(slot, slotBadge, new Color(125, 163, 153));

        if (item == null)
        {
            var empty = new Rectangle(
                bounds.X + 40,
                bounds.Y + 46,
                48,
                48);
            DrawRectangleOutline(empty, 2, new Color(59, 68, 74));
            return;
        }

        Color rarityColor = GetRarityColor(item.Rarity);
        var itemBounds = new Rectangle(
            bounds.Center.X - 25,
            bounds.Y + 40,
            50,
            53);
        DrawEquipmentSymbol(item, itemBounds, rarityColor);
        DrawRarityMarkers(
            item.Rarity,
            bounds.Center.X,
            bounds.Bottom - 16,
            rarityColor,
            markerSize: 5);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bounds.Right - 19, bounds.Y + 10, 9, 9),
            new Color(80, 199, 126));
    }

    private void DrawEquipmentSymbol(
        EquipmentItem item,
        Rectangle bounds,
        Color color)
    {
        if (item?.WeaponDefinition != null)
        {
            int centerX = bounds.Center.X;
            int top = bounds.Y + 3;
            int height = Math.Max(8, bounds.Height - 14);

            if (item.WeaponDefinition.Family == WeaponFamily.DualDaggers)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 10, top + 2, 4, height - 2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX + 6, top + 2, 4, height - 2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 14, top + height - 4, 12, 4), new Color(173, 130, 70));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX + 2, top + height - 4, 12, 4), new Color(173, 130, 70));
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.Bow)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 10, top + 3, 4, height - 1), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top, 7, 4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top + height, 7, 4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX, top + 2, 2, height + 1), new Color(211, 205, 177));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 4, top + height / 2, 18, 2), new Color(173, 130, 70));
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.ArcaneStaff)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top + 6, 5, height + 4), new Color(105, 68, 45));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, top, 15, 12), new Color(45, 35, 57));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 4, top + 3, 9, 6), color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.AbyssRing)
            {
                int size = Math.Min(30, bounds.Width - 8);
                var outer = new Rectangle(
                    centerX - size / 2,
                    bounds.Center.Y - size / 2,
                    size,
                    size);
                DrawRectangleOutline(outer, 4, color);
                var inner = outer;
                inner.Inflate(-8, -8);
                DrawRectangleOutline(inner, 2, new Color(27, 23, 31));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, outer.Top - 4, 5, 8),
                    new Color(255, 221, 94));
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.Scythe)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, top + 5, 5, height + 6),
                    new Color(67, 42, 74));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX + 1, top - 1, 13, 5), color);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX + 10, top - 1, 5, 13), color);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX + 5, top + 8, 9, 4), color);
                return;
            }

            int bladeWidth = item.WeaponDefinition.Family == WeaponFamily.GreatSword
                ? 9
                : 5;
            int guardWidth = item.WeaponDefinition.Family == WeaponFamily.GreatSword
                ? 23
                : 17;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - bladeWidth / 2, top, bladeWidth, height),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - guardWidth / 2, top + height - 4, guardWidth, 4),
                new Color(173, 130, 70));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, top + height, 5, 9),
                new Color(105, 68, 45));
            return;
        }

        if (item?.ArmorDefinition != null)
        {
            int inset = item.ArmorDefinition.Class switch
            {
                ArmorClass.Light => 10,
                ArmorClass.Heavy => 3,
                _ => 7
            };
            int shoulder = item.ArmorDefinition.Class switch
            {
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 8,
                _ => 5
            };
            var chest = new Rectangle(
                bounds.X + inset,
                bounds.Y + 9,
                Math.Max(8, bounds.Width - inset * 2),
                Math.Max(10, bounds.Height - 13));
            _spriteBatch.Draw(_pixel, chest, color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.X - shoulder, chest.Y + 2, shoulder + 1, 11),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.Right - 1, chest.Y + 2, shoulder + 1, 11),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(chest.Center.X - 2, chest.Y + 4, 4, chest.Height - 8),
                new Color(35, 40, 45, 150));
            return;
        }

        DrawEquipmentSymbol(item?.Slot ?? EquipmentSlot.Armor, bounds, color);
    }

    private void DrawEquipmentSymbol(
        EquipmentSlot slot,
        Rectangle bounds,
        Color color)
    {
        if (slot == EquipmentSlot.Weapon)
        {
            int centerX = bounds.Center.X;
            int bladeTop = bounds.Y + 3;
            int bladeHeight = Math.Max(8, bounds.Height - 14);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, bladeTop, 5, bladeHeight),
                color);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 8, bladeTop + bladeHeight - 4, 17, 4),
                new Color(173, 130, 70));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 2, bladeTop + bladeHeight, 5, 9),
                new Color(105, 68, 45));
            return;
        }

        int chestWidth = Math.Max(10, bounds.Width - 14);
        int chestHeight = Math.Max(10, bounds.Height - 13);
        var chest = new Rectangle(
            bounds.Center.X - chestWidth / 2,
            bounds.Y + 9,
            chestWidth,
            chestHeight);
        _spriteBatch.Draw(_pixel, chest, color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.X - 5, chest.Y + 3, 6, 10),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.Right - 1, chest.Y + 3, 6, 10),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(chest.Center.X - 2, chest.Y + 4, 4, chest.Height - 8),
            new Color(35, 40, 45, 150));
    }

    private void DrawRarityMarkers(
        ItemRarity rarity,
        int centerX,
        int y,
        Color color,
        int markerSize)
    {
        int markerCount = (int)rarity + 1;
        int spacing = markerSize + 2;
        int startX = centerX - (markerCount * spacing - 2) / 2;

        for (int index = 0; index < markerCount; index++)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(startX + index * spacing, y, markerSize, markerSize),
                color);
        }
    }

    private void DrawComparisonIndicator(
        EquipmentItem selectedItem,
        PlayerCharacter player,
        int x,
        int y)
    {
        if (selectedItem == null)
            return;

        EquipmentItem equippedItem = player.EquippedItems.GetEquipped(
            selectedItem.Slot);
        int equippedScore = equippedItem?.Score ?? 0;

        if (selectedItem.Score > equippedScore)
        {
            Color color = new(91, 205, 128);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 8, y + 8, 6, 20), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4, y + 4, 14, 6), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y + 8, 22, 5), color);
        }
        else if (selectedItem.Score < equippedScore)
        {
            Color color = new(218, 88, 82);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 8, y, 6, 20), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4, y + 18, 14, 6), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y + 23, 22, 5), color);
        }
        else
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 11, 22, 6),
                new Color(189, 184, 151));
        }
    }

    private void DrawControlHints(Rectangle bounds, bool showFusion)
    {
        DrawDebugText(
            "WASD/ARROWS MOVE",
            bounds.X + 8,
            bounds.Y + 3,
            new Color(156, 170, 181),
            scale: 1);
        DrawDebugText(
            "Q/E TABS  ENTER EQUIP",
            bounds.X + 8,
            bounds.Y + 14,
            new Color(156, 170, 181),
            scale: 1);
        DrawDebugText(
            showFusion ? "F FUSE  I/ESC CLOSE" : "I/ESC CLOSE",
            bounds.X + 8,
            bounds.Y + 25,
            new Color(156, 170, 181),
            scale: 1);
    }

    private void DrawFusionFeedback(GameSession gameSession)
    {
        EquipmentItem source = gameSession.LastFusionSource;
        EquipmentItem result = gameSession.LastFusionResult;

        if (source == null || result == null)
            return;

        Viewport viewport = _graphicsDevice.Viewport;
        var panel = new Rectangle(viewport.Width / 2 - 170, 92, 340, 60);
        _spriteBatch.Draw(_pixel, panel, new Color(25, 39, 35, 242));
        DrawRectangleOutline(panel, 3, new Color(93, 210, 137));
        DrawDebugText(
            "FUSED!",
            panel.Center.X - 21,
            panel.Y + 10,
            new Color(112, 239, 157),
            scale: 1);
        DrawDebugText(
            $"{source.Name} {source.ArmorGradeLabel} X3 TO " +
                $"{result.Name} {result.ArmorGradeLabel}",
            panel.X + 12,
            panel.Y + 33,
            new Color(222, 231, 225),
            scale: 1);
    }

    private void DrawLoreNoteAcquiredFeedback(GameSession gameSession)
    {
        LoreNote note = gameSession.LastAcquiredLoreNote;
        if (note == null)
            return;

        Viewport viewport = _graphicsDevice.Viewport;
        int width = Math.Min(360, viewport.Width - 32);
        var panel = new Rectangle(
            viewport.Width / 2 - width / 2,
            72,
            width,
            64);
        _spriteBatch.Draw(_pixel, panel, new Color(28, 25, 21, 244));
        DrawRectangleOutline(panel, 3, new Color(166, 132, 70));
        DrawDebugText(
            "LORE NOTE ACQUIRED",
            panel.Center.X - 54,
            panel.Y + 11,
            new Color(225, 194, 113),
            scale: 1);
        DrawDebugText(
            note.Title,
            panel.Center.X - note.Title.Length * 3,
            panel.Y + 37,
            new Color(221, 214, 188),
            FitDebugTextScale(note.Title, panel.Width - 24, 1));
    }

    private void DrawSequentialEnemyTestStatus(GameSession gameSession)
    {
        var panel = new Rectangle(12, 82, 250, 46);
        _spriteBatch.Draw(_pixel, panel, new Color(21, 28, 33, 226));
        DrawRectangleOutline(panel, 2, new Color(189, 151, 79));

        DrawDebugText(
            "WILD FOREST SHOWCASE",
            panel.X + 8,
            panel.Y + 7,
            new Color(241, 205, 111),
            scale: 1);
        DrawDebugText(
            "PROXIMITY AI 650 PX",
            panel.X + 8,
            panel.Y + 20,
            new Color(167, 177, 185),
            scale: 1);

        DrawDebugText(
            "F6 RESET",
            panel.Right - 58,
            panel.Bottom - 10,
            new Color(113, 126, 135),
            scale: 1);
    }

    private void DrawEnemyIntroduction(EnemyIntroductionManager introductions)
    {
        EnemyIntroductionDefinition intro = introductions.Active;
        if (intro == null)
            return;
        float opacity = introductions.PresentationOpacity;

        Viewport viewport = _graphicsDevice.Viewport;
        int width = intro.PresentationStrength == 3 ? 520 : 450;
        int height = intro.PresentationStrength == 1 ? 78 : 88;
        var panel = new Rectangle(
            viewport.Width / 2 - width / 2,
            72,
            width,
            height);
        Color border = intro.PresentationStrength switch
        {
            3 => new Color(182, 142, 75),
            2 => new Color(156, 91, 76),
            _ => new Color(113, 142, 121)
        };
        _spriteBatch.Draw(
            _pixel,
            panel,
            new Color(12, 16, 17, 232) * opacity);
        DrawRectangleOutline(
            panel,
            intro.PresentationStrength + 1,
            border * opacity);

        string name = intro.LocalizedName;
        string title = intro.LocalizedTitle;
        string description = intro.LocalizedDescription;
        int nameScale = FitDebugTextScale(name, panel.Width - 20, 2);
        int nameWidth = name.Length * 6 * nameScale;
        DrawDebugText(
            name,
            panel.Center.X - nameWidth / 2,
            panel.Y + 10,
            new Color(237, 226, 190) * opacity,
            scale: nameScale);
        int titleWidth = title.Length * 6;
        DrawDebugText(
            title,
            panel.Center.X - titleWidth / 2,
            panel.Y + 38,
            border * opacity,
            scale: 1);
        int descriptionWidth = description.Length * 6;
        DrawDebugText(
            description,
            Math.Max(panel.X + 10, panel.Center.X - descriptionWidth / 2),
            panel.Y + 57,
            new Color(183, 190, 184) * opacity,
            scale: 1);
    }

    private void DrawWildForestShowcaseLabel(string text, Rectangle bounds)
    {
        if (string.IsNullOrEmpty(text))
            return;

        const int scale = 1;
        int width = text.Length * 6 * scale;
        int x = bounds.Center.X - width / 2;
        int y = bounds.Top - 18;
        var background = new Rectangle(x - 3, y - 3, width + 6, 12);
        _spriteBatch.Draw(_pixel, background, new Color(12, 17, 25, 185));
        DrawDebugText(text, x, y, new Color(230, 220, 179), scale);
    }

    private static Rectangle GetEnemyVisualBounds(Enemy enemy)
    {
        if (enemy is BloodBat bat)
        {
            Vector2 batSize = EnemyVisualProfile.BloodBat.VisualSize;
            return new Rectangle(
                (int)MathF.Round(bat.Position.X - batSize.X / 2f),
                (int)MathF.Round(
                    bat.Position.Y - bat.FlightVisualOffset - batSize.Y / 2f),
                (int)MathF.Ceiling(batSize.X),
                (int)MathF.Ceiling(batSize.Y));
        }

        Vector2 visualSize = enemy switch
        {
            DireWolf => EnemyVisualProfile.DireWolf.VisualSize,
            GiantSpider => EnemyVisualProfile.GiantSpider.VisualSize,
            ThornCrawler => EnemyVisualProfile.ThornCrawler.VisualSize,
            CorruptedTreant => EnemyVisualProfile.CorruptedTreant.VisualSize,
            GoblinChief => EnemyVisualProfile.GoblinChief.VisualSize,
            MotherSpider => EnemyVisualProfile.MotherSpider.VisualSize,
            Spiderling => EnemyVisualProfile.Spiderling.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.Skeleton =>
                EnemyVisualProfile.Skeleton.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.SkeletonArcher =>
                EnemyVisualProfile.SkeletonArcher.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.RottenCorpse =>
                EnemyVisualProfile.RottenCorpse.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.Wraith =>
                EnemyVisualProfile.Wraith.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.UndeadGuard =>
                EnemyVisualProfile.UndeadGuard.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.GraveBat =>
                EnemyVisualProfile.GraveBat.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.CursedKnight =>
                EnemyVisualProfile.CursedKnight.VisualSize,
            CatacombEnemy catacomb when
                catacomb.Type == EnemyType.SoulCollector =>
                EnemyVisualProfile.SoulCollector.VisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.FallenMilitiaman =>
                FallenCityMilitiaSpriteRenderer.MilitiamanVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.RuinedCrossbowman =>
                FallenCityMilitiaSpriteRenderer.CrossbowmanVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.RabidHound =>
                FallenCityHoundPlagueSpriteRenderer.HoundVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.PlagueCarrier =>
                FallenCityHoundPlagueSpriteRenderer.CarrierVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.BellWatcher =>
                FallenCityWatcherExecutionerSpriteRenderer.WatcherVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.StreetExecutioner =>
                FallenCityWatcherExecutionerSpriteRenderer.ExecutionerVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.RoyalGuard =>
                FallenCityRoyalAshSpriteRenderer.RoyalGuardVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.AshKnight =>
                FallenCityRoyalAshSpriteRenderer.AshKnightVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.WarBishop =>
                FallenCityBishopKingSpriteRenderer.WarBishopVisualSize,
            FallenCityEnemy city when
                city.Type == EnemyType.BrokenKing =>
                FallenCityBishopKingSpriteRenderer.BrokenKingVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.ImpRaider =>
                DemonFrontierSpriteRenderer.ImpVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.HellArcher =>
                DemonFrontierSpriteRenderer.ArcherVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.SpineHound =>
                DemonPredatorSpriteRenderer.SpineHoundVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.RiftStalker =>
                DemonPredatorSpriteRenderer.RiftStalkerVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.BoneHarvester =>
                DemonHeavySpriteRenderer.BoneHarvesterVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.Hellbrute =>
                DemonHeavySpriteRenderer.HellbruteVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.CorruptionPriest =>
                DemonRitualSpriteRenderer.PriestVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.RiftWarden =>
                DemonRitualSpriteRenderer.WardenVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.DemonLegionnaire =>
                DemonLegionSpriteRenderer.LegionnaireVisualSize,
            DemonLandsEnemy demon when
                demon.Type == EnemyType.HornedGeneral =>
                DemonLegionSpriteRenderer.GeneralVisualSize,
            DemonCityEnemy demonCity when
                demonCity.Type == EnemyType.DemonCityEnforcer =>
                DemonCityEnemySpriteRenderer.EnforcerVisualSize,
            DemonCityEnemy demonCity when
                demonCity.Type == EnemyType.SoulLanternKeeper =>
                DemonCityEnemySpriteRenderer.KeeperVisualSize,
            DemonCityEnemy demonCity when
                demonCity.Type == EnemyType.AshbladeStalker =>
                DemonCityEnemySpriteRenderer.StalkerVisualSize,
            DemonCityEnemy demonCity when
                demonCity.Type == EnemyType.ChainCollector =>
                DemonCityEnemySpriteRenderer.CollectorVisualSize,
            DemonCityIndustrialEnemy industrial when
                industrial.Type == EnemyType.BloodworksServitor =>
                DemonCityIndustrialEnemySpriteRenderer.ServitorVisualSize,
            DemonCityIndustrialEnemy industrial when
                industrial.Type == EnemyType.SigilCantor =>
                DemonCityIndustrialEnemySpriteRenderer.CantorVisualSize,
            DemonCityEliteEnemy eliteCity when
                eliteCity.Type == EnemyType.VeilWarden =>
                DemonCityEliteEnemySpriteRenderer.WardenVisualSize,
            DemonCityEliteEnemy eliteCity when
                eliteCity.Type == EnemyType.GloomwingHarrier =>
                DemonCityEliteEnemySpriteRenderer.HarrierVisualSize,
            DemonCityBossEnemy cityBoss when
                cityBoss.Type == EnemyType.CathedralButcher =>
                DemonCityBossEnemySpriteRenderer.ButcherVisualSize,
            DemonCityBossEnemy cityBoss when
                cityBoss.Type == EnemyType.AbyssalMagistrate =>
                DemonCityBossEnemySpriteRenderer.MagistrateVisualSize,
            MalacharEnemy _ => MalacharEnemySpriteRenderer.VisualSize,
            DemonKingSanctumEnemy sanctum when
                sanctum.Type == EnemyType.OathboundSentinel =>
                DemonKingSanctumEnemySpriteRenderer.SentinelVisualSize,
            DemonKingSanctumEnemy _ =>
                DemonKingSanctumEnemySpriteRenderer.AcolyteVisualSize,
            DemonKingSanctumPenitentEnemy penitent when
                penitent.Type == EnemyType.PenitentHusk =>
                DemonKingSanctumPenitentSpriteRenderer.HuskVisualSize,
            DemonKingSanctumPenitentEnemy _ =>
                DemonKingSanctumPenitentSpriteRenderer.ScribeVisualSize,
            DemonKingSanctumReliquaryEnemy reliquary when
                reliquary.Type == EnemyType.GraveReliquary =>
                DemonKingSanctumReliquarySpriteRenderer.ReliquaryVisualSize,
            DemonKingSanctumReliquaryEnemy _ =>
                DemonKingSanctumReliquarySpriteRenderer.WraithVisualSize,
            DemonKingSanctumEliteEnemy sanctumElite when
                sanctumElite.Type == EnemyType.RoyalSanctumKnight =>
                DemonKingSanctumEliteSpriteRenderer.KnightVisualSize,
            DemonKingSanctumEliteEnemy _ =>
                DemonKingSanctumEliteSpriteRenderer.OracleVisualSize,
            ThroneboundSeraphEnemy _ => ThroneboundSeraphSpriteRenderer.VisualSize,
            VaeltharEnemy _ => VaeltharSpriteRenderer.VisualSize,
            _ => enemy.Size
        };
        if (enemy is DemonCityEliteEnemy fallenHarrier &&
            fallenHarrier.Type == EnemyType.GloomwingHarrier &&
            !fallenHarrier.IsAlive &&
            !float.IsNaN(fallenHarrier.DeathLandingCenterY))
        {
            int startBottom = enemy.Bounds.Bottom;
            int landingBottom = (int)MathF.Ceiling(
                fallenHarrier.DeathLandingCenterY + enemy.Size.Y / 2f);
            int top = Math.Min(startBottom, landingBottom) -
                (int)MathF.Ceiling(visualSize.Y);
            int bottom = Math.Max(startBottom, landingBottom);
            return new Rectangle(
                (int)MathF.Round(enemy.Position.X - visualSize.X / 2f),
                top,
                (int)MathF.Ceiling(visualSize.X),
                bottom - top);
        }
        return new Rectangle(
            (int)MathF.Round(enemy.Position.X - visualSize.X / 2f),
            enemy.Bounds.Bottom - (int)MathF.Ceiling(visualSize.Y),
            (int)MathF.Ceiling(visualSize.X),
            (int)MathF.Ceiling(visualSize.Y));
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => new Color(86, 193, 103),
            ItemRarity.Rare => new Color(72, 132, 232),
            ItemRarity.Epic => new Color(170, 89, 224),
            ItemRarity.Legendary => new Color(236, 159, 52),
            ItemRarity.SecretMythic => new Color(255, 194, 46),
            _ => new Color(202, 207, 210)
        };
    }

    private static string GetLocalizedEquipmentName(EquipmentItem item)
    {
        if (item?.WeaponDefinition == null)
            return item?.Name.ToUpperInvariant() ?? string.Empty;

        string key = item.WeaponId switch
        {
            "knight-long-sword" => "weapon.knight.name",
            "duelist-dual-swords" => "weapon.duelist.name",
            "iron-vanguard-greatsword" => "weapon.iron_vanguard.name",
            "hunter-bow" => "weapon.phantom_archer.name",
            "forbidden-sovereign-abyss-ring" =>
                "weapon.forbidden_sovereign.name",
            _ => string.Empty
        };
        return key.Length > 0
            ? Loc.Get(key)
            : item.Name.ToUpperInvariant();
    }

    private void DrawDungeonStatus(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var panel = new Rectangle(viewport.Width - 244, 12, 232, 70);
        _spriteBatch.Draw(_pixel, panel, new Color(17, 21, 28, 220));
        DrawRectangleOutline(panel, 2, new Color(112, 119, 126, 230));

        Color depthColor = new(191, 164, 78);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(panel.X + 10, panel.Y + 11, 17, 6),
            depthColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(panel.X + 14, panel.Y + 5, 9, 18),
            depthColor);
        int visibleDepth = Math.Min(gameSession.DungeonDepth, 12);

        for (int index = 0; index < visibleDepth; index++)
        {
            int markerHeight = 7 + index % 3 * 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    panel.X + 36 + index * 10,
                    panel.Y + 23 - markerHeight,
                    7,
                    markerHeight),
                depthColor);
        }

        if (gameSession.DungeonDepth > visibleDepth)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(panel.Right - 28, panel.Y + 8, 16, 12),
                new Color(231, 198, 88));
        }

        Color bossColor = gameSession.BossDefeated
            ? new Color(86, 190, 112)
            : new Color(207, 72, 63);
        int bossX = panel.X + 18;
        int statusY = panel.Y + 42;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX, statusY + 7, 28, 10),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 3, statusY, 6, 9),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 11, statusY - 4, 6, 13),
            bossColor);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(bossX + 19, statusY, 6, 9),
            bossColor);

        Color tierColor = new(151, 104, 221);
        int tierStartX = panel.Center.X -
            (gameSession.WorldTier * 11 - 3) / 2;

        for (int index = 0; index < gameSession.WorldTier; index++)
        {
            int markerX = tierStartX + index * 11;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, statusY + 2, 8, 17),
                tierColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX + 2, statusY - 2, 4, 4),
                new Color(202, 163, 244));
        }

        int exitX = panel.Right - 53;
        Color exitColor = gameSession.IsExitUnlocked
            ? new Color(111, 202, 113)
            : new Color(206, 91, 67);
        var exitFrame = new Rectangle(exitX, statusY - 4, 31, 27);
        DrawRectangleOutline(exitFrame, 3, exitColor);

        if (gameSession.IsExitUnlocked)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.Center.X - 2, exitFrame.Center.Y - 2, 5, 5),
                exitColor);
        }
        else
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.X + 7, exitFrame.Y + 6, 4, 16),
                exitColor);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(exitFrame.Right - 11, exitFrame.Y + 6, 4, 16),
                exitColor);
        }
    }

    private void DrawWorldTierTransition(GameSession gameSession)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        float progress = MathHelper.Clamp(
            gameSession.WorldTierTransitionProgress,
            0f,
            1f);
        float pulse = 0.55f +
            0.45f * MathF.Abs(MathF.Sin(progress * MathHelper.Pi * 4f));
        byte alpha = (byte)(190f * progress * pulse);
        Color borderColor = new(171, 116, 235, (int)alpha);
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        DrawRectangleOutline(screen, 8, borderColor);

        int markerCount = gameSession.WorldTier;
        int spacing = 22;
        int centerX = viewport.Width / 2;
        int startX = centerX - (markerCount * spacing - 6) / 2;
        int y = viewport.Height / 2 - 25;

        for (int index = 0; index < markerCount; index++)
        {
            int x = startX + index * spacing;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, y + 8, 16, 34),
                new Color(151, 91, 222, (int)alpha));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x + 4, y, 8, 50),
                new Color(211, 169, 247, (int)alpha));
        }
    }

    private void DrawLastDiceTableWorld(GameSession session, GameTime gameTime)
    {
        LastDiceTableSystem dice = session.LastDiceTable;
        if (dice.State == LastDiceTableState.Unavailable ||
            !dice.TableBounds.Intersects(session.Camera.ViewBounds))
        {
            return;
        }

        Rectangle table = dice.TableBounds;
        Color woodDark = new(48, 30, 23);
        Color wood = new(91, 57, 34);
        Color edge = new(145, 99, 50);
        _spriteBatch.Draw(_pixel,
            new Rectangle(table.X - 7, table.Y + 15, table.Width + 14, 18),
            woodDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(table.X, table.Y + 5, table.Width, 16), wood);
        _spriteBatch.Draw(_pixel,
            new Rectangle(table.X + 7, table.Y + 7, table.Width - 14, 3), edge);
        _spriteBatch.Draw(_pixel,
            new Rectangle(table.X + 15, table.Y + 21, 12, 45), woodDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(table.Right - 27, table.Y + 21, 12, 45), woodDark);

        int centerX = table.Center.X;
        int plateY = table.Y + 2;
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 48, plateY - 4, 96, 7),
            new Color(133, 126, 104));
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 39, plateY - 7, 78, 5),
            new Color(188, 177, 137));
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 43, plateY - 30, 86, 25),
            new Color(68, 63, 57));
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 34, plateY - 38, 68, 10),
            new Color(91, 84, 73));
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 8, plateY - 43, 16, 7), edge);

        // The dealer remains behind the table and never performs the reveal.
        int dealerX = table.X - 42;
        int groundY = table.Bottom;
        _spriteBatch.Draw(_pixel,
            new Rectangle(dealerX - 15, groundY - 105, 30, 30),
            new Color(73, 55, 45));
        _spriteBatch.Draw(_pixel,
            new Rectangle(dealerX - 23, groundY - 76, 46, 75),
            new Color(35, 39, 43));
        _spriteBatch.Draw(_pixel,
            new Rectangle(dealerX - 19, groundY - 69, 38, 4),
            new Color(129, 91, 49));
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 1.4f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(dealerX - 8, groundY - 94, 4, 3),
            new Color(199, 146, 68, (int)(150 + pulse * 80)));
        _spriteBatch.Draw(_pixel,
            new Rectangle(dealerX + 4, groundY - 94, 4, 3),
            new Color(199, 146, 68, (int)(150 + pulse * 80)));
    }

    private void DrawLastDiceTable(GameSession session)
    {
        LastDiceTableSystem dice = session.LastDiceTable;
        Rectangle viewport = _graphicsDevice.Viewport.Bounds;
        int margin = Math.Max(16, Math.Min(viewport.Width, viewport.Height) / 28);
        int width = Math.Min(900, viewport.Width - margin * 2);
        int height = Math.Min(620, viewport.Height - margin * 2);
        Rectangle panel = new(
            viewport.Center.X - width / 2,
            viewport.Center.Y - height / 2,
            width,
            height);
        int scale = panel.Width >= 700 && panel.Height >= 500 ? 2 : 1;

        _spriteBatch.Draw(_pixel, viewport, new Color(4, 5, 7, 224));
        _spriteBatch.Draw(_pixel, panel, new Color(25, 20, 18, 250));
        DrawRectangleOutline(panel, 3, new Color(151, 103, 55));
        DrawRectangleOutline(
            new Rectangle(panel.X + 8, panel.Y + 8,
                panel.Width - 16, panel.Height - 16),
            1,
            new Color(82, 65, 47));
        DrawCenteredDebugText(
            Loc.Get("dice.title"), panel.Center.X, panel.Y + 24,
            new Color(230, 193, 111), scale);
        DrawCenteredDebugText(
            Loc.Format("dice.round", dice.RoundIndex + 1, dice.Wins),
            panel.Center.X, panel.Y + 54,
            new Color(163, 154, 132), 1);

        Rectangle scene = new(
            panel.X + 34,
            panel.Y + 78,
            panel.Width - 68,
            Math.Max(230, panel.Height - 205));
        _spriteBatch.Draw(_pixel, scene, new Color(39, 29, 24));
        DrawRectangleOutline(scene, 2, new Color(93, 67, 45));
        DrawLastDiceScene(dice, scene);

        int controlsY = panel.Bottom - 52;
        if (dice.State == LastDiceTableState.Preparing)
        {
            DrawCenteredDebugText(
                Loc.Get("dice.preparing"), panel.Center.X, controlsY,
                new Color(185, 166, 128),
                FitDebugTextScale(Loc.Get("dice.preparing"), panel.Width - 48, scale));
            return;
        }

        if (dice.State == LastDiceTableState.Selecting)
        {
            int choiceWidth = Math.Min(270, (panel.Width - 90) / 2);
            int gap = 24;
            int left = panel.Center.X - choiceWidth - gap / 2;
            DrawLastDiceChoice(
                Loc.Get("dice.xiu"),
                new Rectangle(left, controlsY - 38, choiceWidth, 34),
                dice.Choice == LastDiceChoice.Xiu);
            DrawLastDiceChoice(
                Loc.Get("dice.tai"),
                new Rectangle(left + choiceWidth + gap, controlsY - 38,
                    choiceWidth, 34),
                dice.Choice == LastDiceChoice.Tai);
            DrawCenteredDebugText(
                Loc.Get("dice.choose"), panel.Center.X, controlsY + 7,
                new Color(153, 172, 166),
                FitDebugTextScale(Loc.Get("dice.choose"), panel.Width - 44, 1));
            return;
        }

        if (dice.State == LastDiceTableState.Revealing)
        {
            string choice = Loc.Get(dice.Choice == LastDiceChoice.Tai
                ? "dice.outcome.tai" : "dice.outcome.xiu");
            DrawCenteredDebugText(
                Loc.Format("dice.locked", choice),
                panel.Center.X, controlsY - 23,
                new Color(204, 170, 100), 1);
            DrawCenteredDebugText(
                Loc.Get("dice.reveal_hint"), panel.Center.X, controlsY + 4,
                new Color(146, 206, 193),
                FitDebugTextScale(Loc.Get("dice.reveal_hint"), panel.Width - 44, scale));
            return;
        }

        if (dice.State == LastDiceTableState.RoundResult)
        {
            DrawLastDiceResult(dice, panel.Center.X, controlsY - 36);
            DrawCenteredDebugText(
                Loc.Get("dice.continue"), panel.Center.X, controlsY + 13,
                new Color(146, 206, 193), 1);
            return;
        }

        DrawCenteredDebugText(
            Loc.Format("dice.final", dice.Wins),
            panel.Center.X, controlsY - 41,
            dice.Wins >= 2 ? new Color(142, 215, 163) : new Color(196, 141, 124),
            scale);
        string rewardKey = dice.Wins >= 3
            ? "dice.reward.rare"
            : dice.Wins >= 2 ? "dice.reward.weapon" : "dice.reward.none";
        DrawCenteredDebugText(
            Loc.Get(rewardKey), panel.Center.X, controlsY - 10,
            new Color(216, 190, 126),
            FitDebugTextScale(Loc.Get(rewardKey), panel.Width - 44, 1));
        DrawCenteredDebugText(
            Loc.Get("dice.finish"), panel.Center.X, controlsY + 16,
            new Color(146, 206, 193), 1);
    }

    private void DrawLastDiceScene(LastDiceTableSystem dice, Rectangle scene)
    {
        int tableY = scene.Bottom - 48;
        _spriteBatch.Draw(_pixel,
            new Rectangle(scene.X + 20, tableY, scene.Width - 40, 22),
            new Color(87, 51, 30));
        _spriteBatch.Draw(_pixel,
            new Rectangle(scene.X + 28, tableY + 3, scene.Width - 56, 4),
            new Color(151, 97, 49));
        int plateCenterX = scene.Center.X - 25;
        _spriteBatch.Draw(_pixel,
            new Rectangle(plateCenterX - 157, tableY - 8, 314, 11),
            new Color(105, 99, 85));
        _spriteBatch.Draw(_pixel,
            new Rectangle(plateCenterX - 141, tableY - 12, 282, 7),
            new Color(184, 171, 132));

        float progress = dice.State is LastDiceTableState.RoundResult or
            LastDiceTableState.Completion
            ? 1f
            : dice.State == LastDiceTableState.Revealing
                ? dice.RevealProgress
                : 0f;
        float preparation = dice.State == LastDiceTableState.Preparing
            ? dice.PreparationProgress
            : 1f;
        int dieY = tableY - 52;
        int[] dieX = { plateCenterX - 102, plateCenterX - 28, plateCenterX + 46 };
        float[] thresholds = { .27f, .52f, .74f };
        for (int index = 0; index < 3; index++)
        {
            if (progress + .0001f < thresholds[index])
                continue;
            DrawLastDie(
                new Rectangle(dieX[index], dieY, 52, 52),
                dice.Dice[index]);
        }

        float lift = SmoothStep(0f, .15f, progress);
        float slide = SmoothStep(.15f, .85f, progress);
        float clear = SmoothStep(.85f, 1f, progress);
        float shake = dice.State == LastDiceTableState.Preparing && preparation > .35f
            ? MathF.Sin(preparation * 42f) * (1f - preparation) * 12f
            : 0f;
        int bowlX = (int)MathF.Round(
            plateCenterX - 164 + slide * 330f + clear * 38f + shake);
        int bowlY = (int)MathF.Round(tableY - 89 - lift * 18f - clear * 4f);
        DrawLastDiceBowl(new Rectangle(bowlX, bowlY, 328, 86), progress);

        if (dice.State == LastDiceTableState.Revealing)
            DrawLastDicePlayerHand(scene, bowlX, bowlY, progress);
        else if (dice.State == LastDiceTableState.Preparing)
            DrawLastDiceDealerHands(scene, bowlX, bowlY, preparation);
    }

    private void DrawLastDiceBowl(Rectangle bounds, float progress)
    {
        Color shadow = new(20, 16, 14, 190);
        Color iron = new(65, 63, 59);
        Color rim = new(118, 105, 79);
        Color glint = new(151, 132, 91);
        int tilt = (int)MathF.Round(SmoothStep(.15f, 1f, progress) * 7f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 8, bounds.Bottom - 4, bounds.Width, 10), shadow);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 8, bounds.Y + 32 - tilt,
                bounds.Width - 16, bounds.Height - 30 + tilt), iron);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 30, bounds.Y + 15 - tilt,
                bounds.Width - 60, 25), iron);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 74, bounds.Y + 4 - tilt,
                bounds.Width - 148, 15), iron);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X, bounds.Bottom - 10,
                bounds.Width, 12), rim);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 56, bounds.Y + 18 - tilt,
                bounds.Width - 112, 4), glint);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Center.X - 20, bounds.Y - 4 - tilt,
                40, 9), rim);
    }

    private void DrawLastDicePlayerHand(
        Rectangle scene, int bowlX, int bowlY, float progress)
    {
        Vector2 grip = new(bowlX + 302f, bowlY + 58f);
        Vector2 elbow = new(
            MathHelper.Lerp(scene.Right - 48f, scene.Right - 112f, progress),
            MathHelper.Lerp(scene.Bottom - 22f, scene.Center.Y + 12f, progress));
        Color sleeve = new(57, 67, 78);
        Color cuff = new(121, 101, 75);
        Color skin = new(188, 139, 103);
        DrawLastDiceLimb(elbow, grip + new Vector2(13f, 2f), 22f, sleeve);
        DrawLastDiceLimb(grip + new Vector2(6f, 1f), grip, 17f, cuff);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)grip.X - 8, (int)grip.Y - 10, 27, 20), skin);
        for (int finger = 0; finger < 4; finger++)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)grip.X - 7 + finger * 6,
                    (int)grip.Y + 7, 4, 15), skin);
        }
    }

    private void DrawLastDiceDealerHands(
        Rectangle scene, int bowlX, int bowlY, float preparation)
    {
        float withdraw = SmoothStep(.78f, 1f, preparation);
        Color sleeve = new(43, 42, 45);
        Color skin = new(151, 112, 86);
        Vector2 left = new(bowlX + 38f - withdraw * 70f, bowlY + 57f);
        Vector2 right = new(bowlX + 290f + withdraw * 70f, bowlY + 57f);
        DrawLastDiceLimb(new Vector2(scene.X + 34f, scene.Y + 26f), left, 17f, sleeve);
        DrawLastDiceLimb(new Vector2(scene.Right - 34f, scene.Y + 26f), right, 17f, sleeve);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)left.X - 7, (int)left.Y - 8, 20, 16), skin);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)right.X - 13, (int)right.Y - 8, 20, 16), skin);
    }

    private void DrawLastDiceLimb(
        Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(
            _pixel,
            from,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, .5f),
            new Vector2(length, width),
            SpriteEffects.None,
            0f);
    }

    private void DrawLastDie(Rectangle bounds, int value)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 4, bounds.Y + 6, bounds.Width, bounds.Height),
            new Color(25, 21, 19, 170));
        _spriteBatch.Draw(_pixel, bounds, new Color(221, 207, 172));
        DrawRectangleOutline(bounds, 3, new Color(112, 80, 51));
        if (value <= 0)
            return;

        bool[,] pips = new bool[3, 3];
        if (value is 1 or 3 or 5) pips[1, 1] = true;
        if (value >= 2)
        {
            pips[0, 0] = true;
            pips[2, 2] = true;
        }
        if (value >= 4)
        {
            pips[2, 0] = true;
            pips[0, 2] = true;
        }
        if (value == 6)
        {
            pips[0, 1] = true;
            pips[2, 1] = true;
        }
        for (int x = 0; x < 3; x++)
        for (int y = 0; y < 3; y++)
        {
            if (!pips[x, y]) continue;
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.X + 9 + x * 15,
                    bounds.Y + 9 + y * 15, 7, 7),
                value == 1 ? new Color(145, 45, 38) : new Color(38, 34, 31));
        }
    }

    private void DrawLastDiceChoice(string text, Rectangle bounds, bool selected)
    {
        _spriteBatch.Draw(_pixel, bounds,
            selected ? new Color(92, 65, 36) : new Color(34, 31, 29));
        DrawRectangleOutline(bounds, selected ? 3 : 1,
            selected ? new Color(225, 180, 91) : new Color(88, 78, 67));
        DrawCenteredDebugText(text, bounds.Center.X, bounds.Y + 10,
            selected ? new Color(246, 219, 151) : new Color(151, 145, 133), 1);
    }

    private void DrawLastDiceResult(
        LastDiceTableSystem dice, int centerX, int y)
    {
        DrawCenteredDebugText(
            Loc.Format("dice.result_line",
                dice.Dice[0], dice.Dice[1], dice.Dice[2], dice.Total),
            centerX, y,
            new Color(230, 205, 139), 2);
        string outcomeKey = dice.Outcome switch
        {
            LastDiceOutcome.Triple => "dice.outcome.triple",
            LastDiceOutcome.Tai => "dice.outcome.tai",
            _ => "dice.outcome.xiu"
        };
        string result = Loc.Get(outcomeKey) + "  " +
            Loc.Get(dice.PlayerWonRound ? "dice.win" : "dice.loss");
        DrawCenteredDebugText(result, centerX, y + 25,
            dice.PlayerWonRound
                ? new Color(139, 218, 159)
                : new Color(219, 133, 111), 1);
    }

    private static float SmoothStep(float start, float end, float value)
    {
        if (end <= start)
            return value >= end ? 1f : 0f;
        float t = Math.Clamp((value - start) / (end - start), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private void DrawAncientScholarQuiz(GameSession session)
    {
        AncientScholarTombSystem tomb = session.ScholarTomb;
        Rectangle viewport = _graphicsDevice.Viewport.Bounds;
        int safeMargin = Math.Max(18, Math.Min(viewport.Width, viewport.Height) / 30);
        int availableWidth = Math.Max(1, viewport.Width - safeMargin * 2);
        int availableHeight = Math.Max(1, viewport.Height - safeMargin * 2);
        int panelWidth = Math.Min(760, (int)MathF.Round(viewport.Width * .72f));
        int panelHeight = (int)MathF.Round(viewport.Height * .74f);
        panelWidth = Math.Clamp(panelWidth, Math.Min(480, availableWidth), availableWidth);
        panelHeight = Math.Clamp(panelHeight, Math.Min(360, availableHeight), availableHeight);
        Rectangle panel = new(
            viewport.X + (viewport.Width - panelWidth) / 2,
            viewport.Y + (viewport.Height - panelHeight) / 2,
            panelWidth,
            panelHeight);
        bool compact = panel.Height < 500;
        int padding = compact ? 18 : 26;
        int textScale = panel.Height < 430 || panel.Width < 560 ? 1 : 2;
        int controlsY = panel.Bottom - padding - 7;

        _spriteBatch.Draw(_pixel, viewport, new Color(5, 9, 13, 205));
        _spriteBatch.Draw(_pixel, panel, new Color(17, 22, 29, 246));
        DrawRectangleOutline(panel, 3, new Color(92, 137, 139));
        Rectangle inset = new(
            panel.X + 8,
            panel.Y + 8,
            Math.Max(1, panel.Width - 16),
            Math.Max(1, panel.Height - 16));
        DrawRectangleOutline(inset, 1, new Color(153, 126, 69));
        DrawCenteredDebugText(
            Loc.Get("scholar.title"),
            panel.Center.X,
            panel.Y + padding,
            new Color(197, 181, 128),
            textScale);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                panel.X + padding,
                panel.Y + padding + 25 * textScale / 2,
                panel.Width - padding * 2,
                1),
            new Color(75, 105, 106));

        if (tomb.State == AncientScholarTombState.Opening)
        {
            DrawCenteredDebugText(
                Loc.Get("scholar.opening"),
                panel.Center.X,
                panel.Center.Y - 7 * textScale,
                new Color(139, 205, 205),
                textScale);
            return;
        }

        if (tomb.State == AncientScholarTombState.Question)
        {
            DrawScholarQuestion(
                tomb,
                panel,
                padding,
                compact,
                textScale,
                controlsY);
            return;
        }

        int contentX = panel.X + padding + 8;
        int headingY = panel.Y + padding + (compact ? 35 : 46);
        int fittedBodyScale = FitDebugTextScale(
            tomb.ReactionText,
            panel.Width - padding * 2 - 16,
            textScale);

        if (tomb.State == AncientScholarTombState.Reaction)
        {
            DrawDebugText(
                Loc.Format("scholar.answer_count", tomb.QuestionIndex + 1, tomb.QuestionCount),
                contentX,
                headingY,
                new Color(130, 181, 184),
                textScale);
            DrawDebugText(
                tomb.ReactionText,
                contentX,
                headingY + (compact ? 54 : 78),
                new Color(218, 205, 164),
                fittedBodyScale);
            DrawDebugText(
                Loc.Format("scholar.truth_count", tomb.CorrectAnswerCount),
                contentX,
                headingY + (compact ? 102 : 138),
                new Color(159, 192, 172),
                textScale);
            DrawCenteredDebugText(
                Loc.Get("scholar.continue"),
                panel.Center.X,
                controlsY,
                new Color(139, 205, 205),
                1);
            return;
        }

        if (tomb.State == AncientScholarTombState.RewardChoice)
        {
            DrawDebugText(
                Loc.Get("scholar.reward.perfect_heading"),
                contentX,
                headingY,
                new Color(218, 205, 164),
                textScale);
            DrawDebugText(
                Loc.Get("scholar.reward.choose"),
                contentX,
                headingY + (compact ? 32 : 48),
                new Color(164, 176, 174),
                FitDebugTextScale(
                    Loc.Get("scholar.reward.choose"),
                    panel.Width - padding * 2 - 16,
                    textScale));
            int choiceWidth = Math.Min(590, panel.Width - padding * 2 - 36);
            int choiceX = panel.Center.X - choiceWidth / 2;
            int rowHeight = compact ? 30 : 38;
            int rowGap = compact ? 7 : 11;
            int choiceY = headingY + (compact ? 72 : 106);
            for (int index = 0;
                 index < AncientScholarTombSystem.RewardCategoryLabels.Length;
                 index++)
            {
                DrawScholarChoice(
                    AncientScholarTombSystem.RewardCategoryLabels[index],
                    new Rectangle(
                        choiceX,
                        choiceY + index * (rowHeight + rowGap),
                        choiceWidth,
                        rowHeight),
                    tomb.SelectedRewardIndex == index,
                    textScale);
            }
            DrawCenteredDebugText(
                Loc.Get("common.select_hint"),
                panel.Center.X,
                controlsY,
                new Color(124, 164, 168),
                1);
            return;
        }

        DrawDebugText(
            Loc.Format("scholar.final_score", tomb.CorrectAnswerCount),
            contentX,
            headingY,
            new Color(218, 205, 164),
            textScale);
        DrawDebugText(
            tomb.CompletionText,
            contentX,
            headingY + (compact ? 62 : 92),
            tomb.CorrectAnswerCount > 0
                ? new Color(154, 211, 176)
                : new Color(176, 151, 142),
            FitDebugTextScale(
                tomb.CompletionText,
                panel.Width - padding * 2 - 16,
                textScale));
        DrawCenteredDebugText(
            Loc.Get("scholar.leave"),
            panel.Center.X,
            controlsY,
            new Color(139, 205, 205),
            1);
    }

    private void DrawScholarQuestion(
        AncientScholarTombSystem tomb,
        Rectangle panel,
        int padding,
        bool compact,
        int textScale,
        int controlsY)
    {
        AncientScholarQuestion question = tomb.CurrentQuestion;
        int contentX = panel.X + padding + 8;
        int contentWidth = panel.Width - padding * 2 - 16;
        int numberY = panel.Y + padding + (compact ? 34 : 43);
        int titleY = numberY + (compact ? 23 : 29);
        int bodyY = titleY + (compact ? 29 : 38);
        int bodyLineHeight = textScale == 2 ? (compact ? 17 : 20) : 11;

        DrawDebugText(
            Loc.Format("scholar.question_count", tomb.QuestionIndex + 1, tomb.QuestionCount),
            contentX,
            numberY,
            new Color(130, 181, 184),
            textScale);
        DrawDebugText(
            question.Title,
            contentX,
            titleY,
            new Color(218, 205, 164),
            FitDebugTextScale(question.Title, contentWidth, textScale));

        int answerY;
        if (tomb.QuestionIndex == 0)
        {
            DrawDebugText(
                question.Lines[0],
                contentX,
                bodyY,
                new Color(189, 192, 181),
                textScale);
            int codeTop = bodyY + bodyLineHeight + (compact ? 4 : 8);
            int codeLineHeight = textScale == 2 ? 17 : 10;
            int codeHeight = codeLineHeight * 4 + (compact ? 10 : 16);
            int codeWidth = Math.Min(560, contentWidth - 10);
            Rectangle codeBlock = new(
                contentX,
                codeTop,
                codeWidth,
                codeHeight);
            _spriteBatch.Draw(_pixel, codeBlock, new Color(8, 12, 17, 235));
            DrawRectangleOutline(codeBlock, 1, new Color(62, 82, 86));
            int codeY = codeBlock.Y + (compact ? 5 : 8);
            for (int index = 1; index <= 4; index++)
            {
                DrawDebugText(
                    question.Lines[index],
                    codeBlock.X + (compact ? 7 : 11),
                    codeY,
                    new Color(171, 205, 194),
                    textScale);
                codeY += codeLineHeight;
            }
            int promptY = codeBlock.Bottom + (compact ? 7 : 12);
            DrawDebugText(
                question.Lines[5],
                contentX,
                promptY,
                new Color(203, 202, 183),
                textScale);
            answerY = promptY + bodyLineHeight + (compact ? 7 : 13);
        }
        else
        {
            int lineY = bodyY;
            foreach (string line in question.Lines)
            {
                DrawDebugText(
                    line,
                    contentX,
                    lineY,
                    new Color(189, 192, 181),
                    FitDebugTextScale(line, contentWidth, textScale));
                lineY += bodyLineHeight;
            }
            answerY = lineY + (compact ? 8 : 16);
        }

        int rowHeight = compact ? 28 : 36;
        int rowGap = compact ? 5 : 9;
        int choicesHeight = rowHeight * question.Answers.Length +
            rowGap * (question.Answers.Length - 1);
        int maximumChoiceBottom = controlsY - (compact ? 13 : 20);
        if (answerY + choicesHeight > maximumChoiceBottom)
        {
            int overflow = answerY + choicesHeight - maximumChoiceBottom;
            answerY -= Math.Min(overflow, compact ? 18 : 28);
            if (answerY + choicesHeight > maximumChoiceBottom)
            {
                rowGap = 3;
                rowHeight = Math.Max(24, rowHeight - 4);
            }
        }

        int choiceWidth = Math.Min(590, contentWidth - 20);
        int choiceX = panel.Center.X - choiceWidth / 2;
        for (int index = 0; index < question.Answers.Length; index++)
        {
            DrawScholarChoice(
                question.Answers[index],
                new Rectangle(
                    choiceX,
                    answerY + index * (rowHeight + rowGap),
                    choiceWidth,
                    rowHeight),
                tomb.SelectedAnswerIndex == index,
                textScale);
        }

        DrawCenteredDebugText(
            Loc.Get("common.select_hint"),
            panel.Center.X,
            controlsY,
            new Color(124, 164, 168),
            1);
    }

    private void DrawScholarChoice(
        string text,
        Rectangle bounds,
        bool selected,
        int textScale)
    {
        _spriteBatch.Draw(
            _pixel,
            bounds,
            selected
                ? new Color(48, 75, 79, 220)
                : new Color(12, 17, 23, 205));
        DrawRectangleOutline(
            bounds,
            1,
            selected
                ? new Color(116, 180, 181)
                : new Color(53, 63, 67));
        int glyphHeight = 7 * textScale;
        int textY = bounds.Y + Math.Max(1, (bounds.Height - glyphHeight) / 2);
        DrawDebugText(
            selected ? ">" : " ",
            bounds.X + 12,
            textY,
            new Color(138, 216, 213),
            textScale);
        DrawDebugText(
            text,
            bounds.X + 12 + 18 * textScale,
            textY,
            selected
                ? new Color(232, 220, 177)
                : new Color(151, 157, 154),
            FitDebugTextScale(
                text,
                bounds.Width - 28 - 18 * textScale,
                textScale));
    }

    private void DrawCenteredDebugText(
        string text,
        int centerX,
        int y,
        Color color,
        int scale)
    {
        int width = text.Length * 6 * scale;
        DrawDebugText(text, centerX - width / 2, y, color, scale);
    }

    private static int FitDebugTextScale(
        string text,
        int availableWidth,
        int preferredScale)
    {
        if (preferredScale <= 1 || text.Length * 6 * preferredScale <= availableWidth)
            return Math.Max(1, preferredScale);
        return 1;
    }

    private void DrawHudPanel()
    {
        var panel = new Rectangle(12, 12, 238, 94);
        _spriteBatch.Draw(_pixel, panel, new Color(17, 21, 28, 220));
        DrawRectangleOutline(panel, 2, new Color(112, 119, 126, 230));
    }

    private void DrawDeveloperPanel(GameSession session)
    {
        int viewportWidth = _graphicsDevice.Viewport.Width;
        int viewportHeight = _graphicsDevice.Viewport.Height;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, viewportWidth, viewportHeight),
            new Color(4, 6, 8, 205));

        int width = Math.Min(900, viewportWidth - 32);
        int height = Math.Min(650, viewportHeight - 24);
        var panel = new Rectangle(
            (viewportWidth - width) / 2,
            (viewportHeight - height) / 2,
            width,
            height);
        _spriteBatch.Draw(_pixel, panel, new Color(18, 19, 22, 248));
        DrawRectangleOutline(panel, 3, new Color(112, 94, 69));
        DrawRectangleOutline(
            new Rectangle(panel.X + 7, panel.Y + 7, panel.Width - 14, panel.Height - 14),
            1,
            new Color(67, 62, 54));

        int x = panel.X + 24;
        int y = panel.Y + 20;
        DrawDebugText(
            "DUNGEONASCENDANT - DEV PANEL",
            x,
            y,
            new Color(214, 188, 139),
            2);
        y += 38;
        DrawDebugText(
            $"MAP: {session.CurrentMapName}  ZONE: {session.CurrentZoneName}",
            x,
            y,
            new Color(173, 178, 168),
            1);
        y += 15;
        DrawDebugText(
            $"POSITION: {(int)session.Player.Position.X}/{(int)session.Player.Position.Y}  " +
            $"HP: {session.Player.CurrentHealth}/{session.Player.MaxHealth}  " +
            $"STAMINA: {(int)session.Player.CurrentStamina}/{(int)session.Player.MaxStamina}  " +
            $"CURSE: {(int)session.Curse.Value}  " +
            $"CORRUPTION: {(int)session.Corruption.Value}  " +
            $"ENEMIES: {session.ActiveEnemyCount}",
            x,
            y,
            new Color(145, 158, 151),
            1);
        y += 24;

        string lastSection = string.Empty;
        for (int index = 0; index < DeveloperPanel.Entries.Length; index++)
        {
            DeveloperCommand command = DeveloperPanel.Entries[index];
            string section = DeveloperPanel.GetSection(command);
            if (section != lastSection)
            {
                DrawDebugText(section, x, y, new Color(151, 113, 78), 1);
                y += 17;
                lastSection = section;
            }

            bool selected = session.DeveloperPanel.SelectedIndex == index;
            if (selected)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x - 8, y - 4, panel.Width - 48, 20),
                    new Color(70, 59, 43, 220));
                DrawRectangleOutline(
                    new Rectangle(x - 8, y - 4, panel.Width - 48, 20),
                    1,
                    new Color(157, 126, 78));
            }

            DrawDebugText(
                selected ? ">" : " ",
                x,
                y,
                new Color(226, 196, 137),
                1);
            DrawDebugText(
                DeveloperPanel.GetLabel(command),
                x + 16,
                y,
                selected ? new Color(231, 220, 193) : new Color(174, 176, 170),
                1);
            string value = session.DeveloperPanel.GetValue(session, command);
            if (!string.IsNullOrEmpty(value))
            {
                DrawDebugText(
                    value,
                    panel.Right - 310,
                    y,
                    selected ? new Color(213, 174, 104) : new Color(132, 157, 142),
                    1);
            }
            y += 20;
        }

        DrawDebugText(
            session.DeveloperPanel.Feedback,
            x,
            panel.Bottom - 47,
            new Color(148, 177, 145),
            1);
        DrawDebugText(
            "W/S NAVIGATE  A/D CHANGE  ENTER ACTIVATE  ESC/F10 CLOSE",
            x,
            panel.Bottom - 27,
            new Color(124, 128, 126),
            1);
    }

    private void DrawCombatStateDebug(PlayerCharacter player)
    {
        const int x = 270;
        int y = 16;

        if (player.Combat.IsBlockInputHeld)
        {
            DrawDebugLabel("CTRL HELD", x, y, new Color(208, 220, 235));
            y += 22;
        }

        if (player.Combat.IsBlocking)
        {
            DrawDebugLabel("BLOCK ACTIVE", x, y, new Color(99, 191, 247));
        }
    }

    private void DrawDebugLabel(
        string text,
        int x,
        int y,
        Color color)
    {
        const int scale = 2;
        int width = text.Length * 6 * scale + 8;
        var background = new Rectangle(x - 4, y - 4, width, 22);
        _spriteBatch.Draw(_pixel, background, new Color(12, 17, 25, 215));
        DrawRectangleOutline(
            background,
            1,
            new Color(color.R, color.G, color.B, (byte)155));
        DrawDebugText(text, x, y, color, scale);
    }

    private void DrawDebugText(
        string text,
        int x,
        int y,
        Color color,
        int scale)
    {
        int cursorX = x;

        foreach (char character in text)
        {
            char glyphCharacter = GetDebugGlyphCharacter(character);
            if (DebugGlyphs.TryGetValue(glyphCharacter, out byte[] rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if ((rows[row] & (1 << (4 - column))) == 0)
                            continue;

                        _spriteBatch.Draw(
                            _pixel,
                            new Rectangle(
                                cursorX + column * scale,
                                y + row * scale,
                                scale,
                                scale),
                            color);
                    }
                }

                DrawDebugDiacritic(character, cursorX, y, color, scale);
            }

            cursorX += 6 * scale;
        }
    }

    private static char GetDebugGlyphCharacter(char character)
    {
        char upper = char.ToUpperInvariant(character);
        return upper switch
        {
            'À' or 'Á' or 'Ả' or 'Ã' or 'Ạ' or
            'Ă' or 'Ằ' or 'Ắ' or 'Ẳ' or 'Ẵ' or 'Ặ' or
            'Â' or 'Ầ' or 'Ấ' or 'Ẩ' or 'Ẫ' or 'Ậ' => 'A',
            'È' or 'É' or 'Ẻ' or 'Ẽ' or 'Ẹ' or
            'Ê' or 'Ề' or 'Ế' or 'Ể' or 'Ễ' or 'Ệ' => 'E',
            'Ì' or 'Í' or 'Ỉ' or 'Ĩ' or 'Ị' => 'I',
            'Ò' or 'Ó' or 'Ỏ' or 'Õ' or 'Ọ' or
            'Ô' or 'Ồ' or 'Ố' or 'Ổ' or 'Ỗ' or 'Ộ' or
            'Ơ' or 'Ờ' or 'Ớ' or 'Ở' or 'Ỡ' or 'Ợ' => 'O',
            'Ù' or 'Ú' or 'Ủ' or 'Ũ' or 'Ụ' or
            'Ư' or 'Ừ' or 'Ứ' or 'Ử' or 'Ữ' or 'Ự' => 'U',
            'Ỳ' or 'Ý' or 'Ỷ' or 'Ỹ' or 'Ỵ' => 'Y',
            'Đ' => 'D',
            '—' => '-',
            _ => upper
        };
    }

    private void DrawDebugDiacritic(
        char character,
        int x,
        int y,
        Color color,
        int scale)
    {
        char upper = char.ToUpperInvariant(character);
        if (upper == '\u0110')
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x - scale, y + 3 * scale, 4 * scale, scale),
                color);
        }
        bool circumflex = upper is
            'Â' or 'Ầ' or 'Ấ' or 'Ẩ' or 'Ẫ' or 'Ậ' or
            'Ê' or 'Ề' or 'Ế' or 'Ể' or 'Ễ' or 'Ệ' or
            'Ô' or 'Ồ' or 'Ố' or 'Ổ' or 'Ỗ' or 'Ộ';
        bool breve = upper is 'Ă' or 'Ằ' or 'Ắ' or 'Ẳ' or 'Ẵ' or 'Ặ';
        bool horn = upper is
            'Ơ' or 'Ờ' or 'Ớ' or 'Ở' or 'Ỡ' or 'Ợ' or
            'Ư' or 'Ừ' or 'Ứ' or 'Ử' or 'Ữ' or 'Ự';
        bool acute = upper is
            'Á' or 'Ắ' or 'Ấ' or 'É' or 'Ế' or 'Í' or
            'Ó' or 'Ố' or 'Ớ' or 'Ú' or 'Ứ' or 'Ý';
        bool grave = upper is
            'À' or 'Ằ' or 'Ầ' or 'È' or 'Ề' or 'Ì' or
            'Ò' or 'Ồ' or 'Ờ' or 'Ù' or 'Ừ' or 'Ỳ';
        bool hook = upper is
            'Ả' or 'Ẳ' or 'Ẩ' or 'Ẻ' or 'Ể' or 'Ỉ' or
            'Ỏ' or 'Ổ' or 'Ở' or 'Ủ' or 'Ử' or 'Ỷ';
        bool tilde = upper is
            'Ã' or 'Ẵ' or 'Ẫ' or 'Ẽ' or 'Ễ' or 'Ĩ' or
            'Õ' or 'Ỗ' or 'Ỡ' or 'Ũ' or 'Ữ' or 'Ỹ';
        bool dot = upper is
            'Ạ' or 'Ặ' or 'Ậ' or 'Ẹ' or 'Ệ' or 'Ị' or
            'Ọ' or 'Ộ' or 'Ợ' or 'Ụ' or 'Ự' or 'Ỵ';

        if (circumflex)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + scale, y - scale, scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 3 * scale, y - scale, scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 2 * scale, y - 2 * scale, scale, scale), color);
        }
        else if (breve)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + scale, y - 2 * scale, scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 2 * scale, y - scale, 2 * scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4 * scale, y - 2 * scale, scale, scale), color);
        }

        if (horn)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4 * scale, y - scale, 2 * scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 5 * scale, y, scale, scale), color);
        }

        if (acute)
            _spriteBatch.Draw(_pixel, new Rectangle(x + 3 * scale, y - 3 * scale, 2 * scale, scale), color);
        else if (grave)
            _spriteBatch.Draw(_pixel, new Rectangle(x + scale, y - 3 * scale, 2 * scale, scale), color);
        else if (hook)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + 2 * scale, y - 3 * scale, 2 * scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 3 * scale, y - 2 * scale, scale, scale), color);
        }
        else if (tilde)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + scale, y - 3 * scale, 2 * scale, scale), color);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 3 * scale, y - 2 * scale, 2 * scale, scale), color);
        }

        if (dot)
            _spriteBatch.Draw(_pixel, new Rectangle(x + 2 * scale, y + 7 * scale, scale, scale), color);
    }

    private void DrawPlayerStamina(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 44;
        const int barWidth = 198;
        const int barHeight = 10;
        const int borderWidth = 2;
        int fillWidth = (int)((barWidth - borderWidth * 2) *
            (player.CurrentStamina / player.MaxStamina));
        var border = new Rectangle(barX, barY, barWidth, barHeight);
        var background = new Rectangle(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(21, barY + 1, 11, 7),
            new Color(94, 192, 118));
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(24, 54, 35));

        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                new Color(83, 190, 112));
        }
    }

    private void DrawWeaponResource(PlayerCharacter player, GameTime gameTime)
    {
        int maximum = player.Combat.Resources.ActiveMaximum;
        if (maximum <= 0)
            return;

        if (player.WeaponFamily == WeaponFamily.DualSwords)
        {
            DrawDuelistMomentum(player, gameTime);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.HunterBow)
        {
            DrawHunterFocus(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.ArcaneWarStaff)
        {
            DrawElderSigils(player, gameTime);
            return;
        }

        int value = player.Combat.Resources.ActiveValue;
        Color color = player.WeaponFamily switch
        {
            WeaponFamily.DualSwords => new Color(214, 113, 90),
            WeaponFamily.ArcaneWarStaff => new Color(151, 104, 214),
            _ => new Color(154, 158, 164)
        };

        for (int index = 0; index < maximum; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                index < value ? color : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
        }
    }

    private void DrawElderSigils(PlayerCharacter player, GameTime gameTime)
    {
        int value = player.Combat.ElderSigils;
        if (value != _observedElderSigils)
        {
            _elderSigilPulseRemaining = value > _observedElderSigils
                ? .42f
                : 0f;
            _observedElderSigils = value;
        }
        _elderSigilPulseRemaining = MathF.Max(
            0f,
            _elderSigilPulseRemaining - MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                .05f));
        float pulse = _elderSigilPulseRemaining / .42f;
        Color dormant = new(55, 58, 72);
        Color blue = new(76, 151, 224);
        Color gold = new(225, 182, 72);
        Color purple = new(129, 83, 192);
        for (int index = 0; index < AncientStaffTuning.MaximumElderSigils;
             index++)
        {
            bool active = index < value;
            Vector2 center = new(48f + index * 19f, 93f);
            float scale = active && index == value - 1
                ? 1f + pulse * .22f
                : 1f;
            float radius = 5f * scale;
            Color primary = active ? blue : dormant;
            Color accent = active
                ? (index % 2 == 0 ? gold : purple)
                : new Color(81, 83, 91);
            DrawWorldLine(center + new Vector2(0f, -radius),
                center + new Vector2(radius, 0f), 2f, primary);
            DrawWorldLine(center + new Vector2(radius, 0f),
                center + new Vector2(0f, radius), 1f, accent);
            DrawWorldLine(center + new Vector2(0f, radius),
                center + new Vector2(-radius, 0f), 2f, primary);
            DrawWorldLine(center + new Vector2(-radius, 0f),
                center + new Vector2(0f, -radius), 1f, accent);
            if (active)
            {
                DrawWorldLine(center - new Vector2(2f, 0f),
                    center + new Vector2(2f, 0f), 1f,
                    pulse > 0f && index == value - 1
                        ? new Color(241, 245, 255)
                        : gold);
            }
        }
    }

    private void DrawDuelistMomentum(
        PlayerCharacter player,
        GameTime gameTime)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.DuelistMomentumRatio;
        int tier = player.Combat.Resources.DuelistMomentumTier;
        if (_observedDuelistMomentumTier >= 0 &&
            tier > _observedDuelistMomentumTier)
        {
            _duelistMomentumTierPulseRemaining = .34f;
        }
        _observedDuelistMomentumTier = tier;
        _duelistMomentumTierPulseRemaining = MathF.Max(
            0f,
            _duelistMomentumTierPulseRemaining - MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                .05f));
        float pulse = _duelistMomentumTierPulseRemaining / .34f;
        Color momentum = tier switch
        {
            2 => Color.Lerp(new Color(111, 205, 222),
                new Color(185, 151, 224),
                .35f + .18f * MathF.Sin(
                    (float)gameTime.TotalGameTime.TotalSeconds * 5f)),
            1 => new Color(111, 205, 222),
            _ => new Color(157, 167, 172)
        };
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(barX + 1, barY + 1, barWidth - 2, barHeight - 2);
        Color borderColor = pulse > 0f
            ? Color.Lerp(new Color(177, 187, 191),
                new Color(230, 245, 250), pulse)
            : tier == 2
                ? new Color(179, 166, 207)
                : new Color(177, 187, 191);
        _spriteBatch.Draw(_pixel, border, borderColor);
        _spriteBatch.Draw(_pixel, background, new Color(28, 34, 39));

        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                momentum);

            if (tier >= 1 && fillWidth >= 5)
            {
                float travel = ((float)gameTime.TotalGameTime.TotalSeconds *
                    (tier == 2 ? 70f : 52f)) % Math.Max(1, fillWidth);
                int lightX = background.X + Math.Clamp(
                    (int)MathF.Round(travel), 1, Math.Max(1, fillWidth - 3));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(lightX, background.Y, 3, background.Height),
                    tier == 2
                        ? new Color(230, 218, 251, 165)
                        : new Color(224, 251, 255, 145));
            }

            if (tier == 2)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(background.X, background.Bottom - 1,
                        fillWidth, 1),
                    new Color(184, 145, 222, 185));
            }
        }

        for (int index = 0; index < 2; index++)
        {
            int markerX = background.X +
                background.Width * (index == 0 ? 35 : 75) / 100;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(231, 219, 191, 175));
        }

        for (int index = 0; index < 3; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            bool active = ratio > 0f && index <= tier;
            _spriteBatch.Draw(
                _pixel,
                pip,
                active ? momentum : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
        }

        string tierName = tier switch
        {
            2 => "OVERDRIVE",
            1 => "FLOW",
            _ => "NORMAL"
        };
        if (player.Combat.IsDuelistOverdriveActive)
            tierName += $" + BLADE {player.Combat.DuelistOverdriveRemaining:0.0}";
        DrawDebugText(tierName, 92, 90, momentum, 1);

        if (player.Combat.IsDuelistUltimateActive)
        {
            string hit = player.Combat.IsDuelistCelestialX
                ? "CELESTIAL X"
                : $"HIT {player.Combat.DuelistUltimateHitNumber}/" +
                    DuelistTuning.UltimateStrikeCount;
            DrawDebugText(hit, 42, 102, new Color(205, 225, 255), 1);
        }
    }

    private void DrawDuelistUltimateBlinkDeparture(GameSession gameSession)
    {
        if (!gameSession.IsDuelistUltimateActive ||
            gameSession.Player.Combat.State == CombatState.ChargingHeavy ||
            _duelistUltimateDepartureFade <= 0f ||
            _duelistUltimateDepartureDuration <= 0f)
        {
            return;
        }

        Vector2 arrival = gameSession.Player.Position;
        Vector2 departure = _duelistUltimateDeparturePosition;
        Vector2 travel = arrival - departure;
        if (travel.LengthSquared() < 16f)
            return;
        float alpha = Math.Clamp(_duelistUltimateDepartureFade /
            _duelistUltimateDepartureDuration, 0f, 1f);
        Vector2 direction = Vector2.Normalize(travel);
        Vector2 normal = new(-direction.Y, direction.X);
        int stage = gameSession.DuelistUltimateStage;
        int group = Math.Clamp(stage / 8, 0, 3);
        float width = 1f + group * .32f;
        DrawWorldLine(departure - direction * 5f - normal * 3f,
            arrival - direction * 8f - normal * 2f, width,
            new Color(124, 214, 231) * (alpha * (.32f + group * .06f)));
        DrawWorldLine(departure - direction * 5f + normal * 3f,
            arrival - direction * 8f + normal * 2f, width,
            new Color(184, 151, 218) * (alpha * (.30f + group * .06f)));
        _playerSpriteRenderer.DrawDuelistUltimateEcho(
            gameSession.Player,
            departure,
            alpha * (.26f + group * .08f));
    }

    private void DrawDuelistUltimateCinematic(GameSession gameSession)
    {
        if (!gameSession.IsDuelistUltimateActive)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        float intro = gameSession.Player.Combat.DuelistUltimateCinematicRatio;
        int stage = gameSession.DuelistUltimateStage;
        float stageRatio = gameSession.DuelistUltimateStageRatio;
        int group = stage < 0
            ? 0
            : Math.Clamp(stage / 8, 0, 3);
        float assaultVeil = stage >= 0 &&
                stage < DuelistTuning.UltimateStrikeCount
            ? .08f + group * .035f
            : 0f;
        float veil = MathF.Max(intro * .56f, assaultVeil);
        if (stage >= DuelistTuning.UltimateStrikeCount)
            veil = MathF.Max(veil, .34f);
        if (veil > .001f)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(10, 14, 30) * veil);
        }

        if (intro > .001f)
        {
            DrawDuelistOpeningCutIn(intro, width, height);
            return;
        }

        if (stage >= 0 && stage < DuelistTuning.UltimateStrikeCount)
        {
            DrawDuelistAssaultOverlay(gameSession, stage, stageRatio,
                width, height);
            return;
        }

        if (stage >= DuelistTuning.UltimateStrikeCount)
            DrawDuelistFinalStarfall(gameSession, stageRatio, width, height);
    }

    private void DrawDuelistOpeningCutIn(
        float intro,
        int width,
        int height)
    {
        float reveal = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp(intro / .44f, 0f, 1f));
        float title = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((intro - .34f) / .34f, 0f, 1f));
        float transition = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((intro - .78f) / .22f, 0f, 1f));
        int centerX = width / 2;
        int centerY = height / 2 - 20;
        int letterbox = (int)MathF.Round(28f * reveal);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, letterbox),
            new Color(5, 8, 17, 235));
        _spriteBatch.Draw(_pixel,
            new Rectangle(0, height - letterbox, width, letterbox),
            new Color(5, 8, 17, 235));

        Color constellation = new Color(103, 154, 184) *
            (reveal * (1f - transition) * .42f);
        Vector2 starA = new(centerX - 145f, centerY - 66f);
        Vector2 starB = new(centerX - 72f, centerY - 94f);
        Vector2 starC = new(centerX + 82f, centerY - 78f);
        Vector2 starD = new(centerX + 148f, centerY - 30f);
        DrawWorldLine(starA, starB, 1f, constellation);
        DrawWorldLine(starB, starC, 1f, constellation);
        DrawWorldLine(starC, starD, 1f, constellation);
        DrawScreenFourPointStar(starA, 3f, constellation);
        DrawScreenFourPointStar(starC, 3f, constellation);
        DrawScreenFourPointStar(starD, 2f, constellation);

        float faceAlpha = reveal * (1f - transition);
        Color silhouette = new Color(24, 29, 42) * faceAlpha;
        Color skinLight = new Color(171, 143, 128) * faceAlpha;
        Color hair = new Color(30, 31, 38) * faceAlpha;
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 58, centerY + 17, 116, 74),
            silhouette);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 35, centerY - 55, 70, 82),
            skinLight);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 39, centerY - 61, 78, 25), hair);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - 42, centerY - 45, 13, 50), hair);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX + 29, centerY - 45, 13, 50), hair);
        float eyeSweep = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((intro - .42f) / .18f, 0f, 1f));
        int eyeWidth = (int)MathF.Round(34f * eyeSweep);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - eyeWidth - 4, centerY - 12,
                eyeWidth, 2),
            new Color(151, 225, 239) * faceAlpha);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX + 4, centerY - 12, eyeWidth, 2),
            new Color(199, 169, 228) * faceAlpha);

        float bladeSplit = transition * width * .34f;
        Vector2 crossing = new(centerX, centerY + 6f);
        Vector2 cyanStart = new(centerX - 155f - bladeSplit,
            centerY + 104f);
        Vector2 cyanEnd = new(centerX + 130f - bladeSplit,
            centerY - 116f);
        Vector2 violetStart = new(centerX + 155f + bladeSplit,
            centerY + 104f);
        Vector2 violetEnd = new(centerX - 130f + bladeSplit,
            centerY - 116f);
        DrawWorldLine(cyanStart, cyanEnd, 9f,
            new Color(42, 116, 137) * faceAlpha);
        DrawWorldLine(cyanStart, cyanEnd, 3f,
            new Color(198, 246, 252) * faceAlpha);
        DrawWorldLine(violetStart, violetEnd, 9f,
            new Color(91, 61, 127) * faceAlpha);
        DrawWorldLine(violetStart, violetEnd, 3f,
            new Color(220, 202, 245) * faceAlpha);
        if (intro is >= .48f and <= .86f)
        {
            float star = MathF.Sin(
                (intro - .48f) / .38f * MathHelper.Pi);
            DrawScreenFourPointStar(crossing, 8f,
                new Color(231, 249, 255) * star);
        }

        if (title > 0f && transition < .92f)
        {
            int bannerY = height - 112;
            _spriteBatch.Draw(_pixel,
                new Rectangle(centerX - 226 - (int)bladeSplit, bannerY,
                    218, 3),
                new Color(116, 214, 231) * title);
            _spriteBatch.Draw(_pixel,
                new Rectangle(centerX + 8 + (int)bladeSplit, bannerY,
                    218, 3),
                new Color(178, 142, 216) * title);
            DrawDebugText("INFINITE STARBURST: ASCENSION",
                centerX - 207, bannerY + 12,
                new Color(224, 235, 242) * title, 2);
            DrawDebugText("CAN YOU FOLLOW THIS?",
                centerX - 82, bannerY + 43,
                new Color(176, 202, 219) * (title * .86f), 1);
        }
    }

    private void DrawDuelistAssaultOverlay(
        GameSession gameSession,
        int stage,
        float stageRatio,
        int width,
        int height)
    {
        Rectangle target = gameSession.DuelistUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        Vector2 center = Vector2.Transform(
            target.Center.ToVector2(), gameSession.Camera.Transform);
        int group = Math.Clamp(stage / 8, 0, 3);
        int variation = stage % 8;
        bool front = stage % 2 == 0;
        float contact = MathF.Sin(Math.Clamp(stageRatio, 0f, 1f) *
            MathHelper.Pi);
        float lift = Math.Clamp((stage + stageRatio) /
            DuelistTuning.UltimateStrikeCount, 0f, 1f);
        float reach = 26f + group * 7f +
            (variation is 6 or 7 ? 6f : 0f);
        float upward = .30f + lift * .48f;
        Vector2 slash = Vector2.Normalize(new Vector2(1f,
            front ? upward : -upward));
        Color core = front
            ? new Color(195, 244, 251) * (.38f + contact * .48f)
            : new Color(219, 199, 243) * (.38f + contact * .48f);
        Color edge = front
            ? new Color(76, 177, 201) * (contact * .46f)
            : new Color(135, 91, 181) * (contact * .46f);
        Vector2 travelStart = center + new Vector2(
            front ? -width * .22f : width * .22f,
            18f - lift * 34f);
        DrawWorldLine(travelStart, center - slash * (reach * .48f),
            1f + group * .35f, edge);
        DrawWorldLine(center - slash * reach, center + slash * reach,
            group >= 2 ? 3f : 2f, core);
        if (variation is 6 or 7)
        {
            Vector2 opposite = new(slash.X, -slash.Y);
            DrawWorldLine(center - opposite * (reach * .88f),
                center + opposite * (reach * .88f), 2f,
                front
                    ? new Color(203, 181, 231) * contact
                    : new Color(160, 224, 237) * contact);
            DrawScreenFourPointStar(center, 4f + group,
                new Color(229, 246, 251) * contact);
        }

        int retainedStars = group == 0 ? 1 : group + 2;
        for (int index = 0; index < retainedStars; index++)
        {
            float angle = -.75f + index * .58f + stage * .11f;
            float radius = 48f + index * 13f + group * 5f;
            Vector2 point = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius - lift * 22f);
            Vector2 next = center + new Vector2(
                MathF.Cos(angle + .38f) * (radius + 9f),
                MathF.Sin(angle + .38f) * (radius + 9f) - lift * 22f);
            DrawWorldLine(point, next, 1f,
                new Color(110, 159, 187) * (.10f + group * .045f));
            DrawScreenFourPointStar(point, group >= 2 ? 3f : 2f,
                (index % 2 == 0
                    ? new Color(169, 227, 238)
                    : new Color(190, 163, 218)) *
                (.24f + group * .08f));
        }

        for (int force = -1; force <= 1; force++)
        {
            Vector2 forceStart = center + new Vector2(force * 19f,
                36f + group * 4f);
            Vector2 forceEnd = center + new Vector2(force * 13f,
                13f - lift * (18f + group * 5f));
            DrawWorldLine(forceStart, forceEnd, 1f,
                new Color(161, 199, 218) * (.16f + group * .04f));
        }

        if (stage % 8 == 0 && stageRatio < .62f)
        {
            string phase = group switch
            {
                0 => "FIRST CONSTELLATION",
                1 => "FALLING STARS",
                2 => "ASTRAL STORM",
                _ => "INFINITE STARBURST"
            };
            float phaseAlpha = 1f - stageRatio / .62f;
            DrawDebugText(phase, width / 2 - phase.Length * 5,
                height / 5, new Color(201, 221, 235) * phaseAlpha, 1);
        }
    }

    private void DrawDuelistFinalStarfall(
        GameSession gameSession,
        float ratio,
        int width,
        int height)
    {
        Rectangle target = gameSession.DuelistUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        Vector2 center = Vector2.Transform(
            target.Center.ToVector2(), gameSession.Camera.Transform);
        int letterbox = 32;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, letterbox),
            new Color(4, 7, 16, 235));
        _spriteBatch.Draw(_pixel,
            new Rectangle(0, height - letterbox, width, letterbox),
            new Color(4, 7, 16, 235));

        if (ratio < .08f)
        {
            float stillness = 1f - ratio / .08f;
            DrawScreenFourPointStar(center, 6f,
                new Color(210, 230, 241) * (.34f + stillness * .22f));
            DrawDuelistFinalConstellation(center, .42f);
            return;
        }

        if (ratio < .22f)
        {
            float ascent = MathHelper.SmoothStep(0f, 1f,
                (ratio - .08f) / .14f);
            float x = width * .5f;
            DrawWorldLine(new Vector2(x - 5f, height * .72f),
                new Vector2(x - 3f, height * (.72f - ascent * .58f)),
                3f, new Color(153, 229, 241) * .80f);
            DrawWorldLine(new Vector2(x + 5f, height * .72f),
                new Vector2(x + 3f, height * (.72f - ascent * .58f)),
                3f, new Color(205, 180, 234) * .76f);
            DrawDuelistFinalConstellation(center, .34f + ascent * .26f);
            return;
        }

        if (ratio < .40f)
        {
            float judgment = MathF.Sin(
                (ratio - .22f) / .18f * MathHelper.Pi);
            Vector2 close = new(width * .5f, height * .38f);
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)close.X - 46, (int)close.Y - 48, 92, 95),
                new Color(24, 29, 43) * (.70f * judgment));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)close.X - 29, (int)close.Y - 51, 58, 61),
                new Color(162, 139, 129) * (.78f * judgment));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)close.X - 33, (int)close.Y - 55, 66, 18),
                new Color(28, 30, 38) * judgment);
            DrawWorldLine(close + new Vector2(-105f, 82f),
                close + new Vector2(88f, -78f), 8f,
                new Color(68, 151, 171) * judgment);
            DrawWorldLine(close + new Vector2(-105f, 82f),
                close + new Vector2(88f, -78f), 2f,
                new Color(216, 250, 254) * judgment);
            DrawWorldLine(close + new Vector2(105f, 82f),
                close + new Vector2(-88f, -78f), 8f,
                new Color(111, 76, 148) * judgment);
            DrawWorldLine(close + new Vector2(105f, 82f),
                close + new Vector2(-88f, -78f), 2f,
                new Color(229, 213, 249) * judgment);
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)close.X - 27, (int)close.Y - 15, 23, 2),
                new Color(157, 229, 241) * judgment);
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)close.X + 4, (int)close.Y - 15, 23, 2),
                new Color(200, 173, 228) * judgment);
            DrawScreenFourPointStar(close, 8f,
                new Color(235, 250, 255) * judgment);
            DrawDebugText("FALL WITH THE STARS!", width / 2 - 83,
                height - 88, new Color(214, 229, 240) * judgment, 1);
            return;
        }

        float descent = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .40f) / .14f, 0f, 1f));
        Vector2 high = new(center.X, -42f);
        Vector2 cyanEntry = Vector2.Lerp(high + new Vector2(-86f, 0f),
            center, descent);
        Vector2 violetEntry = Vector2.Lerp(high + new Vector2(86f, 8f),
            center, Math.Clamp((descent - .20f) / .80f, 0f, 1f));
        DrawWorldLine(high + new Vector2(-86f, 0f), cyanEntry, 5f,
            new Color(178, 237, 247) * .90f);
        if (descent > .20f)
        {
            DrawWorldLine(high + new Vector2(86f, 8f), violetEntry, 5f,
                new Color(211, 190, 239) * .88f);
        }

        float xLife = ratio < .68f
            ? Math.Clamp((ratio - .475f) / .065f, 0f, 1f)
            : 1f - Math.Clamp((ratio - .68f) / .30f, 0f, 1f);
        if (xLife > 0f)
        {
            float extent = 118f;
            Vector2 cyanA = center + new Vector2(-extent, -extent * .78f);
            Vector2 cyanB = center + new Vector2(extent, extent * .78f);
            Vector2 violetA = center + new Vector2(extent, -extent * .78f);
            Vector2 violetB = center + new Vector2(-extent, extent * .78f);
            DrawWorldLine(cyanA, cyanB, 13f,
                new Color(56, 150, 177) * (xLife * .34f));
            DrawWorldLine(violetA, violetB, 13f,
                new Color(111, 72, 151) * (xLife * .32f));
            DrawWorldLine(cyanA, cyanB, 5f,
                new Color(169, 233, 244) * (xLife * .92f));
            DrawWorldLine(violetA, violetB, 5f,
                new Color(207, 183, 237) * (xLife * .90f));
            DrawWorldLine(cyanA + new Vector2(8f, 6f),
                cyanB - new Vector2(8f, 6f), 1f,
                new Color(235, 253, 255) * xLife);
            DrawWorldLine(violetA + new Vector2(-8f, 6f),
                violetB + new Vector2(8f, -6f), 1f,
                new Color(241, 231, 252) * xLife);
            DrawScreenFourPointStar(center, 13f,
                new Color(232, 249, 255) * xLife);
        }

        if (ratio is >= .475f and <= .54f)
        {
            float impact = MathF.Sin((ratio - .475f) / .065f *
                MathHelper.Pi);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(7, 11, 24) * (impact * .32f));
            DrawDuelistFinalConstellation(center, .78f * impact);
        }

        if (ratio > .58f)
        {
            float aftermath = 1f - Math.Clamp((ratio - .58f) / .40f, 0f, 1f);
            for (int fragment = -2; fragment <= 2; fragment++)
            {
                Vector2 start = center + new Vector2(fragment * 22f,
                    -42f + MathF.Abs(fragment) * 6f);
                Vector2 end = start + new Vector2(fragment * 3f,
                    28f + (fragment + 2) * 5f);
                DrawWorldLine(start, end, 1f,
                    (fragment % 2 == 0
                        ? new Color(172, 229, 239)
                        : new Color(198, 171, 225)) * (aftermath * .62f));
            }
            DrawDuelistFinalConstellation(center, aftermath * .44f);
        }
    }

    private void DrawDuelistFinalConstellation(Vector2 center, float alpha)
    {
        Vector2 a = center + new Vector2(-86f, -34f);
        Vector2 b = center + new Vector2(-39f, -79f);
        Vector2 c = center + new Vector2(34f, -86f);
        Vector2 d = center + new Vector2(88f, -28f);
        Vector2 e = center + new Vector2(54f, 57f);
        Vector2 f = center + new Vector2(-58f, 61f);
        Color line = new Color(113, 162, 190) * (alpha * .38f);
        DrawWorldLine(a, b, 1f, line);
        DrawWorldLine(b, c, 1f, line);
        DrawWorldLine(c, d, 1f, line);
        DrawWorldLine(d, e, 1f, line);
        DrawWorldLine(e, f, 1f, line);
        DrawWorldLine(f, a, 1f, line);
        DrawScreenFourPointStar(a, 3f,
            new Color(184, 232, 241) * alpha);
        DrawScreenFourPointStar(c, 4f,
            new Color(224, 244, 249) * alpha);
        DrawScreenFourPointStar(d, 3f,
            new Color(205, 180, 232) * alpha);
        DrawScreenFourPointStar(f, 2f,
            new Color(193, 165, 222) * alpha);
    }

    private void DrawScreenFourPointStar(
        Vector2 center,
        float radius,
        Color color)
    {
        DrawWorldLine(center - new Vector2(radius, 0f),
            center + new Vector2(radius, 0f), 1f, color);
        DrawWorldLine(center - new Vector2(0f, radius),
            center + new Vector2(0f, radius), 1f, color);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)center.X - 1, (int)center.Y - 1, 3, 3),
            color);
    }

    private void DrawDuelistUltimateTargetLock(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsDuelistUltimateTargetLocked)
            return;
        Rectangle target = gameSession.DuelistUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        float pulse = .65f + .35f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 10f);
        Rectangle frame = new(target.X - 8, target.Y - 8,
            target.Width + 16, target.Height + 16);
        DrawRectangleOutline(frame, 2,
            new Color(190, 218, 255) * pulse);
    }

    private void DrawHunterFocus(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.HunterFocusRatio;
        Color focus = Color.Lerp(
            new Color(99, 139, 113),
            new Color(225, 205, 112),
            ratio);
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(barX + 1, barY + 1, barWidth - 2, barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(30, 47, 37));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                focus);
        }

        int[] focusMarkers = { 34, 67 };
        for (int index = 0; index < focusMarkers.Length; index++)
        {
            int markerX = background.X +
                background.Width * focusMarkers[index] / 100;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(226, 219, 177, 155));
        }

        if (player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.RangerSkyfallMarker &&
            player.Combat.State == CombatState.ChargingHeavy)
        {
            int active = player.Combat.RangerSkyfallDistance switch
            {
                SkyfallDistance.Near => 0,
                SkyfallDistance.Medium => 1,
                _ => 2
            };
            for (int index = 0; index < 3; index++)
            {
                Rectangle distance = new(112 + index * 14, 91, 9, 6);
                _spriteBatch.Draw(
                    _pixel,
                    distance,
                    index == active
                        ? new Color(239, 205, 104)
                        : new Color(79, 77, 65));
                DrawRectangleOutline(distance, 1, new Color(213, 201, 163));
            }
        }
    }

    private void DrawSovereignUltimateWorld(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsSovereignUltimateActive)
            return;
        Rectangle target = gameSession.SovereignUltimateTargetBounds;
        if (target.IsEmpty)
            return;

        int stage = gameSession.SovereignUltimateStage;
        float stageRatio = gameSession.SovereignUltimateStageRatio;
        Vector2 center = target.Center.ToVector2();
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 7f);

        DrawSovereignConstellationRoute(
            gameSession,
            center,
            stage,
            stageRatio);

        if (stage <= 0)
        {
            Color judgment = new(218, 166, 53, (int)(65f + pulse * 45f));
            DrawPortalArc(center, Vector2.UnitX, Vector2.UnitY,
                target.Width * .66f + 12f,
                target.Height * .58f + 8f,
                -.35f, 2.20f, 1f, judgment);
            DrawPortalArc(center, Vector2.UnitX, Vector2.UnitY,
                target.Width * .66f + 12f,
                target.Height * .58f + 8f,
                MathHelper.Pi - .35f, 2.20f, 1f, judgment);
            return;
        }

        if (stage <= AbyssRingTuning.UltimateStrikeCount)
        {
            float acceleration = (stage - 1f) /
                Math.Max(1f, AbyssRingTuning.UltimateStrikeCount - 1f);
            float detail = MathHelper.Lerp(1f, .46f, acceleration);
            Color restraint = new(201, 151, 43,
                (int)(46f + detail * 42f));
            float ringWidth = target.Width * .58f + 8f;
            float ringHeight = target.Height * (.24f + stage % 3 * .05f);
            for (int band = -1; band <= 1; band++)
            {
                float y = center.Y + band * target.Height * .22f;
                DrawPortalArc(new Vector2(center.X, y),
                    Vector2.UnitX, Vector2.UnitY,
                    ringWidth, ringHeight,
                    stage * .31f + band,
                    MathHelper.Pi * 1.34f,
                    1f,
                    restraint);
            }

            int activePortal = SovereignPortalSystem
                .GetUltimateStepPortalIndex(stage);
            int nextPortal = stage < AbyssRingTuning.UltimateStrikeCount
                ? SovereignPortalSystem.GetUltimateStepPortalIndex(stage + 1)
                : AbyssRingTuning.UltimateConstellationCount - 1;
            Vector2 from = SovereignPortalSystem
                .GetUltimateConstellationPosition(center, activePortal);
            Vector2 destination = SovereignPortalSystem
                .GetUltimateConstellationPosition(center, nextPortal);
            Vector2 delta = destination - from;
            float length = delta.Length();
            if (length > .01f)
            {
                Vector2 direction = delta / length;
                Vector2 normal = new(-direction.Y, direction.X);
                float routePulse = 1f - Math.Clamp(
                    MathF.Abs(stageRatio - .48f) * 2.1f,
                    0f,
                    1f);
                float bendSign = stage % 2 == 0 ? -1f : 1f;
                Vector2 bend = normal * bendSign *
                    MathHelper.Lerp(12f, 5f, acceleration);
                Vector2 middle = Vector2.Lerp(from, destination, .50f) + bend;
                Color shadow = new(5, 4, 8, (int)(105f * detail));
                Color route = new(245, 190, 58,
                    (int)((55f + routePulse * 130f) * detail));
                DrawWorldLine(from, middle, 5f, shadow);
                DrawWorldLine(middle, destination, 5f, shadow);
                DrawWorldLine(from, middle,
                    acceleration > .70f ? 1f : 2f,
                    route);
                DrawWorldLine(middle, destination,
                    acceleration > .70f ? 1f : 2f, route);
            }
            return;
        }

        if (stage == AbyssRingTuning.UltimateStrikeCount + 1)
        {
            float setup = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(stageRatio / .38f, 0f, 1f));
            Vector2 gate = gameSession.Player.Bounds.Center.ToVector2();
            Vector2 descent = center - gate;
            if (descent.LengthSquared() < .001f)
                descent = Vector2.UnitY;
            descent.Normalize();
            Vector2 platform = new(-descent.Y, descent.X);
            Color dark = new(4, 3, 8, (int)(220f * setup));
            Color gold = new(255, 207, 76, (int)(235f * setup));
            DrawWorldLine(gate - platform * 18f,
                gate + platform * 18f, 25f, dark);
            DrawPortalArc(gate, descent, platform,
                17f * setup, 34f * setup,
                -.20f, MathHelper.TwoPi * .44f, 3f, gold);
            DrawPortalArc(gate, descent, platform,
                17f * setup, 34f * setup,
                MathHelper.Pi - .20f, MathHelper.TwoPi * .44f, 3f, gold);
            Vector2 launchPoint = gate + descent * 18f;
            DrawWorldLine(launchPoint, center,
                7f, new Color(5, 4, 8, (int)(190f * setup)));
            DrawWorldLine(launchPoint, center,
                2f, gold);
            for (int spark = -2; spark <= 2; spark++)
            {
                Vector2 sparkDirection = -descent * (10f + Math.Abs(spark) * 3f) +
                    platform * spark * 5f;
                DrawWorldLine(gate - descent * 3f,
                    gate + sparkDirection * setup,
                    spark == 0 ? 2f : 1f,
                    gold);
            }
            for (int crack = -2; crack <= 2; crack++)
            {
                DrawWorldLine(center,
                    center + new Vector2(crack * 9f, 12f +
                        Math.Abs(crack) * 5f) * setup,
                    crack == 0 ? 2f : 1f,
                    gold);
            }
        }
    }

    private void DrawSovereignConstellationRoute(
        GameSession gameSession,
        Vector2 center,
        int stage,
        float stageRatio)
    {
        if (stage > AbyssRingTuning.UltimateStrikeCount + 1)
            return;

        int revealed = stage <= 0
            ? AbyssRingTuning.UltimateConstellationCount
            : Math.Min(
                AbyssRingTuning.UltimateConstellationCount,
                3 + stage / 3);
        Color shadow = new(3, 3, 7, stage <= 0 ? 82 : 54);
        Color thread = new(154, 111, 31, stage <= 0 ? 88 : 58);
        for (int index = 0; index < revealed; index++)
        {
            int next = index == revealed - 1 ? 0 : index + 1;
            Vector2 from = SovereignPortalSystem
                .GetUltimateConstellationPosition(center, index);
            Vector2 to = SovereignPortalSystem
                .GetUltimateConstellationPosition(center, next);
            DrawWorldLine(from, to, 3f, shadow);
            DrawWorldLine(from, to, 1f, thread);
        }

        int active = SovereignPortalSystem.GetUltimateStepPortalIndex(stage);
        int nextActive = stage < AbyssRingTuning.UltimateStrikeCount
            ? SovereignPortalSystem.GetUltimateStepPortalIndex(stage + 1)
            : AbyssRingTuning.UltimateConstellationCount - 1;
        Vector2 activePoint = SovereignPortalSystem
            .GetUltimateConstellationPosition(center, active);
        Vector2 nextPoint = SovereignPortalSystem
            .GetUltimateConstellationPosition(center, nextActive);
        float preview = stage <= 0
            ? .35f
            : MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(stageRatio / .42f, 0f, 1f));
        Vector2 routeEnd = Vector2.Lerp(activePoint, nextPoint, preview);
        DrawWorldLine(activePoint, routeEnd, 4f,
            new Color(4, 3, 8, 155));
        DrawWorldLine(activePoint, routeEnd, 1f,
            new Color(244, 187, 57, 178));

        Vector2 converging = center - nextPoint;
        if (converging.LengthSquared() > .001f)
        {
            converging.Normalize();
            DrawWorldLine(nextPoint,
                nextPoint + converging * 14f,
                1f,
                new Color(229, 176, 55, 104));
        }
    }

    private void DrawSovereignPortals(GameSession gameSession)
    {
        IReadOnlyList<SovereignPortal> portals =
            gameSession.SovereignPortals.Portals;
        for (int index = 0; index < portals.Count; index++)
        {
            SovereignPortal portal = portals[index];
            float hierarchy = GetSovereignPortalHierarchy(
                gameSession,
                portal);
            Vector2? directionOverride = null;
            float contactPulse = 0f;
            bool dominant = false;
            if (portal.ConstellationIndex >= 0 &&
                gameSession.IsSovereignUltimateActive)
            {
                int stage = gameSession.SovereignUltimateStage;
                int active = SovereignPortalSystem
                    .GetUltimateStepPortalIndex(stage);
                if (portal.ConstellationIndex == active)
                {
                    int next = stage < AbyssRingTuning.UltimateStrikeCount
                        ? SovereignPortalSystem
                            .GetUltimateStepPortalIndex(stage + 1)
                        : AbyssRingTuning.UltimateConstellationCount - 1;
                    Vector2 destination = SovereignPortalSystem
                        .GetUltimateConstellationPosition(
                            gameSession.SovereignUltimateTargetBounds
                                .Center.ToVector2(),
                            next);
                    Vector2 launch = destination - portal.Position;
                    if (launch.LengthSquared() > .001f)
                    {
                        launch.Normalize();
                        directionOverride = launch;
                    }
                    float contactCenter = stage ==
                            AbyssRingTuning.UltimateStrikeCount + 1
                        ? .25f
                        : .18f;
                    contactPulse = 1f - Math.Clamp(
                        MathF.Abs(gameSession.SovereignUltimateStageRatio -
                            contactCenter) / .18f,
                        0f,
                        1f);
                }
                dominant = portal.ConstellationIndex ==
                        AbyssRingTuning.UltimateConstellationCount - 1 &&
                    stage >= AbyssRingTuning.UltimateStrikeCount - 1;
            }
            DrawSovereignPortal(
                portal,
                index,
                hierarchy,
                directionOverride,
                contactPulse,
                dominant);
        }
    }

    private static float GetSovereignPortalHierarchy(
        GameSession gameSession,
        SovereignPortal portal)
    {
        if (portal.Type != SovereignPortalType.Decorative && portal.IsLogical)
            return 1f;
        if (!gameSession.IsSovereignUltimateActive ||
            portal.Type != SovereignPortalType.Decorative)
            return .38f;

        int stage = Math.Max(0, gameSession.SovereignUltimateStage);
        int active = SovereignPortalSystem.GetUltimateStepPortalIndex(stage);
        int next = stage < AbyssRingTuning.UltimateStrikeCount
            ? SovereignPortalSystem.GetUltimateStepPortalIndex(stage + 1)
            : AbyssRingTuning.UltimateConstellationCount - 1;
        if (portal.ConstellationIndex == active)
            return 1f;
        if (portal.ConstellationIndex == next)
            return .62f;
        if (stage == AbyssRingTuning.UltimateStrikeCount + 1 &&
            portal.ConstellationIndex ==
                AbyssRingTuning.UltimateConstellationCount - 1)
            return .94f;
        return stage <= 0 ? .46f : .25f;
    }

    private void DrawSovereignPortal(
        SovereignPortal portal,
        int portalIndex,
        float hierarchy,
        Vector2? directionOverride,
        float contactPulse,
        bool dominant)
    {
        bool decorative = portal.Type == SovereignPortalType.Decorative ||
            !portal.IsLogical;
        float visibility = portal.Phase switch
        {
            SovereignPortalPhase.Anticipating =>
                .18f + portal.PhaseRatio * .42f,
            SovereignPortalPhase.Closing => portal.PhaseRatio,
            _ => 1f
        } * hierarchy;
        float usePulse = portal.Phase == SovereignPortalPhase.Active
            ? MathF.Sin(Math.Min(1f, portal.ActiveRatio * 3.4f) *
                MathHelper.Pi)
            : 0f;
        if (portal.IsPortalStep)
        {
            float stepPulse = portal.Flow switch
            {
                SovereignPortalFlow.Entry when
                    portal.Phase == SovereignPortalPhase.Anticipating =>
                        MathF.Sin(portal.PhaseRatio * MathHelper.Pi),
                SovereignPortalFlow.Entry when
                    portal.Phase == SovereignPortalPhase.Opening =>
                        1f - portal.PhaseRatio,
                SovereignPortalFlow.Exit when
                    portal.Phase == SovereignPortalPhase.Stabilizing =>
                        MathF.Sin(portal.PhaseRatio * MathHelper.Pi),
                SovereignPortalFlow.Exit when
                    portal.Phase == SovereignPortalPhase.Active =>
                        1f - portal.ActiveRatio,
                _ => 0f
            };
            usePulse = Math.Max(usePulse, stepPulse);
        }
        usePulse = Math.Max(usePulse, contactPulse);
        float open = Math.Max(.04f, portal.OpenRatio);
        float radiusX = (portal.Type == SovereignPortalType.Strike
                ? 14f
                : 18f) * portal.Scale * open;
        float radiusY = (portal.Type == SovereignPortalType.Strike
                ? 27f
                : 32f) * portal.Scale * open;
        if (portal.Flow == SovereignPortalFlow.Entry &&
            portal.Type == SovereignPortalType.Strike)
        {
            radiusX *= 1f - usePulse * .12f;
            radiusY *= 1f - usePulse * .08f;
        }
        else if (portal.Flow == SovereignPortalFlow.Exit &&
            portal.Type == SovereignPortalType.Transit)
        {
            radiusX *= 1f - usePulse * .10f;
        }
        if (dominant)
        {
            radiusX *= 1.16f;
            radiusY *= 1.18f;
        }
        Vector2 direction = directionOverride ?? portal.Direction;
        Vector2 vertical = new(-direction.Y, direction.X);

        if (portal.Phase == SovereignPortalPhase.Anticipating)
        {
            float sparkLength = 5f + 12f * portal.PhaseRatio;
            DrawWorldLine(
                portal.Position - vertical * sparkLength,
                portal.Position + vertical * sparkLength,
                1f + portal.PhaseRatio,
                PortalColor(new Color(255, 220, 116, 230), visibility));
            return;
        }

        DrawPortalCore(portal, direction, vertical, radiusX, radiusY,
            visibility);
        DrawPortalFracturedRing(portal, portalIndex, direction, vertical,
            radiusX, radiusY, visibility, usePulse);
        DrawPortalRuneRing(portal, portalIndex, direction, vertical,
            radiusX * .72f, radiusY * .75f, visibility);
        DrawPortalEnergyFlow(portal, direction, vertical, radiusX, radiusY,
            visibility, usePulse);
        if ((portal.Type == SovereignPortalType.Transit &&
                portal.Flow == SovereignPortalFlow.Exit ||
                portal.ConstellationIndex >= 0 && contactPulse > .04f) &&
            usePulse > .04f)
        {
            DrawPortalPushOff(portal, direction, vertical, radiusX, radiusY,
                visibility, usePulse);
        }

        if (portal.Type == SovereignPortalType.Transit &&
            portal.Flow != SovereignPortalFlow.Neutral &&
            !decorative)
        {
            DrawTransitBodyEcho(portal, direction, vertical, radiusY,
                visibility);
        }
    }

    private void DrawPortalCore(
        SovereignPortal portal,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float visibility)
    {
        Color voidEdge = PortalColor(new Color(22, 17, 25, 225), visibility);
        Color voidCore = PortalColor(new Color(3, 3, 7, 244), visibility);
        for (int band = -4; band <= 4; band++)
        {
            float acrossRatio = band / 4.7f;
            float across = acrossRatio * radiusX;
            float halfHeight = radiusY * .88f * MathF.Sqrt(
                Math.Max(0f, 1f - acrossRatio * acrossRatio));
            Vector2 center = portal.Position + direction * across;
            DrawWorldLine(
                center - vertical * halfHeight,
                center + vertical * halfHeight,
                Math.Max(2f, radiusX * .25f),
                band is -4 or 4 ? voidEdge : voidCore);
        }

        float distortion = .6f + .4f * MathF.Sin(portal.Age * 19f);
        DrawPortalArc(portal.Position, direction, vertical,
            radiusX * 1.12f, radiusY * 1.08f,
            portal.Age * .8f, MathHelper.Pi * .72f, 1f,
            PortalColor(new Color(98, 72, 34, 75),
                visibility * distortion));
        DrawPortalArc(portal.Position, direction, vertical,
            radiusX * 1.08f, radiusY * 1.14f,
            MathHelper.Pi + portal.Age * .8f, MathHelper.Pi * .72f, 1f,
            PortalColor(new Color(98, 72, 34, 75),
                visibility * distortion));
    }

    private void DrawPortalFracturedRing(
        SovereignPortal portal,
        int portalIndex,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float visibility,
        float usePulse)
    {
        const int segmentCount = 16;
        float rotation = portal.Age *
            (portal.Type == SovereignPortalType.Strike ? 2.1f : 1.15f) +
            portalIndex * .37f;
        Color shadow = PortalColor(new Color(92, 59, 13, 120), visibility);
        Color metal = PortalColor(new Color(211, 151, 35, 220), visibility);
        Color edge = PortalColor(new Color(255, 224, 124, 245),
            visibility * (.66f + usePulse * .34f));

        for (int segment = 0; segment < segmentCount; segment++)
        {
            if ((segment + portalIndex) % 5 == 2)
                continue;
            float start = MathHelper.TwoPi * segment / segmentCount + rotation;
            float span = MathHelper.TwoPi / segmentCount *
                (segment % 3 == 0 ? .54f : .72f);
            float fracture = 1f + (segment % 4 - 1.5f) * .018f;
            DrawPortalArc(portal.Position, direction, vertical,
                radiusX * fracture, radiusY * fracture,
                start, span, portal.IsLogical ? 5f : 3f, shadow);
            DrawPortalArc(portal.Position, direction, vertical,
                radiusX * fracture, radiusY * fracture,
                start, span, portal.IsLogical ? 2f : 1f,
                segment % 4 == 0 ? edge : metal);
        }
    }

    private void DrawPortalRuneRing(
        SovereignPortal portal,
        int portalIndex,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float visibility)
    {
        float rotation = -portal.Age * 1.55f + portalIndex * .23f;
        Color rune = PortalColor(new Color(235, 190, 78, 175),
            visibility * .78f);
        for (int index = 0; index < 8; index++)
        {
            float angle = MathHelper.TwoPi * index / 8f + rotation;
            Vector2 point = PortalPoint(portal.Position, direction, vertical,
                radiusX, radiusY, angle);
            Vector2 tangent = Vector2.Normalize(
                direction * (-MathF.Sin(angle) * radiusX) +
                vertical * (MathF.Cos(angle) * radiusY));
            float length = index % 2 == 0 ? 5f : 3f;
            DrawWorldLine(point - tangent * length * .5f,
                point + tangent * length * .5f, 1f, rune);
            if (index % 2 == 0)
            {
                Vector2 inward = Vector2.Normalize(portal.Position - point);
                DrawWorldLine(point, point + inward * 3f, 1f, rune);
            }
        }
    }

    private void DrawPortalEnergyFlow(
        SovereignPortal portal,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float visibility,
        float usePulse)
    {
        Color flow = PortalColor(new Color(248, 194, 62, 190),
            visibility * (.55f + usePulse * .45f));
        float flowSign = portal.Flow == SovereignPortalFlow.Entry ? -1f : 1f;
        if (portal.Flow == SovereignPortalFlow.Neutral)
            flowSign = portal.Type == SovereignPortalType.Strike ? 1f : 0f;

        for (int index = -2; index <= 2; index++)
        {
            float lane = index * radiusY * .17f;
            float phase = (portal.Age * 5.5f + index * .19f) % 1f;
            float near = radiusX * (.22f + phase * .42f);
            float far = near + radiusX * (.34f + usePulse * .20f);
            Vector2 offset = vertical * lane;
            Vector2 from = portal.Position + offset + direction * near * flowSign;
            Vector2 to = portal.Position + offset + direction * far * flowSign;
            if (portal.Flow == SovereignPortalFlow.Entry)
                (from, to) = (to, from);
            DrawWorldLine(from, to, index == 0 ? 2f : 1f, flow);
        }

        if (portal.Type == SovereignPortalType.Strike && usePulse > .05f)
        {
            for (int index = -2; index <= 2; index++)
            {
                Vector2 ray = direction * radiusX * (1.1f + usePulse) +
                    vertical * index * 5f;
                DrawWorldLine(portal.Position + direction * radiusX * .45f,
                    portal.Position + ray, index == 0 ? 3f : 1f,
                    PortalColor(new Color(255, 211, 91, 210),
                        visibility * usePulse));
            }
        }
    }

    private void DrawTransitBodyEcho(
        SovereignPortal portal,
        Vector2 direction,
        Vector2 vertical,
        float radiusY,
        float visibility)
    {
        float use = portal.Phase == SovereignPortalPhase.Active
            ? 1f - Math.Min(1f, portal.ActiveRatio * 2.4f)
            : portal.Phase == SovereignPortalPhase.Stabilizing
                ? portal.PhaseRatio
                : 0f;
        if (use <= .02f)
            return;

        float sign = portal.Flow == SovereignPortalFlow.Entry ? -1f : 1f;
        Color shadow = PortalColor(new Color(10, 8, 14, 150),
            visibility * use);
        Color seam = PortalColor(new Color(223, 166, 48, 125),
            visibility * use);
        for (int echo = 1; echo <= 3; echo++)
        {
            Vector2 center = portal.Position - direction * sign * echo * 7f;
            float fade = 1f - echo * .22f;
            DrawWorldLine(center - vertical * radiusY * .48f,
                center + vertical * radiusY * .24f,
                Math.Max(3f, 8f - echo), PortalColor(shadow, fade));
            DrawWorldLine(center - vertical * radiusY * .08f,
                center + direction * sign * (13f + echo * 3f),
                2f, PortalColor(seam, fade));
        }
    }

    private void DrawPortalPushOff(
        SovereignPortal portal,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float visibility,
        float pulse)
    {
        Vector2 contact = portal.Position + vertical * radiusY * .62f -
            direction * radiusX * .18f;
        Color burst = PortalColor(new Color(255, 211, 92, 225),
            visibility * pulse);
        for (int index = -1; index <= 1; index++)
        {
            Vector2 ray = direction * (9f + pulse * 10f) +
                vertical * index * 5f;
            DrawWorldLine(contact, contact + ray,
                index == 0 ? 2f : 1f, burst);
        }
        DrawPortalArc(portal.Position, direction, vertical,
            radiusX * (1.04f + pulse * .13f),
            radiusY * (1.02f + pulse * .08f),
            -.72f, 1.44f, 1f, burst);
    }

    private void DrawPortalArc(
        Vector2 center,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float start,
        float span,
        float thickness,
        Color color)
    {
        const int steps = 3;
        Vector2 previous = PortalPoint(center, direction, vertical,
            radiusX, radiusY, start);
        for (int step = 1; step <= steps; step++)
        {
            float angle = start + span * step / steps;
            Vector2 next = PortalPoint(center, direction, vertical,
                radiusX, radiusY, angle);
            DrawWorldLine(previous, next, thickness, color);
            previous = next;
        }
    }

    private static Vector2 PortalPoint(
        Vector2 center,
        Vector2 direction,
        Vector2 vertical,
        float radiusX,
        float radiusY,
        float angle) =>
        center + direction * (MathF.Cos(angle) * radiusX) +
        vertical * (MathF.Sin(angle) * radiusY);

    private static Color PortalColor(Color color, float opacity) => new(
        color.R,
        color.G,
        color.B,
        (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255));

    private void DrawReapMarks(
        Rectangle target,
        Vector2 consumeDestination,
        int count,
        float lifetime,
        float gain,
        float refresh,
        float exit,
        int exitCount,
        bool consumed)
    {
        count = Math.Clamp(count, 0, ScytheTuning.MaximumReapMarks);
        int visibleCount = count > 0
            ? count
            : Math.Clamp(exitCount, 0,
                ScytheTuning.MaximumReapMarks);
        if (visibleCount <= 0 || count <= 0 && exit <= 0f)
            return;

        Vector2 markCenter = new(
            target.Center.X + Math.Clamp(target.Width * .20f, 4f, 15f),
            target.Top + Math.Clamp(target.Height * .30f, 12f, 27f));
        float opacity = count > 0
            ? Math.Clamp(lifetime / .07f, 0f, 1f)
            : exit;
        float scale = 1f;

        if (count <= 0 && consumed)
        {
            float travel = MathHelper.SmoothStep(0f, 1f, 1f - exit);
            Vector2 destination = consumeDestination == Vector2.Zero
                ? markCenter
                : consumeDestination;
            markCenter = Vector2.Lerp(markCenter, destination, travel);
            scale = MathHelper.Lerp(1f, .42f, travel);
            DrawWorldLine(
                Vector2.Lerp(markCenter, destination, -.18f),
                markCenter,
                2f,
                new Color(58, 19, 76, (int)(150f * exit)));
        }
        else if (count <= 0)
        {
            scale = MathHelper.Lerp(.28f, 1f, exit);
            DrawWorldLine(markCenter + new Vector2(-7f * exit, -5f),
                markCenter + new Vector2(-2f, -1f), 1f,
                new Color(119, 61, 151, (int)(110f * exit)));
            DrawWorldLine(markCenter + new Vector2(3f, 1f),
                markCenter + new Vector2(8f * exit, 5f), 1f,
                new Color(45, 14, 57, (int)(145f * exit)));
        }

        float pulse = refresh > 0f
            ? MathF.Sin((1f - refresh) * MathHelper.Pi)
            : 0f;
        scale *= 1f + pulse * .10f;
        Color dark = new(40, 14, 52, (int)(215f * opacity));
        Color violet = new(151, 77, 196,
            (int)(220f * Math.Clamp(opacity + pulse * .18f, 0f, 1f)));
        Color cold = new(193, 151, 219,
            (int)(145f * opacity));

        for (int index = 0; index < visibleCount; index++)
        {
            Vector2 offset = visibleCount switch
            {
                1 => Vector2.Zero,
                2 => new Vector2(index == 0 ? -3.5f : 3.5f,
                    index == 0 ? 2f : -2f),
                _ => index switch
                {
                    0 => new Vector2(-4.5f, 2.5f),
                    1 => new Vector2(4.5f, 2.5f),
                    _ => new Vector2(0f, -4.5f)
                }
            };
            float angle = visibleCount < 3
                ? -.28f + index * .56f
                : index * MathHelper.TwoPi / 3f - MathHelper.PiOver2;
            float reveal = count > 0 && index == count - 1 && gain > 0f
                ? Math.Clamp((1f - gain) * 1.45f, .12f, 1f)
                : 1f;
            DrawReapMarkCrescent(markCenter + offset * scale,
                angle, 6.5f * scale, reveal, dark, violet);
        }

        if (visibleCount == ScytheTuning.MaximumReapMarks)
        {
            DrawWorldLine(markCenter + new Vector2(-2.5f, 2f) * scale,
                markCenter + new Vector2(0f, -2.5f) * scale,
                1f, cold);
            DrawWorldLine(markCenter + new Vector2(0f, -2.5f) * scale,
                markCenter + new Vector2(2.5f, 2f) * scale,
                1f, cold);
        }

        if (gain > 0f && count > 0)
        {
            float trace = 1f - gain;
            DrawWorldLine(markCenter + new Vector2(-11f + trace * 6f, 5f),
                markCenter + new Vector2(4f + trace * 8f, -6f),
                1f,
                new Color(127, 58, 173, (int)(155f * gain)));
        }
        if (refresh > 0f)
        {
            float ripple = 5f + (1f - refresh) * 7f;
            Vector2 shadow = new(target.Center.X, target.Bottom - 2f);
            DrawWorldLine(shadow + new Vector2(-ripple, 0f),
                shadow + new Vector2(ripple, 0f), 1f,
                new Color(55, 19, 67, (int)(80f * refresh)));
        }
    }

    private void DrawReapMarkCrescent(
        Vector2 center,
        float angle,
        float radius,
        float reveal,
        Color dark,
        Color violet)
    {
        const int segments = 5;
        float start = angle - 1.12f;
        Vector2 previous = center + new Vector2(
            MathF.Cos(start), MathF.Sin(start)) * radius;
        int visibleSegments = Math.Max(1,
            (int)MathF.Ceiling(segments * reveal));
        for (int index = 1; index <= visibleSegments; index++)
        {
            float t = Math.Min(reveal, index / (float)segments);
            float sample = start + 2.24f * t;
            Vector2 next = center + new Vector2(
                MathF.Cos(sample), MathF.Sin(sample)) * radius;
            DrawWorldLine(previous, next, 3f, dark);
            DrawWorldLine(previous, next, 1f, violet);
            previous = next;
        }
    }

    private void DrawScytheUltimateShadowField(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsScytheUltimateActive ||
            !gameSession.IsScytheUltimateTargetLocked)
            return;
        Rectangle target = gameSession.ScytheUltimateTargetBounds;
        if (target.IsEmpty)
            return;

        int stage = gameSession.ScytheUltimateStage;
        float ratio = gameSession.ScytheUltimateStageRatio;
        float intro = gameSession.Player.Combat.ScytheUltimateCinematicRatio;
        float formation = gameSession.Player.Combat.State ==
                CombatState.ChargingHeavy
            ? MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(intro / .34f, 0f, 1f))
            : 1f;
        if (stage == 7)
            formation *= 1f - ratio;
        Vector2 shadow = new(target.Center.X, target.Bottom - 3f);
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 5.4f);
        float pressure = stage is >= 1 and <= 3
            ? (stage + ratio) / 4f
            : stage >= 4 ? 1f : .18f + formation * .22f;
        float halfWidth = (target.Width * .42f + 14f) * formation;
        DrawWorldLine(shadow + new Vector2(-halfWidth, 1f),
            shadow + new Vector2(halfWidth, 1f),
            9f + pressure * 6f,
            new Color(12, 5, 17, (int)(105f + pressure * 55f)));
        DrawWorldLine(shadow + new Vector2(-halfWidth * .82f, -1f),
            shadow + new Vector2(halfWidth * .82f, -1f), 3f,
            new Color(55, 19, 72, (int)(80f + pulse * 34f)));

        if (stage == 0)
        {
            Vector2 origin = gameSession.ScytheUltimateVisualStartPosition +
                new Vector2(0f, gameSession.Player.Size.Y * .5f - 3f);
            float approach = Math.Clamp(ratio / .70f, 0f, 1f);
            Vector2 seamEnd = Vector2.Lerp(origin, shadow, approach);
            DrawWorldLine(origin, seamEnd, 3f,
                new Color(35, 11, 46, (int)(145f * (1f - ratio * .45f))));
            DrawWorldLine(Vector2.Lerp(origin, seamEnd, .35f), seamEnd, 1f,
                new Color(117, 54, 148, (int)(115f * (1f - ratio * .35f))));
        }

        if (stage is >= 1 and <= 3)
        {
            float squeeze = stage == 3
                ? 1f - MathF.Sin(ratio * MathHelper.Pi) * .22f
                : 1f;
            DrawScytheUltimateArcWorld(shadow, halfWidth * squeeze, 9f,
                MathHelper.Pi, MathHelper.Pi, 8, 2f,
                new Color(93, 37, 119, 135));
            for (int index = 0; index < stage + 1; index++)
            {
                float x = (index - stage * .5f) * 7f;
                float rise = 10f + index * 6f + ratio * 5f;
                DrawWorldLine(shadow + new Vector2(x, 0f),
                    shadow + new Vector2(x * .65f, -rise), 1f,
                    new Color(70, 25, 88,
                        (int)(75f + pressure * 55f)));
            }
        }

        if (stage is 4 or 5)
        {
            float tear = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(ratio / .42f, 0f, 1f));
            DrawWorldLine(shadow + new Vector2(0f, 4f),
                shadow + new Vector2((stage == 4 ? -1f : 1f) *
                    (10f + tear * 17f), -target.Height * .72f),
                7f, new Color(15, 5, 20, (int)(190f * (1f - ratio * .35f))));
            DrawWorldLine(shadow + new Vector2(1f, 2f),
                shadow + new Vector2((stage == 4 ? -1f : 1f) *
                    (10f + tear * 17f), -target.Height * .72f),
                2f, new Color(132, 60, 168, (int)(175f * (1f - ratio * .45f))));
        }

        if (stage == 6 && ratio > .52f)
        {
            float breaking = Math.Clamp((ratio - .52f) / .48f, 0f, 1f);
            float ripple = halfWidth * (.55f + breaking * .95f);
            DrawWorldLine(shadow + new Vector2(-ripple, 0f),
                shadow + new Vector2(ripple, 0f), 2f,
                new Color(69, 22, 84, (int)(145f * (1f - breaking))));
            for (int piece = -2; piece <= 2; piece++)
            {
                float x = piece * (5f + breaking * 7f);
                DrawWorldLine(shadow + new Vector2(x, -2f),
                    shadow + new Vector2(x + piece * 2f, -5f - breaking * 7f),
                    1f, new Color(48, 14, 60,
                        (int)(125f * (1f - breaking))));
            }
        }
    }

    private void DrawScytheUltimateTargetLock(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsScytheUltimateTargetLocked)
            return;
        Rectangle target = gameSession.ScytheUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        int stage = gameSession.ScytheUltimateStage;
        float ratio = gameSession.ScytheUltimateStageRatio;
        float intro = gameSession.Player.Combat.ScytheUltimateCinematicRatio;
        float formation = gameSession.Player.Combat.State ==
                CombatState.ChargingHeavy
            ? MathHelper.SmoothStep(0f, 1f,
                Math.Clamp(intro / .38f, 0f, 1f))
            : 1f;
        if (stage == 7)
            formation *= 1f - ratio;
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 6f);
        Vector2 seal = new(
            target.Center.X + Math.Clamp(target.Width * .20f, 6f, 18f),
            target.Top + Math.Clamp(target.Height * .28f, 15f, 33f));
        float sealFlash = stage == 6
            ? MathF.Sin(Math.Clamp(ratio / .42f, 0f, 1f) * MathHelper.Pi)
            : 0f;
        float sealRadius = (11f + pulse * 1.2f + sealFlash * 3f) * formation;
        DrawScytheUltimateArcWorld(seal, sealRadius, sealRadius * .72f,
            -2.28f, 4.20f, 9, 4f,
            new Color(27, 8, 36, (int)(220f * formation)));
        DrawScytheUltimateArcWorld(seal, sealRadius, sealRadius * .72f,
            -2.28f, 4.20f * formation, 9, 1f,
            new Color(155, 75, 194,
                (int)((175f + sealFlash * 60f) * formation)));
        DrawWorldLine(seal + new Vector2(-sealRadius * .36f, -2f),
            seal + new Vector2(sealRadius * .32f, 3f), 1f,
            new Color(144, 39, 61,
                (int)((90f + sealFlash * 95f) * formation)));

        if (stage is >= 1 and <= 3)
        {
            Vector2 soul = target.Center.ToVector2() +
                new Vector2(stage % 2 == 0 ? 3f : -3f, -2f);
            Color soulColor = new(151, 112, 177,
                (int)(42f + stage * 12f));
            DrawWorldLine(soul + new Vector2(-target.Width * .22f,
                    -target.Height * .34f),
                soul + new Vector2(target.Width * .18f,
                    -target.Height * .34f), 2f, soulColor);
            DrawWorldLine(soul + new Vector2(-target.Width * .20f,
                    -target.Height * .30f),
                soul + new Vector2(-target.Width * .13f,
                    target.Height * .28f), 2f, soulColor);
            DrawWorldLine(soul + new Vector2(target.Width * .18f,
                    -target.Height * .30f),
                soul + new Vector2(target.Width * .14f,
                    target.Height * .28f), 2f, soulColor);

            float cut = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .18f) / .48f, 0f, 1f));
            Vector2 from;
            Vector2 to;
            if (stage == 1)
            {
                from = new Vector2(target.Left + 5f, target.Center.Y + 5f);
                to = Vector2.Lerp(from,
                    new Vector2(target.Right - 5f, target.Center.Y - 7f), cut);
            }
            else if (stage == 2)
            {
                from = new Vector2(target.Right - 4f, target.Top + 10f);
                to = Vector2.Lerp(from,
                    new Vector2(target.Left + 7f, target.Bottom - 12f), cut);
            }
            else
            {
                float arcSpan = MathHelper.TwoPi * cut;
                DrawScytheUltimateArcWorld(target.Center.ToVector2(),
                    target.Width * .38f, target.Height * .30f,
                    -1.4f, arcSpan, 11, 5f,
                    new Color(31, 8, 40, (int)(170f * (1f - ratio * .35f))));
                DrawScytheUltimateArcWorld(target.Center.ToVector2(),
                    target.Width * .38f, target.Height * .30f,
                    -1.4f, arcSpan, 11, 1.5f,
                    new Color(166, 85, 207, (int)(190f * (1f - ratio * .25f))));
                return;
            }
            DrawWorldLine(from, to, 5f,
                new Color(30, 8, 39, (int)(180f * (1f - ratio * .35f))));
            DrawWorldLine(from, to, 1.5f,
                new Color(163, 82, 202, (int)(205f * (1f - ratio * .28f))));
        }
        else if (stage is 4 or 5)
        {
            float cut = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .12f) / .58f, 0f, 1f));
            Vector2 from = stage == 4
                ? new Vector2(target.Left - 9f, target.Bottom - 3f)
                : new Vector2(target.Right + 9f, target.Bottom - 3f);
            Vector2 finish = stage == 4
                ? new Vector2(target.Right + 7f, target.Top + 4f)
                : new Vector2(target.Left - 7f, target.Top + 4f);
            DrawWorldLine(from, Vector2.Lerp(from, finish, cut), 2f,
                new Color(116, 48, 147, (int)(145f * (1f - ratio * .35f))));
        }
        else if (stage == 6 && ratio > .44f)
        {
            float fracture = Math.Clamp((ratio - .44f) / .56f, 0f, 1f);
            Vector2 center = target.Center.ToVector2();
            DrawWorldLine(center + new Vector2(-target.Width * .34f, -3f),
                center + new Vector2(target.Width * .34f, 4f), 2f,
                new Color(126, 35, 57, (int)(180f * (1f - fracture))));
            DrawWorldLine(center + new Vector2(-4f, 0f),
                center + new Vector2(-12f - fracture * 9f,
                    target.Height * .28f), 1f,
                new Color(83, 27, 101, (int)(150f * (1f - fracture))));
        }
    }

    private void DrawScytheUltimateArcWorld(
        Vector2 center,
        float radiusX,
        float radiusY,
        float start,
        float span,
        int segments,
        float thickness,
        Color color)
    {
        Vector2 previous = center + new Vector2(
            MathF.Cos(start) * radiusX,
            MathF.Sin(start) * radiusY);
        for (int index = 1; index <= segments; index++)
        {
            float angle = start + span * index / segments;
            Vector2 next = center + new Vector2(
                MathF.Cos(angle) * radiusX,
                MathF.Sin(angle) * radiusY);
            DrawWorldLine(previous, next, thickness, color);
            previous = next;
        }
    }

    private void DrawScytheUltimateCinematic(GameSession gameSession)
    {
        float ratio = gameSession.Player.Combat.ScytheUltimateCinematicRatio;
        if (ratio <= 0f && !gameSession.IsScytheUltimateActive)
            return;
        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        if (ratio > 0f)
        {
            float fade = MathF.Sin(ratio * MathHelper.Pi);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(7, 5, 9, (int)(126f * fade)));

            float portrait = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .08f) / .30f, 0f, 1f)) *
                (1f - MathHelper.SmoothStep(0f, 1f,
                    Math.Clamp((ratio - .76f) / .22f, 0f, 1f)));
            if (portrait > 0f)
            {
                float scale = MathHelper.Lerp(3.7f, 4.35f,
                    Math.Clamp(ratio / .58f, 0f, 1f));
                Vector2 portraitAnchor = new(width * .30f, height * .90f);
                _spriteBatch.End();
                _spriteBatch.Begin(
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: Matrix.CreateScale(scale) *
                        Matrix.CreateTranslation(
                            portraitAnchor.X, portraitAnchor.Y, 0f));
                _playerSpriteRenderer.DrawScytheCinematicCutIn(
                    gameSession.Player,
                    Vector2.Zero,
                    ratio);
                _spriteBatch.End();
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            }

            float sweep = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .28f) / .28f, 0f, 1f));
            float exit = MathHelper.SmoothStep(0f, 1f,
                Math.Clamp((ratio - .82f) / .18f, 0f, 1f));
            if (sweep > 0f)
            {
                Vector2 center = new(width * (.60f + exit * .36f),
                    height * .49f);
                _spriteBatch.Draw(_pixel, center, null,
                    new Color(25, 7, 34, (int)(220f * fade)), -.17f,
                    new Vector2(.5f, .5f),
                    new Vector2(width * .76f * sweep,
                        Math.Max(76f, height * .15f)),
                    SpriteEffects.None, 0f);
                DrawWorldLine(new Vector2(width * (.16f + exit * .74f),
                        height * .68f),
                    new Vector2(width * (.95f + exit * .44f),
                        height * .25f), 4f,
                    new Color(122, 56, 157, (int)(205f * fade)));
                DrawWorldLine(new Vector2(width * (.25f + exit * .68f),
                        height * .72f),
                    new Vector2(width * (.88f + exit * .50f),
                        height * .38f), 2f,
                    new Color(140, 40, 65, (int)(130f * fade)));
                for (int strip = 0; strip < 4; strip++)
                {
                    float y = height * (.31f + strip * .09f);
                    DrawWorldLine(
                        new Vector2(width * (.54f + exit * .52f), y + 22f),
                        new Vector2(width * (.91f + exit * .64f), y - 18f),
                        strip == 1 ? 3f : 2f,
                        new Color(48, 16, 61, (int)(130f * fade)));
                }
            }
            if (ratio >= .48f && ratio < .94f)
                DrawCenteredDebugText("UMBRA DEVOURER", width * 2 / 3,
                    height / 2 - 58, new Color(194, 139, 215), 3);
            if (ratio >= .68f && ratio < .94f)
                DrawCenteredDebugText("YOUR SHADOW IS MINE.", width * 2 / 3,
                    height / 2 - 13, new Color(198, 185, 203), 2);
            return;
        }

        int stage = gameSession.ScytheUltimateStage;
        float stageRatio = gameSession.ScytheUltimateStageRatio;
        if (stage is >= 1 and <= 3)
        {
            int shade = 22 + stage * 7;
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                new Color(4, 2, 7, shade));
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, 42),
                new Color(7, 2, 10, 68));
            _spriteBatch.Draw(_pixel, new Rectangle(0, height - 42, width, 42),
                new Color(7, 2, 10, 68));
        }
        else if (stage == 6)
        {
            AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
            float active = attack == null
                ? -1f
                : gameSession.Player.Combat.StateElapsed - attack.StartupTime;
            float impact = active >= 0f && active <= .055f
                ? 1f - active / .055f
                : 0f;
            float silence = 1f - Math.Clamp(stageRatio / .34f, 0f, 1f);
            int shade = (int)(44f * silence + 112f * impact);
            if (shade > 0)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                    new Color(4, 2, 6, shade));
                Vector2 impactCenter = new(width * .54f, height * .50f);
                DrawWorldLine(impactCenter + new Vector2(-width * .38f,
                        height * .24f),
                    impactCenter + new Vector2(width * .38f,
                        -height * .25f), 4f,
                    new Color(163, 91, 194, (int)(220f * impact)));
                DrawWorldLine(impactCenter + new Vector2(-18f, 4f),
                    impactCenter + new Vector2(18f, -4f), 2f,
                    new Color(151, 37, 60, (int)(190f * impact)));
            }
        }
    }

    private void DrawRangerUltimateCinematic(GameSession gameSession)
    {
        float ratio = gameSession.Player.Combat.RangerUltimateCinematicRatio;
        if (ratio <= 0f)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        float fade = MathF.Sin(ratio * MathHelper.Pi);
        byte alpha = (byte)Math.Clamp(
            (int)MathF.Round(116f * fade),
            0,
            116);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, width, height),
            new Color(6, 10, 16, (int)alpha));

        float sweep = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .20f) / .34f, 0f, 1f));
        if (sweep > 0f)
        {
            Vector2 bannerCenter = new(width * .58f, height * .48f);
            float bannerWidth = width * .78f * sweep;
            _spriteBatch.Draw(
                _pixel,
                bannerCenter,
                sourceRectangle: null,
                new Color(12, 42, 67, (int)(205f * fade)),
                -.16f,
                new Vector2(.5f, .5f),
                new Vector2(bannerWidth, Math.Max(72f, height * .15f)),
                SpriteEffects.None,
                layerDepth: 0f);
            DrawWorldLine(
                new Vector2(width * .12f, height * .66f),
                new Vector2(width * .94f, height * .25f),
                5f,
                new Color(74, 224, 255, (int)(210f * fade)));
            DrawWorldLine(
                new Vector2(width * .20f, height * .72f),
                new Vector2(width * .88f, height * .38f),
                2f,
                new Color(177, 246, 255, (int)(180f * fade)));
            for (int index = 0; index < 6; index++)
            {
                float offset = index * height * .055f;
                DrawWorldLine(
                    new Vector2(width * (.04f + index * .025f),
                        height * .30f + offset),
                    new Vector2(width * (.34f + index * .045f),
                        height * .16f + offset),
                    index % 2 == 0 ? 3f : 2f,
                    new Color(69, 191, 235, (int)(145f * fade)));
            }
        }

        float portrait = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .16f) / .28f, 0f, 1f));
        if (portrait > 0f)
        {
            float portraitScale = MathHelper.Lerp(3.05f, 3.70f, portrait);
            Vector2 portraitAnchor = new(width * .28f, height * .86f);
            _spriteBatch.End();
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(portraitScale) *
                    Matrix.CreateTranslation(
                        portraitAnchor.X,
                        portraitAnchor.Y,
                        0f));
            _playerSpriteRenderer.DrawCinematicCutIn(
                gameSession.Player,
                Vector2.Zero);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        if (ratio < .48f)
            return;

        DrawCenteredDebugText(
            Loc.Get("weapon.phantom_hunt.name"),
            width / 2,
            height / 2 - 82,
            new Color(190, 242, 255),
            3);
        if (ratio >= .72f)
        {
            DrawCenteredDebugText(
                Loc.Get("weapon.phantom_hunt.voice"),
                width / 2,
                height / 2 - 34,
                new Color(210, 220, 215),
                2);
        }
    }

    private void DrawRangerUltimateTargetLock(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsRangerUltimateTargetLocked)
            return;

        Rectangle target = gameSession.RangerUltimateTargetBounds;
        if (target.Width <= 0 || target.Height <= 0)
            return;

        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 9f);
        int inset = 5 + (int)MathF.Round(pulse * 3f);
        Rectangle lockBounds = target;
        lockBounds.Inflate(inset, inset);
        Color cyan = new(77, 222, 247, 205);
        int corner = Math.Max(8, Math.Min(18, lockBounds.Width / 3));
        DrawWorldLine(new Vector2(lockBounds.Left, lockBounds.Top),
            new Vector2(lockBounds.Left + corner, lockBounds.Top), 2f, cyan);
        DrawWorldLine(new Vector2(lockBounds.Left, lockBounds.Top),
            new Vector2(lockBounds.Left, lockBounds.Top + corner), 2f, cyan);
        DrawWorldLine(new Vector2(lockBounds.Right, lockBounds.Top),
            new Vector2(lockBounds.Right - corner, lockBounds.Top), 2f, cyan);
        DrawWorldLine(new Vector2(lockBounds.Right, lockBounds.Top),
            new Vector2(lockBounds.Right, lockBounds.Top + corner), 2f, cyan);
        DrawWorldLine(new Vector2(lockBounds.Left, lockBounds.Bottom),
            new Vector2(lockBounds.Left + corner, lockBounds.Bottom), 2f, cyan);
        DrawWorldLine(new Vector2(lockBounds.Right, lockBounds.Bottom),
            new Vector2(lockBounds.Right - corner, lockBounds.Bottom), 2f, cyan);
    }

    private void DrawGreatswordUltimateCinematic(GameSession gameSession)
    {
        float ratio =
            gameSession.Player.Combat.GreatswordUltimateCinematicRatio;
        if (ratio <= 0f)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        float fade = MathF.Sin(ratio * MathHelper.Pi);
        int shadeAlpha = Math.Clamp(
            (int)MathF.Round(150f * fade),
            0,
            150);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, width, height),
            new Color(4, 5, 7, shadeAlpha));

        float sweep = MathHelper.SmoothStep(
            0f,
            1f,
            Math.Clamp((ratio - .16f) / .30f, 0f, 1f));
        if (sweep > 0f)
        {
            Vector2 center = new(width * .58f, height * .48f);
            _spriteBatch.Draw(
                _pixel,
                center,
                sourceRectangle: null,
                new Color(12, 13, 15, (int)(225f * fade)),
                -.14f,
                new Vector2(.5f, .5f),
                new Vector2(width * .82f * sweep,
                    Math.Max(82f, height * .18f)),
                SpriteEffects.None,
                layerDepth: 0f);
            DrawWorldLine(
                new Vector2(width * .10f, height * .68f),
                new Vector2(width * .94f, height * .24f),
                6f,
                new Color(190, 148, 55, (int)(220f * fade)));
            DrawWorldLine(
                new Vector2(width * .16f, height * .72f),
                new Vector2(width * .90f, height * .34f),
                2f,
                new Color(245, 218, 142, (int)(185f * fade)));
            for (int index = 0; index < 7; index++)
            {
                float y = height * (.20f + index * .065f);
                DrawWorldLine(
                    new Vector2(width * (.04f + index * .018f), y + 42f),
                    new Vector2(width * (.31f + index * .035f), y - 18f),
                    index % 3 == 0 ? 4f : 2f,
                    new Color(143, 124, 82, (int)(150f * fade)));
            }
        }

        float portrait = MathHelper.SmoothStep(
            0f,
            1f,
            Math.Clamp((ratio - .12f) / .28f, 0f, 1f));
        if (portrait > 0f)
        {
            float scale = MathHelper.Lerp(3.15f, 3.85f, portrait);
            Vector2 anchor = new(width * .29f, height * .87f);
            _spriteBatch.End();
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(scale) *
                    Matrix.CreateTranslation(anchor.X, anchor.Y, 0f));
            _playerSpriteRenderer.DrawCinematicCutIn(
                gameSession.Player,
                Vector2.Zero);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        if (ratio < .45f)
            return;

        DrawCenteredDebugText(
            Loc.Get("weapon.final_verdict.name"),
            width / 2,
            height / 2 - 84,
            new Color(244, 215, 139),
            3);
        if (ratio >= .72f)
        {
            DrawCenteredDebugText(
                Loc.Get("weapon.final_verdict.voice"),
                width / 2,
                height / 2 - 35,
                new Color(221, 219, 208),
                2);
        }
    }

    private void DrawKnightUltimateCinematic(GameSession gameSession)
    {
        float ratio = gameSession.Player.Combat.KnightUltimateCinematicRatio;
        if (ratio <= 0f)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
		float fadeIn = MathHelper.SmoothStep(0f, 1f,
			Math.Clamp(ratio / .13f, 0f, 1f));
		float fadeOut = 1f - MathHelper.SmoothStep(0f, 1f,
			Math.Clamp((ratio - .86f) / .14f, 0f, 1f));
		float visibility = fadeIn * fadeOut;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
			new Color(7, 12, 18, Math.Clamp((int)(205f * visibility), 0, 205)));

		float composition = MathHelper.SmoothStep(0f, 1f,
			Math.Clamp((ratio - .07f) / .24f, 0f, 1f));
		float slabWidth = width * .88f * composition;
		Rectangle slab = new(
			(int)(width * .50f - slabWidth * .50f),
			(int)(height * .28f),
			(int)slabWidth,
			(int)(height * .45f));
		_spriteBatch.Draw(_pixel, slab,
			new Color(18, 28, 36, (int)(184f * visibility)));
		_spriteBatch.Draw(_pixel,
			new Rectangle(slab.X, slab.Y, slab.Width, Math.Max(2, height / 180)),
			new Color(137, 161, 171, (int)(180f * visibility)));
		_spriteBatch.Draw(_pixel,
			new Rectangle(slab.X, slab.Bottom - Math.Max(2, height / 180),
				slab.Width, Math.Max(2, height / 180)),
			new Color(72, 107, 123, (int)(160f * visibility)));

        float reveal = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .10f) / .28f, 0f, 1f));
        if (reveal > 0f)
        {
			DrawWorldLine(new Vector2(width * .10f, height * .70f),
				new Vector2(width * .92f, height * .34f), 4f,
				new Color(116, 153, 169, (int)(210f * visibility)));
			DrawWorldLine(new Vector2(width * .13f, height * .73f),
				new Vector2(width * .88f, height * .40f), 1f,
				new Color(209, 220, 222, (int)(135f * visibility)));
			float shieldRadius = MathHelper.Lerp(16f, 27f, reveal);
			Vector2 shieldCenter = new(width * .73f, height * .52f);
			for (int side = -1; side <= 1; side += 2)
			{
				DrawWorldLine(shieldCenter + new Vector2(side * shieldRadius, -shieldRadius),
					shieldCenter + new Vector2(side * shieldRadius, shieldRadius * .45f),
					2f, new Color(156, 178, 186, (int)(140f * visibility)));
				DrawWorldLine(shieldCenter + new Vector2(side * shieldRadius, shieldRadius * .45f),
					shieldCenter + new Vector2(0f, shieldRadius), 2f,
					new Color(91, 130, 148, (int)(140f * visibility)));
			}
			float scale = MathHelper.Lerp(3.0f, 3.48f, reveal);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(scale) *
                    Matrix.CreateTranslation(width * .28f, height * .87f, 0f));
			_playerSpriteRenderer.DrawKnightCinematicCutIn(
				gameSession.Player, Vector2.Zero, reveal);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        if (ratio >= .42f)
            DrawCenteredDebugText(Loc.Get("weapon.knight_final_oath.name"),
                width / 2, height / 2 - 82,
				new Color(220, 232, 235, (int)(255f * visibility)), 3);
        if (ratio >= .70f)
            DrawCenteredDebugText(Loc.Get("weapon.knight_final_oath.voice"),
                width / 2, height / 2 - 40,
				new Color(147, 181, 196, (int)(255f * visibility)), 2);
    }

    private void DrawSovereignUltimateCinematic(GameSession gameSession)
    {
        float ratio = gameSession.Player.Combat.SovereignUltimateCinematicRatio;
        if (ratio <= 0f)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        float fadeIn = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp(ratio / .14f, 0f, 1f));
        float fadeOut = 1f - MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .88f) / .12f, 0f, 1f));
        float visibility = fadeIn * fadeOut;
        int shadeAlpha = (int)(MathHelper.Lerp(82f, 218f,
            Math.Clamp((ratio - .16f) / .30f, 0f, 1f)) * visibility);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
            new Color(3, 3, 6, shadeAlpha));

        Rectangle target = gameSession.SovereignUltimateTargetBounds;
        if (ratio < .26f && !target.IsEmpty)
        {
            Vector2 targetScreen = Vector2.Transform(
                target.Center.ToVector2(),
                gameSession.Camera.Transform);
            float judgment = MathHelper.SmoothStep(0f, 1f, ratio / .18f);
            float radius = 24f - judgment * 8f;
            Color mark = new(246, 195, 65, (int)(210f * visibility));
            for (int corner = 0; corner < 4; corner++)
            {
                float angle = MathHelper.PiOver2 * corner + .78f;
                Vector2 inward = new(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 tangent = new(-inward.Y, inward.X);
                Vector2 point = targetScreen + inward * radius;
                DrawWorldLine(point, point - inward * 8f, 2f, mark);
                DrawWorldLine(point, point + tangent * 5f, 1f, mark);
            }
        }

        float composition = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .16f) / .24f, 0f, 1f));
        if (composition > 0f)
        {
            float bannerWidth = width * .92f * composition;
            Vector2 bannerCenter = new(width * .52f, height * .52f);
            _spriteBatch.Draw(
                _pixel,
                bannerCenter,
                sourceRectangle: null,
                new Color(7, 6, 10, (int)(242f * visibility)),
                -.16f,
                new Vector2(.5f, .5f),
                new Vector2(bannerWidth, Math.Max(92f, height * .20f)),
                SpriteEffects.None,
                layerDepth: 0f);
            DrawWorldLine(
                new Vector2(width * .04f, height * .72f),
                new Vector2(width * .96f, height * .25f),
                2f,
                new Color(250, 198, 66, (int)(225f * visibility)));

            Vector2 runeCenter = new(width * .76f, height * .45f);
            for (int ring = 0; ring < 3; ring++)
            {
                float radiusX = 42f + ring * 25f;
                float radiusY = radiusX * (.48f + ring * .035f);
                float spin = ratio * (ring % 2 == 0 ? 1.1f : -1.3f);
                for (int segment = 0; segment < 8; segment += 2)
                {
                    float start = MathHelper.TwoPi * segment / 8f + spin;
                    DrawPortalArc(runeCenter, Vector2.UnitX, Vector2.UnitY,
                        radiusX, radiusY, start, .62f, 1f,
                        new Color(214, 158, 43,
                            (int)((72f - ring * 12f) * visibility)));
                }
            }
        }

        float portrait = MathHelper.SmoothStep(0f, 1f,
            Math.Clamp((ratio - .22f) / .24f, 0f, 1f));
        if (portrait > 0f)
        {
            float portraitScale = MathHelper.Lerp(4.15f, 4.72f, portrait);
            _spriteBatch.End();
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(portraitScale) *
                    Matrix.CreateTranslation(
                        width * .19f,
                        height * 1.02f,
                        0f));
            _playerSpriteRenderer.DrawSovereignCinematicCutIn(
                gameSession.Player, Vector2.Zero);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        if (ratio >= .48f)
            DrawCenteredDebugText(
                Loc.Get("weapon.golden_sovereign_cataclysm.name"),
                width / 2,
                height / 2 - 76,
                new Color(255, 213, 91, (int)(255f * visibility)),
                3);
        if (ratio >= .70f)
            DrawCenteredDebugText(
                Loc.Get("weapon.golden_sovereign_cataclysm.voice"),
                width / 2,
                height / 2 - 29,
                new Color(235, 220, 174, (int)(235f * visibility)),
                2);
    }

    private void DrawSovereignAssaultOverlay(GameSession gameSession)
    {
        if (!gameSession.IsSovereignUltimateActive ||
            gameSession.Player.Combat.State == CombatState.ChargingHeavy)
            return;
        int stage = gameSession.SovereignUltimateStage;
        AttackDefinition attack = gameSession.Player.Combat.CurrentAttack;
        if (attack == null)
            return;

        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        if (stage is >= 1 and <= 16)
        {
            float acceleration = (stage - 1f) / 15f;
            int bar = 7 + (int)MathF.Round(acceleration * 7f);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, bar),
                new Color(2, 2, 5, 112));
            _spriteBatch.Draw(_pixel,
                new Rectangle(0, height - bar, width, bar),
                new Color(2, 2, 5, 112));

            bool cadenceBeat = stage is 4 or 8 or 12;
            float recoveryStart = attack.StartupTime + attack.ActiveTime;
            float recoveryRatio = Math.Clamp(
                (gameSession.Player.Combat.StateElapsed - recoveryStart) /
                    Math.Max(.01f, attack.RecoveryTime),
                0f,
                1f);
            if (cadenceBeat && recoveryRatio > 0f)
            {
                float pause = MathF.Sin(recoveryRatio * MathHelper.Pi);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(0, 0, width, height),
                    new Color(2, 2, 5, (int)(54f * pause)));
            }

            if (stage >= 9)
            {
                float streakAlpha = stage >= 13 ? 82f : 52f;
                float slope = stage % 2 == 0 ? -1f : 1f;
                for (int streak = 0; streak < (stage >= 13 ? 3 : 2); streak++)
                {
                    float y = height * (.28f + streak * .18f);
                    DrawWorldLine(
                        new Vector2(width * .12f, y),
                        new Vector2(width * .42f,
                            y + slope * (18f + streak * 7f)),
                        1f,
                        new Color(225, 169, 48, (int)streakAlpha));
                }
            }
            return;
        }

        if (stage != 17)
            return;
        float activeElapsed = gameSession.Player.Combat.StateElapsed -
            attack.StartupTime;
        float impact = 1f - Math.Clamp(
            MathF.Abs(activeElapsed - attack.ActiveTime * .36f) /
                Math.Max(.025f, attack.ActiveTime * .34f),
            0f,
            1f);
        if (impact <= 0f)
            return;

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
            new Color(1, 1, 3, (int)(188f * impact)));
        Rectangle target = gameSession.SovereignUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        Vector2 targetScreen = Vector2.Transform(
            target.Center.ToVector2(),
            gameSession.Camera.Transform);
        Color gold = new(255, 207, 76, (int)(240f * impact));
        DrawWorldLine(targetScreen - new Vector2(0f, height * .32f),
            targetScreen, 5f, gold);
        for (int crack = -3; crack <= 3; crack++)
        {
            Vector2 end = targetScreen + new Vector2(
                crack * 13f,
                18f + Math.Abs(crack) * 7f);
            DrawWorldLine(targetScreen, end,
                crack == 0 ? 2f : 1f, gold);
        }
    }

    private void DrawSovereignUltimateTargetLock(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsSovereignUltimateTargetLocked)
            return;
        Rectangle target = gameSession.SovereignUltimateTargetBounds;
        if (target.IsEmpty)
            return;
        int stage = gameSession.SovereignUltimateStage;
        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 9f);
        Rectangle frame = target;
        frame.Inflate(8 + (int)(pulse * 3f), 7 + (int)(pulse * 2f));
        Color gold = stage <= 0
            ? new Color(250, 205, 76, 225)
            : new Color(231, 178, 50, 145);
        int corner = Math.Max(7, Math.Min(15, frame.Width / 3));
        DrawWorldLine(new Vector2(frame.Left, frame.Top),
            new Vector2(frame.Left + corner, frame.Top), 1f, gold);
        DrawWorldLine(new Vector2(frame.Left, frame.Top),
            new Vector2(frame.Left, frame.Top + corner), 1f, gold);
        DrawWorldLine(new Vector2(frame.Right, frame.Top),
            new Vector2(frame.Right - corner, frame.Top), 1f, gold);
        DrawWorldLine(new Vector2(frame.Right, frame.Top),
            new Vector2(frame.Right, frame.Top + corner), 1f, gold);
        DrawWorldLine(new Vector2(frame.Left, frame.Bottom),
            new Vector2(frame.Left + corner, frame.Bottom), 1f, gold);
        DrawWorldLine(new Vector2(frame.Right, frame.Bottom),
            new Vector2(frame.Right - corner, frame.Bottom), 1f, gold);

        Vector2 targetCenter = target.Center.ToVector2();
        float convergence = stage <= 0 ? .78f : .42f;
        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            float angle = directionIndex * MathHelper.PiOver2 +
                MathHelper.PiOver4;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float outerDistance = Math.Max(target.Width, target.Height) *
                (.72f + pulse * .08f);
            Vector2 outer = targetCenter + direction * outerDistance;
            Vector2 inner = targetCenter + direction *
                (outerDistance * .56f);
            DrawWorldLine(outer, inner, 2f,
                new Color(4, 3, 8, (int)(165f * convergence)));
            DrawWorldLine(outer, inner, 1f,
                new Color(235, 179, 51, (int)(150f * convergence)));
        }
        DrawPortalArc(targetCenter, Vector2.UnitX, Vector2.UnitY,
            target.Width * .34f + 6f,
            target.Height * .24f + 4f,
            stage * .27f + pulse * .12f,
            MathHelper.Pi * .72f,
            1f,
            new Color(219, 165, 45, stage <= 0 ? 150 : 76));
        DrawPortalArc(targetCenter, Vector2.UnitX, Vector2.UnitY,
            target.Width * .34f + 6f,
            target.Height * .24f + 4f,
            MathHelper.Pi + stage * .27f + pulse * .12f,
            MathHelper.Pi * .72f,
            1f,
            new Color(219, 165, 45, stage <= 0 ? 150 : 76));

        if (stage is >= 1 and <= 16)
        {
            for (int band = -1; band <= 1; band++)
            {
                float y = target.Center.Y + band * target.Height * .22f;
                DrawWorldLine(
                    new Vector2(target.Left - 4f, y),
                    new Vector2(target.Left + target.Width * .22f, y + 2f),
                    1f,
                    new Color(213, 164, 48, 95));
                DrawWorldLine(
                    new Vector2(target.Right + 4f, y),
                    new Vector2(target.Right - target.Width * .22f, y - 2f),
                    1f,
                    new Color(213, 164, 48, 95));
            }
        }
        else if (stage == 17)
        {
            Vector2 center = target.Center.ToVector2();
            float contract = MathHelper.Lerp(1.25f, .84f,
                gameSession.SovereignUltimateStageRatio);
            DrawPortalArc(center, Vector2.UnitX, Vector2.UnitY,
                (target.Width * .5f + 12f) * contract,
                (target.Height * .5f + 8f) * contract,
                -.35f, 2.24f, 2f, new Color(255, 208, 76, 220));
            DrawPortalArc(center, Vector2.UnitX, Vector2.UnitY,
                (target.Width * .5f + 12f) * contract,
                (target.Height * .5f + 8f) * contract,
                MathHelper.Pi - .35f, 2.24f, 2f,
                new Color(255, 208, 76, 220));
        }
    }

    private void DrawSovereignExecutionFrame(GameSession gameSession)
    {
        if (!gameSession.IsSovereignExecutionActive)
            return;

        float executionTime = gameSession.SovereignExecutionElapsedSeconds;
        static float Phase(float time, float start, float end) =>
            Math.Clamp((time - start) / MathF.Max(.001f, end - start),
                0f, 1f);
        int width = _graphicsDevice.Viewport.Width;
        int height = _graphicsDevice.Viewport.Height;
        bool suppressed = gameSession.IsSovereignExecutionSuppressed;
        float fadeIn = MathHelper.SmoothStep(0f, 1f,
            Phase(executionTime, 0f,
                AbyssRingTuning.ExecutionSilenceEndSeconds));
        float fadeOut = 1f - MathHelper.SmoothStep(0f, 1f,
            Phase(executionTime,
                AbyssRingTuning.ExecutionShatterEndSeconds,
                AbyssRingTuning.ExecutionDurationSeconds));
        float visibility = fadeIn * fadeOut;
        Color gold = suppressed
            ? new Color(154, 124, 74, (int)(215f * visibility))
            : new Color(255, 207, 73, (int)(245f * visibility));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, width, height),
            new Color(1, 1, 4, (int)(238f * visibility)));

        float eyeReveal = MathHelper.SmoothStep(0f, 1f, Phase(
            executionTime,
            AbyssRingTuning.ExecutionSilenceEndSeconds,
            AbyssRingTuning.ExecutionEyeEndSeconds));
        float eyeFade = 1f - MathHelper.SmoothStep(0f, 1f,
            Phase(executionTime,
                AbyssRingTuning.ExecutionRingEndSeconds,
                AbyssRingTuning.ExecutionPortalEndSeconds));
        float eyeVisibility = eyeReveal * eyeFade * visibility;
        if (eyeVisibility > 0f)
        {
            Vector2 eye = new(width * .43f, height * .36f);
            float eyeWidth = MathHelper.Lerp(38f, 158f, eyeReveal);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle((int)(eye.X - eyeWidth), (int)eye.Y - 25,
                    (int)(eyeWidth * 2f), 50),
                new Color(8, 7, 11, (int)(250f * eyeVisibility)));
            DrawWorldLine(eye - new Vector2(eyeWidth * .88f, 3f),
                eye + new Vector2(eyeWidth * .88f, 2f),
                3f,
                PortalColor(gold, eyeVisibility));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)eye.X - 6, (int)eye.Y - 15, 12, 30),
                PortalColor(gold, eyeVisibility));
            DrawPortalArc(eye, Vector2.UnitX, Vector2.UnitY,
                20f, 11f, -.5f, 2.1f, 1f,
                PortalColor(new Color(248, 191, 57, 145), eyeVisibility));
        }

        float handGateReveal = MathHelper.SmoothStep(0f, 1f, Phase(
            executionTime,
            AbyssRingTuning.ExecutionEyeEndSeconds,
            AbyssRingTuning.ExecutionRingEndSeconds));
        float lastGateReveal = MathHelper.SmoothStep(0f, 1f, Phase(
            executionTime,
            AbyssRingTuning.ExecutionRingEndSeconds,
            AbyssRingTuning.ExecutionPortalEndSeconds));
        float portalFade = 1f - MathHelper.SmoothStep(0f, 1f,
            Phase(executionTime,
                AbyssRingTuning.ExecutionHitStopEndSeconds,
                AbyssRingTuning.ExecutionShatterEndSeconds));
        float portalVisibility = portalFade * visibility;
        Vector2 handGate = new(width * .39f, height * .62f);
        Vector2 lastGate = new(width * .72f, height * .55f);
        if (handGateReveal * portalVisibility > 0f)
        {
            DrawExecutionPortal(handGate, 18f, 33f,
                PortalColor(gold,
                    handGateReveal * portalVisibility * .82f));
            if (lastGateReveal > 0f)
                DrawExecutionPortal(lastGate, 14f, 27f,
                    PortalColor(gold,
                        lastGateReveal * portalVisibility));

            float anticipation = MathHelper.SmoothStep(0f, 1f, Phase(
                executionTime,
                AbyssRingTuning.ExecutionPortalEndSeconds,
                AbyssRingTuning.ExecutionAnticipationEndSeconds));
            float release = MathHelper.SmoothStep(0f, 1f, Phase(
                executionTime,
                AbyssRingTuning.ExecutionAnticipationEndSeconds,
                AbyssRingTuning.ExecutionImpactSeconds));
            float knuckle = MathHelper.Lerp(
                anticipation * .28f,
                1f,
                release);
            Vector2 armStart = new(width * .10f, height * .68f);
            Vector2 armEnd = Vector2.Lerp(armStart, handGate, knuckle);
            DrawWorldLine(armStart, armEnd, 18f,
                new Color(8, 7, 11,
                    (int)(245f * handGateReveal * portalVisibility)));
            DrawWorldLine(armStart, armEnd, 3f,
                PortalColor(gold,
                    handGateReveal * portalVisibility * .72f));

            Vector2 target = new(width * .79f, height * .59f);
            Vector2 fist = Vector2.Lerp(lastGate, target, knuckle);
            DrawWorldLine(lastGate, fist, 16f,
                new Color(7, 6, 10,
                    (int)(250f * lastGateReveal * portalVisibility)));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)fist.X - 8, (int)fist.Y - 7, 17, 14),
                PortalColor(gold,
                    lastGateReveal * portalVisibility));
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)target.X - 14, (int)target.Y - 40,
                    28, 68),
                new Color(5, 5, 8,
                    (int)(235f * lastGateReveal * portalVisibility)));
        }

        float impact = executionTime >= AbyssRingTuning.ExecutionImpactSeconds
            ? 1f - MathHelper.SmoothStep(0f, 1f, Phase(
                executionTime,
                AbyssRingTuning.ExecutionImpactSeconds,
                AbyssRingTuning.ExecutionHitStopEndSeconds))
            : 0f;
        if (impact > 0f)
        {
            Vector2 impactPoint = new(width * .79f, height * .56f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(0, (int)(height * .48f), width,
                    Math.Max(4, (int)(height * .16f))),
                new Color(2, 2, 4, (int)(230f * impact)));
            DrawWorldLine(new Vector2(width * .42f, height * .67f),
                impactPoint, 5f, PortalColor(gold, impact));
            for (int crack = 0; crack < 8; crack++)
            {
                float angle = MathHelper.TwoPi * crack / 8f + .18f;
                float length = 24f + crack % 3 * 12f;
                Vector2 middle = impactPoint + new Vector2(
                    MathF.Cos(angle) * length * .48f,
                    MathF.Sin(angle) * length * .48f);
                Vector2 end = impactPoint + new Vector2(
                    MathF.Cos(angle) * length,
                    MathF.Sin(angle) * length);
                DrawWorldLine(impactPoint, middle, 2f,
                    PortalColor(gold, impact));
                DrawWorldLine(middle,
                    end + new Vector2(crack % 2 == 0 ? 5f : -5f, 3f),
                    1f,
                    PortalColor(gold, impact * .72f));
            }
        }

        if (executionTime >= AbyssRingTuning.ExecutionHitStopEndSeconds)
        {
            float shatter = MathHelper.SmoothStep(0f, 1f, Phase(
                executionTime,
                AbyssRingTuning.ExecutionHitStopEndSeconds,
                AbyssRingTuning.ExecutionShatterEndSeconds));
            Vector2 center = new(width * .79f, height * .56f);
            for (int shard = 0; shard < 9; shard++)
            {
                float angle = MathHelper.TwoPi * shard / 9f + .12f;
                float inner = 18f + shatter * 22f;
                float outer = inner + 12f + shard % 3 * 5f;
                Vector2 from = center + new Vector2(
                    MathF.Cos(angle) * inner,
                    MathF.Sin(angle) * inner);
                Vector2 to = center + new Vector2(
                    MathF.Cos(angle) * outer,
                    MathF.Sin(angle) * outer);
                DrawWorldLine(from, to, shard % 3 == 0 ? 2f : 1f,
                    PortalColor(gold, (1f - shatter) * .82f));
            }
        }

        if (executionTime >= AbyssRingTuning.ExecutionEyeEndSeconds &&
            executionTime < AbyssRingTuning.ExecutionShatterEndSeconds)
            DrawCenteredDebugText(
                Loc.Get("weapon.sovereign_execution.name"),
                width / 2,
                (int)(height * .77f),
                gold,
                3);
        if (executionTime >= AbyssRingTuning.ExecutionPortalEndSeconds &&
            executionTime < AbyssRingTuning.ExecutionAnticipationEndSeconds &&
            !suppressed)
        {
            DrawCenteredDebugText(
                Loc.Get("weapon.sovereign_execution.voice"),
                width / 2,
                (int)(height * .84f),
                new Color(235, 220, 174, (int)(230f * visibility)),
                2);
        }

        if (executionTime >= AbyssRingTuning.ExecutionShatterEndSeconds &&
            !suppressed)
        {
            float pose = MathHelper.SmoothStep(0f, 1f,
                Phase(executionTime,
                    AbyssRingTuning.ExecutionShatterEndSeconds,
                    AbyssRingTuning.ExecutionDurationSeconds));
            _spriteBatch.End();
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(2.75f) *
                    Matrix.CreateTranslation(
                        width * .24f,
                        height * (.96f + (1f - pose) * .04f),
                        0f));
            _playerSpriteRenderer.DrawSovereignCinematicCutIn(
                gameSession.Player, Vector2.Zero);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }
    }

    private void DrawExecutionPortal(
        Vector2 center,
        float radiusX,
        float radiusY,
        Color gold)
    {
        DrawWorldLine(center - new Vector2(radiusX * .55f, 0f),
            center + new Vector2(radiusX * .55f, 0f),
            radiusY * 1.28f,
            new Color(2, 2, 5, (int)gold.A));
        for (int segment = 0; segment < 10; segment++)
        {
            if (segment is 2 or 7)
                continue;
            float start = MathHelper.TwoPi * segment / 10f + .12f;
            DrawPortalArc(center, Vector2.UnitX, Vector2.UnitY,
                radiusX, radiusY, start, .46f, 2f, gold);
        }
    }

    private void DrawGreatswordUltimateTargetLock(
        GameSession gameSession,
        GameTime gameTime)
    {
        if (!gameSession.IsGreatswordUltimateTargetLocked)
            return;

        Rectangle target = gameSession.GreatswordUltimateTargetBounds;
        if (target.IsEmpty)
            return;

        float pulse = .5f + .5f * MathF.Sin(
            (float)gameTime.TotalGameTime.TotalSeconds * 7f);
        Rectangle bounds = target;
		bool knight = gameSession.IsKnightUltimateTargetLocked;
		bounds.Inflate(
			knight ? 4 + (int)MathF.Round(pulse * 2f) :
				7 + (int)MathF.Round(pulse * 4f),
			knight ? 4 : 6);
		Color gold = knight
			? new Color(137, 175, 190, 195)
			: new Color(224, 181, 82, 215);
        int corner = Math.Max(9, Math.Min(20, bounds.Width / 3));
        DrawWorldLine(new Vector2(bounds.Left, bounds.Top),
			new Vector2(bounds.Left + corner, bounds.Top), knight ? 2f : 3f, gold);
        DrawWorldLine(new Vector2(bounds.Left, bounds.Top),
			new Vector2(bounds.Left, bounds.Top + corner), knight ? 2f : 3f, gold);
        DrawWorldLine(new Vector2(bounds.Right, bounds.Top),
			new Vector2(bounds.Right - corner, bounds.Top), knight ? 2f : 3f, gold);
        DrawWorldLine(new Vector2(bounds.Right, bounds.Top),
			new Vector2(bounds.Right, bounds.Top + corner), knight ? 2f : 3f, gold);
        DrawWorldLine(new Vector2(bounds.Left, bounds.Bottom),
			new Vector2(bounds.Left + corner, bounds.Bottom), knight ? 2f : 3f, gold);
        DrawWorldLine(new Vector2(bounds.Right, bounds.Bottom),
			new Vector2(bounds.Right - corner, bounds.Bottom), knight ? 2f : 3f, gold);
    }

    private void DrawTechniqueFeedback(PlayerCharacter player)
    {
        if (!player.Combat.IsTechniqueFeedbackVisible ||
            string.IsNullOrWhiteSpace(player.Combat.TechniqueFeedback))
            return;

        DrawDebugLabel(
            player.Combat.TechniqueFeedback,
            270,
            64,
            new Color(229, 207, 139));
    }

    private void DrawPlayerHealth(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 22;
        const int barWidth = 198;
        const int barHeight = 16;
        const int borderWidth = 2;

        float healthRatio = player.CurrentHealth / (float)player.MaxHealth;
        int fillWidth = (int)((barWidth - borderWidth * 2) * healthRatio);

        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);
        Rectangle health = new(
            background.X,
            background.Y,
            fillWidth,
            background.Height);

        _spriteBatch.Draw(_pixel, new Rectangle(20, 24, 14, 10), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, new Rectangle(23, 21, 4, 16), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, new Rectangle(29, 21, 4, 16), new Color(196, 48, 58));
        _spriteBatch.Draw(_pixel, border, new Color(185, 190, 194));
        _spriteBatch.Draw(_pixel, background, new Color(62, 25, 30));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, health, new Color(196, 48, 58));
    }

    private void DrawPlayerProgression(PlayerCharacter player, int killCount)
    {
        const int barX = 42;
        const int barY = 59;
        const int barWidth = 198;
        const int barHeight = 10;
        const int borderWidth = 2;
        const int levelIndicatorY = 75;
        const int levelIndicatorSize = 8;
        const int levelIndicatorSpacing = 4;
        const int killIndicatorY = 89;
        const int killIndicatorSize = 6;
        const int killIndicatorSpacing = 3;

        float experienceRatio = player.CurrentExperience /
            (float)player.ExperienceToNextLevel;
        int fillWidth = (int)((barWidth - borderWidth * 2) * experienceRatio);

        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + borderWidth,
            barY + borderWidth,
            barWidth - borderWidth * 2,
            barHeight - borderWidth * 2);
        Rectangle experience = new(
            background.X,
            background.Y,
            fillWidth,
            background.Height);

        _spriteBatch.Draw(_pixel, new Rectangle(22, barY + 1, 10, 8), new Color(87, 132, 222));
        _spriteBatch.Draw(_pixel, border, new Color(185, 190, 194));
        _spriteBatch.Draw(_pixel, background, new Color(28, 35, 66));

        if (fillWidth > 0)
            _spriteBatch.Draw(_pixel, experience, new Color(87, 132, 222));

        _spriteBatch.Draw(_pixel, new Rectangle(21, levelIndicatorY, 11, 8), new Color(235, 190, 62));
        int visibleLevel = Math.Min(player.Level, 17);

        for (int level = 0; level < visibleLevel; level++)
        {
            Rectangle indicator = new(
                barX + level * (levelIndicatorSize + levelIndicatorSpacing),
                levelIndicatorY,
                levelIndicatorSize,
                levelIndicatorSize);
            _spriteBatch.Draw(_pixel, indicator, new Color(235, 190, 62));
        }

        _spriteBatch.Draw(_pixel, new Rectangle(22, killIndicatorY, 9, 6), new Color(181, 72, 66));
        int visibleKills = Math.Min(killCount, 22);

        for (int kill = 0; kill < visibleKills; kill++)
        {
            Rectangle indicator = new(
                barX + kill * (killIndicatorSize + killIndicatorSpacing),
                killIndicatorY,
                killIndicatorSize,
                killIndicatorSize);
            _spriteBatch.Draw(_pixel, indicator, new Color(181, 72, 66));
        }
    }

    private void DrawCharacterSelect(GameSession session, GameTime gameTime)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        int gap = Math.Clamp(viewport.Width / 57, 18, 36);
        int preferredMargin = Math.Clamp(viewport.Width / 25, 42, 86);
        int panelWidth = Math.Clamp(
            (viewport.Width - preferredMargin * 2 - gap * 2) /
                SaveSlotManager.SlotCount,
            230,
            540);
        int margin = Math.Max(
            18,
            (viewport.Width - panelWidth * SaveSlotManager.SlotCount -
                gap * (SaveSlotManager.SlotCount - 1)) / 2);
        int top = Math.Clamp((int)MathF.Round(viewport.Height * .11f), 74, 112);
        int panelHeight = Math.Clamp(
            (int)MathF.Round(viewport.Height * .70f),
            430,
            756);
        double time = gameTime.TotalGameTime.TotalSeconds;

        _graphicsDevice.Clear(new Color(6, 9, 12));
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_pixel, screen, new Color(7, 10, 14));
        int bandHeight = Math.Max(1, screen.Height / 6);
        for (int band = 0; band < 6; band++)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(0, band * bandHeight, screen.Width, bandHeight),
                new Color(8 + band * 2, 13 + band * 3, 17 + band * 2));
        }

        int mistShift = (int)MathF.Round(MathF.Sin((float)time * .23f) * 34f);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(-60 + mistShift, top + 28, screen.Width / 2, 18),
            new Color(105, 119, 116, 18));
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(screen.Width / 2 - mistShift, top + 112, screen.Width / 2, 13),
            new Color(118, 125, 119, 14));

        DrawCenteredText(
            Loc.Get("character.select.title"),
            screen.Center.X,
            17,
            new Color(225, 188, 90),
            viewport.Width >= 1800 ? 4 : viewport.Width >= 1100 ? 3 : 2);
        DrawCenteredText(
            Loc.Get("character.select.subtitle"),
            screen.Center.X,
            viewport.Width >= 1800 ? 52 : viewport.Width >= 1100 ? 46 : 39,
            new Color(123, 130, 130),
            viewport.Width >= 1600 ? 2 : 1);

        for (int index = 0; index < SaveSlotManager.SlotCount; index++)
        {
            Rectangle panel = GetCharacterSlotPanel(
                index,
                margin,
                gap,
                top,
                panelWidth,
                panelHeight);
            bool selected = index == session.CharacterSelect.SelectedSlotIndex;
            DrawCharacterSlotBase(panel, index, selected, session.SaveSlots[index].Occupied);
        }

        SaveSlotData selectedSlot =
            session.SaveSlots[session.CharacterSelect.SelectedSlotIndex];
        DrawCenteredText(
            Loc.Get(selectedSlot.Occupied
                ? "character.select.delete"
                : "character.select.controls"),
            screen.Center.X,
            screen.Height - (viewport.Height >= 900 ? 34 : 24),
            new Color(166, 171, 169),
            viewport.Width >= 1600 ? 2 : 1);
        _spriteBatch.End();

        for (int index = 0; index < SaveSlotManager.SlotCount; index++)
        {
            SaveSlotData slot = session.SaveSlots[index];
            PlayerCharacter previewPlayer = session.CharacterSelect.GetPreview(index);
            if (!slot.Occupied || previewPlayer == null)
                continue;

            Rectangle panel = GetCharacterSlotPanel(
                index,
                margin,
                gap,
                top,
                panelWidth,
                panelHeight);
            float largeWindowProgress = Math.Clamp(
                (viewport.Height - 720f) / 360f,
                0f,
                1f);
            float scale = Math.Clamp(
                1.65f + largeWindowProgress * 1.05f,
                1.55f,
                2.70f);
            var anchor = new Vector2(
                panel.Center.X / scale,
                GetCharacterPreviewGroundY(panel) / scale);
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(scale));
            _playerSpriteRenderer.DrawPreview(
                previewPlayer,
                gameTime,
                anchor,
                FacingDirection.Right);
            _spriteBatch.End();
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        for (int index = 0; index < SaveSlotManager.SlotCount; index++)
        {
            Rectangle panel = GetCharacterSlotPanel(
                index,
                margin,
                gap,
                top,
                panelWidth,
                panelHeight);
            DrawCharacterSlotDetails(panel, session.SaveSlots[index]);
        }

        if (!string.IsNullOrEmpty(session.SaveError))
        {
            DrawCenteredText(
                Loc.Get("character.save_unavailable"),
                screen.Center.X,
                screen.Height - 38,
                new Color(211, 91, 83),
                1);
        }

        if (session.CharacterSelect.IsDeleteConfirmationOpen)
            DrawDeleteSlotConfirmation(session);
        _spriteBatch.End();
    }

    private static Rectangle GetCharacterSlotPanel(
        int index,
        int margin,
        int gap,
        int top,
        int panelWidth,
        int panelHeight)
    {
        return new Rectangle(
            margin + index * (panelWidth + gap),
            top,
            panelWidth,
            panelHeight);
    }

    private void DrawCharacterSlotBase(
        Rectangle panel,
        int index,
        bool selected,
        bool occupied)
    {
        Color frame = selected
            ? new Color(214, 169, 71)
            : new Color(82, 82, 78);
        _spriteBatch.Draw(
            _pixel,
            panel,
            selected ? new Color(25, 28, 33, 250) : new Color(16, 20, 26, 245));
        DrawRectangleOutline(panel, selected ? 4 : 2, frame);
        DrawRectangleOutline(
            new Rectangle(panel.X + 6, panel.Y + 6, panel.Width - 12, panel.Height - 12),
            1,
            selected ? new Color(103, 79, 43) : new Color(45, 51, 56));

        DrawCenteredText(
            Loc.Format("character.slot", index + 1),
            panel.Center.X,
            panel.Y + 13,
            selected ? new Color(235, 207, 137) : new Color(137, 142, 141),
            panel.Width >= 470 ? 2 : 1);

        int pedestalY = GetCharacterPreviewGroundY(panel) + 6;
        var pedestal = new Rectangle(
            panel.Center.X - Math.Min(82, panel.Width / 3),
            pedestalY,
            Math.Min(164, panel.Width * 2 / 3),
            panel.Height >= 600 ? 15 : 12);
        _spriteBatch.Draw(
            _pixel,
            pedestal,
            occupied ? new Color(61, 65, 65) : new Color(38, 42, 45));
        DrawRectangleOutline(pedestal, 2, frame);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(pedestal.X + 9, pedestal.Bottom, pedestal.Width - 18, 5),
            new Color(28, 31, 34));

        if (!occupied)
        {
            int runeY = panel.Y + (int)MathF.Round(panel.Height * .39f);
            int runeSize = Math.Clamp(panel.Width / 6, 54, 84);
            DrawRectangleOutline(
                new Rectangle(
                    panel.Center.X - runeSize / 2,
                    runeY - runeSize / 2,
                    runeSize,
                    runeSize),
                panel.Width >= 450 ? 4 : 3,
                new Color(93, 82, 60));
            int stroke = Math.Clamp(runeSize / 12, 4, 7);
            int arm = runeSize / 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(panel.Center.X - stroke / 2, runeY - arm, stroke, arm * 2),
                new Color(142, 119, 69));
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(panel.Center.X - arm, runeY - stroke / 2, arm * 2, stroke),
                new Color(142, 119, 69));
        }
    }

    private static int GetCharacterPreviewGroundY(Rectangle panel) =>
        panel.Y + Math.Clamp((int)MathF.Round(panel.Height * .40f), 190, 302);

    private void DrawCharacterSlotDetails(Rectangle panel, SaveSlotData slot)
    {
        if (!slot.Occupied)
        {
            int centerY = panel.Y + (int)MathF.Round(panel.Height * .39f);
            int emptyScale = panel.Width >= 450 ? 2 : 1;
            DrawCenteredText(
                Loc.Get("character.empty"),
                panel.Center.X,
                centerY + Math.Clamp(panel.Width / 9, 54, 76),
                new Color(160, 164, 161),
                emptyScale);
            DrawCenteredText(
                Loc.Get("character.create"),
                panel.Center.X,
                centerY + Math.Clamp(panel.Width / 9, 78, 106),
                new Color(210, 171, 83),
                emptyScale);
            return;
        }

        int nameScale = panel.Width >= 470 && slot.CharacterName.Length <= 14
            ? 3
            : panel.Width >= 300 && slot.CharacterName.Length <= 16
                ? 2
                : 1;
        DrawCenteredText(
            slot.CharacterName,
            panel.Center.X,
            panel.Y + (panel.Width >= 470 ? 46 : 34),
            new Color(225, 197, 126),
            nameScale);

        int infoScale = panel.Width >= 450 ? 2 : 1;
        int rowHeight = infoScale == 2 ? 27 : 20;
        int x = panel.X + Math.Clamp(panel.Width / 18, 14, 27);
        int y = panel.Y + (int)MathF.Round(panel.Height * .57f);
        Color label = new(112, 120, 124);
        Color value = new(190, 193, 188);
        DrawSlotValue(Loc.Get("character.create.body"),
            Loc.Get(slot.Appearance.BodyType == CharacterBodyType.Male
                ? "character.value.male"
                : "character.value.female"), x, y, panel, label, value, infoScale);
        y += rowHeight;
        DrawSlotValue(Loc.Get("character.level").Split(' ')[0], slot.Level.ToString(), x, y, panel, label, value, infoScale);
        y += rowHeight;
        DrawSlotValue(Loc.Get("character.area").Split(':')[0], slot.CurrentArea, x, y, panel, label, value, infoScale);
        y += rowHeight;
        DrawSlotValue(Loc.Get("character.weapon").Split(':')[0], slot.EquippedWeaponName, x, y, panel, label, value, infoScale);
        y += rowHeight;
        DrawSlotValue(Loc.Get("character.armor").Split(':')[0], slot.EquippedArmorName, x, y, panel, label, value, infoScale);
        y += rowHeight;
        DrawSlotValue(Loc.Get("character.play_time").Split(':')[0], FormatPlayTime(slot.PlayTimeSeconds), x, y, panel, label, value, infoScale);
    }

    private void DrawSlotValue(
        string label,
        string value,
        int x,
        int y,
        Rectangle panel,
        Color labelColor,
        Color valueColor,
        int scale)
    {
        DrawDebugText(label, x, y, labelColor, scale);
        int valueX = panel.X + (int)MathF.Round(panel.Width * .42f);
        string displayedValue = value ?? string.Empty;
        int maximumCharacters = Math.Max(
            4,
            (panel.Right - valueX - 12) / (6 * scale));
        if (displayedValue.Length > maximumCharacters)
            displayedValue = maximumCharacters > 3
                ? displayedValue[..(maximumCharacters - 3)] + "..."
                : displayedValue[..maximumCharacters];
        DrawDebugText(displayedValue, valueX, y, valueColor, scale);
    }

    private static string FormatPlayTime(double totalSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0d, totalSeconds));
        int hours = (int)Math.Min(999d, time.TotalHours);
        return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private void DrawDeleteSlotConfirmation(GameSession session)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - Math.Min(250, viewport.Width / 2 - 18),
            viewport.Height / 2 - 110,
            Math.Min(500, viewport.Width - 36),
            220);
        _spriteBatch.Draw(_pixel, screen, new Color(4, 5, 7, 185));
        _spriteBatch.Draw(_pixel, panel, new Color(20, 20, 23, 252));
        DrawRectangleOutline(panel, 4, new Color(205, 162, 70));
        DrawCenteredText(
            Loc.Get("character.delete.title"),
            panel.Center.X,
            panel.Y + 24,
            new Color(229, 151, 134),
            2);
        DrawCenteredText(
            session.SaveSlots[session.CharacterSelect.SelectedSlotIndex].CharacterName,
            panel.Center.X,
            panel.Y + 58,
            new Color(218, 192, 139),
            1);
        DrawCenteredText(
            Loc.Get("character.delete.question"),
            panel.Center.X,
            panel.Y + 91,
            new Color(170, 154, 147),
            1);
        DrawCenteredText(
            Loc.Get("character.delete.warning"),
            panel.Center.X,
            panel.Y + 116,
            new Color(205, 105, 96),
            1);
        if (session.CharacterSelect.DeleteFailed)
        {
            DrawCenteredText(
                Loc.Get("character.delete.failed"),
                panel.Center.X,
                panel.Y + 146,
                new Color(224, 91, 82),
                1);
        }
        DrawCenteredText(
            Loc.Get("character.delete.controls"),
            panel.Center.X,
            panel.Bottom - 31,
            new Color(215, 111, 104),
            1);
    }

    private void DrawCharacterCreation(GameSession session, GameTime gameTime)
    {
        GetCharacterCreatorLayout(
            out Rectangle screen,
            out Rectangle left,
            out Rectangle preview,
            out Rectangle right,
            out Rectangle footer);

        _graphicsDevice.Clear(new Color(7, 9, 13));
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_pixel, screen, new Color(7, 9, 13));

        int bandHeight = Math.Max(1, screen.Height / 7);
        for (int band = 0; band < 7; band++)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(0, band * bandHeight, screen.Width, bandHeight),
                new Color(10 + band * 2, 13 + band * 2, 20 + band * 2));
        }

        DrawCreatorPanel(left);
        DrawCreatorPanel(preview);
        DrawCreatorPanel(right);
        _spriteBatch.Draw(_pixel, footer, new Color(16, 20, 27, 245));
        DrawRectangleOutline(footer, 2, new Color(92, 76, 50));

        DrawCenteredText(
            Loc.Get("character.create.title"),
            screen.Center.X,
            Math.Max(12, left.Y - 34),
            new Color(224, 188, 94),
            screen.Width >= 1000 ? 3 : 2);
        DrawCenteredText(
            Loc.Get("character.create.preview"),
            preview.Center.X,
            preview.Y + 15,
            new Color(151, 159, 164),
            1);
        _spriteBatch.End();

        float previewScale = Math.Clamp(
            Math.Min(preview.Width / 122f, preview.Height / 104f),
            1.25f,
            3.15f);
        Vector2 previewAnchor = new(
            preview.Center.X / previewScale,
            (preview.Bottom - Math.Max(24, preview.Height / 10)) / previewScale);
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(previewScale));
        _playerSpriteRenderer.DrawPreview(
            session.Player,
            gameTime,
            previewAnchor,
            session.CharacterCreator.PreviewFacing);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawCharacterCreatorOptions(session.CharacterCreator, left, right);
        DrawCenteredText(
            Loc.Get("character.create.navigate"),
            footer.Center.X,
            footer.Center.Y - 4,
            new Color(170, 174, 171),
            screen.Width >= 880 ? 1 : 1);
        _spriteBatch.End();
    }

    private void DrawCharacterCreatorOptions(
        CharacterCreatorState creator,
        Rectangle left,
        Rectangle right)
    {
        const int rowCount = (int)CharacterCreatorRow.Confirm + 1;
        int rowHeight = Math.Max(17, (left.Height - 36) / rowCount);
        int y = left.Y + 27;

        DrawDebugText(Loc.Get("character.create.options"), left.X + 12, left.Y + 9, new Color(202, 172, 99), 1);
        for (int index = 0; index < rowCount; index++)
        {
            CharacterCreatorRow row = (CharacterCreatorRow)index;
            bool selected = row == creator.SelectedRow;
            if (selected)
            {
                var highlight = new Rectangle(
                    left.X + 7,
                    y - 3,
                    left.Width - 14,
                    rowHeight - 1);
                _spriteBatch.Draw(_pixel, highlight, new Color(58, 50, 38));
                DrawRectangleOutline(highlight, 1, new Color(196, 153, 68));
            }

            Color color = row switch
            {
                CharacterCreatorRow.Confirm => new Color(119, 205, 135),
                CharacterCreatorRow.Randomize or CharacterCreatorRow.ResetDefault =>
                    new Color(176, 151, 91),
                _ => selected
                    ? new Color(236, 212, 151)
                    : new Color(159, 164, 164)
            };
            DrawDebugText(
                CharacterCreatorState.GetRowLabel(row),
                left.X + 13,
                y,
                color,
                1);
            y += rowHeight;
        }

        int infoX = right.X + 12;
        int infoY = right.Y + 10;
        DrawDebugText(Loc.Get("character.create.current"), infoX, infoY, new Color(202, 172, 99), 1);
        infoY += 24;
        DrawDebugText(
            CharacterCreatorState.GetRowLabel(creator.SelectedRow),
            infoX,
            infoY,
            new Color(142, 151, 160),
            1);
        infoY += 18;
        string selectedValue = creator.GetValueLabel(creator.SelectedRow);
        if (selectedValue.Length > 0)
        {
            Color valueColor = new(231, 203, 119);
            DrawDebugText("<", infoX, infoY, valueColor, 1);
            DrawDebugText(selectedValue, infoX + 12, infoY, valueColor, 1);
            DrawDebugText(
                ">",
                infoX + 18 + selectedValue.Length * 6,
                infoY,
                valueColor,
                1);
        }
        else
        {
            DrawDebugText(
                creator.SelectedRow == CharacterCreatorRow.Confirm
                    ? Loc.Get("character.create.enter_begin")
                    : Loc.Get("character.create.press_enter"),
                infoX,
                infoY,
                creator.SelectedRow == CharacterCreatorRow.Confirm
                    ? new Color(119, 205, 135)
                    : new Color(210, 183, 112),
                1);
        }

        infoY += 34;
        DrawDebugText(Loc.Get("character.create.options"), infoX, infoY, new Color(202, 172, 99), 1);
        infoY += 19;
        for (int index = 0; index < CharacterCreatorSummaryRows.Length; index++)
        {
            CharacterCreatorRow row = CharacterCreatorSummaryRows[index];
            DrawDebugText(
                GetCreatorSummaryLabel(row),
                infoX,
                infoY,
                new Color(118, 125, 130),
                1);
            DrawDebugText(
                creator.GetValueLabel(row),
                infoX + Math.Max(74, right.Width / 2 - 3),
                infoY,
                new Color(190, 192, 185),
                1);
            infoY += 17;
        }
    }

    private static string GetCreatorSummaryLabel(CharacterCreatorRow row)
    {
        string key = row switch
        {
            CharacterCreatorRow.CharacterName => "name",
            CharacterCreatorRow.BodyType => "body",
            CharacterCreatorRow.BodyBuild => "build",
            CharacterCreatorRow.Face => "face",
            CharacterCreatorRow.SkinTone => "skin",
            CharacterCreatorRow.Hair => "hair",
            CharacterCreatorRow.HairColor => "color",
            CharacterCreatorRow.FacialHair => "beard",
            CharacterCreatorRow.FaceDetail => "detail",
            CharacterCreatorRow.TorsoWidth => "torso",
            CharacterCreatorRow.ArmThickness => "arms",
            CharacterCreatorRow.LegThickness => "legs",
            _ => "height"
        };
        return Loc.Get("character.summary." + key);
    }

    private void GetCharacterCreatorLayout(
        out Rectangle screen,
        out Rectangle left,
        out Rectangle preview,
        out Rectangle right,
        out Rectangle footer)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        int margin = Math.Clamp(viewport.Width / 55, 8, 22);
        int header = Math.Clamp(viewport.Height / 10, 42, 68);
        int footerHeight = Math.Clamp(viewport.Height / 12, 34, 48);
        int contentY = header;
        int contentHeight = Math.Max(260, viewport.Height - header - footerHeight - margin);
        int gap = Math.Clamp(viewport.Width / 100, 6, 14);
        int usableWidth = viewport.Width - margin * 2 - gap * 2;
        int leftWidth = Math.Clamp((int)(usableWidth * .31f), 190, 340);
        int rightWidth = Math.Clamp((int)(usableWidth * .29f), 180, 330);
        int previewWidth = Math.Max(180, usableWidth - leftWidth - rightWidth);

        left = new Rectangle(margin, contentY, leftWidth, contentHeight);
        preview = new Rectangle(left.Right + gap, contentY, previewWidth, contentHeight);
        right = new Rectangle(preview.Right + gap, contentY, rightWidth, contentHeight);
        footer = new Rectangle(
            margin,
            contentY + contentHeight + Math.Max(4, gap / 2),
            viewport.Width - margin * 2,
            Math.Max(28, viewport.Height - (contentY + contentHeight) - margin));
    }

    private void DrawCreatorPanel(Rectangle panel)
    {
        _spriteBatch.Draw(_pixel, panel, new Color(18, 23, 31, 246));
        DrawRectangleOutline(panel, 2, new Color(112, 88, 48));
        DrawRectangleOutline(
            new Rectangle(panel.X + 4, panel.Y + 4, panel.Width - 8, panel.Height - 8),
            1,
            new Color(52, 59, 66));
    }

    private void DrawCenteredText(string text, int centerX, int y, Color color, int scale)
    {
        int width = text.Length * 6 * scale - scale;
        DrawDebugText(text, centerX - width / 2, y, color, scale);
    }

    private void DrawStartScreen(GameSession session)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 180,
            viewport.Height / 2 - 110,
            360,
            220);

        _spriteBatch.Draw(_pixel, screen, new Color(8, 10, 16, 125));
        _spriteBatch.Draw(_pixel, panel, new Color(20, 25, 34, 240));
        DrawRectangleOutline(panel, 4, new Color(155, 121, 56));

        int centerX = panel.Center.X;
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 100, panel.Y + 34, 200, 6), new Color(223, 183, 76));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 72, panel.Y + 48, 144, 4), new Color(124, 101, 58));

        var shield = new Rectangle(centerX - 30, panel.Y + 67, 60, 54);
        _spriteBatch.Draw(_pixel, shield, new Color(49, 89, 126));
        DrawRectangleOutline(shield, 3, new Color(188, 196, 202));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 3, shield.Y + 7, 6, 40), new Color(214, 220, 224));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 18, shield.Y + 17, 36, 6), new Color(214, 220, 224));

        var startButton = new Rectangle(centerX - 108, panel.Bottom - 87, 216, 32);
        _spriteBatch.Draw(_pixel, startButton, new Color(38, 50, 64));
        DrawRectangleOutline(startButton, 2,
            session.MainMenu.SelectedEntry == MainMenuEntry.NewGame
                ? new Color(105, 176, 120)
                : new Color(70, 82, 91));
        var settingsButton = new Rectangle(centerX - 108, panel.Bottom - 49, 216, 28);
        _spriteBatch.Draw(_pixel, settingsButton, new Color(34, 43, 55));
        DrawRectangleOutline(settingsButton, 2,
            session.MainMenu.SelectedEntry == MainMenuEntry.Settings
                ? new Color(105, 176, 120)
                : new Color(70, 82, 91));
        DrawCenteredText(
            Loc.Get("menu.title"),
            centerX,
            panel.Y + 12,
            new Color(223, 183, 76),
            2);
        DrawCenteredText(
            Loc.Get("menu.new_game"),
            centerX,
            startButton.Center.Y - 4,
            session.MainMenu.SelectedEntry == MainMenuEntry.NewGame
                ? new Color(128, 216, 145)
                : new Color(145, 153, 158),
            1);
        DrawCenteredText(
            Loc.Get("menu.settings"),
            centerX,
            settingsButton.Center.Y - 4,
            session.MainMenu.SelectedEntry == MainMenuEntry.Settings
                ? new Color(128, 216, 145)
                : new Color(145, 153, 158),
            1);

        if (session.LanguageSettings.IsOpen)
            DrawLanguageSettings(session);
    }

    private void DrawPauseOverlay(GameSession session)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 190,
            viewport.Height / 2 - 135,
            380,
            270);

        _spriteBatch.Draw(_pixel, screen, new Color(8, 10, 15, 180));
        _spriteBatch.Draw(_pixel, panel, new Color(22, 27, 35, 248));
        DrawRectangleOutline(panel, 3, new Color(142, 151, 160));
        DrawRectangleOutline(
            new Rectangle(panel.X + 6, panel.Y + 6, panel.Width - 12, panel.Height - 12),
            1,
            new Color(66, 72, 79));
        DrawCenteredText(
            Loc.Get("pause.title"),
            panel.Center.X,
            panel.Y + 24,
            new Color(218, 190, 113),
            2);

        DrawPauseMenuEntry(
            panel,
            panel.Y + 74,
            Loc.Get("pause.resume"),
            session.PauseMenu.SelectedEntry == PauseMenuEntry.Resume);
        DrawPauseMenuEntry(
            panel,
            panel.Y + 114,
            Loc.Get("pause.settings"),
            session.PauseMenu.SelectedEntry == PauseMenuEntry.Settings);
        DrawPauseMenuEntry(
            panel,
            panel.Y + 154,
            Loc.Get("pause.return_character_select"),
            session.PauseMenu.SelectedEntry ==
                PauseMenuEntry.ReturnToCharacterSelect);
        DrawCenteredText(
            Loc.Get("common.select_hint"),
            panel.Center.X,
            panel.Bottom - 33,
            new Color(128, 136, 140),
            1);

        if (session.PauseMenu.IsReturnConfirmationOpen)
            DrawReturnToSelectConfirmation(session);
        else if (session.LanguageSettings.IsOpen)
            DrawLanguageSettings(session);
    }

    private void DrawPauseMenuEntry(
        Rectangle panel,
        int y,
        string label,
        bool selected)
    {
        var row = new Rectangle(panel.X + 32, y - 10, panel.Width - 64, 38);
        if (selected)
        {
            _spriteBatch.Draw(_pixel, row, new Color(57, 50, 38));
            DrawRectangleOutline(row, 2, new Color(194, 151, 67));
        }
        DrawCenteredText(
            label,
            panel.Center.X,
            y,
            selected
                ? new Color(235, 209, 143)
                : new Color(157, 164, 166),
            1);
    }

    private void DrawReturnToSelectConfirmation(GameSession session)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 235,
            viewport.Height / 2 - 125,
            470,
            250);
        _spriteBatch.Draw(_pixel, screen, new Color(5, 6, 9, 205));
        _spriteBatch.Draw(_pixel, panel, new Color(27, 25, 29, 252));
        DrawRectangleOutline(panel, 4, new Color(177, 135, 61));
        DrawCenteredText(
            Loc.Get("pause.confirm_title"),
            panel.Center.X,
            panel.Y + 30,
            new Color(232, 202, 124),
            2);
        DrawCenteredText(
            Loc.Get("pause.progress_saved"),
            panel.Center.X,
            panel.Y + 70,
            new Color(164, 165, 158),
            1);

        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 112,
            Loc.Get("common.yes"),
            session.PauseMenu.ConfirmReturn);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 153,
            Loc.Get("common.no"),
            !session.PauseMenu.ConfirmReturn);
        DrawCenteredText(
            Loc.Get("common.cancel_hint"),
            panel.Center.X,
            panel.Bottom - 28,
            new Color(137, 143, 144),
            1);
    }

    private void DrawPauseConfirmationChoice(
        Rectangle panel,
        int y,
        string label,
        bool selected)
    {
        if (selected)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(panel.Center.X - 64, y - 7, 128, 28),
                new Color(61, 52, 38));
            DrawDebugText(
                ">",
                panel.Center.X - 47,
                y,
                new Color(229, 197, 119),
                1);
        }
        DrawCenteredText(
            label,
            panel.Center.X,
            y,
            selected ? new Color(234, 207, 137) : new Color(142, 147, 147),
            1);
    }

    private void DrawLanguageSettings(GameSession session)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 220,
            viewport.Height / 2 - 220,
            440,
            440);
        _spriteBatch.Draw(_pixel, screen, new Color(5, 7, 10, 210));
        _spriteBatch.Draw(_pixel, panel, new Color(22, 27, 35, 252));
        DrawRectangleOutline(panel, 4, new Color(132, 153, 159));
        DrawCenteredText(
            Loc.Get("settings.title"),
            panel.Center.X,
            panel.Y + 27,
            new Color(222, 198, 128),
            2);
        DrawCenteredText(
            Loc.Get("settings.language"),
            panel.Center.X,
            panel.Y + 69,
            new Color(164, 181, 183),
            1);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 96,
            Loc.Get("settings.english"),
            session.LanguageSettings.SelectedRow == SettingsRow.Language &&
                session.LanguageSettings.SelectedLanguage == Language.English);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 130,
            Loc.Get("settings.vietnamese"),
            session.LanguageSettings.SelectedRow == SettingsRow.Language &&
                session.LanguageSettings.SelectedLanguage == Language.Vietnamese);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 169,
            Loc.Format(
                "settings.master_volume",
                (int)MathF.Round(Loc.MasterVolume * 100f)),
            session.LanguageSettings.SelectedRow == SettingsRow.MasterVolume);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 207,
            Loc.Format(
                "settings.music_volume",
                (int)MathF.Round(Loc.MusicVolume * 100f)),
            session.LanguageSettings.SelectedRow == SettingsRow.MusicVolume);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 245,
            Loc.Format(
                "settings.sfx_volume",
                (int)MathF.Round(Loc.SfxVolume * 100f)),
            session.LanguageSettings.SelectedRow == SettingsRow.SfxVolume);
        string displayModeKey = Loc.DisplayMode switch
        {
            GameDisplayMode.BorderlessWindowed => "settings.borderless",
            GameDisplayMode.Fullscreen => "settings.fullscreen",
            _ => "settings.windowed"
        };
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 283,
            Loc.Format("settings.display_mode", Loc.Get(displayModeKey)),
            session.LanguageSettings.SelectedRow == SettingsRow.DisplayMode);
        DrawPauseConfirmationChoice(
            panel,
            panel.Y + 321,
            Loc.Format("settings.resolution", Loc.WindowWidth, Loc.WindowHeight),
            session.LanguageSettings.SelectedRow == SettingsRow.Resolution);
        DrawCenteredText(
            Loc.Get("settings.change_hint"),
            panel.Center.X,
            panel.Bottom - 43,
            new Color(137, 155, 158),
            1);
        DrawCenteredText(
            Loc.Get("settings.back_hint"),
            panel.Center.X,
            panel.Bottom - 25,
            new Color(137, 155, 158),
            1);
    }

    private void DrawGameOverOverlay()
    {
        Viewport viewport = _graphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        var panel = new Rectangle(
            viewport.Width / 2 - 130,
            viewport.Height / 2 - 90,
            260,
            180);

        _spriteBatch.Draw(_pixel, screen, new Color(48, 8, 12, 150));
        _spriteBatch.Draw(_pixel, panel, new Color(35, 20, 25, 240));
        DrawRectangleOutline(panel, 3, new Color(165, 62, 67));

        var skull = new Rectangle(panel.Center.X - 34, panel.Y + 25, 68, 58);
        _spriteBatch.Draw(_pixel, skull, new Color(205, 200, 184));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.X + 13, skull.Y + 18, 13, 14), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Right - 26, skull.Y + 18, 13, 14), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Center.X - 5, skull.Y + 35, 10, 9), new Color(45, 32, 35));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.X + 12, skull.Bottom, 44, 12), new Color(205, 200, 184));
        _spriteBatch.Draw(_pixel, new Rectangle(skull.Center.X - 2, skull.Bottom, 4, 12), new Color(71, 54, 54));

        var restartButton = new Rectangle(panel.Center.X - 65, panel.Bottom - 45, 130, 28);
        _spriteBatch.Draw(_pixel, restartButton, new Color(57, 31, 35));
        DrawRectangleOutline(restartButton, 2, new Color(210, 90, 91));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 35, restartButton.Center.Y - 3, 54, 6), new Color(226, 112, 109));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 35, restartButton.Center.Y - 11, 6, 14), new Color(226, 112, 109));
        _spriteBatch.Draw(_pixel, new Rectangle(restartButton.X + 29, restartButton.Center.Y - 11, 12, 6), new Color(226, 112, 109));
    }

    private void DrawRectangleOutline(Rectangle rectangle, int thickness, Color color)
    {
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height),
            color);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height),
            color);
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}
