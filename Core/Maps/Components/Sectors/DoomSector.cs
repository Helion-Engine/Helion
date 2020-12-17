namespace Helion.Maps.Components.Sectors
{
    public record DoomSector : Sector
    {
        /// <summary>
        /// The sector type.
        /// </summary>
        public DoomSectorType SectorType { get; init; }
    }
}
