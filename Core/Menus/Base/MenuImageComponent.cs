using System;
using Helion.Render.OpenGL.Legacy.Commands.Alignment;

namespace Helion.Menus.Base
{
    public class MenuImageComponent : IMenuComponent
    {
        public readonly string ImageName;
        public readonly int OffsetX;
        public readonly int PaddingY;
        public readonly string? ActiveImage;
        public readonly string? InactiveImage;
        public readonly Align ImageAlign;
        public Func<Menu?>? Action { get; }

        public MenuImageComponent(string imageName, int offsetX = 0, int paddingY = 0, 
            string? activeImage = null, string? inactiveImage = null, Func<Menu?>? action = null,
            Align imageAlign = Align.TopMiddle)
        {
            ImageName = imageName;
            OffsetX = offsetX;
            PaddingY = paddingY;
            ActiveImage = activeImage;
            InactiveImage = inactiveImage;
            Action = action;
            ImageAlign = imageAlign;
        }
    }
}
