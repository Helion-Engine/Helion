using Helion.World.Save;
using Helion.World.Util;
using System;

namespace Helion.Menus.Base;

public class MenuSaveRowComponent(
    string text,
    string mapName,
    bool isAutoOrQuickSave,
    SaveVerificationResult verificationResult,
    Func<Menu?>? action = null,
    Func<Menu?>? deleteAction = null,
    SaveGame? saveGame = null) : IMenuComponent
{
    public string Text { get; set; } = text;
    public string MapName { get; set; } = mapName;
    public Func<Menu?>? Action { get; set; } = action;
    public Func<Menu?>? DeleteAction { get; } = deleteAction;
    public SaveGame? SaveGame { get; } = saveGame;
    public bool IsAutoOrQuickSave { get; } = isAutoOrQuickSave;
    public SaveVerificationResult VerificationResult { get; } = verificationResult;
}
