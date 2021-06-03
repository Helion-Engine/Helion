using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Framebuffers;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Render.OpenGL.Textures;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Framebuffers
{
    public class ModernGLTextureFramebuffer : ModernGLFramebuffer
    {
        public override Dimension Dimension { get; }
        public override GLTexture Texture { get; }
        private readonly ModernGLRenderer m_renderer;
        private readonly GLTexture m_depthStencilTexture;
        private readonly GLRenderbuffer m_renderbuffer;
        private int m_framebufferName;
        private bool m_disposed;
        
        public ModernGLTextureFramebuffer(string name, Dimension dimension, ModernGLTextureManager textureManager,
            ModernGLRenderer renderer) 
            : base(name, textureManager)
        {
            Dimension = dimension;
            Texture = new GLTexture(TextureTarget.Texture2D);
            m_renderer = renderer;
            m_depthStencilTexture = new GLTexture(TextureTarget.Texture2D);
            m_framebufferName = GL.GenFramebuffer();
            m_renderbuffer = new GLRenderbuffer(dimension);
            
            BindAnd(() =>
            {
                CreateAndAttachColorTexture();
                CreateAndAttachDepthStencilTexture();
                // SetRenderbuffer();  // TODO: Swap to this eventually.
                SetDebugLabel();
                ThrowIfNotComplete();
            });
            
            Texture.SetDebugLabel($"[{Name}] color");
            m_depthStencilTexture.SetDebugLabel($"[{Name}] depth, stencil");
            m_renderbuffer.SetDebugLabel(name);
        }

        ~ModernGLTextureFramebuffer()
        { 
            FailedToDispose(this);
            PerformDispose();
        }
        
        private void SetRenderbuffer()
        {
            m_renderbuffer.BindAnd(() =>
            {
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, 
                    RenderbufferTarget.Renderbuffer, m_renderbuffer.RenderbufferName);    
            });
        }
        
        private void SetDebugLabel()
        {
            string label = $"Framebuffer: {Name}";
            GLUtil.Label(label, ObjectLabelIdentifier.Framebuffer, m_framebufferName);
        }
        
        private void CreateAndAttachColorTexture()
        {
            Texture.Bind();
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Dimension.Width, 
                Dimension.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        
            int minLinear = (int)TextureMinFilter.Linear;
            int magLinear = (int)TextureMagFilter.Linear;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref minLinear);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref magLinear);
            
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2D, Texture.TextureName, 0);

            Texture.Unbind();
        }
        
        private void CreateAndAttachDepthStencilTexture()
        {
            m_depthStencilTexture.Bind();
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, Dimension.Width, 
                Dimension.Height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
        
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, 
                TextureTarget.Texture2D, m_depthStencilTexture.TextureName, 0); 
            
            Texture.SetDebugLabel($"Framebuffer Depth/Stencil: {Name}");
            
            m_depthStencilTexture.Unbind();
        }
        
        private static void ThrowIfNotComplete()
        {
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"Failed to make framebuffer (status code: {status})");
        }
        
        private void HandleAnyWindowDimensionChanges()
        {
            if (Dimension == m_renderer.Window.Dimension)
                return;
            
            throw new NotImplementedException("Need to implement window resizing changes");
        }

        public override void Bind()
        {
            HandleAnyWindowDimensionChanges();
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebufferName);  
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
            m_depthStencilTexture.Dispose();
            m_renderbuffer.Dispose();
            
            GL.DeleteFramebuffer(m_framebufferName);
            m_framebufferName = DefaultFramebufferName;
        
            m_disposed = true;
        }
    }
}
