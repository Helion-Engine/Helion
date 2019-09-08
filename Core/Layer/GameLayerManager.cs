using Helion.Audio;
using Helion.Input;
using Helion.Util;
using Helion.Util.Configuration;

namespace Helion.Layer
{
    /// <summary>
    /// A top level concrete implementation of the game layer.
    /// </summary>
    /// <remarks>
    /// This exists because we want to use the abstract method to force any
    /// child classes to remember to implement priority (as it affects a lot!)
    /// but that leaves us with no way to instantiate an instance of it. This
    /// is meant to be the root in the tree of nodes, so the priority also does
    /// not matter.
    /// </remarks>
    public class GameLayerManager : GameLayer
    {
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly IAudioSystem m_audioSystem;

        protected override CIString Name => string.Empty;
        protected override double Priority => 0.5;

        public GameLayerManager(Config config, HelionConsole console, IAudioSystem audioSystem)
        {
            m_config = config;
            m_console = console;
            m_audioSystem = audioSystem;
        }

        public override void HandleInput(ConsumableInput consumableInput)
        {
            if (consumableInput.ConsumeKeyPressed(m_config.Engine.Controls.Console))
            {
                if (Contains(ConsoleLayer.LayerName))
                    RemoveByName(ConsoleLayer.LayerName);
                else
                {
                    // Don't want input that opened the console to be something
                    // added to the console, so first we clear all characters.
                    consumableInput.ConsumeTypedCharacters();
                    Add(new ConsoleLayer(m_console));
                }
            }
            
            base.HandleInput(consumableInput);
        }
    }
}