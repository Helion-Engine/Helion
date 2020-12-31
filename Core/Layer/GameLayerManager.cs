using Helion.Input;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Terminals;

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
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Terminal m_console;

        protected override CIString Name => string.Empty;
        protected override double Priority => 0.5;

        public GameLayerManager(Config config, ArchiveCollection archiveCollection, Terminal console)
        {
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
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

                    ConsoleLayer consoleLayer = new(m_archiveCollection, m_console);
                    Add(consoleLayer);
                }
            }

            base.HandleInput(consumableInput);
        }
    }
}