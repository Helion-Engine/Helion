using Helion.Window;
using NLog;
using OpenTK;

namespace Helion.Client
{
    public static class VerticalSyncExtensions
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
                Log.Error("Unknown VSync type, defauling to Off");
                goto case VerticalSync.Off;
            }
        }
    }
}
