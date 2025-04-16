using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderDataManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    private readonly RenderDataCollection<TVertex> m_nonAlphaData;
    private readonly RenderDataCollection<TVertex> m_alphaData;
    private readonly RenderDataCollection<TVertex> m_fuzzData;
    private readonly RenderData<TVertex> m_healthBarData;
    private bool m_disposed;

    public RenderDataManager(RenderProgram program, GLLegacyTexture healthBarTexture)
    {
        m_nonAlphaData = new(program);
        m_alphaData = new(program);
        m_fuzzData = new(program);
        m_healthBarData = new(healthBarTexture, program);
    }

    ~RenderDataManager()
    {
        Dispose(false);
    }

    public bool HasFuzz() => m_fuzzData.HasDataToRender();
    public bool HasAlpha() => m_alphaData.HasDataToRender();

    public void Clear()
    {
        m_nonAlphaData.Clear();
        m_alphaData.Clear();
        m_fuzzData.Clear();
        m_healthBarData.Clear();
    }

    public RenderData<TVertex> GetHealthBarData() => m_healthBarData;

    public void RenderHealthBars()
    {
        m_healthBarData.Draw(PrimitiveType.Points);
    }

    public RenderData<TVertex> GetNonAlpha(GLLegacyTexture texture)
    {
        return m_nonAlphaData.Get(texture);
    }
    
    public RenderData<TVertex> GetAlpha(GLLegacyTexture texture)
    {
        return m_alphaData.Get(texture);
    }

    public RenderData<TVertex> GetFuzz(GLLegacyTexture texture)
    {
        return m_fuzzData.Get(texture);
    }

    public void RenderNonAlpha(PrimitiveType primitive)
    {
        m_nonAlphaData.Render(primitive);
    }
    
    public void RenderAlpha(PrimitiveType primitive)
    {
        m_alphaData.Render(primitive);
    }

    public void RenderFuzz(PrimitiveType primitive)
    {
        m_fuzzData.Render(primitive);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        m_nonAlphaData.Dispose();
        m_alphaData.Dispose();
        m_fuzzData.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}