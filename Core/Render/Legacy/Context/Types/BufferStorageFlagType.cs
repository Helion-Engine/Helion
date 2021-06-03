using System;

namespace Helion.Render.Legacy.Context.Types
{
    [Flags]
    public enum BufferStorageFlagType
    {
        None = 0,
        MapRead = 1,
        MapWrite = 2,
        MapPersistent = 64,
        MapCoherent = 128,
        DynamicStorage = 256,
        ClientStorage = 512,
    }
}