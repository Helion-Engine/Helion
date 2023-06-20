using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;

namespace Helion.Util.Configs.Impl;

public readonly record struct KeyCommandItem(Key Key, string Command);

/// <summary>
/// A case insensitive two-way lookup.
/// </summary>
public class ConfigKeyMapping : IConfigKeyMapping
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly IReadOnlySet<Key> EmptyKeySet = new HashSet<Key>();
    private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();

    public bool Changed { get; private set; }
    //private readonly Dictionary<Key, HashSet<string>> m_keyToCommands = new();
    //private readonly Dictionary<string, HashSet<Key>> m_commandsToKey = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<KeyCommandItem> m_commands = new();

    //public Dictionary<Key, HashSet<string>> GetKeyToCommandsDictionary() => m_keyToCommands;

    //public IReadOnlySet<string> this[Key key] =>
    //    m_keyToCommands.TryGetValue(key, out HashSet<string>? commands) ?
    //        commands :
    //        EmptyStringSet;

    //public IReadOnlySet<Key> this[string command] =>
    //    m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys) ?
    //        keys :
    //        EmptyKeySet;

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
        AddIfMissing(Key.Left, Constants.Input.TurnLeft, Constants.Input.AutoMapLeft);
        AddIfMissing(Key.Right, Constants.Input.TurnRight, Constants.Input.AutoMapRight);
        AddIfMissing(Key.Up, Constants.Input.LookUp, Constants.Input.AutoMapUp);
        AddIfMissing(Key.Down, Constants.Input.LookDown, Constants.Input.AutoMapDown);
        AddIfMissing(Key.Space, Constants.Input.Jump);
        AddIfMissing(Key.C, Constants.Input.Crouch);
        AddIfMissing(Key.Backtick, Constants.Input.Console);
        AddIfMissing(Key.MouseLeft, Constants.Input.Attack);
        AddIfMissing(Key.ControlLeft, Constants.Input.Attack);
        AddIfMissing(Key.ControlRight, Constants.Input.Attack);
        AddIfMissing(Key.One, Constants.Input.WeaponSlot1);
        AddIfMissing(Key.Two, Constants.Input.WeaponSlot2);
        AddIfMissing(Key.Three, Constants.Input.WeaponSlot3);
        AddIfMissing(Key.Four, Constants.Input.WeaponSlot4);
        AddIfMissing(Key.Five, Constants.Input.WeaponSlot5);
        AddIfMissing(Key.Six, Constants.Input.WeaponSlot6);
        AddIfMissing(Key.Seven, Constants.Input.WeaponSlot7);
        AddIfMissing(Key.PrintScreen, Constants.Input.Screenshot);
        AddIfMissing(Key.Equals, Constants.Input.HudIncrease, Constants.Input.AutoMapIncrease);
        AddIfMissing(Key.Minus, Constants.Input.HudDecrease, Constants.Input.AutoMapDecrease);
        AddIfMissing(Key.MouseWheelUp, Constants.Input.AutoMapIncrease, Constants.Input.NextWeapon);
        AddIfMissing(Key.MouseWheelDown, Constants.Input.AutoMapDecrease, Constants.Input.PreviousWeapon);
        AddIfMissing(Key.F2, Constants.Input.Save);
        AddIfMissing(Key.F3, Constants.Input.Load);
        AddIfMissing(Key.Tab, Constants.Input.Automap);
        AddIfMissing(Key.Pause, Constants.Input.Pause);
        AddIfMissing(Key.F6, Constants.Input.QuickSave);
    }

    public void ClearChanged()
    {
        Changed = false;
    }

    private bool HasKey(Key key)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            if (m_commands[i].Key == key)
                return true;
        }

        return false;
    }

    private void AddIfMissing(Key key, params string[] commands)
    {
        if (HasKey(key))
            return;

        foreach (var command in commands)
            Add(key, command);
    }

    public void Add(Key key, string command)
    {
        if (key == Key.Unknown || command == "")
            return;

        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (cmd.Key == key && command.Equals(cmd.Command, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Changed = true;
        m_commands.Add(new(key, command));
    }

    public bool Remove(Key key)
    {
        int removed = m_commands.RemoveAll(x => x.Key == key);
        Changed = removed > 0;
        return removed > 0;
    }

    public bool Remove(Key key, string command)
    {
        int removed = m_commands.RemoveAll(x => x.Key == key && x.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
        Changed = removed > 0;
        return removed > 0;
    }

    public void AddEmpty(Key key)
    {
        if (HasKey(key))
            return;

        m_commands.Add(new(key, string.Empty));
    }

    public bool ConsumeCommandKeyPress(string command, IConsumableInput input)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command))
                continue;

            if (ConsumeMouseWheel(cmd.Key, input))
                return true;

            if (input.ConsumeKeyPressed(cmd.Key))
                return true;
        }

        return false;
    }

    public bool ConsumeCommandKeyDown(string command, IConsumableInput input)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command))
                continue;

            if (ConsumeMouseWheel(cmd.Key, input))
                return true;

            if (input.ConsumeKeyDown(cmd.Key))
                return true;
        }

        return false;
    }

    public bool ConsumeCommandKeyPressOrContinousHold(string command, IConsumableInput input)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command))
                continue;

            if (input.ConsumeKeyPressed(cmd.Key) || input.Manager.IsKeyContinuousHold(cmd.Key))
                return true;

            if (ConsumeMouseWheel(cmd.Key, input))
                return true;
        }

        return false;
    }

    public bool IsCommandKeyDown(string command, IConsumableInput input)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command))
                continue;

            if (input.Manager.IsKeyDown(cmd.Key))
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
        int removed = m_commands.RemoveAll(x => x.Key == key);
        Changed |= removed > 0;
    }

    //public Dictionary<Key, HashSet<string>> GetKeyMapping() => m_keyToCommands;
    public IList<KeyCommandItem> GetKeyMapping() => m_commands;
}
