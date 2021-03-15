using System.Drawing;
using Helion.Render.Commands;

namespace Helion.Layer
{
    public class ImageLayer : GameLayer
    {
        public string Image { get; set; }
        
        protected override double Priority => 0.8;

        public ImageLayer(string image)
        {
            Image = image;
        }

        public override void Render(RenderCommands commands)
        {
            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            commands.DrawImage(Image, 0, 0, width, height, Color.White);
            
            base.Render(commands);
        }
    }
}
