using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Util;

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
    private float m_yawRadians;
    private float m_pitchRadians;

    public Vec3F PositionInterpolated;
    public Vec3F Position;

    public const double MaxPitch = MathHelper.HalfPi - 0.05;

    public Camera(Vec3F positionInterpolated, float yawRadians, float pitchRadians)
    {
        m_pitchRadians = ClampPitch(pitchRadians);
        PositionInterpolated = positionInterpolated;
        Direction = Vec3F.UnitSphere(yawRadians, m_pitchRadians);
        m_yawRadians = ClampYaw(yawRadians);
        Position = positionInterpolated;
    }

    public void Set(Vec3F positionInterpolated, Vec3F position, float yawRadians, float pitchRadians)
    {
        m_pitchRadians = ClampPitch(pitchRadians);
        PositionInterpolated = positionInterpolated;
        Direction = Vec3F.UnitSphere(yawRadians, m_pitchRadians);
        m_yawRadians = ClampYaw(yawRadians);
        Position = position;
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

    public mat4 ViewMatrix(float frac)
    {
        vec3 pos = PositionInterpolated.GlmVector;
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
        return (float)MathHelper.Clamp(pitchRadians, -MaxPitch, MaxPitch);
    }
}
