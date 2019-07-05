using NLog;
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
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

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
                log.Error("Unknown VSync type, defauling to Off");
                goto case VerticalSync.Off;
            }
        }
    }
}