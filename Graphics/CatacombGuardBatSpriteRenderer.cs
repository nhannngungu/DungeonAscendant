using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only armored and aerial silhouettes for the Undead Guard and Grave
/// Bat. Their poses read directly from the authored combat clocks.
/// </summary>
public sealed class CatacombGuardBatSpriteRenderer
{
    private static readonly Color Outline = new(21, 22, 24);
    private static readonly Color IronDark = new(52, 51, 49);
    private static readonly Color Iron = new(86, 82, 74);
    private static readonly Color IronLight = new(122, 111, 91);
    private static readonly Color Rust = new(116, 67, 44);
    private static readonly Color TabardDark = new(42, 38, 42);
    private static readonly Color Tabard = new(65, 55, 60);
    private static readonly Color DeadFlesh = new(101, 101, 80);
    private static readonly Color Bone = new(155, 149, 126);
    private static readonly Color AmberEye = new(151, 73, 38);
    private static readonly Color BatDark = new(47, 43, 53);
    private static readonly Color BatMembrane = new(82, 76, 87);
    private static readonly Color BatBody = new(112, 108, 108);
    private static readonly Color BatBone = new(164, 157, 143);
    private static readonly Color ColdEye = new(126, 101, 151);
    private static readonly Color DustPulse = new(111, 100, 124);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public CatacombGuardBatSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
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
        bool flip = enemy.Facing == EnemyFacingDirection.Left;

        if (enemy.Type == EnemyType.UndeadGuard)
        {
            Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
            DrawBox(anchor, -22f, -3f, 45f, 5f,
                new Color(7, 8, 10, 130), flip);
            if (!enemy.IsAlive)
                DrawGuardDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawGuard(enemy, anchor, flip, animation);
            return;
        }

        Vector2 batAnchor = enemy.Position;
        if (!enemy.IsAlive)
            DrawBatDeath(enemy, batAnchor, flip, enemy.DeathPresentationProgress);
        else
            DrawGraveBat(enemy, batAnchor, flip, animation);
    }

    private static EnemyAnimationController CreateController(Enemy enemy) =>
        new(enemy.Type == EnemyType.UndeadGuard
            ? EnemyVisualProfile.UndeadGuard
            : EnemyVisualProfile.GraveBat);

    private void DrawGuard(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float step = animation.State == EnemyVisualState.Walk
            ? MathF.Sin(animation.Elapsed * 6.2f) * 4f
            : 0f;
        float bob = animation.State == EnemyVisualState.Walk
            ? MathF.Abs(MathF.Sin(animation.Elapsed * 6.2f)) * 2f
            : MathF.Sin(animation.Elapsed * 2.7f) * .7f;
        float crouch = 0f;
        float lean = 0f;
        float shieldPush = 0f;
        float shieldDrop = 0f;
        float weaponAngle = .82f;

        switch (animation.State)
        {
            case EnemyVisualState.ShieldReady:
                crouch = 3f;
                shieldPush = 4f;
                weaponAngle = .48f;
                break;
            case EnemyVisualState.AttackWindup:
                crouch = 3f + progress * 4f;
                lean = -5f * progress;
                shieldPush = enemy.UndeadGuardAttack ==
                    UndeadGuardAttackKind.ShieldBash ? -5f * progress : 3f;
                weaponAngle = enemy.UndeadGuardAttack ==
                    UndeadGuardAttackKind.HeavyCleave
                        ? MathHelper.Lerp(.7f, -1.65f, progress)
                        : MathHelper.Lerp(.65f, -1.05f, progress);
                break;
            case EnemyVisualState.GuardedStrike:
                lean = 6f;
                shieldPush = 7f;
                weaponAngle = MathHelper.Lerp(-1.05f, .08f, progress);
                break;
            case EnemyVisualState.ShieldBash:
                lean = 9f;
                crouch = 4f;
                shieldPush = 11f + progress * 12f;
                weaponAngle = .5f;
                break;
            case EnemyVisualState.HeavyMeleeAttack:
                lean = 10f;
                crouch = 6f;
                shieldPush = -2f;
                weaponAngle = MathHelper.Lerp(-1.65f, .85f, progress);
                break;
            case EnemyVisualState.AttackRecovery:
                lean = 8f * (1f - progress);
                crouch = 5f * (1f - progress);
                shieldPush = progress * 4f;
                weaponAngle = MathHelper.Lerp(.25f, .72f, progress);
                break;
            case EnemyVisualState.BlockReaction:
                lean = -5f * (1f - progress);
                crouch = 5f;
                shieldPush = -4f * (1f - progress);
                break;
            case EnemyVisualState.GuardBreak:
                lean = -10f + progress * 5f;
                crouch = 8f + progress * 3f;
                shieldDrop = 19f;
                shieldPush = -10f;
                weaponAngle = 1.55f;
                break;
            case EnemyVisualState.Hurt:
                lean = -7f * (1f - progress);
                break;
            case EnemyVisualState.Stagger:
                lean = -12f + MathF.Sin(progress * 13f) * 4f;
                crouch = 7f;
                shieldDrop = 11f;
                break;
        }

        Color iron = enemy.IsHitFlashing ? IronLight : Iron;
        Vector2 rearFoot = new(-9f - step * .5f, 0f);
        Vector2 frontFoot = new(11f + step, 0f);
        Vector2 rearHip = new(-7f + lean * .2f, -27f + crouch + bob);
        Vector2 frontHip = new(8f + lean * .2f, -27f + crouch + bob);
        Vector2 chest = new(lean, -49f + crouch + bob);
        Vector2 head = chest + new Vector2(1f, -18f);

        DrawArmoredLimb(anchor, rearFoot, rearHip, 8f, iron, flip);
        DrawArmoredLimb(anchor, frontFoot, frontHip, 9f, iron, flip);
        DrawBox(anchor, rearFoot.X - 6f, -4f, 13f, 5f,
            IronDark, flip);
        DrawBox(anchor, frontFoot.X - 5f, -4f, 14f, 5f,
            IronDark, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y - 8f, 37f, 33f,
            Outline, flip);
        DrawBox(anchor, chest.X - 16f, chest.Y - 6f, 33f, 28f,
            iron, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y + 9f, 20f, 28f,
            TabardDark, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y + 10f, 14f, 24f,
            Tabard, flip);
        DrawBox(anchor, chest.X - 15f, chest.Y + 1f, 30f, 5f,
            Rust, flip);

        Vector2 rearShoulder = chest + new Vector2(-17f, -2f);
        Vector2 weaponHand = rearShoulder + new Vector2(-6f, 22f);
        Vector2 shieldShoulder = chest + new Vector2(17f, -1f);
        Vector2 shieldHand = shieldShoulder +
            new Vector2(7f + shieldPush, 19f + shieldDrop * .35f);
        DrawArmoredLimb(anchor, rearShoulder, weaponHand, 8f, iron, flip);
        DrawArmoredLimb(anchor, shieldShoulder, shieldHand, 9f, iron, flip);

        Vector2 weaponDirection = new(
            MathF.Cos(weaponAngle),
            MathF.Sin(weaponAngle));
        DrawGuardWeapon(anchor, weaponHand, weaponDirection, flip);
        DrawShield(anchor, shieldHand + new Vector2(4f, shieldDrop),
            iron, flip, enemy.GuardIntegrityRatio);
        DrawHelmet(anchor, head, iron, flip);

        DrawBox(anchor, rearHip.X - 4f, rearHip.Y - 4f, 7f, 7f,
            DeadFlesh, flip);
        DrawBox(anchor, frontHip.X - 3f, frontHip.Y - 3f, 6f, 6f,
            Bone, flip);

        if (animation.State is EnemyVisualState.BlockReaction or
            EnemyVisualState.GuardBreak)
            DrawRustSparks(anchor, shieldHand, progress, flip);
        if (animation.State == EnemyVisualState.ShieldBash)
            DrawShieldDust(anchor, shieldHand, progress, flip);
    }

    private void DrawShield(
        Vector2 anchor,
        Vector2 hand,
        Color iron,
        bool flip,
        float integrity)
    {
        float x = hand.X - 8f;
        float y = hand.Y - 20f;
        DrawBox(anchor, x - 2f, y - 2f, 24f, 42f, Outline, flip);
        DrawBox(anchor, x, y, 20f, 36f, IronDark, flip);
        DrawBox(anchor, x + 2f, y + 3f, 16f, 29f, iron, flip);
        DrawBox(anchor, x + 7f, y + 1f, 4f, 33f, Rust, flip);
        DrawBox(anchor, x + 3f, y + 10f, 14f, 4f, IronLight, flip);
        if (integrity < .65f)
        {
            DrawLineLocal(anchor, new Vector2(x + 3f, y + 4f),
                new Vector2(x + 10f, y + 15f), 2f, Outline, flip);
            DrawLineLocal(anchor, new Vector2(x + 10f, y + 15f),
                new Vector2(x + 6f, y + 25f), 2f, Outline, flip);
        }
        DrawBox(anchor, x + 3f, y + 34f, 5f, 4f, Outline, flip);
        DrawBox(anchor, x + 15f, y + 33f, 4f, 4f, Outline, flip);
    }

    private void DrawHelmet(
        Vector2 anchor,
        Vector2 head,
        Color iron,
        bool flip)
    {
        DrawBox(anchor, head.X - 12f, head.Y - 10f, 25f, 23f,
            Outline, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 21f, 19f,
            IronDark, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 10f, 25f, 7f,
            iron, flip);
        DrawBox(anchor, head.X + 7f, head.Y - 6f, 5f, 17f,
            iron, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 1f, 15f, 4f,
            Outline, flip);
        DrawBox(anchor, head.X + 2f, head.Y, 4f, 2f,
            AmberEye, flip);
        DrawBox(anchor, head.X - 7f, head.Y + 8f, 9f, 5f,
            DeadFlesh, flip);
    }

    private void DrawGuardWeapon(
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip)
    {
        Vector2 pommel = hand - direction * 6f;
        Vector2 guard = hand + direction * 3f;
        Vector2 tip = hand + direction * 38f;
        DrawLineLocal(anchor, pommel, tip, 5f, Outline, flip);
        DrawLineLocal(anchor, pommel, guard, 3f, Rust, flip);
        DrawLineLocal(anchor, guard, tip, 3f, IronLight, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, guard - normal * 7f, guard + normal * 7f,
            4f, IronDark, flip);
    }

    private void DrawGraveBat(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float flap = MathF.Sin(animation.Elapsed *
            (animation.State == EnemyVisualState.GraveCircle ? 10f : 7.5f));
        float leftWing = -13f - flap * 10f;
        float rightWing = 9f + flap * 8f;
        float lean = 0f;
        float bodyDrop = 0f;

        switch (animation.State)
        {
            case EnemyVisualState.AttackWindup:
                leftWing = MathHelper.Lerp(-18f, -4f, progress);
                rightWing = MathHelper.Lerp(14f, 3f, progress);
                bodyDrop = progress * 3f;
                break;
            case EnemyVisualState.GraveScreech:
                leftWing = -23f;
                rightWing = 19f;
                bodyDrop = 2f;
                break;
            case EnemyVisualState.GraveClawPass:
                leftWing = -4f;
                rightWing = 3f;
                lean = 8f;
                bodyDrop = 4f;
                break;
            case EnemyVisualState.GraveFeint:
                leftWing = -8f + MathF.Sin(progress * 15f) * 8f;
                rightWing = 5f - MathF.Sin(progress * 15f) * 6f;
                lean = 5f;
                break;
            case EnemyVisualState.GraveRetreat:
                leftWing = -18f - flap * 8f;
                rightWing = 13f + flap * 7f;
                bodyDrop = -3f;
                break;
            case EnemyVisualState.Hurt:
                lean = -5f * (1f - progress);
                leftWing = -5f;
                rightWing = 4f;
                break;
            case EnemyVisualState.Stagger:
                lean = MathF.Sin(progress * 15f) * 7f;
                bodyDrop = 6f;
                break;
        }

        Color body = enemy.IsHitFlashing ? BatBone : BatBody;
        Vector2 torso = new(lean, bodyDrop);
        DrawBatWing(anchor, torso + new Vector2(-7f, -2f),
            new Vector2(-31f, leftWing), new Vector2(-41f, 3f),
            BatMembrane, flip, torn: true);
        DrawBatWing(anchor, torso + new Vector2(7f, -1f),
            new Vector2(29f, rightWing), new Vector2(38f, 7f),
            new Color(74, 69, 81), flip, torn: false);

        DrawBox(anchor, torso.X - 8f, torso.Y - 10f, 17f, 24f,
            Outline, flip);
        DrawBox(anchor, torso.X - 6f, torso.Y - 8f, 13f, 20f,
            body, flip);
        for (int index = 0; index < 3; index++)
        {
            float y = torso.Y - 3f + index * 5f;
            DrawLineLocal(anchor, new Vector2(torso.X - 7f, y),
                new Vector2(torso.X + 7f, y), 2f, BatBone, flip);
        }

        Vector2 head = torso + new Vector2(4f, -13f);
        DrawBox(anchor, head.X - 7f, head.Y - 6f, 15f, 13f,
            Outline, flip);
        DrawBox(anchor, head.X - 5f, head.Y - 5f, 11f, 10f,
            BatDark, flip);
        DrawLineLocal(anchor, head + new Vector2(-4f, -4f),
            head + new Vector2(-8f, -17f), 4f, BatBody, flip);
        DrawLineLocal(anchor, head + new Vector2(4f, -4f),
            head + new Vector2(7f, -15f), 4f, BatBody, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 3f, 2f,
            ColdEye, flip);
        DrawLineLocal(anchor, torso + new Vector2(-4f, 10f),
            torso + new Vector2(-8f, 20f), 3f, BatBone, flip);
        DrawLineLocal(anchor, torso + new Vector2(4f, 10f),
            torso + new Vector2(10f, 19f), 3f, BatBone, flip);

        if (animation.State == EnemyVisualState.GraveScreech)
            DrawScreech(anchor, flip, progress);
        DrawBatDust(anchor, animation.Elapsed, flip);
    }

    private void DrawBatWing(
        Vector2 anchor,
        Vector2 root,
        Vector2 joint,
        Vector2 tip,
        Color membrane,
        bool flip,
        bool torn)
    {
        DrawLineLocal(anchor, root, joint, 5f, Outline, flip);
        DrawLineLocal(anchor, joint, tip, 4f, Outline, flip);
        DrawLineLocal(anchor, root, joint, 3f, BatBone, flip);
        DrawLineLocal(anchor, joint, tip, 2f, BatBone, flip);
        Vector2 lower = new(tip.X * .72f, 11f);
        DrawLineLocal(anchor, root, lower, 3f, membrane, flip);
        DrawLineLocal(anchor, lower, tip, 3f, membrane, flip);
        DrawLineLocal(anchor, joint, lower, 2f, membrane, flip);
        if (torn)
        {
            DrawLineLocal(anchor, lower, lower + new Vector2(7f, -5f),
                2f, Outline, flip);
        }
    }

    private void DrawScreech(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        for (int index = 0; index < 3; index++)
        {
            float local = Math.Clamp(progress * 1.45f - index * .18f, 0f, 1f);
            if (local <= 0f)
                continue;
            float radius = 13f + local * 84f;
            Color color = DustPulse * ((1f - local) * .7f);
            DrawWaveDiamond(anchor + new Vector2(8f, 0f), radius, color, flip);
        }
    }

    private void DrawBatDust(Vector2 anchor, float elapsed, bool flip)
    {
        for (int index = 0; index < 3; index++)
        {
            float drift = (elapsed * (8f + index) + index * 11f) % 28f;
            DrawBox(anchor, -18f - drift, 9f + index * 4f, 4f, 3f,
                new Color(103, 96, 91, 80), flip);
        }
    }

    private void DrawGuardDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathF.Min(1f, progress * 1.35f);
        float y = fall * 39f;
        float fade = 1f - MathF.Max(0f, progress - .78f) / .22f;
        DrawLineLocal(anchor, new Vector2(-10f, -2f),
            new Vector2(-8f + fall * 15f, -48f + y),
            16f, IronDark * fade, flip);
        DrawBox(anchor, -13f + fall * 16f, -70f + y, 25f, 24f,
            Iron * fade, flip);
        DrawBox(anchor, 7f + fall * 24f, -43f + y, 22f, 38f,
            IronDark * fade, flip);
        DrawLineLocal(anchor, new Vector2(-16f, -38f + y),
            new Vector2(-36f - fall * 9f, -7f + y),
            5f, IronLight * fade, flip);
        if (progress > .62f)
        {
            float dust = (progress - .62f) / .38f;
            DrawLineLocal(anchor, new Vector2(-36f, -2f),
                new Vector2(46f + dust * 18f, -2f), 4f,
                new Color(91, 79, 66) * (1f - dust), flip);
        }
    }

    private void DrawBatDeath(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float fail = MathF.Min(1f, progress / .28f);
        float tumble = MathF.Max(0f, (progress - .18f) / .82f);
        float landing = float.IsNaN(enemy.GraveBatDeathLandingY)
            ? anchor.Y + 90f
            : enemy.GraveBatDeathLandingY - 6f;
        float fallDistance = MathF.Max(18f, landing - anchor.Y);
        float y = tumble * tumble * fallDistance;
        float angle = tumble * 5.4f;
        float alpha = 1f - MathF.Max(0f, progress - .82f) / .18f;
        Vector2 body = anchor + new Vector2(0f, y);
        Vector2 left = Rotate(new Vector2(-29f, -8f + fail * 14f), angle);
        Vector2 right = Rotate(new Vector2(25f, 6f - fail * 8f), angle);
        DrawLine(body, body + left, 5f, BatMembrane * alpha);
        DrawLine(body, body + right, 4f, BatMembrane * alpha);
        DrawWorldBox(body, 15f, 20f, BatBody * alpha);
        DrawWorldBox(body + Rotate(new Vector2(4f, -13f), angle),
            11f, 10f, BatDark * alpha);
        if (tumble > .78f)
        {
            float impact = (tumble - .78f) / .22f;
            DrawLine(body + new Vector2(-25f - impact * 20f, 9f),
                body + new Vector2(28f + impact * 20f, 9f),
                3f,
                new Color(99, 91, 84) * ((1f - impact) * alpha));
        }
    }

    private void DrawRustSparks(
        Vector2 anchor,
        Vector2 shield,
        float progress,
        bool flip)
    {
        for (int index = 0; index < 4; index++)
        {
            float x = shield.X + 13f + progress * (8f + index * 4f);
            float y = shield.Y - 8f + index * 6f - progress * 9f;
            DrawBox(anchor, x, y, 3f, 2f, Rust * (1f - progress * .6f), flip);
        }
    }

    private void DrawShieldDust(
        Vector2 anchor,
        Vector2 shield,
        float progress,
        bool flip)
    {
        DrawLineLocal(anchor, shield + new Vector2(8f, 16f),
            shield + new Vector2(29f + progress * 18f, 19f), 4f,
            new Color(104, 91, 73, 140), flip);
    }

    private void DrawArmoredLimb(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color iron,
        bool flip)
    {
        DrawLineLocal(anchor, from, to, thickness + 3f, Outline, flip);
        DrawLineLocal(anchor, from, to, thickness, iron, flip);
    }

    private void DrawWaveDiamond(
        Vector2 anchor,
        float radius,
        Color color,
        bool flip)
    {
        Vector2 top = new(0f, -radius * .38f);
        Vector2 right = new(radius, 0f);
        Vector2 bottom = new(0f, radius * .38f);
        Vector2 left = new(-radius, 0f);
        DrawLineLocal(anchor, top, right, 2f, color, flip);
        DrawLineLocal(anchor, right, bottom, 2f, color, flip);
        DrawLineLocal(anchor, bottom, left, 2f, color, flip);
        DrawLineLocal(anchor, left, top, 2f, color, flip);
    }

    private static Vector2 Rotate(Vector2 value, float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            value.X * cosine - value.Y * sine,
            value.X * sine + value.Y * cosine);
    }

    private void DrawWorldBox(
        Vector2 center,
        float width,
        float height,
        Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(center.X - width / 2f),
            (int)MathF.Round(center.Y - height / 2f),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
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

    private void DrawLineLocal(
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
        DrawLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawLine(
        Vector2 from,
        Vector2 to,
        float thickness,
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
