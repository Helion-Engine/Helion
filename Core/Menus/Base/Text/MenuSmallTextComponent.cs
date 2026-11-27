using Helion.Render.Common.Enums;
using System;
using static Helion.Util.Constants;

namespace Helion.Menus.Base.Text;

public class MenuSmallTextSaveComponent(string text, Func<Menu?>? action = null, Align? align = null, bool lineWrap = false, string font = Fonts.Options) :
    MenuSmallTextComponent(text, action, align, lineWrap, font)
{
}


public class MenuSmallTextComponent(string text, Func<Menu?>? action = null, Align? align = null, bool lineWrap = false, string font = Fonts.Small) : 
    MenuTextComponent(text, 8, font, action, align, lineWrap)
{
}
