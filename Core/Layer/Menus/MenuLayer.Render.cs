using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Menus
{
    public partial class MenuLayer
    {
        private const int ActiveMillis = 500;
        private const int SelectedOffsetX = -32;
        private const int SelectedOffsetY = 5;
        
        private bool ShouldDrawActive => (m_stopwatch.ElapsedMilliseconds % ActiveMillis) <= ActiveMillis / 2;
        
        public void Render(IHudRenderContext hud)
        {
            if (!m_menus.TryPeek(out Menu? menu))
                return;
            
            hud.DoomVirtualResolution(() =>
            {
                int offsetY = menu.TopPixelPadding;
                
                foreach (IMenuComponent component in menu)
                {
                    bool isSelected = ReferenceEquals(menu.CurrentComponent, component);
                        
                    switch (component)
                    {
                    case MenuImageComponent imageComponent:
                        DrawImage(hud, imageComponent, isSelected, ref offsetY);
                        break;
                    case MenuPaddingComponent paddingComponent:
                        offsetY += paddingComponent.PixelAmount;
                        break;
                    case MenuSmallTextComponent smallTextComponent:
                        DrawText(hud, smallTextComponent, ref offsetY);
                        break;
                    case MenuLargeTextComponent largeTextComponent:
                        DrawText(hud, largeTextComponent, ref offsetY);
                        break;
                    case MenuSaveRowComponent saveRowComponent:
                        DrawSaveRow(hud, saveRowComponent, isSelected, ref offsetY);
                        break;
                    default:
                        throw new Exception($"Unexpected menu component type for drawing: {component.GetType().FullName}");
                    }
                }
            });
        }
        
        private void DrawText(IHudRenderContext hud, MenuTextComponent text, ref int offsetY)
        {
            hud.Text(text.Text, text.FontName, text.Size, (0, offsetY), out Dimension area, both: Align.TopMiddle);
            offsetY += area.Height;
        }

        private void DrawImage(IHudRenderContext hud, MenuImageComponent image, bool isSelected, ref int offsetY)
        {
            int drawY = image.PaddingTopY + offsetY;
            if (image.AddToOffsetY)
                offsetY += image.PaddingTopY;

            if (!hud.Textures.TryGet(image.ImageName, out var handle))
                return;
            
            Vec2I offset = TranslateDoomOffset(handle.Offset);
            int offsetX = offset.X + image.OffsetX;
            
            hud.Image(image.ImageName, (offsetX, drawY + offset.Y), out HudBox area, both: (Align)image.ImageAlign);

            if (isSelected)
            {
                string selectedName = (ShouldDrawActive ? image.ActiveImage : image.InactiveImage) ?? "";

                if (!hud.Textures.TryGet(selectedName, out var selectedHandle))
                    return;
                
                offsetX += SelectedOffsetX;
                Vec2I selectedOffset = TranslateDoomOffset(selectedHandle.Offset);
                Vec2I drawPosition = selectedOffset + (offsetX, drawY - SelectedOffsetY);
                hud.Image(selectedName, drawPosition, both: (Align)image.ImageAlign);
            }
            
            if (image.AddToOffsetY)
                offsetY += area.Height + offset.Y + image.PaddingBottomY;
        }

        private void DrawSaveRow(IHudRenderContext hud, MenuSaveRowComponent saveRowComponent, bool isSelected, 
            ref int offsetY)
        {
            const int LeftOffset = 64;
            const int RowVerticalPadding = 4;
            const int SelectionOffsetX = 4;
            const int RowOffsetY = 7;
            const int FontSize = 8;
            const string FontName = "SmallFont";
            const string LeftBarName = "M_LSLEFT";
            const string MiddleBarName = "M_LSCNTR";
            const string RightBarName = "M_LSRGHT";
            
            if (isSelected)
            {
                string selectedName = ShouldDrawActive ? Constants.MenuSelectIconActive : Constants.MenuSelectIconInactive;
                if (hud.Textures.TryGet(selectedName, out var handle))
                {
                    Vec2I selectedOffset = TranslateDoomOffset(handle.Offset);
                    selectedOffset += (LeftOffset - handle.Dimension.Width - SelectionOffsetX, offsetY);
                    hud.Image(selectedName, selectedOffset); 
                }
            }

            if (!hud.Textures.TryGet(LeftBarName, out var leftHandle) ||
                !hud.Textures.TryGet(MiddleBarName, out var midHandle) ||
                !hud.Textures.TryGet(RightBarName, out var rightHandle))
            {
                return;
            }

            int offsetX = LeftOffset;
            Dimension leftDim = leftHandle.Dimension;
            Dimension midDim = midHandle.Dimension;
            Dimension rightDim = rightHandle.Dimension;

            hud.Image(LeftBarName, (offsetX, offsetY + RowOffsetY));
            offsetX += leftDim.Width;
            
            int blocks = (int)Math.Ceiling((MenuSaveRowComponent.PixelWidth - leftDim.Width - rightDim.Width) / (double)midDim.Width) + 1; 
            for (int i = 0; i < blocks; i++)
            {
                hud.Image(MiddleBarName, (offsetX, offsetY + RowOffsetY));
                offsetX += midDim.Width;
            }
            
            hud.Image(RightBarName, (offsetX, offsetY + RowOffsetY));
            
            string saveText = saveRowComponent.Text.Length > blocks ? saveRowComponent.Text.Substring(0, blocks) : saveRowComponent.Text;
            Vec2I origin = (LeftOffset + leftDim.Width + 4, offsetY + 3 + RowOffsetY);
            hud.Text(saveText, FontName, FontSize, origin, out Dimension area);

            offsetY += MathHelper.Max(area.Height, leftDim.Height, midDim.Height, rightDim.Width) + RowVerticalPadding;
        }
    }
}
