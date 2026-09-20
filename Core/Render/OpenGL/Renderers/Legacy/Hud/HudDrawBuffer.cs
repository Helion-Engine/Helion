using System.Collections.Generic;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;

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
    public readonly DynamicArray<HudDrawBufferData> DrawBuffer = new(256);

    private readonly Dictionary<int, HudDrawBufferData> m_bufferLookup = [];
    private int m_renderCount = 1;

    public void Add(GLLegacyTexture texture, in HudQuad quad, GLLegacyTexture? brightmapTexture = null)
    {
        texture = texture.ParentArrayTexture ?? texture;
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
        m_renderCount++;
        for (int i = 0; i < DrawBuffer.Length; i++)
            DrawBuffer.Data[i].Vertices.Clear();
        DrawBuffer.Clear();
    }

    public HudDrawBufferData GetOrCreate(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        if (m_bufferLookup.TryGetValue(texture.Index, out var buffer))
        {
            if (buffer.RenderCount != m_renderCount)
            {
                buffer.RenderCount = m_renderCount;
                DrawBuffer.Add(buffer);
            }
            return buffer;
        }

        buffer = AllocateNewAndAdd(texture, brightmapTexture);
        m_bufferLookup[texture.Index] = buffer;
        return buffer;
    }

    private HudDrawBufferData AllocateNewAndAdd(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        var newData = new HudDrawBufferData(texture, brightmapTexture);
        DrawBuffer.Add(newData);
        return newData;
    }
}
