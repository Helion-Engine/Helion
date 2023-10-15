using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio;
using Helion.Graphics;
using Helion.Layer.New.Menus;
using Helion.Layer.New.Util;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using Helion.Window;
using NLog;

namespace Helion.Layer.New.Images;

public class TitlepicLayer : GameLayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    protected override double Priority => (double)LayerPriority.Titlepic;
    private readonly GameLayerManager m_manager;
    private readonly List<string> m_pages;
    private readonly Stopwatch m_stopwatch = new();
    private readonly IAudioSystem m_audioSystem;
    private readonly string m_titleMusic;
    private readonly byte[]? m_musicData;
    private readonly TimeSpan m_titleDuration;
    private readonly TimeSpan m_pageDuration;
    private TimeSpan m_cycleDuration;
    private int m_pageIndex;
    private bool m_initRenderPages;

    public TitlepicLayer(GameLayerManager manager, ArchiveCollection archiveCollection, IAudioSystem audioSystem)
    {
        m_manager = manager;
        m_audioSystem = audioSystem;
        m_titleDuration = TimeSpan.FromSeconds(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleTime);
        m_pageDuration = TimeSpan.FromSeconds(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);
        m_cycleDuration = m_titleDuration;
        m_titleMusic = LookupTitleMusicName(archiveCollection);
        m_musicData = LookupTitleMusicData(m_titleMusic, archiveCollection);
        m_pages = FindPages(archiveCollection);
        
        m_stopwatch.Start();
        PlayMusic();
    }

    private static List<string> FindPages(ArchiveCollection archiveCollection)
    {
        List<string> pages = new() { archiveCollection.GameInfo.TitlePage };
        pages.AddRange(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages);
        return pages;
    }

    private static string LookupTitleMusicName(ArchiveCollection archiveCollection)
    {
        string titleMusic = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleMusic;
        return archiveCollection.Definitions.Language.GetMessage(titleMusic);
    }
    
    private static byte[]? LookupTitleMusicData(string name, ArchiveCollection archiveCollection)
    {
        Entry? entry = archiveCollection.Entries.FindByName(name);
        if (entry == null)
            return null;
        
        byte[] data = entry.ReadData();
        return MusToMidi.Convert(data);
    }

    private void PlayMusic()
    {
        if (m_musicData != null)
            m_audioSystem.Music.Play(m_musicData, loop: false, ignoreAlreadyPlaying: false);
        else
            Log.Warn($"Either could not find title music or data was corrupt: {m_titleMusic}");
    }

    public override bool? ShouldFocus()
    {
        return null;
    }

    public override void HandleInput(IConsumableInput input)
    {
        // No input handling.
    }

    public override void RunLogic()
    {
        if (m_stopwatch.Elapsed < m_cycleDuration)
            return;

        m_pageIndex %= (m_pageIndex + 1) % m_pages.Count;
        m_cycleDuration = m_pageIndex == 0 ? m_titleDuration : m_pageDuration;
        
        if (m_pageIndex == 0)
            PlayMusic();
        
        m_stopwatch.Restart();
    }

    public override void Render(IHudRenderContext ctx)
    {
        if (!m_initRenderPages)
            m_pages.InitRenderPages(ctx, repeatIfNotExists: true, ref m_initRenderPages);

        string image = m_pages[m_pageIndex];
        bool ShouldDarken = m_manager.HasLayer<MenuLayer>();
        
        if (ctx.RenderFullscreenImage(image) && ShouldDarken)
        {
            (int width, int height) = ctx.Dimension;
            ctx.FillBox((0, 0, width, height), Color.Black, alpha: 0.5f);
        }
    }
}