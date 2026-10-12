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
    private readonly CatacombEnvironmentRenderer _catacombEnvironmentRenderer;
    private readonly AncientScholarTombRenderer _ancientScholarTombRenderer;
    private readonly FallenCityEnvironmentRenderer _fallenCityEnvironmentRenderer;
    private readonly DemonLandsEnvironmentRenderer _demonLandsEnvironmentRenderer;

    public GameRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
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
    }

    public void Draw(
        GameSession gameSession,
        GameTime gameTime,
        Action<string> diagnosticLog = null)
    {
        RegionTheme theme = RegionTheme.For(gameSession.CurrentRegion);
        _graphicsDevice.Clear(theme.Background);
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
        DrawBreakerWorld(gameSession);

        _playerSpriteRenderer.Draw(
            gameSession.Player,
            gameTime,
            gameSession.Player.WeaponFamily == WeaponFamily.ChainFlail
                ? gameSession.PlayerAttackArea.Center.ToVector2()
                : null);

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
            }
        }

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
            gameSession.PlayerHitEffectPosition);

        DrawWildForestForeground(gameSession, gameSession.Camera.ViewBounds);
        _catacombEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);
        _fallenCityEnvironmentRenderer.DrawForeground(
            gameSession,
            gameSession.Camera.ViewBounds,
            gameTime);

        if (gameSession.ShowMapDebug)
            DrawWildForestMapDebug(gameSession);

        if (gameSession.ShowCombatDebug)
            DrawCombatDebug(gameSession);

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _demonLandsEnvironmentRenderer.DrawCorruptionOverlay(
            gameSession,
            new Rectangle(0, 0,
                _graphicsDevice.Viewport.Width,
                _graphicsDevice.Viewport.Height),
            gameTime);
        DrawHudPanel();
        DrawPlayerHealth(gameSession.Player);
        DrawPlayerStamina(gameSession.Player);
        DrawCurseMeter(gameSession);
        DrawCityAlarm(gameSession);
        DrawDemonicCorruptionMeter(gameSession);
        DrawBrokenKingBossBar(gameSession);
        DrawHornedGeneralBossBar(gameSession);
        DrawPlayerProgression(gameSession.Player, gameSession.KillCount);
        DrawWeaponResource(gameSession.Player);
        DrawTechniqueFeedback(gameSession.Player);
        DrawDungeonStatus(gameSession);

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
            string recordPrompt = Loc.Get("hud.read_record");
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

        if (gameSession.LastDiceTable.IsModalActive)
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
            Color trail = projectile.TechniqueEffect ==
                WeaponTechniqueEffect.RangerHeavensFury
                    ? new Color(239, 215, 122, 180)
                    : projectile.RicochetCount > 0
                        ? new Color(132, 214, 196, 145)
                        : new Color(211, 191, 135, 105);
            DrawWorldLine(
                projectile.PreviousPosition,
                projectile.Position,
                projectile.TechniqueEffect ==
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

    private void DrawBreakerWorld(GameSession gameSession)
    {
        foreach (BreakerShockwave wave in gameSession.BreakerShockwaves)
        {
            Vector2 ground = wave.Position;
            float direction = wave.Direction;
            float fade = 1f - wave.Progress * .55f;
            Color crack = new(
                (int)(96f * fade),
                (int)(82f * fade),
                (int)(67f * fade),
                220);
            Color dust = new(122, 105, 82, (int)(145f * fade));
            DrawWorldLine(
                ground - new Vector2(direction * 25f, 2f),
                ground + new Vector2(direction * 22f, -1f),
                4f,
                crack);
            DrawWorldLine(
                ground - new Vector2(direction * 4f, 1f),
                ground + new Vector2(direction * 13f, -12f),
                2f,
                crack);
            DrawWorldLine(
                ground + new Vector2(direction * 5f, 0f),
                ground + new Vector2(direction * 17f, -7f),
                2f,
                crack);
            for (int debris = 0; debris < 3; debris++)
            {
                int size = 3 + debris;
                Vector2 position = ground + new Vector2(
                    -direction * (8f + debris * 8f),
                    -9f - debris * 5f);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)position.X, (int)position.Y, size, size),
                    dust);
            }
        }

        foreach (BreakerImpactVisual visual in gameSession.BreakerImpactVisuals)
        {
            float progress = visual.Progress;
            float radius = visual.Radius * (.22f + progress * .78f);
            float fade = 1f - progress;
            Color crack = new(91, 76, 61, (int)(220f * fade));
            Color dust = new(135, 112, 84, (int)(165f * fade));
            Vector2 left = visual.Position - new Vector2(radius, 0f);
            Vector2 right = visual.Position + new Vector2(radius, 0f);
            DrawWorldLine(left, right, 3f + visual.Strength * 2f, crack);
            for (int index = 0; index < 6; index++)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                float distance = radius * (.20f + index * .12f);
                Vector2 root = visual.Position + new Vector2(
                    side * distance,
                    -1f);
                Vector2 tip = root + new Vector2(
                    side * (7f + index * 2f),
                    -5f - index % 3 * 4f);
                DrawWorldLine(root, tip, index < 2 ? 3f : 2f, crack);
                int fragment = 3 + index % 3;
                Vector2 fragmentPosition = tip - new Vector2(
                    0f,
                    (8f + index * 3f) * fade);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        (int)fragmentPosition.X,
                        (int)fragmentPosition.Y,
                        fragment,
                        fragment),
                    dust);
            }
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
            DrawInventorySlot(slotBounds, item, isSelected);
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

        int contentY = y + 69;
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
        bool isSelected)
    {
        Color borderColor = isSelected
            ? new Color(245, 230, 151)
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
            item.Name.ToUpperInvariant(),
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
                ? $"RARITY: {item.Rarity}"
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
            "STATUS: UNEQUIPPED",
            x,
            y + 52,
            new Color(137, 149, 158),
            scale: 1);

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

            if (item.WeaponDefinition.Family == WeaponFamily.BattleAxe)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 2, top + 4, 5, height + 4),
                    new Color(105, 68, 45));
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 10, top + 1, 21, 9),
                    color);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(centerX - 13, top + 4, 5, 10),
                    color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.Spear)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top, 4, height + 10), new Color(117, 78, 49));
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 5, top - 1, 10, 9), color);
                _spriteBatch.Draw(_pixel, new Rectangle(centerX - 2, top - 5, 4, 8), color);
                return;
            }

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

            if (item.WeaponDefinition.Family == WeaponFamily.ChainFlail)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, top + 14, 5, height - 3),
                    new Color(105, 68, 45));
                for (int link = 0; link < 4; link++)
                {
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(centerX + 3 + link * 4, top + 12 - link * 3, 4, 3),
                        new Color(112, 119, 126));
                }
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX + 17, top - 4, 13, 13), color);
                return;
            }

            if (item.WeaponDefinition.Family == WeaponFamily.GreatSword)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 2, top + 10, 5, height + 3),
                    new Color(105, 68, 45));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 9, top - 2, 19, 19), color);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(centerX - 13, top + 5, 27, 5), color);
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
        _spriteBatch.Draw(_pixel, panel, new Color(12, 16, 17, 232));
        DrawRectangleOutline(panel, intro.PresentationStrength + 1, border);

        string name = intro.LocalizedName;
        string title = intro.LocalizedTitle;
        string description = intro.LocalizedDescription;
        int nameScale = FitDebugTextScale(name, panel.Width - 20, 2);
        int nameWidth = name.Length * 6 * nameScale;
        DrawDebugText(
            name,
            panel.Center.X - nameWidth / 2,
            panel.Y + 10,
            new Color(237, 226, 190),
            scale: nameScale);
        int titleWidth = title.Length * 6;
        DrawDebugText(
            title,
            panel.Center.X - titleWidth / 2,
            panel.Y + 38,
            border,
            scale: 1);
        int descriptionWidth = description.Length * 6;
        DrawDebugText(
            description,
            Math.Max(panel.X + 10, panel.Center.X - descriptionWidth / 2),
            panel.Y + 57,
            new Color(183, 190, 184),
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
            _ => enemy.Size
        };
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
            _ => new Color(202, 207, 210)
        };
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

    private void DrawWeaponResource(PlayerCharacter player)
    {
        int maximum = player.Combat.Resources.ActiveMaximum;
        if (maximum <= 0)
            return;

        if (player.WeaponFamily == WeaponFamily.DualSwords)
        {
            DrawDuelistMomentum(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.HunterBow)
        {
            DrawHunterFocus(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.SpikedMace)
        {
            DrawBreakerInertia(player);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.ChainFlail)
        {
            DrawChainMomentum(player);
            return;
        }

        int value = player.Combat.Resources.ActiveValue;
        Color color = player.WeaponFamily switch
        {
            WeaponFamily.DualSwords => new Color(214, 113, 90),
            WeaponFamily.ArcaneWarStaff => new Color(151, 104, 214),
            WeaponFamily.ChainFlail => new Color(190, 154, 86),
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

    private void DrawBreakerInertia(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 91;
        const int barWidth = 198;
        const int barHeight = 7;
        float ratio = player.Combat.Resources.BreakerInertiaRatio;
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + 1,
            barY + 1,
            barWidth - 2,
            barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(118, 109, 99));
        _spriteBatch.Draw(_pixel, background, new Color(38, 31, 29));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            Color fill = Color.Lerp(
                new Color(126, 84, 61),
                new Color(174, 61, 45),
                ratio);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    background.X,
                    background.Y,
                    fillWidth,
                    background.Height),
                fill);
        }
        for (int segment = 1; segment < 10; segment++)
        {
            int x = background.X + background.Width * segment / 10;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, background.Y, 1, background.Height),
                new Color(28, 24, 23, 150));
        }
    }

    private void DrawChainMomentum(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.ChainMomentumRatio;
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(
            barX + 1,
            barY + 1,
            barWidth - 2,
            barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(135, 126, 112));
        _spriteBatch.Draw(_pixel, background, new Color(34, 31, 29));
        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            Color fill = Color.Lerp(
                new Color(132, 116, 83),
                new Color(203, 155, 78),
                ratio);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    background.X,
                    background.Y,
                    fillWidth,
                    background.Height),
                fill);
        }

        for (int marker = 1; marker < 4; marker++)
        {
            int x = background.X + background.Width * marker / 4;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(x, background.Y, 1, background.Height),
                new Color(218, 201, 168, 150));
        }

        int activeSegments = player.Combat.Resources.ActiveValue;
        for (int segment = 0; segment < 10; segment++)
        {
            Rectangle pip = new(42 + segment * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                segment < activeSegments
                    ? new Color(190, 154, 86)
                    : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(164, 157, 143));
        }
    }

    private void DrawDuelistMomentum(PlayerCharacter player)
    {
        const int barX = 42;
        const int barY = 78;
        const int barWidth = 198;
        const int barHeight = 8;
        float ratio = player.Combat.Resources.DuelistMomentumRatio;
        Color momentum = Color.Lerp(
            new Color(164, 77, 72),
            new Color(245, 174, 104),
            ratio);
        Rectangle border = new(barX, barY, barWidth, barHeight);
        Rectangle background = new(barX + 1, barY + 1, barWidth - 2, barHeight - 2);
        _spriteBatch.Draw(_pixel, border, new Color(177, 187, 181));
        _spriteBatch.Draw(_pixel, background, new Color(48, 29, 32));

        int fillWidth = (int)MathF.Round(background.Width * ratio);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(background.X, background.Y, fillWidth, background.Height),
                momentum);
        }

        for (int index = 1; index < 3; index++)
        {
            int markerX = background.X + background.Width * index / 3;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(231, 219, 191, 175));
        }

        int tier = player.Combat.Resources.DuelistMomentumTier;
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

        for (int index = 1; index < 4; index++)
        {
            int markerX = background.X + background.Width * index / 4;
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(markerX, background.Y, 1, background.Height),
                new Color(226, 219, 177, 155));
        }

        int segments = player.Combat.Resources.ActiveValue;
        for (int index = 0; index < 4; index++)
        {
            Rectangle pip = new(42 + index * 15, 91, 11, 6);
            _spriteBatch.Draw(
                _pixel,
                pip,
                index < segments ? focus : new Color(54, 59, 64));
            DrawRectangleOutline(pip, 1, new Color(190, 194, 191));
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

        DrawCenteredText(
            Loc.Get("character.select.controls"),
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
            viewport.Width / 2 - Math.Min(220, viewport.Width / 2 - 18),
            viewport.Height / 2 - 82,
            Math.Min(440, viewport.Width - 36),
            164);
        _spriteBatch.Draw(_pixel, screen, new Color(4, 5, 7, 185));
        _spriteBatch.Draw(_pixel, panel, new Color(34, 20, 23, 252));
        DrawRectangleOutline(panel, 4, new Color(169, 67, 65));
        DrawCenteredText(
            Loc.Get("character.delete.title"),
            panel.Center.X,
            panel.Y + 27,
            new Color(229, 151, 134),
            2);
        DrawCenteredText(
            session.SaveSlots[session.CharacterSelect.SelectedSlotIndex].CharacterName,
            panel.Center.X,
            panel.Y + 62,
            new Color(218, 192, 139),
            1);
        DrawCenteredText(
            Loc.Get("character.delete.warning"),
            panel.Center.X,
            panel.Y + 88,
            new Color(170, 154, 147),
            1);
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
