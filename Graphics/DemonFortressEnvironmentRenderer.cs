using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Code-only, collision-free Map 5 military fortress presentation.</summary>
public sealed class DemonFortressEnvironmentRenderer
{
    private static readonly Color SkyTop = new(8, 9, 13);
    private static readonly Color SkyBottom = new(31, 24, 25);
    private static readonly Color FarStone = new(24, 25, 30);
    private static readonly Color MidStone = new(37, 38, 43);
    private static readonly Color Stone = new(53, 53, 58);
    private static readonly Color StoneEdge = new(87, 81, 78);
    private static readonly Color Iron = new(73, 73, 77);
    private static readonly Color Steel = new(113, 110, 108);
    private static readonly Color Bronze = new(135, 83, 45);
    private static readonly Color Crimson = new(103, 29, 34);
    private static readonly Color Ember = new(225, 76, 28);
    private static readonly Color Lava = new(246, 112, 31);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public DemonFortressEnvironmentRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawWorld(GameSession session, Rectangle camera, GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsDemonFortress != true)
            return;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        DrawSky(camera);
        DrawFarFortress(map, camera);
        foreach (DemonFortressZoneDefinition zone in map.DemonFortressZones)
            if (zone.Bounds.Intersects(camera))
                DrawZoneArchitecture(zone);
        DrawProps(map, camera, FortressLayer.Background, time);
        DrawProps(map, camera, FortressLayer.Midground, time);

        foreach (Rectangle corridor in map.Corridors)
            if (corridor.Intersects(camera))
                DrawStone(corridor, new Color(47, 46, 50));
        foreach (Platform platform in map.Platforms)
            if (platform.Bounds.Intersects(camera))
                DrawPlatform(platform);

        DrawProps(map, camera, FortressLayer.Foreground, time);
        DrawBossArenaGates(session, camera, time);
        DrawCheckpoints(map, camera, time);
    }

    private void DrawBossArenaGates(
        GameSession session,
        Rectangle camera,
        float time)
    {
        string active = session.WildForestEncounters.ActiveZoneId;
        if (active is not ("fortress-ballista-gunner" or
            "fortress-black-guard"))
            return;

        EncounterZone arena = null;
        foreach (EncounterZone zone in session.CurrentDungeon.EncounterZones)
            if (zone.ZoneId == active)
            {
                arena = zone;
                break;
            }
        if (arena == null)
            return;

        int pulse = (int)MathF.Round(18f +
            MathF.Abs(MathF.Sin(time * 3.5f)) * 18f);
        DrawPortcullis(arena.ActivationBounds.Left + 10,
            arena.GroundBounds.Bottom, camera, pulse);
        DrawPortcullis(arena.ActivationBounds.Right - 10,
            arena.GroundBounds.Bottom, camera, pulse);
    }

    private void DrawPortcullis(int centerX, int groundY, Rectangle camera, int pulse)
    {
        var bounds = new Rectangle(centerX - 18, groundY - 150, 36, 150);
        if (!bounds.Intersects(camera))
            return;
        _spriteBatch.Draw(_pixel,
            new Rectangle(bounds.Left - 5, bounds.Top, bounds.Width + 10, 9),
            new Color(37, 34, 37));
        for (int x = bounds.Left; x <= bounds.Right; x += 9)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, bounds.Top + 5, 5, bounds.Height - 5), Iron);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, bounds.Bottom - 12, 5, 12),
                new Color(112 + pulse, 54, 40));
        }
        for (int y = bounds.Top + 34; y < bounds.Bottom - 15; y += 38)
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Left, y, bounds.Width, 5), Steel);
    }

    private void DrawSky(Rectangle camera)
    {
        int bandHeight = Math.Max(1, camera.Height / 7);
        for (int i = 0; i < 7; i++)
        {
            Color color = Color.Lerp(SkyTop, SkyBottom, i / 6f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(camera.Left, camera.Top + i * bandHeight,
                    camera.Width, i == 6 ? camera.Height - i * bandHeight : bandHeight),
                color);
        }
    }

    private void DrawFarFortress(DungeonMap map, Rectangle camera)
    {
        int baseY = map.WorldBounds.Top + 430;
        int first = (int)MathF.Floor(camera.Left * .17f / 280f) - 2;
        int last = first + camera.Width / 280 + 7;
        for (int i = first; i <= last; i++)
        {
            int x = i * 280 + (int)(camera.Left * .83f);
            int height = 160 + Math.Abs((i * 73) % 170);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, baseY - height, 220, height), FarStone);
            for (int tooth = 0; tooth < 5; tooth++)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x + tooth * 46, baseY - height - 20, 27, 22), FarStone);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + 36, baseY - height + 55, 18, 38),
                new Color(86, 28, 29));
        }
    }

    private void DrawZoneArchitecture(DemonFortressZoneDefinition zone)
    {
        switch (zone.Kind)
        {
            case DemonFortressZoneKind.OuterBattlefield:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 115, zone.Bounds.Width, 115,
                    new Color(39, 37, 40));
                break;
            case DemonFortressZoneKind.DemonGate:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 470, zone.Bounds.Width, 470,
                    new Color(44, 43, 47));
                DrawArch(zone.Bounds.Center.X, zone.GroundY, 410, 390);
                break;
            case DemonFortressZoneKind.Barracks:
                DrawInterior(zone, 5, new Color(47, 45, 48));
                break;
            case DemonFortressZoneKind.InfernalForge:
                DrawInterior(zone, 4, new Color(46, 41, 42));
                _spriteBatch.Draw(_pixel,
                    new Rectangle(zone.Bounds.Left, zone.GroundY - 18, zone.Bounds.Width, 18),
                    new Color(90, 34, 27));
                break;
            case DemonFortressZoneKind.CitadelRamparts:
                DrawBattlements(zone.Bounds.Left, zone.GroundY - 145, zone.Bounds.Width);
                break;
            case DemonFortressZoneKind.CommandCitadel:
                DrawInterior(zone, 6, new Color(49, 46, 51));
                DrawArch(zone.Bounds.Center.X, zone.GroundY, 500, 410);
                break;
            case DemonFortressZoneKind.BallistaBastion:
            case DemonFortressZoneKind.ExecutionCourt:
                DrawBattlements(zone.Bounds.Left, zone.GroundY - 165, zone.Bounds.Width);
                break;
            case DemonFortressZoneKind.SealedAscent:
                DrawWallBand(zone.Bounds.Left, zone.GroundY - 460, zone.Bounds.Width, 460,
                    new Color(45, 43, 48));
                break;
        }
    }

    private void DrawInterior(DemonFortressZoneDefinition zone, int bays, Color color)
    {
        DrawWallBand(zone.Bounds.Left, zone.GroundY - 430, zone.Bounds.Width, 430, color);
        int bayWidth = zone.Bounds.Width / bays;
        for (int i = 0; i < bays; i++)
        {
            int x = zone.Bounds.Left + i * bayWidth + bayWidth / 4;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, zone.GroundY - 330, bayWidth / 2, 230),
                new Color(20, 21, 25));
            DrawRectangleOutline(new Rectangle(x, zone.GroundY - 330, bayWidth / 2, 230),
                7, StoneEdge);
        }
    }

    private void DrawBattlements(int x, int y, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 145), MidStone);
        for (int px = x; px < x + width; px += 92)
            _spriteBatch.Draw(_pixel, new Rectangle(px, y - 35, 55, 38), MidStone);
        _spriteBatch.Draw(_pixel, new Rectangle(x, y + 28, width, 8), StoneEdge);
    }

    private void DrawArch(int centerX, int groundY, int width, int height)
    {
        var opening = new Rectangle(centerX - width / 2, groundY - height, width, height);
        _spriteBatch.Draw(_pixel, opening, new Color(18, 18, 22));
        DrawRectangleOutline(opening, 15, StoneEdge);
        for (int x = opening.Left + 30; x < opening.Right; x += 44)
            _spriteBatch.Draw(_pixel, new Rectangle(x, opening.Top + 18, 7, opening.Height - 18), Iron);
    }

    private void DrawProps(DungeonMap map, Rectangle camera, FortressLayer layer, float time)
    {
        foreach (FortressPropDefinition prop in map.FortressProps)
        {
            if (prop.Layer != layer || !prop.Bounds.Intersects(camera))
                continue;
            DrawProp(prop, time);
        }
    }

    private void DrawProp(FortressPropDefinition prop, float time)
    {
        Rectangle b = prop.Bounds;
        switch (prop.Kind)
        {
            case FortressPropKind.Trench:
                _spriteBatch.Draw(_pixel, b, new Color(19, 18, 21));
                DrawLine(new Vector2(b.Left, b.Top), new Vector2(b.Right, b.Top), Steel, 4);
                break;
            case FortressPropKind.BrokenSiegeEngine:
            case FortressPropKind.Ballista:
                DrawBallista(b, prop.Kind == FortressPropKind.BrokenSiegeEngine);
                break;
            case FortressPropKind.Barricade:
            case FortressPropKind.Cover:
                _spriteBatch.Draw(_pixel, b, Iron);
                for (int x = b.Left + 12; x < b.Right; x += 32)
                    DrawLine(new Vector2(x, b.Bottom), new Vector2(x + 18, b.Top), Steel, 5);
                break;
            case FortressPropKind.DamagedBanner:
            case FortressPropKind.CommandBanner:
                DrawLine(new Vector2(b.Center.X, b.Bottom), new Vector2(b.Center.X, b.Top), Bronze, 6);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X + 3, b.Top + 8, b.Width / 2, b.Height / 2), Crimson);
                break;
            case FortressPropKind.GateTower:
            case FortressPropKind.Pillar:
                DrawStone(b, MidStone);
                break;
            case FortressPropKind.Battlement:
                DrawBattlements(b.Left, b.Top, b.Width);
                break;
            case FortressPropKind.Chain:
                for (int y = b.Top; y < b.Bottom; y += 18)
                    DrawRectangleOutline(new Rectangle(b.Center.X - 8, y, 16, 22), 3, Steel);
                break;
            case FortressPropKind.GateMechanism:
                _spriteBatch.Draw(_pixel, b, Iron);
                DrawRectangleOutline(b, 6, Bronze);
                DrawRectangleOutline(new Rectangle(b.Center.X - 36, b.Center.Y - 36, 72, 72), 8, Steel);
                break;
            case FortressPropKind.Bunk:
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, b.Width, 13), Iron);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Center.Y, b.Width, 13), Iron);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, 8, b.Height), Steel);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Right - 8, b.Top, 8, b.Height), Steel);
                break;
            case FortressPropKind.WeaponRack:
            case FortressPropKind.ShieldRack:
            case FortressPropKind.TrophyWeapons:
                DrawRack(b);
                break;
            case FortressPropKind.Crate:
                _spriteBatch.Draw(_pixel, b, new Color(79, 55, 42));
                DrawRectangleOutline(b, 5, Bronze);
                DrawLine(b.Location.ToVector2(), new Vector2(b.Right, b.Bottom), Bronze, 4);
                break;
            case FortressPropKind.OfficerTable:
            case FortressPropKind.WarTable:
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, b.Width, 18), Bronze);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left + 18, b.Top, 12, b.Height), Iron);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Right - 30, b.Top, 12, b.Height), Iron);
                break;
            case FortressPropKind.TrainingDummy:
                DrawLine(new Vector2(b.Center.X, b.Bottom), new Vector2(b.Center.X, b.Top), Bronze, 9);
                DrawLine(new Vector2(b.Left, b.Top + 40), new Vector2(b.Right, b.Top + 40), Bronze, 9);
                break;
            case FortressPropKind.WarBell:
                DrawLine(new Vector2(b.Center.X, b.Bottom), new Vector2(b.Center.X, b.Top), Iron, 8);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X - 38, b.Top + 35, 76, 65), Bronze);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X - 45, b.Top + 95, 90, 11), Steel);
                break;
            case FortressPropKind.Furnace:
            case FortressPropKind.ForgeMachine:
                _spriteBatch.Draw(_pixel, b, new Color(57, 47, 46));
                DrawRectangleOutline(b, 8, Iron);
                Color glow = Color.Lerp(Ember, Lava, .5f + MathF.Sin(time * 4f) * .2f);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Left + b.Width / 4, b.Bottom - 78, b.Width / 2, 58), glow);
                break;
            case FortressPropKind.Anvil:
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, b.Width, 25), Steel);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Center.X - 22, b.Top, 44, b.Height), Iron);
                break;
            case FortressPropKind.SuspendedMetal:
                DrawLine(new Vector2(b.Left, b.Top - 90), new Vector2(b.Left + 15, b.Top), Iron, 4);
                DrawLine(new Vector2(b.Right, b.Top - 90), new Vector2(b.Right - 15, b.Top), Iron, 4);
                _spriteBatch.Draw(_pixel, b, new Color(107, 72, 53));
                break;
            case FortressPropKind.MoltenChannel:
                _spriteBatch.Draw(_pixel, b, Lava);
                _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, b.Width, 5),
                    new Color(255, 192, 75));
                break;
            case FortressPropKind.SealedGate:
                _spriteBatch.Draw(_pixel, b, new Color(31, 31, 37));
                DrawRectangleOutline(b, 12, Steel);
                for (int x = b.Left + 28; x < b.Right; x += 44)
                    _spriteBatch.Draw(_pixel, new Rectangle(x, b.Top + 20, 9, b.Height - 20), Iron);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(b.Center.X - 28, b.Center.Y - 28, 56, 56), Crimson);
                DrawRectangleOutline(new Rectangle(b.Center.X - 28, b.Center.Y - 28, 56, 56),
                    5, Bronze);
                break;
        }
    }

    private void DrawCheckpoints(DungeonMap map, Rectangle camera, float time)
    {
        foreach (FortressCheckpointDefinition checkpoint in map.FortressCheckpoints)
        {
            var b = new Rectangle((int)checkpoint.Position.X - 20,
                (int)checkpoint.Position.Y - 92, 40, 94);
            if (!b.Intersects(camera))
                continue;
            DrawLine(new Vector2(b.Center.X, b.Bottom), new Vector2(b.Center.X, b.Top), Iron, 6);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Center.X + 3, b.Top + 8, 28, 37), Crimson);
            int glow = 8 + (int)MathF.Round(MathF.Sin(time * 3f) * 3f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Center.X - glow / 2, b.Top - glow, glow, glow), Ember);
        }
    }

    private void DrawPlatform(Platform platform)
    {
        Color color = platform.Kind == PlatformKind.Raised ? Iron : Stone;
        _spriteBatch.Draw(_pixel, platform.Bounds, color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(platform.Bounds.Left, platform.Bounds.Top,
                platform.Bounds.Width, Math.Min(8, platform.Bounds.Height)), StoneEdge);
        for (int x = platform.Bounds.Left + 38; x < platform.Bounds.Right; x += 96)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, platform.Bounds.Top + 17, 3,
                    Math.Min(32, Math.Max(0, platform.Bounds.Height - 17))),
                new Color(36, 36, 40));
    }

    private void DrawBallista(Rectangle b, bool broken)
    {
        DrawLine(new Vector2(b.Left + 18, b.Bottom), new Vector2(b.Center.X, b.Top + 25), Bronze, 9);
        DrawLine(new Vector2(b.Right - 18, b.Bottom), new Vector2(b.Center.X, b.Top + 25), Bronze, 9);
        DrawLine(new Vector2(b.Left, b.Top + 25),
            new Vector2(broken ? b.Center.X + 20 : b.Right, broken ? b.Top + 42 : b.Top + 25),
            Steel, 8);
    }

    private void DrawRack(Rectangle b)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, 8, b.Height), Bronze);
        _spriteBatch.Draw(_pixel, new Rectangle(b.Right - 8, b.Top, 8, b.Height), Bronze);
        _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top + 28, b.Width, 8), Bronze);
        for (int x = b.Left + 25; x < b.Right - 15; x += 34)
            DrawLine(new Vector2(x, b.Bottom), new Vector2(x + 8, b.Top + 5), Steel, 4);
    }

    private void DrawWallBand(int x, int y, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);
        for (int py = y + 45; py < y + height; py += 62)
            _spriteBatch.Draw(_pixel, new Rectangle(x, py, width, 3), new Color(28, 28, 32));
    }

    private void DrawStone(Rectangle b, Color color)
    {
        _spriteBatch.Draw(_pixel, b, color);
        DrawRectangleOutline(b, 5, StoneEdge);
    }

    private void DrawRectangleOutline(Rectangle b, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, b.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Bottom - thickness, b.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(b.Left, b.Top, thickness, b.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(b.Right - thickness, b.Top, thickness, b.Height), color);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }
}
