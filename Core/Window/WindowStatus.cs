using System;
using OpenTK;

namespace Helion.Window
{
    public enum WindowSize
    {
        Windowed,
        Fullscreen
    }

    public static class WindowSizeExtensions
    {
        public static WindowState ToOpenTKWindowState(this WindowSize windowSize)
        {
            switch (windowSize)
            {
            case WindowSize.Windowed:
                return WindowState.Normal;
            case WindowSize.Fullscreen:
                return WindowState.Fullscreen;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Unknown WindowSize enumeration");
            }
        }
    }
}