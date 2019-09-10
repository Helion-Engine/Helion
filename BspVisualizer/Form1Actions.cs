using System;
using System.Drawing;
using Helion.Util.Geometry.Vectors;

namespace BspVisualizer
{
    public partial class Form1
    {
        private static float MovementFactor = 64.0f;
        private static float MaxMapUnitsPerPixel = 2048.0f;
        private static float MinMapUnitsPerPixel = 1.0f / MaxMapUnitsPerPixel;

        /// <summary>
        /// Takes a point in the world coordinates and transforms it to the
        /// window coordinates.
        /// </summary>
        /// <param name="point">The point to transform.</param>
        /// <returns>The window coordinate for the point.</returns>
        private Point TransformPoint(Vec2D vertex)
        {
            return new Point((int)((vertex.X - camera.X) * zoom), (int)((-vertex.Y + camera.Y) * zoom));
        }

        private void MoveCameraUp()
        {
            camera.Y += (int)(MovementFactor / zoom);
        }

        private void MoveCameraLeft()
        {
            camera.X -= (int)(MovementFactor / zoom);
        }

        private void MoveCameraDown()
        {
            camera.Y -= (int)(MovementFactor / zoom);
        }

        private void MoveCameraRight()
        {
            camera.X += (int)(MovementFactor / zoom);
        }

        private void ZoomOut()
        {
            zoom = Math.Clamp(zoom / 2, MinMapUnitsPerPixel, MaxMapUnitsPerPixel);
        }

        private void ZoomIn()
        {
            zoom = Math.Clamp(zoom * 2, MinMapUnitsPerPixel, MaxMapUnitsPerPixel);
        }
    }
}
