namespace Helion.Resources.Sprites
{
    /// <summary>
    /// An indicator of what kind of rotation the sprite rotations are for.
    /// </summary>
    public enum SpriteRotationType
    {
        // One single frame (most commonly the XXXXX0 frames).
        Single, 

        // A sprite that has all 8 frames defined.
        Octant,

        // A sprite with only 5 frames defined, and 5/6/7 are reflections.
        OctantReflect
    }
}
