using System;
using System.Diagnostics;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Render.Legacy.Commands;
using Helion.Render.Legacy.Commands.Alignment;
using Helion.Render.Legacy.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Legacy.Shared.Drawers
{
    public class MenuDrawer
    {
        private const int ActiveMillis = 500;

        private const int SelectedOffsetX = -32;
        private const int SelectedOffsetY = 5;

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
                        DrawText(helper, smallTextComponent, ref offsetY);
                        break;
                    case MenuLargeTextComponent largeTextComponent:
                        DrawText(helper, largeTextComponent, ref offsetY);
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

        private void DrawText(DrawHelper helper, MenuTextComponent text, ref int offsetY)
        {
            Font? font = m_archiveCollection.GetFont(text.FontName);
            if (font == null)
                return;
            
            helper.Text(text.Text, font, text.Size, out Dimension area, 0, offsetY, both: Align.TopMiddle);
            offsetY += area.Height;
        }

        private void DrawImage(DrawHelper helper, MenuImageComponent image, bool isSelected, ref int offsetY)
        {
            int drawY = image.PaddingTopY + offsetY;
            if (image.AddToOffsetY)
                offsetY += image.PaddingTopY;

            var dimension = helper.DrawInfoProvider.GetImageDimension(image.ImageName);
            Vec2I offset = helper.DrawInfoProvider.GetImageOffset(image.ImageName);
            helper.TranslateDoomOffset(ref offset, dimension);
            int offsetX = image.OffsetX + offset.X;

            helper.Image(image.ImageName, offsetX, drawY + offset.Y, out Dimension area, both: image.ImageAlign);

            if (isSelected)
            {
                string selectedName = (ShouldDrawActive() ? image.ActiveImage : image.InactiveImage) ?? string.Empty;
                dimension = helper.DrawInfoProvider.GetImageDimension(selectedName);
                offsetX += SelectedOffsetX;
                Vec2I selectedOffset = helper.DrawInfoProvider.GetImageOffset(selectedName);
                helper.TranslateDoomOffset(ref selectedOffset, dimension);

                helper.Image(selectedName, offsetX + selectedOffset.X,
                    drawY + selectedOffset.Y - SelectedOffsetY, both: image.ImageAlign);
            }
            
            if (image.AddToOffsetY)
                offsetY += area.Height + offset.Y + image.PaddingBottomY;
        }

        private bool ShouldDrawActive() => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;
        
        private void DrawSaveRow(DrawHelper helper, MenuSaveRowComponent saveRowComponent, bool isSelected, 
            ref int offsetY)
        {
            const int LeftOffset = 64;
            const int RowVerticalPadding = 4;
            const int SelectionOffsetX = 4;
            const int RowOffsetY = 7; // Vanilla offsets by 7...

            Font? font = m_archiveCollection.GetFont("SmallFont");
            if (font == null)
                return;
            
            if (isSelected)
            {
                string selectedName = ShouldDrawActive() ? Constants.MenuSelectIconActive : Constants.MenuSelectIconInactive;
                var dimension = helper.DrawInfoProvider.GetImageDimension(selectedName);
                Vec2I offset = helper.DrawInfoProvider.GetImageOffset(selectedName);
                helper.TranslateDoomOffset(ref offset, dimension);

                helper.Image(selectedName, LeftOffset - dimension.Width - SelectionOffsetX + offset.X, offsetY + offset.Y);
            }

            var leftDimension = helper.DrawInfoProvider.GetImageDimension("M_LSLEFT");
            var midDimension = helper.DrawInfoProvider.GetImageDimension("M_LSCNTR");
            var rightDimension = helper.DrawInfoProvider.GetImageDimension("M_LSRGHT");
            int offsetX = LeftOffset;

            helper.Image("M_LSLEFT", offsetX, offsetY + RowOffsetY);
            offsetX += leftDimension.Width;

            int blocks = (int)Math.Ceiling((MenuSaveRowComponent.PixelWidth - leftDimension.Width - rightDimension.Width) / (double)midDimension.Width) + 1; 
            for (int i = 0; i < blocks; i++)
            {
                helper.Image("M_LSCNTR", offsetX, offsetY + RowOffsetY);
                offsetX += midDimension.Width;
            }
            
            helper.Image("M_LSRGHT", offsetX, offsetY + RowOffsetY);

            string saveText = saveRowComponent.Text.Length > blocks ? saveRowComponent.Text.Substring(0, blocks) : saveRowComponent.Text;
            ColoredString text = ColoredStringBuilder.From(Color.Red, saveText);
            helper.Text(text, font, 8, out Dimension area, LeftOffset + leftDimension.Width + 4, offsetY + 3 + RowOffsetY);

            offsetY += MathHelper.Max(area.Height, leftDimension.Height, midDimension.Height, rightDimension.Width) + RowVerticalPadding;
        }
    }
}
