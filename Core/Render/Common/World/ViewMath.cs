using GlmSharp;
using Helion.Render.Common.Context;
using Helion.Util;

namespace Helion.Render.Common.World;

public static class ViewMath
{
    public static mat4 Mvp(WorldRenderContext renderInfo)
    {
        float w = renderInfo.Viewport.Width;
        float h = renderInfo.Viewport.Height * 0.825f;
        float fovY = (float)MathHelper.ToRadians(63.2);

        // TODO: Propagate an Entity in here.
        float zNear = 4.0f;
        // float zNear = (float)((renderInfo.ViewerEntity.LowestCeilingZ - renderInfo.ViewerEntity.HighestFloorZ - renderInfo.ViewerEntity.ViewZ) * 0.68);
        // zNear = MathHelper.Clamp(zNear, 0.5f, 7.9f);

        mat4 model = mat4.Identity;
        mat4 view = renderInfo.Camera.ViewMatrix(renderInfo.InterpolationFrac);

        mat4 projection = mat4.PerspectiveFov(fovY, w, h, zNear, 65536.0f);
        return projection * view * model;
    }
}

