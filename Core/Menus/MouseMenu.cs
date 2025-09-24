using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Layer.Options;
using Helion.Render.Common.Renderers;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Window;

namespace Helion.Menus;

public class MouseMenu
{
    private readonly IWindow m_window;
    private readonly IConfig m_config;
    private Vec2I m_cursorPos;
    private Vec2I m_prevCursorPos;
    private bool m_resetMouse;
    private bool m_setMouse;
    private bool m_locked;

    private readonly BoxList m_boxList = new();

    public MouseMenu(IWindow window, IConfig config)
    {
        m_window = window;
        m_config = config;

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged += WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged += WindowVirtualDimension_OnChanged;
    }

    public void Clear()
    {
        m_boxList.Clear();
    }

    public void SetLocked(bool set)
    {
        m_locked = set;
    }

    public void Add(Box2I dimension, int index)
    {
        m_boxList.Add(dimension, index);
    }

    public bool MousePositionChanged()
    {
        return m_prevCursorPos != m_cursorPos;
    }

    public bool GetSelectedIndex(out int index)
    {
        return m_boxList.GetIndex(m_cursorPos, out index);
    }

    public void Render(IHudRenderContext hud)
    {
        var set = false;
        if (m_resetMouse)
        {
            SetMousePosition((m_window.ClientDimension.Width / 2, m_config.Window.GetMenuScaled(45)));
            m_resetMouse = false;
            set = true;
        }

        if (m_setMouse)
        {
            m_window.SetMousePosition(m_cursorPos);
            m_setMouse = false;
            set = true;
        }

        if (set)
            return;

        if (!m_locked)
        {
            m_prevCursorPos = m_cursorPos;
            m_cursorPos = m_window.InputManager.MousePosition;
        }

        var hover = m_boxList.GetIndex(m_cursorPos, out _);
        var cursor = hover ? "helion-pointer" : "helion-cursor";
        if (hud.Textures.TryGet(cursor, out var cursorHandle, ResourceNamespace.Graphics))
        {
            var size = hover ? 32 : 24;
            var scale = size / (float)cursorHandle.Dimension.Height;
            hud.Image(cursor, m_cursorPos, resourceNamespace: ResourceNamespace.Graphics, scale: scale);
        }
    }

    public void SetMouseToIndex(int index)
    {
        if (index >= m_boxList.Count)
            return;

        var box = m_boxList.Get(index);
        SetMousePosition((box.Left + box.Width / 2, box.Top - box.Height / 2));
    }

    public void SetMousePosition(Vec2I pos)
    {
        m_prevCursorPos = default;
        m_cursorPos = pos;
        m_window.SetMousePosition(pos);
    }

    private void WindowVirtualDimension_OnChanged(object? sender, Dimension e) => HandleResize();

    private void WindowVirtualEnable_OnChanged(object? sender, bool e) => HandleResize();

    private void WindowState_OnChanged(object? sender, RenderWindowState e) => HandleResize();

    private void HandleResize()
    {
        m_resetMouse = true;
    }
}
