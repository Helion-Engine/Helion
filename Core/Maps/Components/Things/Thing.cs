using Helion.Util.Geometry.Vectors;

namespace Helion.Maps.Components.Things
{
    public class Thing
    {
        /// <summary>
        /// The thing ID for the thing. This is different from the ID since
        /// this ID is for looking up things in the map and may be used on
        /// multiple things, meaning it is not unique.
        /// </summary>
        public int ThingId { get; init; }

        /// <summary>
        /// The position in the map. The Z coordinate will be the most negative
        /// number in implementations that do not support the third dimension.
        /// </summary>
        public Vec3Fixed Position { get; init; }

        /// <summary>
        /// The angle (in degrees).
        /// </summary>
        public int Angle { get; init; }

        /// <summary>
        /// The editor number for what type this is.
        /// </summary>
        public int EditorNumber { get; init; }

        /// <summary>
        /// The flags for the thing.
        /// </summary>
        public ThingFlags Flags { get; init; }
    }
}
