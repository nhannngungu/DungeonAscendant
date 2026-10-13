using System;
using Microsoft.Xna.Framework;
using DungeonAscendant.Player;

namespace DungeonAscendant.World;

public sealed class Camera2D
{
    private int _viewportWidth;
    private int _viewportHeight;
    private float _impactTimeRemaining;
    private float _impactStrength;
    private float _impactPhase;
    private Vector2 _impactOffset;
    private float _zoom = 1f;

    public Vector2 Position { get; private set; }
    public float Zoom => _zoom;
    public Matrix Transform => Matrix.CreateTranslation(
        -Position.X - _impactOffset.X,
        -Position.Y - _impactOffset.Y,
        0f) * Matrix.CreateScale(_zoom, _zoom, 1f);
    public Rectangle ViewBounds => new(
        (int)Position.X,
        (int)Position.Y,
        Math.Max(1, (int)MathF.Ceiling(_viewportWidth / _zoom)),
        Math.Max(1, (int)MathF.Ceiling(_viewportHeight / _zoom)));

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
    }

    public void ResizeViewport(int width, int height)
    {
        _viewportWidth = Math.Max(1, width);
        _viewportHeight = Math.Max(1, height);
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
        float visibleWidth = _viewportWidth / _zoom;
        float visibleHeight = _viewportHeight / _zoom;
        float desiredX = targetPosition.X - visibleWidth * playerScreenRatio;
        float desiredY = targetPosition.Y - visibleHeight / 2f;
        float maximumX = MathHelper.Max(
            worldBounds.Left,
            worldBounds.Right - visibleWidth);
        float maximumY = MathHelper.Max(
            worldBounds.Top,
            worldBounds.Bottom - visibleHeight);

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

    public void SetCinematicZoom(float targetZoom, float elapsedSeconds)
    {
        targetZoom = MathHelper.Clamp(targetZoom, 1f, 1.55f);
        float blend = 1f - MathF.Exp(-5.5f * MathF.Max(0f, elapsedSeconds));
        _zoom = MathHelper.Lerp(_zoom, targetZoom, blend);
        if (MathF.Abs(_zoom - targetZoom) < .001f)
            _zoom = targetZoom;
    }

    public void ResetCinematicZoom()
    {
        _zoom = 1f;
        _impactOffset = Vector2.Zero;
    }

    public void Snap(
        Vector2 targetPosition,
        FacingDirection facing,
        Rectangle worldBounds,
        float forwardViewRatio = .57f)
    {
        _zoom = 1f;
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
        _zoom = 1f;
    }

    public void ResetRuntime()
    {
        Position = Vector2.Zero;
        _impactTimeRemaining = 0f;
        _impactStrength = 0f;
        _impactPhase = 0f;
        _impactOffset = Vector2.Zero;
        _zoom = 1f;
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
