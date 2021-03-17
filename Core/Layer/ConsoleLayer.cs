using System;
using System.Linq;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using MoreLinq.Extensions;
using TextCopy;

namespace Helion.Layer
{
    public class ConsoleLayer : GameLayer
    {
        private const int NoInputMessageIndex = -1;

        private readonly HelionConsole m_console;
        private readonly ConsoleDrawer m_consoleDrawer;
        private int m_submittedInputIndex = NoInputMessageIndex;

        protected override double Priority => 0.9f;

        public ConsoleLayer(ArchiveCollection archiveCollection, HelionConsole console)
        {
            m_console = console;
            m_consoleDrawer = new ConsoleDrawer(archiveCollection);

            console.ClearInputText();
        }

        public override void HandleInput(InputEvent input)
        {
            if (ConsumeControlV(input))
            {
                input.ConsumeTypedCharacter('v', 'V');
                AddClipboardToConsole();
            }

            input.ConsumeTypedCharacters().ForEach(m_console.AddInput);

            if (input.ConsumeKeyPressed(Key.Backspace))
                m_console.RemoveInputCharacter();
            if (input.ConsumeKeyPressed(Key.Up))
                SetToLessRecentInput();
            if (input.ConsumeKeyPressed(Key.Down))
                SetToMoreRecentInput();
            if (input.ConsumeKeyPressed(Key.Tab))
                m_console.ApplyAutocomplete();

            if (input.ConsumeKeyPressed(Key.Enter))
            {
                m_console.SubmitInputText();
                m_submittedInputIndex = NoInputMessageIndex;
            }

            input.ConsumeAll();

            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            // TODO: This should not use the window dimension.
            m_consoleDrawer.Draw(m_console, renderCommands.WindowDimension, renderCommands);

            base.Render(renderCommands);
        }

        protected override void PerformDispose()
        {
            m_console.ClearInputText();
            m_console.LastClosedNanos = Ticker.NanoTime();

            base.PerformDispose();
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