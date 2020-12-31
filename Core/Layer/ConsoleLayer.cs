using System;
using System.Linq;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Terminals;
using Helion.Util.Time;
using MoreLinq;
using TextCopy;

namespace Helion.Layer
{
    public class ConsoleLayer : GameLayer
    {
        private const int NoInputMessageIndex = -1;
        public static readonly CIString LayerName = "CONSOLE";

        private readonly Terminal m_console;
        private readonly ConsoleDrawer m_consoleDrawer;
        private int m_submittedInputIndex = NoInputMessageIndex;

        protected override CIString Name => LayerName;
        protected override double Priority => 0.9f;

        public ConsoleLayer(ArchiveCollection archiveCollection, Terminal console)
        {
            m_console = console;
            m_consoleDrawer = new(archiveCollection);

            console.ClearInputText();
        }

        public override void HandleInput(ConsumableInput consumableInput)
        {
            consumableInput.ConsumeTypedCharacters().ForEach(m_console.AddInput);

            if (consumableInput.ConsumeKeyPressed(InputKey.Backspace))
                m_console.RemoveInputCharacter();
            if (consumableInput.ConsumeKeyPressed(InputKey.Up))
                SetToLessRecentInput();
            if (consumableInput.ConsumeKeyPressed(InputKey.Down))
                SetToMoreRecentInput();
            if (consumableInput.ConsumeKeyPressed(InputKey.Enter))
            {
                m_console.SubmitInputText();
                m_submittedInputIndex = NoInputMessageIndex;
            }

            if (ConsumeControlV(consumableInput))
                AddClipboardToConsole();

            consumableInput.ConsumeAll();

            base.HandleInput(consumableInput);
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

        private static bool ConsumeControlV(ConsumableInput consumableInput)
        {
            // MacOS is going to have problems with this probably!
            bool ctrl = consumableInput.ConsumeKeyPressedOrDown(InputKey.ControlLeft) ||
                        consumableInput.ConsumeKeyPressedOrDown(InputKey.ControlRight);
            bool v = consumableInput.ConsumeKeyPressed(InputKey.V);
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