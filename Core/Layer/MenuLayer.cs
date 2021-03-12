using System.Collections.Generic;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;

namespace Helion.Layer
{
    /// <summary>
    /// A layer for handling menus.
    /// </summary>
    public class MenuLayer : GameLayer
    {
        private readonly Stack<Menu> m_menus = new();
        private readonly MenuDrawer m_menuDrawer;
        private readonly SoundManager m_soundManager;

        protected override double Priority => 0.7;

        public MenuLayer(GameLayer parent, Menu menu, ArchiveCollection archiveCollection, SoundManager soundManager)
        {
            Parent = parent;
            m_soundManager = soundManager;
            m_menuDrawer = new MenuDrawer(archiveCollection);

            m_menus.Push(menu);
        }
        
        public override void HandleInput(InputEvent input)
        {
            Menu menu = m_menus.Peek();

            if (input.ConsumeKeyPressed(Key.Up))
                menu.MoveToPreviousComponent();
            if (input.ConsumeKeyPressed(Key.Down))
                menu.MoveToNextComponent();

            if (menu.CurrentComponent is MenuOptionListComponent options)
            {
                if (input.ConsumeKeyPressed(Key.Left))
                    options.MoveToPrevious();
                else if (input.ConsumeKeyPressed(Key.Right))
                    options.MoveToNext();
            }

            if (input.ConsumeKeyPressed(Key.Enter) && menu.CurrentComponent?.Action != null)
            {
                Menu? subMenu = menu.CurrentComponent.Action();
                if (subMenu != null)
                {
                    m_soundManager.PlayStaticSound("weapons/pistol");
                    m_menus.Push(subMenu);
                }
            }

            if (input.ConsumeKeyPressed(Key.Escape))
            {
                if (m_menus.Count >= 1)
                {
                    m_soundManager.PlayStaticSound("switches/exitbutn");
                    m_menus.Pop();
                }
                
                if (m_menus.Empty())
                    Parent?.Remove<MenuLayer>();
            }
            
            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            if (!m_menus.Empty())
                m_menuDrawer.Draw(m_menus.Peek(), renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
