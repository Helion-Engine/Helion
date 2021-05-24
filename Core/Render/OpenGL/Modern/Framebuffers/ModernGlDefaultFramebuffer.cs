using System;
using Helion.Geometry;
using Helion.Render.Common.Framebuffer;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Render.OpenGL.Textures;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Framebuffers
{
    public class ModernGlDefaultFramebuffer : ModernGLFramebuffer
    {
        public override Dimension Dimension => m_window.Dimension;
        public override GLTexture Texture { get; }
        private readonly IWindow m_window;
        private bool m_disposed;
        
        public ModernGlDefaultFramebuffer(IWindow window, ModernGLTextureManager textureManager) : 
            base(IFramebuffer.DefaultName, textureManager)
        {
            m_window = window;
            
            // Note: This isn't used, but is here to fulfill the interface.
            // Since it only happens once and is lightweight, this is okay.
            Texture = new GLTexture(TextureTarget.Texture2D);
            
            Texture.SetDebugLabel("Default framebuffer texture (do not use)");
        }
        
        ~ModernGlDefaultFramebuffer()
        { 
            FailedToDispose(this);
            PerformDispose();
        }
        
        public override void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, DefaultFramebufferName);  
        }
        
        public override void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, DefaultFramebufferName);  
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
            base.Dispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
        
            Texture.Dispose();
        
            m_disposed = true;
        }
    }
}
