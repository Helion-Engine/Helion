using System;
using Helion.Render.Legacy.Commands.Alignment;

namespace Helion.Menus.Base;

public class MenuImageComponent : IMenuComponent
{
    public readonly string ImageName;
    public readonly int OffsetX;
    public readonly int PaddingTopY;
    public readonly int PaddingBottomY;
    public readonly string? ActiveImage;
    public readonly string? InactiveImage;
    public readonly Align ImageAlign;
    public readonly bool AddToOffsetY;
    public readonly int? OverrideY;

    public Func<Menu?>? Action { get; }

    public MenuImageComponent(string imageName, int offsetX = 0, int paddingTopY = 0,
        string? activeImage = null, string? inactiveImage = null, Func<Menu?>? action = null,
        Align imageAlign = Align.TopMiddle, int paddingBottomY = 0, bool addToOffsetY = true, int? overrideY = null)
    {
        ImageName = imageName;
        OffsetX = offsetX;
        PaddingTopY = paddingTopY;
        PaddingBottomY = paddingBottomY;
        ActiveImage = activeImage;
        InactiveImage = inactiveImage;
        Action = action;
        ImageAlign = imageAlign;
        AddToOffsetY = addToOffsetY;
        OverrideY = overrideY;
    }
}
