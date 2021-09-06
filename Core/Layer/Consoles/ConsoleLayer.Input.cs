using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Graphics.String;
using Helion.Util.Configs.Components;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using Helion.World.Cheats;
using TextCopy;

namespace Helion.Layer.Consoles
{
    public partial class ConsoleLayer
    {
        private const int NoInputMessageIndex = -1;
        
        private DateTime m_lastTabTime = DateTime.Now;
        private int m_submittedInputIndex = NoInputMessageIndex;
        
        public void HandleInput(IConsumableInput input)
        {
            if (ConsumeControlV(input))
            {
                input.ConsumeTypedCharacters();
                AddClipboardToConsole();
            }

            foreach (char c in input.ConsumeTypedCharacters())
                m_console.AddInput(c);
            input.ConsumeTypedCharacters();

            if (input.ConsumeKeyPressed(Key.Backspace))
                m_console.RemoveInputCharacter();
            if (input.ConsumeKeyPressed(Key.Up))
                SetToLessRecentInput();
            if (input.ConsumeKeyPressed(Key.Down))
                SetToMoreRecentInput();
            if (input.ConsumeKeyPressed(Key.Tab))
                ApplyAutocomplete();

            if (input.ConsumeKeyPressed(Key.Enter))
            {
                m_console.SubmitInputText();
                m_submittedInputIndex = NoInputMessageIndex;
            }

            input.ConsumeAll();
        }

        private void ApplyAutocomplete()
        {
            const long DoubleTabMillisecondThreshold = 500;

            if (m_console.Input.Empty())
                return;
            
            TimeSpan delta = DateTime.Now - m_lastTabTime;
            if (delta.TotalMilliseconds < DoubleTabMillisecondThreshold)
                DoAutoCompleteFill();
            else
                DoAutoCompleteEnumeration();

            m_lastTabTime = DateTime.Now;
        }

        private void DoAutoCompleteFill()
        {
            Log.Info("Doing autocomplete fill");
        }

        private void DoAutoCompleteEnumeration()
        {
            bool foundAtLeastOne = false;
            string input = m_console.Input;
            Log.Info($"Matching values for: {input}");
            
            SearchConsoleCommands();
            SearchConfigValues();
            SearchCheats();

            if (!foundAtLeastOne)
                Log.Info("No matches found");
            
            void SearchConsoleCommands()
            {
                foreach ((string command, ConsoleCommandData data) in m_consoleCommands)
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
                foreach ((string path, ConfigComponent component) in m_config)
                {
                    if (path.EqualsIgnoreCase(input))
                    {
                        foundAtLeastOne = true;
                        EmitMessage((Color.SaddleBrown, component.Attribute.Description));
                        if (!component.Attribute.Save)
                            EmitMessage((Color.SaddleBrown, "Note this value is transient and is not saved to the config"));
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
                foreach (ICheat cheat in CheatManager.Instance)
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

            void EmitMessage(params (Color color, string message)[] sentencePieces)
            {
                List<object> elements = new();
                foreach ((Color color, string message) in sentencePieces)
                {
                    elements.Add(color);
                    elements.Add(message);
                }

                ColoredString str = ColoredStringBuilder.From(elements.ToArray());
                m_console.AddMessage(str);
            }
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
            string text = Clipboard.GetText();

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
}
