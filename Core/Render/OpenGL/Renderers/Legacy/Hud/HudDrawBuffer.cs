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
    public readonly List<HudDrawBufferData> DrawBuffer = new(256);

    private readonly DataCache m_dataCache;

    public HudDrawBuffer(DataCache dataCache)
    {
        m_dataCache = dataCache;
    }

    public void Add(GLLegacyTexture texture, HudQuad quad, GLLegacyTexture? brightmapTexture = null)
    {
        var hudDrawBuffer = GetOrCreate(texture, brightmapTexture);

        var length = hudDrawBuffer.Vertices.Length;
        hudDrawBuffer.Vertices.EnsureCapacity(length + 6);
        hudDrawBuffer.Vertices.Data[length] = quad.TopLeft;
        hudDrawBuffer.Vertices.Data[length + 1] = quad.BottomLeft;
        hudDrawBuffer.Vertices.Data[length + 2] = quad.TopRight;
        hudDrawBuffer.Vertices.Data[length + 3] = quad.TopRight;
        hudDrawBuffer.Vertices.Data[length + 4] = quad.BottomLeft;
        hudDrawBuffer.Vertices.Data[length + 5] = quad.BottomRight;
        hudDrawBuffer.Vertices.Length = length + 6;
    }

    public void Clear()
    {
        for (int i = 0; i < DrawBuffer.Count; i++)
            m_dataCache.FreeDrawHudBufferData(DrawBuffer[i]);
        DrawBuffer.Clear();
    }

    public HudDrawBufferData GetOrCreate(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        if (DrawBuffer.Empty())
            return AllocateNewAndAdd(texture, brightmapTexture);

        HudDrawBufferData front = DrawBuffer[^1];
        return front.Texture == texture ? front : AllocateNewAndAdd(texture, brightmapTexture);
    }

    private HudDrawBufferData AllocateNewAndAdd(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        HudDrawBufferData newData = m_dataCache.GetDrawHudBufferData(texture, brightmapTexture);
        DrawBuffer.Add(newData);
        return newData;
    }
}
