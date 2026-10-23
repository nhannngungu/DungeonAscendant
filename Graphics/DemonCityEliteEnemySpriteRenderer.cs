using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Code-built, articulated silhouettes for Map 6 enemies 57 and 58.</summary>
public sealed class DemonCityEliteEnemySpriteRenderer
{
    public static readonly Vector2 WardenVisualSize = new(150f, 122f);
    public static readonly Vector2 HarrierVisualSize = new(196f, 150f);

    private static readonly Color Outline = new(15, 14, 20);
    private static readonly Color ArmorDark = new(37, 36, 46);
    private static readonly Color Armor = new(69, 68, 82);
    private static readonly Color ArmorEdge = new(132, 126, 143);
    private static readonly Color OldGold = new(139, 104, 64);
    private static readonly Color Skin = new(126, 72, 71);
    private static readonly Color Cloth = new(48, 37, 57);
    private static readonly Color Veil = new(92, 71, 132, 150);
    private static readonly Color VeilEdge = new(178, 153, 211, 220);
    private static readonly Color WingDark = new(42, 31, 45);
    private static readonly Color WingMembrane = new(91, 47, 70, 150);
    private static readonly Color Talon = new(153, 137, 132);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonCityEliteEnemy, AnimationClock>
        _clocks = new();

    public DemonCityEliteEnemySpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonCityEliteEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy, static _ => new AnimationClock());
        clock.Update(gameTime, enemy.CombatState);
        bool committed = enemy.CombatState != DemonCityEliteState.Ready;
        bool flip = committed
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        Vector2 shadowAnchor = anchor;

        if (!enemy.IsAlive && enemy.Type == EnemyType.GloomwingHarrier &&
            !float.IsNaN(enemy.DeathLandingCenterY))
        {
            shadowAnchor.Y = enemy.DeathLandingCenterY + enemy.Size.Y / 2f;
            float fall = MathHelper.SmoothStep(
                enemy.Position.Y,
                enemy.DeathLandingCenterY,
                Math.Clamp(enemy.DeathPresentationProgress / .72f, 0f, 1f));
            anchor.Y = fall + enemy.Size.Y / 2f;
        }

        float shadowWidth = enemy.Type == EnemyType.VeilWarden ? 48f : 58f;
        DrawBox(shadowAnchor, -shadowWidth / 2f, -3f, shadowWidth, 6f,
            new Color(6, 5, 9, 150), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.VeilWarden)
                DrawWardenDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawHarrierDeath(anchor, flip, enemy.DeathPresentationProgress);
            return;
        }

        if (enemy.Type == EnemyType.VeilWarden)
            DrawWarden(enemy, anchor, flip, clock.Elapsed);
        else
            DrawHarrier(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawWarden(
        DemonCityEliteEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        float p = enemy.ActionProgress;
        float breath = MathF.Sin(elapsed * 3.2f) * 1.2f;
        float step = MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(elapsed * 8f) * 4f : 0f;
        float lean = 1f;
        float crouch = 0f;
        float swordAngle = .62f;
        float shieldReach = 22f;

        switch (enemy.CombatState)
        {
            case DemonCityEliteState.WardenCutWindup:
                swordAngle = MathHelper.Lerp(.62f, -1.75f, p);
                lean = -5f;
                crouch = 4f * p;
                break;
            case DemonCityEliteState.WardenCutActive:
                swordAngle = MathHelper.Lerp(-1.75f, .62f, p);
                lean = 9f;
                step = 5f;
                break;
            case DemonCityEliteState.WardenThrustWindup:
                swordAngle = -.08f;
                shieldReach = MathHelper.Lerp(22f, 12f, p);
                lean = -6f * p;
                crouch = 5f * p;
                break;
            case DemonCityEliteState.WardenThrustActive:
                swordAngle = .03f;
                shieldReach = 12f;
                lean = 13f;
                step = 8f;
                break;
            case DemonCityEliteState.WardenGuardRaise:
            case DemonCityEliteState.WardenGuardActive:
            case DemonCityEliteState.WardenGuardLower:
                swordAngle = 1.02f;
                shieldReach = 30f;
                crouch = 4f;
                lean = -2f;
                break;
            case DemonCityEliteState.WardenReprisalPause:
                swordAngle = -1.22f;
                shieldReach = 28f;
                crouch = 7f;
                lean = -7f;
                break;
            case DemonCityEliteState.WardenReprisalActive:
                swordAngle = .02f;
                shieldReach = 13f;
                lean = 15f;
                step = 9f;
                break;
            case DemonCityEliteState.WardenCutRecovery:
            case DemonCityEliteState.WardenThrustRecovery:
            case DemonCityEliteState.WardenReprisalRecovery:
                swordAngle = MathHelper.Lerp(.15f, .62f, p);
                lean = MathHelper.Lerp(8f, 1f, p);
                crouch = MathHelper.Lerp(5f, 0f, p);
                break;
        }

        if (enemy.IsHitFlashing || enemy.IsStaggered)
        {
            lean = -8f;
            crouch = 7f;
            swordAngle = 1.4f;
        }

        Vector2 hip = new(lean, -43f + crouch);
        Vector2 chest = new(lean + 1f, -76f + crouch + breath);
        Vector2 rearFoot = new(-14f - step, 0f);
        Vector2 frontFoot = new(15f + step, 0f);
        Vector2 rearKnee = new(-11f + step * .45f, -24f + crouch);
        Vector2 frontKnee = new(11f - step * .4f, -25f + crouch);

        DrawArmoredLeg(anchor, hip + new Vector2(-8f, 0f), rearKnee,
            rearFoot, ArmorDark, flip);
        DrawArmoredLeg(anchor, hip + new Vector2(8f, 0f), frontKnee,
            frontFoot, Armor, flip);
        DrawBox(anchor, hip.X - 17f, hip.Y - 5f, 34f, 11f, Outline, flip);
        DrawBox(anchor, hip.X - 15f, hip.Y - 4f, 30f, 8f, Cloth, flip);

        DrawBox(anchor, chest.X - 20f, chest.Y - 9f, 40f, 38f, Outline, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 7f, 34f, 33f,
            enemy.IsHitFlashing ? ArmorEdge : Armor, flip);
        DrawLine(anchor, chest + new Vector2(-14f, 1f),
            chest + new Vector2(15f, 17f), 3f, OldGold, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y + 20f, 14f, 17f, Cloth, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y - 10f, 13f, 12f,
            ArmorDark, flip);
        DrawBox(anchor, chest.X + 7f, chest.Y - 10f, 13f, 12f,
            Armor, flip);

        Vector2 neck = chest + new Vector2(1f, -14f);
        Vector2 head = neck + new Vector2(1f, -11f);
        DrawLine(anchor, chest + new Vector2(0f, -7f), neck,
            10f, Skin, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 9f, 20f, 19f, Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 7f, 16f, 16f,
            ArmorDark, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 5f, 13f, 12f,
            Armor, flip);
        DrawLine(anchor, head + new Vector2(-7f, -6f),
            head + new Vector2(-12f, -13f), 3f, ArmorEdge, flip);
        DrawLine(anchor, head + new Vector2(-10f, -7f),
            head + new Vector2(-17f, -10f), 3f, OldGold, flip);
        DrawBox(anchor, head.X - 3f, head.Y - 2f, 10f, 2f,
            new Color(211, 89, 103), flip);

        Vector2 swordShoulder = chest + new Vector2(-18f, 1f);
        Vector2 swordElbow = swordShoulder + AngleVector(swordAngle) * 18f;
        Vector2 swordHand = swordElbow + AngleVector(swordAngle + .12f) * 17f;
        DrawLine(anchor, swordShoulder, swordElbow, 10f, ArmorDark, flip);
        DrawLine(anchor, swordElbow, swordHand, 8f, Skin, flip);
        DrawSword(anchor, swordHand, swordAngle - .02f, flip);

        Vector2 shieldShoulder = chest + new Vector2(18f, 1f);
        Vector2 shieldHand = shieldShoulder + new Vector2(shieldReach, 15f);
        DrawLine(anchor, shieldShoulder,
            Vector2.Lerp(shieldShoulder, shieldHand, .55f), 10f, Armor, flip);
        DrawLine(anchor, Vector2.Lerp(shieldShoulder, shieldHand, .55f),
            shieldHand, 8f, Skin, flip);
        DrawBox(anchor, shieldHand.X - 5f, shieldHand.Y - 7f, 12f, 18f,
            ArmorDark, flip);

        if (enemy.IsVeilShieldExpanded)
            DrawVeilShield(anchor, shieldHand, enemy, flip);
        else
            DrawDiamond(anchor, shieldHand + new Vector2(3f, 1f),
                12f, 17f, 2f, VeilEdge, flip);
    }

    private void DrawVeilShield(
        Vector2 anchor,
        Vector2 hand,
        DemonCityEliteEnemy enemy,
        bool flip)
    {
        float pulse = enemy.IsGuardBlockReacting ? 7f : 0f;
        float width = 26f + pulse;
        float height = 46f + pulse;
        Vector2 center = hand + new Vector2(9f, 0f);
        for (float y = -height + 4f; y < height; y += 5f)
        {
            float span = width * (1f - MathF.Abs(y) / height);
            DrawLine(anchor,
                center + new Vector2(-span, y),
                center + new Vector2(span, y),
                3f,
                Veil,
                flip);
        }
        DrawDiamond(anchor, center, width, height, 2f, VeilEdge, flip);
        DrawLine(anchor, center + new Vector2(0f, -height),
            center + new Vector2(width + 7f, -7f), 2f, VeilEdge, flip);
        DrawLine(anchor, center + new Vector2(width + 7f, -7f),
            center + new Vector2(0f, height), 2f, VeilEdge, flip);
    }

    private void DrawHarrier(
        DemonCityEliteEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        float p = enemy.ActionProgress;
        bool airborne = enemy.IsAirborne;
        float wingSpread = airborne ? 1f : .46f;
        float flap = MathF.Sin(elapsed * (airborne ? 10f : 3f)) *
            (airborne ? 10f : 3f);
        float lean = 0f;
        float crouch = airborne ? 9f : 5f;
        float talonReach = 0f;

        switch (enemy.CombatState)
        {
            case DemonCityEliteState.HarrierTakeoff:
                wingSpread = MathHelper.Lerp(.48f, 1f, p);
                crouch = MathHelper.Lerp(9f, 16f, p);
                flap = -12f + p * 22f;
                break;
            case DemonCityEliteState.HarrierDiveSetup:
                wingSpread = MathHelper.Lerp(1f, .72f, p);
                lean = 8f * p;
                talonReach = 8f * p;
                break;
            case DemonCityEliteState.HarrierDiveCommit:
                wingSpread = .42f;
                lean = 16f;
                crouch = 14f;
                talonReach = 15f;
                break;
            case DemonCityEliteState.HarrierDiveBrake:
                wingSpread = MathHelper.Lerp(.65f, 1.12f, p);
                lean = MathHelper.Lerp(15f, -4f, p);
                flap = -15f;
                break;
            case DemonCityEliteState.HarrierLand:
                wingSpread = MathHelper.Lerp(1f, .48f, p);
                crouch = MathHelper.Lerp(15f, 0f, p);
                break;
            case DemonCityEliteState.HarrierFeintSetup:
                wingSpread = 1.12f;
                lean = -7f;
                break;
            case DemonCityEliteState.HarrierFeintRelocate:
                wingSpread = .86f;
                lean = -10f;
                flap = 12f;
                break;
            case DemonCityEliteState.HarrierRendWindup:
            case DemonCityEliteState.HarrierRendSecondWindup:
                crouch = 8f;
                lean = -7f;
                talonReach = -4f;
                break;
            case DemonCityEliteState.HarrierRendFirst:
            case DemonCityEliteState.HarrierRendSecond:
                crouch = 5f;
                lean = 13f;
                talonReach = 14f;
                break;
            case DemonCityEliteState.HarrierShearWindup:
                wingSpread = MathHelper.Lerp(.45f, 1.15f, p);
                crouch = 7f;
                break;
            case DemonCityEliteState.HarrierShearActive:
                wingSpread = 1.24f;
                flap = 16f;
                crouch = 5f;
                break;
        }

        if (enemy.IsHitFlashing || enemy.IsStaggered)
        {
            lean = -9f;
            crouch = 10f;
            wingSpread = .60f;
            flap = 14f;
        }

        Vector2 hip = new(lean, -37f + crouch);
        Vector2 chest = new(lean + 1f, -65f + crouch);
        DrawWing(anchor, chest, rear: true, wingSpread, flap - 7f, flip);
        DrawWing(anchor, chest, rear: false, wingSpread, flap, flip);

        Vector2 rearKnee = hip + new Vector2(-10f, 20f);
        Vector2 frontKnee = hip + new Vector2(10f, 19f);
        Vector2 rearTalon = rearKnee + new Vector2(-3f + talonReach, 22f);
        Vector2 frontTalon = frontKnee + new Vector2(7f + talonReach, 22f);
        DrawLine(anchor, hip + new Vector2(-7f, 0f), rearKnee,
            10f, WingDark, flip);
        DrawLine(anchor, rearKnee, rearTalon, 7f, Skin, flip);
        DrawLine(anchor, hip + new Vector2(7f, 0f), frontKnee,
            10f, Cloth, flip);
        DrawLine(anchor, frontKnee, frontTalon, 7f, Skin, flip);
        DrawTalon(anchor, rearTalon, flip);
        DrawTalon(anchor, frontTalon, flip);

        DrawBox(anchor, chest.X - 17f, chest.Y - 8f, 34f, 33f,
            Outline, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 6f, 28f, 29f,
            enemy.IsHitFlashing ? new Color(177, 91, 99) : Cloth, flip);
        DrawLine(anchor, chest + new Vector2(-12f, 2f),
            chest + new Vector2(12f, 17f), 3f, ArmorEdge, flip);

        Vector2 head = chest + new Vector2(4f, -24f);
        DrawLine(anchor, chest + new Vector2(1f, -6f), head,
            9f, Skin, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 8f, 17f, 17f, Outline, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 6f, 13f, 13f, Skin, flip);
        DrawLine(anchor, head + new Vector2(-5f, -6f),
            head + new Vector2(-12f, -17f), 4f, WingDark, flip);
        DrawLine(anchor, head + new Vector2(5f, -6f),
            head + new Vector2(9f, -18f), 4f, WingDark, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 1f, 4f, 2f,
            new Color(226, 85, 116), flip);

        Vector2 rearShoulder = chest + new Vector2(-15f, 0f);
        Vector2 frontShoulder = chest + new Vector2(15f, 1f);
        Vector2 rearHand = rearShoulder + new Vector2(-16f, 22f);
        Vector2 frontHand = frontShoulder + new Vector2(18f + talonReach, 20f);
        DrawLine(anchor, rearShoulder, rearHand, 8f, Skin, flip);
        DrawLine(anchor, frontShoulder, frontHand, 8f, Skin, flip);
        DrawTalon(anchor, rearHand, flip);
        DrawTalon(anchor, frontHand, flip);
    }

    private void DrawWing(
        Vector2 anchor,
        Vector2 root,
        bool rear,
        float spread,
        float flap,
        bool flip)
    {
        float sign = rear ? -1f : 1f;
        Vector2 shoulder = root + new Vector2(sign * 5f, -4f);
        Vector2 elbow = shoulder + new Vector2(
            sign * (42f * spread), -25f - flap * sign * .25f);
        Vector2 tip = elbow + new Vector2(
            sign * (43f * spread), 25f + flap);
        Vector2 lower = root + new Vector2(sign * (50f * spread), 27f);
        Color membrane = rear
            ? new Color(67, 40, 59, 120)
            : WingMembrane;

        DrawLine(anchor, shoulder, elbow, rear ? 8f : 10f,
            Outline, flip);
        DrawLine(anchor, elbow, tip, rear ? 7f : 9f,
            WingDark, flip);
        DrawLine(anchor, tip, lower, 3f, membrane, flip);
        DrawLine(anchor, lower, root + new Vector2(sign * 9f, 12f),
            4f, membrane, flip);
        for (int rib = 1; rib <= 4; rib++)
        {
            float t = rib / 5f;
            Vector2 bone = Vector2.Lerp(elbow, tip, t);
            Vector2 membranePoint = Vector2.Lerp(
                root + new Vector2(sign * 8f, 10f), lower, t);
            DrawLine(anchor, bone, membranePoint, rear ? 6f : 8f,
                membrane, flip);
        }
        DrawLine(anchor, shoulder, elbow, 3f, ArmorEdge, flip);
    }

    private void DrawWardenDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        Vector2 body = new(8f * settle, -28f + 24f * settle);
        DrawLine(anchor, body + new Vector2(-18f, 2f),
            body + new Vector2(24f, 5f), 22f, ArmorDark, flip);
        DrawLine(anchor, body + new Vector2(-5f, -7f),
            body + new Vector2(42f, -4f), 5f, ArmorEdge, flip);
        DrawDiamond(anchor, body + new Vector2(19f, 5f),
            14f, 20f, 2f, new Color(VeilEdge, 1f - progress), flip);
        DrawLine(anchor, body + new Vector2(-22f, 6f),
            body + new Vector2(-48f, 13f), 5f, ArmorEdge, flip);
    }

    private void DrawHarrierDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = Math.Clamp((progress - .58f) / .42f, 0f, 1f);
        Vector2 body = new(0f, -31f + 26f * settle);
        DrawLine(anchor, body + new Vector2(-17f, 1f),
            body + new Vector2(18f, 5f), 20f, Cloth, flip);
        DrawLine(anchor, body + new Vector2(-8f, -4f),
            body + new Vector2(-56f, 17f), 7f, WingDark, flip);
        DrawLine(anchor, body + new Vector2(8f, -2f),
            body + new Vector2(53f, 20f), 7f, WingDark, flip);
        DrawLine(anchor, body + new Vector2(-3f, 5f),
            body + new Vector2(-28f, 28f), 5f, Skin, flip);
        DrawLine(anchor, body + new Vector2(5f, 6f),
            body + new Vector2(31f, 27f), 5f, Skin, flip);
    }

    private void DrawArmoredLeg(
        Vector2 anchor,
        Vector2 hip,
        Vector2 knee,
        Vector2 foot,
        Color color,
        bool flip)
    {
        DrawLine(anchor, hip, knee, 11f, color, flip);
        DrawLine(anchor, knee, foot, 9f, ArmorDark, flip);
        DrawBox(anchor, knee.X - 6f, knee.Y - 5f, 12f, 10f,
            ArmorEdge, flip);
        DrawLine(anchor, foot + new Vector2(-7f, 0f),
            foot + new Vector2(10f, 0f), 7f, ArmorDark, flip);
    }

    private void DrawSword(Vector2 anchor, Vector2 hand, float angle, bool flip)
    {
        Vector2 direction = AngleVector(angle);
        Vector2 guardA = hand + AngleVector(angle - MathHelper.PiOver2) * 8f;
        Vector2 guardB = hand + AngleVector(angle + MathHelper.PiOver2) * 8f;
        Vector2 bladeStart = hand + direction * 5f;
        Vector2 bladeEnd = bladeStart + direction * 55f;
        DrawLine(anchor, guardA, guardB, 5f, ArmorEdge, flip);
        DrawLine(anchor, bladeStart, bladeEnd, 7f, Outline, flip);
        DrawLine(anchor, bladeStart, bladeEnd, 4f,
            new Color(171, 169, 181), flip);
        DrawLine(anchor, bladeEnd, bladeEnd + direction * 8f,
            2f, new Color(212, 208, 218), flip);
    }

    private void DrawTalon(Vector2 anchor, Vector2 joint, bool flip)
    {
        DrawLine(anchor, joint, joint + new Vector2(12f, 4f), 3f, Talon, flip);
        DrawLine(anchor, joint + new Vector2(2f, 1f),
            joint + new Vector2(10f, 9f), 3f, Talon, flip);
        DrawLine(anchor, joint + new Vector2(-1f, 1f),
            joint + new Vector2(5f, 11f), 2f, Talon, flip);
    }

    private static Vector2 AngleVector(float angle) =>
        new(MathF.Cos(angle), MathF.Sin(angle));

    private void DrawDiamond(
        Vector2 anchor,
        Vector2 center,
        float halfWidth,
        float halfHeight,
        float thickness,
        Color color,
        bool flip)
    {
        Vector2 top = center + new Vector2(0f, -halfHeight);
        Vector2 right = center + new Vector2(halfWidth, 0f);
        Vector2 bottom = center + new Vector2(0f, halfHeight);
        Vector2 left = center + new Vector2(-halfWidth, 0f);
        DrawLine(anchor, top, right, thickness, color, flip);
        DrawLine(anchor, right, bottom, thickness, color, flip);
        DrawLine(anchor, bottom, left, thickness, color, flip);
        DrawLine(anchor, left, top, thickness, color, flip);
    }

    private void DrawBox(
        Vector2 anchor,
        float x,
        float y,
        float width,
        float height,
        Color color,
        bool flip)
    {
        float drawX = flip ? -x - width : x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(anchor.X + drawX),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLine(
        Vector2 anchor,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        bool flip)
    {
        if (flip)
        {
            start.X = -start.X;
            end.X = -end.X;
        }
        start += anchor;
        end += anchor;
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, .5f),
            new Vector2(length, MathF.Max(1f, thickness)),
            SpriteEffects.None,
            0f);
    }

    private sealed class AnimationClock
    {
        private DemonCityEliteState _state;
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, DemonCityEliteState state)
        {
            float elapsed = MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
            if (_state != state)
            {
                _state = state;
                Elapsed = 0f;
            }
            else
            {
                Elapsed += elapsed;
            }
        }
    }
}
