namespace Helion.Layer.Endoom
{
    using Helion.Render.Common.Renderers;
    using Helion.Render.Common.Textures;
    using Helion.Render.OpenGL.Texture;
    using Helion.Resources.Archives.Collection;
    using Helion.Util;
    using Helion.Util.Extensions;
    using Helion.Util.Timing;
    using Helion.Window;
    using System;

    public class EndoomLayer : IGameLayer
    {
        // The ENDOOM format follows these specifications:
        // 1. It is 4000 bytes and represents an 80x25 text block
        // 2. The bytes alternate between "letter" bytes and "color" bytes
        // 3. Letter bytes are extended ASCII code page 437 and either need to be converted to Unicode or rendered with a VGA font
        // 4. Color bytes: bits 0-3 are foreground color, 4-6 are background color, 7 is "blink"
        const int ENDOOMBYTES = 4000;
        const int ENDOOMCOLUMNS = 80;
        const int ENDOOMROWS = ENDOOMBYTES / ENDOOMCOLUMNS / 2;
        const string FONTNAME = Constants.Fonts.VGA;
        const string LUMPNAME = "ENDOOM";
        private const string IMAGENAME1 = "ENDOOM_RENDERED_1";
        private const string IMAGENAME2 = "ENDOOM_RENDERED_2";

        private readonly Action m_closeAction;
        private readonly ArchiveCollection m_archiveCollection;


        private IRenderableTextureHandle? m_texture1;
        private IRenderableTextureHandle? m_texture2;
        private Action? m_texture1Remove;
        private Action? m_texture2Remove;

        private int m_pixelHeight;
        private bool m_disposed;

        private TextScreen? m_endoomScreen;
        private byte[]? m_fontBytes;

        public EndoomLayer(Action closeAction, ArchiveCollection archiveCollection, int height)
        {
            m_closeAction = closeAction;
            m_archiveCollection = archiveCollection;
            m_pixelHeight = height;

            byte[]? endoomData = m_archiveCollection.FindEntry(LUMPNAME)?.ReadData();
            if (endoomData != null)
            {
                m_endoomScreen = new TextScreen(endoomData, ENDOOMROWS, ENDOOMCOLUMNS);
                m_fontBytes = m_archiveCollection.FindEntry(FONTNAME)?.ReadData();
            }
        }

        public void HandleInput(IConsumableInput input)
        {
            if (input.HasAnyKeyPressed())
            {
                m_closeAction();
            }

            input.ConsumeAll();
        }

        public void RunLogic(TickerInfo tickerInfo)
        {
        }

        public virtual void Render(IHudRenderContext hud)
        {
            hud.Clear(Graphics.Color.Black);

            if (m_endoomScreen == null || m_fontBytes == null)
            {
                // If we don't have anything to render, just bail out
                m_closeAction();
                return;
            }

            bool blinkPhase = ((DateTime.Now.Millisecond / 500) == 0) && m_endoomScreen.HasBlink; // cycle 2x/second IF there is something to blink

            string textureName = blinkPhase ? IMAGENAME2 : IMAGENAME1;
            ref IRenderableTextureHandle? handle = ref (blinkPhase ? ref m_texture2 : ref m_texture1);
            ref Action? removeAction = ref (blinkPhase ? ref m_texture2Remove : ref m_texture2Remove);

            EnsureTexture(hud, textureName, blinkPhase, ref handle, ref removeAction);
            hud.RenderFullscreenImage(textureName, aspectRatioDivisor: 1f);
        }

        public void EnsureTexture(IHudRenderContext hud, string textureName, bool blink, ref IRenderableTextureHandle? handle, ref Action? removeAction)
        {
            if (handle != null)
            {
                return;
            }

            Graphics.Image image = m_endoomScreen!.GenerateImage(m_fontBytes!, m_pixelHeight, blink);
            handle = hud.CreateImage(image, textureName, Resources.ResourceNamespace.Textures, out m_texture1Remove);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    m_texture1Remove?.Invoke();
                    m_texture2Remove?.Invoke();
                    (m_texture1 as GLTexture)?.Dispose();
                    (m_texture2 as GLTexture)?.Dispose();
                }

                m_disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
