using System;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures
{
    public class GLTexture : IDisposable
    {
        public readonly TextureTarget Target;
        public int TextureName { get; private set; }
        private bool m_disposed;
        
        public GLTexture(TextureTarget target)
        {
            Target = target;
            TextureName = GL.GenTexture();
        }

        ~GLTexture()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetDebugLabel(string textureName)
        {
            GLUtil.Label($"Texture: {textureName}", ObjectLabelIdentifier.Texture, TextureName);
        }
        
        public void Bind()
        {
            GL.BindTexture(Target, TextureName);
        }
        
        public void Unbind()
        {
            GL.BindTexture(Target, 0);
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
            
            GL.DeleteTexture(TextureName);
            TextureName = 0;

            m_disposed = true;
        }

        public override int GetHashCode() => TextureName.GetHashCode();

        public override string ToString() => $"{TextureName} ({Target})";
    }
}
