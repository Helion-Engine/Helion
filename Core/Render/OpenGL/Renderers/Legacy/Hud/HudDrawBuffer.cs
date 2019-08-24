using System.Collections.Generic;
using Helion.Render.OpenGL.Texture;
using Helion.Util.Extensions;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
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
        public readonly List<HudDrawBufferData> DrawBuffer = new List<HudDrawBufferData>();

        public void Add(GLTexture texture, HudQuad quad)
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

        private HudDrawBufferData GetOrCreate(GLTexture texture)
        {
            if (DrawBuffer.Empty())
                return AllocateNewAndAdd(texture);

            // TODO: Use `^1` in NET Core 3.0 instead of `len - 1`.
            HudDrawBufferData front = DrawBuffer[DrawBuffer.Count - 1];
            return front.Texture == texture ? front : AllocateNewAndAdd(texture);
        }
        
        private HudDrawBufferData AllocateNewAndAdd(GLTexture texture)
        {
            HudDrawBufferData newData = new HudDrawBufferData(texture);
            DrawBuffer.Add(newData);
            return newData;
        }
    }
}