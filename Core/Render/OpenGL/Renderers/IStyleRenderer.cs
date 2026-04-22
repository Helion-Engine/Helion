
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;

namespace Helion.Render.OpenGL.Renderers;

public interface IStyleRenderer
{
    void Render(RenderDataStyle style);
    bool HasStyleToRender(RenderDataStyle style);
    bool HasAlphaToRender();
    void RenderAllAlpha();
}

public abstract class StyleRendererBase : IStyleRenderer
{
    private static readonly RenderDataStyle[] AlphaStyles = [RenderDataStyle.Translucent, RenderDataStyle.Add, RenderDataStyle.ColorAdd];

    public abstract bool HasStyleToRender(RenderDataStyle style);

    public abstract void Render(RenderDataStyle style);

    public bool HasAlphaToRender()
    {
        if (HasStyleToRender(RenderDataStyle.FogBarrier))
            return true;

        for (int i = 0; i < AlphaStyles.Length; i++)
        {
            if (HasStyleToRender(AlphaStyles[i]))
                return true;
        }

        return false;
    }

    public void RenderAllAlpha()
    {
        for (int i = 0; i < AlphaStyles.Length; i++)
            Render(AlphaStyles[i]);
    }
}
