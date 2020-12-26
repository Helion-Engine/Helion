using Helion.Render.Commands;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.Shared.Drawers.Helper
{
    /// <summary>
    /// Helpers for drawing the doom HUD.
    /// </summary>
    public static class DoomHudHelper
    {
        public const int DoomResolutionWidth = 320;
        public const int DoomResolutionHeight = 200;
        private const float DoomViewAspectRatio = 640.0f / 480.0f;
        private const float DoomDrawWidth = 320.0f;
        private const float DoomDrawHeight = 200.0f;
        public static readonly Dimension DoomResolution = new(DoomResolutionWidth, DoomResolutionHeight);
        public static readonly ResolutionInfo DoomResolutionInfo = new()
        {
            VirtualDimensions = DoomResolution,
            Scale = ResolutionScale.Center
        };

        public static void ScaleImageDimensions(Dimension viewport, ref int width, ref int height)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            width = (int)(width * scaleWidth);
            height = (int)(height * scaleHeight);
        }

        public static void ScaleImageOffset(Dimension viewport, ref int x, ref int y)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            x = (int)(x * scaleWidth) + (int)(viewport.Width - viewWidth);
            y = (int)(y * scaleHeight);
        }

        public static Vec2I ScaleWorldOffset(Dimension viewport, in Vec2D offset)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            return new Vec2I((int)(offset.X * scaleWidth), (int)(offset.Y * scaleHeight));
        }
    }
}
