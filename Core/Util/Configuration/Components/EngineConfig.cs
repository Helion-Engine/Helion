using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigSection]
    public class EngineConfig
    {
        public readonly EngineConsoleConfig Console = new EngineConsoleConfig();
        public readonly EngineFilesConfig Files = new EngineFilesConfig();
        public readonly EngineGameConfig Game = new EngineGameConfig();
        public readonly EngineGameplayConfig Gameplay = new EngineGameplayConfig();
        public readonly EngineDeveloperConfig Developer = new EngineDeveloperConfig();
        public readonly EngineMouseConfig Mouse = new EngineMouseConfig();
        public readonly EngineControlConfig Controls = new EngineControlConfig();
        public readonly EngineRenderConfig Render = new EngineRenderConfig();
        public readonly EngineWindowConfig Window = new EngineWindowConfig();
    }
}