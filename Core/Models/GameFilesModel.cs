using System;
using System.Collections.Generic;

namespace Helion.Models;

public struct GameFilesModel
{
    public GameFilesModel()
    {
    }

    public FileModel IWad { get; set; }
    public IList<FileModel> Files { get; set; } = Array.Empty<FileModel>();
}
