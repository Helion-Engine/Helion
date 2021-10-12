using System;
using System.Drawing;
using Helion.Graphics.String;

namespace Helion.Menus.Base.Text;

public class MenuSmallTextComponent : MenuTextComponent
{
    public MenuSmallTextComponent(string text, Func<Menu?>? action = null) :
        this(ColoredStringBuilder.From(Color.Red, text), action)
    {
    }

    public MenuSmallTextComponent(ColoredString text, Func<Menu?>? action = null) :
        base(text, 8, "SmallFont", action)
    {
    }
}
