using Helion.MapsNew.Specials.Vanilla;

namespace Helion.MapsNew.Components.Linedefs
{
    public record DoomLinedef : Linedef
    {
        /// <summary>
        /// The line special type.
        /// </summary>
        public VanillaLineSpecialType LineType;

        /// <summary>
        /// The target sector tag. This is not guaranteed to be correct.
        /// </summary>
        public int SectorTag;
    }
}
