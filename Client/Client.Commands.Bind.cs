using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Window.Input;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;
using Helion.Util.Extensions;
using Helion.Util;

namespace Helion.Client;

public partial class Client
{
    [ConsoleCommand("bind", "Bind a key")]
    private void BindCommand(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count == 0)
        {
            Log.Info("Key bindings");
            foreach (var item in m_config.Keys.GetKeyMapping())
                foreach (var value in item.Value)
                    Log.Info($"{item.Key}: {value}");
            return;
        }

        if (args.Args.Count < 2)
            Log.Error("Bind requries two arguments");

        string key = args.Args[0];
        string command = args.Args[1];

        if (!GetInputKey(key, out var inputKey))
            return;

        var inputCommands = GetAvailableInputCommands();
        if (!inputCommands.Any(x => x.EqualsIgnoreCase(command)))
        {
            Log.Error($"Invalid command: {command}");
            Log.Info("Use inputcommands to view all available commands");
            return;
        }

        m_config.Keys.Add(inputKey.Value, command);
    }

    [ConsoleCommand("unbind", "Unbinds a key")]
    private void UnbindCommand(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count < 1)
            Log.Error("Bind requries one argument");

        if (!GetInputKey(args.Args[0], out var inputKey))
            return;

        m_config.Keys.Remove(inputKey.Value);
    }

    [ConsoleCommand("inputkeys", "List all input keys")]
    private void InputKeys(ConsoleCommandEventArgs args)
    {
        var values = Enum.GetValues(typeof(Key));
        foreach (var value in values)
            Log.Info(value);
    }


    [ConsoleCommand("inputcommands", "List all input commands")]
    private void InputCommands(ConsoleCommandEventArgs args)
    {
        var inputCommands = GetAvailableInputCommands();
        foreach (var inputCommand in inputCommands)
            Log.Info(inputCommand);
    }

    private static bool GetInputKey(string key, [NotNullWhen(true)] out Key? inputKey)
    {
        if (!Enum.TryParse(typeof(Key), key, true, out var parsedKey) || parsedKey == null)
        {
            inputKey = null;
            Log.Error($"Invalid key: {key}");
            Log.Info("Use inputkeys to view all available keys");
            return false;
        }

        inputKey = (Key)parsedKey;
        return true;
    }

    private static IList<string> GetAvailableInputCommands()
    {
        var properties = typeof(Constants.Input).GetFields();
        return properties.Select(x => x.Name).OrderBy(x => x).ToArray();
    }
}
