using System;
using OpenTK;

namespace Helion.Window
{
    public enum VerticalSync
    {
        On,
        Adaptive,
        Off
    }

    public static class VerticalSyncExtensions
    {
        public static VSyncMode ToOpenTKVSync(this VerticalSync vsync)
        {
            switch (vsync)
            {
            case VerticalSync.On:
                return VSyncMode.On;
            case VerticalSync.Off:
                return VSyncMode.Off;
            case VerticalSync.Adaptive:
                return VSyncMode.Adaptive;
            default:
                throw new ArgumentOutOfRangeException(nameof(vsync), vsync, "Unknown vsync enumeration");
            }
        }
    }
}