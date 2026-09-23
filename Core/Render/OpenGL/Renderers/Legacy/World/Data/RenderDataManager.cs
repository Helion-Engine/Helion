using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Util.Assertion;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderDataManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    private static readonly RenderDataStyle[] RenderStyleLookup =
    [
        RenderDataStyle.Normal,
        RenderDataStyle.Normal,
        RenderDataStyle.Fuzzy,
        RenderDataStyle.Translucent,
        RenderDataStyle.Add,
        RenderDataStyle.ColorAdd,
        RenderDataStyle.ColorAdd,
        RenderDataStyle.ColorAdd
    ];

    private readonly RenderDataCollection<TVertex>[][] m_renderData;
    private readonly RenderData<TVertex> m_healthBarData;
    private bool m_disposed;

    public RenderDataManager(RenderProgram program, GLLegacyTexture healthBarTexture, RenderDataPool<TVertex> renderDataPoolArray, RenderDataPool<TVertex> renderDataPoolOverflow, Action? onDraw = null)
    {
        Assert.Precondition(RenderStyleLookup.Length == (int)RenderStyle.Count, "Render style lookup size mismatch");

        m_renderData = new RenderDataCollection<TVertex>[2][];

        // The overflow pool will use dynamic lookups that will be released on clear.
        // The pool used for texture arrays will be locked to their texture id since they are the small number of render buckets.
        m_renderData[0] = CreateRenderDataStyleArray(RenderDataCollectionMode.Recycle, renderDataPoolOverflow, onDraw);
        m_renderData[1] = CreateRenderDataStyleArray(RenderDataCollectionMode.Pinned, renderDataPoolArray, onDraw);

        m_healthBarData = new(program, 8192, healthBarTexture);
    }

    private static RenderDataCollection<TVertex>[] CreateRenderDataStyleArray(RenderDataCollectionMode mode, RenderDataPool<TVertex> renderDataPool, Action? onDraw)
    {
        var renderDataStylesArray = new RenderDataCollection<TVertex>[(int)RenderDataStyle.Count];
        for (int i = 0; i < renderDataStylesArray.Length; i++)
            renderDataStylesArray[i] = new(renderDataPool, mode, onDraw);
        return renderDataStylesArray;
    }

    ~RenderDataManager()
    {
        Dispose(false);
    }

    public bool HasDataToRenderByStyle(RenderDataStyle style) =>
        m_renderData[0][(int)style].HasDataToRender() || m_renderData[1][(int)style].HasDataToRender();

    public void Clear()
    {
        for (int i = 0; i < m_renderData.Length; i++)
        {
            var array = m_renderData[i];
            for (int j = 0; j < array.Length; j++)
                array[j].Clear();
        }
        m_healthBarData.Clear();
    }

    public RenderData<TVertex> GetHealthBarData() => m_healthBarData;

    public void RenderHealthBars() =>
        m_healthBarData.Draw();

    public unsafe RenderData<TVertex> GetByRenderStyle(RenderStyle style, GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        var isParentArray = texture.IsParentArray;
        int index = *(int*)&isParentArray;
        var array = m_renderData[index];
        return array[(int)RenderStyleLookup[(int)style]].Get(texture, brightmapTexture);
    }

    public void RenderByRenderStyle(RenderDataStyle style)
    {
        m_renderData[0][(int)style].Render();
        m_renderData[1][(int)style].Render();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_renderData.Length; i++)
        {
            var array = m_renderData[i];
            for (int j = 0; j < array.Length; j++)
                array[j].Dispose();
        }

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}