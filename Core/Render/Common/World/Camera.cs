using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.World.Entities.Players;

namespace Helion.Render.Common.World;

/// <summary>
/// A camera in a world.
/// </summary>
public class Camera
{
    public static readonly vec3 Up = new(0, 0, 1);

    public Vec3F Direction { get; private set; }
    public float YawRadians { get => m_yawRadians; set => SetYaw(value); }
    public float PitchRadians { get => m_pitchRadians; set => SetPitch(value); }
    private Vec3F m_position;
    private Vec3F m_prevPosition;
    private float m_yawRadians;
    private float m_pitchRadians;

    public Vec3F CurrentPosition => m_position;

    public Camera(Vec3F position, float yawRadians, float pitchRadians)
    {
        m_position = position;
        Direction = Vec3F.UnitSphere(yawRadians, pitchRadians);
        m_yawRadians = ClampYaw(yawRadians);
        m_pitchRadians = ClampPitch(pitchRadians);
        m_prevPosition = position;
    }

    public void Set(Vec3F position, float yawRadians, float pitchRadians)
    {
        m_position = position;
        Direction = Vec3F.UnitSphere(yawRadians, pitchRadians);
        m_yawRadians = ClampYaw(yawRadians);
        m_pitchRadians = ClampPitch(pitchRadians);
        m_prevPosition = position;
    }

    public Camera(Player player, double frac)
    {
        m_position = player.GetPrevViewPosition().Interpolate(player.GetViewPosition(), frac).Float;
        m_prevPosition = m_position;
        YawRadians = ClampYaw(player.AngleRadians);
        PitchRadians = ClampPitch(player.PitchRadians);
        Direction = Vec3F.UnitSphere(YawRadians, PitchRadians);

        // TODO
        // // When rendering, we always want the most up-to-date values. We
        // // would only want to interpolate here if looking at another player
        // // and would likely need to add more logic for wrapping around if
        // // the player rotates from 359 degrees -> 2 degrees since that will
        // // interpolate in the wrong direction.
        //
        // if (IsDead)
        // {
        //     float yaw = (float)(m_prevAngle + t * (AngleRadians - m_prevAngle));
        //     float pitch = (float)(m_prevPitch + t * (PitchRadians - m_prevPitch));
        //
        //     return new Camera(this, t);
        // }
        // else
        // {
        //     float yaw = (float)AngleRadians;
        //     float pitch = (float)PitchRadians;
        //
        //     return new Camera(this);
        // }
    }

    private void SetYaw(float yaw)
    {
        m_yawRadians = ClampYaw(yaw);
        Direction = Vec3F.UnitSphere(m_yawRadians, m_pitchRadians);
    }

    private void SetPitch(float pitch)
    {
        m_pitchRadians = ClampPitch(pitch);
        Direction = Vec3F.UnitSphere(m_yawRadians, m_pitchRadians);
    }

    /// <summary>
    /// Updates to a new position, while setting the previous position to
    /// the current position. To update the previous position as well, use
    /// <see cref="Update(Helion.Geometry.Vectors.Vec3F, Helion.Geometry.Vectors.Vec3F)"/>.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void Update(Vec3F position)
    {
        m_prevPosition = m_position;
        m_position = position;
    }

    /// <summary>
    /// Updates both the current and previous position.
    /// </summary>
    /// <param name="position">Tne current position to use.</param>
    /// <param name="prevPosition">The previous position to use.</param>
    public void Update(Vec3F position, Vec3F prevPosition)
    {
        m_prevPosition = prevPosition;
        m_position = position;
    }

    public Vec3F Position(float frac)
    {
        return m_prevPosition.Interpolate(m_position, frac);
    }

    public mat4 ViewMatrix(float frac)
    {
        vec3 pos = Position(frac).GlmVector;
        vec3 eye = pos + Direction.GlmVector;
        return mat4.LookAt(pos, eye, Up);
    }

    /// <summary>
    /// Takes a desired X field of view in radians and converts it to the Y
    /// field of view with the provided aspect ratio.
    /// </summary>
    /// <remarks>
    /// Intended primarily for libraries that take a Y field of view when
    /// creating a perspective matrix.
    /// </remarks>
    /// <param name="fovX">The field of view in radians.</param>
    /// <param name="aspectRatio">The aspect ratio of the viewport.</param>
    /// <returns>The Y field of view.</returns>
    public static float FieldOfViewXToY(float fovX, float aspectRatio)
    {
        return 2 * (float)Math.Atan((float)Math.Tan(fovX / 2) / aspectRatio);
    }

    protected static float ClampYaw(double yawRadians)
    {
        double clampedYaw = yawRadians % MathHelper.TwoPi;
        if (clampedYaw < 0)
            clampedYaw += MathHelper.TwoPi;
        return (float)clampedYaw;
    }

    protected static float ClampPitch(double pitchRadians)
    {
        return (float)MathHelper.Clamp(pitchRadians, -MathHelper.HalfPi, MathHelper.HalfPi);
    }
}
