using Helion.World.Save;
using System;

namespace Helion.Menus.Base;

public class MenuSaveRowComponent : IMenuComponent
{
    public string Text { get; }
    public Func<Menu?>? Action { get; }
    public Func<Menu?>? DeleteAction { get; }
    public SaveGame? SaveGame { get; }

    public MenuSaveRowComponent(string text, Func<Menu?>? action = null,
        Func<Menu?>? deleteAction = null, SaveGame? saveGame = null)
    {
        Text = text;
        Action = action;
        DeleteAction = deleteAction;
        SaveGame = saveGame;
    }
}
