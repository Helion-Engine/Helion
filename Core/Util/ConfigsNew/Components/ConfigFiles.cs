using System.Collections.Generic;
using System.Linq;
using Helion.Util.ConfigsNew.Values;

namespace Helion.Util.ConfigsNew.Components
{
    public class ConfigFiles
    {
        [ConfigInfo("Locations to look for archives. Earlier paths are checked before later ones.")]
        public readonly ConfigValue<List<string>> Directories = new(new List<string> { ".", "wads" },
            newValue =>
            {
                // We do not want empty paths, and we want to be friendly to the
                // users by removing accidental padding.
                List<string> copy = newValue.ToList()
                    .Select(s => s.Trim())
                    .Where(s => s != "").ToList();
                
                // We should always search in the current directory, and this
                // also makes sure the list has at least one element. By adding
                // it at the end, this also gives the user control to provide
                // other matching directories first instead of the current one.
                if (!copy.Contains("."))
                    copy.Add(".");

                return copy;
            });
    }
}
