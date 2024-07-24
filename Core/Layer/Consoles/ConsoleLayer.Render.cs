using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Consoles;
using Helion.Util.Timing;

namespace Helion.Layer.Consoles;

public partial class ConsoleLayer
{
    private const string FontName = "Console";
    private const long FlashSpanNanos = 500 * 1000L * 1000L;
    private const long HalfFlashSpanNanos = FlashSpanNanos / 2;
    private static bool IsCursorFlashTime => Ticker.NanoTime() % FlashSpanNanos < HalfFlashSpanNanos;

    public static int GetRenderHeight(IHudRenderContext hud) => hud.Height / 2;

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        Animation.Tick();
        ctx.ClearDepth();

        var drawArea = GetDrawArea(hud);

        RenderBackground(hud, drawArea);
        RenderInput(hud, drawArea, out int inputHeight);
        RenderMessages(hud, inputHeight, drawArea);
    }

    private int FontSize => m_config.Console.FontSize;

    private void RenderBackground(IHudRenderContext hud, HudBox drawArea)
    {
        RenderConsoleBackground(hud, drawArea);
        RenderConsoleDivider(hud, drawArea);
    }

    private HudBox GetDrawArea(IHudRenderContext hud)
    {
        return (0, 0, hud.Width, hud.Height);
    }

    private void RenderConsoleBackground(IHudRenderContext hud, HudBox drawArea)
    {
        const string ConsoleBackingImage = "CONBACK";
        const float BackgroundAlpha = 0.65f;

        if (hud.Textures.HasImage(ConsoleBackingImage))
        {
            hud.Image(ConsoleBackingImage, drawArea);
            hud.FillBox(drawArea, Color.Black, alpha: BackgroundAlpha);
        }
        else if (hud.Textures.HasImage(m_backingImage))
        {
            hud.Image(m_backingImage, drawArea);
            hud.FillBox(drawArea, Color.Black, alpha: BackgroundAlpha);
        }
        else
        {
            hud.FillBox((0, 0, hud.Width, hud.Height), Color.Gray);
        }
    }

    private void RenderConsoleDivider(IHudRenderContext hud, HudBox drawArea)
    {
        const int DividerHeight = 3;

        HudBox dividerArea = (0, drawArea.Bottom - DividerHeight, drawArea.Width, drawArea.Bottom);
        hud.FillBox(dividerArea, Color.Black);
    }

    private void RenderInput(IHudRenderContext hud, HudBox drawArea, out int inputHeight)
    {
        hud.Text(m_console.Input, FontName, FontSize, (4, drawArea.Bottom - 4), out Dimension inputArea,
            anchor: Align.BottomLeft, color: Color.Yellow);

        if (inputArea.Height == 0)
            inputArea.Height = FontSize;

        RenderInputCursorFlash(hud, inputArea);

        inputHeight = inputArea.Height;
    }

    private void RenderInputCursorFlash(IHudRenderContext hud, Dimension inputArea)
    {
        const int CaretWidth = 2;

        if (!IsCursorFlashTime)
            return;

        var drawArea = GetDrawArea(hud);
        int offsetX = m_console.Input == "" ? 4 : 6;
        Vec2I origin = (inputArea.Width + offsetX, drawArea.Bottom - 4 - inputArea.Height);
        Vec2I dimension = (CaretWidth, inputArea.Height);

        HudBox area = (origin, origin + dimension);
        hud.FillBox(area, Color.LawnGreen);
    }

    private void RenderMessages(IHudRenderContext hud, int inputHeight, HudBox drawArea)
    {
        const int InputToMessagePadding = 8;
        const int BetweenMessagePadding = 7;

        int bottomY = (drawArea.Bottom) - inputHeight - InputToMessagePadding;
        int offsetCount = 0;

        lock (m_console.Messages)
        {
            foreach (ConsoleMessage message in m_console.Messages)
            {
                if (bottomY <= 0)
                    break;

                if (offsetCount < m_messageRenderOffset)
                {
                    offsetCount++;
                    continue;
                }

                hud.Text(message.Message, FontName, FontSize, (4, bottomY), out Dimension textArea,
                    anchor: Align.BottomLeft, color: message.Color, maxWidth: hud.Width);

                bottomY -= textArea.Height + BetweenMessagePadding;
            }
        }
    }
}
