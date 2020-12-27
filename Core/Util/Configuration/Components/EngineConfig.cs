using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigSection]
    public class EngineConfig
    {
        public readonly EngineConsoleConfig Console = new();
        public readonly EngineDeveloperConfig Developer = new();
        public readonly EngineFilesConfig Files = new();
        public readonly EngineGameConfig Game = new();
        public readonly EngineGameplayConfig Gameplay = new();
        public readonly EngineHudConfig Hud = new();
        public readonly EngineMouseConfig Mouse = new();
        public readonly EngineControlConfig Controls = new();
        public readonly EngineRenderConfig Render = new();
        public readonly EngineWindowConfig Window = new();
        public readonly EngineAudioConfig Audio = new();
    }
}