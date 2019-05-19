using Helion.Util.Geometry;
using System;

namespace BspVisualizer
{
    public partial class Form1
    {
        private static double MaxMapUnitsPerPixel = 2048.0;
        private static double MinMapUnitsPerPixel = 1.0 / MaxMapUnitsPerPixel;

        private Vec2I camera = new Vec2I(0, 0);
        private double mapUnitsPerPixel = 16.0;

        private void MoveCameraUp()
        {
            camera.Y += (int)(mapUnitsPerPixel / 2);
        }

        private void MoveCameraLeft()
        {
            camera.X -= (int)(mapUnitsPerPixel / 2);
        }

        private void MoveCameraDown()
        {
            camera.Y -= (int)(mapUnitsPerPixel / 2);
        }

        private void MoveCameraRight()
        {
            camera.X += (int)(mapUnitsPerPixel / 2);
        }

        private void ZoomOut()
        {
            mapUnitsPerPixel = Math.Clamp(mapUnitsPerPixel * 2, MinMapUnitsPerPixel, MaxMapUnitsPerPixel);
        }

        private void ZoomIn()
        {
            mapUnitsPerPixel = Math.Clamp(mapUnitsPerPixel / 2, MinMapUnitsPerPixel, MaxMapUnitsPerPixel);
        }
    }
}
