using System;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Procedural silhouettes for the Map 5 fortress roster.</summary>
public sealed class DemonFortressEnemySpriteRenderer
{
    private static readonly Color Outline = new(18, 17, 20);
    private static readonly Color Iron = new(93, 91, 94);
    private static readonly Color Steel = new(132, 132, 136);
    private static readonly Color Crimson = new(116, 33, 38);
    private static readonly Color Bronze = new(142, 91, 49);
    private static readonly Color Ember = new(224, 83, 35);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public DemonFortressEnemySpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonFortressEnemy enemy, GameTime gameTime)
    {
        Rectangle b = enemy.Bounds;
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        int bob = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? (int)MathF.Round(MathF.Sin(time * 8f) * 2f)
            : 0;
        bool left = enemy.Facing == EnemyFacingDirection.Left;
        Color plate = enemy.IsHitFlashing ? new Color(238, 224, 207) :
            enemy.Type switch
            {
                EnemyType.HornGuard => Bronze,
                EnemyType.DemonShieldbearer => new Color(71, 75, 82),
                EnemyType.InfernalPikeman => new Color(102, 52, 48),
                EnemyType.WarHound => new Color(77, 43, 42),
                EnemyType.InfernalSmith => new Color(81, 69, 65),
                EnemyType.DemonOfficer => new Color(125, 38, 43),
                EnemyType.BallistaGunner => new Color(86, 61, 49),
                EnemyType.BlackGuardExecutioner => new Color(42, 42, 48),
                _ => Iron
            };

        if (enemy.Type == EnemyType.DemonLegionnaireFortress)
        {
            DrawLegionnaire(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.FortressArcher)
        {
            DrawFortressArcher(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.HornGuard)
        {
            DrawHornGuard(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.DemonShieldbearer)
        {
            DrawDemonShieldbearer(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.InfernalPikeman)
        {
            DrawInfernalPikeman(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.WarHound)
        {
            DrawWarHound(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.InfernalSmith)
        {
            DrawInfernalSmith(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.DemonOfficer)
        {
            DrawDemonOfficer(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.BallistaGunner)
        {
            DrawBallistaGunner(enemy, time);
            return;
        }

        if (enemy.Type == EnemyType.BlackGuardExecutioner)
        {
            DrawBlackGuardExecutioner(enemy, time);
            return;
        }

        if (!enemy.IsAlive)
        {
            byte alpha = (byte)MathHelper.Lerp(210f, 0f, enemy.DeathPresentationProgress);
            _spriteBatch.Draw(_pixel,
                new Rectangle(b.Left, b.Bottom - 17, b.Width, 14),
                new Color(plate.R, plate.G, plate.B, alpha));
            return;
        }

        int center = b.Center.X;
        int foot = b.Bottom;
        int bodyW = Math.Max(22, b.Width * 3 / 5);
        int head = Math.Max(16, b.Width / 3);
        int torsoTop = b.Top + head;
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - bodyW / 2 - 2, torsoTop - 2, bodyW + 4,
                foot - torsoTop - 18), Outline);
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - bodyW / 2, torsoTop, bodyW,
                foot - torsoTop - 22), plate);
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - head / 2 - 2, b.Top + bob, head + 4, head + 5), Outline);
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - head / 2, b.Top + 2 + bob, head, head), Steel);
        _spriteBatch.Draw(_pixel,
            new Rectangle(center - bodyW / 2, foot - 22, 10, 22), Outline);
        _spriteBatch.Draw(_pixel,
            new Rectangle(center + bodyW / 2 - 10, foot - 22, 10, 22), Outline);

        switch (enemy.Type)
        {
            case EnemyType.FortressArcher:
                DrawBow(center + (left ? -bodyW / 2 : bodyW / 2), torsoTop + 15, left);
                break;
            default:
                DrawWeapon(center, torsoTop + 22, left, 62, 6, Steel);
                break;
        }
    }

    private void DrawLegionnaire(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool flip = enemy.CombatState == DemonFortressCombatState.Ready ||
            enemy.CombatState == DemonFortressCombatState.LegionGuard
                ? enemy.Facing == EnemyFacingDirection.Left
                : enemy.AttackFacesLeft;
        Color plate = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(67, 69, 75);
        if (!enemy.IsAlive)
        {
            DrawHumanoidDeath(anchor, flip, plate, enemy.DeathPresentationProgress,
                shield: true, bow: false);
            return;
        }

        float walk = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(time * 7.2f) * 7f
            : MathF.Sin(time * 1.8f) * 1.2f;
        float brace = enemy.IsLegionGuarding ? 5f : 0f;
        float reaction = enemy.IsLegionBlockReacting ? -4f : 0f;
        Vector2 hip = new(-brace + reaction, -36f);
        Vector2 chest = hip + new Vector2(1f, -27f);
        Vector2 head = chest + new Vector2(0f, -18f);

        DrawLimb(anchor, new Vector2(-12f - walk, -1f),
            hip + new Vector2(-8f, 2f), 9f, plate, flip);
        DrawLimb(anchor, new Vector2(13f + walk, -1f),
            hip + new Vector2(8f, 2f), 9f, new Color(79, 48, 48), flip);
        DrawBoot(anchor, new Vector2(-13f - walk, -3f), flip);
        DrawBoot(anchor, new Vector2(13f + walk, -3f), flip);

        DrawBox(anchor, hip.X - 14f, hip.Y - 7f, 29f, 22f,
            new Color(45, 43, 48), flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 13f, 35f, 29f, Outline, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 11f, 29f, 24f, plate, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 7f, 22f, 5f, Crimson, flip);
        DrawBox(anchor, chest.X - 24f, chest.Y - 10f, 11f, 10f, Steel, flip);
        DrawBox(anchor, chest.X + 13f, chest.Y - 10f, 11f, 10f, Steel, flip);

        DrawBox(anchor, head.X - 11f, head.Y - 9f, 22f, 18f, Outline, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 7f, 18f, 14f,
            new Color(72, 73, 79), flip);
        DrawBox(anchor, head.X - 12f, head.Y - 10f, 24f, 5f, Steel, flip);
        DrawLine(World(anchor, head + new Vector2(-7f, -7f), flip),
            World(anchor, head + new Vector2(-12f, -14f), flip), Bronze, 3);
        DrawLine(World(anchor, head + new Vector2(7f, -7f), flip),
            World(anchor, head + new Vector2(12f, -14f), flip), Bronze, 3);
        DrawBox(anchor, head.X + 3f, head.Y - 1f, 4f, 3f, Ember, flip);

        float shieldForward = enemy.IsLegionGuarding ? 39f : 32f;
        float shieldY = enemy.IsLegionGuarding ? -51f : -46f;
        Vector2 shieldElbow = chest + new Vector2(18f, 10f);
        Vector2 shieldHand = new(shieldForward - 9f, shieldY);
        DrawLimb(anchor, shieldElbow, shieldHand, 7f,
            new Color(75, 71, 73), flip);
        DrawLegionShield(anchor, new Vector2(shieldForward, shieldY), flip,
            enemy.IsLegionGuarding, enemy.IsLegionBlockReacting);

        Vector2 swordShoulder = chest + new Vector2(-16f, 2f);
        Vector2 swordHand = chest + new Vector2(-24f, 18f);
        float swordAngle = -.78f;
        if (enemy.CombatState is DemonFortressCombatState.LegionSlashWindup or
            DemonFortressCombatState.LegionFollowupWindup or
            DemonFortressCombatState.LegionCounterWindup)
        {
            swordHand = chest + new Vector2(-28f, -2f);
            swordAngle = -2.2f + enemy.ActionProgress * .42f;
        }
        else if (enemy.CombatState is DemonFortressCombatState.LegionSlashActive or
            DemonFortressCombatState.LegionCounterActive)
        {
            swordHand = chest + new Vector2(18f, 9f);
            swordAngle = -.2f + enemy.ActionProgress * .65f;
        }
        else if (enemy.CombatState == DemonFortressCombatState.LegionFollowupActive)
        {
            swordHand = chest + new Vector2(21f, -1f);
            swordAngle = .72f - enemy.ActionProgress * .62f;
        }
        DrawLimb(anchor, swordShoulder, swordHand, 7f,
            new Color(70, 66, 69), flip);
        DrawSword(anchor, swordHand, swordAngle, flip);

        if (enemy.CombatState is DemonFortressCombatState.LegionSlashActive or
            DemonFortressCombatState.LegionFollowupActive or
            DemonFortressCombatState.LegionCounterActive)
        {
            Vector2 handWorld = World(anchor, swordHand, flip);
            float direction = flip ? -1f : 1f;
            DrawLine(handWorld + new Vector2(direction * 8f, -26f),
                handWorld + new Vector2(direction * 58f, 7f),
                new Color(202, 190, 171, 105), 3);
        }
    }

    private void DrawFortressArcher(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not
            (DemonFortressCombatState.Ready or DemonFortressCombatState.ArcherReposition);
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color leather = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(75, 60, 58);
        if (!enemy.IsAlive)
        {
            DrawHumanoidDeath(anchor, flip, leather, enemy.DeathPresentationProgress,
                shield: false, bow: true);
            return;
        }

        float walk = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(time * 8.4f) * 8f
            : MathF.Sin(time * 1.5f) * 1f;
        float scan = enemy.CombatState == DemonFortressCombatState.Ready
            ? MathF.Sin(time * .9f) * 2f : 0f;
        Vector2 hip = new(0f, -34f);
        Vector2 chest = new(0f, -58f + scan);
        Vector2 head = new(0f, -76f + scan);

        DrawLimb(anchor, new Vector2(-10f - walk, -1f),
            hip + new Vector2(-7f, 3f), 7f, leather, flip);
        DrawLimb(anchor, new Vector2(11f + walk, -1f),
            hip + new Vector2(7f, 3f), 7f, new Color(61, 48, 48), flip);
        DrawBoot(anchor, new Vector2(-11f - walk, -2f), flip);
        DrawBoot(anchor, new Vector2(11f + walk, -2f), flip);
        DrawBox(anchor, hip.X - 11f, hip.Y - 6f, 22f, 18f,
            new Color(45, 41, 43), flip);
        DrawBox(anchor, chest.X - 12f, chest.Y - 12f, 24f, 25f, Outline, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y - 10f, 20f, 21f, leather, flip);
        DrawBox(anchor, chest.X - 9f, chest.Y - 7f, 18f, 4f, Crimson, flip);

        DrawQuiver(anchor, chest + new Vector2(-14f, 5f), flip);
        DrawBox(anchor, head.X - 9f, head.Y - 8f, 18f, 16f, Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 6f, 14f, 12f,
            new Color(92, 66, 58), flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 20f, 4f, Iron, flip);
        DrawBox(anchor, head.X + 3f, head.Y, 3f, 3f, Ember, flip);

        bool aiming = enemy.CombatState is DemonFortressCombatState.ArcherAim or
            DemonFortressCombatState.ArcherDraw or
            DemonFortressCombatState.ArcherHold or
            DemonFortressCombatState.ArcherRelease;
        float draw = enemy.CombatState switch
        {
            DemonFortressCombatState.ArcherDraw => enemy.ActionProgress,
            DemonFortressCombatState.ArcherHold or
            DemonFortressCombatState.ArcherRelease => 1f,
            _ => 0f
        };
        Vector2 bowHand = aiming ? new Vector2(25f, -59f) : new Vector2(21f, -39f);
        Vector2 drawHand = aiming
            ? new Vector2(16f - draw * 30f, -60f)
            : new Vector2(-7f, -45f);
        DrawLimb(anchor, chest + new Vector2(10f, 1f), bowHand, 6f, leather, flip);
        DrawLimb(anchor, chest + new Vector2(-10f, 1f), drawHand, 6f, leather, flip);
        DrawLongBow(anchor, bowHand, drawHand, aiming, draw, flip);
    }

    private void DrawHornGuard(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not
            (DemonFortressCombatState.Ready or DemonFortressCombatState.HornRetreat);
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color armor = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(86, 72, 70);
        if (!enemy.IsAlive)
        {
            DrawHornGuardDeath(anchor, flip, armor, enemy.DeathPresentationProgress);
            return;
        }

        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float step = moving ? MathF.Sin(time * 8.8f) * 8f : 0f;
        float nervous = !moving && enemy.CombatState == DemonFortressCombatState.Ready
            ? MathF.Sin(time * 3.2f) * 1.5f : 0f;
        float flinch = enemy.IsHornAlarmInterrupted ? -5f : 0f;
        Vector2 hip = new(flinch, -33f);
        Vector2 chest = new(flinch, -56f + nervous);
        Vector2 head = new(flinch, -73f + nervous);

        DrawLimb(anchor, new Vector2(-10f - step, -1f),
            hip + new Vector2(-7f, 2f), 7f, armor, flip);
        DrawLimb(anchor, new Vector2(11f + step, -1f),
            hip + new Vector2(7f, 2f), 7f, new Color(70, 48, 48), flip);
        DrawBoot(anchor, new Vector2(-11f - step, -2f), flip);
        DrawBoot(anchor, new Vector2(11f + step, -2f), flip);
        DrawBox(anchor, hip.X - 11f, hip.Y - 7f, 22f, 18f,
            new Color(47, 42, 44), flip);
        DrawBox(anchor, chest.X - 13f, chest.Y - 12f, 26f, 25f, Outline, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 10f, 22f, 21f, armor, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y - 7f, 20f, 5f, Crimson, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 8f, 7f, 8f, Iron, flip);
        DrawBox(anchor, chest.X + 10f, chest.Y - 8f, 7f, 8f, Iron, flip);

        DrawBox(anchor, head.X - 9f, head.Y - 8f, 18f, 16f, Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 6f, 14f, 12f,
            new Color(91, 61, 57), flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 20f, 4f, Iron, flip);
        DrawBox(anchor, head.X + 3f, head.Y, 3f, 3f, Ember, flip);
        DrawLine(World(anchor, chest + new Vector2(-11f, -8f), flip),
            World(anchor, hip + new Vector2(12f, 9f), flip), Crimson, 3);

        bool hornSequence = enemy.CombatState is DemonFortressCombatState.HornReach or
            DemonFortressCombatState.HornRaise or DemonFortressCombatState.HornInhale or
            DemonFortressCombatState.HornHold or DemonFortressCombatState.HornBlast or
            DemonFortressCombatState.HornRecovery;
        float raised = enemy.CombatState switch
        {
            DemonFortressCombatState.HornReach => enemy.ActionProgress * .35f,
            DemonFortressCombatState.HornRaise => .35f + enemy.ActionProgress * .65f,
            DemonFortressCombatState.HornInhale or DemonFortressCombatState.HornHold or
                DemonFortressCombatState.HornBlast => 1f,
            DemonFortressCombatState.HornRecovery => 1f - enemy.ActionProgress,
            _ => 0f
        };
        if (enemy.IsHornAlarmInterrupted)
            raised = .25f;
        Vector2 mouthpiece = Vector2.Lerp(new Vector2(-4f, -43f),
            new Vector2(8f, -70f), raised);
        Vector2 bell = Vector2.Lerp(new Vector2(43f, -35f),
            new Vector2(62f, -74f), raised);
        Vector2 nearHand = Vector2.Lerp(chest + new Vector2(-10f, 6f),
            new Vector2(14f, -65f), raised);
        Vector2 farHand = Vector2.Lerp(chest + new Vector2(12f, 8f),
            new Vector2(36f, -69f), raised);
        if (hornSequence)
            DrawLimb(anchor, chest + new Vector2(-10f, 1f), nearHand, 6f, armor, flip);
        DrawLimb(anchor, chest + new Vector2(10f, 1f), farHand, 6f, armor, flip);
        DrawWarHorn(anchor, mouthpiece, bell, flip,
            enemy.CombatState == DemonFortressCombatState.HornBlast);

        bool jab = enemy.CombatState is DemonFortressCombatState.HornJabWindup or
            DemonFortressCombatState.HornJabActive or
            DemonFortressCombatState.HornJabRecovery;
        Vector2 sidearmHand = jab
            ? new Vector2(enemy.CombatState == DemonFortressCombatState.HornJabActive
                ? 31f : -14f, -40f)
            : new Vector2(-18f, -34f);
        if (!hornSequence)
        {
            DrawLimb(anchor, chest + new Vector2(-11f, 3f), sidearmHand, 6f,
                new Color(73, 55, 54), flip);
            DrawShortSpear(anchor, sidearmHand, jab, flip);
        }
    }

    private void DrawDemonShieldbearer(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not DemonFortressCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color plate = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(57, 60, 67);
        if (!enemy.IsAlive)
        {
            DrawShieldbearerDeath(anchor, flip, plate, enemy.DeathPresentationProgress);
            return;
        }

        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float step = moving ? MathF.Sin(time * 5.6f) * 6f : 0f;
        float drive = enemy.CombatState == DemonFortressCombatState.ShieldPushActive
            ? enemy.ActionProgress * 9f : 0f;
        float brace = enemy.IsShieldbearerGuarding ? 5f : 0f;
        Vector2 hip = new(drive - brace, -36f);
        Vector2 chest = hip + new Vector2(-2f, -28f);
        Vector2 head = chest + new Vector2(-3f, -19f);

        DrawLimb(anchor, new Vector2(-18f - step, -1f),
            hip + new Vector2(-10f, 3f), 12f, plate, flip);
        DrawLimb(anchor, new Vector2(17f + step, -1f),
            hip + new Vector2(10f, 3f), 12f, new Color(47, 45, 50), flip);
        DrawBoot(anchor, new Vector2(-20f - step, -2f), flip);
        DrawBoot(anchor, new Vector2(17f + step, -2f), flip);
        DrawBox(anchor, hip.X - 18f, hip.Y - 8f, 36f, 22f,
            new Color(39, 39, 44), flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 15f, 44f, 33f, Outline, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y - 12f, 38f, 27f, plate, flip);
        DrawBox(anchor, chest.X - 16f, chest.Y - 8f, 32f, 6f, Crimson, flip);
        DrawBox(anchor, chest.X - 29f, chest.Y - 12f, 12f, 13f, Steel, flip);
        DrawBox(anchor, chest.X + 17f, chest.Y - 12f, 12f, 13f, Steel, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 10f, 24f, 20f, Outline, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 20f, 16f, Iron, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 10f, 26f, 6f, Steel, flip);
        DrawBox(anchor, head.X + 4f, head.Y - 1f, 4f, 3f, Ember, flip);

        float shieldX = enemy.CombatState switch
        {
            DemonFortressCombatState.ShieldPushWindup => 31f,
            DemonFortressCombatState.ShieldPushActive => 48f,
            DemonFortressCombatState.ShieldPushRecovery => 38f,
            DemonFortressCombatState.ShieldGuard => 38f,
            _ => 34f
        };
        float shieldY = enemy.IsShieldbearerGuarding ? -46f : -43f;
        Vector2 shieldHand = new(shieldX - 12f, shieldY);
        DrawLimb(anchor, chest + new Vector2(18f, 3f), shieldHand, 10f,
            plate, flip);
        DrawTowerShield(anchor, new Vector2(shieldX, shieldY), flip,
            enemy.IsShieldbearerGuarding, enemy.IsShieldbearerBlockReacting);

        bool striking = enemy.CombatState is
            DemonFortressCombatState.ShieldStrikeWindup or
            DemonFortressCombatState.ShieldStrikeActive or
            DemonFortressCombatState.ShieldStrikeRecovery;
        Vector2 weaponHand = striking
            ? new Vector2(enemy.CombatState == DemonFortressCombatState.ShieldStrikeActive
                ? 19f : -25f, -49f)
            : new Vector2(-20f, -37f);
        DrawLimb(anchor, chest + new Vector2(-18f, 4f), weaponHand, 9f,
            new Color(48, 46, 51), flip);
        DrawCompactMace(anchor, weaponHand, striking, flip);
    }

    private void DrawInfernalPikeman(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not
            (DemonFortressCombatState.Ready or DemonFortressCombatState.PikeReposition);
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color armor = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(75, 66, 68);
        if (!enemy.IsAlive)
        {
            DrawPikemanDeath(anchor, flip, armor, enemy.DeathPresentationProgress);
            return;
        }

        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float step = moving ? MathF.Sin(time * 6.4f) * 7f : 0f;
        bool settling = enemy.CombatState is DemonFortressCombatState.PikeSettle or
            DemonFortressCombatState.PikeAdvanceSettle;
        bool recovering = enemy.CombatState is DemonFortressCombatState.PikeRecovery or
            DemonFortressCombatState.PikeAdvanceRecovery;
        float crouch = settling ? 4f : recovering ? 2f : 0f;
        Vector2 hip = new(-3f, -35f + crouch);
        Vector2 chest = new(-1f, -59f + crouch);
        Vector2 head = new(-2f, -76f + crouch);

        DrawLimb(anchor, new Vector2(-14f - step, -1f),
            hip + new Vector2(-8f, 3f), 8f, armor, flip);
        DrawLimb(anchor, new Vector2(14f + step, -1f),
            hip + new Vector2(8f, 3f), 8f, new Color(62, 48, 49), flip);
        DrawBoot(anchor, new Vector2(-15f - step, -2f), flip);
        DrawBoot(anchor, new Vector2(14f + step, -2f), flip);
        DrawBox(anchor, hip.X - 13f, hip.Y - 7f, 26f, 19f,
            new Color(43, 41, 45), flip);
        DrawBox(anchor, chest.X - 16f, chest.Y - 13f, 32f, 27f, Outline, flip);
        DrawBox(anchor, chest.X - 13f, chest.Y - 10f, 26f, 22f, armor, flip);
        DrawBox(anchor, chest.X - 12f, chest.Y - 6f, 24f, 4f, Crimson, flip);
        DrawBox(anchor, chest.X - 21f, chest.Y - 9f, 9f, 9f, Iron, flip);
        DrawBox(anchor, chest.X + 12f, chest.Y - 9f, 9f, 9f, Iron, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 20f, 17f, Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 6f, 16f, 13f, Iron, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 9f, 22f, 4f, Steel, flip);
        DrawBox(anchor, head.X + 3f, head.Y, 3f, 3f, Ember, flip);

        float extension = enemy.PikeExtensionRatio;
        float pullback = settling ? 15f * enemy.ActionProgress : 0f;
        float tipX = 48f + extension * 108f - pullback;
        Vector2 rearHand = new(-18f, -47f + crouch);
        Vector2 forwardHand = new(16f + extension * 12f, -48f + crouch);
        DrawPike(anchor, tipX, -48f + crouch, flip);
        DrawLimb(anchor, chest + new Vector2(-12f, 2f), rearHand, 7f,
            armor, flip);
        DrawLimb(anchor, chest + new Vector2(12f, 2f), forwardHand, 7f,
            armor, flip);
        if (extension > .45f)
        {
            Vector2 tip = World(anchor, new Vector2(tipX, -48f + crouch), flip);
            float sign = flip ? -1f : 1f;
            DrawLine(tip - new Vector2(sign * 31f, 0f),
                tip - new Vector2(sign * 5f, 0f),
                new Color(218, 196, 164, 105), 3);
        }
    }

    private void DrawWarHound(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool locked = enemy.CombatState is DemonFortressCombatState.HoundLungeWindup or
            DemonFortressCombatState.HoundLungeActive or
            DemonFortressCombatState.HoundLanding or
            DemonFortressCombatState.HoundLungeRecovery or
            DemonFortressCombatState.HoundBiteWindup or
            DemonFortressCombatState.HoundBiteActive or
            DemonFortressCombatState.HoundBiteRecovery;
        bool flip = locked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color hide = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(75, 42, 41);
        if (!enemy.IsAlive)
        {
            DrawWarHoundDeath(anchor, flip, hide, enemy.DeathPresentationProgress);
            return;
        }

        bool running = MathF.Abs(enemy.VisualVelocityX) > 95f ||
            enemy.CombatState == DemonFortressCombatState.HoundLungeActive;
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float cycleSpeed = running ? 13f : 7f;
        float gait = moving ? MathF.Sin(time * cycleSpeed) : 0f;
        float breathe = !moving ? MathF.Sin(time * 2.3f) * 1.2f : 0f;
        float compression = enemy.CombatState switch
        {
            DemonFortressCombatState.HoundLungeWindup => enemy.ActionProgress * 7f,
            DemonFortressCombatState.HoundLanding => 6f,
            DemonFortressCombatState.HoundTurn =>
                MathF.Sin(enemy.ActionProgress * MathF.PI) * 4f,
            _ => 0f
        };
        float stretch = enemy.CombatState == DemonFortressCombatState.HoundLungeActive
            ? 9f : 0f;
        float lift = enemy.CombatState == DemonFortressCombatState.HoundLungeActive
            ? -5f : 0f;
        Vector2 chest = new(16f + stretch, -25f + compression + lift + breathe);
        Vector2 hips = new(-18f - stretch, -22f + compression + lift + breathe);
        Vector2 neck = chest + new Vector2(12f, -6f);
        float idleLook = enemy.CombatState == DemonFortressCombatState.Ready
            ? MathF.Sin(time * 1.6f) * 1.5f : 0f;
        Vector2 head = neck + new Vector2(13f, 1f + idleLook);

        DrawLine(World(anchor, hips + new Vector2(-3f, -2f), flip),
            World(anchor, chest + new Vector2(4f, -3f), flip), Outline, 24);
        DrawLine(World(anchor, hips, flip), World(anchor, chest, flip), hide, 19);
        DrawBox(anchor, chest.X - 9f, chest.Y - 12f, 18f, 23f,
            new Color(69, 54, 53), flip);
        DrawLine(World(anchor, hips + new Vector2(-5f, -4f), flip),
            World(anchor, hips + new Vector2(-31f, -13f - gait * 3f), flip),
            hide, 6);

        float frontStride = gait * (running ? 10f : 6f);
        float rearStride = -gait * (running ? 11f : 6f);
        DrawHoundLeg(anchor, chest + new Vector2(5f, 7f),
            new Vector2(21f + frontStride, -1f), hide, flip, front: true);
        DrawHoundLeg(anchor, chest + new Vector2(-4f, 7f),
            new Vector2(8f - frontStride, -1f), new Color(58, 38, 38), flip,
            front: true);
        DrawHoundLeg(anchor, hips + new Vector2(-4f, 5f),
            new Vector2(-23f + rearStride, -1f), hide, flip, front: false);
        DrawHoundLeg(anchor, hips + new Vector2(5f, 5f),
            new Vector2(-8f - rearStride, -1f), new Color(58, 38, 38), flip,
            front: false);

        DrawLine(World(anchor, neck, flip), World(anchor, head, flip), hide, 15);
        DrawBox(anchor, head.X - 8f, head.Y - 8f, 20f, 16f, hide, flip);
        bool jawsOpen = enemy.CombatState is DemonFortressCombatState.HoundLungeWindup or
            DemonFortressCombatState.HoundLungeActive or
            DemonFortressCombatState.HoundBiteWindup or
            DemonFortressCombatState.HoundBiteActive;
        DrawBox(anchor, head.X + 8f, head.Y + (jawsOpen ? 2f : 0f), 13f, 6f,
            new Color(48, 29, 30), flip);
        if (jawsOpen)
            DrawBox(anchor, head.X + 10f, head.Y + 8f, 11f, 4f,
                new Color(137, 49, 45), flip);
        DrawBox(anchor, head.X + 3f, head.Y - 3f, 3f, 3f, Ember, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y - 10f, 7f, 22f, Steel, flip);
        DrawBox(anchor, neck.X - 4f, neck.Y - 9f, 7f, 18f, Iron, flip);
        DrawLine(World(anchor, chest + new Vector2(-5f, -8f), flip),
            World(anchor, hips + new Vector2(3f, -7f), flip), Crimson, 3);

        if (enemy.CombatState is DemonFortressCombatState.HoundLanding or
            DemonFortressCombatState.HoundLungeRecovery)
        {
            float sign = flip ? -1f : 1f;
            for (int i = 0; i < 3; i++)
                DrawLine(anchor + new Vector2(-sign * (15f + i * 9f), -2f - i * 2f),
                    anchor + new Vector2(-sign * (25f + i * 12f), -2f - i * 2f),
                    new Color(145, 105, 74, 105), 2);
        }
    }

    private void DrawInfernalSmith(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not DemonFortressCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color blackened = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(66, 58, 56);
        if (!enemy.IsAlive)
        {
            DrawSmithDeath(anchor, flip, blackened, enemy.DeathPresentationProgress);
            return;
        }

        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float step = moving ? MathF.Sin(time * 4.8f) * 6f : 0f;
        float breathe = !moving && enemy.CombatState == DemonFortressCombatState.Ready
            ? MathF.Sin(time * 1.5f) * 2f : 0f;
        float crouch = enemy.CombatState == DemonFortressCombatState.SmithSlamImpact
            ? 8f : 0f;
        Vector2 hip = new(-3f, -39f + crouch);
        Vector2 chest = new(-6f, -68f + crouch + breathe);
        Vector2 head = new(-10f, -88f + crouch + breathe);

        DrawLimb(anchor, new Vector2(-18f - step, -1f),
            hip + new Vector2(-11f, 3f), 13f, blackened, flip);
        DrawLimb(anchor, new Vector2(18f + step, -1f),
            hip + new Vector2(11f, 3f), 13f, new Color(53, 47, 47), flip);
        DrawBoot(anchor, new Vector2(-20f - step, -2f), flip);
        DrawBoot(anchor, new Vector2(18f + step, -2f), flip);
        DrawBox(anchor, hip.X - 20f, hip.Y - 8f, 40f, 23f,
            new Color(42, 38, 39), flip);
        DrawBox(anchor, chest.X - 25f, chest.Y - 17f, 50f, 38f, Outline, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 14f, 44f, 32f,
            blackened, flip);
        DrawBox(anchor, chest.X - 30f, chest.Y - 14f, 14f, 15f, Iron, flip);
        DrawBox(anchor, chest.X + 16f, chest.Y - 14f, 14f, 15f, Iron, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y + 5f, 36f, 35f,
            new Color(62, 42, 37), flip);
        DrawBox(anchor, chest.X - 15f, chest.Y + 8f, 4f, 27f, Bronze, flip);
        DrawBox(anchor, chest.X + 11f, chest.Y + 8f, 4f, 27f, Bronze, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 9f, 24f, 19f, Outline, flip);
        DrawBox(anchor, head.X - 10f, head.Y - 7f, 20f, 15f,
            new Color(79, 48, 44), flip);
        DrawBox(anchor, head.X + 3f, head.Y, 4f, 3f, Ember, flip);

        Vector2 rearHand;
        Vector2 frontHand;
        Vector2 hammerHead;
        if (enemy.CombatState == DemonFortressCombatState.SmithSlamRaise)
        {
            float p = enemy.ActionProgress;
            rearHand = Vector2.Lerp(new Vector2(-16f, -38f), new Vector2(8f, -91f), p);
            frontHand = Vector2.Lerp(new Vector2(5f, -50f), new Vector2(8f, -72f), p);
            hammerHead = Vector2.Lerp(new Vector2(-39f, -27f),
                new Vector2(8f, -127f), p);
        }
        else if (enemy.CombatState == DemonFortressCombatState.SmithSlamFall)
        {
            float p = enemy.ActionProgress;
            rearHand = Vector2.Lerp(new Vector2(8f, -91f), new Vector2(26f, -31f), p);
            frontHand = Vector2.Lerp(new Vector2(8f, -72f), new Vector2(10f, -48f), p);
            hammerHead = Vector2.Lerp(new Vector2(8f, -127f),
                new Vector2(48f, -8f), p);
        }
        else if (enemy.CombatState is DemonFortressCombatState.SmithSlamImpact or
            DemonFortressCombatState.SmithSlamRecovery)
        {
            rearHand = new Vector2(26f, -31f);
            frontHand = new Vector2(10f, -48f);
            hammerHead = new Vector2(49f, -7f);
        }
        else if (enemy.CombatState is DemonFortressCombatState.SmithSwingWindup)
        {
            rearHand = new Vector2(-24f, -58f);
            frontHand = new Vector2(-7f, -50f);
            hammerHead = new Vector2(-56f, -67f);
        }
        else if (enemy.CombatState is DemonFortressCombatState.SmithSwingActive)
        {
            float p = enemy.ActionProgress;
            rearHand = Vector2.Lerp(new Vector2(-24f, -58f), new Vector2(19f, -52f), p);
            frontHand = Vector2.Lerp(new Vector2(-7f, -50f), new Vector2(35f, -43f), p);
            hammerHead = Vector2.Lerp(new Vector2(-56f, -67f),
                new Vector2(72f, -38f), p);
        }
        else if (enemy.CombatState == DemonFortressCombatState.SmithSwingRecovery)
        {
            float p = enemy.ActionProgress;
            rearHand = Vector2.Lerp(new Vector2(19f, -52f),
                new Vector2(-16f, -38f), p);
            frontHand = Vector2.Lerp(new Vector2(35f, -43f),
                new Vector2(5f, -50f), p);
            hammerHead = Vector2.Lerp(new Vector2(72f, -38f),
                new Vector2(-40f, -24f), p);
        }
        else
        {
            rearHand = new Vector2(-16f, -38f);
            frontHand = new Vector2(5f, -50f);
            hammerHead = new Vector2(-40f, -24f);
        }
        DrawForgeHammer(anchor, rearHand, hammerHead, flip);
        DrawLimb(anchor, chest + new Vector2(-18f, 1f), rearHand, 11f,
            blackened, flip);
        DrawLimb(anchor, chest + new Vector2(18f, 1f), frontHand, 11f,
            blackened, flip);
        DrawBox(anchor, rearHand.X - 5f, rearHand.Y - 5f, 10f, 10f,
            new Color(37, 35, 37), flip);
        DrawBox(anchor, frontHand.X - 5f, frontHand.Y - 5f, 10f, 10f,
            new Color(37, 35, 37), flip);

        if (enemy.IsSmithGroundImpact)
        {
            float sign = flip ? -1f : 1f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 start = anchor + new Vector2(sign * (17f + i * 12f), -4f);
                DrawLine(start, start + new Vector2(sign * 8f, -8f - i * 2f),
                    i % 2 == 0 ? Ember : Bronze, 3);
            }
        }
    }

    private void DrawDemonOfficer(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not DemonFortressCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color plate = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(61, 64, 72);
        if (!enemy.IsAlive)
        {
            DrawOfficerDeath(anchor, flip, plate, enemy.DeathPresentationProgress);
            return;
        }

        bool moving = MathF.Abs(enemy.VisualVelocityX) > 3f;
        float step = moving ? MathF.Sin(time * 6.6f) * 7f : 0f;
        float straighten = enemy.IsOfficerCommanding ? -3f : 0f;
        Vector2 hip = new(0f, -38f);
        Vector2 chest = new(0f, -66f + straighten);
        Vector2 head = new(0f, -86f + straighten);

        DrawLimb(anchor, new Vector2(-13f - step, -1f),
            hip + new Vector2(-8f, 3f), 9f, plate, flip);
        DrawLimb(anchor, new Vector2(14f + step, -1f),
            hip + new Vector2(8f, 3f), 9f, new Color(49, 48, 54), flip);
        DrawBoot(anchor, new Vector2(-14f - step, -2f), flip);
        DrawBoot(anchor, new Vector2(14f + step, -2f), flip);
        DrawBox(anchor, hip.X - 15f, hip.Y - 7f, 30f, 20f,
            new Color(39, 39, 45), flip);
        DrawBox(anchor, chest.X - 19f, chest.Y - 15f, 38f, 32f, Outline, flip);
        DrawBox(anchor, chest.X - 16f, chest.Y - 12f, 32f, 27f, plate, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 8f, 28f, 5f, Crimson, flip);
        DrawBox(anchor, chest.X - 25f, chest.Y - 12f, 11f, 12f, Steel, flip);
        DrawBox(anchor, chest.X + 14f, chest.Y - 12f, 11f, 12f, Steel, flip);
        DrawBox(anchor, chest.X - 18f, chest.Y + 12f, 10f, 24f, Crimson, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 9f, 22f, 19f, Outline, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 7f, 18f, 15f, Iron, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 10f, 26f, 5f, Bronze, flip);
        DrawBox(anchor, head.X - 3f, head.Y - 21f, 6f, 13f, Crimson, flip);
        DrawBox(anchor, head.X + 3f, head.Y, 4f, 3f, Ember, flip);

        bool commanding = enemy.IsOfficerCommanding;
        Vector2 swordHand;
        Vector2 freeHand;
        float swordAngle;
        if (commanding)
        {
            float raise = enemy.CombatState == DemonFortressCombatState.OfficerCommandRaise
                ? enemy.ActionProgress : 1f;
            swordHand = Vector2.Lerp(new Vector2(-19f, -48f),
                new Vector2(3f, -91f), raise);
            freeHand = Vector2.Lerp(new Vector2(17f, -46f),
                new Vector2(34f, -72f), raise);
            swordAngle = -MathHelper.PiOver2;
        }
        else if (enemy.CombatState is DemonFortressCombatState.OfficerSlashWindup or
            DemonFortressCombatState.OfficerAdvanceWindup)
        {
            swordHand = new Vector2(-25f, -63f);
            freeHand = new Vector2(18f, -47f);
            swordAngle = -2.25f;
        }
        else if (enemy.CombatState is DemonFortressCombatState.OfficerSlashActive or
            DemonFortressCombatState.OfficerAdvanceActive)
        {
            swordHand = new Vector2(25f, -48f);
            freeHand = new Vector2(13f, -43f);
            swordAngle = -.18f;
        }
        else
        {
            swordHand = new Vector2(-18f, -45f);
            freeHand = new Vector2(18f, -48f);
            swordAngle = -.78f;
        }
        DrawLimb(anchor, chest + new Vector2(-16f, 2f), swordHand, 8f,
            plate, flip);
        DrawLimb(anchor, chest + new Vector2(16f, 2f), freeHand, 8f,
            plate, flip);
        DrawOfficerSword(anchor, swordHand, swordAngle, flip);
        if (enemy.CombatState == DemonFortressCombatState.OfficerCommandActive)
        {
            float sign = flip ? -1f : 1f;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 start = World(anchor, freeHand, flip) +
                    new Vector2(sign * 5f, i * 7f);
                DrawLine(start, start + new Vector2(sign * 18f, i * 4f),
                    new Color(211, 119, 63, 150), 2);
            }
        }
    }

    private void DrawForgeHammer(
        Vector2 anchor, Vector2 rearHand, Vector2 hammerHead, bool flip)
    {
        Vector2 grip = World(anchor, rearHand, flip);
        Vector2 head = World(anchor, hammerHead, flip);
        Vector2 direction = head - grip;
        if (direction.LengthSquared() < 1f)
            direction = new Vector2(1f, 0f);
        direction.Normalize();
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(grip - direction * 18f, head, Outline, 9);
        DrawLine(grip - direction * 16f, head - direction * 5f,
            new Color(91, 58, 39), 6);
        DrawLine(head - normal * 14f, head + normal * 14f, Outline, 22);
        DrawLine(head - normal * 11f, head + normal * 11f,
            new Color(48, 48, 53), 17);
        DrawLine(head - normal * 8f, head + normal * 8f, Steel, 3);
    }

    private void DrawOfficerSword(
        Vector2 anchor, Vector2 hand, float angle, bool flip)
    {
        float sign = flip ? -1f : 1f;
        Vector2 grip = World(anchor, hand, flip);
        Vector2 direction = new(MathF.Cos(angle) * sign, MathF.Sin(angle));
        Vector2 bladeStart = grip + direction * 8f;
        Vector2 tip = bladeStart + direction * 55f;
        DrawLine(grip - direction * 9f, bladeStart,
            new Color(86, 54, 37), 5);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(grip - normal * 10f, grip + normal * 10f, Bronze, 4);
        DrawLine(bladeStart, tip, Steel, 7);
        DrawLine(bladeStart, tip, new Color(222, 216, 199), 2);
    }

    private void DrawSmithDeath(
        Vector2 anchor, bool flip, Color body, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(body.R, body.G, body.B, alpha);
        DrawLimb(anchor, new Vector2(-31f, -4f), new Vector2(3f, -16f),
            13f, faded, flip);
        DrawLimb(anchor, new Vector2(5f, -15f), new Vector2(34f, -4f),
            13f, faded, flip);
        DrawBox(anchor, -22f, -35f, 50f, 27f, faded, flip);
        DrawForgeHammer(anchor, new Vector2(14f, -8f),
            new Vector2(69f, -6f), flip);
    }

    private void DrawOfficerDeath(
        Vector2 anchor, bool flip, Color body, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(body.R, body.G, body.B, alpha);
        DrawLimb(anchor, new Vector2(-28f, -4f), new Vector2(3f, -14f),
            9f, faded, flip);
        DrawLimb(anchor, new Vector2(4f, -13f), new Vector2(30f, -4f),
            9f, faded, flip);
        DrawBox(anchor, -17f, -31f, 36f, 23f, faded, flip);
        DrawOfficerSword(anchor, new Vector2(15f, -8f), -.2f, flip);
    }

    private void DrawPike(Vector2 anchor, float tipX, float y, bool flip)
    {
        Vector2 tip = World(anchor, new Vector2(tipX, y), flip);
        Vector2 butt = World(anchor, new Vector2(tipX - 150f, y), flip);
        float sign = flip ? -1f : 1f;
        Vector2 shaftEnd = tip - new Vector2(sign * 15f, 0f);
        DrawLine(butt, shaftEnd, Outline, 7);
        DrawLine(butt, shaftEnd, new Color(103, 65, 40), 4);
        DrawLine(butt - new Vector2(sign * 3f, 0f),
            butt + new Vector2(sign * 8f, 0f), Bronze, 7);
        DrawLine(shaftEnd, tip, Steel, 7);
        DrawLine(tip, tip - new Vector2(sign * 8f, -6f), Steel, 3);
        DrawLine(tip, tip - new Vector2(sign * 8f, 6f), Steel, 3);
    }

    private void DrawPikemanDeath(
        Vector2 anchor, bool flip, Color armor, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(armor.R, armor.G, armor.B, alpha);
        DrawLimb(anchor, new Vector2(-27f, -3f), new Vector2(2f, -13f),
            8f, faded, flip);
        DrawLimb(anchor, new Vector2(3f, -12f), new Vector2(28f, -4f),
            8f, faded, flip);
        DrawBox(anchor, -14f, -29f, 31f, 20f, faded, flip);
        DrawPike(anchor, 72f, -5f, flip);
    }

    private void DrawHoundLeg(
        Vector2 anchor,
        Vector2 joint,
        Vector2 paw,
        Color color,
        bool flip,
        bool front)
    {
        Vector2 knee = Vector2.Lerp(joint, paw, .48f) +
            new Vector2(front ? 5f : -7f, -3f);
        DrawLine(World(anchor, joint, flip), World(anchor, knee, flip), color, 7);
        DrawLine(World(anchor, knee, flip), World(anchor, paw, flip), color, 6);
        DrawLine(World(anchor, paw + new Vector2(-3f, 0f), flip),
            World(anchor, paw + new Vector2(7f, 0f), flip), Outline, 4);
    }

    private void DrawWarHoundDeath(
        Vector2 anchor, bool flip, Color hide, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(hide.R, hide.G, hide.B, alpha);
        DrawLine(World(anchor, new Vector2(-26f, -10f), flip),
            World(anchor, new Vector2(22f, -13f), flip), faded, 20);
        DrawBox(anchor, 18f, -21f, 24f, 15f, faded, flip);
        DrawLimb(anchor, new Vector2(-22f, -1f), new Vector2(-8f, -10f),
            7f, faded, flip);
        DrawLimb(anchor, new Vector2(13f, -2f), new Vector2(25f, -9f),
            7f, faded, flip);
        DrawLine(World(anchor, new Vector2(-25f, -13f), flip),
            World(anchor, new Vector2(-48f, -5f), flip), faded, 5);
    }

    private void DrawWarHorn(
        Vector2 anchor, Vector2 mouthpiece, Vector2 bell, bool flip, bool blasting)
    {
        Vector2 mouthWorld = World(anchor, mouthpiece, flip);
        Vector2 bellWorld = World(anchor, bell, flip);
        Vector2 direction = bellWorld - mouthWorld;
        if (direction.LengthSquared() < 1f)
            direction = Vector2.UnitX;
        direction.Normalize();
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(mouthWorld, bellWorld - direction * 8f, Outline, 11);
        DrawLine(mouthWorld, bellWorld - direction * 8f, Bronze, 7);
        DrawLine(mouthWorld - direction * 5f, mouthWorld + direction * 5f,
            new Color(205, 148, 76), 3);
        DrawLine(bellWorld - normal * 11f, bellWorld + normal * 11f, Outline, 7);
        DrawLine(bellWorld - normal * 9f, bellWorld + normal * 9f,
            new Color(181, 117, 58), 5);
        DrawLine(bellWorld - direction * 12f - normal * 5f,
            bellWorld - normal * 10f, Bronze, 5);
        DrawLine(bellWorld - direction * 12f + normal * 5f,
            bellWorld + normal * 10f, Bronze, 5);
        if (!blasting)
            return;
        for (int i = 0; i < 3; i++)
        {
            float reach = 13f + i * 10f;
            Vector2 center = bellWorld + direction * reach;
            DrawLine(center - normal * (7f + i * 3f),
                center + normal * (7f + i * 3f),
                new Color((byte)238, (byte)174, (byte)83,
                    (byte)(160 - i * 38)), 2);
        }
    }

    private void DrawShortSpear(
        Vector2 anchor, Vector2 hand, bool attacking, bool flip)
    {
        Vector2 grip = World(anchor, hand, flip);
        float sign = flip ? -1f : 1f;
        Vector2 direction = attacking
            ? new Vector2(sign, -.08f)
            : new Vector2(-sign * .35f, .94f);
        Vector2 butt = grip - direction * 15f;
        Vector2 tip = grip + direction * 31f;
        DrawLine(butt, tip, new Color(99, 65, 41), 4);
        DrawLine(tip, tip + direction * 9f, Steel, 6);
    }

    private void DrawTowerShield(
        Vector2 anchor, Vector2 center, bool flip, bool guarding, bool reacting)
    {
        float width = guarding ? 46f : 43f;
        float height = guarding ? 69f : 65f;
        Color rim = reacting ? new Color(239, 192, 91) : Steel;
        DrawBox(anchor, center.X - width / 2f - 3f, center.Y - height / 2f - 3f,
            width + 6f, height + 6f, Outline, flip);
        DrawBox(anchor, center.X - width / 2f, center.Y - height / 2f,
            width, height, new Color(48, 51, 58), flip);
        DrawBox(anchor, center.X - width / 2f + 5f, center.Y - height / 2f + 6f,
            width - 10f, 7f, rim, flip);
        DrawBox(anchor, center.X - 3f, center.Y - height / 2f + 6f,
            6f, height - 12f, rim, flip);
        DrawBox(anchor, center.X - width / 2f + 5f, center.Y - 3f,
            width - 10f, 6f, Crimson, flip);
    }

    private void DrawCompactMace(
        Vector2 anchor, Vector2 hand, bool striking, bool flip)
    {
        Vector2 grip = World(anchor, hand, flip);
        float sign = flip ? -1f : 1f;
        Vector2 direction = striking
            ? new Vector2(sign * .92f, .38f)
            : new Vector2(-sign * .55f, .84f);
        Vector2 head = grip + direction * 32f;
        DrawLine(grip - direction * 7f, head, new Color(95, 67, 44), 5);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(head - normal * 7f, head + normal * 7f, Steel, 10);
    }

    private void DrawHornGuardDeath(
        Vector2 anchor, bool flip, Color armor, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(armor.R, armor.G, armor.B, alpha);
        DrawLimb(anchor, new Vector2(-25f, -3f), new Vector2(2f, -12f),
            7f, faded, flip);
        DrawLimb(anchor, new Vector2(4f, -11f), new Vector2(27f, -4f),
            7f, faded, flip);
        DrawBox(anchor, -14f, -27f, 28f, 19f, faded, flip);
        DrawWarHorn(anchor, new Vector2(8f, -10f), new Vector2(57f, -4f),
            flip, false);
    }

    private void DrawShieldbearerDeath(
        Vector2 anchor, bool flip, Color plate, float progress)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(plate.R, plate.G, plate.B, alpha);
        DrawLimb(anchor, new Vector2(-29f, -4f), new Vector2(1f, -14f),
            12f, faded, flip);
        DrawLimb(anchor, new Vector2(4f, -13f), new Vector2(31f, -4f),
            12f, faded, flip);
        DrawBox(anchor, -18f, -30f, 40f, 22f, faded, flip);
        DrawBox(anchor, 20f, -35f, 45f, 27f,
            new Color((byte)48, (byte)51, (byte)58, alpha), flip);
    }

    private void DrawLegionShield(
        Vector2 anchor, Vector2 center, bool flip, bool guarding, bool reacting)
    {
        int width = guarding ? 31 : 28;
        int height = guarding ? 51 : 47;
        Color rim = reacting ? new Color(230, 190, 103) : Steel;
        DrawBox(anchor, center.X - width / 2f - 3f, center.Y - height / 2f - 3f,
            width + 6f, height + 6f, Outline, flip);
        DrawBox(anchor, center.X - width / 2f, center.Y - height / 2f,
            width, height, new Color(61, 64, 70), flip);
        DrawBox(anchor, center.X - 2f, center.Y - height / 2f + 4f,
            4f, height - 8f, rim, flip);
        DrawBox(anchor, center.X - width / 2f + 4f, center.Y - 2f,
            width - 8f, 4f, Crimson, flip);
    }

    private void DrawSword(Vector2 anchor, Vector2 hand, float angle, bool flip)
    {
        float sign = flip ? -1f : 1f;
        Vector2 grip = World(anchor, hand, flip);
        Vector2 direction = new(MathF.Cos(angle) * sign, MathF.Sin(angle));
        Vector2 bladeStart = grip + direction * 7f;
        Vector2 tip = bladeStart + direction * 47f;
        DrawLine(grip - direction * 8f, bladeStart, new Color(92, 61, 39), 5);
        DrawLine(grip + new Vector2(-direction.Y, direction.X) * 9f,
            grip - new Vector2(-direction.Y, direction.X) * 9f, Bronze, 4);
        DrawLine(bladeStart, tip, new Color(172, 169, 165), 6);
        DrawLine(bladeStart, tip, new Color(218, 211, 195), 2);
    }

    private void DrawLongBow(
        Vector2 anchor, Vector2 bowHand, Vector2 drawHand,
        bool aiming, float draw, bool flip)
    {
        Vector2 grip = World(anchor, bowHand, flip);
        float sign = flip ? -1f : 1f;
        Vector2 top = grip + new Vector2(sign * 5f, -31f);
        Vector2 bottom = grip + new Vector2(sign * 5f, 31f);
        Vector2 upper = grip + new Vector2(sign * 15f, -17f);
        Vector2 lower = grip + new Vector2(sign * 15f, 17f);
        DrawLine(top, upper, Bronze, 4);
        DrawLine(upper, grip, Bronze, 4);
        DrawLine(grip, lower, Bronze, 4);
        DrawLine(lower, bottom, Bronze, 4);
        Vector2 stringPoint = aiming ? World(anchor, drawHand, flip) : grip;
        DrawLine(top, stringPoint, new Color(205, 196, 174), 1);
        DrawLine(stringPoint, bottom, new Color(205, 196, 174), 1);
        if (aiming && draw > .06f)
        {
            Vector2 arrowTip = grip + new Vector2(sign * 46f, 0f);
            DrawLine(stringPoint, arrowTip, Steel, 2);
            DrawLine(arrowTip, arrowTip + new Vector2(-sign * 7f, -4f), Steel, 2);
            DrawLine(arrowTip, arrowTip + new Vector2(-sign * 7f, 4f), Steel, 2);
        }
    }

    private void DrawQuiver(Vector2 anchor, Vector2 center, bool flip)
    {
        DrawBox(anchor, center.X - 5f, center.Y - 12f, 10f, 36f,
            new Color(82, 49, 37), flip);
        for (int i = 0; i < 3; i++)
        {
            float x = center.X - 5f + i * 5f;
            DrawLine(World(anchor, new Vector2(x, center.Y - 10f), flip),
                World(anchor, new Vector2(x - 3f, center.Y - 32f), flip), Steel, 2);
        }
    }

    private void DrawHumanoidDeath(
        Vector2 anchor, bool flip, Color body, float progress, bool shield, bool bow)
    {
        byte alpha = (byte)MathHelper.Lerp(235f, 0f, progress);
        Color faded = new(body.R, body.G, body.B, alpha);
        DrawLimb(anchor, new Vector2(-25f, -4f), new Vector2(4f, -12f),
            11f, faded, flip);
        DrawLimb(anchor, new Vector2(2f, -11f), new Vector2(28f, -5f),
            9f, faded, flip);
        DrawBox(anchor, -12f, -25f, 28f, 18f, faded, flip);
        DrawBox(anchor, -27f, -23f, 17f, 15f, faded, flip);
        if (shield)
            DrawBox(anchor, 18f, -31f, 25f, 39f,
                new Color((byte)61, (byte)64, (byte)70, alpha), flip);
        if (bow)
            DrawLine(World(anchor, new Vector2(12f, -5f), flip),
                World(anchor, new Vector2(47f, -16f), flip),
                new Color(Bronze.R, Bronze.G, Bronze.B, alpha), 4);
    }

    private void DrawLimb(
        Vector2 anchor, Vector2 end, Vector2 start, float thickness,
        Color color, bool flip)
    {
        Vector2 middle = Vector2.Lerp(start, end, .52f) + new Vector2(0f, 2f);
        DrawLine(World(anchor, start, flip), World(anchor, middle, flip), color,
            Math.Max(2, (int)MathF.Round(thickness)));
        DrawLine(World(anchor, middle, flip), World(anchor, end, flip), color,
            Math.Max(2, (int)MathF.Round(thickness * .82f)));
    }

    private void DrawBoot(Vector2 anchor, Vector2 foot, bool flip) =>
        DrawBox(anchor, foot.X - 7f, foot.Y - 4f, 16f, 6f, Outline, flip);

    private void DrawBox(
        Vector2 anchor, float x, float y, float width, float height,
        Color color, bool flip)
    {
        float left = flip ? -x - width : x;
        _spriteBatch.Draw(_pixel,
            new Rectangle(
                (int)MathF.Round(anchor.X + left),
                (int)MathF.Round(anchor.Y + y),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))),
            color);
    }

    private static Vector2 World(Vector2 anchor, Vector2 local, bool flip) =>
        anchor + new Vector2(flip ? -local.X : local.X, local.Y);

    private struct BallistaOperatorPose
    {
        public Vector2 Hip;
        public Vector2 Chest;
        public Vector2 Head;
        public Vector2 RearKnee;
        public Vector2 RearFoot;
        public Vector2 FrontKnee;
        public Vector2 FrontFoot;
        public Vector2 RearElbow;
        public Vector2 RearHand;
        public Vector2 FrontElbow;
        public Vector2 FrontHand;
    }

    private void DrawBallistaGunner(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool flip = enemy.CombatState == DemonFortressCombatState.Ready
            ? enemy.Facing == EnemyFacingDirection.Left
            : enemy.AttackFacesLeft;
        float progress = enemy.ActionProgress;
        bool phaseTwo = enemy.BossCombatPhase == 2;
        Color wood = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(91, 57, 38);
        Color darkWood = new(51, 34, 29);
        Color iron = new(93, 95, 101);
        Color steel = new(137, 137, 140);
        Color operatorPlate = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(62, 62, 67);
        Color leather = new(91, 57, 43);

        if (!enemy.IsAlive)
        {
            DrawRebuiltBallistaDeath(
                enemy, anchor, flip, wood, darkWood, iron, operatorPlate);
            return;
        }

        bool warning = enemy.CombatState == DemonFortressCombatState.BallistaWarning;
        bool firing = enemy.CombatState == DemonFortressCombatState.BallistaFire;
        bool recoilState = enemy.CombatState == DemonFortressCombatState.BallistaRecoil;
        bool cranking = enemy.CombatState == DemonFortressCombatState.BallistaReloadCrank;
        bool loading = enemy.CombatState is
            DemonFortressCombatState.BallistaReloadReach or
            DemonFortressCombatState.BallistaReloadLoad or
            DemonFortressCombatState.BallistaReloadTension;
        bool shove = enemy.CombatState is
            DemonFortressCombatState.BallistaShoveWindup or
            DemonFortressCombatState.BallistaShoveActive or
            DemonFortressCombatState.BallistaShoveRecovery;
        float recoil = recoilState
            ? MathF.Sin(progress * MathHelper.Pi) * 13f
            : firing ? 8f : 0f;
        float tension = enemy.CombatState == DemonFortressCombatState.BallistaReloadTension
            ? MathHelper.Lerp(.25f, .88f, progress)
            : enemy.IsBallistaLoaded
                ? warning ? .96f + MathF.Sin(time * 18f) * .02f : .86f
                : 0f;
        float crankAngle = cranking ? progress * MathHelper.TwoPi * 2f : .55f;
        Vector2 crankCenterLocal = new(-27f, -36f);
        Vector2 crankHandleLocal = crankCenterLocal + new Vector2(
            MathF.Cos(crankAngle) * 15f,
            MathF.Sin(crankAngle) * 12f);
        BallistaOperatorPose operatorPose = GetBallistaOperatorPose(
            enemy, progress, crankHandleLocal);

        DrawBallistaOperatorLegs(
            anchor, operatorPose, flip, operatorPlate, leather, iron);
        DrawRebuiltBallistaBase(
            anchor, flip, wood, darkWood, iron, steel, phaseTwo);

        Vector2 pivotLocal = new(-3f - recoil, -57f);
        Vector2 muzzleLocal = new(98f - recoil,
            -61f + enemy.BallistaAimVerticalOffset);
        DrawRebuiltBallistaMechanism(
            anchor, pivotLocal, muzzleLocal, crankCenterLocal, crankHandleLocal,
            flip, wood, darkWood, iron, steel, tension,
            enemy.IsBallistaLoaded, firing, progress, phaseTwo);

        DrawBallistaOperatorUpperBody(
            anchor, operatorPose, flip, operatorPlate, leather, iron, steel,
            phaseTwo, shove, progress);

        if (warning)
        {
            Vector2 muzzle = World(anchor, muzzleLocal, flip);
            Vector2 pivot = World(anchor, pivotLocal, flip);
            Vector2 lane = muzzle - pivot;
            if (lane.LengthSquared() < .01f)
                lane = flip ? -Vector2.UnitX : Vector2.UnitX;
            lane.Normalize();
            byte laneAlpha = (byte)(108 +
                (int)(66f * MathF.Abs(MathF.Sin(time * 10f))));
            DrawLine(muzzle, muzzle + lane * 210f,
                new Color((byte)224, (byte)74, (byte)48, laneAlpha), 2);
        }
    }

    private static BallistaOperatorPose GetBallistaOperatorPose(
        DemonFortressEnemy enemy,
        float progress,
        Vector2 crankHandle)
    {
        BallistaOperatorPose pose = new()
        {
            Hip = new Vector2(-39f, -31f),
            Chest = new Vector2(-40f, -60f),
            Head = new Vector2(-41f, -83f),
            RearKnee = new Vector2(-51f, -16f),
            RearFoot = new Vector2(-58f, -2f),
            FrontKnee = new Vector2(-27f, -15f),
            FrontFoot = new Vector2(-19f, -2f),
            RearElbow = new Vector2(-31f, -53f),
            RearHand = new Vector2(-16f, -55f),
            FrontElbow = new Vector2(-20f, -48f),
            FrontHand = new Vector2(-5f, -55f)
        };

        switch (enemy.CombatState)
        {
            case DemonFortressCombatState.BallistaSetup:
                pose.Chest += new Vector2(-4f, 0f);
                pose.Head += new Vector2(-5f, 0f);
                pose.RearElbow = new Vector2(-37f, -48f);
                pose.RearHand = new Vector2(-22f, -52f);
                pose.FrontElbow = new Vector2(-24f, -46f);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(-18f, -45f), new Vector2(-5f, -55f), progress);
                break;
            case DemonFortressCombatState.BallistaAcquire:
            case DemonFortressCombatState.BallistaAim:
                pose.Hip += new Vector2(2f, 1f);
                pose.Chest += new Vector2(5f, 2f);
                pose.Head += new Vector2(6f, 1f);
                pose.RearElbow = new Vector2(-28f, -54f);
                pose.RearHand = new Vector2(-12f, -57f);
                pose.FrontElbow = new Vector2(-17f, -48f);
                pose.FrontHand = new Vector2(-1f, -55f);
                break;
            case DemonFortressCombatState.BallistaWarning:
                pose.Hip += new Vector2(4f, 3f);
                pose.Chest += new Vector2(7f, 4f);
                pose.Head += new Vector2(7f, 3f);
                pose.RearKnee += new Vector2(2f, 1f);
                pose.FrontKnee += new Vector2(3f, 2f);
                pose.RearElbow = new Vector2(-23f, -54f);
                pose.RearHand = new Vector2(-8f, -58f);
                pose.FrontElbow = new Vector2(-14f, -49f);
                pose.FrontHand = new Vector2(2f, -55f);
                break;
            case DemonFortressCombatState.BallistaFire:
            case DemonFortressCombatState.BallistaRecoil:
                pose.Hip += new Vector2(-5f, 2f);
                pose.Chest += new Vector2(-10f, 0f);
                pose.Head += new Vector2(-12f, -1f);
                pose.RearKnee += new Vector2(-4f, 1f);
                pose.RearFoot.X -= 5f;
                pose.RearElbow = new Vector2(-38f, -51f);
                pose.RearHand = new Vector2(-22f, -55f);
                pose.FrontElbow = new Vector2(-29f, -45f);
                pose.FrontHand = new Vector2(-12f, -49f);
                break;
            case DemonFortressCombatState.BallistaReloadReach:
                pose.Hip += new Vector2(3f, 2f);
                pose.Chest += new Vector2(7f, 4f);
                pose.Head += new Vector2(7f, 3f);
                pose.RearElbow = new Vector2(-24f, -48f);
                pose.RearHand = new Vector2(-12f, -42f);
                pose.FrontElbow = new Vector2(-16f, -49f);
                pose.FrontHand = new Vector2(-2f, -43f);
                break;
            case DemonFortressCombatState.BallistaReloadCrank:
                pose.Hip += new Vector2(1f, 3f);
                pose.Chest += new Vector2(4f, 4f);
                pose.Head += new Vector2(4f, 3f);
                pose.RearElbow = new Vector2(-36f, -43f);
                pose.RearHand = new Vector2(-26f, -48f);
                pose.FrontElbow = Vector2.Lerp(
                    pose.Chest + new Vector2(15f, 7f), crankHandle, .48f);
                pose.FrontHand = crankHandle;
                break;
            case DemonFortressCombatState.BallistaReloadLoad:
                pose.Hip += new Vector2(7f, 3f);
                pose.Chest += new Vector2(12f, 4f);
                pose.Head += new Vector2(12f, 3f);
                pose.RearElbow = new Vector2(-17f, -59f);
                pose.RearHand = new Vector2(-2f, -61f);
                pose.FrontElbow = new Vector2(-8f, -53f);
                pose.FrontHand = new Vector2(9f, -59f);
                break;
            case DemonFortressCombatState.BallistaReloadTension:
                pose.Hip += new Vector2(3f, 3f);
                pose.Chest += new Vector2(7f, 4f);
                pose.Head += new Vector2(7f, 3f);
                pose.RearElbow = new Vector2(-27f, -46f);
                pose.RearHand = new Vector2(-18f, -38f);
                pose.FrontElbow = new Vector2(-13f, -48f);
                pose.FrontHand = new Vector2(1f, -53f);
                break;
            case DemonFortressCombatState.BallistaReady:
                pose.RearHand = new Vector2(-13f, -57f);
                pose.FrontHand = new Vector2(-1f, -55f);
                break;
            case DemonFortressCombatState.BallistaShoveWindup:
                pose.Hip += new Vector2(-3f, 2f);
                pose.Chest += new Vector2(-6f, 1f);
                pose.Head += new Vector2(-5f, 0f);
                pose.FrontElbow = new Vector2(-20f, -56f);
                pose.FrontHand = new Vector2(-10f, -62f);
                break;
            case DemonFortressCombatState.BallistaShoveActive:
                pose.Hip += new Vector2(9f, 3f);
                pose.Chest += new Vector2(16f, 4f);
                pose.Head += new Vector2(14f, 3f);
                pose.FrontKnee += new Vector2(10f, 1f);
                pose.FrontFoot += new Vector2(15f, 0f);
                pose.RearElbow = new Vector2(-5f, -50f);
                pose.RearHand = new Vector2(9f, -49f);
                pose.FrontElbow = new Vector2(13f, -48f);
                pose.FrontHand = new Vector2(42f, -44f);
                break;
            case DemonFortressCombatState.BallistaShoveRecovery:
                pose.Hip += new Vector2(6f, 3f);
                pose.Chest += new Vector2(10f, 4f);
                pose.Head += new Vector2(9f, 3f);
                pose.FrontKnee += new Vector2(7f, 1f);
                pose.FrontFoot += new Vector2(11f, 0f);
                pose.FrontElbow = new Vector2(8f, -47f);
                pose.FrontHand = new Vector2(30f, -43f);
                break;
        }

        if (enemy.IsStaggered)
        {
            pose.Chest += new Vector2(-8f, 4f);
            pose.Head += new Vector2(-10f, 5f);
            pose.RearElbow += new Vector2(-6f, 4f);
            pose.RearHand += new Vector2(-6f, 5f);
            pose.FrontElbow += new Vector2(-6f, 4f);
            pose.FrontHand += new Vector2(-6f, 5f);
        }
        return pose;
    }

    private void DrawRebuiltBallistaBase(
        Vector2 anchor,
        bool flip,
        Color wood,
        Color darkWood,
        Color iron,
        Color steel,
        bool phaseTwo)
    {
        Vector2 rearShoe = new(-69f, -3f);
        Vector2 frontShoe = new(69f, -3f);
        DrawLine(World(anchor, rearShoe, flip), World(anchor, frontShoe, flip),
            Outline, 15);
        DrawLine(World(anchor, rearShoe + new Vector2(3f, -2f), flip),
            World(anchor, frontShoe + new Vector2(-3f, -2f), flip), darkWood, 9);
        DrawLine(World(anchor, new Vector2(-58f, -1f), flip),
            World(anchor, new Vector2(-48f, -13f), flip), iron, 8);
        DrawLine(World(anchor, new Vector2(58f, -1f), flip),
            World(anchor, new Vector2(49f, -13f), flip), iron, 8);
        DrawLine(World(anchor, new Vector2(-48f, -9f), flip),
            World(anchor, new Vector2(-5f, -53f), flip), Outline, 13);
        DrawLine(World(anchor, new Vector2(-47f, -9f), flip),
            World(anchor, new Vector2(-4f, -52f), flip), wood, 8);
        DrawLine(World(anchor, new Vector2(48f, -9f), flip),
            World(anchor, new Vector2(-1f, -52f), flip), Outline, 13);
        DrawLine(World(anchor, new Vector2(47f, -9f), flip),
            World(anchor, new Vector2(0f, -51f), flip), wood, 8);
        DrawLine(World(anchor, new Vector2(-3f, -51f), flip),
            World(anchor, new Vector2(-3f, -12f), flip), Outline, 19);
        DrawLine(World(anchor, new Vector2(-3f, -49f), flip),
            World(anchor, new Vector2(-3f, -13f), flip), iron, 13);
        DrawBox(anchor, -11f, -62f, 17f, 17f, Outline, flip);
        DrawBox(anchor, -8f, -59f, 11f, 11f, steel, flip);
        if (phaseTwo)
        {
            DrawLine(World(anchor, new Vector2(-32f, -10f), flip),
                World(anchor, new Vector2(-18f, -10f), flip),
                new Color(118, 37, 37), 3);
        }
    }

    private void DrawRebuiltBallistaMechanism(
        Vector2 anchor,
        Vector2 pivotLocal,
        Vector2 muzzleLocal,
        Vector2 crankCenterLocal,
        Vector2 crankHandleLocal,
        bool flip,
        Color wood,
        Color darkWood,
        Color iron,
        Color steel,
        float tension,
        bool loaded,
        bool firing,
        float progress,
        bool phaseTwo)
    {
        Vector2 pivot = World(anchor, pivotLocal, flip);
        Vector2 muzzle = World(anchor, muzzleLocal, flip);
        Vector2 axis = muzzle - pivot;
        if (axis.LengthSquared() < .01f)
            axis = flip ? -Vector2.UnitX : Vector2.UnitX;
        axis.Normalize();
        Vector2 normal = new(-axis.Y, axis.X);
        Vector2 rear = pivot - axis * 52f;
        Vector2 bowCenter = pivot + axis * 18f;
        Vector2 upperJoint = bowCenter - axis * 4f - normal * 16f;
        Vector2 lowerJoint = bowCenter - axis * 4f + normal * 16f;
        Vector2 upperTip = bowCenter + axis * 5f - normal * 43f;
        Vector2 lowerTip = bowCenter + axis * 5f + normal * 43f;

        // Central rail and reinforced spine establish the firing direction.
        DrawLine(rear, muzzle + axis * 8f, Outline, 14);
        DrawLine(rear + normal * 1f, muzzle + axis * 8f + normal * 1f,
            darkWood, 9);
        DrawLine(pivot - axis * 13f, muzzle + axis * 4f, iron, 3);
        DrawLine(bowCenter, upperJoint, Outline, 13);
        DrawLine(upperJoint, upperTip, Outline, 11);
        DrawLine(bowCenter, lowerJoint, Outline, 13);
        DrawLine(lowerJoint, lowerTip, Outline, 11);
        DrawLine(bowCenter, upperJoint, wood, 8);
        DrawLine(upperJoint, upperTip, wood, 7);
        DrawLine(bowCenter, lowerJoint, wood, 8);
        DrawLine(lowerJoint, lowerTip, wood, 7);
        DrawLine(upperJoint - axis * 1f, upperTip, steel, 2);
        DrawLine(lowerJoint - axis * 1f, lowerTip, steel, 2);

        Vector2 stringNotch = bowCenter - axis * (8f + tension * 43f);
        Color cord = new(205, 194, 169);
        DrawLine(upperTip, stringNotch, cord, 2);
        DrawLine(stringNotch, lowerTip, cord, 2);
        if (loaded)
        {
            Vector2 boltRear = stringNotch - axis * 6f;
            Vector2 boltTip = muzzle + axis * 16f;
            DrawLine(boltRear, boltTip, Outline, 7);
            DrawLine(boltRear, boltTip, new Color(174, 169, 157), 4);
            DrawLine(boltTip, boltTip - axis * 10f - normal * 7f, steel, 3);
            DrawLine(boltTip, boltTip - axis * 10f + normal * 7f, steel, 3);
            DrawLine(boltRear, boltRear - axis * 9f - normal * 6f, wood, 3);
            DrawLine(boltRear, boltRear - axis * 9f + normal * 6f, wood, 3);
        }
        else if (firing)
        {
            Vector2 shotTip = muzzle + axis * (35f + progress * 95f);
            byte shotAlpha = (byte)MathHelper.Lerp(235f, 75f, progress);
            DrawLine(muzzle + axis * 8f, shotTip,
                new Color((byte)188, (byte)183, (byte)171, shotAlpha), 5);
            DrawLine(shotTip, shotTip - axis * 9f - normal * 6f,
                new Color(steel.R, steel.G, steel.B, shotAlpha), 3);
            DrawLine(shotTip, shotTip - axis * 9f + normal * 6f,
                new Color(steel.R, steel.G, steel.B, shotAlpha), 3);
        }

        Vector2 crankCenter = World(anchor, crankCenterLocal, flip);
        Vector2 crankHandle = World(anchor, crankHandleLocal, flip);
        DrawBox(anchor, crankCenterLocal.X - 6f, crankCenterLocal.Y - 6f,
            12f, 12f, Outline, flip);
        DrawLine(crankCenter, crankHandle, iron, 4);
        DrawBox(anchor, crankHandleLocal.X - 3f, crankHandleLocal.Y - 3f,
            6f, 6f, steel, flip);
        if (phaseTwo)
        {
            DrawLine(pivot + axis * 9f - normal * 6f,
                pivot + axis * 25f - normal * 6f,
                new Color(126, 39, 39), 2);
        }
    }

    private void DrawBallistaOperatorLegs(
        Vector2 anchor,
        BallistaOperatorPose pose,
        bool flip,
        Color plate,
        Color leather,
        Color iron)
    {
        DrawBallistaOperatorLeg(anchor, pose.Hip + new Vector2(-7f, 1f),
            pose.RearKnee, pose.RearFoot, flip, leather, iron);
        DrawBallistaOperatorLeg(anchor, pose.Hip + new Vector2(8f, 1f),
            pose.FrontKnee, pose.FrontFoot, flip, plate, iron);
    }

    private void DrawBallistaOperatorLeg(
        Vector2 anchor,
        Vector2 hip,
        Vector2 knee,
        Vector2 foot,
        bool flip,
        Color upper,
        Color lower)
    {
        Vector2 ankle = Vector2.Lerp(knee, foot, .78f);
        DrawLine(World(anchor, hip, flip), World(anchor, knee, flip),
            Outline, 12);
        DrawLine(World(anchor, hip, flip), World(anchor, knee, flip), upper, 8);
        DrawLine(World(anchor, knee, flip), World(anchor, ankle, flip),
            Outline, 11);
        DrawLine(World(anchor, knee, flip), World(anchor, ankle, flip), lower, 7);
        DrawLine(World(anchor, knee + new Vector2(-5f, 0f), flip),
            World(anchor, knee + new Vector2(5f, 0f), flip), lower, 5);
        DrawLine(World(anchor, ankle, flip),
            World(anchor, foot + new Vector2(7f, 0f), flip), Outline, 8);
    }

    private void DrawBallistaOperatorUpperBody(
        Vector2 anchor,
        BallistaOperatorPose pose,
        bool flip,
        Color plate,
        Color leather,
        Color iron,
        Color steel,
        bool phaseTwo,
        bool shove,
        float progress)
    {
        Vector2 rearShoulder = pose.Chest + new Vector2(-13f, 0f);
        Vector2 frontShoulder = pose.Chest + new Vector2(14f, 2f);
        DrawLine(World(anchor, pose.Chest, flip), World(anchor, pose.Hip, flip),
            Outline, 31);
        DrawLine(World(anchor, pose.Chest, flip), World(anchor, pose.Hip, flip),
            plate, 24);
        DrawLine(World(anchor, pose.Chest + new Vector2(-10f, 3f), flip),
            World(anchor, pose.Hip + new Vector2(8f, -3f), flip), leather, 4);
        DrawLine(World(anchor, pose.Chest + new Vector2(11f, 4f), flip),
            World(anchor, pose.Hip + new Vector2(-7f, -3f), flip), leather, 4);
        DrawLine(World(anchor, pose.Hip + new Vector2(-13f, -3f), flip),
            World(anchor, pose.Hip + new Vector2(14f, -3f), flip), iron, 6);
        DrawBox(anchor, pose.Hip.X - 3f, pose.Hip.Y - 6f, 7f, 7f,
            phaseTwo ? new Color(118, 37, 37) : steel, flip);

        DrawBallistaOperatorArm(anchor, rearShoulder, pose.RearElbow,
            pose.RearHand, flip, plate, leather, iron);
        DrawBallistaOperatorArm(anchor, frontShoulder, pose.FrontElbow,
            pose.FrontHand, flip, iron, leather, steel);

        // Practical siege helm: low brim, dark face, reinforced rear guard.
        DrawLine(World(anchor, pose.Head + new Vector2(0f, -8f), flip),
            World(anchor, pose.Head + new Vector2(0f, 8f), flip), Outline, 23);
        DrawLine(World(anchor, pose.Head + new Vector2(0f, -7f), flip),
            World(anchor, pose.Head + new Vector2(0f, 7f), flip), iron, 18);
        DrawLine(World(anchor, pose.Head + new Vector2(-12f, -3f), flip),
            World(anchor, pose.Head + new Vector2(12f, -1f), flip), steel, 5);
        DrawLine(World(anchor, pose.Head + new Vector2(-8f, 4f), flip),
            World(anchor, pose.Head + new Vector2(8f, 5f), flip),
            new Color(27, 25, 27), 5);
        DrawLine(World(anchor, pose.Head + new Vector2(-8f, 7f), flip),
            World(anchor, pose.Head + new Vector2(-12f, 14f), flip), iron, 7);

        if (shove)
        {
            float extension = enemyShoveExtension(progress);
            Vector2 hand = World(anchor, pose.FrontHand, flip);
            Vector2 direction = flip ? -Vector2.UnitX : Vector2.UnitX;
            DrawLine(hand - direction * 7f, hand + direction * extension,
                Outline, 7);
            DrawLine(hand - direction * 5f, hand + direction * extension,
                iron, 4);
        }
    }

    private static float enemyShoveExtension(float progress) =>
        29f + MathF.Sin(progress * MathHelper.Pi) * 9f;

    private void DrawBallistaOperatorArm(
        Vector2 anchor,
        Vector2 shoulder,
        Vector2 elbow,
        Vector2 hand,
        bool flip,
        Color upper,
        Color forearm,
        Color glove)
    {
        DrawLine(World(anchor, shoulder, flip), World(anchor, elbow, flip),
            Outline, 10);
        DrawLine(World(anchor, shoulder, flip), World(anchor, elbow, flip),
            upper, 7);
        DrawLine(World(anchor, elbow, flip), World(anchor, hand, flip),
            Outline, 9);
        DrawLine(World(anchor, elbow, flip), World(anchor, hand, flip),
            forearm, 6);
        DrawBox(anchor, hand.X - 4f, hand.Y - 4f, 8f, 8f, glove, flip);
    }

    private void DrawRebuiltBallistaDeath(
        DemonFortressEnemy enemy,
        Vector2 anchor,
        bool flip,
        Color wood,
        Color darkWood,
        Color iron,
        Color operatorPlate)
    {
        float progress = enemy.DeathPresentationProgress;
        byte alpha = (byte)MathHelper.Lerp(235f, 0f,
            Math.Clamp((progress - .78f) / .22f, 0f, 1f));
        Color outline = new(Outline.R, Outline.G, Outline.B, alpha);
        Color fadedWood = new(wood.R, wood.G, wood.B, alpha);
        Color fadedDarkWood = new(darkWood.R, darkWood.G, darkWood.B, alpha);
        Color fadedIron = new(iron.R, iron.G, iron.B, alpha);
        Color fadedPlate = new(operatorPlate.R, operatorPlate.G,
            operatorPlate.B, alpha);
        DrawLine(World(anchor, new Vector2(-70f, -3f), flip),
            World(anchor, new Vector2(69f, -3f), flip), outline, 14);
        DrawLine(World(anchor, new Vector2(-67f, -5f), flip),
            World(anchor, new Vector2(66f, -5f), flip), fadedDarkWood, 8);
        DrawLine(World(anchor, new Vector2(-49f, -9f), flip),
            World(anchor, new Vector2(-4f, -50f), flip), fadedWood, 8);
        DrawLine(World(anchor, new Vector2(47f, -9f), flip),
            World(anchor, new Vector2(-1f, -50f), flip), fadedWood, 8);
        DrawLine(World(anchor, new Vector2(-50f, -55f), flip),
            World(anchor, new Vector2(91f, -48f + progress * 7f), flip),
            outline, 13);
        DrawLine(World(anchor, new Vector2(-49f, -54f), flip),
            World(anchor, new Vector2(89f, -47f + progress * 7f), flip),
            fadedWood, 8);
        DrawLine(World(anchor, new Vector2(10f, -91f), flip),
            World(anchor, new Vector2(21f, -19f), flip), fadedWood, 7);
        DrawLine(World(anchor, new Vector2(10f, -91f), flip),
            World(anchor, new Vector2(-34f, -49f), flip), fadedIron, 2);
        DrawLine(World(anchor, new Vector2(21f, -19f), flip),
            World(anchor, new Vector2(-34f, -49f), flip), fadedIron, 2);

        Vector2 bodyRear = new(-60f, -10f);
        Vector2 bodyFront = new(-18f, -20f + progress * 8f);
        DrawLine(World(anchor, bodyRear, flip), World(anchor, bodyFront, flip),
            outline, 27);
        DrawLine(World(anchor, bodyRear, flip), World(anchor, bodyFront, flip),
            fadedPlate, 20);
        DrawLine(World(anchor, new Vector2(-67f, -3f), flip),
            World(anchor, new Vector2(-47f, -12f), flip), fadedIron, 10);
        DrawLine(World(anchor, new Vector2(-28f, -13f), flip),
            World(anchor, new Vector2(-4f, -3f), flip), fadedIron, 10);
        DrawBox(anchor, bodyFront.X - 2f, bodyFront.Y - 12f, 20f, 18f,
            fadedIron, flip);
    }

    private struct ExecutionerPose
    {
        public Vector2 Hip;
        public Vector2 Chest;
        public Vector2 Head;
        public Vector2 RearKnee;
        public Vector2 RearFoot;
        public Vector2 FrontKnee;
        public Vector2 FrontFoot;
        public Vector2 RearElbow;
        public Vector2 FrontElbow;
        public Vector2 LowerHand;
        public Vector2 UpperHand;
        public float ShoulderTurn;
        public float MantleLag;
        public float HelmetTurn;
    }

    private void DrawBlackGuardExecutioner(DemonFortressEnemy enemy, float time)
    {
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState is not DemonFortressCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Color blackPlate = enemy.IsHitFlashing
            ? new Color(238, 224, 207)
            : new Color(34, 36, 42);
        Color ironEdge = new(70, 73, 80);
        Color steelEdge = new(111, 113, 119);
        Color rank = enemy.BossCombatPhase == 2
            ? new Color(151, 39, 43)
            : new Color(88, 29, 34);

        if (!enemy.IsAlive)
        {
            DrawRebuiltExecutionerDeath(
                enemy, anchor, flip, blackPlate, ironEdge, steelEdge, rank);
            return;
        }

        bool moving = enemy.CombatState == DemonFortressCombatState.Ready &&
            MathF.Abs(enemy.VisualVelocityX) > 3f;
        bool phaseTwo = enemy.BossCombatPhase == 2;
        ExecutionerPose pose = GetRebuiltExecutionerPose(enemy, time, moving);

        DrawRebuiltExecutionerWeaponTrail(enemy, anchor, pose, flip);
        DrawRebuiltExecutionerBody(
            anchor, pose, flip, blackPlate, ironEdge, steelEdge, rank,
            phaseTwo, enemy.IsExecutionerCountering, enemy.CombatState,
            enemy.ActionProgress, byte.MaxValue);
        DrawRebuiltExecutionAxe(
            anchor, pose.LowerHand, pose.UpperHand, flip,
            new Color(82, 85, 92), phaseTwo);
        DrawRebuiltExecutionerGrip(
            anchor, pose.LowerHand, pose.UpperHand, flip, steelEdge,
            bothHands: true);

        if (enemy.IsExecutionerCountering)
        {
            byte cueAlpha = (byte)(62 +
                (int)(32f * MathF.Abs(MathF.Sin(time * 6f))));
            DrawLine(World(anchor, pose.LowerHand, flip),
                World(anchor, pose.UpperHand, flip),
                new Color((byte)132, (byte)34, (byte)39, cueAlpha), 3);
        }

        if (phaseTwo && moving && MathF.Abs(MathF.Sin(time * 4.2f)) > .88f)
        {
            Vector2 planted = MathF.Sin(time * 4.2f) > 0f
                ? pose.FrontFoot
                : pose.RearFoot;
            DrawLine(World(anchor, planted + new Vector2(-9f, 1f), flip),
                World(anchor, planted + new Vector2(10f, 1f), flip),
                new Color((byte)61, (byte)53, (byte)49, (byte)90), 2);
        }

        if (enemy.CombatState == DemonFortressCombatState.ExecutionerJudgmentImpact)
        {
            DrawLine(World(anchor, new Vector2(-58f, -2f), flip),
                World(anchor, new Vector2(62f, -2f), flip),
                new Color((byte)89, (byte)76, (byte)68, (byte)175), 5);
            DrawBox(anchor, -39f, -10f, 8f, 5f, new Color(112, 96, 78), flip);
            DrawBox(anchor, -10f, -14f, 6f, 7f, new Color(82, 74, 68), flip);
            DrawBox(anchor, 31f, -12f, 9f, 6f, new Color(126, 97, 74), flip);
            DrawLine(World(anchor, new Vector2(18f, -6f), flip),
                World(anchor, new Vector2(26f, -17f), flip), rank, 2);
        }
    }

    private static ExecutionerPose CreateExecutionerBasePose(
        float time,
        bool moving,
        bool phaseTwo)
    {
        float stride = moving ? MathF.Sin(time * 4.2f) * 8f : 0f;
        float settle = moving ? MathF.Abs(MathF.Sin(time * 4.2f)) * 1.4f : 0f;
        float aggression = phaseTwo ? 3f : 0f;
        ExecutionerPose pose = new()
        {
            Hip = new Vector2(0f, -59f + settle),
            Chest = new Vector2(2f + aggression - stride * .10f, -98f + settle),
            Head = new Vector2(4f + aggression - stride * .08f, -133f + settle),
            RearKnee = new Vector2(-16f - stride * .45f, -33f + settle),
            RearFoot = new Vector2(-22f - stride, -2f),
            FrontKnee = new Vector2(16f + stride * .45f, -31f + settle),
            FrontFoot = new Vector2(25f + stride, -2f),
            RearElbow = new Vector2(-8f - stride * .10f, -90f + settle),
            FrontElbow = new Vector2(43f - stride * .12f, -88f + settle),
            LowerHand = new Vector2(32f - stride * .18f, -98f + settle),
            UpperHand = new Vector2(39f - stride * .24f, -78f + settle),
            ShoulderTurn = 2f,
            MantleLag = moving ? -8f - stride * .45f : -3f,
            HelmetTurn = 1f
        };

        if (phaseTwo)
        {
            pose.RearFoot.X -= 3f;
            pose.FrontFoot.X += 3f;
            pose.FrontKnee.X += 2f;
            pose.MantleLag *= 1.16f;
        }
        return pose;
    }

    private static ExecutionerPose GetRebuiltExecutionerPose(
        DemonFortressEnemy enemy,
        float time,
        bool moving)
    {
        float p = enemy.ActionProgress;
        bool phaseTwo = enemy.BossCombatPhase == 2;
        ExecutionerPose pose = CreateExecutionerBasePose(time, moving, phaseTwo);

        switch (enemy.CombatState)
        {
            case DemonFortressCombatState.ExecutionerIntro:
            {
                float turn = MathHelper.SmoothStep(0f, 1f,
                    Math.Clamp(p / .32f, 0f, 1f));
                float reach = MathHelper.SmoothStep(0f, 1f,
                    Math.Clamp((p - .20f) / .50f, 0f, 1f));
                float settle = MathHelper.SmoothStep(0f, 1f,
                    Math.Clamp((p - .45f) / .45f, 0f, 1f));
                pose.HelmetTurn = turn;
                pose.Head.X = MathHelper.Lerp(-3f, 4f, turn);
                pose.RearFoot.X = MathHelper.Lerp(-16f, -22f, settle);
                pose.FrontFoot.X = MathHelper.Lerp(18f, 25f, settle);
                pose.FrontKnee.X = MathHelper.Lerp(12f, 16f, settle);
                pose.RearElbow = Vector2.Lerp(
                    new Vector2(-4f, -87f), new Vector2(-8f, -90f), reach);
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(34f, -79f), new Vector2(43f, -88f), reach);
                pose.LowerHand = Vector2.Lerp(
                    new Vector2(29f, -87f), new Vector2(32f, -98f), reach);
                pose.UpperHand = Vector2.Lerp(
                    new Vector2(32f, -60f), new Vector2(39f, -78f), reach);
                pose.MantleLag = -2f;
                break;
            }
            case DemonFortressCombatState.ExecutionerPhaseTransition:
                pose.Hip += new Vector2(4f, 3f);
                pose.Chest += new Vector2(8f, 1f);
                pose.Head += new Vector2(9f, 0f);
                pose.RearKnee = new Vector2(-21f, -34f);
                pose.RearFoot = new Vector2(-30f, -2f);
                pose.FrontKnee = new Vector2(22f, -31f);
                pose.FrontFoot = new Vector2(33f, -2f);
                pose.RearElbow = new Vector2(-5f, -96f);
                pose.FrontElbow = new Vector2(46f, -91f);
                pose.LowerHand = new Vector2(30f, -105f);
                pose.UpperHand = new Vector2(43f, -85f);
                pose.ShoulderTurn = 4f;
                pose.MantleLag = -9f - 5f * MathF.Sin(p * MathHelper.Pi);
                break;
            case DemonFortressCombatState.ExecutionerCleaveWindup:
            case DemonFortressCombatState.ExecutionerReverseTell:
                pose.Hip += new Vector2(-5f, 1f);
                pose.Chest += new Vector2(-11f, -1f);
                pose.Head += new Vector2(-9f, 0f);
                pose.RearKnee = new Vector2(-23f, -35f);
                pose.RearFoot = new Vector2(-31f, -2f);
                pose.FrontKnee = new Vector2(12f, -31f);
                pose.FrontFoot = new Vector2(22f, -2f);
                pose.RearElbow = new Vector2(-31f, -91f);
                pose.FrontElbow = new Vector2(-40f, -76f);
                pose.LowerHand = new Vector2(-27f, -105f);
                pose.UpperHand = new Vector2(-48f, -92f);
                pose.ShoulderTurn = -6f;
                pose.MantleLag = -15f;
                break;
            case DemonFortressCombatState.ExecutionerCleaveActive:
            case DemonFortressCombatState.ExecutionerReverseActive:
                pose.Hip += new Vector2(7f, 3f);
                pose.Chest += new Vector2(14f, 4f);
                pose.Head += new Vector2(12f, 3f);
                pose.RearKnee = new Vector2(-20f, -31f);
                pose.RearFoot = new Vector2(-29f, -2f);
                pose.FrontKnee = new Vector2(24f, -29f);
                pose.FrontFoot = new Vector2(35f, -2f);
                pose.RearElbow = new Vector2(12f, -83f);
                pose.FrontElbow = new Vector2(48f, -77f);
                pose.LowerHand = new Vector2(19f, -89f);
                pose.UpperHand = new Vector2(44f, -73f + p * 5f);
                pose.ShoulderTurn = 8f;
                pose.MantleLag = -24f;
                break;
            case DemonFortressCombatState.ExecutionerCleaveRecovery:
            case DemonFortressCombatState.ExecutionerReverseRecovery:
                pose.Hip += new Vector2(5f, 4f);
                pose.Chest += new Vector2(9f, 6f);
                pose.Head += new Vector2(7f, 5f);
                pose.RearKnee = new Vector2(-18f, -30f);
                pose.FrontKnee = new Vector2(23f, -27f);
                pose.FrontFoot = new Vector2(34f, -2f);
                pose.RearElbow = new Vector2(14f, -75f);
                pose.FrontElbow = new Vector2(49f, -63f);
                pose.LowerHand = new Vector2(20f, -78f);
                pose.UpperHand = new Vector2(45f, -59f);
                pose.ShoulderTurn = 6f;
                pose.MantleLag = -14f * (1f - p);
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakWindup:
                pose.Hip += new Vector2(-2f, 4f);
                pose.Chest += new Vector2(-5f, 3f);
                pose.Head += new Vector2(-3f, 2f);
                pose.RearKnee = new Vector2(-21f, -29f);
                pose.RearFoot = new Vector2(-28f, -2f);
                pose.FrontKnee = new Vector2(18f, -27f);
                pose.FrontFoot = new Vector2(27f, -2f);
                pose.RearElbow = new Vector2(1f, -92f);
                pose.FrontElbow = new Vector2(37f, -82f);
                pose.LowerHand = new Vector2(23f, -103f);
                pose.UpperHand = new Vector2(31f, -83f);
                pose.ShoulderTurn = -2f;
                pose.MantleLag = -7f;
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakActive:
                pose.Hip += new Vector2(10f, 7f);
                pose.Chest += new Vector2(17f, 8f);
                pose.Head += new Vector2(15f, 7f);
                pose.RearKnee = new Vector2(-14f, -28f);
                pose.RearFoot = new Vector2(-25f, -2f);
                pose.FrontKnee = new Vector2(30f, -26f);
                pose.FrontFoot = new Vector2(42f, -2f);
                pose.RearElbow = new Vector2(17f, -79f);
                pose.FrontElbow = new Vector2(52f, -70f);
                pose.LowerHand = new Vector2(31f, -85f);
                pose.UpperHand = new Vector2(50f, -72f);
                pose.ShoulderTurn = 7f;
                pose.MantleLag = -17f;
                break;
            case DemonFortressCombatState.ExecutionerArmorBreakRecovery:
                pose.Hip += new Vector2(8f, 7f);
                pose.Chest += new Vector2(13f, 9f);
                pose.Head += new Vector2(10f, 8f);
                pose.RearKnee = new Vector2(-15f, -28f);
                pose.FrontKnee = new Vector2(27f, -25f);
                pose.FrontFoot = new Vector2(38f, -2f);
                pose.RearElbow = new Vector2(17f, -72f);
                pose.FrontElbow = new Vector2(48f, -60f);
                pose.LowerHand = new Vector2(23f, -75f);
                pose.UpperHand = new Vector2(45f, -58f);
                pose.ShoulderTurn = 5f;
                pose.MantleLag = -11f * (1f - p);
                break;
            case DemonFortressCombatState.ExecutionerCounterEnter:
            case DemonFortressCombatState.ExecutionerCounterWindow:
            case DemonFortressCombatState.ExecutionerCounterRetaliateWindup:
                pose.Hip += new Vector2(-2f, 3f);
                pose.Chest += new Vector2(-5f, 1f);
                pose.Head += new Vector2(4f, 0f);
                pose.RearKnee = new Vector2(-24f, -29f);
                pose.RearFoot = new Vector2(-33f, -2f);
                pose.FrontKnee = new Vector2(25f, -28f);
                pose.FrontFoot = new Vector2(35f, -2f);
                pose.RearElbow = new Vector2(-9f, -98f);
                pose.FrontElbow = new Vector2(42f, -99f);
                pose.LowerHand = new Vector2(12f, -103f);
                pose.UpperHand = new Vector2(34f, -89f);
                pose.ShoulderTurn = 7f;
                pose.MantleLag = -5f;
                break;
            case DemonFortressCombatState.ExecutionerCounterRetaliateActive:
                pose.Hip += new Vector2(7f, 4f);
                pose.Chest += new Vector2(13f, 5f);
                pose.Head += new Vector2(11f, 4f);
                pose.RearKnee = new Vector2(-18f, -29f);
                pose.FrontKnee = new Vector2(26f, -27f);
                pose.FrontFoot = new Vector2(36f, -2f);
                pose.RearElbow = new Vector2(14f, -82f);
                pose.FrontElbow = new Vector2(51f, -72f);
                pose.LowerHand = new Vector2(20f, -88f);
                pose.UpperHand = new Vector2(48f, -70f);
                pose.ShoulderTurn = 8f;
                pose.MantleLag = -20f;
                break;
            case DemonFortressCombatState.ExecutionerCounterRecovery:
                pose.Hip += new Vector2(5f, 5f);
                pose.Chest += new Vector2(9f, 7f);
                pose.Head += new Vector2(7f, 6f);
                pose.RearKnee = new Vector2(-19f, -29f);
                pose.FrontKnee = new Vector2(24f, -27f);
                pose.FrontFoot = new Vector2(34f, -2f);
                pose.RearElbow = new Vector2(14f, -75f);
                pose.FrontElbow = new Vector2(48f, -63f);
                pose.LowerHand = new Vector2(21f, -79f);
                pose.UpperHand = new Vector2(44f, -61f);
                pose.ShoulderTurn = 5f;
                pose.MantleLag = -12f * (1f - p);
                break;
            case DemonFortressCombatState.ExecutionerJudgmentRaise:
            case DemonFortressCombatState.ExecutionerJudgmentHold:
                pose.Hip += new Vector2(0f, 2f);
                pose.Chest += new Vector2(0f, -3f);
                pose.Head += new Vector2(0f, -3f);
                pose.RearKnee = new Vector2(-23f, -30f);
                pose.RearFoot = new Vector2(-34f, -2f);
                pose.FrontKnee = new Vector2(24f, -30f);
                pose.FrontFoot = new Vector2(36f, -2f);
                pose.RearElbow = new Vector2(-20f, -119f);
                pose.FrontElbow = new Vector2(31f, -121f);
                pose.LowerHand = new Vector2(-3f, -124f);
                pose.UpperHand = new Vector2(12f, -140f);
                pose.ShoulderTurn = 1f;
                pose.MantleLag = -7f;
                break;
            case DemonFortressCombatState.ExecutionerJudgmentFall:
            case DemonFortressCombatState.ExecutionerJudgmentImpact:
                pose.Hip += new Vector2(6f, 12f);
                pose.Chest += new Vector2(12f, 14f);
                pose.Head += new Vector2(10f, 13f);
                pose.RearKnee = new Vector2(-24f, -24f);
                pose.RearFoot = new Vector2(-34f, -2f);
                pose.FrontKnee = new Vector2(28f, -23f);
                pose.FrontFoot = new Vector2(39f, -2f);
                pose.RearElbow = new Vector2(-1f, -65f);
                pose.FrontElbow = new Vector2(35f, -54f);
                pose.LowerHand = new Vector2(9f, -69f);
                pose.UpperHand = new Vector2(27f, -48f);
                pose.ShoulderTurn = 7f;
                pose.MantleLag = -15f;
                break;
            case DemonFortressCombatState.ExecutionerJudgmentRecovery:
                pose.Hip += new Vector2(5f, 10f - p * 5f);
                pose.Chest += new Vector2(10f, 12f - p * 6f);
                pose.Head += new Vector2(8f, 10f - p * 5f);
                pose.RearKnee = new Vector2(-22f, -25f);
                pose.RearFoot = new Vector2(-32f, -2f);
                pose.FrontKnee = new Vector2(27f, -23f);
                pose.FrontFoot = new Vector2(38f, -2f);
                pose.RearElbow = new Vector2(5f, -61f);
                pose.FrontElbow = new Vector2(39f, -50f);
                pose.LowerHand = new Vector2(12f, -65f);
                pose.UpperHand = new Vector2(30f, -44f);
                pose.ShoulderTurn = 5f;
                pose.MantleLag = -10f * (1f - p);
                break;
        }

        if (enemy.IsStaggered)
        {
            Vector2 upperShift = new(-10f, 5f);
            pose.Hip += new Vector2(-3f, 3f);
            pose.Chest += upperShift;
            pose.Head += upperShift + new Vector2(-2f, 2f);
            pose.RearElbow += upperShift;
            pose.FrontElbow += upperShift;
            pose.LowerHand += upperShift + new Vector2(-3f, 3f);
            pose.UpperHand += upperShift + new Vector2(-3f, 3f);
            pose.MantleLag = -12f;
        }
        return pose;
    }

    private void DrawRebuiltExecutionerBody(
        Vector2 anchor,
        ExecutionerPose pose,
        bool flip,
        Color blackPlate,
        Color ironEdge,
        Color steelEdge,
        Color rank,
        bool phaseTwo,
        bool counter,
        DemonFortressCombatState state,
        float progress,
        byte alpha)
    {
        Color body = WithExecutionerAlpha(blackPlate, alpha);
        Color edge = WithExecutionerAlpha(ironEdge, alpha);
        Color steel = WithExecutionerAlpha(steelEdge, alpha);
        Color accent = WithExecutionerAlpha(rank, alpha);
        Color undersuit = new((byte)24, (byte)24, (byte)29, alpha);
        Vector2 rearHip = pose.Hip + new Vector2(-10f, 0f);
        Vector2 frontHip = pose.Hip + new Vector2(11f, 0f);
        Vector2 rearShoulder = pose.Chest + new Vector2(-29f, -pose.ShoulderTurn);
        Vector2 frontShoulder = pose.Chest + new Vector2(29f, pose.ShoulderTurn);

        DrawRebuiltExecutionerMantle(
            anchor, pose, rearShoulder, flip, phaseTwo, alpha);
        DrawRebuiltExecutionerLeg(anchor, rearHip, pose.RearKnee,
            pose.RearFoot, flip, body, edge, steel, phaseTwo, alpha);
        DrawRebuiltExecutionerLeg(anchor, frontHip, pose.FrontKnee,
            pose.FrontFoot, flip, edge, body, steel, phaseTwo, alpha);

        // The rear arm sits behind the chest; the near arm remains fully readable.
        DrawRebuiltExecutionerArm(anchor, rearShoulder, pose.RearElbow,
            pose.LowerHand, flip, body, edge, steel, alpha);
        DrawRebuiltExecutionerTorso(anchor, pose, rearShoulder, frontShoulder,
            flip, body, edge, steel, accent, undersuit, phaseTwo, alpha);
        DrawRebuiltExecutionerHelmet(anchor, pose.Head, flip, body, edge,
            steel, accent, phaseTwo, counter, state, progress,
            pose.HelmetTurn, alpha);
        DrawRebuiltExecutionerArm(anchor, frontShoulder, pose.FrontElbow,
            pose.UpperHand, flip, edge, body, steel, alpha);
    }

    private void DrawRebuiltExecutionerTorso(
        Vector2 anchor,
        ExecutionerPose pose,
        Vector2 rearShoulder,
        Vector2 frontShoulder,
        bool flip,
        Color body,
        Color edge,
        Color steel,
        Color accent,
        Color undersuit,
        bool phaseTwo,
        byte alpha)
    {
        Vector2 waist = pose.Hip + new Vector2(0f, -8f);
        Vector2 chestBase = pose.Chest + new Vector2(0f, 7f);
        DrawLine(World(anchor, chestBase, flip), World(anchor, waist, flip),
            WithExecutionerAlpha(Outline, alpha), 46);
        DrawLine(World(anchor, chestBase, flip), World(anchor, waist, flip),
            undersuit, 40);
        DrawLine(World(anchor, pose.Chest + new Vector2(-20f, 2f), flip),
            World(anchor, pose.Chest + new Vector2(21f, 5f), flip), edge, 17);
        DrawLine(World(anchor, pose.Chest + new Vector2(-17f, 5f), flip),
            World(anchor, waist + new Vector2(-9f, -2f), flip), body, 20);
        DrawLine(World(anchor, pose.Chest + new Vector2(18f, 7f), flip),
            World(anchor, waist + new Vector2(10f, -2f), flip), body, 20);

        // A single descending ridge gives the chest an execution-blade motif.
        DrawLine(World(anchor, pose.Chest + new Vector2(1f, -2f), flip),
            World(anchor, waist + new Vector2(1f, -3f), flip), steel, 4);
        DrawLine(World(anchor, pose.Chest + new Vector2(-14f, 6f), flip),
            World(anchor, waist + new Vector2(0f, -1f), flip), edge, 3);
        DrawLine(World(anchor, pose.Chest + new Vector2(15f, 8f), flip),
            World(anchor, waist + new Vector2(0f, -1f), flip), edge, 3);

        // Two clean layered pauldrons leave the shoulder joints exposed.
        DrawLine(World(anchor, rearShoulder + new Vector2(-9f, -3f), flip),
            World(anchor, rearShoulder + new Vector2(10f, 4f), flip),
            WithExecutionerAlpha(Outline, alpha), 17);
        DrawLine(World(anchor, rearShoulder + new Vector2(-8f, -3f), flip),
            World(anchor, rearShoulder + new Vector2(9f, 3f), flip), edge, 11);
        DrawLine(World(anchor, frontShoulder + new Vector2(-8f, 3f), flip),
            World(anchor, frontShoulder + new Vector2(10f, -4f), flip),
            WithExecutionerAlpha(Outline, alpha), 15);
        DrawLine(World(anchor, frontShoulder + new Vector2(-7f, 3f), flip),
            World(anchor, frontShoulder + new Vector2(9f, -3f), flip), body, 9);

        // Belt and short tassets preserve a visible gap between torso and legs.
        DrawLine(World(anchor, waist + new Vector2(-15f, 0f), flip),
            World(anchor, waist + new Vector2(16f, 0f), flip), edge, 7);
        DrawBox(anchor, waist.X - 4f, waist.Y - 4f, 8f, 8f, steel, flip);
        DrawLine(World(anchor, waist + new Vector2(-11f, 3f), flip),
            World(anchor, pose.Hip + new Vector2(-14f, 16f), flip), body, 11);
        DrawLine(World(anchor, waist + new Vector2(11f, 3f), flip),
            World(anchor, pose.Hip + new Vector2(15f, 15f), flip), edge, 11);
        if (phaseTwo)
        {
            DrawLine(World(anchor, pose.Chest + new Vector2(20f, 9f), flip),
                World(anchor, waist + new Vector2(11f, -1f), flip), accent, 2);
        }
    }

    private void DrawRebuiltExecutionerArm(
        Vector2 anchor,
        Vector2 shoulder,
        Vector2 elbow,
        Vector2 hand,
        bool flip,
        Color upperColor,
        Color forearmColor,
        Color jointColor,
        byte alpha)
    {
        Color outline = WithExecutionerAlpha(Outline, alpha);
        DrawLine(World(anchor, shoulder, flip), World(anchor, elbow, flip),
            outline, 14);
        DrawLine(World(anchor, shoulder, flip), World(anchor, elbow, flip),
            upperColor, 9);
        DrawLine(World(anchor, elbow, flip), World(anchor, hand, flip),
            outline, 13);
        DrawLine(World(anchor, elbow, flip), World(anchor, hand, flip),
            forearmColor, 8);
        DrawBox(anchor, elbow.X - 5f, elbow.Y - 5f, 10f, 10f,
            jointColor, flip);
    }

    private void DrawRebuiltExecutionerLeg(
        Vector2 anchor,
        Vector2 hip,
        Vector2 knee,
        Vector2 foot,
        bool flip,
        Color thigh,
        Color greave,
        Color edge,
        bool phaseTwo,
        byte alpha)
    {
        Color outline = WithExecutionerAlpha(Outline, alpha);
        Vector2 ankle = Vector2.Lerp(knee, foot, .78f);
        DrawLine(World(anchor, hip, flip), World(anchor, knee, flip),
            outline, 18);
        DrawLine(World(anchor, hip, flip), World(anchor, knee, flip),
            thigh, 12);
        DrawLine(World(anchor, knee, flip), World(anchor, ankle, flip),
            outline, 16);
        DrawLine(World(anchor, knee, flip), World(anchor, ankle, flip),
            greave, 10);
        DrawLine(World(anchor, knee + new Vector2(-7f, 0f), flip),
            World(anchor, knee + new Vector2(7f, 0f), flip), edge, 7);
        DrawLine(World(anchor, ankle + new Vector2(-2f, 0f), flip),
            World(anchor, foot + new Vector2(8f, 0f), flip), outline, 12);
        DrawLine(World(anchor, ankle, flip),
            World(anchor, foot + new Vector2(7f, -1f), flip), greave, 7);
        if (phaseTwo)
        {
            DrawLine(World(anchor, knee + new Vector2(3f, 5f), flip),
                World(anchor, ankle + new Vector2(2f, -2f), flip),
                new Color((byte)121, (byte)31, (byte)36, alpha), 2);
        }
    }

    private void DrawRebuiltExecutionerMantle(
        Vector2 anchor,
        ExecutionerPose pose,
        Vector2 rearShoulder,
        bool flip,
        bool phaseTwo,
        byte alpha)
    {
        Color clothOutline = new(Outline.R, Outline.G, Outline.B, alpha);
        Color cloth = new((byte)24, (byte)22, (byte)27, alpha);
        Color hem = new((byte)46, (byte)40, (byte)45, alpha);
        Vector2 clasp = rearShoulder + new Vector2(-2f, -3f);
        Vector2 fold = pose.Chest + new Vector2(-27f + pose.MantleLag * .25f, 15f);
        Vector2 tip = pose.Hip + new Vector2(-20f + pose.MantleLag, 4f);
        DrawLine(World(anchor, clasp, flip), World(anchor, fold, flip),
            clothOutline, 17);
        DrawLine(World(anchor, fold, flip), World(anchor, tip, flip), cloth, 16);
        DrawLine(World(anchor, tip, flip),
            World(anchor, tip + new Vector2(9f, -4f), flip), hem, 4);
        DrawBox(anchor, clasp.X - 3f, clasp.Y - 3f, 7f, 7f,
            phaseTwo
                ? new Color((byte)116, (byte)34, (byte)39, alpha)
                : new Color((byte)66, (byte)54, (byte)57, alpha),
            flip);
    }

    private void DrawRebuiltExecutionerHelmet(
        Vector2 anchor,
        Vector2 head,
        bool flip,
        Color body,
        Color edge,
        Color steel,
        Color accent,
        bool phaseTwo,
        bool counter,
        DemonFortressCombatState state,
        float progress,
        float helmetTurn,
        byte alpha)
    {
        Color outline = WithExecutionerAlpha(Outline, alpha);
        Vector2 helmet = head + new Vector2((1f - helmetTurn) * -5f, 0f);
        // Neck guard establishes a clear gap between the helm and chest mass.
        DrawLine(World(anchor, helmet + new Vector2(-8f, 10f), flip),
            World(anchor, helmet + new Vector2(-13f, 21f), flip), outline, 13);
        DrawLine(World(anchor, helmet + new Vector2(-7f, 10f), flip),
            World(anchor, helmet + new Vector2(-12f, 20f), flip), edge, 8);
        // Tall enclosed shell, reinforced jaw, angular brow and compact ridge.
        DrawLine(World(anchor, helmet + new Vector2(0f, -13f), flip),
            World(anchor, helmet + new Vector2(0f, 10f), flip), outline, 32);
        DrawLine(World(anchor, helmet + new Vector2(0f, -11f), flip),
            World(anchor, helmet + new Vector2(0f, 9f), flip), body, 26);
        DrawLine(World(anchor, helmet + new Vector2(-12f, 7f), flip),
            World(anchor, helmet + new Vector2(12f, 9f), flip), edge, 8);
        DrawLine(World(anchor, helmet + new Vector2(-14f, -6f), flip),
            World(anchor, helmet + new Vector2(14f, -2f), flip), steel, 7);
        DrawLine(World(anchor, helmet + new Vector2(-9f, -14f), flip),
            World(anchor, helmet + new Vector2(-16f, -24f), flip), edge, 8);

        float introLight = state == DemonFortressCombatState.ExecutionerIntro
            ? MathHelper.Lerp(.25f, 1f, Math.Clamp(progress * 1.7f, 0f, 1f))
            : 1f;
        int red = counter ? 220 : phaseTwo ? 185 : 106;
        byte eyeAlpha = (byte)(alpha / 255f * introLight *
            (counter ? 245f : phaseTwo ? 220f : 150f));
        DrawLine(World(anchor, helmet + new Vector2(-9f, -1f), flip),
            World(anchor, helmet + new Vector2(10f, 0f), flip),
            new Color((byte)red, (byte)31, (byte)35, eyeAlpha),
            counter ? 3 : 2);
        if (phaseTwo)
        {
            DrawLine(World(anchor, helmet + new Vector2(11f, 4f), flip),
                World(anchor, helmet + new Vector2(8f, 10f), flip), accent, 2);
        }
    }

    private static void GetRebuiltExecutionerWeaponAxis(
        Vector2 lowerGrip,
        Vector2 upperGrip,
        out Vector2 axis,
        out Vector2 head)
    {
        axis = upperGrip - lowerGrip;
        if (axis.LengthSquared() < .01f)
            axis = new Vector2(0f, 1f);
        axis.Normalize();
        head = upperGrip + axis * 58f;
    }

    private void DrawRebuiltExecutionAxe(
        Vector2 anchor,
        Vector2 lowerGrip,
        Vector2 upperGrip,
        bool flip,
        Color metal,
        bool phaseTwo)
    {
        byte alpha = metal.A;
        Color outline = WithExecutionerAlpha(Outline, alpha);
        Color haft = new((byte)57, (byte)43, (byte)39, alpha);
        Color edge = new(Steel.R, Steel.G, Steel.B, alpha);
        Color poll = new((byte)64, (byte)67, (byte)74, alpha);
        Color binding = phaseTwo
            ? new Color((byte)116, (byte)33, (byte)38, alpha)
            : new Color((byte)76, (byte)52, (byte)49, alpha);
        Color groove = phaseTwo
            ? new Color((byte)147, (byte)39, (byte)43, alpha)
            : new Color((byte)72, (byte)42, (byte)45, alpha);
        GetRebuiltExecutionerWeaponAxis(
            lowerGrip, upperGrip, out Vector2 localAxis, out Vector2 localHead);
        Vector2 lower = World(anchor, lowerGrip, flip);
        Vector2 upper = World(anchor, upperGrip, flip);
        Vector2 head = World(anchor, localHead, flip);
        Vector2 axis = upper - lower;
        if (axis.LengthSquared() < .01f)
            axis = flip ? -Vector2.UnitX : Vector2.UnitX;
        axis.Normalize();
        Vector2 normal = new(-axis.Y, axis.X);
        Vector2 butt = lower - axis * 27f;
        DrawLine(butt, head + axis * 6f, outline, 10);
        DrawLine(butt, head + axis * 6f, haft, 6);
        for (int gripBand = 0; gripBand < 4; gripBand++)
        {
            Vector2 band = butt + axis * (7f + gripBand * 7f);
            DrawLine(band - normal * 4f, band + normal * 4f, binding, 2);
        }

        // Broad execution blade with a compact reinforced poll and clear socket.
        Vector2 socket = head - axis * 4f;
        Vector2 bladeRoot = socket + normal * 4f - axis * 7f;
        Vector2 bladeCrown = socket + normal * 29f - axis * 5f;
        Vector2 bladeEdge = socket + normal * 34f + axis * 17f;
        Vector2 bladeHeel = socket + normal * 9f + axis * 20f;
        DrawLine(socket - normal * 3f, socket + normal * 6f, outline, 18);
        DrawLine(bladeRoot, bladeCrown, outline, 23);
        DrawLine(bladeCrown, bladeEdge, outline, 18);
        DrawLine(bladeEdge, bladeHeel, outline, 14);
        DrawLine(bladeRoot + normal * 2f, bladeCrown - normal * 2f, metal, 16);
        DrawLine(bladeCrown - normal * 2f, bladeEdge - normal * 3f, metal, 12);
        DrawLine(bladeEdge - normal * 2f, bladeHeel, edge, 4);
        Vector2 pollEnd = socket - normal * 18f - axis * 2f;
        DrawLine(socket - normal * 2f, pollEnd, outline, 15);
        DrawLine(socket - normal * 3f, pollEnd + normal * 2f, poll, 9);
        DrawLine(socket + normal * 11f,
            bladeEdge - normal * 8f, groove, phaseTwo ? 3 : 2);
    }

    private void DrawRebuiltExecutionerGrip(
        Vector2 anchor,
        Vector2 lowerHand,
        Vector2 upperHand,
        bool flip,
        Color gauntlet,
        bool bothHands)
    {
        DrawBox(anchor, lowerHand.X - 5f, lowerHand.Y - 4f, 10f, 9f,
            gauntlet, flip);
        if (bothHands)
        {
            DrawBox(anchor, upperHand.X - 5f, upperHand.Y - 4f, 10f, 9f,
                gauntlet, flip);
        }
    }

    private void DrawRebuiltExecutionerWeaponTrail(
        DemonFortressEnemy enemy,
        Vector2 anchor,
        ExecutionerPose pose,
        bool flip)
    {
        if (enemy.CombatState is not (
            DemonFortressCombatState.ExecutionerCleaveActive or
            DemonFortressCombatState.ExecutionerReverseActive))
            return;
        GetRebuiltExecutionerWeaponAxis(
            pose.LowerHand, pose.UpperHand, out _, out Vector2 localHead);
        Vector2 head = World(anchor, localHead, flip);
        float sign = flip ? -1f : 1f;
        DrawLine(head + new Vector2(-sign * 61f, -34f),
            head + new Vector2(-sign * 25f, -17f),
            new Color((byte)22, (byte)22, (byte)27, (byte)92), 12);
        DrawLine(head + new Vector2(-sign * 31f, -20f), head,
            new Color((byte)64, (byte)58, (byte)61, (byte)98), 7);
    }

    private void DrawRebuiltExecutionerDeath(
        DemonFortressEnemy enemy,
        Vector2 anchor,
        bool flip,
        Color blackPlate,
        Color ironEdge,
        Color steelEdge,
        Color rank)
    {
        float p = enemy.DeathPresentationProgress;
        byte alpha = (byte)MathHelper.Lerp(245f, 0f,
            Math.Clamp((p - .86f) / .14f, 0f, 1f));
        ExecutionerPose pose = CreateExecutionerBasePose(0f, false, false);
        Vector2 weaponLower;
        Vector2 weaponUpper;
        bool bothHands;

        if (p < .22f)
        {
            float slip = p / .22f;
            Vector2 drop = new(0f, slip * 13f);
            pose.Hip += new Vector2(0f, slip * 3f);
            pose.Chest += new Vector2(1f, slip * 4f);
            pose.Head += new Vector2(1f, slip * 3f);
            pose.RearElbow += drop;
            pose.FrontElbow += drop;
            pose.LowerHand += drop;
            pose.UpperHand += drop;
            pose.MantleLag = -5f;
            weaponLower = pose.LowerHand;
            weaponUpper = pose.UpperHand;
            bothHands = true;
        }
        else if (p < .62f)
        {
            float kneel = (p - .22f) / .40f;
            pose.Hip = new Vector2(3f, -55f + kneel * 21f);
            pose.Chest = new Vector2(6f, -91f + kneel * 18f);
            pose.Head = new Vector2(10f, -125f + kneel * 24f);
            pose.RearKnee = new Vector2(-22f, -28f);
            pose.RearFoot = new Vector2(-31f, -2f);
            pose.FrontKnee = new Vector2(23f, -6f);
            pose.FrontFoot = new Vector2(38f, -2f);
            pose.RearElbow = new Vector2(-8f, -57f);
            pose.LowerHand = new Vector2(-2f, -20f);
            pose.FrontElbow = new Vector2(25f, -70f);
            pose.UpperHand = new Vector2(9f, -58f + kneel * 6f);
            pose.ShoulderTurn = 4f;
            pose.MantleLag = -8f;
            weaponLower = pose.LowerHand;
            weaponUpper = new Vector2(22f, -13f);
            bothHands = false;
        }
        else
        {
            float collapse = (p - .62f) / .38f;
            pose.Hip = new Vector2(-11f, -20f);
            pose.Chest = new Vector2(19f, -34f + collapse * 8f);
            pose.Head = new Vector2(49f, -31f + collapse * 8f);
            pose.RearKnee = new Vector2(-31f, -8f);
            pose.RearFoot = new Vector2(-48f, -2f);
            pose.FrontKnee = new Vector2(8f, -8f);
            pose.FrontFoot = new Vector2(35f, -2f);
            pose.RearElbow = new Vector2(-4f, -21f);
            pose.LowerHand = new Vector2(8f, -7f);
            pose.FrontElbow = new Vector2(30f, -18f);
            pose.UpperHand = new Vector2(43f, -5f);
            pose.ShoulderTurn = 10f;
            pose.MantleLag = -5f;
            weaponLower = new Vector2(-35f, -7f);
            weaponUpper = new Vector2(-5f, -6f);
            bothHands = false;
        }

        DrawRebuiltExecutionerBody(
            anchor, pose, flip, blackPlate, ironEdge, steelEdge, rank,
            phaseTwo: false, counter: false,
            DemonFortressCombatState.Ready, 1f, alpha);
        DrawRebuiltExecutionAxe(anchor, weaponLower, weaponUpper, flip,
            new Color((byte)82, (byte)85, (byte)92, alpha), phaseTwo: false);
        DrawRebuiltExecutionerGrip(anchor, pose.LowerHand, pose.UpperHand,
            flip, WithExecutionerAlpha(steelEdge, alpha), bothHands);
    }

    private static Color WithExecutionerAlpha(Color color, byte alpha) =>
        new(color.R, color.G, color.B, alpha);

    private void DrawBow(int x, int y, bool left)
    {
        int sign = left ? -1 : 1;
        DrawLine(new Vector2(x, y - 18), new Vector2(x + sign * 14, y), Bronze, 3);
        DrawLine(new Vector2(x + sign * 14, y), new Vector2(x, y + 18), Bronze, 3);
        DrawLine(new Vector2(x, y - 18), new Vector2(x, y + 18), Steel, 1);
    }

    private void DrawWeapon(int x, int y, bool left, int length, int thickness, Color color)
    {
        int sign = left ? -1 : 1;
        DrawLine(new Vector2(x, y), new Vector2(x + sign * length / 2f, y - length * .55f),
            color, thickness);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }
}
