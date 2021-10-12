using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Window.Input;

/// <summary>
/// A basic implementation of consumable input.
/// </summary>
public class ConsumableInput : IConsumableInput
{
    private readonly InputManager m_inputManager;
    private readonly HashSet<Key> m_inputConsumed = new();
    private bool m_typedCharsConsumed;
    private Vec2I m_mouseMove = (0, 0);
    private int m_mouseScroll;
    private bool m_allConsumed;

    public IInputManager Manager => m_inputManager;

    public ConsumableInput(InputManager inputManager)
    {
        m_inputManager = inputManager;
    }

    public void ConsumeAll()
    {
        m_allConsumed = true;
        m_mouseMove = (0, 0);
        m_mouseScroll = 0;
    }

    public bool ConsumeKeyDown(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyDown(key);
    }

    public bool ConsumeKeyPrevDown(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyPrevDown(key);
    }

    public bool ConsumeKeyHeldDown(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyHeldDown(key);
    }

    public bool ConsumeKeyUp(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyUp(key);
    }

    public bool ConsumeKeyPrevUp(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyPrevUp(key);
    }

    public bool ConsumeKeyPressed(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyPressed(key);
    }

    public bool ConsumeKeyReleased(Key key)
    {
        if (m_allConsumed || m_inputConsumed.Contains(key))
            return false;

        m_inputConsumed.Add(key);
        return Manager.IsKeyReleased(key);
    }

    public ReadOnlySpan<char> ConsumeTypedCharacters()
    {
        if (m_allConsumed || m_typedCharsConsumed)
            return ReadOnlySpan<char>.Empty;

        m_typedCharsConsumed = true;
        return Manager.TypedCharacters;
    }

    public Vec2I ConsumeMouseMove()
    {
        Vec2I result = m_mouseMove;
        m_mouseMove = (0, 0);
        return result;
    }

    public int ConsumeScroll()
    {
        int result = m_mouseScroll;
        m_mouseScroll = 0;
        return result;
    }

    internal void Reset()
    {
        m_inputConsumed.Clear();
        m_allConsumed = false;
        m_typedCharsConsumed = false;
        m_mouseMove = Manager.MouseMove;
        m_mouseScroll = Manager.Scroll;
    }
}

