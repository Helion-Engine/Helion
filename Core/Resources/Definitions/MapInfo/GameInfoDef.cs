using System.Collections.Generic;

namespace Helion.Resources.Definitions.MapInfo
{
    public class GameInfoDef
    {
        public List<string> CreditPages = new List<string>();
        public List<string> QuitMessages = new List<string>();
        public List<string> FinalePages = new List<string>();
        public List<string> InfoPages = new List<string>();
        public string TitleMusic { get; set; } = string.Empty;
        public string FinaleMusic { get; set; } = string.Empty;
        public string FinaleFlat { get; set; } = string.Empty;
        public string QuitSound { get; set; } = string.Empty;
        public string BorderFlat { get; set; } = string.Empty;
        public string IntermissionMusic { get; set; } = string.Empty;
        public int TitleTime { get; set; } = 5;
        public bool DrawReadThis { get; set; }
    }
}
