using Helion.Render.Commands;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Drawers.Helper
{
    /// <summary>
    /// Helpers for drawing the doom HUD.
    /// </summary>
    public static class DoomHudHelper
    {
        public const int DoomDrawWidth = 320;
        public const int DoomDrawHeight = 200;
        public const int DoomResolutionWidth = 640;
        public const int DoomResolutionHeight = 480;
        private const float DoomViewAspectRatio = DoomResolutionWidth / (float)DoomResolutionHeight;
        public static readonly Dimension DoomResolution = new(DoomDrawWidth, DoomDrawHeight);

        public static readonly ResolutionInfo DoomResolutionInfoCenter = new()
        {
            VirtualDimensions = DoomResolution,
            Scale = ResolutionScale.Center,
            AspectRatio = DoomViewAspectRatio
        };

        public static readonly ResolutionInfo DoomResolutionInfo = new()
        {
            VirtualDimensions = DoomResolution,
            Scale = ResolutionScale.None,
            AspectRatio = DoomViewAspectRatio
        };
    }
}
