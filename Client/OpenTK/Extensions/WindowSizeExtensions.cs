using System;
using Helion.Window;
using OpenTK;

namespace Helion.Client.OpenTK.Extensions
{
    public static class WindowSizeExtensions
    {
        public static WindowState ToOpenTKWindowState(this WindowStatus windowSize)
        {
            switch (windowSize)
            {
            case WindowStatus.Windowed:
                return WindowState.Normal;
            case WindowStatus.Fullscreen:
                return WindowState.Fullscreen;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Unknown WindowSize enumeration");
            }
        }
    }
}
