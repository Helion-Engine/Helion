using System.Collections.Generic;
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

        protected override double Priority => 0.7;

        public MenuLayer(GameLayer parent, Menu menu, ArchiveCollection archiveCollection)
        {
            Parent = parent;
            m_menuDrawer = new MenuDrawer(archiveCollection);
            
            m_menus.Push(menu);
        }
        
        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            Menu menu = m_menus.Peek();

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
                    m_menus.Push(subMenu);
            }

            if (input.ConsumeKeyPressed(Key.Escape))
            {
                if (m_menus.Count > 1)
                    m_menus.Pop();
                else
                    Dispose();
            }
        }

        public override void Render(RenderCommands renderCommands)
        {
            if (!m_menus.Empty())
                m_menuDrawer.Draw(m_menus.Peek(), renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
