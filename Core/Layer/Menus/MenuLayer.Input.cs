using System;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Impl;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Menus;

public partial class MenuLayer
{
    private bool MenuNotChanged(Menu menu) => !m_menus.Empty() && ReferenceEquals(menu, m_menus.Peek());

    private void ClearMenu(bool playSound)
    {
        if (playSound)
            m_soundManager.PlayStaticSound(Constants.MenuSounds.Clear);

        Manager.RemoveMenu();
    }

    public void HandleInput(IConsumableInput input)
    {
        if (m_menus.Empty())
            return;

        Menu menu = m_menus.Peek();
        if (input.Manager.HasAnyKeyPressed() && menu is MessageMenu messageMenu && messageMenu.ShouldClear(input))
        {
            if (messageMenu.ClearMenus)
                ClearMenu(false);
            else if (m_menus.Count > 0)
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

    private void HandleInputForMenu(Menu menu, IConsumableInput input)
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
            Menu? poppedMenu = null;
            bool clear = false;
            if (m_menus.Count >= 1)
            {
                poppedMenu = m_menus.Pop();
                clear = poppedMenu.ClearOnClose;
            }

            if (m_menus.Empty() || clear)
            {
                if (poppedMenu != null)
                    m_menus.Push(poppedMenu);
                ClearMenu(true);
                return;
            }
            
            m_soundManager.PlayStaticSound(Constants.MenuSounds.Backup);
        }
    }
}
