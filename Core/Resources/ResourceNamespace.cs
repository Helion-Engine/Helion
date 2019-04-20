namespace Helion.Resources
{
    /// <summary>
    /// The different namespace locations for a resource.
    /// </summary>
    /// <remarks>
    /// These have a profound effect on how images are rendered. For example:
    /// due to how some wads are made, images are duplicated but exist in
    /// </remarks>
    public enum ResourceNamespace
    {
        Global,
        ACS,
        Flats,
        Fonts,
        Music,
        Sounds,
        Sprites,
        Textures
    }
}
