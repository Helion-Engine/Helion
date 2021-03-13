using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Layer.WorldLayers;
using Helion.Menus.Impl;
using Helion.Resources.Archives.Collection;
using Helion.Util;
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
        private readonly IAudioSystem m_audioSystem;
        private readonly SoundManager m_soundManager;

        protected override double Priority => 0.5;

        public GameLayerManager(Config config, ArchiveCollection archiveCollection, HelionConsole console,
            SoundManager soundManager, IAudioSystem audioSystem)
        {
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
            m_soundManager = soundManager;
            m_audioSystem = audioSystem;
        }

        public override void Add(GameLayer layer)
        {
            if (layer is TitlepicLayer titlepicLayer)
                titlepicLayer.PlayMusic(m_archiveCollection.IwadType, m_audioSystem);
            
            if (layer is SinglePlayerWorldLayer)
            {
                Layers.ForEach(l => l.Dispose());
                OrderLayers();
            }
            
            base.Add(layer);
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.ConsumeTypedKey(m_config.Controls.Console))
                HandleConsoleToggle(input);
            
            if (HasOnlyTitlepicLayer() && input.HasAnyKeyPressed())
            {
                input.ConsumeAll();
                CreateAndAddMenu();
            }

            if (!Contains<MenuLayer>() && input.ConsumeKeyPressed(Key.Escape))
                CreateAndAddMenu();

            base.HandleInput(input);

            void CreateAndAddMenu()
            {
                m_soundManager.PlayStaticSound(Constants.SwitchNormSound);
                
                MainMenu mainMenu = new(m_config, m_console, m_soundManager, m_archiveCollection);
                MenuLayer menuLayer = new(this, mainMenu, m_archiveCollection, m_soundManager);
                Add(menuLayer);
            }
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
                return;
            }
            
            // Don't want input that opened the console to be something
            // added to the console, so first we clear all characters.
            input.ConsumeTypedCharacters();

            ConsoleLayer consoleLayer = new(m_archiveCollection, m_console);
            Add(consoleLayer);
        }
    }
}