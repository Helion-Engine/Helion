using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Layer.WorldLayers;
using Helion.Menus.Impl;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.World.Save;

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
        public readonly SoundManager SoundManager;

        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly HelionConsole m_console;
        private readonly IAudioSystem m_audioSystem;
        private readonly SaveGameManager m_saveGameManager;

        protected override double Priority => 0.5;

        public GameLayerManager(Config config, ArchiveCollection archiveCollection, HelionConsole console,
            SoundManager soundManager, IAudioSystem audioSystem, SaveGameManager saveGameManager)
        {
            SoundManager = soundManager;
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
            m_audioSystem = audioSystem;
            m_saveGameManager = saveGameManager;
        }

        public static SaveMenu CreateSaveMenu(GameLayer parent, Config config, HelionConsole console, 
            SoundManager soundManager, ArchiveCollection archiveCollection, SaveGameManager saveManager, bool isSave)
        {
            bool hasWorld = parent.Contains<SinglePlayerWorldLayer>() && 
                !parent.Contains<IntermissionLayer>() && !parent.Contains<EndGameLayer>();
            return new(parent, config, console, soundManager, archiveCollection,
                saveManager, hasWorld, isSave);
        }

        public override void Add(GameLayer layer)
        {
            if (layer is TitlepicLayer titlepicLayer)
                titlepicLayer.PlayMusic(m_audioSystem);
            
            if (layer is SinglePlayerWorldLayer)
            {
                Layers.ForEach(l => l.Dispose());
                OrderLayers();
            }
            
            base.Add(layer);
        }

        public override void HandleInput(InputEvent input)
        {
            // We give special priority to the console.
            if (input.ConsumeKeyPressed(m_config.Controls.Console))
                HandleConsoleToggle(input);
            
            base.HandleInput(input);

            if (input.ConsumeKeyPressed(m_config.Controls.Save))
                OpenSaveGameMenu();
            else if (input.ConsumeKeyPressed(m_config.Controls.Load))
                OpenLoadGameMenu();
            else if (HasOnlyTitlepicLayer() && input.HasAnyKeyPressed())
            {
                input.ConsumeAll();
                CreateAndAddMenu();
            }
            
            if (!ContainsEither<ConsoleLayer, ImageLayer>() && input.ConsumeKeyPressed(Key.F1))
                CreateAndAddHelp();

            if (Contains<ImageLayer>() && input.ConsumeKeyPressed(Key.Escape))
            {
                SoundManager.PlayStaticSound(Constants.MenuSounds.Clear);
                Remove<ImageLayer>();
            }

            if (!Contains<MenuLayer>() && input.ConsumeKeyPressed(Key.Escape))
                CreateAndAddMenu();

            void CreateAndAddMenu()
            {
                SoundManager.PlayStaticSound(Constants.MenuSounds.Activate);
                
                MainMenu mainMenu = new(this, m_config, m_console, SoundManager, m_archiveCollection, m_saveGameManager);
                MenuLayer menuLayer = new(this, mainMenu, m_archiveCollection, SoundManager);
                Add(menuLayer);
            }
        }

        private void OpenSaveGameMenu() =>
            ShowSaveMenu(true);

        private void OpenLoadGameMenu() =>
            ShowSaveMenu(false);

        private void ShowSaveMenu(bool isSave)
        {
            SoundManager.PlayStaticSound(Constants.MenuSounds.Activate);
            SaveMenu saveMenu = CreateSaveMenu(this, m_config, m_console, SoundManager, 
                m_archiveCollection, new SaveGameManager(m_config), isSave);
            MenuLayer menuLayer = new(this, saveMenu, m_archiveCollection, SoundManager);
            Add(menuLayer);
        }

        private bool HasOnlyTitlepicLayer() => Count == 1 && Contains<TitlepicLayer>();

        private void CreateAndAddHelp()
        {
            if (m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages.Count == 0)
                return;

            SoundManager.PlayStaticSound(Constants.MenuSounds.Prompt);

            CycleImageLayer helpLayer = new(this, SoundManager, m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages);
            Add(helpLayer);
        }

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