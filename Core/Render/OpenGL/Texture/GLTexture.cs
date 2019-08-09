using System;
using System.Numerics;
using Helion.Render.OpenGL.Context;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture
{
    public abstract class GLTexture : IDisposable
    {
        public readonly int Id;
        public readonly Vector2 UVInverse;
        public readonly Dimension Dimension;
        protected readonly IGLFunctions gl;
        private readonly int m_textureId;

        protected GLTexture(int id, int textureId, Dimension dimension, IGLFunctions functions)
        {
            Id = id;
            m_textureId = textureId;
            Dimension = dimension;
            UVInverse = Vector2.One / dimension.ToVector().ToFloat();
            gl = functions;
        }

        ~GLTexture()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        protected virtual void ReleaseUnmanagedResources()
        {
            gl.DeleteTexture(m_textureId);
        }
    }
}