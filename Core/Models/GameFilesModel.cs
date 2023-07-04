using System;
using System.Collections.Generic;

namespace Helion.Models;

public class GameFilesModel
{
    public FileModel IWad { get; set; } = null!;
    public IList<FileModel> Files { get; set; } = Array.Empty<FileModel>();
}
