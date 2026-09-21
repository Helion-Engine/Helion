using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
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
    public readonly DynamicArray<HudDrawBufferData> DrawBuffer = new(256);

    private readonly DynamicArray<HudDrawBufferData> m_freeDrawBuffers = new(256);

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
        for (int i = 0; i < DrawBuffer.Length; i++)
        {
            var data = DrawBuffer.Data[i];
            data.Vertices.Clear();
            m_freeDrawBuffers.Add(data);
        }
        DrawBuffer.Clear();
    }

    public HudDrawBufferData GetOrCreate(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        if (DrawBuffer.Empty())
            return AllocateNewAndAdd(texture, brightmapTexture);

        var front = DrawBuffer.Data[DrawBuffer.Length - 1];
        return front.Texture == texture ? front : AllocateNewAndAdd(texture, brightmapTexture);
    }

    private HudDrawBufferData AllocateNewAndAdd(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        HudDrawBufferData newData;

        if (m_freeDrawBuffers.Length > 0)
        {
            newData = m_freeDrawBuffers.RemoveLast();
            newData.Set(texture, brightmapTexture);
        }
        else
        {
            newData = new(texture, brightmapTexture);
        }

        DrawBuffer.Add(newData);
        return newData;
    }
}
