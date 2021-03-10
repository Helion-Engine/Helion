using Helion.Menus.Base.Text;

namespace Helion.Menus.Impl
{
    public class MainMenu : Menu
    {
        public MainMenu()
        {
            Components = Components.AddRange(new[] {
                new MenuLargeTextComponent("New Game"),
                new MenuLargeTextComponent("Load Game"),
                new MenuLargeTextComponent("Save Game"),
                new MenuLargeTextComponent("Options"),
                new MenuLargeTextComponent("Exit Game")
            });
        }
    }
}
