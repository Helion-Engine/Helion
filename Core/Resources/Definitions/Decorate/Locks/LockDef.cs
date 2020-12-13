using Helion.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Helion.Resources.Definitions.Decorate.Locks
{
    public class LockDef
    {
        public int KeyNumber;
        public string Message;
        public string RemoteMessage;
        public Color MapColor;
        public List<CIString> KeyDefinitionNames = new List<CIString>();
    }
}
