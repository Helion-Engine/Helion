using System;

namespace Helion.Util;

public static class GCUtil
{
    public static void ForceGarbageCollection(int forceCount = 1)
    {
        for (int i = 0; i < forceCount; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }
}
