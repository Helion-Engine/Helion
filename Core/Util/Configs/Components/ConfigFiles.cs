using System.Collections.Generic;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with files on the host system.")]
    public class ConfigFiles
    {
        [ConfigInfo("Locations to look for archives. Earlier paths are checked before later ones.")]
        public readonly ConfigValueString Directories = new(".;wads");
    }
}
