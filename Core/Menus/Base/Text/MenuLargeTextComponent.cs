using System.Drawing;
using Helion.Graphics.String;

namespace Helion.Menus.Base.Text;

public class MenuLargeTextComponent : MenuTextComponent
{
    public MenuLargeTextComponent(string text) :
        this(ColoredStringBuilder.From(Color.Red, text))
    {
    }

    public MenuLargeTextComponent(ColoredString text) :
        base(text, 24, "BigFont")
    {
    }
}
