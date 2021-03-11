using System;
using Helion.Menus.Base;

namespace Helion.Menus.Impl
{
    public class MainMenu : Menu
    {
        private static readonly Func<Menu?> TodoAction = () => null;
        
        public MainMenu() : base(8)
        {
            Components = Components.AddRange(new[] 
            {
                new MenuImageComponent("M_DOOM", paddingY: 8),
                CreateMenuOption("M_NGAME", -6, 2, () => new NewGameMenu()),
                CreateMenuOption("M_OPTION", -15, 2, TodoAction),
                CreateMenuOption("M_LOADG", 1, 2, TodoAction),
                CreateMenuOption("M_SAVEG", 1, 2, TodoAction),
                CreateMenuOption("M_QUITG", -3, 2, TodoAction)
            });

            SetToFirstActiveComponent();

            IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?> action)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action);
            }
        }
    }
}
