using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio;
using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.Window;

namespace Helion.Layer.Images;

public class TitlepicLayer : IGameLayer
{
    private readonly GameLayerManager m_parent;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly IAudioSystem m_audioSystem;
    private readonly Stopwatch m_stopwatch = new();
    private IList<string> m_pages;
    private TimeSpan m_pageDuration;
    private int m_pageIndex;
    private bool m_initRenderPages;

    private bool ShouldDarken => m_parent.MenuLayer != null;

    public TitlepicLayer(GameLayerManager parent, ArchiveCollection archiveCollection, IAudioSystem audioSystem)
    {
        m_parent = parent;
        m_archiveCollection = archiveCollection;
        m_audioSystem = audioSystem;
        m_pageDuration = TimeSpan.FromSeconds(archiveCollection.Definitions.MapInfoDefinition.GameDefinition.TitleTime);

        m_stopwatch.Start();
        List<string> pages = new() { archiveCollection.GameInfo.TitlePage };
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

        audioSystem.Music.Play(entry.ReadData(), MusicPlayerOptions.None);
    }

    public void HandleInput(IConsumableInput input)
    {
        // No input handled here.
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        // No logic to run.
    }

    public void Render(IHudRenderContext hud)
    {
        if (!m_initRenderPages)
        {
            m_initRenderPages = true;
            m_pages = LayerUtil.GetRenderPages(hud, m_pages, true);
        }

        string image = m_pages[m_pageIndex];
        if (hud.RenderFullscreenImage(image) && ShouldDarken)
        {
            (int width, int height) = hud.Dimension;
            hud.FillBox((0, 0, width, height), Color.Black, alpha: 0.5f);
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
        // Nothing to dispose.
    }
}
