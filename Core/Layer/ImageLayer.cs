using System.Drawing;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers.Helper;

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
            DrawHelper draw = new(commands);
            draw.FillWindow(Color.Black);
            var area = draw.DrawInfoProvider.GetImageDimension(Image);
            draw.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
            {
                commands.DrawImage(Image, 0, 0, area.Width, area.Height, Color.White);
            });
            
            base.Render(commands);
        }
    }
}
