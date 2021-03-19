using Helion.Util;
using System.Collections.Generic;
using System.Drawing;

namespace Helion.Resources.Definitions.Locks
{
    public class LockDef
    {
        public int KeyNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RemoteMessage { get; set; } = string.Empty;
        public Color MapColor { get; set; }
        public List<CIString> KeyDefinitionNames { get; set; } = new List<CIString>();
    }
}
