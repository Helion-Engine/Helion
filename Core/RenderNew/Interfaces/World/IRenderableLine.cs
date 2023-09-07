namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableLine
{
    int GetIndex();
    IRenderableSide GetFront();
    IRenderableSide? GetBack();
}