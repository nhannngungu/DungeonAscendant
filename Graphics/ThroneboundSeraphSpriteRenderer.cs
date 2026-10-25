using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Strained anatomy, ruined regalia, broken wing frames and physical chains.</summary>
public sealed class ThroneboundSeraphSpriteRenderer
{
    public static readonly Vector2 VisualSize = new(470, 226);
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<ThroneboundSeraphEnemy, Clock> _clocks = new();
    private static readonly Color Outline = new(16, 12, 21);
    private static readonly Color Ivory = new(174, 165, 143);
    private static readonly Color Ash = new(117, 112, 111);
    private static readonly Color Gold = new(145, 109, 59);
    private static readonly Color Black = new(43, 35, 48);
    private static readonly Color Flesh = new(92, 53, 70);
    private static readonly Color Violet = new(145, 74, 163);
    private static readonly Color Crimson = new(100, 34, 53);
    private static readonly Color ChainMetal = new(102, 87, 96);

    public ThroneboundSeraphSpriteRenderer(SpriteBatch batch, Texture2D pixel)
    { _batch = batch; _pixel = pixel; }

    public void Draw(ThroneboundSeraphEnemy e, GameTime time)
    {
        Clock clock = _clocks.GetValue(e, static _ => new Clock());
        clock.Elapsed += MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        bool flip = e.CombatState != SeraphState.Ready ? e.AttackFacesLeft :
            e.Facing == EnemyFacingDirection.Left;
        Vector2 a = new(e.Position.X, e.Bounds.Bottom);
        DrawOwnedEffects(e);
        if (!e.IsAlive) { DrawDeath(e, a, flip); return; }

        float p = e.ActionProgress, t = clock.Elapsed;
        float stride = MathF.Abs(e.VisualVelocityX) > 2 ? MathF.Sin(t * 4f) * 7f : 0;
        float hipX = 0, shoulderX = -3, crouch = 0, spearAngle = -.57f, gripReach = 0;
        float spread = 0, wingTurn = 0;
        switch (e.CombatState)
        {
            case SeraphState.Intro:
                crouch = (1 - p) * 11; shoulderX = -8; break;
            case SeraphState.PhaseShift:
                crouch = 9 * MathF.Sin(p * MathHelper.Pi);
                shoulderX = -11; spread = 9 * p; spearAngle = -1.25f; break;
            case SeraphState.ThrustWindup:
                hipX = -8 * p; shoulderX = -10 * p; crouch = 7 * p;
                spearAngle = MathHelper.Lerp(-.57f, -.08f, p); gripReach = -17 * p; break;
            case SeraphState.ThrustActive:
                hipX = 10; shoulderX = 19; crouch = 9;
                spearAngle = -.08f; gripReach = 40; break;
            case SeraphState.ThrustRecovery:
                hipX = 10 * (1 - p); shoulderX = 19 * (1 - p); crouch = 7 * (1 - p);
                spearAngle = -.08f - .49f * p; gripReach = 32 * (1 - p); break;
            case SeraphState.WingWindup:
                hipX = -6 * p; shoulderX = -14 * p; crouch = 5 * p;
                wingTurn = -22 * p; spearAngle = -1.1f; break;
            case SeraphState.WingActive:
                hipX = 9; shoulderX = 16; crouch = 6;
                wingTurn = MathHelper.Lerp(-22, 74, p); spearAngle = -1.1f; break;
            case SeraphState.WingRecovery:
                shoulderX = 10 * (1 - p); wingTurn = 74 * (1 - p); break;
            case SeraphState.ChainWindup:
                shoulderX = -9 * p; hipX = -3 * p; spearAngle = -1.32f; break;
            case SeraphState.ChainExtend:
                shoulderX = 9; hipX = 5; spearAngle = -1.32f; break;
            case SeraphState.ChainRetract:
                shoulderX = 7 * (1 - p); spearAngle = -1.32f; break;
            case SeraphState.AscentWindup:
                crouch = 16 * p; spread = -8 * p; spearAngle = -1.5f; break;
            case SeraphState.AscentRise:
                crouch = 6; spread = 27; spearAngle = -1.5f; stride = -9; break;
            case SeraphState.AscentHover:
                crouch = 4; spread = 25; spearAngle = MathHelper.Lerp(-1.5f, .95f, p); stride = -7; break;
            case SeraphState.AscentDescent:
                crouch = 4; spread = 13; shoulderX = 9; spearAngle = 1.05f; gripReach = 7; break;
            case SeraphState.AscentStrike:
                crouch = 19; shoulderX = 13; spread = -7; spearAngle = 1.05f; break;
            case SeraphState.AscentRecovery:
                crouch = 19 * (1 - p); spearAngle = MathHelper.Lerp(1.05f, -.57f, p); break;
            case SeraphState.HaloCharge:
                shoulderX = -8; spearAngle = -1.35f; spread = 5 * p; break;
            case SeraphState.HaloActive:
                shoulderX = 1; spearAngle = -1.35f; spread = 10; break;
        }
        if (e.CombatPhase == 2) { hipX += 3; shoulderX += 4; }
        if (e.IsStaggered || e.IsHitFlashing)
        { hipX = -7; shoulderX = -14; crouch = 13; spearAngle = .7f; }
        Vector2 hip = new(hipX, -55 + crouch);
        Vector2 chest = new(shoulderX, -99 + crouch);
        Color plate = e.IsHitFlashing ? new Color(208, 170, 159) : Ivory;
        Color seam = e.CombatPhase == 2 ? new Color(186, 73, 168) : new Color(113, 53, 126);

        // Exactly two asymmetrical physical frames. One loses its distal brace
        // in phase two; neither is a feather fan or a free-flight animation.
        DrawWing(a, chest + new Vector2(-12, 3), true, spread, 0, false, flip);
        DrawWing(a, chest + new Vector2(12, 4), false, spread, wingTurn,
            e.CombatPhase == 2, flip);
        Vector2 rearFoot = new(-17 - stride, 0), foreFoot = new(19 + stride, 0);
        Vector2 rearKnee = new(-16 + hipX * .3f, -29 + crouch);
        Vector2 foreKnee = new(18 + hipX * .5f, -30 + crouch);
        Limb(a, rearFoot, rearKnee, 9, Ash, flip);
        Limb(a, rearKnee, hip + new Vector2(-9, 0), 11, Black, flip);
        Limb(a, foreFoot, foreKnee, 10, plate, flip);
        Limb(a, foreKnee, hip + new Vector2(9, 0), 12, Flesh, flip);
        Line(a, foreKnee + new Vector2(2, -8), foreKnee + new Vector2(3, 12), 3, Gold, flip);
        Line(a, rearFoot + new Vector2(-7, 0), rearFoot + new Vector2(10, 0), 7, Black, flip);
        Line(a, foreFoot + new Vector2(-6, 0), foreFoot + new Vector2(13, 0), 7, plate, flip);
        // Long exposed waist with individually layered chest and fauld remnants.
        Limb(a, hip, chest, 21, Flesh, flip);
        Limb(a, chest + new Vector2(-9, -3), chest + new Vector2(-7, 25), 14, plate, flip);
        Limb(a, chest + new Vector2(9, -2), chest + new Vector2(6, 23), 13, Black, flip);
        Line(a, chest + new Vector2(-8, 1), chest + new Vector2(5, 24), 3, Gold, flip);
        Line(a, chest + new Vector2(8, 0), hip + new Vector2(3, -5), 2, seam, flip);
        Line(a, hip + new Vector2(-14, -1), hip + new Vector2(14, -1), 6, Gold, flip);
        for (int i = -1; i <= 1; i++)
            Limb(a, hip + new Vector2(i * 10, 5), hip + new Vector2(i * 14, 24),
                10, i == 1 ? Black : Ash, flip);
        Line(a, hip + new Vector2(-6, 8), new Vector2(-18, -14), 8, Crimson, flip);
        Vector2 neck = chest + new Vector2(-1, -15), head = neck + new Vector2(0, -13);
        DrawHalo(a + head, e.CombatState == SeraphState.HaloCharge ? p : 0, 0, flip);
        Limb(a, chest, neck, 8, Flesh, flip);
        Limb(a, head + new Vector2(-1, -10), head + new Vector2(1, 10), 14, plate, flip);
        Line(a, head + new Vector2(2, -2), head + new Vector2(7, -3), 2, seam, flip);
        Line(a, head + new Vector2(-5, -8), head + new Vector2(-8, -21), 3, Gold, flip);
        Line(a, head + new Vector2(4, 5), head + new Vector2(1, 15), 4, Black, flip);

        Vector2 spearDirection = new(MathF.Cos(spearAngle), MathF.Sin(spearAngle));
        Vector2 foreGrip = chest + new Vector2(25 + gripReach, 14);
        Vector2 rearGrip = foreGrip - spearDirection * 38;
        Vector2 rearShoulder = chest + new Vector2(-14, 1), foreShoulder = chest + new Vector2(15, 1);
        Vector2 rearElbow = Vector2.Lerp(rearShoulder, rearGrip, .5f) + new Vector2(-10, 13);
        Vector2 foreElbow = Vector2.Lerp(foreShoulder, foreGrip, .5f) + new Vector2(3, 12);
        bool throwing = e.HasChain;
        if (throwing)
        {
            float extension = e.CombatState == SeraphState.ChainWindup ? -8 * p :
                e.CombatState == SeraphState.ChainExtend ? 27 : 27 * (1 - p);
            rearGrip = chest + new Vector2(15 + extension, 20);
            rearElbow = chest + new Vector2(-6, 16);
        }
        Limb(a, rearShoulder, rearElbow, 9, Flesh, flip);
        Limb(a, rearElbow, rearGrip, 7, Ash, flip);
        Limb(a, foreShoulder, foreElbow, 11, plate, flip);
        Limb(a, foreElbow, foreGrip, 8, Black, flip);
        Line(a, foreShoulder + new Vector2(-6, -5), foreShoulder + new Vector2(9, 1), 5, Gold, flip);
        DrawSpear(a, foreGrip, spearDirection, seam, flip);

        // Logical clasps at chest/waist/back; three chains with restrained sag.
        float sag = e.CombatPhase == 2 ? 4f : 10f;
        DrawChain(a, chest + new Vector2(-13, 10), hip + new Vector2(-34, 18),
            sag + MathF.Sin(t * 2.5f) * 2, flip, 1);
        DrawChain(a, hip + new Vector2(13, 0), new Vector2(40, -12),
            sag + MathF.Sin(t * 2.2f + 1) * 2, flip, 1);
        DrawChain(a, chest + new Vector2(10, 15), hip + new Vector2(-13, 8),
            sag * .45f, flip, 1);
    }

    private void DrawWing(Vector2 a, Vector2 root, bool rear, float spread, float turn, bool collapsed, bool flip)
    {
        float side = rear ? -1 : 1;
        Vector2 elbow = root + new Vector2(side * (35 + spread * .4f) + turn * .25f, -27 - spread * .6f);
        Vector2 tip = elbow + new Vector2(side * (rear ? 36 : 20) + turn, rear ? -35 : collapsed ? 33 : -18);
        Limb(a, root, elbow, 7, Black, flip);
        Limb(a, elbow, tip, 5, rear ? Ash : Gold, flip);
        Line(a, root + new Vector2(0, -2), elbow + new Vector2(0, -3), 2, Gold, flip);
        // Unequal braces, torn scraps and vacant space define the broken wings.
        int braces = rear ? 3 : 2;
        for (int i = 0; i < braces; i++)
        {
            Vector2 hinge = Vector2.Lerp(elbow, tip, .15f + i * .28f);
            Vector2 end = hinge + new Vector2(side * (14 + i * 5), 32 + i * 8 + (collapsed ? 12 : 0));
            Limb(a, hinge, end, 3, Ash, flip);
            if (i != 1)
                Line(a, hinge + new Vector2(0, 8), end + new Vector2(-side * 7, -6), 4,
                    new Color(87, 43, 72, 145), flip);
        }
    }
    private void DrawSpear(Vector2 a, Vector2 grip, Vector2 direction, Color seam, bool flip)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 socket = grip + direction * 47;
        Limb(a, grip - direction * 70, socket, 5, Black, flip);
        Line(a, grip - direction * 64 - normal, socket - normal, 1, Gold, flip);
        // Broken halo socket and broad, tapered sacred leaf blade: visibly a
        // great spear, without Enemy 68's sword crossguard or straight blade.
        for (int i = 0; i < 10; i++)
        {
            if (i is 3 or 7) continue;
            float angleA = i * MathHelper.TwoPi / 10, angleB = (i + 1) * MathHelper.TwoPi / 10;
            Line(a, socket + direction * (MathF.Cos(angleA) * 12) + normal * (MathF.Sin(angleA) * 12),
                socket + direction * (MathF.Cos(angleB) * 12) + normal * (MathF.Sin(angleB) * 12), 3, Gold, flip);
        }
        Vector2 basePoint = socket + direction * 4, broad = socket + direction * 27;
        Vector2 point = socket + direction * 78;
        Line(a, basePoint, broad, 13, Ivory, flip);
        Line(a, broad, socket + direction * 49, 17, Ivory, flip);
        Line(a, socket + direction * 47, socket + direction * 63, 11, Ivory, flip);
        Line(a, socket + direction * 60, point, 5, Ivory, flip);
        Line(a, basePoint + normal * 3, point, 3, Black, flip);
        Line(a, basePoint, socket + direction * 58, 2, seam, flip);
        Line(a, broad - normal * 5, socket + direction * 59 - normal * 2, 2,
            new Color(215, 199, 151), flip);
    }
    private void DrawHalo(Vector2 center, float charge, float collapse, bool flip)
    {
        for (int i = 0; i < 18; i++)
        {
            if (i is 2 or 3 or 11 or 12 or 16) continue;
            float a = i * MathHelper.TwoPi / 18, b = (i + 1) * MathHelper.TwoPi / 18;
            Vector2 shift = new(MathF.Cos(a) * collapse * 15, collapse * (8 + i));
            Line(center, new Vector2(MathF.Cos(a) * 24, MathF.Sin(a) * 27) + shift,
                new Vector2(MathF.Cos(b) * 24, MathF.Sin(b) * 27) + shift, 3,
                Color.Lerp(Gold, new Color(199, 131, 198), charge), flip);
        }
    }
    private void DrawOwnedEffects(ThroneboundSeraphEnemy e)
    {
        if (e.HasChain)
        {
            if (e.CombatState == SeraphState.ChainWindup)
            {
                Line(Vector2.Zero, e.ChainOrigin, e.ChainDestination, 1,
                    new Color(177, 116, 164, 155), false);
                Ring(e.ChainDestination, 9, 9, Gold);
            }
            else
            {
                float amount = e.CombatState == SeraphState.ChainExtend ? e.ActionProgress : 1 - e.ActionProgress;
                DrawChain(Vector2.Zero, e.ChainOrigin,
                    Vector2.Lerp(e.ChainOrigin, e.ChainDestination, amount), 2, false, 1);
            }
        }
        if (e.HasLandingMarker)
        {
            Vector2 foot = e.LandingPosition + new Vector2(0, e.Size.Y / 2);
            Ring(foot, 46, 11, new Color(180, 125, 184, 165));
            Line(foot, new Vector2(-8, -7), new Vector2(8, -7), 2, Gold, false);
        }
        if (e.HaloLinesVisible)
        {
            Color color = e.CombatState == SeraphState.HaloActive ?
                new Color(211, 150, 213, 215) : new Color(170, 123, 184, 140);
            for (int i = 0; i < 3; i++)
                Line(Vector2.Zero, e.HaloOrigin, e.HaloRayEnd(i),
                    e.CombatState == SeraphState.HaloActive ? (e.CombatPhase == 2 ? 10 : 8) : 2, color, false);
        }
    }
    private void DrawDeath(ThroneboundSeraphEnemy e, Vector2 a, bool flip)
    {
        float p = e.DeathPresentationProgress;
        float kneel = Math.Clamp(p / .6f, 0, 1);
        Vector2 hip = new(7 * kneel, -55 + 28 * kneel);
        Vector2 chest = new(24 * kneel, -99 + 64 * kneel);
        DrawWing(a, chest + new Vector2(-12, 0), true, -17 * p, 0, false, flip);
        DrawWing(a, chest + new Vector2(12, 3), false, -20 * p, 28 * p, true, flip);
        Limb(a, new Vector2(-18, 0), hip, 10, Ash, flip);
        Limb(a, new Vector2(21, 0), hip, 11, Black, flip);
        Limb(a, hip, chest, 22, Ivory, flip);
        Line(a, chest + new Vector2(8, 0), hip, 8, Flesh, flip);
        Vector2 head = chest + new Vector2(10 * p, -27 + 8 * p);
        DrawHalo(a + head, 0, p, flip);
        Limb(a, head + new Vector2(0, -9), head + new Vector2(2, 9), 13, Ash, flip);
        DrawChain(a, chest + new Vector2(-13, 8), hip + new Vector2(-38, 18), 10 + p * 28, flip, 1);
        DrawChain(a, hip + new Vector2(12, 0), new Vector2(42, -5), 8 + p * 24, flip, 1);
        Vector2 droppedGrip = new(43 + p * 9, -68 + 63 * kneel);
        float angle = MathHelper.Lerp(-.5f, -.015f, kneel);
        DrawSpear(a, droppedGrip, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), Black, flip);
    }
    private void DrawChain(Vector2 a, Vector2 start, Vector2 end, float sag, bool flip, float opacity)
    {
        int links = Math.Clamp((int)(Vector2.Distance(start, end) / 8), 3, 44);
        Vector2 prior = start;
        for (int i = 1; i <= links; i++)
        {
            float p = (float)i / links;
            Vector2 current = Vector2.Lerp(start, end, p) + new Vector2(0, MathF.Sin(p * MathHelper.Pi) * sag);
            Line(a, prior, current, i % 2 == 0 ? 3 : 2, ChainMetal * opacity, flip);
            if (i % 2 == 0) Line(a, current + new Vector2(-2, -2), current + new Vector2(2, 2),
                1, Gold * opacity, flip);
            prior = current;
        }
    }
    private void Ring(Vector2 c, float rx, float ry, Color color)
    {
        for (int i = 0; i < 20; i++)
        {
            float a = i * MathHelper.TwoPi / 20, b = (i + 1) * MathHelper.TwoPi / 20;
            Line(c, new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry),
                new Vector2(MathF.Cos(b) * rx, MathF.Sin(b) * ry), 2, color, false);
        }
    }
    private void Limb(Vector2 a, Vector2 start, Vector2 end, float width, Color color, bool flip)
    { Line(a, start, end, width + 3, Outline, flip); Line(a, start, end, width, color, flip); }
    private void Line(Vector2 a, Vector2 start, Vector2 end, float width, Color color, bool flip)
    {
        if (flip) { start.X = -start.X; end.X = -end.X; }
        Vector2 delta = end - start;
        float length = delta.Length(); if (length < .1f) return;
        _batch.Draw(_pixel, a + start, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(length, width), SpriteEffects.None, 0f);
    }
    private sealed class Clock { public float Elapsed; }
}
