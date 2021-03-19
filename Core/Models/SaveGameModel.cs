using System;

namespace Helion.Models
{
    public class SaveGameModel
    {
        public GameFilesModel Files { get; set; }

        public string Text { get; set; } = string.Empty;
        public string MapName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string WorldFile { get; set; } = string.Empty;
    }
}
