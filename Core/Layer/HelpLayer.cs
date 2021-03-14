using System.Drawing;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;

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

        public override void Render(RenderCommands commands)
        {
            string helpImage = m_archiveCollection.IWadType.IsDoom1() ? "HELP1" : "HELP";
            
            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            commands.DrawImage(helpImage, 0, 0, width, height, Color.White);
            
            base.Render(commands);
        }
    }
}
