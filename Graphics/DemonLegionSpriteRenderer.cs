using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only military presentation for the Demon Legionnaire and the Horned
/// General. All poses share a stable foot anchor and mirror around that anchor.
/// </summary>
public sealed class DemonLegionSpriteRenderer
{
    public static readonly Vector2 LegionnaireVisualSize = new(154f, 120f);
    public static readonly Vector2 GeneralVisualSize = new(206f, 154f);

    private static readonly Color Outline = new(21, 17, 20);
    private static readonly Color BlackPlate = new(47, 43, 47);
    private static readonly Color PlateLight = new(82, 70, 70);
    private static readonly Color RedPlate = new(111, 39, 39);
    private static readonly Color DarkRed = new(63, 27, 31);
    private static readonly Color Cape = new(92, 27, 32);
    private static readonly Color CapeDark = new(48, 24, 29);
    private static readonly Color Horn = new(169, 146, 112);
    private static readonly Color Metal = new(151, 136, 124);
    private static readonly Color Ember = new(224, 82, 38);
    private static readonly Color Corruption = new(181, 56, 129);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonLandsEnemy, Clock> _clocks = new();

    public DemonLegionSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonLandsEnemy enemy, GameTime gameTime)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Elapsed += MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.FrontierCombatState != DemonFrontierCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        DrawWorldEffects(enemy, clock.Elapsed);
        DrawBox(anchor,
            enemy.Type == EnemyType.HornedGeneral ? -55f : -40f,
            -3f,
            enemy.Type == EnemyType.HornedGeneral ? 110f : 80f,
            7f,
            new Color(7, 6, 8, 160),
            flip);

        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.DemonLegionnaire)
            DrawLegionnaire(enemy, anchor, flip, clock.Elapsed);
        else
            DrawGeneral(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawLegionnaire(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool guard = enemy.IsLegionGuarding;
        bool broken = enemy.IsGuardBroken || enemy.IsStaggered;
        float march = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(elapsed * 5.6f) * 6f : 0f;
        float lean = broken ? -9f : guard ? -2f : 2f;
        Vector2 hip = new(lean, -39f);
        Vector2 chest = hip + new Vector2(0f, -32f);
        Vector2 head = chest + new Vector2(0f, -20f);

        DrawLine(anchor, new Vector2(-13f - march, -1f),
            hip + new Vector2(-9f, 3f), 12f, BlackPlate, flip);
        DrawLine(anchor, new Vector2(15f + march, -1f),
            hip + new Vector2(9f, 3f), 12f, RedPlate, flip);
        DrawCape(anchor, chest, hip, elapsed, flip, false, enemy.CombatPhase);
        DrawBox(anchor, hip.X - 22f, hip.Y - 4f, 45f, 42f, Outline, flip);
        DrawBox(anchor, hip.X - 18f, hip.Y, 37f, 36f, BlackPlate, flip);
        DrawBox(anchor, chest.X - 24f, chest.Y - 11f, 49f, 43f, Outline, flip);
        DrawBox(anchor, chest.X - 20f, chest.Y - 7f, 41f, 36f,
            enemy.IsHitFlashing ? new Color(155, 112, 105) : BlackPlate, flip);
        DrawBox(anchor, chest.X - 6f, chest.Y - 3f, 12f, 22f, RedPlate, flip);
        DrawLegionInsignia(anchor, chest + new Vector2(0f, 7f), flip);

        DrawBox(anchor, head.X - 15f, head.Y - 11f, 30f, 27f, Outline, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 8f, 24f, 21f, BlackPlate, flip);
        DrawLine(anchor, head + new Vector2(-9f, -8f),
            head + new Vector2(-25f, -25f), 7f, Horn, flip);
        DrawLine(anchor, head + new Vector2(9f, -8f),
            head + new Vector2(24f, -26f), 7f, Horn, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 1f, 5f, 3f, Ember, flip);

        DrawLegionShield(enemy, anchor, chest, flip, guard, broken);
        DrawLegionSpear(enemy, anchor, chest, flip, broken);

        if (enemy.IsGuardReacting)
            DrawBox(anchor, 26f, -80f, 8f, 35f,
                new Color(245, 151, 68, 170), flip);
        if (enemy.FrontierCombatState == DemonFrontierCombatState.LegionCommandGuard)
        {
            DrawBox(anchor, -35f, -5f, 70f, 4f,
                new Color(198, 63, 39, 155), flip);
            DrawBox(anchor, -22f, -10f, 44f, 3f,
                new Color(232, 105, 48, 130), flip);
        }
    }

    private void DrawLegionShield(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        bool guard,
        bool broken)
    {
        float x = guard ? 28f : broken ? 5f : 19f;
        float y = guard ? -8f : broken ? 13f : 2f;
        Vector2 arm = chest + new Vector2(12f, 3f);
        Vector2 grip = chest + new Vector2(x - 8f, y + 8f);
        DrawLine(anchor, arm, grip, 9f, RedPlate, flip);
        DrawBox(anchor, chest.X + x - 13f, chest.Y + y - 18f,
            28f, 48f, Outline, flip);
        DrawBox(anchor, chest.X + x - 10f, chest.Y + y - 15f,
            22f, 42f, RedPlate, flip);
        DrawBox(anchor, chest.X + x - 7f, chest.Y + y - 11f,
            16f, 34f, BlackPlate, flip);
        DrawBox(anchor, chest.X + x - 2f, chest.Y + y - 6f,
            5f, 24f, Ember, flip);
    }

    private void DrawLegionSpear(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        bool broken)
    {
        float angle = enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.LegionThrustWindup => -2.35f,
            DemonFrontierCombatState.LegionThrustActive => -.03f,
            DemonFrontierCombatState.LegionSweepWindup => -1.70f,
            DemonFrontierCombatState.LegionSweepActive =>
                MathHelper.Lerp(-1.70f, .62f, enemy.ActionProgress),
            DemonFrontierCombatState.LegionBashActive => -1.15f,
            _ => broken ? .55f : -.82f
        };
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 hand = chest + new Vector2(-7f, 7f);
        Vector2 start = hand - direction * 34f;
        Vector2 end = hand + direction * 79f;
        DrawLine(anchor, chest + new Vector2(-16f, 1f), hand,
            9f, BlackPlate, flip);
        DrawLine(anchor, start, end, 8f, Outline, flip);
        DrawLine(anchor, start, end, 4f, DarkRed, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, end, end - direction * 17f + normal * 16f,
            11f, Outline, flip);
        DrawLine(anchor, end, end - direction * 15f + normal * 14f,
            6f, Metal, flip);
    }

    private void DrawGeneral(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool guard = enemy.IsGeneralGuarding;
        bool broken = enemy.IsGuardBroken || enemy.IsStaggered;
        bool phaseThree = enemy.CombatPhase == 3;
        float march = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(elapsed * 4.6f) * 7f : 0f;
        float lean = broken ? -10f : enemy.FrontierCombatState is
            DemonFrontierCombatState.GeneralChargeActive or
            DemonFrontierCombatState.GeneralLegionsEndCharge ? 13f : 0f;
        Vector2 hip = new(lean, -47f);
        Vector2 chest = hip + new Vector2(0f, -40f);
        Vector2 head = chest + new Vector2(1f, -26f);

        DrawLine(anchor, new Vector2(-19f - march, -1f),
            hip + new Vector2(-13f, 4f), 17f, BlackPlate, flip);
        DrawLine(anchor, new Vector2(21f + march, -1f),
            hip + new Vector2(13f, 4f), 17f, PlateLight, flip);
        DrawCape(anchor, chest, hip, elapsed, flip, phaseThree, enemy.CombatPhase);
        DrawBox(anchor, hip.X - 30f, hip.Y - 7f, 61f, 49f, Outline, flip);
        DrawBox(anchor, hip.X - 26f, hip.Y - 3f, 53f, 43f, BlackPlate, flip);
        DrawBox(anchor, chest.X - 36f, chest.Y - 15f, 73f, 53f, Outline, flip);
        DrawBox(anchor, chest.X - 31f, chest.Y - 10f, 63f, 44f,
            enemy.IsHitFlashing ? new Color(161, 113, 104) : BlackPlate, flip);
        DrawBox(anchor, chest.X - 37f, chest.Y - 9f, 17f, 21f, PlateLight, flip);
        DrawBox(anchor, chest.X + 21f, chest.Y - 9f, 18f, 22f, RedPlate, flip);
        DrawLegionInsignia(anchor, chest + new Vector2(0f, 12f), flip);

        Color core = enemy.CombatPhase switch
        {
            3 => new Color(247, 96, 47),
            2 => Corruption,
            _ => new Color(183, 61, 44)
        };
        int pulse = (int)MathF.Round(MathF.Sin(elapsed * 6f) * 2f);
        DrawBox(anchor, chest.X - 10f - pulse, chest.Y - 1f - pulse,
            21f + pulse * 2, 22f + pulse * 2, Outline, flip);
        DrawBox(anchor, chest.X - 6f, chest.Y + 3f, 13f, 14f, core, flip);
        if (enemy.CombatPhase >= 2)
        {
            DrawLine(anchor, chest + new Vector2(-6f, 17f),
                hip + new Vector2(-21f, 1f), 3f, Corruption, flip);
            DrawLine(anchor, chest + new Vector2(7f, 17f),
                hip + new Vector2(24f, 4f), 3f, core, flip);
        }

        DrawBox(anchor, head.X - 19f, head.Y - 14f, 38f, 32f, Outline, flip);
        DrawBox(anchor, head.X - 15f, head.Y - 10f, 30f, 25f, BlackPlate, flip);
        DrawLine(anchor, head + new Vector2(-12f, -9f),
            head + new Vector2(-34f, -36f), 10f, Horn, flip);
        Vector2 rightHornEnd = phaseThree
            ? head + new Vector2(21f, -25f)
            : head + new Vector2(34f, -38f);
        DrawLine(anchor, head + new Vector2(12f, -9f), rightHornEnd,
            10f, Horn, flip);
        if (phaseThree)
            DrawBox(anchor, rightHornEnd.X - 4f, rightHornEnd.Y - 4f,
                8f, 8f, new Color(93, 70, 61), flip);
        DrawBox(anchor, head.X + 4f, head.Y - 2f, 6f, 3f, core, flip);

        DrawGeneralHalberd(enemy, anchor, chest, flip, broken);
        if (guard)
        {
            DrawBox(anchor, 25f, -102f, 7f, 63f,
                new Color(193, 71, 42, 135), flip);
            DrawBox(anchor, 31f, -91f, 4f, 41f,
                new Color(240, 135, 53, 150), flip);
        }
        if (enemy.FrontierCombatState == DemonFrontierCombatState.GeneralPhaseTransition)
        {
            DrawOutline(new Rectangle(
                (int)anchor.X - 62,
                (int)anchor.Y - 126,
                124,
                126),
                4,
                new Color(211, 70, 106, 170));
        }
    }

    private void DrawGeneralHalberd(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        bool broken)
    {
        float angle = enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.GeneralThrustWindup => -2.42f,
            DemonFrontierCombatState.GeneralThrustActive => -.02f,
            DemonFrontierCombatState.GeneralSweepWindup => -1.86f,
            DemonFrontierCombatState.GeneralSweepActive =>
                MathHelper.Lerp(-1.86f, .74f, enemy.ActionProgress),
            DemonFrontierCombatState.GeneralRiftCleaveWindup => -1.45f,
            DemonFrontierCombatState.GeneralRiftCleaveActive => .18f,
            DemonFrontierCombatState.GeneralChargeActive => -.03f,
            DemonFrontierCombatState.GeneralLegionsEndRaise => -1.57f,
            DemonFrontierCombatState.GeneralLegionsEndWarning => -1.57f,
            DemonFrontierCombatState.GeneralLegionsEndCharge => -.02f,
            DemonFrontierCombatState.GeneralLegionsEndCleave => .36f,
            _ => broken ? .78f : -.78f
        };
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 rearHand = chest + new Vector2(-10f, 8f);
        Vector2 frontHand = rearHand + direction * 22f;
        Vector2 start = rearHand - direction * 47f;
        Vector2 end = frontHand + direction * 92f;
        DrawLine(anchor, chest + new Vector2(-25f, 1f), rearHand,
            13f, BlackPlate, flip);
        DrawLine(anchor, chest + new Vector2(26f, 1f), frontHand,
            13f, RedPlate, flip);
        DrawLine(anchor, start, end, 11f, Outline, flip);
        DrawLine(anchor, start, end, 6f, DarkRed, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, end, end - direction * 22f + normal * 37f,
            16f, Outline, flip);
        DrawLine(anchor, end, end - direction * 20f + normal * 34f,
            9f, Metal, flip);
        DrawLine(anchor, end - direction * 7f,
            end - direction * 24f - normal * 15f, 8f, Metal, flip);
    }

    private void DrawWorldEffects(DemonLandsEnemy enemy, float elapsed)
    {
        if (enemy.FrontierCombatState ==
            DemonFrontierCombatState.GeneralStompWindup)
        {
            Rectangle warning = enemy.ActiveAttackArea;
            _spriteBatch.Draw(_pixel, warning, new Color(203, 70, 35, 38));
            DrawOutline(warning, 2, new Color(238, 127, 54, 165));
        }
        if (enemy.FrontierCombatState ==
            DemonFrontierCombatState.GeneralChargeWindup)
        {
            int direction = enemy.AttackFacesLeft ? -1 : 1;
            Rectangle warning = new(
                enemy.Bounds.Center.X + (direction < 0 ? -280 : 0),
                enemy.Bounds.Bottom - 16,
                280,
                16);
            _spriteBatch.Draw(_pixel, warning, new Color(190, 54, 35, 40));
            DrawOutline(warning, 2, new Color(229, 113, 48, 165));
        }
        if (enemy.FrontierCombatState ==
            DemonFrontierCombatState.GeneralAnchorWindup)
        {
            Vector2 position = enemy.RequestedBossAnchorPosition;
            Rectangle glyph = new(
                (int)position.X - 32,
                enemy.Bounds.Bottom - 10,
                64,
                10);
            _spriteBatch.Draw(_pixel, glyph, new Color(151, 48, 143, 52));
            DrawOutline(glyph, 2, new Color(221, 101, 202, 175));
        }
        if (enemy.HasCorruptionPulse)
        {
            int radius = (int)enemy.CorruptionPulseRadius;
            Rectangle pulse = new(
                (int)enemy.CorruptionPulseCenter.X - radius,
                (int)enemy.CorruptionPulseCenter.Y - 22,
                radius * 2,
                44);
            _spriteBatch.Draw(_pixel, pulse, new Color(139, 38, 103, 42));
            DrawOutline(pulse, 3, new Color(211, 76, 139, 150));
        }
        if (enemy.HasGeneralRiftPath)
        {
            Rectangle path = enemy.GeneralRiftPathBounds;
            _spriteBatch.Draw(_pixel, path, new Color(104, 32, 126, 72));
            DrawOutline(path, 2, new Color(211, 79, 185, 175));
        }
        if (enemy.IsLegionsEndTelegraphing)
        {
            int width = 680 + (int)(MathF.Sin(elapsed * 8f) * 8f);
            bool left = enemy.AttackFacesLeft;
            Rectangle warning = new(
                left ? enemy.Bounds.Center.X - width : enemy.Bounds.Center.X,
                enemy.Bounds.Bottom - 18,
                width,
                18);
            _spriteBatch.Draw(_pixel, warning, new Color(213, 50, 31, 58));
            DrawOutline(warning, 3, new Color(247, 117, 50, 205));
        }
    }

    private void DrawCape(
        Vector2 anchor,
        Vector2 chest,
        Vector2 hip,
        float elapsed,
        bool flip,
        bool broken,
        int phase)
    {
        float sway = MathF.Sin(elapsed * 2.8f) * 4f;
        float length = broken ? 43f : phase >= 2 ? 62f : 55f;
        DrawLine(anchor, chest + new Vector2(-16f, -4f),
            hip + new Vector2(-30f + sway, length - 5f),
            broken ? 14f : 20f, CapeDark, flip);
        DrawLine(anchor, chest + new Vector2(-11f, -2f),
            hip + new Vector2(-22f + sway, length),
            broken ? 10f : 15f, Cape, flip);
    }

    private void DrawLegionInsignia(Vector2 anchor, Vector2 center, bool flip)
    {
        DrawBox(anchor, center.X - 7f, center.Y - 6f, 14f, 12f, DarkRed, flip);
        DrawLine(anchor, center + new Vector2(-5f, 4f),
            center + new Vector2(0f, -5f), 3f, Ember, flip);
        DrawLine(anchor, center + new Vector2(0f, -5f),
            center + new Vector2(5f, 4f), 3f, Ember, flip);
    }

    private void DrawDeath(DemonLandsEnemy enemy, Vector2 anchor, bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        if (enemy.Type == EnemyType.DemonLegionnaire)
        {
            float height = MathF.Max(10f, 78f * (1f - progress * .82f));
            DrawBox(anchor, -31f, -height, 62f, height, BlackPlate, flip);
            DrawLine(anchor, new Vector2(13f, -height),
                new Vector2(69f, -4f), 7f, DarkRed, flip);
            return;
        }

        // Staged boss fall: weapon lodges, kneel, core fades, final collapse.
        float kneel = Math.Clamp(progress / .48f, 0f, 1f);
        float collapse = Math.Clamp((progress - .48f) / .52f, 0f, 1f);
        float bodyHeight = MathF.Max(14f, 116f - kneel * 35f - collapse * 60f);
        DrawLine(anchor, new Vector2(54f, -112f + kneel * 35f),
            new Vector2(72f, -2f), 10f, DarkRed, flip);
        DrawLine(anchor, new Vector2(72f, -2f),
            new Vector2(89f, -30f), 15f, Metal, flip);
        DrawBox(anchor, -43f + collapse * 18f, -bodyHeight,
            86f, bodyHeight, BlackPlate, flip);
        byte alpha = (byte)Math.Clamp((int)(230f * (1f - progress)), 0, 230);
        DrawBox(anchor, -9f + collapse * 18f, -bodyHeight * .65f,
            18f, 16f,
            new Color((byte)224, (byte)76, (byte)43, alpha), flip);
        DrawLine(anchor, new Vector2(-30f, -8f),
            new Vector2(-52f + collapse * 25f, -1f),
            18f, CapeDark, flip);
    }

    private void DrawBox(Vector2 anchor, float x, float y, float width,
        float height, Color color, bool flip)
    {
        if (flip)
            x = -x - width;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(anchor.X + x),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLine(Vector2 anchor, Vector2 start, Vector2 end,
        float thickness, Color color, bool flip)
    {
        if (flip)
        {
            start.X = -start.X;
            end.X = -end.X;
        }
        Vector2 worldStart = anchor + start;
        Vector2 delta = anchor + end - worldStart;
        float length = delta.Length();
        if (length < .01f)
            return;
        _spriteBatch.Draw(_pixel, worldStart, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private void DrawOutline(Rectangle rectangle, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Bottom - thickness,
                rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Y,
                thickness, rectangle.Height), color);
    }

    private sealed class Clock
    {
        public float Elapsed;
    }
}
