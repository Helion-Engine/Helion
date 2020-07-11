using System.Collections.Generic;

namespace Helion.Util
{
    public class CommandArg
    {
        public CommandArg(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public List<string> Values { get; } = new List<string>();
    }
}
