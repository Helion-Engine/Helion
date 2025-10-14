using Helion.Render.Common.Enums;
using System;
using static Helion.Util.Constants;

namespace Helion.Menus.Base.Text;

public class MenuSmallTextComponent(string text, Func<Menu?>? action = null, Align? align = null, bool lineWrap = false) : 
    MenuTextComponent(text, 8, Fonts.Small, action, align, lineWrap)
{
}
