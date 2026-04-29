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

    public int PoolSize { get; set; }
    public int UseCount { get; set; }

    public RenderDataPool(RenderProgram program, int poolSize)
    {
        PoolSize = poolSize;
        m_program = program;
        m_entityRenderData = new(poolSize);
        RefillPool(PoolSize);
    }

    public void RefillPool(int size)
    {
        size = Math.Min(size, PoolSize);
        for (int i = m_entityRenderData.Length; i < size; i++)
            m_entityRenderData.AddUnsafe(new RenderData<TVertex>(m_program));
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
        return new RenderData<TVertex>(m_program, texture, brightMapTexture);
    }

    [Conditional("DEBUG")]
    public void LogExhaustion()
    {
        HelionLog.Info($"RenderDataPool exhausted. Consider increasing the size. {UseCount}");
    }
}
