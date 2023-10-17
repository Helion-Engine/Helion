namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public enum RenderObjectType
{
    Entity,
    Side
}

public interface IRenderObject
{
    double RenderDistanceSquared { get; set; }
    RenderObjectType Type { get; }
}
