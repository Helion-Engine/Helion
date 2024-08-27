using Helion.Window;
using Helion.Window.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.Configs.Impl;

public readonly record struct KeyCommandItem(Key Key, string Command);

/// <summary>
/// A case insensitive two-way lookup.
/// </summary>
public class ConfigKeyMapping : IConfigKeyMapping
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public bool Changed { get; private set; }
    private readonly List<KeyCommandItem> m_commands = new();

    private static readonly (Key key, string command)[] DefaultBindings = new[]
    {
        (Key.W,             Constants.Input.Forward),
        (Key.A,             Constants.Input.Left),
        (Key.S,             Constants.Input.Backward),
        (Key.D,             Constants.Input.Right),
        (Key.E,             Constants.Input.Use),
        (Key.ShiftLeft,     Constants.Input.Run),
        (Key.ShiftRight,    Constants.Input.Run),
        (Key.AltLeft,       Constants.Input.Strafe),
        (Key.AltRight,      Constants.Input.Strafe),
        (Key.Left,          Constants.Input.TurnLeft),
        (Key.Right,         Constants.Input.TurnRight),
        (Key.Up,            Constants.Input.LookUp),
        (Key.Down,          Constants.Input.LookDown),
        (Key.Space,         Constants.Input.Jump),
        (Key.C,             Constants.Input.Crouch),
        (Key.Backtick,      Constants.Input.Console),
        (Key.MouseLeft,     Constants.Input.Attack),
        (Key.ControlLeft,   Constants.Input.Attack),
        (Key.ControlRight,  Constants.Input.Attack),
        (Key.One,           Constants.Input.WeaponSlot1),
        (Key.Two,           Constants.Input.WeaponSlot2),
        (Key.Three,         Constants.Input.WeaponSlot3),
        (Key.Four,          Constants.Input.WeaponSlot4),
        (Key.Five,          Constants.Input.WeaponSlot5),
        (Key.Six,           Constants.Input.WeaponSlot6),
        (Key.Seven,         Constants.Input.WeaponSlot7),
        (Key.PrintScreen,   Constants.Input.Screenshot),
        (Key.Equals,        Constants.Input.HudIncrease),
        (Key.Minus,         Constants.Input.HudDecrease),
        (Key.MouseWheelUp,  Constants.Input.NextWeapon),
        (Key.MouseWheelDown, Constants.Input.PreviousWeapon),
        (Key.F2,            Constants.Input.Save),
        (Key.F3,            Constants.Input.Load),
        (Key.F4,            Constants.Input.OptionsMenu),
        (Key.Tab,           Constants.Input.Automap),
        (Key.Pause,         Constants.Input.Pause),
        (Key.F6,            Constants.Input.QuickSave),
        (Key.Escape,        Constants.Input.Menu),
        // Automap bindings
        (Key.Left,          Constants.Input.AutoMapLeft),
        (Key.Right,         Constants.Input.AutoMapRight),
        (Key.Up,            Constants.Input.AutoMapUp),
        (Key.Down,          Constants.Input.AutoMapDown),
        (Key.Equals,        Constants.Input.AutoMapIncrease),
        (Key.Minus,         Constants.Input.AutoMapDecrease),
        (Key.MouseWheelUp,  Constants.Input.AutoMapIncrease),
        (Key.MouseWheelDown, Constants.Input.AutoMapDecrease),
    };

    public void AddDefaultsIfMissing()
    {
        Log.Trace("Adding default key commands to config keys");

        foreach ((Key key, string action) in DefaultBindings)
            Add(key, action);
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
        bool removed = false;
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (cmd.Key != key || !cmd.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
                continue;

            removed = true;
            m_commands.RemoveAt(i);
            break;
        }

        AddEmpty(key);
        return removed;
    }

    public void AddEmpty(Key key)
    {
        if (HasKey(key))
            return;

        m_commands.Add(new(key, string.Empty));
    }

    public bool ConsumeCommandKeyPress(string command, IConsumableInput input, out int scrollAmount)
    {
        scrollAmount = 0;
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ConsumeMouseWheel(cmd.Key, input, out scrollAmount))
                return true;

            if (input.ConsumeKeyPressed(cmd.Key))
                return true;
        }

        return false;
    }

    public bool ConsumeCommandKeyDown(string command, IConsumableInput input, out int scrollAmount, out Key key)
    {
        scrollAmount = 0;
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            key = cmd.Key;
            if (!cmd.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ConsumeMouseWheel(cmd.Key, input, out scrollAmount))
                return true;

            if (input.ConsumeKeyDown(cmd.Key))
                return true;
        }

        key = Key.Unknown;
        return false;
    }

    public bool ConsumeCommandKeyPressOrContinuousHold(string command, IConsumableInput input, out int scrollAmount)
    {
        scrollAmount = 0;
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
                continue;

            if (input.ConsumeKeyPressed(cmd.Key) || input.Manager.IsKeyContinuousHold(cmd.Key))
                return true;

            if (ConsumeMouseWheel(cmd.Key, input, out scrollAmount))
                return true;
        }

        return false;
    }

    public bool IsCommandKeyDown(string command, IConsumableInput input)
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            var cmd = m_commands[i];
            if (!cmd.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
                continue;

            if (input.Manager.IsKeyDown(cmd.Key))
                return true;
        }

        return false;
    }

    private static bool ConsumeMouseWheel(Key key, IConsumableInput input, out int scrollAmount)
    {
        scrollAmount = 0;
        if (key == Key.MouseWheelUp && input.Scroll > 0)
        {
            scrollAmount = input.ConsumeScroll();
            return true;
        }
        else if (key == Key.MouseWheelDown && input.Scroll < 0)
        {
            scrollAmount = input.ConsumeScroll();
            return true;
        }

        return false;
    }

    public void UnbindAll(Key key)
    {
        int removed = m_commands.RemoveAll(x => x.Key == key);
        Changed |= removed > 0;
    }

    public IList<KeyCommandItem> GetKeyMapping() => m_commands;

    public void ReloadDefaults(string command)
    {
        m_commands.RemoveAll(x => x.Command == command);
        foreach ((Key key, _) in DefaultBindings.Where(binding => binding.command == command))
        {
            Add(key, command);
        }
        Changed = true;
    }

    public void ReloadAllDefaults()
    {
        m_commands.Clear();
        AddDefaultsIfMissing();
        Changed = true;
    }
}
