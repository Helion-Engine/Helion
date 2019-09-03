namespace Helion.MapsNew.Components
{
    /// <summary>
    /// The sector for a map.
    /// </summary>
    public interface ISector
    {
        /// <summary>
        /// The unique ID of the sector.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The Z height of the floor.
        /// </summary>
        short FloorZ { get; }
        
        /// <summary>
        /// The Z height of the ceiling.
        /// </summary>
        short CeilingZ { get; }
        
        /// <summary>
        /// The texture for the floor.
        /// </summary>
        string FloorTexture { get; }
        
        /// <summary>
        /// The texture for the ceiling.
        /// </summary>
        string CeilingTexture { get; }
        
        /// <summary>
        /// The light level. This does not need to be between 0 - 256, it
        /// supports the full range (and some maps use this for light tricks).
        /// </summary>
        short LightLevel { get; }
        
        /// <summary>
        /// The tag lookup ID for the sector.
        /// </summary>
        ushort Tag { get; }
    }
}