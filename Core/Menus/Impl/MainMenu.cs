using System;
using Helion.Menus.Base;

namespace Helion.Menus.Impl
{
    public class MainMenu : Menu
    {
        private const string ActiveImage = "M_SKULL1";
        private const string InactiveImage = "M_SKULL2";
        private static readonly Func<Menu?>? TodoAction = () => null;
        
        public MainMenu() : base(16)
        {
            Components = Components.AddRange(new IMenuComponent[] 
            {
                new MenuImageComponent("M_DOOM", paddingY: 4),
                new MenuImageComponent("M_NGAME", -6, 1, ActiveImage, InactiveImage, TodoAction),
                new MenuImageComponent("M_OPTION", -15, 1, ActiveImage, InactiveImage, TodoAction),
                new MenuImageComponent("M_LOADG", 1, 1, ActiveImage, InactiveImage, TodoAction),
                new MenuImageComponent("M_SAVEG", 1, 1, ActiveImage, InactiveImage, TodoAction),
                new MenuImageComponent("M_QUITG", -3, 0, ActiveImage, InactiveImage, TodoAction)
            });

            SetToFirstActiveComponent();
        }
    }
}
