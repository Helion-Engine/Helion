using System;
using System.Diagnostics;
using Helion.Graphics.Fonts;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Drawers
{
    public class MenuDrawer
    {
        private const int SelectedImagePadding = 8;
        private const int ActiveMillis = 500;
        
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Stopwatch m_stopwatch = new();

        public MenuDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_stopwatch.Start();
        }

        public void Draw(Menu menu, RenderCommands renderCommands)
        {
            DrawHelper helper = new(renderCommands);
            
            helper.AtResolution(DoomHudHelper.DoomResolution, () =>
            {
                int offsetY = menu.TopPixelPadding;
                
                foreach (IMenuComponent component in menu)
                {
                    bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
                        
                    switch (component)
                    {
                    case MenuImageComponent imageComponent:
                        DrawImage(helper, imageComponent, isSelected, ref offsetY);
                        break;
                    case MenuPaddingComponent paddingComponent:
                        offsetY += paddingComponent.PixelAmount;
                        break;
                    case MenuSmallTextComponent smallTextComponent:
                        DrawText(helper, smallTextComponent, isSelected, ref offsetY);
                        break;
                    case MenuLargeTextComponent largeTextComponent:
                        DrawText(helper, largeTextComponent, isSelected, ref offsetY);
                        break;
                    default:
                        throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
                    }
                }
            });
        }

        private void DrawText(DrawHelper helper, MenuTextComponent text, bool isSelected, ref int offsetY)
        {
            Font? font = m_archiveCollection.GetFont(text.FontName);
            if (font == null)
                return;
            
            helper.Text(text.Text, font, text.Size, out Dimension area, 0, offsetY, both: Align.TopMiddle);
            offsetY += area.Height;
        }

        private void DrawImage(DrawHelper helper, MenuImageComponent image, bool isSelected, ref int offsetY)
        {
            string name = image.ImageName.ToString();
            int offsetX = image.OffsetX;
            
            helper.Image(name, offsetX, offsetY, out Dimension area, both: Align.TopMiddle);

            if (isSelected)
            {
                string selectedName = (ShouldDrawActive() ? image.ActiveImage : image.InactiveImage) ?? "";
                var (_, w) = helper.DrawInfoProvider.GetImageDimension(selectedName);
                offsetX -= (area.Width / 2) + (w / 2) + SelectedImagePadding;
                
                helper.Image(selectedName, offsetX, offsetY, both: Align.TopMiddle);
            }
            
            offsetY += area.Height + image.PaddingY;
        }

        private bool ShouldDrawActive() => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;
    }
}
