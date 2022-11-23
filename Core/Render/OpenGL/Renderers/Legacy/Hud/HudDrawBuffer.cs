using System.Collections.Generic;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util;
using Helion.Util.Extensions;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

/// <summary>
/// Queues all the hud draw commands and merges ones that use the same
/// texture to reduce binding overhead.
/// </summary>
/// <remarks>
/// Due to how the legacy renderer works, we would much rather bind a
/// texture once and do multiple draw calls with it. Unfortunately since
/// the implementation requires a painters algorithm approach, we cannot
/// do any reordering (without a lot of computation) for non-overlapping
/// images. This right now is the best trade-off for the least amount of
/// work.
/// </remarks>
public class HudDrawBuffer
{
    public readonly List<HudDrawBufferData> DrawBuffer = new();

    private readonly DataCache m_dataCache;

    public HudDrawBuffer(DataCache dataCache)
    {
        m_dataCache = dataCache;
    }

    public void Add(GLLegacyTexture texture, HudQuad quad)
    {
        HudDrawBufferData data = GetOrCreate(texture);

        // TODO: Can we add the two triangles in one go?
        data.Vertices.Add(quad.TopLeft);
        data.Vertices.Add(quad.BottomLeft);
        data.Vertices.Add(quad.TopRight);
        data.Vertices.Add(quad.TopRight);
        data.Vertices.Add(quad.BottomLeft);
        data.Vertices.Add(quad.BottomRight);
    }

    public void Clear()
    {
        foreach (var data in DrawBuffer)
            m_dataCache.FreeDrawHudBufferData(data);
        DrawBuffer.Clear();
    }

    private HudDrawBufferData GetOrCreate(GLLegacyTexture texture)
    {
        if (DrawBuffer.Empty())
            return AllocateNewAndAdd(texture);

        HudDrawBufferData front = DrawBuffer[^1];
        return front.Texture == texture ? front : AllocateNewAndAdd(texture);
    }

    private HudDrawBufferData AllocateNewAndAdd(GLLegacyTexture texture)
    {
        HudDrawBufferData newData = m_dataCache.GetDrawHudBufferData(texture);
        DrawBuffer.Add(newData);
        return newData;
    }
}
