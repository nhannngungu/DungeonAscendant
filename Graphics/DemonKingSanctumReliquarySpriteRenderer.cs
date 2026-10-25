using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Articulated renderer for the Grave Reliquary and Cloister Wraith.</summary>
public sealed class DemonKingSanctumReliquarySpriteRenderer
{
    public static readonly Vector2 ReliquaryVisualSize = new(164f, 128f);
    public static readonly Vector2 WraithVisualSize = new(142f, 122f);
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonKingSanctumReliquaryEnemy, Clock>
        _clocks = new();
    private static readonly Color Outline = new(16, 14, 18);
    private static readonly Color Bone = new(155, 145, 124);
    private static readonly Color Tissue = new(69, 44, 48);
    private static readonly Color Iron = new(65, 64, 68);
    private static readonly Color Bronze = new(123, 88, 53);
    private static readonly Color Coffin = new(62, 55, 55);
    private static readonly Color Spectral = new(93, 87, 119);
    private static readonly Color Glow = new(167, 145, 194);
    private static readonly Color Cloth = new(47, 39, 57);

    public DemonKingSanctumReliquarySpriteRenderer(
        SpriteBatch batch, Texture2D pixel)
    { _batch = batch; _pixel = pixel; }

    public void Draw(DemonKingSanctumReliquaryEnemy enemy, GameTime gameTime)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Update(gameTime);
        bool committed = enemy.CombatState != ReliquaryCombatState.Ready;
        bool flip = committed ? enemy.AttackFacesLeft :
            enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        Box(anchor, -29, -3, 58, 5, new Color(7, 6, 10, 145), flip);
        if (enemy.HasCloisterGrasp) DrawGrasp(enemy);
        if (enemy.HasEcho) DrawEcho(enemy);
        if (!enemy.IsAlive) { DrawDeath(enemy, anchor, flip); return; }
        if (enemy.Type == EnemyType.GraveReliquary)
            DrawReliquary(enemy, anchor, flip, clock.Elapsed);
        else DrawWraith(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawReliquary(
        DemonKingSanctumReliquaryEnemy e, Vector2 a, bool flip, float elapsed)
    {
        float p = e.ActionProgress;
        float step = MathF.Abs(e.VisualVelocityX) > 2f
            ? MathF.Sin(elapsed * 4.5f) * 4f : 0f;
        float lean = 7f, crouch = 3f, bladeAngle = .78f, lidOpen = 0f;
        switch (e.CombatState)
        {
            case ReliquaryCombatState.SepulchralCleaveWindup:
                lean = MathHelper.Lerp(7, -7, p);
                bladeAngle = MathHelper.Lerp(.78f, -2.0f, p); break;
            case ReliquaryCombatState.SepulchralCleaveActive:
                lean = 15; bladeAngle = MathHelper.Lerp(-2f, .55f, p); break;
            case ReliquaryCombatState.ReliquaryBurstWindup:
                crouch = 8; lean = 1; lidOpen = p; bladeAngle = 1.5f; break;
            case ReliquaryCombatState.ReliquaryBurstActive:
                crouch = 8; lidOpen = 1; bladeAngle = 1.5f; break;
            case ReliquaryCombatState.BoneDragWindup:
                crouch = 10; lean = -5; bladeAngle = .25f; break;
            case ReliquaryCombatState.BoneDragActive:
                crouch = 11; lean = 19; bladeAngle = .08f; step = 8; break;
            case ReliquaryCombatState.OssuaryGuardRaise:
                crouch = MathHelper.Lerp(3, 12, p); lean = 0;
                bladeAngle = MathHelper.Lerp(.78f, 1.55f, p); break;
            case ReliquaryCombatState.OssuaryGuardActive:
                crouch = 12; lean = 0; bladeAngle = 1.55f; break;
        }
        if (e.IsHitFlashing || e.IsStaggered)
        { lean = -9; crouch = 12; bladeAngle = 1.8f; }
        Color tissue = e.IsHitFlashing ? new Color(195, 139, 120) : Tissue;
        Vector2 hip = new(lean, -43 + crouch), chest = new(lean + 5, -70 + crouch);
        Vector2 rf = new(-13 - step, 0), ff = new(14 + step, 0);
        Vector2 rk = new(-10 + step * .4f, -22 + crouch);
        Vector2 fk = new(11 - step * .4f, -23 + crouch);

        // Framed reliquary: casing, lid, bronze bindings, chain and inscription.
        Vector2 caseTop = chest + new Vector2(-21, -23);
        Vector2 caseBottom = hip + new Vector2(-23, 23);
        Line(a, caseTop, caseBottom, 35, Outline, flip);
        Line(a, caseTop + new Vector2(2, 2), caseBottom + new Vector2(2, -2), 29, Coffin, flip);
        Line(a, caseTop + new Vector2(-9, 5), caseBottom + new Vector2(-9, -5), 3, Bronze, flip);
        Line(a, caseTop + new Vector2(10, 5), caseBottom + new Vector2(10, -5), 3, Bronze, flip);
        Line(a, caseTop + new Vector2(-12, 13), caseTop + new Vector2(12, 13), 3, Bronze, flip);
        Line(a, caseBottom + new Vector2(-12, -14), caseBottom + new Vector2(12, -14), 3, Bronze, flip);
        Vector2 lidA = caseTop + new Vector2(12, 2);
        Vector2 lidB = caseBottom + new Vector2(12 + lidOpen * 13f, -2);
        Line(a, lidA, lidB, 7, Iron, flip);
        Line(a, caseTop + new Vector2(-5, 25), caseTop + new Vector2(7, 35), 2,
            lidOpen > .2f ? Glow : new Color(91, 69, 88), flip);

        Limb(a, rf, rk, Bone, 8, flip); Limb(a, rk, hip + new Vector2(-8, 0), tissue, 10, flip);
        Limb(a, ff, fk, Bone, 8, flip); Limb(a, fk, hip + new Vector2(8, 0), tissue, 10, flip);
        Line(a, rf + new Vector2(-7, 0), rf + new Vector2(8, 0), 6, Iron, flip);
        Line(a, ff + new Vector2(-7, 0), ff + new Vector2(10, 0), 6, Iron, flip);
        Box(a, hip.X - 15, hip.Y - 5, 30, 9, Outline, flip);
        Box(a, chest.X - 17, chest.Y - 8, 34, 30, Outline, flip);
        Box(a, chest.X - 15, chest.Y - 6, 30, 26, tissue, flip);
        Line(a, chest + new Vector2(-11, -1), chest + new Vector2(11, 15), 4, Bone, flip);
        Vector2 neck = chest + new Vector2(4, -11), head = neck + new Vector2(4, -10);
        Line(a, chest + new Vector2(2, -5), neck, 8, Bone, flip);
        Box(a, head.X - 8, head.Y - 9, 16, 18, Outline, flip);
        Box(a, head.X - 6, head.Y - 7, 12, 15, Bone, flip);
        Box(a, head.X + 1, head.Y - 2, 4, 3, new Color(112, 75, 116), flip);

        Vector2 dir = new(MathF.Cos(bladeAngle), MathF.Sin(bladeAngle));
        Vector2 shoulder = chest + new Vector2(15, 1);
        Vector2 elbow = shoulder + dir * 16f, hand = elbow + dir * 17f;
        Limb(a, shoulder, elbow, tissue, 10, flip); Limb(a, elbow, hand, Bone, 8, flip);
        Line(a, hand, hand + dir * 35f, 12, Outline, flip);
        Line(a, hand + dir * 2f, hand + dir * 34f, 8, Iron, flip);
        Line(a, hand + dir * 22f, hand + dir * 38f + new Vector2(-dir.Y, dir.X) * 9f,
            5, Bone, flip);
        Vector2 offShoulder = chest + new Vector2(-14, 2);
        Vector2 offElbow = offShoulder + new Vector2(-7, 13);
        Vector2 offHand = offElbow + new Vector2(3, 13);
        Limb(a, offShoulder, offElbow, tissue, 9, flip); Limb(a, offElbow, offHand, Bone, 7, flip);
        Line(a, offHand, caseBottom + new Vector2(-8, -5), 3, Bronze, flip);
        if (e.IsReliquaryBurstActive)
            Ring(a, DemonKingSanctumReliquaryEnemy.ReliquaryBurstRadius,
                new Color(135, 91, 151, 150));
    }

    private void DrawWraith(
        DemonKingSanctumReliquaryEnemy e, Vector2 a, bool flip, float elapsed)
    {
        float p = e.ActionProgress;
        float hover = MathF.Sin(elapsed * 2.2f) * 3f;
        float lean = 0f, armReach = 0f;
        if (e.CombatState == ReliquaryCombatState.WraithClawWindup)
        { lean = -6f * p; armReach = -5f * p; }
        if (e.CombatState == ReliquaryCombatState.WraithClawActive)
        { lean = 11f; armReach = 20f * p; }
        if (e.CombatState == ReliquaryCombatState.VeilPassTelegraph)
            lean = -3f;
        if (e.IsHitFlashing || e.IsStaggered) lean = -8f;
        Vector2 hip = new(lean, -33 + hover), chest = new(lean, -61 + hover);
        // Restrained lower mist: layered taper, never a particle cloud.
        Line(a, hip + new Vector2(-8, 0), new Vector2(-15, -2 + hover), 16,
            new Color(45, 42, 62, 185), flip);
        Line(a, hip + new Vector2(7, 0), new Vector2(12, -8 + hover), 13,
            new Color(67, 60, 83, 165), flip);
        Line(a, chest + new Vector2(-11, 11), hip + new Vector2(-13, 14), 17, Cloth, flip);
        Line(a, chest + new Vector2(9, 11), hip + new Vector2(12, 13), 15, Spectral, flip);
        Box(a, chest.X - 13, chest.Y - 7, 26, 29, Outline, flip);
        Box(a, chest.X - 11, chest.Y - 5, 22, 26, Spectral, flip);
        Line(a, chest + new Vector2(-8, 0), chest + new Vector2(8, 16), 3, Cloth, flip);
        Vector2 head = chest + new Vector2(0, -22);
        Box(a, head.X - 7, head.Y - 10, 14, 20, Outline, flip);
        Box(a, head.X - 5, head.Y - 8, 10, 17, new Color(75, 70, 91), flip);
        Box(a, head.X - 2, head.Y - 3, 5, 3, Glow, flip);
        Vector2 rearShoulder = chest + new Vector2(-11, 0);
        Vector2 rearElbow = rearShoulder + new Vector2(-8, 12);
        Vector2 rearHand = rearElbow + new Vector2(-4, 15);
        Limb(a, rearShoulder, rearElbow, Spectral, 6, flip);
        Limb(a, rearElbow, rearHand, Spectral, 5, flip);
        Vector2 clawShoulder = chest + new Vector2(11, 0);
        Vector2 clawElbow = clawShoulder + new Vector2(8 + armReach * .4f, 9);
        Vector2 clawHand = clawElbow + new Vector2(7 + armReach * .6f, 9);
        Limb(a, clawShoulder, clawElbow, Spectral, 6, flip);
        Limb(a, clawElbow, clawHand, Spectral, 5, flip);
        for (int i = 0; i < 3; i++)
            Line(a, clawHand, clawHand + new Vector2(10 + i * 2, -5 + i * 5),
                2, Glow, flip);
        if (e.CombatState == ReliquaryCombatState.VeilPassTelegraph)
        {
            Vector2 origin = e.VeilPassOrigin, destination = e.VeilPassDestination;
            Line(Vector2.Zero, origin, destination, 3,
                new Color(128, 108, 158, 105), false);
            Box(Vector2.Zero, destination.X - 5, destination.Y - 22, 10, 44,
                new Color(157, 136, 184, 95), false);
        }
    }

    private void DrawGrasp(DemonKingSanctumReliquaryEnemy e)
    {
        Vector2 c = e.CloisterGraspPosition;
        Color color = e.IsCloisterGraspActive ? new Color(166, 144, 190, 210) :
            new Color(111, 94, 136, 85 + (int)(75 * e.GraspTelegraphRatio));
        Line(c, new Vector2(-38, 0), new Vector2(38, 0), 3, color, false);
        for (int i = -2; i <= 2; i++)
        {
            float height = e.IsCloisterGraspActive ? 54f - MathF.Abs(i) * 6f : 9f;
            Line(c, new Vector2(i * 15, 0), new Vector2(i * 12, -height),
                e.IsCloisterGraspActive ? 5 : 2, color, false);
        }
    }

    private void DrawEcho(DemonKingSanctumReliquaryEnemy e)
    {
        Vector2 c = e.EchoPosition;
        bool flip = e.EchoFacesLeft;
        float reach = e.IsEchoActive ? 35f : 5f + e.EchoDelayRatio * 8f;
        Color color = e.IsEchoActive ? new Color(158, 137, 184, 185) :
            new Color(104, 91, 129, 105);
        Line(c, new Vector2(0, -28), new Vector2(0, 18), 16, color, flip);
        Line(c, new Vector2(4, -16), new Vector2(18 + reach, -2), 5, color, flip);
        Box(c, -7, -42, 14, 17, color, flip);
    }

    private void DrawDeath(DemonKingSanctumReliquaryEnemy e, Vector2 a, bool flip)
    {
        float p = e.DeathPresentationProgress;
        if (e.Type == EnemyType.GraveReliquary)
        {
            Vector2 body = new(17 * p, -44 + p * 31);
            Line(a, body + new Vector2(0, -24), body + new Vector2(0, 23), 28, Tissue, flip);
            Line(a, body + new Vector2(-18, -24), body + new Vector2(-24, 25), 27, Coffin, flip);
            Line(a, body + new Vector2(-11, -16), body + new Vector2(-20 + p * 9, 18), 3,
                new Color(105, 92, 76, 190), flip);
        }
        else
        {
            Color fade = new(92, 83, 117, (int)(205 * (1f - p * .75f)));
            Line(a, new Vector2(0, -66 + p * 18), new Vector2(0, -8), 21, fade, flip);
            Box(a, -7, -88 + p * 27, 14, 18,
                new Color(86, 77, 101, (int)fade.A), flip);
        }
    }

    private void Ring(Vector2 c, float radius, Color color)
    {
        const int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float a = i * MathHelper.TwoPi / segments;
            float b = (i + 1) * MathHelper.TwoPi / segments;
            Line(c, new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius,
                new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, 3, color, false);
        }
    }
    private void Limb(Vector2 a, Vector2 x, Vector2 y, Color c, float w, bool f)
    { Line(a, x, y, w + 3, Outline, f); Line(a, x, y, w, c, f); }
    private void Line(Vector2 a, Vector2 x, Vector2 y, float w, Color c, bool f)
    {
        if (f) { x.X = -x.X; y.X = -y.X; }
        Vector2 start = a + x, delta = a + y - start;
        float length = delta.Length(); if (length < .1f) return;
        _batch.Draw(_pixel, start, null, c, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(length, w), SpriteEffects.None, 0f);
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
