using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Menus.Base;

/// <summary>
/// A padding element which consumes vertical space.
/// </summary>
public class MenuPaddingComponent : IMenuComponent
{
    /// <summary>
    /// How many pixels the padding should be vertically.
    /// </summary>
    public readonly int PixelAmount;
    public Func<Menu?>? Action => null;

    public MenuPaddingComponent(int pixelAmount)
    {
        Precondition(pixelAmount >= 0, "Should have a positive menu padding size");

        PixelAmount = pixelAmount;
    }
}
