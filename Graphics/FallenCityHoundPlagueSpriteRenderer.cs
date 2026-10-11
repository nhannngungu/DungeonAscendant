using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only rigs for the low, mobile war hound and the burdened plague
/// worker. Every part is authored facing right and mirrored around a stable
/// ground anchor.
/// </summary>
public sealed class FallenCityHoundPlagueSpriteRenderer
{
    public static readonly Vector2 HoundVisualSize = new(76f, 44f);
    public static readonly Vector2 CarrierVisualSize = new(88f, 82f);

    private static readonly Color Outline = new(24, 23, 22);
    private static readonly Color FurDark = new(48, 43, 40);
    private static readonly Color Fur = new(71, 61, 55);
    private static readonly Color FurLight = new(94, 79, 67);
    private static readonly Color Scar = new(108, 62, 56);
    private static readonly Color SkinPatch = new(120, 82, 70);
    private static readonly Color Leather = new(79, 53, 39);
    private static readonly Color Rust = new(117, 69, 43);
    private static readonly Color Eye = new(196, 104, 89);
    private static readonly Color ClothDark = new(55, 54, 46);
    private static readonly Color Cloth = new(83, 79, 62);
    private static readonly Color ClothLight = new(111, 102, 74);
    private static readonly Color SickSkin = new(132, 127, 87);
    private static readonly Color Bandage = new(139, 128, 98);
    private static readonly Color PlagueDark = new(65, 70, 48);
    private static readonly Color Plague = new(103, 108, 65);
    private static readonly Color Glass = new(119, 125, 91);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<FallenCityEnemy, VisualClock>
        _clocks = new();

    public FallenCityHoundPlagueSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(FallenCityEnemy enemy, GameTime gameTime)
    {
        VisualClock clock = _clocks.GetValue(
            enemy,
            static _ => new VisualClock());
        clock.Advance(gameTime);

        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState != FallenCityCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        float shadowWidth = enemy.Type == EnemyType.RabidHound ? 46f : 42f;
        DrawBox(anchor, -shadowWidth / 2f, -3f, shadowWidth, 5f,
            new Color(12, 11, 10, 135), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.RabidHound)
                DrawHoundDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawCarrierDeath(anchor, flip, enemy.DeathPresentationProgress);
            return;
        }

        if (enemy.Type == EnemyType.RabidHound)
            DrawHound(enemy, anchor, flip, clock.Elapsed);
        else
            DrawCarrier(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawHound(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 8f;
        float stride = moving ? MathF.Sin(elapsed * 18f) * 6f : 0f;
        float breathe = moving ? 0f : MathF.Sin(elapsed * 4.2f) * 1.2f;
        float crouch = enemy.CombatState is
            FallenCityCombatState.HoundThreaten or
            FallenCityCombatState.HoundWindup
                ? 5f * enemy.ActionProgress
                : 0f;
        float leap = enemy.CombatState == FallenCityCombatState.HoundActive
            ? MathF.Sin(enemy.ActionProgress * MathF.PI) * 8f
            : 0f;
        float retreat = enemy.CombatState == FallenCityCombatState.HoundReposition
            ? -3f
            : 0f;
        float hurt = enemy.IsHitFlashing ? -4f : 0f;
        float bodyY = -22f + breathe + crouch - leap;
        float bodyX = retreat + hurt;
        float headX = bodyX + 27f;
        float headY = bodyY - 4f;

        // Fast, lean legs remain close to the floor, unlike the tall Dire Wolf.
        DrawHoundLeg(anchor, bodyX - 18f, bodyY + 7f,
            -20f - stride, 0f, flip);
        DrawHoundLeg(anchor, bodyX - 7f, bodyY + 8f,
            -5f + stride, 0f, flip);
        DrawHoundLeg(anchor, bodyX + 9f, bodyY + 8f,
            8f - stride, 0f, flip);
        DrawHoundLeg(anchor, bodyX + 18f, bodyY + 7f,
            22f + stride, 0f, flip);

        DrawLine(anchor, new Vector2(bodyX - 22f, bodyY - 2f),
            new Vector2(bodyX + 17f, bodyY), 18f, Outline, flip);
        DrawLine(anchor, new Vector2(bodyX - 21f, bodyY - 2f),
            new Vector2(bodyX + 16f, bodyY), 14f,
            enemy.IsHitFlashing ? FurLight : Fur, flip);
        DrawBox(anchor, bodyX - 18f, bodyY - 8f, 9f, 4f,
            FurLight, flip);
        DrawBox(anchor, bodyX - 5f, bodyY + 3f, 10f, 5f,
            SkinPatch, flip);
        DrawLine(anchor, new Vector2(bodyX - 3f, bodyY - 7f),
            new Vector2(bodyX + 4f, bodyY + 5f), 2f, Scar, flip);

        // Broken military harness and loose rusted chain identify city origin.
        DrawLine(anchor, new Vector2(bodyX - 8f, bodyY - 9f),
            new Vector2(bodyX - 3f, bodyY + 9f), 4f, Leather, flip);
        DrawLine(anchor, new Vector2(bodyX + 12f, bodyY - 8f),
            new Vector2(bodyX + 15f, bodyY + 8f), 3f, Leather, flip);
        DrawBox(anchor, bodyX + 11f, bodyY + 5f, 5f, 4f, Rust, flip);
        for (int index = 0; index < 3; index++)
        {
            float chainX = bodyX + 13f - index * 4f;
            DrawBox(anchor, chainX, bodyY + 10f + index * 2f,
                3f, 3f, index % 2 == 0 ? Rust : Outline, flip);
        }

        DrawLine(anchor, new Vector2(bodyX - 22f, bodyY - 3f),
            new Vector2(bodyX - 34f, bodyY - 12f + stride * .3f),
            5f, FurDark, flip);
        DrawLine(anchor, new Vector2(bodyX + 15f, bodyY - 3f),
            new Vector2(headX - 5f, headY + 2f), 11f, FurDark, flip);
        DrawBox(anchor, headX - 7f, headY - 8f, 20f, 15f,
            Outline, flip);
        DrawBox(anchor, headX - 6f, headY - 7f, 18f, 13f,
            enemy.IsHitFlashing ? FurLight : FurDark, flip);
        DrawBox(anchor, headX + 8f, headY, 13f, 8f, Outline, flip);
        DrawBox(anchor, headX + 9f, headY + 1f, 11f, 6f,
            Fur, flip);
        DrawBox(anchor, headX + 18f, headY + 1f, 4f, 4f,
            new Color(35, 31, 29), flip);
        DrawBox(anchor, headX + 4f, headY - 4f, 3f, 3f, Eye, flip);
        DrawLine(anchor, new Vector2(headX - 3f, headY - 7f),
            new Vector2(headX - 8f, headY - 16f), 6f, FurDark, flip);
        DrawLine(anchor, new Vector2(headX + 5f, headY - 7f),
            new Vector2(headX + 7f, headY - 16f), 5f, FurDark, flip);

        if (enemy.CombatState == FallenCityCombatState.HoundActive)
        {
            DrawBox(anchor, headX + 9f, headY + 7f, 13f, 4f,
                new Color(78, 39, 37), flip);
            DrawBox(anchor, headX + 13f, headY + 8f, 2f, 4f,
                new Color(197, 184, 143), flip);
            DrawBox(anchor, headX + 18f, headY + 8f, 2f, 4f,
                new Color(197, 184, 143), flip);
        }
        else if (!moving && (int)(elapsed * 4f) % 4 == 0)
        {
            DrawBox(anchor, headX + 23f, headY + 5f, 3f, 2f,
                new Color(170, 178, 158, 115), flip);
        }

        if (moving && (int)(elapsed * 18f) % 5 == 0)
        {
            DrawBox(anchor, -20f, -4f, 3f, 2f,
                new Color(139, 120, 91, 110), flip);
            DrawBox(anchor, -27f, -3f, 2f, 2f,
                new Color(119, 105, 84, 80), flip);
        }
    }

    private void DrawHoundLeg(
        Vector2 anchor,
        float hipX,
        float hipY,
        float footX,
        float footY,
        bool flip)
    {
        Vector2 knee = new((hipX + footX) * .5f, hipY + 10f);
        DrawLine(anchor, new Vector2(hipX, hipY), knee, 6f,
            FurDark, flip);
        DrawLine(anchor, knee, new Vector2(footX, footY), 5f,
            Fur, flip);
        DrawLine(anchor, new Vector2(footX - 3f, footY),
            new Vector2(footX + 4f, footY), 3f, Outline, flip);
    }

    private void DrawCarrier(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float drag = moving ? MathF.Sin(elapsed * 6f) * 3f : 0f;
        float cough = enemy.CombatState is
            FallenCityCombatState.PlagueCoughWindup or
            FallenCityCombatState.PlagueCoughActive
                ? MathF.Sin(enemy.ActionProgress * MathF.PI) * 7f
                : MathF.Max(0f, MathF.Sin(elapsed * 2.7f)) * 1.5f;
        float throwReach = enemy.CombatState is
            FallenCityCombatState.PlagueFlaskWindup or
            FallenCityCombatState.PlagueFlaskRelease
                ? enemy.ActionProgress * 15f
                : 0f;
        float stagger = enemy.IsStaggered ? -6f : 0f;
        Vector2 hip = new(stagger, -31f);
        Vector2 chest = new(stagger + 5f + cough, -54f + cough * .25f);
        Vector2 head = chest + new Vector2(3f, -17f);

        DrawLine(anchor, new Vector2(-10f - drag, 0f),
            hip + new Vector2(-7f, 0f), 9f, ClothDark, flip);
        DrawLine(anchor, new Vector2(11f + drag, 0f),
            hip + new Vector2(7f, 0f), 9f, Cloth, flip);
        DrawLine(anchor, new Vector2(-16f - drag, 0f),
            new Vector2(-5f - drag, 0f), 6f, Outline, flip);
        DrawLine(anchor, new Vector2(6f + drag, 0f),
            new Vector2(18f + drag, 0f), 6f, Outline, flip);

        // Layered, burdened civilian clothing preserves a human silhouette.
        DrawBox(anchor, chest.X - 19f, chest.Y - 8f, 37f, 34f,
            Outline, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 6f, 33f, 30f,
            enemy.IsHitFlashing ? ClothLight : ClothDark, flip);
        DrawBox(anchor, chest.X - 13f, chest.Y + 5f, 28f, 20f,
            Cloth, flip);
        DrawLine(anchor, chest + new Vector2(-12f, -4f),
            chest + new Vector2(13f, 19f), 4f, Bandage, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y + 19f, 13f, 14f,
            ClothDark, flip);
        DrawBox(anchor, chest.X + 3f, chest.Y + 21f, 14f, 13f,
            Cloth, flip);

        // Medicine bags, broken bottles, and a stained plague satchel.
        DrawLine(anchor, chest + new Vector2(-13f, -5f),
            hip + new Vector2(19f, 8f), 3f, Leather, flip);
        DrawBox(anchor, hip.X + 12f, hip.Y - 3f, 17f, 20f,
            Outline, flip);
        DrawBox(anchor, hip.X + 14f, hip.Y - 1f, 13f, 16f,
            Leather, flip);
        DrawBox(anchor, hip.X + 17f, hip.Y + 3f, 7f, 7f,
            PlagueDark, flip);
        DrawBox(anchor, hip.X - 25f, hip.Y - 5f, 10f, 14f,
            Leather, flip);
        DrawBox(anchor, hip.X - 23f, hip.Y - 12f, 6f, 9f,
            Glass, flip);
        DrawBox(anchor, hip.X - 22f, hip.Y - 15f, 4f, 4f,
            Outline, flip);

        Vector2 rearShoulder = chest + new Vector2(-14f, -2f);
        Vector2 rearHand = chest + new Vector2(-17f, 18f - cough);
        Vector2 frontShoulder = chest + new Vector2(14f, -1f);
        Vector2 frontHand = chest + new Vector2(16f + throwReach,
            16f - throwReach * .8f);
        DrawLine(anchor, rearShoulder, rearHand, 8f, Cloth, flip);
        DrawLine(anchor, frontShoulder, frontHand, 8f, ClothLight, flip);
        DrawBox(anchor, rearHand.X - 4f, rearHand.Y - 3f, 7f, 7f,
            SickSkin, flip);
        DrawBox(anchor, frontHand.X - 3f, frontHand.Y - 3f, 7f, 7f,
            SickSkin, flip);

        DrawBox(anchor, head.X - 12f, head.Y - 11f, 24f, 21f,
            Outline, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 9f, 20f, 18f,
            ClothDark, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 5f, 15f, 13f,
            SickSkin, flip);
        DrawLine(anchor, head + new Vector2(-8f, 0f),
            head + new Vector2(9f, 3f), 6f, Bandage, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 3f, 3f, 2f,
            new Color(61, 58, 43), flip);

        bool holdingFlask = enemy.PlagueCarrierAttack ==
            PlagueCarrierAttackKind.PlagueFlask &&
            enemy.CombatState != FallenCityCombatState.PlagueFlaskRecovery;
        if (holdingFlask)
        {
            DrawBox(anchor, frontHand.X + 3f, frontHand.Y - 8f,
                8f, 11f, Outline, flip);
            DrawBox(anchor, frontHand.X + 4f, frontHand.Y - 7f,
                6f, 9f, Glass, flip);
            DrawBox(anchor, frontHand.X + 5f, frontHand.Y - 10f,
                4f, 4f, Leather, flip);
            DrawBox(anchor, frontHand.X + 5f, frontHand.Y - 2f,
                5f, 3f, Plague, flip);
        }

        if (enemy.CombatState == FallenCityCombatState.PlagueCoughActive)
        {
            for (int index = 0; index < 4; index++)
            {
                DrawBox(anchor, head.X + 14f + index * 7f,
                    head.Y + index * 2f, 4f, 3f,
                    new Color(111, 113, 74, 125 - index * 20), flip);
            }
        }
        else if (!moving && (int)(elapsed * 3f) % 5 == 0)
        {
            DrawBox(anchor, head.X + 14f, head.Y + 2f, 3f, 2f,
                new Color(135, 137, 104, 85), flip);
        }

        if (moving && (int)(elapsed * 8f) % 6 == 0)
            DrawBox(anchor, -18f, -4f, 4f, 2f,
                new Color(134, 117, 87, 100), flip);
    }

    private void DrawHoundDeath(Vector2 anchor, bool flip, float progress)
    {
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        float y = -17f + settle * 14f;
        DrawLine(anchor, new Vector2(-29f, y),
            new Vector2(20f + settle * 6f, y + 2f), 16f,
            Outline, flip);
        DrawLine(anchor, new Vector2(-27f, y),
            new Vector2(19f + settle * 6f, y + 2f), 12f,
            FurDark, flip);
        DrawBox(anchor, 17f + settle * 6f, y - 4f, 22f, 12f,
            Fur, flip);
        DrawLine(anchor, new Vector2(-16f, y + 5f),
            new Vector2(-25f, -1f), 5f, FurDark, flip);
    }

    private void DrawCarrierDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.35f));
        Vector2 shoulder = new(-3f + fall * 21f, -58f + fall * 48f);
        Vector2 hip = new(-7f + fall * 5f, -30f + fall * 26f);
        DrawLine(anchor, hip, shoulder, 29f, Outline, flip);
        DrawLine(anchor, hip, shoulder, 24f, ClothDark, flip);
        DrawBox(anchor, shoulder.X + 9f, shoulder.Y - 10f,
            20f, 18f, Bandage, flip);
        DrawLine(anchor, new Vector2(-13f, -2f), hip, 9f,
            Cloth, flip);
        DrawLine(anchor, new Vector2(13f, -1f), hip, 9f,
            ClothDark, flip);
        DrawBox(anchor, hip.X + 13f, hip.Y - 2f, 17f, 18f,
            Leather, flip);
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
