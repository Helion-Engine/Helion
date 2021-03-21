using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MapInfo
{
    public class GameInfoDef
    {
        public IList<string> CreditPages { get; set; } = Array.Empty<string>();
        public IList<string> QuitMessages { get; set; } = Array.Empty<string>();
        public IList<string> FinalePages { get; set; } = Array.Empty<string>();
        public IList<string> InfoPages { get; set; } = Array.Empty<string>();
        public Dictionary<int, List<string>> WeaponSlots { get; set; } = new Dictionary<int, List<string>>();
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
