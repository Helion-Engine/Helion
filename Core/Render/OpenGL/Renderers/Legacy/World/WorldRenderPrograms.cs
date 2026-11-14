using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public sealed class WorldRenderPrograms
{
    internal InterpolationShader InterpolationProgram = new("Main");
    internal InterpolationTransparentShader InterpolationTransparentProgram = new();
    internal InterpolationCompositeShader InterpolationCompositeProgram = new();
    internal InterpolationPlaneClipAlphaShader InterpolationPlaneClipAlphaProgram = new();
    internal InterpolationPlaneClipShaderMrt InterpolationPlaneClipMrtProgram = new();
    internal InterpolationWallClipShader InterpolationWallClipShader = new();
    internal InterpolationWallClipAlphaShader InterpolationWallClipAlphaProgram = new();
    internal StaticShader StaticProgram = new("Main");
    internal StaticPlaneClipShader StaticPlaneClipProgram = new();
    internal StaticPlaneClipAlphaShader StaticPlaneClipAlphaProgram = new();
    internal StaticPlaneClipShaderMrt StaticPlaneClipMrtProgram = new();
    internal StaticWallClipShader StaticWallClipProgram = new();
    internal StaticWallClipAlphaShader StaticWallClipAlphaProgram = new();
    internal StaticTransparentShader StaticTransparentProgram = new();
    internal StaticCompositeShader StaticCompositeProgram = new();

    internal FloodFillProgram FloodFillProgram = new("Main");
    internal FloodFillWallClipProgram FloodFillWallClipProgram = new();

    internal EntityProgram EntityProgram = new("Main");
    internal EntityHealthBarProgram EntityHealthBarProgram = new();
    internal EntityTransparentProgram EntityTransparentProgram = new();
    internal EntityCompositeProgram EntityCompositeProgram = new();
    internal EntityFuzzRefractionProgram EntityFuzzRefractionProgram = new();

    public WorldRenderPrograms()
    {

    }

    public void Dispose()
    {
        InterpolationProgram.Dispose();
        InterpolationTransparentProgram.Dispose();
        InterpolationCompositeProgram.Dispose();
        InterpolationPlaneClipAlphaProgram.Dispose();
        InterpolationPlaneClipMrtProgram.Dispose();
        InterpolationWallClipShader.Dispose();
        InterpolationWallClipAlphaProgram.Dispose();
        StaticProgram.Dispose();
        StaticPlaneClipProgram.Dispose();
        StaticPlaneClipAlphaProgram.Dispose();
        StaticPlaneClipMrtProgram.Dispose();
        StaticWallClipProgram.Dispose();
        StaticWallClipAlphaProgram.Dispose();
        StaticTransparentProgram.Dispose();
        StaticCompositeProgram.Dispose();
        EntityProgram.Dispose();
        EntityHealthBarProgram.Dispose();
        EntityTransparentProgram.Dispose();
        EntityCompositeProgram.Dispose();
        EntityFuzzRefractionProgram.Dispose();
        FloodFillProgram.Dispose();
        FloodFillWallClipProgram.Dispose();
    }

    public void Recreate()
    {
        InterpolationProgram = (InterpolationShader)InterpolationProgram.Recreate();
        InterpolationTransparentProgram = (InterpolationTransparentShader)InterpolationTransparentProgram.Recreate();
        InterpolationCompositeProgram = (InterpolationCompositeShader)InterpolationCompositeProgram.Recreate();
        InterpolationPlaneClipAlphaProgram = (InterpolationPlaneClipAlphaShader)InterpolationPlaneClipAlphaProgram.Recreate();
        InterpolationPlaneClipMrtProgram = (InterpolationPlaneClipShaderMrt)InterpolationPlaneClipMrtProgram.Recreate();
        InterpolationWallClipShader = (InterpolationWallClipShader)InterpolationWallClipShader.Recreate();
        InterpolationWallClipAlphaProgram = (InterpolationWallClipAlphaShader)InterpolationWallClipAlphaProgram.Recreate();

        StaticProgram = (StaticShader)StaticProgram.Recreate();
        StaticPlaneClipProgram = (StaticPlaneClipShader)StaticPlaneClipProgram.Recreate();
        StaticPlaneClipAlphaProgram = (StaticPlaneClipAlphaShader)StaticPlaneClipAlphaProgram.Recreate();
        StaticPlaneClipMrtProgram = (StaticPlaneClipShaderMrt)StaticPlaneClipMrtProgram.Recreate();
        StaticWallClipProgram = (StaticWallClipShader)StaticWallClipProgram.Recreate();
        StaticWallClipAlphaProgram = (StaticWallClipAlphaShader)StaticWallClipAlphaProgram.Recreate();
        StaticTransparentProgram = (StaticTransparentShader)StaticTransparentProgram.Recreate();
        StaticCompositeProgram = (StaticCompositeShader)StaticCompositeProgram.Recreate();

        EntityProgram = (EntityProgram)EntityProgram.Recreate();
        EntityHealthBarProgram = (EntityHealthBarProgram)EntityHealthBarProgram.Recreate();
        EntityTransparentProgram = (EntityTransparentProgram)EntityTransparentProgram.Recreate();
        EntityCompositeProgram = (EntityCompositeProgram)EntityCompositeProgram.Recreate();
        EntityFuzzRefractionProgram = (EntityFuzzRefractionProgram)EntityFuzzRefractionProgram.Recreate();

        FloodFillProgram = (FloodFillProgram)FloodFillProgram.Recreate();
        FloodFillWallClipProgram = (FloodFillWallClipProgram)FloodFillWallClipProgram.Recreate();
    }
}
