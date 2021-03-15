using System;
using System.Collections.Generic;

namespace Helion.Models
{
    public class GameFilesModel
    {
        public static readonly GameFilesModel Default = new GameFilesModel();

        public FileModel IWad { get; set; } = FileModel.Default;
        public IList<FileModel> Files { get; set; } = Array.Empty<FileModel>();
    }
}
