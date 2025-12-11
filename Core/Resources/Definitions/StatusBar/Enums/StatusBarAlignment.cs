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
    
    // SBARDEF v1.1 - Extensions
    DynamicLeft = 16,
    DynamicRight = 32
}