using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

public sealed class PlayerSpriteRenderer
{
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

		public float OffSwordAngle;

		public float OffSwordLength;

		public float ShieldX;

		public float ShieldY;

		public bool UseMartialLimbs;

		public bool UseKnightHeldStance;

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

	public const int ConceptualFrameWidth = 100;

	public const int ConceptualFrameHeight = 100;

	public const int VisualBodyWidth = 46;

	public const int VisualHeight = 68;

	public const int SwordLength = 31;

	public const int ShieldWidth = 20;

	public const int ShieldHeight = 31;

	private static readonly Color Outline = new Color(24, 25, 29);

	private static readonly Color Cloth = new Color(47, 48, 53);

	private static readonly Color ClothLight = new Color(67, 68, 73);

	private static readonly Color Leather = new Color(65, 43, 32);

	private static readonly Color LeatherLight = new Color(91, 61, 43);

	private static readonly Color SteelDark = new Color(63, 69, 75);

	private static readonly Color Steel = new Color(100, 109, 117);

	private static readonly Color SteelLight = new Color(151, 159, 164);

	private static readonly Color CloakDark = new Color(73, 23, 29);

	private static readonly Color Cloak = new Color(119, 35, 42);

	private static readonly Color SunGold = new Color(191, 145, 61);

	private static readonly Color SlashEdge = new Color(112, 132, 143, 105);

	private static readonly Color SlashBody = new Color(171, 181, 183, 155);

	private static readonly Color SlashCore = new Color(222, 218, 199, 210);

	private static readonly Color HitSpark = new Color(226, 196, 119, 230);

	private static readonly float[] LightOneSwordAngles = new float[6] { -0.52f, -1.45f, -1.1f, -0.35f, 0.35f, 0.55f };

	private static readonly float[] LightOneLean = new float[6] { 0f, -2f, 0f, 3f, 4f, 2f };

	private static readonly float[] LightTwoSwordAngles = new float[6] { 0.55f, 0.72f, 0.35f, -0.35f, -1.05f, -1.25f };

	private static readonly float[] LightTwoLean = new float[6] { 1f, -1f, 0f, 2f, 3f, 1f };

	private static readonly float[] LightThreeSwordAngles = new float[9] { -1.25f, -1.65f, -1.92f, -1.48f, -0.78f, 0.02f, 0.62f, 0.88f, 1.02f };

	private static readonly float[] LightThreeLean = new float[9] { 0f, -2f, -3f, 0f, 3f, 6f, 7f, 5f, 2f };

	private static readonly float[] HeavySwordAngles = new float[6] { -0.52f, -1.55f, -2.28f, -1.05f, 0.28f, 0.82f };

	private static readonly float[] HeavyLean = new float[6] { 0f, -2f, -4f, 1f, 7f, 3f };

	private static readonly float[] DuelistStellarTrailStart = new float[8] { -1.18f, 0.28f, 0f, -1.25f, 0.82f, -1.4f, -1.48f, -1.62f };

	private static readonly float[] DuelistStellarTrailEnd = new float[8] { 0.58f, -0.48f, 0f, -0.48f, -1.22f, -0.62f, 0.48f, 0.72f };

	private static readonly float[] DuelistLunarTrailStart = new float[8] { -1.78f, 0.78f, 0f, 0.32f, -1.88f, -1.62f, 0.82f, 0.98f };

	private static readonly float[] DuelistLunarTrailEnd = new float[8] { -1.42f, -1.18f, 0f, -2.28f, -1.88f, 0.68f, -1.02f, -1.02f };

	private readonly SpriteBatch _spriteBatch;

	private readonly Texture2D _pixel;

	private readonly SpriteAnimationController _animation = new SpriteAnimationController();

	private int _observedHitEffectId;

	private float _hitEffectDuration;

	private float _hitEffectTimeRemaining;

	private Vector2 _hitEffectPosition;

	private Vector2? _chainHeadWorldPosition;

	private float _partScaleX = 1f;

	private float _partPivotX;

	private float _lineThicknessScale = 1f;

	private float _spatialPhaseStrength;

	private Color _spatialPhaseColor = new Color(70, 91, 166);

	private int _observedDuelistMomentumTier = -1;

	private float _duelistTierPulseRemaining;

	private float _duelistOverdriveVisualStrength;

	private float _visualTimeSeconds;

	private bool _perfectGuardWasActive;

	private float _perfectGuardPulseRemaining;

	public PlayerSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
	{
		_spriteBatch = spriteBatch;
		_pixel = pixel;
	}

	public void Draw(DungeonAscendant.Player.Player player, GameTime gameTime, Vector2? chainHeadWorldPosition = null, Vector2? visualPosition = null, Vector2? portalTravelDirection = null, float portalTransitionStrength = 0f)
	{
		ResetPartTransform();
		float visualDelta = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);
		_visualTimeSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
		bool perfectGuardActive = player.Combat.IsPerfectGuardFeedbackActive && player.UsesShield;
		if (perfectGuardActive && !_perfectGuardWasActive)
		{
			_perfectGuardPulseRemaining = 0.24f;
		}
		else
		{
			_perfectGuardPulseRemaining = MathF.Max(0f, _perfectGuardPulseRemaining - visualDelta);
		}
		_perfectGuardWasActive = perfectGuardActive;
		UpdateDuelistVisualState(player, visualDelta);
		_spatialPhaseStrength = (player.Combat.IsFourthVeilVisualActive ? (0.42f * player.Combat.FourthVeilVisualStrength) : 0f);
		_spatialPhaseColor = new Color(61, 77, 145);
		_chainHeadWorldPosition = chainHeadWorldPosition;
		_animation.Update(gameTime, player);
		Vector2 obj = visualPosition ?? player.Position;
		float x = MathF.Round(obj.X);
		Vector2 obj2 = visualPosition ?? player.Position;
		Vector2 anchor = new Vector2(x, MathF.Round(obj2.Y + player.Size.Y / 2f));
		bool flip = player.Facing == FacingDirection.Left;
		if (player.IsGrounded || _animation.State == PlayerVisualState.Dead)
		{
			DrawBox(anchor, -22f, -3f, 44f, 7f, new Color(12, 14, 17, 145), flip);
		}
		if (_animation.State == PlayerVisualState.Dead && _animation.Frame >= 2)
		{
			DrawCollapsed(player, anchor, flip);
		}
		else
		{
			Pose pose = CreatePose(player);
			if (player.WeaponFamily == WeaponFamily.AbyssRing && portalTravelDirection.HasValue && portalTransitionStrength > 0f)
			{
				Vector2 value = portalTravelDirection.Value;
				WeaponTechnique currentTechnique = player.Combat.CurrentTechnique;
				ApplyPortalTransitPose(ref pose, value, portalTransitionStrength, flip, currentTechnique != null && currentTechnique.Effect == WeaponTechniqueEffect.SovereignGoldenCataclysm, player.Combat.TechniqueStageIndex);
			}
			DrawDuelistAfterimages(player, anchor, flip, pose);
			DrawRangerAfterimages(player, anchor, flip, pose);
			DrawSovereignAfterimages(player, anchor, flip, pose);
			DrawScytheMovementAfterimage(player, anchor, flip, pose);
			DrawStanding(player, anchor, flip, pose);
		}
		if (player.IsSlowed)
		{
			DrawWebStrands(anchor, flip);
		}
		_spatialPhaseStrength = 0f;
	}

	private void UpdateDuelistVisualState(DungeonAscendant.Player.Player player, float elapsedSeconds)
	{
		if (player.WeaponFamily != WeaponFamily.DualSwords)
		{
			_observedDuelistMomentumTier = -1;
			_duelistTierPulseRemaining = 0f;
			_duelistOverdriveVisualStrength = 0f;
			return;
		}
		int tier = player.Combat.Resources.DuelistMomentumTier;
		if (_observedDuelistMomentumTier >= 0 && tier > _observedDuelistMomentumTier)
		{
			_duelistTierPulseRemaining = 0.3f;
		}
		_observedDuelistMomentumTier = tier;
		_duelistTierPulseRemaining = MathF.Max(0f, _duelistTierPulseRemaining - elapsedSeconds);
		float target = (player.Combat.IsDuelistOverdriveActive ? 1f : 0f);
		float response = ((target > _duelistOverdriveVisualStrength) ? 10f : 4f);
		_duelistOverdriveVisualStrength = MathHelper.Lerp(_duelistOverdriveVisualStrength, target, Math.Clamp(elapsedSeconds * response, 0f, 1f));
		if (target <= 0f && _duelistOverdriveVisualStrength < 0.01f)
		{
			_duelistOverdriveVisualStrength = 0f;
		}
	}

	public void DrawFourthVeilEcho(DungeonAscendant.Player.Player player, Vector2 visualPosition, float strength, bool purpleLayer)
	{
		if (!(strength <= 0f))
		{
			ResetPartTransform();
			_chainHeadWorldPosition = null;
			_spatialPhaseStrength = Math.Clamp(strength, 0f, 1f);
			_spatialPhaseColor = (purpleLayer ? new Color(104, 62, 174) : new Color(54, 112, 194));
			Vector2 anchor = new Vector2(MathF.Round(visualPosition.X), MathF.Round(visualPosition.Y + player.Size.Y / 2f));
			DrawStanding(player, anchor, player.Facing == FacingDirection.Left, CreatePose(player));
			_spatialPhaseStrength = 0f;
		}
	}

	public void DrawDuelistUltimateEcho(DungeonAscendant.Player.Player player, Vector2 visualPosition, float strength)
	{
		if (player != null && player.WeaponFamily == WeaponFamily.DualSwords && !(strength <= 0f))
		{
			ResetPartTransform();
			_chainHeadWorldPosition = null;
			Vector2 anchor = new Vector2(MathF.Round(visualPosition.X), MathF.Round(visualPosition.Y + player.Size.Y / 2f));
			Pose pose = CreatePose(player);
			DrawDuelistAfterimageSilhouette(anchor, player.Facing == FacingDirection.Left, pose, Math.Clamp(strength, 0f, 0.62f));
		}
	}

	private static void ApplyPortalTransitPose(ref Pose pose, Vector2 direction, float strength, bool flip, bool ultimatePortalStep, int stepIndex)
	{
		strength = Math.Clamp(strength, 0f, 1f);
		float localX = (flip ? (0f - direction.X) : direction.X) * strength;
		if (ultimatePortalStep)
		{
			float preparation = 1f - MathHelper.SmoothStep(0f, 1f, Math.Clamp(strength / 0.28f, 0f, 1f));
			float flight = MathHelper.SmoothStep(0f, 1f, Math.Clamp((strength - 0.22f) / 0.48f, 0f, 1f));
			float plant = 1f - Math.Clamp(MathF.Abs(strength - 0.8f) / 0.13f, 0f, 1f);
			float propulsion = MathHelper.SmoothStep(0f, 1f, Math.Clamp((strength - 0.86f) / 0.14f, 0f, 1f));
			float localDirection = ((!(MathF.Abs(localX) < 0.08f)) ? ((float)MathF.Sign(localX)) : ((stepIndex % 2 == 0) ? 1f : (-1f)));
			float redirect = ((stepIndex % 3 == 1) ? (-1f) : 1f);
			pose.Crouch += preparation * 5f + plant * 7f - propulsion * 4f;
			pose.Lean += localX * (7f + flight * 8f) + redirect * plant * 3f;
			pose.CloakTail -= flight * 6f + propulsion * 5f;
			float tuck = MathF.Sin(Math.Clamp((strength - 0.18f) / 0.58f, 0f, 1f) * (float)Math.PI);
			pose.FrontKneeX = MathHelper.Lerp(pose.FrontKneeX, 13f + localDirection * 11f, tuck);
			pose.FrontKneeY = MathHelper.Lerp(pose.FrontKneeY, -22f - direction.Y * 7f, tuck);
			pose.RearKneeX = MathHelper.Lerp(pose.RearKneeX, -10f - localDirection * 8f, tuck);
			pose.RearKneeY = MathHelper.Lerp(pose.RearKneeY, -18f + direction.Y * 5f, tuck);
			if (stepIndex % 2 == 0)
			{
				pose.FrontKneeX = MathHelper.Lerp(pose.FrontKneeX, 18f * localDirection, plant);
				pose.FrontKneeY = MathHelper.Lerp(pose.FrontKneeY, -13f, plant);
				pose.FrontFootX = MathHelper.Lerp(pose.FrontFootX, 29f * localDirection, plant);
				pose.FrontFootY = MathHelper.Lerp(pose.FrontFootY, -4f, plant);
				pose.RearFootX -= localDirection * 10f * propulsion;
				pose.RearFootY -= 10f * propulsion;
			}
			else
			{
				pose.RearKneeX = MathHelper.Lerp(pose.RearKneeX, 15f * localDirection, plant);
				pose.RearKneeY = MathHelper.Lerp(pose.RearKneeY, -14f, plant);
				pose.RearFootX = MathHelper.Lerp(pose.RearFootX, 27f * localDirection, plant);
				pose.RearFootY = MathHelper.Lerp(pose.RearFootY, -5f, plant);
				pose.FrontFootX -= localDirection * 11f * propulsion;
				pose.FrontFootY -= 9f * propulsion;
			}
			pose.SwordHandX += localDirection * (plant * -7f + propulsion * 16f);
			pose.SwordHandY -= propulsion * (6f - direction.Y * 5f);
			pose.OffHandX -= localDirection * (plant * 8f + propulsion * 5f);
		}
		else
		{
			pose.Crouch += 4f * strength;
			pose.Lean += 8f * localX;
			pose.Step += 9f * localX;
			pose.SwordHandX += 11f * localX;
			pose.SwordHandY += direction.Y * 8f * strength;
			pose.LeftFootLift = Math.Max(pose.LeftFootLift, Math.Max(0f, 0f - localX) * 11f + Math.Max(0f, 0f - direction.Y) * 7f * strength);
			pose.RightFootLift = Math.Max(pose.RightFootLift, Math.Max(0f, localX) * 11f + Math.Max(0f, direction.Y) * 7f * strength);
		}
	}

	public void DrawPreview(DungeonAscendant.Player.Player player, GameTime gameTime, Vector2 anchor, FacingDirection facing)
	{
		ResetPartTransform();
		_chainHeadWorldPosition = null;
		bool flip = facing == FacingDirection.Left;
		int frame = (int)(gameTime.TotalGameTime.TotalSeconds * 5.0) % 6;
		Pose pose = default(Pose);
		bool flag = (uint)(frame - 2) <= 1u;
		pose.Bob = (flag ? (-1f) : 0f);
		pose.Crouch = 1f;
		pose.Lean = 1f;
		pose.SwordHandX = 18f;
		bool flag2 = (uint)(frame - 2) <= 1u;
		pose.SwordHandY = (flag2 ? (-37f) : (-36f));
		bool flag3 = (uint)(frame - 2) <= 1u;
		pose.SwordAngle = (flag3 ? (-0.56f) : (-0.52f));
		pose.SwordLength = 31f;
		pose.ShieldX = -4f;
		bool flag4 = (uint)(frame - 2) <= 1u;
		pose.ShieldY = (flag4 ? (-46f) : (-45f));
		bool flag5 = (uint)(frame - 3) <= 1u;
		pose.CloakTail = (flag5 ? (-2f) : (-1f));
		Pose pose2 = pose;
		if (player.IsKnightSwordAndShield)
		{
			ConfigureKnightHeldStance(ref pose2, flag ? 1f : 0f);
			FinalizeKnightBodyPose(player, ref pose2);
		}
		DrawBox(anchor, -22f, -3f, 44f, 7f, new Color(12, 14, 17, 145), flip);
		DrawStanding(player, anchor, flip, pose2);
	}

	public void DrawCinematicCutIn(DungeonAscendant.Player.Player player, Vector2 anchor)
	{
		ResetPartTransform();
		_chainHeadWorldPosition = null;
		bool flip = player.Facing == FacingDirection.Left;
		Pose pose = CreatePose(player);
		DrawStanding(player, anchor, flip, pose);
	}

	public void DrawKnightCinematicCutIn(DungeonAscendant.Player.Player player,
		Vector2 anchor, float progress)
	{
		ResetPartTransform();
		_chainHeadWorldPosition = null;
		bool flip = player.Facing == FacingDirection.Left;
		progress = Math.Clamp(progress, 0f, 1f);
		Pose pose = new Pose
		{
			Crouch = 4f,
			Lean = 2f,
			Step = 7f,
			SwordLength = 33f,
			SwordHandX = -3f,
			SwordHandY = -31f,
			SwordAngle = 1.08f,
			ShieldX = 6f,
			ShieldY = -52f,
			CloakTail = -3f
		};
		DrawStanding(player, anchor, flip, pose);
		Color focus = ScaleAlpha(new Color(185, 218, 229),
			MathHelper.SmoothStep(0f, 1f, progress));
		DrawLine(anchor, new Vector2(pose.Lean + 2f, pose.Crouch - 60f),
			new Vector2(pose.Lean + 8f, pose.Crouch - 61f), 1f, focus, flip);
	}

	public void DrawSovereignCinematicCutIn(DungeonAscendant.Player.Player player, Vector2 anchor)
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
		DrawBox(color: new Color(255, 215, 91, 235), anchor: anchor, x: pose.Lean + 2f, y: pose.Crouch - 61f, width: 5f, height: 2f, flip: flip);
		DrawLine(anchor, new Vector2(pose.Lean - 7f, pose.Crouch - 60f), new Vector2(pose.Lean + 14f, pose.Crouch - 62f), 1f, new Color(255, 198, 57, 105), flip);
	}

	public void DrawScytheCinematicCutIn(DungeonAscendant.Player.Player player, Vector2 anchor, float progress)
	{
		ResetPartTransform();
		_chainHeadWorldPosition = null;
		bool flip = player.Facing == FacingDirection.Left;
		progress = Math.Clamp(progress, 0f, 1f);
		Pose pose = new Pose
		{
			Crouch = 3f,
			Lean = 5f,
			Step = -2f,
			SwordLength = 58f,
			SwordHandX = 12f,
			SwordHandY = -43f,
			SwordAngle = -0.72f,
			OffHandX = 1f,
			OffHandY = -35f,
			FrontElbowX = 23f,
			FrontElbowY = -45f,
			RearElbowX = -9f,
			RearElbowY = -38f,
			CloakTail = -9f
		};
		DrawStanding(player, anchor, flip, pose);
		float eyePulse = MathF.Sin(progress * (float)Math.PI);
		Color iris = new Color(42, 12, 54, (int)(235f * eyePulse));
		Color core = new Color(185, 116, 219, (int)(245f * eyePulse));
		DrawBox(anchor, pose.Lean + 1f, pose.Crouch - 61f, 6f, 3f, iris, flip);
		DrawBox(anchor, pose.Lean + 3f, pose.Crouch - 61f, 2f, 2f, core, flip);
		DrawLine(anchor, new Vector2(pose.Lean - 7f, pose.Crouch - 60f), new Vector2(pose.Lean + 18f, pose.Crouch - 63f), 1f, new Color(111, 51, 139, (int)(105f * eyePulse)), flip);
		float sweep = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.35f) / 0.45f, 0f, 1f));
		Vector2 bladeStart = new Vector2(-34f + sweep * 18f, -16f);
		Vector2 bladeEnd = new Vector2(46f + sweep * 12f, -68f);
		DrawLine(anchor, bladeStart, bladeEnd, 11f, new Color(18, 8, 23, (int)(225f * sweep)), flip);
		DrawLine(anchor, bladeStart + new Vector2(0f, -2f), bladeEnd + new Vector2(0f, -2f), 2f, new Color(151, 78, 187, (int)(210f * sweep)), flip);
	}

	private void DrawStanding(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		float bodyX = pose.Lean;
		float upperBodyY = pose.Bob + pose.Crouch + GetHeightOffset(player.Appearance.ApparentHeight);
		int num;
		if (pose.SwordAngle < -1.25f)
		{
			WeaponFamily weaponFamily = player.WeaponFamily;
			num = ((weaponFamily != WeaponFamily.HunterBow && weaponFamily != WeaponFamily.ArcaneWarStaff && weaponFamily != WeaponFamily.AbyssRing) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool weaponBehindBody = (byte)num != 0 || pose.UseKnightHeldStance;
		if (player.WeaponFamily == WeaponFamily.HunterBow)
		{
			DrawQuiver(anchor, flip, bodyX, upperBodyY);
		}
		float torsoScale = GetTorsoScale(player.Appearance);
		SetPartTransform(torsoScale, bodyX);
		DrawCloak(player, anchor, flip, pose, bodyX, upperBodyY);
		ResetPartTransform();
		if (weaponBehindBody)
		{
			DrawWeapon(player, anchor, flip, pose);
		}
		SetPartTransform(GetLegScale(player.Appearance), bodyX);
		DrawLegs(player, anchor, flip, pose, bodyX, 0f);
		ResetPartTransform();
		_lineThicknessScale = GetArmScale(player.Appearance);
		DrawRearArm(player, anchor, flip, pose, bodyX, upperBodyY);
		ResetPartTransform();
		SetPartTransform(torsoScale, bodyX);
		DrawTorso(player, anchor, flip, pose, bodyX, upperBodyY);
		ResetPartTransform();
		DrawHead(player, anchor, flip, pose, bodyX, upperBodyY);
		_lineThicknessScale = GetArmScale(player.Appearance);
		DrawFrontArm(player, anchor, flip, pose, bodyX, upperBodyY);
		ResetPartTransform();
		if (player.UsesShield)
		{
			DrawShield(player, anchor, flip, pose);
		}
		if (!weaponBehindBody && player.WeaponFamily != WeaponFamily.AbyssRing)
		{
			DrawWeapon(player, anchor, flip, pose);
		}
		DrawSlashTrail(player, anchor, flip, pose);
		DrawDuelistTechniqueEffects(player, anchor, flip, pose);
		DrawSpellbladeTechniqueEffects(player, anchor, flip, pose);
		DrawKnightTechniqueEffects(player, anchor, flip, pose);
		DrawGreatswordTechniqueEffects(player, anchor, flip, pose);
		DrawScytheTechniqueEffects(player, anchor, flip, pose);
		DrawSovereignTransformation(player, anchor, flip, pose);
		DrawSovereignMartialEffects(player, anchor, flip, pose);
		if (player.Combat.IsChargingHeavy)
		{
			WeaponTechnique currentTechnique = player.Combat.CurrentTechnique;
			if (currentTechnique == null || currentTechnique.Effect != WeaponTechniqueEffect.ScytheUmbraDevourer)
			{
				WeaponTechnique currentTechnique2 = player.Combat.CurrentTechnique;
				if ((currentTechnique2 == null || currentTechnique2.Effect != WeaponTechniqueEffect.DuelistHeavenSplitter) &&
					(currentTechnique2 == null || currentTechnique2.Effect != WeaponTechniqueEffect.KnightFinalOath))
				{
					DrawChargeEffect(player, anchor, flip, pose);
				}
			}
		}
		if (player.Combat.IsBlockFeedbackActive && player.UsesShield && !player.Combat.IsPerfectGuardFeedbackActive)
		{
			DrawBlockImpact(anchor, flip, pose);
		}
		if (player.Combat.IsPerfectGuardFeedbackActive && player.UsesShield)
		{
			DrawPerfectGuardImpact(anchor, flip, pose);
		}
		if (player.WeaponFamily == WeaponFamily.GreatSword && (player.Combat.IsBlockFeedbackActive || player.Combat.IsPerfectGuardFeedbackActive))
		{
			DrawGreatswordGuardImpact(player, anchor, flip, pose);
		}
		if (player.WeaponFamily == WeaponFamily.DualSwords && (player.Combat.IsBlockFeedbackActive || player.Combat.IsPerfectPhantomFeedbackActive))
		{
			DrawDuelistGuardImpact(player, anchor, flip, pose);
		}
	}

	private Pose CreatePose(DungeonAscendant.Player.Player player)
	{
		Pose pose = new Pose
		{
			SwordHandX = 18f,
			SwordHandY = -35f,
			SwordAngle = -0.52f,
			SwordLength = 31f,
			ShieldX = -4f,
			ShieldY = -45f,
			CloakTail = -1f
		};
		switch (_animation.State)
		{
		case PlayerVisualState.Idle:
		{
			int frame = _animation.Frame;
			bool flag = (uint)(frame - 2) <= 1u;
			pose.Bob = (flag ? (-1f) : 0f);
			pose.Crouch = 1f;
			pose.Lean = 1f;
			pose.SwordHandX = 18f;
			pose.SwordHandY = -36f;
			frame = _animation.Frame;
			flag = (uint)(frame - 2) <= 1u;
			pose.SwordAngle = -0.52f + (flag ? (-0.04f) : 0f);
			ref float shieldY = ref pose.ShieldY;
			float num = shieldY;
			frame = _animation.Frame;
			flag = (uint)(frame - 2) <= 1u;
			shieldY = num + (flag ? (-1f) : 0f);
			frame = _animation.Frame;
			flag = (uint)(frame - 3) <= 1u;
			pose.CloakTail = (flag ? (-2f) : (-1f));
			break;
		}
		case PlayerVisualState.Run:
		{
			pose.Lean = 2f;
			ConfigureRunStride(ref pose, _animation.Frame);
			pose.CloakTail = -7f - MathF.Abs(pose.Step) * 0.35f;
			int frame = _animation.Frame;
			bool flag = (uint)(frame - 1) <= 2u;
			pose.SwordHandX = 18f + (flag ? 1f : 0f);
			pose.SwordAngle = -0.42f + (float)(_animation.Frame % 2) * 0.07f;
			pose.ShieldX = -5f;
			frame = _animation.Frame;
			flag = ((frame == 2 || frame == 6) ? true : false);
			pose.ShieldY = -44f + (flag ? (-1f) : 0f);
			break;
		}
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
			ConfigureAttackPose(ref pose, LightOneSwordAngles, LightOneLean, 0f);
			break;
		case PlayerVisualState.LightAttack2:
			ConfigureAttackPose(ref pose, LightTwoSwordAngles, LightTwoLean, -2f);
			break;
		case PlayerVisualState.LightAttack3:
		{
			ConfigureAttackPose(ref pose, LightThreeSwordAngles, LightThreeLean, -5f);
			pose.SwordLength = 33f;
			int frame = _animation.Frame;
			pose.Crouch = ((frame >= 4 && frame <= 7) ? 3f : 1f);
			break;
		}
		case PlayerVisualState.LightAttack4:
			ConfigureAttackPose(ref pose, LightTwoSwordAngles, LightTwoLean, -4f);
			pose.Crouch = 3f;
			break;
		case PlayerVisualState.LightAttack5:
			ConfigureAttackPose(ref pose, LightThreeSwordAngles, LightThreeLean, -6f);
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
		{
			ConfigureAttackPose(ref pose, HeavySwordAngles, HeavyLean, -7f);
			pose.SwordLength = 34f;
			int frame = _animation.Frame;
			bool flag = (uint)(frame - 3) <= 1u;
			pose.Crouch = (flag ? 4f : 2f);
			pose.ShieldX = -21f;
			break;
		}
		case PlayerVisualState.Dodge:
			pose.Crouch = 12f;
			pose.Lean = 7f;
			pose.Step = 7f;
			pose.SwordHandX = 4f;
			pose.SwordHandY = -27f;
			pose.SwordAngle = 0.02f;
			pose.ShieldX = -5f;
			pose.ShieldY = -32f;
			pose.CloakTail = -12f - (float)_animation.Frame * 1.5f;
			break;
		case PlayerVisualState.Block:
			pose.Crouch = 4f;
			pose.Lean = -1f;
			pose.SwordHandX = -5f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = -2.02f;
			pose.ShieldX = (player.Combat.IsBlockFeedbackActive ? 6f : 8f);
			pose.ShieldY = -51f + (float)_animation.Frame;
			pose.CloakTail = -3f;
			break;
		case PlayerVisualState.GuardBreak:
			pose.Crouch = 5f;
			pose.Lean = -7f + (float)_animation.Frame;
			pose.SwordAngle = -1.75f;
			pose.ShieldX = -25f;
			pose.ShieldY = -53f + (float)_animation.Frame * 3f;
			pose.CloakTail = 4f;
			break;
		case PlayerVisualState.Hurt:
			pose.Crouch = 4f;
			pose.Lean = -6f + (float)_animation.Frame * 2f;
			pose.SwordAngle = -1.25f;
			pose.ShieldX = -20f;
			pose.CloakTail = 3f;
			break;
		case PlayerVisualState.Dead:
			pose.Crouch = 9f + (float)_animation.Frame * 3f;
			pose.Lean = -4f;
			pose.SwordAngle = 0.35f;
			pose.ShieldX = -21f;
			pose.ShieldY = -35f;
			pose.CloakTail = 5f;
			break;
		}
		ApplyEquipmentPose(player, ref pose);
		ApplyKnightBasePose(player, ref pose);
		ApplyKnightTechniquePose(player, ref pose);
		FinalizeKnightBodyPose(player, ref pose);
		ApplyDuelistTechniquePose(player, ref pose);
		ApplyRangerTechniquePose(player, ref pose);
		ApplySpellbladeTechniquePose(player, ref pose);
		ApplyGreatswordTechniquePose(player, ref pose);
		ApplyScytheTechniquePose(player, ref pose);
		ApplySovereignTechniquePose(player, ref pose);
		return pose;
	}

	private void ApplyKnightBasePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (!player.IsKnightSwordAndShield)
		{
			return;
		}
		pose.SwordLength = 33f;
		if (_animation.State == PlayerVisualState.Idle)
		{
			float breath = _animation.Frame is 2 or 3 ? 1f : 0f;
			ConfigureKnightHeldStance(ref pose, breath);
		}
		else if (_animation.State == PlayerVisualState.Run)
		{
			pose.Crouch = 3f;
			pose.Lean = 1f;
			pose.SwordHandX = 5f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = -1.08f;
			pose.ShieldX = 4f;
			pose.ShieldY = -47f;
		}
		else if (_animation.State == PlayerVisualState.Jump)
		{
			pose.Crouch = 5f;
			pose.Lean = 0f;
			pose.Step = 5f;
			pose.SwordHandX = -2f;
			pose.SwordHandY = -36f;
			pose.SwordAngle = -1.32f;
			pose.ShieldX = 6f;
			pose.ShieldY = -45f;
		}
		else if (_animation.State == PlayerVisualState.Fall)
		{
			pose.Crouch = 6f;
			pose.Lean = 1f;
			pose.Step = -3f;
			pose.SwordHandX = -5f;
			pose.SwordHandY = -39f;
			pose.SwordAngle = -1.86f;
			pose.ShieldX = 7f;
			pose.ShieldY = -40f;
		}
		else if (_animation.State == PlayerVisualState.Block)
		{
			bool perfect = player.Combat.IsPerfectGuardFeedbackActive;
			bool contact = player.Combat.IsBlockFeedbackActive;
			pose.Crouch = perfect ? 6f : 8f;
			pose.Lean = perfect ? 0f : contact ? -5f : -3f;
			pose.Step = perfect ? 10f : 12f;
			pose.ShieldX = perfect ? 15f : contact ? 10f : 13f;
			pose.ShieldY = perfect ? -54f : -52f;
			pose.SwordHandX = -9f;
			pose.SwordHandY = -35f;
			pose.SwordAngle = -1.86f;
			pose.CloakTail = -2f;
		}
	}

	private static void ConfigureKnightHeldStance(ref Pose pose, float breath)
	{
		pose.UseKnightHeldStance = true;
		pose.Bob = -breath;
		pose.Crouch = 7f;
		pose.Lean = -2f;
		pose.Step = 11f;
		pose.SwordLength = 39f;
		pose.SwordHandX = -11f;
		pose.SwordHandY = -35f - breath;
		pose.SwordAngle = 2.18f;
		pose.ShieldX = 13f;
		pose.ShieldY = -52f - breath;
		pose.CloakTail = -3f;
	}

	private static void FinalizeKnightBodyPose(
		DungeonAscendant.Player.Player player,
		ref Pose pose)
	{
		if (!player.IsKnightSwordAndShield)
			return;

		pose.UseMartialLimbs = true;
		Vector2 swordShoulder = pose.UseKnightHeldStance
			? new Vector2(pose.Lean + 7f, pose.Crouch - 45f)
			: new Vector2(pose.Lean + 12f, pose.Crouch - 44f);
		Vector2 swordHand = new(pose.SwordHandX, pose.SwordHandY);
		Vector2 swordArm = swordHand - swordShoulder;
		Vector2 swordBend = swordArm.LengthSquared() > .01f
			? Vector2.Normalize(new Vector2(-swordArm.Y, swordArm.X)) * 4f
			: new Vector2(0f, -4f);
		Vector2 swordElbow = Vector2.Lerp(swordShoulder, swordHand, .52f) +
			swordBend;
		pose.FrontElbowX = swordElbow.X;
		pose.FrontElbowY = swordElbow.Y;

		Vector2 shieldShoulder = pose.UseKnightHeldStance
			? new Vector2(pose.Lean - 5f, pose.Crouch - 45f)
			: new Vector2(pose.Lean - 10f, pose.Crouch - 44f);
		Vector2 shieldGrip = new(pose.ShieldX + 6f, pose.ShieldY + 17f);
		float brace = Math.Clamp((pose.ShieldX + 3f) / 20f, 0f, 1f);
		Vector2 shieldElbow = Vector2.Lerp(shieldShoulder, shieldGrip, .48f) +
			new Vector2(-4f + brace * 5f, 4f - brace * 6f);
		pose.RearElbowX = shieldElbow.X;
		pose.RearElbowY = shieldElbow.Y;

		float stride = pose.Step;
		pose.FrontFootX = pose.Lean + 9f + stride * .70f;
		pose.FrontFootY = -1f - pose.LeftFootLift;
		pose.RearFootX = pose.Lean - 9f - stride * .62f;
		pose.RearFootY = -1f - pose.RightFootLift;
		pose.FrontKneeX = pose.Lean + 5f + stride * .32f;
		pose.FrontKneeY = -12f + pose.Crouch * .22f -
			pose.LeftFootLift * .55f;
		pose.RearKneeX = pose.Lean - 5f - stride * .26f;
		pose.RearKneeY = -12f + pose.Crouch * .22f -
			pose.RightFootLift * .55f;
		if (pose.UseKnightHeldStance)
		{
			pose.FrontKneeX += 2f;
			pose.FrontKneeY += 1.5f;
			pose.RearKneeX -= 1f;
			pose.RearKneeY -= .75f;
		}
	}

	private void ApplyScytheTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.Scythe || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		int stage = player.Combat.TechniqueStageIndex;
		if (effect == WeaponTechniqueEffect.ScytheUmbraDevourer)
		{
			AttackDefinition ultimateAttack = player.Combat.CurrentAttack;
			float ultimateElapsed = player.Combat.StateElapsed;
			float ultimateWindup = ((ultimateAttack == null) ? 1f : MathHelper.SmoothStep(0f, 1f, Math.Clamp(ultimateElapsed / MathF.Max(0.01f, ultimateAttack.StartupTime), 0f, 1f)));
			float ultimateStrike = ((ultimateAttack == null) ? 0f : MathHelper.SmoothStep(0f, 1f, Math.Clamp((ultimateElapsed - ultimateAttack.StartupTime) / MathF.Max(0.01f, ultimateAttack.ActiveTime), 0f, 1f)));
			float ultimateRecovery = ((ultimateAttack == null) ? 0f : MathHelper.SmoothStep(0f, 1f, Math.Clamp((ultimateElapsed - ultimateAttack.StartupTime - ultimateAttack.ActiveTime) / MathF.Max(0.01f, ultimateAttack.RecoveryTime), 0f, 1f)));
			if (1 == 0)
			{
			}
			(float, float, float) tuple = stage switch
			{
				0 => (-0.64f, -2.3f, -2.05f),
				4 => (-2.52f, 0.68f, 0.96f),
				5 => (0.86f, -2.42f, -2.7f),
				6 => (-2.28f, 0.82f, 1.06f),
				7 => (1.04f, -0.88f, -0.96f),
				_ => (-1.35f, -1.35f, -1.35f),
			};
			if (1 == 0)
			{
			}
			(float, float, float) tuple2 = tuple;
			float ultimateStart = tuple2.Item1;
			float ultimateEnd = tuple2.Item2;
			float ultimateSettle = tuple2.Item3;
			float ultimateAngle = ((ultimateElapsed < (ultimateAttack?.StartupTime ?? 0f)) ? MathHelper.Lerp(ultimateStart - 0.18f, ultimateStart, ultimateWindup) : ((ultimateRecovery <= 0f) ? MathHelper.Lerp(ultimateStart, ultimateEnd, ultimateStrike) : MathHelper.Lerp(ultimateEnd, ultimateSettle, ultimateRecovery)));
			pose.SwordLength = 58f;
			pose.SwordAngle = ultimateAngle;
			float drive = MathF.Sin(ultimateStrike * (float)Math.PI);
			if (1 == 0)
			{
			}
			float crouch;
			switch (stage)
			{
			case 0:
				crouch = 11f - ultimateRecovery * 4f;
				break;
			case 4:
			case 5:
				crouch = 7f + drive * 3f;
				break;
			case 6:
				crouch = 4f + ultimateWindup * 5f - ultimateStrike * 2f;
				break;
			case 7:
				crouch = 8f - ultimateRecovery * 5f;
				break;
			default:
				crouch = 13f;
				break;
			}
			if (1 == 0)
			{
			}
			pose.Crouch = crouch;
			if (1 == 0)
			{
			}
			crouch = stage switch
			{
				0 => 13f - ultimateRecovery * 5f,
				4 => 11f * drive,
				5 => -12f * drive,
				6 => 5f + 13f * drive,
				7 => 10f * (1f - ultimateRecovery),
				_ => 0f,
			};
			if (1 == 0)
			{
			}
			pose.Lean = crouch;
			if (1 == 0)
			{
			}
			crouch = stage switch
			{
				0 => 10f,
				4 => 8f,
				5 => -7f,
				6 => 6f,
				7 => 7f * (1f - ultimateRecovery),
				_ => 0f,
			};
			if (1 == 0)
			{
			}
			pose.Step = crouch;
			bool flag = (uint)(stage - 5) <= 1u;
			pose.LeftFootLift = (flag ? (drive * 8f) : 0f);
			flag = ((stage == 4 || stage == 6) ? true : false);
			pose.RightFootLift = (flag ? (drive * 6f) : 0f);
			pose.SwordHandX = 13f + pose.Lean * 0.2f;
			pose.SwordHandY = ((stage == 6) ? (-40f - ultimateWindup * 7f + ultimateStrike * 7f) : (-36f - MathF.Sin(ultimateAngle) * 3f));
			float grip = ((stage == 6) ? 18f : (15f + drive * 3f));
			pose.OffHandX = pose.SwordHandX - MathF.Cos(ultimateAngle) * grip;
			pose.OffHandY = pose.SwordHandY - MathF.Sin(ultimateAngle) * grip;
			pose.FrontElbowX = pose.SwordHandX + 9f;
			pose.FrontElbowY = pose.SwordHandY - 6f;
			pose.RearElbowX = pose.OffHandX - 10f;
			pose.RearElbowY = pose.OffHandY - 5f;
			pose.CloakTail = ((stage == 0) ? (-16f) : (-10f - drive * 9f));
			return;
		}
		AttackDefinition attack = player.Combat.CurrentAttack;
		float elapsed = player.Combat.StateElapsed;
		float windup = ((attack == null || attack.StartupTime <= 0.001f) ? 1f : Math.Clamp(elapsed / attack.StartupTime, 0f, 1f));
		float strike = ((attack == null) ? 0f : Math.Clamp((elapsed - attack.StartupTime) / MathF.Max(0.01f, attack.ActiveTime), 0f, 1f));
		float recovery = ((attack == null) ? 0f : Math.Clamp((elapsed - attack.StartupTime - attack.ActiveTime) / MathF.Max(0.01f, attack.RecoveryTime), 0f, 1f));
		windup = MathHelper.SmoothStep(0f, 1f, windup);
		strike = MathHelper.SmoothStep(0f, 1f, strike);
		recovery = MathHelper.SmoothStep(0f, 1f, recovery);
		GetScytheMotionProfile(effect, stage, out var startAngle, out var endAngle, out var settleAngle, out var stance, out var turn, out var gripSlide);
		float travelSign = MathF.Sign(endAngle - startAngle);
		float preloadAngle = startAngle - travelSign * 0.28f;
		float angle = ((elapsed < (attack?.StartupTime ?? 0f)) ? MathHelper.Lerp(preloadAngle, startAngle, windup) : ((recovery <= 0f) ? MathHelper.Lerp(startAngle, endAngle, strike) : (MathHelper.Lerp(endAngle, settleAngle, recovery) + travelSign * 0.22f * MathF.Sin(recovery * (float)Math.PI))));
		pose.SwordLength = 58f;
		pose.SwordAngle = angle;
		float bodyDrive = MathF.Sin(strike * (float)Math.PI) * turn;
		float settle = recovery * recovery;
		pose.Crouch = stance + MathF.Abs(bodyDrive) * 0.22f - settle * 1.5f;
		pose.Lean = bodyDrive + travelSign * (2f - settle * 2f);
		pose.Step = travelSign * (4f + MathF.Abs(bodyDrive) * 0.34f) * (1f - settle * 0.55f);
		pose.LeftFootLift = ((stage % 2 == 0) ? (MathF.Sin(strike * (float)Math.PI) * 2.5f) : 0f);
		pose.RightFootLift = ((stage % 2 != 0) ? (MathF.Sin(strike * (float)Math.PI) * 2.5f) : 0f);
		float regrip = MathF.Sin((windup + recovery) * (float)Math.PI);
		pose.SwordHandX = 13f + bodyDrive * 0.25f;
		pose.SwordHandY = -36f - MathF.Sin(angle) * 3f;
		float gripDistance = 14f + gripSlide * regrip;
		pose.OffHandX = pose.SwordHandX - MathF.Cos(angle) * gripDistance;
		pose.OffHandY = pose.SwordHandY - MathF.Sin(angle) * gripDistance;
		pose.FrontElbowX = pose.SwordHandX + 9f + bodyDrive * 0.18f;
		pose.FrontElbowY = pose.SwordHandY - 5f - MathF.Abs(bodyDrive) * 0.12f;
		pose.RearElbowX = pose.OffHandX - 10f - bodyDrive * 0.12f;
		pose.RearElbowY = pose.OffHandY - 5f;
		pose.CloakTail = -7f - MathF.Abs(bodyDrive) * 0.7f - MathF.Abs(endAngle - startAngle) * 1.3f;
		switch (effect)
		{
		case WeaponTechniqueEffect.ScytheShadowSlip:
			pose.Crouch = 11f;
			pose.Lean = 13f * player.Combat.ScytheMoveDirection;
			pose.Step = 10f * player.Combat.ScytheMoveDirection;
			pose.SwordAngle = -1.45f;
			return;
		case WeaponTechniqueEffect.ScytheCrescentDive:
		{
			float tuck = MathF.Sin(Math.Clamp(strike + windup * 0.4f, 0f, 1f) * (float)Math.PI);
			pose.Crouch = 8f + tuck * 7f;
			pose.Lean = 8f + strike * 4f;
			pose.Step = -6f + strike * 9f;
			pose.LeftFootLift = 7f * tuck;
			pose.RightFootLift = 10f * tuck;
			pose.OffHandX -= 3f * tuck;
			return;
		}
		}
		bool flag2 = effect == WeaponTechniqueEffect.ScytheAbyssalRequiem;
		bool flag3 = flag2;
		if (flag3)
		{
			bool flag = (uint)(stage - 2) <= 1u;
			flag3 = flag;
		}
		if (flag3)
		{
			float tuck2 = MathF.Sin(Math.Clamp(strike + windup * 0.35f, 0f, 1f) * (float)Math.PI);
			pose.Crouch += tuck2 * 5f;
			pose.LeftFootLift = 8f * tuck2;
			pose.RightFootLift = 11f * tuck2;
			pose.Step *= 0.35f;
			return;
		}
		bool flag4 = effect == WeaponTechniqueEffect.ScytheEclipseWaltz;
		bool flag5 = flag4;
		if (flag5)
		{
			bool flag = ((stage == 0 || stage == 2) ? true : false);
			flag5 = flag;
		}
		if (flag5)
		{
			pose.Lean += travelSign * 5f * (1f - recovery);
			pose.Crouch += 3f;
			pose.Step += travelSign * 4f;
		}
	}

	private static void GetScytheMotionProfile(WeaponTechniqueEffect effect, int stage, out float start, out float end, out float settle, out float stance, out float turn, out float gripSlide)
	{
		start = -2.15f;
		end = 0.62f;
		settle = 0.82f;
		stance = 3f;
		turn = 7f;
		gripSlide = 3f;
		switch (effect)
		{
		case WeaponTechniqueEffect.ScytheReapersDance:
		{
			if (1 == 0)
			{
			}
			(float, float, float, float, float, float) tuple = stage switch
			{
				0 => (-2.42f, 0.24f, 0.4f, 2f, 6f, 2f),
				1 => (0.48f, -2.28f, -2.48f, 3f, -8f, 6f),
				2 => (-2.7f, -7.5f, -7.75f, 7f, 9f, 5f),
				3 => (-7.9f, -2.56f, -2.34f, 4f, 12f, 7f),
				4 => (-1.82f, 0.52f, -0.18f, 6f, 8f, 8f),
				_ => (-0.46f, 7.3f, 7.58f, 8f, 14f, 8f),
			};
			if (1 == 0)
			{
			}
			(start, end, settle, stance, turn, gripSlide) = tuple;
			break;
		}
		case WeaponTechniqueEffect.ScytheDeathSpiral:
		{
			if (1 == 0)
			{
			}
			(float, float, float, float, float, float) tuple = stage switch
			{
				0 => (1.02f, -1.95f, -2.14f, 8f, -8f, 4f),
				1 => (-2.05f, 0.68f, 0.88f, 4f, 9f, 7f),
				2 => (0.82f, -2.38f, -2.58f, 3f, -11f, 5f),
				3 => (-2.32f, 0.96f, 1.18f, 5f, 12f, 7f),
				4 => (1.04f, -2.16f, -2.38f, 4f, -12f, 6f),
				_ => (-2.42f, 1.12f, 1.38f, 8f, 15f, 8f),
			};
			if (1 == 0)
			{
			}
			(start, end, settle, stance, turn, gripSlide) = tuple;
			break;
		}
		case WeaponTechniqueEffect.ScythePhantomCross:
			if (stage != 0)
			{
				float num6 = 0.82f;
				float num5 = -2.38f;
				float num4 = -2.62f;
				float num3 = 6f;
				float num2 = -13f;
				float num = 8f;
				start = num6;
				end = num5;
				settle = num4;
				stance = num3;
				turn = num2;
				gripSlide = num;
			}
			else
			{
				float num = -2.26f;
				float num2 = 0.56f;
				float num3 = 0.78f;
				float num4 = 7f;
				float num5 = 10f;
				float num6 = 5f;
				start = num;
				end = num2;
				settle = num3;
				stance = num4;
				turn = num5;
				gripSlide = num6;
			}
			break;
		case WeaponTechniqueEffect.ScytheEclipseWaltz:
		{
			if (1 == 0)
			{
			}
			(float, float, float, float, float, float) tuple = stage switch
			{
				0 => (0.98f, -1.92f, -2.12f, 9f, -8f, 5f),
				1 => (-2.18f, 0.42f, 0.66f, 2f, 9f, 6f),
				2 => (0.72f, -2.52f, -2.7f, 6f, -10f, 7f),
				3 => (-2.42f, 0.92f, 1.12f, 4f, 11f, 5f),
				_ => (-2.58f, 1.08f, 1.38f, 7f, 15f, 8f),
			};
			if (1 == 0)
			{
			}
			(start, end, settle, stance, turn, gripSlide) = tuple;
			break;
		}
		case WeaponTechniqueEffect.ScytheAbyssalRequiem:
		{
			if (1 == 0)
			{
			}
			(float, float, float, float, float, float) tuple = stage switch
			{
				0 => (-1.34f, 0.3f, 0.82f, 7f, 7f, 8f),
				1 => (1.34f, -1.28f, -1.54f, 7f, 10f, 6f),
				2 => (-2.22f, 1.02f, 1.24f, 6f, 11f, 5f),
				3 => (1.1f, -2.3f, -2.52f, 7f, -12f, 6f),
				4 => (-2.56f, 0.94f, 1.28f, 9f, 15f, 9f),
				_ => (0.62f, -2.22f, -2.46f, 6f, -9f, 7f),
			};
			if (1 == 0)
			{
			}
			(start, end, settle, stance, turn, gripSlide) = tuple;
			break;
		}
		case WeaponTechniqueEffect.ScytheCrescentDive:
		{
			float num6 = -2.36f;
			float num5 = 0.64f;
			float num4 = 0.92f;
			float num3 = 9f;
			float num2 = 12f;
			float num = 7f;
			start = num6;
			end = num5;
			settle = num4;
			stance = num3;
			turn = num2;
			gripSlide = num;
			break;
		}
		case WeaponTechniqueEffect.ScytheShadowPass:
			if (stage != 0)
			{
				float num = -2.04f;
				float num2 = 0.54f;
				float num3 = -0.18f;
				float num4 = 7f;
				float num5 = 11f;
				float num6 = 9f;
				start = num;
				end = num2;
				settle = num3;
				stance = num4;
				turn = num5;
				gripSlide = num6;
			}
			else
			{
				float num6 = 0.78f;
				float num5 = -1.8f;
				float num4 = -2.02f;
				float num3 = 10f;
				float num2 = -8f;
				float num = 4f;
				start = num6;
				end = num5;
				settle = num4;
				stance = num3;
				turn = num2;
				gripSlide = num;
			}
			break;
		case WeaponTechniqueEffect.ScytheWraithCut:
		{
			float num = -2.48f;
			float num2 = 0.92f;
			float num3 = 1.16f;
			float num4 = 6f;
			float num5 = 13f;
			float num6 = 7f;
			start = num;
			end = num2;
			settle = num3;
			stance = num4;
			turn = num5;
			gripSlide = num6;
			break;
		}
		case WeaponTechniqueEffect.ScytheShadowSlip:
			break;
		}
	}

	private void DrawScytheTechniqueEffects(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.Scythe || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		float direction = player.Combat.ScytheMoveDirection;
		switch (effect)
		{
		case WeaponTechniqueEffect.ScytheUmbraDevourer:
		{
			if (player.Combat.State == CombatState.ChargingHeavy)
			{
				return;
			}
			int stage = player.Combat.TechniqueStageIndex;
			float progress2 = ((player.Combat.CurrentAttack == null) ? 0f : Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack.TotalTime), 0f, 1f));
			bool flag;
			switch (stage)
			{
			case 0:
			{
				float fade = 1f - progress2;
				Color shadow = new Color(39, 12, 50, (int)(115f * fade));
				for (int echo = 1; echo <= 2; echo++)
				{
					float offset = (0f - direction) * (12f + (float)echo * 11f);
					float alpha = fade / (float)echo;
					DrawLine(anchor, new Vector2(offset, -51f), new Vector2(offset - direction * 8f, -14f), 7f, ScaleAlpha(shadow, alpha), flip);
					DrawLine(anchor, new Vector2(offset - 5f, -35f), new Vector2(offset + direction * 13f, -31f), 3f, new Color(104, 47, 130, (int)(65f * alpha)), flip);
				}
				DrawLine(anchor, new Vector2((0f - direction) * 34f, -3f), new Vector2(direction * 8f, -3f), 2f, new Color(57, 18, 71, (int)(125f * MathF.Sin(progress2 * (float)Math.PI))), flip);
				return;
			}
			case 4:
			case 5:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				float rupture = 1f - Math.Clamp(progress2 / 0.72f, 0f, 1f);
				float side = ((stage == 4) ? (-1f) : 1f);
				DrawLine(anchor, new Vector2(side * 4f, -4f), new Vector2(side * 16f, -58f), 8f, new Color(18, 5, 24, (int)(175f * rupture)), flip);
				DrawLine(anchor, new Vector2(side * 6f, -5f), new Vector2(side * 17f, -56f), 2f, new Color(135, 62, 169, (int)(155f * rupture)), flip);
				for (int shard = 0; shard < 3; shard++)
				{
					float y = -13f - (float)shard * 13f;
					DrawLine(anchor, new Vector2(side * (8f + progress2 * 8f), y), new Vector2(side * (15f + progress2 * 14f), y - 4f), 2f, new Color(48, 14, 60, (int)(105f * rupture)), flip);
				}
				return;
			}
			switch (stage)
			{
			case 6:
			{
				float quiet = 1f - Math.Clamp(progress2 / 0.34f, 0f, 1f);
				DrawLine(anchor, new Vector2(-15f, -3f), new Vector2(15f, -3f), 3f, new Color(35, 10, 44, (int)(115f * quiet)), flip);
				break;
			}
			case 7:
			{
				float catchup = 1f - progress2;
				float offset2 = (0f - direction) * (9f + catchup * 16f);
				DrawLine(anchor, new Vector2(offset2, -49f), new Vector2(offset2 + direction * 5f, -13f), 7f, new Color(43, 14, 54, (int)(100f * catchup)), flip);
				DrawLine(anchor, new Vector2(-18f, -2f), new Vector2(18f, -2f), 1f, new Color(95, 47, 112, (int)(75f * catchup)), flip);
				break;
			}
			}
			return;
		}
		case WeaponTechniqueEffect.ScytheShadowSlip:
		{
			float progress = Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack.TotalTime), 0f, 1f);
			float launch = 1f - Math.Clamp(progress / 0.28f, 0f, 1f);
			float settle = Math.Clamp((progress - 0.7f) / 0.3f, 0f, 1f);
			DrawLine(color: new Color(69, 27, 88, (int)(100f * MathF.Sin(progress * (float)Math.PI))), anchor: anchor, localStart: new Vector2((0f - direction) * (12f + progress * 20f), -3f), localEnd: new Vector2(direction * 7f, -3f), thickness: 2f, flip: flip);
			DrawLine(anchor, new Vector2((0f - direction) * 15f, -2f), new Vector2((0f - direction) * 3f, -6f), 2f, new Color(42, 15, 52, (int)(120f * launch)), flip);
			if (settle > 0f)
			{
				float width = 5f + settle * 10f;
				DrawLine(anchor, new Vector2(0f - width, -2f), new Vector2(width, -2f), 1f, new Color(91, 45, 112, (int)(75f * (1f - settle))), flip);
			}
			float perfect = player.Combat.ScythePerfectDodgeVisualRatio;
			if (perfect > 0f)
			{
				float flash = MathF.Sin((1f - perfect) * (float)Math.PI);
				DrawLine(anchor, new Vector2((0f - direction) * 6f, -57f), new Vector2(direction * 12f, -19f), 2f, new Color(174, 113, 207, (int)(135f * flash)), flip);
				DrawLine(anchor, new Vector2((0f - direction) * 20f, -34f), new Vector2(direction * 18f, -34f), 1f, new Color(76, 29, 94, (int)(95f * perfect)), flip);
			}
			break;
		}
		}
		if (effect == WeaponTechniqueEffect.ScytheShadowPass)
		{
			float progress3 = Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack.TotalTime), 0f, 1f);
			float trail = MathF.Sin(progress3 * (float)Math.PI);
			DrawLine(anchor, new Vector2((0f - direction) * (20f + progress3 * 24f), -4f), new Vector2(direction * 5f, -4f), 2f, new Color(54, 18, 69, (int)(115f * trail)), flip);
			DrawLine(anchor, new Vector2((0f - direction) * (16f + progress3 * 10f), -7f), new Vector2((0f - direction) * 2f, -4f), 1f, new Color(125, 62, 160, (int)(90f * trail)), flip);
		}
		if (effect == WeaponTechniqueEffect.ScythePhantomCross && player.Combat.IsAttackActive)
		{
			float fade2 = 1f - Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.12f, player.Combat.CurrentAttack.TotalTime), 0f, 1f);
			float offset3 = (float)(-MathF.Sign((pose.Step == 0f) ? direction : pose.Step)) * (10f + fade2 * 13f);
			Color shadow2 = new Color(64, 20, 83, (int)(72f * fade2));
			Color edge = new Color(116, 39, 125, (int)(46f * fade2));
			DrawLine(anchor, new Vector2(offset3, -51f), new Vector2(offset3 - 3f, -14f), 8f, shadow2, flip);
			DrawLine(anchor, new Vector2(offset3 - 6f, -36f), new Vector2(offset3 + 13f, -31f), 4f, shadow2, flip);
			DrawLine(anchor, new Vector2(offset3 - 7f, -10f), new Vector2(offset3 + 8f, -10f), 2f, edge, flip);
		}
	}

	private void DrawScytheMovementAfterimage(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.Scythe)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		float perfect = player.Combat.ScythePerfectDodgeVisualRatio;
		bool flag = ((effect == WeaponTechniqueEffect.ScytheShadowSlip || effect == WeaponTechniqueEffect.ScytheShadowPass) ? true : false);
		bool movement = flag;
		if (!movement && perfect <= 0f)
		{
			return;
		}
		float direction = player.Combat.ScytheMoveDirection;
		if (MathF.Abs(direction) < 0.1f)
		{
			direction = (flip ? (-1f) : 1f);
		}
		float fade = ((movement && player.Combat.CurrentAttack != null) ? (1f - Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.12f, player.Combat.CurrentAttack.TotalTime), 0f, 1f)) : 0f);
		float alpha = Math.Clamp(0.18f + fade * 0.25f + perfect * 0.22f, 0f, 0.52f);
		Vector2 echoAnchor = anchor - new Vector2(direction * (16f + fade * 9f + perfect * 5f), 0f);
		Color body = ScaleAlpha(new Color(40, 16, 50, 210), alpha);
		Color edge = ScaleAlpha(new Color(117, 62, 146, 190), alpha * 0.72f);
		float stretch = direction * (5f + fade * 5f);
		float bodyX = pose.Lean;
		DrawLine(echoAnchor, new Vector2(bodyX - stretch, pose.Crouch - 50f), new Vector2(bodyX, pose.Crouch - 19f), 10f, body, flip);
		DrawBox(echoAnchor, bodyX - 7f - stretch * 0.3f, pose.Crouch - 65f, 14f + MathF.Abs(stretch) * 0.3f, 15f, body, flip);
		DrawLine(echoAnchor, new Vector2(-8f - stretch * 0.25f, -19f), new Vector2(-13f - stretch, -1f), 5f, body, flip);
		DrawLine(echoAnchor, new Vector2(8f - stretch * 0.25f, -19f), new Vector2(13f - stretch, -1f), 5f, body, flip);
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 shaft = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		DrawLine(echoAnchor, hand - shaft * 17f, hand + shaft * (pose.SwordLength - 10f), 3f, body, flip);
		Vector2 tip = GetScytheBladeTip(hand, pose.SwordAngle, pose.SwordLength);
		DrawLine(echoAnchor, hand + shaft * (pose.SwordLength - 18f), tip, 2f, edge, flip);
		if (perfect > 0f)
		{
			Vector2 sourceDelta = player.Combat.ScythePerfectDodgeSource - player.Position;
			if (sourceDelta.LengthSquared() > 0.01f)
			{
				sourceDelta.Normalize();
			}
			else
			{
				sourceDelta = new Vector2(0f - direction, 0f);
			}
			Vector2 miss = new Vector2(sourceDelta.X * 18f, sourceDelta.Y * 8f);
			DrawLine(echoAnchor, new Vector2(0f - miss.X, -38f - miss.Y), new Vector2(miss.X, -38f + miss.Y), 1f, ScaleAlpha(new Color(183, 132, 205), perfect * 0.55f), flip);
		}
	}

	private void ApplyGreatswordTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.GreatSword || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		int stage = player.Combat.TechniqueStageIndex;
		float progress = ((player.Combat.CurrentAttack == null) ? 0f : Math.Clamp(player.Combat.StateElapsed / player.Combat.CurrentAttack.TotalTime, 0f, 1f));
		pose.SwordLength = 50f;
		pose.SwordHandX = 10f;
		pose.SwordHandY = -35f;
		switch (effect)
		{
		case WeaponTechniqueEffect.GreatswordCombo:
		{
			pose.Crouch = ((stage >= 2) ? 8f : 5f);
			if (1 == 0)
			{
			}
			float num = stage switch
			{
				0 => MathHelper.Lerp(-4f, 7f, progress),
				1 => MathHelper.Lerp(5f, -4f, progress),
				2 => MathHelper.Lerp(7f, -2f, progress),
				_ => MathHelper.Lerp(-6f, 10f, progress),
			};
			if (1 == 0)
			{
			}
			pose.Lean = num;
			pose.Step = ((stage < 2) ? 5f : ((stage == 2) ? 3f : 1f));
			if (1 == 0)
			{
			}
			num = stage switch
			{
				0 => MathHelper.Lerp(-1.92f, 0.62f, progress),
				1 => MathHelper.Lerp(0.72f, -1.72f, progress),
				2 => MathHelper.Lerp(0.58f, -1.62f, progress),
				_ => MathHelper.Lerp(-2.2f, 1.18f, progress),
			};
			if (1 == 0)
			{
			}
			pose.SwordAngle = num;
			pose.CloakTail = ((stage >= 2) ? (-10f) : (-7f));
			break;
		}
		case WeaponTechniqueEffect.GreatswordIronResolve:
			pose.Crouch = 8f;
			pose.Lean = -3f;
			pose.Step = -4f;
			pose.SwordHandX = -2f;
			pose.SwordHandY = -31f;
			pose.SwordAngle = -0.18f;
			pose.CloakTail = -2f;
			break;
		case WeaponTechniqueEffect.GreatswordWhirlingJudgment:
		{
			pose.Crouch = (player.Combat.IsGreatswordWhirlStopping ? MathHelper.Lerp(8f, 3f, progress) : 8f);
			float side = (((stage & 1) == 0) ? 1f : (-1f));
			pose.Lean = (player.Combat.IsGreatswordWhirlStopping ? MathHelper.Lerp(side * 4f, 0f, progress) : (side * MathF.Sin(progress * (float)Math.PI) * 5f));
			pose.Step = (0f - side) * 4f;
			pose.SwordAngle = (player.Combat.IsGreatswordWhirlStopping ? MathHelper.Lerp(side * 0.65f, -0.72f, progress) : ((stage == 6) ? MathHelper.Lerp(-2.12f, 0.92f, progress) : ((side > 0f) ? MathHelper.Lerp(-2.12f, 0.72f, progress) : MathHelper.Lerp(0.78f, -1.98f, progress))));
			pose.CloakTail = (player.Combat.IsGreatswordWhirlStopping ? (-3f) : (-10f));
			break;
		}
		case WeaponTechniqueEffect.GreatswordVanguardAssault:
			pose.Crouch = 11f;
			pose.Lean = 11f;
			pose.Step = 8f;
			pose.SwordAngle = MathHelper.Lerp(-2.18f, 0.96f, progress);
			pose.CloakTail = -15f;
			break;
		case WeaponTechniqueEffect.GreatswordFallingBastion:
			pose.Crouch = 6f;
			pose.Lean = 6f;
			pose.Step = -3f;
			pose.SwordAngle = MathHelper.Lerp(-2.06f, 1.1f, progress);
			pose.CloakTail = -9f;
			break;
		case WeaponTechniqueEffect.GreatswordTitanReckoning:
			pose.Crouch = (player.Combat.IsGreatswordCharging ? 11f : 8f);
			pose.Lean = (player.Combat.IsGreatswordCharging ? (-6f) : MathHelper.Lerp(-7f, 10f, progress));
			pose.Step = (player.Combat.IsGreatswordCharging ? (-6f) : 5f);
			pose.SwordHandX = (player.Combat.IsGreatswordCharging ? (-3f) : 7f);
			pose.SwordAngle = (player.Combat.IsGreatswordCharging ? (-0.18f) : MathHelper.Lerp(0.62f, -1.82f, progress));
			pose.CloakTail = (player.Combat.IsGreatswordCharging ? (-3f) : (-12f));
			break;
		case WeaponTechniqueEffect.GreatswordIronDominion:
		{
			pose.Crouch = ((stage == 0) ? 12f : 9f);
			pose.Lean = ((stage == 0) ? (-5f) : (((stage & 1) == 0) ? (-4f) : 5f));
			pose.Step = ((stage == 0) ? (-5f) : 3f);
			if (1 == 0)
			{
			}
			float num = stage switch
			{
				0 => MathHelper.Lerp(-0.25f, 0.12f, progress),
				1 => MathHelper.Lerp(-2.04f, 0.72f, progress),
				2 => MathHelper.Lerp(0.76f, -1.98f, progress),
				_ => MathHelper.Lerp(0.62f, -1.72f, progress),
			};
			if (1 == 0)
			{
			}
			pose.SwordAngle = num;
			pose.CloakTail = ((stage == 0) ? (-2f) : (-10f));
			break;
		}
		case WeaponTechniqueEffect.GreatswordKingsRupture:
		{
			pose.Crouch = (player.Combat.IsGreatswordCharging ? 12f : 9f);
			pose.Lean = (player.Combat.IsGreatswordCharging ? (-8f) : ((stage == 0) ? 4f : 10f));
			pose.Step = (player.Combat.IsGreatswordCharging ? (-6f) : 4f);
			float swordAngle2;
			if (player.Combat.IsGreatswordCharging)
			{
				swordAngle2 = -2.24f;
			}
			else
			{
				if (1 == 0)
				{
				}
				float num = stage switch
				{
					0 => MathHelper.Lerp(-2.1f, 0.72f, progress),
					1 => MathHelper.Lerp(-2.28f, 1.12f, progress),
					_ => MathHelper.Lerp(0.95f, 0.15f, progress),
				};
				if (1 == 0)
				{
				}
				swordAngle2 = num;
			}
			pose.SwordAngle = swordAngle2;
			pose.CloakTail = -12f;
			break;
		}
		case WeaponTechniqueEffect.GreatswordFinalVerdict:
		{
			pose.Crouch = ((player.Combat.State == CombatState.ChargingHeavy) ? 8f : ((stage >= 4) ? 12f : 9f));
			pose.Lean = ((player.Combat.State == CombatState.ChargingHeavy) ? (-5f) : ((stage == 0) ? 7f : MathHelper.Lerp(-8f, 11f, progress)));
			pose.Step = ((stage == 0) ? 7f : 2f);
			pose.SwordHandX = 4f;
			float swordAngle;
			if (player.Combat.State == CombatState.ChargingHeavy)
			{
				swordAngle = -2.12f;
			}
			else
			{
				if (1 == 0)
				{
				}
				float num = stage switch
				{
					0 => -1.92f,
					1 => MathHelper.Lerp(0.7f, -1.95f, progress),
					2 => MathHelper.Lerp(-2.3f, 1.2f, progress),
					3 => MathHelper.Lerp(-2.42f, 1.3f, progress),
					_ => 0.24f,
				};
				if (1 == 0)
				{
				}
				swordAngle = num;
			}
			pose.SwordAngle = swordAngle;
			pose.CloakTail = ((stage == 0) ? (-15f) : (-8f));
			break;
		}
		}
	}

	private void ApplySpellbladeTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.ArcaneWarStaff || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		int stage = player.Combat.TechniqueStageIndex;
		pose.SwordLength = 57f;
		pose.SwordHandX = 10f;
		pose.SwordHandY = -34f;
		pose.UseMartialLimbs = true;
		pose.FrontElbowX = 15f;
		pose.FrontElbowY = -42f;
		pose.OffHandX = -5f;
		pose.OffHandY = -39f;
		pose.RearElbowX = -16f;
		pose.RearElbowY = -43f;
		float cast = ((player.Combat.CurrentAttack == null) ? 0f : Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack.StartupTime), 0f, 1f));
		switch (player.Combat.CurrentTechnique.Effect)
		{
		case WeaponTechniqueEffect.AncientStaffArcaneBoltChain:
		{
			pose.Crouch = ((stage == 4) ? 6f : (2f + (float)stage * 0.45f));
			pose.Lean = ((stage == 4) ? MathHelper.Lerp(-5f, 7f, cast) : MathHelper.Lerp(-2f, 3f + (float)stage, cast));
			pose.Step = ((stage == 4) ? (-5f + cast * 10f) : ((float)stage - 1f));
			pose.SwordHandX = MathHelper.Lerp(7f, 17f + (float)stage, cast);
			pose.SwordHandY = MathHelper.Lerp((stage == 4) ? (-29f) : (-38f), (stage == 3) ? (-31f) : (-36f), cast);
			if (1 == 0)
			{
			}
			float swordAngle = stage switch
			{
				0 => MathHelper.Lerp(-1.02f, -0.12f, cast),
				1 => MathHelper.Lerp(-1.34f, -0.05f, cast),
				2 => MathHelper.Lerp(-0.82f, 0.02f, cast),
				3 => MathHelper.Lerp(-1.66f, -0.01f, cast),
				_ => MathHelper.Lerp(-2.18f, -0.04f, cast),
			};
			if (1 == 0)
			{
			}
			pose.SwordAngle = swordAngle;
			pose.OffHandX = ((stage == 2) ? MathHelper.Lerp(-8f, 24f, cast) : MathHelper.Lerp(-7f, 11f + (float)stage * 2f, cast));
			pose.OffHandY = ((stage == 4) ? MathHelper.Lerp(-32f, -48f, cast) : (-41f - (float)stage));
			pose.RearElbowX = MathHelper.Lerp(-17f, 3f + (float)stage, cast);
			pose.RearElbowY = -46f;
			pose.CloakTail = ((stage == 4) ? (-11f) : (-4f - (float)stage));
			break;
		}
		case WeaponTechniqueEffect.AncientStaffAegisWard:
			pose.Crouch = 5f;
			pose.Lean = -4f;
			pose.SwordHandX = 4f;
			pose.SwordHandY = -48f;
			pose.SwordAngle = -1.48f;
			pose.FrontElbowX = 10f;
			pose.FrontElbowY = -45f;
			pose.OffHandX = 24f;
			pose.OffHandY = -43f;
			pose.RearElbowX = 3f;
			pose.RearElbowY = -47f;
			pose.CloakTail = -3f;
			break;
		case WeaponTechniqueEffect.AncientStaffRunePrison:
			pose.Crouch = ((stage == 0) ? 3f : 6f);
			pose.Lean = ((stage == 0) ? (-3f) : 5f);
			pose.SwordHandX = ((stage == 0) ? 8f : 18f);
			pose.SwordHandY = ((stage == 0) ? (-46f) : (-34f));
			pose.SwordAngle = ((stage == 0) ? MathHelper.Lerp(-1.6f, -0.25f, cast) : (-0.04f));
			pose.OffHandX = ((stage == 0) ? 25f : 31f);
			pose.OffHandY = ((stage == 0) ? (-34f) : (-28f));
			pose.RearElbowX = ((stage == 0) ? 5f : 12f);
			pose.RearElbowY = -40f;
			pose.CloakTail = ((stage == 0) ? (-4f) : (-8f));
			break;
		case WeaponTechniqueEffect.AncientStaffBlinkCast:
			pose.Crouch = 9f;
			pose.Lean = 11f;
			pose.Step = 9f;
			pose.SwordHandX = 21f;
			pose.SwordHandY = -35f;
			pose.SwordAngle = -0.03f;
			pose.FrontElbowX = 17f;
			pose.FrontElbowY = -42f;
			pose.OffHandX = 10f;
			pose.OffHandY = -48f;
			pose.RearElbowX = -2f;
			pose.RearElbowY = -47f;
			pose.CloakTail = -16f;
			break;
		case WeaponTechniqueEffect.AncientStaffThunderSpirit:
			pose.Crouch = 3f;
			pose.Lean = -2f;
			pose.SwordHandX = 7f;
			pose.SwordHandY = -50f;
			pose.SwordAngle = -1.46f;
			pose.FrontElbowX = 12f;
			pose.FrontElbowY = -48f;
			pose.OffHandX = -18f + cast * 28f;
			pose.OffHandY = -35f - cast * 10f;
			pose.RearElbowX = -15f + cast * 12f;
			pose.RearElbowY = -44f;
			pose.CloakTail = -5f;
			break;
		case WeaponTechniqueEffect.AncientStaffSpiritBastion:
			pose.Crouch = 6f;
			pose.Lean = -3f + (float)stage * 2f;
			pose.SwordHandX = 5f;
			pose.SwordHandY = -45f;
			pose.SwordAngle = ((stage < 4) ? (-1.4f) : (-0.12f));
			pose.OffHandX = MathHelper.Lerp(25f, 5f, (stage < 4) ? cast : (1f - cast));
			pose.OffHandY = -42f;
			pose.RearElbowX = 4f;
			pose.RearElbowY = -48f;
			pose.CloakTail = ((stage == 4) ? (-10f) : (-5f));
			break;
		case WeaponTechniqueEffect.AncientStaffElderInvocation:
			pose.Crouch = ((stage == 3) ? 7f : 3f);
			pose.Lean = ((stage == 3) ? 5f : (-4f));
			pose.SwordHandX = ((stage == 3) ? 18f : 5f);
			pose.SwordHandY = ((stage == 3) ? (-35f) : (-55f));
			pose.SwordAngle = ((stage == 3) ? (-0.06f) : (-1.52f));
			pose.FrontElbowX = 10f;
			pose.FrontElbowY = -49f;
			pose.OffHandX = 27f;
			pose.OffHandY = ((stage == 0) ? (-45f) : (-50f));
			pose.RearElbowX = 7f;
			pose.RearElbowY = -49f;
			pose.CloakTail = ((stage == 3) ? (-12f) : (-6f));
			break;
		case WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate:
		{
			float sequence = ((player.Combat.CurrentAttack == null) ? 0f : Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack.TotalTime), 0f, 1f));
			if (player.Combat.State == CombatState.ChargingHeavy)
			{
				float judgment = player.Combat.AncientStaffUltimateCinematicRatio;
				pose.Crouch = MathHelper.Lerp(6f, 2f, judgment);
				pose.Lean = MathHelper.Lerp(-2f, -5f, judgment);
				pose.SwordHandX = MathHelper.Lerp(7f, 3f, judgment);
				pose.SwordHandY = MathHelper.Lerp(-44f, -58f, judgment);
				pose.SwordAngle = MathHelper.Lerp(-1.08f, -1.56f, judgment);
				pose.OffHandX = MathHelper.Lerp(-8f, 26f, judgment);
				pose.OffHandY = MathHelper.Lerp(-37f, -52f, judgment);
				pose.RearElbowX = MathHelper.Lerp(-15f, 7f, judgment);
				pose.RearElbowY = -48f;
				pose.CloakTail = -5f;
			}
			else if (stage <= 2)
			{
				pose.Crouch = ((stage == 1) ? 7f : 3f);
				pose.Lean = ((stage == 1) ? 2f : (-5f));
				pose.SwordHandX = 4f;
				pose.SwordHandY = -57f;
				pose.SwordAngle = -1.54f;
				pose.OffHandX = 28f;
				pose.OffHandY = -50f;
				pose.RearElbowX = 7f;
				pose.RearElbowY = -48f;
				pose.CloakTail = ((stage == 1) ? (-9f) : (-6f));
			}
			else if (stage <= 7)
			{
				pose.Crouch = 4f;
				pose.Lean = -4f;
				pose.SwordHandX = 5f;
				pose.SwordHandY = -55f;
				pose.SwordAngle = -1.5f;
				pose.OffHandX = 25f + MathF.Sin((float)stage * 1.8f) * 2f;
				pose.OffHandY = -49f;
				pose.RearElbowX = 6f;
				pose.RearElbowY = -47f;
				pose.CloakTail = -7f;
			}
			else if (stage == 8)
			{
				float release = MathHelper.SmoothStep(0f, 1f, Math.Clamp((sequence - 0.3f) / 0.3f, 0f, 1f));
				pose.Crouch = MathHelper.Lerp(7f, 11f, release);
				pose.Lean = MathHelper.Lerp(-5f, 4f, release);
				pose.SwordHandX = MathHelper.Lerp(3f, 14f, release);
				pose.SwordHandY = MathHelper.Lerp(-58f, -42f, release);
				pose.SwordAngle = MathHelper.Lerp(-1.56f, -0.2f, release);
				pose.OffHandX = MathHelper.Lerp(3f, 20f, release);
				pose.OffHandY = MathHelper.Lerp(-56f, -43f, release);
				pose.RearElbowX = MathHelper.Lerp(-5f, 5f, release);
				pose.RearElbowY = -48f;
				pose.CloakTail = MathHelper.Lerp(-5f, -14f, release);
			}
			else
			{
				pose.Crouch = MathHelper.Lerp(10f, 4f, sequence);
				pose.Lean = MathHelper.Lerp(4f, 0f, sequence);
				pose.SwordHandX = MathHelper.Lerp(14f, 9f, sequence);
				pose.SwordHandY = MathHelper.Lerp(-42f, -35f, sequence);
				pose.SwordAngle = MathHelper.Lerp(-0.2f, -0.72f, sequence);
				pose.OffHandX = MathHelper.Lerp(20f, -4f, sequence);
				pose.OffHandY = MathHelper.Lerp(-43f, -38f, sequence);
				pose.RearElbowX = MathHelper.Lerp(5f, -14f, sequence);
				pose.RearElbowY = -43f;
				pose.CloakTail = MathHelper.Lerp(-14f, -3f, sequence);
			}
			break;
		}
		case WeaponTechniqueEffect.AncientStaffFourthVeil:
			pose.Crouch = 4f;
			pose.Lean = -2f;
			pose.Step = -2f;
			pose.SwordHandX = 7f;
			pose.SwordHandY = -48f;
			pose.SwordAngle = -1.36f;
			pose.OffHandX = 18f;
			pose.OffHandY = -41f;
			pose.RearElbowX = 0f;
			pose.RearElbowY = -47f;
			pose.CloakTail = -7f;
			break;
		case WeaponTechniqueEffect.SpellbladeRunicStrikes:
		{
			pose.Crouch = ((stage == 2) ? 7f : 4f);
			pose.Lean = stage switch
			{
				1 => -3f,
				0 => 5f,
				_ => 7f,
			};
			if (1 == 0)
			{
			}
			float swordAngle = stage switch
			{
				0 => LerpByFrame(-1.38f, 0.52f),
				1 => LerpByFrame(0.72f, -1.42f),
				_ => LerpByFrame(-0.38f, 0.03f),
			};
			if (1 == 0)
			{
			}
			pose.SwordAngle = swordAngle;
			pose.CloakTail = ((stage == 2) ? (-8f) : (-5f));
			break;
		}
		case WeaponTechniqueEffect.SpellbladeRuneBrand:
			pose.Crouch = 3f;
			pose.Lean = -2f;
			pose.SwordAngle = (player.Combat.IsRuneBrandCharging ? (-0.08f) : LerpByFrame(-1.55f, -0.08f));
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
			pose.SwordAngle = 0.02f;
			pose.CloakTail = -15f;
			break;
		case WeaponTechniqueEffect.SpellbladeRunicSpear:
			pose.Crouch = 8f;
			pose.Lean = 7f;
			pose.SwordAngle = LerpByFrame(-1.2f, 0.02f);
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
		{
			pose.Crouch = ((stage == 3) ? 8f : 4f);
			pose.Lean = ((stage == 3) ? 5f : (-2f));
			pose.SwordHandX = ((stage == 0) ? 2f : 6f);
			if (1 == 0)
			{
			}
			float swordAngle = stage switch
			{
				0 => LerpByFrame(-1.5f, -2.18f),
				1 => 1.48f,
				2 => 1.48f,
				_ => LerpByFrame(-2.2f, 1.18f),
			};
			if (1 == 0)
			{
			}
			pose.SwordAngle = swordAngle;
			pose.CloakTail = ((stage == 3) ? (-9f) : (-4f));
			break;
		}
		case WeaponTechniqueEffect.AncientStaffAstralDescent:
			break;
		}
	}

	private static void ApplyRangerTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily == WeaponFamily.HunterBow && player.Combat.CurrentTechnique != null)
		{
			float charge = player.Combat.RangerChargeRatio;
			WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
			bool flag;
			switch (effect)
			{
			case WeaponTechniqueEffect.RangerMobileShot:
			case WeaponTechniqueEffect.RangerEvasiveShot:
			case WeaponTechniqueEffect.RangerTwinFang:
			case WeaponTechniqueEffect.RangerPhantomRetreat:
			case WeaponTechniqueEffect.RangerPiercingVolley:
			case WeaponTechniqueEffect.RangerSkyfallShot:
			case WeaponTechniqueEffect.RangerBackstepShot:
			case WeaponTechniqueEffect.RangerCounterShot:
			case WeaponTechniqueEffect.RangerAerialShot:
			case WeaponTechniqueEffect.RangerSkyfallVolley:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag && player.IsGrounded && MathF.Abs(player.Velocity.X) > 1f)
			{
				pose.Step = MathF.Sin(player.Combat.StateElapsed * 24f) * 8f;
				pose.Crouch = 1f + MathF.Abs(MathF.Sin(player.Combat.StateElapsed * 24f)) * 2f;
				pose.CloakTail = -7f - MathF.Abs(pose.Step) * 0.35f;
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
				pose.Lean = ((player.Combat.TechniqueStageIndex >= 3) ? (-1f) : 2f);
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
				pose.Crouch = ((player.Combat.TechniqueStageIndex == 0) ? 8f : 3f);
				pose.Lean = ((player.Combat.TechniqueStageIndex == 0) ? (-7f) : 3f);
				pose.CloakTail = 11f;
				break;
			case WeaponTechniqueEffect.RangerPhantomRetreat:
				pose.Crouch = ((player.Combat.TechniqueStageIndex == 0) ? 9f : 3f);
				pose.Lean = ((player.Combat.TechniqueStageIndex == 0) ? (-8f) : 1f);
				pose.CloakTail = 12f;
				break;
			case WeaponTechniqueEffect.RangerPiercingVolley:
				pose.Crouch = ((player.Combat.TechniqueStageIndex >= 2) ? 7f : 2f);
				pose.Lean = ((player.Combat.TechniqueStageIndex >= 2) ? (-6f) : 2f);
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
			{
				int techniqueStageIndex = player.Combat.TechniqueStageIndex;
				flag = ((techniqueStageIndex == 0 || techniqueStageIndex == 3) ? true : false);
				pose.Crouch = (flag ? 8f : 3f);
				techniqueStageIndex = player.Combat.TechniqueStageIndex;
				flag = ((techniqueStageIndex == 0 || techniqueStageIndex == 3) ? true : false);
				pose.Lean = (flag ? 8f : (-2f));
				pose.CloakTail = -12f;
				break;
			}
			case WeaponTechniqueEffect.RangerArrowStorm:
				pose.Crouch = 5f;
				pose.Lean = -7f;
				pose.SwordHandY = -48f;
				break;
			case WeaponTechniqueEffect.RangerSkyfallShot:
			case WeaponTechniqueEffect.RangerAerialShot:
			case WeaponTechniqueEffect.RangerSkyfallVolley:
				pose.Crouch = 4f;
				pose.Step = -4f;
				pose.Lean = -1f;
				pose.SwordHandY = -43f;
				break;
			case WeaponTechniqueEffect.RangerPhantomHunt:
				pose.Crouch = ((player.Combat.State == CombatState.ChargingHeavy) ? (7f - player.Combat.RangerUltimateCinematicRatio * 3f) : 5f);
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
				pose.Crouch = ((player.Combat.TechniqueStageIndex == 0) ? 8f : 5f);
				pose.Lean = ((player.Combat.TechniqueStageIndex == 0) ? (-6f) : 4f);
				pose.CloakTail = ((player.Combat.TechniqueStageIndex == 0) ? (-13f) : (-5f));
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
	}

	private void ApplyDuelistTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily == WeaponFamily.DualSwords && player.Combat.CurrentTechnique != null)
		{
			int stage = player.Combat.TechniqueStageIndex;
			switch (player.Combat.CurrentTechnique.Effect)
			{
			case WeaponTechniqueEffect.DuelistStarburstSwordArt:
				ApplyDuelistStarburstPose(player, stage, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistPhantomParry:
				ApplyDuelistParryPose(player, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistPhantomRiposte:
				ApplyDuelistRipostePose(player, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistFlashBlade:
				ApplyDuelistFlashBladePose(player, stage, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistBladeOverdrive:
				ApplyDuelistOverdrivePose(player, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistCrossRequiem:
				ApplyDuelistCrossRequiemPose(player, stage, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistThousandEdgeWaltz:
				ApplyDuelistWaltzPose(player, stage, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistHeavenSplitter:
				ApplyDuelistHeavenSplitterPose(player, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistFallingStar:
				ApplyDuelistFallingStarPose(player, ref pose);
				break;
			case WeaponTechniqueEffect.DuelistInfiniteStarburst:
				ApplyDuelistUltimatePose(player, stage, ref pose);
				break;
			}
		}
	}

	private static void PrepareDuelistSkillPose(ref Pose pose)
	{
		pose.UseMartialLimbs = true;
		pose.SwordLength = 34f;
		pose.OffSwordLength = 30f;
		pose.SwordHandX = 17f;
		pose.SwordHandY = -32f;
		pose.OffHandX = -6f;
		pose.OffHandY = -30f;
	}

	private static void FinishDuelistSkillArms(ref Pose pose)
	{
		pose.FrontElbowX = MathHelper.Lerp(11f, pose.SwordHandX - 4f, 0.58f);
		pose.FrontElbowY = MathHelper.Lerp(-43f, pose.SwordHandY, 0.52f) - 2f;
		pose.RearElbowX = MathHelper.Lerp(-11f, pose.OffHandX, 0.55f) - 1f;
		pose.RearElbowY = MathHelper.Lerp(-42f, pose.OffHandY, 0.52f) + 1f;
	}

	private static void ApplyDuelistOverdrivePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		float progress = GetDuelistMotionProgress(player);
		float raise = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.42f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.58f) / 0.42f, 0f, 1f));
		pose.Crouch = 4f + settle * 4f;
		pose.Lean = -2f + settle * 7f;
		pose.Step = -2f + settle * 6f;
		pose.SwordHandX = MathHelper.Lerp(15f, 7f, raise) + settle * 11f;
		pose.SwordHandY = MathHelper.Lerp(-31f, -45f, raise) + settle * 12f;
		pose.OffHandX = MathHelper.Lerp(-7f, 6f, raise) - settle * 9f;
		pose.OffHandY = MathHelper.Lerp(-30f, -44f, raise) + settle * 13f;
		pose.SwordAngle = MathHelper.Lerp(-0.72f, -2.23f, raise);
		pose.OffSwordAngle = MathHelper.Lerp(-1.88f, 0.66f, raise);
		if (settle > 0f)
		{
			pose.SwordAngle = MathHelper.Lerp(pose.SwordAngle, -0.42f, settle);
			pose.OffSwordAngle = MathHelper.Lerp(pose.OffSwordAngle, 0.8f, settle);
		}
		pose.CloakTail = -4f - settle * 8f;
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistCrossRequiemPose(DungeonAscendant.Player.Player player, int stage, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		float progress = GetDuelistMotionProgress(player);
		float strike = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.62f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.64f) / 0.36f, 0f, 1f));
		pose.CloakTail = ((stage == 0) ? (-3f) : (-10f));
		switch (stage)
		{
		case 0:
			pose.Crouch = 7f;
			pose.Lean = -2f;
			pose.Step = -3f;
			pose.SwordHandX = 10f;
			pose.SwordHandY = -36f;
			pose.OffHandX = 7f;
			pose.OffHandY = -35f;
			pose.SwordAngle = MathHelper.Lerp(-0.48f, -2.24f, strike);
			pose.OffSwordAngle = MathHelper.Lerp(-1.78f, 0.7f, strike);
			break;
		case 1:
			pose.Crouch = 9f;
			pose.Lean = -4f + strike * 8f - settle * 3f;
			pose.Step = -5f + strike * 8f - settle * 2f;
			pose.SwordAngle = -0.38f;
			pose.OffSwordAngle = DuelistSweep(-1.94f, 0.48f, 0.82f, strike, settle);
			pose.OffHandY += 6f;
			break;
		case 2:
			pose.Crouch = 8f - strike * 3f;
			pose.Lean = 5f - strike * 9f + settle * 3f;
			pose.Step = 6f - strike * 10f + settle * 3f;
			pose.SwordAngle = DuelistSweep(0.76f, -1.3f, -1.58f, strike, settle);
			pose.OffSwordAngle = 0.86f;
			pose.LeftFootLift = MathF.Sin(strike * (float)Math.PI) * 3f;
			break;
		default:
			pose.Crouch = 8f;
			pose.Lean = -6f + strike * 16f - settle * 7f;
			pose.Step = -7f + strike * 16f - settle * 7f;
			pose.SwordAngle = DuelistSweep(-1.64f, 0.66f, -0.34f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.92f, -1.08f, -1.72f, strike, settle);
			pose.SwordHandX += strike * 7f;
			pose.OffHandX += strike * 8f;
			pose.RightFootLift = MathF.Sin(strike * (float)Math.PI) * 2f;
			pose.CloakTail = -15f + settle * 7f;
			break;
		}
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistWaltzPose(DungeonAscendant.Player.Player player, int stage, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		float progress = GetDuelistMotionProgress(player);
		float strike = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.62f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.64f) / 0.36f, 0f, 1f));
		bool flag = (uint)(stage - 4) <= 1u;
		bool reverse = flag;
		float side = ((stage % 2 == 0) ? 1f : (-1f));
		pose.Crouch = ((stage == 6) ? 8f : (6f + MathF.Abs(side) * 1f));
		pose.Lean = side * (-5f + strike * 11f) - settle * side * 4f;
		pose.Step = (reverse ? (0f - side) : side) * (3f + strike * 5f) - settle * side * 2f;
		pose.LeftFootLift = ((side > 0f) ? (MathF.Sin(strike * (float)Math.PI) * 2.5f) : 0f);
		pose.RightFootLift = ((side < 0f) ? (MathF.Sin(strike * (float)Math.PI) * 2.5f) : 0f);
		pose.CloakTail = (reverse ? (8f * side) : (-10f * side));
		if (stage == 0)
		{
			pose.SwordAngle = -0.48f;
			pose.OffSwordAngle = DuelistSweep(-1.86f, 0.62f, 0.92f, strike, settle);
			pose.OffHandY += 5f;
		}
		else if (stage == 1)
		{
			pose.SwordAngle = DuelistSweep(0.72f, -0.38f, -1.12f, strike, settle);
			pose.OffSwordAngle = 0.82f;
		}
		else if ((uint)(stage - 2) <= 1u)
		{
			pose.SwordAngle = ((stage == 2) ? DuelistSweep(-1.26f, 0.54f, 0.18f, strike, settle) : (-0.62f));
			pose.OffSwordAngle = ((stage == 3) ? DuelistSweep(0.82f, -1.24f, -1.64f, strike, settle) : 0.58f);
			pose.SwordHandX += ((stage == 2) ? (strike * 7f) : 0f);
			pose.OffHandX += ((stage == 3) ? (strike * 8f) : 0f);
		}
		else if ((uint)(stage - 4) <= 1u)
		{
			pose.SwordAngle = ((stage == 5) ? DuelistSweep(0.78f, -1.32f, -1.62f, strike, settle) : (-0.48f));
			pose.OffSwordAngle = ((stage == 4) ? DuelistSweep(-1.72f, 0.58f, 0.92f, strike, settle) : 0.7f);
		}
		else
		{
			pose.Lean = -7f + strike * 17f - settle * 8f;
			pose.Step = -8f + strike * 16f - settle * 7f;
			pose.SwordAngle = DuelistSweep(-1.68f, 0.74f, -0.38f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.96f, -1.06f, -1.7f, strike, settle);
			pose.SwordHandX += strike * 8f;
			pose.OffHandX += strike * 8f;
			pose.CloakTail = -16f + settle * 8f;
		}
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistHeavenSplitterPose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		bool charging = player.Combat.State == CombatState.ChargingHeavy;
		float progress = (charging ? Math.Clamp(player.Combat.StateElapsed / 0.9f, 0f, 1f) : GetDuelistMotionProgress(player));
		float release = (charging ? 0f : MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.42f, 0f, 1f)));
		float settle = (charging ? 0f : MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.56f) / 0.44f, 0f, 1f)));
		pose.Crouch = (charging ? (7f + progress * 2f) : (9f - release * 4f));
		pose.Lean = (charging ? (-4f) : (-7f + release * 14f - settle * 7f));
		pose.Step = (charging ? (-3f) : (-6f + release * 12f - settle * 6f));
		pose.SwordHandX = (charging ? 9f : (9f + release * 14f));
		pose.SwordHandY = (charging ? (-37f) : (-37f + release * 6f));
		pose.OffHandX = (charging ? 6f : (6f + release * 10f));
		pose.OffHandY = (charging ? (-36f) : (-36f + release * 7f));
		pose.SwordAngle = (charging ? (-2.2f) : DuelistSweep(-2.2f, -0.36f, -0.7f, release, settle));
		pose.OffSwordAngle = (charging ? 0.7f : DuelistSweep(0.7f, -1.9f, -1.28f, release, settle));
		pose.CloakTail = (charging ? (-3f - progress * 3f) : (-15f + settle * 8f));
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistFallingStarPose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		float progress = GetDuelistMotionProgress(player);
		float compress = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.28f, 0f, 1f));
		float descend = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.18f) / 0.58f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.78f) / 0.22f, 0f, 1f));
		pose.Crouch = 3f + compress * 5f + descend * 2f;
		pose.Lean = -2f + descend * 10f - settle * 6f;
		pose.Step = -3f + descend * 8f - settle * 4f;
		pose.SwordAngle = DuelistSweep(-1.72f, 1.22f, 0.68f, descend, settle);
		pose.OffSwordAngle = DuelistSweep(0.78f, 1.88f, 2.2f, descend, settle);
		pose.SwordHandY = -37f + descend * 10f;
		pose.OffHandY = -36f + descend * 9f;
		pose.CloakTail = -5f - descend * 10f + settle * 7f;
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistFlashBladePose(DungeonAscendant.Player.Player player, int stage, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		float progress = GetDuelistMotionProgress(player);
		float strike = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.58f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.6f) / 0.4f, 0f, 1f));
		pose.Crouch = 10f - settle * 3f;
		pose.Lean = 7f + strike * 5f - settle * 7f;
		pose.Step = 8f + strike * 5f - settle * 8f;
		pose.CloakTail = -17f + settle * 8f;
		if (stage == 0)
		{
			pose.SwordAngle = MathHelper.Lerp(-0.58f, -0.04f, strike);
			pose.OffSwordAngle = 0.88f;
			pose.SwordHandX += strike * 13f;
			pose.SwordHandY -= 2f;
		}
		else
		{
			pose.SwordAngle = -0.68f;
			pose.OffSwordAngle = DuelistSweep(0.82f, -1.1f, -1.62f, strike, settle);
			pose.OffHandX += strike * 13f;
			pose.Step -= strike * 4f;
		}
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistUltimatePose(DungeonAscendant.Player.Player player, int stage, ref Pose pose)
	{
		PrepareDuelistSkillPose(ref pose);
		pose.SwordLength = 36f;
		pose.OffSwordLength = 32f;
		if (player.Combat.State == CombatState.ChargingHeavy)
		{
			float gaze = MathHelper.SmoothStep(0f, 1f, player.Combat.DuelistUltimateCinematicRatio);
			pose.Crouch = 4f + gaze * 2f;
			pose.Lean = -2f;
			pose.Step = -2f;
			pose.SwordHandX = MathHelper.Lerp(16f, 8f, gaze);
			pose.SwordHandY = MathHelper.Lerp(-31f, -40f, gaze);
			pose.OffHandX = MathHelper.Lerp(-6f, 7f, gaze);
			pose.OffHandY = MathHelper.Lerp(-30f, -39f, gaze);
			pose.SwordAngle = MathHelper.Lerp(-0.72f, -2.2f, gaze);
			pose.OffSwordAngle = MathHelper.Lerp(-1.82f, 0.72f, gaze);
			pose.CloakTail = -3f;
			FinishDuelistSkillArms(ref pose);
			return;
		}
		float progress = GetDuelistMotionProgress(player);
		if (stage >= 32)
		{
			float ascent = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.08f) / 0.14f, 0f, 1f));
			float dive = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.38f) / 0.15f, 0f, 1f));
			float recover = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.62f) / 0.38f, 0f, 1f));
			pose.Crouch = 7f + ascent * 4f + dive * 2f - recover * 5f;
			pose.Lean = -3f + dive * 13f - recover * 8f;
			pose.Step = -4f + dive * 12f - recover * 7f;
			pose.SwordHandX = 9f + dive * 15f - recover * 7f;
			pose.SwordHandY = -39f + dive * 12f;
			pose.OffHandX = 7f + dive * 12f - recover * 6f;
			pose.OffHandY = -38f + dive * 11f;
			pose.SwordAngle = ((dive < 0.58f) ? (-2.18f) : MathHelper.Lerp(-2.18f, 0.7f, Math.Clamp((dive - 0.58f) / 0.42f, 0f, 1f)));
			pose.OffSwordAngle = ((dive < 0.25f) ? 0.72f : MathHelper.Lerp(0.72f, -1.1f, Math.Clamp((dive - 0.25f) / 0.75f, 0f, 1f)));
			pose.CloakTail = -5f - ascent * 7f - dive * 8f + recover * 10f;
			FinishDuelistSkillArms(ref pose);
			return;
		}
		int group = Math.Clamp(stage / 8, 0, 3);
		int variation = stage % 8;
		bool frontStrike = stage % 2 == 0;
		float strike = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.56f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.6f) / 0.4f, 0f, 1f));
		float side = (frontStrike ? 1f : (-1f));
		float lift = (float)group * 1.5f + (float)variation * 0.18f;
		pose.Crouch = 7f - (float)group * 0.55f;
		pose.Lean = side * (-4f + strike * (10f + (float)group)) - settle * side * 4f;
		pose.Step = side * (-3f + strike * (8f + (float)group)) - settle * side * 3f;
		pose.SwordHandY -= lift;
		pose.OffHandY -= lift * 0.86f;
		pose.CloakTail = -10f - (float)group * 2f;
		pose.LeftFootLift = (frontStrike ? (MathF.Sin(strike * (float)Math.PI) * (2f + (float)group * 0.35f)) : 0f);
		pose.RightFootLift = (frontStrike ? 0f : (MathF.Sin(strike * (float)Math.PI) * (2f + (float)group * 0.35f)));
		switch (variation)
		{
		case 0:
			pose.SwordAngle = DuelistSweep(-1.44f, 0.54f, 0.18f, strike, settle);
			pose.OffSwordAngle = 0.82f;
			pose.SwordHandX += strike * 7f;
			break;
		case 1:
			pose.SwordAngle = -0.54f;
			pose.OffSwordAngle = DuelistSweep(0.86f, -1.18f, -1.58f, strike, settle);
			pose.OffHandX += strike * 8f;
			break;
		case 2:
			pose.SwordAngle = MathHelper.Lerp(-0.3f, -0.05f, strike);
			pose.OffSwordAngle = 0.76f;
			pose.SwordHandX += MathF.Sin(strike * (float)Math.PI) * 13f;
			pose.Crouch += 2f;
			break;
		case 3:
			pose.SwordAngle = -0.68f;
			pose.OffSwordAngle = DuelistSweep(0.74f, -1.42f, -1.72f, strike, settle);
			pose.OffHandY += 4f - strike * 7f;
			break;
		case 4:
			pose.SwordAngle = DuelistSweep(-1.22f, -0.1f, 0.42f, strike, settle);
			pose.OffSwordAngle = 1.02f;
			break;
		case 5:
			pose.SwordAngle = -0.42f;
			pose.OffSwordAngle = DuelistSweep(-1.74f, 0.58f, 0.92f, strike, settle);
			pose.Step *= -1f;
			break;
		case 6:
			pose.SwordAngle = DuelistSweep(-1.62f, 0.58f, -0.34f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.92f, -1.08f, -1.66f, strike, settle);
			pose.SwordHandX += strike * 5f;
			pose.OffHandX += strike * 6f;
			break;
		default:
			pose.Crouch += 2f;
			pose.Lean = -7f + strike * 16f - settle * 7f;
			pose.Step = -7f + strike * 14f - settle * 6f;
			pose.SwordAngle = DuelistSweep(-1.72f, 0.7f, -0.44f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(1.02f, -1.18f, -1.78f, strike, settle);
			pose.SwordHandY -= (float)group * 1.5f;
			pose.OffHandY -= (float)group * 1.2f;
			pose.CloakTail = -15f;
			break;
		}
		FinishDuelistSkillArms(ref pose);
	}

	private static void ApplyDuelistStarburstPose(DungeonAscendant.Player.Player player, int stage, ref Pose pose)
	{
		float progress = GetDuelistMotionProgress(player);
		float strike = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.58f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.58f) / 0.42f, 0f, 1f));
		pose.UseMartialLimbs = true;
		pose.SwordLength = 33f;
		pose.OffSwordLength = 29f;
		pose.Crouch = ((stage >= 6) ? 7f : 4f);
		pose.CloakTail = ((stage >= 6) ? (-12f) : (-8f));
		pose.SwordHandX = 18f;
		pose.SwordHandY = -31f;
		pose.OffHandX = -7f;
		pose.OffHandY = -30f;
		switch (stage)
		{
		case 0:
			pose.SwordHandY = -32f;
			pose.OffHandX = -3f;
			pose.OffHandY = -41f;
			pose.Lean = MathHelper.Lerp(1f, 7f, strike) - settle * 3f;
			pose.Step = MathHelper.Lerp(2f, 8f, strike) - settle * 2f;
			pose.SwordAngle = DuelistSweep(-0.78f, 0.58f, 0.28f, strike, settle);
			pose.OffSwordAngle = MathHelper.Lerp(MathHelper.Lerp(-1.86f, -1.42f, strike), 0.78f, settle);
			pose.SwordHandX += strike * 5f;
			pose.LeftFootLift = settle * 1.5f;
			break;
		case 1:
			pose.Lean = MathHelper.Lerp(5f, -4f, strike) + settle * 2f;
			pose.Step = MathHelper.Lerp(6f, -5f, strike) + settle * 2f;
			pose.SwordAngle = DuelistSweep(0.28f, -0.48f, -0.1f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.78f, -1.18f, -0.04f, strike, settle);
			pose.OffHandX += strike * 7f;
			pose.RearElbowX = -12f + strike * 9f;
			pose.RightFootLift = strike * 2f;
			break;
		case 2:
		{
			float secondThrust = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.24f) / 0.34f, 0f, 1f));
			pose.Crouch = 6f;
			pose.Lean = 3f + strike * 5f - settle * 3f;
			pose.Step = 4f + strike * 5f - settle * 2f;
			pose.SwordAngle = MathHelper.Lerp(-0.1f, -1.25f, settle);
			pose.OffSwordAngle = MathHelper.Lerp(-0.04f, 0.32f, settle);
			pose.SwordHandX += MathF.Sin(strike * (float)Math.PI) * 12f;
			pose.OffHandX += MathF.Sin(secondThrust * (float)Math.PI) * 13f;
			pose.SwordHandY -= 3f;
			pose.OffHandY += 4f;
			break;
		}
		case 3:
			pose.Crouch = 6f;
			pose.Lean = MathHelper.Lerp(-5f, 7f, strike) - settle * 3f;
			pose.Step = MathHelper.Lerp(-7f, 7f, strike) - settle * 3f;
			pose.SwordAngle = DuelistSweep(-1.25f, -0.48f, 0.82f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.32f, -2.28f, -1.88f, strike, settle);
			pose.LeftFootLift = MathF.Sin(strike * (float)Math.PI) * 3f;
			pose.SwordHandX -= 3f * strike;
			pose.OffHandX += 8f * strike;
			break;
		case 4:
			pose.Crouch = 9f - strike * 5f + settle * 2f;
			pose.Lean = -3f + strike * 8f - settle * 2f;
			pose.Step = -3f + strike * 8f - settle * 2f;
			pose.SwordAngle = DuelistSweep(0.82f, -1.22f, -1.4f, strike, settle);
			pose.OffSwordAngle = MathHelper.Lerp(-1.88f, -1.62f, settle);
			pose.SwordHandY += 5f - strike * 8f;
			break;
		case 5:
			pose.Crouch = 4f + strike * 4f;
			pose.Lean = 5f - strike * 10f + settle * 3f;
			pose.Step = 5f - strike * 9f + settle * 2f;
			pose.SwordAngle = DuelistSweep(-1.4f, -0.62f, -1.48f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(-1.62f, 0.68f, 0.82f, strike, settle);
			pose.OffHandY -= 6f * (1f - strike);
			pose.RightFootLift = MathF.Sin(strike * (float)Math.PI) * 2f;
			break;
		case 6:
			pose.Crouch = 7f;
			pose.Lean = -2f + strike * 5f - settle * 2f;
			pose.Step = -4f + strike * 7f - settle * 2f;
			pose.SwordAngle = DuelistSweep(-1.48f, 0.48f, -1.62f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.82f, -1.02f, 0.98f, strike, settle);
			pose.SwordHandX += strike * 4f;
			pose.OffHandX += strike * 5f;
			break;
		default:
			pose.Crouch = MathHelper.Lerp(10f, 6f, strike) + settle * 2f;
			pose.Lean = MathHelper.Lerp(-6f, 10f, strike) - settle * 6f;
			pose.Step = MathHelper.Lerp(-7f, 9f, strike) - settle * 6f;
			pose.SwordAngle = DuelistSweep(-1.62f, 0.72f, -0.42f, strike, settle);
			pose.OffSwordAngle = DuelistSweep(0.98f, -1.02f, -1.72f, strike, settle);
			pose.SwordHandX += strike * 7f - settle * 3f;
			pose.OffHandX += strike * 8f - settle * 3f;
			pose.CloakTail = -15f + settle * 6f;
			break;
		}
		pose.FrontElbowX = MathHelper.Lerp(12f, pose.SwordHandX - 5f, 0.58f);
		pose.FrontElbowY = MathHelper.Lerp(-43f, pose.SwordHandY, 0.52f) - 3f;
		pose.RearElbowX = MathHelper.Lerp(-12f, pose.OffHandX, 0.55f) - 2f;
		pose.RearElbowY = MathHelper.Lerp(-43f, pose.OffHandY, 0.52f) + 2f;
	}

	private static void ApplyDuelistParryPose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		float progress = GetDuelistMotionProgress(player);
		float brace = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.28f, 0f, 1f));
		float recoil = (player.Combat.IsBlockFeedbackActive ? MathF.Sin(Math.Clamp(progress * 7f, 0f, 1f) * (float)Math.PI) : 0f);
		pose.UseMartialLimbs = true;
		pose.Crouch = 7f + recoil * 2f;
		pose.Lean = -2f - recoil * 2f;
		pose.Step = -2f;
		pose.SwordHandX = MathHelper.Lerp(21f, 12f, brace) - recoil * 3f;
		pose.SwordHandY = MathHelper.Lerp(-33f, -35f, brace);
		pose.OffHandX = MathHelper.Lerp(-3f, 9f, brace) - recoil * 4f;
		pose.OffHandY = MathHelper.Lerp(-43f, -34f, brace);
		pose.SwordAngle = MathHelper.Lerp(-0.74f, -2.28f, brace);
		pose.OffSwordAngle = MathHelper.Lerp(-1.86f, 0.68f, brace);
		pose.SwordLength = 33f;
		pose.OffSwordLength = 29f;
		pose.FrontElbowX = MathHelper.Lerp(13f, 4f, brace);
		pose.FrontElbowY = MathHelper.Lerp(-39f, -41f, brace);
		pose.RearElbowX = MathHelper.Lerp(-14f, -3f, brace);
		pose.RearElbowY = MathHelper.Lerp(-47f, -40f, brace);
		pose.CloakTail = -4f;
	}

	private static void ApplyDuelistRipostePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		float progress = GetDuelistMotionProgress(player);
		float first = MathHelper.SmoothStep(0f, 1f, Math.Clamp(progress / 0.34f, 0f, 1f));
		float second = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.24f) / 0.34f, 0f, 1f));
		float thrust = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.52f) / 0.28f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.8f) / 0.2f, 0f, 1f));
		pose.UseMartialLimbs = true;
		pose.Crouch = 7f - thrust * 2f + settle * 2f;
		pose.Lean = 1f + thrust * 9f - settle * 6f;
		pose.Step = 2f + thrust * 8f - settle * 5f;
		pose.SwordHandX = 17f + thrust * 13f - settle * 7f;
		pose.SwordHandY = -32f;
		pose.OffHandX = -5f + second * 9f - settle * 4f;
		pose.OffHandY = -30f;
		pose.SwordAngle = ((thrust > 0.05f) ? MathHelper.Lerp(0.42f, -0.06f, thrust) : MathHelper.Lerp(-1.12f, 0.42f, first));
		pose.OffSwordAngle = MathHelper.Lerp(0.72f, -1.08f, second);
		pose.SwordLength = 34f;
		pose.OffSwordLength = 29f;
		pose.FrontElbowX = 13f + thrust * 7f;
		pose.FrontElbowY = -39f;
		pose.RearElbowX = -9f + second * 8f;
		pose.RearElbowY = -38f;
		pose.CloakTail = -12f + settle * 6f;
	}

	private static float GetDuelistMotionProgress(DungeonAscendant.Player.Player player)
	{
		float duration = player.Combat.CurrentAttack?.TotalTime ?? 0.2f;
		return Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, duration), 0f, 1f);
	}

	private static float DuelistSweep(float windup, float contact, float recovery, float strike, float settle)
	{
		return MathHelper.Lerp(MathHelper.Lerp(windup, contact, strike), recovery, settle);
	}

	private void DrawDuelistTechniqueEffects(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.DualSwords)
		{
			return;
		}
		Vector2 stellarHand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 lunarHand = new Vector2(pose.OffHandX, pose.OffHandY);
		Vector2 stellarDirection = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 lunarDirection = new Vector2(MathF.Cos(pose.OffSwordAngle), MathF.Sin(pose.OffSwordAngle));
		Vector2 stellarTip = stellarHand + stellarDirection * pose.SwordLength;
		Vector2 lunarTip = lunarHand + lunarDirection * pose.OffSwordLength;
		Vector2 crossing = (stellarHand + stellarDirection * 17f + lunarHand + lunarDirection * 15f) * 0.5f;
		if (_duelistTierPulseRemaining > 0f)
		{
			float pulse = MathF.Sin(_duelistTierPulseRemaining / 0.3f * (float)Math.PI);
			DrawDuelistStarFlash(anchor, flip, crossing, 4f + pulse * 3f, pulse * 0.82f);
			DrawLine(anchor, stellarHand + stellarDirection * 4f, stellarTip, 1f, ScaleAlpha(new Color(207, 249, 255), pulse * 0.72f), flip);
			DrawLine(anchor, lunarHand + lunarDirection * 4f, lunarTip, 1f, ScaleAlpha(new Color(224, 205, 248), pulse * 0.68f), flip);
		}
		if (_duelistOverdriveVisualStrength > 0.02f)
		{
			float strength = _duelistOverdriveVisualStrength;
			DrawLine(anchor, stellarHand + stellarDirection * 7f, stellarTip, 1f, ScaleAlpha(new Color(218, 252, 255), strength * 0.82f), flip);
			DrawLine(anchor, lunarHand + lunarDirection * 7f, lunarTip, 1f, ScaleAlpha(new Color(229, 214, 250), strength * 0.78f), flip);
			if (player.Combat.IsAttackActive)
			{
				float shimmer = 0.55f + MathF.Sin(_visualTimeSeconds * 24f) * 0.25f;
				DrawBox(anchor, stellarTip.X - 1f, stellarTip.Y - 1f, 2f, 2f, ScaleAlpha(new Color(215, 250, 255), strength * shimmer), flip);
				DrawBox(anchor, lunarTip.X - 1f, lunarTip.Y - 1f, 2f, 2f, ScaleAlpha(new Color(225, 205, 248), strength * shimmer), flip);
			}
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		if (effect == WeaponTechniqueEffect.DuelistInfiniteStarburst)
		{
			return;
		}
		float progress = GetDuelistMotionProgress(player);
		int stage = player.Combat.TechniqueStageIndex;
		switch (effect)
		{
		case WeaponTechniqueEffect.DuelistBladeOverdrive:
		{
			float contact = Math.Clamp(1f - MathF.Abs(progress - 0.48f) / 0.24f, 0f, 1f);
			if (contact > 0f)
			{
				DrawDuelistStarFlash(anchor, flip, crossing, 5f + contact * 4f, contact);
			}
			return;
		}
		case WeaponTechniqueEffect.DuelistHeavenSplitter:
			if (player.Combat.State == CombatState.ChargingHeavy)
			{
				float charge = Math.Clamp(player.Combat.StateElapsed / 0.9f, 0f, 1f);
				int level = ((player.Combat.StateElapsed >= 0.9f) ? 3 : ((!(player.Combat.StateElapsed >= 0.45f)) ? 1 : 2));
				float breath = 0.82f + MathF.Sin(_visualTimeSeconds * 9f) * 0.12f;
				float reach = 4f + (float)level * 2f;
				Color cyan = ScaleAlpha(new Color(210, 249, 255), (0.48f + charge * 0.36f) * breath);
				Color violet = ScaleAlpha(new Color(222, 203, 246), (0.44f + charge * 0.34f) * breath);
				Vector2 diagonal = Vector2.Normalize(new Vector2(1f, 0.72f));
				Vector2 reverse = new Vector2(diagonal.X, 0f - diagonal.Y);
				DrawLine(anchor, crossing - diagonal * reach, crossing + diagonal * reach, (level >= 2) ? 2f : 1f, cyan, flip);
				DrawLine(anchor, crossing - reverse * reach, crossing + reverse * reach, (level >= 3) ? 2f : 1f, violet, flip);
				if (level >= 2)
				{
					DrawDuelistStarFlash(anchor, flip, crossing, (level == 3) ? 6f : 4f, breath * 0.72f);
				}
				if (level == 3)
				{
					DrawDuelistConstellation(anchor, flip, crossing + new Vector2(8f, -7f), 0.55f);
				}
				return;
			}
			break;
		}
		if (effect == WeaponTechniqueEffect.DuelistCrossRequiem && stage == 0 && progress >= 0.18f && progress <= 0.76f)
		{
			float contact2 = MathF.Sin(Math.Clamp((progress - 0.18f) / 0.58f, 0f, 1f) * (float)Math.PI);
			DrawDuelistStarFlash(anchor, flip, crossing, 4f, contact2 * (player.Combat.IsPerfectPhantomFeedbackActive ? 1f : 0.62f));
		}
		if (((effect == WeaponTechniqueEffect.DuelistStarburstSwordArt && stage == 7) || (effect == WeaponTechniqueEffect.DuelistCrossRequiem && stage == 3) || (effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz && stage == 6)) && progress >= 0.48f && progress <= 0.88f)
		{
			float alpha = MathF.Sin((progress - 0.48f) / 0.4f * (float)Math.PI);
			DrawDuelistConstellation(anchor, flip, (stellarTip + lunarTip) * 0.5f, alpha * 0.7f);
		}
		if (effect == WeaponTechniqueEffect.DuelistFallingStar && progress >= 0.42f && progress <= 0.88f)
		{
			float impact = MathF.Sin((progress - 0.42f) / 0.46f * (float)Math.PI);
			Vector2 ground = new Vector2(pose.Lean + 20f, -3f);
			Vector2 slash = Vector2.Normalize(new Vector2(1f, 0.48f));
			Vector2 reverse2 = new Vector2(slash.X, 0f - slash.Y);
			DrawLine(anchor, ground - slash * 8f, ground + slash * 8f, 2f, ScaleAlpha(new Color(204, 245, 250), impact), flip);
			DrawLine(anchor, ground - reverse2 * 7f, ground + reverse2 * 7f, 2f, ScaleAlpha(new Color(215, 194, 239), impact), flip);
			DrawDuelistStarFlash(anchor, flip, ground, 4f, impact * 0.75f);
		}
	}

	private void DrawDuelistConstellation(Vector2 anchor, bool flip, Vector2 center, float alpha)
	{
		Color line = ScaleAlpha(new Color(157, 204, 222), alpha * 0.34f);
		Color cyan = ScaleAlpha(new Color(218, 251, 255), alpha);
		Color violet = ScaleAlpha(new Color(224, 206, 247), alpha * 0.88f);
		Vector2 first = center + new Vector2(-7f, 3f);
		Vector2 second = center + new Vector2(0f, -5f);
		Vector2 third = center + new Vector2(8f, 1f);
		DrawLine(anchor, first, second, 1f, line, flip);
		DrawLine(anchor, second, third, 1f, line, flip);
		DrawBox(anchor, first.X - 1f, first.Y - 1f, 2f, 2f, violet, flip);
		DrawBox(anchor, second.X - 1f, second.Y - 1f, 3f, 3f, cyan, flip);
		DrawBox(anchor, third.X - 1f, third.Y - 1f, 2f, 2f, violet, flip);
	}

	private void DrawDuelistAfterimages(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.DualSwords || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		if ((effect == WeaponTechniqueEffect.DuelistPhantomRiposte || effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz || effect == WeaponTechniqueEffect.DuelistFlashBlade) ? true : false)
		{
			float intensity = 0.42f;
			if ((effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz || effect == WeaponTechniqueEffect.DuelistFlashBlade) ? true : false)
			{
				intensity += player.Combat.Resources.DuelistMomentumTier switch
				{
					1 => 0.05f,
					2 => 0.1f,
					_ => 0f,
				};
				intensity += _duelistOverdriveVisualStrength * 0.08f;
			}
			float direction = (flip ? 1f : (-1f));
			DrawDuelistAfterimageSilhouette(anchor + new Vector2(direction * 20f, 0f), flip, pose, intensity);
			if (effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz)
			{
				DrawDuelistAfterimageSilhouette(anchor + new Vector2(direction * 39f, -4f), flip, pose, (effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz) ? (intensity * 0.34f) : (intensity * 0.55f));
			}
		}
	}

	private void DrawDuelistAfterimageSilhouette(Vector2 anchor, bool flip, Pose pose, float alpha)
	{
		Color body = ScaleAlpha(new Color(105, 132, 142, 155), alpha);
		Color blade = ScaleAlpha(new Color(194, 216, 218, 190), alpha);
		DrawBox(anchor, pose.Lean - 11f, -50f + pose.Crouch, 22f, 31f, body, flip);
		DrawBox(anchor, pose.Lean - 7f, -65f + pose.Crouch, 15f, 16f, body, flip);
		DrawLine(anchor, new Vector2(-8f, -19f), new Vector2(-12f, -1f), 5f, body, flip);
		DrawLine(anchor, new Vector2(8f, -19f), new Vector2(13f, -1f), 5f, body, flip);
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 mainDirection = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		float offAngle = pose.OffSwordAngle;
		Vector2 offDirection = new Vector2(MathF.Cos(offAngle), MathF.Sin(offAngle));
		DrawLine(anchor, hand, hand + mainDirection * pose.SwordLength, 2f, blade, flip);
		Vector2 offHand = new Vector2(pose.OffHandX, pose.OffHandY);
		DrawLine(anchor, offHand, offHand + offDirection * pose.OffSwordLength, 2f, blade, flip);
	}

	private void DrawRangerAfterimages(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.HunterBow || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		bool flag = player.Combat.TechniqueStageIndex == 0;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = (((uint)(effect - 31) <= 2u || effect == WeaponTechniqueEffect.RangerBackstepShot || effect == WeaponTechniqueEffect.RangerPhantomHunt) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			float direction = player.Combat.RangerWindrunnerDirection;
			if (MathF.Abs(direction) < 0.1f)
			{
				direction = (flip ? 1f : (-1f));
			}
			Vector2 echo = anchor - new Vector2(direction * 18f, 0f);
			Color body = new Color(57, 137, 163, 72);
			Color bow = new Color(129, 224, 239, 92);
			DrawBox(echo, pose.Lean - 10f, -49f + pose.Crouch, 20f, 30f, body, flip);
			DrawBox(echo, pose.Lean - 7f, -64f + pose.Crouch, 14f, 15f, body, flip);
			DrawLine(echo, new Vector2(12f, -52f), new Vector2(21f, -39f), 3f, bow, flip);
			DrawLine(echo, new Vector2(21f, -39f), new Vector2(12f, -25f), 3f, bow, flip);
		}
	}

	private void ApplyKnightTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (!player.IsKnightSwordAndShield || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		string techniqueId = player.Combat.CurrentTechnique.Id;
		int stage = player.Combat.TechniqueStageIndex;
		AttackDefinition attack = player.Combat.CurrentAttack;
		float elapsed = player.Combat.StateElapsed;
		float anticipate = KnightPhase(elapsed, 0f, attack?.StartupTime ?? 0.01f);
		float strike = KnightPhase(elapsed, attack?.StartupTime ?? 0f, attack?.ActiveTime ?? 0.01f);
		float recover = KnightPhase(elapsed,
			(attack?.StartupTime ?? 0f) + (attack?.ActiveTime ?? 0f),
			attack?.RecoveryTime ?? 0.01f);
		switch (techniqueId)
		{
		case "knights-combo":
			ApplyKnightComboPose(ref pose, stage, anticipate, strike, recover);
			break;
		case "shield-guard":
			pose.Crouch = player.Combat.IsPerfectGuardFeedbackActive ? 6f : 8f;
			pose.Lean = player.Combat.IsPerfectGuardFeedbackActive ? 0f :
				player.Combat.IsBlockFeedbackActive ? -5f : -3f;
			pose.Step = player.Combat.IsPerfectGuardFeedbackActive ? 10f : 12f;
			pose.ShieldX = player.Combat.IsPerfectGuardFeedbackActive ? 15f :
				player.Combat.IsBlockFeedbackActive ? 10f : 13f;
			pose.ShieldY = player.Combat.IsPerfectGuardFeedbackActive ? -54f : -52f;
			pose.SwordHandX = -9f;
			pose.SwordHandY = -35f;
			pose.SwordAngle = -1.86f;
			pose.CloakTail = -2f;
			break;
		case "shield-bash":
			pose.Crouch = KnightMotion(8f, 10f, 6f, 7f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(-3f, -5f, 9f, 1f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(9f, 11f, 14f, 8f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(11f, 14f, 23f, 8f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-51f, -53f, -46f, -50f,
				anticipate, strike, recover);
			pose.SwordHandX = -11f;
			pose.SwordHandY = -36f;
			pose.SwordAngle = -1.96f;
			pose.CloakTail = -3f;
			break;
		case "vanguard-slash":
			pose.Crouch = KnightMotion(7f, 9f, 5f, 6f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(3f, 5f, 10f, 0f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 11f, 14f, 8f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(4f, 2f, 5f, 9f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-47f, -44f, -45f, -50f,
				anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(-2f, -7f, 18f, -3f,
				anticipate, strike, recover);
			pose.SwordHandY = KnightMotion(-36f, -39f, -34f, -35f,
				anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-1.22f, -1.70f, 0.16f, -1.34f,
				anticipate, strike, recover);
			pose.CloakTail = KnightMotion(-6f, -10f, -13f, -3f,
				anticipate, strike, recover);
			break;
		case "guard-riposte":
			pose.Crouch = KnightMotion(7f, 8f, 3f, 6f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(-2f, -4f, 9f, 0f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(10f, 11f, 13f, 8f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(15f, 16f, 5f, 10f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-54f, -54f, -46f, -50f,
				anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(-7f, 5f, 23f, -3f,
				anticipate, strike, recover);
			pose.SwordHandY = -35f;
			pose.SwordAngle = KnightMotion(-1.86f, -0.34f, 0.02f, -1.34f,
				anticipate, strike, recover);
			break;
		case "guard-counter":
			ApplyKnightGuardCounterPose(ref pose, stage, anticipate, strike, recover);
			break;
		case "blade-breaker":
			ApplyKnightBladeBreakerPose(ref pose, stage, anticipate, strike, recover);
			break;
		case "shield-wall":
			ApplyKnightShieldWallPose(ref pose, stage, anticipate, strike, recover);
			break;
		case "falling-shield":
			if (!player.IsGrounded)
			{
				pose.Crouch = 13f;
				pose.Lean = 2f;
				pose.Step = 7f;
				pose.LeftFootLift = 9f;
				pose.RightFootLift = 5f;
				pose.ShieldX = 13f;
				pose.ShieldY = -24f;
				pose.SwordHandX = -12f;
				pose.SwordHandY = -48f;
				pose.SwordAngle = -2.38f;
				pose.CloakTail = -10f;
			}
			else
			{
				pose.Crouch = KnightMotion(13f, 14f, 10f, 6f,
					anticipate, strike, recover);
				pose.Lean = KnightMotion(2f, 3f, -1f, -2f,
					anticipate, strike, recover);
				pose.Step = KnightMotion(7f, 10f, 12f, 8f,
					anticipate, strike, recover);
				pose.ShieldX = KnightMotion(13f, 16f, 18f, 9f,
					anticipate, strike, recover);
				pose.ShieldY = KnightMotion(-24f, -20f, -17f, -50f,
					anticipate, strike, recover);
				pose.SwordHandX = KnightMotion(-12f, -13f, -8f, -4f,
					anticipate, strike, recover);
				pose.SwordHandY = KnightMotion(-48f, -49f, -42f, -35f,
					anticipate, strike, recover);
				pose.SwordAngle = KnightMotion(-2.38f, -2.40f, -1.92f, -1.36f,
					anticipate, strike, recover);
			}
			break;
		case "knights-final-oath":
			ApplyKnightFinalOathPose(ref pose, stage, anticipate, strike, recover,
				player.IsGrounded);
			break;
		}
	}

	private static void ApplyKnightFinalOathPose(ref Pose pose, int stage,
		float anticipate, float strike, float recover, bool grounded)
	{
		pose.SwordLength = 35f;
		pose.CloakTail = -4f;
		switch (stage)
		{
		case 0:
			pose.Crouch = KnightMotion(7f, 12f, 11f, 10f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-2f, -6f, -5f, -4f, anticipate, strike, recover);
			pose.Step = KnightMotion(10f, 15f, 16f, 15f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(12f, 18f, 20f, 19f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-52f, -56f, -54f, -54f, anticipate, strike, recover);
			pose.SwordHandX = -11f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = KnightMotion(-1.82f, -2.02f, -1.94f, -1.90f,
				anticipate, strike, recover);
			break;
		case 1:
			pose.Crouch = KnightMotion(10f, 12f, 7f, 7f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-4f, -1f, 12f, 5f, anticipate, strike, recover);
			pose.Step = KnightMotion(15f, 17f, 18f, 12f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(19f, 21f, 28f, 13f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-54f, -52f, -46f, -49f, anticipate, strike, recover);
			pose.SwordHandX = -12f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = -1.96f;
			break;
		case 2:
			pose.Crouch = KnightMotion(7f, 8f, 4f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(6f, 1f, 8f, 4f, anticipate, strike, recover);
			pose.Step = KnightMotion(12f, 9f, 13f, 9f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(13f, 9f, 5f, 8f, anticipate, strike, recover);
			pose.ShieldY = -49f;
			pose.SwordAngle = KnightMotion(-1.72f, -1.24f, 0.40f, 0.28f,
				anticipate, strike, recover);
			break;
		case 3:
			pose.Crouch = KnightMotion(6f, 7f, 4f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(4f, -1f, 6f, 2f, anticipate, strike, recover);
			pose.Step = KnightMotion(9f, 7f, -9f, -5f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(8f, 6f, 3f, 7f, anticipate, strike, recover);
			pose.ShieldY = -48f;
			pose.SwordAngle = KnightMotion(0.28f, 0.72f, -1.12f, -0.84f,
				anticipate, strike, recover);
			break;
		case 4:
			pose.Crouch = KnightMotion(6f, 8f, 3f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(2f, -3f, 9f, 5f, anticipate, strike, recover);
			pose.Step = KnightMotion(-5f, 5f, 14f, 9f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(7f, 9f, 5f, 9f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-48f, -50f, -51f, -50f, anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(10f, 8f, 20f, 15f,
				anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-0.84f, -0.34f, 0.02f, -0.18f,
				anticipate, strike, recover);
			break;
		case 5:
			pose.Crouch = KnightMotion(8f, 12f, grounded ? 4f : 1f, 1f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(4f, -2f, 3f, 1f, anticipate, strike, recover);
			pose.Step = KnightMotion(7f, 10f, 4f, 2f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(9f, 7f, 5f, 6f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-50f, -47f, -43f, -44f, anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(15f, -3f, -6f, -5f,
				anticipate, strike, recover);
			pose.SwordHandY = KnightMotion(-34f, -56f, -58f, -57f,
				anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-0.18f, -1.46f, -1.58f, -1.52f,
				anticipate, strike, recover);
			break;
		case 6:
			pose.Crouch = KnightMotion(-2f, -3f, 2f, 4f, anticipate, strike, recover);
			pose.Lean = KnightMotion(1f, -2f, 10f, 7f, anticipate, strike, recover);
			pose.Step = KnightMotion(2f, 3f, 10f, 8f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(6f, 4f, 8f, 11f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-44f, -42f, -47f, -51f, anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(-5f, -7f, 14f, 10f,
				anticipate, strike, recover);
			pose.SwordHandY = KnightMotion(-57f, -60f, -31f, -28f,
				anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-1.52f, -1.66f, 1.10f, 0.86f,
				anticipate, strike, recover);
			break;
		case 7:
			pose.Crouch = KnightMotion(6f, 13f, 14f, 9f, anticipate, strike, recover);
			pose.Lean = KnightMotion(7f, 4f, 1f, -1f, anticipate, strike, recover);
			pose.Step = KnightMotion(10f, 15f, 16f, 14f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(11f, 17f, 22f, 19f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-51f, -55f, -56f, -54f, anticipate, strike, recover);
			pose.SwordHandX = 6f;
			pose.SwordHandY = -29f;
			pose.SwordAngle = KnightMotion(0.86f, 1.02f, 0.74f, 0.54f,
				anticipate, strike, recover);
			break;
		default:
			pose.Crouch = KnightMotion(9f, 8f, 7f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-3f, -4f, -2f, -2f, anticipate, strike, recover);
			pose.Step = 13f;
			pose.ShieldX = KnightMotion(19f, 21f, 19f, 15f, anticipate, strike, recover);
			pose.ShieldY = -54f;
			pose.SwordHandX = -10f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = KnightMotion(-1.70f, -1.78f, -1.66f, -1.62f,
				anticipate, strike, recover);
			break;
		}
	}

	private static void ApplyKnightGuardCounterPose(ref Pose pose, int stage,
		float anticipate, float strike, float recover)
	{
		pose.CloakTail = -4f;
		if (stage == 0)
		{
			pose.Crouch = KnightMotion(8f, 10f, 6f, 7f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-3f, -5f, 8f, 2f, anticipate, strike, recover);
			pose.Step = KnightMotion(10f, 12f, 14f, 9f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(12f, 15f, 22f, 9f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-52f, -54f, -47f, -50f, anticipate, strike, recover);
			pose.SwordHandX = -10f;
			pose.SwordAngle = -1.92f;
			return;
		}
		if (stage == 1)
		{
			pose.Crouch = KnightMotion(7f, 6f, 4f, 5f, anticipate, strike, recover);
			pose.Lean = KnightMotion(2f, -3f, 8f, 2f, anticipate, strike, recover);
			pose.Step = KnightMotion(9f, 7f, 11f, 7f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(9f, 6f, 3f, 6f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-50f, -47f, -45f, -48f, anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-1.92f, -1.24f, 0.28f, 0.20f, anticipate, strike, recover);
			return;
		}
		pose.Crouch = KnightMotion(5f, 6f, 4f, 6f, anticipate, strike, recover);
		pose.Lean = KnightMotion(2f, -2f, 7f, 0f, anticipate, strike, recover);
		pose.Step = KnightMotion(7f, 6f, -7f, 8f, anticipate, strike, recover);
		pose.ShieldX = KnightMotion(6f, 4f, 2f, 9f, anticipate, strike, recover);
		pose.ShieldY = KnightMotion(-48f, -46f, -44f, -50f, anticipate, strike, recover);
		pose.SwordAngle = KnightMotion(0.20f, 0.68f, -1.04f, -1.34f, anticipate, strike, recover);
	}

	private static void ApplyKnightShieldWallPose(ref Pose pose, int stage,
		float anticipate, float strike, float recover)
	{
		pose.SwordHandX = -10f;
		pose.SwordHandY = -36f;
		pose.SwordAngle = -1.92f;
		pose.CloakTail = -2f;
		if (stage == 0)
		{
			pose.Crouch = KnightMotion(8f, 12f, 12f, 11f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-3f, -6f, -5f, -4f, anticipate, strike, recover);
			pose.Step = KnightMotion(12f, 15f, 16f, 15f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(13f, 17f, 19f, 18f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-52f, -55f, -53f, -53f, anticipate, strike, recover);
			return;
		}
		if (stage == 1)
		{
			pose.Crouch = KnightMotion(11f, 12f, 7f, 9f, anticipate, strike, recover);
			pose.Lean = KnightMotion(-4f, -6f, 10f, 3f, anticipate, strike, recover);
			pose.Step = KnightMotion(15f, 16f, 14f, 12f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(18f, 20f, 25f, 14f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-53f, -54f, -47f, -50f, anticipate, strike, recover);
			return;
		}
		pose.Crouch = KnightMotion(9f, 10f, 7f, 7f, anticipate, strike, recover);
		pose.Lean = KnightMotion(3f, 1f, 7f, -1f, anticipate, strike, recover);
		pose.Step = KnightMotion(12f, 13f, 11f, 9f, anticipate, strike, recover);
		pose.ShieldX = KnightMotion(14f, 16f, 21f, 11f, anticipate, strike, recover);
		pose.ShieldY = KnightMotion(-50f, -48f, -42f, -51f, anticipate, strike, recover);
	}

	private static void ApplyKnightBladeBreakerPose(ref Pose pose, int stage,
		float anticipate, float strike, float recover)
	{
		pose.CloakTail = -4f;
		if (stage == 0)
		{
			pose.Crouch = KnightMotion(5f, 7f, 4f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(0f, -4f, 8f, 2f, anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 7f, 12f, 8f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(8f, 10f, 4f, 7f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-49f, -50f, -46f, -48f, anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(-1.34f, -1.58f, 0.48f, 0.34f, anticipate, strike, recover);
			return;
		}
		if (stage == 1)
		{
			pose.Crouch = KnightMotion(6f, 8f, 5f, 6f, anticipate, strike, recover);
			pose.Lean = KnightMotion(2f, 0f, 9f, 3f, anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 10f, 13f, 8f, anticipate, strike, recover);
			pose.ShieldX = KnightMotion(7f, 13f, 22f, 6f, anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-48f, -51f, -46f, -48f, anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(12f, -8f, -10f, 12f,
				anticipate, strike, recover);
			pose.SwordAngle = KnightMotion(.34f, -1.88f, -1.94f, .34f,
				anticipate, strike, recover);
			return;
		}
		pose.SwordLength = 35f;
		pose.Crouch = KnightMotion(6f, 7f, 4f, 6f, anticipate, strike, recover);
		pose.Lean = KnightMotion(3f, -2f, 10f, 1f, anticipate, strike, recover);
		pose.Step = KnightMotion(8f, 7f, 14f, 8f, anticipate, strike, recover);
		pose.ShieldX = KnightMotion(6f, 5f, 3f, 9f, anticipate, strike, recover);
		pose.ShieldY = KnightMotion(-48f, -47f, -50f, -50f, anticipate, strike, recover);
		pose.SwordHandX = KnightMotion(10f, 9f, 22f, 15f, anticipate, strike, recover);
		pose.SwordAngle = KnightMotion(0.34f, -0.24f, 0.01f, -1.34f, anticipate, strike, recover);
	}

	private static float KnightPhase(float elapsed, float start, float duration)
	{
		return MathHelper.SmoothStep(0f, 1f,
			Math.Clamp((elapsed - start) / MathF.Max(0.01f, duration), 0f, 1f));
	}

	private static float KnightMotion(float ready, float prepared, float released,
		float settled, float anticipate, float strike, float recover)
	{
		float value = MathHelper.Lerp(ready, prepared, anticipate);
		value = MathHelper.Lerp(value, released, strike);
		return MathHelper.Lerp(value, settled, recover);
	}

	private static void ApplyKnightComboPose(ref Pose pose, int stage,
		float anticipate, float strike, float recover)
	{
		pose.SwordLength = stage == 2 ? 35f : 33f;
		pose.CloakTail = -4f;
		switch (stage)
		{
		case 0:
			pose.SwordAngle = KnightMotion(-1.34f, -1.05f, 0.24f, 0.20f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(-2f, -4f, 5f, 0f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 7f, 11f, 8f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(7f, 9f, 12f, 7f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-50f, -51f, -48f, -50f,
				anticipate, strike, recover);
			pose.Crouch = KnightMotion(5f, 6f, 4f, 5f,
				anticipate, strike, recover);
			break;
		case 1:
			pose.SwordAngle = KnightMotion(0.20f, 0.58f, -1.02f, -0.82f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(0f, -3f, 4f, -1f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 6f, -7f, -4f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(7f, 5f, 3f, 5f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-50f, -47f, -45f, -48f,
				anticipate, strike, recover);
			pose.Crouch = KnightMotion(5f, 6f, 4f, 5f,
				anticipate, strike, recover);
			break;
		case 2:
			pose.SwordAngle = KnightMotion(-0.82f, -0.26f, 0.01f, -0.12f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(-1f, -3f, 9f, 2f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(-4f, 4f, 14f, 8f,
				anticipate, strike, recover);
			pose.SwordHandX = KnightMotion(11f, 9f, 24f, 15f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(5f, 7f, 4f, 6f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-48f, -51f, -47f, -49f,
				anticipate, strike, recover);
			pose.Crouch = KnightMotion(5f, 7f, 3f, 5f,
				anticipate, strike, recover);
			break;
		default:
			pose.SwordAngle = KnightMotion(-0.12f, -1.62f, 0.62f, -1.34f,
				anticipate, strike, recover);
			pose.Lean = KnightMotion(2f, -5f, 9f, -1f,
				anticipate, strike, recover);
			pose.Step = KnightMotion(8f, 6f, 13f, 8f,
				anticipate, strike, recover);
			pose.Crouch = KnightMotion(5f, 8f, 5f, 6f,
				anticipate, strike, recover);
			pose.ShieldX = KnightMotion(6f, 5f, 1f, 9f,
				anticipate, strike, recover);
			pose.ShieldY = KnightMotion(-49f, -47f, -41f, -50f,
				anticipate, strike, recover);
			pose.CloakTail = -6f;
			break;
		}
		if (stage != 2)
		{
			pose.SwordHandX = 10f + MathF.Max(0f, pose.Lean) * .65f;
			pose.SwordHandY = -36f;
		}
	}

	private float LerpByFrame(float start, float end)
	{
		float amount = Math.Clamp((float)_animation.Frame / 8f, 0f, 1f);
		return MathHelper.Lerp(start, end, amount);
	}

	private void ApplyEquipmentPose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		PlayerVisualState state = _animation.State;
		bool flag = (((uint)(state - 4) <= 4u || state == PlayerVisualState.HeavyAttack) ? true : false);
		bool isAttack = flag;
		switch (player.WeaponFamily)
		{
		case WeaponFamily.GreatSword:
			pose.SwordLength += 14f;
			pose.SwordHandX -= 3f;
			pose.SwordHandY += 2f;
			pose.Crouch += (isAttack ? 2f : 0f);
			if (!isAttack)
			{
				pose.SwordAngle = -0.92f;
			}
			ApplyFamilyAttackAngles(ref pose, new float[6] { -0.86f, -1.78f, -1.42f, -0.46f, 0.38f, 0.68f }, new float[6] { 0.62f, 0.92f, 0.48f, -0.42f, -1.18f, -1.46f }, new float[9] { -1.36f, -1.85f, -2.22f, -1.62f, -0.68f, 0.24f, 0.78f, 1.02f, 1.12f }, new float[6] { -0.72f, -1.72f, -2.42f, -1.64f, -0.28f, 0.88f });
			break;
		case WeaponFamily.DualSwords:
			ApplyDuelistHoldingPose(player, state, isAttack, ref pose);
			break;
		case WeaponFamily.HunterBow:
			pose.SwordLength = 43f;
			pose.SwordHandX = 21f;
			pose.SwordHandY = -39f;
			pose.SwordAngle = -1.57f;
			pose.Crouch += (isAttack ? 1f : 0f);
			break;
		case WeaponFamily.ArcaneWarStaff:
			pose.SwordLength += 22f;
			pose.SwordHandX = 15f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = (isAttack ? (-1.05f) : (-1.3f));
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
		case WeaponFamily.Scythe:
			pose.SwordLength = 58f;
			pose.SwordHandX = 13f;
			pose.SwordHandY = -35f;
			pose.OffHandX = -1f;
			pose.OffHandY = -29f;
			pose.FrontElbowX = 22f;
			pose.FrontElbowY = -38f;
			pose.RearElbowX = -10f;
			pose.RearElbowY = -34f;
			if (!isAttack)
			{
				pose.SwordAngle = -0.96f;
			}
			break;
		}
	}

	private void ApplyDuelistHoldingPose(DungeonAscendant.Player.Player player, PlayerVisualState state, bool isAttack, ref Pose pose)
	{
		pose.SwordLength = 31f;
		pose.OffSwordLength = 28f;
		pose.UseMartialLimbs = true;

		if (isAttack)
		{
			pose.SwordHandX = 20f;
			pose.SwordHandY = -31f;
			pose.OffHandX = -7f;
			pose.OffHandY = -29f;
			pose.OffSwordAngle = -1.72f;
			pose.Crouch += 5f;
			pose.Lean += 3f;
			pose.FrontElbowX = 15f;
			pose.FrontElbowY = -39f;
			pose.RearElbowX = -14f;
			pose.RearElbowY = -39f;
			ApplyFamilyAttackAngles(ref pose, new float[6] { -0.62f, -1.18f, -0.52f, 0.08f, 0.48f, 0.3f }, new float[6] { 0.55f, 0.92f, 0.35f, -0.18f, -0.72f, -0.55f }, new float[9] { -0.92f, -0.45f, 0.18f, 0.72f, 0.38f, -0.32f, -0.7f, -0.22f, 0.12f }, new float[6] { -0.72f, -1.02f, -0.35f, 0.32f, 0.82f, 0.45f }, new float[6] { 0.85f, 0.55f, -0.05f, -0.68f, -0.32f, 0.12f }, new float[9] { -1.05f, -0.6f, 0.05f, 0.72f, 0.35f, -0.38f, -0.82f, -0.25f, 0.18f });
			return;
		}

		float combatReady = player.Combat.Resources.DuelistMomentum > 0f ? 1f : 0f;
		float breath = MathF.Sin(_visualTimeSeconds * 2.05f);
		float bladeSway = MathF.Sin(_visualTimeSeconds * 1.35f + 0.7f);
		if (state == PlayerVisualState.Idle)
		{
			pose.Crouch += 5f + combatReady;
			pose.Lean += 3f + combatReady;
			pose.Step += 5f + combatReady;
			pose.Bob += breath * 0.35f;
			pose.SwordHandX = 21f + combatReady * 2f + bladeSway * 0.35f;
			pose.SwordHandY = -33f + breath * 0.45f;
			pose.SwordAngle = -0.74f - combatReady * 0.07f + bladeSway * 0.018f;
			pose.OffHandX = -3f + combatReady;
			pose.OffHandY = -43f - combatReady + breath * 0.3f;
			pose.OffSwordAngle = -1.86f - combatReady * 0.08f - bladeSway * 0.014f;
			pose.FrontElbowX = 13f + combatReady;
			pose.FrontElbowY = -39f + breath * 0.2f;
			pose.RearElbowX = -14f;
			pose.RearElbowY = -47f - combatReady * 0.5f;
			return;
		}

		if (state == PlayerVisualState.Run)
		{
			float counterMotion = Math.Clamp(pose.Step / 6f, -1f, 1f);
			pose.Crouch += 3f;
			pose.Lean += 5f;
			pose.SwordHandX = 8f - counterMotion * 1.5f;
			pose.SwordHandY = -31f + MathF.Abs(counterMotion);
			pose.SwordAngle = -2.46f + counterMotion * 0.05f;
			pose.OffHandX = -9f + counterMotion * 1.2f;
			pose.OffHandY = -40f - MathF.Abs(counterMotion) * 0.6f;
			pose.OffSwordAngle = -2.92f - counterMotion * 0.04f;
			pose.FrontElbowX = 4f - counterMotion * 2f;
			pose.FrontElbowY = -38f;
			pose.RearElbowX = -15f + counterMotion * 2f;
			pose.RearElbowY = -44f;
			return;
		}

		if (state is PlayerVisualState.Jump or PlayerVisualState.Fall)
		{
			pose.Crouch += 3f;
			pose.Lean += 3f;
			pose.SwordHandX = 15f;
			pose.SwordHandY = -34f;
			pose.SwordAngle = -1.02f;
			pose.OffHandX = -5f;
			pose.OffHandY = -41f;
			pose.OffSwordAngle = -2.18f;
			pose.FrontElbowX = 9f;
			pose.FrontElbowY = -40f;
			pose.RearElbowX = -13f;
			pose.RearElbowY = -44f;
			return;
		}

		pose.SwordHandX = 18f;
		pose.SwordHandY = -33f;
		pose.OffHandX = -5f;
		pose.OffHandY = -38f;
		pose.OffSwordAngle = -1.78f;
		pose.FrontElbowX = 12f;
		pose.FrontElbowY = -40f;
		pose.RearElbowX = -13f;
		pose.RearElbowY = -43f;
	}

	private void ApplySovereignTechniquePose(DungeonAscendant.Player.Player player, ref Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.AbyssRing)
		{
			return;
		}
		ConfigureSovereignStance(ref pose);
		int stage = player.Combat.TechniqueStageIndex;
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		if (player.Combat.CurrentAttack != null)
		{
			GetSovereignMotion(player.Combat.CurrentAttack, player.Combat.StateElapsed, out var drive, out var contact, out var recovery);
			switch (effect)
			{
			case WeaponTechniqueEffect.SovereignMartialArts:
				ApplySovereignComboPose(ref pose, stage, drive, contact, recovery);
				break;
			case WeaponTechniqueEffect.SovereignOmnipresentStep:
				ApplyOmnipresentStepPose(player, ref pose, drive, recovery);
				break;
			case WeaponTechniqueEffect.SovereignParadoxImpact:
				ApplyParadoxImpactPose(ref pose, stage, drive, contact, recovery);
				break;
			case WeaponTechniqueEffect.SovereignInfiniteRiftTheatre:
				ApplyRiftTheatrePose(ref pose, stage, drive, contact, recovery);
				break;
			case WeaponTechniqueEffect.SovereignHeavenReversal:
				ApplyHeavenReversalPose(ref pose, drive, contact, recovery);
				break;
			case WeaponTechniqueEffect.SovereignZeroDistanceStrike:
				ApplyZeroDistancePose(ref pose, stage, drive, contact, recovery);
				break;
			case WeaponTechniqueEffect.SovereignGoldenCataclysm:
				ApplyGoldenCataclysmPose(ref pose, stage, drive, contact, recovery);
				break;
			}
			if (player.Combat.SovereignPerfectRiftVisualRatio > 0f)
			{
				ApplyPerfectRiftCounterPose(ref pose, 1f - player.Combat.SovereignPerfectRiftVisualRatio);
			}
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

	private static void GetSovereignMotion(AttackDefinition attack, float elapsed, out float drive, out float contact, out float recovery)
	{
		float startup = Math.Max(0.01f, attack.StartupTime);
		float activeEnd = attack.StartupTime + attack.ActiveTime;
		float startupRatio = Math.Clamp(elapsed / startup, 0f, 1f);
		recovery = Math.Clamp((elapsed - activeEnd) / Math.Max(0.01f, attack.RecoveryTime), 0f, 1f);
		drive = MathHelper.SmoothStep(0f, 1f, startupRatio);
		if (elapsed >= attack.StartupTime)
		{
			drive = 1f;
		}
		drive = MathHelper.Lerp(drive, 0.18f, recovery);
		float activeRatio = Math.Clamp((elapsed - attack.StartupTime) / Math.Max(0.01f, attack.ActiveTime), 0f, 1f);
		contact = ((elapsed < attack.StartupTime) ? 0f : (1f - Math.Clamp(MathF.Abs(activeRatio - 0.32f) * 2.1f, 0f, 1f)));
	}

	private static void ApplySovereignComboPose(ref Pose pose, int stage, float drive, float contact, float recovery)
	{
		switch (stage)
		{
		case 0:
			pose.Crouch = MathHelper.Lerp(5f, 2f, drive);
			pose.Lean = MathHelper.Lerp(-2f, 9f, drive);
			pose.Step = MathHelper.Lerp(-2f, 8f, drive);
			SetFrontArm(ref pose, drive, 7f, -40f, 3f, -45f, 43f, -39f, 25f, -43f);
			SetGuardArm(ref pose, -5f, -39f, -17f, -43f);
			pose.FrontFootX = MathHelper.Lerp(11f, 21f, drive);
			break;
		case 1:
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
		case 2:
			pose.Crouch = MathHelper.Lerp(7f, 3f, drive);
			pose.Lean = MathHelper.Lerp(-7f, 10f, drive);
			pose.Step = MathHelper.Lerp(-4f, 7f, drive);
			SetFrontArm(ref pose, drive, 1f, -37f, -7f, -42f, 46f, -35f, 24f, -41f);
			SetGuardArm(ref pose, -2f, -42f, -15f, -45f);
			break;
		case 3:
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
		case 4:
			pose.Crouch = MathHelper.Lerp(13f, 1f, drive);
			pose.Lean = MathHelper.Lerp(-6f, 8f, drive);
			pose.Step = MathHelper.Lerp(-5f, 5f, drive);
			SetFrontArm(ref pose, drive, 3f, -20f, 8f, -29f, 25f, -63f, 17f, -45f);
			SetGuardArm(ref pose, -5f, -39f, -17f, -43f);
			pose.FrontFootX = 17f;
			pose.RearFootX = -18f;
			break;
		case 5:
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
		case 6:
			pose.Crouch = MathHelper.Lerp(8f, 4f, drive);
			pose.Lean = MathHelper.Lerp(-5f, 7f, drive);
			pose.Step = 6f;
			pose.SwordHandX = MathHelper.Lerp(1f, 43f, drive);
			pose.SwordHandY = MathHelper.Lerp(-42f, -35f, drive);
			pose.FrontElbowX = MathHelper.Lerp(9f, 25f, drive);
			pose.FrontElbowY = -42f;
			pose.OffHandX = MathHelper.Lerp(-2f, 36f, Math.Clamp(drive + contact * 0.22f, 0f, 1f));
			pose.OffHandY = MathHelper.Lerp(-34f, -47f, drive);
			pose.RearElbowX = MathHelper.Lerp(-12f, 19f, drive);
			pose.RearElbowY = MathHelper.Lerp(-46f, -49f, drive);
			break;
		default:
			pose.Crouch = MathHelper.Lerp(10f, 3f, drive);
			pose.Lean = MathHelper.Lerp(-12f, 16f, drive);
			pose.Step = MathHelper.Lerp(-9f, 12f, drive);
			SetFrontArm(ref pose, drive, -12f, -29f, -8f, -38f, 55f, -35f, 29f, -41f);
			SetGuardArm(ref pose, -8f, -45f, -19f, -46f);
			pose.FrontFootX = MathHelper.Lerp(12f, 25f, drive);
			pose.RearFootX = MathHelper.Lerp(-16f, -26f, drive);
			pose.CloakTail = MathHelper.Lerp(-2f, -13f, drive);
			break;
		}
		if (recovery > 0f)
		{
			pose.Crouch += recovery * ((stage == 7) ? 5f : 2f);
			pose.Lean -= recovery * ((stage == 7) ? 5f : 2f);
		}
	}

	private static void ApplyOmnipresentStepPose(DungeonAscendant.Player.Player player, ref Pose pose, float drive, float recovery)
	{
		float vertical = Math.Clamp(player.Combat.SovereignTransitDirection.Y, -1f, 1f);
		pose.Crouch = MathHelper.Lerp(10f, 5f, drive) + recovery * 3f;
		pose.Lean = MathHelper.Lerp(-3f, 12f, drive);
		pose.Step = MathHelper.Lerp(-7f, 11f, drive);
		pose.FrontKneeY -= Math.Max(0f, 0f - vertical) * 10f * drive;
		pose.RearKneeY -= Math.Max(0f, vertical) * 8f * drive;
		pose.FrontFootX += 8f * drive;
		pose.RearFootX -= 7f * (1f - drive);
		pose.SwordHandX = MathHelper.Lerp(5f, 28f, drive);
		pose.SwordHandY = -43f + vertical * 8f;
		pose.FrontElbowX = MathHelper.Lerp(7f, 22f, drive);
		pose.FrontElbowY = -46f + vertical * 5f;
		SetGuardArm(ref pose, -5f, -40f, -16f, -44f);
	}

	private static void ApplyPerfectRiftCounterPose(ref Pose pose, float progress)
	{
		float counter = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.28f) / 0.42f, 0f, 1f));
		float settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - 0.72f) / 0.28f, 0f, 1f));
		counter = MathHelper.Lerp(counter, 0.22f, settle);
		pose.Crouch = MathHelper.Lerp(10f, 4f, counter);
		pose.Lean = MathHelper.Lerp(-7f, 11f, counter);
		pose.Step = MathHelper.Lerp(-6f, 7f, counter);
		SetFrontArm(ref pose, counter, 3f, -47f, 16f, -49f, 46f, -37f, 25f, -42f);
		pose.OffHandX = MathHelper.Lerp(9f, -4f, counter);
		pose.OffHandY = MathHelper.Lerp(-34f, -43f, counter);
		pose.RearElbowX = MathHelper.Lerp(-6f, -17f, counter);
		pose.RearElbowY = MathHelper.Lerp(-38f, -46f, counter);
		pose.FrontFootX = MathHelper.Lerp(12f, 21f, counter);
		pose.RearFootX = MathHelper.Lerp(-12f, -19f, counter);
	}

	private static void ApplyParadoxImpactPose(ref Pose pose, int stage, float drive, float contact, float recovery)
	{
		if (stage == 0)
		{
			pose.Crouch = 6f;
			pose.Lean = MathHelper.Lerp(-4f, 7f, drive);
			SetFrontArm(ref pose, drive, 2f, -39f, 2f, -45f, 31f, -38f, 23f, -43f);
			pose.SwordHandX -= contact * 9f;
		}
		else if (stage >= 1 && stage <= 3)
		{
			bool reverse = stage % 2 == 0;
			pose.Crouch = 5f + recovery * 2f;
			pose.Lean = MathHelper.Lerp(reverse ? 5f : (-5f), reverse ? (-7f) : 10f, drive);
			pose.Step = (reverse ? (-5f) : 7f);
			if (!reverse)
			{
				SetFrontArm(ref pose, drive, 1f, -36f, -4f, -42f, 45f, -37f, 27f, -42f);
				SetGuardArm(ref pose, -3f, -43f, -16f, -46f);
				return;
			}
			pose.SwordHandX = 2f;
			pose.SwordHandY = -44f;
			pose.FrontElbowX = 17f;
			pose.FrontElbowY = -47f;
			pose.OffHandX = MathHelper.Lerp(-7f, 42f, drive);
			pose.OffHandY = MathHelper.Lerp(-35f, -38f, drive);
			pose.RearElbowX = MathHelper.Lerp(-18f, 21f, drive);
			pose.RearElbowY = -43f;
		}
		else if (stage == 4)
		{
			pose.Crouch = 10f;
			pose.Lean = 13f;
			pose.Step = 12f;
			pose.FrontFootX = 23f;
			pose.RearFootX = -22f;
			pose.CloakTail = -14f;
		}
		else
		{
			pose.Crouch = MathHelper.Lerp(7f, 3f, drive);
			pose.Lean = MathHelper.Lerp(8f, -10f, drive);
			pose.Step = -8f;
			pose.FrontKneeX = MathHelper.Lerp(10f, 25f, drive);
			pose.FrontKneeY = MathHelper.Lerp(-10f, -31f, drive);
			pose.FrontFootX = MathHelper.Lerp(13f, 51f, drive);
			pose.FrontFootY = MathHelper.Lerp(-1f, -24f, drive);
			pose.CloakTail = -11f;
		}
	}

	private static void ApplyRiftTheatrePose(ref Pose pose, int stage, float drive, float contact, float recovery)
	{
		switch (stage)
		{
		case 0:
			pose.Crouch = 5f;
			pose.Lean = MathHelper.Lerp(-5f, 11f, drive);
			pose.Step = 8f;
			SetFrontArm(ref pose, drive, 2f, -31f, -4f, -39f, 43f, -49f, 24f, -44f);
			break;
		case 1:
			pose.Crouch = 3f;
			pose.Lean = MathHelper.Lerp(5f, -8f, drive);
			pose.FrontKneeX = MathHelper.Lerp(11f, 27f, drive);
			pose.FrontKneeY = MathHelper.Lerp(-11f, -28f, drive);
			pose.FrontFootX = MathHelper.Lerp(14f, 55f, drive);
			pose.FrontFootY = MathHelper.Lerp(-1f, -28f, drive);
			break;
		case 2:
			pose.Crouch = MathHelper.Lerp(11f, 1f, drive);
			pose.Lean = MathHelper.Lerp(-5f, 9f, drive);
			pose.SwordHandX = MathHelper.Lerp(2f, 20f, drive);
			pose.SwordHandY = MathHelper.Lerp(-28f, -57f, drive);
			pose.FrontElbowX = MathHelper.Lerp(-2f, 31f, drive);
			pose.FrontElbowY = MathHelper.Lerp(-34f, -53f, drive);
			break;
		case 3:
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
			SetFrontArm(ref pose, drive, -5f, -31f, -9f, -40f, 48f, -36f, 27f, -42f);
			break;
		}
		pose.CloakTail = -9f - drive * 5f;
	}

	private static void ApplyHeavenReversalPose(ref Pose pose, float drive, float contact, float recovery)
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

	private static void ApplyZeroDistancePose(ref Pose pose, int stage, float drive, float contact, float recovery)
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
		}
		else
		{
			pose.Crouch = MathHelper.Lerp(10f, 4f, drive) + recovery * 3f;
			pose.Lean = MathHelper.Lerp(8f, 16f, drive) - recovery * 4f;
			pose.Step = 12f;
			SetFrontArm(ref pose, drive, 2f, -29f, 4f, -38f, 54f, -35f, 29f, -41f);
			SetGuardArm(ref pose, -5f, -43f, -18f, -45f);
			pose.FrontFootX = 25f;
			pose.RearFootX = -23f;
			pose.CloakTail = -14f;
		}
	}

	private static void ApplyGoldenCataclysmPose(ref Pose pose, int stage, float drive, float contact, float recovery)
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
		}
		else if (stage <= 16)
		{
			int pattern = (stage - 1) % 6;
			float commitment = 0.82f + (float)stage / 16f * 0.18f;
			switch (pattern)
			{
			case 0:
				pose.Crouch = MathHelper.Lerp(8f, 3f, drive) + recovery * 3f;
				pose.Lean = MathHelper.Lerp(-8f, 14f, drive);
				pose.Step = MathHelper.Lerp(-6f, 10f, drive);
				SetFrontArm(ref pose, drive, 0f, -48f, -5f, -44f, 46f, -29f, 25f, -38f);
				SetGuardArm(ref pose, -4f, -42f, -17f, -45f);
				break;
			case 1:
				pose.Crouch = MathHelper.Lerp(6f, 1f, drive);
				pose.Lean = MathHelper.Lerp(9f, -12f, drive);
				pose.FrontKneeX = MathHelper.Lerp(7f, 25f, drive);
				pose.FrontKneeY = MathHelper.Lerp(-12f, -28f, drive);
				pose.FrontFootX = MathHelper.Lerp(12f, 54f, drive);
				pose.FrontFootY = MathHelper.Lerp(-2f, -30f, drive);
				pose.RearKneeX = -13f;
				pose.RearKneeY = -21f;
				pose.RearFootX = -19f;
				pose.RearFootY = -8f;
				pose.SwordHandX = 13f;
				pose.SwordHandY = -48f;
				pose.FrontElbowX = 22f;
				pose.FrontElbowY = -47f;
				break;
			case 2:
				pose.Crouch = MathHelper.Lerp(11f, 1f, drive);
				pose.Lean = MathHelper.Lerp(-5f, 10f, drive);
				pose.FrontKneeX = MathHelper.Lerp(8f, 19f, drive);
				pose.FrontKneeY = MathHelper.Lerp(-10f, -32f, drive);
				pose.FrontFootX = MathHelper.Lerp(13f, 15f, drive);
				pose.FrontFootY = MathHelper.Lerp(-1f, -21f, drive);
				pose.SwordHandX = MathHelper.Lerp(1f, 20f, drive);
				pose.SwordHandY = MathHelper.Lerp(-28f, -58f, drive);
				pose.FrontElbowX = MathHelper.Lerp(-3f, 32f, drive);
				pose.FrontElbowY = MathHelper.Lerp(-34f, -53f, drive);
				SetGuardArm(ref pose, -4f, -40f, -16f, -44f);
				break;
			case 3:
				pose.Crouch = 7f + contact * 2f;
				pose.Lean = MathHelper.Lerp(10f, -11f, drive);
				pose.Step = MathHelper.Lerp(8f, -7f, drive);
				SetFrontArm(ref pose, drive, 2f, -42f, 5f, -47f, 43f, -35f, 24f, -41f);
				pose.OffHandX = MathHelper.Lerp(-4f, 34f, Math.Clamp(drive + contact * 0.18f, 0f, 1f));
				pose.OffHandY = MathHelper.Lerp(-35f, -47f, drive);
				pose.RearElbowX = MathHelper.Lerp(-17f, 18f, drive);
				pose.RearElbowY = -47f;
				break;
			case 4:
				pose.Crouch = MathHelper.Lerp(2f, 7f, drive);
				pose.Lean = MathHelper.Lerp(-9f, 8f, drive);
				pose.FrontKneeX = MathHelper.Lerp(5f, 23f, drive);
				pose.FrontKneeY = MathHelper.Lerp(-25f, -10f, drive);
				pose.FrontFootX = MathHelper.Lerp(4f, 38f, drive);
				pose.FrontFootY = MathHelper.Lerp(-49f, -3f, drive);
				SetFrontArm(ref pose, drive, 5f, -45f, 16f, -47f, 31f, -35f, 19f, -42f);
				SetGuardArm(ref pose, -9f, -39f, -20f, -43f);
				break;
			default:
				pose.Crouch = MathHelper.Lerp(8f, 3f, drive);
				pose.Lean = MathHelper.Lerp(12f, -8f, drive);
				pose.Step = MathHelper.Lerp(9f, -10f, drive);
				pose.SwordHandX = MathHelper.Lerp(12f, -30f, drive);
				pose.SwordHandY = MathHelper.Lerp(-45f, -36f, drive);
				pose.FrontElbowX = MathHelper.Lerp(20f, -12f, drive);
				pose.FrontElbowY = -43f;
				pose.OffHandX = MathHelper.Lerp(-7f, 39f, drive);
				pose.OffHandY = MathHelper.Lerp(-38f, -42f, drive);
				pose.RearElbowX = MathHelper.Lerp(-18f, 20f, drive);
				pose.RearElbowY = -46f;
				break;
			}
			pose.Lean *= commitment;
			pose.CloakTail = MathHelper.Lerp(-8f, -15f, commitment);
		}
		else if (stage == 17)
		{
			float compression = 1f - drive;
			pose.Crouch = MathHelper.Lerp(15f, 0f, drive) + recovery * 6f;
			pose.Lean = MathHelper.Lerp(-13f, 17f, drive) - recovery * 5f;
			pose.Step = MathHelper.Lerp(-13f, 15f, drive);
			pose.SwordHandX = MathHelper.Lerp(-13f, 40f, drive);
			pose.SwordHandY = MathHelper.Lerp(-30f, -7f, drive);
			pose.FrontElbowX = MathHelper.Lerp(-8f, 28f, drive);
			pose.FrontElbowY = MathHelper.Lerp(-43f, -25f, drive);
			SetGuardArm(ref pose, -7f, -45f, -19f, -47f);
			pose.FrontKneeX = MathHelper.Lerp(17f, 25f, drive);
			pose.FrontKneeY = MathHelper.Lerp(-9f, -16f, drive);
			pose.FrontFootX = MathHelper.Lerp(31f, 36f, drive);
			pose.FrontFootY = MathHelper.Lerp(-2f, -13f, drive);
			pose.RearKneeX = MathHelper.Lerp(-5f, -18f, drive);
			pose.RearKneeY = MathHelper.Lerp(-8f, -22f, drive);
			pose.RearFootX = MathHelper.Lerp(-19f, -34f, drive);
			pose.RearFootY = MathHelper.Lerp(-1f, -13f, drive);
			pose.CloakTail = -17f - drive * 4f + compression * 2f;
		}
		else
		{
			pose.Crouch = MathHelper.Lerp(7f, 3f, recovery);
			pose.Lean = MathHelper.Lerp(8f, 1f, recovery);
			pose.Step = MathHelper.Lerp(7f, 2f, recovery);
			pose.SwordHandX = MathHelper.Lerp(31f, 17f, recovery);
			pose.SwordHandY = MathHelper.Lerp(-37f, -42f, recovery);
			pose.FrontElbowX = MathHelper.Lerp(24f, 16f, recovery);
			pose.FrontElbowY = -44f;
			SetGuardArm(ref pose, -4f, -40f, -16f, -44f);
		}
	}

	private static void SetFrontArm(ref Pose pose, float amount, float handStartX, float handStartY, float elbowStartX, float elbowStartY, float handEndX, float handEndY, float elbowEndX, float elbowEndY)
	{
		pose.SwordHandX = MathHelper.Lerp(handStartX, handEndX, amount);
		pose.SwordHandY = MathHelper.Lerp(handStartY, handEndY, amount);
		pose.FrontElbowX = MathHelper.Lerp(elbowStartX, elbowEndX, amount);
		pose.FrontElbowY = MathHelper.Lerp(elbowStartY, elbowEndY, amount);
	}

	private static void SetGuardArm(ref Pose pose, float handX, float handY, float elbowX, float elbowY)
	{
		pose.OffHandX = handX;
		pose.OffHandY = handY;
		pose.RearElbowX = elbowX;
		pose.RearElbowY = elbowY;
	}

	private void DrawSovereignTransformation(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily == WeaponFamily.AbyssRing)
		{
			float x = pose.Lean;
			float y = pose.Bob + pose.Crouch + GetHeightOffset(player.Appearance.ApparentHeight);
			Color obsidian = (player.IsHitFlashing ? new Color(72, 60, 42) : new Color(16, 17, 22));
			Color blackGold = new Color(77, 58, 25);
			Color gold = (player.Combat.IsSovereignThroneActive ? new Color(255, 209, 73) : new Color(220, 164, 48));
			if (player.Combat.IsSovereignRiftEvading)
			{
				float trail = (flip ? 18f : (-18f));
				DrawBox(anchor, x - 11f + trail, y - 47f, 22f, 37f, new Color(12, 10, 17, 105), flip);
				DrawLine(anchor, new Vector2(x - 6f + trail, y - 39f), new Vector2(x + 9f + trail, y - 27f), 2f, new Color(231, 178, 48, 125), flip);
			}
			DrawBox(anchor, x - 12f, y - 48f, 24f, 25f, obsidian, flip);
			DrawBox(anchor, x - 10f, y - 46f, 4f, 18f, blackGold, flip);
			DrawBox(anchor, x + 3f, y - 43f, 3f, 15f, gold, flip);
			DrawLine(anchor, new Vector2(x - 8f, y - 40f), new Vector2(x + 8f, y - 32f), 2f, gold, flip);
			DrawBox(anchor, x + 2f, y - 59f, 7f, 4f, obsidian, flip);
			DrawBox(anchor, x + 5f, y - 58f, 3f, 2f, gold, flip);
			Vector2 fist = new Vector2(x + pose.SwordHandX, y + pose.SwordHandY);
			DrawBox(anchor, fist.X - 5f, fist.Y - 5f, 11f, 10f, obsidian, flip);
			DrawBox(anchor, fist.X + 1f, fist.Y - 4f, 4f, 3f, gold, flip);
			DrawRectangleOutlineLocal(anchor, fist.X - 7f, fist.Y - 7f, 15f, 14f, 2f, gold, flip);
			if (player.Combat.IsSovereignThroneActive)
			{
				DrawRectangleOutlineLocal(anchor, x - 25f, y - 72f, 50f, 58f, 2f, new Color(208, 156, 45, 120), flip);
				DrawRectangleOutlineLocal(anchor, x - 19f, y - 66f, 38f, 46f, 1f, new Color(24, 21, 19, 180), flip);
			}
		}
	}

	private void DrawSovereignAfterimages(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.AbyssRing || player.Combat.CurrentTechnique == null)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		int stage = player.Combat.TechniqueStageIndex;
		bool flag = ((effect == WeaponTechniqueEffect.SovereignOmnipresentStep || (uint)(effect - 102) <= 1u) ? true : false);
		if (flag || (effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage >= 4) || (effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre && stage <= 4) || (effect == WeaponTechniqueEffect.SovereignGoldenCataclysm && stage >= 1 && stage <= 17))
		{
			Vector2 travel = player.Combat.SovereignTransitDirection;
			if (travel.LengthSquared() < 0.01f)
			{
				travel = new Vector2(flip ? (-1f) : 1f, 0f);
			}
			else
			{
				travel.Normalize();
			}
			float speed = effect switch
			{
				WeaponTechniqueEffect.SovereignInfiniteRiftTheatre => 1f,
				WeaponTechniqueEffect.SovereignGoldenCataclysm => MathHelper.Lerp(0.72f, 1.18f, Math.Clamp((float)stage / 16f, 0f, 1f)),
				_ => 0.78f,
			};
			DrawSovereignEcho(anchor - travel * (16f * speed), flip, pose, 0.34f);
			flag = ((effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre || effect == WeaponTechniqueEffect.SovereignHeavenReversal) ? true : false);
			if (flag || (effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage >= 4) || (effect == WeaponTechniqueEffect.SovereignGoldenCataclysm && stage >= 9))
			{
				DrawSovereignEcho(anchor - travel * (31f * speed), flip, pose, 0.16f);
			}
		}
	}

	private void DrawSovereignEcho(Vector2 anchor, bool flip, Pose pose, float alpha)
	{
		Color body = ScaleAlpha(new Color(13, 12, 18, 185), alpha);
		Color seam = ScaleAlpha(new Color(224, 169, 49, 180), alpha);
		float y = pose.Bob + pose.Crouch;
		DrawBox(anchor, pose.Lean - 11f, y - 48f, 22f, 27f, body, flip);
		DrawBox(anchor, pose.Lean - 7f, y - 65f, 15f, 16f, body, flip);
		DrawLine(anchor, new Vector2(pose.Lean + 6f, y - 22f), new Vector2(pose.FrontKneeX, pose.FrontKneeY), 6f, body, flip);
		DrawLine(anchor, new Vector2(pose.FrontKneeX, pose.FrontKneeY), new Vector2(pose.FrontFootX, pose.FrontFootY), 5f, body, flip);
		DrawLine(anchor, new Vector2(pose.Lean + 10f, y - 44f), new Vector2(pose.FrontElbowX, pose.FrontElbowY), 5f, body, flip);
		DrawLine(anchor, new Vector2(pose.FrontElbowX, pose.FrontElbowY), new Vector2(pose.SwordHandX, pose.SwordHandY), 3f, seam, flip);
	}

	private void DrawSovereignMartialEffects(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.AbyssRing || player.Combat.CurrentTechnique == null || player.Combat.CurrentAttack == null)
		{
			return;
		}
		float perfectRift = player.Combat.SovereignPerfectRiftVisualRatio;
		if (perfectRift > 0f)
		{
			float counterProgress = 1f - perfectRift;
			float counterFlash = 1f - Math.Clamp(MathF.Abs(counterProgress - 0.58f) * 4.2f, 0f, 1f);
			if (counterFlash > 0.02f)
			{
				Vector2 fist = new Vector2(pose.SwordHandX, pose.SwordHandY);
				Color counterGold = ScaleAlpha(new Color(255, 217, 105), counterFlash * 0.88f);
				DrawBox(anchor, fist.X - 3f, fist.Y - 3f, 7f, 7f, counterGold, flip);
				DrawLine(anchor, fist, fist + new Vector2(18f, 0f), 2f, counterGold, flip);
			}
		}
		AttackDefinition attack = player.Combat.CurrentAttack;
		float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
		if (activeElapsed < -0.025f || activeElapsed > attack.ActiveTime + 0.055f || attack.DamageMultiplier <= 0f)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
		int stage = player.Combat.TechniqueStageIndex;
		bool flag = effect == WeaponTechniqueEffect.SovereignMartialArts;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = ((stage == 3 || stage == 5) ? true : false);
			flag2 = flag3;
		}
		bool flag4 = flag2 || (effect == WeaponTechniqueEffect.SovereignParadoxImpact && stage == 5);
		bool flag5 = flag4;
		if (!flag5)
		{
			bool flag6 = effect == WeaponTechniqueEffect.SovereignInfiniteRiftTheatre;
			bool flag7 = flag6;
			if (flag7)
			{
				bool flag3 = ((stage == 1 || stage == 3) ? true : false);
				flag7 = flag3;
			}
			flag5 = flag7;
		}
		bool legContact = flag5 || effect == WeaponTechniqueEffect.SovereignHeavenReversal;
		Vector2 contact = (legContact ? new Vector2(pose.FrontFootX, pose.FrontFootY) : new Vector2(pose.SwordHandX, pose.SwordHandY));
		Vector2 strikeDirection = ((legContact && effect == WeaponTechniqueEffect.SovereignHeavenReversal) ? Vector2.UnitY : Vector2.UnitX);
		float fade = 1f - Math.Clamp(MathF.Max(0f, activeElapsed) / Math.Max(0.01f, attack.ActiveTime + 0.055f), 0f, 1f);
		Color gold = ScaleAlpha(new Color(250, 202, 83), 0.35f + fade * 0.55f);
		DrawBox(anchor, contact.X - 3f, contact.Y - 3f, 6f, 6f, gold, flip);
		DrawLine(anchor, contact, contact + strikeDirection * (13f + ((stage == 7) ? 8f : 0f)), (stage == 7) ? 3f : 2f, gold, flip);
		DrawLine(anchor, contact, contact + strikeDirection * 9f + new Vector2(0f, -6f), 1f, gold, flip);
		DrawLine(anchor, contact, contact + strikeDirection * 8f + new Vector2(0f, 6f), 1f, gold, flip);
		if (effect == WeaponTechniqueEffect.SovereignMartialArts && stage == 6)
		{
			Vector2 second = new Vector2(pose.OffHandX, pose.OffHandY);
			DrawBox(anchor, second.X - 2f, second.Y - 2f, 5f, 5f, gold, flip);
			DrawLine(anchor, second, second + new Vector2(13f, -3f), 2f, gold, flip);
		}
	}

	private void ApplyFamilyAttackAngles(ref Pose pose, float[] lightOne, float[] lightTwo, float[] lightThree, float[] heavy, float[] lightFour = null, float[] lightFive = null)
	{
		PlayerVisualState state = _animation.State;
		if (1 == 0)
		{
		}
		float[] array = state switch
		{
			PlayerVisualState.LightAttack1 => lightOne,
			PlayerVisualState.LightAttack2 => lightTwo,
			PlayerVisualState.LightAttack3 => lightThree,
			PlayerVisualState.LightAttack4 => lightFour ?? lightTwo,
			PlayerVisualState.LightAttack5 => lightFive ?? lightThree,
			PlayerVisualState.HeavyAttack => heavy,
			_ => null,
		};
		if (1 == 0)
		{
		}
		float[] angles = array;
		if (angles != null)
		{
			pose.SwordAngle = angles[Math.Min(_animation.Frame, angles.Length - 1)];
		}
	}

	private static void ConfigureRunStride(ref Pose pose, int frame)
	{
		switch (frame)
		{
		case 0:
			pose.Step = 0f;
			break;
		case 1:
			pose.Step = 4f;
			pose.RightFootLift = 1f;
			break;
		case 2:
			pose.Step = 6f;
			pose.RightFootLift = 3f;
			break;
		case 3:
			pose.Step = 4f;
			pose.RightFootLift = 1f;
			break;
		case 4:
			pose.Step = 0f;
			break;
		case 5:
			pose.Step = -4f;
			pose.LeftFootLift = 1f;
			break;
		case 6:
			pose.Step = -6f;
			pose.LeftFootLift = 3f;
			break;
		case 7:
			pose.Step = -4f;
			pose.LeftFootLift = 1f;
			break;
		}
	}

	private void ConfigureAttackPose(ref Pose pose, float[] swordAngles, float[] bodyLean, float cloakTail)
	{
		int frame = Math.Min(_animation.Frame, Math.Min(swordAngles.Length, bodyLean.Length) - 1);
		pose.SwordAngle = swordAngles[frame];
		pose.Lean = bodyLean[frame];
		pose.SwordHandX = 12f + MathF.Max(0f, pose.Lean) * 0.45f;
		pose.ShieldX = -16f;
		pose.ShieldY = -43f;
		pose.CloakTail = cloakTail - MathF.Max(0f, pose.Lean) * 0.65f;
	}

	private void DrawCloak(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
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

	private void DrawLegs(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		if (player.IsKnightSwordAndShield && pose.UseMartialLimbs)
		{
			DrawKnightLegs(player, anchor, flip, pose, bodyX, bodyY);
			return;
		}
		if (player.WeaponFamily == WeaponFamily.AbyssRing && pose.UseMartialLimbs)
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
		float rightStep = 0f - pose.Step;
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

	private void DrawKnightLegs(DungeonAscendant.Player.Player player,
		Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		Color cloth = Tint(Cloth, player);
		Color steel = Tint(SteelDark, player);
		Color leather = Tint(Leather, player);
		Vector2 frontHip = new(bodyX + 6f, bodyY - 23f + pose.Crouch);
		Vector2 rearHip = new(bodyX - 6f, bodyY - 23f + pose.Crouch);
		Vector2 frontKnee = new(pose.FrontKneeX, pose.FrontKneeY);
		Vector2 rearKnee = new(pose.RearKneeX, pose.RearKneeY);
		Vector2 frontFoot = new(pose.FrontFootX, pose.FrontFootY);
		Vector2 rearFoot = new(pose.RearFootX, pose.RearFootY);

		DrawLine(anchor, rearHip, rearKnee, 9f, Outline, flip);
		DrawLine(anchor, rearKnee, rearFoot, 8f, Outline, flip);
		DrawLine(anchor, rearHip, rearKnee, 6f, cloth, flip);
		DrawLine(anchor, rearKnee, rearFoot, 5f, steel, flip);
		DrawLine(anchor, rearFoot - new Vector2(3f, 0f),
			rearFoot + new Vector2(8f, 0f), 6f, leather, flip);

		DrawLine(anchor, frontHip, frontKnee, 10f, Outline, flip);
		DrawLine(anchor, frontKnee, frontFoot, 9f, Outline, flip);
		DrawLine(anchor, frontHip, frontKnee, 7f, cloth, flip);
		DrawLine(anchor, frontKnee, frontFoot, 6f, steel, flip);
		DrawLine(anchor, frontKnee, frontFoot, 1f,
			Tint(SteelLight, player), flip);
		DrawLine(anchor, frontFoot - new Vector2(3f, 0f),
			frontFoot + new Vector2(9f, 0f), 7f, leather, flip);
	}

	private void DrawSovereignLegs(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		Color obsidian = Tint(new Color(17, 18, 23), player);
		Color gold = Tint(new Color(174, 126, 34), player);
		Vector2 frontHip = new Vector2(bodyX + 6f, bodyY - 22f);
		Vector2 rearHip = new Vector2(bodyX - 6f, bodyY - 22f);
		Vector2 frontKnee = new Vector2(pose.FrontKneeX, pose.FrontKneeY);
		Vector2 frontFoot = new Vector2(pose.FrontFootX, pose.FrontFootY);
		Vector2 rearKnee = new Vector2(pose.RearKneeX, pose.RearKneeY);
		Vector2 rearFoot = new Vector2(pose.RearFootX, pose.RearFootY);
		DrawLine(anchor, rearHip, rearKnee, 8f, Outline, flip);
		DrawLine(anchor, rearKnee, rearFoot, 7f, Outline, flip);
		DrawLine(anchor, rearHip, rearKnee, 5f, obsidian, flip);
		DrawLine(anchor, rearKnee, rearFoot, 4f, obsidian, flip);
		DrawLine(anchor, frontHip, frontKnee, 9f, Outline, flip);
		DrawLine(anchor, frontKnee, frontFoot, 8f, Outline, flip);
		DrawLine(anchor, frontHip, frontKnee, 6f, obsidian, flip);
		DrawLine(anchor, frontKnee, frontFoot, 5f, obsidian, flip);
		DrawLine(anchor, frontKnee, frontFoot, 1f, gold, flip);
		Vector2 footDirection = Vector2.Normalize(frontFoot - frontKnee + new Vector2(0.01f, 0f));
		DrawLine(anchor, frontFoot - footDirection * 3f, frontFoot + footDirection * 8f, 6f, Outline, flip);
		DrawLine(anchor, frontFoot - footDirection * 2f, frontFoot + footDirection * 7f, 3f, obsidian, flip);
		Vector2 rearFootDirection = Vector2.Normalize(rearFoot - rearKnee + new Vector2(0.01f, 0f));
		DrawLine(anchor, rearFoot - rearFootDirection * 2f, rearFoot + rearFootDirection * 7f, 5f, obsidian, flip);
	}

	private void DrawLightLegs(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		float leftStep = pose.Step;
		float rightStep = 0f - pose.Step;
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

	private void DrawHeavyLegs(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		float leftStep = pose.Step;
		float rightStep = 0f - pose.Step;
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

	private void DrawRearArm(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		float shoulderWidth = 10f * GetTorsoScale(player.Appearance);
		Vector2 shoulder = player.IsKnightSwordAndShield && pose.UseKnightHeldStance
			? new Vector2(bodyX - 5f, bodyY - 45f)
			: new Vector2(bodyX - shoulderWidth, bodyY - 44f);
		if (player.WeaponFamily == WeaponFamily.AbyssRing && pose.UseMartialLimbs)
		{
			DrawSovereignArm(player, anchor, flip, shoulder, new Vector2(pose.RearElbowX, pose.RearElbowY), new Vector2(pose.OffHandX, pose.OffHandY), front: false);
		}
		else if (player.WeaponFamily == WeaponFamily.ArcaneWarStaff && pose.UseMartialLimbs)
		{
			DrawCastingArm(player, anchor, flip, shoulder, new Vector2(pose.RearElbowX, pose.RearElbowY), new Vector2(pose.OffHandX, pose.OffHandY), front: false);
		}
		if (player.WeaponFamily == WeaponFamily.DualSwords)
		{
			Vector2 daggerHand = new Vector2(pose.OffHandX, pose.OffHandY);
			Vector2 daggerElbow = (pose.UseMartialLimbs ? new Vector2(pose.RearElbowX, pose.RearElbowY) : (Vector2.Lerp(shoulder, daggerHand, 0.52f) + new Vector2(-3f, 5f)));
			DrawLine(anchor, shoulder, daggerElbow, 8f, Outline, flip);
			DrawLine(anchor, shoulder, daggerElbow, 5f, Tint(ClothLight, player), flip);
			DrawLine(anchor, daggerElbow, daggerHand, 7f, Outline, flip);
			DrawLine(anchor, daggerElbow, daggerHand, 4f, Tint(Leather, player), flip);
		}
		else if (player.WeaponFamily == WeaponFamily.HunterBow)
		{
			Vector2 stringHand = new Vector2(pose.SwordHandX - 9f, pose.SwordHandY);
			Vector2 bowElbow = Vector2.Lerp(shoulder, stringHand, 0.48f) + new Vector2(-7f, -2f);
			DrawLine(anchor, shoulder, bowElbow, 8f, Outline, flip);
			DrawLine(anchor, shoulder, bowElbow, 5f, Tint(ClothLight, player), flip);
			DrawLine(anchor, bowElbow, stringHand, 7f, Outline, flip);
			DrawLine(anchor, bowElbow, stringHand, 4f, Tint(Leather, player), flip);
		}
		else if (player.WeaponFamily == WeaponFamily.Scythe)
		{
			DrawScytheArm(player, anchor, flip, shoulder, new Vector2(pose.RearElbowX, pose.RearElbowY), new Vector2(pose.OffHandX, pose.OffHandY), front: false);
		}
		else if (player.IsKnightSwordAndShield && pose.UseMartialLimbs)
		{
			Vector2 shieldGrip = new(pose.ShieldX + 6f, pose.ShieldY + 17f);
			Vector2 elbow = new(pose.RearElbowX, pose.RearElbowY);
			DrawLine(anchor, shoulder, elbow, 9f, Outline, flip);
			DrawLine(anchor, shoulder, elbow, 6f, Tint(ClothLight, player), flip);
			DrawLine(anchor, elbow, shieldGrip, 8f, Outline, flip);
			DrawLine(anchor, elbow, shieldGrip, 5f, Tint(SteelDark, player), flip);
			DrawBox(anchor, shieldGrip.X - 2f, shieldGrip.Y - 2f, 5f, 5f,
				Tint(LeatherLight, player), flip);
		}
		else if (!player.UsesShield)
		{
			Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
			Vector2 supportGrip = new Vector2(pose.SwordHandX, pose.SwordHandY) - direction * 6f;
			Vector2 supportElbow = Vector2.Lerp(shoulder, supportGrip, 0.55f) + new Vector2(-2f, 4f);
			DrawLine(anchor, shoulder, supportElbow, 8f, Outline, flip);
			DrawLine(anchor, shoulder, supportElbow, 5f, Tint(ClothLight, player), flip);
			DrawLine(anchor, supportElbow, supportGrip, 7f, Outline, flip);
			DrawLine(anchor, supportElbow, supportGrip, 4f, Tint(Leather, player), flip);
			DrawBox(anchor, supportGrip.X - 2f, supportGrip.Y - 2f, 5f, 5f, Tint(GetSkinColor(player.Appearance.SkinTone), player), flip);
		}
		else
		{
			Vector2 shieldGrip = new Vector2(pose.ShieldX + 5.6f, pose.ShieldY + 17.050001f);
			Vector2 elbow = Vector2.Lerp(shoulder, shieldGrip, 0.52f) + new Vector2(-2f, 3f);
			DrawLine(anchor, shoulder, elbow, 8f, Outline, flip);
			DrawLine(anchor, shoulder, elbow, 5f, Tint(ClothLight, player), flip);
			DrawLine(anchor, elbow, shieldGrip, 7f, Outline, flip);
			DrawLine(anchor, elbow, shieldGrip, 4f, Tint(SteelDark, player), flip);
			DrawBox(anchor, shieldGrip.X - 2f, shieldGrip.Y - 2f, 5f, 5f, Tint(LeatherLight, player), flip);
		}
	}

	private bool DrawDuelistBladeTrails(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		if ((effect == WeaponTechniqueEffect.DuelistPhantomParry || effect == WeaponTechniqueEffect.DuelistBladeOverdrive) ? true : false)
		{
			return true;
		}
		bool flag = (uint)(effect - 15) <= 4u;
		bool passTwoTechnique = flag;
		bool ultimate = effect == WeaponTechniqueEffect.DuelistInfiniteStarburst;
		bool flag2 = !passTwoTechnique && !ultimate;
		bool flag3 = flag2;
		if (flag3)
		{
			flag = ((effect == WeaponTechniqueEffect.DuelistStarburstSwordArt || effect == WeaponTechniqueEffect.DuelistPhantomRiposte) ? true : false);
			flag3 = !flag;
		}
		if (flag3)
		{
			return false;
		}
		float elapsed = player.Combat.StateElapsed - attack.StartupTime * 0.55f;
		float duration = attack.ActiveTime + MathF.Min(0.11f, attack.RecoveryTime);
		int momentumTier = player.Combat.Resources.DuelistMomentumTier;
		duration *= momentumTier switch
		{
			1 => 1.09f,
			2 => 1.18f,
			_ => 1f,
		};
		if (elapsed < 0f || elapsed > duration)
		{
			return true;
		}
		float progress = Math.Clamp(elapsed / MathF.Max(0.01f, duration), 0f, 1f);
		float fade = MathF.Sin(progress * (float)Math.PI);
		float overdrive = _duelistOverdriveVisualStrength;
		float clarity = 1f + (float)momentumTier * 0.07f + overdrive * 0.13f;
		Color cyan = ScaleAlpha(new Color(201, 246, 252), Math.Clamp(fade * 0.82f * clarity, 0f, 1f));
		Color cyanEdge = ScaleAlpha(new Color(91, 190, 211), Math.Clamp(fade * 0.56f * clarity, 0f, 1f));
		Color violet = ScaleAlpha(new Color(218, 201, 244), Math.Clamp(fade * 0.8f * clarity, 0f, 1f));
		Color violetEdge = ScaleAlpha(new Color(139, 101, 190), Math.Clamp(fade * 0.55f * clarity, 0f, 1f));
		Vector2 stellarCenter = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 lunarCenter = new Vector2(pose.OffHandX, pose.OffHandY);
		float span = momentumTier switch
		{
			1 => 0.38f,
			2 => 0.43f,
			_ => 0.32f,
		};
		if (ultimate)
		{
			int ultimateStage = player.Combat.TechniqueStageIndex;
			int group = Math.Clamp(ultimateStage / 8, 0, 3);
			int variation = ultimateStage % 8;
			float ultimateSpan = 0.27f + (float)group * 0.055f;
			float radiusBonus = (float)group * 3f;
			if (ultimateStage >= 32)
			{
				float firstCut = Math.Clamp((progress - 0.39f) / 0.12f, 0f, 1f);
				float secondCut = Math.Clamp((progress - 0.43f) / 0.12f, 0f, 1f);
				if (firstCut <= 0f)
				{
					return true;
				}
				DrawDuelistArc(anchor, flip, stellarCenter, -2.18f, 0.7f, 72f, firstCut, ScaleAlpha(cyanEdge, 0.92f), cyan, 0.52f);
				if (secondCut > 0f)
				{
					DrawDuelistArc(anchor, flip, lunarCenter, 0.72f, -1.1f, 68f, secondCut, ScaleAlpha(violetEdge, 0.94f), violet, 0.5f);
				}
				return true;
			}
			switch (variation)
			{
			case 0:
			case 2:
			case 4:
			case 6:
			case 7:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool stellarLead = flag;
			switch (variation)
			{
			case 1:
			case 3:
			case 5:
			case 6:
			case 7:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool lunarLead = flag;
			float rise = (float)group * 0.1f;
			if (1 == 0)
			{
			}
			float num;
			switch (variation)
			{
			case 2:
				num = -0.3f;
				break;
			case 4:
				num = -1.22f;
				break;
			case 6:
			case 7:
				num = -1.68f;
				break;
			default:
				num = -1.44f;
				break;
			}
			if (1 == 0)
			{
			}
			float stellarStart = num;
			if (1 == 0)
			{
			}
			switch (variation)
			{
			case 2:
				num = -0.04f;
				break;
			case 4:
				num = -0.1f - rise;
				break;
			case 6:
			case 7:
				num = 0.62f - rise;
				break;
			default:
				num = 0.54f - rise;
				break;
			}
			if (1 == 0)
			{
			}
			float stellarEnd = num;
			if (1 == 0)
			{
			}
			switch (variation)
			{
			case 3:
				num = 0.74f;
				break;
			case 5:
				num = -1.74f;
				break;
			case 6:
			case 7:
				num = 0.96f;
				break;
			default:
				num = 0.86f;
				break;
			}
			if (1 == 0)
			{
			}
			float lunarStart = num;
			if (1 == 0)
			{
			}
			switch (variation)
			{
			case 3:
				num = -1.42f - rise;
				break;
			case 5:
				num = 0.58f - rise;
				break;
			case 6:
			case 7:
				num = -1.12f - rise;
				break;
			default:
				num = -1.18f - rise;
				break;
			}
			if (1 == 0)
			{
			}
			float lunarEnd = num;
			if (stellarLead)
			{
				DrawDuelistArc(anchor, flip, stellarCenter, stellarStart, stellarEnd, 36f + radiusBonus, progress, cyanEdge, cyan, ultimateSpan);
			}
			if (lunarLead)
			{
				DrawDuelistArc(anchor, flip, lunarCenter, lunarStart, lunarEnd, 33f + radiusBonus, progress, violetEdge, violet, ultimateSpan);
			}
			flag = (uint)(variation - 6) <= 1u;
			if (flag && progress >= 0.4f && progress <= 0.82f)
			{
				Vector2 contact = (stellarCenter + lunarCenter) * 0.5f + new Vector2(23f, -6f - (float)group * 2f);
				DrawDuelistStarFlash(anchor, flip, contact, 4f + (float)group, fade * 0.88f);
			}
			return true;
		}
		switch (effect)
		{
		case WeaponTechniqueEffect.DuelistFlashBlade:
			if (player.Combat.TechniqueStageIndex == 0)
			{
				Vector2 forward2 = Vector2.Normalize(new Vector2(1f, -0.04f));
				DrawLine(anchor, stellarCenter - forward2 * (10f + progress * 12f), stellarCenter + forward2 * (37f + progress * 8f), 2f, cyan, flip);
				DrawLine(anchor, stellarCenter - forward2 * (18f + progress * 10f), stellarCenter + forward2 * 19f, 1f, cyanEdge, flip);
			}
			else
			{
				DrawDuelistArc(anchor, flip, lunarCenter, 0.82f, -1.1f, 34f, progress, violetEdge, violet, span);
				DrawDuelistArc(anchor, flip, stellarCenter, -0.62f, 0.46f, 36f, progress, cyanEdge, cyan, span * 0.8f);
			}
			DrawDuelistAstralLine(anchor, flip, stellarCenter, lunarCenter, progress, fade, momentumTier, overdrive);
			return true;
		case WeaponTechniqueEffect.DuelistCrossRequiem:
			switch (player.Combat.TechniqueStageIndex)
			{
			case 0:
				return true;
			case 1:
				DrawDuelistArc(anchor, flip, lunarCenter, -1.94f, 0.48f, 33f, progress, violetEdge, violet, span);
				break;
			case 2:
				DrawDuelistArc(anchor, flip, stellarCenter, 0.76f, -1.3f, 37f, progress, cyanEdge, cyan, span);
				break;
			default:
				DrawDuelistArc(anchor, flip, stellarCenter, -1.64f, 0.66f, 41f, progress, cyanEdge, cyan, span + 0.05f);
				DrawDuelistArc(anchor, flip, lunarCenter, 0.92f, -1.08f, 38f, progress, violetEdge, violet, span + 0.05f);
				if (progress >= 0.46f && progress <= 0.82f)
				{
					Vector2 contact2 = (stellarCenter + lunarCenter) * 0.5f + new Vector2(24f, -5f);
					DrawDuelistStarFlash(anchor, flip, contact2, 6f, fade);
				}
				break;
			}
			return true;
		case WeaponTechniqueEffect.DuelistThousandEdgeWaltz:
		{
			int waltzStage = player.Combat.TechniqueStageIndex;
			switch (waltzStage)
			{
			case 0:
			case 3:
			case 4:
			case 6:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool lunarLead2 = flag;
			flag = (((uint)(waltzStage - 1) <= 1u || (uint)(waltzStage - 5) <= 1u) ? true : false);
			bool stellarLead2 = flag;
			flag = (uint)(waltzStage - 4) <= 1u;
			bool reverse = flag;
			float stellarStart2 = (reverse ? 0.78f : (-1.34f));
			float stellarEnd2 = (reverse ? (-1.3f) : 0.58f);
			float lunarStart2 = (reverse ? (-1.72f) : 0.88f);
			float lunarEnd2 = (reverse ? 0.56f : (-1.2f));
			if (stellarLead2)
			{
				DrawDuelistArc(anchor, flip, stellarCenter, stellarStart2, stellarEnd2, (waltzStage == 6) ? 42f : 36f, progress, cyanEdge, cyan, span);
			}
			if (lunarLead2)
			{
				DrawDuelistArc(anchor, flip, lunarCenter, lunarStart2, lunarEnd2, (waltzStage == 6) ? 39f : 33f, progress, violetEdge, violet, span);
			}
			if (waltzStage == 6 && progress >= 0.46f && progress <= 0.82f)
			{
				Vector2 contact3 = (stellarCenter + lunarCenter) * 0.5f + new Vector2(25f, -6f);
				DrawDuelistStarFlash(anchor, flip, contact3, 6f, fade);
			}
			return true;
		}
		case WeaponTechniqueEffect.DuelistHeavenSplitter:
			DrawDuelistArc(anchor, flip, stellarCenter, -2.2f, -0.36f, 43f, progress, cyanEdge, cyan, span + 0.04f);
			DrawDuelistArc(anchor, flip, lunarCenter, 0.7f, -1.9f, 40f, progress, violetEdge, violet, span + 0.04f);
			return true;
		case WeaponTechniqueEffect.DuelistFallingStar:
			DrawDuelistArc(anchor, flip, stellarCenter, -1.72f, 1.22f, 38f, progress, cyanEdge, cyan, span);
			DrawDuelistArc(anchor, flip, lunarCenter, 0.78f, 1.88f, 35f, progress, violetEdge, violet, span);
			return true;
		case WeaponTechniqueEffect.DuelistPhantomRiposte:
			if (progress < 0.36f)
			{
				DrawDuelistArc(anchor, flip, stellarCenter, -1.12f, 0.4f, 34f, progress / 0.36f, cyanEdge, cyan);
			}
			else if (progress < 0.68f)
			{
				DrawDuelistArc(anchor, flip, lunarCenter, 0.72f, -1.08f, 31f, (progress - 0.36f) / 0.32f, violetEdge, violet);
			}
			else
			{
				float thrust = (progress - 0.68f) / 0.32f;
				Vector2 direction = Vector2.Normalize(new Vector2(1f, -0.05f));
				DrawLine(anchor, stellarCenter + direction * (8f + thrust * 7f), stellarCenter + direction * (38f + thrust * 8f), 2f, cyan, flip);
			}
			return true;
		default:
		{
			int stage = player.Combat.TechniqueStageIndex;
			if (stage == 2)
			{
				float first = Math.Clamp(progress / 0.55f, 0f, 1f);
				float second = Math.Clamp((progress - 0.28f) / 0.6f, 0f, 1f);
				Vector2 forward = Vector2.Normalize(new Vector2(1f, -0.08f));
				if (first > 0f && first < 1f)
				{
					DrawLine(anchor, stellarCenter + forward * 8f, stellarCenter + forward * (32f + first * 8f), 2f, cyan, flip);
				}
				if (second > 0f && second < 1f)
				{
					DrawLine(anchor, lunarCenter + forward * 7f, lunarCenter + forward * (29f + second * 8f), 2f, violet, flip);
				}
				return true;
			}
			stage = Math.Clamp(stage, 0, 7);
			flag = ((stage == 0 || stage == 4 || (uint)(stage - 6) <= 1u) ? true : false);
			bool stellarDominant = flag;
			switch (stage)
			{
			case 1:
			case 3:
			case 5:
			case 6:
			case 7:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			bool lunarDominant = flag;
			if (stellarDominant)
			{
				DrawDuelistArc(anchor, flip, stellarCenter, DuelistStellarTrailStart[stage], DuelistStellarTrailEnd[stage], (stage >= 6) ? 38f : 34f, progress, cyanEdge, cyan);
			}
			if (lunarDominant)
			{
				DrawDuelistArc(anchor, flip, lunarCenter, DuelistLunarTrailStart[stage], DuelistLunarTrailEnd[stage], (stage >= 6) ? 35f : 31f, progress, violetEdge, violet);
			}
			if (stage >= 6 && progress >= 0.48f && progress <= 0.78f)
			{
				Vector2 crossing = (stellarCenter + lunarCenter) * 0.5f + new Vector2(23f, -7f);
				DrawDuelistStarFlash(anchor, flip, crossing, (stage == 7) ? 7f : 5f, fade);
			}
			return true;
		}
		}
	}

	private void DrawDuelistArc(Vector2 anchor, bool flip, Vector2 center, float startAngle, float endAngle, float radius, float progress, Color edge, Color core, float trailSpan = 0.34f)
	{
		float head = Math.Clamp(progress * 1.18f, 0f, 1f);
		float tail = MathF.Max(0f, head - trailSpan);
		Vector2 previous = center + new Vector2(MathF.Cos(MathHelper.Lerp(startAngle, endAngle, tail)), MathF.Sin(MathHelper.Lerp(startAngle, endAngle, tail))) * radius;
		for (int index = 1; index <= 7; index++)
		{
			float sample = MathHelper.Lerp(tail, head, (float)index / 7f);
			float angle = MathHelper.Lerp(startAngle, endAngle, sample);
			Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
			float taper = MathF.Sin((float)index / 7f * (float)Math.PI);
			DrawLine(anchor, previous, next, 1f + taper * 1.4f, edge, flip);
			DrawLine(anchor, previous, next, 1f, core, flip);
			previous = next;
		}
		if (head > 0.1f && head < 1f)
		{
			float headAngle = MathHelper.Lerp(startAngle, endAngle, head);
			Vector2 headPoint = center + new Vector2(MathF.Cos(headAngle), MathF.Sin(headAngle)) * radius;
			Vector2 tangent = new Vector2(0f - MathF.Sin(headAngle), MathF.Cos(headAngle));
			DrawLine(anchor, headPoint - tangent * 2.5f, headPoint + tangent * 2.5f, 1f, ScaleAlpha(new Color(239, 251, 255), (float)(int)core.A / 255f), flip);
		}
	}

	private void DrawDuelistAstralLine(Vector2 anchor, bool flip, Vector2 stellarCenter, Vector2 lunarCenter, float progress, float fade, int momentumTier, float overdrive)
	{
		if (momentumTier >= 2 && !(overdrive < 0.2f))
		{
			Vector2 start = (stellarCenter + lunarCenter) * 0.5f - new Vector2(13f + progress * 6f, 1f);
			Vector2 end = start + new Vector2(43f, -5f);
			DrawLine(anchor, start, end, 1f, ScaleAlpha(new Color(231, 244, 255), fade * 0.42f), flip);
		}
	}

	private void DrawDuelistStarFlash(Vector2 anchor, bool flip, Vector2 center, float radius, float alpha)
	{
		Color cyan = ScaleAlpha(new Color(217, 251, 255), alpha);
		Color violet = ScaleAlpha(new Color(225, 207, 250), alpha);
		DrawLine(anchor, center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), 1f, cyan, flip);
		DrawLine(anchor, center - new Vector2(0f, radius), center + new Vector2(0f, radius), 1f, violet, flip);
		DrawBox(anchor, center.X - 1f, center.Y - 1f, 3f, 3f, Color.White * alpha, flip);
	}

	private void DrawTorso(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
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
		DrawBox(anchor, bodyX - 1f, bodyY - 42f, 3f, 11f, SunGold, flip);
		DrawBox(anchor, bodyX - 4f, bodyY - 39f, 9f, 3f, SunGold, flip);
		DrawBox(anchor, bodyX - 2f, bodyY - 44f, 5f, 3f, SunGold, flip);
		DrawBox(anchor, bodyX - 10f, bodyY - 22f, 4f, 6f, new Color(68, 93, 72), flip);
		DrawLine(anchor, new Vector2(bodyX + 8f, bodyY - 22f), new Vector2(bodyX + 14f, bodyY - 13f), 2f, armorLight, flip);
		DrawBox(anchor, bodyX - 17f, bodyY - 50f, 10f, 8f, Outline, flip);
		DrawBox(anchor, bodyX - 15f, bodyY - 49f, 8f, 5f, armorDark, flip);
		DrawBox(anchor, bodyX + 7f, bodyY - 52f, 13f, 10f, Outline, flip);
		DrawBox(anchor, bodyX + 8f, bodyY - 50f, 10f, 6f, armor, flip);
		DrawBox(anchor, bodyX + 10f, bodyY - 49f, 7f, 2f, armorLight, flip);
	}

	private void DrawLightTorso(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, float bodyX, float bodyY)
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

	private void DrawHeavyTorso(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, float bodyX, float bodyY)
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

	private void DrawHead(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		CharacterAppearance appearance = player.Appearance;
		Color skinBase = GetSkinColor(appearance.SkinTone);
		Color skin = Tint(skinBase, player);
		Color skinLight = Tint(Color.Lerp(skinBase, Color.White, 0.18f), player);
		Color hair = Tint(GetHairColor(appearance.HairColor), player);
		Color hairLight = Tint(Color.Lerp(hair, Color.White, 0.16f), player);
		FacePreset facePreset = appearance.FacePreset;
		if (1 == 0)
		{
		}
		float num = facePreset switch
		{
			FacePreset.Round => 2f,
			FacePreset.Narrow => -1f,
			FacePreset.Strong => 1f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float faceWidth = num;
		if (appearance.BodyType == CharacterBodyType.Female)
		{
			faceWidth -= 1f;
		}
		float headX = bodyX - 8f - faceWidth * 0.5f;
		float headWidth = 17f + faceWidth;
		float jawY = ((appearance.FacePreset == FacePreset.Round) ? (-50f) : (-51f));
		FacePreset facePreset2 = appearance.FacePreset;
		bool flag = ((facePreset2 == FacePreset.Angular || facePreset2 == FacePreset.Strong) ? true : false);
		float noseLength = (flag ? 5f : 4f);
		DrawBox(anchor, headX, bodyY - 66f, headWidth, 18f, Outline, flip);
		DrawBox(anchor, headX + 2f, bodyY - 64f, headWidth - 3f, 15f, skin, flip);
		DrawBox(anchor, bodyX - 4f, bodyY - 62f, 10f, 7f, skinLight, flip);
		if (appearance.FacePreset == FacePreset.Round)
		{
			DrawBox(anchor, bodyX - 5f, bodyY - 55f, 12f, 5f, skin, flip);
		}
		else if (appearance.FacePreset == FacePreset.Strong)
		{
			DrawBox(anchor, bodyX - 3f, bodyY - 52f, 10f, 4f, skin, flip);
		}
		DrawBox(anchor, bodyX + 7f, bodyY - 60f, noseLength, 6f, Outline, flip);
		DrawBox(anchor, bodyX + 7f, bodyY - 59f, noseLength - 2f, 4f, skinLight, flip);
		DrawBox(anchor, bodyX + 4f, bodyY + jawY, 5f, 3f, Outline, flip);
		DrawBox(anchor, bodyX + 3f, bodyY - 53f, 5f, 3f, skin, flip);
		DrawHair(appearance.HairStyle, anchor, flip, bodyX, bodyY, hair, hairLight);
		int eyeY = ((appearance.FacePreset == FacePreset.Narrow) ? (-58) : (-59));
		DrawBox(anchor, bodyX + 4f, bodyY + (float)eyeY, 3f, 3f, new Color(30, 27, 27), flip);
		DrawBox(anchor, bodyX + 4f, bodyY - 54f, 4f, 1f, new Color(91, 48, 42), flip);
		DrawFacialHair(appearance.FacialHair, anchor, flip, bodyX, bodyY, hair);
		DrawFaceDetail(appearance.FaceDetail, anchor, flip, bodyX, bodyY);
	}

	private void DrawHair(HairStyle style, Vector2 anchor, bool flip, float bodyX, float bodyY, Color hair, Color hairLight)
	{
		switch (style)
		{
		case HairStyle.Bald:
			break;
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

	private void DrawFacialHair(FacialHairStyle style, Vector2 anchor, bool flip, float bodyX, float bodyY, Color hair)
	{
		switch (style)
		{
		case FacialHairStyle.LightStubble:
		{
			Color stubble = new Color(hair.R, hair.G, hair.B, (byte)155);
			DrawBox(anchor, bodyX + 2f, bodyY - 55f, 6f, 2f, stubble, flip);
			DrawBox(anchor, bodyX, bodyY - 52f, 7f, 2f, stubble, flip);
			break;
		}
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

	private void DrawFaceDetail(FaceDetail detail, Vector2 anchor, bool flip, float bodyX, float bodyY)
	{
		Color mark = new Color(104, 54, 48);
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

	private void DrawFrontArm(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, float bodyX, float bodyY)
	{
		float shoulderWidth = 12f * GetTorsoScale(player.Appearance);
		Vector2 shoulder = player.IsKnightSwordAndShield && pose.UseKnightHeldStance
			? new Vector2(bodyX + 7f, bodyY - 45f)
			: new Vector2(bodyX + shoulderWidth, bodyY - 44f);
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		if (player.WeaponFamily == WeaponFamily.AbyssRing && pose.UseMartialLimbs)
		{
			DrawSovereignArm(player, anchor, flip, shoulder, new Vector2(pose.FrontElbowX, pose.FrontElbowY), hand, front: true);
		}
		else if (player.WeaponFamily == WeaponFamily.ArcaneWarStaff && pose.UseMartialLimbs)
		{
			DrawCastingArm(player, anchor, flip, shoulder, new Vector2(pose.FrontElbowX, pose.FrontElbowY), hand, front: true);
		}
		else if (player.WeaponFamily == WeaponFamily.Scythe)
		{
			DrawScytheArm(player, anchor, flip, shoulder, new Vector2(pose.FrontElbowX, pose.FrontElbowY), hand, front: true);
		}
		else if (player.WeaponFamily == WeaponFamily.DualSwords && pose.UseMartialLimbs)
		{
			Vector2 elbow = new Vector2(pose.FrontElbowX, pose.FrontElbowY);
			DrawLine(anchor, shoulder, elbow, 8f, Outline, flip);
			DrawLine(anchor, shoulder, elbow, 5f, Tint(ClothLight, player), flip);
			DrawLine(anchor, elbow, hand, 7f, Outline, flip);
			DrawLine(anchor, elbow, hand, 4f, Tint(Leather, player), flip);
			DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, Tint(GetSkinColor(player.Appearance.SkinTone), player), flip);
		}
		else if (player.IsKnightSwordAndShield && pose.UseMartialLimbs)
		{
			Vector2 elbow = new(pose.FrontElbowX, pose.FrontElbowY);
			DrawLine(anchor, shoulder, elbow, 9f, Outline, flip);
			DrawLine(anchor, shoulder, elbow, 6f, Tint(ClothLight, player), flip);
			DrawLine(anchor, elbow, hand, 8f, Outline, flip);
			DrawLine(anchor, elbow, hand, 5f, Tint(Leather, player), flip);
			DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f,
				Tint(GetSkinColor(player.Appearance.SkinTone), player), flip);
		}
		else
		{
			DrawLine(anchor, shoulder, hand, 8f, Outline, flip);
			DrawLine(anchor, shoulder, hand, 5f, Tint(Leather, player), flip);
			DrawLine(anchor, Vector2.Lerp(shoulder, hand, 0.48f), hand, 5f, Tint(SteelDark, player), flip);
			DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, Tint(GetSkinColor(player.Appearance.SkinTone), player), flip);
		}
	}

	private void DrawSovereignArm(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Vector2 shoulder, Vector2 elbow, Vector2 hand, bool front)
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

	private void DrawScytheArm(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Vector2 shoulder, Vector2 elbow, Vector2 hand, bool front)
	{
		Color sleeve = Tint(ClothLight, player);
		Color bracer = Tint(new Color(59, 42, 67), player);
		Color skin = Tint(GetSkinColor(player.Appearance.SkinTone), player);
		DrawLine(anchor, shoulder, elbow, front ? 9f : 8f, Outline, flip);
		DrawLine(anchor, elbow, hand, front ? 8f : 7f, Outline, flip);
		DrawLine(anchor, shoulder, elbow, front ? 6f : 5f, sleeve, flip);
		DrawLine(anchor, elbow, hand, front ? 5f : 4f, bracer, flip);
		DrawBox(anchor, hand.X - 3f, hand.Y - 3f, 7f, 7f, Outline, flip);
		DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, skin, flip);
	}

	private void DrawCastingArm(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Vector2 shoulder, Vector2 elbow, Vector2 hand, bool front)
	{
		Color sleeve = Tint(ClothLight, player);
		Color bracer = Tint(new Color(68, 72, 91), player);
		Color skin = Tint(GetSkinColor(player.Appearance.SkinTone), player);
		DrawLine(anchor, shoulder, elbow, front ? 9f : 8f, Outline, flip);
		DrawLine(anchor, elbow, hand, front ? 8f : 7f, Outline, flip);
		DrawLine(anchor, shoulder, elbow, front ? 6f : 5f, sleeve, flip);
		DrawLine(anchor, elbow, hand, front ? 5f : 4f, bracer, flip);
		DrawBox(anchor, hand.X - 3f, hand.Y - 3f, 7f, 7f, Outline, flip);
		DrawBox(anchor, hand.X - 2f, hand.Y - 2f, 5f, 5f, skin, flip);
	}

	private void DrawWeapon(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 perpendicular = new Vector2(0f - direction.Y, direction.X);
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 bladeStart = hand + direction * 5f;
		Vector2 bladeEnd = bladeStart + direction * pose.SwordLength;
		Vector2 handleEnd = hand - direction * 7f;
		Color blade = (player.Combat.IsAttackWindingUp ? new Color(178, 133, 83) : Tint(SteelLight, player));
		if (player.WeaponFamily == WeaponFamily.DualSwords)
		{
			int momentumTier = player.Combat.Resources.DuelistMomentumTier;
			if (1 == 0)
			{
			}
			float num = momentumTier switch
			{
				2 => 0.46f,
				1 => 0.28f,
				_ => 0.1f,
			};
			if (1 == 0)
			{
			}
			float rhythmGlow = num;
			WeaponTechnique currentTechnique = player.Combat.CurrentTechnique;
			float splitterCharge = ((currentTechnique != null && currentTechnique.Effect == WeaponTechniqueEffect.DuelistHeavenSplitter && player.Combat.State == CombatState.ChargingHeavy) ? Math.Clamp(player.Combat.StateElapsed / 0.9f, 0f, 1f) : 0f);
			float channelStrength = Math.Clamp(rhythmGlow + _duelistOverdriveVisualStrength * 0.48f + splitterCharge * 0.38f, 0f, 0.94f);
			Color stellarFang = (player.Combat.IsAttackWindingUp ? new Color(124, 197, 216) : Color.Lerp(new Color(176, 188, 193), new Color(209, 244, 248), rhythmGlow));
			Color lunarEdge = (player.Combat.IsAttackWindingUp ? new Color(167, 137, 207) : Color.Lerp(new Color(184, 181, 193), new Color(218, 204, 240), rhythmGlow));
			DrawStellarFang(anchor, flip, hand, direction, pose.SwordLength, stellarFang, channelStrength);
			Vector2 rearHand = new Vector2(pose.OffHandX, pose.OffHandY);
			float rearAngle = pose.OffSwordAngle;
			Vector2 rearDirection = new Vector2(MathF.Cos(rearAngle), MathF.Sin(rearAngle));
			DrawLunarEdge(anchor, flip, rearHand, rearDirection, pose.OffSwordLength, lunarEdge, channelStrength);
			return;
		}
		if (player.WeaponFamily == WeaponFamily.HunterBow)
		{
			WeaponTechniqueEffect rangerEffect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
			Vector2 vector;
			if ((rangerEffect == WeaponTechniqueEffect.RangerSkyfallMarker || rangerEffect == WeaponTechniqueEffect.RangerFallingStar || rangerEffect == WeaponTechniqueEffect.RangerArrowStorm) ? true : false)
			{
				vector = Vector2.Normalize(new Vector2(0.45f, -0.89f));
			}
			else
			{
				bool flag = ((rangerEffect == WeaponTechniqueEffect.RangerSkyfallShot || rangerEffect == WeaponTechniqueEffect.RangerSkyfallVolley) ? true : false);
				vector = (flag ? Vector2.Normalize(new Vector2(1f, 1f)) : Vector2.UnitX);
			}
			Vector2 shotDirection = vector;
			if (player.Combat.RangerDrawState == RangerDrawState.Perfect && player.Combat.State == CombatState.ChargingHeavy)
			{
				hand += new Vector2(MathF.Sin(player.Combat.StateElapsed * 82f) * 0.8f, 0f);
			}
			shotDirection.Normalize();
			Vector2 bowAxis = new Vector2(0f - shotDirection.Y, shotDirection.X);
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
			bool flag2;
			switch (player.Combat.CurrentTechnique?.Effect)
			{
			case WeaponTechniqueEffect.RangerHeavensFury:
			case WeaponTechniqueEffect.RangerPhantomHunt:
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			bool isHeavensFury = flag2;
			WeaponTechnique currentTechnique2 = player.Combat.CurrentTechnique;
			bool phantomExecution = currentTechnique2 != null && currentTechnique2.Effect == WeaponTechniqueEffect.RangerPhantomHunt && player.Combat.TechniqueStageIndex == 13;
			float num2;
			if (!isHeavensFury)
			{
				num2 = 0f;
			}
			else
			{
				WeaponTechnique currentTechnique3 = player.Combat.CurrentTechnique;
				num2 = ((currentTechnique3 == null || currentTechnique3.Effect != WeaponTechniqueEffect.RangerPhantomHunt) ? player.Combat.HeavyChargeProgress : ((player.Combat.State == CombatState.ChargingHeavy) ? player.Combat.RangerUltimateCinematicRatio : (phantomExecution ? Math.Clamp(player.Combat.StateElapsed / MathF.Max(0.01f, player.Combat.CurrentAttack?.StartupTime ?? 0.01f), 0f, 1f) : 0.38f)));
			}
			float ultimateFormation = num2;
			float drawAmount = ((player.Combat.State != CombatState.ChargingHeavy) ? (phantomExecution ? MathHelper.Lerp(0.58f, 1f, ultimateFormation) : (player.Combat.IsAttackWindingUp ? 0.48f : 0.12f)) : (isHeavensFury ? MathHelper.Lerp(0.38f, 0.78f, ultimateFormation) : player.Combat.RangerChargeRatio));
			Vector2 stringHand = hand - shotDirection * (7f + drawAmount * 13f);
			DrawLine(anchor, bowTop, stringHand, 1f, new Color(202, 194, 164), flip);
			DrawLine(anchor, stringHand, bowBottom, 1f, new Color(202, 194, 164), flip);
			bool flag3 = player.Combat.State == CombatState.ChargingHeavy;
			bool flag4 = flag3;
			if (flag4)
			{
				switch (player.Combat.CurrentTechnique?.Effect)
				{
				case WeaponTechniqueEffect.RangerHuntersDraw:
				case WeaponTechniqueEffect.RangerPiercingDraw:
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag4 = flag2;
			}
			if (flag4 && player.Combat.RangerChargeRatio < 0.22f)
			{
				float nockProgress = player.Combat.RangerChargeRatio / 0.22f;
				Vector2 quiverReach = hand + new Vector2(-29f, 8f);
				Vector2 nockPosition = Vector2.Lerp(quiverReach, stringHand, nockProgress);
				DrawLine(anchor, nockPosition, nockPosition + shotDirection * 22f, 2f, new Color(151, 111, 65), flip);
			}
			if (drawAmount > 0.18f)
			{
				WeaponTechnique currentTechnique4 = player.Combat.CurrentTechnique;
				bool phantomHunt = currentTechnique4 != null && currentTechnique4.Effect == WeaponTechniqueEffect.RangerPhantomHunt;
				Color arrowColor = ((!isHeavensFury) ? ((drawAmount >= 0.98f) ? new Color(245, 218, 118) : new Color(166, 131, 78)) : (phantomHunt ? Color.Lerp(new Color(72, 166, 192), new Color(177, 245, 255), ultimateFormation) : Color.Lerp(new Color(151, 127, 85), new Color(226, 205, 135), ultimateFormation)));
				Vector2 arrowTip = hand + shotDirection * 31f;
				DrawLine(anchor, stringHand, arrowTip, (drawAmount >= 0.98f) ? 3f : 2f, arrowColor, flip);
				if (!isHeavensFury && player.Combat.RangerDrawState == RangerDrawState.Perfect)
				{
					DrawBox(anchor, arrowTip.X - 2f, arrowTip.Y - 2f, 4f, 4f, arrowColor, flip);
				}
			}
			return;
		}
		if (player.WeaponFamily == WeaponFamily.Scythe)
		{
			Vector2 shaftEnd = hand + direction * pose.SwordLength;
			Vector2 rearHand2 = new Vector2(pose.OffHandX, pose.OffHandY);
			Vector2 gripEnd = rearHand2 - direction * 8f;
			Color shadowSteel = (player.Combat.IsUmbralAttached ? new Color(104, 52, 151, 175) : new Color(82, 61, 103));
			DrawLine(anchor, gripEnd, shaftEnd, 6f, Outline, flip);
			DrawLine(anchor, gripEnd, shaftEnd, 3f, Tint(new Color(58, 40, 67), player), flip);
			Vector2 neck = shaftEnd - direction * 3f;
			Vector2 curveMid = neck + perpendicular * 13f - direction * 4f;
			Vector2 tip = neck + perpendicular * 27f - direction * 18f;
			DrawLine(anchor, neck, curveMid, 10f, Outline, flip);
			DrawLine(anchor, curveMid, tip, 8f, Outline, flip);
			DrawLine(anchor, neck, curveMid, 6f, shadowSteel, flip);
			DrawLine(anchor, curveMid, tip, 4f, new Color(159, 105, 193), flip);
			return;
		}
		if (player.IsKnightSwordAndShield)
		{
			DrawWayfarerBlade(player, anchor, flip, hand, direction,
				pose.SwordLength);
			return;
		}
		DrawLine(anchor, hand, handleEnd, 5f, Outline, flip);
		DrawLine(anchor, hand, handleEnd, 3f, Tint(LeatherLight, player), flip);
		if (player.WeaponFamily == WeaponFamily.ArcaneWarStaff)
		{
			DrawLine(anchor, handleEnd, bladeEnd, 7f, Outline, flip);
			DrawLine(anchor, handleEnd, bladeEnd, 4f, Tint(new Color(48, 39, 38), player), flip);
			Vector2 focus = bladeEnd + direction * 5f;
			Color arcane = (player.Combat.IsChargingHeavy ? Color.Lerp(new Color(106, 77, 151), new Color(202, 155, 239), player.Combat.HeavyChargeProgress) : new Color(124, 91, 169));
			DrawLine(anchor, focus - perpendicular * 8f, focus + perpendicular * 8f, 5f, Outline, flip);
			DrawLine(anchor, focus - direction * 8f, focus + direction * 8f, 5f, Outline, flip);
			for (int ring = 0; ring < 3; ring++)
			{
				Vector2 ringCenter = bladeEnd - direction * (3f + (float)ring * 6f);
				float ringHalf = 7f - (float)ring;
				DrawLine(anchor, ringCenter - perpendicular * ringHalf, ringCenter + perpendicular * ringHalf, 2f, (ring == 1) ? SteelLight : SteelDark, flip);
			}
			DrawLine(anchor, focus - perpendicular * 6f, focus + direction * 6f, 4f, Outline, flip);
			DrawLine(anchor, focus + direction * 6f, focus + perpendicular * 6f, 4f, Outline, flip);
			DrawLine(anchor, focus + perpendicular * 6f, focus - direction * 6f, 4f, Outline, flip);
			DrawLine(anchor, focus - direction * 6f, focus - perpendicular * 6f, 4f, Outline, flip);
			DrawBox(anchor, focus.X - 3f, focus.Y - 3f, 7f, 7f, arcane, flip);
		}
		else
		{
			DrawLine(anchor, hand - perpendicular * 6f, hand + perpendicular * 6f, 4f, Outline, flip);
			DrawLine(anchor, hand - perpendicular * 5f, hand + perpendicular * 5f, 2f, SunGold, flip);
			float bladeOutline = ((player.WeaponFamily == WeaponFamily.GreatSword) ? 9f : 5f);
			float bladeWidth = ((player.WeaponFamily == WeaponFamily.GreatSword) ? 6f : 3f);
			float guardHalfWidth = ((player.WeaponFamily == WeaponFamily.GreatSword) ? 8f : 5f);
			DrawLine(anchor, hand - perpendicular * guardHalfWidth, hand + perpendicular * guardHalfWidth, (player.WeaponFamily == WeaponFamily.GreatSword) ? 5f : 2f, (player.WeaponFamily == WeaponFamily.GreatSword) ? SteelDark : SunGold, flip);
			DrawLine(anchor, bladeStart, bladeEnd, bladeOutline, Outline, flip);
			DrawLine(anchor, bladeStart, bladeEnd, bladeWidth, blade, flip);
			DrawLine(anchor, bladeStart + perpendicular, bladeEnd + perpendicular, 1f, new Color(210, 216, 218), flip);
			Vector2 mark = bladeStart + direction * 7f;
			DrawBox(anchor, mark.X - 1f, mark.Y - 1f, 3f, 3f, SunGold, flip);
		}
	}

	private void DrawWayfarerBlade(DungeonAscendant.Player.Player player,
		Vector2 anchor, bool flip, Vector2 hand, Vector2 direction, float length)
	{
		Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
		Vector2 pommel = hand - direction * 9f;
		Vector2 gripStart = hand - direction * 7f;
		Vector2 guard = hand + direction;
		Vector2 bladeRoot = hand + direction * 5f;
		Vector2 tip = hand + direction * length;
		Vector2 bladeShoulder = Vector2.Lerp(bladeRoot, tip, 0.68f);
		Color darkSteel = Tint(new Color(53, 62, 70), player);
		Color steel = Tint(new Color(157, 170, 179), player);
		Color brightSteel = Tint(new Color(210, 220, 224), player);
		Color blueSilver = new Color(113, 159, 184, 190);
		bool strongResponse = player.Combat.CurrentAttack?.Kind is
			AttackKind.LightFour or AttackKind.Heavy ||
			player.Combat.CurrentTechnique?.Effect ==
				WeaponTechniqueEffect.KnightGuardRiposte;

		DrawLine(anchor, pommel, gripStart, 6f, Outline, flip);
		DrawLine(anchor, pommel, gripStart, 3f, Tint(new Color(83, 55, 39), player), flip);
		for (int wrap = 0; wrap < 3; wrap++)
		{
			Vector2 mark = gripStart + direction * (1.5f + wrap * 2f);
			DrawLine(anchor, mark - perpendicular * 2f,
				mark + perpendicular * 2f, 1f, Tint(LeatherLight, player), flip);
		}
		DrawLine(anchor, pommel - perpendicular * 3f,
			pommel + perpendicular * 3f, 4f, Outline, flip);
		DrawLine(anchor, pommel - perpendicular * 2f,
			pommel + perpendicular * 2f, 2f, darkSteel, flip);
		DrawLine(anchor, guard - perpendicular * 7f,
			guard + perpendicular * 7f, 5f, Outline, flip);
		DrawLine(anchor, guard - perpendicular * 6f,
			guard + perpendicular * 6f, 2f, darkSteel, flip);
		DrawBox(anchor, guard.X - 2f, guard.Y - 2f, 4f, 4f,
			new Color(91, 132, 157), flip);

		DrawLine(anchor, bladeRoot, bladeShoulder, 7f, Outline, flip);
		DrawLine(anchor, bladeShoulder, tip, 5f, Outline, flip);
		DrawLine(anchor, bladeRoot, bladeShoulder, 4f, steel, flip);
		DrawLine(anchor, bladeShoulder, tip, 3f, brightSteel, flip);
		DrawLine(anchor, bladeRoot - perpendicular,
			bladeShoulder - perpendicular * 0.5f, 1f, darkSteel, flip);
		DrawLine(anchor, bladeRoot + perpendicular * 1.5f,
			tip - direction * 3f, 1f,
			strongResponse ? new Color(163, 206, 224) : blueSilver, flip);
		Vector2 emblem = bladeRoot + direction * 6f;
		DrawBox(anchor, emblem.X - 1f, emblem.Y - 1f, 3f, 3f,
			new Color(91, 138, 163), flip);
		if (strongResponse)
		{
			DrawLine(anchor, bladeRoot + direction * 3f,
				tip - direction * 4f, 1f,
				new Color(180, 220, 234, 155), flip);
		}
	}

	private void DrawDagger(Vector2 anchor, bool flip, Vector2 hand, Vector2 direction, float length, Color blade)
	{
		Vector2 perpendicular = new Vector2(0f - direction.Y, direction.X);
		Vector2 tip = hand + direction * length;
		DrawLine(anchor, hand - direction * 5f, hand, 4f, Outline, flip);
		DrawLine(anchor, hand - perpendicular * 4f, hand + perpendicular * 4f, 3f, SunGold, flip);
		DrawLine(anchor, hand, tip, 5f, Outline, flip);
		DrawLine(anchor, hand + direction * 2f, tip, 2f, blade, flip);
	}

	private void DrawStellarFang(Vector2 anchor, bool flip, Vector2 hand, Vector2 direction, float length, Color blade, float channelStrength)
	{
		Vector2 perpendicular = new Vector2(0f - direction.Y, direction.X);
		Vector2 pommel = hand - direction * 10f;
		Vector2 gripStart = hand - direction * 8f;
		Vector2 bladeRoot = hand + direction * 4f;
		Vector2 bladeMid = hand + direction * (length * 0.63f);
		Vector2 tip = hand + direction * length;
		Color darkMetal = new Color(45, 63, 72);
		Color silver = new Color(188, 207, 213);
		Color cyan = new Color(89, 207, 224);
		DrawLine(anchor, pommel - perpendicular * 3f, pommel + perpendicular * 3f, 4f, Outline, flip);
		DrawLine(anchor, pommel - perpendicular * 2f, pommel + perpendicular * 2f, 2f, cyan, flip);
		DrawLine(anchor, gripStart, hand, 6f, Outline, flip);
		DrawLine(anchor, gripStart, hand, 3f, new Color(37, 45, 53), flip);
		for (int wrap = 0; wrap < 3; wrap++)
		{
			Vector2 mark = gripStart + direction * (2f + (float)wrap * 2.5f);
			DrawLine(anchor, mark - perpendicular * 2f, mark + perpendicular * 2f, 1f, (wrap % 2 == 0) ? silver : cyan, flip);
		}
		Vector2 guardCenter = hand + direction * 1f;
		DrawLine(anchor, guardCenter - perpendicular * 7f - direction * 2f, guardCenter, 4f, Outline, flip);
		DrawLine(anchor, guardCenter, guardCenter + perpendicular * 7f - direction * 2f, 4f, Outline, flip);
		DrawLine(anchor, guardCenter - perpendicular * 6f - direction, guardCenter, 2f, cyan, flip);
		DrawLine(anchor, guardCenter, guardCenter + perpendicular * 6f - direction, 2f, silver, flip);
		DrawBox(anchor, guardCenter.X - 2f, guardCenter.Y - 2f, 4f, 4f, new Color(206, 244, 248), flip);
		Vector2 spineRoot = bladeRoot - perpendicular * 2.4f;
		Vector2 spineMid = bladeMid - perpendicular * 2.1f;
		Vector2 edgeRoot = bladeRoot + perpendicular * 3f;
		Vector2 edgeMid = bladeMid + perpendicular * 1.5f;
		DrawLine(anchor, spineRoot, spineMid, 5f, Outline, flip);
		DrawLine(anchor, spineMid, tip, 4f, Outline, flip);
		DrawLine(anchor, edgeRoot, edgeMid, 5f, Outline, flip);
		DrawLine(anchor, edgeMid, tip, 4f, Outline, flip);
		DrawLine(anchor, spineRoot, spineMid, 2f, darkMetal, flip);
		DrawLine(anchor, spineMid, tip, 2f, silver, flip);
		DrawLine(anchor, edgeRoot, edgeMid, 2f, blade, flip);
		DrawLine(anchor, edgeMid, tip, 2f, new Color(222, 243, 247), flip);
		if (channelStrength > 0.01f)
		{
			DrawLine(anchor, bladeRoot + direction * 4f, tip - direction * 6f, 1f, ScaleAlpha(new Color(202, 249, 255), channelStrength), flip);
			Vector2 rune = bladeRoot + direction * (length * 0.48f);
			DrawBox(anchor, rune.X - 1f, rune.Y - 1f, 3f, 3f, ScaleAlpha(new Color(221, 253, 255), channelStrength), flip);
		}
		DrawLine(anchor, tip - direction * 6f, tip + direction * 2f, 1f, new Color(235, 254, 255), flip);
	}

	private void DrawLunarEdge(Vector2 anchor, bool flip, Vector2 hand, Vector2 direction, float length, Color blade, float channelStrength)
	{
		Vector2 perpendicular = new Vector2(0f - direction.Y, direction.X);
		Vector2 pommel = hand - direction * 10f;
		Vector2 gripStart = hand - direction * 8f;
		Vector2 bladeRoot = hand + direction * 4f;
		Vector2 firstCurve = hand + direction * (length * 0.43f) - perpendicular * 2.1f;
		Vector2 secondCurve = hand + direction * (length * 0.76f) - perpendicular * 4.2f;
		Vector2 tip = hand + direction * length - perpendicular * 2f;
		Color violet = new Color(152, 112, 202);
		Color deepViolet = new Color(65, 48, 88);
		Color moonSilver = new Color(202, 197, 216);
		DrawLine(anchor, pommel - perpendicular * 3f, pommel + perpendicular * 1f, 4f, Outline, flip);
		DrawLine(anchor, pommel - perpendicular * 2f, pommel + perpendicular * 1f, 2f, violet, flip);
		DrawLine(anchor, gripStart, hand, 6f, Outline, flip);
		DrawLine(anchor, gripStart, hand, 3f, new Color(44, 38, 55), flip);
		for (int wrap = 0; wrap < 3; wrap++)
		{
			Vector2 mark = gripStart + direction * (2f + (float)wrap * 2.5f);
			DrawLine(anchor, mark - perpendicular * 2f, mark + perpendicular * 2f, 1f, (wrap % 2 == 0) ? moonSilver : violet, flip);
		}
		Vector2 guardCenter = hand + direction;
		Vector2 upperGuard = guardCenter - perpendicular * 7f - direction * 2f;
		Vector2 lowerGuard = guardCenter + perpendicular * 6f - direction * 3f;
		DrawLine(anchor, upperGuard, guardCenter, 5f, Outline, flip);
		DrawLine(anchor, guardCenter, lowerGuard, 5f, Outline, flip);
		DrawLine(anchor, upperGuard + direction, guardCenter, 2f, moonSilver, flip);
		DrawLine(anchor, guardCenter, lowerGuard + direction, 2f, violet, flip);
		DrawBox(anchor, guardCenter.X - 2f, guardCenter.Y - 2f, 4f, 4f, new Color(226, 211, 244), flip);
		DrawLine(anchor, bladeRoot, firstCurve, 7f, Outline, flip);
		DrawLine(anchor, firstCurve, secondCurve, 6f, Outline, flip);
		DrawLine(anchor, secondCurve, tip, 4f, Outline, flip);
		DrawLine(anchor, bladeRoot - perpendicular * 1.2f, firstCurve - perpendicular * 1.2f, 3f, deepViolet, flip);
		DrawLine(anchor, firstCurve - perpendicular * 1.2f, secondCurve - perpendicular * 0.8f, 3f, blade, flip);
		DrawLine(anchor, secondCurve, tip, 2f, moonSilver, flip);
		DrawLine(anchor, bladeRoot + perpendicular * 1.5f, firstCurve + perpendicular * 1.1f, 1f, new Color(232, 226, 241), flip);
		DrawLine(anchor, firstCurve + perpendicular, tip, 1f, new Color(237, 226, 249), flip);
		if (channelStrength > 0.01f)
		{
			DrawLine(anchor, bladeRoot + direction * 3f, firstCurve, 1f, ScaleAlpha(new Color(213, 190, 243), channelStrength), flip);
			DrawLine(anchor, firstCurve, tip - direction * 4f, 1f, ScaleAlpha(new Color(235, 222, 252), channelStrength), flip);
			Vector2 rune = firstCurve + direction * 3f;
			DrawBox(anchor, rune.X - 1f, rune.Y - 1f, 3f, 3f, ScaleAlpha(new Color(231, 214, 251), channelStrength), flip);
		}
	}

	private void DrawQuiver(Vector2 anchor, bool flip, float bodyX, float bodyY)
	{
		DrawLine(anchor, new Vector2(bodyX - 13f, bodyY - 52f), new Vector2(bodyX - 21f, bodyY - 18f), 8f, Outline, flip);
		DrawLine(anchor, new Vector2(bodyX - 13f, bodyY - 51f), new Vector2(bodyX - 21f, bodyY - 19f), 5f, new Color(74, 46, 34), flip);
		for (int index = 0; index < 3; index++)
		{
			float x = bodyX - 17f + (float)index * 3f;
			DrawLine(anchor, new Vector2(x, bodyY - 58f), new Vector2(x - 7f, bodyY - 27f), 2f, new Color(151, 112, 67), flip);
		}
	}

	private void DrawChargeEffect(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		float charge = player.Combat.HeavyChargeProgress;
		float radius = 5f + charge * 11f;
		Vector2 center = ((player.WeaponFamily == WeaponFamily.HunterBow) ? new Vector2(pose.SwordHandX + 7f, pose.SwordHandY) : new Vector2(pose.SwordHandX + MathF.Cos(pose.SwordAngle) * (pose.SwordLength + 10f), pose.SwordHandY + MathF.Sin(pose.SwordAngle) * (pose.SwordLength + 10f)));
		Color color = ((player.WeaponFamily == WeaponFamily.HunterBow) ? ScaleAlpha(new Color(220, 190, 103), 0.35f + charge * 0.55f) : ScaleAlpha(new Color(174, 111, 224), 0.35f + charge * 0.6f));
		DrawLine(anchor, center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), 2f, color, flip);
		DrawLine(anchor, center - new Vector2(0f, radius), center + new Vector2(0f, radius), 2f, color, flip);
		DrawBox(anchor, center.X - 2f, center.Y - 2f, 5f, 5f, color, flip);
	}

	private void DrawSlashTrail(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		AttackDefinition attack = player.Combat.CurrentAttack;
		if (attack == null || (_animation.State != PlayerVisualState.LightAttack1 && _animation.State != PlayerVisualState.LightAttack2 && _animation.State != PlayerVisualState.LightAttack3 && _animation.State != PlayerVisualState.LightAttack4 && _animation.State != PlayerVisualState.LightAttack5 && _animation.State != PlayerVisualState.HeavyAttack) || attack.Delivery == AttackDelivery.Arrow || (player.WeaponFamily == WeaponFamily.DualSwords && DrawDuelistBladeTrails(player, anchor, flip, pose, attack)))
		{
			return;
		}
		if (player.IsKnightSwordAndShield)
		{
			WeaponTechniqueEffect knightEffect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
			bool shieldOnly = knightEffect is WeaponTechniqueEffect.KnightShieldGuard or
				WeaponTechniqueEffect.KnightShieldBash or
				WeaponTechniqueEffect.KnightFallingShield ||
				knightEffect == WeaponTechniqueEffect.KnightGuardCounter && player.Combat.TechniqueStageIndex == 0 ||
				knightEffect == WeaponTechniqueEffect.KnightShieldWall ||
				knightEffect == WeaponTechniqueEffect.KnightBladeBreaker && player.Combat.TechniqueStageIndex == 1 ||
				knightEffect == WeaponTechniqueEffect.KnightFinalOath &&
					player.Combat.TechniqueStageIndex is 0 or 1 or 5 or 7 or 8;
			if (shieldOnly)
			{
				return;
			}
			bool thrust = attack.HitboxShape == AttackHitboxShape.Thrust;
			if (thrust)
			{
				DrawThrustTrail(player, anchor, flip, pose, attack);
				return;
			}
			DrawKnightBladeTrail(player, anchor, flip, pose, attack,
				knightEffect);
			return;
		}
		if (attack.Delivery == AttackDelivery.ArcaneProjectile)
		{
			DrawArcaneCastFlash(player, anchor, flip, pose, attack);
			return;
		}
		if (player.WeaponFamily == WeaponFamily.Scythe)
		{
			DrawScytheBladeTrail(player, anchor, flip, pose, attack);
			return;
		}
		GetSlashProfile(player.WeaponFamily, attack.Kind, out var startAngle, out var endAngle, out var radius, out var duration, out var trailSpan, out var segments, out var weight);
		if (player.WeaponFamily == WeaponFamily.DualSwords)
		{
			if (attack.HitboxShape == AttackHitboxShape.Circular)
			{
				bool reverse = player.Combat.TechniqueStageIndex % 2 != 0;
				startAngle = (reverse ? 1.15f : (-2.15f));
				endAngle = (reverse ? (-2.1f) : 1.2f);
				radius = 43f;
				duration = 0.13f;
				trailSpan = 0.68f;
				segments = 12;
				weight = 0.72f;
			}
			else if (attack.HitboxShape == AttackHitboxShape.FrontalFan)
			{
				radius = 46f;
				duration = 0.105f;
				trailSpan = 0.5f;
				segments = 9;
				weight = 0.64f;
			}
			else if (attack.HitboxShape == AttackHitboxShape.Cross)
			{
				radius = 44f;
				duration = 0.14f;
				trailSpan = 0.62f;
				segments = 10;
				weight = 0.78f;
			}
			WeaponTechnique currentTechnique = player.Combat.CurrentTechnique;
			if (currentTechnique != null && currentTechnique.Effect == WeaponTechniqueEffect.DuelistInfiniteStarburst)
			{
				float intensity = 1f;
				radius += 4f + intensity * 7f;
				segments += 2;
				weight *= 1f + intensity * 0.38f;
			}
		}
		float effectStart = attack.StartupTime + attack.ActiveTime * 0.4f;
		float elapsed = player.Combat.StateElapsed - effectStart;
		if (elapsed < 0f || elapsed > duration)
		{
			return;
		}
		float travelDuration = duration * 0.64f;
		float headProgress = Math.Clamp(elapsed / travelDuration, 0f, 1f);
		float fade = ((elapsed <= travelDuration) ? Math.Clamp(elapsed / 0.025f, 0f, 1f) : (1f - Math.Clamp((elapsed - travelDuration) / (duration - travelDuration), 0f, 1f)));
		float tailProgress = MathF.Max(0f, headProgress - trailSpan);
		Vector2 center = new Vector2(pose.SwordHandX, pose.SwordHandY);
		DrawSlashArc(anchor, flip, center, startAngle, endAngle, radius, tailProgress, headProgress, segments, weight, fade);
		if (player.WeaponFamily == WeaponFamily.DualSwords)
		{
			DrawSlashArc(anchor, flip, center + new Vector2(-9f, 4f), 0f - endAngle, 0f - startAngle, radius - 5f, tailProgress, headProgress, Math.Max(5, segments - 2), weight * 0.72f, fade * 0.85f);
		}
		if (attack.Kind == AttackKind.Heavy)
		{
			DrawSlashArc(anchor, flip, center, startAngle - 0.08f, endAngle - 0.08f, radius - 6f, MathF.Max(0f, tailProgress - 0.08f), MathF.Max(0f, headProgress - 0.08f), segments - 2, weight * 0.55f, fade * 0.5f);
			if (headProgress >= 0.72f && fade > 0.45f)
			{
				float peakAngle = MathHelper.Lerp(startAngle, endAngle, headProgress);
				Vector2 peak = center + new Vector2(MathF.Cos(peakAngle), MathF.Sin(peakAngle)) * radius;
				DrawLine(anchor, peak - new Vector2(4f, 0f), peak + new Vector2(5f, 0f), 2f, ScaleAlpha(SlashCore, fade * 0.7f), flip);
				DrawLine(anchor, peak - new Vector2(0f, 4f), peak + new Vector2(0f, 5f), 2f, ScaleAlpha(SlashCore, fade * 0.7f), flip);
			}
		}
	}

	private void DrawKnightBladeTrail(DungeonAscendant.Player.Player player,
		Vector2 anchor, bool flip, Pose pose, AttackDefinition attack,
		WeaponTechniqueEffect effect)
	{
		int stage = player.Combat.TechniqueStageIndex;
		float startAngle = -1.05f;
		float endAngle = .28f;
		float radius = 37f;
		float trailSpan = .24f;
		int segments = 7;
		float weight = .56f;

		switch (effect)
		{
		case WeaponTechniqueEffect.KnightCombo when stage == 1:
			startAngle = .58f;
			endAngle = -1.02f;
			radius = 35f;
			break;
		case WeaponTechniqueEffect.KnightCombo when stage >= 3:
			startAngle = -1.58f;
			endAngle = .60f;
			radius = 39f;
			trailSpan = .27f;
			weight = .64f;
			break;
		case WeaponTechniqueEffect.KnightGuardCounter when stage == 1:
			startAngle = -1.22f;
			endAngle = .26f;
			radius = 36f;
			break;
		case WeaponTechniqueEffect.KnightGuardCounter:
			startAngle = .66f;
			endAngle = -1.02f;
			radius = 35f;
			break;
		case WeaponTechniqueEffect.KnightBladeBreaker:
			startAngle = -1.54f;
			endAngle = .46f;
			radius = 38f;
			trailSpan = .26f;
			break;
		case WeaponTechniqueEffect.KnightVanguardSlash:
			startAngle = -1.42f;
			endAngle = .12f;
			radius = 38f;
			trailSpan = .18f;
			segments = 6;
			weight = .48f;
			break;
		case WeaponTechniqueEffect.KnightFinalOath when stage == 2:
			startAngle = -1.20f;
			endAngle = .36f;
			break;
		case WeaponTechniqueEffect.KnightFinalOath when stage == 3:
			startAngle = .70f;
			endAngle = -1.08f;
			break;
		case WeaponTechniqueEffect.KnightFinalOath when stage == 6:
			startAngle = -1.64f;
			endAngle = .94f;
			radius = 41f;
			trailSpan = .28f;
			weight = .66f;
			break;
		}

		float duration = attack.ActiveTime +
			MathF.Min(.055f, attack.RecoveryTime);
		float elapsed = player.Combat.StateElapsed -
			(attack.StartupTime + attack.ActiveTime * .12f);
		if (elapsed < 0f || elapsed > duration)
			return;

		float travelDuration = duration * .70f;
		float head = Math.Clamp(elapsed / MathF.Max(.01f, travelDuration), 0f, 1f);
		float fade = elapsed <= travelDuration
			? Math.Clamp(elapsed / .025f, 0f, 1f)
			: 1f - Math.Clamp((elapsed - travelDuration) /
				MathF.Max(.01f, duration - travelDuration), 0f, 1f);
		float tail = MathF.Max(0f, head - trailSpan);
		Vector2 center = new(pose.SwordHandX, pose.SwordHandY);
		DrawSlashArc(anchor, flip, center, startAngle, endAngle, radius,
			tail, head, segments, weight, fade * .78f);
	}

	private void DrawScytheBladeTrail(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		switch (effect)
		{
		case WeaponTechniqueEffect.ScytheUmbraDevourer:
			DrawScytheUltimateBladeTrail(player, anchor, flip, pose, attack);
			return;
		case WeaponTechniqueEffect.ScytheShadowSlip:
			return;
		}
		int stage = player.Combat.TechniqueStageIndex;
		GetScytheMotionProfile(effect, stage, out var startAngle, out var endAngle, out var settleAngle, out var _, out var _, out var _);
		float motionTime = attack.ActiveTime + MathF.Min(0.16f, attack.RecoveryTime);
		float elapsed = player.Combat.StateElapsed - attack.StartupTime;
		if (elapsed < 0f || elapsed > motionTime)
		{
			return;
		}
		float head = Math.Clamp(elapsed / MathF.Max(0.01f, motionTime), 0f, 1f);
		float span = ((effect == WeaponTechniqueEffect.ScytheDeathSpiral) ? 0.25f : ((stage >= 4 || attack.Kind == AttackKind.Heavy) ? 0.36f : 0.29f));
		float tail = MathF.Max(0f, head - span);
		int segments = ((stage >= 4 || attack.Kind == AttackKind.Heavy) ? 12 : 9);
		float fade = ((head < 0.12f) ? (head / 0.12f) : (1f - Math.Clamp((head - 0.78f) / 0.22f, 0f, 1f)));
		Vector2 grip = new Vector2(pose.SwordHandX, pose.SwordHandY);
		bool strong = attack.Kind == AttackKind.Heavy || (effect == WeaponTechniqueEffect.ScytheReapersDance && stage >= 4) || (effect == WeaponTechniqueEffect.ScytheAbyssalRequiem && stage >= 4) || (effect == WeaponTechniqueEffect.ScytheEclipseWaltz && stage == 4);
		if ((uint)(effect - 112) <= 1u)
		{
			float delayedHead = MathF.Max(0f, head - 0.075f);
			float delayedTail = MathF.Max(0f, delayedHead - span * 0.82f);
			Vector2 shadowPrevious = GetScytheBladeTip(grip, EvaluateScytheTrailAngle(startAngle, endAngle, settleAngle, delayedTail), pose.SwordLength) + new Vector2(-3f, 2f);
			for (int index = 1; index <= segments; index++)
			{
				float sample = MathHelper.Lerp(delayedTail, delayedHead, (float)index / (float)segments);
				Vector2 next = GetScytheBladeTip(grip, EvaluateScytheTrailAngle(startAngle, endAngle, settleAngle, sample), pose.SwordLength) + new Vector2(-3f, 2f);
				float taper = MathF.Sin((float)index / (float)segments * (float)Math.PI);
				DrawLine(anchor, shadowPrevious, next, 3f + taper * 2f, ScaleAlpha(new Color(31, 8, 40), fade * taper * 0.58f), flip);
				shadowPrevious = next;
			}
		}
		Vector2 previous = GetScytheBladeTip(grip, EvaluateScytheTrailAngle(startAngle, endAngle, settleAngle, tail), pose.SwordLength);
		for (int i = 1; i <= segments; i++)
		{
			float local = (float)i / (float)segments;
			float sample2 = MathHelper.Lerp(tail, head, local);
			float angle = EvaluateScytheTrailAngle(startAngle, endAngle, settleAngle, sample2);
			Vector2 next2 = GetScytheBladeTip(grip, angle, pose.SwordLength);
			float taper2 = MathF.Sin(local * (float)Math.PI);
			float alpha = fade * (0.18f + taper2 * 0.72f) * local;
			DrawLine(anchor, previous, next2, 4f + taper2 * (strong ? 2f : 1f), ScaleAlpha(new Color(24, 8, 31), alpha * 0.72f), flip);
			DrawLine(anchor, previous, next2, 1.5f + taper2 * (strong ? 1.2f : 0.6f), ScaleAlpha(new Color(153, 72, 205), alpha), flip);
			if (strong && local > 0.55f)
			{
				DrawLine(anchor, previous, next2, 1f, ScaleAlpha(new Color(151, 42, 62), alpha * 0.52f), flip);
			}
			previous = next2;
		}
	}

	private static float EvaluateScytheTrailAngle(float start, float end, float settle, float progress)
	{
		if (progress <= 0.68f)
		{
			float strike = MathHelper.SmoothStep(0f, 1f, progress / 0.68f);
			return MathHelper.Lerp(start, end, strike);
		}
		float recovery = MathHelper.SmoothStep(0f, 1f, (progress - 0.68f) / 0.32f);
		float travelSign = MathF.Sign(end - start);
		return MathHelper.Lerp(end, settle, recovery) + travelSign * 0.18f * MathF.Sin(recovery * (float)Math.PI);
	}

	private static Vector2 GetScytheBladeTip(Vector2 grip, float angle, float shaftLength)
	{
		Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
		Vector2 perpendicular = new Vector2(0f - direction.Y, direction.X);
		return grip + direction * (shaftLength - 18f) + perpendicular * 27f;
	}

	private void DrawScytheUltimateBladeTrail(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		int stage = player.Combat.TechniqueStageIndex;
		bool flag;
		switch (stage)
		{
		case 1:
		case 2:
		case 3:
		case 7:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return;
		}
		if (1 == 0)
		{
		}
		(float, float, float) tuple = stage switch
		{
			0 => (-0.64f, -2.3f, -2.05f),
			4 => (-2.52f, 0.68f, 0.96f),
			5 => (0.86f, -2.42f, -2.7f),
			6 => (-2.28f, 0.82f, 1.06f),
			_ => (pose.SwordAngle, pose.SwordAngle, pose.SwordAngle),
		};
		if (1 == 0)
		{
		}
		(float, float, float) tuple2 = tuple;
		float start = tuple2.Item1;
		float end = tuple2.Item2;
		float settle = tuple2.Item3;
		float elapsed = player.Combat.StateElapsed - attack.StartupTime;
		float duration = attack.ActiveTime + MathF.Min((stage == 6) ? 0.24f : 0.15f, attack.RecoveryTime);
		if (elapsed < 0f || elapsed > duration)
		{
			return;
		}
		float head = Math.Clamp(elapsed / MathF.Max(0.01f, duration), 0f, 1f);
		float num;
		if (stage == 6)
		{
			num = 0.52f;
		}
		else
		{
			flag = (uint)(stage - 4) <= 1u;
			num = (flag ? 0.38f : 0.28f);
		}
		float span = num;
		float tail = MathF.Max(0f, head - span);
		int segments = ((stage == 6) ? 18 : 12);
		float fade = ((head < 0.1f) ? (head / 0.1f) : (1f - Math.Clamp((head - 0.82f) / 0.18f, 0f, 1f)));
		float reachScale = ((stage == 6) ? 1.2f : 1f);
		Vector2 grip = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 previous = grip + (GetScytheBladeTip(grip, EvaluateScytheTrailAngle(start, end, settle, tail), pose.SwordLength) - grip) * reachScale;
		Vector2 shadowPrevious = previous + new Vector2(-4f, 3f);
		for (int index = 1; index <= segments; index++)
		{
			float local = (float)index / (float)segments;
			float sample = MathHelper.Lerp(tail, head, local);
			Vector2 next = grip + (GetScytheBladeTip(grip, EvaluateScytheTrailAngle(start, end, settle, sample), pose.SwordLength) - grip) * reachScale;
			float taper = MathF.Sin(local * (float)Math.PI);
			Vector2 shadowNext = next + new Vector2(-4f, 3f);
			DrawLine(anchor, shadowPrevious, shadowNext, 5f + taper * ((stage == 6) ? 5f : 3f), ScaleAlpha(new Color(24, 6, 31), fade * taper * 0.72f), flip);
			DrawLine(anchor, previous, next, 2f + taper * ((stage == 6) ? 2.5f : 1.3f), ScaleAlpha(new Color(166, 91, 202), fade * taper), flip);
			if (stage == 6 && local > 0.58f)
			{
				DrawLine(anchor, previous, next, 1f, ScaleAlpha(new Color(151, 39, 61), fade * taper * 0.64f), flip);
			}
			previous = next;
			shadowPrevious = shadowNext;
		}
	}

	private void DrawSpellbladeTechniqueEffects(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.ArcaneWarStaff || player.Combat.CurrentTechnique == null || player.Combat.CurrentAttack == null)
		{
			return;
		}
		AttackDefinition attack = player.Combat.CurrentAttack;
		float activeElapsed = player.Combat.StateElapsed - attack.StartupTime;
		if (!(activeElapsed < -0.08f) && !(activeElapsed > attack.ActiveTime + 0.2f))
		{
			float fade = 1f - Math.Clamp(MathF.Max(0f, activeElapsed) / MathF.Max(0.01f, attack.ActiveTime + 0.2f), 0f, 1f);
			Color violet = ScaleAlpha(new Color(161, 132, 205), 0.2f + fade * 0.62f);
			Color arcaneWhite = ScaleAlpha(new Color(222, 218, 233), 0.16f + fade * 0.55f);
			Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
			Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
			Vector2 staffTip = hand + direction * (pose.SwordLength + 9f);
			WeaponTechniqueEffect effect = player.Combat.CurrentTechnique.Effect;
			if ((uint)(effect - 56) <= 1u)
			{
				DrawLine(anchor, hand + new Vector2(12f, 0f), hand + new Vector2(82f, 0f), 3f, violet, flip);
				DrawLine(anchor, hand + new Vector2(28f, 0f), hand + new Vector2(88f, 0f), 1f, arcaneWhite, flip);
			}
			else if ((uint)(effect - 59) <= 1u)
			{
				Vector2 ground = new Vector2(0f, -2f);
				DrawLine(anchor, ground - new Vector2(19f, 0f), ground + new Vector2(19f, 0f), 2f, violet, flip);
				DrawLine(anchor, ground - new Vector2(0f, 8f), ground + new Vector2(0f, 3f), 2f, arcaneWhite, flip);
				DrawLine(anchor, ground + new Vector2(-12f, 0f), ground + new Vector2(0f, -8f), 1f, violet, flip);
				DrawLine(anchor, ground + new Vector2(0f, -8f), ground + new Vector2(12f, 0f), 1f, violet, flip);
			}
			else
			{
				DrawLine(anchor, staffTip - direction * 17f, staffTip, 2f, violet, flip);
				DrawLine(anchor, staffTip - new Vector2(4f, 0f), staffTip + new Vector2(5f, 0f), 1f, arcaneWhite, flip);
			}
		}
	}

	private void DrawKnightTechniqueEffects(DungeonAscendant.Player.Player player,
		Vector2 anchor, bool flip, Pose pose)
	{
		if (!player.IsKnightSwordAndShield)
		{
			return;
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		if (effect == WeaponTechniqueEffect.KnightFallingShield && player.Combat.KnightLandingPulse > 0f)
		{
			float fade = Math.Clamp(player.Combat.KnightLandingPulse / 0.30f, 0f, 1f);
			float expansion = 1f - fade;
			Color dust = ScaleAlpha(new Color(126, 116, 99), fade * 0.72f);
			Color air = ScaleAlpha(new Color(164, 181, 185), fade * 0.62f);
			float radius = 15f + expansion * 18f;
			DrawLine(anchor, new Vector2(-radius, -2f), new Vector2(-5f, -3f), 2f, dust, flip);
			DrawLine(anchor, new Vector2(5f, -3f), new Vector2(radius, -2f), 2f, dust, flip);
			DrawLine(anchor, new Vector2(-radius * 0.65f, -5f), new Vector2(-3f, -7f), 1f, air, flip);
			DrawLine(anchor, new Vector2(3f, -7f), new Vector2(radius * 0.65f, -5f), 1f, air, flip);
			DrawBox(anchor, -11f - expansion * 7f, -7f, 4f, 3f, dust, flip);
			DrawBox(anchor, 12f + expansion * 6f, -6f, 3f, 2f, dust, flip);
		}
		if (effect == WeaponTechniqueEffect.KnightShieldWall &&
			player.Combat.TechniqueStageIndex == 2 && player.Combat.IsAttackActive)
		{
			Color force = new Color(181, 196, 198, 145);
			DrawLine(anchor, new Vector2(pose.ShieldX + 18f, pose.ShieldY + 8f),
				new Vector2(pose.ShieldX + 30f, pose.ShieldY + 6f), 2f, force, flip);
			DrawLine(anchor, new Vector2(-12f, -3f), new Vector2(24f, -3f), 2f,
				new Color(126, 116, 99, 120), flip);
		}
		if (effect == WeaponTechniqueEffect.KnightVanguardSlash && player.Combat.IsAttackActive)
		{
			Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
			Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
			Color edge = new Color(151, 190, 205, 135);
			DrawLine(anchor, hand + direction * 12f,
				hand + direction * (pose.SwordLength + 7f), 1f, edge, flip);
			DrawLine(anchor, new Vector2(-17f, -3f), new Vector2(-7f, -3f), 1f,
				new Color(126, 116, 99, 90), flip);
		}
		if (effect == WeaponTechniqueEffect.KnightFinalOath)
		{
			int stage = player.Combat.TechniqueStageIndex;
			AttackDefinition attack = player.Combat.CurrentAttack;
			float stageRatio = attack == null ? 0f : Math.Clamp(
				player.Combat.StateElapsed / MathF.Max(.01f, attack.TotalTime), 0f, 1f);
			Color steel = ScaleAlpha(new Color(190, 211, 218),
				.30f + MathF.Sin(stageRatio * MathHelper.Pi) * .35f);
			Color dust = ScaleAlpha(new Color(126, 116, 99),
				.25f + MathF.Sin(stageRatio * MathHelper.Pi) * .30f);
			Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
			Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle),
				MathF.Sin(pose.SwordAngle));
			if (stage == 0)
			{
				DrawLine(anchor, new Vector2(pose.ShieldX + 12f, pose.ShieldY + 5f),
					new Vector2(pose.ShieldX + 21f, pose.ShieldY + 5f), 1f,
					steel, flip);
				DrawLine(anchor, new Vector2(-12f, -3f), new Vector2(13f, -3f),
					1f, dust, flip);
			}
			else if (stage == 1)
			{
				DrawLine(anchor, new Vector2(pose.ShieldX + 14f, pose.ShieldY + 3f),
					new Vector2(pose.ShieldX + 31f, pose.ShieldY + 2f), 2f,
					steel, flip);
				DrawLine(anchor, new Vector2(-26f, -3f), new Vector2(-8f, -3f),
					2f, dust, flip);
			}
			else if (stage is >= 2 and <= 4)
			{
				DrawLine(anchor, hand + direction * 17f,
					hand + direction * (pose.SwordLength + 5f), 1f, steel, flip);
			}
			else if (stage == 5)
			{
				DrawLine(anchor, new Vector2(-10f, -2f), new Vector2(-18f, 7f),
					2f, dust, flip);
				DrawLine(anchor, new Vector2(8f, -4f), new Vector2(14f, 8f),
					1f, steel, flip);
			}
			else if (stage == 6)
			{
				DrawLine(anchor, hand + direction * 8f,
					hand + direction * (pose.SwordLength + 8f), 2f, steel, flip);
			}
			if (stage is 7 or 8 && player.Combat.KnightLandingPulse > 0f)
			{
				float fade = Math.Clamp(player.Combat.KnightLandingPulse / .30f, 0f, 1f);
				float radius = 13f + (1f - fade) * 21f;
				Color landingDust = ScaleAlpha(new Color(126, 116, 99), fade * .70f);
				Color landingAir = ScaleAlpha(new Color(164, 181, 185), fade * .52f);
				DrawLine(anchor, new Vector2(-radius, -2f), new Vector2(-5f, -3f),
					2f, landingDust, flip);
				DrawLine(anchor, new Vector2(5f, -3f), new Vector2(radius, -2f),
					2f, landingDust, flip);
				DrawLine(anchor, new Vector2(-radius * .55f, -6f),
					new Vector2(radius * .55f, -6f), 1f, landingAir, flip);
			}
		}
	}

	private void DrawGreatswordTechniqueEffects(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		if (player.WeaponFamily != WeaponFamily.GreatSword)
		{
			return;
		}
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 bladeEnd = hand + direction * pose.SwordLength;
		if (player.Combat.IsFinalVerdictReady)
		{
			float pulse = 0.35f + (MathF.Sin(player.Combat.StateElapsed * 8f) + 1f) * 0.16f;
			Color readiness = ScaleAlpha(new Color(214, 190, 126), pulse);
			DrawLine(anchor, bladeEnd - direction * 9f, bladeEnd, 2f, readiness, flip);
		}
		if (player.Combat.GreatswordLandingPulse > 0f)
		{
			float fade = Math.Clamp(player.Combat.GreatswordLandingPulse / 0.32f, 0f, 1f);
			Color dust = ScaleAlpha(new Color(135, 118, 91), fade);
			DrawLine(anchor, new Vector2(-7f, -2f), new Vector2(25f, -3f), 3f, dust, flip);
			DrawBox(anchor, 8f, -9f, 5f, 4f, dust, flip);
			DrawBox(anchor, 21f, -6f, 4f, 3f, dust, flip);
		}
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		bool flag = (((uint)(effect - 90) <= 1u || effect == WeaponTechniqueEffect.GreatswordKingsRupture) ? true : false);
		if (flag && player.Combat.IsAttackActive)
		{
			DrawLine(color: new Color(211, 178, 103, 190), anchor: anchor, localStart: bladeEnd - new Vector2(8f, 0f), localEnd: bladeEnd + new Vector2(7f, 0f), thickness: 2f, flip: flip);
		}
		if (player.Combat.IsGreatswordCharging)
		{
			float intensity = 0.25f + player.Combat.GreatswordChargeRatio * 0.65f;
			Color force = ScaleAlpha(new Color(220, 182, 91), intensity);
			DrawLine(anchor, hand, bladeEnd, 2f, force, flip);
			DrawBox(anchor, -12f, -5f, 24f, 3f, force, flip);
			if (player.Combat.GreatswordChargeLevel >= 2 || player.Combat.GreatswordChargeRatio >= 0.48f)
			{
				DrawLine(anchor, new Vector2(-18f, -9f), new Vector2(17f, -11f), 2f, force, flip);
			}
		}
		if (effect == WeaponTechniqueEffect.GreatswordKingsRupture && player.Combat.TechniqueStageIndex == 2 && player.Combat.IsAttackActive)
		{
			Color fracture = new Color(154, 126, 78, 175);
			DrawLine(anchor, new Vector2(8f, -2f), new Vector2(72f, -3f), 4f, fracture, flip);
			DrawLine(anchor, new Vector2(28f, -3f), new Vector2(38f, -13f), 2f, fracture, flip);
			DrawLine(anchor, new Vector2(51f, -3f), new Vector2(63f, -10f), 2f, fracture, flip);
		}
		if (effect == WeaponTechniqueEffect.GreatswordFinalVerdict && player.Combat.TechniqueStageIndex == 3 && player.Combat.IsAttackActive)
		{
			Color spectral = new Color(236, 205, 120, 150);
			Vector2 spectralStart = hand - new Vector2(7f, 10f);
			Vector2 spectralEnd = spectralStart + direction * 82f;
			DrawLine(anchor, spectralStart, spectralEnd, 12f, spectral, flip);
			DrawLine(anchor, spectralStart, spectralEnd, 4f, new Color(255, 239, 184, 205), flip);
		}
		if (effect == WeaponTechniqueEffect.GreatswordFinalVerdict && player.Combat.TechniqueStageIndex == 4 && player.Combat.IsAttackActive)
		{
			Color impact = new Color(196, 165, 91, 170);
			DrawLine(anchor, new Vector2(-34f, -2f), new Vector2(38f, -2f), 5f, impact, flip);
			DrawLine(anchor, new Vector2(-18f, -3f), new Vector2(-28f, -17f), 3f, impact, flip);
			DrawLine(anchor, new Vector2(17f, -3f), new Vector2(31f, -14f), 3f, impact, flip);
		}
	}

	private void DrawThrustTrail(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		float elapsed = player.Combat.StateElapsed - attack.StartupTime;
		if (!(elapsed < 0f) && !(elapsed > attack.ActiveTime + 0.08f))
		{
			float fade = 1f - Math.Clamp(elapsed / (attack.ActiveTime + 0.08f), 0f, 1f);
			Vector2 start = new Vector2(pose.SwordHandX + 25f, pose.SwordHandY);
			Vector2 end = start + new Vector2(55f + (attack.IsHeavy ? 25f : 0f), 0f);
			DrawLine(anchor, start, end, attack.IsHeavy ? 5f : 3f, ScaleAlpha(SlashEdge, fade), flip);
			DrawLine(anchor, start + new Vector2(8f, 0f), end, 1f, ScaleAlpha(SlashCore, fade), flip);
		}
	}

	private void DrawArcaneCastFlash(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		WeaponTechniqueEffect effect = player.Combat.CurrentTechnique?.Effect ?? WeaponTechniqueEffect.None;
		if ((effect == WeaponTechniqueEffect.AncientStaffArcaneBoltChain || effect == WeaponTechniqueEffect.AncientStaffBlinkCast) ? true : false)
		{
			DrawAncientStaffCastRune(player, anchor, flip, pose, attack);
			return;
		}
		float elapsed = player.Combat.StateElapsed - attack.StartupTime;
		if (!(elapsed < 0f) && !(elapsed > attack.ActiveTime + 0.12f))
		{
			float fade = 1f - Math.Clamp(elapsed / (attack.ActiveTime + 0.12f), 0f, 1f);
			Vector2 center = new Vector2(pose.SwordHandX + MathF.Cos(pose.SwordAngle) * (pose.SwordLength + 10f), pose.SwordHandY + MathF.Sin(pose.SwordAngle) * (pose.SwordLength + 10f));
			float radius = 10f + (1f - fade) * 12f;
			Color color = ScaleAlpha(new Color(183, 119, 232), fade);
			DrawLine(anchor, center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), 3f, color, flip);
			DrawLine(anchor, center - new Vector2(0f, radius), center + new Vector2(0f, radius), 3f, color, flip);
			DrawLine(anchor, center - new Vector2(radius * 0.7f), center + new Vector2(radius * 0.7f), 2f, color, flip);
		}
	}

	private void DrawAncientStaffCastRune(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose, AttackDefinition attack)
	{
		WeaponTechnique currentTechnique = player.Combat.CurrentTechnique;
		int stage = ((currentTechnique != null && currentTechnique.Effect == WeaponTechniqueEffect.AncientStaffBlinkCast) ? 3 : Math.Clamp(player.Combat.TechniqueStageIndex, 0, 4));
		float elapsed = player.Combat.StateElapsed;
		float draw = Math.Clamp(elapsed / MathF.Max(0.01f, attack.StartupTime), 0f, 1f);
		float afterRelease = MathF.Max(0f, elapsed - attack.StartupTime);
		float dissolve = 1f - Math.Clamp(afterRelease / MathF.Max(0.06f, attack.ActiveTime + 0.1f), 0f, 1f);
		float visibility = ((elapsed < attack.StartupTime) ? MathHelper.SmoothStep(0f, 1f, draw) : dissolve);
		Vector2 staffDirection = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 center = new Vector2(pose.SwordHandX, pose.SwordHandY) + staffDirection * (pose.SwordLength + 8f);
		Color blue = ScaleAlpha(new Color(82, 166, 240), 0.25f + visibility * 0.68f);
		Color gold = ScaleAlpha(new Color(233, 190, 78), 0.18f + visibility * 0.72f);
		Color purple = ScaleAlpha(new Color(132, 82, 201), 0.14f + visibility * 0.52f);
		float radius = 6f + (float)stage * 2.6f;
		WeaponTechnique currentTechnique2 = player.Combat.CurrentTechnique;
		if (currentTechnique2 != null && currentTechnique2.Effect == WeaponTechniqueEffect.AncientStaffBlinkCast)
		{
			radius = 14f;
		}
		if (draw < 0.12f)
		{
			DrawBox(anchor, center.X - 1f, center.Y - 1f, 3f, 3f, gold, flip);
			return;
		}
		DrawLocalBrokenRune(anchor, flip, center, radius, Math.Clamp((draw - 0.1f) / 0.68f, 0f, 1f), -0.35f + (float)stage * 0.23f, blue, gold, 5 + stage);
		if (stage >= 1)
		{
			DrawLocalBrokenRune(anchor, flip, center, radius * 0.58f, Math.Clamp((draw - 0.28f) / 0.52f, 0f, 1f), 0.7f - (float)stage * 0.18f, purple, gold, 4 + stage);
		}
		Vector2 freeHand = new Vector2(pose.OffHandX, pose.OffHandY);
		float handGlyph = stage switch
		{
			4 => 9f,
			2 => 7f,
			_ => 4f,
		};
		DrawLine(anchor, freeHand + new Vector2(0f - handGlyph, 0f), freeHand + new Vector2(0f, 0f - handGlyph), 1f, gold, flip);
		DrawLine(anchor, freeHand + new Vector2(0f, 0f - handGlyph), freeHand + new Vector2(handGlyph, 0f), 1f, blue, flip);
		switch (stage)
		{
		case 2:
			DrawLocalBrokenRune(anchor, flip, center + new Vector2(0f, -11f), 6f, draw, 0.35f, gold, blue, 5);
			DrawLocalBrokenRune(anchor, flip, center + new Vector2(0f, 11f), 6f, draw, -0.48f, blue, gold, 5);
			break;
		case 4:
		{
			Vector2 rear = new Vector2(pose.SwordHandX - 5f, pose.SwordHandY);
			DrawLocalBrokenRune(anchor, flip, rear, 19f, draw, -0.72f, purple, gold, 9);
			break;
		}
		}
		if (afterRelease > 0f)
		{
			float fracture = 1f - dissolve;
			for (int shard = 0; shard < Math.Min(5, stage + 2); shard++)
			{
				float angle = -0.9f + (float)shard * 0.47f;
				Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
				Vector2 start = center + direction * radius;
				DrawLine(anchor, start, start + direction * (3f + fracture * 8f), 1f, (shard % 2 == 0) ? gold : blue, flip);
			}
		}
	}

	private void DrawLocalBrokenRune(Vector2 anchor, bool flip, Vector2 center, float radius, float completion, float rotation, Color primary, Color accent, int segments)
	{
		completion = Math.Clamp(completion, 0f, 1f);
		int visible = Math.Clamp((int)MathF.Ceiling((float)segments * completion), 0, segments);
		for (int index = 0; index < visible; index++)
		{
			float startAngle = rotation + (float)Math.PI * 2f * (float)index / (float)segments + 0.055f;
			float endAngle = rotation + (float)Math.PI * 2f * ((float)index + 0.64f) / (float)segments;
			Vector2 start = center + new Vector2(MathF.Cos(startAngle) * radius, MathF.Sin(startAngle) * radius * 0.72f);
			Vector2 end = center + new Vector2(MathF.Cos(endAngle) * radius, MathF.Sin(endAngle) * radius * 0.72f);
			DrawLine(anchor, start, end, (index % 3 == 0) ? 2f : 1f, (index % 2 == 0) ? primary : accent, flip);
			if (index % 2 == 0)
			{
				Vector2 mark = center + new Vector2(MathF.Cos(startAngle) * (radius + 3f), MathF.Sin(startAngle) * (radius + 3f) * 0.72f);
				DrawLine(anchor, mark, mark + new Vector2(MathF.Cos(startAngle) * 4f, MathF.Sin(startAngle) * 3f), 1f, accent, flip);
			}
		}
	}

	private void DrawSlashArc(Vector2 anchor, bool flip, Vector2 center, float startAngle, float endAngle, float radius, float tailProgress, float headProgress, int segments, float weight, float fade)
	{
		if (headProgress <= tailProgress || fade <= 0f)
		{
			return;
		}
		for (int index = 0; index < segments; index++)
		{
			float segmentStart = (float)index / (float)segments;
			float segmentEnd = (float)(index + 1) / (float)segments;
			float progressStart = MathHelper.Lerp(tailProgress, headProgress, segmentStart);
			float progressEnd = MathHelper.Lerp(tailProgress, headProgress, segmentEnd);
			float angleStart = MathHelper.Lerp(startAngle, endAngle, progressStart);
			float angleEnd = MathHelper.Lerp(startAngle, endAngle, progressEnd);
			Vector2 pointStart = center + new Vector2(MathF.Cos(angleStart), MathF.Sin(angleStart)) * radius;
			Vector2 pointEnd = center + new Vector2(MathF.Cos(angleEnd), MathF.Sin(angleEnd)) * radius;
			float middle = ((float)index + 0.5f) / (float)segments;
			float taper = MathF.Sin(middle * (float)Math.PI);
			float edgeThickness = 1f + 4f * taper * weight;
			float bodyThickness = 1f + 2.2f * taper * weight;
			DrawLine(anchor, pointStart, pointEnd, edgeThickness, ScaleAlpha(SlashEdge, fade), flip);
			DrawLine(anchor, pointStart, pointEnd, bodyThickness, ScaleAlpha(SlashBody, fade), flip);
			if (taper > 0.42f)
			{
				DrawLine(anchor, pointStart, pointEnd, 1f, ScaleAlpha(SlashCore, fade * taper), flip);
			}
		}
	}

	private static void GetSlashProfile(WeaponFamily family, AttackKind kind, out float startAngle, out float endAngle, out float radius, out float duration, out float trailSpan, out int segments, out float weight)
	{
		if (family == WeaponFamily.GreatSword)
		{
			if (1 == 0)
			{
			}
			float num = kind switch
			{
				AttackKind.LightTwo => 0.78f,
				AttackKind.LightThree => 0.72f,
				_ => -2.05f,
			};
			if (1 == 0)
			{
			}
			startAngle = num;
			if (1 == 0)
			{
			}
			num = kind switch
			{
				AttackKind.LightTwo => -1.92f,
				AttackKind.LightThree => -1.55f,
				_ => 1.02f,
			};
			if (1 == 0)
			{
			}
			endAngle = num;
			radius = ((kind == AttackKind.Heavy) ? 66f : 58f);
			duration = ((kind == AttackKind.Heavy) ? 0.3f : 0.22f);
			trailSpan = 0.86f;
			segments = 16;
			weight = 1.35f;
			return;
		}
		switch (family)
		{
		case WeaponFamily.DualSwords:
		{
			bool flag = ((kind == AttackKind.LightTwo || kind == AttackKind.LightFour) ? true : false);
			startAngle = (flag ? 0.65f : (-1.02f));
			flag = ((kind == AttackKind.LightTwo || kind == AttackKind.LightFour) ? true : false);
			endAngle = (flag ? (-0.78f) : 0.55f);
			radius = ((kind == AttackKind.Heavy) ? 34f : 29f);
			duration = ((kind == AttackKind.Heavy) ? 0.18f : 0.09f);
			trailSpan = 0.48f;
			segments = 7;
			weight = 0.58f;
			return;
		}
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
			endAngle = 0.9f;
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

	public void DrawHitImpact(DungeonAscendant.Player.Player player, GameTime gameTime, int hitEffectId, Vector2 hitPosition, WeaponTechniqueEffect techniqueEffect = WeaponTechniqueEffect.None, int techniqueStage = -1)
	{
		if (hitEffectId != _observedHitEffectId)
		{
			_observedHitEffectId = hitEffectId;
			_hitEffectPosition = hitPosition;
			float hitEffectDuration;
			if ((techniqueEffect == WeaponTechniqueEffect.AncientStaffArcaneBoltChain || techniqueEffect == WeaponTechniqueEffect.AncientStaffRunePrison || techniqueEffect == WeaponTechniqueEffect.AncientStaffBlinkCast) ? true : false)
			{
				hitEffectDuration = ((techniqueStage >= 4) ? 0.2f : 0.15f);
			}
			else if (techniqueEffect == WeaponTechniqueEffect.KnightFinalOath)
			{
				hitEffectDuration = techniqueStage == 6 ? .20f :
					techniqueStage == 1 ? .15f : .12f;
			}
			else
			{
				AttackDefinition currentAttack = player.Combat.CurrentAttack;
				hitEffectDuration = ((currentAttack != null && currentAttack.Kind == AttackKind.Heavy) ? 0.16f : 0.11f);
			}
			_hitEffectDuration = hitEffectDuration;
			_hitEffectTimeRemaining = _hitEffectDuration;
		}
		else
		{
			_hitEffectTimeRemaining = MathF.Max(0f, _hitEffectTimeRemaining - MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f));
		}
		if (_hitEffectTimeRemaining <= 0f)
		{
			return;
		}
		bool flip = player.Facing == FacingDirection.Left;
		float progress = ((_hitEffectDuration <= 0f) ? 0f : (_hitEffectTimeRemaining / _hitEffectDuration));
		float spread = 3f + (1f - progress) * 5f;
		Vector2 anchor = new Vector2(MathF.Round(_hitEffectPosition.X), MathF.Round(_hitEffectPosition.Y));
		if ((techniqueEffect == WeaponTechniqueEffect.AncientStaffArcaneBoltChain || techniqueEffect == WeaponTechniqueEffect.AncientStaffRunePrison || techniqueEffect == WeaponTechniqueEffect.AncientStaffBlinkCast) ? true : false)
		{
			DrawAncientStaffHitImpact(anchor, flip, techniqueEffect, techniqueStage, progress);
			return;
		}
		if ((techniqueEffect == WeaponTechniqueEffect.ScytheReapersDance || (uint)(techniqueEffect - 107) <= 7u) ? true : false)
		{
			DrawScytheHitImpact(anchor, flip, techniqueEffect, techniqueStage, progress);
			return;
		}
		bool flag;
		switch (techniqueEffect)
		{
		case WeaponTechniqueEffect.DuelistStarburstSwordArt:
		case WeaponTechniqueEffect.DuelistPhantomRiposte:
		case WeaponTechniqueEffect.DuelistCrossRequiem:
		case WeaponTechniqueEffect.DuelistThousandEdgeWaltz:
		case WeaponTechniqueEffect.DuelistHeavenSplitter:
		case WeaponTechniqueEffect.DuelistFallingStar:
		case WeaponTechniqueEffect.DuelistFlashBlade:
		case WeaponTechniqueEffect.DuelistInfiniteStarburst:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			DrawDuelistHitImpact(anchor, flip, techniqueEffect, techniqueStage, progress);
			return;
		}
		if (techniqueEffect == WeaponTechniqueEffect.KnightFinalOath &&
			techniqueStage is 2 or 3 or 4 or 6)
		{
			DrawKnightUltimateSwordImpact(anchor, flip, progress, techniqueStage);
			return;
		}
		bool knightShieldImpact = techniqueEffect == WeaponTechniqueEffect.KnightShieldBash ||
			techniqueEffect == WeaponTechniqueEffect.KnightGuardCounter && techniqueStage == 0 ||
			techniqueEffect == WeaponTechniqueEffect.KnightShieldWall && techniqueStage is 1 or 2 ||
			techniqueEffect == WeaponTechniqueEffect.KnightBladeBreaker && techniqueStage == 1 ||
			techniqueEffect == WeaponTechniqueEffect.KnightFallingShield ||
			techniqueEffect == WeaponTechniqueEffect.KnightFinalOath && techniqueStage is 1 or 7;
		if (knightShieldImpact)
		{
			DrawKnightShieldImpact(anchor, flip, progress, techniqueEffect, techniqueStage);
			return;
		}
		Color spark = ScaleAlpha(HitSpark, Math.Clamp(progress * 1.4f, 0f, 1f));
		Color glint = ScaleAlpha(SlashCore, Math.Clamp(progress * 1.2f, 0f, 1f));
		DrawBox(anchor, -2f, -2f, 5f, 5f, spark, flip);
		DrawLine(anchor, new Vector2(0f - spread, 0f), new Vector2(spread, 0f), 2f, glint, flip);
		DrawLine(anchor, new Vector2(0f, 0f - spread), new Vector2(0f, spread), 2f, glint, flip);
		DrawLine(anchor, new Vector2((0f - spread) * 0.65f, (0f - spread) * 0.65f), new Vector2(spread * 0.65f, spread * 0.65f), 1f, spark, flip);
	}

	private void DrawKnightShieldImpact(Vector2 anchor, bool flip, float remaining,
		WeaponTechniqueEffect effect, int stage)
	{
		float expansion = 1f - remaining;
		Color steel = ScaleAlpha(new Color(211, 224, 227), remaining);
		Color blue = ScaleAlpha(new Color(104, 157, 181), remaining * 0.85f);
		Color dust = ScaleAlpha(new Color(121, 112, 96), remaining * 0.55f);
		bool forcePulse = effect == WeaponTechniqueEffect.KnightShieldWall && stage == 2;
		bool groundImpact = effect == WeaponTechniqueEffect.KnightFallingShield ||
			effect == WeaponTechniqueEffect.KnightFinalOath && stage == 7;
		bool strongContact = effect is WeaponTechniqueEffect.KnightGuardCounter or
			WeaponTechniqueEffect.KnightShieldWall ||
			effect == WeaponTechniqueEffect.KnightFinalOath && stage == 1;
		if (forcePulse || groundImpact)
		{
			float radius = (groundImpact ? 11f : 8f) + expansion * (groundImpact ? 18f : 13f);
			DrawLine(anchor, new Vector2(-radius, 1f), new Vector2(-4f, 0f),
				groundImpact ? 3f : 2f, dust, flip);
			DrawLine(anchor, new Vector2(4f, 0f), new Vector2(radius, 1f),
				groundImpact ? 3f : 2f, dust, flip);
			DrawLine(anchor, new Vector2(-radius * 0.65f, -4f),
				new Vector2(radius * 0.65f, -4f), 1f, blue, flip);
		}
		DrawBox(anchor, -2f, -2f, 5f, 5f, steel, flip);
		DrawLine(anchor, new Vector2(-3f, -2f),
			new Vector2(-8f - expansion * (strongContact ? 7f : 4f), -7f), 1f, blue, flip);
		DrawLine(anchor, new Vector2(3f, -1f),
			new Vector2(9f + expansion * (strongContact ? 8f : 5f), -4f), strongContact ? 2f : 1f, steel, flip);
		DrawLine(anchor, new Vector2(1f, 3f),
			new Vector2(6f + expansion * 3f, 8f), 1f, blue, flip);
		DrawBox(anchor, -5f - expansion * 5f, 5f + expansion * 2f,
			3f, 2f, dust, flip);
		DrawBox(anchor, 5f + expansion * 4f, 6f + expansion * 2f,
			2f, 2f, dust, flip);
	}

	private void DrawKnightUltimateSwordImpact(Vector2 anchor, bool flip,
		float remaining, int stage)
	{
		float expansion = 1f - remaining;
		float strength = stage == 6 ? 1f : .55f + (stage - 2) * .10f;
		Color steel = ScaleAlpha(new Color(225, 235, 237), remaining * strength);
		Color blue = ScaleAlpha(new Color(112, 161, 181), remaining * strength * .78f);
		Color dust = ScaleAlpha(new Color(126, 116, 99),
			remaining * (stage == 6 ? .62f : .28f));
		float reach = (stage == 6 ? 18f : 10f) + expansion *
			(stage == 6 ? 15f : 8f);
		DrawBox(anchor, -2f, -2f, stage == 6 ? 6f : 4f,
			stage == 6 ? 6f : 4f, steel, flip);
		if (stage == 3)
		{
			DrawLine(anchor, new Vector2(-reach, 5f), new Vector2(reach, -5f),
				stage == 6 ? 3f : 2f, steel, flip);
		}
		else
		{
			DrawLine(anchor, new Vector2(-reach, -6f), new Vector2(reach, 6f),
				stage == 6 ? 3f : 2f, steel, flip);
		}
		DrawLine(anchor, new Vector2(-reach * .55f, 2f),
			new Vector2(reach * .55f, -2f), 1f, blue, flip);
		if (stage == 6)
		{
			DrawLine(anchor, new Vector2(0f, -reach), new Vector2(0f, reach * .65f),
				2f, blue, flip);
			DrawLine(anchor, new Vector2(-reach * .85f, 7f),
				new Vector2(-4f, 3f), 2f, dust, flip);
			DrawLine(anchor, new Vector2(4f, 3f),
				new Vector2(reach * .85f, 7f), 2f, dust, flip);
		}
	}

	private void DrawDuelistHitImpact(Vector2 anchor, bool flip, WeaponTechniqueEffect effect, int stage, float remaining)
	{
		bool flag = effect == WeaponTechniqueEffect.DuelistStarburstSwordArt;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = (uint)(stage - 6) <= 1u;
			flag2 = flag3;
		}
		bool flag4 = flag2 || (effect == WeaponTechniqueEffect.DuelistFlashBlade && stage == 1) || (effect == WeaponTechniqueEffect.DuelistCrossRequiem && stage == 3) || (effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz && stage == 6);
		bool flag5 = flag4;
		if (!flag5)
		{
			bool flag3 = (uint)(effect - 17) <= 1u;
			flag5 = flag3;
		}
		bool flag6 = flag5;
		if (!flag6)
		{
			bool flag7 = effect == WeaponTechniqueEffect.DuelistInfiniteStarburst;
			bool flag8 = flag7;
			if (flag8)
			{
				int num = stage % 8;
				bool flag3 = (uint)(num - 6) <= 1u;
				flag8 = flag3 || stage >= 32;
			}
			flag6 = flag8;
		}
		bool cross = flag6;
		bool flag9 = effect == WeaponTechniqueEffect.DuelistStarburstSwordArt;
		bool flag10 = flag9;
		if (flag10)
		{
			bool flag3;
			switch (stage)
			{
			case 1:
			case 3:
			case 5:
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag10 = flag3;
		}
		bool flag11 = flag10 || (effect == WeaponTechniqueEffect.DuelistCrossRequiem && stage == 1);
		bool flag12 = flag11;
		if (!flag12)
		{
			bool flag13 = effect == WeaponTechniqueEffect.DuelistThousandEdgeWaltz;
			bool flag14 = flag13;
			if (flag14)
			{
				bool flag3 = ((stage == 0 || (uint)(stage - 3) <= 1u) ? true : false);
				flag14 = flag3;
			}
			flag12 = flag14;
		}
		bool lunar = flag12 || (effect == WeaponTechniqueEffect.DuelistFlashBlade && stage == 1) || (effect == WeaponTechniqueEffect.DuelistInfiniteStarburst && stage < 32 && stage % 2 != 0);
		bool major = effect switch
		{
			WeaponTechniqueEffect.DuelistInfiniteStarburst => stage >= 32,
			WeaponTechniqueEffect.DuelistHeavenSplitter => true,
			_ => false,
		};
		float expansion = 1f - remaining;
		float reach = (major ? 12f : (cross ? 8f : 5f)) + expansion * (major ? 7f : (cross ? 4f : 2f));
		Color primary = ScaleAlpha(lunar ? new Color(211, 190, 241) : new Color(194, 240, 247), remaining);
		Color accent = ScaleAlpha(lunar ? new Color(133, 94, 183) : new Color(82, 177, 198), remaining * 0.78f);
		Vector2 direction = Vector2.Normalize(new Vector2(1f, lunar ? (-0.62f) : 0.55f));
		DrawLine(anchor, -direction * reach, direction * reach, major ? 3f : (cross ? 2f : 1f), primary, flip);
		if (cross)
		{
			Vector2 opposite = new Vector2(direction.X, 0f - direction.Y);
			DrawLine(anchor, -opposite * reach, opposite * reach, major ? 3f : 2f, accent, flip);
			DrawDuelistStarFlash(anchor, flip, Vector2.Zero, major ? 9f : ((stage == 7) ? 7f : 5f), remaining);
			if (major)
			{
				DrawDuelistConstellation(anchor, flip, new Vector2(8f, -7f), remaining * 0.72f);
			}
		}
		else
		{
			DrawBox(anchor, -1f, -1f, 3f, 3f, accent, flip);
		}
	}

	private void DrawScytheHitImpact(Vector2 anchor, bool flip, WeaponTechniqueEffect effect, int stage, float remaining)
	{
		if (effect == WeaponTechniqueEffect.ScytheUmbraDevourer)
		{
			DrawScytheUltimateHitImpact(anchor, flip, stage, remaining);
			return;
		}
		float expansion = 1f - remaining;
		bool reverse = stage % 2 != 0;
		bool strong = effect == WeaponTechniqueEffect.ScytheCrescentDive || effect == WeaponTechniqueEffect.ScytheWraithCut || (effect == WeaponTechniqueEffect.ScytheReapersDance && stage >= 4) || (effect == WeaponTechniqueEffect.ScytheDeathSpiral && stage == 5) || (effect == WeaponTechniqueEffect.ScytheEclipseWaltz && stage == 4) || (effect == WeaponTechniqueEffect.ScytheAbyssalRequiem && stage >= 4);
		float reach = (strong ? 10f : 7f) + expansion * (strong ? 7f : 4f);
		Vector2 slash = Vector2.Normalize(new Vector2(1f, reverse ? (-0.62f) : 0.62f));
		Vector2 normal = new Vector2(0f - slash.Y, slash.X);
		Color violet = ScaleAlpha(new Color(170, 82, 215), remaining);
		Color shadow = ScaleAlpha(new Color(45, 15, 57), remaining * 0.82f);
		Color crimson = ScaleAlpha(new Color(157, 45, 65), remaining * (strong ? 0.68f : 0.26f));
		if ((uint)(effect - 112) <= 1u)
		{
			DrawLine(anchor, -slash * (reach + 3f) + normal * 3f, slash * (reach - 1f) + normal * 3f, 3f, ScaleAlpha(new Color(29, 8, 39), remaining * 0.76f), flip);
			DrawLine(anchor, -slash * (reach * 0.45f) - normal * 5f, -slash * (reach * 0.05f) + normal * 5f, 1f, ScaleAlpha(new Color(102, 39, 127), remaining * 0.62f), flip);
		}
		DrawLine(anchor, -slash * reach, slash * reach, strong ? 4f : 3f, shadow, flip);
		DrawLine(anchor, -slash * (reach - 2f), slash * reach, 1.5f, violet, flip);
		DrawLine(anchor, -normal * (3f + expansion * 2f), normal * (4f + expansion * 3f), 1f, crimson, flip);
		if (strong)
		{
			DrawLine(anchor, normal * 2f, normal * 2f + slash * (7f + expansion * 5f), 1f, crimson, flip);
		}
		if (effect == WeaponTechniqueEffect.ScytheShadowPass && stage == 1)
		{
			float radius = 9f + expansion * 5f;
			float start = -2.4f;
			Vector2 previous = new Vector2(MathF.Cos(start) * radius, MathF.Sin(start) * radius * 0.62f);
			for (int index = 1; index <= 6; index++)
			{
				float angle = start + 4.4f * (float)index / 6f;
				Vector2 next = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * 0.62f);
				DrawLine(anchor, previous, next, 1.5f, ScaleAlpha(new Color(121, 54, 158), remaining * 0.72f), flip);
				previous = next;
			}
		}
	}

	private void DrawScytheUltimateHitImpact(Vector2 anchor, bool flip, int stage, float remaining)
	{
		float expansion = 1f - remaining;
		Color shadow = ScaleAlpha(new Color(25, 6, 32), remaining * 0.9f);
		Color violet = ScaleAlpha(new Color(166, 86, 204), remaining);
		Color crimson = ScaleAlpha(new Color(151, 38, 59), remaining * 0.78f);
		if (stage >= 1 && stage <= 3)
		{
			switch (stage)
			{
			case 1:
				DrawLine(anchor, new Vector2(-13f - expansion * 5f, 3f), new Vector2(13f + expansion * 5f, -4f), 5f, shadow, flip);
				DrawLine(anchor, new Vector2(-12f, 2f), new Vector2(13f, -4f), 1.5f, violet, flip);
				return;
			case 2:
				DrawLine(anchor, new Vector2(11f, -13f - expansion * 4f), new Vector2(-11f, 13f + expansion * 4f), 5f, shadow, flip);
				DrawLine(anchor, new Vector2(10f, -12f), new Vector2(-11f, 13f), 1.5f, violet, flip);
				return;
			}
			float radius = 9f + expansion * 8f;
			Vector2 previous = new Vector2(radius, 0f);
			for (int index = 1; index <= 9; index++)
			{
				float angle = (float)Math.PI * 2f * (float)index / 9f;
				Vector2 next = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * 0.72f);
				DrawLine(anchor, previous, next, 4f, shadow, flip);
				DrawLine(anchor, previous, next, 1f, violet, flip);
				previous = next;
			}
		}
		else if ((uint)(stage - 4) <= 1u)
		{
			Vector2 slash = Vector2.Normalize(new Vector2(1f, (stage == 4) ? (-0.82f) : 0.82f));
			float reach = 17f + expansion * 8f;
			DrawLine(anchor, -slash * reach, slash * reach, 6f, shadow, flip);
			DrawLine(anchor, -slash * (reach - 2f), slash * reach, 2f, violet, flip);
		}
		else if (stage == 6)
		{
			Vector2 slash2 = Vector2.Normalize(new Vector2(1f, -0.7f));
			Vector2 normal = new Vector2(0f - slash2.Y, slash2.X);
			float reach2 = 25f + expansion * 16f;
			DrawLine(anchor, -slash2 * reach2, slash2 * reach2, 9f, shadow, flip);
			DrawLine(anchor, -slash2 * (reach2 - 3f), slash2 * reach2, 3f, violet, flip);
			DrawLine(anchor, -normal * (7f + expansion * 7f), normal * (8f + expansion * 8f), 2f, crimson, flip);
			DrawBox(anchor, -2f, -2f, 4f, 4f, crimson, flip);
		}
		else
		{
			DrawLine(anchor, new Vector2(-9f, 0f), new Vector2(9f, 0f), 2f, violet, flip);
		}
	}

	private void DrawAncientStaffHitImpact(Vector2 anchor, bool flip, WeaponTechniqueEffect effect, int stage, float remaining)
	{
		float expansion = 1f - remaining;
		Color blue = ScaleAlpha(new Color(78, 164, 239), remaining);
		Color purple = ScaleAlpha(new Color(132, 82, 203), remaining * 0.88f);
		Color gold = ScaleAlpha(new Color(234, 192, 79), remaining);
		Color white = ScaleAlpha(new Color(235, 247, 255), Math.Clamp(remaining * 1.35f, 0f, 1f));
		switch (effect)
		{
		case WeaponTechniqueEffect.AncientStaffRunePrison:
		{
			float radius = 7f + expansion * 16f;
			DrawLocalBrokenRune(anchor, flip, Vector2.Zero, radius, 1f, -0.35f + expansion, purple, gold, 8);
			for (int mark = -1; mark <= 1; mark++)
			{
				DrawLine(anchor, new Vector2((float)mark * 7f, 2f), new Vector2((float)mark * 10f, -7f - expansion * 5f), 1f, (stage == 0) ? blue : gold, flip);
			}
			return;
		}
		case WeaponTechniqueEffect.AncientStaffBlinkCast:
			stage = 5;
			break;
		}
		stage = Math.Clamp(stage, 0, 5);
		switch (stage)
		{
		case 0:
			DrawBox(anchor, -2f, -2f, 5f, 5f, white, flip);
			DrawLine(anchor, new Vector2(-6f - expansion * 3f, 0f), new Vector2(7f + expansion * 3f, 0f), 1f, blue, flip);
			return;
		case 1:
			DrawLocalBrokenRune(anchor, flip, Vector2.Zero, 7f + expansion * 5f, 1f, 0.2f, purple, gold, 7);
			DrawBox(anchor, -3f, -3f, 7f, 7f, white, flip);
			return;
		case 2:
		{
			for (int side = -1; side <= 1; side += 2)
			{
				Vector2 glyph = new Vector2(0f, (float)side * (5f + expansion * 5f));
				DrawLine(anchor, glyph + new Vector2(-6f, 0f), glyph + new Vector2(0f, (float)side * -4f), 2f, (side < 0) ? gold : blue, flip);
				DrawLine(anchor, glyph + new Vector2(0f, (float)side * -4f), glyph + new Vector2(6f, 0f), 1f, white, flip);
			}
			return;
		}
		case 3:
		{
			for (int band = -1; band <= 1; band += 2)
			{
				DrawLine(anchor, new Vector2(-12f - expansion * 7f, (float)band * 5f), new Vector2(13f + expansion * 9f, (float)(-band) * 3f), (band < 0) ? 2f : 1f, (band < 0) ? purple : gold, flip);
			}
			DrawLine(anchor, new Vector2(-8f, 0f), new Vector2(17f + expansion * 8f, 0f), 2f, white, flip);
			return;
		}
		}
		float radius2 = 9f + expansion * ((stage == 5) ? 13f : 9f);
		DrawLocalBrokenRune(anchor, flip, Vector2.Zero, radius2, 1f, expansion * 1.2f, gold, blue, 9);
		DrawLocalBrokenRune(anchor, flip, Vector2.Zero, radius2 * 0.58f, 1f, 0f - expansion, purple, white, 6);
		for (int fracture = 0; fracture < 4; fracture++)
		{
			float angle = (float)fracture * ((float)Math.PI / 2f) + 0.35f;
			Vector2 start = new Vector2(MathF.Cos(angle) * radius2, MathF.Sin(angle) * radius2 * 0.72f);
			DrawLine(anchor, start, start * (1.25f + expansion * 0.3f), 1f, (fracture % 2 == 0) ? gold : purple, flip);
		}
	}

	private void DrawShield(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		float x = pose.ShieldX;
		float y = pose.ShieldY;
		bool perfect = player.Combat.IsPerfectGuardFeedbackActive;
		bool contact = player.Combat.IsBlockFeedbackActive;
		Color fill = perfect ? new Color(154, 181, 194) :
			contact ? new Color(124, 145, 153) : Tint(new Color(76, 87, 96), player);
		Color face = perfect ? new Color(205, 224, 230) :
			contact ? new Color(164, 178, 181) : Tint(new Color(126, 139, 148), player);
		Color rim = perfect ? new Color(220, 241, 248) : Tint(new Color(43, 49, 55), player);
		DrawBox(anchor, x - 2f, y, 24f, 18f, Outline, flip);
		DrawBox(anchor, x, y + 18f, 20f, 8f, Outline, flip);
		DrawBox(anchor, x + 3f, y + 26f, 14f, 5f, Outline, flip);
		DrawBox(anchor, x + 7f, y + 31f, 6f, 3f, Outline, flip);
		DrawBox(anchor, x, y + 2f, 20f, 15f, fill, flip);
		DrawBox(anchor, x + 2f, y + 17f, 16f, 7f, fill, flip);
		DrawBox(anchor, x + 5f, y + 24f, 10f, 5f, fill, flip);
		DrawBox(anchor, x + 8f, y + 29f, 4f, 2f, fill, flip);
		DrawBox(anchor, x + 2f, y + 3f, 16f, 3f, face, flip);
		DrawBox(anchor, x + 2f, y + 6f, 3f, 13f, rim, flip);
		float centerX = x + 10f;
		Color emblem = perfect ? new Color(182, 224, 240) : new Color(91, 137, 164);
		DrawLine(anchor, new Vector2(centerX, y + 8f),
			new Vector2(centerX, y + 23f), 3f, rim, flip);
		DrawLine(anchor, new Vector2(centerX - 5f, y + 12f),
			new Vector2(centerX, y + 8f), 2f, emblem, flip);
		DrawLine(anchor, new Vector2(centerX, y + 8f),
			new Vector2(centerX + 5f, y + 12f), 2f, emblem, flip);
		DrawLine(anchor, new Vector2(centerX - 5f, y + 12f),
			new Vector2(centerX, y + 22f), 2f, emblem, flip);
		DrawLine(anchor, new Vector2(centerX + 5f, y + 12f),
			new Vector2(centerX, y + 22f), 2f, emblem, flip);
	}

	private void DrawBlockImpact(Vector2 anchor, bool flip, Pose pose)
	{
		float x = pose.ShieldX + 22f;
		float y = pose.ShieldY + 13f;
		Color spark = new Color(185, 202, 207);
		Color blue = new Color(105, 157, 181, 190);
		float vibration = MathF.Sin(_visualTimeSeconds * 82f) * 1.2f;
		DrawBox(anchor, x, y + vibration, 4f, 4f, spark, flip);
		DrawLine(anchor, new Vector2(x + 2f, y - 1f), new Vector2(x + 4f, y - 7f), 1f, blue, flip);
		DrawLine(anchor, new Vector2(x + 3f, y + 4f), new Vector2(x + 7f, y + 8f), 1f, spark, flip);
		DrawLine(anchor, new Vector2(x + 4f, y + 1f), new Vector2(x + 10f, y), 1f, blue, flip);
	}

	private void DrawPerfectGuardImpact(Vector2 anchor, bool flip, Pose pose)
	{
		if (_perfectGuardPulseRemaining <= 0.01f)
		{
			return;
		}
		float x = pose.ShieldX + 22f;
		float y = pose.ShieldY + 13f;
		float progress = 1f - Math.Clamp(_perfectGuardPulseRemaining / 0.24f, 0f, 1f);
		float fade = 1f - progress;
		Color core = ScaleAlpha(new Color(230, 247, 250), 0.55f + fade * 0.45f);
		Color spark = ScaleAlpha(new Color(124, 188, 214), fade);
		DrawBox(anchor, x - 2f, y - 2f, 7f, 7f, core, flip);
		float radius = 7f + progress * 11f;
		for (int segment = 0; segment < 8; segment++)
		{
			float a0 = MathHelper.TwoPi * segment / 8f;
			float a1 = MathHelper.TwoPi * (segment + 0.62f) / 8f;
			DrawLine(anchor,
				new Vector2(x + MathF.Cos(a0) * radius, y + MathF.Sin(a0) * radius),
				new Vector2(x + MathF.Cos(a1) * radius, y + MathF.Sin(a1) * radius),
				1f, spark, flip);
		}
		DrawLine(anchor, new Vector2(x + 1f, y - 3f), new Vector2(x + 3f, y - 13f), 2f, core, flip);
		DrawLine(anchor, new Vector2(x + 4f, y + 3f), new Vector2(x + 12f, y + 8f), 2f, spark, flip);
		DrawLine(anchor, new Vector2(x + 5f, y), new Vector2(x + 15f, y - 3f), 2f, core, flip);
	}

	private void DrawGreatswordGuardImpact(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		Vector2 hand = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 direction = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 impact = hand + direction * 29f;
		Color core = (player.Combat.IsPerfectGuardFeedbackActive ? new Color(245, 228, 177) : new Color(190, 196, 193));
		Color spark = new Color(218, 164, 78);
		DrawBox(anchor, impact.X - 3f, impact.Y - 3f, 7f, 7f, core, flip);
		DrawLine(anchor, impact, impact + new Vector2(12f, -10f), 2f, spark, flip);
		DrawLine(anchor, impact, impact + new Vector2(14f, 3f), 2f, core, flip);
		DrawLine(anchor, impact, impact + new Vector2(5f, 13f), 2f, spark, flip);
	}

	private void DrawDuelistGuardImpact(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip, Pose pose)
	{
		Vector2 stellarDirection = new Vector2(MathF.Cos(pose.SwordAngle), MathF.Sin(pose.SwordAngle));
		Vector2 lunarDirection = new Vector2(MathF.Cos(pose.OffSwordAngle), MathF.Sin(pose.OffSwordAngle));
		Vector2 stellar = new Vector2(pose.SwordHandX, pose.SwordHandY);
		Vector2 lunar = new Vector2(pose.OffHandX, pose.OffHandY);
		Vector2 impact = (stellar + stellarDirection * 20f + lunar + lunarDirection * 18f) * 0.5f;
		bool perfect = player.Combat.IsPerfectPhantomFeedbackActive;
		float size = (perfect ? 10f : 6f);
		Color violet = (perfect ? new Color(225, 205, 255) : new Color(177, 151, 213));
		Color cyan = (perfect ? new Color(218, 251, 255) : new Color(137, 211, 222));
		DrawBox(anchor, impact.X - 2f, impact.Y - 2f, perfect ? 5f : 4f, perfect ? 5f : 4f, Color.Lerp(violet, cyan, 0.5f), flip);
		DrawLine(anchor, impact - new Vector2(size, 0f), impact + new Vector2(size, 0f), perfect ? 2f : 1f, cyan, flip);
		DrawLine(anchor, impact - new Vector2(0f, size), impact + new Vector2(0f, size), perfect ? 2f : 1f, violet, flip);
		if (perfect)
		{
			DrawLine(anchor, impact - new Vector2(5f, 5f), impact + new Vector2(5f, 5f), 1f, cyan, flip);
			DrawLine(anchor, impact + new Vector2(5f, -5f), impact + new Vector2(-5f, 5f), 1f, violet, flip);
		}
	}

	private void DrawCollapsed(DungeonAscendant.Player.Player player, Vector2 anchor, bool flip)
	{
		float settle = ((_animation.Frame >= 3) ? 0f : (-2f));
		Color armor = Tint(SteelDark, player);
		ArmorClass armorClass = player.ArmorClass;
		if (1 == 0)
		{
		}
		float num = armorClass switch
		{
			ArmorClass.Light => 25f,
			ArmorClass.Heavy => 34f,
			_ => 29f,
		};
		if (1 == 0)
		{
		}
		float torsoWidth = num;
		DrawBox(anchor, -24f, -11f + settle, 37f, 9f, Outline, flip);
		DrawBox(anchor, -22f, -10f + settle, 34f, 6f, Tint(CloakDark, player), flip);
		DrawBox(anchor, -9f, -18f + settle, torsoWidth, 13f, Outline, flip);
		DrawBox(anchor, -7f, -16f + settle, torsoWidth - 4f, 10f, armor, flip);
		DrawBox(anchor, 16f, -16f + settle, 15f, 14f, Outline, flip);
		DrawBox(anchor, 17f, -14f + settle, 12f, 10f, Tint(GetSkinColor(player.Appearance.SkinTone), player), flip);
		DrawBox(anchor, 18f, -16f + settle, 13f, 5f, GetHairColor(player.Appearance.HairColor), flip);
		DrawBox(anchor, -31f, -10f + settle, 14f, 8f, Tint(Leather, player), flip);
		if (player.WeaponFamily == WeaponFamily.DualSwords)
		{
			DrawLine(anchor, new Vector2(-24f, -7f), new Vector2(-3f, -2f), 5f, Outline, flip);
			DrawLine(anchor, new Vector2(-22f, -7f), new Vector2(-3f, -2f), 2f, SteelLight, flip);
			DrawLine(anchor, new Vector2(6f, -3f), new Vector2(28f, -8f), 5f, Outline, flip);
			DrawLine(anchor, new Vector2(7f, -3f), new Vector2(27f, -8f), 2f, SteelLight, flip);
		}
		else if (player.WeaponFamily == WeaponFamily.HunterBow)
		{
			DrawLine(anchor, new Vector2(-22f, -9f), new Vector2(4f, -2f), 4f, Outline, flip);
			DrawLine(anchor, new Vector2(4f, -2f), new Vector2(30f, -8f), 4f, Outline, flip);
			DrawLine(anchor, new Vector2(-22f, -9f), new Vector2(30f, -8f), 1f, new Color(205, 194, 158), flip);
		}
		else if (player.WeaponFamily == WeaponFamily.ArcaneWarStaff)
		{
			DrawLine(anchor, new Vector2(-26f, -2f), new Vector2(31f, -5f), 7f, Outline, flip);
			DrawLine(anchor, new Vector2(-24f, -2f), new Vector2(29f, -5f), 4f, new Color(83, 54, 43), flip);
			DrawBox(anchor, 26f, -11f, 13f, 13f, Outline, flip);
			DrawBox(anchor, 29f, -8f, 7f, 7f, new Color(145, 94, 187), flip);
		}
		else
		{
			float bladeWidth = ((player.WeaponFamily == WeaponFamily.GreatSword) ? 9f : 5f);
			float bladeLength = ((player.WeaponFamily == WeaponFamily.GreatSword) ? 51f : 43f);
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
		Color web = new Color(188, 205, 199, 145);
		DrawLine(anchor, new Vector2(-20f, -41f), new Vector2(17f, -13f), 2f, web, flip);
		DrawLine(anchor, new Vector2(-17f, -17f), new Vector2(19f, -39f), 2f, web, flip);
		DrawLine(anchor, new Vector2(-14f, -29f), new Vector2(16f, -29f), 1f, web, flip);
	}

	private void DrawBox(Vector2 anchor, float x, float y, float width, float height, Color color, bool flip)
	{
		x = _partPivotX + (x - _partPivotX) * _partScaleX;
		width *= _partScaleX;
		int drawX = (flip ? ((int)MathF.Round(anchor.X - x - width)) : ((int)MathF.Round(anchor.X + x)));
		Rectangle destination = new Rectangle(drawX, (int)MathF.Round(anchor.Y + y), Math.Max(1, (int)MathF.Round(width)), Math.Max(1, (int)MathF.Round(height)));
		_spriteBatch.Draw(_pixel, destination, ApplySpatialPhase(color));
	}

	private void DrawRectangleOutlineLocal(Vector2 anchor, float x, float y, float width, float height, float thickness, Color color, bool flip)
	{
		DrawBox(anchor, x, y, width, thickness, color, flip);
		DrawBox(anchor, x, y + height - thickness, width, thickness, color, flip);
		DrawBox(anchor, x, y, thickness, height, color, flip);
		DrawBox(anchor, x + width - thickness, y, thickness, height, color, flip);
	}

	private void DrawLine(Vector2 anchor, Vector2 localStart, Vector2 localEnd, float thickness, Color color, bool flip)
	{
		Vector2 start = ToWorld(anchor, localStart, flip);
		Vector2 end = ToWorld(anchor, localEnd, flip);
		Vector2 delta = end - start;
		float length = delta.Length();
		if (!(length <= 0.01f))
		{
			start = new Vector2(MathF.Round(start.X), MathF.Round(start.Y));
			float angle = MathF.Atan2(delta.Y, delta.X);
			_spriteBatch.Draw(_pixel, start, null, ApplySpatialPhase(color), angle, new Vector2(0f, 0.5f), new Vector2(MathF.Round(length), MathF.Max(1f, MathF.Round(thickness * _lineThicknessScale))), SpriteEffects.None, 0f);
		}
	}

	private Vector2 ToWorld(Vector2 anchor, Vector2 local, bool flip)
	{
		local.X = _partPivotX + (local.X - _partPivotX) * _partScaleX;
		return new Vector2(anchor.X + (flip ? (0f - local.X) : local.X), anchor.Y + local.Y);
	}

	private static Color Tint(Color color, DungeonAscendant.Player.Player player)
	{
		if (player.IsHitFlashing)
		{
			return Color.Lerp(color, new Color(232, 105, 96), 0.58f);
		}
		if (player.Combat.IsGuardBroken)
		{
			return Color.Lerp(color, new Color(169, 91, 57), 0.3f);
		}
		if (player.Combat.IsDodgeInvulnerable)
		{
			return Color.Lerp(color, new Color(111, 141, 154), 0.22f);
		}
		return color;
	}

	private static Color ScaleAlpha(Color color, float amount)
	{
		byte alpha = (byte)Math.Clamp((int)MathF.Round((float)(int)color.A * Math.Clamp(amount, 0f, 1f)), 0, 255);
		return new Color(color.R, color.G, color.B, alpha);
	}

	private Color ApplySpatialPhase(Color color)
	{
		if (_spatialPhaseStrength <= 0f)
		{
			return color;
		}
		Color shifted = Color.Lerp(color, _spatialPhaseColor, Math.Clamp(0.38f + _spatialPhaseStrength * 0.38f, 0f, 1f));
		return ScaleAlpha(shifted, 1f - _spatialPhaseStrength * 0.58f);
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
		BodyBuild bodyBuild = appearance.BodyBuild;
		if (1 == 0)
		{
		}
		float num = bodyBuild switch
		{
			BodyBuild.Slim => -0.05f,
			BodyBuild.Broad => 0.07f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float build = num;
		VisualProportion torsoWidth = appearance.TorsoWidth;
		if (1 == 0)
		{
		}
		num = torsoWidth switch
		{
			VisualProportion.Slim => -0.06f,
			VisualProportion.Broad => 0.07f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float proportion = num;
		float bodyType = ((appearance.BodyType == CharacterBodyType.Female) ? (-0.025f) : 0f);
		return 1f + build + proportion + bodyType;
	}

	private static float GetArmScale(CharacterAppearance appearance)
	{
		BodyBuild bodyBuild = appearance.BodyBuild;
		if (1 == 0)
		{
		}
		float num = bodyBuild switch
		{
			BodyBuild.Slim => -0.08f,
			BodyBuild.Broad => 0.1f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float build = num;
		VisualProportion armThickness = appearance.ArmThickness;
		if (1 == 0)
		{
		}
		num = armThickness switch
		{
			VisualProportion.Slim => -0.12f,
			VisualProportion.Broad => 0.12f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float proportion = num;
		return 1f + build + proportion;
	}

	private static float GetLegScale(CharacterAppearance appearance)
	{
		BodyBuild bodyBuild = appearance.BodyBuild;
		if (1 == 0)
		{
		}
		float num = bodyBuild switch
		{
			BodyBuild.Slim => -0.05f,
			BodyBuild.Broad => 0.07f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float build = num;
		VisualProportion legThickness = appearance.LegThickness;
		if (1 == 0)
		{
		}
		num = legThickness switch
		{
			VisualProportion.Slim => -0.08f,
			VisualProportion.Broad => 0.09f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		float proportion = num;
		return 1f + build + proportion;
	}

	private static float GetHeightOffset(ApparentHeight height)
	{
		if (1 == 0)
		{
		}
		float result = height switch
		{
			ApparentHeight.Short => 3f,
			ApparentHeight.Tall => -4f,
			_ => 0f,
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static Color GetSkinColor(SkinTone tone)
	{
		if (1 == 0)
		{
		}
		Color result = tone switch
		{
			SkinTone.Fair => new Color(230, 190, 161),
			SkinTone.Light => new Color(211, 166, 132),
			SkinTone.Warm => new Color(202, 146, 104),
			SkinTone.Brown => new Color(145, 94, 64),
			SkinTone.Deep => new Color(92, 58, 45),
			_ => new Color(190, 137, 99),
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static Color GetHairColor(HairColor color)
	{
		if (1 == 0)
		{
		}
		Color result = color switch
		{
			HairColor.Black => new Color(24, 23, 25),
			HairColor.Brown => new Color(91, 58, 39),
			HairColor.LightBrown => new Color(139, 98, 60),
			HairColor.Gray => new Color(111, 109, 105),
			HairColor.White => new Color(205, 201, 187),
			HairColor.DarkRed => new Color(91, 38, 32),
			_ => new Color(39, 29, 27),
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
