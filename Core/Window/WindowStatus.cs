using System;
using OpenTK;

namespace Helion.Window
{
    public enum WindowStatus
    {
        Windowed,
        Fullscreen
    }

    public static class WindowStatusExtensions
    {
        public static WindowState ToOpenTKWindowState(this WindowStatus windowStatus)
        {
            switch (windowStatus)
            {
            case WindowStatus.Windowed:
                return WindowState.Normal;
            case WindowStatus.Fullscreen:
                return WindowState.Fullscreen;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowStatus), windowStatus, "Unknown WindowSize enumeration");
            }
        }
    }
}