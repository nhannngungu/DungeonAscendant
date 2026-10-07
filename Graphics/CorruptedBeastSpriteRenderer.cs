using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered procedural renderer for non-humanoid Wild Forest creatures. Poses
/// are authored facing right and mirrored as a complete composition for left
/// facing, so frame order and gait direction always remain forward.
/// </summary>
public sealed class CorruptedBeastSpriteRenderer
{
    public static readonly Vector2 DireWolfVisualScale = new(0.76f, 0.69f);
    public static readonly Vector2 GiantSpiderVisualScale = new(0.69f, 0.88f);

    private static readonly Color Outline = new(20, 19, 23);
    private static readonly Color WolfDark = new(45, 43, 40);
    private static readonly Color WolfFur = new(73, 69, 62);
    private static readonly Color WolfLight = new(116, 105, 88);
    private static readonly Color WolfBlack = new(19, 19, 20);
    private static readonly Color Scar = new(124, 75, 67);
    private static readonly Color Bone = new(220, 207, 169);
    private static readonly Color EyeGlow = new(242, 80, 37);
    private static readonly Color SpiderShell = new(45, 31, 52);
    private static readonly Color SpiderShellLight = new(72, 45, 78);
    private static readonly Color SpiderLeg = new(37, 27, 43);
    private static readonly Color SpiderBlood = new(111, 38, 55);
    private static readonly Color SpiderEye = new(235, 218, 116);
    private static readonly Color Silk = new(221, 229, 218);
    private static readonly Color Dirt = new(80, 60, 43);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();
    private Vector2 _visualScale = Vector2.One;

    public CorruptedBeastSpriteRenderer(
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

        if (enemy is DireWolf wolf)
            DrawDireWolf(wolf, animation);
        else if (enemy is GiantSpider spider)
            DrawGiantSpider(spider, animation);
    }

    private static EnemyAnimationController CreateController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy is DireWolf
                ? EnemyVisualProfile.DireWolf
                : EnemyVisualProfile.GiantSpider);
    }

    private void DrawDireWolf(
        DireWolf wolf,
        EnemyAnimationController animation)
    {
        _visualScale = DireWolfVisualScale;
        Vector2 anchor = new(wolf.Position.X, wolf.Bounds.Bottom);
        bool flip = wolf.Facing == EnemyFacingDirection.Left;
        DrawBox(anchor, -43f, -3f, 87f, 5f,
            new Color(10, 12, 12, 135), flip);

        if (animation.State == EnemyVisualState.Death)
        {
            DrawWolfDeath(anchor, flip, animation.Progress);
            return;
        }

        WolfPose pose = CreateWolfPose(animation);
        Color fur = wolf.IsHitFlashing
            ? new Color(205, 197, 177)
            : WolfFur;
        float bodyX = pose.BodyX;
        float bodyY = pose.BodyY;

        DrawWolfTail(anchor, flip, bodyX, bodyY, pose);
        DrawWolfLeg(anchor, flip, -23f + bodyX, -20f + bodyY,
            pose.RearStride, pose.LegCompression, WolfDark);
        DrawWolfLeg(anchor, flip, -10f + bodyX, -19f + bodyY,
            -pose.RearStride, pose.LegCompression, WolfFur);
        DrawWolfLeg(anchor, flip, 12f + bodyX, -20f + bodyY,
            pose.FrontStride, pose.LegCompression, WolfDark);
        DrawWolfLeg(anchor, flip, 24f + bodyX, -19f + bodyY,
            -pose.FrontStride, pose.LegCompression, WolfFur);

        DrawBox(anchor, -31f + bodyX, -43f + bodyY, 61f, 25f,
            Outline, flip);
        DrawBox(anchor, -29f + bodyX, -41f + bodyY, 57f, 21f,
            fur, flip);
        DrawBox(anchor, -24f + bodyX, -25f + bodyY, 43f, 6f,
            WolfLight, flip);
        DrawBox(anchor, 13f + bodyX, -45f + bodyY, 18f, 28f,
            WolfDark, flip);
        DrawBox(anchor, 16f + bodyX, -43f + bodyY, 14f, 24f,
            fur, flip);

        DrawWolfHead(anchor, flip, fur, bodyX + pose.HeadX,
            bodyY + pose.HeadY, pose.JawOpen, pose.EarsBack);

        DrawLine(anchor, new Vector2(-16f + bodyX, -40f + bodyY),
            new Vector2(-5f + bodyX, -34f + bodyY), 2f, Scar, flip);
        DrawLine(anchor, new Vector2(-11f + bodyX, -42f + bodyY),
            new Vector2(0f + bodyX, -36f + bodyY), 2f, Scar, flip);
        DrawLine(anchor, new Vector2(1f + bodyX, -40f + bodyY),
            new Vector2(9f + bodyX, -36f + bodyY), 2f, Scar, flip);
    }

    private void DrawWolfHead(
        Vector2 anchor,
        bool flip,
        Color fur,
        float offsetX,
        float offsetY,
        float jawOpen,
        bool earsBack)
    {
        DrawBox(anchor, 24f + offsetX, -49f + offsetY, 29f, 25f,
            Outline, flip);
        DrawBox(anchor, 26f + offsetX, -47f + offsetY, 25f, 21f,
            fur, flip);

        float earLean = earsBack ? -5f : 0f;
        DrawLine(anchor, new Vector2(30f + offsetX, -47f + offsetY),
            new Vector2(27f + earLean + offsetX, -63f + offsetY),
            8f, Outline, flip);
        DrawLine(anchor, new Vector2(31f + offsetX, -47f + offsetY),
            new Vector2(28f + earLean + offsetX, -60f + offsetY),
            4f, WolfDark, flip);
        DrawLine(anchor, new Vector2(43f + offsetX, -47f + offsetY),
            new Vector2(46f + earLean + offsetX, -58f + offsetY),
            8f, Outline, flip);
        DrawBox(anchor, 44f + earLean + offsetX, -59f + offsetY,
            7f, 5f, WolfDark, flip);

        DrawBox(anchor, 45f + offsetX, -40f + offsetY, 25f, 14f,
            Outline, flip);
        DrawBox(anchor, 47f + offsetX, -38f + offsetY, 21f, 10f,
            WolfDark, flip);
        DrawBox(anchor, 66f + offsetX, -37f + offsetY, 7f, 7f,
            WolfBlack, flip);
        DrawBox(anchor, 43f + offsetX, -43f + offsetY, 5f, 5f,
            EyeGlow, flip);
        DrawBox(anchor, 45f + offsetX, -42f + offsetY, 2f, 2f,
            new Color(255, 171, 57), flip);

        float jawY = -27f + offsetY + jawOpen;
        DrawLine(anchor, new Vector2(48f + offsetX, jawY),
            new Vector2(68f + offsetX, jawY + 2f), 7f,
            Outline, flip);
        DrawLine(anchor, new Vector2(49f + offsetX, jawY - 1f),
            new Vector2(66f + offsetX, jawY + 1f), 4f,
            WolfDark, flip);
        DrawBox(anchor, 53f + offsetX, -29f + offsetY, 3f,
            7f + jawOpen * 0.3f, Bone, flip);
        DrawBox(anchor, 62f + offsetX, -29f + offsetY, 3f,
            6f + jawOpen * 0.3f, Bone, flip);
    }

    private void DrawWolfTail(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY,
        WolfPose pose)
    {
        Vector2 basePoint = new(-28f + bodyX, -36f + bodyY);
        Vector2 middle = new(-45f + bodyX, -36f + bodyY + pose.TailDrop);
        Vector2 tip = new(-58f + bodyX, -31f + bodyY + pose.TailDrop * 1.5f);
        DrawLine(anchor, basePoint, middle, 11f, Outline, flip);
        DrawLine(anchor, middle, tip, 9f, Outline, flip);
        DrawLine(anchor, basePoint, middle, 7f, WolfDark, flip);
        DrawLine(anchor, middle, tip, 5f, WolfBlack, flip);
    }

    private void DrawWolfLeg(
        Vector2 anchor,
        bool flip,
        float rootX,
        float rootY,
        float stride,
        float compression,
        Color color)
    {
        Vector2 root = new(rootX, rootY);
        Vector2 knee = new(rootX + stride * 0.35f, -10f + compression);
        Vector2 foot = new(rootX + stride, -2f);
        DrawLine(anchor, root, knee, 8f, Outline, flip);
        DrawLine(anchor, knee, foot, 6f, Outline, flip);
        DrawLine(anchor, root, knee, 5f, color, flip);
        DrawLine(anchor, knee, foot, 3f, color, flip);
        DrawLine(anchor, foot, foot + new Vector2(7f, 0f), 3f,
            WolfBlack, flip);
    }

    private void DrawWolfDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        float drop = 13f * settle;
        DrawLine(anchor, new Vector2(-37f, -16f + drop),
            new Vector2(-55f, -4f), 8f, WolfBlack, flip);
        DrawBox(anchor, -34f, -30f + drop, 65f, 24f,
            Outline, flip);
        DrawBox(anchor, -32f, -28f + drop, 61f, 20f,
            WolfFur, flip);
        DrawBox(anchor, 23f, -26f + drop, 30f, 21f,
            Outline, flip);
        DrawBox(anchor, 25f, -24f + drop, 27f, 17f,
            WolfDark, flip);
        DrawBox(anchor, 48f, -16f + drop, 17f, 9f,
            WolfBlack, flip);
        DrawLine(anchor, new Vector2(-21f, -9f + drop),
            new Vector2(-6f, -2f), 6f, WolfDark, flip);
        DrawLine(anchor, new Vector2(5f, -9f + drop),
            new Vector2(20f, -2f), 6f, WolfDark, flip);
    }

    private static WolfPose CreateWolfPose(EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new WolfPose();

        switch (animation.State)
        {
            case EnemyVisualState.Prowl:
                pose.BodyY = MathF.Sin(cycle * 2f) * 1.2f;
                pose.RearStride = MathF.Sin(cycle) * 6f;
                pose.FrontStride = MathF.Sin(cycle + MathF.PI) * 6f;
                break;
            case EnemyVisualState.Run:
                pose.BodyY = MathF.Sin(cycle * 2f) * 1.5f;
                pose.RearStride = MathF.Sin(cycle) * 11f;
                pose.FrontStride = MathF.Sin(cycle + MathF.PI) * 11f;
                pose.LegCompression = MathF.Max(0f, MathF.Cos(cycle)) * 3f;
                pose.EarsBack = true;
                break;
            case EnemyVisualState.Retreat:
                pose.BodyY = 2f + MathF.Sin(cycle * 2f);
                pose.RearStride = MathF.Sin(cycle) * 12f;
                pose.FrontStride = MathF.Sin(cycle + MathF.PI) * 12f;
                pose.TailDrop = 9f;
                pose.EarsBack = true;
                break;
            case EnemyVisualState.AttackWindup:
                pose.BodyX = -4f * animation.Progress;
                pose.BodyY = 6f * animation.Progress;
                pose.HeadX = -5f * animation.Progress;
                pose.LegCompression = 5f * animation.Progress;
                pose.EarsBack = true;
                break;
            case EnemyVisualState.LungeAttack:
                pose.BodyX = 10f * MathHelper.SmoothStep(0f, 1f,
                    animation.Progress);
                pose.BodyY = -2f;
                pose.HeadX = -7f;
                pose.JawOpen = 8f;
                pose.RearStride = -9f;
                pose.FrontStride = 12f;
                pose.EarsBack = true;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.BodyX = MathHelper.Lerp(8f, 0f, animation.Progress);
                pose.HeadX = MathHelper.Lerp(-6f, 0f, animation.Progress);
                pose.BodyY = 3f * (1f - animation.Progress);
                pose.JawOpen = 4f * (1f - animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -6f * (1f - animation.Progress);
                pose.HeadX = -4f;
                pose.BodyY = 2f;
                break;
            case EnemyVisualState.Stagger:
                pose.BodyX = -8f * MathF.Sin(animation.Progress * MathF.PI);
                pose.BodyY = 7f;
                pose.HeadY = 5f;
                pose.RearStride = -6f;
                pose.FrontStride = 7f;
                pose.TailDrop = 7f;
                pose.EarsBack = true;
                break;
            default:
                pose.BodyY = MathF.Sin(cycle) * 0.8f;
                pose.HeadY = MathF.Sin(cycle + 0.7f) * 0.5f;
                break;
        }

        return pose;
    }

    private void DrawGiantSpider(
        GiantSpider spider,
        EnemyAnimationController animation)
    {
        _visualScale = GiantSpiderVisualScale;
        Vector2 anchor = new(spider.Position.X, spider.Bounds.Bottom);
        bool flip = spider.Facing == EnemyFacingDirection.Left;
        DrawBox(anchor, -49f, -2f, 99f, 4f,
            new Color(9, 10, 12, 140), flip);

        if (animation.State == EnemyVisualState.Death)
        {
            DrawSpiderDeath(anchor, flip, animation.Progress);
            return;
        }

        SpiderPose pose = CreateSpiderPose(animation);
        Color shell = spider.IsHitFlashing
            ? new Color(206, 195, 205)
            : SpiderShell;

        for (int index = 0; index < 4; index++)
            DrawSpiderLeg(anchor, flip, index, pose, backLayer: true);

        DrawSpiderAbdomen(anchor, flip, pose, shell);
        DrawSpiderHead(anchor, flip, pose, shell);

        for (int index = 0; index < 4; index++)
            DrawSpiderLeg(anchor, flip, index, pose, backLayer: false);

        if (animation.State == EnemyVisualState.WebPrepare ||
            animation.State == EnemyVisualState.WebShoot)
        {
            float size = animation.State == EnemyVisualState.WebPrepare
                ? 3f + animation.Progress * 6f
                : 9f - animation.Progress * 3f;
            DrawBox(anchor, 40f + pose.BodyX, -31f + pose.BodyY,
                size, size, Silk, flip);
            DrawLine(anchor,
                new Vector2(37f + pose.BodyX, -27f + pose.BodyY),
                new Vector2(44f + size + pose.BodyX, -31f + pose.BodyY),
                2f,
                new Color(235, 240, 232, 190),
                flip);
        }
    }

    private void DrawSpiderAbdomen(
        Vector2 anchor,
        bool flip,
        SpiderPose pose,
        Color shell)
    {
        float x = -34f + pose.BodyX;
        float y = -38f + pose.BodyY;
        DrawBox(anchor, x + 5f, y - 3f, 42f, 35f, Outline, flip);
        DrawBox(anchor, x, y + 3f, 52f, 25f, Outline, flip);
        DrawBox(anchor, x + 7f, y - 1f, 38f, 31f, shell, flip);
        DrawBox(anchor, x + 2f, y + 5f, 48f, 20f, shell, flip);
        DrawBox(anchor, x + 10f, y + 4f, 31f, 5f,
            SpiderShellLight, flip);
        DrawBox(anchor, x + 13f, y + 10f, 7f, 8f,
            SpiderBlood, flip);
        DrawBox(anchor, x + 31f, y + 10f, 7f, 8f,
            SpiderBlood, flip);
        DrawLine(anchor, new Vector2(x + 19f, y + 21f),
            new Vector2(x + 31f, y + 25f), 3f, SpiderBlood, flip);
        DrawLine(anchor, new Vector2(x + 31f, y + 25f),
            new Vector2(x + 40f, y + 20f), 3f, SpiderBlood, flip);
        DrawBox(anchor, x - 5f, y + 13f, 8f, 7f,
            Outline, flip);
        DrawBox(anchor, x - 4f, y + 14f, 5f, 5f,
            new Color(157, 151, 139), flip);
    }

    private void DrawSpiderHead(
        Vector2 anchor,
        bool flip,
        SpiderPose pose,
        Color shell)
    {
        float x = 12f + pose.BodyX + pose.HeadX;
        float y = -33f + pose.BodyY - pose.FrontRise;
        DrawBox(anchor, x, y, 29f, 24f, Outline, flip);
        DrawBox(anchor, x + 2f, y + 2f, 25f, 20f, shell, flip);

        DrawBox(anchor, x + 14f, y + 4f, 5f, 5f, SpiderEye, flip);
        DrawBox(anchor, x + 21f, y + 5f, 5f, 5f, SpiderEye, flip);
        DrawBox(anchor, x + 10f, y + 10f, 4f, 4f, SpiderEye, flip);
        DrawBox(anchor, x + 18f, y + 11f, 4f, 4f, SpiderEye, flip);
        DrawBox(anchor, x + 25f, y + 11f, 2f, 2f,
            new Color(244, 233, 160), flip);
        DrawBox(anchor, x + 7f, y + 13f, 2f, 2f,
            new Color(244, 233, 160), flip);

        float fangSpread = pose.FangOpen;
        DrawLine(anchor, new Vector2(x + 19f, y + 18f),
            new Vector2(x + 24f + fangSpread, y + 29f), 4f,
            Outline, flip);
        DrawLine(anchor, new Vector2(x + 20f, y + 19f),
            new Vector2(x + 24f + fangSpread, y + 28f), 2f,
            Bone, flip);
        DrawLine(anchor, new Vector2(x + 12f, y + 18f),
            new Vector2(x + 15f - fangSpread, y + 29f), 4f,
            Outline, flip);
        DrawLine(anchor, new Vector2(x + 13f, y + 19f),
            new Vector2(x + 15f - fangSpread, y + 28f), 2f,
            Bone, flip);
    }

    private void DrawSpiderLeg(
        Vector2 anchor,
        bool flip,
        int index,
        SpiderPose pose,
        bool backLayer)
    {
        float layerOffset = backLayer ? -3f : 2f;
        float rootX = -13f + index * 12f + pose.BodyX;
        float rootY = -24f + pose.BodyY + layerOffset;
        float groupSign = (index + (backLayer ? 1 : 0)) % 2 == 0
            ? 1f
            : -1f;
        float stride = pose.CrawlStride * groupSign;
        float direction = index < 2 ? -1f : 1f;
        float reach = pose.LegSpan + (index == 0 || index == 3 ? 7f : 0f);
        Vector2 joint = new(
            rootX + direction * (20f + stride * 0.35f),
            -38f + pose.BodyY + index * 2f + layerOffset);
        Vector2 foot = new(
            rootX + direction * reach + stride,
            -2f);
        Color color = backLayer
            ? new Color(29, 22, 34)
            : SpiderLeg;
        DrawLine(anchor, new Vector2(rootX, rootY), joint, 6f,
            Outline, flip);
        DrawLine(anchor, joint, foot, 5f, Outline, flip);
        DrawLine(anchor, new Vector2(rootX, rootY), joint, 3f,
            color, flip);
        DrawLine(anchor, joint, foot, 2f, color, flip);
        DrawBox(anchor, joint.X - 2f, joint.Y - 2f, 5f, 5f,
            new Color(78, 51, 82), flip);
        DrawLine(anchor, joint, joint + new Vector2(direction * 5f, -5f),
            2f, Outline, flip);

        if (index == 1 || index == 3)
            DrawBox(anchor, foot.X - 2f, foot.Y - 3f, 5f, 3f,
                Dirt, flip);
    }

    private void DrawSpiderDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        float drop = settle * 11f;

        for (int index = 0; index < 4; index++)
        {
            float rootX = -15f + index * 11f;
            float curl = MathHelper.Lerp(42f, 20f, settle);
            float direction = index < 2 ? -1f : 1f;
            Vector2 root = new(rootX, -15f + drop);
            Vector2 joint = new(rootX + direction * 18f, -25f + drop);
            Vector2 foot = new(rootX + direction * curl, -4f);
            DrawLine(anchor, root, joint, 5f, SpiderLeg, flip);
            DrawLine(anchor, joint, foot, 4f, SpiderLeg, flip);
        }

        DrawBox(anchor, -31f, -27f + drop, 52f, 22f,
            Outline, flip);
        DrawBox(anchor, -29f, -25f + drop, 48f, 18f,
            SpiderShell, flip);
        DrawBox(anchor, 15f, -22f + drop, 27f, 17f,
            Outline, flip);
        DrawBox(anchor, 17f, -20f + drop, 23f, 13f,
            SpiderShellLight, flip);
    }

    private static SpiderPose CreateSpiderPose(
        EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new SpiderPose { LegSpan = 46f };

        switch (animation.State)
        {
            case EnemyVisualState.Move:
                pose.CrawlStride = MathF.Sin(cycle) * 8f;
                pose.BodyY = MathF.Sin(cycle * 2f) * 0.8f;
                break;
            case EnemyVisualState.AttackWindup:
                pose.FrontRise = animation.Progress * 8f;
                pose.FangOpen = animation.Progress * 5f;
                pose.LegSpan = 49f;
                break;
            case EnemyVisualState.BiteAttack:
                pose.HeadX = MathHelper.SmoothStep(0f, 12f,
                    animation.Progress);
                pose.FangOpen = 6f * (1f - animation.Progress * 0.45f);
                pose.FrontRise = 5f * (1f - animation.Progress);
                break;
            case EnemyVisualState.AttackRecovery:
                pose.HeadX = MathHelper.Lerp(8f, 0f, animation.Progress);
                pose.FangOpen = 3f * (1f - animation.Progress);
                break;
            case EnemyVisualState.WebPrepare:
                pose.BodyY = 1f;
                pose.LegSpan = 50f;
                pose.FangOpen = 2f + animation.Progress * 2f;
                break;
            case EnemyVisualState.WebShoot:
                pose.HeadX = 4f;
                pose.FangOpen = 5f;
                pose.LegSpan = 51f;
                break;
            case EnemyVisualState.WebRecovery:
                pose.HeadX = MathHelper.Lerp(4f, 0f, animation.Progress);
                pose.FangOpen = 3f * (1f - animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -4f * (1f - animation.Progress);
                pose.LegSpan = 39f;
                pose.BodyY = 2f;
                break;
            case EnemyVisualState.Stagger:
                pose.BodyY = 9f;
                pose.BodyX = -5f * MathF.Sin(animation.Progress * MathF.PI);
                pose.LegSpan = 36f;
                pose.FrontRise = -3f;
                break;
            default:
                pose.BodyY = MathF.Sin(cycle) * 0.7f;
                pose.FangOpen = (MathF.Sin(cycle) + 1f) * 0.5f;
                pose.CrawlStride = MathF.Sin(cycle) * 1.5f;
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
        float scaledX = localX * _visualScale.X;
        float scaledY = localY * _visualScale.Y;
        float scaledWidth = width * _visualScale.X;
        float scaledHeight = height * _visualScale.Y;
        float x = flip ? -scaledX - scaledWidth : scaledX;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                (int)MathF.Round(anchor.X + x),
                (int)MathF.Round(anchor.Y + scaledY),
                Math.Max(1, (int)MathF.Round(scaledWidth)),
                Math.Max(1, (int)MathF.Round(scaledHeight))),
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
        from *= _visualScale;
        to *= _visualScale;

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
            new Vector2(
                length,
                MathF.Max(1f, thickness * _visualScale.Y)),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private struct WolfPose
    {
        public float BodyX;
        public float BodyY;
        public float HeadX;
        public float HeadY;
        public float RearStride;
        public float FrontStride;
        public float LegCompression;
        public float JawOpen;
        public float TailDrop;
        public bool EarsBack;
    }

    private struct SpiderPose
    {
        public float BodyX;
        public float BodyY;
        public float HeadX;
        public float FrontRise;
        public float FangOpen;
        public float CrawlStride;
        public float LegSpan;
    }
}
