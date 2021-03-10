using Helion.Input;
using Helion.Layer.WorldLayers;
using Helion.Menus.Impl;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;

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
        private readonly HelionConsole m_console;

        protected override double Priority => 0.5;

        public GameLayerManager(Config config, ArchiveCollection archiveCollection, HelionConsole console)
        {
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
        }

        public override void Add(GameLayer layer)
        {
            base.Add(layer);
            
            if (layer is SinglePlayerWorldLayer)
                foreach (GameLayer existingLayer in Layers.Values)
                    Remove(existingLayer.GetType());    
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.ConsumeTypedKey(m_config.Controls.Console))
                HandleConsoleToggle(input);
            
            if (HasOnlyTitlepicLayer() && input.HasAnyKeyPressed())
            {
                input.ConsumeAll();
                
                MainMenu mainMenu = new();
                MenuLayer menuLayer = new(this, mainMenu, m_archiveCollection);
                Add(menuLayer);
            }

            base.HandleInput(input);
        }

        private bool HasOnlyTitlepicLayer() => Count == 1 && Contains<TitlepicLayer>();

        private void HandleConsoleToggle(InputEvent input)
        {
            // Due to the workaround above, we also want to prune it from
            // anyone else's visibility.
            input.ConsumeKeyPressedOrDown(m_config.Controls.Console);

            if (Contains<ConsoleLayer>())
            {
                Remove<ConsoleLayer>();
                
                // TODO: Only resume if the world layer is the top layer now after removing the above.
                if (TryGetLayer(out SinglePlayerWorldLayer? layer))
                    layer.World.Resume();
            }
            else
            {
                // Don't want input that opened the console to be something
                // added to the console, so first we clear all characters.
                input.ConsumeTypedCharacters();

                ConsoleLayer consoleLayer = new(m_archiveCollection, m_console);
                Add(consoleLayer);

                if (TryGetLayer(out SinglePlayerWorldLayer? layer))
                    layer.World.Pause();
            }
        }
    }
}