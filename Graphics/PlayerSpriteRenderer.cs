using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Layered, procedural sprite-style presentation for the player. The logical
/// layer boundaries mirror a future body/armor/cloak/weapon/shield sprite set,
/// so production sheets can replace these drawing methods without touching
/// gameplay state or collision geometry.
/// </summary>
public sealed class PlayerSpriteRenderer
{
    public const int ConceptualFrameWidth = 100;
    public const int ConceptualFrameHeight = 100;
    public const int VisualBodyWidth = 46;
    public const int VisualHeight = 68;
    public const int SwordLength = 31;
    public const int ShieldWidth = 20;
    public const int ShieldHeight = 31;

    private static readonly Color Outline = new(24, 25, 29);
    private static readonly Color Skin = new(190, 137, 99);
    private static readonly Color SkinLight = new(218, 164, 119);
    private static readonly Color Hair = new(39, 29, 27);
    private static readonly Color HairLight = new(61, 42, 34);
    private static readonly Color Cloth = new(47, 48, 53);
    private static readonly Color ClothLight = new(67, 68, 73);
    private static readonly Color Leather = new(65, 43, 32);
    private static readonly Color LeatherLight = new(91, 61, 43);
    private static readonly Color SteelDark = new(63, 69, 75);
    private static readonly Color Steel = new(100, 109, 117);
    private static readonly Color SteelLight = new(151, 159, 164);
    private static readonly Color CloakDark = new(73, 23, 29);
    private static readonly Color Cloak = new(119, 35, 42);
    private static readonly Color SunGold = new(191, 145, 61);
    private static readonly Color SlashEdge = new(112, 132, 143, 105);
    private static readonly Color SlashBody = new(171, 181, 183, 155);
    private static readonly Color SlashCore = new(222, 218, 199, 210);
    private static readonly Color HitSpark = new(226, 196, 119, 230);

    // Every pose is authored facing right. Facing left mirrors the finished
    // local-coordinate composition; it never changes animation frame order.
    private static readonly float[] LightOneSwordAngles =
        { -0.52f, -1.45f, -1.10f, -0.35f, 0.35f, 0.55f };
    private static readonly float[] LightOneLean =
        { 0f, -2f, 0f, 3f, 4f, 2f };
    private static readonly float[] LightTwoSwordAngles =
        { 0.55f, 0.72f, 0.35f, -0.35f, -1.05f, -1.25f };
    private static readonly float[] LightTwoLean =
        { 1f, -1f, 0f, 2f, 3f, 1f };
    private static readonly float[] LightThreeSwordAngles =
        { -1.25f, -1.65f, -1.92f, -1.48f, -0.78f, 0.02f, 0.62f, 0.88f, 1.02f };
    private static readonly float[] LightThreeLean =
        { 0f, -2f, -3f, 0f, 3f, 6f, 7f, 5f, 2f };
    private static readonly float[] HeavySwordAngles =
        { -0.52f, -1.55f, -2.28f, -1.05f, 0.28f, 0.82f };
    private static readonly float[] HeavyLean =
        { 0f, -2f, -4f, 1f, 7f, 3f };

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly SpriteAnimationController _animation = new();
    private int _observedHitEffectId;
    private float _hitEffectDuration;
    private float _hitEffectTimeRemaining;
    private Vector2 _hitEffectPosition;

    public PlayerSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(PlayerCharacter player, GameTime gameTime)
    {
        _animation.Update(gameTime, player);

        Vector2 anchor = new(
            MathF.Round(player.Position.X),
            MathF.Round(player.Position.Y + player.Size.Y / 2f));
        bool flip = player.Facing == FacingDirection.Left;

        if (player.IsGrounded || _animation.State == PlayerVisualState.Dead)
        {
            DrawBox(
                anchor,
                -22f,
                -3f,
                44f,
                7f,
                new Color(12, 14, 17, 145),
                flip);
        }

        if (_animation.State == PlayerVisualState.Dead &&
            _animation.Frame >= 2)
        {
            DrawCollapsed(player, anchor, flip);
        }
        else
        {
            Pose pose = CreatePose(player);
            DrawStanding(player, anchor, flip, pose);
        }

        if (player.IsSlowed)
            DrawWebStrands(anchor, flip);
    }

    private void DrawStanding(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        float bodyX = pose.Lean;
        const float feetY = 0f;
        float upperBodyY = pose.Bob + pose.Crouch;
        bool weaponBehindBody = pose.SwordAngle < -1.25f &&
            player.WeaponFamily is not WeaponFamily.Bow and
            not WeaponFamily.ArcaneStaff;

        if (player.WeaponFamily == WeaponFamily.Bow)
            DrawQuiver(anchor, flip, bodyX, upperBodyY);

        DrawCloak(player, anchor, flip, pose, bodyX, upperBodyY);

        if (weaponBehindBody)
            DrawWeapon(player, anchor, flip, pose);

        DrawLegs(player, anchor, flip, pose, bodyX, feetY);
        DrawRearArm(player, anchor, flip, pose, bodyX, upperBodyY);
        DrawTorso(player, anchor, flip, pose, bodyX, upperBodyY);
        DrawHead(player, anchor, flip, pose, bodyX, upperBodyY);

        // Both arms sit behind the shield face. The sword itself is drawn
        // afterward when it is on the foreground side, keeping the weapon
        // readable without leaving a hand floating over the shield heraldry.
        DrawFrontArm(player, anchor, flip, pose, bodyX, upperBodyY);

        if (player.UsesShield)
            DrawShield(player, anchor, flip, pose);

        if (!weaponBehindBody)
            DrawWeapon(player, anchor, flip, pose);

        DrawSlashTrail(player, anchor, flip, pose);

        if (player.Combat.IsChargingHeavy)
            DrawChargeEffect(player, anchor, flip, pose);

        if (player.Combat.IsBlockFeedbackActive &&
            _animation.State == PlayerVisualState.Block &&
            player.UsesShield)
        {
            DrawBlockImpact(anchor, flip, pose);
        }
    }

    private Pose CreatePose(PlayerCharacter player)
    {
        var pose = new Pose
        {
            SwordHandX = 18f,
            SwordHandY = -35f,
            SwordAngle = -0.52f,
            SwordLength = SwordLength,
            ShieldX = -4f,
            ShieldY = -45f,
            CloakTail = -1f
        };

        switch (_animation.State)
        {
            case PlayerVisualState.Idle:
                pose.Bob = _animation.Frame is 2 or 3 ? -1f : 0f;
                pose.Crouch = 1f;
                pose.Lean = 1f;
                pose.SwordHandX = 18f;
                pose.SwordHandY = -36f;
                pose.SwordAngle = -0.52f +
                    (_animation.Frame is 2 or 3 ? -0.04f : 0f);
                pose.ShieldY += _animation.Frame is 2 or 3 ? -1f : 0f;
                pose.CloakTail = _animation.Frame is 3 or 4 ? -2f : -1f;
                break;

            case PlayerVisualState.Run:
                pose.Lean = 2f;
                ConfigureRunStride(ref pose, _animation.Frame);
                pose.CloakTail = -7f - MathF.Abs(pose.Step) * 0.35f;
                pose.SwordHandX = 18f + (_animation.Frame is 1 or 2 or 3 ? 1f : 0f);
                pose.SwordAngle = -0.42f + (_animation.Frame % 2) * 0.07f;
                pose.ShieldX = -5f;
                pose.ShieldY = -44f + (_animation.Frame is 2 or 6 ? -1f : 0f);
                break;

            case PlayerVisualState.Jump:
                pose.Step = 4f;
                pose.Crouch = 2f;
                pose.SwordAngle = -0.38f;
                pose.CloakTail = -5f;
                break;

            case PlayerVisualState.Fall:
                pose.Step = -2f;
                pose.SwordAngle = -0.86f;
                pose.CloakTail = -4f;
                pose.ShieldY = -41f;
                break;

            case PlayerVisualState.LightAttack1:
                ConfigureAttackPose(
                    ref pose,
                    LightOneSwordAngles,
                    LightOneLean,
                    cloakTail: 0f);
                break;

            case PlayerVisualState.LightAttack2:
                ConfigureAttackPose(
                    ref pose,
                    LightTwoSwordAngles,
                    LightTwoLean,
                    cloakTail: -2f);
                break;

            case PlayerVisualState.LightAttack3:
                ConfigureAttackPose(
                    ref pose,
                    LightThreeSwordAngles,
                    LightThreeLean,
                    cloakTail: -5f);
                pose.SwordLength = SwordLength + 2f;
                pose.Crouch = _animation.Frame is >= 4 and <= 7 ? 3f : 1f;
                break;

            case PlayerVisualState.LightAttack4:
                ConfigureAttackPose(
                    ref pose,
                    LightTwoSwordAngles,
                    LightTwoLean,
                    cloakTail: -4f);
                pose.Crouch = 3f;
                break;

            case PlayerVisualState.LightAttack5:
                ConfigureAttackPose(
                    ref pose,
                    LightThreeSwordAngles,
                    LightThreeLean,
                    cloakTail: -6f);
                pose.Crouch = 4f;
                break;

            case PlayerVisualState.HeavyCharge:
                pose.Crouch = 3f;
                pose.Lean = -2f;
                pose.SwordHandX = 20f;
                pose.SwordHandY = -38f;
                pose.SwordAngle = -1.45f;
                pose.CloakTail = -3f;
                break;

            case PlayerVisualState.HeavyAttack:
                ConfigureAttackPose(
                    ref pose,
                    HeavySwordAngles,
                    HeavyLean,
                    cloakTail: -7f);
                pose.SwordLength = SwordLength + 3f;
                pose.Crouch = _animation.Frame is 3 or 4 ? 4f : 2f;
                pose.ShieldX = -21f;
                break;

            case PlayerVisualState.Dodge:
                pose.Crouch = 12f;
                pose.Lean = 7f;
                pose.Step = 7f;
                pose.SwordHandX = 4f;
                pose.SwordHandY = -27f;
                pose.SwordAngle = 0.02f;
                pose.ShieldX = -5f;
                pose.ShieldY = -32f;
                pose.CloakTail = -12f - _animation.Frame * 1.5f;
                break;

            case PlayerVisualState.Block:
                pose.Crouch = 4f;
                pose.Lean = -1f;
                pose.SwordHandX = -5f;
                pose.SwordHandY = -34f;
                pose.SwordAngle = -2.02f;
                pose.ShieldX = player.Combat.IsBlockFeedbackActive ? 6f : 8f;
                pose.ShieldY = -51f + _animation.Frame;
                pose.CloakTail = -3f;
                break;

            case PlayerVisualState.GuardBreak:
                pose.Crouch = 5f;
                pose.Lean = -7f + _animation.Frame;
                pose.SwordAngle = -1.75f;
                pose.ShieldX = -25f;
                pose.ShieldY = -53f + _animation.Frame * 3f;
                pose.CloakTail = 4f;
                break;

            case PlayerVisualState.Hurt:
                pose.Crouch = 4f;
                pose.Lean = -6f + _animation.Frame * 2f;
                pose.SwordAngle = -1.25f;
                pose.ShieldX = -20f;
                pose.CloakTail = 3f;
                break;

            case PlayerVisualState.Dead:
                pose.Crouch = 9f + _animation.Frame * 3f;
                pose.Lean = -4f;
                pose.SwordAngle = 0.35f;
                pose.ShieldX = -21f;
                pose.ShieldY = -35f;
                pose.CloakTail = 5f;
                break;
        }

        ApplyEquipmentPose(player, ref pose);
        return pose;
    }

    private void ApplyEquipmentPose(PlayerCharacter player, ref Pose pose)
    {
        bool isAttack = _animation.State is PlayerVisualState.LightAttack1 or
            PlayerVisualState.LightAttack2 or PlayerVisualState.LightAttack3 or
            PlayerVisualState.LightAttack4 or PlayerVisualState.LightAttack5 or
            PlayerVisualState.HeavyAttack;

        switch (player.WeaponFamily)
        {
            case WeaponFamily.GreatSword:
                pose.SwordLength += 14f;
                pose.SwordHandX -= 3f;
                pose.SwordHandY += 2f;
                pose.Crouch += isAttack ? 2f : 0f;
                if (!isAttack)
                    pose.SwordAngle = -.92f;
                ApplyFamilyAttackAngles(
                    ref pose,
                    new[] { -0.86f, -1.78f, -1.42f, -0.46f, 0.38f, 0.68f },
                    new[] { 0.62f, 0.92f, 0.48f, -0.42f, -1.18f, -1.46f },
                    new[] { -1.36f, -1.85f, -2.22f, -1.62f, -0.68f, 0.24f, 0.78f, 1.02f, 1.12f },
                    new[] { -0.72f, -1.72f, -2.42f, -1.64f, -0.28f, 0.88f });
                break;

            case WeaponFamily.BattleAxe:
                pose.SwordLength += 4f;
                pose.SwordHandX -= 1f;
                pose.Lean += 2f;
                if (!isAttack)
                    pose.SwordAngle = .38f;
                ApplyFamilyAttackAngles(
                    ref pose,
                    new[] { -0.30f, -1.36f, -1.64f, -0.72f, 0.48f, 0.72f },
                    new[] { 0.82f, 1.02f, 0.42f, -0.52f, -1.32f, -1.52f },
                    new[] { -1.10f, -1.72f, -2.04f, -1.32f, -0.42f, 0.56f, 0.94f, 1.12f, 1.20f },
                    new[] { -0.42f, -1.48f, -2.12f, -1.18f, 0.48f, 0.96f });
                break;

            case WeaponFamily.Spear:
                pose.SwordLength += 36f;
                pose.SwordHandX = 8f;
                pose.SwordHandY = -34f;
                if (!isAttack)
                    pose.SwordAngle = -.10f;
                ApplyFamilyAttackAngles(
                    ref pose,
                    new[] { -.35f, -.18f, -.08f, -.02f, .02f, -.05f },
                    new[] { -.24f, -.12f, -.02f, .03f, .08f, 0f },
                    new[] { .55f, .82f, .48f, .05f, -.42f, -.68f, -.40f, -.12f, .08f },
                    new[] { -.30f, -.18f, -.08f, -.02f, .02f, .04f });
                break;

            case WeaponFamily.DualDaggers:
                pose.SwordLength = 18f;
                pose.SwordHandX = 17f;
                pose.SwordHandY = -27f;
                pose.Crouch += 5f;
                pose.Lean += 3f;
                if (!isAttack)
                    pose.SwordAngle = -.28f;
                ApplyFamilyAttackAngles(
                    ref pose,
                    new[] { -.62f, -1.18f, -.52f, .08f, .48f, .30f },
                    new[] { .55f, .92f, .35f, -.18f, -.72f, -.55f },
                    new[] { -.92f, -.45f, .18f, .72f, .38f, -.32f, -.70f, -.22f, .12f },
                    new[] { -.72f, -1.02f, -.35f, .32f, .82f, .45f },
                    new[] { .85f, .55f, -.05f, -.68f, -.32f, .12f },
                    new[] { -1.05f, -.60f, .05f, .72f, .35f, -.38f, -.82f, -.25f, .18f });
                break;

            case WeaponFamily.Bow:
                pose.SwordLength = 43f;
                pose.SwordHandX = 21f;
                pose.SwordHandY = -39f;
                pose.SwordAngle = -1.57f;
                pose.Crouch += isAttack ? 1f : 0f;
                break;

            case WeaponFamily.ArcaneStaff:
                pose.SwordLength += 22f;
                pose.SwordHandX = 15f;
                pose.SwordHandY = -34f;
                pose.SwordAngle = isAttack ? -1.05f : -1.30f;
                pose.Lean -= 2f;
                break;
        }
    }

    private void ApplyFamilyAttackAngles(
        ref Pose pose,
        float[] lightOne,
        float[] lightTwo,
        float[] lightThree,
        float[] heavy,
        float[] lightFour = null,
        float[] lightFive = null)
    {
        float[] angles = _animation.State switch
        {
            PlayerVisualState.LightAttack1 => lightOne,
            PlayerVisualState.LightAttack2 => lightTwo,
            PlayerVisualState.LightAttack3 => lightThree,
            PlayerVisualState.LightAttack4 => lightFour ?? lightTwo,
            PlayerVisualState.LightAttack5 => lightFive ?? lightThree,
            PlayerVisualState.HeavyAttack => heavy,
            _ => null
        };

        if (angles != null)
            pose.SwordAngle = angles[Math.Min(_animation.Frame, angles.Length - 1)];
    }

    private static void ConfigureRunStride(ref Pose pose, int frame)
    {
        switch (frame)
        {
            case 0: pose.Step = 0f; break;
            case 1: pose.Step = 4f; pose.RightFootLift = 1f; break;
            case 2: pose.Step = 6f; pose.RightFootLift = 3f; break;
            case 3: pose.Step = 4f; pose.RightFootLift = 1f; break;
            case 4: pose.Step = 0f; break;
            case 5: pose.Step = -4f; pose.LeftFootLift = 1f; break;
            case 6: pose.Step = -6f; pose.LeftFootLift = 3f; break;
            case 7: pose.Step = -4f; pose.LeftFootLift = 1f; break;
        }
    }

    private void ConfigureAttackPose(
        ref Pose pose,
        float[] swordAngles,
        float[] bodyLean,
        float cloakTail)
    {
        int frame = Math.Min(
            _animation.Frame,
            Math.Min(swordAngles.Length, bodyLean.Length) - 1);
        pose.SwordAngle = swordAngles[frame];
        pose.Lean = bodyLean[frame];
        pose.SwordHandX = 12f + MathF.Max(0f, pose.Lean) * 0.45f;
        pose.ShieldX = -16f;
        pose.ShieldY = -43f;
        pose.CloakTail = cloakTail - MathF.Max(0f, pose.Lean) * 0.65f;
    }

    private void DrawCloak(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        Color dark = Tint(CloakDark, player);
        Color red = Tint(Cloak, player);
        float tail = pose.CloakTail;

        DrawBox(anchor, bodyX - 14f + tail, bodyY - 49f, 22f, 8f, Outline, flip);
        DrawBox(anchor, bodyX - 16f + tail, bodyY - 43f, 23f, 7f, Outline, flip);
        DrawBox(anchor, bodyX - 17f + tail, bodyY - 36f, 21f, 7f, Outline, flip);
        DrawBox(anchor, bodyX - 12f + tail, bodyY - 47f, 18f, 7f, dark, flip);
        DrawBox(anchor, bodyX - 14f + tail, bodyY - 41f, 18f, 6f, red, flip);
        DrawBox(anchor, bodyX - 15f + tail, bodyY - 35f, 15f, 5f, dark, flip);
        DrawBox(anchor, bodyX - 8f + tail, bodyY - 33f, 5f, 4f, red, flip);
    }

    private void DrawLegs(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        if (player.ArmorClass == ArmorClass.Light)
        {
            DrawLightLegs(player, anchor, flip, pose, bodyX, bodyY);
            return;
        }

        if (player.ArmorClass == ArmorClass.Heavy)
        {
            DrawHeavyLegs(player, anchor, flip, pose, bodyX, bodyY);
            return;
        }

        float leftStep = pose.Step;
        float rightStep = -pose.Step;
        float leftTop = bodyY - 24f - pose.LeftFootLift;
        float rightTop = bodyY - 24f - pose.RightFootLift;
        Color cloth = Tint(Cloth, player);
        Color steel = Tint(SteelDark, player);
        Color leather = Tint(Leather, player);

        DrawBox(anchor, bodyX - 10f + leftStep * 0.25f, leftTop, 8f, 18f, Outline, flip);
        DrawBox(anchor, bodyX + 2f + rightStep * 0.25f, rightTop, 8f, 18f, Outline, flip);
        DrawBox(anchor, bodyX - 9f + leftStep * 0.25f, leftTop + 1f, 6f, 12f, cloth, flip);
        DrawBox(anchor, bodyX + 3f + rightStep * 0.25f, rightTop + 1f, 6f, 12f, cloth, flip);
        DrawBox(anchor, bodyX - 10f + leftStep * 0.45f, leftTop + 10f, 8f, 10f, steel, flip);
        DrawBox(anchor, bodyX + 2f + rightStep * 0.45f, rightTop + 10f, 8f, 10f, steel, flip);
        DrawBox(anchor, bodyX - 12f + leftStep * 0.65f, leftTop + 17f, 11f, 7f, leather, flip);
        DrawBox(anchor, bodyX + 1f + rightStep * 0.65f, rightTop + 17f, 11f, 7f, leather, flip);
        DrawBox(anchor, bodyX - 9f + leftStep * 0.45f, leftTop + 11f, 2f, 7f, Tint(SteelLight, player), flip);
        DrawBox(anchor, bodyX + 4f + rightStep * 0.45f, rightTop + 11f, 2f, 7f, Tint(SteelLight, player), flip);
    }

    private void DrawLightLegs(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        float leftStep = pose.Step;
        float rightStep = -pose.Step;
        float leftTop = bodyY - 24f - pose.LeftFootLift;
        float rightTop = bodyY - 24f - pose.RightFootLift;
        Color cloth = Tint(ClothLight, player);
        Color leather = Tint(Leather, player);

        DrawBox(anchor, bodyX - 9f + leftStep * 0.25f, leftTop, 7f, 18f, Outline, flip);
        DrawBox(anchor, bodyX + 2f + rightStep * 0.25f, rightTop, 7f, 18f, Outline, flip);
        DrawBox(anchor, bodyX - 8f + leftStep * 0.25f, leftTop + 1f, 5f, 14f, cloth, flip);
        DrawBox(anchor, bodyX + 3f + rightStep * 0.25f, rightTop + 1f, 5f, 14f, cloth, flip);
        DrawBox(anchor, bodyX - 10f + leftStep * 0.65f, leftTop + 15f, 9f, 6f, leather, flip);
        DrawBox(anchor, bodyX + 1f + rightStep * 0.65f, rightTop + 15f, 9f, 6f, leather, flip);
        DrawBox(anchor, bodyX - 7f + leftStep * 0.25f, leftTop + 3f, 2f, 8f, Tint(LeatherLight, player), flip);
    }

    private void DrawHeavyLegs(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        float leftStep = pose.Step;
        float rightStep = -pose.Step;
        float leftTop = bodyY - 25f - pose.LeftFootLift;
        float rightTop = bodyY - 25f - pose.RightFootLift;
        Color plate = Tint(Steel, player);
        Color plateDark = Tint(SteelDark, player);

        DrawBox(anchor, bodyX - 13f + leftStep * 0.22f, leftTop, 11f, 21f, Outline, flip);
        DrawBox(anchor, bodyX + 2f + rightStep * 0.22f, rightTop, 11f, 21f, Outline, flip);
        DrawBox(anchor, bodyX - 11f + leftStep * 0.22f, leftTop + 1f, 8f, 18f, plateDark, flip);
        DrawBox(anchor, bodyX + 4f + rightStep * 0.22f, rightTop + 1f, 8f, 18f, plateDark, flip);
        DrawBox(anchor, bodyX - 11f + leftStep * 0.35f, leftTop + 9f, 8f, 7f, plate, flip);
        DrawBox(anchor, bodyX + 4f + rightStep * 0.35f, rightTop + 9f, 8f, 7f, plate, flip);
        DrawBox(anchor, bodyX - 14f + leftStep * 0.55f, leftTop + 17f, 13f, 7f, plateDark, flip);
        DrawBox(anchor, bodyX + 1f + rightStep * 0.55f, rightTop + 17f, 13f, 7f, plateDark, flip);
        DrawBox(anchor, bodyX - 9f + leftStep * 0.35f, leftTop + 10f, 2f, 8f, SteelLight, flip);
        DrawBox(anchor, bodyX + 6f + rightStep * 0.35f, rightTop + 10f, 2f, 8f, SteelLight, flip);
    }

    private void DrawRearArm(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        Vector2 shoulder = new(bodyX - 10f, bodyY - 44f);

        if (player.WeaponFamily == WeaponFamily.DualDaggers)
        {
            Vector2 daggerHand = new(pose.SwordHandX - 12f, pose.SwordHandY + 4f);
            Vector2 daggerElbow = Vector2.Lerp(shoulder, daggerHand, .52f) + new Vector2(-3f, 5f);
            DrawLine(anchor, shoulder, daggerElbow, 8f, Outline, flip);
            DrawLine(anchor, shoulder, daggerElbow, 5f, Tint(ClothLight, player), flip);
            DrawLine(anchor, daggerElbow, daggerHand, 7f, Outline, flip);
            DrawLine(anchor, daggerElbow, daggerHand, 4f, Tint(Leather, player), flip);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.Bow)
        {
            Vector2 stringHand = new(pose.SwordHandX - 9f, pose.SwordHandY);
            Vector2 bowElbow = Vector2.Lerp(shoulder, stringHand, .48f) + new Vector2(-7f, -2f);
            DrawLine(anchor, shoulder, bowElbow, 8f, Outline, flip);
            DrawLine(anchor, shoulder, bowElbow, 5f, Tint(ClothLight, player), flip);
            DrawLine(anchor, bowElbow, stringHand, 7f, Outline, flip);
            DrawLine(anchor, bowElbow, stringHand, 4f, Tint(Leather, player), flip);
            return;
        }

        if (!player.UsesShield)
        {
            Vector2 direction = new(
                MathF.Cos(pose.SwordAngle),
                MathF.Sin(pose.SwordAngle));
            Vector2 supportGrip = new Vector2(
                pose.SwordHandX,
                pose.SwordHandY) - direction * 6f;
            Vector2 supportElbow = Vector2.Lerp(shoulder, supportGrip, 0.55f) +
                new Vector2(-2f, 4f);
            DrawLine(anchor, shoulder, supportElbow, 8f, Outline, flip);
            DrawLine(
                anchor,
                shoulder,
                supportElbow,
                5f,
                Tint(ClothLight, player),
                flip);
            DrawLine(anchor, supportElbow, supportGrip, 7f, Outline, flip);
            DrawLine(
                anchor,
                supportElbow,
                supportGrip,
                4f,
                Tint(Leather, player),
                flip);
            DrawBox(
                anchor,
                supportGrip.X - 2f,
                supportGrip.Y - 2f,
                5f,
                5f,
                Tint(Skin, player),
                flip);
            return;
        }

        Vector2 shieldGrip = new(
            pose.ShieldX + ShieldWidth * 0.28f,
            pose.ShieldY + ShieldHeight * 0.55f);
        Vector2 elbow = Vector2.Lerp(shoulder, shieldGrip, 0.52f) +
            new Vector2(-2f, 3f);

        DrawLine(
            anchor,
            shoulder,
            elbow,
            8f,
            Outline,
            flip);
        DrawLine(
            anchor,
            shoulder,
            elbow,
            5f,
            Tint(ClothLight, player),
            flip);
        DrawLine(anchor, elbow, shieldGrip, 7f, Outline, flip);
        DrawLine(
            anchor,
            elbow,
            shieldGrip,
            4f,
            Tint(SteelDark, player),
            flip);
        DrawBox(
            anchor,
            shieldGrip.X - 2f,
            shieldGrip.Y - 2f,
            5f,
            5f,
            Tint(LeatherLight, player),
            flip);
    }

    private void DrawTorso(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        if (player.ArmorClass == ArmorClass.Light)
        {
            DrawLightTorso(player, anchor, flip, bodyX, bodyY);
            return;
        }

        if (player.ArmorClass == ArmorClass.Heavy)
        {
            DrawHeavyTorso(player, anchor, flip, bodyX, bodyY);
            return;
        }

        Color armorDark = Tint(SteelDark, player);
        Color armor = Tint(Steel, player);
        Color armorLight = Tint(SteelLight, player);

        DrawBox(anchor, bodyX - 13f, bodyY - 49f, 26f, 28f, Outline, flip);
        DrawBox(anchor, bodyX - 11f, bodyY - 48f, 22f, 25f, armorDark, flip);
        DrawBox(anchor, bodyX - 8f, bodyY - 47f, 16f, 20f, armor, flip);
        DrawBox(anchor, bodyX - 7f, bodyY - 46f, 3f, 17f, armorLight, flip);
        DrawBox(anchor, bodyX - 13f, bodyY - 26f, 26f, 6f, Outline, flip);
        DrawBox(anchor, bodyX - 11f, bodyY - 25f, 22f, 4f, Tint(Leather, player), flip);
        DrawBox(anchor, bodyX - 3f, bodyY - 25f, 6f, 5f, Tint(LeatherLight, player), flip);

        // Sword + sun chest mark.
        DrawBox(anchor, bodyX - 1f, bodyY - 42f, 3f, 11f, SunGold, flip);
        DrawBox(anchor, bodyX - 4f, bodyY - 39f, 9f, 3f, SunGold, flip);
        DrawBox(anchor, bodyX - 2f, bodyY - 44f, 5f, 3f, SunGold, flip);

        // Small belt potion and dagger, kept subordinate to the silhouette.
        DrawBox(anchor, bodyX - 10f, bodyY - 22f, 4f, 6f, new Color(68, 93, 72), flip);
        DrawLine(
            anchor,
            new Vector2(bodyX + 8f, bodyY - 22f),
            new Vector2(bodyX + 14f, bodyY - 13f),
            2f,
            armorLight,
            flip);

        // Asymmetric shoulder plates.
        DrawBox(anchor, bodyX - 17f, bodyY - 50f, 10f, 8f, Outline, flip);
        DrawBox(anchor, bodyX - 15f, bodyY - 49f, 8f, 5f, armorDark, flip);
        DrawBox(anchor, bodyX + 7f, bodyY - 52f, 13f, 10f, Outline, flip);
        DrawBox(anchor, bodyX + 8f, bodyY - 50f, 10f, 6f, armor, flip);
        DrawBox(anchor, bodyX + 10f, bodyY - 49f, 7f, 2f, armorLight, flip);
    }

    private void DrawLightTorso(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY)
    {
        Color cloth = Tint(ClothLight, player);
        Color leather = Tint(Leather, player);
        Color leatherLight = Tint(LeatherLight, player);

        DrawBox(anchor, bodyX - 11f, bodyY - 48f, 22f, 27f, Outline, flip);
        DrawBox(anchor, bodyX - 9f, bodyY - 47f, 18f, 24f, cloth, flip);
        DrawBox(anchor, bodyX - 7f, bodyY - 46f, 5f, 21f, leather, flip);
        DrawBox(anchor, bodyX + 3f, bodyY - 45f, 3f, 18f, leatherLight, flip);
        DrawBox(anchor, bodyX - 11f, bodyY - 26f, 22f, 5f, Outline, flip);
        DrawBox(anchor, bodyX - 9f, bodyY - 25f, 18f, 3f, leather, flip);
        DrawBox(anchor, bodyX - 13f, bodyY - 49f, 7f, 6f, Outline, flip);
        DrawBox(anchor, bodyX + 7f, bodyY - 49f, 7f, 6f, Outline, flip);
        DrawBox(anchor, bodyX - 1f, bodyY - 41f, 3f, 10f, SunGold, flip);
        DrawBox(anchor, bodyX - 4f, bodyY - 38f, 9f, 3f, SunGold, flip);
    }

    private void DrawHeavyTorso(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY)
    {
        Color plateDark = Tint(SteelDark, player);
        Color plate = Tint(Steel, player);
        Color plateLight = Tint(SteelLight, player);

        DrawBox(anchor, bodyX - 17f, bodyY - 51f, 34f, 32f, Outline, flip);
        DrawBox(anchor, bodyX - 15f, bodyY - 49f, 30f, 28f, plateDark, flip);
        DrawBox(anchor, bodyX - 11f, bodyY - 48f, 22f, 23f, plate, flip);
        DrawBox(anchor, bodyX - 9f, bodyY - 47f, 4f, 20f, plateLight, flip);
        DrawBox(anchor, bodyX - 16f, bodyY - 26f, 32f, 7f, Outline, flip);
        DrawBox(anchor, bodyX - 14f, bodyY - 25f, 28f, 5f, Tint(Leather, player), flip);
        DrawBox(anchor, bodyX - 22f, bodyY - 53f, 13f, 11f, Outline, flip);
        DrawBox(anchor, bodyX - 20f, bodyY - 51f, 11f, 7f, plate, flip);
        DrawBox(anchor, bodyX + 9f, bodyY - 54f, 15f, 12f, Outline, flip);
        DrawBox(anchor, bodyX + 10f, bodyY - 52f, 12f, 8f, plate, flip);
        DrawBox(anchor, bodyX - 1f, bodyY - 43f, 3f, 14f, SunGold, flip);
        DrawBox(anchor, bodyX - 5f, bodyY - 39f, 11f, 3f, SunGold, flip);
        DrawBox(anchor, bodyX - 3f, bodyY - 45f, 7f, 4f, SunGold, flip);
    }

    private void DrawHead(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        Color skin = Tint(Skin, player);
        Color skinLight = Tint(SkinLight, player);
        Color hair = Tint(Hair, player);

        DrawBox(anchor, bodyX - 8f, bodyY - 66f, 17f, 18f, Outline, flip);
        DrawBox(anchor, bodyX - 6f, bodyY - 64f, 14f, 15f, skin, flip);
        DrawBox(anchor, bodyX - 4f, bodyY - 62f, 10f, 7f, skinLight, flip);

        // A small forward nose makes the canonical right-facing profile
        // unambiguous before the whole composition is mirrored.
        DrawBox(anchor, bodyX + 7f, bodyY - 60f, 5f, 6f, Outline, flip);
        DrawBox(anchor, bodyX + 7f, bodyY - 59f, 3f, 4f, skinLight, flip);
        DrawBox(anchor, bodyX + 4f, bodyY - 51f, 5f, 3f, Outline, flip);
        DrawBox(anchor, bodyX + 3f, bodyY - 53f, 5f, 3f, skin, flip);

        // Hair is concentrated at the crown and rear of the right-facing
        // head; it no longer masks the forward cheek and reverses the read.
        DrawBox(anchor, bodyX - 8f, bodyY - 68f, 17f, 8f, hair, flip);
        DrawBox(anchor, bodyX - 9f, bodyY - 64f, 5f, 11f, HairLight, flip);
        DrawBox(anchor, bodyX - 6f, bodyY - 62f, 3f, 8f, hair, flip);
        DrawBox(anchor, bodyX + 1f, bodyY - 62f, 7f, 2f, hair, flip);

        DrawBox(anchor, bodyX + 4f, bodyY - 59f, 3f, 3f, new Color(30, 27, 27), flip);
        DrawBox(anchor, bodyX + 4f, bodyY - 54f, 4f, 1f, new Color(91, 48, 42), flip);

        // Small scar crossing the forward eyebrow.
        DrawBox(anchor, bodyX + 2f, bodyY - 62f, 2f, 2f, new Color(104, 54, 48), flip);
        DrawBox(anchor, bodyX + 4f, bodyY - 60f, 2f, 2f, new Color(104, 54, 48), flip);
    }

    private void DrawFrontArm(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        Vector2 shoulder = new(bodyX + 12f, bodyY - 44f);
        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        DrawLine(anchor, shoulder, hand, 8f, Outline, flip);
        DrawLine(anchor, shoulder, hand, 5f, Tint(Leather, player), flip);
        DrawLine(
            anchor,
            Vector2.Lerp(shoulder, hand, 0.48f),
            hand,
            5f,
            Tint(SteelDark, player),
            flip);
        DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, Tint(Skin, player), flip);
    }

    private void DrawWeapon(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        Vector2 direction = new(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
        Vector2 perpendicular = new(-direction.Y, direction.X);
        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        Vector2 bladeStart = hand + direction * 5f;
        Vector2 bladeEnd = bladeStart + direction * pose.SwordLength;
        Vector2 handleEnd = hand - direction * 7f;
        Color blade = player.Combat.IsAttackWindingUp
            ? new Color(178, 133, 83)
            : Tint(SteelLight, player);

        if (player.WeaponFamily == WeaponFamily.DualDaggers)
        {
            DrawDagger(anchor, flip, hand, direction, pose.SwordLength, blade);
            Vector2 rearHand = new(pose.SwordHandX - 12f, pose.SwordHandY + 4f);
            float rearAngle = -pose.SwordAngle - .15f;
            Vector2 rearDirection = new(MathF.Cos(rearAngle), MathF.Sin(rearAngle));
            DrawDagger(anchor, flip, rearHand, rearDirection, pose.SwordLength - 2f, blade);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.Bow)
        {
            Vector2 bowTop = hand + new Vector2(1f, -23f);
            Vector2 bowUpper = hand + new Vector2(8f, -12f);
            Vector2 bowLower = hand + new Vector2(8f, 12f);
            Vector2 bowBottom = hand + new Vector2(1f, 23f);
            Color wood = Tint(new Color(117, 74, 42), player);
            DrawLine(anchor, bowTop, bowUpper, 4f, Outline, flip);
            DrawLine(anchor, bowUpper, bowLower, 4f, Outline, flip);
            DrawLine(anchor, bowLower, bowBottom, 4f, Outline, flip);
            DrawLine(anchor, bowTop, bowUpper, 2f, wood, flip);
            DrawLine(anchor, bowUpper, bowLower, 2f, wood, flip);
            DrawLine(anchor, bowLower, bowBottom, 2f, wood, flip);
            Vector2 stringHand = new(pose.SwordHandX - 9f, pose.SwordHandY);
            DrawLine(anchor, bowTop, stringHand, 1f, new Color(202, 194, 164), flip);
            DrawLine(anchor, stringHand, bowBottom, 1f, new Color(202, 194, 164), flip);
            return;
        }

        DrawLine(anchor, hand, handleEnd, 5f, Outline, flip);
        DrawLine(anchor, hand, handleEnd, 3f, Tint(LeatherLight, player), flip);

        if (player.WeaponFamily == WeaponFamily.Spear)
        {
            DrawLine(anchor, handleEnd, bladeEnd, 5f, Outline, flip);
            DrawLine(anchor, handleEnd, bladeEnd, 3f, Tint(LeatherLight, player), flip);
            Vector2 spearTip = bladeEnd + direction * 13f;
            DrawLine(anchor, bladeEnd - perpendicular * 5f, spearTip, 6f, Outline, flip);
            DrawLine(anchor, bladeEnd + perpendicular * 5f, spearTip, 6f, Outline, flip);
            DrawLine(anchor, bladeEnd, spearTip, 3f, blade, flip);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.ArcaneStaff)
        {
            DrawLine(anchor, handleEnd, bladeEnd, 7f, Outline, flip);
            DrawLine(anchor, handleEnd, bladeEnd, 4f, Tint(new Color(83, 54, 43), player), flip);
            Vector2 focus = bladeEnd + direction * 5f;
            Color arcane = player.Combat.IsChargingHeavy
                ? Color.Lerp(new Color(106, 77, 151), new Color(202, 155, 239), player.Combat.HeavyChargeProgress)
                : new Color(124, 91, 169);
            DrawLine(anchor, focus - perpendicular * 8f, focus + perpendicular * 8f, 5f, Outline, flip);
            DrawLine(anchor, focus - direction * 8f, focus + direction * 8f, 5f, Outline, flip);
            DrawBox(anchor, focus.X - 5f, focus.Y - 5f, 11f, 11f, Outline, flip);
            DrawBox(anchor, focus.X - 3f, focus.Y - 3f, 7f, 7f, arcane, flip);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.BattleAxe)
        {
            DrawLine(anchor, hand, bladeEnd, 6f, Outline, flip);
            DrawLine(anchor, hand, bladeEnd, 3f, Tint(LeatherLight, player), flip);
            Vector2 axeNeck = bladeEnd - direction * 5f;
            DrawLine(
                anchor,
                axeNeck - perpendicular * 9f,
                axeNeck + perpendicular * 8f,
                8f,
                Outline,
                flip);
            DrawLine(
                anchor,
                axeNeck - perpendicular * 8f,
                axeNeck + perpendicular * 7f,
                5f,
                blade,
                flip);
            DrawLine(
                anchor,
                bladeEnd - perpendicular * 9f,
                bladeEnd + perpendicular * 8f,
                2f,
                SteelLight,
                flip);
            return;
        }
        DrawLine(
            anchor,
            hand - perpendicular * 6f,
            hand + perpendicular * 6f,
            4f,
            Outline,
            flip);
        DrawLine(
            anchor,
            hand - perpendicular * 5f,
            hand + perpendicular * 5f,
            2f,
            SunGold,
            flip);
        float bladeOutline = player.WeaponFamily == WeaponFamily.GreatSword ? 9f : 5f;
        float bladeWidth = player.WeaponFamily == WeaponFamily.GreatSword ? 6f : 3f;
        float guardHalfWidth = player.WeaponFamily == WeaponFamily.GreatSword ? 8f : 5f;
        DrawLine(
            anchor,
            hand - perpendicular * guardHalfWidth,
            hand + perpendicular * guardHalfWidth,
            player.WeaponFamily == WeaponFamily.GreatSword ? 5f : 2f,
            player.WeaponFamily == WeaponFamily.GreatSword ? SteelDark : SunGold,
            flip);
        DrawLine(anchor, bladeStart, bladeEnd, bladeOutline, Outline, flip);
        DrawLine(anchor, bladeStart, bladeEnd, bladeWidth, blade, flip);
        DrawLine(
            anchor,
            bladeStart + perpendicular,
            bladeEnd + perpendicular,
            1f,
            new Color(210, 216, 218),
            flip);

        // Compact sun mark near the forte.
        Vector2 mark = bladeStart + direction * 7f;
        DrawBox(anchor, mark.X - 1f, mark.Y - 1f, 3f, 3f, SunGold, flip);
    }

    private void DrawDagger(
        Vector2 anchor,
        bool flip,
        Vector2 hand,
        Vector2 direction,
        float length,
        Color blade)
    {
        Vector2 perpendicular = new(-direction.Y, direction.X);
        Vector2 tip = hand + direction * length;
        DrawLine(anchor, hand - direction * 5f, hand, 4f, Outline, flip);
        DrawLine(anchor, hand - perpendicular * 4f, hand + perpendicular * 4f, 3f, SunGold, flip);
        DrawLine(anchor, hand, tip, 5f, Outline, flip);
        DrawLine(anchor, hand + direction * 2f, tip, 2f, blade, flip);
    }

    private void DrawQuiver(
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY)
    {
        DrawLine(anchor, new Vector2(bodyX - 13f, bodyY - 52f), new Vector2(bodyX - 21f, bodyY - 18f), 8f, Outline, flip);
        DrawLine(anchor, new Vector2(bodyX - 13f, bodyY - 51f), new Vector2(bodyX - 21f, bodyY - 19f), 5f, new Color(74, 46, 34), flip);
        for (int index = 0; index < 3; index++)
        {
            float x = bodyX - 17f + index * 3f;
            DrawLine(anchor, new Vector2(x, bodyY - 58f), new Vector2(x - 7f, bodyY - 27f), 2f, new Color(151, 112, 67), flip);
        }
    }

    private void DrawChargeEffect(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        float charge = player.Combat.HeavyChargeProgress;
        float radius = 5f + charge * 11f;
        Vector2 center = player.WeaponFamily == WeaponFamily.Bow
            ? new Vector2(pose.SwordHandX + 7f, pose.SwordHandY)
            : new Vector2(
                pose.SwordHandX + MathF.Cos(pose.SwordAngle) * (pose.SwordLength + 10f),
                pose.SwordHandY + MathF.Sin(pose.SwordAngle) * (pose.SwordLength + 10f));
        Color color = player.WeaponFamily == WeaponFamily.Bow
            ? ScaleAlpha(new Color(220, 190, 103), .35f + charge * .55f)
            : ScaleAlpha(new Color(174, 111, 224), .35f + charge * .60f);
        DrawLine(anchor, center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), 2f, color, flip);
        DrawLine(anchor, center - new Vector2(0f, radius), center + new Vector2(0f, radius), 2f, color, flip);
        DrawBox(anchor, center.X - 2f, center.Y - 2f, 5f, 5f, color, flip);
    }

    private void DrawSlashTrail(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        AttackDefinition attack = player.Combat.CurrentAttack;

        if (attack == null ||
            (_animation.State != PlayerVisualState.LightAttack1 &&
             _animation.State != PlayerVisualState.LightAttack2 &&
             _animation.State != PlayerVisualState.LightAttack3 &&
             _animation.State != PlayerVisualState.LightAttack4 &&
             _animation.State != PlayerVisualState.LightAttack5 &&
             _animation.State != PlayerVisualState.HeavyAttack))
        {
            return;
        }

        if (attack.Delivery == AttackDelivery.Arrow)
            return;

        if (attack.Delivery == AttackDelivery.ArcaneProjectile)
        {
            DrawArcaneCastFlash(player, anchor, flip, pose, attack);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.Spear &&
            attack.HitboxShape == AttackHitboxShape.Thrust)
        {
            DrawThrustTrail(player, anchor, flip, pose, attack);
            return;
        }

        GetSlashProfile(
            player.WeaponFamily,
            attack.Kind,
            out float startAngle,
            out float endAngle,
            out float radius,
            out float duration,
            out float trailSpan,
            out int segments,
            out float weight);

        float effectStart = attack.StartupTime + attack.ActiveTime * 0.4f;
        float elapsed = player.Combat.StateElapsed - effectStart;

        if (elapsed < 0f || elapsed > duration)
            return;

        float travelDuration = duration * 0.64f;
        float headProgress = Math.Clamp(elapsed / travelDuration, 0f, 1f);
        float fade = elapsed <= travelDuration
            ? Math.Clamp(elapsed / 0.025f, 0f, 1f)
            : 1f - Math.Clamp(
                (elapsed - travelDuration) / (duration - travelDuration),
                0f,
                1f);
        float tailProgress = MathF.Max(0f, headProgress - trailSpan);
        Vector2 center = new(pose.SwordHandX, pose.SwordHandY);

        DrawSlashArc(
            anchor,
            flip,
            center,
            startAngle,
            endAngle,
            radius,
            tailProgress,
            headProgress,
            segments,
            weight,
            fade);

        if (player.WeaponFamily == WeaponFamily.DualDaggers)
        {
            DrawSlashArc(
                anchor,
                flip,
                center + new Vector2(-9f, 4f),
                -endAngle,
                -startAngle,
                radius - 5f,
                tailProgress,
                headProgress,
                Math.Max(5, segments - 2),
                weight * .72f,
                fade * .85f);
        }

        if (attack.Kind == AttackKind.Heavy)
        {
            DrawSlashArc(
                anchor,
                flip,
                center,
                startAngle - 0.08f,
                endAngle - 0.08f,
                radius - 6f,
                MathF.Max(0f, tailProgress - 0.08f),
                MathF.Max(0f, headProgress - 0.08f),
                segments - 2,
                weight * 0.55f,
                fade * 0.5f);

            if (headProgress >= 0.72f && fade > 0.45f)
            {
                float peakAngle = MathHelper.Lerp(
                    startAngle,
                    endAngle,
                    headProgress);
                Vector2 peak = center + new Vector2(
                    MathF.Cos(peakAngle),
                    MathF.Sin(peakAngle)) * radius;
                DrawLine(
                    anchor,
                    peak - new Vector2(4f, 0f),
                    peak + new Vector2(5f, 0f),
                    2f,
                    ScaleAlpha(SlashCore, fade * 0.7f),
                    flip);
                DrawLine(
                    anchor,
                    peak - new Vector2(0f, 4f),
                    peak + new Vector2(0f, 5f),
                    2f,
                    ScaleAlpha(SlashCore, fade * 0.7f),
                    flip);
            }
        }
    }

    private void DrawThrustTrail(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        AttackDefinition attack)
    {
        float elapsed = player.Combat.StateElapsed - attack.StartupTime;
        if (elapsed < 0f || elapsed > attack.ActiveTime + .08f)
            return;

        float fade = 1f - Math.Clamp(elapsed / (attack.ActiveTime + .08f), 0f, 1f);
        Vector2 start = new(pose.SwordHandX + 25f, pose.SwordHandY);
        Vector2 end = start + new Vector2(55f + (attack.IsHeavy ? 25f : 0f), 0f);
        DrawLine(anchor, start, end, attack.IsHeavy ? 5f : 3f, ScaleAlpha(SlashEdge, fade), flip);
        DrawLine(anchor, start + new Vector2(8f, 0f), end, 1f, ScaleAlpha(SlashCore, fade), flip);
    }

    private void DrawArcaneCastFlash(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        AttackDefinition attack)
    {
        float elapsed = player.Combat.StateElapsed - attack.StartupTime;
        if (elapsed < 0f || elapsed > attack.ActiveTime + .12f)
            return;

        float fade = 1f - Math.Clamp(elapsed / (attack.ActiveTime + .12f), 0f, 1f);
        Vector2 center = new(
            pose.SwordHandX + MathF.Cos(pose.SwordAngle) * (pose.SwordLength + 10f),
            pose.SwordHandY + MathF.Sin(pose.SwordAngle) * (pose.SwordLength + 10f));
        float radius = 10f + (1f - fade) * 12f;
        Color color = ScaleAlpha(new Color(183, 119, 232), fade);
        DrawLine(anchor, center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), 3f, color, flip);
        DrawLine(anchor, center - new Vector2(0f, radius), center + new Vector2(0f, radius), 3f, color, flip);
        DrawLine(anchor, center - new Vector2(radius * .7f), center + new Vector2(radius * .7f), 2f, color, flip);
    }

    private void DrawSlashArc(
        Vector2 anchor,
        bool flip,
        Vector2 center,
        float startAngle,
        float endAngle,
        float radius,
        float tailProgress,
        float headProgress,
        int segments,
        float weight,
        float fade)
    {
        if (headProgress <= tailProgress || fade <= 0f)
            return;

        for (int index = 0; index < segments; index++)
        {
            float segmentStart = index / (float)segments;
            float segmentEnd = (index + 1) / (float)segments;
            float progressStart = MathHelper.Lerp(
                tailProgress,
                headProgress,
                segmentStart);
            float progressEnd = MathHelper.Lerp(
                tailProgress,
                headProgress,
                segmentEnd);
            float angleStart = MathHelper.Lerp(
                startAngle,
                endAngle,
                progressStart);
            float angleEnd = MathHelper.Lerp(
                startAngle,
                endAngle,
                progressEnd);
            Vector2 pointStart = center + new Vector2(
                MathF.Cos(angleStart),
                MathF.Sin(angleStart)) * radius;
            Vector2 pointEnd = center + new Vector2(
                MathF.Cos(angleEnd),
                MathF.Sin(angleEnd)) * radius;
            float middle = (index + 0.5f) / segments;
            float taper = MathF.Sin(middle * MathHelper.Pi);
            float edgeThickness = 1f + 4f * taper * weight;
            float bodyThickness = 1f + 2.2f * taper * weight;

            DrawLine(
                anchor,
                pointStart,
                pointEnd,
                edgeThickness,
                ScaleAlpha(SlashEdge, fade),
                flip);
            DrawLine(
                anchor,
                pointStart,
                pointEnd,
                bodyThickness,
                ScaleAlpha(SlashBody, fade),
                flip);

            if (taper > 0.42f)
            {
                DrawLine(
                    anchor,
                    pointStart,
                    pointEnd,
                    1f,
                    ScaleAlpha(SlashCore, fade * taper),
                    flip);
            }
        }
    }

    private static void GetSlashProfile(
        WeaponFamily family,
        AttackKind kind,
        out float startAngle,
        out float endAngle,
        out float radius,
        out float duration,
        out float trailSpan,
        out int segments,
        out float weight)
    {
        if (family == WeaponFamily.GreatSword)
        {
            startAngle = -2.05f;
            endAngle = kind == AttackKind.LightTwo ? -1.05f : 1.02f;
            radius = kind == AttackKind.Heavy ? 66f : 58f;
            duration = kind == AttackKind.Heavy ? .30f : .22f;
            trailSpan = .86f;
            segments = 16;
            weight = 1.35f;
            return;
        }

        if (family == WeaponFamily.BattleAxe)
        {
            startAngle = kind == AttackKind.LightTwo ? .65f : -1.95f;
            endAngle = kind == AttackKind.LightTwo ? -.95f : .62f;
            radius = 44f;
            duration = kind == AttackKind.Heavy ? .24f : .17f;
            trailSpan = .64f;
            segments = 10;
            weight = 1.12f;
            return;
        }

        if (family == WeaponFamily.DualDaggers)
        {
            startAngle = kind is AttackKind.LightTwo or AttackKind.LightFour ? .65f : -1.02f;
            endAngle = kind is AttackKind.LightTwo or AttackKind.LightFour ? -.78f : .55f;
            radius = kind == AttackKind.Heavy ? 34f : 29f;
            duration = kind == AttackKind.Heavy ? .18f : .09f;
            trailSpan = .48f;
            segments = 7;
            weight = .58f;
            return;
        }

        switch (kind)
        {
            case AttackKind.LightTwo:
                startAngle = 0.48f;
                endAngle = -1.12f;
                radius = 38f;
                duration = 0.13f;
                trailSpan = 0.58f;
                segments = 9;
                weight = 0.78f;
                break;
            case AttackKind.LightThree:
                startAngle = -1.55f;
                endAngle = 0.90f;
                radius = 43f;
                duration = 0.18f;
                trailSpan = 0.75f;
                segments = 12;
                weight = 1.03f;
                break;
            case AttackKind.Heavy:
                startAngle = -2.15f;
                endAngle = 0.92f;
                radius = 47f;
                duration = 0.22f;
                trailSpan = 0.82f;
                segments = 14;
                weight = 1.18f;
                break;
            default:
                startAngle = -1.15f;
                endAngle = 0.38f;
                radius = 37f;
                duration = 0.12f;
                trailSpan = 0.55f;
                segments = 9;
                weight = 0.74f;
                break;
        }
    }

    public void DrawHitImpact(
        PlayerCharacter player,
        GameTime gameTime,
        int hitEffectId,
        Vector2 hitPosition)
    {
        if (hitEffectId != _observedHitEffectId)
        {
            _observedHitEffectId = hitEffectId;
            _hitEffectPosition = hitPosition;
            _hitEffectDuration = player.Combat.CurrentAttack?.Kind ==
                AttackKind.Heavy
                    ? 0.16f
                    : 0.11f;
            _hitEffectTimeRemaining = _hitEffectDuration;
        }
        else
        {
            _hitEffectTimeRemaining = MathF.Max(
                0f,
                _hitEffectTimeRemaining - MathF.Min(
                    (float)gameTime.ElapsedGameTime.TotalSeconds,
                    0.1f));
        }

        if (_hitEffectTimeRemaining <= 0f)
            return;

        bool flip = player.Facing == FacingDirection.Left;
        float progress = _hitEffectDuration <= 0f
            ? 0f
            : _hitEffectTimeRemaining / _hitEffectDuration;
        float spread = 3f + (1f - progress) * 5f;
        Vector2 anchor = new(
            MathF.Round(_hitEffectPosition.X),
            MathF.Round(_hitEffectPosition.Y));
        Color spark = ScaleAlpha(HitSpark, Math.Clamp(progress * 1.4f, 0f, 1f));
        Color glint = ScaleAlpha(SlashCore, Math.Clamp(progress * 1.2f, 0f, 1f));

        DrawBox(anchor, -2f, -2f, 5f, 5f, spark, flip);
        DrawLine(anchor, new Vector2(-spread, 0f), new Vector2(spread, 0f), 2f, glint, flip);
        DrawLine(anchor, new Vector2(0f, -spread), new Vector2(0f, spread), 2f, glint, flip);
        DrawLine(
            anchor,
            new Vector2(-spread * 0.65f, -spread * 0.65f),
            new Vector2(spread * 0.65f, spread * 0.65f),
            1f,
            spark,
            flip);
    }

    private void DrawShield(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        float x = pose.ShieldX;
        float y = pose.ShieldY;
        Color fill = player.Combat.IsBlockFeedbackActive
            ? new Color(135, 157, 164)
            : Tint(SteelDark, player);
        Color rim = player.Combat.IsBlockFeedbackActive
            ? new Color(220, 225, 218)
            : Tint(SteelLight, player);

        // Stepped silhouette gives the temporary shield a rounded lower edge.
        DrawBox(anchor, x - 1f, y, ShieldWidth + 2f, 20f, Outline, flip);
        DrawBox(anchor, x + 1f, y + 20f, ShieldWidth - 2f, 7f, Outline, flip);
        DrawBox(anchor, x + 4f, y + 27f, ShieldWidth - 8f, 5f, Outline, flip);
        DrawBox(anchor, x + 1f, y + 2f, ShieldWidth - 2f, 17f, fill, flip);
        DrawBox(anchor, x + 3f, y + 19f, ShieldWidth - 6f, 6f, fill, flip);
        DrawBox(anchor, x + 6f, y + 25f, ShieldWidth - 12f, 4f, fill, flip);
        DrawBox(anchor, x + 1f, y + 2f, 3f, 17f, rim, flip);
        DrawBox(anchor, x + 4f, y + 25f, 3f, 3f, rim, flip);

        // Sword + sun shield heraldry.
        float centerX = x + ShieldWidth / 2f;
        DrawBox(anchor, centerX - 1f, y + 7f, 3f, 15f, rim, flip);
        DrawBox(anchor, centerX - 5f, y + 11f, 11f, 3f, SunGold, flip);
        DrawBox(anchor, centerX - 3f, y + 8f, 7f, 8f, SunGold, flip);
        DrawBox(anchor, centerX - 1f, y + 9f, 3f, 6f, fill, flip);

        // Sparse damage scratches.
        DrawLine(
            anchor,
            new Vector2(x + 5f, y + 5f),
            new Vector2(x + 8f, y + 9f),
            1f,
            new Color(45, 49, 53),
            flip);
        DrawLine(
            anchor,
            new Vector2(x + 14f, y + 18f),
            new Vector2(x + 11f, y + 23f),
            1f,
            new Color(45, 49, 53),
            flip);
    }

    private void DrawBlockImpact(Vector2 anchor, bool flip, Pose pose)
    {
        float x = pose.ShieldX + ShieldWidth + 4f;
        float y = pose.ShieldY + 13f;
        Color spark = new(239, 213, 132);
        DrawBox(anchor, x, y, 5f, 5f, spark, flip);
        DrawLine(anchor, new Vector2(x + 2f, y - 2f), new Vector2(x + 4f, y - 9f), 2f, spark, flip);
        DrawLine(anchor, new Vector2(x + 4f, y + 5f), new Vector2(x + 8f, y + 10f), 2f, spark, flip);
        DrawLine(anchor, new Vector2(x + 6f, y + 1f), new Vector2(x + 12f, y), 2f, spark, flip);
    }

    private void DrawCollapsed(PlayerCharacter player, Vector2 anchor, bool flip)
    {
        float settle = _animation.Frame >= 3 ? 0f : -2f;
        Color armor = Tint(SteelDark, player);
        float torsoWidth = player.ArmorClass switch
        {
            ArmorClass.Light => 25f,
            ArmorClass.Heavy => 34f,
            _ => 29f
        };

        DrawBox(anchor, -24f, -11f + settle, 37f, 9f, Outline, flip);
        DrawBox(anchor, -22f, -10f + settle, 34f, 6f, Tint(CloakDark, player), flip);
        DrawBox(anchor, -9f, -18f + settle, torsoWidth, 13f, Outline, flip);
        DrawBox(anchor, -7f, -16f + settle, torsoWidth - 4f, 10f, armor, flip);
        DrawBox(anchor, 16f, -16f + settle, 15f, 14f, Outline, flip);
        DrawBox(anchor, 17f, -14f + settle, 12f, 10f, Tint(Skin, player), flip);
        DrawBox(anchor, 18f, -16f + settle, 13f, 5f, Hair, flip);
        DrawBox(anchor, -31f, -10f + settle, 14f, 8f, Tint(Leather, player), flip);

        if (player.WeaponFamily == WeaponFamily.BattleAxe)
        {
            DrawLine(anchor, new Vector2(-18f, -2f), new Vector2(27f, -3f), 5f, Outline, flip);
            DrawLine(anchor, new Vector2(-16f, -2f), new Vector2(27f, -3f), 3f, LeatherLight, flip);
            DrawBox(anchor, 20f, -13f, 12f, 18f, Outline, flip);
            DrawBox(anchor, 22f, -11f, 9f, 14f, SteelLight, flip);
        }
        else if (player.WeaponFamily == WeaponFamily.Spear)
        {
            DrawLine(anchor, new Vector2(-27f, -2f), new Vector2(43f, -3f), 5f, Outline, flip);
            DrawLine(anchor, new Vector2(-25f, -2f), new Vector2(41f, -3f), 3f, LeatherLight, flip);
            DrawBox(anchor, 39f, -7f, 13f, 9f, SteelLight, flip);
        }
        else if (player.WeaponFamily == WeaponFamily.DualDaggers)
        {
            DrawLine(anchor, new Vector2(-24f, -7f), new Vector2(-3f, -2f), 5f, Outline, flip);
            DrawLine(anchor, new Vector2(-22f, -7f), new Vector2(-3f, -2f), 2f, SteelLight, flip);
            DrawLine(anchor, new Vector2(6f, -3f), new Vector2(28f, -8f), 5f, Outline, flip);
            DrawLine(anchor, new Vector2(7f, -3f), new Vector2(27f, -8f), 2f, SteelLight, flip);
        }
        else if (player.WeaponFamily == WeaponFamily.Bow)
        {
            DrawLine(anchor, new Vector2(-22f, -9f), new Vector2(4f, -2f), 4f, Outline, flip);
            DrawLine(anchor, new Vector2(4f, -2f), new Vector2(30f, -8f), 4f, Outline, flip);
            DrawLine(anchor, new Vector2(-22f, -9f), new Vector2(30f, -8f), 1f, new Color(205, 194, 158), flip);
        }
        else if (player.WeaponFamily == WeaponFamily.ArcaneStaff)
        {
            DrawLine(anchor, new Vector2(-26f, -2f), new Vector2(31f, -5f), 7f, Outline, flip);
            DrawLine(anchor, new Vector2(-24f, -2f), new Vector2(29f, -5f), 4f, new Color(83, 54, 43), flip);
            DrawBox(anchor, 26f, -11f, 13f, 13f, Outline, flip);
            DrawBox(anchor, 29f, -8f, 7f, 7f, new Color(145, 94, 187), flip);
        }
        else
        {
            float bladeWidth = player.WeaponFamily == WeaponFamily.GreatSword ? 9f : 5f;
            float bladeLength = player.WeaponFamily == WeaponFamily.GreatSword ? 51f : 43f;
            DrawLine(anchor, new Vector2(-18f, -2f), new Vector2(-18f + bladeLength, -3f), bladeWidth, Outline, flip);
            DrawLine(anchor, new Vector2(-15f, -2f), new Vector2(-18f + bladeLength, -3f), MathF.Max(3f, bladeWidth - 3f), SteelLight, flip);
        }

        if (player.UsesShield)
        {
            DrawBox(anchor, -27f, -10f, 29f, 9f, Outline, flip);
            DrawBox(anchor, -25f, -9f, 25f, 6f, Tint(SteelDark, player), flip);
            DrawBox(anchor, -14f, -9f, 3f, 6f, SunGold, flip);
            DrawBox(anchor, -18f, -8f, 11f, 3f, SunGold, flip);
        }
    }

    private void DrawWebStrands(Vector2 anchor, bool flip)
    {
        Color web = new(188, 205, 199, 145);
        DrawLine(anchor, new Vector2(-20f, -41f), new Vector2(17f, -13f), 2f, web, flip);
        DrawLine(anchor, new Vector2(-17f, -17f), new Vector2(19f, -39f), 2f, web, flip);
        DrawLine(anchor, new Vector2(-14f, -29f), new Vector2(16f, -29f), 1f, web, flip);
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
        int drawX = flip
            ? (int)MathF.Round(anchor.X - x - width)
            : (int)MathF.Round(anchor.X + x);
        var destination = new Rectangle(
            drawX,
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height)));
        _spriteBatch.Draw(_pixel, destination, color);
    }

    private void DrawLine(
        Vector2 anchor,
        Vector2 localStart,
        Vector2 localEnd,
        float thickness,
        Color color,
        bool flip)
    {
        Vector2 start = ToWorld(anchor, localStart, flip);
        Vector2 end = ToWorld(anchor, localEnd, flip);
        Vector2 delta = end - start;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        start = new Vector2(MathF.Round(start.X), MathF.Round(start.Y));
        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            new Vector2(0f, 0.5f),
            new Vector2(MathF.Round(length), MathF.Max(1f, MathF.Round(thickness))),
            SpriteEffects.None,
            0f);
    }

    private static Vector2 ToWorld(Vector2 anchor, Vector2 local, bool flip)
    {
        return new Vector2(
            anchor.X + (flip ? -local.X : local.X),
            anchor.Y + local.Y);
    }

    private static Color Tint(Color color, PlayerCharacter player)
    {
        if (player.IsHitFlashing)
            return Color.Lerp(color, new Color(232, 105, 96), 0.58f);
        if (player.Combat.IsGuardBroken)
            return Color.Lerp(color, new Color(169, 91, 57), 0.3f);
        if (player.Combat.IsDodgeInvulnerable)
            return Color.Lerp(color, new Color(111, 141, 154), 0.22f);

        return color;
    }

    private static Color ScaleAlpha(Color color, float amount)
    {
        byte alpha = (byte)Math.Clamp(
            (int)MathF.Round(color.A * Math.Clamp(amount, 0f, 1f)),
            0,
            byte.MaxValue);
        return new Color(color.R, color.G, color.B, alpha);
    }

    private struct Pose
    {
        public float Bob;
        public float Crouch;
        public float Lean;
        public float Step;
        public float LeftFootLift;
        public float RightFootLift;
        public float CloakTail;
        public float SwordHandX;
        public float SwordHandY;
        public float SwordAngle;
        public float SwordLength;
        public float ShieldX;
        public float ShieldY;
    }
}
