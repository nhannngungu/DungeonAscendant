using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Code-only, collision-free Map 6 demonic city presentation.</summary>
public sealed class DemonCityEnvironmentRenderer
{
    private static readonly Color SkyTop = new(7, 6, 12);
    private static readonly Color SkyBottom = new(38, 15, 24);
    private static readonly Color FarSilhouette = new(18, 15, 24);
    private static readonly Color FarGlow = new(80, 22, 35);
    private static readonly Color DeepStone = new(29, 27, 35);
    private static readonly Color CityStone = new(45, 40, 48);
    private static readonly Color StoneEdge = new(78, 66, 73);
    private static readonly Color Iron = new(70, 65, 72);
    private static readonly Color Steel = new(111, 99, 103);
    private static readonly Color MutedBronze = new(104, 76, 52);
    private static readonly Color OldGold = new(133, 103, 55);
    private static readonly Color Crimson = new(112, 28, 42);
    private static readonly Color Window = new(183, 58, 37);
    private static readonly Color Ember = new(224, 79, 34);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private bool _exitUnlocked;

    public DemonCityEnvironmentRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawWorld(GameSession session, Rectangle camera, GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsDemonCity != true)
            return;
        _exitUnlocked = session.IsExitUnlocked;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        DrawSky(camera);
        DrawFarCity(map, camera, time);
        foreach (DemonCityZoneDefinition zone in map.DemonCityZones)
            if (zone.Bounds.Intersects(camera))
                DrawZoneArchitecture(zone, time);
        DrawProps(map, camera, DemonCityLayer.Background, time);
        DrawProps(map, camera, DemonCityLayer.Midground, time);

        foreach (Rectangle corridor in map.Corridors)
            if (corridor.Intersects(camera))
                DrawMasonry(corridor, new Color(40, 35, 42));
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
        if (session.CurrentDungeon?.IsDemonCity != true)
            return;
        _exitUnlocked = session.IsExitUnlocked;
        DrawProps(session.CurrentDungeon, camera, DemonCityLayer.Foreground,
            (float)gameTime.TotalGameTime.TotalSeconds);
    }

    private void DrawSky(Rectangle camera)
    {
        int bandHeight = Math.Max(1, camera.Height / 8);
        for (int band = 0; band < 8; band++)
        {
            Color color = Color.Lerp(SkyTop, SkyBottom, band / 7f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(camera.Left, camera.Top + band * bandHeight,
                    camera.Width,
                    band == 7 ? camera.Height - band * bandHeight : bandHeight),
                color);
        }
    }

    private void DrawFarCity(DungeonMap map, Rectangle camera, float time)
    {
        int horizon = map.WorldBounds.Top + 485;
        int first = (int)MathF.Floor(camera.Left * .13f / 230f) - 3;
        int last = first + camera.Width / 230 + 9;
        for (int index = first; index <= last; index++)
        {
            int x = index * 230 + (int)(camera.Left * .87f);
            int height = 180 + Math.Abs(index * 83 % 250);
            int width = 145 + Math.Abs(index * 31 % 75);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, horizon - height, width, height),
                FarSilhouette);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + width / 2 - 10, horizon - height - 72,
                    20, 74), FarSilhouette);
            for (int row = 0; row < 3; row++)
            {
                int glow = 42 + (int)(8f * MathF.Sin(time * 1.8f + index + row));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x + 24 + row * 34, horizon - height + 52,
                        11, 24),
                    new Color(FarGlow.R + glow / 5, FarGlow.G, FarGlow.B));
            }
        }

        // A fixed monumental core anchors the city beyond the playable route.
        int coreX = map.WorldBounds.Left + map.WorldBounds.Width * 3 / 4;
        _spriteBatch.Draw(_pixel,
            new Rectangle(coreX - 260, map.WorldBounds.Top + 105, 520, 390),
            new Color(21, 17, 27));
        _spriteBatch.Draw(_pixel,
            new Rectangle(coreX - 52, map.WorldBounds.Top + 35, 104, 465),
            new Color(17, 14, 23));
    }

    private void DrawZoneArchitecture(DemonCityZoneDefinition zone, float time)
    {
        switch (zone.Kind)
        {
            case DemonCityZoneKind.OuterApproach:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 135,
                    zone.Bounds.Width, 135, new Color(36, 31, 38));
                DrawLine(new Vector2(zone.Bounds.Left + 210, zone.GroundY - 12),
                    new Vector2(zone.Bounds.Left + 390, zone.GroundY - 88),
                    new Color(24, 21, 27), 8);
                DrawLine(new Vector2(zone.Bounds.Left + 980, zone.GroundY - 8),
                    new Vector2(zone.Bounds.Left + 1110, zone.GroundY - 70),
                    new Color(24, 21, 27), 7);
                break;
            case DemonCityZoneKind.CityEntrance:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 460,
                    zone.Bounds.Width, 460, new Color(40, 35, 43));
                DrawArch(zone.Bounds.Center.X, zone.GroundY, 680, 430);
                break;
            case DemonCityZoneKind.LowerDistrict:
                DrawStreetFacades(zone, 6, 330, new Color(42, 37, 45), time);
                break;
            case DemonCityZoneKind.MarketDepths:
                DrawStreetFacades(zone, 5, 360, new Color(45, 35, 42), time);
                DrawCanopyBand(zone.Bounds.Left + 180, zone.GroundY - 175,
                    zone.Bounds.Width - 360);
                break;
            case DemonCityZoneKind.IndustrialRitualDistrict:
                DrawIndustrialDistrict(zone, time);
                break;
            case DemonCityZoneKind.HighCity:
                DrawStreetFacades(zone, 4, 430, new Color(39, 36, 46), time);
                DrawBridgeShadow(zone.Bounds.Left + 170,
                    zone.GroundY - 255, zone.Bounds.Width - 340);
                break;
            case DemonCityZoneKind.EliteDistrict:
                DrawStreetFacades(zone, 4, 470, new Color(38, 36, 44), time);
                DrawLine(new Vector2(zone.Bounds.Left + 270, zone.GroundY - 420),
                    new Vector2(zone.Bounds.Right - 270, zone.GroundY - 420),
                    OldGold, 5);
                break;
            case DemonCityZoneKind.PalaceApproach:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 520,
                    zone.Bounds.Width, 520, new Color(42, 37, 46));
                DrawArch(zone.Bounds.Center.X, zone.GroundY, 880, 500);
                DrawLine(new Vector2(zone.Bounds.Left + 190, zone.GroundY - 475),
                    new Vector2(zone.Bounds.Right - 190, zone.GroundY - 475),
                    MutedBronze, 6);
                break;
            case DemonCityZoneKind.MainBossArena:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 540,
                    zone.Bounds.Width, 540, new Color(38, 34, 43));
                DrawArenaRecess(zone);
                break;
            case DemonCityZoneKind.MapSevenExit:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 540,
                    zone.Bounds.Width, 540, new Color(35, 32, 41));
                break;
        }
    }

    private void DrawStreetFacades(
        DemonCityZoneDefinition zone,
        int bays,
        int height,
        Color color,
        float time)
    {
        DrawWallBand(zone.Bounds.Left, zone.GroundY - height,
            zone.Bounds.Width, height, color);
        int bayWidth = zone.Bounds.Width / bays;
        for (int bay = 0; bay < bays; bay++)
        {
            int x = zone.Bounds.Left + bay * bayWidth + 36;
            var facade = new Rectangle(x, zone.GroundY - height + 45,
                bayWidth - 72, height - 90);
            DrawRectangleOutline(facade, 7, StoneEdge);
            for (int row = 0; row < 2; row++)
            {
                int pulse = 10 + (int)(5f * MathF.Sin(time * 2f + bay + row));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(facade.Left + 30,
                        facade.Top + 38 + row * 86,
                        Math.Max(12, facade.Width - 60), 30),
                    new Color(Window.R, Window.G + pulse, Window.B));
            }
        }
    }

    private void DrawCanopyBand(int x, int y, int width)
    {
        DrawLine(new Vector2(x, y), new Vector2(x + width, y - 18), Iron, 5);
        for (int px = x + 70; px < x + width; px += 220)
            _spriteBatch.Draw(_pixel,
                new Rectangle(px, y - 10, 145, 46), new Color(70, 27, 40));
    }

    private void DrawIndustrialDistrict(
        DemonCityZoneDefinition zone,
        float time)
    {
        DrawWallBand(zone.Bounds.Left, zone.GroundY - 475,
            zone.Bounds.Width, 475, new Color(35, 31, 39));
        for (int x = zone.Bounds.Left + 150;
            x < zone.Bounds.Right - 120; x += 340)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, zone.GroundY - 430, 94, 360),
                new Color(27, 25, 32));
            DrawRectangleOutline(
                new Rectangle(x, zone.GroundY - 430, 94, 360),
                6, Iron);
            int glow = 16 + (int)(6f * MathF.Sin(time * 2.4f + x));
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + 20, zone.GroundY - 128, 54, 42),
                new Color(150 + glow, 45 + glow / 2, 28));
        }
    }

    private void DrawBridgeShadow(int x, int y, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 66), DeepStone);
        DrawRectangleOutline(new Rectangle(x, y, width, 66), 6, StoneEdge);
        for (int px = x + 55; px < x + width; px += 115)
            DrawLine(new Vector2(px, y + 66), new Vector2(px + 35, y + 116),
                Iron, 6);
    }

    private void DrawArenaRecess(DemonCityZoneDefinition zone)
    {
        var recess = new Rectangle(zone.Bounds.Left + 310,
            zone.GroundY - 455, zone.Bounds.Width - 620, 385);
        _spriteBatch.Draw(_pixel, recess, new Color(17, 14, 22));
        DrawRectangleOutline(recess, 14, StoneEdge);
        DrawLine(new Vector2(recess.Center.X, recess.Top + 35),
            new Vector2(recess.Center.X, recess.Bottom - 35), Iron, 8);
    }

    private void DrawProps(
        DungeonMap map,
        Rectangle camera,
        DemonCityLayer layer,
        float time)
    {
        foreach (DemonCityPropDefinition prop in map.DemonCityProps)
        {
            if (prop.Layer != layer || !prop.Bounds.Intersects(camera))
                continue;
            DrawProp(prop, time);
        }
    }

    private void DrawProp(DemonCityPropDefinition prop, float time)
    {
        Rectangle b = prop.Bounds;
        switch (prop.Kind)
        {
            case DemonCityPropKind.Tower:
            case DemonCityPropKind.BuildingFacade:
                DrawMasonry(b, DeepStone);
                for (int y = b.Top + 55; y < b.Bottom - 30; y += 88)
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(b.Left + 28, y, Math.Max(12, b.Width - 56), 22),
                        new Color(93, 27, 35));
                break;
            case DemonCityPropKind.Roofline:
                _spriteBatch.Draw(_pixel, b, DeepStone);
                for (int x = b.Left; x < b.Right; x += 90)
                    DrawLine(new Vector2(x, b.Bottom),
                        new Vector2(x + 45, b.Top), StoneEdge, 6);
                break;
            case DemonCityPropKind.Arch:
                DrawArch(b.Center.X, b.Bottom, b.Width, b.Height);
                break;
            case DemonCityPropKind.Bridge:
                DrawBridgeShadow(b.Left, b.Top, b.Width);
                break;
            case DemonCityPropKind.Balcony:
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Left, b.Top, b.Width, 18), CityStone);
                for (int x = b.Left + 15; x < b.Right; x += 38)
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, b.Top - 50, 5, 52), Iron);
                break;
            case DemonCityPropKind.WindowStack:
                DrawMasonry(b, DeepStone);
                for (int y = b.Top + 34; y < b.Bottom - 30; y += 72)
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(b.Left + 38, y, b.Width - 76, 25),
                        Color.Lerp(Window, Ember,
                            .25f + MathF.Sin(time * 2.3f + y) * .08f));
                break;
            case DemonCityPropKind.MarketStall:
                DrawLine(new Vector2(b.Left + 12, b.Bottom),
                    new Vector2(b.Left + 12, b.Top), Iron, 7);
                DrawLine(new Vector2(b.Right - 12, b.Bottom),
                    new Vector2(b.Right - 12, b.Top), Iron, 7);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Left, b.Top, b.Width, 24),
                    new Color(80, 30, 42));
                break;
            case DemonCityPropKind.Furnace:
                DrawMasonry(b, new Color(34, 31, 37));
                var furnaceMouth = new Rectangle(
                    b.Left + b.Width / 5,
                    b.Bottom - b.Height / 3,
                    b.Width * 3 / 5,
                    b.Height / 4);
                _spriteBatch.Draw(_pixel, furnaceMouth,
                    Color.Lerp(new Color(116, 29, 25), Ember,
                        .40f + MathF.Sin(time * 3.1f + b.Left) * .08f));
                DrawRectangleOutline(furnaceMouth, 6, Iron);
                break;
            case DemonCityPropKind.Pipe:
                for (int x = b.Left + 24; x < b.Right; x += 62)
                {
                    DrawLine(new Vector2(x, b.Bottom),
                        new Vector2(x, b.Top + 35), Iron, 13);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x - 10, b.Top + 20, 48, 18), Steel);
                }
                DrawLine(new Vector2(b.Left + 24, b.Top + 35),
                    new Vector2(b.Right - 18, b.Top + 35), Iron, 13);
                break;
            case DemonCityPropKind.Chain:
                for (int y = b.Top; y < b.Bottom; y += 24)
                {
                    DrawRectangleOutline(
                        new Rectangle(b.Center.X - 9, y, 18, 26),
                        4, y / 24 % 2 == 0 ? Steel : Iron);
                }
                break;
            case DemonCityPropKind.RitualCircle:
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Left, b.Center.Y - 3, b.Width, 6), Crimson);
                DrawLine(new Vector2(b.Left + 30, b.Bottom),
                    new Vector2(b.Center.X, b.Top), Crimson, 5);
                DrawLine(new Vector2(b.Center.X, b.Top),
                    new Vector2(b.Right - 30, b.Bottom), Crimson, 5);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X - 12, b.Center.Y - 12, 24, 24),
                    new Color(165, 42, 49));
                break;
            case DemonCityPropKind.Statue:
                DrawStatue(b);
                break;
            case DemonCityPropKind.Pillar:
                DrawMasonry(b, CityStone);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Left - 10, b.Top, b.Width + 20, 18), StoneEdge);
                break;
            case DemonCityPropKind.StreetLamp:
                DrawLine(new Vector2(b.Center.X, b.Bottom),
                    new Vector2(b.Center.X, b.Top + 24), Iron, 6);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X - 9, b.Top, 18, 25), Ember);
                break;
            case DemonCityPropKind.Banner:
                DrawLine(new Vector2(b.Center.X, b.Bottom),
                    new Vector2(b.Center.X, b.Top), Iron, 5);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X + 3, b.Top + 12,
                        b.Width / 2, b.Height * 2 / 3), Crimson);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X + 3, b.Top + 12,
                        b.Width / 2, 6), OldGold);
                break;
            case DemonCityPropKind.Rail:
                DrawLine(new Vector2(b.Left, b.Top),
                    new Vector2(b.Right, b.Top), Steel, 7);
                for (int x = b.Left + 8; x < b.Right; x += 42)
                    DrawLine(new Vector2(x, b.Bottom),
                        new Vector2(x, b.Top), Iron, 5);
                break;
            case DemonCityPropKind.Debris:
                for (int x = b.Left; x < b.Right; x += 34)
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, b.Bottom - 10 - Math.Abs(x % 29),
                            25, 12 + Math.Abs(x % 29)), CityStone);
                break;
            case DemonCityPropKind.SealedGate:
                DrawSealedGate(b, time);
                break;
        }
    }

    private void DrawStatue(Rectangle b)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Bottom - 28, b.Width, 28), StoneEdge);
        DrawLine(new Vector2(b.Center.X, b.Bottom - 26),
            new Vector2(b.Center.X, b.Top + 72), CityStone, b.Width / 2);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Center.X - b.Width / 5, b.Top + 35,
                b.Width * 2 / 5, 48), CityStone);
        DrawLine(new Vector2(b.Center.X - 18, b.Top + 35),
            new Vector2(b.Center.X - 42, b.Top), StoneEdge, 9);
        DrawLine(new Vector2(b.Center.X + 18, b.Top + 35),
            new Vector2(b.Center.X + 42, b.Top), StoneEdge, 9);
    }

    private void DrawSealedGate(Rectangle b, float time)
    {
        _spriteBatch.Draw(_pixel, b,
            _exitUnlocked ? new Color(10, 10, 17) : new Color(20, 18, 26));
        DrawRectangleOutline(b, 13,
            _exitUnlocked ? OldGold : StoneEdge);
        if (_exitUnlocked)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Left + 20, b.Top + 18, 18, b.Height - 18),
                Iron);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Right - 38, b.Top + 18, 18, b.Height - 18),
                Iron);
            DrawLine(new Vector2(b.Left + 37, b.Center.Y),
                new Vector2(b.Center.X - 34, b.Top + 32), MutedBronze, 5);
            DrawLine(new Vector2(b.Right - 37, b.Center.Y),
                new Vector2(b.Center.X + 34, b.Top + 32), MutedBronze, 5);
            return;
        }
        for (int x = b.Left + 30; x < b.Right; x += 48)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, b.Top + 22, 10, b.Height - 22), Iron);
        int pulse = 42 + (int)(14f * MathF.Sin(time * 2.2f));
        var seal = new Rectangle(b.Center.X - 34, b.Center.Y - 34, 68, 68);
        _spriteBatch.Draw(_pixel, seal,
            new Color(Crimson.R + pulse / 5, Crimson.G, Crimson.B));
        DrawRectangleOutline(seal, 5, Steel);
    }

    private void DrawCheckpoints(DungeonMap map, Rectangle camera, float time)
    {
        foreach (DemonCityCheckpointDefinition checkpoint in map.DemonCityCheckpoints)
        {
            var bounds = new Rectangle((int)checkpoint.Position.X - 24,
                (int)checkpoint.Position.Y - 104, 48, 106);
            if (!bounds.Intersects(camera))
                continue;
            DrawLine(new Vector2(bounds.Center.X, bounds.Bottom),
                new Vector2(bounds.Center.X, bounds.Top + 18), Iron, 7);
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X + 4, bounds.Top + 22, 34, 42),
                new Color(75, 25, 38));
            int glow = 9 + (int)(3f * MathF.Sin(time * 3f));
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Center.X - glow / 2, bounds.Top,
                    glow, glow), Ember);
        }
    }

    private void DrawPlatform(Platform platform)
    {
        Color color = platform.Kind == PlatformKind.Raised ? Iron : CityStone;
        _spriteBatch.Draw(_pixel, platform.Bounds, color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(platform.Bounds.Left, platform.Bounds.Top,
                platform.Bounds.Width, Math.Min(8, platform.Bounds.Height)),
            StoneEdge);
        for (int x = platform.Bounds.Left + 42;
             x < platform.Bounds.Right; x += 104)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, platform.Bounds.Top + 18, 3,
                    Math.Min(38, Math.Max(0, platform.Bounds.Height - 18))),
                new Color(31, 28, 35));
        }
    }

    private void DrawWallBand(int x, int y, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);
        for (int py = y + 48; py < y + height; py += 68)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, py, width, 3), new Color(25, 22, 29));
    }

    private void DrawArch(int centerX, int groundY, int width, int height)
    {
        var opening = new Rectangle(centerX - width / 2,
            groundY - height, width, height);
        _spriteBatch.Draw(_pixel, opening, new Color(15, 13, 20));
        DrawRectangleOutline(opening, 14, StoneEdge);
        DrawLine(new Vector2(opening.Left, opening.Top + 70),
            new Vector2(opening.Center.X, opening.Top), CityStone, 18);
        DrawLine(new Vector2(opening.Center.X, opening.Top),
            new Vector2(opening.Right, opening.Top + 70), CityStone, 18);
    }

    private void DrawMasonry(Rectangle bounds, Color color)
    {
        _spriteBatch.Draw(_pixel, bounds, color);
        DrawRectangleOutline(bounds, 5, StoneEdge);
        for (int y = bounds.Top + 46; y < bounds.Bottom; y += 58)
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Left, y, bounds.Width, 3),
                new Color(27, 24, 31));
    }

    private void DrawRectangleOutline(Rectangle b, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Top, b.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Bottom - thickness, b.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Left, b.Top, thickness, b.Height), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(b.Right - thickness, b.Top, thickness, b.Height), color);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }
}
