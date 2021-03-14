using System;

namespace Helion.Models
{
    public class SaveGameModel
    {
        public string Text { get; set; }
        public string MapName { get; set; }
        public DateTime Date { get; set; }
        public string WorldFile { get; set; }
    }
}
