namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableSector
{
    int GetIndex();
    IRenderableSectorPlane GetFloor();
    IRenderableSectorPlane GetCeiling();
    int GetLightLevel();
}