using System.Collections.Generic;
using Helion.Audio.Sounds;
using Helion.Menus.New;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.New.Menus;

public class MenuLayer : GameLayer
{
    protected override double Priority => 0.3;
    private readonly SoundManager m_soundManager;
    private readonly Stack<Menu> m_menus = new();
    private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
    
    public MenuLayer(SoundManager soundManager, Menu initialMenu)
    {
        m_soundManager = soundManager;
        
        m_menus.Push(initialMenu);
    }
    
    public void ClearAndUse(Menu menu)
    {
        m_menus.Clear();
        m_menus.Push(menu);
    }
    
    public override bool? ShouldFocus()
    {
        return false;
    }

    public override void HandleInput(IConsumableInput input)
    {
        if (!m_menus.TryPeek(out Menu? menu))
            return;

        if (menu.HandleInput(input, out Menu? newMenu))
            m_menus.Push(newMenu);

        // This is after handling input from any menus in case the menu consumes Escape.
        // Example: escaping from setting a key in the options menu.
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(Constants.MenuSounds.Backup);
            m_menus.Pop();
        }
    }

    public override void RunLogic()
    {
        TickerInfo tickerInfo = m_ticker.GetTickerInfo();
        
        if (m_menus.TryPeek(out Menu? menu))
            menu.RunLogic(tickerInfo);

        if (m_menus.Empty())
            Dispose();
    }

    public override void Render(IHudRenderContext ctx)
    {
        if (m_menus.TryPeek(out Menu? menu))
            menu.Render(ctx);
    }
}