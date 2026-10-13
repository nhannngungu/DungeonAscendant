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
    private Vector2? _chainHeadWorldPosition;
    private float _partScaleX = 1f;
    private float _partPivotX;
    private float _lineThicknessScale = 1f;

    public PlayerSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(
        PlayerCharacter player,
        GameTime gameTime,
        Vector2? chainHeadWorldPosition = null,
        Vector2? visualPosition = null,
        Vector2? portalTravelDirection = null,
        float portalTransitionStrength = 0f)
    {
        ResetPartTransform();
        _chainHeadWorldPosition = chainHeadWorldPosition;
        _animation.Update(gameTime, player);

        Vector2 anchor = new(
            MathF.Round((visualPosition ?? player.Position).X),
            MathF.Round((visualPosition ?? player.Position).Y +
                player.Size.Y / 2f));
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
            if (player.WeaponFamily == WeaponFamily.AbyssRing &&
                portalTravelDirection.HasValue &&
                portalTransitionStrength > 0f)
            {
                ApplyPortalTransitPose(
                    ref pose,
                    portalTravelDirection.Value,
                    portalTransitionStrength,
                    flip);
            }
            DrawDuelistAfterimages(player, anchor, flip, pose);
            DrawRangerAfterimages(player, anchor, flip, pose);
            DrawSovereignAfterimages(player, anchor, flip, pose);
            DrawStanding(player, anchor, flip, pose);
        }

        if (player.IsSlowed)
            DrawWebStrands(anchor, flip);
    }

    private static void ApplyPortalTransitPose(
        ref Pose pose,
        Vector2 direction,
        float strength,
        bool flip)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        float localX = (flip ? -direction.X : direction.X) * strength;
        pose.Crouch += 4f * strength;
        pose.Lean += 8f * localX;
        pose.Step += 9f * localX;
        pose.SwordHandX += 11f * localX;
        pose.SwordHandY += direction.Y * 8f * strength;
        pose.LeftFootLift = Math.Max(
            pose.LeftFootLift,
            Math.Max(0f, -localX) * 11f +
                Math.Max(0f, -direction.Y) * 7f * strength);
        pose.RightFootLift = Math.Max(
            pose.RightFootLift,
            Math.Max(0f, localX) * 11f +
                Math.Max(0f, direction.Y) * 7f * strength);
    }

    /// <summary>
    /// Uses the same layered player renderer as gameplay, but anchors a forced
    /// idle pose in screen space for character creation.
    /// </summary>
    public void DrawPreview(
        PlayerCharacter player,
        GameTime gameTime,
        Vector2 anchor,
        FacingDirection facing)
    {
        ResetPartTransform();
        _chainHeadWorldPosition = null;
        bool flip = facing == FacingDirection.Left;
        int frame = (int)(gameTime.TotalGameTime.TotalSeconds * 5d) % 6;
        var pose = new Pose
        {
            Bob = frame is 2 or 3 ? -1f : 0f,
            Crouch = 1f,
            Lean = 1f,
            SwordHandX = 18f,
            SwordHandY = frame is 2 or 3 ? -37f : -36f,
            SwordAngle = frame is 2 or 3 ? -0.56f : -0.52f,
            SwordLength = SwordLength,
            ShieldX = -4f,
            ShieldY = frame is 2 or 3 ? -46f : -45f,
            CloakTail = frame is 3 or 4 ? -2f : -1f
        };

        DrawBox(anchor, -22f, -3f, 44f, 7f, new Color(12, 14, 17, 145), flip);
        DrawStanding(player, anchor, flip, pose);
    }

    /// <summary>
    /// Reuses the live combat pose for a screen-space cinematic cut-in. The
    /// caller owns the SpriteBatch transform, allowing the code-rendered
    /// player, current bow, and current outfit to remain visually canonical.
    /// </summary>
    public void DrawCinematicCutIn(PlayerCharacter player, Vector2 anchor)
    {
        ResetPartTransform();
        _chainHeadWorldPosition = null;
        bool flip = player.Facing == FacingDirection.Left;
        Pose pose = CreatePose(player);
        DrawStanding(player, anchor, flip, pose);
    }

    public void DrawSovereignCinematicCutIn(
        PlayerCharacter player,
        Vector2 anchor)
    {
        ResetPartTransform();
        _chainHeadWorldPosition = null;
        bool flip = player.Facing == FacingDirection.Left;
        Pose pose = CreatePose(player);
        ConfigureSovereignStance(ref pose);
        pose.Crouch = 2f;
        pose.Lean = 5f;
        pose.SwordHandX = 18f;
        pose.SwordHandY = -55f;
        pose.FrontElbowX = 30f;
        pose.FrontElbowY = -48f;
        pose.OffHandX = -2f;
        pose.OffHandY = -40f;
        pose.RearElbowX = -16f;
        pose.RearElbowY = -44f;
        DrawStanding(player, anchor, flip, pose);

        Color eyeGlow = new(255, 215, 91, 235);
        DrawBox(anchor, pose.Lean + 2f, pose.Crouch - 61f,
            5f, 2f, eyeGlow, flip);
        DrawLine(anchor,
            new Vector2(pose.Lean - 7f, pose.Crouch - 60f),
            new Vector2(pose.Lean + 14f, pose.Crouch - 62f),
            1f,
            new Color(255, 198, 57, 105),
            flip);
    }

    private void DrawStanding(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        float bodyX = pose.Lean;
        const float feetY = 0f;
        float upperBodyY = pose.Bob + pose.Crouch +
            GetHeightOffset(player.Appearance.ApparentHeight);
        bool weaponBehindBody = pose.SwordAngle < -1.25f &&
            player.WeaponFamily is not WeaponFamily.Bow and
            not WeaponFamily.ArcaneStaff and
            not WeaponFamily.AbyssRing;

        if (player.WeaponFamily == WeaponFamily.Bow)
            DrawQuiver(anchor, flip, bodyX, upperBodyY);

        float torsoScale = GetTorsoScale(player.Appearance);
        SetPartTransform(torsoScale, bodyX);
        DrawCloak(player, anchor, flip, pose, bodyX, upperBodyY);
        ResetPartTransform();

        if (weaponBehindBody)
            DrawWeapon(player, anchor, flip, pose);

        SetPartTransform(GetLegScale(player.Appearance), bodyX);
        DrawLegs(player, anchor, flip, pose, bodyX, feetY);
        ResetPartTransform();
        _lineThicknessScale = GetArmScale(player.Appearance);
        DrawRearArm(player, anchor, flip, pose, bodyX, upperBodyY);
        ResetPartTransform();
        SetPartTransform(torsoScale, bodyX);
        DrawTorso(player, anchor, flip, pose, bodyX, upperBodyY);
        ResetPartTransform();
        DrawHead(player, anchor, flip, pose, bodyX, upperBodyY);

        // Both arms sit behind the shield face. The sword itself is drawn
        // afterward when it is on the foreground side, keeping the weapon
        // readable without leaving a hand floating over the shield heraldry.
        _lineThicknessScale = GetArmScale(player.Appearance);
        DrawFrontArm(player, anchor, flip, pose, bodyX, upperBodyY);
        ResetPartTransform();

        if (player.UsesShield)
            DrawShield(player, anchor, flip, pose);

        if (!weaponBehindBody && player.WeaponFamily != WeaponFamily.AbyssRing)
            DrawWeapon(player, anchor, flip, pose);

        DrawSlashTrail(player, anchor, flip, pose);
        DrawRaiderTechniqueEffects(player, anchor, flip, pose);
        DrawSpellbladeTechniqueEffects(player, anchor, flip, pose);
        DrawBreakerTechniqueEffects(player, anchor, flip, pose);
        DrawGreatswordTechniqueEffects(player, anchor, flip, pose);
        DrawSovereignTransformation(player, anchor, flip, pose);
        DrawSovereignMartialEffects(player, anchor, flip, pose);

        if (player.Combat.IsChargingHeavy)
            DrawChargeEffect(player, anchor, flip, pose);

        if (player.Combat.IsBlockFeedbackActive && player.UsesShield)
        {
            DrawBlockImpact(anchor, flip, pose);
        }

        if (player.Combat.IsPerfectGuardFeedbackActive && player.UsesShield)
            DrawPerfectGuardImpact(anchor, flip, pose);

        if (player.WeaponFamily == WeaponFamily.GreatSword &&
            (player.Combat.IsBlockFeedbackActive ||
             player.Combat.IsPerfectGuardFeedbackActive))
        {
            DrawGreatswordGuardImpact(player, anchor, flip, pose);
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
        ApplyKnightTechniquePose(player, ref pose);
        ApplyDuelistTechniquePose(player, ref pose);
        ApplyRangerTechniquePose(player, ref pose);
        ApplyRaiderTechniquePose(player, ref pose);
        ApplySpellbladeTechniquePose(player, ref pose);
        ApplyGreatswordTechniquePose(player, ref pose);
        ApplyBreakerTechniquePose(player, ref pose);
        ApplyReckonerTechniquePose(player, ref pose);
        ApplySovereignTechniquePose(player, ref pose);
        return pose;
    }

    private void ApplyReckonerTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.ChainFlail ||
            player.Combat.CurrentTechnique == null)
        {
            return;
        }

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        float time = player.Combat.StateElapsed;
        pose.SwordLength = 34f;
        pose.SwordHandX = 10f;
        pose.SwordHandY = -35f;

        switch (effect)
        {
            case WeaponTechniqueEffect.ReckonerChainLash:
                pose.Lean = stage == 0 ? 5f : stage == 1 ? -5f : 9f;
                pose.Step = stage == 2 ? 7f : stage == 0 ? 3f : -2f;
                pose.Crouch = stage == 2 ? 5f : 2f;
                pose.SwordAngle = stage switch
                {
                    0 => LerpByFrame(-1.25f, .48f),
                    1 => LerpByFrame(.62f, -1.72f),
                    _ => LerpByFrame(-1.35f, -.06f)
                };
                pose.CloakTail = stage == 2 ? -9f : -6f;
                break;

            case WeaponTechniqueEffect.ReckonerOrbitingMaelstrom:
                pose.Crouch = 5f;
                pose.Lean = MathF.Sin(time * 6.4f) * 4f;
                pose.Step = -pose.Lean * .65f;
                pose.SwordAngle = -1.3f + time *
                    (5.5f + player.Combat.ReckonerOrbitStage * .8f);
                pose.CloakTail = -7f - player.Combat.ReckonerOrbitStage * 2f;
                break;

            case WeaponTechniqueEffect.ReckonerChainHarpoon:
                pose.Crouch = 6f;
                pose.Lean = stage == 0 ? 8f : -7f;
                pose.Step = stage == 0 ? 6f : -4f;
                pose.SwordAngle = stage == 0
                    ? LerpByFrame(-1.85f, -.04f)
                    : LerpByFrame(.20f, -2.25f);
                pose.CloakTail = stage == 0 ? -10f : 4f;
                break;

            case WeaponTechniqueEffect.ReckonerReapersPassage:
                pose.Crouch = 10f;
                pose.Lean = 12f;
                pose.Step = 9f;
                pose.SwordAngle = LerpByFrame(1.15f, -2.20f);
                pose.CloakTail = -17f;
                break;

            case WeaponTechniqueEffect.ReckonerCrescentRequiem:
                pose.Crouch = stage == 0 ? 5f : 8f;
                pose.Lean = stage == 0
                    ? MathF.Sin(time * 9f) * 3f
                    : 10f;
                pose.Step = stage == 0 ? -3f : 7f;
                pose.SwordAngle = stage == 0
                    ? -1.4f + time * 8f
                    : LerpByFrame(-2.0f, .82f);
                pose.CloakTail = stage == 0 ? -5f : -13f;
                break;

            case WeaponTechniqueEffect.ReckonerSerpentsFang:
                pose.Crouch = 6f;
                pose.Lean = 7f * player.Combat.ReckonerCurveSide;
                pose.Step = 4f;
                pose.SwordHandY = -40f;
                pose.SwordAngle = LerpByFrame(-1.92f, .18f);
                pose.CloakTail = -9f;
                break;

            case WeaponTechniqueEffect.ReckonerVortexSnare:
                pose.Crouch = 7f;
                pose.Lean = stage == 6 ? -9f : MathF.Sin(time * 7f) * 4f;
                pose.Step = stage == 6 ? -6f : 2f;
                pose.SwordAngle = stage == 6
                    ? LerpByFrame(.35f, -2.42f)
                    : -1.1f + time * 6.5f;
                pose.CloakTail = stage == 6 ? 5f : -8f;
                break;

            case WeaponTechniqueEffect.ReckonerChainsOfJudgment:
                pose.Crouch = stage == 7 ? 11f : 7f;
                pose.Lean = stage == 6 ? -8f : stage == 7 ? 12f :
                    MathF.Sin(time * 6f) * 5f;
                pose.Step = stage == 7 ? 8f : stage == 6 ? -5f : 2f;
                pose.SwordAngle = stage == 7
                    ? LerpByFrame(-1.82f, 1.28f)
                    : -1.45f + time * (stage == 0 ? 3.8f : 7.4f);
                pose.CloakTail = stage == 7 ? -15f : -10f;
                break;
        }
    }

    private void ApplyGreatswordTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.GreatSword ||
            player.Combat.CurrentTechnique == null)
        {
            return;
        }

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        float progress = player.Combat.CurrentAttack == null
            ? 0f
            : Math.Clamp(
                player.Combat.StateElapsed /
                    player.Combat.CurrentAttack.TotalTime,
                0f,
                1f);
        pose.SwordLength = 50f;
        pose.SwordHandX = 10f;
        pose.SwordHandY = -35f;

        switch (effect)
        {
            case WeaponTechniqueEffect.GreatswordCombo:
                pose.Crouch = stage >= 2 ? 8f : 5f;
                pose.Lean = stage switch
                {
                    0 => MathHelper.Lerp(-4f, 7f, progress),
                    1 => MathHelper.Lerp(5f, -4f, progress),
                    2 => MathHelper.Lerp(7f, -2f, progress),
                    _ => MathHelper.Lerp(-6f, 10f, progress)
                };
                pose.Step = stage < 2 ? 5f : stage == 2 ? 3f : 1f;
                pose.SwordAngle = stage switch
                {
                    0 => MathHelper.Lerp(-1.92f, .62f, progress),
                    1 => MathHelper.Lerp(.72f, -1.72f, progress),
                    2 => MathHelper.Lerp(.58f, -1.62f, progress),
                    _ => MathHelper.Lerp(-2.20f, 1.18f, progress)
                };
                pose.CloakTail = stage >= 2 ? -10f : -7f;
                break;

            case WeaponTechniqueEffect.GreatswordIronResolve:
                pose.Crouch = 8f;
                pose.Lean = -3f;
                pose.Step = -4f;
                pose.SwordHandX = -2f;
                pose.SwordHandY = -31f;
                pose.SwordAngle = -.18f;
                pose.CloakTail = -2f;
                break;

            case WeaponTechniqueEffect.GreatswordCrushingBreaker:
                pose.Crouch = 10f;
                pose.Lean = MathHelper.Lerp(-5f, 9f, progress);
                pose.Step = 5f;
                pose.SwordAngle = MathHelper.Lerp(-2.05f, .92f, progress);
                pose.CloakTail = -9f;
                break;

            case WeaponTechniqueEffect.GreatswordReversalCleave:
                pose.Crouch = 7f;
                pose.Lean = MathHelper.Lerp(-7f, 8f, progress);
                pose.Step = 4f;
                pose.SwordAngle = MathHelper.Lerp(.52f, -1.82f, progress);
                pose.CloakTail = -11f;
                break;

            case WeaponTechniqueEffect.GreatswordWhirlingJudgment:
                pose.Crouch = player.Combat.IsGreatswordWhirlStopping
                    ? MathHelper.Lerp(8f, 3f, progress)
                    : 8f;
                float side = (stage & 1) == 0 ? 1f : -1f;
                pose.Lean = player.Combat.IsGreatswordWhirlStopping
                    ? MathHelper.Lerp(side * 4f, 0f, progress)
                    : side * MathF.Sin(progress * MathHelper.Pi) * 5f;
                pose.Step = -side * 4f;
                pose.SwordAngle = player.Combat.IsGreatswordWhirlStopping
                    ? MathHelper.Lerp(side * .65f, -.72f, progress)
                    : stage == 6
                        ? MathHelper.Lerp(-2.12f, .92f, progress)
                        : side > 0f
                            ? MathHelper.Lerp(-2.12f, .72f, progress)
                            : MathHelper.Lerp(.78f, -1.98f, progress);
                pose.CloakTail = player.Combat.IsGreatswordWhirlStopping
                    ? -3f
                    : -10f;
                break;

            case WeaponTechniqueEffect.GreatswordVanguardStrike:
            case WeaponTechniqueEffect.GreatswordVanguardAssault:
                pose.Crouch = 11f;
                pose.Lean = 11f;
                pose.Step = 8f;
                pose.SwordAngle = MathHelper.Lerp(-2.18f, .96f, progress);
                pose.CloakTail = -15f;
                break;

            case WeaponTechniqueEffect.GreatswordFallingCleave:
            case WeaponTechniqueEffect.GreatswordFallingBastion:
                pose.Crouch = 6f;
                pose.Lean = 6f;
                pose.Step = -3f;
                pose.SwordAngle = MathHelper.Lerp(-2.06f, 1.10f, progress);
                pose.CloakTail = -9f;
                break;

            case WeaponTechniqueEffect.GreatswordTitanReckoning:
                pose.Crouch = player.Combat.IsGreatswordCharging ? 11f : 8f;
                pose.Lean = player.Combat.IsGreatswordCharging
                    ? -6f
                    : MathHelper.Lerp(-7f, 10f, progress);
                pose.Step = player.Combat.IsGreatswordCharging ? -6f : 5f;
                pose.SwordHandX = player.Combat.IsGreatswordCharging ? -3f : 7f;
                pose.SwordAngle = player.Combat.IsGreatswordCharging
                    ? -.18f
                    : MathHelper.Lerp(.62f, -1.82f, progress);
                pose.CloakTail = player.Combat.IsGreatswordCharging ? -3f : -12f;
                break;

            case WeaponTechniqueEffect.GreatswordIronDominion:
                pose.Crouch = stage == 0 ? 12f : 9f;
                pose.Lean = stage == 0 ? -5f :
                    ((stage & 1) == 0 ? -4f : 5f);
                pose.Step = stage == 0 ? -5f : 3f;
                pose.SwordAngle = stage switch
                {
                    0 => MathHelper.Lerp(-.25f, .12f, progress),
                    1 => MathHelper.Lerp(-2.04f, .72f, progress),
                    2 => MathHelper.Lerp(.76f, -1.98f, progress),
                    _ => MathHelper.Lerp(.62f, -1.72f, progress)
                };
                pose.CloakTail = stage == 0 ? -2f : -10f;
                break;

            case WeaponTechniqueEffect.GreatswordKingsRupture:
                pose.Crouch = player.Combat.IsGreatswordCharging ? 12f : 9f;
                pose.Lean = player.Combat.IsGreatswordCharging
                    ? -8f
                    : stage == 0 ? 4f : 10f;
                pose.Step = player.Combat.IsGreatswordCharging ? -6f : 4f;
                pose.SwordAngle = player.Combat.IsGreatswordCharging
                    ? -2.24f
                    : stage switch
                    {
                        0 => MathHelper.Lerp(-2.10f, .72f, progress),
                        1 => MathHelper.Lerp(-2.28f, 1.12f, progress),
                        _ => MathHelper.Lerp(.95f, .15f, progress)
                    };
                pose.CloakTail = -12f;
                break;

            case WeaponTechniqueEffect.GreatswordFinalVerdict:
                pose.Crouch = player.Combat.State == CombatState.ChargingHeavy
                    ? 8f
                    : stage >= 4 ? 12f : 9f;
                pose.Lean = player.Combat.State == CombatState.ChargingHeavy
                    ? -5f
                    : stage == 0 ? 7f : MathHelper.Lerp(-8f, 11f, progress);
                pose.Step = stage == 0 ? 7f : 2f;
                pose.SwordHandX = 4f;
                pose.SwordAngle = player.Combat.State == CombatState.ChargingHeavy
                    ? -2.12f
                    : stage switch
                    {
                        0 => -1.92f,
                        1 => MathHelper.Lerp(.70f, -1.95f, progress),
                        2 => MathHelper.Lerp(-2.30f, 1.20f, progress),
                        3 => MathHelper.Lerp(-2.42f, 1.30f, progress),
                        _ => .24f
                    };
                pose.CloakTail = stage == 0 ? -15f : -8f;
                break;
        }
    }

    private void ApplyBreakerTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.SpikedMace ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        pose.SwordLength = 58f;
        pose.SwordHandX = 8f;
        pose.SwordHandY = -32f;
        switch (effect)
        {
            case WeaponTechniqueEffect.BreakerIronCrush:
                pose.Crouch = stage == 2 ? 10f : 7f;
                pose.Lean = stage == 0 ? -5f : stage == 1 ? 6f : 8f;
                pose.Step = stage == 0 ? -4f : 5f;
                pose.SwordAngle = stage switch
                {
                    0 => LerpByFrame(-.28f, -2.58f),
                    1 => LerpByFrame(-2.35f, .48f),
                    _ => LerpByFrame(-1.95f, 1.28f)
                };
                pose.CloakTail = -7f;
                break;
            case WeaponTechniqueEffect.BreakerPendulumSwing:
                pose.Crouch = 8f;
                pose.Lean = MathF.Sin(player.Combat.StateElapsed * 7f) * 5f;
                pose.Step = -pose.Lean * .5f;
                if (player.Combat.IsPendulumCharging)
                {
                    float charge = player.Combat.HeavyChargeProgress;
                    pose.SwordAngle = -1.55f + charge * MathHelper.TwoPi;
                    pose.CloakTail = -5f - charge * 6f;
                }
                else
                {
                    pose.SwordAngle = LerpByFrame(-2.35f, .70f);
                    pose.CloakTail = -10f;
                }
                break;
            case WeaponTechniqueEffect.BreakerEarthbreaker:
                pose.Crouch = 11f;
                pose.Lean = 8f;
                pose.SwordHandX = 2f;
                pose.SwordAngle = LerpByFrame(-1.82f, 1.34f);
                pose.CloakTail = -6f;
                break;
            case WeaponTechniqueEffect.BreakerBatteringRush:
                pose.Crouch = 14f;
                pose.Lean = 13f;
                pose.Step = 8f;
                pose.SwordHandX = -8f;
                pose.SwordHandY = -22f;
                pose.SwordAngle = LerpByFrame(.55f, -.52f);
                pose.CloakTail = -17f;
                break;
            case WeaponTechniqueEffect.BreakerTitansBackhand:
                pose.Crouch = 9f;
                pose.Lean = LerpByFrame(8f, -7f);
                pose.Step = -5f;
                pose.SwordAngle = LerpByFrame(.72f, -2.62f);
                pose.CloakTail = -11f;
                break;
            case WeaponTechniqueEffect.BreakerAnvilFall:
                pose.Crouch = 12f;
                pose.Lean = 9f;
                pose.Step = 6f;
                pose.SwordHandX = 4f;
                pose.SwordAngle = LerpByFrame(-2.22f, 1.30f);
                pose.CloakTail = -8f;
                break;
            case WeaponTechniqueEffect.BreakerCataclysmWheel:
                pose.Crouch = 11f;
                pose.Lean = LerpByFrame(-6f, 9f);
                pose.Step = 5f;
                pose.SwordAngle = LerpByFrame(-2.48f, .98f);
                pose.CloakTail = -13f;
                break;
            case WeaponTechniqueEffect.BreakerWorldbreaker:
                float progress = Math.Clamp(
                    player.Combat.StateElapsed / 1.55f,
                    0f,
                    1f);
                pose.Crouch = progress < .74f ? 8f : 12f;
                pose.Step = progress < .74f ? -5f : 4f;
                if (progress < .30f)
                {
                    pose.Lean = -4f;
                    pose.SwordAngle = MathHelper.Lerp(-.45f, -1.62f,
                        progress / .30f);
                }
                else if (progress < .68f)
                {
                    float rotation = (progress - .30f) / .38f;
                    pose.Lean = MathF.Sin(rotation * MathHelper.TwoPi) * 6f;
                    pose.SwordAngle = -1.62f + rotation *
                        MathHelper.TwoPi * 1.35f;
                }
                else if (progress < .84f)
                {
                    float lift = (progress - .68f) / .16f;
                    pose.Lean = -5f;
                    pose.SwordAngle = MathHelper.Lerp(.62f, -1.92f, lift);
                }
                else
                {
                    float slam = (progress - .84f) / .16f;
                    pose.Lean = MathHelper.Lerp(-4f, 11f, slam);
                    pose.SwordAngle = MathHelper.Lerp(-1.92f, 1.30f, slam);
                }
                pose.CloakTail = -5f - progress * 7f;
                break;
        }
    }

    private void ApplySpellbladeTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            player.Combat.CurrentTechnique == null)
            return;

        int stage = player.Combat.TechniqueStageIndex;
        pose.SwordLength = 57f;
        pose.SwordHandX = 10f;
        pose.SwordHandY = -34f;
        switch (player.Combat.CurrentTechnique.Effect)
        {
            case WeaponTechniqueEffect.SpellbladeRunicStrikes:
                pose.Crouch = stage == 2 ? 7f : 4f;
                pose.Lean = stage == 0 ? 5f : stage == 1 ? -3f : 7f;
                pose.SwordAngle = stage switch
                {
                    0 => LerpByFrame(-1.38f, .52f),
                    1 => LerpByFrame(.72f, -1.42f),
                    _ => LerpByFrame(-.38f, .03f)
                };
                pose.CloakTail = stage == 2 ? -8f : -5f;
                break;
            case WeaponTechniqueEffect.SpellbladeRuneBrand:
                pose.Crouch = 3f;
                pose.Lean = -2f;
                pose.SwordAngle = player.Combat.IsRuneBrandCharging
                    ? -.08f
                    : LerpByFrame(-1.55f, -.08f);
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDetonation:
                pose.Crouch = 8f;
                pose.Lean = 2f;
                pose.SwordHandX = 5f;
                pose.SwordAngle = LerpByFrame(-2.12f, 1.42f);
                pose.CloakTail = -6f;
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneLunge:
                pose.Crouch = 11f;
                pose.Lean = 10f;
                pose.SwordHandX = 15f;
                pose.SwordAngle = .02f;
                pose.CloakTail = -15f;
                break;
            case WeaponTechniqueEffect.SpellbladeRunicSpear:
                pose.Crouch = 8f;
                pose.Lean = 7f;
                pose.SwordAngle = LerpByFrame(-1.20f, .02f);
                pose.CloakTail = -10f;
                break;
            case WeaponTechniqueEffect.SpellbladeResonanceBreaker:
                pose.Crouch = 7f;
                pose.Lean = 5f;
                pose.SwordAngle = LerpByFrame(-2.05f, 1.16f);
                pose.CloakTail = -7f;
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneConvergence:
                pose.Crouch = 9f;
                pose.Lean = -1f;
                pose.SwordHandX = 1f;
                pose.SwordAngle = 1.48f;
                break;
            case WeaponTechniqueEffect.SpellbladeArcaneDominion:
                pose.Crouch = stage == 3 ? 8f : 4f;
                pose.Lean = stage == 3 ? 5f : -2f;
                pose.SwordHandX = stage == 0 ? 2f : 6f;
                pose.SwordAngle = stage switch
                {
                    0 => LerpByFrame(-1.50f, -2.18f),
                    1 => 1.48f,
                    2 => 1.48f,
                    _ => LerpByFrame(-2.20f, 1.18f)
                };
                pose.CloakTail = stage == 3 ? -9f : -4f;
                break;
        }
    }

    private void ApplyRaiderTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.WarAxe ||
            player.Combat.CurrentTechnique == null)
            return;

        int stage = player.Combat.TechniqueStageIndex;
        switch (player.Combat.CurrentTechnique.Effect)
        {
            case WeaponTechniqueEffect.RaiderSavageCleave:
                pose.Crouch = stage == 2 ? 7f : 4f;
                pose.Lean = stage == 0 ? 5f : stage == 1 ? -3f : 8f;
                pose.SwordAngle = stage == 0
                    ? LerpByFrame(-.35f, 1.02f)
                    : stage == 1
                        ? LerpByFrame(.82f, -1.28f)
                        : LerpByFrame(-2.18f, .78f);
                pose.CloakTail = stage == 2 ? -10f : -6f;
                break;

            case WeaponTechniqueEffect.RaiderHookingAxe:
                pose.Crouch = 6f;
                pose.Lean = -2f;
                pose.SwordHandX = 14f;
                pose.SwordAngle = LerpByFrame(.72f, 2.30f);
                pose.CloakTail = -5f;
                break;

            case WeaponTechniqueEffect.RaiderBucklerRam:
                pose.Crouch = 11f;
                pose.Lean = 10f;
                pose.ShieldX = 11f;
                pose.ShieldY = -45f;
                pose.SwordHandX = -7f;
                pose.SwordAngle = .58f;
                pose.CloakTail = -13f;
                break;

            case WeaponTechniqueEffect.RaiderRavagersRush:
                pose.Crouch = 12f;
                pose.Lean = 11f;
                pose.ShieldX = 8f;
                pose.ShieldY = -43f;
                pose.SwordHandX = -13f;
                pose.SwordHandY = -24f;
                pose.SwordAngle = LerpByFrame(.68f, -.82f);
                pose.CloakTail = -16f;
                break;

            case WeaponTechniqueEffect.RaiderExecutionersGrip:
                pose.Crouch = 6f;
                pose.Lean = stage == 1 ? -4f : 5f;
                pose.SwordAngle = stage switch
                {
                    0 => LerpByFrame(-1.48f, .42f),
                    1 => LerpByFrame(.55f, 2.38f),
                    _ => LerpByFrame(1.02f, -1.20f)
                };
                pose.CloakTail = stage == 2 ? -9f : -5f;
                break;

            case WeaponTechniqueEffect.RaiderSkullbreaker:
                pose.Crouch = stage == 0 ? 10f : 7f;
                pose.Lean = stage == 0 ? 9f : 6f;
                pose.ShieldX = stage == 0 ? 12f : -13f;
                pose.ShieldY = stage == 0 ? -47f : -35f;
                pose.SwordAngle = stage == 0
                    ? -.72f
                    : LerpByFrame(-2.08f, .66f);
                pose.CloakTail = stage == 0 ? -11f : -8f;
                break;

            case WeaponTechniqueEffect.RaiderCrowdCrusher:
                pose.Crouch = 8f;
                pose.Lean = stage == 1 ? -5f : 7f;
                pose.ShieldX = stage == 2 ? 11f : -9f;
                pose.SwordAngle = stage == 0
                    ? LerpByFrame(.62f, 2.32f)
                    : stage == 1
                        ? LerpByFrame(2.10f, -.55f)
                        : .22f;
                pose.CloakTail = -10f;
                break;

            case WeaponTechniqueEffect.RaiderWarbringersDominion:
                if (stage == 0)
                {
                    pose.Crouch = 4f;
                    pose.Lean = -2f;
                    pose.ShieldX = 3f;
                    pose.ShieldY = -43f;
                    pose.SwordHandX = 4f;
                    pose.SwordAngle = 2.36f;
                }
                else if (stage == 1)
                {
                    pose.Crouch = 6f;
                    pose.Lean = 5f;
                    pose.SwordAngle = LerpByFrame(-2.20f, 1.08f);
                }
                else if (stage == 2)
                {
                    pose.Crouch = 12f;
                    pose.Lean = 12f;
                    pose.ShieldX = 12f;
                    pose.ShieldY = -45f;
                    pose.SwordAngle = .62f;
                    pose.CloakTail = -16f;
                }
                else
                {
                    pose.Crouch = 7f;
                    pose.Lean = 6f;
                    pose.SwordAngle = LerpByFrame(-2.42f, .74f);
                    pose.CloakTail = -11f;
                }
                break;
        }
    }

    private static void ApplyRangerTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.HunterBow ||
            player.Combat.CurrentTechnique == null)
            return;

        float charge = player.Combat.RangerChargeRatio;
        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        bool preservesLocomotion = effect is
            WeaponTechniqueEffect.RangerMobileShot or
            WeaponTechniqueEffect.RangerEvasiveShot or
            WeaponTechniqueEffect.RangerTwinFang or
            WeaponTechniqueEffect.RangerPhantomRetreat or
            WeaponTechniqueEffect.RangerPiercingVolley or
            WeaponTechniqueEffect.RangerBackstepShot or
            WeaponTechniqueEffect.RangerCounterShot or
            WeaponTechniqueEffect.RangerAerialShot or
            WeaponTechniqueEffect.RangerSkyfallVolley or
            WeaponTechniqueEffect.RangerSkyfallShot;
        if (preservesLocomotion && player.IsGrounded &&
            MathF.Abs(player.Velocity.X) > 1f)
        {
            pose.Step = MathF.Sin(player.Combat.StateElapsed * 24f) * 8f;
            pose.Crouch = 1f + MathF.Abs(
                MathF.Sin(player.Combat.StateElapsed * 24f)) * 2f;
            pose.CloakTail = -7f - MathF.Abs(pose.Step) * .35f;
        }
        switch (effect)
        {
            case WeaponTechniqueEffect.RangerHuntersDraw:
            case WeaponTechniqueEffect.RangerPiercingDraw:
                pose.Crouch = 2f + charge * 3f;
                pose.Lean = -2f - charge * 3f;
                pose.CloakTail = -3f - charge * 3f;
                break;
            case WeaponTechniqueEffect.RangerMobileShot:
                pose.Lean = player.Combat.TechniqueStageIndex >= 3 ? -1f : 2f;
                pose.SwordHandX = 21f;
                pose.SwordHandY = -39f;
                break;
            case WeaponTechniqueEffect.RangerEvasiveStep:
            case WeaponTechniqueEffect.RangerEvasiveShot:
                pose.Crouch = 10f;
                pose.Lean = -8f;
                pose.Step = -8f;
                pose.CloakTail = 12f;
                break;
            case WeaponTechniqueEffect.RangerTwinFang:
                pose.Crouch = player.Combat.TechniqueStageIndex == 0 ? 8f : 3f;
                pose.Lean = player.Combat.TechniqueStageIndex == 0 ? -7f : 3f;
                pose.CloakTail = 11f;
                break;
            case WeaponTechniqueEffect.RangerPhantomRetreat:
                pose.Crouch = player.Combat.TechniqueStageIndex == 0 ? 9f : 3f;
                pose.Lean = player.Combat.TechniqueStageIndex == 0 ? -8f : 1f;
                pose.CloakTail = 12f;
                break;
            case WeaponTechniqueEffect.RangerPiercingVolley:
                pose.Crouch = player.Combat.TechniqueStageIndex >= 2 ? 7f : 2f;
                pose.Lean = player.Combat.TechniqueStageIndex >= 2 ? -6f : 2f;
                pose.SwordHandX = 24f;
                break;
            case WeaponTechniqueEffect.RangerBackstepShot:
            case WeaponTechniqueEffect.RangerCounterShot:
                pose.Crouch = 6f;
                pose.Lean = 6f;
                pose.SwordHandX = 23f;
                pose.CloakTail = 10f;
                break;
            case WeaponTechniqueEffect.RangerSlidingShot:
                pose.Crouch = 14f;
                pose.Lean = 12f;
                pose.Step = 10f;
                pose.SwordHandY = -28f;
                pose.CloakTail = -16f;
                break;
            case WeaponTechniqueEffect.RangerPhantomBarrage:
                pose.Crouch = player.Combat.TechniqueStageIndex is 0 or 3
                    ? 8f
                    : 3f;
                pose.Lean = player.Combat.TechniqueStageIndex is 0 or 3
                    ? 8f
                    : -2f;
                pose.CloakTail = -12f;
                break;
            case WeaponTechniqueEffect.RangerArrowStorm:
                pose.Crouch = 5f;
                pose.Lean = -7f;
                pose.SwordHandY = -48f;
                break;
            case WeaponTechniqueEffect.RangerAerialShot:
            case WeaponTechniqueEffect.RangerSkyfallVolley:
            case WeaponTechniqueEffect.RangerSkyfallShot:
                pose.Crouch = 4f;
                pose.Step = -4f;
                pose.Lean = -1f;
                pose.SwordHandY = -43f;
                break;
            case WeaponTechniqueEffect.RangerPhantomHunt:
                pose.Crouch = player.Combat.State == CombatState.ChargingHeavy
                    ? 7f - player.Combat.RangerUltimateCinematicRatio * 3f
                    : 5f;
                pose.Lean = -6f;
                pose.SwordHandX = 24f;
                pose.SwordHandY = -44f;
                pose.CloakTail = -12f;
                break;
            case WeaponTechniqueEffect.RangerWindrunnerShot:
                pose.Crouch = 9f;
                pose.Lean = 9f;
                pose.CloakTail = -14f;
                break;
            case WeaponTechniqueEffect.RangerSkyfallMarker:
            case WeaponTechniqueEffect.RangerFallingStar:
                pose.Crouch = 5f;
                pose.Lean = -3f;
                pose.SwordHandY = -44f;
                pose.CloakTail = -7f;
                break;
            case WeaponTechniqueEffect.RangerDragonPiercer:
                pose.Crouch = 9f;
                pose.Lean = -5f;
                pose.CloakTail = -5f;
                break;
            case WeaponTechniqueEffect.RangerPredatorsHorizon:
                pose.Crouch = player.Combat.TechniqueStageIndex == 0 ? 8f : 5f;
                pose.Lean = player.Combat.TechniqueStageIndex == 0 ? -6f : 4f;
                pose.CloakTail = player.Combat.TechniqueStageIndex == 0 ? -13f : -5f;
                break;
            case WeaponTechniqueEffect.RangerHeavensFury:
                pose.Crouch = 4f;
                pose.Lean = -5f;
                pose.SwordHandX = 23f;
                pose.CloakTail = -8f;
                break;
            default:
                pose.Crouch += 2f;
                pose.Lean += 2f;
                break;
        }
    }

    private void ApplyDuelistTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.DualSwords ||
            player.Combat.CurrentTechnique == null)
            return;

        int stage = player.Combat.TechniqueStageIndex;
        switch (player.Combat.CurrentTechnique.Effect)
        {
            case WeaponTechniqueEffect.DuelistPhantomStep:
                pose.Crouch = 8f;
                pose.Lean = 8f;
                pose.SwordAngle = stage == 0 ? -.18f : .72f;
                pose.CloakTail = -13f;
                break;
            case WeaponTechniqueEffect.DuelistBladeTempest:
                pose.Crouch = 4f;
                pose.Lean = stage % 2 == 0 ? 5f : -3f;
                pose.SwordAngle = stage % 2 == 0 ? .72f : -1.28f;
                pose.CloakTail = -9f;
                break;
            case WeaponTechniqueEffect.DuelistCrossFangDash:
                pose.Crouch = 10f;
                pose.Lean = 9f;
                pose.SwordAngle = LerpByFrame(-.72f, .48f);
                pose.CloakTail = -15f;
                break;
            case WeaponTechniqueEffect.DuelistAfterimageExecution:
                pose.Crouch = stage == 0 ? 3f : 8f;
                pose.Lean = stage == 0 ? -2f : 7f;
                pose.SwordAngle = stage == 0
                    ? -.45f
                    : stage == 1 ? -1.15f : .65f;
                pose.CloakTail = stage == 0 ? -3f : -13f;
                break;
            case WeaponTechniqueEffect.DuelistHundredFangs:
                pose.Crouch = 6f;
                pose.Lean = 5f + stage % 2 * 2f;
                pose.SwordAngle = stage % 2 == 0 ? -.82f : .58f;
                pose.CloakTail = -8f;
                break;
            case WeaponTechniqueEffect.DuelistMirageCyclone:
                pose.Crouch = 7f;
                pose.Lean = stage % 2 == 0 ? 8f : -5f;
                pose.SwordAngle = stage % 2 == 0 ? -.95f : .78f;
                pose.CloakTail = -14f;
                break;
            case WeaponTechniqueEffect.DuelistFinalWaltz:
                pose.Crouch = stage == 0 ? 9f : 7f;
                pose.Lean = stage == 0 ? 0f : stage % 2 == 0 ? 8f : -5f;
                pose.SwordAngle = stage == 0
                    ? -1.52f
                    : stage == player.Combat.CurrentTechnique.StageCount - 1
                        ? .12f
                        : stage % 2 == 0 ? -.98f : .78f;
                pose.CloakTail = stage == 0 ? -4f : -15f;
                break;
        }
    }

    private void DrawDuelistAfterimages(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.DualSwords ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect =
            player.Combat.CurrentTechnique.Effect;
        bool active = effect is
            WeaponTechniqueEffect.DuelistPhantomStep or
            WeaponTechniqueEffect.DuelistCrossFangDash or
            WeaponTechniqueEffect.DuelistAfterimageExecution or
            WeaponTechniqueEffect.DuelistMirageCyclone or
            WeaponTechniqueEffect.DuelistFinalWaltz;
        if (!active)
            return;

        float intensity = effect == WeaponTechniqueEffect.DuelistFinalWaltz
            ? .30f + DuelistTuning.GetRatio(
                player.Combat.UltimateMomentumSnapshot) * .28f
            : .42f;
        float direction = flip ? 1f : -1f;
        DrawDuelistAfterimageSilhouette(
            anchor + new Vector2(direction * 20f, 0f),
            flip,
            pose,
            intensity);

        if (effect is WeaponTechniqueEffect.DuelistMirageCyclone or
            WeaponTechniqueEffect.DuelistFinalWaltz)
        {
            DrawDuelistAfterimageSilhouette(
                anchor + new Vector2(direction * 39f, -4f),
                flip,
                pose,
                intensity * .55f);
        }
    }

    private void DrawDuelistAfterimageSilhouette(
        Vector2 anchor,
        bool flip,
        Pose pose,
        float alpha)
    {
        Color body = ScaleAlpha(new Color(105, 132, 142, 155), alpha);
        Color blade = ScaleAlpha(new Color(194, 216, 218, 190), alpha);
        DrawBox(anchor, pose.Lean - 11f, -50f + pose.Crouch, 22f, 31f, body, flip);
        DrawBox(anchor, pose.Lean - 7f, -65f + pose.Crouch, 15f, 16f, body, flip);
        DrawLine(anchor, new Vector2(-8f, -19f), new Vector2(-12f, -1f), 5f, body, flip);
        DrawLine(anchor, new Vector2(8f, -19f), new Vector2(13f, -1f), 5f, body, flip);

        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        Vector2 mainDirection = new(
            MathF.Cos(pose.SwordAngle),
            MathF.Sin(pose.SwordAngle));
        float offAngle = -pose.SwordAngle - .15f;
        Vector2 offDirection = new(MathF.Cos(offAngle), MathF.Sin(offAngle));
        DrawLine(anchor, hand, hand + mainDirection * pose.SwordLength, 2f, blade, flip);
        Vector2 offHand = hand + new Vector2(-12f, 4f);
        DrawLine(anchor, offHand, offHand + offDirection * (pose.SwordLength - 9f), 2f, blade, flip);
    }

    private void DrawRangerAfterimages(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.HunterBow ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        bool movingEcho = player.Combat.TechniqueStageIndex == 0 &&
            effect is (WeaponTechniqueEffect.RangerEvasiveShot or
                WeaponTechniqueEffect.RangerTwinFang or
                WeaponTechniqueEffect.RangerPhantomRetreat or
                WeaponTechniqueEffect.RangerBackstepShot or
                WeaponTechniqueEffect.RangerPhantomHunt);
        if (!movingEcho)
            return;

        float direction = player.Combat.RangerWindrunnerDirection;
        if (MathF.Abs(direction) < .1f)
            direction = flip ? 1f : -1f;
        Vector2 echo = anchor - new Vector2(direction * 18f, 0f);
        Color body = new(57, 137, 163, 72);
        Color bow = new(129, 224, 239, 92);
        DrawBox(echo, pose.Lean - 10f, -49f + pose.Crouch, 20f, 30f,
            body, flip);
        DrawBox(echo, pose.Lean - 7f, -64f + pose.Crouch, 14f, 15f,
            body, flip);
        DrawLine(echo, new Vector2(12f, -52f), new Vector2(21f, -39f),
            3f, bow, flip);
        DrawLine(echo, new Vector2(21f, -39f), new Vector2(12f, -25f),
            3f, bow, flip);
    }

    private void ApplyKnightTechniquePose(PlayerCharacter player, ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.LongSword ||
            player.Combat.CurrentTechnique == null)
            return;

        string techniqueId = player.Combat.CurrentTechnique.Id;
        int stage = player.Combat.TechniqueStageIndex;

        switch (techniqueId)
        {
            case "iron-bastion":
                pose.Crouch = 4f;
                pose.Lean = -1f;
                pose.ShieldX = 8f;
                pose.ShieldY = -51f;
                pose.SwordHandX = -5f;
                pose.SwordAngle = -2.02f;
                break;
            case "shield-breaker":
                pose.Crouch += 3f;
                pose.Lean += 5f;
                pose.ShieldX = 10f;
                pose.ShieldY = -47f;
                pose.SwordHandX = -4f;
                pose.SwordAngle = -1.85f;
                break;
            case "vanguard-slash":
                pose.Crouch = 7f;
                pose.Lean = 7f;
                pose.ShieldX = 5f;
                pose.ShieldY = -45f;
                pose.SwordAngle = .05f;
                break;
            case "guardians-reversal":
                pose.Crouch += 2f;
                pose.ShieldX = 7f;
                pose.ShieldY = -49f;
                pose.SwordAngle = LerpByFrame(.85f, -1.08f);
                break;
            case "royal-cleaver":
                pose.Crouch += 4f;
                pose.Lean += 3f;
                pose.ShieldX = -9f;
                pose.ShieldY = -35f;
                pose.SwordAngle = LerpByFrame(-1.85f, .72f);
                break;
            case "iron-juggernaut":
                pose.Crouch = stage < 2 ? 9f : 6f;
                pose.Lean = stage < 2 ? 8f : 3f;
                pose.ShieldX = stage < 2 ? 10f : 4f;
                pose.ShieldY = stage < 2 ? -47f : -31f;
                pose.SwordAngle = -1.70f;
                break;
            case "oath-unbroken":
                if (stage == 0)
                {
                    pose.Crouch = 3f;
                    pose.ShieldX = 8f;
                    pose.ShieldY = -52f;
                    pose.SwordHandX = 2f;
                    pose.SwordHandY = -28f;
                    pose.SwordAngle = 1.48f;
                }
                else if (stage == 1)
                {
                    pose.Crouch = 7f;
                    pose.ShieldX = 4f;
                    pose.ShieldY = -30f;
                    pose.SwordAngle = -1.55f;
                }
                else
                {
                    pose.Crouch = 4f;
                    pose.ShieldX = -12f;
                    pose.SwordAngle = LerpByFrame(-2.25f, .62f);
                }
                break;
        }
    }

    private float LerpByFrame(float start, float end)
    {
        float amount = Math.Clamp(_animation.Frame / 8f, 0f, 1f);
        return MathHelper.Lerp(start, end, amount);
    }

    private void ApplyEquipmentPose(PlayerCharacter player, ref Pose pose)
    {
        bool isAttack = _animation.State is PlayerVisualState.LightAttack1 or
            PlayerVisualState.LightAttack2 or PlayerVisualState.LightAttack3 or
            PlayerVisualState.LightAttack4 or PlayerVisualState.LightAttack5 or
            PlayerVisualState.HeavyAttack;

        switch (player.WeaponFamily)
        {
            case WeaponFamily.SpikedMace:
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
                pose.SwordLength = 31f;
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

            case WeaponFamily.AbyssRing:
                pose.SwordLength = 0f;
                pose.SwordHandX = 16f;
                pose.SwordHandY = -34f;
                pose.Crouch += 4f;
                pose.Lean += 2f;
                pose.ShieldX = -30f;
                break;
        }
    }

    private void ApplySovereignTechniquePose(
        PlayerCharacter player,
        ref Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.AbyssRing)
            return;

        ConfigureSovereignStance(ref pose);
        int stage = player.Combat.TechniqueStageIndex;
        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (player.Combat.CurrentAttack == null)
            return;

        GetSovereignMotion(
            player.Combat.CurrentAttack,
            player.Combat.StateElapsed,
            out float drive,
            out float contact,
            out float recovery);

        switch (effect)
        {
            case WeaponTechniqueEffect.SovereignMartialArts:
                ApplySovereignComboPose(ref pose, stage, drive, contact,
                    recovery);
                break;
            case WeaponTechniqueEffect.SovereignOmnipresentStep:
                ApplyOmnipresentStepPose(player, ref pose, drive, recovery);
                break;
            case WeaponTechniqueEffect.SovereignParadoxImpact:
                ApplyParadoxImpactPose(ref pose, stage, drive, contact,
                    recovery);
                break;
            case WeaponTechniqueEffect.SovereignInfiniteRiftTheatre:
                ApplyRiftTheatrePose(ref pose, stage, drive, contact,
                    recovery);
                break;
            case WeaponTechniqueEffect.SovereignHeavenReversal:
                ApplyHeavenReversalPose(ref pose, drive, contact, recovery);
                break;
            case WeaponTechniqueEffect.SovereignZeroDistanceStrike:
                ApplyZeroDistancePose(ref pose, stage, drive, contact,
                    recovery);
                break;
            case WeaponTechniqueEffect.SovereignGoldenCataclysm:
                ApplyGoldenCataclysmPose(ref pose, stage, drive, contact,
                    recovery);
                break;
        }

        if (player.Combat.SovereignPerfectRiftVisualRatio > 0f)
        {
            ApplyPerfectRiftCounterPose(
                ref pose,
                1f - player.Combat.SovereignPerfectRiftVisualRatio);
        }
    }

    private static void ConfigureSovereignStance(ref Pose pose)
    {
        pose.UseMartialLimbs = true;
        pose.Crouch = 4f;
        pose.Lean = 1f;
        pose.Step = 2f;
        pose.SwordHandX = 19f;
        pose.SwordHandY = -38f;
        pose.FrontElbowX = 15f;
        pose.FrontElbowY = -43f;
        pose.OffHandX = -3f;
        pose.OffHandY = -39f;
        pose.RearElbowX = -16f;
        pose.RearElbowY = -43f;
        pose.FrontKneeX = 9f;
        pose.FrontKneeY = -12f;
        pose.FrontFootX = 14f;
        pose.FrontFootY = -1f;
        pose.RearKneeX = -8f;
        pose.RearKneeY = -12f;
        pose.RearFootX = -14f;
        pose.RearFootY = -1f;
        pose.CloakTail = -3f;
    }

    private static void GetSovereignMotion(
        AttackDefinition attack,
        float elapsed,
        out float drive,
        out float contact,
        out float recovery)
    {
        float startup = Math.Max(.01f, attack.StartupTime);
        float activeEnd = attack.StartupTime + attack.ActiveTime;
        float startupRatio = Math.Clamp(elapsed / startup, 0f, 1f);
        recovery = Math.Clamp(
            (elapsed - activeEnd) / Math.Max(.01f, attack.RecoveryTime),
            0f,
            1f);
        drive = MathHelper.SmoothStep(0f, 1f, startupRatio);
        if (elapsed >= attack.StartupTime)
            drive = 1f;
        drive = MathHelper.Lerp(drive, .18f, recovery);
        float activeRatio = Math.Clamp(
            (elapsed - attack.StartupTime) /
                Math.Max(.01f, attack.ActiveTime),
            0f,
            1f);
        contact = elapsed < attack.StartupTime
            ? 0f
            : 1f - Math.Clamp(MathF.Abs(activeRatio - .32f) * 2.1f, 0f, 1f);
    }

    private static void ApplySovereignComboPose(
        ref Pose pose,
        int stage,
        float drive,
        float contact,
        float recovery)
    {
        switch (stage)
        {
            case 0: // Sovereign Jab
                pose.Crouch = MathHelper.Lerp(5f, 2f, drive);
                pose.Lean = MathHelper.Lerp(-2f, 9f, drive);
                pose.Step = MathHelper.Lerp(-2f, 8f, drive);
                SetFrontArm(ref pose, drive, 7f, -40f, 3f, -45f,
                    43f, -39f, 25f, -43f);
                SetGuardArm(ref pose, -5f, -39f, -17f, -43f);
                pose.FrontFootX = MathHelper.Lerp(11f, 21f, drive);
                break;
            case 1: // Reversal Elbow
                pose.Crouch = MathHelper.Lerp(4f, 6f, drive);
                pose.Lean = MathHelper.Lerp(-8f, 11f, drive);
                pose.Step = MathHelper.Lerp(6f, -5f, drive);
                pose.SwordHandX = MathHelper.Lerp(-3f, 22f, drive);
                pose.SwordHandY = MathHelper.Lerp(-31f, -38f, drive);
                pose.FrontElbowX = MathHelper.Lerp(-8f, 34f, drive);
                pose.FrontElbowY = MathHelper.Lerp(-30f, -39f, drive);
                SetGuardArm(ref pose, 0f, -47f, -15f, -46f);
                pose.RearFootX = MathHelper.Lerp(-13f, -20f, drive);
                break;
            case 2: // Rift Palm
                pose.Crouch = MathHelper.Lerp(7f, 3f, drive);
                pose.Lean = MathHelper.Lerp(-7f, 10f, drive);
                pose.Step = MathHelper.Lerp(-4f, 7f, drive);
                SetFrontArm(ref pose, drive, 1f, -37f, -7f, -42f,
                    46f, -35f, 24f, -41f);
                SetGuardArm(ref pose, -2f, -42f, -15f, -45f);
                break;
            case 3: // Crescent Kick
                pose.Crouch = MathHelper.Lerp(7f, 2f, drive);
                pose.Lean = MathHelper.Lerp(4f, -8f, drive);
                pose.Step = -5f;
                pose.FrontKneeX = MathHelper.Lerp(10f, 27f, drive);
                pose.FrontKneeY = MathHelper.Lerp(-10f, -35f, drive);
                pose.FrontFootX = MathHelper.Lerp(13f, 53f, drive);
                pose.FrontFootY = MathHelper.Lerp(-1f, -43f, drive);
                SetGuardArm(ref pose, -1f, -43f, -15f, -45f);
                pose.SwordHandX = 12f;
                pose.SwordHandY = -47f;
                pose.FrontElbowX = 20f;
                pose.FrontElbowY = -48f;
                break;
            case 4: // Phantom Uppercut
                pose.Crouch = MathHelper.Lerp(13f, 1f, drive);
                pose.Lean = MathHelper.Lerp(-6f, 8f, drive);
                pose.Step = MathHelper.Lerp(-5f, 5f, drive);
                SetFrontArm(ref pose, drive, 3f, -20f, 8f, -29f,
                    25f, -63f, 17f, -45f);
                SetGuardArm(ref pose, -5f, -39f, -17f, -43f);
                pose.FrontFootX = 17f;
                pose.RearFootX = -18f;
                break;
            case 5: // Falling Knee
                pose.Crouch = MathHelper.Lerp(5f, -3f, drive);
                pose.Lean = MathHelper.Lerp(-3f, 9f, drive);
                pose.FrontKneeX = MathHelper.Lerp(9f, 34f, drive);
                pose.FrontKneeY = MathHelper.Lerp(-11f, -38f, drive);
                pose.FrontFootX = MathHelper.Lerp(14f, 20f, drive);
                pose.FrontFootY = MathHelper.Lerp(-1f, -21f, drive);
                pose.RearKneeY = MathHelper.Lerp(-12f, -18f, drive);
                SetGuardArm(ref pose, -2f, -43f, -15f, -46f);
                pose.SwordHandX = 17f;
                pose.SwordHandY = -47f;
                pose.FrontElbowX = 25f;
                pose.FrontElbowY = -43f;
                break;
            case 6: // Spatial Cross
                pose.Crouch = MathHelper.Lerp(8f, 4f, drive);
                pose.Lean = MathHelper.Lerp(-5f, 7f, drive);
                pose.Step = 6f;
                pose.SwordHandX = MathHelper.Lerp(1f, 43f, drive);
                pose.SwordHandY = MathHelper.Lerp(-42f, -35f, drive);
                pose.FrontElbowX = MathHelper.Lerp(9f, 25f, drive);
                pose.FrontElbowY = -42f;
                pose.OffHandX = MathHelper.Lerp(-2f, 36f,
                    Math.Clamp(drive + contact * .22f, 0f, 1f));
                pose.OffHandY = MathHelper.Lerp(-34f, -47f, drive);
                pose.RearElbowX = MathHelper.Lerp(-12f, 19f, drive);
                pose.RearElbowY = MathHelper.Lerp(-46f, -49f, drive);
                break;
            default: // Sovereign Break
                pose.Crouch = MathHelper.Lerp(10f, 3f, drive);
                pose.Lean = MathHelper.Lerp(-12f, 16f, drive);
                pose.Step = MathHelper.Lerp(-9f, 12f, drive);
                SetFrontArm(ref pose, drive, -12f, -29f, -8f, -38f,
                    55f, -35f, 29f, -41f);
                SetGuardArm(ref pose, -8f, -45f, -19f, -46f);
                pose.FrontFootX = MathHelper.Lerp(12f, 25f, drive);
                pose.RearFootX = MathHelper.Lerp(-16f, -26f, drive);
                pose.CloakTail = MathHelper.Lerp(-2f, -13f, drive);
                break;
        }

        if (recovery > 0f)
        {
            pose.Crouch += recovery * (stage == 7 ? 5f : 2f);
            pose.Lean -= recovery * (stage == 7 ? 5f : 2f);
        }
    }

    private static void ApplyOmnipresentStepPose(
        PlayerCharacter player,
        ref Pose pose,
        float drive,
        float recovery)
    {
        Vector2 travel = player.Combat.SovereignTransitDirection;
        float vertical = Math.Clamp(travel.Y, -1f, 1f);
        pose.Crouch = MathHelper.Lerp(10f, 5f, drive) + recovery * 3f;
        pose.Lean = MathHelper.Lerp(-3f, 12f, drive);
        pose.Step = MathHelper.Lerp(-7f, 11f, drive);
        pose.FrontKneeY -= Math.Max(0f, -vertical) * 10f * drive;
        pose.RearKneeY -= Math.Max(0f, vertical) * 8f * drive;
        pose.FrontFootX += 8f * drive;
        pose.RearFootX -= 7f * (1f - drive);
        pose.SwordHandX = MathHelper.Lerp(5f, 28f, drive);
        pose.SwordHandY = -43f + vertical * 8f;
        pose.FrontElbowX = MathHelper.Lerp(7f, 22f, drive);
        pose.FrontElbowY = -46f + vertical * 5f;
        SetGuardArm(ref pose, -5f, -40f, -16f, -44f);

    }

    private static void ApplyPerfectRiftCounterPose(
        ref Pose pose,
        float progress)
    {
        float counter = MathHelper.SmoothStep(
            0f,
            1f,
            Math.Clamp((progress - .28f) / .42f, 0f, 1f));
        float settle = MathHelper.SmoothStep(
            0f,
            1f,
            Math.Clamp((progress - .72f) / .28f, 0f, 1f));
        counter = MathHelper.Lerp(counter, .22f, settle);
        pose.Crouch = MathHelper.Lerp(10f, 4f, counter);
        pose.Lean = MathHelper.Lerp(-7f, 11f, counter);
        pose.Step = MathHelper.Lerp(-6f, 7f, counter);
        SetFrontArm(ref pose, counter, 3f, -47f, 16f, -49f,
            46f, -37f, 25f, -42f);
        pose.OffHandX = MathHelper.Lerp(9f, -4f, counter);
        pose.OffHandY = MathHelper.Lerp(-34f, -43f, counter);
        pose.RearElbowX = MathHelper.Lerp(-6f, -17f, counter);
        pose.RearElbowY = MathHelper.Lerp(-38f, -46f, counter);
        pose.FrontFootX = MathHelper.Lerp(12f, 21f, counter);
        pose.RearFootX = MathHelper.Lerp(-12f, -19f, counter);
    }

    private static void ApplyParadoxImpactPose(
        ref Pose pose,
        int stage,
        float drive,
        float contact,
        float recovery)
    {
        if (stage == 0)
        {
            pose.Crouch = 6f;
            pose.Lean = MathHelper.Lerp(-4f, 7f, drive);
            SetFrontArm(ref pose, drive, 2f, -39f, 2f, -45f,
                31f, -38f, 23f, -43f);
            pose.SwordHandX -= contact * 9f;
            return;
        }
        if (stage is >= 1 and <= 3)
        {
            bool reverse = stage % 2 == 0;
            pose.Crouch = 5f + recovery * 2f;
            pose.Lean = MathHelper.Lerp(reverse ? 5f : -5f,
                reverse ? -7f : 10f, drive);
            pose.Step = reverse ? -5f : 7f;
            if (!reverse)
            {
                SetFrontArm(ref pose, drive, 1f, -36f, -4f, -42f,
                    45f, -37f, 27f, -42f);
                SetGuardArm(ref pose, -3f, -43f, -16f, -46f);
            }
            else
            {
                pose.SwordHandX = 2f;
                pose.SwordHandY = -44f;
                pose.FrontElbowX = 17f;
                pose.FrontElbowY = -47f;
                pose.OffHandX = MathHelper.Lerp(-7f, 42f, drive);
                pose.OffHandY = MathHelper.Lerp(-35f, -38f, drive);
                pose.RearElbowX = MathHelper.Lerp(-18f, 21f, drive);
                pose.RearElbowY = -43f;
            }
            return;
        }
        if (stage == 4)
        {
            pose.Crouch = 10f;
            pose.Lean = 13f;
            pose.Step = 12f;
            pose.FrontFootX = 23f;
            pose.RearFootX = -22f;
            pose.CloakTail = -14f;
            return;
        }

        pose.Crouch = MathHelper.Lerp(7f, 3f, drive);
        pose.Lean = MathHelper.Lerp(8f, -10f, drive);
        pose.Step = -8f;
        pose.FrontKneeX = MathHelper.Lerp(10f, 25f, drive);
        pose.FrontKneeY = MathHelper.Lerp(-10f, -31f, drive);
        pose.FrontFootX = MathHelper.Lerp(13f, 51f, drive);
        pose.FrontFootY = MathHelper.Lerp(-1f, -24f, drive);
        pose.CloakTail = -11f;
    }

    private static void ApplyRiftTheatrePose(
        ref Pose pose,
        int stage,
        float drive,
        float contact,
        float recovery)
    {
        switch (stage)
        {
            case 0: // diagonal punch
                pose.Crouch = 5f;
                pose.Lean = MathHelper.Lerp(-5f, 11f, drive);
                pose.Step = 8f;
                SetFrontArm(ref pose, drive, 2f, -31f, -4f, -39f,
                    43f, -49f, 24f, -44f);
                break;
            case 1: // horizontal kick
                pose.Crouch = 3f;
                pose.Lean = MathHelper.Lerp(5f, -8f, drive);
                pose.FrontKneeX = MathHelper.Lerp(11f, 27f, drive);
                pose.FrontKneeY = MathHelper.Lerp(-11f, -28f, drive);
                pose.FrontFootX = MathHelper.Lerp(14f, 55f, drive);
                pose.FrontFootY = MathHelper.Lerp(-1f, -28f, drive);
                break;
            case 2: // rising elbow
                pose.Crouch = MathHelper.Lerp(11f, 1f, drive);
                pose.Lean = MathHelper.Lerp(-5f, 9f, drive);
                pose.SwordHandX = MathHelper.Lerp(2f, 20f, drive);
                pose.SwordHandY = MathHelper.Lerp(-28f, -57f, drive);
                pose.FrontElbowX = MathHelper.Lerp(-2f, 31f, drive);
                pose.FrontElbowY = MathHelper.Lerp(-34f, -53f, drive);
                break;
            case 3: // spinning kick, articulated rather than whole-body spin
                pose.Crouch = 4f;
                pose.Lean = MathHelper.Lerp(7f, -11f, drive);
                pose.Step = -9f;
                pose.FrontKneeX = MathHelper.Lerp(-2f, 28f, drive);
                pose.FrontKneeY = MathHelper.Lerp(-15f, -36f, drive);
                pose.FrontFootX = MathHelper.Lerp(-13f, 53f, drive);
                pose.FrontFootY = MathHelper.Lerp(-8f, -39f, drive);
                pose.OffHandX = -8f;
                pose.OffHandY = -48f;
                break;
            default:
                pose.Crouch = 8f + recovery * 3f;
                pose.Lean = MathHelper.Lerp(-8f, 13f, drive);
                pose.Step = MathHelper.Lerp(-8f, 10f, drive);
                SetFrontArm(ref pose, drive, -5f, -31f, -9f, -40f,
                    48f, -36f, 27f, -42f);
                break;
        }
        pose.CloakTail = -9f - drive * 5f;
    }

    private static void ApplyHeavenReversalPose(
        ref Pose pose,
        float drive,
        float contact,
        float recovery)
    {
        pose.Crouch = MathHelper.Lerp(-2f, 2f, recovery);
        pose.Lean = MathHelper.Lerp(7f, -9f, drive);
        pose.Step = -6f;
        pose.FrontKneeX = MathHelper.Lerp(12f, 25f, drive);
        pose.FrontKneeY = MathHelper.Lerp(-31f, -38f, drive);
        pose.FrontFootX = MathHelper.Lerp(5f, 35f, drive);
        pose.FrontFootY = MathHelper.Lerp(-24f, 4f, drive);
        pose.RearKneeX = -1f;
        pose.RearKneeY = -31f;
        pose.RearFootX = -18f;
        pose.RearFootY = -20f;
        pose.SwordHandX = 10f;
        pose.SwordHandY = -48f;
        pose.FrontElbowX = 23f;
        pose.FrontElbowY = -46f;
        SetGuardArm(ref pose, -3f, -45f, -17f, -46f);
        pose.CloakTail = -13f;
    }

    private static void ApplyZeroDistancePose(
        ref Pose pose,
        int stage,
        float drive,
        float contact,
        float recovery)
    {
        if (stage == 0)
        {
            pose.Crouch = 13f;
            pose.Lean = 14f;
            pose.Step = 13f;
            pose.SwordHandX = 18f;
            pose.SwordHandY = -31f;
            pose.FrontElbowX = 25f;
            pose.FrontElbowY = -37f;
            pose.OffHandX = -5f;
            pose.OffHandY = -33f;
            pose.RearElbowX = -15f;
            pose.RearElbowY = -39f;
            pose.FrontFootX = 24f;
            pose.RearFootX = -23f;
            pose.CloakTail = -16f;
            return;
        }

        pose.Crouch = MathHelper.Lerp(10f, 4f, drive) + recovery * 3f;
        pose.Lean = MathHelper.Lerp(8f, 16f, drive) - recovery * 4f;
        pose.Step = 12f;
        SetFrontArm(ref pose, drive, 2f, -29f, 4f, -38f,
            54f, -35f, 29f, -41f);
        SetGuardArm(ref pose, -5f, -43f, -18f, -45f);
        pose.FrontFootX = 25f;
        pose.RearFootX = -23f;
        pose.CloakTail = -14f;
    }

    private static void ApplyGoldenCataclysmPose(
        ref Pose pose,
        int stage,
        float drive,
        float contact,
        float recovery)
    {
        if (stage <= 0)
        {
            pose.Crouch = 6f;
            pose.Lean = -2f;
            pose.Step = -5f;
            pose.SwordHandX = 7f;
            pose.SwordHandY = -45f;
            pose.FrontElbowX = 19f;
            pose.FrontElbowY = -48f;
            SetGuardArm(ref pose, 4f, -34f, -10f, -40f);
            return;
        }

        if (stage <= AbyssRingTuning.UltimateStrikeCount)
        {
            int pattern = (stage - 1) % 4;
            float commitment = .82f + stage /
                (float)AbyssRingTuning.UltimateStrikeCount * .18f;
            switch (pattern)
            {
                case 0: // descending diagonal punch
                    pose.Crouch = 5f;
                    pose.Lean = MathHelper.Lerp(-6f, 12f, drive);
                    pose.Step = 8f;
                    SetFrontArm(ref pose, drive, 0f, -48f, -5f, -44f,
                        46f, -29f, 25f, -38f);
                    SetGuardArm(ref pose, -4f, -42f, -17f, -45f);
                    break;
                case 1: // crossing kick
                    pose.Crouch = 3f;
                    pose.Lean = MathHelper.Lerp(6f, -9f, drive);
                    pose.FrontKneeX = MathHelper.Lerp(8f, 26f, drive);
                    pose.FrontKneeY = MathHelper.Lerp(-10f, -31f, drive);
                    pose.FrontFootX = MathHelper.Lerp(13f, 52f, drive);
                    pose.FrontFootY = MathHelper.Lerp(-1f, -33f, drive);
                    pose.SwordHandX = 13f;
                    pose.SwordHandY = -48f;
                    pose.FrontElbowX = 22f;
                    pose.FrontElbowY = -47f;
                    break;
                case 2: // rising elbow
                    pose.Crouch = MathHelper.Lerp(11f, 1f, drive);
                    pose.Lean = MathHelper.Lerp(-5f, 10f, drive);
                    pose.SwordHandX = MathHelper.Lerp(1f, 20f, drive);
                    pose.SwordHandY = MathHelper.Lerp(-28f, -58f, drive);
                    pose.FrontElbowX = MathHelper.Lerp(-3f, 32f, drive);
                    pose.FrontElbowY = MathHelper.Lerp(-34f, -53f, drive);
                    SetGuardArm(ref pose, -4f, -40f, -16f, -44f);
                    break;
                default: // compact cross
                    pose.Crouch = 6f;
                    pose.Lean = MathHelper.Lerp(-7f, 9f, drive);
                    pose.Step = 7f;
                    SetFrontArm(ref pose, drive, 2f, -42f, 5f, -47f,
                        43f, -35f, 24f, -41f);
                    pose.OffHandX = MathHelper.Lerp(-4f, 34f,
                        Math.Clamp(drive + contact * .18f, 0f, 1f));
                    pose.OffHandY = MathHelper.Lerp(-35f, -47f, drive);
                    pose.RearElbowX = MathHelper.Lerp(-17f, 18f, drive);
                    pose.RearElbowY = -47f;
                    break;
            }
            pose.Lean *= commitment;
            pose.CloakTail = MathHelper.Lerp(-8f, -15f, commitment);
            return;
        }

        if (stage == AbyssRingTuning.UltimateStrikeCount + 1)
        {
            pose.Crouch = MathHelper.Lerp(12f, 2f, drive) + recovery * 5f;
            pose.Lean = MathHelper.Lerp(-10f, 13f, drive) - recovery * 4f;
            pose.Step = MathHelper.Lerp(-10f, 12f, drive);
            pose.SwordHandX = MathHelper.Lerp(-9f, 35f, drive);
            pose.SwordHandY = MathHelper.Lerp(-31f, -8f, drive);
            pose.FrontElbowX = MathHelper.Lerp(-5f, 25f, drive);
            pose.FrontElbowY = MathHelper.Lerp(-42f, -27f, drive);
            SetGuardArm(ref pose, -7f, -45f, -19f, -47f);
            pose.FrontFootX = MathHelper.Lerp(11f, 24f, drive);
            pose.RearFootX = MathHelper.Lerp(-15f, -25f, drive);
            pose.CloakTail = -16f;
            return;
        }

        pose.Crouch = MathHelper.Lerp(7f, 3f, recovery);
        pose.Lean = MathHelper.Lerp(8f, 1f, recovery);
        pose.Step = MathHelper.Lerp(7f, 2f, recovery);
        pose.SwordHandX = MathHelper.Lerp(31f, 17f, recovery);
        pose.SwordHandY = MathHelper.Lerp(-37f, -42f, recovery);
        pose.FrontElbowX = MathHelper.Lerp(24f, 16f, recovery);
        pose.FrontElbowY = -44f;
        SetGuardArm(ref pose, -4f, -40f, -16f, -44f);
    }

    private static void SetFrontArm(
        ref Pose pose,
        float amount,
        float handStartX,
        float handStartY,
        float elbowStartX,
        float elbowStartY,
        float handEndX,
        float handEndY,
        float elbowEndX,
        float elbowEndY)
    {
        pose.SwordHandX = MathHelper.Lerp(handStartX, handEndX, amount);
        pose.SwordHandY = MathHelper.Lerp(handStartY, handEndY, amount);
        pose.FrontElbowX = MathHelper.Lerp(elbowStartX, elbowEndX, amount);
        pose.FrontElbowY = MathHelper.Lerp(elbowStartY, elbowEndY, amount);
    }

    private static void SetGuardArm(
        ref Pose pose,
        float handX,
        float handY,
        float elbowX,
        float elbowY)
    {
        pose.OffHandX = handX;
        pose.OffHandY = handY;
        pose.RearElbowX = elbowX;
        pose.RearElbowY = elbowY;
    }

    private void DrawSovereignTransformation(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.AbyssRing)
            return;

        float x = pose.Lean;
        float y = pose.Bob + pose.Crouch +
            GetHeightOffset(player.Appearance.ApparentHeight);
        Color obsidian = player.IsHitFlashing
            ? new Color(72, 60, 42)
            : new Color(16, 17, 22);
        Color blackGold = new(77, 58, 25);
        Color gold = player.Combat.IsSovereignThroneActive
            ? new Color(255, 209, 73)
            : new Color(220, 164, 48);

        if (player.Combat.IsSovereignRiftEvading)
        {
            float trail = flip ? 18f : -18f;
            DrawBox(anchor, x - 11f + trail, y - 47f,
                22f, 37f, new Color(12, 10, 17, 105), flip);
            DrawLine(anchor,
                new Vector2(x - 6f + trail, y - 39f),
                new Vector2(x + 9f + trail, y - 27f),
                2f,
                new Color(231, 178, 48, 125),
                flip);
        }

        DrawBox(anchor, x - 12f, y - 48f, 24f, 25f, obsidian, flip);
        DrawBox(anchor, x - 10f, y - 46f, 4f, 18f, blackGold, flip);
        DrawBox(anchor, x + 3f, y - 43f, 3f, 15f, gold, flip);
        DrawLine(anchor, new Vector2(x - 8f, y - 40f),
            new Vector2(x + 8f, y - 32f), 2f, gold, flip);
        DrawBox(anchor, x + 2f, y - 59f, 7f, 4f, obsidian, flip);
        DrawBox(anchor, x + 5f, y - 58f, 3f, 2f, gold, flip);

        Vector2 fist = new(x + pose.SwordHandX, y + pose.SwordHandY);
        DrawBox(anchor, fist.X - 5f, fist.Y - 5f, 11f, 10f, obsidian, flip);
        DrawBox(anchor, fist.X + 1f, fist.Y - 4f, 4f, 3f, gold, flip);
        DrawRectangleOutlineLocal(anchor, fist.X - 7f, fist.Y - 7f,
            15f, 14f, 2f, gold, flip);

        if (player.Combat.IsSovereignThroneActive)
        {
            DrawRectangleOutlineLocal(anchor, x - 25f, y - 72f,
                50f, 58f, 2f, new Color(208, 156, 45, 120), flip);
            DrawRectangleOutlineLocal(anchor, x - 19f, y - 66f,
                38f, 46f, 1f, new Color(24, 21, 19, 180), flip);
        }
    }

    private void DrawSovereignAfterimages(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.AbyssRing ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        bool moving = effect is
                WeaponTechniqueEffect.SovereignOmnipresentStep or
                WeaponTechniqueEffect.SovereignHeavenReversal or
                WeaponTechniqueEffect.SovereignZeroDistanceStrike ||
            effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage >= 4 ||
            effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre &&
                stage <= 4 ||
            effect == WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                stage is >= 1 and <= 17;
        if (!moving)
            return;

        Vector2 travel = player.Combat.SovereignTransitDirection;
        if (travel.LengthSquared() < .01f)
            travel = new Vector2(flip ? -1f : 1f, 0f);
        else
            travel.Normalize();
        float speed = effect == WeaponTechniqueEffect.SovereignGoldenCataclysm
            ? MathHelper.Lerp(.72f, 1.18f,
                Math.Clamp(stage / 16f, 0f, 1f))
            : effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre
                ? 1f
                : .78f;
        DrawSovereignEcho(anchor - travel * (16f * speed), flip, pose, .34f);
        if (effect is WeaponTechniqueEffect.SovereignInfiniteRiftTheatre or
            WeaponTechniqueEffect.SovereignHeavenReversal ||
            effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage >= 4 ||
            effect == WeaponTechniqueEffect.SovereignGoldenCataclysm &&
                stage >= 9)
        {
            DrawSovereignEcho(
                anchor - travel * (31f * speed), flip, pose, .16f);
        }
    }

    private void DrawSovereignEcho(
        Vector2 anchor,
        bool flip,
        Pose pose,
        float alpha)
    {
        Color body = ScaleAlpha(new Color(13, 12, 18, 185), alpha);
        Color seam = ScaleAlpha(new Color(224, 169, 49, 180), alpha);
        float y = pose.Bob + pose.Crouch;
        DrawBox(anchor, pose.Lean - 11f, y - 48f, 22f, 27f, body, flip);
        DrawBox(anchor, pose.Lean - 7f, y - 65f, 15f, 16f, body, flip);
        DrawLine(anchor, new Vector2(pose.Lean + 6f, y - 22f),
            new Vector2(pose.FrontKneeX, pose.FrontKneeY), 6f, body, flip);
        DrawLine(anchor, new Vector2(pose.FrontKneeX, pose.FrontKneeY),
            new Vector2(pose.FrontFootX, pose.FrontFootY), 5f, body, flip);
        DrawLine(anchor, new Vector2(pose.Lean + 10f, y - 44f),
            new Vector2(pose.FrontElbowX, pose.FrontElbowY), 5f, body, flip);
        DrawLine(anchor, new Vector2(pose.FrontElbowX, pose.FrontElbowY),
            new Vector2(pose.SwordHandX, pose.SwordHandY), 3f, seam, flip);
    }

    private void DrawSovereignMartialEffects(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.AbyssRing ||
            player.Combat.CurrentTechnique == null ||
            player.Combat.CurrentAttack == null)
            return;

        float perfectRift = player.Combat.SovereignPerfectRiftVisualRatio;
        if (perfectRift > 0f)
        {
            float counterProgress = 1f - perfectRift;
            float counterFlash = 1f - Math.Clamp(
                MathF.Abs(counterProgress - .58f) * 4.2f,
                0f,
                1f);
            if (counterFlash > .02f)
            {
                Vector2 fist = new(pose.SwordHandX, pose.SwordHandY);
                Color counterGold = ScaleAlpha(
                    new Color(255, 217, 105),
                    counterFlash * .88f);
                DrawBox(anchor, fist.X - 3f, fist.Y - 3f,
                    7f, 7f, counterGold, flip);
                DrawLine(anchor, fist, fist + new Vector2(18f, 0f),
                    2f, counterGold, flip);
            }
        }

        AttackDefinition attack = player.Combat.CurrentAttack;
        float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
        if (activeElapsed < -.025f ||
            activeElapsed > attack.ActiveTime + .055f ||
            attack.DamageMultiplier <= 0f)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        bool legContact =
            effect == WeaponTechniqueEffect.SovereignMartialArts &&
                stage is 3 or 5 ||
            effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage == 5 ||
            effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre &&
                stage is 1 or 3 ||
            effect == WeaponTechniqueEffect.SovereignHeavenReversal;
        Vector2 contact = legContact
            ? new Vector2(pose.FrontFootX, pose.FrontFootY)
            : new Vector2(pose.SwordHandX, pose.SwordHandY);
        Vector2 strikeDirection = legContact &&
            effect == WeaponTechniqueEffect.SovereignHeavenReversal
                ? Vector2.UnitY
                : Vector2.UnitX;
        float fade = 1f - Math.Clamp(
            MathF.Max(0f, activeElapsed) /
                Math.Max(.01f, attack.ActiveTime + .055f),
            0f,
            1f);
        Color gold = ScaleAlpha(new Color(250, 202, 83), .35f + fade * .55f);
        DrawBox(anchor, contact.X - 3f, contact.Y - 3f, 6f, 6f, gold, flip);
        DrawLine(anchor, contact,
            contact + strikeDirection * (13f + (stage == 7 ? 8f : 0f)),
            stage == 7 ? 3f : 2f, gold, flip);
        DrawLine(anchor, contact,
            contact + strikeDirection * 9f + new Vector2(0f, -6f),
            1f, gold, flip);
        DrawLine(anchor, contact,
            contact + strikeDirection * 8f + new Vector2(0f, 6f),
            1f, gold, flip);

        if (effect == WeaponTechniqueEffect.SovereignMartialArts && stage == 6)
        {
            Vector2 second = new(pose.OffHandX, pose.OffHandY);
            DrawBox(anchor, second.X - 2f, second.Y - 2f, 5f, 5f, gold, flip);
            DrawLine(anchor, second, second + new Vector2(13f, -3f),
                2f, gold, flip);
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
        if (player.WeaponFamily == WeaponFamily.AbyssRing &&
            pose.UseMartialLimbs)
        {
            DrawSovereignLegs(player, anchor, flip, pose, bodyX, bodyY);
            return;
        }

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

    private void DrawSovereignLegs(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        Color obsidian = Tint(new Color(17, 18, 23), player);
        Color gold = Tint(new Color(174, 126, 34), player);
        Vector2 frontHip = new(bodyX + 6f, bodyY - 22f);
        Vector2 rearHip = new(bodyX - 6f, bodyY - 22f);
        Vector2 frontKnee = new(pose.FrontKneeX, pose.FrontKneeY);
        Vector2 frontFoot = new(pose.FrontFootX, pose.FrontFootY);
        Vector2 rearKnee = new(pose.RearKneeX, pose.RearKneeY);
        Vector2 rearFoot = new(pose.RearFootX, pose.RearFootY);

        DrawLine(anchor, rearHip, rearKnee, 8f, Outline, flip);
        DrawLine(anchor, rearKnee, rearFoot, 7f, Outline, flip);
        DrawLine(anchor, rearHip, rearKnee, 5f, obsidian, flip);
        DrawLine(anchor, rearKnee, rearFoot, 4f, obsidian, flip);
        DrawLine(anchor, frontHip, frontKnee, 9f, Outline, flip);
        DrawLine(anchor, frontKnee, frontFoot, 8f, Outline, flip);
        DrawLine(anchor, frontHip, frontKnee, 6f, obsidian, flip);
        DrawLine(anchor, frontKnee, frontFoot, 5f, obsidian, flip);
        DrawLine(anchor, frontKnee, frontFoot, 1f, gold, flip);
        Vector2 footDirection = Vector2.Normalize(
            frontFoot - frontKnee + new Vector2(.01f, 0f));
        DrawLine(anchor, frontFoot - footDirection * 3f,
            frontFoot + footDirection * 8f, 6f, Outline, flip);
        DrawLine(anchor, frontFoot - footDirection * 2f,
            frontFoot + footDirection * 7f, 3f, obsidian, flip);
        Vector2 rearFootDirection = Vector2.Normalize(
            rearFoot - rearKnee + new Vector2(.01f, 0f));
        DrawLine(anchor, rearFoot - rearFootDirection * 2f,
            rearFoot + rearFootDirection * 7f, 5f, obsidian, flip);
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
        float shoulderWidth = 10f * GetTorsoScale(player.Appearance);
        Vector2 shoulder = new(bodyX - shoulderWidth, bodyY - 44f);

        if (player.WeaponFamily == WeaponFamily.AbyssRing &&
            pose.UseMartialLimbs)
        {
            DrawSovereignArm(player, anchor, flip, shoulder,
                new Vector2(pose.RearElbowX, pose.RearElbowY),
                new Vector2(pose.OffHandX, pose.OffHandY),
                front: false);
            return;
        }

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
                Tint(GetSkinColor(player.Appearance.SkinTone), player),
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
        CharacterAppearance appearance = player.Appearance;
        Color skinBase = GetSkinColor(appearance.SkinTone);
        Color skin = Tint(skinBase, player);
        Color skinLight = Tint(Color.Lerp(skinBase, Color.White, .18f), player);
        Color hair = Tint(GetHairColor(appearance.HairColor), player);
        Color hairLight = Tint(Color.Lerp(hair, Color.White, .16f), player);
        float faceWidth = appearance.FacePreset switch
        {
            FacePreset.Round => 2f,
            FacePreset.Narrow => -1f,
            FacePreset.Strong => 1f,
            _ => 0f
        };
        if (appearance.BodyType == CharacterBodyType.Female)
            faceWidth -= 1f;

        float headX = bodyX - 8f - faceWidth * .5f;
        float headWidth = 17f + faceWidth;
        float jawY = appearance.FacePreset == FacePreset.Round ? -50f : -51f;
        float noseLength = appearance.FacePreset is FacePreset.Angular or FacePreset.Strong
            ? 5f
            : 4f;

        DrawBox(anchor, headX, bodyY - 66f, headWidth, 18f, Outline, flip);
        DrawBox(anchor, headX + 2f, bodyY - 64f, headWidth - 3f, 15f, skin, flip);
        DrawBox(anchor, bodyX - 4f, bodyY - 62f, 10f, 7f, skinLight, flip);
        if (appearance.FacePreset == FacePreset.Round)
            DrawBox(anchor, bodyX - 5f, bodyY - 55f, 12f, 5f, skin, flip);
        else if (appearance.FacePreset == FacePreset.Strong)
            DrawBox(anchor, bodyX - 3f, bodyY - 52f, 10f, 4f, skin, flip);

        DrawBox(anchor, bodyX + 7f, bodyY - 60f, noseLength, 6f, Outline, flip);
        DrawBox(anchor, bodyX + 7f, bodyY - 59f, noseLength - 2f, 4f, skinLight, flip);
        DrawBox(anchor, bodyX + 4f, bodyY + jawY, 5f, 3f, Outline, flip);
        DrawBox(anchor, bodyX + 3f, bodyY - 53f, 5f, 3f, skin, flip);

        DrawHair(
            appearance.HairStyle,
            anchor,
            flip,
            bodyX,
            bodyY,
            hair,
            hairLight);

        int eyeY = appearance.FacePreset == FacePreset.Narrow ? -58 : -59;
        DrawBox(anchor, bodyX + 4f, bodyY + eyeY, 3f, 3f, new Color(30, 27, 27), flip);
        DrawBox(anchor, bodyX + 4f, bodyY - 54f, 4f, 1f, new Color(91, 48, 42), flip);
        DrawFacialHair(appearance.FacialHair, anchor, flip, bodyX, bodyY, hair);
        DrawFaceDetail(appearance.FaceDetail, anchor, flip, bodyX, bodyY);
    }

    private void DrawHair(
        HairStyle style,
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY,
        Color hair,
        Color hairLight)
    {
        if (style == HairStyle.Bald)
            return;

        switch (style)
        {
            case HairStyle.Medium:
                DrawBox(anchor, bodyX - 9f, bodyY - 68f, 18f, 8f, hair, flip);
                DrawBox(anchor, bodyX - 10f, bodyY - 64f, 6f, 16f, hairLight, flip);
                DrawBox(anchor, bodyX - 7f, bodyY - 55f, 5f, 8f, hair, flip);
                DrawBox(anchor, bodyX + 1f, bodyY - 62f, 7f, 2f, hair, flip);
                break;
            case HairStyle.Long:
                DrawBox(anchor, bodyX - 9f, bodyY - 68f, 18f, 8f, hair, flip);
                DrawBox(anchor, bodyX - 11f, bodyY - 64f, 7f, 23f, hairLight, flip);
                DrawBox(anchor, bodyX - 8f, bodyY - 56f, 6f, 18f, hair, flip);
                DrawBox(anchor, bodyX + 1f, bodyY - 62f, 7f, 2f, hair, flip);
                break;
            case HairStyle.Messy:
                DrawBox(anchor, bodyX - 8f, bodyY - 69f, 17f, 9f, hair, flip);
                DrawBox(anchor, bodyX - 7f, bodyY - 73f, 5f, 6f, hairLight, flip);
                DrawBox(anchor, bodyX, bodyY - 75f, 5f, 8f, hair, flip);
                DrawBox(anchor, bodyX + 5f, bodyY - 71f, 6f, 5f, hairLight, flip);
                DrawBox(anchor, bodyX - 9f, bodyY - 64f, 5f, 10f, hair, flip);
                break;
            case HairStyle.TiedBack:
                DrawBox(anchor, bodyX - 8f, bodyY - 68f, 17f, 8f, hair, flip);
                DrawBox(anchor, bodyX - 9f, bodyY - 64f, 5f, 11f, hairLight, flip);
                DrawBox(anchor, bodyX - 14f, bodyY - 62f, 7f, 7f, hair, flip);
                DrawBox(anchor, bodyX - 17f, bodyY - 59f, 6f, 11f, hairLight, flip);
                DrawBox(anchor, bodyX - 15f, bodyY - 49f, 4f, 7f, hair, flip);
                break;
            case HairStyle.Undercut:
                DrawBox(anchor, bodyX - 7f, bodyY - 69f, 17f, 7f, hair, flip);
                DrawBox(anchor, bodyX - 3f, bodyY - 72f, 13f, 5f, hairLight, flip);
                DrawBox(anchor, bodyX - 8f, bodyY - 63f, 3f, 6f, hair, flip);
                break;
            default:
                DrawBox(anchor, bodyX - 8f, bodyY - 68f, 17f, 8f, hair, flip);
                DrawBox(anchor, bodyX - 9f, bodyY - 64f, 5f, 11f, hairLight, flip);
                DrawBox(anchor, bodyX - 6f, bodyY - 62f, 3f, 8f, hair, flip);
                DrawBox(anchor, bodyX + 1f, bodyY - 62f, 7f, 2f, hair, flip);
                break;
        }
    }

    private void DrawFacialHair(
        FacialHairStyle style,
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY,
        Color hair)
    {
        switch (style)
        {
            case FacialHairStyle.LightStubble:
                Color stubble = new(hair.R, hair.G, hair.B, (byte)155);
                DrawBox(anchor, bodyX + 2f, bodyY - 55f, 6f, 2f, stubble, flip);
                DrawBox(anchor, bodyX, bodyY - 52f, 7f, 2f, stubble, flip);
                break;
            case FacialHairStyle.ShortBeard:
                DrawBox(anchor, bodyX + 1f, bodyY - 54f, 8f, 4f, hair, flip);
                DrawBox(anchor, bodyX - 2f, bodyY - 52f, 9f, 5f, hair, flip);
                break;
            case FacialHairStyle.FullBeard:
                DrawBox(anchor, bodyX, bodyY - 55f, 9f, 6f, hair, flip);
                DrawBox(anchor, bodyX - 4f, bodyY - 52f, 11f, 8f, hair, flip);
                DrawBox(anchor, bodyX - 1f, bodyY - 45f, 7f, 4f, hair, flip);
                break;
            case FacialHairStyle.Mustache:
                DrawBox(anchor, bodyX + 2f, bodyY - 55f, 4f, 2f, hair, flip);
                DrawBox(anchor, bodyX + 6f, bodyY - 54f, 3f, 2f, hair, flip);
                break;
        }
    }

    private void DrawFaceDetail(
        FaceDetail detail,
        Vector2 anchor,
        bool flip,
        float bodyX,
        float bodyY)
    {
        Color mark = new(104, 54, 48);
        switch (detail)
        {
            case FaceDetail.EyebrowScar:
                DrawBox(anchor, bodyX + 2f, bodyY - 62f, 2f, 2f, mark, flip);
                DrawBox(anchor, bodyX + 4f, bodyY - 60f, 2f, 2f, mark, flip);
                break;
            case FaceDetail.CheekScar:
                DrawBox(anchor, bodyX + 3f, bodyY - 57f, 2f, 2f, mark, flip);
                DrawBox(anchor, bodyX + 5f, bodyY - 55f, 2f, 3f, mark, flip);
                break;
            case FaceDetail.EyeScar:
                DrawBox(anchor, bodyX + 5f, bodyY - 62f, 1f, 7f, mark, flip);
                break;
            case FaceDetail.SmallFacialMark:
                DrawBox(anchor, bodyX + 1f, bodyY - 55f, 2f, 2f, mark, flip);
                break;
        }
    }

    private void DrawFrontArm(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose,
        float bodyX,
        float bodyY)
    {
        float shoulderWidth = 12f * GetTorsoScale(player.Appearance);
        Vector2 shoulder = new(bodyX + shoulderWidth, bodyY - 44f);
        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        if (player.WeaponFamily == WeaponFamily.AbyssRing &&
            pose.UseMartialLimbs)
        {
            DrawSovereignArm(player, anchor, flip, shoulder,
                new Vector2(pose.FrontElbowX, pose.FrontElbowY),
                hand,
                front: true);
            return;
        }
        DrawLine(anchor, shoulder, hand, 8f, Outline, flip);
        DrawLine(anchor, shoulder, hand, 5f, Tint(Leather, player), flip);
        DrawLine(
            anchor,
            Vector2.Lerp(shoulder, hand, 0.48f),
            hand,
            5f,
            Tint(SteelDark, player),
            flip);
        DrawBox(
            anchor,
            hand.X - 2f,
            hand.Y - 2f,
            5f,
            5f,
            Tint(GetSkinColor(player.Appearance.SkinTone), player),
            flip);
    }

    private void DrawSovereignArm(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Vector2 shoulder,
        Vector2 elbow,
        Vector2 hand,
        bool front)
    {
        Color sleeve = Tint(new Color(18, 19, 24), player);
        Color bracer = Tint(new Color(87, 65, 27), player);
        Color skin = Tint(GetSkinColor(player.Appearance.SkinTone), player);
        DrawLine(anchor, shoulder, elbow, front ? 9f : 8f, Outline, flip);
        DrawLine(anchor, elbow, hand, front ? 8f : 7f, Outline, flip);
        DrawLine(anchor, shoulder, elbow, front ? 6f : 5f, sleeve, flip);
        DrawLine(anchor, elbow, hand, front ? 5f : 4f, bracer, flip);
        DrawBox(anchor, hand.X - 3f, hand.Y - 3f, 7f, 7f, Outline, flip);
        DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, skin, flip);
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
            DrawDagger(anchor, flip, rearHand, rearDirection, pose.SwordLength - 9f, blade);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.Bow)
        {
            WeaponTechniqueEffect rangerEffect =
                player.Combat.CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None;
            Vector2 shotDirection = rangerEffect is
                WeaponTechniqueEffect.RangerSkyfallMarker or
                WeaponTechniqueEffect.RangerFallingStar or
                WeaponTechniqueEffect.RangerArrowStorm
                    ? Vector2.Normalize(new Vector2(.45f, -.89f))
                    : rangerEffect is WeaponTechniqueEffect.RangerSkyfallVolley or
                        WeaponTechniqueEffect.RangerSkyfallShot
                        ? Vector2.Normalize(new Vector2(1f, 1f))
                    : Vector2.UnitX;
            if (player.Combat.RangerDrawState == RangerDrawState.Perfect &&
                player.Combat.State == CombatState.ChargingHeavy)
            {
                hand += new Vector2(
                    MathF.Sin(player.Combat.StateElapsed * 82f) * .8f,
                    0f);
            }
            shotDirection.Normalize();
            Vector2 bowAxis = new(-shotDirection.Y, shotDirection.X);
            Vector2 bowTop = hand - bowAxis * 23f;
            Vector2 bowUpper = hand - bowAxis * 12f + shotDirection * 8f;
            Vector2 bowLower = hand + bowAxis * 12f + shotDirection * 8f;
            Vector2 bowBottom = hand + bowAxis * 23f;
            Color wood = Tint(new Color(117, 74, 42), player);
            DrawLine(anchor, bowTop, bowUpper, 4f, Outline, flip);
            DrawLine(anchor, bowUpper, bowLower, 4f, Outline, flip);
            DrawLine(anchor, bowLower, bowBottom, 4f, Outline, flip);
            DrawLine(anchor, bowTop, bowUpper, 2f, wood, flip);
            DrawLine(anchor, bowUpper, bowLower, 2f, wood, flip);
            DrawLine(anchor, bowLower, bowBottom, 2f, wood, flip);
            bool isHeavensFury = player.Combat.CurrentTechnique?.Effect is
                WeaponTechniqueEffect.RangerHeavensFury or
                WeaponTechniqueEffect.RangerPhantomHunt;
            bool phantomExecution = player.Combat.CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.RangerPhantomHunt &&
                player.Combat.TechniqueStageIndex ==
                    RangerTuning.PhantomHuntArrowCount + 1;
            float ultimateFormation = isHeavensFury
                ? player.Combat.CurrentTechnique?.Effect ==
                        WeaponTechniqueEffect.RangerPhantomHunt
                    ? player.Combat.State == CombatState.ChargingHeavy
                        ? player.Combat.RangerUltimateCinematicRatio
                        : phantomExecution
                            ? Math.Clamp(
                                player.Combat.StateElapsed /
                                    MathF.Max(.01f,
                                        player.Combat.CurrentAttack?.StartupTime ?? .01f),
                                0f,
                                1f)
                            : .38f
                    : player.Combat.HeavyChargeProgress
                : 0f;
            float drawAmount = player.Combat.State == CombatState.ChargingHeavy
                ? isHeavensFury
                    ? MathHelper.Lerp(.38f, .78f, ultimateFormation)
                    : player.Combat.RangerChargeRatio
                : phantomExecution
                    ? MathHelper.Lerp(.58f, 1f, ultimateFormation)
                : player.Combat.IsAttackWindingUp ? .48f : .12f;
            Vector2 stringHand = hand - shotDirection * (7f + drawAmount * 13f);
            DrawLine(anchor, bowTop, stringHand, 1f, new Color(202, 194, 164), flip);
            DrawLine(anchor, stringHand, bowBottom, 1f, new Color(202, 194, 164), flip);
            bool isHunterDraw = player.Combat.State == CombatState.ChargingHeavy &&
                player.Combat.CurrentTechnique?.Effect is
                    WeaponTechniqueEffect.RangerHuntersDraw or
                    WeaponTechniqueEffect.RangerPiercingDraw;
            if (isHunterDraw && player.Combat.RangerChargeRatio < .22f)
            {
                float nockProgress = player.Combat.RangerChargeRatio / .22f;
                Vector2 quiverReach = hand + new Vector2(-29f, 8f);
                Vector2 nockPosition = Vector2.Lerp(
                    quiverReach,
                    stringHand,
                    nockProgress);
                DrawLine(
                    anchor,
                    nockPosition,
                    nockPosition + shotDirection * 22f,
                    2f,
                    new Color(151, 111, 65),
                    flip);
            }
            if (drawAmount > .18f)
            {
                bool phantomHunt = player.Combat.CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.RangerPhantomHunt;
                Color arrowColor = isHeavensFury
                    ? phantomHunt
                        ? Color.Lerp(
                            new Color(72, 166, 192),
                            new Color(177, 245, 255),
                            ultimateFormation)
                        : Color.Lerp(
                            new Color(151, 127, 85),
                            new Color(226, 205, 135),
                            ultimateFormation)
                    : drawAmount >= .98f
                    ? new Color(245, 218, 118)
                    : new Color(166, 131, 78);
                Vector2 arrowTip = hand + shotDirection * 31f;
                DrawLine(
                    anchor,
                    stringHand,
                    arrowTip,
                    drawAmount >= .98f ? 3f : 2f,
                    arrowColor,
                    flip);
                if (!isHeavensFury &&
                    player.Combat.RangerDrawState == RangerDrawState.Perfect)
                    DrawBox(anchor, arrowTip.X - 2f, arrowTip.Y - 2f, 4f, 4f, arrowColor, flip);
            }
            return;
        }

        if (player.WeaponFamily == WeaponFamily.ChainFlail)
        {
            DrawChainFlail(player, anchor, flip, hand, handleEnd);
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
            DrawLine(anchor, handleEnd, bladeEnd, 4f,
                Tint(new Color(48, 39, 38), player), flip);
            Vector2 focus = bladeEnd + direction * 5f;
            Color arcane = player.Combat.IsChargingHeavy
                ? Color.Lerp(new Color(106, 77, 151), new Color(202, 155, 239), player.Combat.HeavyChargeProgress)
                : new Color(124, 91, 169);
            DrawLine(anchor, focus - perpendicular * 8f, focus + perpendicular * 8f, 5f, Outline, flip);
            DrawLine(anchor, focus - direction * 8f, focus + direction * 8f, 5f, Outline, flip);
            for (int ring = 0; ring < 3; ring++)
            {
                Vector2 ringCenter = bladeEnd - direction * (3f + ring * 6f);
                float ringHalf = 7f - ring;
                DrawLine(anchor,
                    ringCenter - perpendicular * ringHalf,
                    ringCenter + perpendicular * ringHalf,
                    2f,
                    ring == 1 ? SteelLight : SteelDark,
                    flip);
            }
            DrawLine(anchor, focus - perpendicular * 6f,
                focus + direction * 6f, 4f, Outline, flip);
            DrawLine(anchor, focus + direction * 6f,
                focus + perpendicular * 6f, 4f, Outline, flip);
            DrawLine(anchor, focus + perpendicular * 6f,
                focus - direction * 6f, 4f, Outline, flip);
            DrawLine(anchor, focus - direction * 6f,
                focus - perpendicular * 6f, 4f, Outline, flip);
            DrawBox(anchor, focus.X - 3f, focus.Y - 3f, 7f, 7f, arcane, flip);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.BattleAxe)
        {
            DrawLine(anchor, hand, bladeEnd, 6f, Outline, flip);
            DrawLine(anchor, hand, bladeEnd, 3f, Tint(LeatherLight, player), flip);
            Vector2 axeNeck = bladeEnd - direction * 5f;
            Vector2 frontHeel = axeNeck - perpendicular * 4f;
            Vector2 frontEdge = axeNeck - perpendicular * 13f + direction * 3f;
            DrawLine(
                anchor,
                frontHeel,
                frontEdge,
                8f,
                Outline,
                flip);
            DrawLine(
                anchor,
                frontHeel,
                frontEdge,
                5f,
                blade,
                flip);
            DrawLine(
                anchor,
                frontEdge,
                bladeEnd - perpendicular * 12f,
                2f,
                SteelLight,
                flip);
            Vector2 hookRoot = axeNeck + perpendicular * 4f;
            Vector2 hookBend = axeNeck + perpendicular * 11f - direction * 7f;
            Vector2 hookTip = axeNeck + perpendicular * 5f - direction * 14f;
            DrawLine(anchor, hookRoot, hookBend, 6f, Outline, flip);
            DrawLine(anchor, hookBend, hookTip, 5f, Outline, flip);
            DrawLine(anchor, hookRoot, hookBend, 3f, blade, flip);
            DrawLine(anchor, hookBend, hookTip, 2f, SteelLight, flip);
            return;
        }

        if (player.WeaponFamily == WeaponFamily.SpikedMace)
        {
            DrawSpikedMace(player, anchor, flip, hand, bladeEnd, direction, perpendicular);
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

    private void DrawSpikedMace(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Vector2 hand,
        Vector2 head,
        Vector2 direction,
        Vector2 perpendicular)
    {
        DrawLine(anchor, hand - direction * 8f, head, 8f, Outline, flip);
        DrawLine(anchor, hand - direction * 7f, head - direction * 2f, 4f,
            Tint(new Color(44, 38, 36), player), flip);
        DrawLine(anchor,
            hand - direction * 2f - perpendicular * 3f,
            head - direction * 8f - perpendicular * 3f,
            1f,
            new Color(78, 70, 65),
            flip);
        DrawBox(anchor, head.X - 10f, head.Y - 8f, 21f, 17f, Outline, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 7f, 17f, 14f,
            Tint(new Color(66, 69, 70), player), flip);
        DrawBox(anchor, head.X - 5f, head.Y - 5f, 5f, 3f,
            new Color(88, 87, 83), flip);
        DrawBox(anchor, head.X + 2f, head.Y + 2f, 4f, 3f,
            new Color(48, 51, 53), flip);

        DrawLine(anchor, head - direction * 17f, head + direction * 17f, 5f, Outline, flip);
        DrawLine(anchor, head - perpendicular * 17f, head + perpendicular * 17f, 5f, Outline, flip);
        Vector2 diagonal = Vector2.Normalize(direction + perpendicular);
        Vector2 otherDiagonal = Vector2.Normalize(direction - perpendicular);
        DrawLine(anchor, head - diagonal * 14f, head + diagonal * 14f, 4f, Outline, flip);
        DrawLine(anchor, head - otherDiagonal * 14f, head + otherDiagonal * 14f, 4f, Outline, flip);
        Vector2 irregularOne = Vector2.Normalize(direction * .35f + perpendicular);
        Vector2 irregularTwo = Vector2.Normalize(direction - perpendicular * .28f);
        DrawLine(anchor, head + irregularOne * 6f,
            head + irregularOne * 18f, 3f, Outline, flip);
        DrawLine(anchor, head - irregularTwo * 5f,
            head - irregularTwo * 16f, 3f, Outline, flip);
    }

    private void DrawChainFlail(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Vector2 hand,
        Vector2 handleEnd)
    {
        DrawLine(anchor, hand, handleEnd, 7f, Outline, flip);
        DrawLine(anchor, hand, handleEnd, 4f, Tint(LeatherLight, player), flip);

        AttackDefinition attack = player.Combat.CurrentAttack;
        WeaponTechniqueEffect effect =
            player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        Vector2 head;
        if (_chainHeadWorldPosition.HasValue)
        {
            Vector2 worldDelta = _chainHeadWorldPosition.Value - anchor;
            head = new Vector2(flip ? -worldDelta.X : worldDelta.X, worldDelta.Y);
        }
        else
        {
            Vector2 offset = ChainFlailMotion.GetHeadOffset(
                effect,
                player.Combat.TechniqueStageIndex,
                attack,
                player.Combat.StateElapsed,
                player.Combat.CalculateRange(attack),
                effect == WeaponTechniqueEffect.ReckonerChainsOfJudgment
                    ? player.Combat.ReckonerMomentumSnapshotRatio
                    : player.Combat.Resources.ChainMomentumRatio,
                player.Combat.ReckonerOrbitHoldSeconds,
                player.Combat.ReckonerCurveSide);
            Vector2 playerCenterDelta = player.Position - anchor;
            head = new Vector2(
                offset.X + (flip ? -playerCenterDelta.X : playerCenterDelta.X),
                offset.Y + playerCenterDelta.Y);
        }
        Vector2 chainStart = hand + new Vector2(6f, 0f);
        Vector2 previous = chainStart;
        Vector2 chainVector = head - chainStart;
        float chainDistance = Vector2.Distance(chainStart, head);
        float tautness = Math.Clamp(chainDistance / 205f, .18f, 1f);
        float sag = MathHelper.Lerp(11f, 2f, tautness);

        for (int index = 1; index <= ReckonerTuning.ChainSegmentCount; index++)
        {
            float t = index / (float)ReckonerTuning.ChainSegmentCount;
            Vector2 point = Vector2.Lerp(chainStart, head, t);
            point.Y += MathF.Sin(t * MathF.PI) * sag;
            point.X += MathF.Sin(t * MathF.PI * 2f + index * .37f) *
                (1f - tautness) * 2f;
            DrawLine(anchor, previous, point, 4f, Outline, flip);
            Color link = index % 4 == 0
                ? new Color(91, 79, 69)
                : index % 3 == 0
                    ? new Color(62, 67, 69)
                    : SteelDark;
            DrawLine(anchor, previous, point, 2f, Tint(link, player), flip);
            previous = point;
        }

        if (player.Combat.IsAttackActive && chainDistance > 70f)
        {
            Vector2 trailDirection = new(-chainVector.Y, chainVector.X);
            if (player.Combat.TechniqueStageIndex % 2 != 0)
                trailDirection = -trailDirection;
            if (trailDirection.LengthSquared() > .01f)
            {
                trailDirection.Normalize();
                Vector2 normal = new(-trailDirection.Y, trailDirection.X);
                DrawLine(
                    anchor,
                    head - trailDirection * 17f - normal * 3f,
                    head - trailDirection * 4f - normal * 1f,
                    2f,
                    new Color(138, 143, 142, 100),
                    flip);
            }
        }

        DrawBox(anchor, head.X - 9f, head.Y - 9f, 19f, 19f, Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 7f, 15f, 15f,
            Tint(new Color(50, 54, 55), player), flip);
        DrawBox(anchor, head.X - 5f, head.Y - 5f, 7f, 5f,
            Tint(new Color(77, 78, 75), player), flip);
        for (int spike = 0; spike < 8; spike++)
        {
            float angle = spike * MathHelper.PiOver4;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            DrawLine(
                anchor,
                head + direction * 7f,
                head + direction * (spike % 2 == 0 ? 16f : 14f),
                spike % 2 == 0 ? 4f : 3f,
                Outline,
                flip);
        }

        float pulse = attack?.IsHeavy == true && player.Combat.IsAttackActive
            ? .78f
            : .48f;
        DrawBox(
            anchor,
            head.X - 2f,
            head.Y - 2f,
            5f,
            5f,
            new Color(116, 31, 34) * pulse,
            flip);
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

        if (player.WeaponFamily == WeaponFamily.WarAxe)
        {
            radius += 6f;
            duration = MathF.Max(duration, .16f);
            trailSpan = MathF.Max(trailSpan, .58f);
            segments += 2;
            weight *= 1.22f;
        }

        if (player.WeaponFamily == WeaponFamily.DualSwords)
        {
            if (attack.HitboxShape == AttackHitboxShape.Circular)
            {
                bool reverse = player.Combat.TechniqueStageIndex % 2 != 0;
                startAngle = reverse ? 1.15f : -2.15f;
                endAngle = reverse ? -2.10f : 1.20f;
                radius = 43f;
                duration = .13f;
                trailSpan = .68f;
                segments = 12;
                weight = .72f;
            }
            else if (attack.HitboxShape == AttackHitboxShape.FrontalFan)
            {
                radius = 46f;
                duration = .105f;
                trailSpan = .50f;
                segments = 9;
                weight = .64f;
            }
            else if (attack.HitboxShape == AttackHitboxShape.Cross)
            {
                radius = 44f;
                duration = .14f;
                trailSpan = .62f;
                segments = 10;
                weight = .78f;
            }

            if (player.Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz)
            {
                float intensity = DuelistTuning.GetRatio(
                    player.Combat.UltimateMomentumSnapshot);
                radius += 4f + intensity * 7f;
                segments += 2;
                weight *= 1f + intensity * .38f;
            }
        }

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

    private void DrawRaiderTechniqueEffects(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.WarAxe ||
            player.Combat.CurrentTechnique == null ||
            player.Combat.CurrentAttack == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        int stage = player.Combat.TechniqueStageIndex;
        AttackDefinition attack = player.Combat.CurrentAttack;
        float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
        if (activeElapsed < -.08f ||
            activeElapsed > attack.ActiveTime + .18f)
            return;

        float pulse = 1f - Math.Clamp(
            MathF.Max(0f, activeElapsed) /
                MathF.Max(.01f, attack.ActiveTime + .18f),
            0f,
            1f);
        Color dust = ScaleAlpha(new Color(126, 104, 78), .22f + pulse * .48f);
        Color spark = ScaleAlpha(new Color(225, 184, 104), .25f + pulse * .65f);

        bool bucklerImpact = effect is WeaponTechniqueEffect.RaiderBucklerRam or
            WeaponTechniqueEffect.RaiderRavagersRush ||
            effect == WeaponTechniqueEffect.RaiderSkullbreaker && stage == 0 ||
            effect == WeaponTechniqueEffect.RaiderCrowdCrusher && stage == 2 ||
            effect == WeaponTechniqueEffect.RaiderWarbringersDominion &&
                stage is 0 or 2;
        if (bucklerImpact)
        {
            Vector2 impact = new(pose.ShieldX + 25f, pose.ShieldY + 17f);
            DrawBox(anchor, impact.X - 2f, impact.Y - 2f, 5f, 5f, spark, flip);
            DrawLine(anchor, impact, impact + new Vector2(12f, -6f), 2f, spark, flip);
            DrawLine(anchor, impact, impact + new Vector2(14f, 5f), 2f, spark, flip);
        }

        bool heavyGroundImpact =
            effect == WeaponTechniqueEffect.RaiderWarbringersDominion && stage == 3 ||
            effect == WeaponTechniqueEffect.RaiderSavageCleave && stage == 2 ||
            effect == WeaponTechniqueEffect.RaiderSkullbreaker && stage == 1;
        if (!heavyGroundImpact)
            return;

        float reach = effect == WeaponTechniqueEffect.RaiderWarbringersDominion
            ? 104f
            : 58f;
        for (int index = 0; index < 5; index++)
        {
            float start = 18f + index * 9f;
            float end = MathF.Min(reach, start + 20f + index * 5f);
            float y = -2f - index % 2 * 3f;
            DrawLine(
                anchor,
                new Vector2(start, y),
                new Vector2(end, y - index),
                index == 4 ? 3f : 2f,
                dust,
                flip);
        }
    }

    private void DrawSpellbladeTechniqueEffects(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.ArcaneWarStaff ||
            player.Combat.CurrentTechnique == null ||
            player.Combat.CurrentAttack == null)
            return;

        AttackDefinition attack = player.Combat.CurrentAttack;
        float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
        if (activeElapsed < -.08f || activeElapsed > attack.ActiveTime + .20f)
            return;

        float fade = 1f - Math.Clamp(
            MathF.Max(0f, activeElapsed) /
                MathF.Max(.01f, attack.ActiveTime + .20f),
            0f,
            1f);
        Color violet = ScaleAlpha(
            new Color(161, 132, 205),
            .20f + fade * .62f);
        Color arcaneWhite = ScaleAlpha(
            new Color(222, 218, 233),
            .16f + fade * .55f);
        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        Vector2 direction = new(
            MathF.Cos(pose.SwordAngle),
            MathF.Sin(pose.SwordAngle));
        Vector2 staffTip = hand + direction * (pose.SwordLength + 9f);
        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;

        if (effect is WeaponTechniqueEffect.SpellbladeArcaneLunge or
            WeaponTechniqueEffect.SpellbladeRunicSpear)
        {
            DrawLine(anchor, hand + new Vector2(12f, 0f),
                hand + new Vector2(82f, 0f), 3f, violet, flip);
            DrawLine(anchor, hand + new Vector2(28f, 0f),
                hand + new Vector2(88f, 0f), 1f, arcaneWhite, flip);
            return;
        }

        if (effect is WeaponTechniqueEffect.SpellbladeArcaneConvergence or
            WeaponTechniqueEffect.SpellbladeArcaneDominion)
        {
            Vector2 ground = new(0f, -2f);
            DrawLine(anchor, ground - new Vector2(19f, 0f),
                ground + new Vector2(19f, 0f), 2f, violet, flip);
            DrawLine(anchor, ground - new Vector2(0f, 8f),
                ground + new Vector2(0f, 3f), 2f, arcaneWhite, flip);
            DrawLine(anchor, ground + new Vector2(-12f, 0f),
                ground + new Vector2(0f, -8f), 1f, violet, flip);
            DrawLine(anchor, ground + new Vector2(0f, -8f),
                ground + new Vector2(12f, 0f), 1f, violet, flip);
            return;
        }

        DrawLine(anchor,
            staffTip - direction * 17f,
            staffTip,
            2f,
            violet,
            flip);
        DrawLine(anchor,
            staffTip - new Vector2(4f, 0f),
            staffTip + new Vector2(5f, 0f),
            1f,
            arcaneWhite,
            flip);
    }

    private void DrawBreakerTechniqueEffects(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.SpikedMace ||
            player.Combat.CurrentTechnique == null)
            return;

        WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
        float inertia = MathF.Max(
            player.Combat.Resources.BreakerInertiaRatio,
            player.Combat.BreakerInertiaSnapshotRatio);
        Color dust = ScaleAlpha(
            new Color(132, 111, 84),
            .28f + inertia * .32f);
        Color spark = ScaleAlpha(
            new Color(211, 164, 91),
            .32f + inertia * .38f);
        Color stress = ScaleAlpha(
            new Color(133, 48, 40),
            .16f + inertia * .42f);

        if (player.Combat.IsPendulumCharging)
        {
            float radius = 24f +
                (int)player.Combat.BreakerPendulumStage * 7f;
            float angle = player.Combat.HeavyChargeProgress *
                MathHelper.TwoPi * 1.5f;
            Vector2 trailEnd = new(
                MathF.Cos(angle) * radius,
                -30f + MathF.Sin(angle) * radius * .45f);
            DrawLine(anchor,
                new Vector2(0f, -30f),
                trailEnd,
                2f,
                dust,
                flip);
            DrawBox(anchor, -18f, -3f, 7f, 3f, dust, flip);
            DrawBox(anchor, 12f, -4f, 6f, 3f, dust, flip);
            return;
        }

        AttackDefinition attack = player.Combat.CurrentAttack;
        if (attack == null)
            return;
        float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
        bool nearImpact = activeElapsed >= -.08f &&
            activeElapsed <= attack.ActiveTime + .18f;

        if (effect == WeaponTechniqueEffect.BreakerWorldbreaker &&
            player.Combat.StateElapsed < attack.StartupTime)
        {
            float windup = Math.Clamp(
                player.Combat.StateElapsed / attack.StartupTime,
                0f,
                1f);
            for (int index = 0; index < 4; index++)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                float x = side * (10f + index * 7f + windup * 8f);
                DrawBox(anchor, x, -3f - index % 2 * 3f, 5f, 3f,
                    dust, flip);
            }
            if (windup > .30f)
            {
                Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
                Vector2 direction = new(
                    MathF.Cos(pose.SwordAngle),
                    MathF.Sin(pose.SwordAngle));
                Vector2 head = hand + direction * pose.SwordLength;
                DrawLine(anchor, head - new Vector2(7f, 5f),
                    head + new Vector2(8f, 4f), 2f, stress, flip);
                DrawLine(anchor, head - new Vector2(5f, -6f),
                    head + new Vector2(6f, -7f), 1f, stress, flip);
            }
            return;
        }

        if (!nearImpact)
            return;

        bool groundImpact = effect is
            WeaponTechniqueEffect.BreakerEarthbreaker or
            WeaponTechniqueEffect.BreakerAnvilFall or
            WeaponTechniqueEffect.BreakerCataclysmWheel or
            WeaponTechniqueEffect.BreakerWorldbreaker;
        if (groundImpact)
        {
            float reach = effect == WeaponTechniqueEffect.BreakerWorldbreaker
                ? 72f
                : effect == WeaponTechniqueEffect.BreakerCataclysmWheel
                    ? 58f
                    : 44f;
            DrawLine(anchor, new Vector2(4f, -2f),
                new Vector2(reach, -3f), 4f, dust, flip);
            DrawLine(anchor, new Vector2(18f, -2f),
                new Vector2(31f, -14f), 2f, dust, flip);
            DrawLine(anchor, new Vector2(34f, -2f),
                new Vector2(48f, -10f), 2f, dust, flip);
            DrawBox(anchor, 21f, -18f, 4f, 4f, spark, flip);
            DrawBox(anchor, 43f, -14f, 5f, 5f, dust, flip);
        }
        else
        {
            Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
            Vector2 direction = new(
                MathF.Cos(pose.SwordAngle),
                MathF.Sin(pose.SwordAngle));
            Vector2 head = hand + direction * pose.SwordLength;
            DrawLine(anchor, head - direction * 18f, head, 3f, dust, flip);
            DrawLine(anchor, head - new Vector2(5f, 0f),
                head + new Vector2(6f, 0f), 2f, spark, flip);
        }
    }

    private void DrawGreatswordTechniqueEffects(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        if (player.WeaponFamily != WeaponFamily.GreatSword)
            return;

        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        Vector2 direction = new(
            MathF.Cos(pose.SwordAngle),
            MathF.Sin(pose.SwordAngle));
        Vector2 bladeEnd = hand + direction * pose.SwordLength;

        if (player.Combat.IsFinalVerdictReady)
        {
            float pulse = .35f +
                (MathF.Sin(player.Combat.StateElapsed * 8f) + 1f) * .16f;
            Color readiness = ScaleAlpha(new Color(214, 190, 126), pulse);
            DrawLine(
                anchor,
                bladeEnd - direction * 9f,
                bladeEnd,
                2f,
                readiness,
                flip);
        }

        if (player.Combat.GreatswordLandingPulse > 0f)
        {
            float fade = Math.Clamp(
                player.Combat.GreatswordLandingPulse / .32f,
                0f,
                1f);
            Color dust = ScaleAlpha(new Color(135, 118, 91), fade);
            DrawLine(anchor, new Vector2(-7f, -2f),
                new Vector2(25f, -3f), 3f, dust, flip);
            DrawBox(anchor, 8f, -9f, 5f, 4f, dust, flip);
            DrawBox(anchor, 21f, -6f, 4f, 3f, dust, flip);
        }

        WeaponTechniqueEffect effect =
            player.Combat.CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (effect is WeaponTechniqueEffect.GreatswordCrushingBreaker or
                WeaponTechniqueEffect.GreatswordTitanReckoning or
                WeaponTechniqueEffect.GreatswordKingsRupture or
                WeaponTechniqueEffect.GreatswordFinalVerdict &&
            player.Combat.IsAttackActive)
        {
            Color spark = new(211, 178, 103, 190);
            DrawLine(anchor, bladeEnd - new Vector2(8f, 0f),
                bladeEnd + new Vector2(7f, 0f), 2f, spark, flip);
        }

        if (player.Combat.IsGreatswordCharging)
        {
            float intensity = .25f +
                player.Combat.GreatswordChargeRatio * .65f;
            Color force = ScaleAlpha(new Color(220, 182, 91), intensity);
            DrawLine(anchor, hand, bladeEnd, 2f, force, flip);
            DrawBox(anchor, -12f, -5f, 24f, 3f, force, flip);
            if (player.Combat.GreatswordChargeLevel >= 2 ||
                player.Combat.GreatswordChargeRatio >= .48f)
            {
                DrawLine(anchor, new Vector2(-18f, -9f),
                    new Vector2(17f, -11f), 2f, force, flip);
            }
        }

        if (effect == WeaponTechniqueEffect.GreatswordKingsRupture &&
            player.Combat.TechniqueStageIndex == 2 &&
            player.Combat.IsAttackActive)
        {
            Color fracture = new(154, 126, 78, 175);
            DrawLine(anchor, new Vector2(8f, -2f),
                new Vector2(72f, -3f), 4f, fracture, flip);
            DrawLine(anchor, new Vector2(28f, -3f),
                new Vector2(38f, -13f), 2f, fracture, flip);
            DrawLine(anchor, new Vector2(51f, -3f),
                new Vector2(63f, -10f), 2f, fracture, flip);
        }

        if (effect == WeaponTechniqueEffect.GreatswordFinalVerdict &&
            player.Combat.TechniqueStageIndex == 3 &&
            player.Combat.IsAttackActive)
        {
            Color spectral = new(236, 205, 120, 150);
            Vector2 spectralStart = hand - new Vector2(7f, 10f);
            Vector2 spectralEnd = spectralStart + direction * 82f;
            DrawLine(anchor, spectralStart, spectralEnd, 12f, spectral, flip);
            DrawLine(anchor, spectralStart, spectralEnd, 4f,
                new Color(255, 239, 184, 205), flip);
        }

        if (effect == WeaponTechniqueEffect.GreatswordFinalVerdict &&
            player.Combat.TechniqueStageIndex == 4 &&
            player.Combat.IsAttackActive)
        {
            Color impact = new(196, 165, 91, 170);
            DrawLine(anchor, new Vector2(-34f, -2f),
                new Vector2(38f, -2f), 5f, impact, flip);
            DrawLine(anchor, new Vector2(-18f, -3f),
                new Vector2(-28f, -17f), 3f, impact, flip);
            DrawLine(anchor, new Vector2(17f, -3f),
                new Vector2(31f, -14f), 3f, impact, flip);
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
        if (family is WeaponFamily.GreatSword or WeaponFamily.SpikedMace)
        {
            startAngle = kind switch
            {
                AttackKind.LightTwo => .78f,
                AttackKind.LightThree => .72f,
                _ => -2.05f
            };
            endAngle = kind switch
            {
                AttackKind.LightTwo => -1.92f,
                AttackKind.LightThree => -1.55f,
                _ => 1.02f
            };
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

        if (player.WeaponFamily == WeaponFamily.BattleAxe)
        {
            const float bucklerSize = 20f;
            DrawBox(anchor, x + 4f, y + 7f, bucklerSize, bucklerSize, Outline, flip);
            DrawBox(anchor, x + 6f, y + 9f, bucklerSize - 4f, bucklerSize - 4f,
                fill, flip);
            DrawBox(anchor, x + 10f, y + 13f, 8f, 8f, rim, flip);
            return;
        }

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

    private void DrawPerfectGuardImpact(
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        float x = pose.ShieldX + ShieldWidth + 3f;
        float y = pose.ShieldY + 13f;
        Color core = new(250, 236, 184);
        Color spark = new(224, 174, 87);
        DrawBox(anchor, x - 2f, y - 2f, 8f, 8f, core, flip);
        DrawLine(anchor, new Vector2(x + 2f, y - 3f), new Vector2(x + 4f, y - 16f), 2f, spark, flip);
        DrawLine(anchor, new Vector2(x + 5f, y + 5f), new Vector2(x + 13f, y + 13f), 2f, spark, flip);
        DrawLine(anchor, new Vector2(x + 7f, y + 1f), new Vector2(x + 18f, y - 2f), 2f, core, flip);
        DrawLine(anchor, new Vector2(x, y + 5f), new Vector2(x - 5f, y + 14f), 2f, spark, flip);
    }

    private void DrawGreatswordGuardImpact(
        PlayerCharacter player,
        Vector2 anchor,
        bool flip,
        Pose pose)
    {
        Vector2 hand = new(pose.SwordHandX, pose.SwordHandY);
        Vector2 direction = new(
            MathF.Cos(pose.SwordAngle),
            MathF.Sin(pose.SwordAngle));
        Vector2 impact = hand + direction * 29f;
        Color core = player.Combat.IsPerfectGuardFeedbackActive
            ? new Color(245, 228, 177)
            : new Color(190, 196, 193);
        Color spark = new(218, 164, 78);
        DrawBox(anchor, impact.X - 3f, impact.Y - 3f, 7f, 7f,
            core, flip);
        DrawLine(anchor, impact, impact + new Vector2(12f, -10f),
            2f, spark, flip);
        DrawLine(anchor, impact, impact + new Vector2(14f, 3f),
            2f, core, flip);
        DrawLine(anchor, impact, impact + new Vector2(5f, 13f),
            2f, spark, flip);
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
        DrawBox(
            anchor,
            17f,
            -14f + settle,
            12f,
            10f,
            Tint(GetSkinColor(player.Appearance.SkinTone), player),
            flip);
        DrawBox(
            anchor,
            18f,
            -16f + settle,
            13f,
            5f,
            GetHairColor(player.Appearance.HairColor),
            flip);
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
        x = _partPivotX + (x - _partPivotX) * _partScaleX;
        width *= _partScaleX;
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

    private void DrawRectangleOutlineLocal(
        Vector2 anchor,
        float x,
        float y,
        float width,
        float height,
        float thickness,
        Color color,
        bool flip)
    {
        DrawBox(anchor, x, y, width, thickness, color, flip);
        DrawBox(anchor, x, y + height - thickness,
            width, thickness, color, flip);
        DrawBox(anchor, x, y, thickness, height, color, flip);
        DrawBox(anchor, x + width - thickness, y,
            thickness, height, color, flip);
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
            new Vector2(
                MathF.Round(length),
                MathF.Max(1f, MathF.Round(thickness * _lineThicknessScale))),
            SpriteEffects.None,
            0f);
    }

    private Vector2 ToWorld(Vector2 anchor, Vector2 local, bool flip)
    {
        local.X = _partPivotX + (local.X - _partPivotX) * _partScaleX;
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

    private void SetPartTransform(float scaleX, float pivotX)
    {
        _partScaleX = scaleX;
        _partPivotX = pivotX;
    }

    private void ResetPartTransform()
    {
        _partScaleX = 1f;
        _partPivotX = 0f;
        _lineThicknessScale = 1f;
    }

    private static float GetTorsoScale(CharacterAppearance appearance)
    {
        float build = appearance.BodyBuild switch
        {
            BodyBuild.Slim => -.05f,
            BodyBuild.Broad => .07f,
            _ => 0f
        };
        float proportion = appearance.TorsoWidth switch
        {
            VisualProportion.Slim => -.06f,
            VisualProportion.Broad => .07f,
            _ => 0f
        };
        float bodyType = appearance.BodyType == CharacterBodyType.Female
            ? -.025f
            : 0f;
        return 1f + build + proportion + bodyType;
    }

    private static float GetArmScale(CharacterAppearance appearance)
    {
        float build = appearance.BodyBuild switch
        {
            BodyBuild.Slim => -.08f,
            BodyBuild.Broad => .10f,
            _ => 0f
        };
        float proportion = appearance.ArmThickness switch
        {
            VisualProportion.Slim => -.12f,
            VisualProportion.Broad => .12f,
            _ => 0f
        };
        return 1f + build + proportion;
    }

    private static float GetLegScale(CharacterAppearance appearance)
    {
        float build = appearance.BodyBuild switch
        {
            BodyBuild.Slim => -.05f,
            BodyBuild.Broad => .07f,
            _ => 0f
        };
        float proportion = appearance.LegThickness switch
        {
            VisualProportion.Slim => -.08f,
            VisualProportion.Broad => .09f,
            _ => 0f
        };
        return 1f + build + proportion;
    }

    private static float GetHeightOffset(ApparentHeight height)
    {
        return height switch
        {
            ApparentHeight.Short => 3f,
            ApparentHeight.Tall => -4f,
            _ => 0f
        };
    }

    private static Color GetSkinColor(SkinTone tone)
    {
        return tone switch
        {
            SkinTone.Fair => new Color(230, 190, 161),
            SkinTone.Light => new Color(211, 166, 132),
            SkinTone.Warm => new Color(202, 146, 104),
            SkinTone.Brown => new Color(145, 94, 64),
            SkinTone.Deep => new Color(92, 58, 45),
            _ => new Color(190, 137, 99)
        };
    }

    private static Color GetHairColor(HairColor color)
    {
        return color switch
        {
            HairColor.Black => new Color(24, 23, 25),
            HairColor.Brown => new Color(91, 58, 39),
            HairColor.LightBrown => new Color(139, 98, 60),
            HairColor.Gray => new Color(111, 109, 105),
            HairColor.White => new Color(205, 201, 187),
            HairColor.DarkRed => new Color(91, 38, 32),
            _ => new Color(39, 29, 27)
        };
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
        public bool UseMartialLimbs;
        public float OffHandX;
        public float OffHandY;
        public float FrontElbowX;
        public float FrontElbowY;
        public float RearElbowX;
        public float RearElbowY;
        public float FrontKneeX;
        public float FrontKneeY;
        public float FrontFootX;
        public float FrontFootY;
        public float RearKneeX;
        public float RearKneeY;
        public float RearFootX;
        public float RearFootY;
    }
}
