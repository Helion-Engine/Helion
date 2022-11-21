using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Renderers.World;

namespace Helion.Render.Renderers.World;

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
