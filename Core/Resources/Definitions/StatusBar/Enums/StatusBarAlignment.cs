using System;

namespace Helion.Resources.Definitions.StatusBar.Enums;

[Flags]
public enum StatusBarAlignment
{
    // Horizontal
    Left = 0,
    HCenter = 1,
    Right = 2,
    
    // Vertical
    Top = 0,
    VCenter = 4,
    Bottom = 8,
    
    // SBARDEF v1.1.1 / 1.2 - Ignore Offsets
    IgnoreLeftOffset = 16,
    IgnoreTopOffset = 32,
    
    // SBARDEF v1.2 - Widescreen
    WidescreenLeft = 64,
    WidescreenRight = 128
}