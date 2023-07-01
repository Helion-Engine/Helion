using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.Util.Extensions;

namespace Helion.Window.Input;

public class ConsumableInput : IConsumableInput
{
    private readonly InputManager m_inputManager;
    private readonly DynamicArray<Key> m_inputDownConsumed = new();
    private readonly DynamicArray<Key> m_inputUpConsumed = new();
    private readonly DynamicArray<Key> m_pressedKeys = new();
    private bool m_typedCharsConsumed;
    private Vec2I m_mouseMove = (0, 0);
    private int m_mouseScroll;
    private bool m_allConsumed;

    public IInputManager Manager => m_inputManager;
    public bool HandleKeyInput { get; set; }
    public int Scroll => m_mouseScroll;

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
        if (m_allConsumed || m_inputDownConsumed.Contains(key))
            return false;

        m_inputDownConsumed.Add(key);
        return Manager.IsKeyDown(key);
    }

    public bool ConsumeKeyPressed(Key key)
    {
        if (m_allConsumed || m_inputDownConsumed.Contains(key))
            return false;

        m_inputDownConsumed.Add(key);
        return Manager.IsKeyPressed(key);
    }

    public bool ConsumePressOrContinuousHold(Key key)
    {
        if (Manager.IsKeyDown(key) && Manager.IsKeyContinuousHold(key))
            return true;

        return ConsumeKeyPressed(key);
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

    public Vec2I GetMouseMove() => m_mouseMove;

    public int ConsumeScroll()
    {
        int result = m_mouseScroll;
        m_mouseScroll = 0;
        return result;
    }

    public bool HasAnyKeyPressed()
    {
        m_pressedKeys.Clear();
        m_inputManager.GetPressedKeys(m_pressedKeys);

        for (int i = 0; i < m_pressedKeys.Length; i++)
        {
            if (m_inputDownConsumed.Contains(m_pressedKeys[i]))
                continue;

            return true;
        }

        return false;
    }

    internal void Reset(int mouseScroll)
    {
        m_inputDownConsumed.Clear();
        m_inputUpConsumed.Clear();
        m_allConsumed = false;
        m_typedCharsConsumed = false;
        m_mouseScroll = mouseScroll;
        m_mouseMove = Manager.MouseMove;
    }
}
