using System;
using Helion.Input;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Impl;
using Helion.Util;
using Helion.Util.Extensions;

namespace Helion.Layer.Menus
{
    public partial class MenuLayer
    {
        private bool MenuNotChanged(Menu menu) => !m_menus.Empty() && ReferenceEquals(menu, m_menus.Peek());
        
        private void ClearMenu(bool playSound)
        {
            if (playSound)
                m_soundManager.PlayStaticSound(Constants.MenuSounds.Clear);
            
            Manager.Remove(this);
        }
        
        public void HandleInput(InputEvent input)
        {
            if (m_menus.Empty()) 
                return;
            
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
        
        private void InvokeAndPushMenu(Func<Menu?> action)
        {
            Menu? subMenu = action();
            if (subMenu != null)
                m_menus.Push(subMenu);
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
    }
}
