using System;
using System.Collections.Generic;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Impl;
using Helion.Render.OpenGL.Legacy.Commands;
using Helion.Render.OpenGL.Legacy.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Util;
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
            if (!m_menus.Empty())
            {
                Menu menu = m_menus.Peek();
                if (input.HasAnyKeyPressed() && menu is MessageMenu messageMenu && messageMenu.ShouldClear(input))
                {
                    if (messageMenu.ClearMenus)
                        ClearMenu(false);
                    else
                        m_menus.Pop();
                }

                menu.HandleInput(input);

                if (MenuNotChanged(menu))
                    HandleInputForMenu(menu, input);
            }

            base.HandleInput(input);

            bool MenuNotChanged(Menu menu) => !m_menus.Empty() && ReferenceEquals(menu, m_menus.Peek());
        }

        private void HandleInputForMenu(Menu menu, InputEvent input)
        {
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
                if (menu.CurrentComponent.PlaySelectedSound)
                {
                    m_soundManager.PlayStaticSound(Constants.MenuSounds.Choose);
                    m_soundManager.Update();
                }

                InvokeAndPushMenu(menu.CurrentComponent.Action);
            }

            if (input.ConsumeKeyPressed(Key.Delete) && menu.CurrentComponent?.DeleteAction != null)
                InvokeAndPushMenu(menu.CurrentComponent.DeleteAction);

            if (input.ConsumeKeyPressed(Key.Escape))
            {
                if (m_menus.Count >= 1)
                    m_menus.Pop();

                if (m_menus.Empty())
                    ClearMenu(true);
                else
                    m_soundManager.PlayStaticSound(Constants.MenuSounds.Backup);
            }
        }

        private void InvokeAndPushMenu(Func<Menu?> action)
        {
            Menu? subMenu = action();
            if (subMenu != null)
                m_menus.Push(subMenu);
        }

        private void ClearMenu(bool playSound)
        {
            Parent?.Remove<MenuLayer>();
            if (playSound)
                m_soundManager.PlayStaticSound(Constants.MenuSounds.Clear);
        }

        public override void Render(RenderCommands renderCommands)
        {
            if (!m_menus.Empty())
                m_menuDrawer.Draw(m_menus.Peek(), renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
