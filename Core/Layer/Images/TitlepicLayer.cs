﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Layer.Menus;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Sounds.Mus;
using Helion.Window.Input;
using Helion.World.Save;

namespace Helion.Layer.Images
{
    public class TitlepicLayer : IGameLayer
    {
        private const string Titlepic = "TITLEPIC";

        private readonly GameLayerManager m_parent;
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

        private bool ShouldDarken => m_parent.MenuLayer != null;
        private bool ShouldMakeMenu => m_parent.ConsoleLayer == null && m_parent.MenuLayer == null;
        
        public TitlepicLayer(GameLayerManager parent, Config config, HelionConsole console, SoundManager soundManager,
            ArchiveCollection archiveCollection, SaveGameManager saveGameManager, IAudioSystem audioSystem)
        {
            m_parent = parent;
            m_archiveCollection = archiveCollection;
            m_config = config;
            m_console = console;
            m_soundManager = soundManager;
            m_saveGameManager = saveGameManager;
            m_audioSystem = audioSystem;

            m_pageDuration = TimeSpan.FromSeconds(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleTime);

            m_stopwatch.Start();
            List<string> pages = new() { Titlepic };
            pages.AddRange(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages);
            m_pages = pages;

            PlayMusic(m_audioSystem);
        }
        
        private void PlayMusic(IAudioSystem audioSystem)
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
        
        public void HandleInput(InputEvent input)
        {
            if (m_disposed)
                return;
            
            if (input.HasAnyKeyPressed() && ShouldMakeMenu)
            {
                // We want the user to be able to get to the menu from the
                // 'Read This' menu, but we don't want any other keys making
                // menus appear when someone might be trying to read it.
                if (input.ConsumeKeyPressed(Key.Escape) || m_parent.ReadThisLayer == null)
                {
                    m_soundManager.PlayStaticSound(Constants.MenuSounds.Activate);
                
                    MenuLayer menuLayer = new(m_parent, m_config, m_console, m_archiveCollection, m_soundManager, m_saveGameManager);
                    m_parent.Add(menuLayer);
                }
            }
        }

        public void RunLogic()
        {
            // No logic to run.
        }

        public void Render(IHudRenderContext hud)
        {
            if (m_disposed)
                return;

            if (!m_initRenderPages)
            {
                m_initRenderPages = true;
                m_pages = LayerUtil.GetRenderPages(hud, m_pages, true);
            }

            string image = m_pages[m_pageIndex];
            if (hud.Textures.TryGet(image, out var handle))
            {
                hud.DoomVirtualResolution(() =>
                {
                    Dimension dim = handle.Dimension;
                    hud.Image(image, (0, 0, dim.Width, dim.Height));
                });

                if (ShouldDarken)
                {
                    (int width, int height) = hud.Dimension;
                    hud.FillBox((0, 0, width, height), Color.Black, alpha: 0.5f);
                }
            }
            
            if (m_pages.Count > 1)
                CheckCycle();
        }
        
        private void CheckCycle()
        {
            if (m_stopwatch.Elapsed < m_pageDuration) 
                return;
            
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

        public void Dispose()
        {
            if (m_disposed)
                return;
            
            m_stopwatch.Stop();
            m_disposed = true;
        }
    }
}
