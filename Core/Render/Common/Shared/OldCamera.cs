using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Util;

namespace Helion.Render.OpenGL.Shared;

/// <summary>
/// Represents a camera as a point in some world, while also supporting
/// interpolation between a previous and current state.
/// </summary>
public class OldCamera
{
    /// <summary>
    /// The up direction for renderers to orient themselves with.
    /// </summary>
    /// <remarks>
    /// Because Doom uses the Z axis as the vertical axis, we have to
    /// reflect that. It's much easier for us to go with the convention
    /// used by the game rather than have Y be the vertical axis, or else
    /// it's easy to forget and mix up the Y and Z axes.
    /// </remarks>
    public static readonly Vec3F Up = new(0, 0, 1);

    /// <summary>
    /// The Up vector in GLM's vector format.
    /// </summary>
    public static readonly vec3 UpGlm = Up.GlmVector;

    /// <summary>
    /// The current camera position.
    /// </summary>
    public Vec3F Position;

    /// <summary>
    /// The directional vector we're facing. This is made up from the yaw
    /// and pitch.
    /// </summary>
    public Vec3F Direction;

    /// <summary>
    /// The horizontal viewing angle, where zero radians is equal to facing
    /// east, and proceeds counter clockwise.
    /// </summary>
    public float YawRadians;

    /// <summary>
    /// The vertical viewing pitch, where zero radians is at a horizontal.
    /// </summary>
    public float PitchRadians;

    /// <summary>
    /// Creates a camera at the location with the angles provided.
    /// </summary>
    /// <param name="position">The location of the camera.</param>
    /// <param name="yawRadians">The horizontal looking angle in radians.
    /// </param>
    /// <param name="pitchRadians">The vertical looking angle in radians.
    /// This should be between [-pi/2, pi/2], or else it will be clamped
    /// to that range.</param>
    public OldCamera(Vec3F position, float yawRadians, float pitchRadians)
    {
        Position = position;
        YawRadians = ClampYaw(yawRadians);
        PitchRadians = ClampPitch(pitchRadians);
        Direction = DirectionFrom(yawRadians, pitchRadians);
    }

    public void Set(Vec3F position, float yawRadians, float pitchRadians)
    {
        Position = position;
        YawRadians = ClampYaw(yawRadians);
        PitchRadians = ClampPitch(pitchRadians);
        Direction = DirectionFrom(yawRadians, pitchRadians);
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

    /// <summary>
    /// Creates a view matrix for a camera information object.
    /// </summary>
    /// <returns>The view matrix for the camera information.</returns>
    public mat4 CalculateViewMatrix(bool onlyXY = false)
    {
        vec3 pos = Position.GlmVector;
        vec3 eye = pos + Direction.WithZ(onlyXY ? 0 : Direction.Z).GlmVector;
        return mat4.LookAt(pos, eye, UpGlm);
    }

    /// <summary>
    /// Calculates the direction from the yaw/pitch.
    /// </summary>
    /// <param name="yawRadians">The yaw in radians.</param>
    /// <param name="pitchRadians">The pitch in radians. This will be
    /// clamped to the range of [-pi/2, pi/2] if not in range.</param>
    /// <returns>The direction from the yaw/pitch combination.</returns>
    private static Vec3F DirectionFrom(float yawRadians, float pitchRadians)
    {
        float x = (float)(Math.Cos(yawRadians) * Math.Cos(pitchRadians));
        float y = (float)(Math.Sin(yawRadians) * Math.Cos(pitchRadians));
        float z = (float)Math.Sin(pitchRadians);
        return new Vec3F(x, y, z).Unit();
    }

    private static float ClampYaw(double yawRadians)
    {
        double clampedYaw = yawRadians % MathHelper.TwoPi;
        if (clampedYaw < 0)
            clampedYaw += MathHelper.TwoPi;
        return (float)clampedYaw;
    }

    private static float ClampPitch(double pitchRadians)
    {
        return (float)MathHelper.Clamp(pitchRadians, -MathHelper.HalfPi, MathHelper.HalfPi);
    }
}
