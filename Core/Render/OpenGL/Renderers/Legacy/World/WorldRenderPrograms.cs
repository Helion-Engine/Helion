using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public sealed class WorldRenderPrograms
{
    internal RenderProgramHandle<InterpolationShader> InterpolationProgram = new(new InterpolationShader("Main"));
    internal RenderProgramHandle<InterpolationTransparentShader> InterpolationTransparentProgram = new(new InterpolationTransparentShader());
    internal RenderProgramHandle<InterpolationCompositeShader> InterpolationCompositeProgram = new(new InterpolationCompositeShader());
    internal RenderProgramHandle<InterpolationPlaneClipAlphaShader> InterpolationPlaneClipAlphaProgram = new(new InterpolationPlaneClipAlphaShader());
    internal RenderProgramHandle<InterpolationPlaneClipShaderMrt> InterpolationPlaneClipMrtProgram = new(new InterpolationPlaneClipShaderMrt());
    internal RenderProgramHandle<InterpolationWallClipShader> InterpolationWallClipShader = new(new InterpolationWallClipShader());
    internal RenderProgramHandle<InterpolationWallClipAlphaShader> InterpolationWallClipAlphaProgram = new(new InterpolationWallClipAlphaShader());
    internal RenderProgramHandle<StaticShader> StaticProgram = new(new StaticShader("Main"));
    internal RenderProgramHandle<StaticPlaneClipShader> StaticPlaneClipProgram = new(new StaticPlaneClipShader());
    internal RenderProgramHandle<StaticPlaneClipAlphaShader> StaticPlaneClipAlphaProgram = new(new StaticPlaneClipAlphaShader());
    internal RenderProgramHandle<StaticPlaneClipShaderMrt> StaticPlaneClipMrtProgram = new(new StaticPlaneClipShaderMrt());
    internal RenderProgramHandle<StaticWallClipShader> StaticWallClipProgram = new(new StaticWallClipShader());
    internal RenderProgramHandle<StaticWallClipAlphaShader> StaticWallClipAlphaProgram = new(new StaticWallClipAlphaShader());
    internal RenderProgramHandle<StaticTransparentShader> StaticTransparentProgram = new(new StaticTransparentShader());
    internal RenderProgramHandle<StaticCompositeShader> StaticCompositeProgram = new(new StaticCompositeShader());

    internal RenderProgramHandle<FloodFillProgram> FloodFillProgram = new(new FloodFillProgram("Main"));
    internal RenderProgramHandle<FloodFillWallClipProgram> FloodFillWallClipProgram = new(new FloodFillWallClipProgram());

    internal RenderProgramHandle<EntityProgram> EntityProgram = new(new EntityProgram("Main"));
    internal RenderProgramHandle<EntityHealthBarProgram> EntityHealthBarProgram = new(new EntityHealthBarProgram());
    internal RenderProgramHandle<EntityTransparentProgram> EntityTransparentProgram = new(new EntityTransparentProgram());
    internal RenderProgramHandle<EntityCompositeProgram> EntityCompositeProgram = new(new EntityCompositeProgram());
    internal RenderProgramHandle<EntityFuzzRefractionProgram> EntityFuzzRefractionProgram = new(new EntityFuzzRefractionProgram());

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
        InterpolationProgram.Recreate();
        InterpolationTransparentProgram.Recreate();
        InterpolationCompositeProgram.Recreate();
        InterpolationPlaneClipAlphaProgram.Recreate();
        InterpolationPlaneClipMrtProgram.Recreate();
        InterpolationWallClipShader.Recreate();
        InterpolationWallClipAlphaProgram.Recreate();

        StaticProgram.Recreate();
        StaticPlaneClipProgram.Recreate();
        StaticPlaneClipAlphaProgram.Recreate();
        StaticPlaneClipMrtProgram.Recreate();
        StaticWallClipProgram.Recreate();
        StaticWallClipAlphaProgram.Recreate();
        StaticTransparentProgram.Recreate();
        StaticCompositeProgram.Recreate();

        EntityProgram.Recreate();
        EntityHealthBarProgram.Recreate();
        EntityTransparentProgram.Recreate();
        EntityCompositeProgram.Recreate();
        EntityFuzzRefractionProgram.Recreate();

        FloodFillProgram.Recreate();
        FloodFillWallClipProgram.Recreate();
    }
}
