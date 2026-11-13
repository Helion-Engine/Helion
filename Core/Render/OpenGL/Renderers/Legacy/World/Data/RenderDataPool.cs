using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using Helion.Util.Loggers;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderDataPool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> where TVertex : struct
{
    private readonly RenderProgram m_program;
    private readonly DynamicArray<RenderData<TVertex>> m_entityRenderData;
    private bool m_disposed;

    public int PoolSize { get; }
    public int VboCapacity { get; }
    public int UseCount { get; private set; }

    public RenderDataPool(RenderProgram program, int poolSize, int vboCapacity)
    {
        PoolSize = poolSize;
        VboCapacity = vboCapacity;
        m_program = program;
        m_entityRenderData = new(poolSize);
        RefillPool(PoolSize);
    }

    public void RefillPool(int size)
    {
        size = Math.Min(size, PoolSize);
        for (int i = m_entityRenderData.Length; i < size; i++)
            m_entityRenderData.AddUnsafe(new RenderData<TVertex>(m_program, VboCapacity));
    }

    public void Return(RenderData<TVertex> data)
    {
        data.Texture = null!;
        data.BrightMapTexture = null!;
        m_entityRenderData.Add(data);
        UseCount--;
    }

    public RenderData<TVertex> Get(GLLegacyTexture texture, GLLegacyTexture? brightMapTexture = null)
    {
        if (m_entityRenderData.Length > 0)
        {
            var data = m_entityRenderData.RemoveLast();
            data.Set(texture, brightMapTexture);
            UseCount++;
            return data;
        }

        UseCount++;
        LogExhaustion();
        return new RenderData<TVertex>(m_program, VboCapacity, texture, brightMapTexture);
    }

    [Conditional("DEBUG")]
    public void LogExhaustion()
    {
        HelionLog.Debug($"RenderDataPool exhausted. Consider increasing the size. {UseCount}");
    }

    public void Dispose()
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_entityRenderData.Length; i++)
            m_entityRenderData[i].Dispose();

        m_disposed = true;
    }
}
