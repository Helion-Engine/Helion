using Helion.MapsNew.Components;
using Helion.MapsNew.Doom.Components.Types;

namespace Helion.MapsNew.Doom.Components
{
    public class DoomSector : ISector
    {
        public int Id { get; }
        public short FloorZ { get; }
        public short CeilingZ { get; }
        public string FloorTexture { get; }
        public string CeilingTexture { get; }
        public short LightLevel { get; }
        public ushort Tag { get; }
        public readonly DoomSectorType SectorType;

        internal DoomSector(int id, short floorZ, short ceilingZ, string floorTexture, string ceilingTexture, 
            short lightLevel, DoomSectorType sectorType, ushort tag)
        {
            Id = id;
            FloorZ = floorZ;
            CeilingZ = ceilingZ;
            FloorTexture = floorTexture;
            CeilingTexture = ceilingTexture;
            LightLevel = lightLevel;
            Tag = tag;
            SectorType = sectorType;
        }
    }
}