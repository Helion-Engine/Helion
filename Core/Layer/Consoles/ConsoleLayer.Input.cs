using System;
using System.Linq;
using Helion.Util.Extensions;
using Helion.Window.Input;
using TextCopy;

namespace Helion.Layer.Consoles
{
    public partial class ConsoleLayer
    {
        private const int NoInputMessageIndex = -1;
        
        private int m_submittedInputIndex = NoInputMessageIndex;
        
        public void HandleInput(InputEvent input)
        {
            if (ConsumeControlV(input))
            {
                input.ConsumeTypedCharacter('v', 'V');
                AddClipboardToConsole();
            }

            foreach (char c in input.GetTypedCharacters())
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
            // TODO
        }

        private static bool ConsumeControlV(InputEvent input)
        {
            // MacOS is going to have problems with this probably!
            bool ctrl = input.ConsumeKeyPressedOrDown(Key.ControlLeft) ||
                        input.ConsumeKeyPressedOrDown(Key.ControlRight);
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
