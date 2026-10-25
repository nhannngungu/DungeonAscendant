using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Body-first royal duelist and staffless seer silhouettes.</summary>
public sealed class DemonKingSanctumEliteSpriteRenderer
{
    public static readonly Vector2 KnightVisualSize = new(242, 166);
    public static readonly Vector2 OracleVisualSize = new(178, 168);
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonKingSanctumEliteEnemy, Clock> _clocks = new();
    private static readonly Color Outline = new(15, 13, 20);
    private static readonly Color Plate = new(46, 43, 51);
    private static readonly Color PlateLight = new(86, 79, 87);
    private static readonly Color Gold = new(153, 117, 65);
    private static readonly Color Crimson = new(94, 29, 43);
    private static readonly Color Steel = new(174, 168, 162);
    private static readonly Color Robe = new(45, 38, 62);
    private static readonly Color RobeLight = new(83, 66, 99);
    private static readonly Color Skin = new(138, 119, 142);
    private static readonly Color Abyss = new(157, 125, 199);

    public DemonKingSanctumEliteSpriteRenderer(SpriteBatch batch, Texture2D pixel)
    { _batch = batch; _pixel = pixel; }

    public void Draw(DemonKingSanctumEliteEnemy enemy, GameTime time)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Elapsed += MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        bool flip = enemy.CombatState != SanctumEliteState.Ready ? enemy.AttackFacesLeft :
            enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        Line(anchor, new Vector2(-24, 0), new Vector2(24, 0), 5, new Color(8, 6, 15, 145), false);
        DrawProphecies(enemy);
        if (!enemy.IsAlive) { DrawDeath(enemy, anchor, flip); return; }
        if (enemy.Type == EnemyType.RoyalSanctumKnight)
            DrawKnight(enemy, anchor, flip, clock.Elapsed);
        else DrawOracle(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawKnight(DemonKingSanctumEliteEnemy e, Vector2 a, bool f, float elapsed)
    {
        float p = e.ActionProgress;
        bool windup = e.CombatState == SanctumEliteState.Windup;
        bool active = e.CombatState == SanctumEliteState.Active;
        float gait = MathF.Abs(e.VisualVelocityX) > 2f ? MathF.Sin(elapsed * 6f) * 7f : 0f;
        float hipX = 0, shoulderX = 4, crouch = 0, angle = -.55f, reach = 0;
        switch (e.AttackKind)
        {
            case SanctumEliteAttack.RoyalSever:
                hipX = windup ? -5f * p : active ? 8f : 4f;
                shoulderX = windup ? -7f * p : 12f;
                angle = windup ? MathHelper.Lerp(-.55f, -1.95f, p) :
                    active ? MathHelper.Lerp(-1.95f, .62f, p) : .62f - p * .4f;
                break;
            case SanctumEliteAttack.ThroneThrust:
            case SanctumEliteAttack.RoyalRiposte:
                crouch = 7;
                hipX = windup ? -7f : active ? 13f : 7f * (1f - p);
                shoulderX = hipX + (active ? 10f : 1f);
                angle = -.08f;
                reach = windup ? -15f : active ? 46f : 24f * (1f - p);
                break;
            case SanctumEliteAttack.CrownSweep:
                hipX = windup ? -8f : 11f; shoulderX = hipX + (active ? 13f : -4f);
                crouch = 9;
                angle = windup ? MathHelper.Lerp(-.55f, -2.7f, p) :
                    active ? MathHelper.Lerp(-2.7f, .32f, p) : .32f;
                break;
            case SanctumEliteAttack.SovereignGuard:
                crouch = 5; hipX = -5; shoulderX = -2; angle = -1.18f; reach = 4;
                break;
        }
        if (e.IsStaggered || e.IsHitFlashing)
        { hipX = -8; shoulderX = -13; crouch = 9; angle = .9f; }
        Color armor = e.IsHitFlashing ? new Color(158, 131, 123) : Plate;
        Vector2 hip = new(hipX, -49 + crouch);
        Vector2 chest = new(shoulderX, -82 + crouch);
        Vector2 rearFoot = new(-17 - gait, 0), frontFoot = new(18 + gait, 0);
        Vector2 rearKnee = new(-14 + hipX * .4f, -25 + crouch);
        Vector2 frontKnee = new(17 + hipX * .5f, -26 + crouch);

        // Two individually hanging mantle tails reveal the legs and hip rotation.
        Limb(a, chest + new Vector2(-13, 1), new Vector2(-26 - gait * .4f, -10),
            16, Crimson, f);
        Limb(a, chest + new Vector2(9, 3), new Vector2(1 - gait * .3f, -15),
            12, new Color(69, 22, 36), f);
        Line(a, chest + new Vector2(-18, 4), new Vector2(-31, -12), 2, Gold, f);
        Limb(a, rearFoot, rearKnee, 10, armor, f);
        Limb(a, rearKnee, hip + new Vector2(-9, 0), 13, armor, f);
        Limb(a, frontFoot, frontKnee, 11, armor, f);
        Limb(a, frontKnee, hip + new Vector2(9, 0), 14, armor, f);
        Line(a, rearKnee + new Vector2(-5, -2), rearKnee + new Vector2(5, 2), 3, Gold, f);
        Line(a, frontKnee + new Vector2(-6, -2), frontKnee + new Vector2(6, 2), 3, Gold, f);
        Limb(a, rearFoot + new Vector2(-6, -1), rearFoot + new Vector2(10, -1), 7, PlateLight, f);
        Limb(a, frontFoot + new Vector2(-7, -1), frontFoot + new Vector2(13, -1), 7, PlateLight, f);
        Limb(a, hip, chest, 27, armor, f);
        Line(a, chest + new Vector2(-9, -3), hip + new Vector2(-6, -2), 3, Gold, f);
        Line(a, chest + new Vector2(10, -3), hip + new Vector2(7, -2), 3, PlateLight, f);
        Line(a, hip + new Vector2(-14, 0), hip + new Vector2(14, 0), 6, Gold, f);
        for (int i = -1; i <= 1; i++)
            Limb(a, hip + new Vector2(i * 10, 4), hip + new Vector2(i * 13, 19), 9, armor, f);
        Vector2 neck = chest + new Vector2(2, -15), head = neck + new Vector2(1, -12);
        Limb(a, chest, neck, 9, PlateLight, f);
        Limb(a, head + new Vector2(0, -10), head + new Vector2(2, 10), 15, armor, f);
        Line(a, head + new Vector2(-6, -5), head + new Vector2(6, -5), 2, Gold, f);
        Line(a, head + new Vector2(1, -2), head + new Vector2(8, -2), 2,
            new Color(153, 52, 57), f);
        // Restrained swept horn crest, with a narrow ceremonial face plate.
        Line(a, head + new Vector2(-5, -8), head + new Vector2(-10, -21), 4, Gold, f);
        Line(a, head + new Vector2(5, -8), head + new Vector2(8, -19), 3, Gold, f);
        Line(a, head + new Vector2(4, 1), head + new Vector2(1, 13), 3, Steel, f);

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 cross = new(-direction.Y, direction.X);
        Vector2 foreGrip = chest + new Vector2(24 + reach, 9);
        Vector2 rearGrip = foreGrip - direction * 27f;
        Vector2 rearShoulder = chest + new Vector2(-13, -1);
        Vector2 foreShoulder = chest + new Vector2(14, -1);
        Vector2 rearElbow = Vector2.Lerp(rearShoulder, rearGrip, .5f) + new Vector2(-8, 10);
        Vector2 foreElbow = Vector2.Lerp(foreShoulder, foreGrip, .5f) + new Vector2(1, 10);
        Limb(a, rearShoulder, rearElbow, 10, armor, f);
        Limb(a, rearElbow, rearGrip, 8, PlateLight, f);
        Limb(a, foreShoulder, foreElbow, 11, armor, f);
        Limb(a, foreElbow, foreGrip, 8, PlateLight, f);
        Line(a, rearShoulder + new Vector2(-7, -4), rearShoulder + new Vector2(5, 1), 5, Gold, f);
        Line(a, foreShoulder + new Vector2(-5, -4), foreShoulder + new Vector2(9, 0), 5, Gold, f);
        // Royal blade-spear: long two-hand grip, proper crossguard and a broad
        // extended sword blade with taper, fuller and edge. No polearm axe head.
        Vector2 guard = foreGrip + direction * 15f;
        Limb(a, rearGrip - direction * 18f, guard, 5, new Color(61, 38, 37), f);
        for (int i = 0; i < 5; i++)
            Line(a, rearGrip + direction * (i * 7 - 9) - cross * 3,
                rearGrip + direction * (i * 7 - 9) + cross * 3, 2, Gold, f);
        Line(a, guard - cross * 12, guard + cross * 12, 5, Gold, f);
        Limb(a, guard + direction * 3, guard + direction * 65, 12, Steel, f);
        Line(a, guard + direction * 7, guard + direction * 62, 3, PlateLight, f);
        Line(a, guard + direction * 63, guard + direction * 82, 7, Steel, f);
        Line(a, guard + direction * 75, guard + direction * 91, 3, Steel, f);
        if (e.IsSovereignGuarding)
            Line(a, foreGrip + new Vector2(7, -28), foreGrip + new Vector2(7, 25),
                2, new Color(201, 162, 89, 175), f);
        if (e.AttackKind == SanctumEliteAttack.RoyalRiposte && windup)
            Ring(a + new Vector2(0, -88), 11, 11, Gold, false);
    }

    private void DrawOracle(DemonKingSanctumEliteEnemy e, Vector2 a, bool f, float elapsed)
    {
        float p = e.ActionProgress;
        bool casting = e.CombatState == SanctumEliteState.Windup;
        float breath = MathF.Sin(elapsed * 1.8f) * 1.5f;
        float shoulderTurn = casting ? 4f * MathF.Sin(p * MathHelper.Pi) : 0f;
        Vector2 hip = new(0, -38), chest = new(shoulderTurn, -78 + breath);
        Vector2 head = chest + new Vector2(0, -24);
        // Elongated ritual halo BEHIND the skull; close-body lenses belong to
        // this silhouette rather than a cloud of free-floating particles.
        Ring(a + head, 22, 34, Gold, false);
        Line(a, head + new Vector2(-11, -29), head + new Vector2(-14, -45), 3, Gold, f);
        Line(a, head + new Vector2(11, -29), head + new Vector2(14, -45), 3, Gold, f);
        Line(a, head + new Vector2(0, -34), head + new Vector2(0, -52), 3, Gold, f);
        for (int i = 0; i < 4; i++)
        {
            Vector2 lens = new((i % 2 == 0 ? -1 : 1) * (28 + (i / 2) * 4), -59 + (i / 2) * 25);
            Ring(a + lens, 5, 8, Gold, false);
            Line(a, lens + new Vector2(0, -3), lens + new Vector2(0, 3), 2,
                casting ? Abyss : new Color(97, 77, 127), f);
        }
        // Separate layered robe panels preserve a slender upper torso and long arms.
        Limb(a, hip, chest, 23, Robe, f);
        for (int i = -1; i <= 1; i++)
        {
            Vector2 top = hip + new Vector2(i * 8, 0);
            Vector2 hem = new(i * 19 + MathF.Sin(elapsed * 2 + i) * 2, -4 - Math.Abs(i) * 3);
            Limb(a, top, hem, 17, i == 0 ? RobeLight : Robe, f);
            Line(a, top + new Vector2(5, 0), hem + new Vector2(5, -3), 2, Gold, f);
        }
        Line(a, chest + new Vector2(-8, -4), hip, 3, Gold, f);
        Line(a, chest + new Vector2(8, -4), hip, 3, Gold, f);
        Limb(a, chest + new Vector2(0, -8), head, 9, Skin, f);
        Limb(a, head + new Vector2(0, -8), head + new Vector2(0, 8), 13, Skin, f);
        Line(a, head + new Vector2(-7, -3), head + new Vector2(7, -3), 6, Outline, f);
        Line(a, head + new Vector2(-6, -5), head + new Vector2(6, -5), 2, Gold, f);
        Vector2 leftShoulder = chest + new Vector2(-11, 0);
        Vector2 rightShoulder = chest + new Vector2(11, 0);
        float lift = casting ? MathF.Sin(MathF.Min(1f, p * 3f) * MathHelper.PiOver2) : 0f;
        Vector2 leftElbow = leftShoulder + new Vector2(-11, 17 - lift * 13);
        Vector2 rightElbow = rightShoulder + new Vector2(10 + lift * 5, 16 - lift * 12);
        Vector2 leftHand = leftElbow + new Vector2(-3 - lift * 8, 15 - lift * 28);
        Vector2 rightHand = rightElbow + new Vector2(3 + lift * 12, 15 - lift * 30);
        if (e.AttackKind == SanctumEliteAttack.ForeseenSpear && casting)
            rightHand += new Vector2(12 * p, -7 * p);
        Limb(a, leftShoulder, leftElbow, 12, RobeLight, f);
        Limb(a, rightShoulder, rightElbow, 12, RobeLight, f);
        Limb(a, leftElbow, leftHand, 5, Skin, f);
        Limb(a, rightElbow, rightHand, 5, Skin, f);
        for (int i = -1; i <= 1; i++)
        {
            Line(a, leftHand, leftHand + new Vector2(-4 + i * 3, -10), 2, Skin, f);
            Line(a, rightHand, rightHand + new Vector2(4 + i * 3, -11), 2, Skin, f);
        }
        if (e.AttackKind == SanctumEliteAttack.VeilShift && casting)
        {
            Ring(e.ShiftOrigin, 20, 9, new Color(157, 125, 199, 125), false);
            Ring(e.ShiftDestination, 23, 11, Abyss, false);
            Line(Vector2.Zero, e.ShiftOrigin, e.ShiftDestination, 1,
                new Color(157, 125, 199, 90), false);
        }
    }

    private void DrawProphecies(DemonKingSanctumEliteEnemy e)
    {
        foreach (SanctumProphecyMarker marker in e.Markers)
        {
            float age = e.ProphecyAge;
            if (!marker.IsVisible(age)) continue;
            bool active = marker.IsActive(age);
            Color color = marker.IsFalse ? new Color(98, 84, 124, 100) :
                active ? new Color(202, 163, 224, 215) : new Color(173, 130, 197, 170);
            if (marker.IsFalse)
                color *= Math.Clamp((marker.ActivateAt - .40f - age) / .32f, 0f, 1f);
            Ring(marker.Position, 34, 10, color, marker.IsFalse);
            if (e.AttackKind == SanctumEliteAttack.FracturedFuture && !marker.IsFalse)
            {
                Ring(marker.Position, 27, 7, Gold, false);
                // Solid crown rune remains visible from placement through activation.
                for (int i = -1; i <= 1; i++)
                    Line(marker.Position, new Vector2(i * 8, -8), new Vector2(i * 8, -21), 3, Gold, false);
            }
            else if (e.AttackKind == SanctumEliteAttack.ThreeOmens)
                for (int i = 0; i < marker.Order; i++)
                    Line(marker.Position, new Vector2(-8 + i * 8, -12), new Vector2(-8 + i * 8, -22), 3, Gold, false);
            if (active)
            {
                for (int i = -1; i <= 1; i++)
                    Line(marker.Position, new Vector2(i * 17, 0), new Vector2(i * 11, -61),
                        6 - Math.Abs(i), color, false);
            }
            else if (!marker.IsFalse)
            {
                float progress = Math.Clamp((age - marker.RevealAt) /
                    (marker.ActivateAt - marker.RevealAt), 0, 1);
                Ring(marker.Position, 34 * (1 - progress), 10 * (1 - progress), color, false);
            }
        }
        if (e.AttackKind == SanctumEliteAttack.ForeseenSpear &&
            e.CombatState == SanctumEliteState.Windup)
        {
            Line(Vector2.Zero, e.SpearOrigin, e.SpearDestination, 1,
                new Color(176, 130, 208, 150), false);
            Ring(e.SpearDestination, 6, 6, Gold, false);
        }
        if (e.HasLance)
        {
            Vector2 delta = e.SpearDestination - e.SpearOrigin;
            Vector2 direction = delta.LengthSquared() > 1 ? Vector2.Normalize(delta) : Vector2.UnitX;
            Line(Vector2.Zero, e.SpearPosition - direction * 32, e.SpearPosition, 7, Abyss, false);
            Line(Vector2.Zero, e.SpearPosition - direction * 15, e.SpearPosition + direction * 9, 2, Steel, false);
        }
    }

    private void DrawDeath(DemonKingSanctumEliteEnemy e, Vector2 a, bool f)
    {
        float p = e.DeathPresentationProgress;
        if (e.Type == EnemyType.RoyalSanctumKnight)
        {
            Vector2 hip = new(12 * p, -45 + 28 * p), chest = new(25 * p, -80 + 55 * p);
            Limb(a, new Vector2(-15, 0), hip, 12, Plate, f);
            Limb(a, new Vector2(18, 0), hip, 12, Plate, f);
            Limb(a, hip, chest, 26, Plate, f);
            Limb(a, chest, new Vector2(-30, -4), 12, Crimson, f);
            Line(a, chest + new Vector2(0, -10), chest + new Vector2(5, -24), 13, PlateLight, f);
            Line(a, new Vector2(5, -15 + p * 12), new Vector2(106, -18 + p * 12), 6, Steel, f);
        }
        else
        {
            Color fade = new(112, 90, 135, (int)(190 * (1 - p)));
            Limb(a, new Vector2(-17, -4), new Vector2(8 * p, -73 + p * 35), 24, fade, f);
            Ring(a + new Vector2(10 * p, -100 + p * 70), 21, 30, fade, false);
            Line(a, new Vector2(0, -108 + p * 67), new Vector2(0, -91 + p * 67), 12, fade, f);
        }
    }
    private void Ring(Vector2 c, float rx, float ry, Color color, bool broken)
    {
        for (int i = 0; i < 24; i++)
        {
            if (broken && i % 3 == 0) continue;
            float a = i * MathHelper.TwoPi / 24, b = (i + 1) * MathHelper.TwoPi / 24;
            Line(c, new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry),
                new Vector2(MathF.Cos(b) * rx, MathF.Sin(b) * ry), 2, color, false);
        }
    }
    private void Limb(Vector2 a, Vector2 x, Vector2 y, float width, Color color, bool flip)
    { Line(a, x, y, width + 3, Outline, flip); Line(a, x, y, width, color, flip); }
    private void Line(Vector2 anchor, Vector2 x, Vector2 y, float width, Color color, bool flip)
    {
        if (flip) { x.X = -x.X; y.X = -y.X; }
        Vector2 delta = y - x;
        float length = delta.Length();
        if (length < .1f) return;
        _batch.Draw(_pixel, anchor + x, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(length, width), SpriteEffects.None, 0f);
    }
    private sealed class Clock { public float Elapsed; }
}
