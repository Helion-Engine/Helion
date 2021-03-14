using System.Drawing;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus.Impl;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Sounds.Mus;
using Helion.World.Save;

namespace Helion.Layer
{
    public class TitlepicLayer : GameLayer
    {
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly SoundManager m_soundManager;
        private readonly SaveGameManager m_saveGameManager;
        
        protected override double Priority => 0.1;

        public TitlepicLayer(GameLayer parent, Config config, HelionConsole console, SoundManager soundManager,
            ArchiveCollection archiveCollection, SaveGameManager saveGameManager)
        {
            m_archiveCollection = archiveCollection;
            m_config = config;
            m_console = console;
            m_soundManager = soundManager;
            m_saveGameManager = saveGameManager;
            Parent = parent;
        }
        
        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            if (input.HasAnyKeyPressed() && Parent?.Count == 1)
            {
                MainMenu mainMenu = new(m_config, m_console, m_soundManager, m_archiveCollection, m_saveGameManager);
                MenuLayer menuLayer = new(this, mainMenu, m_archiveCollection, m_soundManager);
                Parent.Add(menuLayer);
            }
        }

        public override void Render(RenderCommands commands)
        {
            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            commands.DrawImage("TITLEPIC", 0, 0, width, height, Color.White);

            if (ShouldDarken())
                commands.FillRect(new(0, 0, width, height), Color.Black, 0.5f);
            
            base.Render(commands);
        }

        public void PlayMusic(IWadType iwadType, IAudioSystem audioSystem)
        {
            string entryName = iwadType.IsDoom1() ? "D_INTRO" : "D_DM2TTL";
            Entry? entry = m_archiveCollection.Entries.FindByName(entryName);
            if (entry == null) 
                return;
            
            byte[] data = entry.ReadData();
            byte[]? convertedData = MusToMidi.Convert(data);
            if (convertedData == null) 
                return;
            
            audioSystem.Music.Play(convertedData, false);
        }

        private bool ShouldDarken() => Parent != null && Parent.Contains<MenuLayer>();
    }
}
