using System;
using Helion.Util;

namespace Helion.Menus.Base
{
    public class MenuImageComponent : IMenuComponent
    {
        public readonly CIString ImageName;
        public readonly int OffsetX;
        public readonly int PaddingY;
        public readonly string? ActiveImage;
        public readonly string? InactiveImage;
        public Func<Menu?>? Action { get; }
        public bool PlaySelectedSound { get; private set; }

        public MenuImageComponent(CIString imageName, int offsetX = 0, int paddingY = 0, 
            string? activeImage = null, string? inactiveImage = null, Func<Menu?>? action = null,
            bool playSelectedSound = true)
        {
            ImageName = imageName;
            OffsetX = offsetX;
            PaddingY = paddingY;
            ActiveImage = activeImage;
            InactiveImage = inactiveImage;
            Action = action;
            PlaySelectedSound = playSelectedSound;
        }
    }
}
