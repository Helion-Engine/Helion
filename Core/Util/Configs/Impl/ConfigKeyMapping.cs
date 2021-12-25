using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;

namespace Helion.Util.Configs.Impl;

/// <summary>
/// A case insensitive two-way lookup.
/// </summary>
public class ConfigKeyMapping : IConfigKeyMapping
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly IReadOnlySet<Key> EmptyKeySet = new HashSet<Key>();
    private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();

    public bool Changed { get; private set; }
    private readonly Dictionary<Key, HashSet<string>> m_keyToCommands = new();
    private readonly Dictionary<string, HashSet<Key>> m_commandsToKey = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> this[Key key] =>
        m_keyToCommands.TryGetValue(key, out HashSet<string>? commands) ?
            commands :
            EmptyStringSet;

    public IReadOnlySet<Key> this[string command] =>
        m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys) ?
            keys :
            EmptyKeySet;

    public void AddDefaults()
    {
        Log.Trace("Adding default key commands to config keys");

        Add(Key.W, Constants.Input.Forward);
        Add(Key.A, Constants.Input.Left);
        Add(Key.S, Constants.Input.Backward);
        Add(Key.D, Constants.Input.Right);
        Add(Key.E, Constants.Input.Use);
        Add(Key.ShiftLeft, Constants.Input.Run);
        Add(Key.ShiftRight, Constants.Input.Run);
        Add(Key.AltLeft, Constants.Input.Strafe);
        Add(Key.AltRight, Constants.Input.Strafe);
        Add(Key.Left, Constants.Input.TurnLeft);
        Add(Key.Right, Constants.Input.TurnRight);
        Add(Key.Up, Constants.Input.LookUp);
        Add(Key.Down, Constants.Input.LookDown);
        Add(Key.Space, Constants.Input.Jump);
        Add(Key.C, Constants.Input.Crouch);
        Add(Key.Backtick, Constants.Input.Console);
        Add(Key.MouseLeft, Constants.Input.Attack);
        Add(Key.ControlLeft, Constants.Input.Attack);
        Add(Key.ControlRight, Constants.Input.Attack);
        Add(Key.Up, Constants.Input.NextWeapon);
        Add(Key.Down, Constants.Input.PreviousWeapon);
        Add(Key.One, Constants.Input.WeaponSlot1);
        Add(Key.Two, Constants.Input.WeaponSlot2);
        Add(Key.Three, Constants.Input.WeaponSlot3);
        Add(Key.Four, Constants.Input.WeaponSlot4);
        Add(Key.Five, Constants.Input.WeaponSlot5);
        Add(Key.Six, Constants.Input.WeaponSlot6);
        Add(Key.Seven, Constants.Input.WeaponSlot7);
        Add(Key.PrintScreen, Constants.Input.Screenshot);
        Add(Key.Equals, Constants.Input.HudIncrease);
        Add(Key.Minus, Constants.Input.HudDecrease);
        Add(Key.Equals, Constants.Input.AutoMapIncrease);
        Add(Key.Minus, Constants.Input.AutoMapDecrease);
        Add(Key.Up, Constants.Input.AutoMapUp);
        Add(Key.Down, Constants.Input.AutoMapDown);
        Add(Key.Left, Constants.Input.AutoMapLeft);
        Add(Key.Right, Constants.Input.AutoMapRight);
        Add(Key.F2, Constants.Input.Save);
        Add(Key.F3, Constants.Input.Load);
        Add(Key.Tab, Constants.Input.Automap);
        Add(Key.MouseWheelUp, Constants.Input.NextWeapon);
        Add(Key.MouseWheelDown, Constants.Input.PreviousWeapon);
    }

    public void ClearChanged()
    {
        Changed = false;
    }

    public void Add(Key key, string command)
    {
        if (key == Key.Unknown || command == "")
            return;

        if (m_keyToCommands.TryGetValue(key, out HashSet<string>? commands))
        {
            Changed |= !commands.Contains(command);
            commands.Add(command);
        }
        else
        {
            Changed = true;
            m_keyToCommands[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { command };
        }

        if (m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys))
        {
            Changed |= !keys.Contains(key);
            keys.Add(key);
        }
        else
        {
            Changed = true;
            m_commandsToKey[command] = new HashSet<Key> { key };
        }
    }

    public bool ConsumeCommandKeyPress(string command, IConsumableInput input)
    {
        foreach (Key key in this[command])
        {
            if (ConsumeMouseWheel(key, input))
                return true;

            if (input.ConsumeKeyPressed(key))
                return true;
        }

        return false;
    }

    public bool ConsumeCommandKeyDown(string command, IConsumableInput input)
    {
        foreach (Key key in this[command])
        {
            if (ConsumeMouseWheel(key, input))
                return true;

            if (input.ConsumeKeyDown(key))
                return true;
        }

        return false;
    }

    public bool IsCommandKeyDown(string command, IConsumableInput input)
    {
        foreach (Key key in this[command])
        {
            if (input.Manager.IsKeyDown(key))
                return true;
        }

        return false;
    }

    private static bool ConsumeMouseWheel(Key key, IConsumableInput input)
    {
        if (key == Key.MouseWheelUp && input.Manager.Scroll > 0)
            return input.ConsumeScroll() > 0;
        else if (key == Key.MouseWheelDown && input.Manager.Scroll < 0)
            return input.ConsumeScroll() < 0;

        return false;
    }

    public void UnbindAll(Key key)
    {
        if (!m_keyToCommands.TryGetValue(key, out HashSet<string>? commands))
            return;

        foreach (string command in commands)
        {
            if (!m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys))
                continue;

            keys.Remove(key);
            if (keys.Empty())
                m_commandsToKey.Remove(command);
        }

        Changed |= m_keyToCommands.ContainsKey(key);
        m_keyToCommands.Remove(key);
    }

    public IEnumerator<(Key Key, IEnumerable<string> Commands)> GetEnumerator()
    {
        foreach ((Key key, HashSet<string> commands) in m_keyToCommands)
            yield return (key, commands);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
