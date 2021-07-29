using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Texture
{
    public abstract class GLTexture : IRenderableTextureHandle, IDisposable
    {
        public int Index => TextureId;
        public Box2I Area => (Vec2I.Zero, Dimension.Vector);
        public Box2F UV => (Vec2F.Zero, Vec2F.One);
        public Dimension Dimension { get; }
        public Vec2I Offset => Metadata.Offset;
        public readonly int TextureId;
        public readonly string Name;
        public readonly Vec2F UVInverse;
        public readonly TextureTargetType TextureType;
        public readonly ImageMetadata Metadata;
        protected readonly IGLFunctions gl;

        public int Width => Dimension.Width;
        public int Height => Dimension.Height;

        protected GLTexture(int textureId, string name, Dimension dimension, ImageMetadata metadata, IGLFunctions functions, 
            TextureTargetType textureType)
        {
            TextureId = textureId;
            Name = name;
            Dimension = dimension;
            Metadata = metadata;
            UVInverse = Vec2F.One / dimension.Vector.Float;
            gl = functions;
            TextureType = textureType;
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