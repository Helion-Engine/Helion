using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World;

namespace Helion.Render.Legacy.Renderers.World;

public enum RenderObjectType
{
    Entity,
    Side
}

public interface IRenderObject
{
    double RenderDistance { get; set; }
    RenderObjectType Type { get; }
}
