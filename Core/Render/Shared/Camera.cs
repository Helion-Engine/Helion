using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using System;
using System.Numerics;

namespace Helion.Render.Shared
{
    /// <summary>
    /// An atomic location and viewing angle in a world.
    /// </summary>
    public class CameraInfo
    {
        /// <summary>
        /// The location in the world.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// A lookup position in fixed point, primarily for various 2D 
        /// calculations involving the BSP tree.
        /// </summary>
        public Vec2Fixed PositionFixed => Position.To2D().ToFixed();

        /// <summary>
        /// The unit vector direction the camera is facing.
        /// </summary>
        public Vector3 Direction { get; private set; }

        /// <summary>
        /// The yaw in radians.
        /// </summary>
        public float Yaw 
        {
            get => yaw;
            set 
            {
                yaw = value;
                Direction = DirectionFrom(yaw, pitch);
            }
        }
        private float yaw;

        /// <summary>
        /// The pitch in radians. This will be in the range of [-pi/2, pi/2].
        /// </summary>
        public float Pitch 
        {
            get => pitch;
            set 
            {
                pitch = ClampPitch(value);
                Direction = DirectionFrom(yaw, pitch);
            }
        }
        private float pitch;

        /// <summary>
        /// Creates a new camera information object at some position and with
        /// some pitch/yaw.
        /// </summary>
        /// <param name="position">The location in the world.</param>
        /// <param name="yawRadians">The horizontal angle, whereby zero is the
        /// East direction, pi/2 is North, etc.</param>
        /// <param name="pitchRadians">The pitch in radians, whereby zero is
        /// facing horizontally, pi/2 is looking straight up, and -pi/2 is down
        /// to the ground.</param>
        public CameraInfo(Vector3 position, float yawRadians, float pitchRadians)
        {
            Position = position;
            Direction = DirectionFrom(yawRadians, pitchRadians);
            Yaw = yawRadians;
            Pitch = ClampPitch(pitchRadians);
        }

        /// <summary>
        /// A copy constructor, which does a deep copy.
        /// </summary>
        /// <param name="info">The camera info to copy from.</param>
        public CameraInfo(CameraInfo info)
        {
            Position = info.Position;
            Direction = DirectionFrom(info.Yaw, info.Pitch);
            Yaw = info.Yaw;
            Pitch = ClampPitch(info.Pitch);
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

        /// <summary>
        /// Clamps the pitch to the (-pi/2, pi/2) range.
        /// </summary>
        /// <param name="pitchRadians">The pitch in radians.</param>
        /// <returns>The clamped pitch.</returns>
        public static float ClampPitch(float pitchRadians)
        {
            // We want to avoid looking straight up or down, if there is a
            // danger of gimbal lock.
            float notQuiteVertical = MathHelper.HalfPi - 0.001f;
            return Math.Clamp(pitchRadians, -notQuiteVertical, notQuiteVertical);
        }

        internal void MoveForward(float amount) => Position += (amount * Direction);

        internal void MoveBackward(float amount) => MoveForward(-amount);

        internal void MoveLeft(float amount)
        {
            Vector3 pos = amount * Direction;
            Position = new Vector3(Position.X - pos.Y, Position.Y + pos.X, Position.Z);
        }

        internal void MoveRight(float amount) => MoveLeft(-amount);

        internal void MoveUp(float amount) => Position = new Vector3(Position.X, Position.Y, Position.Z + amount);

        internal void MoveDown(float amount) => MoveUp(-amount);

        /// <summary>
        /// Performs an interpolation with this value (as the start) and some
        /// next value (the ending point..
        /// </summary>
        /// <param name="next">The value to interpolate towards.</param>
        /// <param name="t">The interpolation time, intended for the [0, 1]
        /// range.</param>
        /// <param name="useLatestAngles">True if the yaw/pitch from `next`
        /// should be used, false if they should be interpolated.</param>
        /// <returns>The info for the interpolated time.</returns>
        public CameraInfo InterpolateWith(CameraInfo next, float t, bool useLatestAngles)
        {
            Vector3 pos = Position.Interpolate(next.Position, t);
            float yaw = (useLatestAngles ? next.Yaw : MathHelper.Interpolate(Yaw, next.Yaw, t));
            float pitch = (useLatestAngles ? next.Pitch : MathHelper.Interpolate(Pitch, next.Pitch, t));
            return new CameraInfo(pos, yaw, pitch);
        }

        public override string ToString() => $"{Position}, yaw={Yaw}, pitch={Pitch}";
    }

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
        public static readonly OpenTK.Vector3 UpOpenTk = Up.ToOpenTKVector();

        /// <summary>
        /// The current position of the camera. Unless you want bleeding edge
        /// updates from this, use <see cref="Interpolated(float)"/> instead.
        /// </summary>
        private CameraInfo Current;

        /// <summary>
        /// Gets the last position upon ticking. This is used as the base point
        /// when calling <see cref="Interpolated(float)"/>.
        /// </summary>
        private CameraInfo Previous;

        /// <summary>
        /// Creates a camera at the origin facing north.
        /// </summary>
        public Camera() : this(new Vector3(0, 0, 0), MathHelper.HalfPi)
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
            Current = new CameraInfo(position, yawRadians, pitchRadians);
            Previous = Current;
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
        /// <param name="aspectRatio"></param>
        /// <returns></returns>
        public static float FieldOfViewXToY(float fovX, float aspectRatio)
        {
            return 2 * (float)Math.Atan((float)Math.Tan(fovX / 2) / aspectRatio);
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

        /// <summary>
        /// Clamps the pitch to the (-pi/2, pi/2) range.
        /// </summary>
        /// <param name="pitchRadians">The pitch in radians.</param>
        /// <returns>The clamped pitch.</returns>
        public static float ClampPitch(float pitchRadians)
        {
            // We want to avoid looking straight up or down, if there is a
            // danger of gimbal lock.
            float notQuiteVertical = MathHelper.HalfPi - 0.001f;
            return Math.Clamp(pitchRadians, -notQuiteVertical, notQuiteVertical);
        }

        /// <summary>
        /// Ticks the camera, which will advance it from the previous position
        /// to the current.
        /// </summary>
        public void Tick()
        {
            Previous = Current;
            Current = new CameraInfo(Current);
        }

        public void AddToYaw(float delta) => Current.Yaw += delta;

        public void AddToPitch(float delta) => Current.Pitch += delta;

        public void MoveForward(float amount) => Current.MoveForward(amount);

        public void MoveBackward(float amount) => Current.MoveBackward(amount);

        public void MoveLeft(float amount) => Current.MoveLeft(amount);

        public void MoveRight(float amount) => Current.MoveRight(amount);

        public void MoveUp(float amount) => Current.MoveUp(amount);

        public void MoveDown(float amount) => Current.MoveDown(amount);

        /// <summary>
        /// Gets the interpolated camera information from this objects current
        /// and previous states.
        /// </summary>
        /// <param name="t">The interpolation time, usually between 0 and 1.
        /// </param>
        /// <param name="useLatestAngles">True if the yaw/pitch from the latest
        /// should be used, false if they should be interpolated.</param>
        /// <returns>The interpolated camera information.</returns>
        public CameraInfo Interpolated(float t, bool useLatestAngles = false)
        {
            return Previous.InterpolateWith(Current, t, useLatestAngles);
        }

        /// <summary>
        /// Creates a view matrix for a camera information object.
        /// </summary>
        /// <param name="cameraInfo">The information to make the view matrix 
        /// from.</param>
        /// <returns>The view matrix for the camera information.</returns>
        public static OpenTK.Matrix4 ViewMatrix(CameraInfo cameraInfo)
        {
            OpenTK.Vector3 pos = cameraInfo.Position.ToOpenTKVector();
            OpenTK.Vector3 eye = pos + cameraInfo.Direction.ToOpenTKVector();
            return OpenTK.Matrix4.LookAt(pos, eye, UpOpenTk);
        }
    }
}
