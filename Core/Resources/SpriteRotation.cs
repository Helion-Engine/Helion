namespace Helion.Resources;

public class SpriteRotation
{
    public readonly Texture Texture;
    public readonly bool Mirror;

    public SpriteRotation(Texture texture, bool mirror)
    {
        Texture = texture;
        Mirror = mirror;
    }
}

