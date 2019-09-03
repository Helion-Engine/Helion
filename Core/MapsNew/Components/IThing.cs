using Helion.Util.Geometry;

namespace Helion.MapsNew.Components
{
    /// <summary>
    /// Represents an entity in a map.
    /// </summary>
    public interface IThing
    {
        /// <summary>
        /// The ID of the thing.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The position in the map. The Z coordinate will be the most negative
        /// number in implementations that do not support the third dimension.
        /// </summary>
        Vec3Fixed Position { get; }
        
        /// <summary>
        /// The angle (in degrees).
        /// </summary>
        ushort Angle { get; }
        
        /// <summary>
        /// The editor number for what type this is.
        /// </summary>
        ushort EditorNumber { get; }
    }
}