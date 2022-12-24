using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderDataManager<TVertex> : IDisposable where TVertex : struct
{
    private readonly RenderDataCollection<TVertex> m_nonAlphaData;
    private readonly RenderDataCollection<TVertex> m_alphaData;
    private bool m_disposed;

    public RenderDataManager(RenderProgram program)
    {
        m_nonAlphaData = new(program);
        m_alphaData = new(program);
    }

    ~RenderDataManager()
    {
        Dispose(false);
    }

    public void Clear()
    {
        m_nonAlphaData.Clear();
        m_alphaData.Clear();
    }

    public RenderData<TVertex> GetNonAlpha(GLLegacyTexture texture)
    {
        return m_nonAlphaData.Get(texture);
    }
    
    public RenderData<TVertex> GetAlpha(GLLegacyTexture texture)
    {
        return m_alphaData.Get(texture);
    }

    public void RenderNonAlpha(PrimitiveType primitive)
    {
        m_nonAlphaData.Render(primitive);
    }
    
    public void RenderAlpha(PrimitiveType primitive)
    {
        m_alphaData.Render(primitive);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        m_nonAlphaData.Dispose();
        m_alphaData.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}