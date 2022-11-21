using System;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context.Types;

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
