using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered, code-only Ancient Catacombs environment presentation. All props
/// are non-colliding and deterministic; visible platform caps are drawn from
/// the actual collision rectangles.
/// </summary>
public sealed class CatacombEnvironmentRenderer
{
    private static readonly Color FarDark = new(10, 12, 17);
    private static readonly Color FarStone = new(20, 23, 29);
    private static readonly Color MidStone = new(31, 33, 37);
    private static readonly Color Stone = new(45, 44, 43);
    private static readonly Color StoneDark = new(32, 32, 34);
    private static readonly Color StoneEdge = new(76, 73, 68);
    private static readonly Color FloorStone = new(57, 54, 49);
    private static readonly Color FloorEdge = new(104, 98, 86);
    private static readonly Color Bone = new(157, 150, 129);
    private static readonly Color BoneDark = new(93, 89, 78);
    private static readonly Color Rust = new(82, 52, 42);
    private static readonly Color Iron = new(52, 50, 51);
    private static readonly Color Cloth = new(62, 43, 47);
    private static readonly Color Soul = new(79, 126, 139);
    private static readonly Color SoulLight = new(126, 170, 171);
    private static readonly Color Curse = new(91, 50, 111);
    private static readonly Color CurseRed = new(126, 48, 63);
    private static readonly Color Warm = new(167, 117, 62);
    private static readonly Color Moss = new(57, 70, 56);
    private static readonly Color Rot = new(67, 70, 48);
    private static readonly Color Blood = new(77, 35, 36);
    private static readonly Color RoyalGold = new(119, 88, 51);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public CatacombEnvironmentRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawDepth(
        GameSession session,
        Rectangle camera,
        GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsAncientCatacombs != true)
            return;

        _spriteBatch.Draw(_pixel, camera, FarDark);
        float cameraX = camera.X;
        int farFirst = (int)MathF.Floor(cameraX * .16f / 310f) - 2;
        int farLast = farFirst + camera.Width / 310 + 6;
        for (int index = farFirst; index <= farLast; index++)
        {
            float worldX = index * 310f + cameraX * .84f;
            int x = (int)MathF.Round(worldX);
            int top = camera.Top + 74 + Math.Abs(Hash(index, 17) % 24);
            DrawFarArch(x, top, 228, 485);
        }

        int midFirst = (int)MathF.Floor(cameraX * .34f / 188f) - 2;
        int midLast = midFirst + camera.Width / 188 + 9;
        for (int index = midFirst; index <= midLast; index++)
        {
            float worldX = index * 188f + cameraX * .66f;
            int x = (int)MathF.Round(worldX);
            int baseY = map.WorldBounds.Bottom - 34;
            int height = 270 + Math.Abs(Hash(index, 31) % 105);
            DrawDistantPillar(x, baseY, height);
        }

        DrawDepthDust(camera, session.Curse.Ratio,
            (float)gameTime.TotalGameTime.TotalSeconds);
        DrawCursePressureEchoes(camera, session.Curse.Ratio,
            (float)gameTime.TotalGameTime.TotalSeconds);
    }

    public void DrawWorld(
        GameSession session,
        Rectangle visible,
        GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsAncientCatacombs != true)
            return;

        float elapsed = (float)gameTime.TotalGameTime.TotalSeconds;
        foreach (Rectangle corridor in map.Corridors)
        {
            if (corridor.Intersects(visible))
                DrawCorridorShell(corridor);
        }
        foreach (CatacombZoneDefinition zone in map.CatacombZones)
        {
            if (!zone.Bounds.Intersects(visible))
                continue;
            DrawZoneShell(zone, session.Curse.Ratio);
            DrawZoneIdentity(zone, elapsed, session.Curse.Ratio);
        }

        DrawCurseResidue(map, visible, elapsed, session.Curse.Ratio);

        foreach (Platform platform in map.Platforms)
        {
            if (platform.Bounds.Intersects(visible))
                DrawCollisionPlatform(platform);
        }

        foreach (TombInteractionDefinition tomb in map.Tombs)
        {
            if (!tomb.Bounds.Intersects(visible))
                continue;
            TombState state = session.Tombs.States.TryGetValue(
                tomb.Id, out TombRuntime runtime)
                ? runtime.State
                : TombState.Sealed;
            DrawTomb(tomb.Bounds, state, elapsed,
                runtime?.Remaining ?? 0f);
        }

        foreach (SafeShrineDefinition shrine in map.SafeShrines)
        {
            Rectangle shrineBounds = new(
                (int)shrine.Position.X - 86,
                (int)shrine.Position.Y - 92,
                172,
                96);
            if (shrineBounds.Intersects(visible))
                DrawSafeShrine(shrine, elapsed);
        }
    }

    public void DrawForeground(
        GameSession session,
        Rectangle camera,
        GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsAncientCatacombs != true)
            return;

        float elapsed = (float)gameTime.TotalGameTime.TotalSeconds;
        CatacombZoneDefinition zone = FindVisibleZone(map, camera.Center.X);
        float zoneCurse = zone?.CursePressure ?? 0f;
        float pressure = MathF.Max(zoneCurse * .58f, session.Curse.Ratio);

        int first = camera.Left - PositiveModulo(camera.Left, 94) - 94;
        for (int x = first; x < camera.Right + 94; x += 94)
        {
            int seed = Hash(x / 94, zone == null ? 0 : (int)zone.Kind + 1);
            float drift = (elapsed * (7f + Math.Abs(seed % 6)) +
                Math.Abs(seed % 83)) % 92f;
            float worldX = x + drift;
            float worldY = camera.Top + 115f + Math.Abs(seed % 430);
            Vector2 position = new(worldX, worldY);
            if (IsInsideShrineCalm(map, position))
                continue;
            Color mote = pressure > .62f
                ? Curse * (.10f + pressure * .12f)
                : new Color(119, 113, 103) * (.08f + pressure * .07f);
            DrawBox(worldX, worldY, 3f, 2f, mote);
        }

        if (zone != null)
            DrawForegroundDebris(zone, camera);
    }

    private void DrawZoneShell(CatacombZoneDefinition zone, float curseRatio)
    {
        Color wall = zone.Kind switch
        {
            CatacombZoneKind.WraithHalls => new Color(31, 35, 42),
            CatacombZoneKind.SoulChapel => new Color(30, 32, 40),
            CatacombZoneKind.RotPits => new Color(39, 41, 36),
            CatacombZoneKind.FallenHall => new Color(38, 36, 40),
            _ => Stone
        };
        _spriteBatch.Draw(_pixel,
            new Rectangle(
                zone.Bounds.Left,
                zone.Bounds.Top,
                zone.Bounds.Width,
                zone.GroundY - zone.Bounds.Top),
            wall);

        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, zone.Bounds.Top,
                zone.Bounds.Width, 24), StoneDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, zone.Bounds.Top + 24,
                zone.Bounds.Width, 4), StoneEdge * .58f);

        int row = 0;
        for (int y = zone.Bounds.Top + 56; y < zone.GroundY - 44; y += 58)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(zone.Bounds.Left, y, zone.Bounds.Width, 2),
                StoneDark * .48f);
            int offset = row % 2 == 0 ? 18 : 78;
            for (int x = zone.Bounds.Left + offset;
                 x < zone.Bounds.Right;
                 x += 126)
            {
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, y - 56, 2, 56),
                    StoneDark * .42f);
            }
            row++;
        }

        int crackStep = zone.Kind is CatacombZoneKind.FallenHall or
            CatacombZoneKind.DeathKnightWarTomb ? 238 : 314;
        for (int x = zone.Bounds.Left + 126;
             x < zone.Bounds.Right - 70;
             x += crackStep)
        {
            int seed = Hash(x, (int)zone.Kind);
            float startY = zone.Bounds.Top + 72 + Math.Abs(seed % 145);
            DrawWallCrack(new Vector2(x, startY),
                23f + Math.Abs(seed % 20), StoneDark * .74f,
                seed < 0 ? -1f : 1f);
        }

        float corruption = MathF.Max(zone.CursePressure, curseRatio * .7f);
        if (corruption > .42f)
            DrawCursedWallCracks(zone, corruption);
    }

    private void DrawCorridorShell(Rectangle corridor)
    {
        _spriteBatch.Draw(_pixel, corridor, StoneDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(corridor.Left, corridor.Top,
                corridor.Width, 26), new Color(27, 28, 31));
        DrawBox(corridor.Left, corridor.Top + 26, 9,
            corridor.Height - 26, StoneEdge * .62f);
        DrawBox(corridor.Right - 9, corridor.Top + 26, 9,
            corridor.Height - 26, StoneEdge * .62f);
        DrawBox(corridor.Left + 9, corridor.Top + 62,
            corridor.Width - 18, corridor.Height - 102,
            new Color(22, 23, 26));
        DrawBox(corridor.Left - 5, corridor.Top + 48,
            corridor.Width + 10, 14, StoneEdge * .58f);
    }

    private void DrawZoneIdentity(
        CatacombZoneDefinition zone,
        float elapsed,
        float curseRatio)
    {
        switch (zone.Kind)
        {
            case CatacombZoneKind.TombEntrance:
                DrawTombEntrance(zone);
                break;
            case CatacombZoneKind.OssuaryCorridors:
                DrawOssuaryCorridors(zone);
                break;
            case CatacombZoneKind.ArcherGalleries:
                DrawArcherGalleries(zone);
                break;
            case CatacombZoneKind.RotPits:
                DrawRotPits(zone, elapsed);
                break;
            case CatacombZoneKind.WraithHalls:
                DrawWraithHalls(zone, elapsed);
                break;
            case CatacombZoneKind.GuardBarracks:
                DrawGuardBarracks(zone);
                break;
            case CatacombZoneKind.CursedKnightMausoleum:
                DrawCursedKnightMausoleum(zone, elapsed);
                break;
            case CatacombZoneKind.SoulChapel:
                DrawSoulChapel(zone, elapsed);
                break;
            case CatacombZoneKind.DeathKnightWarTomb:
                DrawDeathKnightWarTomb(zone, elapsed);
                break;
            case CatacombZoneKind.FallenHall:
                DrawFallenHall(zone, elapsed, curseRatio);
                break;
        }
    }

    private void DrawTombEntrance(CatacombZoneDefinition zone)
    {
        int ground = zone.GroundY;
        int gateX = zone.Bounds.Left + 72;
        DrawMonumentalArch(gateX, ground, 218, 300, StoneEdge, false);
        DrawSealedDoor(new Rectangle(gateX + 54, ground - 220, 110, 220));
        DrawBurialStatue(new Vector2(zone.Bounds.Left + 360, ground), false,
            StoneEdge * .88f);
        DrawBurialStatue(new Vector2(zone.Bounds.Left + 545, ground), true,
            StoneEdge * .82f);
        DrawBrokenMarker(new Vector2(zone.Bounds.Left + 690, ground), -1f);
        DrawStoneSteps(zone.Bounds.Left, ground, 510);
    }

    private void DrawOssuaryCorridors(CatacombZoneDefinition zone)
    {
        int ground = zone.GroundY;
        for (int x = zone.Bounds.Left + 70;
             x < zone.Bounds.Right - 64;
             x += 158)
        {
            Rectangle alcove = new(x, ground - 176, 112, 132);
            DrawBurialAlcove(alcove, (x / 158) % 3 == 0);
            DrawBoneStack(new Vector2(x + 55, ground - 47),
                3 + Math.Abs(Hash(x, 2) % 4));
        }
        DrawCollapsedNiche(
            new Vector2(zone.Bounds.Left + 810, ground - 40), 1f);
    }

    private void DrawArcherGalleries(CatacombZoneDefinition zone)
    {
        int ground = zone.GroundY;
        for (int x = zone.Bounds.Left + 54;
             x < zone.Bounds.Right - 30;
             x += 176)
        {
            DrawArrowSlit(new Rectangle(x, zone.Bounds.Top + 90, 18, 74));
        }
        DrawBalconyBackdrop(zone.Bounds.Left + 190, ground - 48, 930);
        DrawHangingCloth(new Vector2(zone.Bounds.Left + 402, ground - 190),
            58, 103, false);
        DrawHangingCloth(new Vector2(zone.Bounds.Left + 855, ground - 220),
            64, 128, true);
        DrawBrokenMarker(new Vector2(zone.Bounds.Left + 1120, ground), 1f);
    }

    private void DrawRotPits(CatacombZoneDefinition zone, float elapsed)
    {
        int ground = zone.GroundY;
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left + 118, ground - 8,
                zone.Bounds.Width - 236, 8),
            new Color(45, 48, 37));
        for (int x = zone.Bounds.Left + 154;
             x < zone.Bounds.Right - 110;
             x += 112)
        {
            int seed = Hash(x, 4);
            DrawWetStain(new Vector2(x, ground - 5),
                35 + Math.Abs(seed % 29));
            if ((x / 112) % 2 == 0)
                DrawCorpsePile(new Vector2(x + 22, ground), seed);
        }
        DrawRotHaze(zone, elapsed);
        DrawCollapsedNiche(new Vector2(zone.Bounds.Left + 720, ground), -1f);
    }

    private void DrawWraithHalls(CatacombZoneDefinition zone, float elapsed)
    {
        int ground = zone.GroundY;
        for (int x = zone.Bounds.Left + 80;
             x < zone.Bounds.Right - 100;
             x += 260)
        {
            DrawMemorialArch(x, ground, 180, 346);
            DrawMemorialStatue(new Vector2(x + 90, ground - 9),
                (x / 260) % 2 == 0);
        }
        for (int x = zone.Bounds.Left + 185;
             x < zone.Bounds.Right - 70;
             x += 226)
        {
            DrawSoulLamp(new Vector2(x, ground - 123), elapsed,
                .42f, false);
        }
        DrawSpectralFog(zone, elapsed, .42f);
    }

    private void DrawGuardBarracks(CatacombZoneDefinition zone)
    {
        int ground = zone.GroundY;
        DrawIronGate(new Rectangle(zone.Bounds.Left + 36,
            ground - 228, 142, 228), false);
        DrawIronGate(new Rectangle(zone.Bounds.Right - 178,
            ground - 228, 142, 228), true);
        DrawWeaponRack(new Vector2(zone.Bounds.Left + 505, ground), 184);
        DrawWeaponRack(new Vector2(zone.Bounds.Left + 760, ground), 164);
        DrawShield(new Vector2(zone.Bounds.Left + 385, ground - 105), false);
        DrawShield(new Vector2(zone.Bounds.Left + 940, ground - 116), true);
        DrawMilitaryInsignia(
            new Vector2(zone.Bounds.Center.X, ground - 235), .68f, false);
    }

    private void DrawCursedKnightMausoleum(
        CatacombZoneDefinition zone,
        float elapsed)
    {
        int ground = zone.GroundY;
        DrawArenaColumns(zone, 4, 248, false);
        DrawGrandSarcophagus(new Rectangle(
            zone.Bounds.Center.X - 128, ground - 96, 256, 96),
            false, true);
        DrawHangingCloth(new Vector2(zone.Bounds.Left + 260, ground - 284),
            78, 164, true);
        DrawHangingCloth(new Vector2(zone.Bounds.Right - 338, ground - 284),
            78, 164, false);
        DrawRitualSeal(new Vector2(zone.Bounds.Center.X, ground - 4),
            136f, Curse * .44f, elapsed);
    }

    private void DrawSoulChapel(CatacombZoneDefinition zone, float elapsed)
    {
        int ground = zone.GroundY;
        DrawChapelApse(zone.Bounds.Center.X, ground, 286, 410);
        DrawRitualAltar(new Vector2(zone.Bounds.Center.X, ground - 4),
            elapsed);
        for (int x = zone.Bounds.Left + 122;
             x < zone.Bounds.Right - 80;
             x += 238)
        {
            int length = 76 + Math.Abs(Hash(x, 8) % 110);
            DrawChain(new Vector2(x, zone.Bounds.Top + 5), length,
                x % 2 == 0);
            if ((x / 238) % 2 == 0)
                DrawSoulLamp(new Vector2(x, zone.Bounds.Top + length),
                    elapsed, .62f, true);
        }
        DrawSpectralFog(zone, elapsed, .62f);
    }

    private void DrawDeathKnightWarTomb(
        CatacombZoneDefinition zone,
        float elapsed)
    {
        int ground = zone.GroundY;
        DrawArenaColumns(zone, 5, 265, true);
        DrawGrandSarcophagus(new Rectangle(
            zone.Bounds.Center.X - 174, ground - 118, 348, 118),
            true, true);
        DrawArmorStand(new Vector2(zone.Bounds.Left + 330, ground), false,
            true);
        DrawArmorStand(new Vector2(zone.Bounds.Right - 330, ground), true,
            true);
        DrawBurialStatue(new Vector2(zone.Bounds.Left + 525, ground), false,
            new Color(67, 62, 63));
        DrawBurialStatue(new Vector2(zone.Bounds.Right - 525, ground), true,
            new Color(67, 62, 63));
        DrawMilitaryInsignia(new Vector2(zone.Bounds.Center.X, ground - 265),
            .92f, true);
        DrawRitualSeal(new Vector2(zone.Bounds.Center.X, ground - 4),
            178f, CurseRed * .42f, elapsed);
    }

    private void DrawFallenHall(
        CatacombZoneDefinition zone,
        float elapsed,
        float curseRatio)
    {
        int ground = zone.GroundY;
        DrawRoyalColonnade(zone, 6);
        DrawRoyalTomb(new Rectangle(
            zone.Bounds.Center.X - 152, ground - 130, 304, 130));
        DrawRoyalEmblem(new Vector2(zone.Bounds.Center.X, ground - 308),
            1.18f, true);
        DrawBrokenRoyalStatue(
            new Vector2(zone.Bounds.Left + 285, ground), false);
        DrawBrokenRoyalStatue(
            new Vector2(zone.Bounds.Right - 285, ground), true);
        DrawHangingCloth(new Vector2(zone.Bounds.Left + 475, ground - 330),
            92, 191, true);
        DrawHangingCloth(new Vector2(zone.Bounds.Right - 567, ground - 330),
            92, 191, false);
        DrawRitualSeal(new Vector2(zone.Bounds.Center.X, ground - 4),
            215f, Curse * (.35f + curseRatio * .12f), elapsed);
        DrawBattleScars(zone);
    }

    private void DrawCollisionPlatform(Platform platform)
    {
        Rectangle bounds = platform.Bounds;
        Color body = platform.Kind == PlatformKind.Raised
            ? new Color(73, 69, 62)
            : FloorStone;
        _spriteBatch.Draw(_pixel, bounds, body);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X, bounds.Y, bounds.Width,
                Math.Min(5, bounds.Height)), FloorEdge);
        for (int x = bounds.Left + 18; x < bounds.Right; x += 82)
        {
            int width = Math.Min(2, bounds.Right - x);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, bounds.Top + 5, width,
                    Math.Max(1, Math.Min(bounds.Height - 5, 13))),
                StoneDark * .52f);
        }
        if (platform.Kind == PlatformKind.Raised)
        {
            DrawLine(new Vector2(bounds.Left + 4, bounds.Bottom),
                new Vector2(bounds.Left + 16, bounds.Bottom + 12),
                3f, StoneDark * .72f);
            DrawLine(new Vector2(bounds.Right - 4, bounds.Bottom),
                new Vector2(bounds.Right - 16, bounds.Bottom + 12),
                3f, StoneDark * .72f);
        }
    }

    private void DrawTomb(
        Rectangle bounds,
        TombState state,
        float elapsed,
        float remaining)
    {
        Color baseColor = state == TombState.Disturbed
            ? new Color(86, 73, 61)
            : new Color(65, 63, 59);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X, bounds.Bottom - 29, bounds.Width, 29),
            StoneDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.X + 4, bounds.Bottom - 26,
                bounds.Width - 8, 22), baseColor);

        if (state == TombState.Destroyed)
        {
            DrawTombRubble(bounds);
            return;
        }

        bool open = state is TombState.Opened or TombState.Empty;
        if (open)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.X + 12, bounds.Y + 12,
                    bounds.Width - 24, 22), new Color(14, 15, 18));
            DrawTombLid(bounds, 35f, -17f, -.16f,
                new Color(75, 72, 66));
            if (state == TombState.Opened)
                DrawSkeletalHand(new Vector2(bounds.Center.X + 12,
                    bounds.Y + 22), .48f);
            else
                DrawBox(bounds.Center.X - 13, bounds.Y + 18,
                    26, 3, BoneDark * .42f);
            return;
        }

        float shake = state == TombState.Disturbed
            ? MathF.Sin(elapsed * 35f) *
                MathHelper.Clamp(remaining / MathF.Max(.01f,
                    TombInteractionManager.TelegraphSeconds), .25f, 1f) * 3f
            : 0f;
        DrawTombLid(bounds, shake,
            state == TombState.Disturbed ? -5f : -2f,
            state == TombState.Disturbed ? -.035f : 0f,
            state == TombState.Disturbed
                ? new Color(107, 90, 70)
                : new Color(78, 75, 69));

        if (state is TombState.Cracked or TombState.Disturbed)
        {
            Vector2 crack = new(bounds.Center.X + shake, bounds.Y + 11);
            DrawWallCrack(crack, 24f,
                state == TombState.Disturbed ? CurseRed * .72f : StoneDark,
                -1f);
            DrawSkeletalHand(new Vector2(bounds.Center.X + 21 + shake,
                bounds.Y + 4), state == TombState.Disturbed ? 1f : .45f);
        }
        if (state == TombState.Disturbed)
        {
            for (int index = 0; index < 4; index++)
            {
                float rise = (elapsed * (13f + index * 2f) + index * 9f) % 22f;
                DrawBox(bounds.Left + 16 + index * 19, bounds.Top - rise,
                    4, 3, new Color(122, 111, 94) * .45f);
            }
        }
    }

    private void DrawSafeShrine(SafeShrineDefinition shrine, float elapsed)
    {
        Vector2 p = shrine.Position;
        float pulse = .76f + MathF.Sin(elapsed * 2.4f) * .10f;
        for (int ring = 3; ring >= 1; ring--)
        {
            float radius = 24f + ring * 17f;
            DrawDiamond(new Vector2(p.X, p.Y - 3f), radius,
                SoulLight * (.05f * ring));
        }
        DrawLine(p + new Vector2(-72f, -3f),
            p + new Vector2(72f, -3f), 4f,
            new Color(103, 102, 90) * .78f);
        DrawRuneCircle(p + new Vector2(0f, -4f), 55f,
            SoulLight * (.42f * pulse));
        DrawBox(p.X - 27f, p.Y - 37f, 54f, 34f,
            new Color(70, 69, 63));
        DrawBox(p.X - 20f, p.Y - 43f, 40f, 8f,
            new Color(99, 96, 83));
        DrawBox(p.X - 5f, p.Y - 78f, 10f, 36f,
            new Color(84, 85, 78));
        DrawDiamond(p + new Vector2(0f, -79f), 13f,
            SoulLight * (.72f * pulse));
        for (int index = 0; index < 4; index++)
        {
            float angle = elapsed * .36f + index * MathHelper.PiOver2;
            DrawBox(p.X + MathF.Cos(angle) * 42f - 2f,
                p.Y - 29f + MathF.Sin(angle) * 9f,
                4, 3, SoulLight * .38f);
        }
    }

    private void DrawCurseResidue(
        DungeonMap map,
        Rectangle visible,
        float elapsed,
        float curseRatio)
    {
        foreach (CurseZoneDefinition zone in map.CurseZones)
        {
            if (!zone.Bounds.Intersects(visible))
                continue;
            float strength = .22f + curseRatio * .24f;
            Color color = zone.Id.Contains("war", StringComparison.Ordinal) ||
                zone.Id.Contains("fallen", StringComparison.Ordinal)
                ? CurseRed * strength
                : Curse * strength;
            DrawLine(new Vector2(zone.Bounds.Left, zone.Bounds.Bottom - 4),
                new Vector2(zone.Bounds.Right, zone.Bounds.Bottom - 4),
                3f, color);
            for (int x = zone.Bounds.Left + 24;
                 x < zone.Bounds.Right;
                 x += 68)
            {
                int seed = Hash(x, zone.Bounds.Width);
                float rise = (elapsed * (8f + Math.Abs(seed % 5)) +
                    Math.Abs(seed % 41)) % 56f;
                DrawBox(x, zone.Bounds.Bottom - 10f - rise,
                    3, 4, color * (1f - rise / 75f));
            }
        }
    }

    private void DrawDepthDust(Rectangle camera, float curseRatio,
        float elapsed)
    {
        int count = 14 + (int)(curseRatio * 8f);
        for (int index = 0; index < count; index++)
        {
            int seed = Hash(index, 913);
            float x = camera.Left + PositiveModulo(
                (int)(seed + elapsed * (5f + index % 4)),
                camera.Width + 120) - 60;
            float y = camera.Top + 70 + PositiveModulo(seed / 7,
                Math.Max(1, camera.Height - 125));
            Color color = curseRatio > .6f && index % 3 == 0
                ? Curse * (.07f + curseRatio * .06f)
                : new Color(120, 116, 108) * .07f;
            DrawBox(x, y, index % 4 == 0 ? 5f : 3f, 2f, color);
        }
    }

    private void DrawCursePressureEchoes(
        Rectangle camera,
        float curseRatio,
        float elapsed)
    {
        if (curseRatio <= .58f)
            return;
        float strength = (curseRatio - .58f) / .42f;
        for (int index = 0; index < 5; index++)
        {
            int seed = Hash(index, 1201);
            float x = camera.Left + PositiveModulo(seed, camera.Width - 80);
            float y = camera.Top + 72f + index * 83f;
            float offset = MathF.Sin(elapsed * (1.7f + index * .11f) +
                index) * (3f + strength * 5f);
            Color color = index % 2 == 0
                ? Curse * (.05f + strength * .05f)
                : CurseRed * (.04f + strength * .04f);
            DrawLine(new Vector2(x + offset, y),
                new Vector2(x + 38f + offset, y + 1f), 2f, color);
            DrawLine(new Vector2(x - offset * .5f, y + 6f),
                new Vector2(x + 25f - offset * .5f, y + 6f), 1f,
                color * .72f);
        }
    }

    private void DrawForegroundDebris(
        CatacombZoneDefinition zone,
        Rectangle camera)
    {
        int first = Math.Max(zone.Bounds.Left,
            camera.Left - PositiveModulo(camera.Left, 146));
        int last = Math.Min(zone.Bounds.Right, camera.Right + 80);
        for (int x = first; x < last; x += 146)
        {
            int seed = Hash(x, (int)zone.Kind + 71);
            int width = 12 + Math.Abs(seed % 22);
            int height = 3 + Math.Abs(seed % 6);
            Color color = zone.Kind == CatacombZoneKind.OssuaryCorridors
                ? BoneDark * .62f
                : StoneDark * .72f;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, zone.GroundY - height, width, height),
                color);
        }
    }

    private void DrawFarArch(int x, int top, int width, int height)
    {
        DrawBox(x, top + 86, 34, height - 86, FarStone);
        DrawBox(x + width - 34, top + 86, 34, height - 86, FarStone);
        DrawBox(x + 20, top + 44, width - 40, 44, FarStone);
        DrawLine(new Vector2(x + 18, top + 89),
            new Vector2(x + width / 2f, top + 8), 28f, FarStone);
        DrawLine(new Vector2(x + width - 18, top + 89),
            new Vector2(x + width / 2f, top + 8), 28f, FarStone);
        DrawBox(x + 48, top + 102, width - 96, height - 114,
            new Color(8, 10, 14));
    }

    private void DrawDistantPillar(int x, int baseY, int height)
    {
        DrawBox(x, baseY - height, 30, height, MidStone * .72f);
        DrawBox(x - 8, baseY - height - 8, 46, 10,
            MidStone * .78f);
        DrawBox(x - 10, baseY - 12, 50, 12, MidStone * .76f);
    }

    private void DrawMonumentalArch(int x, int ground, int width, int height,
        Color color, bool broken)
    {
        DrawBox(x, ground - height + 78, 42, height - 78, color);
        DrawBox(x + width - 42, ground - height + 78, 42,
            height - 78, color);
        DrawLine(new Vector2(x + 20, ground - height + 86),
            new Vector2(x + width / 2f, ground - height + 8),
            29f, color);
        if (!broken)
            DrawLine(new Vector2(x + width - 20, ground - height + 86),
                new Vector2(x + width / 2f, ground - height + 8),
                29f, color);
        else
        {
            DrawLine(new Vector2(x + width - 20, ground - height + 86),
                new Vector2(x + width * .72f, ground - height + 42),
                29f, color);
            DrawStoneFragment(new Vector2(x + width * .77f,
                ground - height + 64), 12, 8);
        }
        DrawBox(x - 10, ground - 14, 62, 14, StoneDark);
        DrawBox(x + width - 52, ground - 14, 62, 14, StoneDark);
    }

    private void DrawSealedDoor(Rectangle bounds)
    {
        DrawBox(bounds.X, bounds.Y, bounds.Width, bounds.Height, StoneDark);
        DrawBox(bounds.X + 8, bounds.Y + 7, bounds.Width - 16,
            bounds.Height - 7, new Color(54, 52, 49));
        for (int y = bounds.Top + 34; y < bounds.Bottom; y += 48)
            DrawBox(bounds.X + 8, y, bounds.Width - 16, 3,
                StoneEdge * .48f);
        DrawRoyalEmblem(new Vector2(bounds.Center.X, bounds.Y + 82),
            .48f, false);
        DrawLine(new Vector2(bounds.Center.X, bounds.Top + 8),
            new Vector2(bounds.Center.X, bounds.Bottom), 4f,
            StoneDark);
    }

    private void DrawStoneSteps(int left, int ground, int width)
    {
        for (int index = 0; index < 4; index++)
        {
            int stepLeft = left + index * 58;
            int y = ground - 24 + index * 3;
            DrawBox(stepLeft, y, width - index * 82, 5,
                FloorEdge * (.72f - index * .08f));
        }
    }

    private void DrawBurialAlcove(Rectangle bounds, bool collapsed)
    {
        DrawBox(bounds.X, bounds.Y + 18, bounds.Width, bounds.Height - 18,
            StoneDark * .82f);
        DrawLine(new Vector2(bounds.Left, bounds.Top + 26),
            new Vector2(bounds.Center.X, bounds.Top), 14f, StoneEdge * .65f);
        if (!collapsed)
            DrawLine(new Vector2(bounds.Right, bounds.Top + 26),
                new Vector2(bounds.Center.X, bounds.Top), 14f,
                StoneEdge * .65f);
        DrawBox(bounds.X + 12, bounds.Bottom - 30, bounds.Width - 24,
            7, new Color(74, 69, 61));
        if (collapsed)
        {
            DrawStoneFragment(new Vector2(bounds.Right - 19,
                bounds.Bottom - 8), 18, 11);
            DrawStoneFragment(new Vector2(bounds.Right - 39,
                bounds.Bottom - 5), 13, 7);
        }
    }

    private void DrawBoneStack(Vector2 p, int count)
    {
        for (int index = 0; index < count; index++)
        {
            float x = p.X - count * 5f + index * 10f;
            float y = p.Y - (index % 2) * 6f;
            DrawSkull(new Vector2(x, y), .58f);
            DrawLine(new Vector2(x - 8f, y + 8f),
                new Vector2(x + 9f, y + 14f), 3f, BoneDark);
        }
    }

    private void DrawSkull(Vector2 p, float scale)
    {
        DrawBox(p.X - 8f * scale, p.Y - 9f * scale,
            16f * scale, 14f * scale, Bone);
        DrawBox(p.X - 5f * scale, p.Y + 4f * scale,
            10f * scale, 6f * scale, BoneDark);
        DrawBox(p.X - 5f * scale, p.Y - 4f * scale,
            3f * scale, 3f * scale, StoneDark);
        DrawBox(p.X + 2f * scale, p.Y - 4f * scale,
            3f * scale, 3f * scale, StoneDark);
    }

    private void DrawCollapsedNiche(Vector2 p, float direction)
    {
        for (int index = 0; index < 6; index++)
        {
            float x = p.X + direction * index * 17f;
            DrawStoneFragment(new Vector2(x, p.Y - 4f - index % 2 * 5f),
                18 - index, 8 + index % 3 * 3);
        }
        DrawLine(p + new Vector2(0f, -12f),
            p + new Vector2(direction * 65f, -54f), 5f,
            StoneDark * .78f);
    }

    private void DrawArrowSlit(Rectangle bounds)
    {
        DrawBox(bounds.X - 7, bounds.Y - 5, bounds.Width + 14,
            bounds.Height + 10, StoneEdge * .58f);
        DrawBox(bounds.X, bounds.Y, bounds.Width, bounds.Height,
            new Color(12, 14, 18));
        DrawBox(bounds.X + bounds.Width / 2 - 2, bounds.Y - 10, 4,
            bounds.Height + 20, StoneDark);
    }

    private void DrawBalconyBackdrop(int left, int y, int width)
    {
        DrawBox(left, y - 18, width, 10, StoneEdge * .65f);
        for (int x = left + 20; x < left + width; x += 72)
        {
            DrawBox(x, y - 14, 8, 62, StoneDark * .76f);
            DrawLine(new Vector2(x + 4, y + 48),
                new Vector2(x + 28, y + 76), 5f, StoneDark * .72f);
        }
    }

    private void DrawHangingCloth(Vector2 p, int width, int height,
        bool tornRight)
    {
        DrawBox(p.X, p.Y, width, 7, Iron);
        DrawBox(p.X + 7, p.Y + 6, width - 14, height - 18, Cloth * .82f);
        float tearX = tornRight ? p.X + width - 21f : p.X + 8f;
        DrawLine(new Vector2(tearX, p.Y + height - 34),
            new Vector2(tearX + (tornRight ? 13f : -11f), p.Y + height),
            5f, Cloth * .68f);
        DrawLine(new Vector2(p.X + width / 2f, p.Y + 18f),
            new Vector2(p.X + width / 2f - 9f, p.Y + height - 22f),
            3f, new Color(36, 29, 33) * .64f);
    }

    private void DrawWetStain(Vector2 p, int width)
    {
        DrawLine(new Vector2(p.X - width / 2f, p.Y),
            new Vector2(p.X + width / 2f, p.Y), 6f,
            new Color(35, 39, 31) * .88f);
        DrawBox(p.X - width * .22f, p.Y - 4f, width * .44f, 3f,
            Rot * .36f);
    }

    private void DrawCorpsePile(Vector2 p, int seed)
    {
        DrawLine(p + new Vector2(-25f, -4f),
            p + new Vector2(20f, -18f), 13f,
            new Color(62, 59, 47));
        DrawSkull(p + new Vector2(seed % 11, -26f), .65f);
        DrawLine(p + new Vector2(-14f, -16f),
            p + new Vector2(-35f, -3f), 5f, BoneDark);
        DrawLine(p + new Vector2(8f, -15f),
            p + new Vector2(37f, -5f), 5f, BoneDark);
        DrawBox(p.X - 18f, p.Y - 10f, 38f, 4f, Blood * .48f);
    }

    private void DrawRotHaze(CatacombZoneDefinition zone, float elapsed)
    {
        for (int index = 0; index < 10; index++)
        {
            int seed = Hash(index, (int)zone.Kind);
            float x = zone.Bounds.Left + 70 + PositiveModulo(
                (int)(index * 127 + elapsed * (5 + index % 3)),
                zone.Bounds.Width - 140);
            float y = zone.GroundY - 24f - Math.Abs(seed % 74);
            DrawLine(new Vector2(x - 18f, y),
                new Vector2(x + 22f, y - 3f), 3f,
                Rot * (.10f + index % 3 * .025f));
        }
    }

    private void DrawMemorialArch(int x, int ground, int width, int height)
    {
        DrawMonumentalArch(x, ground, width, height,
            new Color(57, 59, 63), false);
        DrawBox(x + 44, ground - height + 94, width - 88, height - 110,
            new Color(21, 24, 31));
    }

    private void DrawMemorialStatue(Vector2 p, bool broken)
    {
        Color color = new(69, 70, 72);
        DrawBox(p.X - 26, p.Y - 18, 52, 18, StoneDark);
        DrawLine(p + new Vector2(0f, -18f),
            p + new Vector2(broken ? 12f : 0f, -105f), 25f, color);
        if (!broken)
            DrawBox(p.X - 12, p.Y - 134, 24, 28, color);
        else
            DrawStoneFragment(p + new Vector2(26f, -7f), 17, 13);
        DrawLine(p + new Vector2(-5f, -82f),
            p + new Vector2(-30f, -42f), 8f, color);
        DrawLine(p + new Vector2(7f, -79f),
            p + new Vector2(27f, -39f), 8f, color);
    }

    private void DrawSoulLamp(Vector2 p, float elapsed, float strength,
        bool chained)
    {
        if (chained)
            DrawLine(p + new Vector2(0f, -24f), p, 3f, Iron);
        DrawBox(p.X - 8f, p.Y - 10f, 16f, 20f, Iron);
        DrawBox(p.X - 4f, p.Y - 6f, 8f, 12f,
            SoulLight * (strength + MathF.Sin(elapsed * 3.5f + p.X) * .08f));
        DrawDiamond(p, 18f, Soul * (strength * .32f));
    }

    private void DrawSpectralFog(
        CatacombZoneDefinition zone,
        float elapsed,
        float strength)
    {
        for (int index = 0; index < 8; index++)
        {
            float x = zone.Bounds.Left + 45f +
                PositiveModulo((int)(index * 181f + elapsed * 9f),
                    zone.Bounds.Width - 90);
            float y = zone.GroundY - 24f - index % 3 * 20f;
            DrawLine(new Vector2(x - 32f, y),
                new Vector2(x + 36f, y - 4f), 3f,
                Soul * (strength * .16f));
        }
    }

    private void DrawIronGate(Rectangle bounds, bool broken)
    {
        DrawBox(bounds.X, bounds.Y, 12, bounds.Height, Iron);
        DrawBox(bounds.Right - 12, bounds.Y, 12, bounds.Height, Iron);
        DrawBox(bounds.X, bounds.Y, bounds.Width, 13, Iron);
        DrawBox(bounds.X, bounds.Y + 78, bounds.Width, 8, Rust * .82f);
        for (int x = bounds.Left + 24; x < bounds.Right - 12; x += 24)
        {
            int height = broken && x > bounds.Center.X
                ? bounds.Height - 58
                : bounds.Height - 13;
            DrawBox(x, bounds.Y + 13, 7, height, Iron * .90f);
            DrawLine(new Vector2(x - 4, bounds.Y + 13),
                new Vector2(x + 3, bounds.Y - 2), 5f, Rust);
        }
    }

    private void DrawWeaponRack(Vector2 p, int width)
    {
        DrawLine(p + new Vector2(-width / 2f, -18f),
            p + new Vector2(-width / 2f + 13f, -103f), 9f, Rust);
        DrawLine(p + new Vector2(width / 2f, -18f),
            p + new Vector2(width / 2f - 13f, -103f), 9f, Rust);
        DrawLine(p + new Vector2(-width / 2f + 8f, -84f),
            p + new Vector2(width / 2f - 8f, -84f), 8f, Rust);
        for (int index = 0; index < 5; index++)
        {
            float x = p.X - width * .36f + index * width * .18f;
            DrawLine(new Vector2(x, p.Y - 25f),
                new Vector2(x + (index % 2 == 0 ? 12f : -9f), p.Y - 128f),
                4f, new Color(104, 99, 90));
        }
    }

    private void DrawShield(Vector2 p, bool damaged)
    {
        DrawDiamond(p, 31f, Iron);
        DrawDiamond(p, 24f, new Color(85, 73, 65));
        DrawLine(p + new Vector2(0f, -10f),
            p + new Vector2(0f, 15f), 4f, Rust);
        if (damaged)
            DrawWallCrack(p + new Vector2(4f, -8f), 24f,
                StoneDark, 1f);
    }

    private void DrawMilitaryInsignia(Vector2 p, float scale, bool broken)
    {
        DrawLine(p + new Vector2(0f, -48f * scale),
            p + new Vector2(0f, 50f * scale), 5f, RoyalGold * .62f);
        DrawLine(p + new Vector2(-35f * scale, -18f * scale),
            p + new Vector2(35f * scale, 18f * scale), 5f,
            RoyalGold * .62f);
        DrawLine(p + new Vector2(35f * scale, -18f * scale),
            p + new Vector2(-35f * scale, 18f * scale), 5f,
            RoyalGold * .62f);
        if (broken)
            DrawBox(p.X + 5f, p.Y - 4f, 38f * scale, 7f,
                Stone * .92f);
    }

    private void DrawArenaColumns(
        CatacombZoneDefinition zone,
        int count,
        int height,
        bool martial)
    {
        float spacing = zone.Bounds.Width / (count + 1f);
        for (int index = 1; index <= count; index++)
        {
            int x = (int)(zone.Bounds.Left + spacing * index);
            bool broken = index == 2 && count > 4;
            int drawnHeight = broken ? height - 58 : height;
            DrawColumn(x, zone.GroundY, drawnHeight,
                martial ? new Color(66, 61, 60) : new Color(68, 65, 62),
                broken);
        }
    }

    private void DrawColumn(int x, int ground, int height, Color color,
        bool broken)
    {
        DrawBox(x - 25, ground - height, 50, height, StoneDark);
        DrawBox(x - 18, ground - height + 8, 36, height - 15, color);
        DrawBox(x - 34, ground - 16, 68, 16, StoneEdge * .70f);
        if (!broken)
        {
            DrawBox(x - 32, ground - height - 12, 64, 15,
                StoneEdge * .72f);
            DrawBox(x - 24, ground - height + 44, 48, 5,
                StoneDark * .58f);
        }
        else
        {
            DrawStoneFragment(new Vector2(x + 38, ground - 7), 22, 12);
            DrawStoneFragment(new Vector2(x + 62, ground - 5), 15, 9);
        }
    }

    private void DrawGrandSarcophagus(Rectangle bounds, bool martial,
        bool cracked)
    {
        DrawBox(bounds.X, bounds.Bottom - 34, bounds.Width, 34, StoneDark);
        DrawBox(bounds.X + 8, bounds.Bottom - 48,
            bounds.Width - 16, 22, new Color(73, 69, 65));
        DrawBox(bounds.X + 23, bounds.Top + 22,
            bounds.Width - 46, bounds.Height - 62,
            new Color(81, 76, 70));
        DrawLine(new Vector2(bounds.Left + 22, bounds.Top + 24),
            new Vector2(bounds.Center.X, bounds.Top + 3),
            12f, StoneEdge);
        DrawLine(new Vector2(bounds.Right - 22, bounds.Top + 24),
            new Vector2(bounds.Center.X, bounds.Top + 3),
            12f, StoneEdge);
        if (martial)
            DrawMilitaryInsignia(new Vector2(bounds.Center.X,
                bounds.Center.Y - 4), .48f, true);
        else
            DrawRoyalEmblem(new Vector2(bounds.Center.X,
                bounds.Center.Y - 4), .48f, false);
        if (cracked)
            DrawWallCrack(new Vector2(bounds.Center.X + 32,
                bounds.Top + 23), 38f, Curse * .68f, 1f);
    }

    private void DrawRitualSeal(Vector2 p, float radius, Color color,
        float elapsed)
    {
        float pulse = .86f + MathF.Sin(elapsed * 2f) * .10f;
        DrawRuneCircle(p, radius, color * pulse);
        DrawDiamond(p, radius * .56f, color * (.76f * pulse));
        DrawLine(p + new Vector2(-radius * .72f, 0f),
            p + new Vector2(radius * .72f, 0f), 2f, color * .72f);
    }

    private void DrawChapelApse(int centerX, int ground, int width,
        int height)
    {
        int left = centerX - width / 2;
        DrawMonumentalArch(left, ground, width, height,
            new Color(62, 62, 68), false);
        DrawBox(left + 50, ground - height + 112, width - 100,
            height - 130, new Color(22, 24, 31));
        DrawRoyalEmblem(new Vector2(centerX, ground - height + 130),
            .72f, true);
    }

    private void DrawRitualAltar(Vector2 p, float elapsed)
    {
        DrawBox(p.X - 84, p.Y - 31, 168, 31, StoneDark);
        DrawBox(p.X - 66, p.Y - 50, 132, 21,
            new Color(78, 73, 70));
        DrawBox(p.X - 5, p.Y - 84, 10, 35,
            new Color(91, 84, 78));
        DrawSoulLamp(p + new Vector2(0f, -99f), elapsed, .68f, false);
        DrawLine(p + new Vector2(-52f, -54f),
            p + new Vector2(52f, -54f), 3f, Curse * .52f);
    }

    private void DrawChain(Vector2 p, int length, bool broken)
    {
        int segments = length / 12;
        for (int index = 0; index < segments; index++)
        {
            if (broken && index == segments - 2)
                break;
            float sway = MathF.Sin(index * .85f) * 3f;
            DrawDiamond(new Vector2(p.X + sway, p.Y + index * 12f),
                5f, Iron * .82f);
        }
    }

    private void DrawArmorStand(Vector2 p, bool flip, bool broken)
    {
        float sign = flip ? -1f : 1f;
        DrawLine(p + new Vector2(0f, -8f),
            p + new Vector2(0f, -132f), 7f, Rust);
        DrawLine(p + new Vector2(-29f, -91f),
            p + new Vector2(29f, -91f), 7f, Rust);
        DrawBox(p.X - 22, p.Y - 126, 44, 49, Iron);
        DrawBox(p.X - 15, p.Y - 153, 30, 28,
            new Color(62, 60, 63));
        DrawLine(p + new Vector2(sign * 25f, -86f),
            p + new Vector2(sign * 47f, broken ? -21f : -152f),
            6f, new Color(106, 102, 95));
        if (broken)
            DrawStoneFragment(p + new Vector2(sign * 34f, -5f), 18, 10);
    }

    private void DrawRoyalColonnade(CatacombZoneDefinition zone, int count)
    {
        float spacing = zone.Bounds.Width / (count + 1f);
        for (int index = 1; index <= count; index++)
        {
            int x = (int)(zone.Bounds.Left + spacing * index);
            DrawColumn(x, zone.GroundY,
                index is 2 or 5 ? 266 : 338,
                new Color(69, 65, 66), index is 2 or 5);
            if (index % 2 == 1)
                DrawBox(x - 21, zone.GroundY - 226, 42, 5,
                    RoyalGold * .46f);
        }
    }

    private void DrawRoyalTomb(Rectangle bounds)
    {
        DrawGrandSarcophagus(bounds, false, true);
        DrawBox(bounds.X - 34, bounds.Bottom - 22, 32, 22, StoneDark);
        DrawBox(bounds.Right + 2, bounds.Bottom - 22, 32, 22, StoneDark);
        DrawRoyalEmblem(new Vector2(bounds.Center.X, bounds.Center.Y - 4f),
            .72f, true);
    }

    private void DrawRoyalEmblem(Vector2 p, float scale, bool broken)
    {
        Color color = RoyalGold * .66f;
        DrawDiamond(p, 36f * scale, color);
        DrawLine(p + new Vector2(0f, -31f * scale),
            p + new Vector2(0f, 35f * scale), 4f, color);
        DrawLine(p + new Vector2(-28f * scale, 2f),
            p + new Vector2(28f * scale, 2f), 4f, color);
        DrawLine(p + new Vector2(-20f * scale, -22f * scale),
            p + new Vector2(0f, -39f * scale), 4f, color);
        DrawLine(p + new Vector2(20f * scale, -22f * scale),
            p + new Vector2(0f, -39f * scale), 4f, color);
        if (broken)
            DrawBox(p.X + 5f, p.Y - 7f, 29f * scale, 9f,
                Stone * .92f);
    }

    private void DrawBrokenRoyalStatue(Vector2 p, bool flip)
    {
        float sign = flip ? -1f : 1f;
        DrawBox(p.X - 38, p.Y - 17, 76, 17, StoneDark);
        DrawLine(p + new Vector2(0f, -15f),
            p + new Vector2(sign * 10f, -139f), 34f,
            new Color(73, 69, 68));
        DrawLine(p + new Vector2(sign * 2f, -114f),
            p + new Vector2(sign * 42f, -74f), 10f,
            new Color(73, 69, 68));
        DrawStoneFragment(p + new Vector2(sign * 52f, -7f), 27, 18);
        DrawStoneFragment(p + new Vector2(sign * 82f, -5f), 18, 12);
    }

    private void DrawBattleScars(CatacombZoneDefinition zone)
    {
        int ground = zone.GroundY;
        DrawLine(new Vector2(zone.Bounds.Left + 545, ground - 6),
            new Vector2(zone.Bounds.Left + 610, ground - 28), 5f, Blood * .56f);
        DrawLine(new Vector2(zone.Bounds.Left + 625, ground - 8),
            new Vector2(zone.Bounds.Left + 702, ground - 4), 4f, Blood * .42f);
        DrawLine(new Vector2(zone.Bounds.Right - 510, ground - 9),
            new Vector2(zone.Bounds.Right - 450, ground - 35), 4f,
            StoneDark);
        DrawShield(new Vector2(zone.Bounds.Left + 180, ground - 37), true);
        DrawLine(new Vector2(zone.Bounds.Right - 190, ground - 4),
            new Vector2(zone.Bounds.Right - 126, ground - 75), 6f,
            new Color(105, 98, 88));
    }

    private void DrawBurialStatue(Vector2 p, bool flip, Color color)
    {
        float sign = flip ? -1f : 1f;
        DrawBox(p.X - 33, p.Y - 15, 66, 15, StoneDark);
        DrawLine(p + new Vector2(0f, -12f),
            p + new Vector2(0f, -146f), 31f, color);
        DrawBox(p.X - 14, p.Y - 177, 28, 31, color);
        DrawLine(p + new Vector2(-12f, -115f),
            p + new Vector2(sign * -36f, -65f), 9f, color);
        DrawLine(p + new Vector2(12f, -114f),
            p + new Vector2(sign * 34f, -58f), 9f, color);
        DrawLine(p + new Vector2(sign * 28f, -56f),
            p + new Vector2(sign * 44f, -13f), 5f,
            new Color(95, 91, 83));
    }

    private void DrawBrokenMarker(Vector2 p, float direction)
    {
        DrawBox(p.X - 22, p.Y - 12, 44, 12, StoneDark);
        DrawLine(p + new Vector2(0f, -9f),
            p + new Vector2(direction * 18f, -82f), 17f, StoneEdge * .72f);
        DrawWallCrack(p + new Vector2(direction * 12f, -70f),
            26f, StoneDark, direction);
    }

    private void DrawCursedWallCracks(
        CatacombZoneDefinition zone,
        float corruption)
    {
        Color color = zone.Kind is CatacombZoneKind.DeathKnightWarTomb or
            CatacombZoneKind.FallenHall
            ? CurseRed * (.24f + corruption * .30f)
            : Curse * (.20f + corruption * .28f);
        int count = corruption > .72f ? 5 : 3;
        for (int index = 0; index < count; index++)
        {
            float x = zone.Bounds.Left + zone.Bounds.Width *
                ((index + 1f) / (count + 1f));
            float y = zone.Bounds.Top + 108 + index % 2 * 78;
            DrawWallCrack(new Vector2(x, y),
                47f + index * 7f, color, index % 2 == 0 ? 1f : -1f);
        }
    }

    private void DrawWallCrack(Vector2 start, float length, Color color,
        float direction)
    {
        Vector2 middle = start + new Vector2(direction * 8f, length * .38f);
        Vector2 end = start + new Vector2(direction * -3f, length);
        DrawLine(start, middle, 2f, color);
        DrawLine(middle, end, 2f, color);
        DrawLine(middle, middle + new Vector2(direction * 15f, 11f),
            2f, color * .82f);
    }

    private void DrawTombLid(Rectangle bounds, float xOffset,
        float yOffset, float angle, Color color)
    {
        Vector2 position = new(bounds.X + xOffset, bounds.Y + yOffset);
        _spriteBatch.Draw(_pixel, position, null, color, angle,
            Vector2.Zero, new Vector2(bounds.Width - 8f, 16f),
            SpriteEffects.None, 0f);
        DrawLine(position + new Vector2(8f, 7f),
            position + new Vector2(bounds.Width - 18f, 7f),
            2f, StoneEdge * .72f);
    }

    private void DrawSkeletalHand(Vector2 p, float extension)
    {
        DrawLine(p, p + new Vector2(0f, -17f * extension), 4f, Bone);
        for (int finger = -2; finger <= 2; finger++)
        {
            DrawLine(p + new Vector2(0f, -14f * extension),
                p + new Vector2(finger * 4f, -25f * extension),
                2f, Bone);
        }
    }

    private void DrawTombRubble(Rectangle bounds)
    {
        for (int index = 0; index < 7; index++)
        {
            int seed = Hash(bounds.X, index);
            DrawStoneFragment(new Vector2(
                    bounds.Left + 7 + PositiveModulo(seed, bounds.Width - 14),
                    bounds.Bottom - 3 - Math.Abs(seed % 8)),
                9 + Math.Abs(seed % 13), 5 + Math.Abs(seed % 8));
        }
    }

    private void DrawStoneFragment(Vector2 p, int width, int height)
    {
        DrawBox(p.X - width / 2f, p.Y - height, width, height,
            new Color(70, 67, 62));
        DrawLine(new Vector2(p.X - width / 2f, p.Y - height),
            new Vector2(p.X + width / 2f, p.Y - height + 2f),
            2f, StoneEdge * .62f);
    }

    private void DrawRuneCircle(Vector2 center, float radius, Color color)
    {
        const int segments = 12;
        Vector2 prior = center + new Vector2(radius, 0f);
        for (int index = 1; index <= segments; index++)
        {
            float angle = MathHelper.TwoPi * index / segments;
            Vector2 next = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * .18f);
            DrawLine(prior, next, 2f, color);
            prior = next;
        }
        for (int index = 0; index < 4; index++)
        {
            float x = center.X - radius * .52f + index * radius * .35f;
            DrawLine(new Vector2(x, center.Y - 5f),
                new Vector2(x + 8f, center.Y + 5f), 2f, color * .72f);
        }
    }

    private void DrawDiamond(Vector2 center, float radius, Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius * .48f);
        Vector2 right = center + new Vector2(radius, 0f);
        Vector2 bottom = center + new Vector2(0f, radius * .48f);
        Vector2 left = center + new Vector2(-radius, 0f);
        DrawLine(top, right, 2f, color);
        DrawLine(right, bottom, 2f, color);
        DrawLine(bottom, left, 2f, color);
        DrawLine(left, top, 2f, color);
    }

    private CatacombZoneDefinition FindVisibleZone(DungeonMap map, int x)
    {
        foreach (CatacombZoneDefinition zone in map.CatacombZones)
            if (x >= zone.Bounds.Left && x < zone.Bounds.Right)
                return zone;
        return null;
    }

    private static bool IsInsideShrineCalm(DungeonMap map, Vector2 position)
    {
        foreach (SafeShrineDefinition shrine in map.SafeShrines)
        {
            float calmRadius = shrine.Radius * .88f;
            if (Vector2.DistanceSquared(position, shrine.Position) <=
                calmRadius * calmRadius)
                return true;
        }
        return false;
    }

    private static int Hash(int first, int second)
    {
        unchecked
        {
            int hash = first * 73856093 ^ second * 19349663;
            hash ^= hash >> 13;
            return hash;
        }
    }

    private static int PositiveModulo(int value, int divisor)
    {
        if (divisor <= 0)
            return 0;
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private void DrawBox(float x, float y, float width, float height,
        Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(x),
            (int)MathF.Round(y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(_pixel, from, null, color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
