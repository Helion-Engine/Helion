using System.Collections.Generic;

namespace Helion.Models;

public class GameFilesModel
{
    public readonly FileModel IWad;
    public readonly IList<FileModel> Files;

    public GameFilesModel(FileModel iwad, IList<FileModel> files)
    {
        IWad = iwad;
        Files = files;
    }
}
