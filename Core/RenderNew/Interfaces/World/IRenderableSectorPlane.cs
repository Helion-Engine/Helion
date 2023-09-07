namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableSectorPlane
{
    int GetIndex();
    float GetZ();
    int GetLightLevel();
    int GetTextureIdx();
}