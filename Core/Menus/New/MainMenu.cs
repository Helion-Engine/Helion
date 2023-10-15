using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Audio.Sounds;
using Helion.Menus.New.NewGame;
using Helion.Menus.New.Options;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Menus.New;

public class MainMenu : Menu
{
    private readonly List<Menu> m_children;
    private int m_currentChildIndex; 

    public MainMenu(IConfig config, ArchiveCollection archiveCollection, SoundManager soundManager) : 
        base(soundManager)
    {
        m_children = new()
        {
            archiveCollection.IWadType.HasEpisodes() ? new NewGameEpisodeMenu(soundManager) : new NewGameSkillMenu(soundManager),
            new OptionsMenu(config, soundManager),
            new ExitMenu(soundManager)
        };
    }

    public override bool HandleInput(IConsumableInput input, [NotNullWhen(true)] out Menu? newMenu)
    {
        if (input.ConsumeKeyPressed(Key.Down))
        {
            m_currentChildIndex = (m_currentChildIndex + 1) % m_children.Count;
            PlayNextOptionSound();
        }
        
        if (input.ConsumeKeyPressed(Key.Up))
        {
            m_currentChildIndex--;
            if (m_currentChildIndex < 0)
                m_currentChildIndex = m_children.Count - 1;
            PlayNextOptionSound();
        }

        if (input.ConsumeKeyPressed(Key.Enter))
        {
            PlayChooseOptionSound();
            newMenu = m_children[m_currentChildIndex];
            return true;
        }
        
        newMenu = null;
        return false;
    }

    public override void RunLogic(TickerInfo tickerInfo)
    {
        // TODO: Change skull icon if enough time has elapsed.
    }

    public override void Render(IHudRenderContext ctx)
    {
        // TODO
    }
}