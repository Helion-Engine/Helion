namespace Helion.RenderNew.Interfaces.World;

// These numbers are necessary so it can be casted to bits.
public enum WallSection
{
    Lower = 0,
    Middle = 1,
    Upper = 2
}

public interface IRenderableWall
{
    int GetIndex();
    int GetTextureIndex();
}
