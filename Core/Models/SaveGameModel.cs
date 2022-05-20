using System;

namespace Helion.Models;

public class SaveGameModel
{
    public readonly GameFilesModel Files;
    public readonly string Text;
    public readonly string MapName;
    public readonly DateTime Date;
    public readonly string WorldFile;

    public SaveGameModel(GameFilesModel files, string text, string mapName, DateTime date, string worldFile)
    {
        Files = files;
        Text = text;
        MapName = mapName;
        Date = date;
        WorldFile = worldFile;
    }
}
