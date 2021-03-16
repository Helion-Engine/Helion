using System;

namespace Helion.Menus.Base
{
    public class MenuSaveRowComponent : IMenuComponent
    {
        public const int PixelWidth = 200;
            
        public string Text { get; }
        public Func<Menu?>? Action { get; }

        public MenuSaveRowComponent(string text, Func<Menu?>? action = null)
        {
            Text = text;
            Action = action;
        }
    }
}
