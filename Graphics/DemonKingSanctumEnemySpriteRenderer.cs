using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Articulated procedural figures for the first Sanctum pair.</summary>
public sealed class DemonKingSanctumEnemySpriteRenderer
{
    public static readonly Vector2 SentinelVisualSize = new(166f, 122f);
    public static readonly Vector2 AcolyteVisualSize = new(126f, 110f);

    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonKingSanctumEnemy, Clock> _clocks = new();
    private static readonly Color Outline = new(17, 15, 20);
    private static readonly Color IronDark = new(39, 39, 44);
    private static readonly Color Iron = new(74, 72, 75);
    private static readonly Color Bronze = new(126, 91, 54);
    private static readonly Color Cloth = new(54, 40, 49);
    private static readonly Color Crimson = new(102, 39, 45);
    private static readonly Color Stone = new(104, 99, 91);
    private static readonly Color Violet = new(157, 113, 180);

    public DemonKingSanctumEnemySpriteRenderer(SpriteBatch batch, Texture2D pixel)
    {
        _batch = batch;
        _pixel = pixel;
    }

    public void Draw(DemonKingSanctumEnemy enemy, GameTime gameTime)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Update(gameTime);
        bool committed = enemy.CombatState != SanctumEnemyState.Ready;
        bool flip = committed ? enemy.AttackFacesLeft :
            enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        Box(anchor, -25f, -3f, 50f, 5f, new Color(8, 7, 11, 145), flip);

        if (enemy.HasFracturedOathMarker)
            DrawMarker(enemy, clock.Elapsed);
        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }
        if (enemy.Type == EnemyType.OathboundSentinel)
            DrawSentinel(enemy, anchor, flip, clock.Elapsed);
        else
            DrawAcolyte(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawSentinel(
        DemonKingSanctumEnemy enemy, Vector2 a, bool flip, float elapsed)
    {
        float p = enemy.ActionProgress;
        float step = MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(elapsed * 5.2f) * 5f : 0f;
        float crouch = 0f, lean = 0f, angle = -.72f;
        switch (enemy.CombatState)
        {
            case SanctumEnemyState.OathThrustWindup:
                crouch = 6f * p; lean = -6f * p; angle = MathHelper.Lerp(-.72f, 3.02f, p); break;
            case SanctumEnemyState.OathThrustActive:
                crouch = 6f; lean = 14f * p; angle = .02f; step = 7f; break;
            case SanctumEnemyState.VowSweepWindup:
                lean = -7f * p; angle = MathHelper.Lerp(-.72f, -2.65f, p); break;
            case SanctumEnemyState.VowSweepActive:
                lean = 8f; angle = MathHelper.Lerp(-2.65f, .42f, p); break;
            case SanctumEnemyState.BrokenVowWindup:
                crouch = 7f * p; angle = MathHelper.Lerp(-.72f, -1.58f, p); break;
            case SanctumEnemyState.BrokenVowActive:
                crouch = 8f; lean = 10f; angle = MathHelper.Lerp(-1.58f, .72f, p); break;
            case SanctumEnemyState.PommelWindup: angle = .2f; lean = -4f; break;
            case SanctumEnemyState.PommelActive: angle = MathHelper.Lerp(.2f, 2.8f, p); lean = 6f; break;
        }
        if (enemy.IsHitFlashing || enemy.IsStaggered)
        { lean = -8f; crouch = enemy.IsStaggered ? 8f : 4f; }

        Color skin = enemy.IsHitFlashing ? new Color(188, 145, 126) : new Color(92, 64, 62);
        Vector2 hip = new(lean, -45f + crouch);
        Vector2 chest = new(lean, -72f + crouch);
        Vector2 rearFoot = new(-10f - step, 0f), frontFoot = new(12f + step, 0f);
        Vector2 rearKnee = new(-8f + step * .45f, -23f + crouch);
        Vector2 frontKnee = new(9f - step * .45f, -24f + crouch);
        Limb(a, rearFoot, rearKnee, IronDark, 7f, flip);
        Limb(a, rearKnee, hip + new Vector2(-7f, 0), Cloth, 9f, flip);
        Limb(a, frontFoot, frontKnee, Iron, 7f, flip);
        Limb(a, frontKnee, hip + new Vector2(7f, 0), Cloth, 9f, flip);
        Line(a, rearFoot + new Vector2(-7, 0), rearFoot + new Vector2(7, 0), 6, IronDark, flip);
        Line(a, frontFoot + new Vector2(-6, 0), frontFoot + new Vector2(9, 0), 6, Iron, flip);
        Box(a, hip.X - 15, hip.Y - 5, 30, 10, Outline, flip);
        for (int i = -1; i <= 1; i++)
            Box(a, hip.X + i * 9 - 4, hip.Y + 2, 8, 15, i == 0 ? Bronze : IronDark, flip);
        Box(a, chest.X - 16, chest.Y - 8, 32, 30, Outline, flip);
        Box(a, chest.X - 14, chest.Y - 6, 28, 26, IronDark, flip);
        Line(a, chest + new Vector2(-10, -2), chest + new Vector2(10, 17), 4, Bronze, flip);
        Box(a, chest.X - 22, chest.Y - 5, 12, 9, Iron, flip);
        Vector2 neck = chest + new Vector2(0, -12), head = neck + new Vector2(0, -11);
        Line(a, chest + new Vector2(0, -5), neck, 7, skin, flip);
        Box(a, head.X - 8, head.Y - 11, 16, 22, Outline, flip);
        Box(a, head.X - 6, head.Y - 9, 12, 19, Iron, flip);
        Box(a, head.X - 3, head.Y - 5, 8, 2, new Color(132, 91, 111), flip);
        Line(a, head + new Vector2(-6, -7), head + new Vector2(-10, -13), 3, Bronze, flip);

        Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 rearHand = chest + new Vector2(-10, 7), frontHand = chest + new Vector2(10, 10);
        Vector2 rearElbow = rearHand + new Vector2(-6, 10), frontElbow = frontHand + new Vector2(7, 9);
        Limb(a, chest + new Vector2(-12, -1), rearElbow, skin, 7, flip);
        Limb(a, rearElbow, rearHand, IronDark, 6, flip);
        Limb(a, chest + new Vector2(12, 0), frontElbow, skin, 7, flip);
        Limb(a, frontElbow, frontHand, Iron, 6, flip);
        Vector2 shaftCenter = (rearHand + frontHand) * .5f;
        Line(a, shaftCenter - dir * 52f, shaftCenter + dir * 62f, 4, new Color(49, 39, 37), flip);
        Line(a, shaftCenter + dir * 55f, shaftCenter + dir * 78f, 9, Outline, flip);
        Line(a, shaftCenter + dir * 57f, shaftCenter + dir * 78f, 6, Iron, flip);
        Line(a, shaftCenter + dir * 69f, shaftCenter + dir * 82f + new Vector2(-dir.Y, dir.X) * 8f, 4, Bronze, flip);
        for (int i = 0; i < 3; i++)
        {
            float sway = MathF.Sin(elapsed * 1.7f + i) * 3f;
            Line(a, chest + new Vector2(-13 + i * 7, 13),
                chest + new Vector2(-18 + i * 8 + sway, 37 + i * 3), 2,
                i == 1 ? Crimson : new Color(91, 65, 54), flip);
        }
    }

    private void DrawAcolyte(
        DemonKingSanctumEnemy enemy, Vector2 a, bool flip, float elapsed)
    {
        float p = enemy.ActionProgress;
        float step = MathF.Abs(enemy.VisualVelocityX) > 2f ? MathF.Sin(elapsed * 4.5f) * 3f : 0f;
        float lean = 4f, crouch = 4f, cast = 0f;
        if (enemy.IsCombatAttackTelegraphing) { cast = p; lean = MathHelper.Lerp(4, -3, p); }
        if (enemy.IsCombatAttackActive) { cast = 1f; lean = 7f; }
        if (enemy.CombatState == SanctumEnemyState.ResetStep) { lean = -5f; step = -4f; }
        if (enemy.IsHitFlashing || enemy.IsStaggered) { lean = -8f; crouch = 9f; }
        Color skin = enemy.IsHitFlashing ? new Color(184, 147, 135) : new Color(93, 70, 74);
        Vector2 hip = new(lean, -38 + crouch), chest = new(lean - 2, -62 + crouch);
        Vector2 rf = new(-7 - step, 0), ff = new(8 + step, 0);
        Vector2 rk = new(-6 + step * .4f, -19 + crouch), fk = new(7 - step * .4f, -20 + crouch);
        Limb(a, rf, rk, Cloth, 6, flip); Limb(a, rk, hip + new Vector2(-5, 0), Cloth, 7, flip);
        Limb(a, ff, fk, new Color(45, 38, 45), 6, flip); Limb(a, fk, hip + new Vector2(5, 0), Cloth, 7, flip);
        Line(a, chest + new Vector2(-10, 9), hip + new Vector2(-15, 31), 15, new Color(35, 29, 38), flip);
        Line(a, chest + new Vector2(8, 10), hip + new Vector2(12, 32), 14, Cloth, flip);
        Box(a, hip.X - 12, hip.Y - 3, 24, 6, new Color(52, 43, 43), flip);
        Box(a, chest.X - 12, chest.Y - 6, 24, 26, Outline, flip);
        Box(a, chest.X - 10, chest.Y - 4, 20, 23, new Color(43, 34, 46), flip);
        Vector2 head = chest + new Vector2(-1, -22);
        Box(a, head.X - 7, head.Y - 9, 14, 18, Outline, flip);
        Line(a, head + new Vector2(-6, -6), head + new Vector2(6, 6), 5, new Color(48, 39, 48), flip);
        Box(a, head.X + 1, head.Y - 2, 4, 2, new Color(119, 81, 128), flip);
        Vector2 lowShoulder = chest + new Vector2(-11, 1), lowElbow = lowShoulder + new Vector2(-7, 12);
        Vector2 lowHand = lowElbow + new Vector2(-2, 12);
        Limb(a, lowShoulder, lowElbow, skin, 6, flip); Limb(a, lowElbow, lowHand, skin, 5, flip);
        Vector2 castShoulder = chest + new Vector2(11, 0);
        Vector2 castElbow = castShoulder + new Vector2(7 + cast * 7, 9 - cast * 10);
        Vector2 castHand = castElbow + new Vector2(5 + cast * 9, 8 - cast * 9);
        Limb(a, castShoulder, castElbow, skin, 6, flip); Limb(a, castElbow, castHand, skin, 5, flip);
        for (int i = 0; i < 5; i++)
        {
            float pulse = enemy.IsCombatAttackTelegraphing ? 2f * cast : 0f;
            Vector2 s = chest + new Vector2(-17 + (i % 2) * 34,
                -5 + (i / 2) * 17 + MathF.Sin(elapsed * 1.4f + i) * (1f + pulse));
            Box(a, s.X - 5, s.Y - 6, 10, 12, Outline, flip);
            Box(a, s.X - 3, s.Y - 4, 6, 8, Stone, flip);
            Box(a, s.X - 1, s.Y - 1, 3, 2,
                enemy.IsCombatAttackTelegraphing ? Violet : new Color(91, 70, 96), flip);
        }
        if (enemy.IsCombatAttackTelegraphing)
            Box(a, castHand.X - 3, castHand.Y - 3, 7, 7,
                new Color(Violet, 180), flip);
    }

    private void DrawMarker(DemonKingSanctumEnemy enemy, float elapsed)
    {
        Vector2 c = enemy.FracturedOathPosition;
        Color color = enemy.IsFracturedOathActive
            ? new Color(188, 128, 205, 215)
            : new Color(133, 96, 148, 110 + (int)(60 * enemy.MarkerTelegraphRatio));
        float r = DemonKingSanctumEnemy.MarkerRadius;
        Line(c, new Vector2(-r, 0), new Vector2(r, 0), 3, color, false);
        Line(c, new Vector2(0, -8), new Vector2(0, 8), 3, color, false);
        Line(c, new Vector2(-r * .65f, -5), new Vector2(-r * .35f, 5), 2, color, false);
        Line(c, new Vector2(r * .35f, 5), new Vector2(r * .65f, -5), 2, color, false);
        if (enemy.IsFracturedOathActive)
            for (int i = -2; i <= 2; i++)
                Line(c, new Vector2(i * 13, 0), new Vector2(i * 9, -67),
                    4, new Color(157, 111, 181, 185), false);
    }

    private void DrawDeath(DemonKingSanctumEnemy enemy, Vector2 a, bool flip)
    {
        float p = enemy.DeathPresentationProgress;
        float angle = MathHelper.Lerp(0f, 1.35f, p);
        Vector2 body = new(MathF.Sin(angle) * 24f, -40f + p * 28f);
        Color main = enemy.Type == EnemyType.OathboundSentinel ? Iron : Cloth;
        Line(a, body + new Vector2(0, -25), body + new Vector2(0, 25), 24, main, flip);
        Line(a, body + new Vector2(-9, 18), new Vector2(-18, 0), 8, IronDark, flip);
        Line(a, body + new Vector2(8, 18), new Vector2(19, 0), 8, IronDark, flip);
        if (enemy.Type == EnemyType.OathboundSentinel)
            Line(a, new Vector2(-44, -3), new Vector2(54, -7), 4,
                new Color(49, 39, 37), flip);
        else
            for (int i = 0; i < 4; i++)
                Box(a, -24 + i * 14, -5 - i * 2, 8, 6,
                    new Color(80, 76, 72, 210 - i * 35), flip);
    }

    private void Limb(Vector2 a, Vector2 x, Vector2 y, Color c, float w, bool f) => Line(a, x, y, w + 3, Outline, f, c, w);
    private void Line(Vector2 a, Vector2 x, Vector2 y, float w, Color c, bool f, Color inner = default, float iw = 0)
    {
        if (f) { x.X = -x.X; y.X = -y.X; }
        Vector2 s = a + x, d = a + y - s;
        float len = d.Length(); if (len < .1f) return;
        float rot = MathF.Atan2(d.Y, d.X);
        _batch.Draw(_pixel, s, null, c, rot, new Vector2(0, .5f),
            new Vector2(len, w), SpriteEffects.None, 0f);
        if (iw > 0)
            _batch.Draw(_pixel, s, null, inner, rot, new Vector2(0, .5f),
                new Vector2(len, iw), SpriteEffects.None, 0f);
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
        public void Update(GameTime t) => Elapsed += MathF.Min(
            (float)t.ElapsedGameTime.TotalSeconds, .05f);
    }
}
