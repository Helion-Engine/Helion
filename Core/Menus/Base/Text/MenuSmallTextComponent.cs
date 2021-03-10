using Helion.Graphics.String;
using SixLabors.ImageSharp;

namespace Helion.Menus.Base.Text
{
    public class MenuSmallTextComponent : MenuTextComponent
    {
        public MenuSmallTextComponent(string text) : 
            this(ColoredStringBuilder.From(Color.Red, text))
        {
        }
        
        public MenuSmallTextComponent(ColoredString text) : 
            base(text, 16, "SmallFont")
        {
        }
    }
}
