using System;
using System.Diagnostics;
using Helion.Geometry.Vectors;
using Helion.Util.Container;

namespace Helion.Window.Input;

/// <summary>
/// A simple implementation of an input manager.
/// </summary>
public class InputManager : IInputManager
{
    public Vec2I MouseMove { get; private set; } = (0, 0);
    private readonly ConsumableInput m_consumableInput;
    private readonly DynamicArray<Key> m_inputDown = new();
    private readonly DynamicArray<Key> m_inputUp = new();
    private readonly DynamicArray<Key> m_inputPrevDown = new();
    private readonly DynamicArray<Key> m_inputDownRemove = new();
    private readonly DynamicArray<Key> m_inputDownUpdate = new();
    private readonly DynamicArray<char> m_typedCharacters = new();
    private readonly Stopwatch m_keyHold = new();
    private readonly Stopwatch m_keyDelay = new();
    private double m_mouseScroll;
    private Key m_prevKeyDown;

    public int Scroll => (int)m_mouseScroll;
    public ReadOnlySpan<char> TypedCharacters => new(m_typedCharacters.Data, 0, m_typedCharacters.Length);

    public InputManager()
    {
        m_consumableInput = new ConsumableInput(this);
        m_prevKeyDown = Key.Unknown;
    }

    private void CheckContinuousKeyHold()
    {
        if (m_prevKeyDown == Key.Unknown || !IsKeyDown(m_prevKeyDown))
        {
            m_keyHold.Reset();
            return;
        }

        if (!m_keyHold.IsRunning)
        {
            m_keyDelay.Restart();
            m_keyHold.Start();
        }
    }

    public void SetKeyDown(Key key)
    {
        m_prevKeyDown = key;
        m_inputDown.Add(key);

        CheckContinuousKeyHold();
    }

    public void SetKeyUp(Key key)
    {
        if (m_prevKeyDown == key)
            m_prevKeyDown = Key.Unknown;

        m_inputUp.Add(key);
        CheckContinuousKeyHold();
    }

    public void AddTypedCharacters(ReadOnlySpan<char> str)
    {
        for (int i = 0; i < str.Length; i++)
            m_typedCharacters.Add(str[i]);
    }

    public void AddMouseMovement(Vec2I movement)
    {
        MouseMove += movement;
    }

    public void AddMouseScroll(double amount)
    {
        m_mouseScroll += amount;
    }

    public bool IsKeyDown(Key key) => m_inputDown.Contains(key);
    public bool IsKeyPrevDown(Key key) => m_inputPrevDown.Contains(key);
    public bool IsKeyHeldDown(Key key) => IsKeyDown(key) && IsKeyPrevDown(key);
    public bool IsKeyUp(Key key) => !m_inputDown.Contains(key);
    public bool IsKeyPrevUp(Key key) => !m_inputPrevDown.Contains(key);
    public bool IsKeyPressed(Key key) => IsKeyDown(key) && !IsKeyPrevDown(key);
    public bool IsKeyReleased(Key key) => !IsKeyDown(key) && IsKeyPrevDown(key);
    public bool HasAnyKeyPressed()
    {
        for (int i = 0; i < m_inputDown.Length; i++)
        {
            if (IsKeyPressed(m_inputDown[i]))
                return true;
        }

        return false;
    }

    public bool HasAnyKeyDown() => m_inputDown.Length > 0;
    public void GetPressedKeys(DynamicArray<Key> pressedKeys)
    {
        for (int i = 0; i < m_inputDown.Length; i++)
        {
            Key key = m_inputDown[i];
            if (IsKeyPressed(key))
                pressedKeys.Add(key);
        }
    }

    public bool IsKeyContinuousHold(Key key)
    {
        if (m_prevKeyDown != key)
            return false;

        if (m_keyHold.ElapsedMilliseconds > 700 && m_keyDelay.ElapsedMilliseconds > 20)
        {
            m_keyDelay.Restart();
            return true;
        }

        return false;
    }

    public void Clear()
    {
        m_inputPrevDown.Clear();
        m_inputDownRemove.Clear();
        m_inputDownUpdate.Clear();
        m_inputDown.Clear();
        m_inputUp.Clear();
        MouseMove = (0, 0);
        m_mouseScroll = 0;
        m_typedCharacters.Clear();
    }

    public void Processed()
    {
        m_inputPrevDown.Clear();
        for (int i = 0; i < m_inputDown.Length; i++)
            m_inputPrevDown.Add(m_inputDown[i]);

        m_inputDownRemove.Clear();
        m_inputDownUpdate.Clear();
        for (int i = 0; i < m_inputUp.Length; i++)
            m_inputDownRemove.Add(m_inputUp[i]);
        
        for (int i = 0; i < m_inputDown.Length; i++)
        {
            Key key = m_inputDown[i];
            if (!m_inputDownRemove.Contains(key))
                m_inputDownUpdate.Add(key);
        }

        m_inputDown.Clear();
        for (int i = 0; i < m_inputDownUpdate.Length; i++)
            m_inputDown.Add(m_inputDownUpdate[i]);

        m_inputUp.Clear();

        MouseMove = (0, 0);
        m_mouseScroll = 0;
        m_typedCharacters.Clear();
    }

    public IConsumableInput Poll()
    {
        m_consumableInput.Reset();
        return m_consumableInput;
    }
}
