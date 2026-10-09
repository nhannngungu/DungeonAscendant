using System;
using Microsoft.Xna.Framework;
using DungeonAscendant.Player;

namespace DungeonAscendant.World;

public sealed class Camera2D
{
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;
    private float _impactTimeRemaining;
    private float _impactStrength;
    private float _impactPhase;
    private Vector2 _impactOffset;

    public Vector2 Position { get; private set; }
    public Matrix Transform => Matrix.CreateTranslation(
        -Position.X - _impactOffset.X,
        -Position.Y - _impactOffset.Y,
        0f);
    public Rectangle ViewBounds => new(
        (int)Position.X,
        (int)Position.Y,
        _viewportWidth,
        _viewportHeight);

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
    }

    public void Follow(
        Vector2 targetPosition,
        FacingDirection facing,
        Rectangle worldBounds,
        float elapsedSeconds,
        float forwardViewRatio = .57f)
    {
        forwardViewRatio = MathHelper.Clamp(forwardViewRatio, .50f, .62f);
        float playerScreenRatio = facing == FacingDirection.Right
            ? 1f - forwardViewRatio
            : forwardViewRatio;
        float desiredX = targetPosition.X - _viewportWidth * playerScreenRatio;
        float desiredY = targetPosition.Y - _viewportHeight / 2f;
        float maximumX = MathHelper.Max(
            worldBounds.Left,
            worldBounds.Right - _viewportWidth);
        float maximumY = MathHelper.Max(
            worldBounds.Top,
            worldBounds.Bottom - _viewportHeight);

        Vector2 clampedTarget = new(
            MathHelper.Clamp(desiredX, worldBounds.Left, maximumX),
            MathHelper.Clamp(desiredY, worldBounds.Top, maximumY));
        float horizontalBlend = 1f - System.MathF.Exp(-9f * elapsedSeconds);
        float verticalBlend = 1f - System.MathF.Exp(-4f * elapsedSeconds);
        Position = new Vector2(
            MathHelper.Lerp(Position.X, clampedTarget.X, horizontalBlend),
            MathHelper.Lerp(Position.Y, clampedTarget.Y, verticalBlend));
        Position = new Vector2(
            MathHelper.Clamp(Position.X, worldBounds.Left, maximumX),
            MathHelper.Clamp(Position.Y, worldBounds.Top, maximumY));
        UpdateImpactFeedback(elapsedSeconds);
    }

    public void AddImpactFeedback(float strength)
    {
        strength = MathHelper.Clamp(strength, 0f, 5f);
        if (strength <= 0f)
            return;

        _impactStrength = MathF.Max(_impactStrength, strength);
        _impactTimeRemaining = MathF.Max(_impactTimeRemaining, .12f);
        _impactOffset = new Vector2(_impactStrength, 0f);
    }

    public void Snap(
        Vector2 targetPosition,
        FacingDirection facing,
        Rectangle worldBounds,
        float forwardViewRatio = .57f)
    {
        forwardViewRatio = MathHelper.Clamp(forwardViewRatio, .50f, .62f);
        float playerScreenRatio = facing == FacingDirection.Right
            ? 1f - forwardViewRatio
            : forwardViewRatio;
        float maximumX = MathHelper.Max(worldBounds.Left, worldBounds.Right - _viewportWidth);
        float maximumY = MathHelper.Max(worldBounds.Top, worldBounds.Bottom - _viewportHeight);
        Position = new Vector2(
            MathHelper.Clamp(
                targetPosition.X - _viewportWidth * playerScreenRatio,
                worldBounds.Left,
                maximumX),
            MathHelper.Clamp(
                targetPosition.Y - _viewportHeight / 2f,
                worldBounds.Top,
                maximumY));
        _impactTimeRemaining = 0f;
        _impactStrength = 0f;
        _impactOffset = Vector2.Zero;
    }

    private void UpdateImpactFeedback(float elapsedSeconds)
    {
        if (_impactTimeRemaining <= 0f)
        {
            _impactOffset = Vector2.Zero;
            _impactStrength = 0f;
            return;
        }

        _impactTimeRemaining = MathF.Max(
            0f,
            _impactTimeRemaining - MathF.Max(0f, elapsedSeconds));
        _impactPhase += MathF.Max(0f, elapsedSeconds) * 62f;
        float attenuation = _impactTimeRemaining / .12f;
        _impactOffset = new Vector2(
            MathF.Sin(_impactPhase),
            MathF.Cos(_impactPhase * 1.37f) * .55f) *
            _impactStrength * attenuation;
    }
}
