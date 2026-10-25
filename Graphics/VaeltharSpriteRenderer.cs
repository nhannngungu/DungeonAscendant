using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Regal, articulated sovereign with a split-channel royal blade.</summary>
public sealed class VaeltharSpriteRenderer
{
    public static readonly Vector2 VisualSize = new(520, 236);
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<VaeltharEnemy, Clock> _clocks = new();
    private static readonly Color Outline = new(12, 9, 19);
    private static readonly Color Plate = new(39, 33, 45);
    private static readonly Color PlateEdge = new(85, 71, 90);
    private static readonly Color Gold = new(162, 119, 62);
    private static readonly Color Crimson = new(93, 25, 44);
    private static readonly Color Skin = new(118, 78, 93);
    private static readonly Color Horn = new(101, 85, 91);
    private static readonly Color Core = new(177, 86, 207);
    public VaeltharSpriteRenderer(SpriteBatch batch, Texture2D pixel)
    { _batch = batch; _pixel = pixel; }

    public void Draw(VaeltharEnemy e, GameTime time)
    {
        Clock clock = _clocks.GetValue(e, static _ => new Clock());
        clock.Elapsed += MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        bool flip = e.CombatState == VaeltharState.Ready ? e.Facing == EnemyFacingDirection.Left : e.AttackFacesLeft;
        Vector2 a = new(e.Position.X, e.Bounds.Bottom);
        DrawEffects(e);
        if (!e.IsAlive) { DrawDeath(e, a, flip); return; }
        float p = e.ActionProgress, t = clock.Elapsed;
        float gait = MathF.Abs(e.VisualVelocityX) > 2 ? MathF.Sin(t * 4.8f) * 8 : 0;
        float hipX = 0, chestX = 2, crouch = 0, angle = -.55f, reach = 0;
        float crownAlign = e.CombatPhase >= 2 ? .5f : 0;
        if (e.CombatState == VaeltharState.Intro)
        {
            float rise = Math.Clamp(p / .65f, 0, 1);
            crouch = 29 * (1 - rise); chestX = -10 * (1 - rise);
            angle = MathHelper.Lerp(1.15f, -.55f, p); crownAlign = MathF.Max(0, (p - .65f) / .35f) * .3f;
        }
        else if (e.CombatState == VaeltharState.PhaseTransition)
        { chestX = -5; angle = -1.3f; crownAlign = p; }
        else if (e.CombatState is VaeltharState.GuardRaise or VaeltharState.GuardActive)
        { hipX = -6; chestX = -3; crouch = 7; angle = -1.10f; reach = 3; }
        else if (e.CombatState == VaeltharState.CounterTell)
        { hipX = -7; chestX = -9; angle = -1.7f; crouch = 5; }
        else if (e.CombatState is VaeltharState.UltimateRaise or VaeltharState.UltimateOmens)
        {
            chestX = -5; angle = e.CombatState == VaeltharState.UltimateRaise ?
                MathHelper.Lerp(-.55f, -1.6f, p) : -1.6f;
            reach = 4; crownAlign = e.CombatState == VaeltharState.UltimateRaise ? p : 1;
        }
        else if (e.CombatState is VaeltharState.PatternTell or VaeltharState.PatternSequence)
        { hipX = -2; chestX = 5; angle = -1.32f; crownAlign = .85f; }
        else if (e.CombatState is VaeltharState.MoveTell or VaeltharState.Pursuit or VaeltharState.CenterAdvance)
        { hipX = 4; chestX = 12; crouch = 7; angle = -.13f; reach = -5; }
        else if (e.CombatState is VaeltharState.Windup or VaeltharState.FollowupTell or
            VaeltharState.Active or VaeltharState.FinalWindup or VaeltharState.FinalActive)
        {
            bool active = e.IsCombatAttackActive;
            bool downward = e.AttackKind is VaeltharAttack.ThroneBreaker or VaeltharAttack.FinalSever or
                VaeltharAttack.KingsDomain or VaeltharAttack.EndOfKings;
            if (downward)
            {
                hipX = active ? 12 : -6 * p; chestX = active ? 21 : -8 * p;
                crouch = active ? 16 : 5 * p;
                angle = active ? MathHelper.Lerp(-1.7f, 1.0f, p) : MathHelper.Lerp(-.55f, -1.85f, p);
                reach = active ? 15 : -2;
            }
            else
            {
                float sweep = e.ComboIndex % 2 == 0 ? 1 : -1;
                hipX = active ? 11 : -7 * p; chestX = active ? 18 : -11 * p;
                crouch = active ? 8 : 4 * p;
                float start = sweep > 0 ? -1.8f : .85f, finish = sweep > 0 ? .65f : -1.25f;
                angle = active ? MathHelper.Lerp(start, finish, p) : MathHelper.Lerp(-.55f, start, p);
                reach = active ? 16 : -6;
            }
        }
        else if (e.CombatState == VaeltharState.Recovery)
        {
            float weight = e.AttackKind is VaeltharAttack.FinalSever or VaeltharAttack.EndOfKings ? 18 : 7;
            crouch = weight * (1 - p); chestX = 12 * (1 - p);
            angle = MathHelper.Lerp(.75f, -.55f, p);
        }
        if (e.CombatPhase == 3) { hipX += 3; chestX += 5; }
        if (e.IsStaggered || e.IsHitFlashing)
        { hipX = -8; chestX = -15; crouch = 13; angle = .8f; }
        Color plate = e.IsHitFlashing ? new Color(152, 112, 126) : Plate;
        Color seam = e.CombatPhase == 1 ? new Color(72, 42, 87) :
            e.CombatPhase == 2 ? new Color(128, 65, 159) : Core;
        Vector2 hip = new(hipX, -61 + crouch), chest = new(chestX, -109 + crouch);
        DrawMantle(a, chest, hip, t, e.CombatPhase == 3, flip);
        Vector2 rearFoot = new(-20 - gait, 0), frontFoot = new(22 + gait, 0);
        Vector2 rearKnee = new(-16 + hipX * .4f, -32 + crouch);
        Vector2 frontKnee = new(20 + hipX * .5f, -33 + crouch);
        Limb(a, rearFoot, rearKnee, 11, plate, flip);
        Limb(a, rearKnee, hip + new Vector2(-10, 0), 15, plate, flip);
        Limb(a, frontFoot, frontKnee, 12, plate, flip);
        Limb(a, frontKnee, hip + new Vector2(11, 0), 16, plate, flip);
        Line(a, frontKnee + new Vector2(0, -10), frontKnee + new Vector2(1, 16), 3, Gold, flip);
        Line(a, rearKnee + new Vector2(-5, -3), rearKnee + new Vector2(5, 3), 3, Gold, flip);
        Limb(a, rearFoot + new Vector2(-7, 0), rearFoot + new Vector2(12, 0), 8, PlateEdge, flip);
        Limb(a, frontFoot + new Vector2(-7, 0), frontFoot + new Vector2(16, 0), 8, PlateEdge, flip);
        // Separate chest layers and an exposed articulated waist, not one armor block.
        Limb(a, hip, chest, 30, plate, flip);
        Line(a, chest + new Vector2(-12, -3), hip + new Vector2(-9, -5), 5, PlateEdge, flip);
        Line(a, chest + new Vector2(13, -2), hip + new Vector2(9, -5), 4, Gold, flip);
        Line(a, chest + new Vector2(-9, 4), chest + new Vector2(10, 18), 3, Gold, flip);
        Line(a, chest + new Vector2(10, 4), hip + new Vector2(1, -10), 2, seam, flip);
        Line(a, hip + new Vector2(-17, -1), hip + new Vector2(17, -1), 7, Gold, flip);
        for (int i = -1; i <= 1; i++)
            Limb(a, hip + new Vector2(i * 11, 6), hip + new Vector2(i * 16, 28),
                11, i < 0 ? PlateEdge : plate, flip);
        if (e.CombatPhase == 3)
        {
            Line(a, chest + new Vector2(-11, 3), chest + new Vector2(2, 19), 3, Skin, flip);
            Line(a, frontKnee + new Vector2(-4, -5), frontKnee + new Vector2(6, 6), 2, seam, flip);
        }
        Vector2 head = chest + new Vector2(2, -28);
        DrawCrown(a + head, crownAlign, 1, flip);
        Limb(a, chest + new Vector2(1, -6), head + new Vector2(0, 9), 10, Skin, flip);
        // Exposed face and swept sovereign horns clearly outrank the sealed elites.
        Limb(a, head + new Vector2(-1, -9), head + new Vector2(1, 9), 17, Skin, flip);
        Line(a, head + new Vector2(-6, 2), head + new Vector2(3, 12), 5, PlateEdge, flip);
        Line(a, head + new Vector2(2, -3), head + new Vector2(8, -4), 2,
            e.CombatPhase == 3 ? new Color(236, 132, 209) : new Color(165, 64, 94), flip);
        Limb(a, head + new Vector2(-7, -7), head + new Vector2(-24, -18), 8, Horn, flip);
        Limb(a, head + new Vector2(-24, -18), head + new Vector2(-34, -38), 5, Horn, flip);
        Line(a, head + new Vector2(-34, -38), head + new Vector2(-29, -45), 2, Gold, flip);
        Limb(a, head + new Vector2(7, -8), head + new Vector2(25, -18), 7, Horn, flip);
        Limb(a, head + new Vector2(25, -18), head + new Vector2(34, -36), 4, Horn, flip);

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 foreGrip = chest + new Vector2(28 + reach, 12);
        Vector2 rearGrip = foreGrip - direction * 31;
        Vector2 rearShoulder = chest + new Vector2(-17, 0), frontShoulder = chest + new Vector2(18, 0);
        Vector2 rearElbow = Vector2.Lerp(rearShoulder, rearGrip, .5f) + new Vector2(-11, 14);
        Vector2 frontElbow = Vector2.Lerp(frontShoulder, foreGrip, .5f) + new Vector2(3, 13);
        Limb(a, rearShoulder, rearElbow, 13, plate, flip);
        Limb(a, rearElbow, rearGrip, 10, PlateEdge, flip);
        Limb(a, frontShoulder, frontElbow, 14, plate, flip);
        Limb(a, frontElbow, foreGrip, 10, PlateEdge, flip);
        Line(a, rearShoulder + new Vector2(-8, -6), rearShoulder + new Vector2(8, 0), 6, Gold, flip);
        Line(a, frontShoulder + new Vector2(-8, -6), frontShoulder + new Vector2(12, 1), 7, Gold, flip);
        DrawKingsRuin(a, foreGrip, direction, e.CombatPhase, 1, flip);
        if (e.IsReversing)
            Line(a, foreGrip + new Vector2(11, -32), foreGrip + new Vector2(11, 26), 2,
                new Color(194, 148, 83, 155), flip);
    }

    private void DrawMantle(Vector2 a, Vector2 chest, Vector2 hip, float time, bool torn, bool flip)
    {
        float sway = MathF.Sin(time * 1.8f) * 3;
        Limb(a, chest + new Vector2(-15, -2), new Vector2(-38 + sway, -12), 27, Crimson, flip);
        Limb(a, chest + new Vector2(11, 0), hip + new Vector2(26 + sway, 19), 18,
            new Color(67, 20, 39), flip);
        Line(a, chest + new Vector2(-23, 1), new Vector2(-49 + sway, torn ? -34 : -12), 3, Gold, flip);
        if (torn)
        {
            Line(a, new Vector2(-43 + sway, -32), new Vector2(-54 + sway, -9), 5, Crimson, flip);
            Line(a, hip + new Vector2(30, 12), hip + new Vector2(33, 30), 4, Crimson, flip);
        }
    }
    private void DrawKingsRuin(Vector2 a, Vector2 grip, Vector2 direction, int phase, float opacity, bool flip)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 guard = grip + direction * 16;
        Limb(a, grip - direction * 46, guard, 6, new Color(58, 28, 44) * opacity, flip);
        for (int i = 0; i < 6; i++)
            Line(a, grip + direction * (i * 8 - 34) - normal * 3,
                grip + direction * (i * 8 - 34) + normal * 3, 2, Gold * opacity, flip);
        // Old-gold swept royal guard, not a polearm socket or axe-shaped head.
        Limb(a, guard - normal * 20 - direction * 4, guard, 6, Gold * opacity, flip);
        Limb(a, guard, guard + normal * 18 - direction * 7, 6, Gold * opacity, flip);
        Color channel = phase == 1 ? new Color(66, 35, 84) : phase == 2 ? Core : new Color(213, 120, 235);
        // Two tapered dark edges around an actual split central channel.
        for (int i = 0; i < 12; i++)
        {
            float u = i / 12f, v = (i + 1) / 12f;
            float width = MathHelper.Lerp(13, 1.5f, u);
            float offset = MathHelper.Lerp(8, 1.5f, u);
            Vector2 start = guard + direction * (7 + 117 * u);
            Vector2 end = guard + direction * (7 + 117 * v);
            Line(a, start + normal * offset, end + normal * MathHelper.Lerp(8, 1.5f, v),
                width, PlateEdge * opacity, flip);
            Line(a, start - normal * offset, end - normal * MathHelper.Lerp(8, 1.5f, v),
                width, new Color(29, 21, 39) * opacity, flip);
        }
        Line(a, guard + direction * 8, guard + direction * 116, phase == 3 ? 4 : 2, channel * opacity, flip);
        Line(a, guard + direction * 17 + normal * 14, guard + direction * 51 + normal * 10, 2, Gold * opacity, flip);
        Line(a, guard + direction * 120, guard + direction * 132, 2, channel * opacity, flip);
    }
    private void DrawCrown(Vector2 head, float alignment, float opacity, bool flip)
    {
        for (int i = 0; i < 7; i++)
        {
            float angle = MathHelper.Pi + i * MathHelper.Pi / 6;
            Vector2 center = new(MathF.Cos(angle) * 31, MathF.Sin(angle) * (34 + alignment * 5));
            Line(head, center, center + new Vector2(MathF.Cos(angle) * 5, -9 - alignment * 6),
                4, Gold * opacity, flip);
            if (alignment > .25f)
                Line(head, center + new Vector2(0, -3), center + new Vector2(0, -7), 2,
                    Core * (opacity * alignment), flip);
        }
    }
    private void DrawEffects(VaeltharEnemy e)
    {
        foreach (VaeltharLane lane in e.Lanes)
        {
            if (!lane.IsVisible(e.PatternAge)) continue;
            bool active = lane.IsFinisher ? e.CombatState == VaeltharState.FinalActive : lane.IsActive(e.PatternAge);
            Color color = active ? new Color(198, 112, 222, 210) : new Color(151, 92, 177, 150);
            Vector2 c = lane.Position;
            Ring(c, lane.Width / 2, 11, color);
            Line(c, new Vector2(-lane.Width / 2, 0), new Vector2(lane.Width / 2, 0), 2, Gold, false);
            for (int i = 0; i < lane.Order; i++)
                Line(c, new Vector2((i - (lane.Order - 1) * .5f) * 9, -12),
                    new Vector2((i - (lane.Order - 1) * .5f) * 9, -25), 3, Gold, false);
            if (active)
                for (int i = -1; i <= 1; i++)
                    Line(c, new Vector2(i * lane.Width * .28f, -2),
                        new Vector2(i * lane.Width * .2f, -148), 8, color, false);
            else if (lane.IsFinisher)
            {
                Line(c, new Vector2(-16, -18), new Vector2(0, -38), 2, color, false);
                Line(c, new Vector2(16, -18), new Vector2(0, -38), 2, color, false);
            }
        }
        if (e.CombatState == VaeltharState.MoveTell)
        {
            Line(Vector2.Zero, e.MovementOrigin, e.MovementDestination, 1, new Color(171, 116, 195, 110), false);
            Ring(e.MovementDestination + new Vector2(0, e.Size.Y / 2), 28, 8, Gold);
        }
        if (e.HasWave)
        {
            Vector2 c = e.WavePosition;
            Line(c, new Vector2(-e.WaveDirection * 14, -29), new Vector2(e.WaveDirection * 7, 0), 7, Core, false);
            Line(c, new Vector2(e.WaveDirection * 7, 0), new Vector2(-e.WaveDirection * 14, 29), 7, Core, false);
            Line(c, new Vector2(-e.WaveDirection * 17, -24), new Vector2(-e.WaveDirection * 17, 24), 2,
                new Color(109, 61, 146, 105), false);
        }
    }
    private void DrawDeath(VaeltharEnemy e, Vector2 a, bool flip)
    {
        float p = e.DeathPresentationProgress;
        float kneel = Math.Clamp((p - .20f) / .38f, 0, 1);
        float fall = Math.Clamp((p - .64f) / .28f, 0, 1);
        Vector2 hip = new(9 * kneel + 15 * fall, -61 + 30 * kneel + 15 * fall);
        Vector2 chest = new(12 * kneel + 42 * fall, -109 + 57 * kneel + 35 * fall);
        DrawMantle(a, chest, hip, 0, true, flip);
        Limb(a, new Vector2(-21, 0), hip, 13, Plate, flip);
        Limb(a, new Vector2(23, 0), hip, 13, PlateEdge, flip);
        Limb(a, hip, chest, 30, Plate, flip);
        Line(a, chest + new Vector2(-10, -2), hip + new Vector2(-8, 0), 4, Gold, flip);
        Vector2 head = chest + new Vector2(7 * fall, -27 + fall * 14);
        DrawCrown(a + head, .5f, Math.Clamp(1 - p * 1.7f, 0, 1), flip);
        Limb(a, head + new Vector2(-1, -9), head + new Vector2(1, 9), 16, Skin, flip);
        Line(a, head + new Vector2(-7, -7), head + new Vector2(-29, -31), 6, Horn, flip);
        Line(a, head + new Vector2(7, -7), head + new Vector2(29, -30), 5, Horn, flip);
        // Remains upright briefly, then leans on the blade, loosens the grip,
        // and collapses while King's Ruin stays visible beside the throne.
        Vector2 grip = new(38 + 9 * fall, -68 + 24 * kneel + 37 * fall);
        Vector2 elbow = Vector2.Lerp(chest, grip, .5f) + new Vector2(0, 12);
        Limb(a, chest + new Vector2(15, 0), elbow, 12, Plate, flip);
        Limb(a, elbow, grip + new Vector2(-10 * fall, -8 * fall), 9, PlateEdge, flip);
        float angle = MathHelper.Lerp(.70f, .27f, kneel);
        if (fall > 0) angle = MathHelper.Lerp(.27f, -.02f, fall);
        DrawKingsRuin(a, grip, new Vector2(MathF.Cos(angle), MathF.Sin(angle)),
            1, 1 - MathF.Max(0, (p - .94f) / .06f) * .3f, flip);
    }
    private void Ring(Vector2 c, float rx, float ry, Color color)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = i * MathHelper.TwoPi / 24, b = (i + 1) * MathHelper.TwoPi / 24;
            Line(c, new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry),
                new Vector2(MathF.Cos(b) * rx, MathF.Sin(b) * ry), 2, color, false);
        }
    }
    private void Limb(Vector2 a, Vector2 x, Vector2 y, float width, Color color, bool flip)
    { Line(a, x, y, width + 3, Outline, flip); Line(a, x, y, width, color, flip); }
    private void Line(Vector2 a, Vector2 x, Vector2 y, float width, Color color, bool flip)
    {
        if (flip) { x.X = -x.X; y.X = -y.X; }
        Vector2 delta = y - x; float length = delta.Length(); if (length < .1f) return;
        _batch.Draw(_pixel, a + x, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(length, width), SpriteEffects.None, 0f);
    }
    private sealed class Clock { public float Elapsed; }
}
