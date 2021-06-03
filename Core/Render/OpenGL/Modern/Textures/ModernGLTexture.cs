using System;
using Helion.Graphics;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Textures
{
    public class ModernGLTexture : IDisposable
    {
        public const ulong NoHandle = 0;
        
        public readonly string Name;
        public readonly Image Image;
        public ulong Handle { get; set; } = NoHandle;
        public bool IsResident { get; private set; }
        private readonly TextureTarget m_target;
        private int m_textureId;
        private bool m_disposed;

        public ModernGLTexture(string name, Image image, TextureTarget target)
        {
            Name = name;
            Image = image;
            m_target = target;
            m_textureId = GL.GenTexture();
        }

        ~ModernGLTexture()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetDebugLabel(string textureName)
        {
            GLUtil.Label($"Texture: {textureName}", ObjectLabelIdentifier.Texture, m_textureId);
        }

        public void InitializeBindlessHandle(bool makeResident)
        {
            Precondition(Handle == NoHandle, "Trying to recreate a bindless texture handle");
            
            Handle = (ulong)GL.Arb.GetTextureHandle(m_textureId);
            
            if (makeResident)
                SetResidency(true);
        }

        public bool SetResidency(bool resident)
        {
            if (Handle == NoHandle)
                return false;
            
            switch (resident)
            {
            case true when !IsResident:
                GL.Arb.MakeTextureHandleResident(Handle);
                IsResident = true;
                break;
            case false when IsResident:
                GL.Arb.MakeTextureHandleNonResident(Handle);
                IsResident = false;
                break;
            }

            return true;
        }

        private void Bind()
        {
            GL.BindTexture(m_target, m_textureId);
        }

        private void Unbind()
        {
            GL.BindTexture(m_target, 0);
        }

        public void BindAnd(Action action)
        {
            Bind();
            action();
            Unbind();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            SetResidency(false);
            GL.DeleteTexture(m_textureId);

            m_textureId = 0;
            Handle = NoHandle;

            m_disposed = true;
        }
    }
}
