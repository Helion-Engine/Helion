using System;
using System.Diagnostics.CodeAnalysis;
using Helion.Geometry.Vectors;
using Helion.Menus;
using Helion.Menus.Impl;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Menus;

public partial class MenuLayer
{
    private Vec2I m_lastCursorPos;

    private Menu Peek() => m_menus[^1];

    private Menu Pop()
    {
        var menu = m_menus[^1];
        m_menus.RemoveAt(m_menus.Count - 1);
        return menu;
    }

    private bool TryPeekMenu([NotNullWhen(true)] out Menu? menu)
    {
        if (m_menus.Count > 0)
        {
            menu = Peek();
            return true;
        }

        menu = null;
        return false;
    }

    private bool MenuNotChanged(Menu menu) => !m_menus.Empty() && ReferenceEquals(menu, Peek());

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

        Menu menu = Peek();
        if (input.Manager.HasAnyKeyPressed() && menu is MessageMenu messageMenu && messageMenu.ShouldClear(input))
        {
            if (messageMenu.ClearMenus)
                ClearMenu(false);
            else if (m_menus.Count > 0)
                Pop();
        }

        menu.HandleInput(input);

        if (MenuNotChanged(menu))
            HandleInputForMenu(menu, input);

        // Consume all remaining input--when menu layer is on top, users are not expecting input to fall through into other layers.
        input.ConsumeAll();
    }

    private void InvokeAndPushMenu(Func<Menu?> action)
    {
        var subMenu = action();
        if (subMenu != null)
            PushMenu(subMenu);
    }

    public void PushMenu(Menu menu)
    {
        m_resetMouse = true;
        UpdateMouseMenu();
        m_menus.Add(menu);
    }

    private void HandleInputForMenu(Menu menu, IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Up) || input.ConsumeKeyPressed(Key.DPadUp))
            menu.MoveToPreviousComponent();
        if (input.ConsumeKeyPressed(Key.Down) || input.ConsumeKeyPressed(Key.DPadDown))
            menu.MoveToNextComponent();

        if ((input.ConsumeKeyPressed(Key.Enter) || input.ConsumeKeyPressed(Key.ButtonA) || input.ConsumeKeyPressed(Key.MouseLeft)) && menu.CurrentComponent?.Action != null)
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

        if (input.ConsumeKeyPressed(Key.Escape) || input.ConsumeKeyPressed(Key.ButtonB))
        {
            Menu? poppedMenu = null;
            var clear = false;
            if (m_menus.Count >= 1)
            {
                UpdateMouseMenu();
                poppedMenu = Pop();
                clear = poppedMenu.ClearOnClose;
            }

            if (m_menus.Empty() || clear)
            {
                if (poppedMenu != null)
                    m_menus.Add(poppedMenu);
                ClearMenu(true);
                return;
            }

            m_soundManager.PlayStaticSound(Constants.MenuSounds.Backup);
        }
    }

    private void UpdateMouseMenu()
    {
        if (m_mouseMenu.CursorPos != m_lastCursorPos)
            m_forceCheckMouse = true;
        m_lastCursorPos = m_mouseMenu.CursorPos;
    }
}
