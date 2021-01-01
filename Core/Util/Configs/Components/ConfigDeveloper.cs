using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Developer options for debugging the engine.")]
    public class ConfigDeveloper
    {
        [ConfigInfo("Whether garbage collection status should be printed.")]
        public readonly ConfigValueBoolean GCStats = new();

        [ConfigInfo("If rendering should have debugging information drawn.")]
        public readonly ConfigValueBoolean RenderDebug = new();
    }
}
