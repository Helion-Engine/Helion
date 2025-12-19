using System;

namespace Helion.Util;

public static class GCNotifier
{
    public static event Action? GarbageCollected;

    private sealed class Sentinel
    {
        ~Sentinel()
        {
            GarbageCollected?.Invoke();
            if (GarbageCollected != null)
                _ = new Sentinel();
        }
    }

    public static void Start() => _ = new Sentinel();
}
