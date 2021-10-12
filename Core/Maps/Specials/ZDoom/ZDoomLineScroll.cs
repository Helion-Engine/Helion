using System;

namespace Helion.Maps.Specials.ZDoom;

[Flags]
public enum ZDoomLineScroll
{
    All,
    UpperTexture = 1,
    MiddleTexture = 2,
    LowerTexture = 4,
}
