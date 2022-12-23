using System.Collections.Generic;
using System.Diagnostics;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Util.Atlas;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Textures;

public readonly record struct GLImageHandle(Box2I Area, Box2F UV, int Layer);

public class GLTexture2DArrayAtlas : GLTexture2DArray
{
    private readonly List<Atlas2D> m_layerAtlases = new();
    
    public GLTexture2DArrayAtlas(string label, Dimension dimension, int depth, TextureWrapMode wrapMode, float? anisotropy, bool clear) : 
        base(label, dimension, depth, wrapMode, anisotropy)
    {
        Debug.Assert(dimension is { Width: <= 4096, Height: <= 4096 }, "Need to re-evaluate why we're needing to upload so many pixels per layer, and not getting more layers");
        
        ResetAtlasHandles();
    }
    
    private void ResetAtlasHandles()
    {
        m_layerAtlases.Clear();
        
        for (int i = 0; i < Depth; i++)
            m_layerAtlases.Add(new(Dimension));
    }
    
    protected void Clear()
    {
        ResetAtlasHandles();

        Bind();
        ClearLayers();
        Unbind();
    }

    // Assumes the texture is bound.
    private void ClearLayers()
    {
        // We will assume clearing is extremely rare and only happens once, and if this ever
        // needs to happen, then we're okay with the minor (if any) hitch that happens when
        // doing this.
        byte[] zeroedData = new byte[Dimension.Area * sizeof(int)];
        
        for (int layer = 0; layer < m_layerAtlases.Count; layer++)
        {
            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, Dimension.Width, Dimension.Height, 
                1, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, zeroedData);    
        }
    }

    // Assumes the texture is bound.
    public GLImageHandle UploadImage(Image image)
    {
        Debug.Assert(image.Dimension.Area > 0, "Cannot upload an image with zero or negative area");

        foreach ((int layer, Atlas2D atlas) in m_layerAtlases.Enumerate())
        {
            AtlasHandle? handle = atlas.Add(image.Dimension);
            if (handle == null) 
                continue;
            
            (Vec2F min, Vec2F max) = handle.Location.Float;
            return new(handle.Location, new(min * UVInverse, max * UVInverse), layer);
        }
        
        throw new("TODO: Atlas overflow (need to resize the texture, copy, etc)");
    }
}