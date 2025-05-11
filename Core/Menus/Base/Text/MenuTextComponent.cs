using Helion.Render.Common.Enums;
using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Menus.Base.Text;

/// <summary>
/// A menu component that is made up of text.
/// </summary>
public abstract class MenuTextComponent : IMenuComponent
{
    public readonly string Text;
    public readonly int Size;
    public readonly string FontName;
    public readonly Align? Align;
    public readonly bool LineWrap;
    public Func<Menu?>? Action { get; }

    public MenuTextComponent(string text, int size, string fontName, Func<Menu?>? action = null, Align? align = null, 
        bool lineWrap = false)
    {
        Precondition(size > 0, "Cannot have a zero or negative font size");

        Text = text;
        Size = size;
        FontName = fontName;
        Action = action;
        Align = align;
        LineWrap = lineWrap;
    }

    public override string ToString() => Text.ToString() ?? string.Empty;
}
