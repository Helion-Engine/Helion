using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Textures;
using Helion.Resources;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class GLLegacyTextureManager : IGLTextureManager
    {
        public GLTextureHandle NullHandle { get; }
        public GLFontTexture NullFont { get; }
        private readonly IResources m_resources;
        private readonly List<AtlasGLTexture> m_textures = new() { new AtlasGLTexture() };
        private readonly List<GLTextureHandle> m_handles = new();
        private readonly List<GLFontTexture> m_fontHandles = new();
        private bool m_disposed;

        public GLLegacyTextureManager(IResources resources)
        {
            m_resources = resources;

            AddNullTexture();
            NullHandle = m_handles[0];

            AddNullFontTexture();
            NullFont = m_fontHandles[0];
        }

        ~GLLegacyTextureManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private void AddNullTexture()
        {
            Image nullImage = ImageHelper.CreateNullImage();
            AddImage(nullImage);
        }

        private GLTextureHandle? AddImage(Image image)
        {
            Dimension neededDim = image.Dimension;
            Dimension maxDim = m_textures[0].Dimension;
            if (neededDim.Width > maxDim.Width || neededDim.Height > maxDim.Height)
                return null;

            for (int i = 0; i < m_textures.Count; i++)
            {
                AtlasGLTexture texture = m_textures[i];
                if (texture.TryUpload(image, out Box2I box))
                    return CreateHandle(i, box, image, texture);
            }

            // Since we know it has to fit, but it didn't fit anywhere, then we
            // will make a new texture and use that, which must fit via precondition.
            AtlasGLTexture newTexture = new();
            m_textures.Add(newTexture);

            if (!newTexture.TryUpload(image, out Box2I newBox))
            {
                Fail("Should never fail to upload an image when we allocated enough space for it (GL atlas texture)");
                return null;
            }

            return CreateHandle(m_textures.Count - 1, newBox, image, newTexture);
        }

        private GLTextureHandle CreateHandle(int textureIndex, Box2I box, Image image, AtlasGLTexture glTexture)
        {
            int index = m_handles.Count;
            Vec2F uvFactor = glTexture.Dimension.Vector.Float;
            Vec2F min = box.Min.Float / uvFactor;
            Vec2F max = box.Max.Float / uvFactor;
            Box2F uvBox = new(min, max);

            GLTextureHandle handle = new(index, box, uvBox, image.Metadata.Offset, glTexture);
            m_handles.Add(handle);

            return handle;
        }

        private void AddNullFontTexture()
        {
            // TODO
        }

        public bool TryGet(string name, out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null)
        {
            // TODO: This is not correct, and is temporary.
            GLTextureHandle texture = Get(name, specificNamespace ?? ResourceNamespace.Global);
            handle = texture;
            return true;
        }

        public GLTextureHandle Get(string name, ResourceNamespace priority)
        {
            // TODO
            return NullHandle;
        }

        public GLTextureHandle Get(Texture texture)
        {
            // TODO
            return NullHandle;
        }

        public GLFontTexture GetFont(string name)
        {
            // TODO
            return NullFont;
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

            m_handles.Clear();

            foreach (var texture in m_fontHandles)
                texture.Dispose();
            m_fontHandles.Clear();

            foreach (GLTexture texture in m_textures)
                texture.Dispose();
            m_textures.Clear();

            m_disposed = true;
        }
    }
}
