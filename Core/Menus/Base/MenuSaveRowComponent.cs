using Helion.World.Save;
using System;

namespace Helion.Menus.Base;

public class MenuSaveRowComponent : IMenuComponent
{
    public string Text { get; set; }
    public string MapName { get; set; }
    public Func<Menu?>? Action { get; set; }
    public Func<Menu?>? DeleteAction { get; }
    public SaveGame? SaveGame { get; }
    public bool IsAutoOrQuickSave { get; }

    public MenuSaveRowComponent(string text, string mapName, bool isAutoOrQuickSave, Func<Menu?>? action = null,
        Func<Menu?>? deleteAction = null, SaveGame? saveGame = null)
    {
        Text = text;
        MapName = mapName;
        Action = action;
        DeleteAction = deleteAction;
        SaveGame = saveGame;
        IsAutoOrQuickSave = isAutoOrQuickSave;
    }
}
