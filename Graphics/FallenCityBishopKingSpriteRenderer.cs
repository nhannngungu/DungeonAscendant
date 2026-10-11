using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only ceremonial rigs for the Fallen City's support elite and human-scale
/// ruler. Both are mirrored around a stable collision-bottom ground anchor.
/// </summary>
public sealed class FallenCityBishopKingSpriteRenderer
{
    public static readonly Vector2 WarBishopVisualSize = new(104f, 102f);
    public static readonly Vector2 BrokenKingVisualSize = new(138f, 116f);

    private static readonly Color Outline = new(23, 21, 21);
    private static readonly Color Ivory = new(160, 148, 121);
    private static readonly Color DirtyIvory = new(119, 108, 91);
    private static readonly Color DarkRed = new(103, 42, 39);
    private static readonly Color DirtyGold = new(158, 119, 57);
    private static readonly Color Leather = new(70, 49, 38);
    private static readonly Color SteelDark = new(57, 59, 61);
    private static readonly Color Steel = new(101, 103, 101);
    private static readonly Color SteelLight = new(151, 146, 132);
    private static readonly Color RoyalBlue = new(49, 56, 79);
    private static readonly Color RoyalRed = new(102, 43, 44);
    private static readonly Color Skin = new(137, 111, 89);
    private static readonly Color Ash = new(87, 79, 72);
    private static readonly Color Ember = new(205, 81, 38);
    private static readonly Color EmberHot = new(239, 145, 59);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<FallenCityEnemy, VisualClock>
        _clocks = new();

    public FallenCityBishopKingSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(FallenCityEnemy enemy, GameTime gameTime)
    {
        VisualClock clock = _clocks.GetValue(enemy, static _ => new VisualClock());
        clock.Advance(gameTime);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState != FallenCityCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        float shadowWidth = enemy.Type == EnemyType.BrokenKing ? 68f : 48f;
        DrawBox(anchor, -shadowWidth / 2f, -5f, shadowWidth, 7f,
            new Color(7, 7, 8, 160), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.WarBishop)
                DrawBishopDeath(enemy, anchor, flip);
            else
                DrawKingDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.WarBishop)
            DrawBishop(enemy, anchor, flip, clock.Elapsed);
        else
            DrawKing(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawBishop(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float step = moving ? MathF.Sin(elapsed * 5.2f) * 3f : 0f;
        bool casting = enemy.CombatState is
            FallenCityCombatState.BishopWindup or
            FallenCityCombatState.BishopActive;
        float raise = casting ? MathHelper.SmoothStep(0f, 1f,
            enemy.CombatState == FallenCityCombatState.BishopWindup
                ? enemy.ActionProgress
                : 1f) : 0f;
        float recoil = enemy.IsHitFlashing ? -4f : enemy.IsStaggered ? -7f : 0f;
        Vector2 hip = new(recoil, -39f);
        Vector2 chest = new(recoil, -67f + MathF.Sin(elapsed * 2f));
        Vector2 head = chest + new Vector2(0f, -18f);

        DrawLine(anchor, new Vector2(-11f - step, -2f),
            hip + new Vector2(-8f, 2f), 10f, Leather, flip);
        DrawLine(anchor, new Vector2(12f + step, -2f),
            hip + new Vector2(8f, 2f), 10f, DirtyIvory, flip);
        DrawBox(anchor, hip.X - 25f, hip.Y - 4f, 50f, 43f,
            Outline, flip);
        DrawBox(anchor, hip.X - 22f, hip.Y - 2f, 44f, 40f,
            DarkRed, flip);
        DrawLine(anchor, hip + new Vector2(-2f, 1f),
            hip + new Vector2(-12f, 36f), 4f, DirtyGold, flip);

        DrawBox(anchor, chest.X - 20f, chest.Y - 8f, 40f, 34f,
            Outline, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y - 6f, 36f, 30f,
            enemy.IsHitFlashing ? Ivory : DirtyIvory, flip);
        DrawBox(anchor, chest.X - 13f, chest.Y + 5f, 26f, 18f,
            Ivory, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y + 7f, 14f, 12f,
            DarkRed, flip);
        DrawLine(anchor, chest + new Vector2(-17f, 20f),
            chest + new Vector2(17f, 20f), 4f, DirtyGold, flip);

        DrawBox(anchor, head.X - 10f, head.Y - 6f, 20f, 17f,
            Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 4f, 17f, 14f,
            Skin, flip);
        DrawBox(anchor, head.X + 4f, head.Y, 3f, 2f,
            new Color(50, 37, 32), flip);
        DrawBox(anchor, head.X - 14f, head.Y - 17f, 28f, 11f,
            Outline, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 15f, 22f, 8f,
            DarkRed, flip);
        DrawBox(anchor, head.X - 4f, head.Y - 24f, 8f, 10f,
            DirtyGold, flip);

        Vector2 rearShoulder = chest + new Vector2(-15f, 0f);
        Vector2 rearHand = chest + new Vector2(-20f, 17f - raise * 22f);
        DrawLine(anchor, rearShoulder, rearHand, 8f, DirtyIvory, flip);
        DrawBox(anchor, rearHand.X - 3f, rearHand.Y - 3f, 6f, 6f,
            Skin, flip);
        DrawBishopStaff(enemy, anchor, chest, raise, flip);

        for (int index = 0; index < 3; index++)
        {
            float sway = MathF.Sin(elapsed * 2.3f + index) * 3f;
            DrawLine(anchor,
                chest + new Vector2(-12f + index * 11f, 22f),
                hip + new Vector2(-15f + index * 13f + sway, 25f),
                3f,
                index == 1 ? DirtyGold : Ivory,
                flip);
        }

        if (casting && enemy.WarBishopAction is WarBishopActionKind.WarChant or
            WarBishopActionKind.FinalBenediction)
        {
            float radius = 30f + enemy.ActionProgress * 22f;
            DrawRitualCorners(anchor, radius,
                enemy.WarBishopAction == WarBishopActionKind.FinalBenediction
                    ? EmberHot
                    : DirtyGold,
                flip);
        }
        if (moving && (int)(elapsed * 7f) % 5 == 0)
            DrawBox(anchor, -23f, -4f, 4f, 2f,
                new Color(135, 120, 94, 95), flip);
    }

    private void DrawBishopStaff(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        float raise,
        bool flip)
    {
        float strike = enemy.WarBishopAction == WarBishopActionKind.StaffStrike
            ? enemy.CombatState == FallenCityCombatState.BishopWindup
                ? -enemy.ActionProgress * 1.15f
                : enemy.CombatState == FallenCityCombatState.BishopActive
                    ? -1.15f + enemy.ActionProgress * 2.05f
                    : .85f
            : 0f;
        float angle = .72f - raise * 1.45f + strike;
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 shoulder = chest + new Vector2(16f, 0f);
        Vector2 hand = shoulder + direction * 18f;
        DrawLine(anchor, shoulder, hand, 8f, DarkRed, flip);
        DrawLine(anchor, hand - direction * 18f,
            hand + direction * 58f, 7f, Outline, flip);
        DrawLine(anchor, hand - direction * 16f,
            hand + direction * 56f, 3f, Leather, flip);
        Vector2 head = hand + direction * 59f;
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, head - normal * 9f, head + normal * 9f,
            9f, Outline, flip);
        DrawLine(anchor, head - normal * 7f, head + normal * 7f,
            5f, DirtyGold, flip);
    }

    private void DrawRitualCorners(
        Vector2 anchor,
        float radius,
        Color color,
        bool flip)
    {
        for (int index = 0; index < 4; index++)
        {
            float x = index < 2 ? -radius : radius;
            float y = index % 2 == 0 ? -74f : -29f;
            DrawBox(anchor, x - 2f, y - 2f, 5f, 5f,
                new Color(color, 165), flip);
        }
    }

    private void DrawKing(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float step = moving ? MathF.Sin(elapsed * 6.4f) * 4f : 0f;
        float exhaustion = enemy.CombatPhase == 3
            ? 4f + MathF.Abs(MathF.Sin(elapsed * 3f)) * 2f
            : 0f;
        float recoil = enemy.IsKingGuardBroken ? -9f :
            enemy.IsKingGuardReacting ? -4f : enemy.IsHitFlashing ? -3f : 0f;
        Vector2 hip = new(recoil, -44f);
        Vector2 chest = new(recoil - exhaustion * .35f, -77f + exhaustion);
        Vector2 head = chest + new Vector2(0f, -20f);

        DrawLine(anchor, chest + new Vector2(-20f, 1f),
            hip + new Vector2(-30f, 37f), 22f, Outline, flip);
        DrawLine(anchor, chest + new Vector2(-19f, 2f),
            hip + new Vector2(-29f, 36f), 17f,
            enemy.CombatPhase >= 2 ? new Color(68, 39, 38) : RoyalBlue, flip);

        DrawLine(anchor, new Vector2(-15f - step, -2f),
            hip + new Vector2(-10f, 2f), 14f, SteelDark, flip);
        DrawLine(anchor, new Vector2(15f + step, -2f),
            hip + new Vector2(10f, 2f), 14f, Steel, flip);
        DrawBox(anchor, -27f - step, -9f, 23f, 9f, Leather, flip);
        DrawBox(anchor, 5f + step, -9f, 23f, 9f, Leather, flip);

        DrawBox(anchor, chest.X - 25f, chest.Y - 9f, 50f, 40f,
            Outline, flip);
        DrawBox(anchor, chest.X - 23f, chest.Y - 7f, 46f, 36f,
            enemy.IsHitFlashing ? SteelLight : Steel, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y + 20f, 38f, 35f,
            RoyalRed, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y + 29f, 16f, 12f,
            DirtyGold, flip);
        DrawLine(anchor, chest + new Vector2(-16f, 5f),
            chest + new Vector2(14f, 24f), 3f, SteelDark, flip);

        DrawBox(anchor, head.X - 15f, head.Y - 11f, 30f, 25f,
            Outline, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 9f, 26f, 21f,
            SteelDark, flip);
        DrawBox(anchor, head.X + 1f, head.Y - 4f, 11f, 13f,
            Skin, flip);
        DrawLine(anchor, head + new Vector2(2f, 1f),
            head + new Vector2(11f, 4f), 2f, new Color(86, 56, 48), flip);
        DrawBrokenCrown(anchor, head, flip);

        DrawLine(anchor, chest + new Vector2(-16f, 1f),
            chest + new Vector2(-24f, 20f), 10f, Steel, flip);
        DrawBox(anchor, chest.X - 29f, chest.Y + 16f, 10f, 9f,
            Skin, flip);
        DrawKingSword(enemy, anchor, chest, flip);

        if (enemy.CombatPhase >= 2)
            DrawKingEmbers(enemy, anchor, elapsed, flip);
        if (enemy.IsLastCommandWarning)
            DrawRitualCorners(anchor, 48f + enemy.ActionProgress * 18f,
                new Color(194, 122, 62), flip);
    }

    private void DrawBrokenCrown(Vector2 anchor, Vector2 head, bool flip)
    {
        DrawBox(anchor, head.X - 14f, head.Y - 17f, 27f, 8f,
            DirtyGold, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 24f, 6f, 8f,
            DirtyGold, flip);
        DrawBox(anchor, head.X - 1f, head.Y - 28f, 6f, 12f,
            DirtyGold, flip);
        DrawBox(anchor, head.X + 9f, head.Y - 21f, 5f, 5f,
            Ash, flip);
    }

    private void DrawKingSword(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip)
    {
        float angle = .76f;
        if (enemy.CombatState == FallenCityCombatState.KingWindup)
        {
            angle = enemy.BrokenKingAttack switch
            {
                BrokenKingAttackKind.KingsThrust =>
                    MathHelper.Lerp(.76f, .02f, enemy.ActionProgress),
                BrokenKingAttackKind.RoyalCharge =>
                    MathHelper.Lerp(.76f, .10f, enemy.ActionProgress),
                BrokenKingAttackKind.CollapseStrike =>
                    MathHelper.Lerp(.76f, -1.58f, enemy.ActionProgress),
                _ => MathHelper.Lerp(.76f, -1.32f, enemy.ActionProgress)
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.KingActive)
        {
            angle = enemy.BrokenKingAttack switch
            {
                BrokenKingAttackKind.KingsThrust => .02f,
                BrokenKingAttackKind.RoyalCharge => .08f,
                BrokenKingAttackKind.CollapseStrike =>
                    MathHelper.Lerp(-1.58f, 1.12f, enemy.ActionProgress),
                BrokenKingAttackKind.AshCleave =>
                    MathHelper.Lerp(-1.32f, .34f, enemy.ActionProgress),
                _ => MathHelper.Lerp(-1.32f, .48f, enemy.ActionProgress)
            };
        }
        else if (enemy.IsKingGuarding || enemy.IsKingCounterTell)
            angle = -.42f;
        else if (enemy.IsLastCommandWarning)
            angle = MathHelper.Lerp(.76f, -1.55f, enemy.ActionProgress);
        else if (enemy.IsLastCommandCharging)
            angle = .04f;
        else if (enemy.CombatState == FallenCityCombatState.KingLastCommandRecovery)
            angle = .62f;

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 shoulder = chest + new Vector2(20f, 0f);
        Vector2 hand = shoulder + direction * 23f;
        DrawLine(anchor, shoulder, hand, 11f, SteelDark, flip);
        DrawLine(anchor, chest + new Vector2(8f, 9f),
            hand - direction * 8f, 9f, Steel, flip);
        DrawLine(anchor, hand - direction * 12f,
            hand + direction * 61f, 10f, Outline, flip);
        DrawLine(anchor, hand - direction * 10f,
            hand + direction * 58f, 6f, SteelLight, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, hand - normal * 13f,
            hand + normal * 13f, 6f, DirtyGold, flip);
        if (enemy.CombatPhase >= 2)
            DrawLine(anchor, hand + direction * 4f,
                hand + direction * 54f, 2f, Ember, flip);
    }

    private void DrawKingEmbers(
        FallenCityEnemy enemy,
        Vector2 anchor,
        float elapsed,
        bool flip)
    {
        DrawLine(anchor, new Vector2(-12f, -83f),
            new Vector2(-3f, -57f), 3f, Ember, flip);
        DrawLine(anchor, new Vector2(11f, -70f),
            new Vector2(4f, -42f), 2f, EmberHot, flip);
        int count = enemy.CombatPhase == 3 ? 5 : 3;
        for (int index = 0; index < count; index++)
        {
            float drift = (elapsed * (13f + index) + index * 9f) % 34f;
            DrawBox(anchor,
                -21f + index * 10f + MathF.Sin(elapsed * 2f + index) * 3f,
                -24f - drift,
                index % 2 == 0 ? 3f : 2f,
                3f,
                new Color(220, 91, 39, 150),
                flip);
        }
    }

    private void DrawBishopDeath(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float kneel = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, enemy.DeathPresentationProgress * 1.7f));
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, enemy.DeathPresentationProgress - .45f) / .55f);
        Vector2 hip = new(fall * 12f, -38f + kneel * 25f);
        Vector2 chest = new(fall * 30f, -68f + kneel * 28f + fall * 24f);
        DrawLine(anchor, hip, chest, 37f, Outline, flip);
        DrawLine(anchor, hip, chest, 31f, DarkRed, flip);
        DrawBox(anchor, chest.X + 8f, chest.Y - 11f, 20f, 18f,
            Skin, flip);
        DrawLine(anchor, new Vector2(-12f, -2f), hip, 11f,
            DirtyIvory, flip);
        DrawLine(anchor, new Vector2(11f, -2f), hip, 11f,
            DarkRed, flip);
        DrawLine(anchor, new Vector2(-24f, -3f),
            new Vector2(46f + fall * 12f, -3f), 5f, Leather, flip);
    }

    private void DrawKingDeath(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float kneel = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, enemy.DeathPresentationProgress * 1.45f));
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, enemy.DeathPresentationProgress - .52f) / .48f);
        Vector2 hip = new(fall * 9f, -44f + kneel * 29f);
        Vector2 shoulder = new(fall * 34f, -78f + kneel * 31f + fall * 28f);
        DrawLine(anchor, hip, shoulder, 46f, Outline, flip);
        DrawLine(anchor, hip, shoulder, 39f, SteelDark, flip);
        DrawBox(anchor, shoulder.X + 14f, shoulder.Y - 12f,
            28f, 23f, SteelDark, flip);
        DrawBox(anchor, shoulder.X + 4f, shoulder.Y - 19f,
            25f, 7f, DirtyGold, flip);
        DrawLine(anchor, new Vector2(-20f, -2f), hip, 14f,
            RoyalRed, flip);
        DrawLine(anchor, new Vector2(20f, -2f), hip, 14f,
            Steel, flip);
        DrawLine(anchor, new Vector2(-32f, -3f),
            new Vector2(64f + fall * 15f, -3f), 9f, Outline, flip);
        DrawLine(anchor, new Vector2(-30f, -3f),
            new Vector2(62f + fall * 15f, -3f), 5f, SteelLight, flip);
        if (enemy.CombatPhase >= 2 && enemy.DeathPresentationProgress < .82f)
            DrawBox(anchor, shoulder.X - 5f, shoulder.Y + 5f, 4f, 4f,
                new Color(Ember, 125), flip);
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
            Math.Max(1, (int)MathF.Ceiling(width)),
            Math.Max(1, (int)MathF.Ceiling(height))), color);
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
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    private sealed class VisualClock
    {
        public float Elapsed { get; private set; }

        public void Advance(GameTime gameTime)
        {
            Elapsed += Math.Min(
                .05f,
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}
