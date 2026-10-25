using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered, code-only presentation for the Map 7 foundation. Decorative
/// geometry is intentionally collision-free and foreground coverage is sparse.
/// </summary>
public sealed class DemonKingSanctumEnvironmentRenderer
{
    private static readonly Color AbyssTop = new(5, 4, 10);
    private static readonly Color AbyssBottom = new(24, 10, 27);
    private static readonly Color FarStone = new(15, 14, 22);
    private static readonly Color DeepStone = new(27, 25, 34);
    private static readonly Color TempleStone = new(43, 39, 49);
    private static readonly Color StoneEdge = new(78, 69, 82);
    private static readonly Color OldBronze = new(112, 80, 48);
    private static readonly Color RoyalGold = new(164, 120, 58);
    private static readonly Color RitualRed = new(119, 31, 44);
    private static readonly Color AbyssViolet = new(77, 45, 111);
    private static readonly Color VoidBlue = new(39, 43, 91);
    private static readonly Color Ember = new(211, 82, 37);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public DemonKingSanctumEnvironmentRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawWorld(
        GameSession session,
        Rectangle camera,
        GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsDemonKingSanctum != true)
            return;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        DrawAbyss(camera);
        DrawFarTemple(map, camera, time);
        foreach (DemonKingSanctumZoneDefinition zone in
            map.DemonKingSanctumZones)
        {
            if (zone.Bounds.Intersects(camera))
                DrawZoneArchitecture(zone, time);
        }
        DrawProps(map, camera, SanctumLayer.Background, time);
        DrawProps(map, camera, SanctumLayer.Midground, time);

        foreach (Rectangle corridor in map.Corridors)
            if (corridor.Intersects(camera))
                DrawMasonry(corridor, DeepStone, 56);
        foreach (Platform platform in map.Platforms)
            if (platform.Bounds.Intersects(camera))
                DrawPlatform(platform);

        DrawCheckpoints(map, camera, time);
    }

    public void DrawForeground(
        GameSession session,
        Rectangle camera,
        GameTime gameTime)
    {
        if (session.CurrentDungeon?.IsDemonKingSanctum != true)
            return;
        DrawProps(
            session.CurrentDungeon,
            camera,
            SanctumLayer.Foreground,
            (float)gameTime.TotalGameTime.TotalSeconds);
    }

    private void DrawAbyss(Rectangle camera)
    {
        int bandHeight = Math.Max(1, camera.Height / 10);
        for (int band = 0; band < 10; band++)
        {
            Color color = Color.Lerp(AbyssTop, AbyssBottom, band / 9f);
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    camera.Left,
                    camera.Top + band * bandHeight,
                    camera.Width,
                    band == 9
                        ? camera.Height - band * bandHeight
                        : bandHeight),
                color);
        }
    }

    private void DrawFarTemple(DungeonMap map, Rectangle camera, float time)
    {
        int horizon = map.WorldBounds.Top + 535;
        int parallax = (int)(camera.Left * .82f);
        int first = (int)MathF.Floor(camera.Left * .18f / 330f) - 3;
        int last = first + camera.Width / 330 + 8;
        for (int index = first; index <= last; index++)
        {
            int x = parallax + index * 330;
            int height = 250 + Math.Abs(index * 71 % 190);
            int width = 190 + Math.Abs(index * 37 % 80);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, horizon - height, width, height), FarStone);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + width / 2 - 25, horizon - height - 85,
                    50, 90), FarStone);
            DrawLine(
                new Vector2(x + 20, horizon - height + 36),
                new Vector2(x + width - 20, horizon - height + 36),
                new Color(45, 34, 52),
                4);
        }

        // Deep shafts and suspended fragments sell scale without affecting play.
        for (int index = first; index <= last + 2; index++)
        {
            int x = parallax + index * 270 + 90;
            int y = map.WorldBounds.Top + 145 + Math.Abs(index * 53 % 190);
            int pulse = 10 + (int)(5f * MathF.Sin(time * .7f + index));
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, y, 90, 24),
                new Color(50 + pulse, 35, 69 + pulse, 135));
        }
    }

    private void DrawZoneArchitecture(
        DemonKingSanctumZoneDefinition zone,
        float time)
    {
        switch (zone.Kind)
        {
            case DemonKingSanctumZoneKind.SanctumApproach:
                DrawWallBand(zone, 420, new Color(35, 31, 39));
                DrawDistantCityRemnants(zone);
                break;
            case DemonKingSanctumZoneKind.HallOfBrokenVows:
                DrawWallBand(zone, 500, new Color(39, 35, 44));
                DrawReliefBand(zone, zone.GroundY - 390, OldBronze);
                break;
            case DemonKingSanctumZoneKind.PilgrimsDescent:
                DrawWallBand(zone, 520, new Color(31, 29, 39));
                DrawShaftDepth(zone);
                break;
            case DemonKingSanctumZoneKind.AltarOfSacrifice:
                DrawWallBand(zone, 510, new Color(39, 29, 36));
                DrawRitualFloor(zone, time);
                break;
            case DemonKingSanctumZoneKind.HallOfAncientKings:
                DrawWallBand(zone, 530, new Color(39, 36, 45));
                DrawReliefBand(zone, zone.GroundY - 470, RoyalGold);
                break;
            case DemonKingSanctumZoneKind.AbyssalCloister:
                DrawCloisterDepth(zone, time);
                break;
            case DemonKingSanctumZoneKind.ThroneAscent:
                DrawWallBand(zone, 520, new Color(31, 29, 39));
                DrawThronePromise(zone);
                break;
            case DemonKingSanctumZoneKind.RoyalAntechamber:
                DrawWallBand(zone, 520, new Color(34, 32, 39));
                DrawReliefBand(zone, zone.GroundY - 445, OldBronze);
                break;
            case DemonKingSanctumZoneKind.ThroneSanctum:
                DrawThroneChamber(zone, time);
                break;
            case DemonKingSanctumZoneKind.FinalEndpoint:
                DrawWallBand(zone, 500, new Color(27, 25, 34));
                DrawLine(
                    new Vector2(zone.Bounds.Left + 90, zone.GroundY - 410),
                    new Vector2(zone.Bounds.Right - 90, zone.GroundY - 410),
                    AbyssViolet,
                    5);
                break;
        }
    }

    private void DrawDistantCityRemnants(DemonKingSanctumZoneDefinition zone)
    {
        for (int i = 0; i < 4; i++)
        {
            int x = zone.Bounds.Left + 60 + i * 350;
            int height = 190 + i * 42;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, zone.GroundY - height, 190, height),
                new Color(28, 25, 32));
            DrawLine(
                new Vector2(x, zone.GroundY - height),
                new Vector2(x + 190, zone.GroundY - height + 65),
                StoneEdge,
                6);
        }
    }

    private void DrawShaftDepth(DemonKingSanctumZoneDefinition zone)
    {
        var shaft = new Rectangle(
            zone.Bounds.Left + 320,
            zone.Bounds.Top + 80,
            zone.Bounds.Width - 640,
            zone.Bounds.Height - 160);
        _spriteBatch.Draw(_pixel, shaft, new Color(8, 7, 14));
        DrawRectangleOutline(shaft, 14, StoneEdge);
        for (int y = shaft.Top + 60; y < shaft.Bottom; y += 95)
            DrawLine(new Vector2(shaft.Left + 25, y),
                new Vector2(shaft.Right - 25, y + 38), DeepStone, 7);
    }

    private void DrawRitualFloor(
        DemonKingSanctumZoneDefinition zone,
        float time)
    {
        int pulse = 25 + (int)(8f * MathF.Sin(time * 1.4f));
        Color glow = new(
            RitualRed.R + pulse / 4,
            RitualRed.G,
            RitualRed.B,
            150);
        int center = zone.Bounds.Center.X;
        DrawLine(new Vector2(zone.Bounds.Left + 160, zone.GroundY - 18),
            new Vector2(center, zone.GroundY - 70), glow, 7);
        DrawLine(new Vector2(center, zone.GroundY - 70),
            new Vector2(zone.Bounds.Right - 160, zone.GroundY - 18), glow, 7);
    }

    private void DrawCloisterDepth(
        DemonKingSanctumZoneDefinition zone,
        float time)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, zone.Bounds.Top,
                zone.Bounds.Width, zone.Bounds.Height),
            new Color(10, 8, 18));
        for (int i = 0; i < 6; i++)
        {
            int x = zone.Bounds.Left + 120 + i * 290;
            int y = zone.Bounds.Top + 135 + Math.Abs(i * 61 % 180);
            int lift = (int)(MathF.Sin(time * .65f + i) * 8f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, y + lift, 170, 34),
                new Color(47, 41, 61));
            DrawRectangleOutline(
                new Rectangle(x, y + lift, 170, 34),
                4,
                new Color(84, 58, 105));
        }
    }

    private void DrawThronePromise(DemonKingSanctumZoneDefinition zone)
    {
        int center = zone.Bounds.Right + 1050;
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - 310, zone.GroundY - 490, 620, 490),
            new Color(12, 10, 18));
        DrawLine(new Vector2(center - 310, zone.GroundY - 490),
            new Vector2(center, zone.GroundY - 560), FarStone, 28);
        DrawLine(new Vector2(center, zone.GroundY - 560),
            new Vector2(center + 310, zone.GroundY - 490), FarStone, 28);
    }

    private void DrawThroneChamber(
        DemonKingSanctumZoneDefinition zone,
        float time)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, zone.Bounds.Top,
                zone.Bounds.Width, zone.Bounds.Height),
            new Color(18, 15, 24));
        var voidWindow = new Rectangle(
            zone.Bounds.Left + 270,
            zone.Bounds.Top + 42,
            zone.Bounds.Width - 540,
            zone.Bounds.Height - 125);
        _spriteBatch.Draw(_pixel, voidWindow, new Color(8, 6, 16));
        DrawRectangleOutline(voidWindow, 18, new Color(62, 51, 70));
        int pulse = 24 + (int)(8f * MathF.Sin(time * .8f));
        DrawLine(new Vector2(voidWindow.Center.X, voidWindow.Top + 28),
            new Vector2(voidWindow.Center.X, voidWindow.Bottom - 30),
            new Color(70 + pulse, 45, 102 + pulse, 120), 5);
    }

    private void DrawReliefBand(
        DemonKingSanctumZoneDefinition zone,
        int y,
        Color accent)
    {
        DrawLine(new Vector2(zone.Bounds.Left + 70, y),
            new Vector2(zone.Bounds.Right - 70, y), accent, 5);
        for (int x = zone.Bounds.Left + 140; x < zone.Bounds.Right - 90; x += 250)
        {
            DrawLine(new Vector2(x, y - 30),
                new Vector2(x + 55, y), StoneEdge, 5);
            DrawLine(new Vector2(x + 55, y),
                new Vector2(x + 110, y - 30), StoneEdge, 5);
        }
    }

    private void DrawWallBand(
        DemonKingSanctumZoneDefinition zone,
        int height,
        Color color)
    {
        DrawMasonry(
            new Rectangle(zone.Bounds.Left, zone.GroundY - height,
                zone.Bounds.Width, height),
            color,
            72);
    }

    private void DrawProps(
        DungeonMap map,
        Rectangle camera,
        SanctumLayer layer,
        float time)
    {
        foreach (SanctumPropDefinition prop in map.SanctumProps)
        {
            if (prop.Layer != layer || !prop.Bounds.Intersects(camera))
                continue;
            DrawProp(prop, time);
        }
    }

    private void DrawProp(SanctumPropDefinition prop, float time)
    {
        Rectangle b = prop.Bounds;
        switch (prop.Kind)
        {
            case SanctumPropKind.BuriedWall:
                DrawMasonry(b, DeepStone, 64);
                for (int x = b.Left + 80; x < b.Right; x += 190)
                    DrawLine(new Vector2(x, b.Top + 25),
                        new Vector2(x - 45, b.Bottom), StoneEdge, 5);
                break;
            case SanctumPropKind.SealedArch:
            case SanctumPropKind.RoyalGate:
                DrawArch(b, prop.Kind == SanctumPropKind.RoyalGate);
                break;
            case SanctumPropKind.MonumentalColumn:
                DrawColumn(b);
                break;
            case SanctumPropKind.OathTablet:
                for (int x = b.Left; x < b.Right; x += 190)
                {
                    var tablet = new Rectangle(x, b.Top + Math.Abs(x % 21),
                        145, b.Height - 22);
                    DrawMasonry(tablet, new Color(50, 45, 52), 42);
                    DrawLine(new Vector2(tablet.Left + 25, tablet.Top + 38),
                        new Vector2(tablet.Right - 25, tablet.Top + 62),
                        OldBronze, 4);
                }
                break;
            case SanctumPropKind.BrokenBanner:
                for (int x = b.Left; x < b.Right; x += 250)
                {
                    DrawLine(new Vector2(x, b.Top),
                        new Vector2(x, b.Bottom), OldBronze, 5);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x + 5, b.Top + 25, 115, b.Height - 80),
                        new Color(91, 28, 42, 175));
                }
                break;
            case SanctumPropKind.RoyalStatue:
                DrawRoyalStatues(b);
                break;
            case SanctumPropKind.Terrace:
            case SanctumPropKind.NarrowBridge:
            case SanctumPropKind.BrokenBridge:
                _spriteBatch.Draw(_pixel, b,
                    prop.Kind == SanctumPropKind.NarrowBridge
                        ? new Color(48, 43, 56)
                        : TempleStone);
                DrawRectangleOutline(b, 7, StoneEdge);
                break;
            case SanctumPropKind.RitualAltar:
                DrawRitualAltar(b);
                break;
            case SanctumPropKind.RitualChannel:
                _spriteBatch.Draw(_pixel, b, new Color(54, 38, 48));
                DrawLine(new Vector2(b.Left, b.Center.Y),
                    new Vector2(b.Right, b.Center.Y), RitualRed, 5);
                break;
            case SanctumPropKind.SuspendedChain:
                DrawChains(b);
                break;
            case SanctumPropKind.AncientVessel:
                for (int x = b.Left; x < b.Right; x += 240)
                {
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, b.Bottom - 78, 82, 78), DeepStone);
                    DrawRectangleOutline(
                        new Rectangle(x, b.Bottom - 78, 82, 78),
                        5,
                        OldBronze);
                }
                break;
            case SanctumPropKind.KingAlcove:
                _spriteBatch.Draw(_pixel, b, new Color(20, 18, 26));
                DrawRectangleOutline(b, 12, StoneEdge);
                break;
            case SanctumPropKind.BrokenCrown:
                for (int x = b.Left; x < b.Right; x += 360)
                {
                    DrawLine(new Vector2(x, b.Bottom),
                        new Vector2(x + 45, b.Top), OldBronze, 9);
                    DrawLine(new Vector2(x + 45, b.Top),
                        new Vector2(x + 90, b.Bottom), OldBronze, 9);
                }
                break;
            case SanctumPropKind.VoidWindow:
                _spriteBatch.Draw(_pixel, b, new Color(8, 7, 16));
                DrawRectangleOutline(b, 13, new Color(66, 50, 82));
                break;
            case SanctumPropKind.FloatingStone:
                DrawFloatingStones(b, time);
                break;
            case SanctumPropKind.ThroneSilhouette:
                DrawThrone(b, silhouette: true);
                break;
            case SanctumPropKind.Throne:
                DrawThrone(b, silhouette: false);
                break;
            case SanctumPropKind.SacredFragment:
                DrawSacredFragments(b, time,
                    prop.Layer == SanctumLayer.Foreground);
                break;
            case SanctumPropKind.Brazier:
                DrawBrazier(b, time);
                break;
        }
    }

    private void DrawArch(Rectangle b, bool royal)
    {
        _spriteBatch.Draw(_pixel, b, new Color(12, 10, 18));
        DrawRectangleOutline(b, royal ? 16 : 12,
            royal ? OldBronze : StoneEdge);
        DrawLine(new Vector2(b.Left, b.Top + 90),
            new Vector2(b.Center.X, b.Top), TempleStone, 22);
        DrawLine(new Vector2(b.Center.X, b.Top),
            new Vector2(b.Right, b.Top + 90), TempleStone, 22);
        if (royal)
            DrawLine(new Vector2(b.Center.X, b.Top + 35),
                new Vector2(b.Center.X, b.Bottom - 35), RoyalGold, 5);
    }

    private void DrawColumn(Rectangle b)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left + b.Width / 6, b.Top,
                b.Width * 2 / 3, b.Height), TempleStone);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Top, b.Width, 25), StoneEdge);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left - 8, b.Bottom - 30, b.Width + 16, 30),
            StoneEdge);
        DrawLine(new Vector2(b.Left + b.Width / 3, b.Top + 30),
            new Vector2(b.Left + b.Width / 3, b.Bottom - 30),
            OldBronze, 4);
    }

    private void DrawRoyalStatues(Rectangle b)
    {
        int count = Math.Max(1, b.Width / 280);
        int spacing = b.Width / count;
        for (int i = 0; i < count; i++)
        {
            int center = b.Left + spacing * i + spacing / 2;
            int variance = (i % 3) * 12;
            _spriteBatch.Draw(_pixel,
                new Rectangle(center - 48, b.Bottom - 45, 96, 45),
                StoneEdge);
            DrawLine(new Vector2(center, b.Bottom - 42),
                new Vector2(center + (i % 2 == 0 ? -10 : 10),
                    b.Top + 78 + variance),
                TempleStone, 62);
            _spriteBatch.Draw(_pixel,
                new Rectangle(center - 30, b.Top + 38 + variance, 60, 56),
                TempleStone);
            DrawLine(new Vector2(center - 24, b.Top + 48 + variance),
                new Vector2(center - 62, b.Top + 8), StoneEdge, 9);
            DrawLine(new Vector2(center + 24, b.Top + 48 + variance),
                new Vector2(center + 62, b.Top + 18 + variance), StoneEdge, 9);
        }
    }

    private void DrawRitualAltar(Rectangle b)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Bottom - 55, b.Width, 55), DeepStone);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left + 90, b.Bottom - 120, b.Width - 180, 65),
            TempleStone);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left + 220, b.Top, b.Width - 440,
                b.Height - 120), DeepStone);
        DrawRectangleOutline(b, 7, OldBronze);
    }

    private void DrawChains(Rectangle b)
    {
        for (int x = b.Left + 15; x < b.Right; x += 34)
            for (int y = b.Top; y < b.Bottom; y += 26)
                DrawRectangleOutline(new Rectangle(x - 8, y, 16, 28),
                    3, (y / 26 & 1) == 0 ? OldBronze : StoneEdge);
    }

    private void DrawFloatingStones(Rectangle b, float time)
    {
        for (int i = 0; i < 12; i++)
        {
            int x = b.Left + 35 + i * Math.Max(40, b.Width / 12);
            int baseY = b.Top + 30 + Math.Abs(i * 73 % Math.Max(60, b.Height - 70));
            int y = baseY + (int)(MathF.Sin(time * .75f + i) * 7f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, y, 42 + i % 3 * 18, 18 + i % 2 * 12),
                new Color(55, 47, 66));
        }
    }

    private void DrawThrone(Rectangle b, bool silhouette)
    {
        Color stone = silhouette ? new Color(13, 11, 19) : DeepStone;
        Color edge = silhouette ? FarStone : RoyalGold;
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left + b.Width / 5, b.Top + 90,
                b.Width * 3 / 5, b.Height - 90), stone);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Bottom - 95, b.Width, 95), stone);
        DrawLine(new Vector2(b.Left + b.Width / 5, b.Top + 90),
            new Vector2(b.Center.X, b.Top), edge, 24);
        DrawLine(new Vector2(b.Center.X, b.Top),
            new Vector2(b.Right - b.Width / 5, b.Top + 90), edge, 24);
        if (!silhouette)
        {
            DrawRectangleOutline(
                new Rectangle(b.Left + b.Width / 5, b.Top + 90,
                    b.Width * 3 / 5, b.Height - 90),
                10,
                StoneEdge);
            DrawLine(new Vector2(b.Center.X, b.Top + 80),
                new Vector2(b.Center.X, b.Bottom - 105),
                new Color(112, 70, 121), 6);
        }
    }

    private void DrawSacredFragments(
        Rectangle b,
        float time,
        bool foreground)
    {
        Color color = foreground
            ? new Color(78, 62, 83, 115)
            : new Color(65, 53, 75, 175);
        for (int i = 0; i < 10; i++)
        {
            int x = b.Left + 20 + i * Math.Max(38, b.Width / 10);
            int baseY = b.Top + 20 + Math.Abs(i * 67 % Math.Max(50, b.Height - 70));
            int y = baseY + (int)(MathF.Sin(time * .55f + i) * 9f);
            DrawLine(new Vector2(x, y),
                new Vector2(x + 28 + i % 4 * 8, y + 16),
                color, foreground ? 5 : 8);
        }
    }

    private void DrawBrazier(Rectangle b, float time)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Center.X - 20, b.Bottom - 65, 40, 65),
            OldBronze);
        int flame = 26 + (int)(MathF.Sin(time * 4f + b.Left) * 7f);
        DrawLine(new Vector2(b.Center.X, b.Bottom - 62),
            new Vector2(b.Center.X + 4, b.Top + 12), Ember, flame);
    }

    private void DrawCheckpoints(DungeonMap map, Rectangle camera, float time)
    {
        foreach (SanctumCheckpointDefinition checkpoint in map.SanctumCheckpoints)
        {
            var bounds = new Rectangle(
                (int)checkpoint.Position.X - 28,
                (int)checkpoint.Position.Y - 112,
                56,
                114);
            if (!bounds.Intersects(camera))
                continue;
            DrawLine(new Vector2(bounds.Center.X, bounds.Bottom),
                new Vector2(bounds.Center.X, bounds.Top + 24),
                OldBronze, 8);
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X + 5, bounds.Top + 28, 36, 46),
                new Color(55, 31, 68));
            int glow = 10 + (int)(3f * MathF.Sin(time * 2.6f));
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X - glow / 2, bounds.Top,
                    glow, glow),
                new Color(147, 91, 177));
        }
    }

    private void DrawPlatform(Platform platform)
    {
        Color stone = platform.Kind == PlatformKind.Raised
            ? new Color(57, 50, 62)
            : TempleStone;
        _spriteBatch.Draw(_pixel, platform.Bounds, stone);
        _spriteBatch.Draw(_pixel,
            new Rectangle(platform.Bounds.Left, platform.Bounds.Top,
                platform.Bounds.Width, Math.Min(9, platform.Bounds.Height)),
            StoneEdge);
        for (int x = platform.Bounds.Left + 55;
            x < platform.Bounds.Right; x += 125)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, platform.Bounds.Top + 20, 4,
                    Math.Min(45, Math.Max(0, platform.Bounds.Height - 20))),
                new Color(29, 26, 35));
        }
    }

    private void DrawMasonry(
        Rectangle bounds,
        Color color,
        int rowSpacing)
    {
        _spriteBatch.Draw(_pixel, bounds, color);
        for (int y = bounds.Top + rowSpacing;
            y < bounds.Bottom; y += rowSpacing)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Left, y, bounds.Width, 3),
                new Color(24, 22, 29));
        }
        for (int y = bounds.Top + 10;
            y < bounds.Bottom; y += rowSpacing)
        {
            int offset = (y / rowSpacing & 1) == 0 ? 70 : 145;
            for (int x = bounds.Left + offset; x < bounds.Right; x += 210)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, y, 3,
                        Math.Min(rowSpacing - 8, bounds.Bottom - y)),
                    new Color(31, 28, 36));
        }
    }

    private void DrawRectangleOutline(Rectangle rectangle, int thickness, Color color)
    {
        thickness = Math.Max(1, thickness);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Top,
                rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Bottom - thickness,
                rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Top,
                thickness, rectangle.Height), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Top,
                thickness, rectangle.Height), color);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int width)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= .01f)
            return;
        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            new Vector2(0f, .5f),
            new Vector2(length, Math.Max(1, width)),
            SpriteEffects.None,
            0f);
    }
}
