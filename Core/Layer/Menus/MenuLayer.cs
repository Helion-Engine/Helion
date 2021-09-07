using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio.Sounds;
using Helion.Menus;
using Helion.Menus.Impl;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.World.Save;

namespace Helion.Layer.Menus
{
    public partial class MenuLayer : IGameLayer
    {
        internal readonly GameLayerManager Manager;
        private readonly IConfig m_config;
        private readonly HelionConsole m_console;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly SoundManager m_soundManager;
        private readonly SaveGameManager m_saveGameManager;
        private readonly Stack<Menu> m_menus = new();
        private readonly Stopwatch m_stopwatch = new();
        private bool m_disposed;

        public MenuLayer(GameLayerManager manager, IConfig config, HelionConsole console, 
            ArchiveCollection archiveCollection, SoundManager soundManager, SaveGameManager saveGameManager)
        {
            Manager = manager;
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
            m_soundManager = soundManager;
            m_saveGameManager = saveGameManager;
            m_stopwatch.Start();

            MainMenu mainMenu = new(this, config, console, soundManager, archiveCollection, saveGameManager);
            m_menus.Push(mainMenu);
        }
        
        public void AddSaveOrLoadMenuIfMissing(bool isSave)
        {
            foreach (Menu menu in m_menus)
                if (menu.GetType() == typeof(SaveMenu))
                    return;

            bool hasWorld = Manager.WorldLayer != null;
            SaveMenu saveMenu = new(this, m_config, m_console, m_soundManager, m_archiveCollection,
                m_saveGameManager, hasWorld, isSave);
            
            m_menus.Push(saveMenu);
        }
        
        public void RunLogic()
        {
            // No logic.
        }
        
        public void Dispose()
        {
            if (m_disposed)
                return;

            // This comes first because when we're removing ourselves from the
            // parent, we run into an infinite loop. This short-circuits it.
            m_disposed = true;
            
            m_menus.Clear();

            Manager.Remove(this);
        }
    }
}
