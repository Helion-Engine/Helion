namespace Helion.Resources;

public class SpriteRotation
{
    public Texture Texture;
    public bool Mirror;
    public float FlipU;
    public object? RenderStore;

    public SpriteRotation(Texture texture, bool mirror)
    {
        Texture = texture;
        Mirror = mirror;
        FlipU = mirror ? 1 : 0;
    }
}
