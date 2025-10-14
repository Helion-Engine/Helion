using System;
using System.Runtime;

namespace Helion.Util;

public static class GCUtil
{
    public static void SetDefaultLatencyMode() => GCSettings.LatencyMode = GCLatencyMode.Interactive;

    public static void SetGameplayLatencyMode() => GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

    public static void ForceGarbageCollection(int forceCount = 1)
    {
        for (int i = 0; i < forceCount; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }
}
