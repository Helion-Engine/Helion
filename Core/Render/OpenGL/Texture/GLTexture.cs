using System;
using System.Numerics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture
{
    public abstract class GLTexture : IDisposable
    {
        public readonly int Id;
        public readonly Vector2 UVInverse;
        public readonly Dimension Dimension;
        public readonly TextureTargetType TextureType;
        protected readonly int TextureId;
        protected readonly IGLFunctions gl;

        protected GLTexture(int id, int textureId, Dimension dimension, IGLFunctions functions, 
            TextureTargetType textureType)
        {
            Id = id;
            TextureId = textureId;
            Dimension = dimension;
            UVInverse = Vector2.One / dimension.ToVector().ToFloat();
            gl = functions;
            TextureType = textureType;
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
            gl.DeleteTexture(TextureId);
        }
    }
}