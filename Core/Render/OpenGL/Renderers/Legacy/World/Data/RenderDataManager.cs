using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Util.Assertion;
using OpenTK.Graphics.OpenGL;
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
        RenderDataStyle.ColorAdd
    ];

    private readonly RenderDataCollection<TVertex>[] m_renderDataStyles;
    private readonly RenderData<TVertex> m_healthBarData;
    private bool m_disposed;

    public RenderDataManager(RenderProgram program, GLLegacyTexture healthBarTexture)
    {
        Assert.Precondition(RenderStyleLookup.Length == (int)RenderStyle.Count, "Render style lookup size mismatch");
        m_renderDataStyles = new RenderDataCollection<TVertex>[(int)RenderDataStyle.Count];
        for (int i = 0; i < m_renderDataStyles.Length; i++)
            m_renderDataStyles[i] = new(program);

        m_healthBarData = new(healthBarTexture, program);
    }

    ~RenderDataManager()
    {
        Dispose(false);
    }

    public bool HasDataToRenderByStyle(RenderStyle style) =>
        m_renderDataStyles[(int)RenderStyleLookup[(int)style]].HasDataToRender();

    public void Clear()
    {
        for (int i = 0; i < m_renderDataStyles.Length; i++)
            m_renderDataStyles[i].Clear();
        m_healthBarData.Clear();
    }

    public RenderData<TVertex> GetHealthBarData() => m_healthBarData;

    public void RenderHealthBars() =>
        m_healthBarData.Draw(PrimitiveType.Points);

    public RenderData<TVertex> GetByRenderStyle(RenderStyle style, GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null) =>
         m_renderDataStyles[(int)RenderStyleLookup[(int)style]].Get(texture, brightmapTexture);

    public void RenderByRenderStyle(RenderStyle style, PrimitiveType primitive) =>
        m_renderDataStyles[(int)RenderStyleLookup[(int)style]].Render(primitive);

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_renderDataStyles.Length; i++)
            m_renderDataStyles[i].Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}