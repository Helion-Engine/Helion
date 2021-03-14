using System.Drawing;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Resources;
using Helion.Resources.Archives.Collection;

namespace Helion.Layer
{
    public class HelpLayer : GameLayer
    {
        private readonly ArchiveCollection m_archiveCollection;
        
        protected override double Priority => 0.3;

        public HelpLayer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        // public override void HandleInput(InputEvent input)
        // {
        //     if (input.ConsumeKeyPressed(Key.Escape))
        //         Parent?.Remove<HelpLayer>();
        //     
        //     base.HandleInput(input);
        // }

        public override void Render(RenderCommands commands)
        {
            string helpImage = m_archiveCollection.IwadType == IwadType.Doom2 ? "HELP" : "HELP1";
            
            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            commands.DrawImage(helpImage, 0, 0, width, height, Color.White);
            
            base.Render(commands);
        }
    }
}
