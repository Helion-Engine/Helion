using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics.New;
using Helion.Graphics.New.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class GLLegacyTextureManager : IGLTextureManager
    {
        public GLTextureHandle NullHandle { get; }
        public GLFontTexture NullFont { get; }
        private readonly IResources m_resources;
        private readonly List<AtlasGLTexture> m_textures = new();
        private readonly List<GLTextureHandle> m_handles = new();
        private readonly ResourceTracker<GLTextureHandle> m_handlesTable = new();
        private readonly Dictionary<string, GLFontTexture> m_fontTextures = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<GLFontTexture> m_fontHandles = new();
        private bool m_disposed;

        public GLLegacyTextureManager(IResources resources)
        {
            m_resources = resources;

            NullHandle = AddNullTexture();
            NullFont = AddNullFontTexture();
        }

        ~GLLegacyTextureManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private GLTextureHandle AddNullTexture()
        {
            Image nullImage = Image.NullImage();
            GLTextureHandle? handle = AddImage("NULL", nullImage, Mipmap.Generate, Binding.Bind);
            return handle ?? throw new Exception("Should never fail to allocate the null texture");
        }

        private GLTextureHandle? AddImage(string name, Image image, Mipmap mipmap, Binding bind)
        {
            Dimension neededDim = image.Dimension;
            Dimension maxDim = m_textures[0].Dimension;
            if (neededDim.Width > maxDim.Width || neededDim.Height > maxDim.Height)
                return null;

            for (int i = 0; i < m_textures.Count; i++)
            {
                AtlasGLTexture texture = m_textures[i];
                if (texture.TryUpload(image, out Box2I box, mipmap, bind))
                    return CreateHandle(name, i, box, image, texture);
            }

            // Since we know it has to fit, but it didn't fit anywhere, then we
            // will make a new texture and use that, which must fit via precondition.
            AtlasGLTexture newTexture = new($"Atlas layer {m_textures.Count}");
            m_textures.Add(newTexture);

            if (!newTexture.TryUpload(image, out Box2I newBox, mipmap, bind))
            {
                Fail("Should never fail to upload an image when we allocated enough space for it (GL atlas texture)");
                return null;
            }

            return CreateHandle(name, m_textures.Count - 1, newBox, image, newTexture);
        }

        private GLTextureHandle CreateHandle(string name, int layerIndex, Box2I box, Image image, AtlasGLTexture glTexture)
        {
            int index = m_handles.Count;
            Vec2F uvFactor = glTexture.Dimension.Vector.Float;
            Vec2F min = box.Min.Float / uvFactor;
            Vec2F max = box.Max.Float / uvFactor;
            Box2F uvBox = new(min, max);

            GLTextureHandle handle = new(index, layerIndex, box, uvBox, image.Offset, glTexture);
            m_handles.Add(handle);
            m_handlesTable.Insert(name, image.Namespace, handle);

            return handle;
        }

        private GLFontTexture AddNullFontTexture()
        {
            Image nullImage = Image.NullImage();
            Glyph glyph = new Glyph('?', Box2F.UnitBox, new Box2I((0, 0), nullImage.Dimension.Vector));
            Dictionary<char, Glyph> glyphs = new() { ['?'] = glyph };
            Font font = new("Null font", glyphs, nullImage);

            GLTexture texture = new("Null font", TextureTarget.Texture2D);
            // TODO: Upload data
            //throw new NotImplementedException();

            GLFontTexture fontTexture = new(texture, font);
            m_fontTextures["NULL"] = fontTexture;

            return fontTexture;
        }

        public bool TryGet(string name, out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null)
        {
            GLTextureHandle texture = Get(name, specificNamespace ?? ResourceNamespace.Global);
            handle = texture;
            return ReferenceEquals(texture, NullHandle);
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
            if (m_fontTextures.TryGetValue(name, out GLFontTexture? fontTexture))
                return fontTexture;

            // TODO: Try to create it from m_resources.

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
            m_handlesTable.Clear();

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
