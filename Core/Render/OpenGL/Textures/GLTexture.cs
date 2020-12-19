using System;
using System.Numerics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Resource.Textures;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures
{
    public abstract class GLTexture : IDisposable
    {
        /// <summary>
        /// The OpenGL texture 'name' handle.
        /// </summary>
        public readonly int TextureId;

        /// <summary>
        /// A readable name for this texture.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// A precalculated inverse of the UV coordinates.
        /// </summary>
        public readonly Vector2 UVInverse;

        /// <summary>
        /// The dimension of this texture.
        /// </summary>
        public readonly Dimension Dimension;

        /// <summary>
        /// What type of texture it is with respect to OpenGL.
        /// </summary>
        public readonly TextureTargetType TextureType;

        /// <summary>
        /// The backing texture for this GL texture.
        /// </summary>
        public readonly Texture Texture;

        protected readonly IGLFunctions gl;

        public int Width => Dimension.Width;
        public int Height => Dimension.Height;

        protected GLTexture(int textureId, string name, Dimension dimension, IGLFunctions functions,
            TextureTargetType textureType, Texture texture)
        {
            TextureId = textureId;
            Name = name;
            Dimension = dimension;
            UVInverse = Vector2.One / dimension.ToVector().ToFloat();
            gl = functions;
            TextureType = textureType;
            Texture = texture;
        }

        ~GLTexture()
        {
            FailedToDispose(this);
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