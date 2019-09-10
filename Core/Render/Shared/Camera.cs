using System;
using System.Numerics;
using GlmSharp;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared
{
    /// <summary>
    /// Represents a camera as a point in some world, while also supporting
    /// interpolation between a previous and current state.
    /// </summary>
    public class Camera
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
        public static readonly Vector3 Up = new Vector3(0, 0, 1);
        
        /// <summary>
        /// The Up vector in GLM's vector format.
        /// </summary>
        public static readonly vec3 UpGlm = Up.ToGlmVector();

        /// <summary>
        /// The current camera position.
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// The directional vector we're facing. This is made up from the yaw
        /// and pitch.
        /// </summary>
        public readonly Vector3 Direction;
        
        /// <summary>
        /// The horizontal viewing angle, where zero radians is equal to facing
        /// east, and proceeds counter clockwise.
        /// </summary>
        public readonly float YawRadians;
        
        /// <summary>
        /// The vertical viewing pitch, where zero radians is at a horizontal.
        /// </summary>
        public readonly float PitchRadians;
        
        /// <summary>
        /// Creates a camera at the location with the angles provided.
        /// </summary>
        /// <param name="position">The location of the camera.</param>
        /// <param name="yawRadians">The horizontal looking angle in radians.
        /// </param>
        /// <param name="pitchRadians">The vertical looking angle in radians. 
        /// This should be between [-pi/2, pi/2], or else it will be clamped
        /// to that range.</param>
        public Camera(Vector3 position, float yawRadians, float pitchRadians)
        {
            Precondition(yawRadians >= 0.0f && yawRadians < MathHelper.TwoPi, $"Out of range yaw, should be in [0, 2*pi), got {yawRadians}");
            Precondition(pitchRadians > -MathHelper.HalfPi && pitchRadians < MathHelper.TwoPi, $"Out of range pitch, should be in (-pi/2, pi/2), got {pitchRadians}");
            
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
        public mat4 CalculateViewMatrix()
        {
            vec3 pos = Position.ToGlmVector();
            vec3 eye = pos + Direction.ToGlmVector();
            return mat4.LookAt(pos, eye, UpGlm);
        }
        
        /// <summary>
        /// Calculates the direction from the yaw/pitch.
        /// </summary>
        /// <param name="yawRadians">The yaw in radians.</param>
        /// <param name="pitchRadians">The pitch in radians. This will be 
        /// clamped to the range of [-pi/2, pi/2] if not in range.</param>
        /// <returns>The direction from the yaw/pitch combination.</returns>
        private static Vector3 DirectionFrom(float yawRadians, float pitchRadians)
        {
            float x = (float)Math.Cos(yawRadians);
            float y = (float)Math.Sin(yawRadians);
            float z = (float)Math.Sin(pitchRadians);
            return Vector3.Normalize(new Vector3(x, y, z));
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
}