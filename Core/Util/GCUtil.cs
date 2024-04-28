using System;

namespace Helion.Util;

public static class GCUtil
{
    public static void ForceGarbageCollection()
    {
        for (int i = 0; i < 4; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }
}
