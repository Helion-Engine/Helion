using System.Collections.Generic;
using Helion.Audio.Sounds;
using Helion.Menus.New;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Menus.New;

public class MenuLayer : IGameLayer
{
    internal readonly GameLayerManager Manager;
    private readonly SoundManager m_soundManager;
    private readonly Stack<Menu> m_menus = new();
    private bool m_disposed;

    public MenuLayer(GameLayerManager manager, IConfig config, ArchiveCollection archiveCollection, 
        SoundManager soundManager)
    {
        Manager = manager;
        m_soundManager = soundManager;
        
        m_menus.Push(new MainMenu(config, archiveCollection, soundManager));
    }

    public void HandleInput(IConsumableInput input)
    {
        if (!m_menus.TryPeek(out Menu? menu))
            return;

        if (menu.HandleInput(input, out Menu? newMenu))
            m_menus.Push(newMenu);

        // This is after handling input from any menus in case the menu consumes Escape.
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(Constants.MenuSounds.Backup);
            m_menus.Pop();
        }
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        if (m_menus.TryPeek(out Menu? menu))
            menu.RunLogic(tickerInfo);
    }

    public void Render(IHudRenderContext ctx)
    {
        if (m_menus.TryPeek(out Menu? menu))
            menu.Render(ctx);
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

    public void ClearAndUse(Menu menu)
    {
        m_menus.Clear();
        m_menus.Push(menu);
    }
}