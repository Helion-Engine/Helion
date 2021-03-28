using System;
using System.Diagnostics;
using System.Drawing;
using Helion.Geometry;
using Helion.Graphics.String;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Geometry;
using Font = Helion.Graphics.Fonts.Font;

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
            
            helper.AtResolution(DoomHudHelper.DoomResolutionInfoCenter, () =>
            {
                int offsetY = menu.TopPixelPadding;
                int? maxWidth = CalculateMaxWidth(helper, menu);
                
                foreach (IMenuComponent component in menu)
                {
                    bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
                        
                    switch (component)
                    {
                    case MenuImageComponent imageComponent:
                        DrawImage(helper, imageComponent, isSelected, maxWidth, ref offsetY);
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
                    case MenuSaveRowComponent saveRowComponent:
                        DrawSaveRow(helper, saveRowComponent, isSelected, ref offsetY);
                        break;
                    default:
                        throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
                    }
                }
            });
        }
        
        private static int? CalculateMaxWidth(DrawHelper helper, Menu menu)
        {
            if (!menu.LeftAlign)
                return null;

            int maxWidth = 0;

            foreach (IMenuComponent menuComponent in menu)
            {
                // This is done this way to keep the logic simple, as we will
                // eventually replace this UI with something better.
                switch (menuComponent)
                {
                    case MenuImageComponent imageComponent:
                    {
                        int width = helper.DrawInfoProvider.GetImageDimension(imageComponent.ImageName.ToString()).Width;
                        maxWidth = Math.Max(maxWidth, width);
                        break;
                    }
                    case MenuSaveRowComponent:
                    {
                        maxWidth = Math.Max(maxWidth, MenuSaveRowComponent.PixelWidth);
                        break;
                    }
                }
            }

            return maxWidth;
        }

        private void DrawText(DrawHelper helper, MenuTextComponent text, bool isSelected, ref int offsetY)
        {
            Font? font = m_archiveCollection.GetFont(text.FontName);
            if (font == null)
                return;
            
            helper.Text(text.Text, font, text.Size, out Dimension area, 0, offsetY, both: Align.TopMiddle);
            offsetY += area.Height;
        }

        private void DrawImage(DrawHelper helper, MenuImageComponent image, bool isSelected, int? maxWidth,
            ref int offsetY)
        {
            string name = image.ImageName.ToString();
            int imageWidth = helper.DrawInfoProvider.GetImageDimension(image.ImageName.ToString()).Width;
            int offsetX = -((maxWidth - imageWidth) / 2 ?? 0) + image.OffsetX;
            
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
        
        private void DrawSaveRow(DrawHelper helper, MenuSaveRowComponent saveRowComponent, bool isSelected, 
            ref int offsetY)
        {
            const int LeftOffset = 64;
            const int RowVerticalPadding = 8;
            const int SelectionOffsetX = 4;
            
            Font? font = m_archiveCollection.GetFont("SmallFont");
            if (font == null)
                return;
            
            if (isSelected)
            {
                string selectedName = ShouldDrawActive() ? Constants.MenuSelectIconActive : Constants.MenuSelectIconInactive;
                var (w, _) = helper.DrawInfoProvider.GetImageDimension(selectedName);
                
                helper.Image(selectedName, LeftOffset - w - SelectionOffsetX, offsetY);
            }

            var (leftWidth, leftHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSLEFT");
            var (middleWidth, middleHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSCNTR");
            var (rightWidth, rightHeight) = helper.DrawInfoProvider.GetImageDimension("M_LSRGHT");
            int offsetX = LeftOffset;

            helper.Image("M_LSLEFT", offsetX, offsetY);
            offsetX += leftWidth;

            int blocks = (int)Math.Ceiling((MenuSaveRowComponent.PixelWidth - leftWidth - rightWidth) / (double)middleWidth); 
            for (int i = 0; i < blocks; i++)
            {
                helper.Image("M_LSCNTR", offsetX, offsetY);
                offsetX += middleWidth;
            }
            
            helper.Image("M_LSRGHT", offsetX, offsetY);

            ColoredString text = ColoredStringBuilder.From(Color.Red, saveRowComponent.Text);
            helper.Text(text, font, 8, out Dimension area, LeftOffset + leftWidth + 4, offsetY + 3);

            offsetY += MathHelper.Max(area.Height, leftHeight, middleHeight, rightHeight) + RowVerticalPadding;
        }
    }
}
