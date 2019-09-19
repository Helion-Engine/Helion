using System.Linq;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Util;
using Helion.Util.Extensions;
using MoreLinq;
using TextCopy;

namespace Helion.Layer
{
    public class ConsoleLayer : GameLayer
    {
        public static readonly CIString LayerName = "CONSOLE";
        private readonly HelionConsole m_console;

        protected override CIString Name => LayerName;
        protected override double Priority => 0.9f;

        public ConsoleLayer(HelionConsole console)
        {
            m_console = console;

            console.ClearInputText();
        }
        
        public override void HandleInput(ConsumableInput consumableInput)
        {
            consumableInput.ConsumeTypedCharacters().ForEach(m_console.AddInput);

            if (consumableInput.ConsumeKeyPressed(InputKey.Backspace))
                m_console.RemoveInputCharacter();
            
            if (consumableInput.ConsumeKeyPressed(InputKey.Enter))
                m_console.SubmitInputText();

            if (ConsumeControlV(consumableInput))
                AddClipboardToConsole();
            
            consumableInput.ConsumeAll();
            
            base.HandleInput(consumableInput);
        }

        public override void Render(RenderCommands renderCommands)
        {
            // TODO: This should not use the window dimension.
            ConsoleDrawer.Draw(m_console, renderCommands.WindowDimension, renderCommands);
            
            base.Render(renderCommands);
        }

        protected override void PerformDispose()
        {
            m_console.ClearInputText();
            
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
        
        private void AddClipboardToConsole()
        {
            string text = Clipboard.GetText();

            if (text.Contains('\n'))
                text = text.Split('\n').First();

            text = text.Trim();
            if (text.Empty())
                return;
            
            m_console.AddInput(text);
        }
    }
}