using System;

namespace Helion.Menus.Base.Text;

public class MenuSmallTextComponent : MenuTextComponent
{
    public MenuSmallTextComponent(string text, Func<Menu?>? action = null) :
        base(text, 8, "SmallFont", action)
    {
    }
}
