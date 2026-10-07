using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Procedural presentation for the Wild Forest's airborne and buried ambush
/// enemies. Poses are authored facing right and mirrored as a whole for left.
/// </summary>
public sealed class WildForestAmbushSpriteRenderer
{
    private static readonly Color Outline = new(20, 18, 22);
    private static readonly Color BatWing = new(62, 24, 34);
    private static readonly Color BatWingLight = new(92, 31, 42);
    private static readonly Color BatFur = new(47, 28, 33);
    private static readonly Color BatBlood = new(119, 34, 39);
    private static readonly Color BatEye = new(244, 54, 47);
    private static readonly Color Bone = new(217, 204, 173);
    private static readonly Color BarkDark = new(50, 38, 29);
    private static readonly Color Bark = new(82, 60, 39);
    private static readonly Color BarkLight = new(112, 78, 47);
    private static readonly Color Moss = new(67, 91, 45);
    private static readonly Color Thorn = new(151, 43, 48);
    private static readonly Color PlantEye = new(139, 222, 81);
    private static readonly Color Soil = new(78, 57, 38);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public WildForestAmbushSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
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

        if (enemy is BloodBat bat)
            DrawBloodBat(bat, animation);
        else if (enemy is ThornCrawler crawler)
            DrawThornCrawler(crawler, animation);
    }

    private static EnemyAnimationController CreateController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy is BloodBat
                ? EnemyVisualProfile.BloodBat
                : EnemyVisualProfile.ThornCrawler);
    }

    private void DrawBloodBat(
        BloodBat bat,
        EnemyAnimationController animation)
    {
        bool flip = bat.Facing == EnemyFacingDirection.Left;
        float visualY = bat.Position.Y - bat.FlightVisualOffset;

        if (animation.State == EnemyVisualState.Death)
        {
            float landingY = float.IsNaN(bat.DeathLandingY)
                ? bat.Position.Y + 72f
                : bat.DeathLandingY;
            visualY = MathHelper.Lerp(
                visualY,
                landingY - 3f,
                MathHelper.SmoothStep(0f, 1f, animation.Progress));
            DrawBatDeath(new Vector2(bat.Position.X, visualY), flip,
                animation.Progress);
            return;
        }

        BatPose pose = CreateBatPose(animation);
        Vector2 anchor = new(
            bat.Position.X + pose.BodyX,
            visualY + pose.BodyY);
        Color wing = bat.IsHitFlashing
            ? new Color(210, 190, 185)
            : BatWing;
        Color fur = bat.IsHitFlashing
            ? new Color(225, 211, 201)
            : BatFur;

        if (animation.State == EnemyVisualState.DiveAttack)
        {
            for (int streak = 0; streak < 3; streak++)
            {
                float y = -11f + streak * 8f;
                DrawLine(anchor, new Vector2(-35f - streak * 4f, y),
                    new Vector2(-18f, y + 3f), 2f,
                    new Color(126, 71, 68, 85), flip);
            }
        }

        DrawBatWing(anchor, flip, left: true, pose, wing);
        DrawBatWing(anchor, flip, left: false, pose, wing);

        DrawBox(anchor, -9f, -12f, 18f, 25f, Outline, flip);
        DrawBox(anchor, -7f, -10f, 14f, 21f, fur, flip);
        DrawBox(anchor, -5f, 2f, 10f, 10f, BatBlood, flip);

        DrawLine(anchor, new Vector2(-5f, -9f),
            new Vector2(-8f + pose.EarFold, -23f), 6f, Outline, flip);
        DrawLine(anchor, new Vector2(-4f, -9f),
            new Vector2(-7f + pose.EarFold, -21f), 3f, BatWing, flip);
        DrawLine(anchor, new Vector2(4f, -9f),
            new Vector2(7f + pose.EarFold, -23f), 6f, Outline, flip);
        DrawLine(anchor, new Vector2(3f, -9f),
            new Vector2(6f + pose.EarFold, -21f), 3f, BatWing, flip);

        DrawBox(anchor, 3f, -7f, 4f, 4f, BatEye, flip);
        DrawBox(anchor, 5f, -6f, 2f, 2f,
            new Color(255, 157, 82), flip);
        DrawBox(anchor, 6f, -2f, 8f, 6f, Outline, flip);
        DrawBox(anchor, 7f, -1f, 6f, 4f, BatBlood, flip);
        DrawBox(anchor, 8f, 2f, 2f, 6f + pose.FangDrop, Bone, flip);
        DrawBox(anchor, 12f, 2f, 2f, 5f + pose.FangDrop, Bone, flip);
        DrawBox(anchor, 10f, 1f, 3f, 2f,
            new Color(152, 37, 42), flip);

        DrawHookedFoot(anchor, flip, -5f, pose.ClawReach);
        DrawHookedFoot(anchor, flip, 4f, pose.ClawReach);
    }

    private void DrawBatWing(
        Vector2 anchor,
        bool flip,
        bool left,
        BatPose pose,
        Color wing)
    {
        float direction = left ? -1f : 1f;
        float shoulderX = direction * 6f;
        float tipX = direction * pose.WingReach;
        float tipY = -5f - pose.WingLift;
        Vector2 shoulder = new(shoulderX, -7f);
        Vector2 upper = new(direction * pose.WingReach * 0.58f,
            -20f - pose.WingLift * 0.5f);
        Vector2 tip = new(tipX, tipY);
        Vector2 tornOne = new(direction * (pose.WingReach - 9f),
            7f + pose.WingLift * 0.18f);
        Vector2 tornTwo = new(direction * (pose.WingReach - 20f),
            2f + pose.WingLift * 0.12f);

        DrawLine(anchor, shoulder, upper, 8f, Outline, flip);
        DrawLine(anchor, upper, tip, 7f, Outline, flip);
        DrawLine(anchor, shoulder, upper, 5f, wing, flip);
        DrawLine(anchor, upper, tip, 4f, wing, flip);
        DrawLine(anchor, shoulder, tip, 10f,
            new Color(wing.R, wing.G, wing.B, (byte)210), flip);
        DrawLine(anchor, tip, tornOne, 7f, BatWingLight, flip);
        DrawLine(anchor, tornOne, tornTwo, 6f, wing, flip);
        DrawLine(anchor, tornTwo, shoulder + new Vector2(0f, 10f),
            5f, wing, flip);
        DrawLine(anchor, upper, tornTwo, 2f,
            new Color(129, 51, 59, 170), flip);
    }

    private void DrawHookedFoot(
        Vector2 anchor,
        bool flip,
        float x,
        float reach)
    {
        Vector2 ankle = new(x, 10f);
        Vector2 hook = new(x + 2f, 16f + reach);
        DrawLine(anchor, ankle, hook, 3f, Outline, flip);
        DrawLine(anchor, hook, hook + new Vector2(4f, -3f), 2f,
            Bone, flip);
    }

    private void DrawBatDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float fold = MathHelper.SmoothStep(0f, 1f, progress);
        DrawLine(anchor, new Vector2(-6f, -5f),
            new Vector2(-34f + 18f * fold, -8f + 7f * fold),
            7f, BatWing, flip);
        DrawLine(anchor, new Vector2(6f, -5f),
            new Vector2(34f - 18f * fold, -8f + 7f * fold),
            7f, BatWing, flip);
        DrawBox(anchor, -10f, -10f + 5f * fold, 22f, 13f,
            Outline, flip);
        DrawBox(anchor, -8f, -9f + 5f * fold, 18f, 10f,
            BatFur, flip);
        DrawLine(anchor, new Vector2(-13f, -2f),
            new Vector2(-22f + 8f * fold, 1f), 4f,
            BatWingLight, flip);
        DrawLine(anchor, new Vector2(13f, -2f),
            new Vector2(22f - 8f * fold, 1f), 4f,
            BatWingLight, flip);
    }

    private static BatPose CreateBatPose(EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new BatPose
        {
            WingReach = 44f,
            WingLift = MathF.Sin(cycle) * 9f,
            BodyY = MathF.Sin(cycle * 2f) * 1.2f
        };

        switch (animation.State)
        {
            case EnemyVisualState.IdleHover:
                pose.WingLift = MathF.Sin(cycle) * 7f;
                pose.BodyY = MathF.Sin(cycle) * 1.4f;
                break;
            case EnemyVisualState.Fly:
                pose.WingLift = MathF.Sin(cycle) * 13f;
                pose.BodyX = MathF.Max(0f, MathF.Sin(cycle)) * 2f;
                break;
            case EnemyVisualState.Approach:
                pose.WingReach = 36f;
                pose.WingLift = MathF.Sin(cycle) * 7f - 3f;
                pose.BodyX = 3f;
                pose.BodyY = 2f;
                pose.EarFold = -2f;
                break;
            case EnemyVisualState.LowAltitude:
                pose.WingReach = 46f;
                pose.WingLift = MathF.Sin(cycle) * 4f + 3f;
                pose.BodyY = 5f;
                pose.ClawReach = 3f;
                pose.FangDrop = 2f;
                break;
            case EnemyVisualState.DiveWindup:
                pose.WingReach = MathHelper.Lerp(44f, 28f,
                    animation.Progress);
                pose.WingLift = MathHelper.Lerp(8f, -5f,
                    animation.Progress);
                pose.BodyX = -3f * animation.Progress;
                pose.ClawReach = 4f * animation.Progress;
                break;
            case EnemyVisualState.DiveAttack:
                pose.WingReach = 29f;
                pose.WingLift = -7f;
                pose.BodyX = 9f * animation.Progress;
                pose.BodyY = 7f * animation.Progress;
                pose.ClawReach = 7f;
                pose.FangDrop = 3f;
                pose.EarFold = -3f;
                break;
            case EnemyVisualState.DiveRecovery:
                pose.WingReach = MathHelper.Lerp(30f, 44f,
                    animation.Progress);
                pose.WingLift = MathHelper.Lerp(-4f, 9f,
                    animation.Progress);
                pose.BodyY = MathHelper.Lerp(6f, -3f,
                    animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.WingReach = 38f;
                pose.WingLift = -5f * (1f - animation.Progress);
                pose.BodyX = -5f * (1f - animation.Progress);
                break;
            case EnemyVisualState.Stagger:
                pose.WingReach = 31f;
                pose.WingLift = -9f + MathF.Sin(cycle) * 2f;
                pose.BodyY = 10f;
                pose.EarFold = -4f;
                break;
        }

        return pose;
    }

    private void DrawThornCrawler(
        ThornCrawler crawler,
        EnemyAnimationController animation)
    {
        Vector2 anchor = new(crawler.Position.X, crawler.Bounds.Bottom);
        bool flip = crawler.Facing == EnemyFacingDirection.Left;

        if (animation.State == EnemyVisualState.Death)
        {
            DrawCrawlerDeath(anchor, flip, animation.Progress);
            return;
        }

        if (animation.State == EnemyVisualState.Hidden ||
            animation.State == EnemyVisualState.Warning)
        {
            DrawCrawlerGroundState(anchor, flip, animation);
            return;
        }

        CrawlerPose pose = CreateCrawlerPose(animation);
        DrawCrawlerGroundRoots(anchor, flip, pose.RootSpread,
            new Color(43, 32, 25));

        float bodyY = MathHelper.Lerp(8f, -19f, pose.Exposure) + pose.BodyY;
        Color bark = crawler.IsHitFlashing
            ? new Color(206, 196, 160)
            : Bark;
        DrawBox(anchor, -25f + pose.BodyX, bodyY, 50f, 20f,
            Outline, flip);
        DrawBox(anchor, -23f + pose.BodyX, bodyY + 2f, 46f, 16f,
            bark, flip);
        DrawBox(anchor, -20f + pose.BodyX, bodyY + 2f, 25f, 5f,
            Moss, flip);
        DrawBox(anchor, 5f + pose.BodyX, bodyY + 5f, 15f, 4f,
            BarkLight, flip);

        DrawBox(anchor, 11f + pose.BodyX, bodyY + 3f, 8f, 16f,
            Outline, flip);
        DrawBox(anchor, 13f + pose.BodyX, bodyY + 5f, 4f, 12f,
            new Color(31, 20, 22), flip);

        for (int tooth = 0; tooth < 3; tooth++)
        {
            DrawBox(anchor, 12f + pose.BodyX, bodyY + 6f + tooth * 4f,
                3f, 2f, Thorn, flip);
        }

        DrawBox(anchor, 5f + pose.BodyX, bodyY + 5f, 3f, 3f,
            PlantEye, flip);
        DrawBox(anchor, 7f + pose.BodyX, bodyY + 5f, 2f, 2f,
            new Color(207, 245, 122), flip);

        DrawCrawlerThorn(anchor, flip,
            new Vector2(-16f + pose.BodyX, bodyY + 2f), 10f, pose.ThornLift);
        DrawCrawlerThorn(anchor, flip,
            new Vector2(-3f + pose.BodyX, bodyY), 13f, pose.ThornLift);
        DrawCrawlerThorn(anchor, flip,
            new Vector2(9f + pose.BodyX, bodyY + 2f), 9f, pose.ThornLift);

        DrawCrawlerVine(anchor, flip, -20f + pose.BodyX, bodyY + 13f,
            -1f, pose.VineReach * 0.65f, Moss);
        DrawCrawlerVine(anchor, flip, 19f + pose.BodyX, bodyY + 12f,
            1f, pose.VineReach, BarkLight);

        if (animation.State == EnemyVisualState.Emerging ||
            animation.State == EnemyVisualState.Burrow)
        {
            DrawDirtMotes(anchor, flip, animation.Frame,
                animation.State == EnemyVisualState.Emerging
                    ? animation.Progress
                    : 1f - animation.Progress);
        }
    }

    private void DrawCrawlerGroundState(
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float warning = animation.State == EnemyVisualState.Warning
            ? animation.Progress
            : 0f;
        float twitch = animation.State == EnemyVisualState.Warning
            ? MathF.Sin(animation.Progress * MathHelper.Pi * 5f) * 2f
            : MathF.Sin(animation.Progress * MathHelper.TwoPi) * 0.7f;
        DrawCrawlerGroundRoots(anchor, flip, 25f + warning * 6f, Soil);
        DrawBox(anchor, -20f, -5f, 40f, 6f, Soil, flip);
        DrawBox(anchor, -14f, -7f, 18f, 4f, Moss, flip);
        DrawLine(anchor, new Vector2(-18f, -3f),
            new Vector2(-29f, -2f + twitch), 3f, BarkDark, flip);
        DrawLine(anchor, new Vector2(13f, -3f),
            new Vector2(28f, -1f - twitch), 3f, BarkDark, flip);
        DrawCrawlerThorn(anchor, flip, new Vector2(-9f, -5f),
            5f + warning * 8f, warning);
        DrawCrawlerThorn(anchor, flip, new Vector2(5f, -5f),
            4f + warning * 10f, warning);

        if (warning > 0f)
            DrawDirtMotes(anchor, flip, animation.Frame, warning);
    }

    private void DrawCrawlerGroundRoots(
        Vector2 anchor,
        bool flip,
        float spread,
        Color color)
    {
        DrawLine(anchor, new Vector2(-5f, -3f),
            new Vector2(-spread, 0f), 4f, color, flip);
        DrawLine(anchor, new Vector2(4f, -3f),
            new Vector2(spread, 0f), 4f, color, flip);
        DrawLine(anchor, new Vector2(-2f, -2f),
            new Vector2(-spread * 0.7f, -6f), 3f, color, flip);
        DrawLine(anchor, new Vector2(3f, -2f),
            new Vector2(spread * 0.65f, -5f), 3f, color, flip);
    }

    private void DrawCrawlerVine(
        Vector2 anchor,
        bool flip,
        float rootX,
        float rootY,
        float direction,
        float reach,
        Color color)
    {
        Vector2 root = new(rootX, rootY);
        Vector2 joint = new(rootX + direction * reach * 0.55f, rootY - 5f);
        Vector2 tip = new(rootX + direction * reach, rootY + 3f);
        DrawLine(anchor, root, joint, 5f, Outline, flip);
        DrawLine(anchor, joint, tip, 4f, Outline, flip);
        DrawLine(anchor, root, joint, 3f, color, flip);
        DrawLine(anchor, joint, tip, 2f, color, flip);
        DrawLine(anchor, tip, tip + new Vector2(direction * 5f, -5f),
            2f, Thorn, flip);
    }

    private void DrawCrawlerThorn(
        Vector2 anchor,
        bool flip,
        Vector2 root,
        float height,
        float lift)
    {
        DrawLine(anchor, root,
            root + new Vector2(2f, -height * MathF.Max(0.35f, lift)),
            3f, Outline, flip);
        DrawLine(anchor, root + new Vector2(1f, -1f),
            root + new Vector2(2f, -height * MathF.Max(0.35f, lift)),
            1.5f, Thorn, flip);
    }

    private void DrawDirtMotes(
        Vector2 anchor,
        bool flip,
        int frame,
        float amount)
    {
        int count = Math.Clamp((int)MathF.Ceiling(amount * 4f), 1, 4);

        for (int index = 0; index < count; index++)
        {
            float direction = index % 2 == 0 ? -1f : 1f;
            float x = direction * (10f + index * 5f);
            float y = -4f - ((frame + index * 2) % 5) * 2f;
            DrawBox(anchor, x, y, 3f, 3f,
                new Color(104, 76, 47, 155), flip);
        }
    }

    private void DrawCrawlerDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float collapse = MathHelper.SmoothStep(0f, 1f, progress);
        float y = MathHelper.Lerp(-18f, -8f, collapse);
        DrawCrawlerGroundRoots(anchor, flip,
            MathHelper.Lerp(30f, 38f, collapse), BarkDark);
        DrawBox(anchor, -27f, y, 54f, 12f, Outline, flip);
        DrawBox(anchor, -25f, y + 2f, 50f, 8f, BarkDark, flip);
        DrawBox(anchor, -16f, y + 1f, 18f, 4f,
            new Color(55, 70, 39), flip);
        DrawLine(anchor, new Vector2(-11f, y),
            new Vector2(-17f, y + 8f), 3f, Thorn, flip);
        DrawLine(anchor, new Vector2(8f, y),
            new Vector2(14f, y + 8f), 3f, Thorn, flip);
    }

    private static CrawlerPose CreateCrawlerPose(
        EnemyAnimationController animation)
    {
        var pose = new CrawlerPose
        {
            Exposure = 1f,
            RootSpread = 31f,
            ThornLift = 1f,
            VineReach = 25f
        };

        switch (animation.State)
        {
            case EnemyVisualState.Emerging:
                pose.Exposure = MathHelper.SmoothStep(0f, 1f,
                    animation.Progress);
                pose.RootSpread = MathHelper.Lerp(22f, 31f,
                    animation.Progress);
                pose.ThornLift = animation.Progress;
                pose.VineReach = MathHelper.Lerp(10f, 25f,
                    animation.Progress);
                break;
            case EnemyVisualState.Move:
                pose.BodyY = MathF.Sin(animation.Progress *
                    MathHelper.TwoPi) * 1.2f;
                pose.VineReach = 27f;
                break;
            case EnemyVisualState.AttackWindup:
                pose.BodyX = -4f * animation.Progress;
                pose.BodyY = 3f * animation.Progress;
                pose.VineReach = MathHelper.Lerp(25f, 14f,
                    animation.Progress);
                pose.ThornLift = 1f + animation.Progress * 0.25f;
                break;
            case EnemyVisualState.Attack:
                pose.BodyX = 5f * animation.Progress;
                pose.VineReach = MathHelper.Lerp(18f, 48f,
                    animation.Progress);
                pose.ThornLift = 1.2f;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.BodyX = MathHelper.Lerp(4f, 0f, animation.Progress);
                pose.VineReach = MathHelper.Lerp(44f, 24f,
                    animation.Progress);
                pose.BodyY = 2f;
                break;
            case EnemyVisualState.Recovery:
                pose.BodyY = 3f + animation.Progress * 2f;
                pose.VineReach = MathHelper.Lerp(26f, 18f,
                    animation.Progress);
                pose.ThornLift = MathHelper.Lerp(1f, 0.7f,
                    animation.Progress);
                break;
            case EnemyVisualState.Burrow:
                pose.Exposure = 1f - MathHelper.SmoothStep(0f, 1f,
                    animation.Progress);
                pose.RootSpread = MathHelper.Lerp(31f, 23f,
                    animation.Progress);
                pose.ThornLift = 1f - animation.Progress;
                pose.VineReach = MathHelper.Lerp(22f, 8f,
                    animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -5f * (1f - animation.Progress);
                pose.VineReach = 17f;
                pose.ThornLift = 1f + MathF.Sin(
                    animation.Progress * MathHelper.Pi * 4f) * 0.25f;
                break;
            case EnemyVisualState.Stagger:
                pose.BodyY = 7f;
                pose.RootSpread = 37f;
                pose.VineReach = 34f;
                pose.ThornLift = 0.55f;
                break;
        }

        return pose;
    }

    private void DrawBox(
        Vector2 anchor,
        float localX,
        float localY,
        float width,
        float height,
        Color color,
        bool flip)
    {
        float x = flip ? -localX - width : localX;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                (int)MathF.Round(anchor.X + x),
                (int)MathF.Round(anchor.Y + localY),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))),
            color);
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

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            start,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private struct BatPose
    {
        public float BodyX;
        public float BodyY;
        public float WingReach;
        public float WingLift;
        public float EarFold;
        public float ClawReach;
        public float FangDrop;
    }

    private struct CrawlerPose
    {
        public float BodyX;
        public float BodyY;
        public float Exposure;
        public float RootSpread;
        public float ThornLift;
        public float VineReach;
    }
}
