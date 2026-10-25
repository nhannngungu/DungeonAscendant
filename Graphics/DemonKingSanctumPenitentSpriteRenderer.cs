using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Articulated procedural rendering for the penitent and scribe.</summary>
public sealed class DemonKingSanctumPenitentSpriteRenderer
{
    public static readonly Vector2 HuskVisualSize = new(154f, 126f);
    public static readonly Vector2 ScribeVisualSize = new(138f, 120f);

    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonKingSanctumPenitentEnemy, Clock>
        _clocks = new();
    private static readonly Color Outline = new(18, 15, 18);
    private static readonly Color Scarred = new(112, 69, 61);
    private static readonly Color Burned = new(68, 46, 45);
    private static readonly Color Iron = new(70, 68, 70);
    private static readonly Color Chain = new(112, 103, 91);
    private static readonly Color Stone = new(92, 87, 80);
    private static readonly Color RobeDark = new(35, 29, 42);
    private static readonly Color Robe = new(56, 43, 62);
    private static readonly Color Bronze = new(126, 91, 56);
    private static readonly Color Glyph = new(173, 126, 190);

    public DemonKingSanctumPenitentSpriteRenderer(
        SpriteBatch batch, Texture2D pixel)
    {
        _batch = batch;
        _pixel = pixel;
    }

    public void Draw(
        DemonKingSanctumPenitentEnemy enemy, GameTime gameTime)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Update(gameTime);
        bool committed = enemy.CombatState != PenitentCombatState.Ready;
        bool flip = committed ? enemy.AttackFacesLeft :
            enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        Box(anchor, -29, -3, 58, 6, new Color(8, 7, 10, 150), flip);
        if (enemy.HasCondemnedMark) DrawCondemnedMark(enemy);
        if (enemy.HasForbiddenLine) DrawForbiddenLine(enemy);
        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }
        if (enemy.Type == EnemyType.PenitentHusk)
            DrawHusk(enemy, anchor, flip, clock.Elapsed);
        else
            DrawScribe(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawHusk(
        DemonKingSanctumPenitentEnemy enemy, Vector2 a, bool flip,
        float elapsed)
    {
        float p = enemy.ActionProgress;
        float step = MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(elapsed * 4.2f) * 4f : 0f;
        float crouch = 7f, lean = 8f, armAngle = .85f, slabTilt = -.08f;
        switch (enemy.CombatState)
        {
            case PenitentCombatState.PenitentSwingWindup:
                lean = MathHelper.Lerp(8, -8, p);
                armAngle = MathHelper.Lerp(.85f, -2.35f, p); break;
            case PenitentCombatState.PenitentSwingActive:
                lean = 15f; armAngle = MathHelper.Lerp(-2.35f, .25f, p); break;
            case PenitentCombatState.BurdenDropWindup:
                crouch = MathHelper.Lerp(7, 1, p); lean = -4f;
                slabTilt = MathHelper.Lerp(-.08f, -.34f, p); break;
            case PenitentCombatState.BurdenDropActive:
                crouch = 18f; lean = 18f; slabTilt = .28f; break;
            case PenitentCombatState.ChainLungeWindup:
                crouch = 12f; lean = -7f; armAngle = -.45f; break;
            case PenitentCombatState.ChainLungeActive:
                crouch = 10f; lean = 20f; armAngle = .05f; step = 9f; break;
            case PenitentCombatState.RepentanceBraceStart:
                crouch = MathHelper.Lerp(7, 16, p); lean = 2f;
                armAngle = MathHelper.Lerp(.85f, 1.55f, p); break;
            case PenitentCombatState.RepentanceBraceActive:
                crouch = 16f; lean = 2f; armAngle = 1.55f; break;
        }
        if (enemy.IsHitFlashing || enemy.IsStaggered)
        { lean = -9f; crouch = 15f; armAngle = 1.8f; }
        Color flesh = enemy.IsHitFlashing
            ? new Color(205, 139, 112) : Scarred;
        Vector2 hip = new(lean, -43 + crouch);
        Vector2 chest = new(lean + 7, -70 + crouch);
        Vector2 rearFoot = new(-15 - step, 0), frontFoot = new(16 + step, 0);
        Vector2 rearKnee = new(-12 + step * .4f, -22 + crouch);
        Vector2 frontKnee = new(12 - step * .4f, -23 + crouch);

        // The slab is a separate back mass; the body remains readable in front.
        Vector2 slabTop = chest + new Vector2(-19 + slabTilt * 20f, -19);
        Vector2 slabBottom = hip + new Vector2(-22, 18);
        Line(a, slabTop, slabBottom, 30, Outline, flip);
        Line(a, slabTop + new Vector2(2, 2), slabBottom + new Vector2(2, -2),
            24, Stone, flip);
        Line(a, slabTop + new Vector2(0, 8), slabBottom + new Vector2(0, -8),
            3, new Color(57, 53, 51), flip);

        Limb(a, rearFoot, rearKnee, Burned, 9, flip);
        Limb(a, rearKnee, hip + new Vector2(-9, 0), flesh, 12, flip);
        Limb(a, frontFoot, frontKnee, Burned, 9, flip);
        Limb(a, frontKnee, hip + new Vector2(9, 0), flesh, 12, flip);
        Line(a, rearFoot + new Vector2(-8, 0), rearFoot + new Vector2(8, 0), 7, Burned, flip);
        Line(a, frontFoot + new Vector2(-7, 0), frontFoot + new Vector2(11, 0), 7, Burned, flip);
        Box(a, hip.X - 16, hip.Y - 5, 32, 9, Burned, flip);
        Line(a, hip + new Vector2(-13, 2), hip + new Vector2(-17, 25), 13, new Color(70, 42, 43), flip);
        Line(a, hip + new Vector2(12, 2), hip + new Vector2(15, 24), 12, new Color(83, 47, 45), flip);
        Box(a, chest.X - 18, chest.Y - 8, 36, 30, Outline, flip);
        Box(a, chest.X - 16, chest.Y - 6, 32, 26, flesh, flip);
        Line(a, chest + new Vector2(-12, 0), chest + new Vector2(11, 16), 3, Burned, flip);
        Line(a, chest + new Vector2(-9, 14), chest + new Vector2(13, 2), 3, Burned, flip);
        Vector2 neck = chest + new Vector2(5, -11), head = neck + new Vector2(5, -10);
        Line(a, chest + new Vector2(3, -5), neck, 9, flesh, flip);
        Box(a, head.X - 8, head.Y - 9, 16, 18, Outline, flip);
        Box(a, head.X - 6, head.Y - 7, 12, 15, Burned, flip);
        Box(a, head.X + 1, head.Y - 2, 4, 2, new Color(121, 76, 82), flip);

        Vector2 dir = new(MathF.Cos(armAngle), MathF.Sin(armAngle));
        Vector2 shoulderA = chest + new Vector2(-14, 1);
        Vector2 shoulderB = chest + new Vector2(15, 3);
        Vector2 elbowA = shoulderA + dir * 16f;
        Vector2 wristA = elbowA + dir * 17f;
        Vector2 elbowB = shoulderB + dir * 13f;
        Vector2 wristB = elbowB + dir * 14f;
        Limb(a, shoulderA, elbowA, flesh, 10, flip);
        Limb(a, elbowA, wristA, Burned, 9, flip);
        Limb(a, shoulderB, elbowB, flesh, 10, flip);
        Limb(a, elbowB, wristB, Burned, 9, flip);
        Line(a, wristA, wristB, 4, Chain, flip);
        for (int i = 0; i < 4; i++)
        {
            float t = (i + 1) / 5f;
            Vector2 chainPoint = Vector2.Lerp(wristA, wristA + dir * 43f, t);
            Box(a, chainPoint.X - 3, chainPoint.Y - 2, 6, 4,
                i % 2 == 0 ? Chain : Iron, flip);
        }
        Vector2 cudgel = wristA + dir * 47f;
        Line(a, cudgel - dir * 10f, cudgel + dir * 11f, 13, Outline, flip);
        Line(a, cudgel - dir * 8f, cudgel + dir * 9f, 9, Iron, flip);
        if (enemy.IsRepentanceBraced)
            Box(a, chest.X - 23, chest.Y - 13, 46, 4,
                new Color(145, 104, 73, 150), flip);
    }

    private void DrawScribe(
        DemonKingSanctumPenitentEnemy enemy, Vector2 a, bool flip,
        float elapsed)
    {
        float p = enemy.ActionProgress;
        float step = MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(elapsed * 4.4f) * 3f : 0f;
        float lean = 1f, crouch = 1f, write = 0f;
        if (enemy.IsCombatAttackTelegraphing) write = p;
        if (enemy.CombatState == PenitentCombatState.Reposition)
        { lean = -5f; step = -4f; }
        if (enemy.CombatState == PenitentCombatState.EraseActive)
        { lean = 7f; write = 1f; }
        if (enemy.IsHitFlashing || enemy.IsStaggered)
        { lean = -8f; crouch = 8f; }
        Color skin = enemy.IsHitFlashing
            ? new Color(190, 154, 144) : new Color(89, 70, 78);
        Vector2 hip = new(lean, -41 + crouch), chest = new(lean, -70 + crouch);
        Vector2 rf = new(-7 - step, 0), ff = new(8 + step, 0);
        Vector2 rk = new(-6 + step * .4f, -20 + crouch), fk = new(7 - step * .4f, -21 + crouch);
        Limb(a, rf, rk, RobeDark, 6, flip); Limb(a, rk, hip + new Vector2(-5, 0), Robe, 7, flip);
        Limb(a, ff, fk, RobeDark, 6, flip); Limb(a, fk, hip + new Vector2(5, 0), Robe, 7, flip);
        Line(a, chest + new Vector2(-11, 12), hip + new Vector2(-15, 38), 17, RobeDark, flip);
        Line(a, chest + new Vector2(9, 12), hip + new Vector2(14, 38), 16, Robe, flip);
        Line(a, hip + new Vector2(0, 0), hip + new Vector2(1, 36), 3, Bronze, flip);
        Box(a, hip.X - 13, hip.Y - 4, 26, 7, new Color(52, 42, 43), flip);
        Box(a, chest.X - 13, chest.Y - 7, 26, 30, Outline, flip);
        Box(a, chest.X - 11, chest.Y - 5, 22, 27, RobeDark, flip);
        Line(a, chest + new Vector2(-8, -2), chest + new Vector2(8, 18), 3, Bronze, flip);
        Vector2 head = chest + new Vector2(0, -23);
        Box(a, head.X - 7, head.Y - 12, 14, 24, Outline, flip);
        Box(a, head.X - 5, head.Y - 10, 10, 21, Iron, flip);
        Box(a, head.X - 1, head.Y - 6, 3, 11, new Color(111, 77, 119), flip);

        // Scroll/tablet bundle stays bound behind the torso.
        Box(a, chest.X - 22, chest.Y - 1, 10, 34, Outline, flip);
        Box(a, chest.X - 20, chest.Y + 1, 6, 30, Stone, flip);
        Line(a, chest + new Vector2(-20, 7), chest + new Vector2(-14, 7), 2, Bronze, flip);
        Line(a, chest + new Vector2(-20, 22), chest + new Vector2(-14, 22), 2, Bronze, flip);

        Vector2 leftShoulder = chest + new Vector2(-11, 1);
        Vector2 leftElbow = leftShoulder + new Vector2(-8, 12);
        Vector2 leftHand = leftElbow + new Vector2(-2, 14);
        Limb(a, leftShoulder, leftElbow, skin, 6, flip);
        Limb(a, leftElbow, leftHand, skin, 5, flip);
        for (int finger = 0; finger < 3; finger++)
            Line(a, leftHand, leftHand + new Vector2(-5 + finger * 3, 7),
                1.5f, skin, flip);
        Vector2 rightShoulder = chest + new Vector2(11, 0);
        Vector2 rightElbow = rightShoulder + new Vector2(8 + write * 5, 9 - write * 9);
        Vector2 rightHand = rightElbow + new Vector2(6 + write * 10, 9 - write * 8);
        Limb(a, rightShoulder, rightElbow, skin, 6, flip);
        Limb(a, rightElbow, rightHand, skin, 5, flip);
        Vector2 stylusTip = rightHand + new Vector2(13, -7 + MathF.Sin(p * MathHelper.TwoPi) * 5f);
        Line(a, rightHand, stylusTip, 3, new Color(184, 166, 139), flip);

        for (int i = 0; i < 4; i++)
        {
            float phase = elapsed * 1.25f + i * 1.4f;
            Vector2 glyph = chest + new Vector2(17 + (i % 2) * 10,
                -11 + i * 12 + MathF.Sin(phase) * 2f);
            Box(a, glyph.X - 4, glyph.Y - 4, 8, 8,
                new Color(66, 50, 72), flip);
            Line(a, glyph + new Vector2(-2, 0), glyph + new Vector2(2, 0),
                2, write > 0 ? Glyph : new Color(105, 78, 114), flip);
        }
        if (write > 0)
        {
            Vector2 glyphCenter = stylusTip + new Vector2(8, 0);
            Line(a, glyphCenter + new Vector2(-7, -7),
                glyphCenter + new Vector2(7, -7), 2, Glyph, flip);
            Line(a, glyphCenter + new Vector2(7, -7),
                glyphCenter + new Vector2(-5, 7), 2, Glyph, flip);
            Line(a, glyphCenter + new Vector2(-5, 7),
                glyphCenter + new Vector2(8, 7), 2, Glyph, flip);
        }
    }

    private void DrawCondemnedMark(DemonKingSanctumPenitentEnemy enemy)
    {
        Vector2 c = enemy.CondemnedMarkPosition;
        Color color = enemy.IsCondemnedMarkActive
            ? new Color(184, 132, 199, 220)
            : new Color(132, 91, 145,
                95 + (int)(75 * enemy.MarkTelegraphRatio));
        Line(c, new Vector2(-19, 0), new Vector2(19, 0), 3, color, false);
        Line(c, new Vector2(-8, -6), new Vector2(0, 5), 2, color, false);
        Line(c, new Vector2(0, 5), new Vector2(10, -7), 2, color, false);
        if (enemy.IsCondemnedMarkActive)
            Line(c, new Vector2(0, 0), new Vector2(0, -112), 7,
                new Color(164, 111, 183, 190), false);
    }

    private void DrawForbiddenLine(DemonKingSanctumPenitentEnemy enemy)
    {
        Vector2 c = enemy.ForbiddenLinePosition;
        float half = DemonKingSanctumPenitentEnemy.ForbiddenLineLength * .5f;
        Color color = enemy.IsForbiddenLineActive
            ? new Color(181, 126, 197, 215)
            : new Color(126, 89, 140,
                90 + (int)(80 * enemy.LineTelegraphRatio));
        Line(c, new Vector2(-half, -2), new Vector2(half, -2),
            enemy.IsForbiddenLineActive ? 7 : 3, color, false);
        for (int i = -3; i <= 3; i++)
            Line(c, new Vector2(i * 28 - 5, -8),
                new Vector2(i * 28 + 5, 5), 2, color, false);
    }

    private void DrawDeath(
        DemonKingSanctumPenitentEnemy enemy, Vector2 a, bool flip)
    {
        float p = enemy.DeathPresentationProgress;
        Vector2 body = new(18f * p, -45 + p * 33);
        float width = enemy.Type == EnemyType.PenitentHusk ? 31f : 21f;
        Color color = enemy.Type == EnemyType.PenitentHusk ? Scarred : Robe;
        Line(a, body + new Vector2(0, -24), body + new Vector2(0, 23),
            width, color, flip);
        Line(a, body + new Vector2(-8, 18), new Vector2(-19, 0), 8, Burned, flip);
        Line(a, body + new Vector2(8, 18), new Vector2(18, 0), 8, Burned, flip);
        if (enemy.Type == EnemyType.PenitentHusk)
            Line(a, body + new Vector2(-15, -22),
                body + new Vector2(-25 + p * 10, 22), 24, Stone, flip);
        else
            for (int i = 0; i < 4; i++)
                Box(a, body.X - 23 + i * 14, body.Y + 18 + i, 7, 5,
                    new Color(91, 78, 98, 190 - i * 35), flip);
    }

    private void Limb(Vector2 a, Vector2 x, Vector2 y, Color c, float w, bool f)
    { Line(a, x, y, w + 3, Outline, f); Line(a, x, y, w, c, f); }
    private void Line(Vector2 a, Vector2 x, Vector2 y, float w, Color c, bool f)
    {
        if (f) { x.X = -x.X; y.X = -y.X; }
        Vector2 start = a + x, delta = a + y - start;
        float length = delta.Length(); if (length < .1f) return;
        _batch.Draw(_pixel, start, null, c, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(length, w),
            SpriteEffects.None, 0f);
    }
    private void Box(Vector2 a, float x, float y, float w, float h, Color c, bool f)
    {
        if (f) x = -x - w;
        _batch.Draw(_pixel, new Rectangle((int)(a.X + x), (int)(a.Y + y),
            Math.Max(1, (int)w), Math.Max(1, (int)h)), c);
    }
    private sealed class Clock
    {
        public float Elapsed { get; private set; }
        public void Update(GameTime time) => Elapsed += MathF.Min(
            (float)time.ElapsedGameTime.TotalSeconds, .05f);
    }
}
