using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineFilesConfig 
    {
        public readonly ConfigValue<string> Directories = new ConfigValue<string>(";");
    }
}