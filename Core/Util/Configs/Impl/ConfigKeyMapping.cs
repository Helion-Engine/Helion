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

    public Dictionary<Key, HashSet<string>> GetKeyToCommandsDictionary() => m_keyToCommands;

    public IReadOnlySet<string> this[Key key] =>
        m_keyToCommands.TryGetValue(key, out HashSet<string>? commands) ?
            commands :
            EmptyStringSet;

    public IReadOnlySet<Key> this[string command] =>
        m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys) ?
            keys :
            EmptyKeySet;

    public void AddDefaultsIfMissing()
    {
        Log.Trace("Adding default key commands to config keys");

        AddIfMissing(Key.W, Constants.Input.Forward);
        AddIfMissing(Key.A, Constants.Input.Left);
        AddIfMissing(Key.S, Constants.Input.Backward);
        AddIfMissing(Key.D, Constants.Input.Right);
        AddIfMissing(Key.E, Constants.Input.Use);
        AddIfMissing(Key.ShiftLeft, Constants.Input.Run);
        AddIfMissing(Key.ShiftRight, Constants.Input.Run);
        AddIfMissing(Key.AltLeft, Constants.Input.Strafe);
        AddIfMissing(Key.AltRight, Constants.Input.Strafe);
        AddIfMissing(Key.Left, Constants.Input.TurnLeft);
        AddIfMissing(Key.Right, Constants.Input.TurnRight);
        AddIfMissing(Key.Up, Constants.Input.LookUp);
        AddIfMissing(Key.Down, Constants.Input.LookDown);
        AddIfMissing(Key.Space, Constants.Input.Jump);
        AddIfMissing(Key.C, Constants.Input.Crouch);
        AddIfMissing(Key.Backtick, Constants.Input.Console);
        AddIfMissing(Key.MouseLeft, Constants.Input.Attack);
        AddIfMissing(Key.ControlLeft, Constants.Input.Attack);
        AddIfMissing(Key.ControlRight, Constants.Input.Attack);
        AddIfMissing(Key.Up, Constants.Input.NextWeapon);
        AddIfMissing(Key.Down, Constants.Input.PreviousWeapon);
        AddIfMissing(Key.One, Constants.Input.WeaponSlot1);
        AddIfMissing(Key.Two, Constants.Input.WeaponSlot2);
        AddIfMissing(Key.Three, Constants.Input.WeaponSlot3);
        AddIfMissing(Key.Four, Constants.Input.WeaponSlot4);
        AddIfMissing(Key.Five, Constants.Input.WeaponSlot5);
        AddIfMissing(Key.Six, Constants.Input.WeaponSlot6);
        AddIfMissing(Key.Seven, Constants.Input.WeaponSlot7);
        AddIfMissing(Key.PrintScreen, Constants.Input.Screenshot);
        AddIfMissing(Key.Equals, Constants.Input.HudIncrease);
        AddIfMissing(Key.Minus, Constants.Input.HudDecrease);
        AddIfMissing(Key.Equals, Constants.Input.AutoMapIncrease);
        AddIfMissing(Key.Minus, Constants.Input.AutoMapDecrease);
        AddIfMissing(Key.Up, Constants.Input.AutoMapUp);
        AddIfMissing(Key.Down, Constants.Input.AutoMapDown);
        AddIfMissing(Key.Left, Constants.Input.AutoMapLeft);
        AddIfMissing(Key.Right, Constants.Input.AutoMapRight);
        AddIfMissing(Key.F2, Constants.Input.Save);
        AddIfMissing(Key.F3, Constants.Input.Load);
        AddIfMissing(Key.Tab, Constants.Input.Automap);
        AddIfMissing(Key.MouseWheelUp, Constants.Input.NextWeapon);
        AddIfMissing(Key.MouseWheelDown, Constants.Input.PreviousWeapon);
        AddIfMissing(Key.Pause, Constants.Input.Pause);
        AddIfMissing(Key.F6, Constants.Input.QuickSave);
    }

    public void ClearChanged()
    {
        Changed = false;
    }

    public void AddIfMissing(Key key, string command)
    {
        if (key == Key.Unknown || command == "")
            return;

        if (m_keyToCommands.ContainsKey(key))
            return;

        Add(key, command);
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

    public bool ConsumeCommandKeyPressOrContinousHold(string command, IConsumableInput input)
    {
        foreach (Key key in this[command])
        {
            if (input.ConsumeKeyPressed(key) || input.Manager.IsKeyContinuousHold(key))
                return true;

            if (ConsumeMouseWheel(key, input))
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

    public Dictionary<Key, HashSet<string>> GetKeyMapping() => m_keyToCommands;
}
