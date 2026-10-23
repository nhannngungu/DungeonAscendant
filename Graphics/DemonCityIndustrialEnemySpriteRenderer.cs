using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Articulated silhouettes for the Industrial/Ritual District pair. The
/// Servitor preserves anatomy beneath its burden; the Cantor remains upright,
/// layered, and technical rather than reading as another armored soldier.
/// </summary>
public sealed class DemonCityIndustrialEnemySpriteRenderer
{
    public static readonly Vector2 ServitorVisualSize = new(164f, 124f);
    public static readonly Vector2 CantorVisualSize = new(132f, 118f);

    private static readonly Color Outline = new(19, 17, 20);
    private static readonly Color Skin = new(114, 66, 60);
    private static readonly Color SkinLight = new(158, 91, 75);
    private static readonly Color IronDark = new(46, 44, 48);
    private static readonly Color Iron = new(82, 78, 80);
    private static readonly Color IronLight = new(134, 124, 116);
    private static readonly Color Leather = new(61, 45, 43);
    private static readonly Color BloodTube = new(112, 35, 41);
    private static readonly Color RobeDark = new(38, 31, 45);
    private static readonly Color Robe = new(60, 45, 69);
    private static readonly Color Violet = new(117, 72, 139);
    private static readonly Color BronzeDark = new(73, 52, 43);
    private static readonly Color Bronze = new(134, 96, 61);
    private static readonly Color Sigil = new(187, 147, 202);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<
        DemonCityIndustrialEnemy,
        AnimationClock> _clocks = new();

    public DemonCityIndustrialEnemySpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonCityIndustrialEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy,
            static _ => new AnimationClock());
        clock.Update(gameTime, enemy.CombatState);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool committed = enemy.CombatState != DemonCityIndustrialState.Ready;
        bool flip = committed
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        float shadowWidth = enemy.Type == EnemyType.BloodworksServitor
            ? 66f
            : 42f;
        DrawBox(anchor, -shadowWidth * .5f, -3f, shadowWidth, 6f,
            new Color(8, 7, 10, 150), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.BloodworksServitor)
                DrawServitorDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawCantorDeath(anchor, flip, enemy.DeathPresentationProgress);
            return;
        }

        if (enemy.Type == EnemyType.BloodworksServitor)
            DrawServitor(enemy, anchor, flip, clock.Elapsed);
        else
            DrawCantor(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawServitor(
        DemonCityIndustrialEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        float p = enemy.CombatState == DemonCityIndustrialState.Ready
            ? 0f : enemy.ActionProgress;
        float breath = MathF.Sin(elapsed * MathHelper.TwoPi * .55f);
        float step = MathF.Abs(enemy.VisualVelocityX) > 1f
            ? MathF.Sin(elapsed * MathHelper.TwoPi * .78f) * 5f
            : 0f;
        float crouch = 2f + MathF.Max(0f, breath) * 1.2f;
        float lean = 5f;
        float pressAngle = 1.35f;
        float clampAngle = 1.85f;
        float clampOpen = 3f;

        switch (enemy.CombatState)
        {
            case DemonCityIndustrialState.PressWindup:
                crouch = MathHelper.Lerp(2f, 9f, p);
                lean = MathHelper.Lerp(5f, -6f, p);
                pressAngle = MathHelper.Lerp(1.35f, -1.55f, p);
                break;
            case DemonCityIndustrialState.PressImpact:
                crouch = 9f;
                lean = MathHelper.Lerp(-5f, 13f, p);
                pressAngle = MathHelper.Lerp(-1.55f, 1.35f, p);
                step = 4f;
                break;
            case DemonCityIndustrialState.ClampWindup:
                crouch = 5f;
                lean = -2f;
                clampAngle = MathHelper.Lerp(1.85f, -2.65f, p);
                clampOpen = MathHelper.Lerp(3f, 10f, p);
                break;
            case DemonCityIndustrialState.ClampSweep:
                lean = 11f;
                clampAngle = MathHelper.Lerp(-2.65f, .35f, p);
                clampOpen = MathHelper.Lerp(10f, 5f, p);
                step = 5f;
                break;
            case DemonCityIndustrialState.ShoveWindup:
                crouch = 7f;
                lean = -6f;
                pressAngle = .18f;
                clampAngle = 2.65f;
                break;
            case DemonCityIndustrialState.ShoveActive:
                crouch = 5f;
                lean = 14f;
                pressAngle = .05f;
                clampAngle = .25f;
                step = 7f;
                break;
            case DemonCityIndustrialState.VentWindup:
                crouch = MathHelper.Lerp(2f, 10f, p);
                lean = -2f;
                pressAngle = 1.6f;
                clampAngle = 1.55f;
                break;
            case DemonCityIndustrialState.VentActive:
                crouch = 8f;
                lean = 3f;
                pressAngle = 1.85f;
                clampAngle = 1.30f;
                break;
            case DemonCityIndustrialState.ServitorReposition:
                lean = -2f;
                step = -4f;
                break;
            case DemonCityIndustrialState.PressRecovery:
            case DemonCityIndustrialState.ClampRecovery:
            case DemonCityIndustrialState.ShoveRecovery:
            case DemonCityIndustrialState.VentRecovery:
                crouch = MathHelper.Lerp(10f, 2f, p);
                lean = MathHelper.Lerp(11f, 5f, p);
                pressAngle = MathHelper.Lerp(1.8f, 1.35f, p);
                clampAngle = MathHelper.Lerp(.7f, 1.85f, p);
                break;
        }
        if (enemy.IsHitFlashing || enemy.IsStaggered)
        {
            lean = -9f;
            crouch = enemy.IsStaggered ? 12f : 7f;
            pressAngle = 1.9f;
            clampAngle = 2.2f;
        }

        Color skin = enemy.IsHitFlashing
            ? new Color(216, 143, 117)
            : Skin;
        Vector2 hip = new(lean - 3f, -42f + crouch);
        Vector2 chest = new(lean + 1f, -75f + crouch + breath * .7f);
        Vector2 rearFoot = new(-16f - step, 0f);
        Vector2 frontFoot = new(17f + step, 0f);
        Vector2 rearKnee = new(-13f + step * .4f, -23f + crouch);
        Vector2 frontKnee = new(13f - step * .4f, -24f + crouch);

        // Back restraint frame and pulsing tubes sit behind visible anatomy.
        DrawLine(anchor, chest + new Vector2(-15f, -8f),
            hip + new Vector2(-20f, 13f), 8f, Outline, flip);
        DrawLine(anchor, chest + new Vector2(-14f, -7f),
            hip + new Vector2(-18f, 12f), 5f, IronDark, flip);
        DrawLine(anchor, chest + new Vector2(-17f, -3f),
            chest + new Vector2(-24f, 25f), 3f, BloodTube, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 12f, 10f, 12f,
            Iron, flip);

        DrawLimb(anchor, rearFoot, rearKnee, IronDark, 10f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-9f, 0f),
            Leather, 12f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, Iron, 10f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(9f, 0f),
            new Color(91, 60, 55), 12f, flip);
        DrawLine(anchor, rearFoot + new Vector2(-9f, 0f),
            rearFoot + new Vector2(9f, 0f), 8f, IronDark, flip);
        DrawLine(anchor, frontFoot + new Vector2(-7f, 0f),
            frontFoot + new Vector2(12f, 0f), 8f, Iron, flip);
        DrawBox(anchor, rearKnee.X - 7f, rearKnee.Y - 5f, 14f, 10f,
            IronDark, flip);
        DrawBox(anchor, frontKnee.X - 8f, frontKnee.Y - 5f, 16f, 10f,
            Iron, flip);

        DrawBox(anchor, hip.X - 20f, hip.Y - 5f, 40f, 10f,
            Outline, flip);
        DrawBox(anchor, hip.X - 18f, hip.Y - 3f, 36f, 7f,
            Leather, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 9f, 44f, 36f,
            Outline, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y - 7f, 38f, 32f,
            new Color(93, 52, 49), flip);
        for (int rib = 0; rib < 3; rib++)
        {
            DrawLine(anchor,
                chest + new Vector2(-18f, 2f + rib * 8f),
                chest + new Vector2(18f, 5f + rib * 7f),
                3f, rib == 1 ? BronzeDark : IronDark, flip);
        }
        DrawLine(anchor, chest + new Vector2(-7f, -5f),
            hip + new Vector2(9f, 0f), 4f, Leather, flip);
        DrawBox(anchor, chest.X - 3f, chest.Y + 4f, 7f, 8f,
            new Color(130, 45, 47), flip);

        Vector2 neck = chest + new Vector2(2f, -14f);
        Vector2 head = neck + new Vector2(1f, -10f);
        DrawLine(anchor, chest + new Vector2(0f, -7f), neck,
            11f, skin, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 20f, 18f,
            Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 7f, 16f, 16f,
            skin, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 4f, 20f, 9f,
            IronDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 4f, 2f,
            new Color(207, 67, 49), flip);
        DrawLine(anchor, head + new Vector2(-6f, -6f),
            head + new Vector2(-11f, -11f), 3f, IronDark, flip);
        DrawLine(anchor, head + new Vector2(5f, -6f),
            head + new Vector2(8f, -12f), 3f, IronDark, flip);

        DrawToolArm(anchor, chest + new Vector2(19f, -1f),
            pressAngle, skin, flip, press: true, clampOpen: 0f);
        DrawToolArm(anchor, chest + new Vector2(-20f, 1f),
            clampAngle, skin, flip, press: false, clampOpen);

        if (enemy.CombatState == DemonCityIndustrialState.VentWindup)
        {
            float radius = MathHelper.Lerp(28f, 65f, p);
            DrawDiamond(anchor, new Vector2(0f, -48f), radius,
                radius * .55f, 2f, new Color(151, 61, 68, 105), flip);
        }
        else if (enemy.CombatState == DemonCityIndustrialState.VentActive)
        {
            float radius = MathHelper.Lerp(54f, 76f, p);
            DrawDiamond(anchor, new Vector2(0f, -48f), radius,
                radius * .62f, 3f,
                new Color(181, 79, 76, (int)(175f * (1f - p))), flip);
        }
    }

    private void DrawToolArm(
        Vector2 anchor,
        Vector2 shoulder,
        float angle,
        Color skin,
        bool flip,
        bool press,
        float clampOpen)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 elbow = shoulder + direction * 16f;
        Vector2 wrist = elbow + direction * 17f;
        DrawBox(anchor, shoulder.X - 9f, shoulder.Y - 8f, 18f, 14f,
            Outline, flip);
        DrawBox(anchor, shoulder.X - 7f, shoulder.Y - 6f, 14f, 10f,
            Iron, flip);
        DrawLimb(anchor, shoulder, elbow, skin, 12f, flip);
        DrawLimb(anchor, elbow, wrist, IronDark, 13f, flip);
        DrawLine(anchor, elbow, wrist, 4f, IronLight, flip);
        if (press)
        {
            Vector2 head = wrist + direction * 13f;
            Vector2 normal = new(-direction.Y, direction.X);
            DrawLine(anchor, wrist, head, 13f, IronDark, flip);
            DrawLine(anchor, head - normal * 12f, head + normal * 12f,
                16f, Outline, flip);
            DrawLine(anchor, head - normal * 10f, head + normal * 10f,
                12f, Iron, flip);
            DrawLine(anchor, head - normal * 8f,
                head + normal * 8f, 3f, Bronze, flip);
        }
        else
        {
            Vector2 jaw = wrist + direction * 12f;
            Vector2 normal = new(-direction.Y, direction.X);
            DrawLine(anchor, wrist, jaw, 12f, IronDark, flip);
            DrawLine(anchor, jaw,
                jaw + direction * 15f + normal * clampOpen,
                8f, Iron, flip);
            DrawLine(anchor, jaw,
                jaw + direction * 15f - normal * clampOpen,
                8f, Iron, flip);
            DrawLine(anchor,
                jaw + direction * 14f + normal * clampOpen,
                jaw + direction * 20f + normal * (clampOpen - 3f),
                5f, IronLight, flip);
            DrawLine(anchor,
                jaw + direction * 14f - normal * clampOpen,
                jaw + direction * 20f - normal * (clampOpen - 3f),
                5f, IronLight, flip);
        }
    }

    private void DrawCantor(
        DemonCityIndustrialEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        float p = enemy.CombatState == DemonCityIndustrialState.Ready
            ? 0f : enemy.ActionProgress;
        float wave = MathF.Sin(elapsed * MathHelper.TwoPi * .58f);
        float lean = 0f;
        float crouch = 0f;
        float step = MathF.Abs(enemy.VisualVelocityX) > 1f ? wave * 4f : 0f;
        float staffAngle = -1.48f;
        float handRaise = 2f;
        float plateLift = 0f;
        bool binding = enemy.CombatState is
            DemonCityIndustrialState.BindingCast or
            DemonCityIndustrialState.BindingCommit;
        bool war = enemy.CombatState is
            DemonCityIndustrialState.WarCast or
            DemonCityIndustrialState.WarCommit;
        bool lance = enemy.CombatState is
            DemonCityIndustrialState.LanceAim or
            DemonCityIndustrialState.LanceRelease;

        if (binding)
        {
            crouch = MathHelper.Lerp(0f, 5f, p);
            lean = 4f;
            staffAngle = MathHelper.Lerp(-1.48f, .92f, p);
            handRaise = MathHelper.Lerp(2f, -12f, p);
        }
        else if (war)
        {
            lean = -3f;
            staffAngle = MathHelper.Lerp(-1.48f, -1.02f, p);
            handRaise = MathHelper.Lerp(2f, 24f, p);
            plateLift = MathHelper.Lerp(0f, 20f, p);
        }
        else if (lance)
        {
            lean = 6f;
            staffAngle = MathHelper.Lerp(-1.48f, -.05f, p);
            handRaise = 10f;
        }
        else if (enemy.CombatState is DemonCityIndustrialState.BreakWindup or
            DemonCityIndustrialState.BreakActive)
        {
            crouch = 5f;
            staffAngle = 1.0f;
            handRaise = 17f;
        }
        else if (enemy.CombatState == DemonCityIndustrialState.CantorReposition)
        {
            lean = -4f;
            step = -4f;
        }
        else if (enemy.CombatState is DemonCityIndustrialState.BindingRecovery or
            DemonCityIndustrialState.WarRecovery or
            DemonCityIndustrialState.LanceRecovery or
            DemonCityIndustrialState.BreakRecovery)
        {
            crouch = MathHelper.Lerp(5f, 0f, p);
            lean = MathHelper.Lerp(5f, 0f, p);
            staffAngle = MathHelper.Lerp(.7f, -1.48f, p);
        }
        if (enemy.IsHitFlashing || enemy.IsStaggered)
        {
            lean = -8f;
            crouch = enemy.IsStaggered ? 8f : 4f;
            staffAngle = -2.1f;
            handRaise = -3f;
        }

        Color skin = enemy.IsHitFlashing
            ? new Color(204, 149, 152)
            : new Color(96, 65, 81);
        Vector2 hip = new(lean, -42f + crouch);
        Vector2 chest = new(lean, -72f + crouch + wave * .45f);
        Vector2 rearFoot = new(-7f - step, 0f);
        Vector2 frontFoot = new(8f + step, 0f);
        Vector2 rearKnee = new(-6f + step * .45f, -23f + crouch);
        Vector2 frontKnee = new(7f - step * .45f, -24f + crouch);
        DrawLimb(anchor, rearFoot, rearKnee, RobeDark, 6f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f), Robe, 7f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, RobeDark, 6f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 0f), Robe, 7f, flip);
        DrawLine(anchor, rearFoot + new Vector2(-5f, 0f),
            rearFoot + new Vector2(6f, 0f), 5f, IronDark, flip);
        DrawLine(anchor, frontFoot + new Vector2(-4f, 0f),
            frontFoot + new Vector2(8f, 0f), 5f, IronDark, flip);

        DrawLine(anchor, chest + new Vector2(-11f, 12f),
            hip + new Vector2(-15f - wave, 39f), 17f, RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(9f, 12f),
            hip + new Vector2(13f, 38f), 14f, Robe, flip);
        DrawLine(anchor, hip + new Vector2(-2f, 1f),
            hip + new Vector2(-1f, 38f), 4f, Violet, flip);
        DrawBox(anchor, hip.X - 14f, hip.Y - 4f, 28f, 7f,
            BronzeDark, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 8f, 28f, 35f,
            Outline, flip);
        DrawBox(anchor, chest.X - 12f, chest.Y - 6f, 24f, 31f,
            RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(-8f, -3f),
            chest + new Vector2(8f, 20f), 4f, BronzeDark, flip);
        DrawBox(anchor, chest.X - 3f, chest.Y + 5f, 6f, 8f,
            Violet, flip);

        Vector2 head = chest + new Vector2(0f, -22f);
        DrawLine(anchor, chest + new Vector2(0f, -6f),
            head + new Vector2(0f, 8f), 6f, skin, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 10f, 18f, 21f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 8f, 14f, 18f,
            IronDark, flip);
        DrawLine(anchor, head + new Vector2(-7f, -7f),
            head + new Vector2(0f, -17f), 4f, BronzeDark, flip);
        DrawLine(anchor, head + new Vector2(0f, -17f),
            head + new Vector2(7f, -7f), 4f, Bronze, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 3f, 2f,
            Sigil, flip);

        Vector2 staffShoulder = chest + new Vector2(12f, 0f);
        Vector2 staffDirection = new(MathF.Cos(staffAngle), MathF.Sin(staffAngle));
        Vector2 staffElbow = staffShoulder + staffDirection * 13f;
        Vector2 staffHand = staffElbow + staffDirection * 12f;
        DrawLimb(anchor, staffShoulder, staffElbow, Robe, 6f, flip);
        DrawLimb(anchor, staffElbow, staffHand, skin, 5f, flip);
        DrawShortStaff(anchor, staffHand, staffDirection, flip,
            binding ? new Color(158, 91, 174) :
            war ? new Color(184, 122, 89) : Sigil);

        Vector2 freeShoulder = chest + new Vector2(-12f, 1f);
        Vector2 freeElbow = freeShoulder + new Vector2(-8f, 12f - handRaise * .35f);
        Vector2 freeHand = freeElbow + new Vector2(7f, 10f - handRaise * .65f);
        DrawLimb(anchor, freeShoulder, freeElbow, RobeDark, 6f, flip);
        DrawLimb(anchor, freeElbow, freeHand, skin, 5f, flip);
        DrawLine(anchor, freeHand, freeHand + new Vector2(7f, -3f),
            2f, SkinLight, flip);
        DrawLine(anchor, freeHand, freeHand + new Vector2(6f, 2f),
            2f, SkinLight, flip);

        Vector2 beltPlate = hip + new Vector2(-17f, 8f - plateLift);
        DrawSigilPlate(anchor, beltPlate, flip,
            war ? Math.Clamp(p, .25f, 1f) : .15f);
        DrawSigilPlate(anchor, hip + new Vector2(16f, 12f + wave),
            flip, .12f);

        if (binding)
        {
            float radius = 12f + p * 10f;
            DrawDiamond(anchor, freeHand + new Vector2(4f, 4f),
                radius, radius * .45f, 2f,
                new Color(154, 101, 178, 165), flip);
        }
        else if (war)
        {
            DrawDiamond(anchor, beltPlate, 14f + p * 7f, 17f + p * 7f,
                2f, new Color(199, 139, 94, 175), flip);
        }
        else if (lance)
        {
            Vector2 focus = staffHand + staffDirection * 35f;
            DrawDiamond(anchor, focus, 11f + p * 5f, 9f + p * 4f,
                2f, new Color(180, 137, 201, 190), flip);
        }
        else if (enemy.CombatState == DemonCityIndustrialState.BreakActive)
        {
            float radius = MathHelper.Lerp(30f, 61f, p);
            DrawDiamond(anchor, new Vector2(0f, -45f), radius,
                radius * .62f, 2f,
                new Color(142, 94, 165, (int)(160f * (1f - p))), flip);
        }
    }

    private void DrawShortStaff(
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip,
        Color glow)
    {
        Vector2 basePoint = hand - direction * 17f;
        Vector2 tip = hand + direction * 29f;
        DrawLine(anchor, basePoint, tip, 6f, Outline, flip);
        DrawLine(anchor, basePoint, tip, 3f, Bronze, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, tip - normal * 7f, tip + normal * 7f,
            3f, Bronze, flip);
        DrawDiamond(anchor, tip + direction * 3f, 7f, 7f, 2f, glow, flip);
    }

    private void DrawSigilPlate(
        Vector2 anchor,
        Vector2 center,
        bool flip,
        float glow)
    {
        DrawBox(anchor, center.X - 6f, center.Y - 8f, 12f, 16f,
            Outline, flip);
        DrawBox(anchor, center.X - 4f, center.Y - 6f, 8f, 12f,
            BronzeDark, flip);
        DrawLine(anchor, center + new Vector2(-3f, -2f),
            center + new Vector2(3f, 2f), 2f,
            Color.Lerp(Bronze, Sigil, glow), flip);
        DrawLine(anchor, center + new Vector2(-3f, 3f),
            center + new Vector2(3f, -3f), 2f,
            Color.Lerp(Bronze, Sigil, glow), flip);
    }

    private void DrawServitorDeath(Vector2 anchor, bool flip, float progress)
    {
        float fail = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 1.6f));
        float drop = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .20f) / .80f));
        DrawLine(anchor, new Vector2(-8f, -78f + fail * 48f),
            new Vector2(30f + drop * 30f, -38f + fail * 30f),
            27f, new Color(79, 48, 47), flip);
        DrawBox(anchor, 25f + drop * 30f, -69f + fail * 51f,
            21f, 18f, IronDark, flip);
        DrawLine(anchor, new Vector2(-21f, -37f + fail * 23f),
            new Vector2(-58f - drop * 10f, -5f), 17f, Iron, flip);
        DrawLine(anchor, new Vector2(19f, -35f + fail * 22f),
            new Vector2(62f + drop * 12f, -4f), 15f, IronDark, flip);
        DrawLine(anchor, new Vector2(-18f, -57f + fail * 31f),
            new Vector2(-36f, -3f), 3f,
            new Color(89, 39, 43, (int)(180f * (1f - progress))), flip);
        DrawLine(anchor, new Vector2(-17f, -4f),
            new Vector2(16f, -3f), 11f, Leather, flip);
    }

    private void DrawCantorDeath(Vector2 anchor, bool flip, float progress)
    {
        float fold = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 1.7f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .28f) / .72f));
        DrawLine(anchor, new Vector2(-5f, -72f + fold * 52f),
            new Vector2(24f + settle * 18f, -34f + fold * 28f),
            19f, RobeDark, flip);
        DrawBox(anchor, 20f + settle * 18f, -67f + fold * 51f,
            18f, 17f, IronDark, flip);
        DrawLine(anchor, new Vector2(-9f, -45f + fold * 24f),
            new Vector2(-49f - settle * 9f, -4f), 5f, Bronze, flip);
        DrawSigilPlate(anchor, new Vector2(-17f, -6f), flip, 0f);
        DrawSigilPlate(anchor, new Vector2(7f, -5f), flip, 0f);
    }

    private void DrawLimb(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness,
        bool flip)
    {
        DrawLine(anchor, from, to, thickness + 2f, Outline, flip);
        DrawLine(anchor, from, to, thickness, color, flip);
    }

    private void DrawDiamond(
        Vector2 anchor,
        Vector2 center,
        float radiusX,
        float radiusY,
        float thickness,
        Color color,
        bool flip)
    {
        Vector2 top = center + new Vector2(0f, -radiusY);
        Vector2 right = center + new Vector2(radiusX, 0f);
        Vector2 bottom = center + new Vector2(0f, radiusY);
        Vector2 left = center + new Vector2(-radiusX, 0f);
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
        float worldX = flip ? anchor.X - x - width : anchor.X + x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(worldX),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLine(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color,
        bool flip)
    {
        if (flip)
        {
            from.X = -from.X;
            to.X = -to.X;
        }
        Vector2 start = anchor + from;
        Vector2 delta = to - from;
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
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    private sealed class AnimationClock
    {
        public DemonCityIndustrialState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, DemonCityIndustrialState state)
        {
            if (State != state)
            {
                State = state;
                Elapsed = 0f;
                return;
            }
            Elapsed += MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds, .10f);
        }
    }
}
