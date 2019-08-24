using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Util;
using Helion.Util.Configuration;
using MoreLinq;

namespace Helion.Layer
{
    public class ConsoleWorldLayer : GameLayer
    {
        public static readonly CIString LayerName = "CONSOLE";
        private readonly HelionConsole m_console;

        protected override CIString Name => LayerName;
        protected override double Priority => 0.9f;

        public ConsoleWorldLayer(HelionConsole console)
        {
            m_console = console;

            console.ClearInputText();
        }
        
        public override void HandleInput(ConsumableInput consumableInput)
        {
            consumableInput.ConsumeTypedCharacters().ForEach(m_console.AddInput);
            consumableInput.ConsumeAll();
            
            base.HandleInput(consumableInput);
        }

        public override void Render(RenderCommands renderCommands)
        {
            // TODO: This should not use the window dimension.
            ConsoleDrawer.Draw(m_console, renderCommands.WindowDimension, renderCommands);
            
            base.Render(renderCommands);
        }
    }
}