using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Components.Linedefs
{
    public record HexenLinedef : DoomLinedef
    {
        /// <summary>
        /// The type of line this is.
        /// </summary>
        public ZDoomLineSpecialType LineType { get; init; }

        /// <summary>
        /// The args for the line special.
        /// </summary>
        public SpecialArgs Args { get; init; }
    }
}
