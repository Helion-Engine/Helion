using Helion.Graphics;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Window;
using Helion.Window.Input;
using Helion.World.Cheats;
using System;
using System.Collections.Generic;
using System.Linq;
using TextCopy;

namespace Helion.Layer.Consoles;

public partial class ConsoleLayer
{
    private const int NoInputMessageIndex = -1;
    private static readonly Clipboard Clipboard = new Clipboard();

    private readonly List<string> m_bestOptions = new();
    private DateTime m_lastTabTime = DateTime.Now;
    private int m_submittedInputIndex = NoInputMessageIndex;
    private int m_bestOptionIndex;

    public void HandleInput(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            Animation.AnimateOut();
            return;
        }

        if (ConsumeControlV(input))
        {
            input.ConsumeTypedCharacters();
            AddClipboardToConsole();
        }

        foreach (char c in input.ConsumeTypedCharacters())
            if (c != '`')
                m_console.AddInput(c);

        if (input.ConsumePressOrContinuousHold(Key.Backspace))
            m_console.RemoveInputCharacter();
        if (input.ConsumePressOrContinuousHold(Key.Up))
            SetToLessRecentInput();
        if (input.ConsumePressOrContinuousHold(Key.Down))
            SetToMoreRecentInput();
        if (input.ConsumePressOrContinuousHold(Key.PageUp))
            GoBackInMessageHistory();
        if (input.ConsumePressOrContinuousHold(Key.PageDown))
            GoForwardInMessageHistory();
        if (input.ConsumeKeyPressed(Key.Tab))
        {
            ResetMessageHistory();
            ApplyAutocomplete();
        }

        if (input.ConsumeKeyPressed(Key.Enter))
        {
            ResetMessageHistory();
            m_console.SubmitInputText();
            m_submittedInputIndex = NoInputMessageIndex;
        }

        input.ConsumeAll();
    }

    private void GoBackInMessageHistory() =>
        m_messageRenderOffset = Math.Min(m_messageRenderOffset + 10, m_console.Messages.Count - 1);

    private void GoForwardInMessageHistory() =>
        m_messageRenderOffset = Math.Max(0, m_messageRenderOffset - 10);

    private void ResetMessageHistory() => 
        m_messageRenderOffset = 0;

    private void ApplyAutocomplete()
    {
        const long DoubleTabMillisecondThreshold = 500;

        if (m_console.Input.Empty())
        {
            if (m_console.SubmittedInput.Count > 0 && 
                !m_console.SubmittedInput.Last().Equals(Constants.ConsoleCommands.Commands, StringComparison.OrdinalIgnoreCase))
            {
                m_console.AddInput(Constants.ConsoleCommands.Commands);
                m_console.SubmitInputText();
            }
            return;
        }

        TimeSpan delta = DateTime.Now - m_lastTabTime;
        if (delta.TotalMilliseconds < DoubleTabMillisecondThreshold)
            DoAutoCompleteFill();
        else
            DoAutoCompleteEnumeration();

        m_lastTabTime = DateTime.Now;
    }

    private void DoAutoCompleteFill()
    {
        string input = m_console.Input;

        if (m_bestOptions.Count > 1 && input == m_bestOptions[m_bestOptionIndex])
        {
            m_console.ClearInputText();
            m_bestOptionIndex = ++m_bestOptionIndex % m_bestOptions.Count;
            m_console.AddInput(m_bestOptions[m_bestOptionIndex]);
            return;
        }

        m_bestOptions.Clear();
        m_bestOptionIndex = 0;

        foreach ((string command, _) in m_consoleCommands.OrderBy(x => x.command))
            AssignIfBest(input, command);
        foreach (var component in m_config.GetComponents().Where(x => !x.Value.Attribute.Legacy).OrderBy(x => x.Key))
            AssignIfBestPath(input, component.Key);
        foreach (ICheat cheat in CheatManager.Cheats.OrderBy(x => x.ConsoleCommand))
            if (cheat.ConsoleCommand != null)
                AssignIfBest(input, cheat.ConsoleCommand);

        if (m_bestOptions.Count > 0)
        {
            m_console.ClearInputText();
            m_console.AddInput(m_bestOptions[0]);
        }
    }

    void AssignIfBest(string input, string item)
    {
        if (!item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            return;

        if (m_bestOptions.Count == 0)
        {
            m_bestOptions.Add(item);
            return;
        }

        m_bestOptions.Add(item);
    }

    void AssignIfBestPath(string input, string path)
    {
        if (!path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            return;

        var partial = GetAutoCompletePathText(input, path);
        if (m_bestOptions.Count > 0 && m_bestOptions[0].Equals(partial))
            return;

        m_bestOptions.Add(partial);
    }

    private void DoAutoCompleteEnumeration()
    {
        bool foundAtLeastOne = false;
        string input = m_console.Input;
        HelionLog.Info($"Matching values for: {input}");

        SearchConsoleCommands();
        SearchConfigValues();
        SearchCheats();

        if (!foundAtLeastOne)
            HelionLog.Info("No matches found");

        void SearchConsoleCommands()
        {
            foreach ((string command, ConsoleCommandData data) in m_consoleCommands.OrderBy(x => x.command))
            {
                if (command.EqualsIgnoreCase(input))
                {
                    EmitMessage((Color.SaddleBrown, data.Info.Description));
                    foreach (ConsoleCommandArgAttribute arg in data.Args)
                    {
                        string name = arg.Optional ? $"[{arg.Name}]" : $"<{arg.Name}>";
                        EmitMessage((Color.RosyBrown, $"  {name} "), (Color.SaddleBrown, $"- {arg.Description}"));
                    }

                    foundAtLeastOne = true;
                }
                else if (command.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    foundAtLeastOne = true;
                    EmitMessage((Color.Khaki, $"  {command}"));
                }
            }
        }

        void SearchConfigValues()
        {
            foreach ((string path, ConfigComponent component) in m_config.GetComponents().OrderBy(x => x.Key))
            {
                if (component.Attribute.Legacy)
                    continue;

                if (path.EqualsIgnoreCase(input))
                {
                    foundAtLeastOne = true;
                    EmitMessage((Color.SaddleBrown, component.Attribute.Description));
                    if (!component.Attribute.Save)
                        EmitMessage((Color.SaddleBrown, "Note this value is transient and is not saved to the config"));

                    Type componentType = component.Value.ObjectValue.GetType();
                    if (componentType.IsEnum)
                    {
                        EmitMessage((Color.SaddleBrown, "Eligible values:"));
                        foreach (object? enumValue in Enum.GetValues(componentType))
                            EmitMessage((Color.SaddleBrown, $"    {(int)enumValue} ({enumValue})"));
                    }
                }
                else if (path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    foundAtLeastOne = true;
                    EmitMessage((Color.Chocolate, $"  {path}"));
                }
            }
        }

        void SearchCheats()
        {
            foreach (ICheat cheat in CheatManager.Cheats.OrderBy(x => x.ConsoleCommand))
            {
                string? cmd = cheat.ConsoleCommand;
                if (cmd == null)
                    continue;

                if (cmd.EqualsIgnoreCase(input))
                {
                    foundAtLeastOne = true;
                    EmitMessage((Color.SaddleBrown, cheat.CheatType.ToString()));
                }
                else if (cmd.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    foundAtLeastOne = true;
                    EmitMessage((Color.RosyBrown, $"  {cmd}"));
                }
            }
        }
    }

    void EmitMessage(params (Color color, string message)[] sentencePieces)
    {
        foreach ((Color color, string message) in sentencePieces)
            m_console.AddMessage(color, message);
    }

    private string GetAutoCompletePathText(string input, string path)
    {
        int pathCount = input.Count(x => x == '.') + 1;
        if (input.EndsWith('.'))
            pathCount--;
        int currentCount = 0;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '.')
                currentCount++;

            if (currentCount == pathCount)
                return path[..(i+1)];
        }

        return path;
    }

    private static bool ConsumeControlV(IConsumableInput input)
    {
        // Note: If we support the OS, then MacOS is going to have problems with this.
        bool ctrl = input.ConsumeKeyDown(Key.ControlLeft) || input.ConsumeKeyDown(Key.ControlRight);
        bool v = input.ConsumeKeyPressed(Key.V);
        return ctrl && v;
    }

    private void SetToMoreRecentInput()
    {
        if (m_console.SubmittedInput.Empty() || m_submittedInputIndex == NoInputMessageIndex)
            return;

        m_submittedInputIndex = Math.Max(m_submittedInputIndex - 1, NoInputMessageIndex);

        m_console.ClearInputText();

        // If we press down and currently was at the most recent message,
        // we will just clear the input and exit to act like both ZDoom (I
        // think it does that?) and like the terminal does on Linux.
        if (m_submittedInputIndex == NoInputMessageIndex)
            return;

        string message = m_console.SubmittedInput.ElementAt(m_submittedInputIndex);
        m_console.AddInput(message);
    }

    private void SetToLessRecentInput()
    {
        if (m_console.SubmittedInput.Empty())
            return;

        m_submittedInputIndex = Math.Min(m_submittedInputIndex + 1, m_console.SubmittedInput.Count - 1);
        string message = m_console.SubmittedInput.ElementAt(m_submittedInputIndex);

        m_console.ClearInputText();
        m_console.AddInput(message);
    }

    private void AddClipboardToConsole()
    {
        bool resetInputTracking = false;
        var text = Clipboard.GetText();
        if (text == null)
            return;

        if (text.Contains('\n'))
        {
            resetInputTracking = true;
            text = text.Split('\n').First();
        }

        text = text.Trim();
        if (text.Empty())
            return;

        m_console.AddInput(text);

        if (resetInputTracking)
            m_submittedInputIndex = NoInputMessageIndex;
    }
}
