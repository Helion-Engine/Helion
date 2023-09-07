namespace Helion.RenderNew.Interfaces.World;

public enum WallSection
{
    Lower,
    Middle,
    Upper
}

public interface IRenderableWall
{
    int GetIndex();
    int GetTextureIndex();
}
