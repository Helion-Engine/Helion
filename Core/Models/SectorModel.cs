namespace Helion.Models
{
    public class SectorModel
    {
        public int Id;
        public int SoundValidationCount;
        public int SoundBlock;
        public int? SoundTarget;

        public int SectorDataChanges;
        public double? FloorZ;
        public double? CeilingZ;
        public short? LightLevel;
        public int? FloorTexture;
        public int? CeilingTexture;
        public int? SectorSpecialType;
    }
}
