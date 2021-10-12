namespace Helion.Render.Legacy.Renderers.Legacy.World;

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

