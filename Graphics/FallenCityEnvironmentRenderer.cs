using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered, deterministic, code-only presentation for the Fallen City.
/// Everything drawn here is decorative: collision remains sourced from the
/// authored map platforms so scenery cannot obstruct the validated route.
/// </summary>
public sealed class FallenCityEnvironmentRenderer
{
    private static readonly Color SkyTop = new(91, 88, 86);
    private static readonly Color SkyBottom = new(124, 112, 101);
    private static readonly Color Smoke = new(50, 49, 50);
    private static readonly Color FarStone = new(47, 47, 50);
    private static readonly Color MidStone = new(58, 57, 59);
    private static readonly Color Stone = new(72, 69, 68);
    private static readonly Color StoneDark = new(45, 44, 46);
    private static readonly Color StoneEdge = new(116, 105, 94);
    private static readonly Color Floor = new(62, 59, 57);
    private static readonly Color Wood = new(82, 58, 43);
    private static readonly Color CharredWood = new(39, 34, 33);
    private static readonly Color Iron = new(49, 48, 50);
    private static readonly Color RoyalRed = new(103, 43, 45);
    private static readonly Color RoyalGold = new(143, 109, 57);
    private static readonly Color Cloth = new(91, 67, 61);
    private static readonly Color Ember = new(187, 78, 37);
    private static readonly Color Flame = new(220, 126, 51);
    private static readonly Color Ash = new(151, 143, 133);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public FallenCityEnvironmentRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawWorld(GameSession session, Rectangle camera, GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsFallenCity != true)
            return;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        DrawSky(camera);
        DrawFarSkyline(map, camera, time);
        DrawMidSkyline(map, camera);

        foreach (FallenCityZoneDefinition zone in map.FallenCityZones)
        {
            if (!zone.Bounds.Intersects(camera))
                continue;
            DrawDistrictWall(zone);
            DrawDistrictIdentity(zone, time);
        }

        foreach (Rectangle corridor in map.Corridors)
        {
            if (corridor.Intersects(camera))
                DrawTransition(corridor);
        }

        foreach (Platform platform in map.Platforms)
        {
            if (platform.Bounds.Intersects(camera))
                DrawCollisionPlatform(platform);
        }

        foreach (CollapsingStructureDefinition structure in map.CollapsingStructures)
        {
            Rectangle cull = structure.Bounds;
            cull.Inflate(28, 45);
            if (cull.Intersects(camera))
                DrawCollapsingStructure(
                    structure,
                    session.CollapsingCity.IsBroken(structure.StructureId),
                    session.CollapsingCity.RecentlyBrokenStructureId == structure.StructureId,
                    session.CollapsingCity.BreakFeedbackTimeRemaining,
                    time);
        }

        foreach (CityAlarmObjectDefinition alarm in map.CityAlarmObjects)
        {
            var cull = new Rectangle((int)alarm.Position.X - 70,
                (int)alarm.Position.Y - 145, 140, 150);
            if (cull.Intersects(camera))
                DrawAlarm(alarm.Position,
                    session.CityAlarm.IsDisabled(alarm.ObjectId),
                    session.CityAlarm.Level,
                    session.CityAlarm.WatcherSignalFeedbackTimeRemaining,
                    time);
        }
    }

    public void DrawForeground(GameSession session, Rectangle camera, GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsFallenCity != true)
            return;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        FallenCityZoneDefinition zone = FindZone(map, camera.Center.X);
        int zoneSeed = zone == null ? 0 : (int)zone.Kind + 1;
        int spacing = zone?.Kind is FallenCityZoneKind.BurnedQuarter or
            FallenCityZoneKind.RuinedThronePlaza ? 72 : 118;
        int first = camera.Left - PositiveModulo(camera.Left, spacing) - spacing;
        for (int x = first; x < camera.Right + spacing; x += spacing)
        {
            int seed = Hash(x / spacing, zoneSeed * 17);
            float drift = PositiveModulo((int)(time * (5 + Math.Abs(seed % 7))), spacing);
            int px = (int)(x + drift);
            int py = camera.Top + 80 + PositiveModulo(seed, Math.Max(120, camera.Height - 180));
            int size = 1 + Math.Abs(seed % 3);
            Color mote = zone?.Kind is FallenCityZoneKind.BurnedQuarter or
                FallenCityZoneKind.RuinedThronePlaza
                ? new Color(166, 122, 91, 58)
                : new Color(Ash, 35);
            _spriteBatch.Draw(_pixel, new Rectangle(px, py, size + 1, size), mote);
        }

        if (zone == null)
            return;
        int ground = zone.GroundY;
        int rubbleFirst = zone.Bounds.Left + 38;
        for (int x = rubbleFirst; x < zone.Bounds.Right; x += 173)
        {
            if (x < camera.Left - 40 || x > camera.Right + 40)
                continue;
            int seed = Hash(x, zoneSeed);
            int width = 18 + Math.Abs(seed % 25);
            int height = 5 + Math.Abs(seed / 7 % 8);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, ground - height, width, height),
                new Color(39, 37, 37, 175));
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + width / 3, ground - height - 3, width / 2, 4),
                new Color(86, 78, 72, 145));
        }
    }

    private void DrawSky(Rectangle camera)
    {
        int bandHeight = Math.Max(1, camera.Height / 5);
        for (int band = 0; band < 5; band++)
        {
            float amount = band / 4f;
            _spriteBatch.Draw(_pixel,
                new Rectangle(camera.Left, camera.Top + band * bandHeight,
                    camera.Width, band == 4 ? camera.Height - band * bandHeight : bandHeight),
                Color.Lerp(SkyTop, SkyBottom, amount));
        }
    }

    private void DrawFarSkyline(DungeonMap map, Rectangle camera, float time)
    {
        int first = (int)MathF.Floor(camera.Left * .15f / 250f) - 2;
        int last = first + camera.Width / 250 + 6;
        int baseY = map.WorldBounds.Top + 405;
        for (int index = first; index <= last; index++)
        {
            int seed = Hash(index, 13);
            int x = (int)MathF.Round(index * 250f + camera.Left * .85f);
            int width = 150 + Math.Abs(seed % 90);
            int height = 145 + Math.Abs(seed / 11 % 165);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, baseY - height, width, height), FarStone);
            if ((seed & 3) == 0)
                DrawBrokenTower(x + width / 2, baseY - height, 54, 100, FarStone);
            for (int window = x + 28; window < x + width - 14; window += 46)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(window, baseY - height + 54, 9, 22),
                    new Color(34, 35, 38));
        }

        for (int column = 0; column < 4; column++)
        {
            int seed = Hash(column, 97);
            int anchor = map.WorldBounds.Left + 1600 + column * 3250;
            int x = (int)MathF.Round(anchor + camera.Left * .22f);
            float rise = PositiveModulo((int)(time * 8f + column * 71), 140);
            for (int puff = 0; puff < 5; puff++)
            {
                int px = x + puff * 17 + (int)MathF.Sin(time * .18f + puff) * 12;
                int py = baseY - 170 - puff * 45 - (int)rise;
                int size = 56 + puff * 17 + Math.Abs(seed % 9);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(px - size / 2, py - size / 3, size, size / 2),
                    new Color(Smoke, 34 + puff * 5));
            }
        }
    }

    private void DrawMidSkyline(DungeonMap map, Rectangle camera)
    {
        int first = (int)MathF.Floor(camera.Left * .38f / 205f) - 2;
        int last = first + camera.Width / 205 + 8;
        int baseY = map.WorldBounds.Top + 500;
        for (int index = first; index <= last; index++)
        {
            int seed = Hash(index, 41);
            int x = (int)MathF.Round(index * 205f + camera.Left * .62f);
            int width = 126 + Math.Abs(seed % 62);
            int height = 105 + Math.Abs(seed / 19 % 130);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, baseY - height, width, height), MidStone);
            DrawJaggedRoof(x - 5, baseY - height, width + 10, seed, MidStone);
            if ((seed & 1) == 0)
                DrawBanner(x + width - 35, baseY - height + 18, 26, 66,
                    new Color(RoyalRed, 105), 0f);
        }
    }

    private void DrawDistrictWall(FallenCityZoneDefinition zone)
    {
        Color wall = zone.Kind switch
        {
            FallenCityZoneKind.BurnedQuarter => new Color(48, 41, 39),
            FallenCityZoneKind.NobleAvenue => new Color(70, 68, 70),
            FallenCityZoneKind.PalaceApproach => new Color(69, 65, 65),
            FallenCityZoneKind.RuinedThronePlaza => new Color(65, 57, 59),
            _ => new Color(63, 61, 61)
        };
        int top = zone.Bounds.Top + 318;
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, top, zone.Bounds.Width, zone.GroundY - top), wall);
        for (int y = top + 34, row = 0; y < zone.GroundY - 10; y += 38, row++)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(zone.Bounds.Left, y, zone.Bounds.Width, 2),
                new Color(StoneDark, 85));
            int offset = row % 2 == 0 ? 14 : 58;
            for (int x = zone.Bounds.Left + offset; x < zone.Bounds.Right; x += 92)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, y - 36, 2, 36), new Color(StoneDark, 58));
        }
    }

    private void DrawDistrictIdentity(FallenCityZoneDefinition zone, float time)
    {
        switch (zone.Kind)
        {
            case FallenCityZoneKind.BrokenOuterGate: DrawOuterGate(zone, time); break;
            case FallenCityZoneKind.RefugeeStreet: DrawRefugeeStreet(zone, time); break;
            case FallenCityZoneKind.MarketRuins: DrawMarket(zone, time); break;
            case FallenCityZoneKind.WatchDistrict: DrawWatchDistrict(zone, time); break;
            case FallenCityZoneKind.BurnedQuarter: DrawBurnedQuarter(zone, time); break;
            case FallenCityZoneKind.NobleAvenue: DrawNobleAvenue(zone, time); break;
            case FallenCityZoneKind.ExecutionSquare: DrawExecutionSquare(zone); break;
            case FallenCityZoneKind.RoyalGuardBarracks: DrawBarracks(zone, time); break;
            case FallenCityZoneKind.PalaceApproach: DrawPalaceApproach(zone, time); break;
            case FallenCityZoneKind.RuinedThronePlaza: DrawThronePlaza(zone, time); break;
        }
    }

    private void DrawOuterGate(FallenCityZoneDefinition z, float time)
    {
        int l = z.Bounds.Left + 35;
        DrawTower(l, z.GroundY, 205, 390, true);
        DrawTower(z.Bounds.Right - 240, z.GroundY, 205, 350, true);
        DrawStoneArch(z.Bounds.Left + 255, z.GroundY - 338, z.Bounds.Width - 510, 338);
        int gateLeft = z.Bounds.Left + 425;
        int gateRight = z.Bounds.Right - 405;
        for (int x = gateLeft; x < gateRight; x += 23)
        {
            int length = 184 + Math.Abs(Hash(x, 3) % 80);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, z.GroundY - 310, 8, length), Iron);
            DrawTriangleDown(x - 3, z.GroundY - 310 + length, 14, 18, Iron);
        }
        DrawCart(z.Bounds.Left + 265, z.GroundY, true);
        DrawBarricade(z.Bounds.Left + 760, z.GroundY, true);
        DrawShield(z.Bounds.Left + 715, z.GroundY - 15, 20, new Color(83, 54, 48));
        DrawBrokenStatue(z.Bounds.Left + 88, z.GroundY, false);
        DrawBanner(z.Bounds.Left + 185, z.GroundY - 356, 34, 105, RoyalRed, time);
        DrawBanner(z.Bounds.Right - 220, z.GroundY - 326, 34, 98, RoyalRed, time + 1f);
    }

    private void DrawRefugeeStreet(FallenCityZoneDefinition z, float time)
    {
        DrawHouse(z.Bounds.Left + 25, z.GroundY, 250, 286, 2, false);
        DrawHouse(z.Bounds.Left + 300, z.GroundY, 205, 240, 1, true);
        DrawHouse(z.Bounds.Left + 525, z.GroundY, 275, 315, 2, true);
        DrawHouse(z.Bounds.Left + 830, z.GroundY, 330, 260, 2, false);
        DrawClothesLine(z.Bounds.Left + 285, z.GroundY - 188,
            z.Bounds.Left + 610, z.GroundY - 225, time);
        DrawCart(z.Bounds.Left + 570, z.GroundY, true);
        DrawCrateStack(z.Bounds.Left + 930, z.GroundY, 3);
        DrawBarricade(z.Bounds.Left + 1040, z.GroundY, false);
        DrawChair(z.Bounds.Left + 430, z.GroundY);
    }

    private void DrawMarket(FallenCityZoneDefinition z, float time)
    {
        DrawMarketStall(z.Bounds.Left + 70, z.GroundY, 225, RoyalRed, time);
        DrawMarketStall(z.Bounds.Left + 360, z.GroundY, 250, new Color(107, 76, 49), time + .8f);
        DrawMarketStall(z.Bounds.Left + 920, z.GroundY, 255, Cloth, time + 1.5f);
        DrawCart(z.Bounds.Left + 675, z.GroundY, true);
        DrawCrateStack(z.Bounds.Left + 300, z.GroundY, 3);
        DrawBarrels(z.Bounds.Left + 790, z.GroundY, 3);
        DrawHangingSign(z.Bounds.Left + 1110, z.GroundY - 222, "SCALES", time);
        DrawScales(z.Bounds.Left + 665, z.GroundY - 17);
        for (int x = z.Bounds.Left + 190; x < z.Bounds.Right - 80; x += 235)
            DrawBasket(x, z.GroundY - 5);
    }

    private void DrawWatchDistrict(FallenCityZoneDefinition z, float time)
    {
        DrawWatchTower(z.Bounds.Left + 40, z.GroundY, 220, 410);
        DrawWatchTower(z.Bounds.Right - 315, z.GroundY, 230, 355);
        DrawScaffold(z.Bounds.Left + 315, z.GroundY, 300, 205);
        DrawLadder(z.Bounds.Left + 635, z.GroundY - 10, 185);
        DrawGuardPlatform(z.Bounds.Left + 690, z.GroundY - 142, 310);
        DrawBanner(z.Bounds.Left + 155, z.GroundY - 388, 31, 90,
            new Color(126, 51, 44), time);
        DrawSignalFlag(z.Bounds.Left + 920, z.GroundY - 280, time);
        DrawBarricade(z.Bounds.Left + 1010, z.GroundY, false);
    }

    private void DrawBurnedQuarter(FallenCityZoneDefinition z, float time)
    {
        DrawBurnedHouse(z.Bounds.Left + 20, z.GroundY, 290, 275);
        DrawBurnedHouse(z.Bounds.Left + 325, z.GroundY, 330, 340);
        DrawBurnedHouse(z.Bounds.Left + 745, z.GroundY, 245, 240);
        DrawBurnedHouse(z.Bounds.Left + 990, z.GroundY, 220, 300);
        DrawCart(z.Bounds.Left + 675, z.GroundY, true, charred: true);
        DrawFire(z.Bounds.Left + 175, z.GroundY - 8, 23, time);
        DrawFire(z.Bounds.Left + 705, z.GroundY - 7, 18, time + .7f);
        DrawFire(z.Bounds.Left + 1080, z.GroundY - 6, 20, time + 1.1f);
        DrawSmokeColumn(z.Bounds.Left + 470, z.GroundY - 310, time, 1);
        DrawSmokeColumn(z.Bounds.Left + 1050, z.GroundY - 250, time, 2);
        for (int x = z.Bounds.Left + 65; x < z.Bounds.Right; x += 120)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, z.GroundY - 5, 46, 5),
                new Color(85, 52, 40));
    }

    private void DrawNobleAvenue(FallenCityZoneDefinition z, float time)
    {
        DrawNobleHouse(z.Bounds.Left + 20, z.GroundY, 390, 360, true);
        DrawNobleHouse(z.Bounds.Left + 455, z.GroundY, 405, 330, false);
        DrawNobleHouse(z.Bounds.Left + 900, z.GroundY, 400, 375, true);
        DrawIronFence(z.Bounds.Left + 120, z.GroundY, 360);
        DrawIronFence(z.Bounds.Left + 875, z.GroundY, 300);
        DrawFountain(z.Bounds.Center.X, z.GroundY);
        DrawBrokenStatue(z.Bounds.Left + 1050, z.GroundY, true);
        DrawBanner(z.Bounds.Left + 525, z.GroundY - 302, 40, 112,
            new Color(RoyalRed, 165), time);
    }

    private void DrawExecutionSquare(FallenCityZoneDefinition z)
    {
        DrawGallows(z.Bounds.Left + 145, z.GroundY, 245, 245);
        DrawExecutionPlatform(z.Bounds.Left + 430, z.GroundY, 390, 92);
        DrawCage(z.Bounds.Left + 875, z.GroundY, 105, 142);
        DrawNoticeBoard(z.Bounds.Left + 1035, z.GroundY, 145, 125);
        DrawCrowdRail(z.Bounds.Left + 45, z.GroundY, 150);
        DrawCrowdRail(z.Bounds.Left + 1000, z.GroundY, 190);
        DrawChain(z.Bounds.Left + 720, z.GroundY - 210, 108, 8, 0f);
    }

    private void DrawBarracks(FallenCityZoneDefinition z, float time)
    {
        DrawBarracksBuilding(z.Bounds.Left + 20, z.GroundY, 390, 300);
        DrawBarracksBuilding(z.Bounds.Right - 440, z.GroundY, 395, 320);
        DrawWeaponRack(z.Bounds.Left + 480, z.GroundY, 130);
        DrawShieldRack(z.Bounds.Left + 650, z.GroundY, 175);
        DrawArmorStand(z.Bounds.Left + 890, z.GroundY);
        DrawCrateStack(z.Bounds.Left + 1100, z.GroundY, 4);
        for (int x = z.Bounds.Left + 470; x < z.Bounds.Left + 1010; x += 95)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x, z.GroundY - 3, 55, 3),
                new Color(RoyalGold, 110));
        DrawBanner(z.Bounds.Left + 270, z.GroundY - 270, 38, 104,
            RoyalRed, time);
    }

    private void DrawPalaceApproach(FallenCityZoneDefinition z, float time)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(z.Bounds.Left + 25, z.GroundY - 28,
                z.Bounds.Width - 50, 28), new Color(82, 76, 72));
        for (int x = z.Bounds.Left + 60; x < z.Bounds.Right - 60; x += 120)
            DrawRoadSlab(x, z.GroundY - 28, 105);
        DrawMonument(z.Bounds.Left + 140, z.GroundY, false);
        DrawMonument(z.Bounds.Left + 1040, z.GroundY, true);
        DrawPalaceWall(z.Bounds.Left + 320, z.GroundY, 700, 380);
        DrawDestroyedPalaceGate(z.Bounds.Left + 500, z.GroundY, 350, 300);
        DrawCeremonialBrazier(z.Bounds.Left + 400, z.GroundY, time, false);
        DrawCeremonialBrazier(z.Bounds.Left + 960, z.GroundY, time + .5f, false);
        DrawBanner(z.Bounds.Left + 465, z.GroundY - 330, 44, 135,
            new Color(RoyalRed, 190), time);
        DrawBanner(z.Bounds.Left + 865, z.GroundY - 330, 44, 135,
            new Color(RoyalRed, 190), time + .6f);
    }

    private void DrawThronePlaza(FallenCityZoneDefinition z, float time)
    {
        DrawPalaceRuin(z.Bounds.Left + 40, z.GroundY, z.Bounds.Width - 80, 500);
        DrawBrokenThrone(z.Bounds.Center.X, z.GroundY - 115);
        DrawColumn(z.Bounds.Left + 190, z.GroundY, 335, true);
        DrawColumn(z.Bounds.Left + 430, z.GroundY, 285, false);
        DrawColumn(z.Bounds.Right - 430, z.GroundY, 300, true);
        DrawColumn(z.Bounds.Right - 205, z.GroundY, 360, false);
        DrawBrokenStatue(z.Bounds.Left + 315, z.GroundY, true);
        DrawBrokenStatue(z.Bounds.Right - 340, z.GroundY, false);
        DrawRoyalInsignia(z.Bounds.Center.X, z.GroundY - 405);
        DrawBanner(z.Bounds.Center.X - 215, z.GroundY - 445, 52, 180,
            new Color(RoyalRed, 178), time);
        DrawBanner(z.Bounds.Center.X + 165, z.GroundY - 445, 52, 180,
            new Color(RoyalRed, 178), time + .8f);
        DrawCeremonialBrazier(z.Bounds.Left + 510, z.GroundY, time, true);
        DrawCeremonialBrazier(z.Bounds.Right - 510, z.GroundY, time + .4f, true);
    }

    private void DrawCollisionPlatform(Platform platform)
    {
        Color body = platform.Kind == PlatformKind.Raised
            ? new Color(77, 71, 68)
            : Floor;
        _spriteBatch.Draw(_pixel, platform.Bounds, body);
        _spriteBatch.Draw(_pixel,
            new Rectangle(platform.Bounds.Left, platform.Bounds.Top,
                platform.Bounds.Width, Math.Min(6, platform.Bounds.Height)), StoneEdge);
        if (platform.Kind is PlatformKind.Ground or PlatformKind.Transition)
        {
            for (int x = platform.Bounds.Left + 20; x < platform.Bounds.Right; x += 74)
            {
                int seed = Hash(x, platform.Bounds.Top);
                int width = 28 + Math.Abs(seed % 26);
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, platform.Bounds.Top + 15, width, 3),
                    new Color(StoneDark, 120));
                if ((seed & 3) == 0)
                    DrawLine(new Vector2(x + 10, platform.Bounds.Top + 2),
                        new Vector2(x + 20, platform.Bounds.Top + 13),
                        new Color(StoneDark, 175), 2f);
            }
        }
    }

    private void DrawCollapsingStructure(
        CollapsingStructureDefinition structure,
        bool broken,
        bool recentlyBroken,
        float feedback,
        float time)
    {
        Rectangle b = structure.Bounds;
        int shake = recentlyBroken && feedback > 0f
            ? (int)MathF.Round(MathF.Sin(time * 42f) * feedback * 3f)
            : 0;
        b.Offset(shake, 0);
        if (broken)
        {
            // Raised structures retain a low, fragmented cap so their existing
            // collision never becomes invisible. Vertical props collapse into
            // harmless floor rubble.
            bool horizontal = b.Width > b.Height;
            if (horizontal)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(b.X, b.Y + b.Height - 7,
                    b.Width, 7), new Color(82, 70, 63));
                for (int x = b.Left + 4; x < b.Right - 8; x += 34)
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, b.Bottom - 12 - Math.Abs(Hash(x, 5) % 7),
                            24, 8), new Color(102, 86, 74));
            }
            else
            {
                for (int x = b.Left - 18; x < b.Right + 30; x += 21)
                {
                    int h = 7 + Math.Abs(Hash(x, b.Y) % 13);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(x, b.Bottom - h, 18, h),
                        new Color(93, 78, 68));
                }
            }
            if (recentlyBroken)
            {
                for (int mote = 0; mote < 8; mote++)
                {
                    int seed = Hash(mote, b.X);
                    int px = b.Center.X + seed % Math.Max(16, b.Width / 2);
                    int py = b.Bottom - 12 - mote * 9 - (int)((1f - feedback) * 22f);
                    _spriteBatch.Draw(_pixel, new Rectangle(px, py, 5, 4),
                        new Color(142, 126, 111, (int)(feedback * 120f)));
                }
            }
            return;
        }

        Color material = structure.Kind == CollapsingStructureKind.DestructibleBarricade
            ? Wood : new Color(104, 87, 76);
        if (b.Height > b.Width)
        {
            // Vertical collapse props are open braces rather than opaque walls;
            // the authored route intentionally treats them as non-colliding.
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Left, b.Top, 7, b.Height), material);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Right - 7, b.Top, 7, b.Height), material);
            DrawLine(new Vector2(b.Left + 3, b.Top + 5),
                new Vector2(b.Right - 3, b.Bottom - 5), material, 6f);
            DrawLine(new Vector2(b.Right - 3, b.Top + 12),
                new Vector2(b.Left + 3, b.Bottom - 8), material, 5f);
        }
        else
        {
            _spriteBatch.Draw(_pixel, b, material);
            DrawOutline(b, 2, new Color(154, 125, 96));
        }
        DrawLine(new Vector2(b.Left + 4, b.Top + 2),
            new Vector2(b.Center.X, b.Center.Y), StoneDark, 2f);
        DrawLine(new Vector2(b.Center.X, b.Center.Y),
            new Vector2(b.Right - 3, b.Bottom - 2), StoneDark, 2f);
        DrawLine(new Vector2(b.Center.X, b.Center.Y),
            new Vector2(b.Center.X - 12, b.Bottom - 2), StoneDark, 2f);
        for (int dust = 0; dust < 3; dust++)
        {
            int seed = Hash(dust, b.X);
            int y = b.Top - 8 - PositiveModulo((int)(time * 8f + seed), 24);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Left + PositiveModulo(seed, Math.Max(1, b.Width)), y, 3, 2),
                new Color(Ash, 45));
        }
    }

    private void DrawAlarm(Vector2 position, bool disabled, int level, float signal, float time)
    {
        int x = (int)position.X;
        int y = (int)position.Y;
        Color metal = disabled ? new Color(68, 65, 62) : RoyalGold;
        _spriteBatch.Draw(_pixel, new Rectangle(x - 7, y - 116, 14, 116), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 43, y - 119, 86, 10), Wood);
        DrawLine(new Vector2(x, y - 109), new Vector2(x, y - 86), Iron, 4f);
        float swing = disabled ? 0f : MathF.Sin(time * (signal > 0f ? 14f : 1.2f)) *
            (signal > 0f ? 8f : 2f);
        var bell = new Rectangle(x - 24 + (int)swing, y - 88, 48, 39);
        _spriteBatch.Draw(_pixel, bell, metal);
        _spriteBatch.Draw(_pixel,
            new Rectangle(bell.Left - 5, bell.Bottom - 7, bell.Width + 10, 9),
            disabled ? new Color(57, 56, 55) : new Color(181, 137, 64));
        _spriteBatch.Draw(_pixel,
            new Rectangle(bell.Center.X - 4, bell.Bottom, 8, 9), Iron);
        if (!disabled && (level > 0 || signal > 0f))
        {
            int alpha = signal > 0f ? 190 : 75 + level * 25;
            DrawArcMarks(bell.Center.X, bell.Center.Y, new Color(232, 172, 72, alpha));
            _spriteBatch.Draw(_pixel,
                new Rectangle(x - 10, y - 138, 20, 7),
                new Color(188, 72, 48, alpha));
        }
        if (disabled)
            DrawLine(new Vector2(bell.Left, bell.Top),
                new Vector2(bell.Right, bell.Bottom), new Color(42, 41, 41), 4f);
    }

    private void DrawHouse(int x, int ground, int width, int height, int floors, bool damaged)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, width, height),
            new Color(71, 65, 62));
        DrawTimberFrame(x, top, width, height);
        DrawJaggedRoof(x - 8, top, width + 16, damaged ? x : x + 1, Wood);
        for (int floor = 0; floor < floors; floor++)
        {
            int wy = top + 48 + floor * 88;
            DrawWindow(x + 35, wy, damaged);
            DrawWindow(x + width - 69, wy, damaged && floor == 0);
        }
        DrawDoor(x + width / 2 - 24, ground - 70, 48, 70, damaged);
    }

    private void DrawBurnedHouse(int x, int ground, int width, int height)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top + 55, width, height - 55),
            new Color(49, 43, 41));
        for (int beam = 0; beam < 5; beam++)
        {
            int bx = x + 18 + beam * (width - 36) / 4;
            DrawLine(new Vector2(bx, ground),
                new Vector2(bx + (beam % 2 == 0 ? -32 : 38), top + 18),
                CharredWood, 10f);
        }
        DrawJaggedRoof(x - 6, top + 46, width + 12, x * 7, CharredWood);
        for (int w = x + 38; w < x + width - 30; w += 86)
            _spriteBatch.Draw(_pixel,
                new Rectangle(w, top + 104, 30, 45), new Color(24, 25, 26));
    }

    private void DrawNobleHouse(int x, int ground, int width, int height, bool damaged)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, width, height),
            new Color(82, 79, 77));
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, width, 15), StoneEdge);
        for (int pillar = x + 24; pillar < x + width - 15; pillar += 95)
            DrawColumn(pillar, ground, height - 30, damaged && pillar > x + width / 2);
        DrawBalcony(x + 70, top + 145, width - 140, damaged);
        DrawDoor(x + width / 2 - 30, ground - 96, 60, 96, damaged);
        DrawWindow(x + 38, top + 55, damaged);
        DrawWindow(x + width - 72, top + 55, damaged);
    }

    private void DrawMarketStall(int x, int ground, int width, Color cloth, float time)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 122, 9, 122), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 9, ground - 122, 9, 122), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 8, ground - 128, width + 16, 10), Wood);
        int strips = 5;
        for (int i = 0; i < strips; i++)
        {
            int sx = x - 6 + i * (width + 12) / strips;
            int sw = (width + 12) / strips + 1;
            int drop = 26 + (int)(MathF.Sin(time * .7f + i) * 3f);
            _spriteBatch.Draw(_pixel, new Rectangle(sx, ground - 118, sw, drop),
                i % 2 == 0 ? cloth : new Color(124, 104, 82));
        }
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 48, width, 12), Wood);
    }

    private void DrawWatchTower(int x, int ground, int width, int height)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top + 55, width, height - 55), Stone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 18, top + 45, width + 36, 18), StoneEdge);
        for (int merlon = x - 10; merlon < x + width; merlon += 44)
            _spriteBatch.Draw(_pixel, new Rectangle(merlon, top + 20, 26, 28), Stone);
        for (int y = top + 105; y < ground - 70; y += 90)
            DrawArrowSlit(x + width / 2, y);
    }

    private void DrawTower(int x, int ground, int width, int height, bool damaged)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top + 35, width, height - 35), Stone);
        for (int merlon = x; merlon < x + width; merlon += 46)
        {
            if (damaged && ((merlon / 46) & 2) != 0)
                continue;
            _spriteBatch.Draw(_pixel, new Rectangle(merlon, top, 30, 48), Stone);
        }
        DrawArrowSlit(x + width / 3, top + 92);
        DrawArrowSlit(x + width * 2 / 3, top + 158);
        DrawStoneCourses(new Rectangle(x, top + 35, width, height - 35));
    }

    private void DrawStoneArch(int x, int top, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, width, 55), Stone);
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, 55, height), Stone);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 55, top, 55, height), Stone);
        for (int step = 0; step < 5; step++)
        {
            int inset = step * 30;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + 55 + inset, top + 45 + step * 20,
                    Math.Max(0, width - 110 - inset * 2), 23), Stone);
        }
        DrawStoneCourses(new Rectangle(x, top, width, height));
    }

    private void DrawPalaceWall(int x, int ground, int width, int height)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top, width, height),
            new Color(75, 71, 70));
        for (int merlon = x; merlon < x + width; merlon += 58)
            _spriteBatch.Draw(_pixel, new Rectangle(merlon, top - 32, 36, 35),
                new Color(81, 76, 73));
        DrawStoneCourses(new Rectangle(x, top, width, height));
    }

    private void DrawDestroyedPalaceGate(int x, int ground, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, 76, height), StoneDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + width - 76, ground - height, 76, height), StoneDark);
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, width, 48), StoneDark);
        for (int bar = x + 78; bar < x + width - 70; bar += 25)
        {
            int broken = Math.Abs(Hash(bar, ground) % 75);
            _spriteBatch.Draw(_pixel,
                new Rectangle(bar, ground - height + 44, 8, height - 76 - broken), Iron);
        }
    }

    private void DrawPalaceRuin(int x, int ground, int width, int height)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top + 100, width, height - 100),
            new Color(68, 63, 64));
        for (int block = x; block < x + width; block += 78)
        {
            int loss = Math.Abs(Hash(block, 117) % 95);
            _spriteBatch.Draw(_pixel,
                new Rectangle(block, top + loss, 58, 112 - loss / 2),
                new Color(73, 67, 67));
        }
        DrawStoneCourses(new Rectangle(x, top + 100, width, height - 100));
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + width / 2 - 310, ground - 175, 620, 175),
            new Color(57, 52, 54));
    }

    private void DrawBrokenThrone(int centerX, int baseY)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 112, baseY - 38, 224, 38),
            new Color(83, 74, 70));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 72, baseY - 175, 144, 140),
            new Color(91, 76, 70));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 92, baseY - 165, 28, 128),
            new Color(104, 83, 70));
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 64, baseY - 130, 28, 93),
            new Color(104, 83, 70));
        DrawLine(new Vector2(centerX - 16, baseY - 175),
            new Vector2(centerX + 30, baseY - 72), StoneDark, 5f);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 38, baseY - 213, 76, 22),
            RoyalGold);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 5, baseY - 238, 10, 30),
            RoyalGold);
    }

    private void DrawRoyalInsignia(int x, int y)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 55, y - 4, 110, 8), RoyalGold);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 5, y - 55, 10, 110), RoyalGold);
        DrawTriangleDown(x - 48, y + 7, 32, 27, RoyalGold);
        DrawTriangleDown(x + 16, y + 7, 32, 27, RoyalGold);
    }

    private void DrawBarracksBuilding(int x, int ground, int width, int height)
    {
        int top = ground - height;
        _spriteBatch.Draw(_pixel, new Rectangle(x, top + 35, width, height - 35),
            new Color(66, 63, 63));
        DrawJaggedRoof(x - 9, top, width + 18, x, new Color(61, 51, 45));
        DrawDoor(x + width / 2 - 38, ground - 105, 76, 105, false);
        DrawArrowSlit(x + 70, top + 105);
        DrawArrowSlit(x + width - 70, top + 105);
        DrawRoyalInsignia(x + width / 2, top + 92);
    }

    private void DrawGallows(int x, int ground, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, 14, height), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 14, ground - height, 14, height), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, width, 16), Wood);
        DrawLine(new Vector2(x + width / 2, ground - height + 15),
            new Vector2(x + width / 2, ground - height + 103),
            new Color(103, 88, 70), 4f);
        DrawChain(x + width - 48, ground - height + 15, 102, 7, 0f);
    }

    private void DrawExecutionPlatform(int x, int ground, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, width, 22), Wood);
        for (int leg = x + 18; leg < x + width; leg += width - 40)
            _spriteBatch.Draw(_pixel, new Rectangle(leg, ground - height + 20, 18, height - 20), Wood);
        for (int rail = x + 48; rail < x + width - 35; rail += 55)
            _spriteBatch.Draw(_pixel, new Rectangle(rail, ground - height - 35, 7, 36), Wood);
    }

    private void DrawCage(int x, int ground, int width, int height)
    {
        DrawOutline(new Rectangle(x, ground - height, width, height), 6, Iron);
        for (int bar = x + 18; bar < x + width; bar += 22)
            _spriteBatch.Draw(_pixel, new Rectangle(bar, ground - height, 5, height), Iron);
        DrawChain(x + width / 2, ground - height - 52, 52, 6, 0f);
    }

    private void DrawNoticeBoard(int x, int ground, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, width, height - 35), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 18, ground - height + 16, 43, 54),
            new Color(151, 132, 100));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 73, ground - height + 9, 51, 62),
            new Color(132, 116, 90));
        _spriteBatch.Draw(_pixel, new Rectangle(x + 18, ground - 35, 10, 35), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 28, ground - 35, 10, 35), Wood);
    }

    private void DrawFountain(int x, int ground)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 105, ground - 32, 210, 32),
            new Color(83, 80, 78));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 78, ground - 56, 156, 26),
            new Color(95, 91, 87));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 18, ground - 145, 36, 92),
            new Color(104, 99, 92));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 53, ground - 154, 106, 17),
            new Color(104, 99, 92));
        DrawLine(new Vector2(x - 12, ground - 128),
            new Vector2(x + 27, ground - 77), StoneDark, 4f);
    }

    private void DrawMonument(int x, int ground, bool mirrored)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 45, ground - 50, 90, 50), Stone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 32, ground - 115, 64, 67),
            new Color(91, 87, 83));
        int direction = mirrored ? -1 : 1;
        DrawLine(new Vector2(x, ground - 112),
            new Vector2(x + direction * 42, ground - 185),
            new Color(98, 94, 89), 16f);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 18, ground - 215, 36, 36),
            new Color(98, 94, 89));
        DrawLine(new Vector2(x + direction * 20, ground - 145),
            new Vector2(x + direction * 70, ground - 176), Iron, 6f);
    }

    private void DrawBrokenStatue(int x, int ground, bool facingRight)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 34, ground - 40, 68, 40), Stone);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 20, ground - 102, 40, 64),
            new Color(100, 96, 91));
        DrawLine(new Vector2(x, ground - 95),
            new Vector2(x + (facingRight ? 38 : -38), ground - 135),
            new Color(100, 96, 91), 13f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + (facingRight ? 38 : -58), ground - 28, 28, 20),
            new Color(96, 91, 87));
    }

    private void DrawColumn(int x, int ground, int height, bool broken)
    {
        int shown = broken ? height * 3 / 4 : height;
        _spriteBatch.Draw(_pixel, new Rectangle(x - 20, ground - shown, 40, shown),
            new Color(94, 89, 86));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 30, ground - shown, 60, 14), StoneEdge);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 32, ground - 18, 64, 18), StoneEdge);
        for (int groove = -12; groove <= 12; groove += 12)
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + groove, ground - shown + 18, 3, shown - 38),
                new Color(StoneDark, 70));
    }

    private void DrawWeaponRack(int x, int ground, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 92, width, 9), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 8, ground - 92, 8, 92), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 16, ground - 92, 8, 92), Wood);
        for (int weapon = x + 28; weapon < x + width - 12; weapon += 34)
        {
            DrawLine(new Vector2(weapon, ground - 105),
                new Vector2(weapon + 7, ground - 9), Iron, 4f);
            DrawTriangleDown(weapon - 3, ground - 115, 12, 18, StoneEdge);
        }
    }

    private void DrawShieldRack(int x, int ground, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 68, width, 9), Wood);
        for (int shield = x + 25; shield < x + width - 18; shield += 48)
            DrawShield(shield, ground - 40, 20, new Color(94, 43, 44));
    }

    private void DrawArmorStand(int x, int ground)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 5, ground - 122, 10, 122), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 35, ground - 103, 70, 10), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 25, ground - 92, 50, 64), Iron);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 20, ground - 132, 40, 33),
            new Color(72, 70, 70));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 25, ground - 136, 50, 7), RoyalGold);
    }

    private void DrawCeremonialBrazier(int x, int ground, float time, bool lit)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 25, ground - 64, 50, 16), Iron);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 6, ground - 49, 12, 49), Iron);
        if (lit)
            DrawFire(x, ground - 66, 22, time);
        else
            _spriteBatch.Draw(_pixel, new Rectangle(x - 17, ground - 70, 34, 5),
                new Color(79, 55, 43));
    }

    private void DrawCart(int x, int ground, bool overturned, bool charred = false)
    {
        Color wood = charred ? CharredWood : Wood;
        int bodyY = ground - (overturned ? 45 : 70);
        _spriteBatch.Draw(_pixel, new Rectangle(x, bodyY, 130, 42), wood);
        DrawWheel(x + 24, ground - 18, 24, charred);
        DrawWheel(x + 105, ground - 18, 24, charred);
        DrawLine(new Vector2(x + 125, bodyY + 14),
            new Vector2(x + 180, ground - (overturned ? 8 : 46)), wood, 7f);
    }

    private void DrawWheel(int x, int y, int radius, bool charred)
    {
        Color color = charred ? new Color(37, 34, 34) : new Color(65, 49, 39);
        DrawOutline(new Rectangle(x - radius, y - radius, radius * 2, radius * 2), 5, color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 3, y - radius, 6, radius * 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - radius, y - 3, radius * 2, 6), color);
    }

    private void DrawBarricade(int x, int ground, bool broken)
    {
        int count = broken ? 3 : 5;
        for (int beam = 0; beam < count; beam++)
        {
            int bx = x + beam * 31;
            DrawLine(new Vector2(bx, ground),
                new Vector2(bx + (beam % 2 == 0 ? 34 : -22), ground - 90),
                Wood, 10f);
        }
        DrawLine(new Vector2(x - 10, ground - 45),
            new Vector2(x + count * 32, ground - 48), Wood, 11f);
    }

    private void DrawScaffold(int x, int ground, int width, int height)
    {
        for (int post = x; post <= x + width; post += width)
            _spriteBatch.Draw(_pixel, new Rectangle(post, ground - height, 10, height), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, width, 13), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height / 2, width, 10), Wood);
        DrawLine(new Vector2(x, ground), new Vector2(x + width, ground - height), Wood, 7f);
        DrawLine(new Vector2(x + width, ground), new Vector2(x, ground - height), Wood, 7f);
    }

    private void DrawLadder(int x, int ground, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - height, 7, height), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 42, ground - height, 7, height), Wood);
        for (int y = ground - height + 16; y < ground; y += 24)
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, 49, 6), Wood);
    }

    private void DrawGuardPlatform(int x, int y, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 14), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 20, y, 10, 142), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 30, y, 10, 142), Wood);
        for (int rail = x; rail < x + width; rail += 45)
            _spriteBatch.Draw(_pixel, new Rectangle(rail, y - 34, 6, 35), Wood);
    }

    private void DrawIronFence(int x, int ground, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 72, width, 7), Iron);
        for (int post = x; post <= x + width; post += 34)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(post, ground - 100, 6, 100), Iron);
            DrawTriangleDown(post - 4, ground - 112, 14, 17, Iron);
        }
    }

    private void DrawCrowdRail(int x, int ground, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 54, width, 8), Wood);
        for (int post = x + 8; post < x + width; post += 50)
            _spriteBatch.Draw(_pixel, new Rectangle(post, ground - 62, 8, 62), Wood);
    }

    private void DrawCrateStack(int x, int ground, int count)
    {
        for (int index = 0; index < count; index++)
        {
            int size = 38 + (index & 1) * 8;
            int px = x + (index % 2) * 43;
            int py = ground - size - (index / 2) * 42;
            DrawCrate(new Rectangle(px, py, size, size));
        }
    }

    private void DrawCrate(Rectangle b)
    {
        _spriteBatch.Draw(_pixel, b, Wood);
        DrawOutline(b, 4, new Color(57, 43, 36));
        DrawLine(new Vector2(b.Left + 4, b.Top + 4),
            new Vector2(b.Right - 4, b.Bottom - 4), new Color(57, 43, 36), 3f);
        DrawLine(new Vector2(b.Right - 4, b.Top + 4),
            new Vector2(b.Left + 4, b.Bottom - 4), new Color(57, 43, 36), 3f);
    }

    private void DrawBarrels(int x, int ground, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int bx = x + i * 42;
            var barrel = new Rectangle(bx, ground - 58, 38, 58);
            _spriteBatch.Draw(_pixel, barrel, new Color(77, 55, 42));
            _spriteBatch.Draw(_pixel, new Rectangle(bx, ground - 49, 38, 5), Iron);
            _spriteBatch.Draw(_pixel, new Rectangle(bx, ground - 15, 38, 5), Iron);
        }
    }

    private void DrawBasket(int x, int ground)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 18, 36, 18),
            new Color(106, 80, 52));
        DrawOutline(new Rectangle(x + 5, ground - 32, 26, 22), 3,
            new Color(106, 80, 52));
    }

    private void DrawChair(int x, int ground)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 47, 42, 9), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x, ground - 83, 8, 83), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 34, ground - 47, 8, 47), Wood);
    }

    private void DrawScales(int x, int y)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 3, y - 65, 6, 65), Iron);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 38, y - 62, 76, 5), Iron);
        DrawLine(new Vector2(x - 32, y - 58), new Vector2(x - 43, y - 28), Iron, 2f);
        DrawLine(new Vector2(x + 32, y - 58), new Vector2(x + 43, y - 28), Iron, 2f);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 57, y - 29, 29, 5), RoyalGold);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 29, y - 29, 29, 5), RoyalGold);
    }

    private void DrawHangingSign(int x, int y, string ignoredLabel, float time)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 60, 8), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 52, y, 8, 80), Wood);
        float sway = MathF.Sin(time * .8f + x * .01f) * .045f;
        DrawLine(new Vector2(x + 8, y + 6), new Vector2(x + 10 + sway * 80, y + 36), Iron, 3f);
        var sign = new Rectangle(x - 20 + (int)(sway * 50), y + 34, 68, 42);
        _spriteBatch.Draw(_pixel, sign, new Color(83, 57, 42));
        DrawOutline(sign, 3, new Color(48, 39, 34));
        _spriteBatch.Draw(_pixel,
            new Rectangle(sign.X + 12, sign.Y + 13, sign.Width - 24, 4),
            new Color(142, 116, 70));
    }

    private void DrawClothesLine(int x1, int y1, int x2, int y2, float time)
    {
        DrawLine(new Vector2(x1, y1), new Vector2(x2, y2),
            new Color(63, 52, 45), 2f);
        for (int cloth = 0; cloth < 4; cloth++)
        {
            float amount = (cloth + 1) / 5f;
            int x = (int)MathHelper.Lerp(x1, x2, amount);
            int y = (int)MathHelper.Lerp(y1, y2, amount) + 5;
            int sway = (int)(MathF.Sin(time * .7f + cloth) * 3f);
            _spriteBatch.Draw(_pixel, new Rectangle(x + sway, y, 40, 40 + cloth * 4),
                cloth % 2 == 0 ? new Color(109, 74, 66) : new Color(113, 104, 88));
        }
    }

    private void DrawBanner(int x, int y, int width, int height, Color color, float time)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 5, y - 8, width + 10, 7), Iron);
        int segments = Math.Max(3, height / 24);
        int segmentHeight = height / segments;
        for (int segment = 0; segment < segments; segment++)
        {
            int sway = (int)MathF.Round(MathF.Sin(time * .65f + segment * .65f + x * .01f) *
                (2f + segment * .75f));
            int tear = segment == segments - 1 ? width / 4 : 0;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + sway, y + segment * segmentHeight,
                    Math.Max(7, width - tear), segmentHeight + 1), color);
        }
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + width / 2 - 2, y + 14, 4, Math.Max(6, height / 3)),
            new Color(RoyalGold, color.A));
    }

    private void DrawSignalFlag(int x, int y, float time)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 7, 190), Wood);
        int sway = (int)(MathF.Sin(time * .9f) * 5f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + 7, y + 12, 78 + sway, 43),
            new Color(128, 47, 42));
        DrawTriangleRight(x + 60 + sway, y + 12, 34, 43,
            new Color(128, 47, 42));
    }

    private void DrawFire(int x, int baseY, int size, float time)
    {
        int flicker = (int)MathF.Round(MathF.Sin(time * 8f + x * .03f) * 4f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - size, baseY - size - flicker, size * 2, size + flicker),
            new Color(Ember, 195));
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - size / 2, baseY - size * 2 - flicker,
                size, size + 6), new Color(Flame, 215));
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - size * 3, baseY - size * 2,
                size * 6, size * 3), new Color(210, 94, 41, 18));
    }

    private void DrawSmokeColumn(int x, int y, float time, int seed)
    {
        for (int puff = 0; puff < 6; puff++)
        {
            float lift = PositiveModulo((int)(time * 12f + puff * 39 + seed * 17), 210);
            int drift = (int)(MathF.Sin(time * .3f + puff) * 22f);
            int size = 42 + puff * 8;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + drift - size / 2, y - (int)lift, size, size / 2),
                new Color(Smoke, 62 - puff * 5));
        }
    }

    private void DrawTransition(Rectangle corridor)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(corridor.Left, corridor.Top + 318,
                corridor.Width, corridor.Height - 318), new Color(58, 56, 57));
        _spriteBatch.Draw(_pixel,
            new Rectangle(corridor.Left, corridor.Top + 314, corridor.Width, 8),
            new Color(89, 81, 76));
    }

    private void DrawTimberFrame(int x, int y, int width, int height)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 10), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 10, height), Wood);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 10, y, 10, height), Wood);
        DrawLine(new Vector2(x + 8, y + 15),
            new Vector2(x + width - 8, y + height - 8), Wood, 7f);
        DrawLine(new Vector2(x + width - 8, y + 15),
            new Vector2(x + 8, y + height - 8), Wood, 7f);
    }

    private void DrawJaggedRoof(int x, int y, int width, int seed, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 15), color);
        for (int piece = 0; piece < width; piece += 34)
        {
            int drop = 8 + Math.Abs(Hash(seed + piece, 7) % 18);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + piece, y + 12, Math.Min(29, width - piece), drop), color);
        }
    }

    private void DrawStoneCourses(Rectangle bounds)
    {
        for (int y = bounds.Top + 34, row = 0; y < bounds.Bottom; y += 40, row++)
        {
            _spriteBatch.Draw(_pixel,
                new Rectangle(bounds.Left, y, bounds.Width, 2),
                new Color(StoneDark, 90));
            int offset = row % 2 == 0 ? 26 : 68;
            for (int x = bounds.Left + offset; x < bounds.Right; x += 86)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, y - 38, 2, 38), new Color(StoneDark, 72));
        }
    }

    private void DrawArrowSlit(int x, int y)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 5, y, 10, 42),
            new Color(31, 31, 34));
        _spriteBatch.Draw(_pixel, new Rectangle(x - 15, y + 16, 30, 8),
            new Color(31, 31, 34));
    }

    private void DrawWindow(int x, int y, bool damaged)
    {
        var frame = new Rectangle(x, y, 34, 48);
        _spriteBatch.Draw(_pixel, frame, new Color(39, 37, 38));
        DrawOutline(frame, 4, Wood);
        if (!damaged)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + 15, y + 4, 4, 40), Wood);
            _spriteBatch.Draw(_pixel, new Rectangle(x + 4, y + 21, 26, 4), Wood);
        }
        else
        {
            DrawLine(new Vector2(x + 4, y + 5),
                new Vector2(x + 29, y + 42), new Color(69, 54, 45), 3f);
        }
    }

    private void DrawDoor(int x, int y, int width, int height, bool damaged)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height),
            damaged ? new Color(48, 42, 39) : new Color(72, 52, 41));
        DrawOutline(new Rectangle(x, y, width, height), 5, Wood);
        if (damaged)
            DrawLine(new Vector2(x + 5, y + 10),
                new Vector2(x + width - 4, y + height - 6), CharredWood, 4f);
        else
            _spriteBatch.Draw(_pixel, new Rectangle(x + width - 14, y + height / 2, 5, 5),
                RoyalGold);
    }

    private void DrawBalcony(int x, int y, int width, bool broken)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 14), StoneEdge);
        for (int rail = x + 8; rail < x + width - 6; rail += 24)
        {
            if (broken && rail > x + width * 2 / 3)
                continue;
            _spriteBatch.Draw(_pixel, new Rectangle(rail, y - 44, 5, 45), Iron);
        }
        _spriteBatch.Draw(_pixel, new Rectangle(x, y - 43, width, 5), Iron);
    }

    private void DrawShield(int x, int y, int radius, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - radius, y - radius, radius * 2, radius * 2), color);
        DrawTriangleDown(x - radius, y + radius - 2, radius * 2, radius, color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 3, y - radius + 4, 6, radius * 2),
            RoyalGold);
    }

    private void DrawChain(int x, int y, int length, int linkSize, float sway)
    {
        int links = Math.Max(1, length / (linkSize + 2));
        for (int link = 0; link < links; link++)
        {
            int px = x + (int)(MathF.Sin(link * .8f) * sway);
            DrawOutline(new Rectangle(px - linkSize / 2,
                y + link * (linkSize + 2), linkSize, linkSize + 3), 2, Iron);
        }
    }

    private void DrawRoadSlab(int x, int y, int width)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 24),
            new Color(91, 84, 79));
        DrawOutline(new Rectangle(x, y, width, 24), 2, new Color(62, 58, 57));
    }

    private void DrawBrokenTower(int centerX, int top, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - width / 2, top - height + 35, width, height), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX - width / 2, top - height + 8, width / 3, 32), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(centerX + width / 6, top - height + 22, width / 3, 20), color);
    }

    private void DrawArcMarks(int x, int y, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 48, y - 15, 14, 4), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 34, y - 15, 14, 4), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - 58, y - 29, 12, 4), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 46, y - 29, 12, 4), color);
    }

    private void DrawTriangleDown(int x, int y, int width, int height, Color color)
    {
        int rows = Math.Max(1, height / 3);
        for (int row = 0; row < rows; row++)
        {
            int inset = row * width / (rows * 2);
            int drawWidth = Math.Max(1, width - inset * 2);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + inset, y + row * 3, drawWidth, 3), color);
        }
    }

    private void DrawTriangleRight(int x, int y, int width, int height, Color color)
    {
        int columns = Math.Max(1, width / 3);
        for (int column = 0; column < columns; column++)
        {
            int inset = column * height / (columns * 2);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + column * 3, y + inset, 3,
                    Math.Max(1, height - inset * 2)), color);
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(length, width), SpriteEffects.None, 0f);
    }

    private void DrawOutline(Rectangle r, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(r.Left, r.Top, r.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Left, r.Bottom - thickness, r.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Left, r.Top, thickness, r.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Top, thickness, r.Height), color);
    }

    private static FallenCityZoneDefinition FindZone(DungeonMap map, int x)
    {
        foreach (FallenCityZoneDefinition zone in map.FallenCityZones)
            if (x >= zone.Bounds.Left && x < zone.Bounds.Right)
                return zone;
        return null;
    }

    private static int Hash(int value, int salt)
    {
        unchecked
        {
            int hash = value * 374761393 + salt * 668265263;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            return hash ^ (hash >> 16);
        }
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
