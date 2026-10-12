using System;
using DungeonAscendant.Core;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Deterministic, code-only presentation for the Demon Lands. All scenery is
/// decorative; authored platforms remain the only source of collision.
/// </summary>
public sealed class DemonLandsEnvironmentRenderer
{
    private static readonly Color SkyTop = new(20, 7, 14);
    private static readonly Color SkyBottom = new(70, 25, 21);
    private static readonly Color FarRock = new(31, 20, 24);
    private static readonly Color MidRock = new(48, 27, 28);
    private static readonly Color Rock = new(68, 37, 32);
    private static readonly Color RockEdge = new(126, 59, 39);
    private static readonly Color Charcoal = new(35, 28, 28);
    private static readonly Color Ash = new(156, 143, 132);
    private static readonly Color Bone = new(174, 159, 132);
    private static readonly Color BoneDark = new(91, 78, 66);
    private static readonly Color Iron = new(67, 57, 55);
    private static readonly Color Cloth = new(100, 31, 35);
    private static readonly Color Ember = new(220, 73, 33);
    private static readonly Color Lava = new(234, 91, 31);
    private static readonly Color Rift = new(155, 58, 187);
    private static readonly Color RiftCore = new(226, 99, 213);
    private static readonly Color Corruption = new(168, 38, 81);
    private static readonly Color Shrine = new(108, 190, 172);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public DemonLandsEnvironmentRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void DrawWorld(GameSession session, Rectangle camera, GameTime gameTime)
    {
        DungeonMap map = session.CurrentDungeon;
        if (map?.IsDemonLands != true)
            return;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        DrawSky(camera);
        DrawFarParallax(map, camera, time);
        DrawMidParallax(map, camera);

        foreach (DemonLandsZoneDefinition zone in map.DemonLandsZones)
        {
            if (!zone.Bounds.Intersects(camera))
                continue;
            DrawZoneBackdrop(zone);
            DrawZoneIdentity(zone, time);
        }

        foreach (Rectangle corridor in map.Corridors)
            if (corridor.Intersects(camera))
                DrawTransition(corridor);

        foreach (Platform platform in map.Platforms)
            if (platform.Bounds.Intersects(camera))
                DrawPlatform(platform);

        foreach (CorruptionFieldDefinition field in map.CorruptionFields)
        {
            if (!field.Bounds.Intersects(camera) ||
                (field.AnchorId.Length > 0 &&
                 !session.CorruptionAnchors.IsActive(field.AnchorId)))
                continue;
            DrawCorruptionField(field, time);
        }

        foreach (PurificationShrineDefinition shrine in map.PurificationShrines)
        {
            var cull = new Rectangle((int)shrine.Position.X - 86,
                (int)shrine.Position.Y - 130, 172, 140);
            if (cull.Intersects(camera))
                DrawShrine(shrine, time);
        }

        foreach (CorruptionAnchorRuntime anchor in
            session.CorruptionAnchors.States.Values)
        {
            var cull = new Rectangle((int)anchor.Definition.Position.X - 100,
                (int)anchor.Definition.Position.Y - 150, 200, 170);
            if (cull.Intersects(camera))
                DrawAnchor(anchor, time);
        }

        DrawAtmosphere(map, camera, time);
    }

    public void DrawCorruptionOverlay(
        GameSession session,
        Rectangle screen,
        GameTime gameTime)
    {
        if (session.CurrentDungeon?.IsDemonLands != true ||
            session.Corruption.State == DemonicCorruptionState.Stable)
            return;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        int level = (int)session.Corruption.State;
        float pulse = .82f + MathF.Sin(time * (1.2f + level * .17f)) * .12f;
        byte alpha = (byte)((12 + level * 8) * pulse);
        Color edge = session.Corruption.State == DemonicCorruptionState.Overrun
            ? new Color(181, 31, 52, (int)alpha)
            : new Color(128, 39, 136, (int)alpha);
        int thickness = 12 + level * 6;

        _spriteBatch.Draw(_pixel,
            new Rectangle(screen.Left, screen.Top, screen.Width, thickness), edge);
        _spriteBatch.Draw(_pixel,
            new Rectangle(screen.Left, screen.Bottom - thickness,
                screen.Width, thickness), edge);
        _spriteBatch.Draw(_pixel,
            new Rectangle(screen.Left, screen.Top, thickness, screen.Height), edge);
        _spriteBatch.Draw(_pixel,
            new Rectangle(screen.Right - thickness, screen.Top,
                thickness, screen.Height), edge);

        int tendrils = 2 + level;
        for (int index = 0; index < tendrils; index++)
        {
            int seed = Hash(index, level * 37);
            bool fromLeft = (index & 1) == 0;
            float y = screen.Top + 70 + PositiveModulo(seed, Math.Max(80,
                screen.Height - 140));
            float x = fromLeft ? screen.Left + thickness : screen.Right - thickness;
            float direction = fromLeft ? 1f : -1f;
            Vector2 start = new(x, y);
            Vector2 middle = start + new Vector2(direction * (34 + level * 8),
                -12 + PositiveModulo(seed / 7, 25));
            Vector2 end = middle + new Vector2(direction * (24 + level * 5),
                -9 + PositiveModulo(seed / 13, 19));
            DrawLine(start, middle, edge * .75f, 2f);
            DrawLine(middle, end, edge * .48f, 2f);
        }
    }

    private void DrawSky(Rectangle camera)
    {
        int bandHeight = Math.Max(1, camera.Height / 6);
        for (int band = 0; band < 6; band++)
        {
            float amount = band / 5f;
            Color color = Color.Lerp(SkyTop, SkyBottom, amount);
            _spriteBatch.Draw(_pixel,
                new Rectangle(camera.Left, camera.Top + band * bandHeight,
                    camera.Width,
                    band == 5 ? camera.Height - band * bandHeight : bandHeight),
                color);
        }
    }

    private void DrawFarParallax(DungeonMap map, Rectangle camera, float time)
    {
        int horizon = map.WorldBounds.Top + 410;
        int first = (int)MathF.Floor(camera.Left * .13f / 260f) - 2;
        int last = first + camera.Width / 260 + 7;
        for (int index = first; index <= last; index++)
        {
            int seed = Hash(index, 11);
            int x = (int)MathF.Round(index * 260f + camera.Left * .87f);
            int width = 210 + Math.Abs(seed % 105);
            int height = 130 + Math.Abs(seed / 13 % 170);
            DrawJaggedMass(x, horizon, width, height, seed, FarRock);
            if ((seed & 3) == 0)
            {
                DrawLine(new Vector2(x + width * .62f, horizon - height + 38),
                    new Vector2(x + width * .56f, horizon - height - 86),
                    new Color(45, 27, 29), 13f);
                DrawLine(new Vector2(x + width * .56f, horizon - height - 76),
                    new Vector2(x + width * .42f, horizon - height - 125),
                    new Color(45, 27, 29), 8f);
            }
        }

        for (int column = 0; column < 4; column++)
        {
            int anchor = map.WorldBounds.Left + 1100 + column * 3500;
            int x = (int)MathF.Round(anchor + camera.Left * .18f);
            for (int puff = 0; puff < 5; puff++)
            {
                int phase = PositiveModulo((int)(time * 9f + puff * 51 + column * 23),
                    220);
                int drift = (int)MathF.Sin(time * .22f + puff) * 18;
                int size = 50 + puff * 13;
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x + drift - size / 2,
                        horizon - 80 - phase - puff * 23, size, size / 2),
                    new Color(48, 37, 39, 36 + puff * 4));
            }
        }
    }

    private void DrawMidParallax(DungeonMap map, Rectangle camera)
    {
        int baseY = map.WorldBounds.Top + 530;
        int first = (int)MathF.Floor(camera.Left * .36f / 190f) - 2;
        int last = first + camera.Width / 190 + 8;
        for (int index = first; index <= last; index++)
        {
            int seed = Hash(index, 43);
            int x = (int)MathF.Round(index * 190f + camera.Left * .64f);
            int width = 150 + Math.Abs(seed % 70);
            int height = 75 + Math.Abs(seed / 17 % 115);
            DrawJaggedMass(x, baseY, width, height, seed, MidRock);
            if ((seed & 7) == 1)
                DrawDistantRift(new Vector2(x + width / 2f, baseY - height - 18f),
                    55f + Math.Abs(seed % 35));
        }
    }

    private void DrawZoneBackdrop(DemonLandsZoneDefinition zone)
    {
        Color wash = zone.Kind switch
        {
            DemonLandsZoneKind.AshWastes => new Color(79, 69, 63, 62),
            DemonLandsZoneKind.FleshRavine => new Color(72, 27, 39, 68),
            DemonLandsZoneKind.RiftFields => new Color(63, 28, 75, 62),
            DemonLandsZoneKind.BoneForest => new Color(86, 78, 65, 55),
            DemonLandsZoneKind.InfernalQuarry => new Color(84, 40, 28, 58),
            DemonLandsZoneKind.BloodShrine => new Color(91, 20, 35, 62),
            DemonLandsZoneKind.LegionCamp => new Color(73, 30, 28, 57),
            DemonLandsZoneKind.GateOfHorns => new Color(82, 35, 25, 64),
            DemonLandsZoneKind.InfernalHeart => new Color(91, 23, 34, 72),
            _ => new Color(64, 35, 31, 54)
        };
        int top = zone.Bounds.Top + 150;
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, top, zone.Bounds.Width,
                Math.Max(1, zone.GroundY - top)), wash);
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left, zone.GroundY - 7,
                zone.Bounds.Width, 7), new Color(RockEdge, 92));
    }

    private void DrawZoneIdentity(DemonLandsZoneDefinition zone, float time)
    {
        switch (zone.Kind)
        {
            case DemonLandsZoneKind.BlackenedFrontier:
                DrawFrontier(zone, time);
                break;
            case DemonLandsZoneKind.AshWastes:
                DrawAshWastes(zone, time);
                break;
            case DemonLandsZoneKind.FleshRavine:
                DrawFleshRavine(zone, time);
                break;
            case DemonLandsZoneKind.RiftFields:
                DrawRiftFields(zone, time);
                break;
            case DemonLandsZoneKind.BoneForest:
                DrawBoneForest(zone);
                break;
            case DemonLandsZoneKind.InfernalQuarry:
                DrawQuarry(zone, time);
                break;
            case DemonLandsZoneKind.BloodShrine:
                DrawBloodShrine(zone, time);
                break;
            case DemonLandsZoneKind.LegionCamp:
                DrawLegionCamp(zone, time);
                break;
            case DemonLandsZoneKind.GateOfHorns:
                DrawGateOfHorns(zone, time);
                break;
            case DemonLandsZoneKind.InfernalHeart:
                DrawInfernalHeart(zone, time);
                break;
        }
    }

    private void DrawFrontier(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        DrawDeadTree(zone.Bounds.Left + 115, g, 150, false);
        DrawDeadTree(zone.Bounds.Left + 930, g, 185, true);
        DrawBrokenMarker(zone.Bounds.Left + 175, g, -1);
        DrawBrokenMarker(zone.Bounds.Left + 520, g, 1);
        DrawBanner(zone.Bounds.Left + 705, g - 205, 38, 106, time,
            new Color(Cloth, 180));
        for (int i = 0; i < 4; i++)
            DrawGroundCrack(new Vector2(zone.Bounds.Left + 310 + i * 155,
                g - 3), 26 + i * 5, new Color(Ember, 80));
    }

    private void DrawAshWastes(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        DrawRibCage(zone.Bounds.Left + 180, g, 150, 135, BoneDark * .72f);
        DrawBonePillar(zone.Bounds.Left + 700, g, 205, true);
        DrawBonePillar(zone.Bounds.Left + 1080, g, 130, false);
        DrawSpearDebris(zone.Bounds.Left + 555, g);
        DrawSpearDebris(zone.Bounds.Left + 940, g);
        DrawSmokeColumn(zone.Bounds.Left + 865, g - 65, time, 7);
    }

    private void DrawFleshRavine(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left + 100, g - 14,
                zone.Bounds.Width - 200, zone.Bounds.Bottom - g + 14),
            new Color(21, 12, 18));
        for (int i = 0; i < 6; i++)
        {
            int x = zone.Bounds.Left + 135 + i * 180;
            Color vein = i % 2 == 0
                ? new Color(122, 37, 64, 110)
                : new Color(82, 38, 62, 92);
            DrawLine(new Vector2(x, g - 12),
                new Vector2(x + (i % 2 == 0 ? 50 : -42), g - 165), vein, 7f);
            DrawLine(new Vector2(x + (i % 2 == 0 ? 50 : -42), g - 165),
                new Vector2(x + (i % 2 == 0 ? 92 : -76), g - 215), vein, 3f);
        }
        DrawBoneBridge(zone.Bounds.Left + 340, g - 118, 420);
        float glow = .68f + MathF.Sin(time * 1.4f) * .12f;
        _spriteBatch.Draw(_pixel,
            new Rectangle(zone.Bounds.Left + 175, g + 47,
                zone.Bounds.Width - 350, 11), new Color(158, 34, 73) * glow);
    }

    private void DrawRiftFields(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        DrawWorldRift(new Vector2(zone.Bounds.Left + 445, g - 125), 88f, time);
        DrawWorldRift(new Vector2(zone.Bounds.Left + 970, g - 182), 112f, time + .7f);
        for (int i = 0; i < 7; i++)
        {
            int seed = Hash(i, zone.Bounds.X);
            float bob = MathF.Sin(time * .7f + i) * 4f;
            float x = zone.Bounds.Left + 120 + i * 155;
            float y = g - 95 - PositiveModulo(seed, 155) + bob;
            DrawRockShard(new Vector2(x, y), 12 + Math.Abs(seed % 17),
                9 + Math.Abs(seed / 7 % 14), MidRock);
        }
        DrawPortalRemnant(zone.Bounds.Left + 690, g, 165);
    }

    private void DrawBoneForest(DemonLandsZoneDefinition zone)
    {
        int g = zone.GroundY;
        for (int i = 0; i < 5; i++)
        {
            int x = zone.Bounds.Left + 95 + i * 250;
            DrawRibArch(x, g, 155 + i % 2 * 45, i % 2 == 0);
        }
        DrawSpineColumn(zone.Bounds.Left + 610, g, 250);
        DrawSpineColumn(zone.Bounds.Left + 1110, g, 190);
        for (int i = 0; i < 5; i++)
            DrawBonePile(zone.Bounds.Left + 160 + i * 220, g, i);
    }

    private void DrawQuarry(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        for (int i = 0; i < 5; i++)
            DrawCutBlock(zone.Bounds.Left + 90 + i * 220, g,
                90 + i % 2 * 25, 48 + i % 3 * 15);
        DrawCrane(zone.Bounds.Left + 560, g, 270, time);
        DrawMineCart(zone.Bounds.Left + 1030, g);
        DrawLavaSeam(zone.Bounds.Left + 780, g - 2, 210, time);
        DrawFallingDebris(zone.Bounds.Left + 1180, g - 210, time);
    }

    private void DrawBloodShrine(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        DrawRitualPillar(zone.Bounds.Left + 165, g, 215);
        DrawRitualPillar(zone.Bounds.Right - 165, g, 215);
        DrawRitualAltar(new Vector2(zone.Bounds.Center.X, g), time);
        DrawGroundRune(new Vector2(zone.Bounds.Center.X, g - 5), 190,
            new Color(Corruption, 135));
        for (int i = 0; i < 4; i++)
            DrawLine(new Vector2(zone.Bounds.Center.X - 150 + i * 100, g - 3),
                new Vector2(zone.Bounds.Center.X - 205 + i * 136, g - 28),
                new Color(118, 24, 45, 130), 4f);
        DrawBanner(zone.Bounds.Left + 300, g - 240, 42, 120, time, Cloth);
        DrawBanner(zone.Bounds.Right - 340, g - 240, 42, 120, time + .4f, Cloth);
    }

    private void DrawLegionCamp(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        DrawTent(zone.Bounds.Left + 175, g, 190, 120);
        DrawTent(zone.Bounds.Left + 830, g, 235, 145);
        DrawWeaponRack(zone.Bounds.Left + 480, g);
        DrawCage(zone.Bounds.Left + 690, g, 100, 125);
        DrawSiegeFrame(zone.Bounds.Left + 1170, g);
        DrawCampFire(zone.Bounds.Left + 380, g - 2, time);
        DrawBanner(zone.Bounds.Left + 1040, g - 245, 46, 132, time, Cloth);
    }

    private void DrawGateOfHorns(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        int center = zone.Bounds.Center.X;
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - 205, g - 390, 410, 390),
            new Color(36, 25, 27));
        DrawOutline(new Rectangle(center - 205, g - 390, 410, 390),
            13, new Color(88, 46, 38));
        DrawHorn(new Vector2(center - 205, g - 355), -1f, 265f);
        DrawHorn(new Vector2(center + 205, g - 355), 1f, 265f);
        DrawChain(center - 132, g - 380, 270, 12, 3f);
        DrawChain(center + 132, g - 380, 270, 12, 3f);
        DrawGroundRune(new Vector2(center, g - 8), 150,
            new Color(Ember, 112));
        DrawLavaSeam(zone.Bounds.Left + 65, g - 2, 160, time);
        DrawLavaSeam(zone.Bounds.Right - 225, g - 2, 160, time + .3f);
    }

    private void DrawInfernalHeart(DemonLandsZoneDefinition zone, float time)
    {
        int g = zone.GroundY;
        int center = zone.Bounds.Center.X;
        DrawRitualPillar(zone.Bounds.Left + 190, g, 300);
        DrawRitualPillar(zone.Bounds.Right - 190, g, 300);
        DrawPortalRemnant(center - 390, g, 245);
        float beat = .88f + MathF.Sin(time * 2.1f) * .12f;
        DrawDiamond(new Vector2(center, g - 260), 78f * beat,
            new Color(205, 37, 58, 190));
        DrawDiamond(new Vector2(center, g - 260), 49f * beat,
            new Color(249, 91, 49, 220));
        for (int i = 0; i < 8; i++)
        {
            float angle = MathHelper.TwoPi * i / 8f + time * .08f;
            Vector2 p = new(center + MathF.Cos(angle) * 205f,
                g - 250 + MathF.Sin(angle) * 92f);
            DrawRockShard(p, 19 + i % 3 * 7, 13 + i % 2 * 8, MidRock);
        }
        for (int i = 0; i < 5; i++)
            DrawGroundCrack(new Vector2(center - 300 + i * 150, g - 3),
                65, new Color(Ember, 135));
    }

    private void DrawTransition(Rectangle corridor)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(corridor.Left, corridor.Top + 385,
                corridor.Width, corridor.Height - 385), Rock);
        _spriteBatch.Draw(_pixel,
            new Rectangle(corridor.Left, corridor.Top + 380,
                corridor.Width, 7), RockEdge);
    }

    private void DrawPlatform(Platform platform)
    {
        Color fill = platform.Kind == PlatformKind.Raised
            ? new Color(86, 46, 38)
            : Rock;
        _spriteBatch.Draw(_pixel, platform.Bounds, fill);
        _spriteBatch.Draw(_pixel,
            new Rectangle(platform.Bounds.Left, platform.Bounds.Top,
                platform.Bounds.Width, Math.Min(6, platform.Bounds.Height)),
            RockEdge);
        if (platform.Bounds.Height > 24)
        {
            int step = 58;
            for (int x = platform.Bounds.Left + 24;
                x < platform.Bounds.Right; x += step)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, platform.Bounds.Top + 12, 2,
                        Math.Min(28, platform.Bounds.Height - 12)),
                    new Color(Charcoal, 95));
        }
    }

    private void DrawCorruptionField(CorruptionFieldDefinition field, float time)
    {
        Color color = field.IsRift ? Rift : Corruption;
        _spriteBatch.Draw(_pixel, field.Bounds, new Color(color, 24));
        DrawOutline(field.Bounds, 2, new Color(color, 105));
        int segments = Math.Max(2, field.Bounds.Width / 48);
        for (int i = 0; i < segments; i++)
        {
            int seed = Hash(field.Bounds.X, i);
            float x = field.Bounds.Left + (i + .5f) * field.Bounds.Width / segments;
            float wave = MathF.Sin(time * 2f + i * .7f) * 3f;
            Vector2 a = new(x - 13, field.Bounds.Center.Y + wave);
            Vector2 b = new(x, field.Bounds.Top + 5);
            Vector2 c = new(x + 11, field.Bounds.Bottom - 4);
            DrawLine(a, b, new Color(color, 145), field.IsRift ? 3f : 2f);
            DrawLine(b, c, new Color(color, 105), 2f);
            if (field.IsRift && (seed & 1) == 0)
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)x - 2, field.Bounds.Top - 8, 4, 4),
                    new Color(RiftCore, 130));
        }
    }

    private void DrawShrine(PurificationShrineDefinition shrine, float time)
    {
        Vector2 p = shrine.Position;
        DrawGroundRune(p + new Vector2(0f, -3f), shrine.Radius * .64f,
            new Color(Shrine, 105));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 22, (int)p.Y - 72, 44, 72), BoneDark);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 15, (int)p.Y - 84, 30, 18), Bone);
        float pulse = .8f + MathF.Sin(time * 2f + p.X * .01f) * .18f;
        DrawDiamond(p + new Vector2(0f, -99f), 17f,
            new Color(Shrine, 230) * pulse);
        DrawLine(p + new Vector2(-42f, -4f), p + new Vector2(-25f, -50f),
            Bone, 6f);
        DrawLine(p + new Vector2(42f, -4f), p + new Vector2(25f, -50f),
            Bone, 6f);
    }

    private void DrawAnchor(CorruptionAnchorRuntime anchor, float time)
    {
        Vector2 p = anchor.Definition.Position;
        bool active = anchor.IsActive;
        Color core = anchor.IsTemporaryBossAnchor ? Rift : Corruption;
        Color edge = anchor.IsTemporaryBossAnchor ? RiftCore : new Color(235, 83, 103);
        if (!active)
        {
            DrawRockShard(p + new Vector2(-25f, -6f), 34, 18, new Color(61, 49, 49));
            DrawRockShard(p + new Vector2(12f, -3f), 26, 14, new Color(72, 55, 52));
            DrawLine(p + new Vector2(-9f, -20f), p + new Vector2(18f, -66f),
                new Color(92, 70, 65), 9f);
            DrawGroundRune(p - new Vector2(0f, 3f), 56f,
                new Color(80, 68, 67, 55));
            return;
        }

        DrawGroundRune(p - new Vector2(0f, 3f),
            64f + MathF.Sin(time * 1.7f) * 3f, new Color(core, 95));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 27, (int)p.Y - 76, 54, 76),
            new Color(49, 31, 35));
        DrawOutline(new Rectangle((int)p.X - 27, (int)p.Y - 76, 54, 76),
            4, new Color(edge, 190));
        DrawHorn(p + new Vector2(-22f, -64f), -1f, 56f);
        DrawHorn(p + new Vector2(22f, -64f), 1f, 56f);
        float pulse = .84f + MathF.Sin(time * 3.1f + p.X * .01f) * .14f;
        DrawDiamond(p + new Vector2(0f, -43f), 18f,
            new Color(edge, 235) * pulse);

        if (anchor.HealthRatio < .7f)
        {
            DrawLine(p + new Vector2(-20f, -70f), p + new Vector2(4f, -47f),
                Charcoal, 3f);
            DrawLine(p + new Vector2(4f, -47f), p + new Vector2(-9f, -24f),
                Charcoal, 3f);
        }
        if (anchor.HealthRatio < .35f)
            DrawRockShard(p + new Vector2(36f, -3f), 19, 11, Rock);

        if (anchor.IsBlessed)
        {
            Color blessed = new(247, 174, 64, 180);
            DrawGroundRune(p + new Vector2(0f, -42f), 39f, blessed);
            DrawDiamond(p + new Vector2(0f, -43f), 28f, blessed);
        }

        int healthWidth = (int)MathF.Round(48f * anchor.HealthRatio);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 24, (int)p.Y - 88, 48, 4),
            new Color(42, 25, 29));
        if (healthWidth > 0)
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)p.X - 24, (int)p.Y - 88, healthWidth, 4),
                edge);
        if (anchor.IsTemporaryBossAnchor)
        {
            int lifetime = (int)MathF.Round(48f * Math.Clamp(
                anchor.LifetimeRemaining /
                    CorruptionAnchorSystem.BossAnchorLifetimeSeconds, 0f, 1f));
            if (lifetime > 0)
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)p.X - 24, (int)p.Y + 5, lifetime, 3),
                    RiftCore);
        }
    }

    private void DrawAtmosphere(DungeonMap map, Rectangle camera, float time)
    {
        DemonLandsZoneDefinition zone = FindZone(map, camera.Center.X);
        if (zone == null)
            return;
        bool pale = zone.Kind == DemonLandsZoneKind.BoneForest;
        bool ashHeavy = zone.Kind is DemonLandsZoneKind.AshWastes or
            DemonLandsZoneKind.InfernalQuarry;
        int spacing = ashHeavy ? 74 : 112;
        int first = camera.Left - PositiveModulo(camera.Left, spacing) - spacing;
        for (int x = first; x < camera.Right + spacing; x += spacing)
        {
            int seed = Hash(x / spacing, (int)zone.Kind * 31);
            float drift = PositiveModulo((int)(time * (7 + Math.Abs(seed % 6))),
                spacing);
            int px = (int)(x + drift);
            int py = camera.Top + 55 + PositiveModulo(seed,
                Math.Max(100, camera.Height - 130));
            int size = 1 + Math.Abs(seed % 3);
            Color mote = pale
                ? new Color(210, 202, 181, 48)
                : ashHeavy
                    ? new Color(Ash, 64)
                    : new Color(219, 86, 48, 43);
            _spriteBatch.Draw(_pixel, new Rectangle(px, py, size + 1, size), mote);
        }

        if (pale)
            _spriteBatch.Draw(_pixel,
                new Rectangle(camera.Left, zone.GroundY - 115,
                    camera.Width, 58), new Color(188, 181, 165, 18));
    }

    private void DrawDeadTree(int x, int ground, int height, bool flip)
    {
        float s = flip ? -1f : 1f;
        DrawLine(new Vector2(x, ground), new Vector2(x + s * 12, ground - height),
            new Color(44, 31, 29), 13f);
        DrawLine(new Vector2(x + s * 8, ground - height * .58f),
            new Vector2(x + s * 54, ground - height * .83f),
            new Color(44, 31, 29), 8f);
        DrawLine(new Vector2(x + s * 7, ground - height * .75f),
            new Vector2(x - s * 38, ground - height * .96f),
            new Color(44, 31, 29), 7f);
    }

    private void DrawBrokenMarker(int x, int ground, int direction)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 21, ground - 11, 42, 11),
            Charcoal);
        DrawLine(new Vector2(x, ground - 8),
            new Vector2(x + direction * 16, ground - 82), BoneDark, 12f);
        DrawLine(new Vector2(x + direction * 12, ground - 61),
            new Vector2(x + direction * 41, ground - 53), BoneDark, 7f);
    }

    private void DrawRibCage(int x, int ground, int width, int height, Color color)
    {
        DrawLine(new Vector2(x, ground), new Vector2(x, ground - height), color, 9f);
        for (int i = 0; i < 5; i++)
        {
            float y = ground - height + 22 + i * 22;
            DrawLine(new Vector2(x, y),
                new Vector2(x - width / 2f + i * 5f, y + 45f), color, 6f);
            DrawLine(new Vector2(x, y),
                new Vector2(x + width / 2f - i * 5f, y + 45f), color, 6f);
        }
    }

    private void DrawBonePillar(int x, int ground, int height, bool broken)
    {
        int h = broken ? height - 45 : height;
        DrawLine(new Vector2(x, ground), new Vector2(x, ground - h), BoneDark, 24f);
        for (int y = ground - 28; y > ground - h; y -= 34)
            DrawDiamond(new Vector2(x, y), 16f, new Color(Bone, 125));
        if (broken)
            DrawBonePile(x + 34, ground, 3);
    }

    private void DrawSpearDebris(int x, int ground)
    {
        DrawLine(new Vector2(x - 35, ground - 4), new Vector2(x + 38, ground - 86),
            Iron, 4f);
        DrawLine(new Vector2(x + 26, ground - 3), new Vector2(x - 13, ground - 68),
            BoneDark, 4f);
        DrawDiamond(new Vector2(x + 40, ground - 88), 7f, Iron);
    }

    private void DrawBoneBridge(int x, int y, int width)
    {
        DrawLine(new Vector2(x, y), new Vector2(x + width, y + 12), BoneDark, 14f);
        for (int i = 0; i <= 8; i++)
        {
            float px = x + width * i / 8f;
            DrawLine(new Vector2(px, y + 4),
                new Vector2(px - 14, y + 42 + i % 2 * 10), BoneDark, 5f);
        }
    }

    private void DrawWorldRift(Vector2 center, float height, float time)
    {
        float pulse = 1f + MathF.Sin(time * 2.2f + center.X * .01f) * .05f;
        Vector2 a = center + new Vector2(-7f, -height * .5f * pulse);
        Vector2 b = center + new Vector2(8f, -height * .15f);
        Vector2 c = center + new Vector2(-5f, height * .16f);
        Vector2 d = center + new Vector2(7f, height * .5f * pulse);
        DrawLine(a, b, new Color(Rift, 105), 12f);
        DrawLine(b, c, new Color(RiftCore, 185), 10f);
        DrawLine(c, d, new Color(Rift, 115), 12f);
        DrawLine(a, b, RiftCore, 3f);
        DrawLine(b, c, new Color(246, 143, 229), 3f);
        DrawLine(c, d, RiftCore, 3f);
        for (int i = 0; i < 3; i++)
        {
            float y = center.Y - height * .35f + i * height * .34f;
            float drift = MathF.Sin(time * 1.3f + i) * 7f;
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)(center.X + 17 + drift), (int)y, 4, 4),
                new Color(RiftCore, 130));
        }
    }

    private void DrawDistantRift(Vector2 center, float height)
    {
        DrawLine(center + new Vector2(-3f, -height * .5f),
            center + new Vector2(4f, -height * .1f), new Color(Rift, 54), 5f);
        DrawLine(center + new Vector2(4f, -height * .1f),
            center + new Vector2(-2f, height * .5f), new Color(RiftCore, 48), 4f);
    }

    private void DrawPortalRemnant(int centerX, int ground, int height)
    {
        DrawLine(new Vector2(centerX - 72, ground),
            new Vector2(centerX - 62, ground - height + 55), RockEdge, 18f);
        DrawLine(new Vector2(centerX + 72, ground),
            new Vector2(centerX + 62, ground - height + 55), RockEdge, 18f);
        DrawLine(new Vector2(centerX - 62, ground - height + 55),
            new Vector2(centerX - 22, ground - height), RockEdge, 17f);
        DrawLine(new Vector2(centerX + 62, ground - height + 55),
            new Vector2(centerX + 22, ground - height), RockEdge, 17f);
        DrawLine(new Vector2(centerX - 22, ground - height),
            new Vector2(centerX + 22, ground - height), RockEdge, 14f);
    }

    private void DrawRibArch(int x, int ground, int height, bool flip)
    {
        float s = flip ? 1f : -1f;
        DrawLine(new Vector2(x, ground), new Vector2(x + s * 8, ground - height),
            BoneDark, 12f);
        DrawLine(new Vector2(x + s * 8, ground - height),
            new Vector2(x + s * 72, ground - height - 38), Bone, 9f);
        DrawLine(new Vector2(x + s * 72, ground - height - 38),
            new Vector2(x + s * 132, ground - height + 16), BoneDark, 7f);
    }

    private void DrawSpineColumn(int x, int ground, int height)
    {
        DrawLine(new Vector2(x, ground), new Vector2(x, ground - height),
            BoneDark, 11f);
        for (int y = ground - 18; y > ground - height; y -= 24)
        {
            DrawLine(new Vector2(x - 22, y), new Vector2(x + 22, y), Bone, 5f);
            DrawDiamond(new Vector2(x, y), 9f, Bone);
        }
    }

    private void DrawBonePile(int x, int ground, int seed)
    {
        for (int i = 0; i < 4; i++)
        {
            int width = 18 + PositiveModulo(Hash(seed, i), 17);
            DrawLine(new Vector2(x - width / 2f + i * 5, ground - 3 - i * 3),
                new Vector2(x + width / 2f + i * 5, ground - 7 - i * 3),
                new Color(BoneDark, 190), 5f);
        }
    }

    private void DrawCutBlock(int x, int ground, int width, int height)
    {
        var block = new Rectangle(x, ground - height, width, height);
        _spriteBatch.Draw(_pixel, block, new Color(78, 47, 38));
        DrawOutline(block, 3, RockEdge);
        DrawLine(new Vector2(x + width * .25f, ground - height),
            new Vector2(x + width * .58f, ground - height * .48f), Charcoal, 2f);
    }

    private void DrawCrane(int x, int ground, int height, float time)
    {
        DrawLine(new Vector2(x, ground), new Vector2(x, ground - height), Iron, 13f);
        DrawLine(new Vector2(x - 20, ground - height + 8),
            new Vector2(x + 165, ground - height + 8), Iron, 11f);
        DrawLine(new Vector2(x, ground - height + 12),
            new Vector2(x + 126, ground - height + 66), Iron, 7f);
        float sway = MathF.Sin(time * .55f) * 4f;
        DrawChain((int)(x + 142 + sway), ground - height + 12, 155, 10, 2f);
    }

    private void DrawMineCart(int x, int ground)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 58, ground - 54, 116, 45),
            new Color(77, 52, 43));
        DrawOutline(new Rectangle(x - 58, ground - 54, 116, 45), 5, Iron);
        DrawDiamond(new Vector2(x - 34, ground - 5), 13f, Iron);
        DrawDiamond(new Vector2(x + 34, ground - 5), 13f, Iron);
    }

    private void DrawLavaSeam(int x, int y, int width, float time)
    {
        float pulse = .76f + MathF.Sin(time * 3.4f + x * .02f) * .17f;
        _spriteBatch.Draw(_pixel, new Rectangle(x, y - 7, width, 10),
            new Color(Lava, 185) * pulse);
        _spriteBatch.Draw(_pixel, new Rectangle(x + 18, y - 10, width - 42, 3),
            new Color(255, 173, 62, 210) * pulse);
    }

    private void DrawFallingDebris(int x, int y, float time)
    {
        for (int i = 0; i < 4; i++)
        {
            int fall = PositiveModulo((int)(time * 43f + i * 57), 190);
            DrawRockShard(new Vector2(x + i * 12, y + fall), 5 + i * 2,
                4 + i, new Color(RockEdge, 130));
        }
    }

    private void DrawRitualPillar(int x, int ground, int height)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - 24, ground - height, 48, height),
            new Color(59, 34, 34));
        DrawOutline(new Rectangle(x - 24, ground - height, 48, height),
            5, RockEdge);
        DrawDiamond(new Vector2(x, ground - height + 48), 15f,
            new Color(Corruption, 160));
    }

    private void DrawRitualAltar(Vector2 p, float time)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 92, (int)p.Y - 38, 184, 38), Charcoal);
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - 73, (int)p.Y - 57, 146, 21), Rock);
        float pulse = .8f + MathF.Sin(time * 2.3f) * .16f;
        DrawDiamond(p + new Vector2(0f, -82f), 19f,
            new Color(Corruption, 220) * pulse);
    }

    private void DrawTent(int x, int ground, int width, int height)
    {
        DrawTriangleUp(x, ground - height, width, height, new Color(68, 34, 34));
        DrawLine(new Vector2(x + width / 2f, ground - height),
            new Vector2(x + width / 2f, ground), Iron, 5f);
        DrawLine(new Vector2(x, ground),
            new Vector2(x + width / 2f, ground - height), new Color(Cloth, 170), 4f);
        DrawLine(new Vector2(x + width, ground),
            new Vector2(x + width / 2f, ground - height), new Color(Cloth, 170), 4f);
    }

    private void DrawWeaponRack(int x, int ground)
    {
        DrawLine(new Vector2(x - 45, ground), new Vector2(x - 35, ground - 105),
            Iron, 8f);
        DrawLine(new Vector2(x + 45, ground), new Vector2(x + 35, ground - 105),
            Iron, 8f);
        DrawLine(new Vector2(x - 38, ground - 78),
            new Vector2(x + 38, ground - 78), Iron, 7f);
        for (int i = 0; i < 4; i++)
            DrawLine(new Vector2(x - 30 + i * 20, ground - 15),
                new Vector2(x - 14 + i * 16, ground - 123), BoneDark, 4f);
    }

    private void DrawCage(int x, int ground, int width, int height)
    {
        var cage = new Rectangle(x - width / 2, ground - height, width, height);
        DrawOutline(cage, 6, Iron);
        for (int i = 1; i < 5; i++)
            _spriteBatch.Draw(_pixel,
                new Rectangle(cage.Left + i * width / 5, cage.Top, 4, height), Iron);
    }

    private void DrawSiegeFrame(int x, int ground)
    {
        DrawLine(new Vector2(x - 70, ground), new Vector2(x, ground - 155), Iron, 12f);
        DrawLine(new Vector2(x + 70, ground), new Vector2(x, ground - 155), Iron, 12f);
        DrawLine(new Vector2(x - 54, ground - 35),
            new Vector2(x + 54, ground - 35), Iron, 10f);
        DrawLine(new Vector2(x, ground - 145),
            new Vector2(x + 95, ground - 205), BoneDark, 9f);
    }

    private void DrawCampFire(int x, int ground, float time)
    {
        int flicker = (int)MathF.Round(MathF.Sin(time * 8f + x * .02f) * 5f);
        DrawLine(new Vector2(x - 31, ground), new Vector2(x + 28, ground - 16),
            Charcoal, 8f);
        DrawLine(new Vector2(x + 31, ground), new Vector2(x - 28, ground - 16),
            Charcoal, 8f);
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - 16, ground - 47 - flicker, 32, 43 + flicker),
            new Color(Ember, 205));
        _spriteBatch.Draw(_pixel,
            new Rectangle(x - 7, ground - 65 - flicker, 14, 42),
            new Color(247, 155, 51, 220));
    }

    private void DrawHorn(Vector2 root, float direction, float length)
    {
        Vector2 middle = root + new Vector2(direction * length * .42f,
            -length * .34f);
        Vector2 tip = root + new Vector2(direction * length * .62f,
            -length * .82f);
        DrawLine(root, middle, BoneDark, MathF.Max(7f, length * .10f));
        DrawLine(middle, tip, Bone, MathF.Max(4f, length * .055f));
    }

    private void DrawChain(int x, int y, int length, int linkSize, float sway)
    {
        int links = Math.Max(1, length / (linkSize + 3));
        for (int i = 0; i < links; i++)
        {
            int px = x + (int)(MathF.Sin(i * .8f) * sway);
            DrawOutline(new Rectangle(px - linkSize / 2,
                y + i * (linkSize + 3), linkSize, linkSize + 4), 2, Iron);
        }
    }

    private void DrawGroundCrack(Vector2 start, float length, Color color)
    {
        Vector2 mid = start + new Vector2(length * .42f, -11f);
        Vector2 end = start + new Vector2(length, -3f);
        DrawLine(start, mid, color, 3f);
        DrawLine(mid, end, color, 2f);
        DrawLine(mid, mid + new Vector2(9f, -12f), color * .72f, 2f);
    }

    private void DrawGroundRune(Vector2 center, float radius, Color color)
    {
        const int segments = 12;
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * .17f);
            DrawLine(previous, next, color, 2f);
            previous = next;
        }
        DrawLine(center + new Vector2(-radius * .55f, 0f),
            center + new Vector2(radius * .55f, 0f), color * .72f, 2f);
    }

    private void DrawBanner(int x, int y, int width, int height, float time,
        Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x - 4, y - 8, width + 8, 6), Iron);
        int segments = Math.Max(3, height / 24);
        int segmentHeight = height / segments;
        for (int i = 0; i < segments; i++)
        {
            int sway = (int)MathF.Round(MathF.Sin(time * .8f + i * .6f + x * .01f) *
                (2f + i));
            int tear = i == segments - 1 ? width / 4 : 0;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + sway, y + i * segmentHeight,
                    Math.Max(6, width - tear), segmentHeight + 1), color);
        }
        _spriteBatch.Draw(_pixel,
            new Rectangle(x + width / 2 - 2, y + 14, 4, height / 3),
            new Color(Bone, color.A));
    }

    private void DrawSmokeColumn(int x, int y, float time, int seed)
    {
        for (int i = 0; i < 5; i++)
        {
            int lift = PositiveModulo((int)(time * 11f + i * 43 + seed * 17), 190);
            int drift = (int)(MathF.Sin(time * .28f + i) * 19f);
            int size = 37 + i * 9;
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + drift - size / 2, y - lift, size, size / 2),
                new Color(45, 37, 39, 58 - i * 6));
        }
    }

    private void DrawJaggedMass(int x, int baseY, int width, int height,
        int seed, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(x, baseY - height + 34, width, height - 34), color);
        int chunk = Math.Max(22, width / 7);
        for (int px = x; px < x + width; px += chunk)
        {
            int peak = 18 + PositiveModulo(Hash(seed, px), 42);
            DrawTriangleUp(px, baseY - height - peak + 36,
                Math.Min(chunk + 8, x + width - px), peak, color);
        }
    }

    private void DrawRockShard(Vector2 p, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)p.X - width / 2, (int)p.Y - height,
                width, height), color);
        DrawLine(new Vector2(p.X - width / 2f, p.Y - height),
            new Vector2(p.X + width / 2f, p.Y - height + 3),
            new Color(RockEdge, color.A), 2f);
    }

    private void DrawDiamond(Vector2 center, float radius, Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius);
        Vector2 right = center + new Vector2(radius * .66f, 0f);
        Vector2 bottom = center + new Vector2(0f, radius);
        Vector2 left = center + new Vector2(-radius * .66f, 0f);
        DrawLine(top, right, color, 3f);
        DrawLine(right, bottom, color, 3f);
        DrawLine(bottom, left, color, 3f);
        DrawLine(left, top, color, 3f);
    }

    private void DrawTriangleUp(int x, int y, int width, int height, Color color)
    {
        int rows = Math.Max(1, height / 4);
        for (int row = 0; row < rows; row++)
        {
            int drawWidth = Math.Max(1, width * (row + 1) / rows);
            _spriteBatch.Draw(_pixel,
                new Rectangle(x + (width - drawWidth) / 2, y + row * 4,
                    drawWidth, 4), color);
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(length, width), SpriteEffects.None, 0f);
    }

    private void DrawOutline(Rectangle rectangle, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness),
            color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Bottom - thickness,
                rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height),
            color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Top,
                thickness, rectangle.Height), color);
    }

    private static DemonLandsZoneDefinition FindZone(DungeonMap map, int x)
    {
        foreach (DemonLandsZoneDefinition zone in map.DemonLandsZones)
            if (x >= zone.Bounds.Left && x < zone.Bounds.Right)
                return zone;
        return null;
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
}
