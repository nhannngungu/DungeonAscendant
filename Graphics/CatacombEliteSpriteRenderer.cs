using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Code-only elite silhouettes for Cursed Knight and Soul Collector.</summary>
public sealed class CatacombEliteSpriteRenderer
{
    private static readonly Color Outline = new(18, 19, 23);
    private static readonly Color PlateDark = new(38, 39, 45);
    private static readonly Color Plate = new(63, 61, 67);
    private static readonly Color PlateEdge = new(94, 84, 82);
    private static readonly Color Rust = new(94, 54, 46);
    private static readonly Color Curse = new(112, 51, 91);
    private static readonly Color CurseHot = new(157, 65, 100);
    private static readonly Color Cape = new(48, 36, 45);
    private static readonly Color Blade = new(117, 111, 112);
    private static readonly Color RobeDark = new(31, 32, 44);
    private static readonly Color Robe = new(55, 55, 76);
    private static readonly Color Soul = new(105, 113, 155);
    private static readonly Color SoulLight = new(157, 158, 191);
    private static readonly Color Bone = new(142, 139, 128);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public CatacombEliteSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(CatacombEnemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _controllers.GetValue(
            enemy, CreateController);
        animation.Update(gameTime, enemy);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool flip = enemy.Facing == EnemyFacingDirection.Left;

        if (enemy.Type == EnemyType.SoulCollector)
        {
            DrawSoulField(enemy, animation.Elapsed);
            if (!enemy.IsAlive)
                DrawCollectorDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawCollector(enemy, anchor, flip, animation);
            return;
        }

        DrawBox(anchor, -28f, -3f, 57f, 6f,
            new Color(5, 6, 9, 145), flip);
        if (!enemy.IsAlive)
            DrawKnightDeath(anchor, flip, enemy.DeathPresentationProgress);
        else
            DrawKnight(enemy, anchor, flip, animation);
    }

    private static EnemyAnimationController CreateController(Enemy enemy) =>
        new(enemy.Type == EnemyType.CursedKnight
            ? EnemyVisualProfile.CursedKnight
            : EnemyVisualProfile.SoulCollector);

    private void DrawKnight(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float step = animation.State == EnemyVisualState.Walk
            ? MathF.Sin(animation.Elapsed * 5.2f) * 5f : 0f;
        float bob = animation.State == EnemyVisualState.Walk
            ? MathF.Abs(MathF.Sin(animation.Elapsed * 5.2f)) * 2f : 0f;
        float crouch = animation.State == EnemyVisualState.KnightDormant
            ? 26f * (1f - progress) : 0f;
        float lean = 0f;
        float swordAngle = .72f;
        float swordReach = 0f;

        switch (animation.State)
        {
            case EnemyVisualState.SwordReady:
                crouch = 3f; swordAngle = .35f; break;
            case EnemyVisualState.AttackWindup:
                crouch = 4f + progress * 5f;
                lean = -7f * progress;
                swordAngle = enemy.CursedKnightAttack switch
                {
                    CursedKnightAttackKind.ExecutionStrike =>
                        MathHelper.Lerp(.5f, -1.62f, progress),
                    CursedKnightAttackKind.AdvancingThrust =>
                        MathHelper.Lerp(.55f, -.08f, progress),
                    _ => MathHelper.Lerp(.4f, -2.45f, progress)
                };
                break;
            case EnemyVisualState.CursedSweep:
                lean = 11f; crouch = 6f;
                swordAngle = MathHelper.Lerp(-2.45f, .12f, progress);
                break;
            case EnemyVisualState.ExecutionStrike:
                lean = 9f; crouch = 9f;
                swordAngle = MathHelper.Lerp(-1.62f, 1.10f, progress);
                break;
            case EnemyVisualState.AdvancingThrust:
                lean = 14f; crouch = 6f; swordAngle = -.05f;
                swordReach = 17f; break;
            case EnemyVisualState.AttackRecovery:
                lean = 10f * (1f - progress);
                crouch = 7f * (1f - progress);
                swordAngle = MathHelper.Lerp(.15f, .72f, progress);
                break;
            case EnemyVisualState.CursedArmorBreak:
                lean = -10f + MathF.Sin(animation.Elapsed * 14f) * 3f;
                crouch = 8f; swordAngle = 1.28f; break;
            case EnemyVisualState.Hurt:
                lean = -7f * (1f - progress); break;
            case EnemyVisualState.Stagger:
                lean = -13f + MathF.Sin(progress * 14f) * 4f;
                crouch = 7f; swordAngle = 1.35f; break;
        }

        Color plate = enemy.IsHitFlashing ? PlateEdge : Plate;
        Vector2 rearFoot = new(-11f - step * .4f, 0f);
        Vector2 frontFoot = new(13f + step, 0f);
        Vector2 hips = new(lean * .2f, -35f + crouch + bob);
        Vector2 chest = new(lean, -65f + crouch + bob);
        Vector2 head = chest + new Vector2(1f, -21f);

        DrawLineLocal(anchor, rearFoot, hips + new Vector2(-9f, 2f),
            14f, Outline, flip);
        DrawLineLocal(anchor, rearFoot, hips + new Vector2(-9f, 2f),
            10f, PlateDark, flip);
        DrawLineLocal(anchor, frontFoot, hips + new Vector2(10f, 1f),
            15f, Outline, flip);
        DrawLineLocal(anchor, frontFoot, hips + new Vector2(10f, 1f),
            11f, plate, flip);
        DrawBox(anchor, chest.X - 23f, chest.Y - 10f, 47f, 43f,
            Outline, flip);
        DrawBox(anchor, chest.X - 20f, chest.Y - 8f, 41f, 38f,
            PlateDark, flip);
        DrawBox(anchor, chest.X - 15f, chest.Y - 5f, 30f, 31f,
            plate, flip);
        DrawBox(anchor, chest.X - 4f, chest.Y - 4f, 6f, 34f,
            Rust, flip);
        DrawBox(anchor, chest.X - 15f, chest.Y + 28f, 31f, 29f,
            Cape, flip);

        Vector2 shoulder = chest + new Vector2(17f, -2f);
        Vector2 hand = shoulder + new Vector2(9f + swordReach, 21f);
        DrawLineLocal(anchor, chest + new Vector2(-18f, -2f), hand, 13f,
            Outline, flip);
        DrawLineLocal(anchor, chest + new Vector2(-18f, -2f), hand, 9f,
            PlateDark, flip);
        DrawLineLocal(anchor, shoulder, hand, 14f, Outline, flip);
        DrawLineLocal(anchor, shoulder, hand, 10f, plate, flip);
        DrawGreatsword(anchor, hand, swordAngle, flip, enemy.IsCursedArmorBroken);

        DrawBox(anchor, head.X - 14f, head.Y - 11f, 29f, 27f,
            Outline, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 9f, 25f, 23f,
            PlateDark, flip);
        DrawBox(anchor, head.X - 14f, head.Y - 9f, 29f, 8f,
            plate, flip);
        DrawBox(anchor, head.X - 9f, head.Y + 1f, 18f, 5f,
            Outline, flip);
        DrawBox(anchor, head.X + 2f, head.Y + 2f, 6f, 2f,
            enemy.IsCursedArmorBroken ? CurseHot : Curse, flip);

        DrawArmorCracks(anchor, chest, enemy.IsCursedArmorBroken, flip,
            animation.Elapsed);
        if (animation.State is EnemyVisualState.CursedSweep or
            EnemyVisualState.ExecutionStrike or
            EnemyVisualState.AdvancingThrust)
            DrawSwordTrail(anchor, hand, swordAngle, progress, flip);
    }

    private void DrawGreatsword(Vector2 anchor, Vector2 hand, float angle,
        bool flip, bool broken)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 guard = hand + direction * 5f;
        Vector2 tip = hand + direction * 61f;
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, hand - direction * 11f, tip, 9f, Outline, flip);
        DrawLineLocal(anchor, guard, tip, 6f, Blade, flip);
        DrawLineLocal(anchor, hand - direction * 10f, guard, 5f, Rust, flip);
        DrawLineLocal(anchor, guard - normal * 12f, guard + normal * 12f,
            6f, PlateDark, flip);
        DrawLineLocal(anchor, guard + direction * 8f, tip - direction * 5f,
            2f, broken ? CurseHot : Curse, flip);
    }

    private void DrawArmorCracks(Vector2 anchor, Vector2 chest, bool broken,
        bool flip, float elapsed)
    {
        float pulse = broken ? .72f + MathF.Sin(elapsed * 12f) * .2f : .38f;
        Color color = (broken ? CurseHot : Curse) * pulse;
        DrawLineLocal(anchor, chest + new Vector2(-5f, -5f),
            chest + new Vector2(2f, 6f), 2f, color, flip);
        DrawLineLocal(anchor, chest + new Vector2(2f, 6f),
            chest + new Vector2(-2f, 17f), 2f, color, flip);
        DrawLineLocal(anchor, chest + new Vector2(11f, 8f),
            chest + new Vector2(16f, 19f), 2f, color, flip);
    }

    private void DrawSwordTrail(Vector2 anchor, Vector2 hand, float angle,
        float progress, bool flip)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        Color trail = Curse * ((1f - progress * .55f) * .55f);
        DrawLineLocal(anchor, hand + direction * 28f - normal * 7f,
            hand + direction * 61f - normal * 11f, 4f, trail, flip);
    }

    private void DrawCollector(CatacombEnemy enemy, Vector2 anchor, bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float hover = MathF.Sin(animation.Elapsed * 3.4f) * 2f;
        float lean = 0f;
        float staffLift = 0f;
        float handReach = 0f;
        if (animation.State == EnemyVisualState.StaffRaise)
        {
            staffLift = progress * 24f;
            handReach = progress * 9f;
            lean = -3f * progress;
        }
        else if (animation.State is EnemyVisualState.SoulBoltCast or
            EnemyVisualState.RitualField or EnemyVisualState.GravePull or
            EnemyVisualState.SoulBurst)
        {
            staffLift = 24f; handReach = 13f; lean = 5f;
        }
        else if (animation.State == EnemyVisualState.Stagger)
            lean = -11f + MathF.Sin(progress * 13f) * 4f;

        Vector2 waist = new(lean * .2f, -31f + hover);
        Vector2 chest = new(lean, -65f + hover);
        Vector2 head = chest + new Vector2(-2f, -19f);
        DrawLineLocal(anchor, new Vector2(-22f, -2f), waist, 21f,
            RobeDark, flip);
        DrawLineLocal(anchor, new Vector2(20f, -1f), waist, 19f,
            Robe, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 8f, 35f, 41f,
            Outline, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 6f, 29f, 37f,
            RobeDark, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y - 4f, 11f, 35f,
            Robe, flip);

        Vector2 staffHand = chest + new Vector2(17f + handReach, 15f - staffLift);
        DrawLineLocal(anchor, chest + new Vector2(13f, -1f), staffHand,
            7f, Bone, flip);
        DrawStaff(anchor, staffHand, staffLift, flip);
        Vector2 ritualHand = chest + new Vector2(-15f, 4f);
        DrawLineLocal(anchor, ritualHand,
            ritualHand + new Vector2(-13f - handReach, 16f - staffLift * .35f),
            6f, Bone, flip);

        DrawBox(anchor, head.X - 11f, head.Y - 10f, 23f, 23f,
            Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 7f, 17f, 17f,
            new Color(52, 52, 61), flip);
        DrawBox(anchor, head.X - 11f, head.Y - 11f, 23f, 9f,
            RobeDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 4f, 3f,
            SoulLight, flip);

        DrawSoulWisps(anchor, chest, animation.Elapsed, flip,
            animation.State == EnemyVisualState.SoulBurst ? 1.5f : 1f);
        DrawRobeMist(anchor, animation.Elapsed, flip);
        if (animation.State == EnemyVisualState.GravePull)
            DrawPullTrail(anchor, progress, flip);
        else if (animation.State == EnemyVisualState.SoulBurst)
            DrawSoulBurst(anchor, progress);
    }

    private void DrawStaff(Vector2 anchor, Vector2 hand, float lift, bool flip)
    {
        Vector2 bottom = hand + new Vector2(2f, 63f + lift);
        Vector2 top = hand + new Vector2(-2f, -35f);
        DrawLineLocal(anchor, bottom, top, 6f, Outline, flip);
        DrawLineLocal(anchor, bottom, top, 3f, new Color(76, 59, 57), flip);
        DrawLineLocal(anchor, top, top + new Vector2(12f, -12f),
            5f, Outline, flip);
        DrawLineLocal(anchor, top + new Vector2(12f, -12f),
            top + new Vector2(18f, 2f), 4f, Bone, flip);
        DrawBox(anchor, top.X + 8f, top.Y - 9f, 10f, 13f,
            Soul * .7f, flip);
    }

    private void DrawSoulWisps(Vector2 anchor, Vector2 chest, float elapsed,
        bool flip, float spread)
    {
        for (int index = 0; index < 4; index++)
        {
            float angle = elapsed * (.8f + index * .09f) + index * 1.57f;
            float x = chest.X + MathF.Cos(angle) * (25f + index * 3f) * spread;
            float y = chest.Y - 8f + MathF.Sin(angle) * 14f - index * 4f;
            DrawBox(anchor, x - 2f, y - 2f, 5f, 5f,
                Soul * (.48f + index * .08f), flip);
        }
    }

    private void DrawRobeMist(Vector2 anchor, float elapsed, bool flip)
    {
        for (int index = 0; index < 3; index++)
        {
            float drift = (elapsed * (7f + index) + index * 15f) % 34f;
            DrawBox(anchor, -20f - drift, -5f + index * 3f,
                13f, 3f, Soul * .20f, flip);
        }
    }

    private void DrawPullTrail(Vector2 anchor, float progress, bool flip)
    {
        float reach = 45f + progress * 105f;
        DrawLineLocal(anchor, new Vector2(-reach, -24f),
            new Vector2(-18f, -41f), 4f, Soul * .58f, flip);
        DrawLineLocal(anchor, new Vector2(-reach + 18f, -7f),
            new Vector2(-15f, -35f), 2f, SoulLight * .52f, flip);
    }

    private void DrawSoulBurst(Vector2 anchor, float progress)
    {
        float radius = 27f + progress * 70f;
        DrawDiamond(anchor + new Vector2(0f, -42f), radius,
            Soul * ((1f - progress) * .58f));
    }

    private void DrawSoulField(CatacombEnemy enemy, float elapsed)
    {
        Rectangle bounds = enemy.RitualPreviewBounds;
        Color color = new(100, 83, 133, 125);
        if (enemy.HasActiveSoulField)
        {
            bounds = enemy.SoulFieldBounds;
            color = new Color(92, 76, 126, 95);
        }
        if (bounds.IsEmpty)
            return;
        DrawLine(new Vector2(bounds.Left, bounds.Bottom - 4),
            new Vector2(bounds.Right, bounds.Bottom - 4), 3f, color);
        DrawLine(new Vector2(bounds.Left + 18, bounds.Top + 8),
            new Vector2(bounds.Center.X, bounds.Bottom - 4), 2f, color);
        DrawLine(new Vector2(bounds.Right - 18, bounds.Top + 8),
            new Vector2(bounds.Center.X, bounds.Bottom - 4), 2f, color);
        float pulse = MathF.Sin(elapsed * 5f) * 5f;
        DrawDiamond(bounds.Center.ToVector2(), 25f + pulse, color);
    }

    private void DrawKnightDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathF.Min(1f, progress * 1.25f);
        float y = fall * 44f;
        float fade = 1f - MathF.Max(0f, progress - .84f) / .16f;
        DrawLineLocal(anchor, new Vector2(-12f, -2f),
            new Vector2(-4f + fall * 19f, -61f + y),
            22f, PlateDark * fade, flip);
        DrawBox(anchor, -15f + fall * 20f, -91f + y, 30f, 29f,
            Plate * fade, flip);
        DrawLineLocal(anchor, new Vector2(-12f, -47f + y),
            new Vector2(-55f - fall * 11f, -5f + y),
            8f, Blade * fade, flip);
        if (progress > .64f)
        {
            float dust = (progress - .64f) / .36f;
            DrawLineLocal(anchor, new Vector2(-48f, -2f),
                new Vector2(63f + dust * 20f, -2f), 5f,
                new Color(77, 68, 67) * (1f - dust), flip);
        }
    }

    private void DrawCollectorDeath(Vector2 anchor, bool flip, float progress)
    {
        float collapse = MathF.Min(1f, progress * 1.3f);
        float alpha = 1f - MathF.Max(0f, progress - .72f) / .28f;
        DrawLineLocal(anchor, new Vector2(-22f, -2f),
            new Vector2(-6f, -61f + collapse * 54f),
            20f, RobeDark * alpha, flip);
        DrawLineLocal(anchor, new Vector2(21f, -1f),
            new Vector2(7f, -60f + collapse * 53f),
            18f, Robe * alpha, flip);
        DrawLineLocal(anchor, new Vector2(27f, -5f),
            new Vector2(31f + progress * 18f, -96f + progress * 12f),
            4f, Bone * alpha, flip);
        for (int index = 0; index < 6; index++)
        {
            float side = index % 2 == 0 ? -1f : 1f;
            DrawBox(anchor, side * (8f + index * 4f),
                -44f - index * 9f - progress * 30f,
                5f, 5f, Soul * (alpha * .8f), flip);
        }
    }

    private void DrawDiamond(Vector2 center, float radius, Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius * .42f);
        Vector2 right = center + new Vector2(radius, 0f);
        Vector2 bottom = center + new Vector2(0f, radius * .42f);
        Vector2 left = center + new Vector2(-radius, 0f);
        DrawLine(top, right, 2f, color); DrawLine(right, bottom, 2f, color);
        DrawLine(bottom, left, 2f, color); DrawLine(left, top, 2f, color);
    }

    private void DrawBox(Vector2 anchor, float x, float y, float width,
        float height, Color color, bool flip)
    {
        float worldX = flip ? anchor.X - x - width : anchor.X + x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(worldX), (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLineLocal(Vector2 anchor, Vector2 from, Vector2 to,
        float thickness, Color color, bool flip)
    {
        if (flip) { from.X = -from.X; to.X = -to.X; }
        DrawLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f) return;
        _spriteBatch.Draw(_pixel, from, null, color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
