namespace Helion.Resources.Textures.Sprites;

public class ResourceSpriteDefinition
{
    private readonly ResourceSpriteRotation[] Rotations;

    public ResourceSpriteRotation Frame1 => Rotations[0];
    public ResourceSpriteRotation Frame2 => Rotations[1];
    public ResourceSpriteRotation Frame3 => Rotations[2];
    public ResourceSpriteRotation Frame4 => Rotations[3];
    public ResourceSpriteRotation Frame5 => Rotations[4];
    public ResourceSpriteRotation Frame6 => Rotations[5];
    public ResourceSpriteRotation Frame7 => Rotations[6];
    public ResourceSpriteRotation Frame8 => Rotations[7];

    public ResourceSpriteDefinition(ResourceSpriteRotation frame1, ResourceSpriteRotation frame2, 
        ResourceSpriteRotation frame3, ResourceSpriteRotation frame4, ResourceSpriteRotation frame5, 
        ResourceSpriteRotation frame6, ResourceSpriteRotation frame7, ResourceSpriteRotation frame8)
    {
        Rotations = new[] { frame1, frame2, frame3, frame4, frame5, frame6, frame7, frame8 };
    }

    /// <summary>
    /// Gets the sprite at the offset provided. Note this is zero index based.
    /// </summary>
    /// <param name="index">The rotation index.</param>
    public ResourceSpriteRotation this[int index] => Rotations[index];
}