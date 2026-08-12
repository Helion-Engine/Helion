using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using Helion.Util.Loggers;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataPool
{
    private readonly RenderProgram m_program;
    private readonly DynamicArray<RenderWorldData> m_renderData;

    public int PoolSize { get; set; }
    public int UseCount { get; set; }

    public RenderWorldDataPool(RenderProgram program, int poolSize)
    {
        PoolSize = poolSize;
        m_program = program;
        m_renderData = new(poolSize);
        RefillPool(PoolSize);
    }

    public void RefillPool(int size)
    {
        size = Math.Min(size, PoolSize);
        for (int i = m_renderData.Length; i < size; i++)
            m_renderData.AddUnsafe(new RenderWorldData(m_program));
    }

    public RenderWorldData Get(GLLegacyTexture texture, GLLegacyTexture? brightMapTexture = null)
    {
        if (m_renderData.Length > 0)
        {
            var data = m_renderData.RemoveLast();
            data.Set(texture, brightMapTexture);
            UseCount++;
            return data;
        }

        UseCount++;
        LogExhaustion();
        return new RenderWorldData(texture, m_program, brightMapTexture);
    }

    [Conditional("DEBUG")]
    public void LogExhaustion()
    {
        HelionLog.Info($"RenderWorldDataPool exhausted. Consider increasing the size. {UseCount}");
    }
}

