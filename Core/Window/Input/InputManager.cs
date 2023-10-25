using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.Util.Extensions;

namespace Helion.Window.Input;

public class InputManager : IInputManager
{
    public Vec2I MouseMove { get; private set; } = (0, 0);
    public Vec2I MousePosition { get; private set; } = (0, 0);
    private readonly ConsumableInput m_consumableInput;
    private readonly DynamicArray<Key> m_downKeys = new();
    private readonly DynamicArray<Key> m_upKeys = new();
    private readonly DynamicArray<Key> m_prevDownKeys = new();

    private readonly DynamicArray<Key> m_addedDownKeys = new();
    private readonly DynamicArray<Key> m_downKeysToRemove = new();
    private readonly DynamicArray<Key> m_removeAllKeys = new();

    public readonly DynamicArray<char> m_typedKeys = new();

    private readonly DynamicArray<InputKey> m_events = new();
    private readonly DynamicArray<InputKey> m_processEvents = new();

    private readonly Stopwatch m_keyHold = new();
    private readonly Stopwatch m_keyDelay = new();
    private double m_mouseScroll;
    private double m_processMouseScroll;
    private Key m_prevKeyDown;

    public int CurrentScroll => (int)m_mouseScroll;
    public int Scroll => (int)m_processMouseScroll;
    public ReadOnlySpan<char> TypedCharacters => new(m_typedKeys.Data, 0, m_typedKeys.Length);
    public DynamicArray<Key> GetDownKeys() => m_downKeys;
    public DynamicArray<Key> GetPrevDownKeys() => m_prevDownKeys;
    public DynamicArray<InputKey> GetEvents() => m_events;

    public InputManager()
    {
        m_consumableInput = new ConsumableInput(this);
        m_prevKeyDown = Key.Unknown;
    }

    public void SetKeyDown(Key key)
    {
        m_prevKeyDown = key;

        m_events.Add(new InputKey(key, true));
        CheckContinuousKeyHold();
    }

    public void SetKeyUp(Key key)
    {
        if (m_prevKeyDown == key)
            m_prevKeyDown = Key.Unknown;

        m_events.Add(new InputKey(key, false));
        CheckContinuousKeyHold();
    }

    public void AddTypedCharacters(ReadOnlySpan<char> str)
    {
        for (int i = 0; i < str.Length; i++)
            m_typedKeys.Add(str[i]);
    }

    public void SetMousePosition(Vec2I pos)
    {
        MousePosition = pos;
    }

    public void AddMouseMovement(Vec2I movement)
    {
        MouseMove += movement;
    }

    public void AddMouseScroll(double amount)
    {
        m_mouseScroll += amount;
    }

    public bool IsKeyDown(Key key) => SearchKeyState(key, true);
    public bool IsKeyPrevDown(Key key) => SearchPreviousKeyState(key, true);
    public bool IsKeyHeldDown(Key key) => IsKeyDown(key) && IsKeyPrevDown(key);
    public bool IsKeyUp(Key key) => SearchKeyState(key, false);

    public bool IsKeyPressed(Key key)
    {
        bool unpressed = false;
        for (int i = 0; i < m_processEvents.Length; i++)
        {
            var ev = m_processEvents[i];
            if (ev.Key == key && !ev.Pressed)
                unpressed = true;
            else if (ev.Key == key && ev.Pressed && unpressed)
                return true;
        }

        return SearchKeyState(key, true) && !SearchPreviousKeyState(key, true);
    }

    public bool HasAnyKeyPressed()
    {
        for (int i = 0; i < m_processEvents.Length; i++)
        {
            if (IsKeyPressed(m_processEvents[i].Key))
                return true;           
        }

        return false;
    }

    public bool HasAnyKeyDown()
    {
        for (int i = 0; i < m_processEvents.Length; i++)
        {
            if (m_processEvents[i].Pressed)
                return true;
        }

        return m_downKeys.Length > 0;
    }

    public void GetPressedKeys(DynamicArray<Key> pressedKeys)
    {
        for (int i = 0; i < m_processEvents.Length; i++)
        {
            var ev = m_processEvents[i];
            if (!pressedKeys.Contains(ev.Key) && IsKeyPressed(ev.Key))
                pressedKeys.Add(ev.Key);
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
        m_processEvents.Clear();
        m_events.Clear();
        m_downKeys.Clear();
        m_prevDownKeys.Clear();
        m_upKeys.Clear();
        m_typedKeys.Clear();
        MouseMove = (0, 0);
        m_mouseScroll = 0;
        m_processMouseScroll = 0;
    }

    public void ProcessedKeys()
    {
        m_processMouseScroll = 0;

        m_downKeysToRemove.Clear();
        for (int i = 0; i < m_processEvents.Length; i++)
        {
            var ev = m_processEvents[i];
            if (!GetLastKeyState(m_processEvents, ev.Key))
                m_downKeysToRemove.AddUnique(ev.Key);
        }

        RemoveAll(m_downKeys, m_downKeysToRemove);

        m_typedKeys.Clear();
        m_upKeys.Clear();
    }

    private bool GetLastKeyState(DynamicArray<InputKey> events, Key key)
    {
        bool pressed = false;
        for (int i = 0; i <  events.Length; i++)
        {
            var ev = events[i];
            if (ev.Key != key)
                continue;

            pressed = ev.Pressed;
        }

        return pressed;
    }

    public void ProcessedMouseMovement()
    {
        MouseMove = (0, 0);
    }

    public IConsumableInput Poll(bool pollKeys)
    {
        if (pollKeys)
            CreateCurrentInputKeys();

        m_consumableInput.Reset((int)m_processMouseScroll);
        m_consumableInput.HandleKeyInput = pollKeys;
        return m_consumableInput;
    }

    private void CreateCurrentInputKeys()
    {
        m_processMouseScroll = m_mouseScroll;
        m_mouseScroll = 0;

        m_prevDownKeys.Clear();
        m_prevDownKeys.AddRange(m_downKeys);

        m_addedDownKeys.Clear();
        m_processEvents.Clear();
        for (int i = 0; i < m_events.Length; i++)
        {
            var ev = m_events[i];
            if (ev.Pressed)
            {
                m_downKeys.AddUnique(ev.Key);
                m_addedDownKeys.AddUnique(ev.Key);
            }
            else
            {
                m_upKeys.AddUnique(ev.Key);
            }

            m_processEvents.Add(m_events[i]);
        }
        
        m_downKeysToRemove.Clear();
        for (int i = 0; i < m_upKeys.Length; i++)
        {
            Key key = m_upKeys[i];
            if (!m_addedDownKeys.Contains(key))
                m_downKeysToRemove.Add(key);
        }

        RemoveAll(m_downKeys, m_downKeysToRemove);
        m_events.Clear();
    }

    private void CheckContinuousKeyHold()
    {
        if (m_prevKeyDown == Key.Unknown || !IsKeyDown(m_events, m_prevKeyDown))
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

    private bool IsKeyDown(DynamicArray<InputKey> events, Key key)
    {
        bool pressed = false;
        for (int i = 0; i < events.Length; i++)
        {
            var ev = events[i];
            if (ev.Key != key)
                continue;

            pressed = ev.Pressed;
        }

        return pressed;
    }

    private bool SearchKeyState(Key key, bool pressed)
    {
        if (pressed)
            return m_downKeys.Contains(key);

        return !m_downKeys.Contains(key);
    }

    private bool SearchPreviousKeyState(Key key, bool pressed)
    {
        if (pressed)
            return m_prevDownKeys.Contains(key);

        return !m_prevDownKeys.Contains(key);
    }

    private void RemoveAll(DynamicArray<Key> search, DynamicArray<Key> keysToRemove)
    {
        if (keysToRemove.Length == 0)
            return;

        m_removeAllKeys.Clear();
        for (int i = 0; i < search.Length; i++)
        {
            if (keysToRemove.Contains(search[i]))
                continue;
            m_removeAllKeys.Add(search[i]);
        }

        search.Clear();
        search.AddRange(m_removeAllKeys);
    }
}
