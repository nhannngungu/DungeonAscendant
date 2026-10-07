using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered procedural sprite renderer for the Goblin family. Every pose is
/// authored facing right and the complete composition is mirrored for left
/// facing; animation frame order is never reversed.
/// </summary>
public sealed class GoblinFamilySpriteRenderer
{
    private static readonly Color Outline = new(24, 25, 22);
    private static readonly Color GoblinSkin = new(91, 112, 62);
    private static readonly Color GoblinSkinLight = new(122, 139, 75);
    private static readonly Color HunterSkin = new(61, 87, 48);
    private static readonly Color HunterSkinLight = new(88, 111, 58);
    private static readonly Color YellowEye = new(230, 190, 54);
    private static readonly Color HideDark = new(65, 46, 34);
    private static readonly Color Hide = new(91, 65, 43);
    private static readonly Color DirtyCloth = new(65, 62, 48);
    private static readonly Color RustDark = new(65, 55, 50);
    private static readonly Color RustMetal = new(112, 91, 69);
    private static readonly Color BladeDark = new(65, 68, 66);
    private static readonly Color Blade = new(128, 128, 113);
    private static readonly Color CloakDark = new(47, 49, 33);
    private static readonly Color Cloak = new(69, 72, 42);
    private static readonly Color BowDark = new(62, 41, 27);
    private static readonly Color BowWood = new(105, 70, 38);
    private static readonly Color Bone = new(178, 165, 132);
    private static readonly Color BloodDark = new(89, 35, 31);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public GoblinFamilySpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(Enemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _controllers.GetValue(
            enemy,
            CreateController);
        animation.Update(gameTime, enemy);

        switch (enemy)
        {
            case Goblin goblin:
                DrawGoblin(goblin, animation);
                break;
            case GoblinHunter hunter:
                DrawHunter(hunter, animation);
                break;
        }
    }

    private static EnemyAnimationController CreateController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy is GoblinHunter
                ? EnemyVisualProfile.GoblinHunter
                : EnemyVisualProfile.Goblin);
    }

    private void DrawGoblin(
        Goblin goblin,
        EnemyAnimationController animation)
    {
        Vector2 anchor = GetFeetAnchor(goblin);
        bool flip = goblin.Facing == EnemyFacingDirection.Left;

        DrawBox(anchor, -20f, -3f, 40f, 6f,
            new Color(12, 15, 13, 145), flip);
        DrawEliteOutline(anchor, EnemyVisualProfile.Goblin.VisualSize,
            goblin.IsElite, flip);

        if (animation.State == EnemyVisualState.Death)
        {
            DrawGoblinDeath(anchor, flip, animation.Progress);
            return;
        }

        GoblinPose pose = CreateGoblinPose(animation);
        Color skin = goblin.IsHitFlashing
            ? new Color(211, 211, 181)
            : GoblinSkin;
        float bodyX = pose.Lean;
        float upperY = pose.Bob + pose.Crouch;
        bool weaponBehind = animation.State == EnemyVisualState.Move;

        if (weaponBehind)
            DrawCrudeSword(anchor, flip, pose, bodyX, upperY);

        DrawBox(anchor, -8f + bodyX, -20f, 7f, 20f,
            Outline, flip);
        DrawBox(anchor, -7f + bodyX + pose.LegSwing, -19f, 6f, 18f,
            DirtyCloth, flip);
        DrawBox(anchor, 3f + bodyX, -20f, 7f, 20f,
            Outline, flip);
        DrawBox(anchor, 4f + bodyX - pose.LegSwing, -19f, 6f, 18f,
            HideDark, flip);

        DrawBox(anchor, -10f + bodyX, -34f + upperY, 22f, 19f,
            Outline, flip);
        DrawBox(anchor, -8f + bodyX, -33f + upperY, 18f, 17f,
            Hide, flip);
        DrawBox(anchor, -9f + bodyX, -25f + upperY, 20f, 5f,
            DirtyCloth, flip);
        DrawBox(anchor, -11f + bodyX, -33f + upperY, 6f, 9f,
            RustDark, flip);
        DrawBox(anchor, -10f + bodyX, -32f + upperY, 5f, 7f,
            RustMetal, flip);

        DrawGoblinHead(anchor, flip, bodyX, upperY, skin);
        DrawGoblinArms(anchor, flip, pose, bodyX, upperY, skin);

        if (!weaponBehind)
            DrawCrudeSword(anchor, flip, pose, bodyX, upperY);

        if (goblin.Variant == GoblinVariant.Fast)
            DrawBox(anchor, -9f + bodyX, -29f + upperY, 20f, 3f,
                new Color(55, 112, 104), flip);
        else if (goblin.Variant == GoblinVariant.Brute)
            DrawBox(anchor, -14f + bodyX, -34f + upperY, 8f, 7f,
                RustMetal, flip);
    }

    private void DrawGoblinHead(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float upperY,
        Color skin)
    {
        DrawBox(anchor, -24f + bodyX, -48f + upperY, 14f, 8f,
            Outline, flip);
        DrawBox(anchor, -23f + bodyX, -47f + upperY, 14f, 6f,
            skin, flip);
        DrawBox(anchor, 12f + bodyX, -47f + upperY, 15f, 8f,
            Outline, flip);
        DrawBox(anchor, 12f + bodyX, -46f + upperY, 13f, 6f,
            skin, flip);
        DrawBox(anchor, -14f + bodyX, -51f + upperY, 31f, 24f,
            Outline, flip);
        DrawBox(anchor, -12f + bodyX, -49f + upperY, 27f, 20f,
            skin, flip);
        DrawBox(anchor, 14f + bodyX, -42f + upperY, 7f, 6f,
            Outline, flip);
        DrawBox(anchor, 14f + bodyX, -41f + upperY, 6f, 4f,
            GoblinSkinLight, flip);
        DrawBox(anchor, 7f + bodyX, -44f + upperY, 5f, 5f,
            YellowEye, flip);
        DrawBox(anchor, 2f + bodyX, -35f + upperY, 14f, 5f,
            BloodDark, flip);
        DrawBox(anchor, 6f + bodyX, -34f + upperY, 3f, 4f,
            Bone, flip);
        DrawBox(anchor, 12f + bodyX, -34f + upperY, 3f, 3f,
            Bone, flip);
        DrawBox(anchor, -4f + bodyX, -29f + upperY, 3f, 5f,
            Bone, flip);
        DrawBox(anchor, 1f + bodyX, -28f + upperY, 3f, 4f,
            Bone, flip);
    }

    private void DrawGoblinArms(
        Vector2 anchor,
        bool flip,
        GoblinPose pose,
        float bodyX,
        float upperY,
        Color skin)
    {
        Vector2 rearShoulder = new(-7f + bodyX, -31f + upperY);
        Vector2 rearHand = new(
            -13f + bodyX - pose.ArmSpread,
            -20f + upperY + pose.Crouch);
        Vector2 swordShoulder = new(9f + bodyX, -31f + upperY);
        Vector2 swordHand = new(
            13f + bodyX + pose.ArmSpread,
            -23f + upperY);
        DrawLimb(anchor, rearShoulder, rearHand, skin, flip, 5f);
        DrawLimb(anchor, swordShoulder, swordHand, skin, flip, 5f);
    }

    private void DrawCrudeSword(
        Vector2 anchor,
        bool flip,
        GoblinPose pose,
        float bodyX,
        float upperY)
    {
        Vector2 hand = new(
            13f + bodyX + pose.ArmSpread,
            -23f + upperY);
        Vector2 direction = new(
            MathF.Cos(pose.WeaponAngle),
            MathF.Sin(pose.WeaponAngle));
        Vector2 guard = hand + direction * 4f;
        Vector2 tip = guard + direction * 27f;
        Vector2 perpendicular = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, hand - direction * 6f, guard, 5f,
            Outline, flip);
        DrawLineLocal(anchor, hand - direction * 5f, guard, 3f,
            HideDark, flip);
        DrawLineLocal(anchor, guard - perpendicular * 6f,
            guard + perpendicular * 6f, 4f, RustMetal, flip);
        DrawLineLocal(anchor, guard, tip, 7f, BladeDark, flip);
        DrawLineLocal(anchor, guard + perpendicular,
            tip + perpendicular, 4f, Blade, flip);
    }

    private void DrawGoblinDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        float drop = settle * 9f;
        DrawBox(anchor, -19f, -13f + drop, 39f, 12f,
            Outline, flip);
        DrawBox(anchor, -17f, -12f + drop, 35f, 9f,
            Hide, flip);
        DrawBox(anchor, 8f, -24f + drop, 28f, 18f,
            Outline, flip);
        DrawBox(anchor, 10f, -22f + drop, 25f, 14f,
            GoblinSkin, flip);
        DrawBox(anchor, 27f, -18f + drop, 5f, 4f,
            new Color(116, 83, 42), flip);
        DrawLineLocal(anchor, new Vector2(-9f, -7f),
            new Vector2(-39f, -3f), 6f, BladeDark, flip);
        DrawLineLocal(anchor, new Vector2(-10f, -8f),
            new Vector2(-38f, -4f), 3f, Blade, flip);
    }

    private void DrawHunter(
        GoblinHunter hunter,
        EnemyAnimationController animation)
    {
        Vector2 anchor = GetFeetAnchor(hunter);
        bool flip = hunter.Facing == EnemyFacingDirection.Left;
        DrawBox(anchor, -20f, -3f, 42f, 6f,
            new Color(12, 15, 13, 145), flip);
        DrawEliteOutline(anchor, EnemyVisualProfile.GoblinHunter.VisualSize,
            hunter.IsElite, flip);

        if (animation.State == EnemyVisualState.Death)
        {
            DrawHunterDeath(anchor, flip, animation.Progress);
            return;
        }

        HunterPose pose = CreateHunterPose(animation);
        Color skin = hunter.IsHitFlashing
            ? new Color(199, 207, 177)
            : HunterSkin;
        float bodyX = pose.Lean;
        float upperY = pose.Bob + pose.Crouch;

        DrawHunterQuiver(anchor, flip, bodyX, upperY);
        DrawHunterCloak(anchor, flip, bodyX, upperY, pose.CloakTrail);

        DrawBox(anchor, -7f + bodyX + pose.LegSwing, -23f, 6f, 22f,
            Outline, flip);
        DrawBox(anchor, -6f + bodyX + pose.LegSwing, -22f, 5f, 20f,
            DirtyCloth, flip);
        DrawBox(anchor, 3f + bodyX - pose.LegSwing, -22f, 6f, 21f,
            Outline, flip);
        DrawBox(anchor, 4f + bodyX - pose.LegSwing, -21f, 5f, 19f,
            HideDark, flip);

        DrawBox(anchor, -9f + bodyX, -39f + upperY, 19f, 22f,
            Outline, flip);
        DrawBox(anchor, -7f + bodyX, -38f + upperY, 16f, 20f,
            new Color(56, 54, 38), flip);
        DrawBox(anchor, -9f + bodyX, -29f + upperY, 19f, 5f,
            Hide, flip);
        DrawBox(anchor, 4f + bodyX, -38f + upperY, 6f, 10f,
            RustMetal, flip);
        DrawHunterHead(anchor, flip, bodyX, upperY, skin);
        DrawHunterBowAndArms(hunter, anchor, flip, animation, bodyX,
            upperY, skin);
    }

    private void DrawHunterQuiver(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float upperY)
    {
        DrawLineLocal(anchor,
            new Vector2(-9f + bodyX, -36f + upperY),
            new Vector2(-15f + bodyX, -12f),
            9f, Outline, flip);
        DrawLineLocal(anchor,
            new Vector2(-9f + bodyX, -36f + upperY),
            new Vector2(-15f + bodyX, -12f),
            5f, HideDark, flip);

        for (int index = 0; index < 3; index++)
        {
            float x = -12f + bodyX + index * 3f;
            DrawLineLocal(anchor,
                new Vector2(x, -34f + upperY),
                new Vector2(x - 4f, -50f + upperY),
                2f, Bone, flip);
            DrawBox(anchor, x - 7f, -52f + upperY, 5f, 3f,
                new Color(76, 64, 43), flip);
        }
    }

    private void DrawHunterCloak(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float upperY,
        float trail)
    {
        DrawBox(anchor, -12f + bodyX, -41f + upperY, 19f, 7f,
            CloakDark, flip);
        DrawBox(anchor, -13f + bodyX - trail, -35f + upperY,
            17f + trail, 20f, Cloak, flip);
        DrawBox(anchor, -15f + bodyX - trail, -18f + upperY,
            8f, 7f, CloakDark, flip);
        DrawBox(anchor, -5f + bodyX - trail * 0.4f, -18f + upperY,
            7f, 5f, Cloak, flip);
    }

    private void DrawHunterHead(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float upperY,
        Color skin)
    {
        DrawBox(anchor, -18f + bodyX, -54f + upperY, 12f, 7f,
            Outline, flip);
        DrawBox(anchor, -17f + bodyX, -53f + upperY, 11f, 5f,
            skin, flip);
        DrawBox(anchor, 9f + bodyX, -53f + upperY, 14f, 7f,
            Outline, flip);
        DrawBox(anchor, 10f + bodyX, -52f + upperY, 12f, 5f,
            skin, flip);
        DrawBox(anchor, -10f + bodyX, -58f + upperY, 25f, 23f,
            Outline, flip);
        DrawBox(anchor, -8f + bodyX, -56f + upperY, 21f, 19f,
            skin, flip);
        DrawBox(anchor, 12f + bodyX, -49f + upperY, 7f, 5f,
            HunterSkinLight, flip);
        DrawBox(anchor, 5f + bodyX, -51f + upperY, 5f, 4f,
            YellowEye, flip);
        DrawBox(anchor, -8f + bodyX, -60f + upperY, 5f, 5f,
            new Color(34, 30, 24), flip);
        DrawBox(anchor, -1f + bodyX, -61f + upperY, 5f, 6f,
            new Color(34, 30, 24), flip);
        DrawBox(anchor, 6f + bodyX, -59f + upperY, 5f, 5f,
            new Color(34, 30, 24), flip);
        DrawBox(anchor, 3f + bodyX, -41f + upperY, 11f, 3f,
            BloodDark, flip);
    }

    private void DrawHunterBowAndArms(
        GoblinHunter hunter,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation,
        float bodyX,
        float upperY,
        Color skin)
    {
        bool isAiming = animation.State == EnemyVisualState.Aim ||
            animation.State == EnemyVisualState.Shoot ||
            animation.State == EnemyVisualState.AttackRecovery;
        Vector2 aim = isAiming
            ? hunter.VisualAimDirection
            : new Vector2(flip ? -1f : 1f, 0f);

        if (aim.LengthSquared() < 0.001f)
            aim = new Vector2(flip ? -1f : 1f, 0f);
        else
            aim.Normalize();

        Vector2 bowCenter = hunter.GetBowGripPosition();
        Vector2 perpendicular = new(-aim.Y, aim.X);
        Vector2 bowTop = bowCenter - perpendicular * 18f - aim * 4f;
        Vector2 bowBottom = bowCenter + perpendicular * 18f - aim * 4f;
        Vector2 bowUpperMid = bowCenter - perpendicular * 9f + aim * 2f;
        Vector2 bowLowerMid = bowCenter + perpendicular * 9f + aim * 2f;
        float drawAmount = animation.State == EnemyVisualState.Aim
            ? MathHelper.Lerp(3f, 14f, animation.Progress)
            : 0f;
        Vector2 stringHand = bowCenter - aim * drawAmount;
        Vector2 shoulder = LocalToWorld(anchor,
            new Vector2(7f + bodyX, -35f + upperY), flip);
        Vector2 rearShoulder = LocalToWorld(anchor,
            new Vector2(-5f + bodyX, -34f + upperY), flip);

        DrawLine(shoulder, bowCenter, 6f, Outline);
        DrawLine(shoulder, bowCenter, 4f, skin);
        DrawLine(rearShoulder, stringHand, 6f, Outline);
        DrawLine(rearShoulder, stringHand, 4f, skin);
        DrawLine(bowTop, bowUpperMid, 6f, BowDark);
        DrawLine(bowUpperMid, bowCenter + aim * 2f, 5f, BowWood);
        DrawLine(bowCenter + aim * 2f, bowLowerMid, 5f, BowWood);
        DrawLine(bowLowerMid, bowBottom, 6f, BowDark);
        DrawLine(bowTop, stringHand, 1.5f, Bone);
        DrawLine(stringHand, bowBottom, 1.5f, Bone);

        if (animation.State == EnemyVisualState.Aim)
        {
            Vector2 arrowTip = hunter.GetArrowReleasePosition();
            Vector2 arrowTail = stringHand - aim * 3f;
            DrawLine(arrowTail, arrowTip, 2.5f, BowWood);
            DrawLine(arrowTip - aim * 3f, arrowTip + aim * 3f,
                4f, BladeDark);
        }
    }

    private void DrawHunterDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        float drop = settle * 10f;
        DrawBox(anchor, -20f, -14f + drop, 41f, 12f,
            Outline, flip);
        DrawBox(anchor, -18f, -13f + drop, 37f, 9f,
            Cloak, flip);
        DrawBox(anchor, 10f, -25f + drop, 25f, 17f,
            Outline, flip);
        DrawBox(anchor, 12f, -23f + drop, 22f, 13f,
            HunterSkin, flip);
        DrawLineLocal(anchor, new Vector2(-5f, -7f),
            new Vector2(-37f, -2f), 5f, BowDark, flip);
        DrawLineLocal(anchor, new Vector2(-37f, -2f),
            new Vector2(-30f, -18f), 3f, BowWood, flip);
    }

    private static GoblinPose CreateGoblinPose(
        EnemyAnimationController animation)
    {
        float wave = MathF.Sin(animation.Progress * MathHelper.TwoPi);
        var pose = new GoblinPose { WeaponAngle = -0.55f };

        switch (animation.State)
        {
            case EnemyVisualState.Idle:
                pose.Bob = wave * 1f;
                pose.Lean = 2f;
                break;
            case EnemyVisualState.Move:
                pose.Bob = MathF.Abs(wave) * 2f;
                pose.Lean = 5f;
                pose.Crouch = 2f;
                pose.LegSwing = wave * 4f;
                pose.WeaponAngle = 2.45f;
                break;
            case EnemyVisualState.AttackWindup:
                pose.Lean = MathHelper.Lerp(1f, -3f, animation.Progress);
                pose.Crouch = animation.Progress * 3f;
                pose.WeaponAngle = MathHelper.Lerp(
                    -0.55f, -2.25f, animation.Progress);
                break;
            case EnemyVisualState.Attack:
                pose.Lean = MathHelper.Lerp(0f, 8f, animation.Progress);
                pose.Crouch = 3f;
                pose.WeaponAngle = MathHelper.Lerp(
                    -2.25f, 0.22f, animation.Progress);
                break;
            case EnemyVisualState.AttackRecovery:
                pose.Lean = MathHelper.Lerp(7f, 2f, animation.Progress);
                pose.Crouch = MathHelper.Lerp(4f, 0f, animation.Progress);
                pose.WeaponAngle = MathHelper.Lerp(
                    0.45f, -0.55f, animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.Lean = -5f * (1f - animation.Progress);
                pose.Crouch = 2f;
                pose.WeaponAngle = 1.2f;
                break;
            case EnemyVisualState.Stagger:
                pose.Lean = -7f + animation.Progress * 3f;
                pose.Crouch = 4f;
                pose.ArmSpread = 6f;
                pose.WeaponAngle = 2.2f;
                break;
        }

        return pose;
    }

    private static HunterPose CreateHunterPose(
        EnemyAnimationController animation)
    {
        float wave = MathF.Sin(animation.Progress * MathHelper.TwoPi);
        var pose = new HunterPose { Crouch = 3f };

        switch (animation.State)
        {
            case EnemyVisualState.Idle:
                pose.Bob = wave * 0.7f;
                pose.Lean = 2f;
                break;
            case EnemyVisualState.Move:
                pose.Bob = MathF.Abs(wave) * 1.5f;
                pose.Lean = 3f;
                pose.LegSwing = wave * 3f;
                pose.CloakTrail = 3f + MathF.Abs(wave) * 2f;
                break;
            case EnemyVisualState.Retreat:
                pose.Bob = MathF.Abs(wave) * 1.3f;
                pose.Lean = -1f;
                pose.LegSwing = wave * 3f;
                pose.CloakTrail = 5f + MathF.Abs(wave) * 2f;
                break;
            case EnemyVisualState.Aim:
                pose.Lean = MathHelper.Lerp(2f, 4f, animation.Progress);
                pose.Crouch = 5f;
                break;
            case EnemyVisualState.Shoot:
                pose.Lean = 6f;
                pose.Crouch = 4f;
                pose.CloakTrail = 2f;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.Lean = MathHelper.Lerp(5f, 2f, animation.Progress);
                pose.Crouch = MathHelper.Lerp(4f, 3f, animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.Lean = -5f * (1f - animation.Progress);
                pose.Crouch = 5f;
                break;
            case EnemyVisualState.Stagger:
                pose.Lean = -7f + animation.Progress * 3f;
                pose.Crouch = 7f;
                pose.CloakTrail = 4f;
                break;
        }

        return pose;
    }

    private void DrawEliteOutline(
        Vector2 anchor,
        Vector2 visualSize,
        bool isElite,
        bool flip)
    {
        if (!isElite)
            return;

        float x = -visualSize.X / 2f - 3f;
        float y = -visualSize.Y - 5f;
        float width = visualSize.X + 6f;
        float height = visualSize.Y + 7f;
        Color color = new(226, 169, 51, 210);
        DrawBox(anchor, x, y, width, 2f, color, flip);
        DrawBox(anchor, x, y + height - 2f, width, 2f, color, flip);
        DrawBox(anchor, x, y, 2f, height, color, flip);
        DrawBox(anchor, x + width - 2f, y, 2f, height, color, flip);
    }

    private void DrawLimb(
        Vector2 anchor,
        Vector2 start,
        Vector2 end,
        Color color,
        bool flip,
        float thickness)
    {
        DrawLineLocal(anchor, start, end, thickness + 2f, Outline, flip);
        DrawLineLocal(anchor, start, end, thickness, color, flip);
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
        if (flip)
            x = -x - width;

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                (int)MathF.Round(anchor.X + x),
                (int)MathF.Round(anchor.Y + y),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))),
            color);
    }

    private void DrawLineLocal(
        Vector2 anchor,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        bool flip)
    {
        DrawLine(
            LocalToWorld(anchor, start, flip),
            LocalToWorld(anchor, end, flip),
            thickness,
            color);
    }

    private void DrawLine(
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color)
    {
        Vector2 offset = end - start;
        float length = offset.Length();

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            start,
            sourceRectangle: null,
            color,
            MathF.Atan2(offset.Y, offset.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private static Vector2 GetFeetAnchor(Enemy enemy)
    {
        return new Vector2(
            MathF.Round(enemy.Position.X),
            MathF.Round(enemy.Position.Y + enemy.Size.Y / 2f));
    }

    private static Vector2 LocalToWorld(
        Vector2 anchor,
        Vector2 local,
        bool flip)
    {
        return anchor + new Vector2(flip ? -local.X : local.X, local.Y);
    }

    private struct GoblinPose
    {
        public float Bob;
        public float Lean;
        public float Crouch;
        public float LegSwing;
        public float ArmSpread;
        public float WeaponAngle;
    }

    private struct HunterPose
    {
        public float Bob;
        public float Lean;
        public float Crouch;
        public float LegSwing;
        public float CloakTrail;
    }
}
