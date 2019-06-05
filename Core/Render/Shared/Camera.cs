using Helion.Util;
using Helion.Util.Geometry;
using OpenTK;
using System;

namespace Helion.Render.Shared
{
    /// <summary>
    /// Represents a camera as a point in some world.
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
        /// The location in the world.
        /// </summary>
        public Vector3 Position 
        {
            get => position;
            set
            {
                position = value;
                positionFixed = new Vec2Fixed(new Fixed(value.X), new Fixed(value.Y));
            }
        }
        private Vector3 position;

        /// <summary>
        /// A cached lookup position
        /// </summary>
        public Vec2Fixed PositionFixed { get => positionFixed; }
        private Vec2Fixed positionFixed;

        /// <summary>
        /// The unit vector direction the camera is facing.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// The yaw in radians.
        /// </summary>
        public float Yaw;

        /// <summary>
        /// The pitch in radians. This will be in the range of [-pi/2, pi/2].
        /// </summary>
        public float Pitch;

        /// <summary>
        /// Creates a camera at the origin facing north.
        /// </summary>
        public Camera() : this(new Vector3(0, 0, 0), Helion.Util.MathHelper.HalfPi)
        {
        }

        /// <summary>
        /// Creates a camera at the location with the angles provided.
        /// </summary>
        /// <param name="position">The location of the camera.</param>
        /// <param name="yawRadians">The horizontal looking angle in radians.
        /// </param>
        /// <param name="pitchRadians">The vertical looking angle in radians. 
        /// This should be between [-pi/2, pi/2], or else it will be clamped
        /// to that range.</param>
        public Camera(Vector3 position, float yawRadians, float pitchRadians = 0)
        {
            Position = position;
            Direction = DirectionFrom(yawRadians, pitchRadians);
            Yaw = yawRadians;
            Pitch = pitchRadians;
        }

        private static float ClampPitch(float pitchRadians)
        {
            // We want to avoid looking straight up or down, if there is a
            // danger of gimbal lock.
            float notQuiteVertical = Util.MathHelper.HalfPi - 0.001f;
            return Math.Clamp(pitchRadians, -notQuiteVertical, notQuiteVertical);
        }

        /// <summary>
        /// Calculates the direction from the yaw/pitch.
        /// </summary>
        /// <param name="yawRadians">The yaw in radians.</param>
        /// <param name="pitchRadians">The pitch in radians. This will be 
        /// clamped to the range of [-pi/2, pi/2] if not in range.</param>
        /// <returns></returns>
        public static Vector3 DirectionFrom(float yawRadians, float pitchRadians)
        {
            float x = (float)Math.Cos(yawRadians);
            float y = (float)Math.Sin(yawRadians);
            float z = (float)Math.Sin(ClampPitch(pitchRadians));
            return Vector3.Normalize(new Vector3(x, y, z));
        }

        public Matrix4 ViewMatrix() => Matrix4.LookAt(Position, Position + Direction, Up);
    }
}
