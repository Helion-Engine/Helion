using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus.Impl;
using Helion.Render.OpenGL.Legacy.Commands;
using Helion.Render.OpenGL.Legacy.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Sounds.Mus;
using Helion.World.Save;

namespace Helion.Layer
{
    public class TitlepicLayer : GameLayer
    {
        private const string Titlepic = "TITLEPIC";

        private readonly ArchiveCollection m_archiveCollection;
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly SoundManager m_soundManager;
        private readonly SaveGameManager m_saveGameManager;
        private readonly IAudioSystem m_audioSystem;
        private readonly Stopwatch m_stopwatch = new();
        private IList<string> m_pages;
        private TimeSpan m_pageDuration;
        private int m_pageIndex;
        private bool m_initRenderPages;
        private bool m_disposed;
        
        protected override double Priority => 0.1;

        public TitlepicLayer(GameLayer parent, Config config, HelionConsole console, SoundManager soundManager,
            ArchiveCollection archiveCollection, SaveGameManager saveGameManager, IAudioSystem audioSystem)
        {
            m_archiveCollection = archiveCollection;
            m_config = config;
            m_console = console;
            m_soundManager = soundManager;
            m_saveGameManager = saveGameManager;
            m_audioSystem = audioSystem;
            Parent = parent;

            m_pageDuration = TimeSpan.FromSeconds(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleTime);

            m_stopwatch.Start();
            List<string> pages = new() { Titlepic };
            pages.AddRange(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages);
            m_pages = pages;

            PlayMusic(m_audioSystem);
        }

        public override void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        protected new void PerformDispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;
            m_stopwatch.Stop();
        }

        public override void HandleInput(InputEvent input)
        {
            if (m_disposed)
                return;

            base.HandleInput(input);

            if (input.HasAnyKeyPressed() && Parent?.Count == 1)
            {
                m_soundManager.PlayStaticSound(Constants.MenuSounds.Activate);

                MainMenu mainMenu = new(Parent, m_config, m_console, m_soundManager, m_archiveCollection, m_saveGameManager);
                MenuLayer menuLayer = new(Parent, mainMenu, m_archiveCollection, m_soundManager);
                Parent.Add(menuLayer);
            }
        }

        public override void Render(RenderCommands commands)
        {
            if (m_disposed)
                return;

            DrawHelper draw = new(commands);
            if (!m_initRenderPages)
            {
                m_initRenderPages = true;
                m_pages = LayerUtil.GetRenderPages(draw, m_pages, true);
            }

            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            
            draw.FillWindow(Color.Black);

            string image = m_pages[m_pageIndex];
            var area = draw.DrawInfoProvider.GetImageDimension(image);
            draw.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
            {
                commands.DrawImage(image, 0, 0, area.Width, area.Height, Color.White);
            });

            if (ShouldDarken())
                commands.FillRect(new(0, 0, width, height), Color.Black, 0.5f);
            
            base.Render(commands);

            if (m_pages.Count > 1)
                CheckCycle();
        }

        private void CheckCycle()
        {
            if (m_stopwatch.Elapsed >= m_pageDuration)
            {
                m_pageIndex++;
                m_pageIndex %= m_pages.Count;
                m_stopwatch.Restart();

                if (m_pageIndex == 0)
                {
                    m_pageDuration = TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleTime);
                    PlayMusic(m_audioSystem);
                }
                else
                {
                    m_pageDuration = TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);
                }
            }
        }

        public void PlayMusic(IAudioSystem audioSystem)
        {
            string entryName = m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleMusic;
            entryName = m_archiveCollection.Definitions.Language.GetMessage(entryName);
            Entry? entry = m_archiveCollection.Entries.FindByName(entryName);
            if (entry == null) 
                return;
            
            byte[] data = entry.ReadData();
            byte[]? convertedData = MusToMidi.Convert(data);
            if (convertedData == null) 
                return;
            
            audioSystem.Music.Play(convertedData, loop: false, ignoreAlreadyPlaying: false);
        }

        private bool ShouldDarken() => Parent != null && Parent.Contains<MenuLayer>();
    }
}
