using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only silhouettes for Rotten Corpse and Wraith. Gameplay clocks drive
/// every anticipation and active pose, keeping their drawn attacks honest.
/// </summary>
public sealed class CatacombRotSpriteRenderer
{
    private static readonly Color Outline = new(22, 23, 25);
    private static readonly Color CorpseDark = new(47, 53, 43);
    private static readonly Color CorpseSkin = new(91, 101, 76);
    private static readonly Color CorpseLight = new(118, 123, 91);
    private static readonly Color Bruise = new(62, 49, 61);
    private static readonly Color BurialDark = new(57, 54, 49);
    private static readonly Color BurialCloth = new(103, 98, 84);
    private static readonly Color Rot = new(91, 91, 48);
    private static readonly Color WraithDark = new(34, 39, 53);
    private static readonly Color WraithRobe = new(73, 84, 111);
    private static readonly Color WraithLight = new(116, 137, 158);
    private static readonly Color Soul = new(105, 133, 155);
    private static readonly Color Curse = new(99, 78, 119);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public CatacombRotSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(CatacombEnemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _controllers.GetValue(
            enemy,
            CreateController);
        animation.Update(gameTime, enemy);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool flip = enemy.Facing == EnemyFacingDirection.Left;

        DrawBox(anchor, -22f, -3f, 44f, 5f,
            new Color(7, 9, 12, 120), flip);
        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.RottenCorpse)
                DrawRottenDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawWraithDeath(anchor, flip, enemy.DeathPresentationProgress);
            return;
        }

        if (enemy.Type == EnemyType.RottenCorpse)
            DrawRottenCorpse(enemy, anchor, flip, animation);
        else
            DrawWraith(enemy, anchor, flip, animation);
    }

    public void DrawBurst(RottenCorpseBurst burst)
    {
        Vector2 center = burst.Position + new Vector2(0f, 20f);
        if (!burst.HasBurst)
        {
            float progress = burst.WarningProgress;
            float radius = 28f + progress * 58f;
            Color haze = new Color(93, 91, 57, (int)(70 + progress * 95f));
            DrawDiamond(center, radius, 3f, haze);
            DrawDiamond(center, radius * .62f, 2f,
                new Color(108, 88, 79, 155));
            for (int index = 0; index < 5; index++)
            {
                float angle = index * 1.256f + progress * .9f;
                Vector2 mote = center + new Vector2(
                    MathF.Cos(angle) * radius * .38f,
                    MathF.Sin(angle) * radius * .2f - progress * 12f);
                DrawWorldBox(mote, 3f, 3f, new Color(99, 100, 62, 145));
            }
            return;
        }

        float after = burst.AftermathProgress;
        float alpha = 1f - after;
        DrawLine(center + new Vector2(-62f - after * 25f, 7f),
            center + new Vector2(62f + after * 25f, 7f),
            5f,
            Rot * (.55f * alpha));
        for (int index = 0; index < 7; index++)
        {
            float direction = index % 2 == 0 ? -1f : 1f;
            float x = direction * (12f + index * 8f) * (1f + after);
            float y = -8f - (index % 3) * 8f - after * 24f;
            DrawWorldBox(center + new Vector2(x, y), 4f, 3f,
                new Color(92, 87, 56) * alpha);
        }
    }

    private static EnemyAnimationController CreateController(Enemy enemy) =>
        new(enemy.Type == EnemyType.RottenCorpse
            ? EnemyVisualProfile.RottenCorpse
            : EnemyVisualProfile.Wraith);

    private void DrawRottenCorpse(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float wave = MathF.Sin(animation.Elapsed * 4.1f);
        float breath = animation.State == EnemyVisualState.Idle
            ? wave * 2.2f
            : 0f;
        float step = animation.State == EnemyVisualState.Walk
            ? MathF.Sin(animation.Elapsed * 6.6f) * 5f
            : 0f;
        float progress = animation.Progress;
        float crouch = 0f;
        float lean = -3f;
        float frontReach = 4f;
        float rearReach = -2f;

        switch (animation.State)
        {
            case EnemyVisualState.GrabWindup:
                lean = -8f * progress;
                crouch = 3f * progress;
                frontReach = -8f + progress * 8f;
                break;
            case EnemyVisualState.GrabAttack:
                lean = 8f * progress;
                frontReach = 25f + progress * 13f;
                rearReach = 14f;
                break;
            case EnemyVisualState.BodySlamWindup:
                crouch = progress * 14f;
                lean = -10f * progress;
                frontReach = -10f;
                rearReach = -12f;
                break;
            case EnemyVisualState.BodySlamAttack:
                crouch = 13f - progress * 4f;
                lean = 18f - progress * 5f;
                frontReach = 29f;
                rearReach = 18f;
                break;
            case EnemyVisualState.AttackWindup:
                lean = -6f * progress;
                crouch = 4f * progress;
                break;
            case EnemyVisualState.RotVomit:
                lean = 12f;
                crouch = 4f;
                frontReach = 3f;
                break;
            case EnemyVisualState.AttackRecovery:
                lean = 10f * (1f - progress) - 4f * progress;
                crouch = 5f * (1f - progress);
                break;
            case EnemyVisualState.Hurt:
                lean = -7f * (1f - progress);
                break;
            case EnemyVisualState.Stagger:
                lean = -13f + MathF.Sin(progress * MathHelper.TwoPi) * 5f;
                crouch = 7f;
                break;
        }

        Color skin = enemy.IsHitFlashing ? CorpseLight : CorpseSkin;
        Vector2 rearFoot = new(-13f - step * .4f, 0f);
        Vector2 frontFoot = new(14f + step, 0f);
        Vector2 hips = new(lean * .25f, -27f + crouch);
        Vector2 chest = new(lean, -51f + crouch - breath * .25f);
        Vector2 head = chest + new Vector2(5f, -17f);

        DrawLimb(anchor, rearFoot, hips + new Vector2(-9f, 2f), 9f,
            CorpseDark, skin, flip);
        DrawLimb(anchor, frontFoot, hips + new Vector2(10f, 1f), 11f,
            CorpseDark, skin, flip);
        DrawBox(anchor, hips.X - 18f, hips.Y - 8f, 36f, 18f,
            Outline, flip);
        DrawBox(anchor, hips.X - 16f, hips.Y - 7f, 32f, 15f,
            BurialDark, flip);

        DrawBox(anchor, chest.X - 25f - breath * .35f, chest.Y - 13f,
            52f + breath * .7f, 32f, Outline, flip);
        DrawBox(anchor, chest.X - 23f - breath * .35f, chest.Y - 11f,
            48f + breath * .7f, 28f, skin, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y + 4f, 33f, 12f,
            Bruise, flip);
        DrawBox(anchor, chest.X - 25f, chest.Y - 2f, 14f, 19f,
            BurialCloth, flip);
        DrawBox(anchor, chest.X + 10f, chest.Y - 10f, 13f, 11f,
            BurialDark, flip);

        Vector2 rearShoulder = chest + new Vector2(-20f, -5f);
        Vector2 frontShoulder = chest + new Vector2(21f, -4f);
        Vector2 rearHand = rearShoulder + new Vector2(rearReach - 2f, 31f);
        Vector2 frontHand = frontShoulder + new Vector2(frontReach, 31f);
        DrawLimb(anchor, rearShoulder, rearHand, 9f,
            CorpseDark, skin, flip);
        DrawLimb(anchor, frontShoulder, frontHand, 10f,
            CorpseDark, skin, flip);
        DrawBox(anchor, rearHand.X - 4f, rearHand.Y - 3f, 9f, 9f,
            CorpseDark, flip);
        DrawBox(anchor, frontHand.X - 4f, frontHand.Y - 3f, 10f, 9f,
            skin, flip);

        DrawBox(anchor, head.X - 12f, head.Y - 10f, 25f, 22f,
            Outline, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 9f, 21f, 19f,
            skin, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 3f, 5f, 4f,
            new Color(31, 29, 27), flip);
        DrawBox(anchor, head.X - 7f, head.Y + 3f, 11f, 5f,
            Bruise, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 12f, 19f, 5f,
            BurialDark, flip);

        DrawBox(anchor, hips.X - 15f, hips.Y + 4f, 11f, 18f,
            BurialCloth, flip);
        DrawBox(anchor, hips.X + 4f, hips.Y + 5f, 13f, 14f,
            BurialDark, flip);
        DrawBox(anchor, chest.X - 3f, chest.Y + 16f, 4f, 5f, Rot, flip);

        if (animation.State == EnemyVisualState.RotVomit)
            DrawRotVomit(anchor, head, flip, progress);
        if (animation.State == EnemyVisualState.BodySlamAttack)
            DrawSlamDust(anchor, flip, progress);
    }

    private void DrawWraith(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float opacity = enemy.IsEthereal ? .42f : .88f;
        if (animation.State == EnemyVisualState.Materialize)
            opacity = .38f + progress * .55f;
        else if (animation.State == EnemyVisualState.PhaseOut)
            opacity = .9f - progress * .55f;
        if (enemy.IsHitFlashing)
            opacity = 1f;

        float hover = MathF.Sin(animation.Elapsed * 4f) * 2.5f;
        float lean = 0f;
        float clawReach = 0f;
        if (animation.State == EnemyVisualState.AttackWindup)
        {
            lean = -7f * progress;
            clawReach = -5f * progress;
        }
        else if (animation.State == EnemyVisualState.SpectralClaw)
        {
            lean = 9f;
            clawReach = 24f + progress * 16f;
        }
        else if (animation.State == EnemyVisualState.SpectralDash)
        {
            lean = 16f;
            clawReach = 19f;
        }
        else if (animation.State == EnemyVisualState.Stagger)
        {
            lean = -10f + MathF.Sin(progress * 12f) * 4f;
        }

        Color dark = WraithDark * opacity;
        Color robe = WraithRobe * opacity;
        Color light = (enemy.IsHitFlashing ? WraithLight : Soul) * opacity;
        Vector2 waist = new(lean * .25f, -27f + hover);
        Vector2 chest = new(lean, -54f + hover);
        Vector2 head = chest + new Vector2(2f, -17f);

        DrawLineLocal(anchor, new Vector2(-19f, -2f), waist, 18f,
            dark, flip);
        DrawLineLocal(anchor, new Vector2(18f, -1f), waist, 17f,
            robe, flip);
        DrawLineLocal(anchor, new Vector2(-10f, -8f), chest, 28f,
            dark, flip);
        DrawLineLocal(anchor, new Vector2(11f, -7f), chest, 25f,
            robe, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y - 10f, 37f, 29f,
            dark, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 9f, 29f, 25f,
            robe, flip);
        DrawBox(anchor, chest.X - 9f, chest.Y - 6f, 7f, 28f,
            WraithDark * (opacity * .8f), flip);

        Vector2 rearShoulder = chest + new Vector2(-15f, -4f);
        Vector2 frontShoulder = chest + new Vector2(16f, -3f);
        Vector2 rearHand = rearShoulder + new Vector2(-8f, 24f);
        Vector2 frontHand = frontShoulder + new Vector2(8f + clawReach, 20f);
        DrawLineLocal(anchor, rearShoulder, rearHand, 7f, dark, flip);
        DrawLineLocal(anchor, frontShoulder, frontHand, 8f, robe, flip);
        DrawClaw(anchor, frontHand, light, flip);

        DrawBox(anchor, head.X - 12f, head.Y - 11f, 25f, 24f,
            dark, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 8f, 19f, 18f,
            new Color(40, 44, 57) * opacity, flip);
        DrawBox(anchor, head.X + 1f, head.Y - 2f, 4f, 3f,
            light, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 1f, 3f, 2f,
            Soul * (opacity * .55f), flip);

        DrawMist(anchor, opacity, animation.Elapsed, flip);
        if (animation.State == EnemyVisualState.CurseWave)
            DrawCurseWave(anchor, flip, progress, opacity);
        if (animation.State is EnemyVisualState.Materialize or
            EnemyVisualState.PhaseOut)
            DrawPhaseFragments(anchor, progress, opacity, flip);
    }

    private void DrawRottenDeath(Vector2 anchor, bool flip, float progress)
    {
        if (progress < .74f)
        {
            float warning = progress / .74f;
            float swell = 1f + warning * .18f +
                MathF.Sin(warning * 32f) * .025f;
            DrawBox(anchor, -25f * swell, -63f, 50f * swell, 47f,
                Outline, flip);
            DrawBox(anchor, -22f * swell, -60f, 44f * swell, 42f,
                Color.Lerp(CorpseSkin, Rot, warning * .55f), flip);
            DrawBox(anchor, -13f, -77f + warning * 4f, 24f, 20f,
                CorpseDark, flip);
            DrawLineLocal(anchor, new Vector2(-20f, -43f),
                new Vector2(-33f, -9f), 9f, CorpseDark, flip);
            DrawLineLocal(anchor, new Vector2(20f, -43f),
                new Vector2(35f, -8f), 9f, CorpseSkin, flip);
            return;
        }

        float fall = (progress - .74f) / .26f;
        DrawBox(anchor, -31f + fall * 9f, -19f + fall * 13f,
            63f, MathF.Max(5f, 28f - fall * 19f),
            CorpseDark * (1f - fall * .75f), flip);
        DrawBox(anchor, -23f - fall * 14f, -28f + fall * 19f,
            22f, 12f, BurialCloth * (1f - fall * .6f), flip);
        DrawBox(anchor, 12f + fall * 16f, -15f + fall * 8f,
            13f, 9f, CorpseSkin * (1f - fall * .7f), flip);
    }

    private void DrawWraithDeath(Vector2 anchor, bool flip, float progress)
    {
        float alpha = 1f - progress;
        float lift = progress * 24f;
        DrawLineLocal(anchor, new Vector2(-18f, -2f),
            new Vector2(-8f, -48f - lift), 18f,
            WraithDark * alpha, flip);
        DrawLineLocal(anchor, new Vector2(17f, -1f),
            new Vector2(8f, -50f - lift), 16f,
            WraithRobe * alpha, flip);
        DrawBox(anchor, -10f, -75f - lift, 21f, 20f,
            new Color(38, 43, 57) * alpha, flip);
        for (int index = 0; index < 6; index++)
        {
            float side = index % 2 == 0 ? -1f : 1f;
            DrawBox(anchor,
                side * (7f + index * 3f) - 2f,
                -35f - index * 8f - lift * (1f + index * .08f),
                4f,
                4f,
                Soul * (alpha * .8f),
                flip);
        }
    }

    private void DrawRotVomit(
        Vector2 anchor,
        Vector2 head,
        bool flip,
        float progress)
    {
        for (int index = 0; index < 6; index++)
        {
            float phase = Math.Clamp(progress * 1.35f - index * .09f, 0f, 1f);
            if (phase <= 0f)
                continue;
            float x = head.X + 11f + phase * (24f + index * 11f);
            float y = head.Y + 7f + phase * phase * 23f +
                MathF.Sin(index * 2.1f) * 3f;
            DrawBox(anchor, x, y, index % 2 == 0 ? 5f : 3f,
                index % 2 == 0 ? 4f : 3f,
                index % 3 == 0 ? Rot : new Color(74, 78, 49, 205),
                flip);
        }
    }

    private void DrawSlamDust(Vector2 anchor, bool flip, float progress)
    {
        float spread = 20f + progress * 48f;
        DrawLineLocal(anchor, new Vector2(8f, -2f),
            new Vector2(spread, -2f), 4f,
            new Color(99, 89, 73, 180), flip);
        DrawBox(anchor, spread - 3f, -8f - progress * 8f, 6f, 6f,
            new Color(86, 78, 67, 155), flip);
    }

    private void DrawMist(
        Vector2 anchor,
        float opacity,
        float elapsed,
        bool flip)
    {
        for (int index = 0; index < 4; index++)
        {
            float drift = (elapsed * (9f + index * 2f) + index * 13f) % 35f;
            float side = index % 2 == 0 ? -1f : 1f;
            DrawBox(anchor, side * (7f + drift), -5f - index * 3f,
                9f + index * 2f, 3f,
                Soul * (opacity * .28f), flip);
        }
    }

    private void DrawCurseWave(
        Vector2 anchor,
        bool flip,
        float progress,
        float opacity)
    {
        float reach = 28f + progress * 94f;
        DrawLineLocal(anchor, new Vector2(13f, -5f),
            new Vector2(reach, -5f), 5f, Curse * opacity, flip);
        DrawLineLocal(anchor, new Vector2(reach - 17f, -5f),
            new Vector2(reach - 8f, -18f), 3f,
            Soul * (opacity * .75f), flip);
        DrawLineLocal(anchor, new Vector2(reach - 34f, -5f),
            new Vector2(reach - 27f, -13f), 3f,
            Curse * (opacity * .75f), flip);
    }

    private void DrawPhaseFragments(
        Vector2 anchor,
        float progress,
        float opacity,
        bool flip)
    {
        for (int index = 0; index < 5; index++)
        {
            float side = index % 2 == 0 ? -1f : 1f;
            DrawBox(anchor,
                side * (18f + progress * (7f + index * 2f)),
                -15f - index * 12f,
                3f,
                5f,
                Soul * (opacity * .55f),
                flip);
        }
    }

    private void DrawClaw(
        Vector2 anchor,
        Vector2 hand,
        Color color,
        bool flip)
    {
        DrawLineLocal(anchor, hand, hand + new Vector2(9f, -5f),
            2f, color, flip);
        DrawLineLocal(anchor, hand + new Vector2(0f, 2f),
            hand + new Vector2(10f, 1f), 2f, color, flip);
        DrawLineLocal(anchor, hand + new Vector2(-1f, 4f),
            hand + new Vector2(8f, 7f), 2f, color, flip);
    }

    private void DrawLimb(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color outline,
        Color fill,
        bool flip)
    {
        DrawLineLocal(anchor, from, to, thickness + 3f, outline, flip);
        DrawLineLocal(anchor, from, to, thickness, fill, flip);
    }

    private void DrawDiamond(Vector2 center, float radius, float thickness,
        Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius * .45f);
        Vector2 right = center + new Vector2(radius, 0f);
        Vector2 bottom = center + new Vector2(0f, radius * .45f);
        Vector2 left = center + new Vector2(-radius, 0f);
        DrawLine(top, right, thickness, color);
        DrawLine(right, bottom, thickness, color);
        DrawLine(bottom, left, thickness, color);
        DrawLine(left, top, thickness, color);
    }

    private void DrawWorldBox(Vector2 center, float width, float height,
        Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(center.X - width / 2f),
            (int)MathF.Round(center.Y - height / 2f),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawBox(Vector2 anchor, float x, float y, float width,
        float height, Color color, bool flip)
    {
        float worldX = flip ? anchor.X - x - width : anchor.X + x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(worldX),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLineLocal(Vector2 anchor, Vector2 from, Vector2 to,
        float thickness, Color color, bool flip)
    {
        if (flip)
        {
            from.X = -from.X;
            to.X = -to.X;
        }
        DrawLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(
            _pixel,
            from,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, .5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }
}
