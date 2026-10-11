using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only rigs for the Fallen City's defensive and fire-corrupted elites.
/// Every pose is built from the collision-box bottom so movement, attack arcs,
/// and death presentations retain one stable ground anchor.
/// </summary>
public sealed class FallenCityRoyalAshSpriteRenderer
{
    public static readonly Vector2 RoyalGuardVisualSize = new(116f, 94f);
    public static readonly Vector2 AshKnightVisualSize = new(122f, 98f);

    private static readonly Color Outline = new(22, 22, 23);
    private static readonly Color SteelDark = new(63, 66, 68);
    private static readonly Color Steel = new(104, 107, 106);
    private static readonly Color SteelLight = new(151, 149, 137);
    private static readonly Color RoyalCloth = new(93, 42, 38);
    private static readonly Color RoyalFaded = new(129, 68, 53);
    private static readonly Color OldGold = new(154, 119, 59);
    private static readonly Color Leather = new(67, 49, 39);
    private static readonly Color AshPlate = new(47, 45, 45);
    private static readonly Color AshLight = new(77, 73, 69);
    private static readonly Color CharredCloth = new(48, 37, 35);
    private static readonly Color Ember = new(207, 91, 43);
    private static readonly Color EmberHot = new(238, 151, 65);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<FallenCityEnemy, VisualClock>
        _clocks = new();

    public FallenCityRoyalAshSpriteRenderer(
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
        float width = enemy.Type == EnemyType.RoyalGuard ? 55f : 51f;
        DrawBox(anchor, -width / 2f, -5f, width, 7f,
            new Color(8, 8, 9, 155), flip);

        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.RoyalGuard)
            DrawRoyalGuard(enemy, anchor, flip, clock.Elapsed);
        else
            DrawAshKnight(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawRoyalGuard(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float step = moving ? MathF.Sin(elapsed * 6.2f) * 3.5f : 0f;
        float breathe = MathF.Sin(elapsed * 1.9f) * .8f;
        float recoil = enemy.IsRoyalBlockReacting ? -4f :
            enemy.IsRoyalGuardBroken ? -8f : enemy.IsHitFlashing ? -3f : 0f;
        Vector2 hip = new(recoil, -39f);
        Vector2 chest = new(recoil, -66f + breathe);
        Vector2 head = chest + new Vector2(0f, -19f);

        DrawLine(anchor, new Vector2(-13f - step, -2f),
            hip + new Vector2(-9f, 2f), 12f, SteelDark, flip);
        DrawLine(anchor, new Vector2(13f + step, -2f),
            hip + new Vector2(9f, 2f), 12f, Steel, flip);
        DrawBox(anchor, -23f - step, -8f, 21f, 8f, Leather, flip);
        DrawBox(anchor, 3f + step, -8f, 21f, 8f, Leather, flip);

        DrawBox(anchor, chest.X - 22f, chest.Y - 8f, 44f, 36f,
            Outline, flip);
        DrawBox(anchor, chest.X - 20f, chest.Y - 6f, 40f, 32f,
            enemy.IsHitFlashing ? SteelLight : Steel, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y + 18f, 34f, 34f,
            RoyalCloth, flip);
        DrawLine(anchor, chest + new Vector2(0f, 19f),
            chest + new Vector2(0f, 46f), 3f, OldGold, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y + 28f, 16f, 10f,
            RoyalFaded, flip);
        DrawBox(anchor, chest.X - 4f, chest.Y + 29f, 8f, 7f,
            OldGold, flip);

        DrawBox(anchor, head.X - 15f, head.Y - 11f, 30f, 24f,
            Outline, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 9f, 26f, 21f,
            SteelDark, flip);
        DrawBox(anchor, head.X - 13f, head.Y + 1f, 26f, 5f,
            SteelLight, flip);
        DrawBox(anchor, head.X + 4f, head.Y + 2f, 7f, 3f,
            new Color(25, 25, 24), flip);
        DrawBox(anchor, head.X - 3f, head.Y - 17f, 6f, 8f,
            RoyalFaded, flip);

        DrawRoyalShield(enemy, anchor, chest, flip);
        DrawRoyalWeapon(enemy, anchor, chest, flip);

        if (moving && (int)(elapsed * 9f) % 5 == 0)
            DrawBox(anchor, -27f, -4f, 5f, 3f,
                new Color(131, 116, 91, 105), flip);
    }

    private void DrawRoyalShield(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip)
    {
        float push = enemy.RoyalGuardAttack == RoyalGuardAttackKind.ShieldBash &&
            enemy.CombatState == FallenCityCombatState.RoyalActive
                ? 17f * enemy.ActionProgress
                : enemy.IsRoyalGuarding ? 6f : 0f;
        float drop = enemy.IsRoyalGuardBroken ? 18f : 0f;
        float x = chest.X + 24f + push;
        float y = chest.Y + 5f + drop;
        DrawLine(anchor, chest + new Vector2(10f, 1f),
            new Vector2(x, y + 13f), 9f, SteelDark, flip);
        DrawBox(anchor, x - 3f, y - 12f, 27f, 48f, Outline, flip);
        DrawBox(anchor, x - 1f, y - 10f, 23f, 44f, SteelDark, flip);
        DrawBox(anchor, x + 2f, y - 7f, 17f, 35f, RoyalCloth, flip);
        DrawBox(anchor, x + 7f, y + 2f, 8f, 12f, OldGold, flip);
        DrawLine(anchor, new Vector2(x + 2f, y + 28f),
            new Vector2(x + 19f, y - 7f), 2f, SteelLight, flip);
    }

    private void DrawRoyalWeapon(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip)
    {
        float angle = .92f;
        if (enemy.CombatState == FallenCityCombatState.RoyalWindup)
        {
            angle = enemy.RoyalGuardAttack switch
            {
                RoyalGuardAttackKind.RoyalThrust =>
                    MathHelper.Lerp(.92f, .05f, enemy.ActionProgress),
                RoyalGuardAttackKind.GuardedSlash =>
                    MathHelper.Lerp(.92f, -1.48f, enemy.ActionProgress),
                _ => .76f
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.RoyalActive)
        {
            angle = enemy.RoyalGuardAttack switch
            {
                RoyalGuardAttackKind.RoyalThrust => .02f,
                RoyalGuardAttackKind.GuardedSlash =>
                    MathHelper.Lerp(-1.48f, .48f, enemy.ActionProgress),
                _ => .78f
            };
        }

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 hand = chest + new Vector2(-15f, 8f);
        float thrust = enemy.RoyalGuardAttack == RoyalGuardAttackKind.RoyalThrust &&
            enemy.CombatState == FallenCityCombatState.RoyalActive
                ? 21f * enemy.ActionProgress
                : 0f;
        hand += direction * thrust;
        DrawLine(anchor, chest + new Vector2(-10f, -1f), hand,
            9f, Steel, flip);
        DrawLine(anchor, hand - direction * 12f,
            hand + direction * 53f, 6f, Outline, flip);
        DrawLine(anchor, hand - direction * 10f,
            hand + direction * 51f, 3f, Leather, flip);
        Vector2 tip = hand + direction * 61f;
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, hand + direction * 49f - normal * 6f,
            tip, 8f, SteelLight, flip);
        DrawLine(anchor, hand + direction * 49f + normal * 6f,
            tip, 8f, SteelLight, flip);
    }

    private void DrawAshKnight(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float step = moving ? MathF.Sin(elapsed * 8f) * 4f : 0f;
        float pulse = .5f + .5f * MathF.Sin(elapsed * 7f);
        float surgeLean = enemy.IsEmberSurging ? 3f : 0f;
        float stagger = enemy.IsStaggered ? -7f : enemy.IsHitFlashing ? -3f : 0f;
        Vector2 hip = new(stagger + surgeLean, -39f);
        Vector2 chest = new(stagger + surgeLean, -68f);
        Vector2 head = chest + new Vector2(0f, -18f);

        DrawLine(anchor, new Vector2(-12f - step, -2f),
            hip + new Vector2(-8f, 2f), 12f, AshPlate, flip);
        DrawLine(anchor, new Vector2(13f + step, -2f),
            hip + new Vector2(8f, 2f), 12f, AshLight, flip);
        DrawBox(anchor, -22f - step, -8f, 20f, 8f, Outline, flip);
        DrawBox(anchor, 3f + step, -8f, 21f, 8f, Outline, flip);

        DrawLine(anchor, chest + new Vector2(-17f, 2f),
            hip + new Vector2(-25f, 29f), 18f, Outline, flip);
        DrawLine(anchor, chest + new Vector2(-16f, 2f),
            hip + new Vector2(-24f, 28f), 13f, CharredCloth, flip);
        DrawBox(anchor, chest.X - 23f, chest.Y - 8f, 46f, 38f,
            Outline, flip);
        DrawBox(anchor, chest.X - 21f, chest.Y - 6f, 42f, 34f,
            enemy.IsHitFlashing ? new Color(113, 92, 79) : AshPlate, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y + 19f, 36f, 31f,
            AshLight, flip);

        DrawLine(anchor, chest + new Vector2(-12f, -2f),
            chest + new Vector2(-2f, 27f), 3f, Ember, flip);
        DrawLine(anchor, chest + new Vector2(8f, -4f),
            chest + new Vector2(2f, 16f), 2f, EmberHot, flip);
        DrawLine(anchor, hip + new Vector2(10f, -1f),
            hip + new Vector2(3f, 22f), 3f, Ember, flip);

        DrawBox(anchor, head.X - 15f, head.Y - 11f, 30f, 24f,
            Outline, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 9f, 26f, 21f,
            AshPlate, flip);
        DrawBox(anchor, head.X - 13f, head.Y + 1f, 26f, 5f,
            new Color(29, 27, 27), flip);
        DrawBox(anchor, head.X + 5f, head.Y + 2f, 6f, 2f,
            EmberHot, flip);

        DrawAshWeapon(enemy, anchor, chest, flip);

        int emberCount = enemy.IsEmberSurging ? 5 : 2;
        for (int index = 0; index < emberCount; index++)
        {
            float drift = (elapsed * (17f + index * 2f) + index * 13f) % 31f;
            float x = -19f + index * 10f + MathF.Sin(elapsed * 3f + index) * 3f;
            DrawBox(anchor, x, -20f - drift, index % 2 == 0 ? 3f : 2f,
                3f, new Color(
                    (byte)225,
                    (byte)103,
                    (byte)44,
                    (byte)(115 + pulse * 90f)), flip);
        }
    }

    private void DrawAshWeapon(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip)
    {
        float angle = .78f;
        if (enemy.CombatState == FallenCityCombatState.AshWindup)
        {
            angle = enemy.AshKnightAttack switch
            {
                AshKnightAttackKind.AshSweep =>
                    MathHelper.Lerp(.62f, -2.25f, enemy.ActionProgress),
                AshKnightAttackKind.CinderStrike =>
                    MathHelper.Lerp(.72f, -1.55f, enemy.ActionProgress),
                _ => MathHelper.Lerp(.78f, -1.18f, enemy.ActionProgress)
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.AshActive)
        {
            angle = enemy.AshKnightAttack switch
            {
                AshKnightAttackKind.AshSweep =>
                    MathHelper.Lerp(-2.25f, .10f, enemy.ActionProgress),
                AshKnightAttackKind.CinderStrike =>
                    MathHelper.Lerp(-1.55f, 1.10f, enemy.ActionProgress),
                _ => MathHelper.Lerp(-1.18f, .48f, enemy.ActionProgress)
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.AshSurgeWindup)
            angle = MathHelper.Lerp(.78f, -1.48f, enemy.ActionProgress);

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 shoulder = chest + new Vector2(16f, 0f);
        Vector2 hand = shoulder + direction * 21f;
        DrawLine(anchor, chest + new Vector2(-14f, 0f),
            hand - direction * 10f, 10f, AshLight, flip);
        DrawLine(anchor, shoulder, hand, 10f, AshPlate, flip);
        DrawLine(anchor, hand - direction * 10f,
            hand + direction * 49f, 9f, Outline, flip);
        DrawLine(anchor, hand - direction * 8f,
            hand + direction * 47f, 5f, AshLight, flip);
        DrawLine(anchor, hand + direction * 3f,
            hand + direction * 43f, 2f, Ember, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, hand - normal * 10f,
            hand + normal * 10f, 5f, Outline, flip);
    }

    private void DrawDeath(FallenCityEnemy enemy, Vector2 anchor, bool flip)
    {
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, enemy.DeathPresentationProgress * 1.15f));
        Color plate = enemy.Type == EnemyType.RoyalGuard ? Steel : AshPlate;
        Color cloth = enemy.Type == EnemyType.RoyalGuard
            ? RoyalCloth
            : CharredCloth;
        Vector2 hip = new(-5f + fall * 11f, -39f + fall * 33f);
        Vector2 shoulder = new(2f + fall * 31f, -69f + fall * 58f);
        DrawLine(anchor, hip, shoulder, 40f, Outline, flip);
        DrawLine(anchor, hip, shoulder, 34f, plate, flip);
        DrawBox(anchor, shoulder.X + 12f, shoulder.Y - 12f,
            29f, 23f, plate, flip);
        DrawLine(anchor, new Vector2(-18f, -2f), hip, 12f, cloth, flip);
        DrawLine(anchor, new Vector2(19f, -2f), hip, 12f, plate, flip);
        if (enemy.Type == EnemyType.RoyalGuard)
            DrawBox(anchor, 35f + fall * 13f, -18f, 25f, 43f,
                SteelDark, flip);
        else
            DrawLine(anchor, new Vector2(-26f, -3f),
                new Vector2(54f + fall * 10f, -2f), 7f, AshLight, flip);
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
