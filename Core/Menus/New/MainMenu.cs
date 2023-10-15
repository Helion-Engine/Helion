using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Audio.Sounds;
using Helion.Menus.New.LoadSave;
using Helion.Menus.New.NewGame;
using Helion.Menus.New.Options;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
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
    private readonly bool m_hasEpisodes;
    private readonly bool m_hasReadThis;
    private int m_currentChildIndex;
    private int m_ticksAccumulated;
    private readonly List<Menu> m_children = new();

    public MainMenu(IConfig config, ArchiveCollection archiveCollection, SoundManager soundManager) : 
        base(soundManager)
    {
        m_hasEpisodes = archiveCollection.IWadType.HasEpisodes();
        m_hasReadThis = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.DrawReadThis;
        
        if (m_hasEpisodes)
            m_children.Add(new NewGameEpisodeMenu(soundManager));
        else
            m_children.Add(new NewGameSkillMenu(soundManager));
        m_children.Add(new OptionsMenu(config, soundManager));
        m_children.Add(new LoadGameMenu(soundManager));
        m_children.Add(new SaveGameMenu(soundManager));
        if (m_hasReadThis)
            m_children.Add(new ReadThisMenu(soundManager));
        m_children.Add(new ExitMenu(soundManager));
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
        m_ticksAccumulated += tickerInfo.Ticks;
    }

    public override void Render(IHudRenderContext ctx)
    {
        const int SkullTickPeriod = 35;
        const int PaddingY = 1;

        ctx.DoomVirtualResolution(_ =>
        {
            ctx.Image("M_DOOM", (97, 2));
            int y = m_hasEpisodes ? 64 : 72;

            DrawRow("M_NGAME", 0, ref y);
            DrawRow("M_OPTION", 1, ref y);
            DrawRow("M_LOADG", 2, ref y);
            DrawRow("M_SAVEG", 3, ref y);
            if (m_hasReadThis)
                DrawRow("M_RDTHIS", 4, ref y);
            DrawRow("M_QUITG", m_hasReadThis ? 5 : 4, ref y);

            void DrawRow(string imageName, int childIndex, ref int offsetY)
            {
                ctx.Image(imageName, (97, offsetY), out HudBox area);
                
                if (m_currentChildIndex == childIndex)
                {
                    string skullImage = m_ticksAccumulated % SkullTickPeriod < SkullTickPeriod / 2 ? "M_SKULL1" : "M_SKULL2";
                    ctx.Image(skullImage, (87, offsetY), anchor: Align.TopRight);    
                }
                
                offsetY += area.Height + PaddingY;
            }
        }, ctx);
    }
}