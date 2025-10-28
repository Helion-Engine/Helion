using Helion.Maps.Components;
using Helion.Util;

namespace Helion.Maps.Udmf.Components;

public class UdmfSector : ISector
{
    public int Id { get; set; }
    public short FloorZ { get; set; }
    public short CeilingZ { get; set; }
    public string FloorTexture { get; set; } = Constants.NoTexture;
    public string CeilingTexture { get; set; } = Constants.NoTexture;
    public short LightLevel { get; set; } = 160;
    public ushort Tag { get; set; }
    public int Special;
    public double RotationFloor;
    public double RotationCeiling;
    public double PanningFloorX;
    public double PanningFloorY;
    public double PanningCeilingX;
    public double PanningCeilingY;
    public double ScaleFloorX = 1;
    public double ScaleFloorY = 1;
    public double ScaleCeilingX = 1;
    public double ScaleCeilingY = 1;
    public double Gravity = 1;
    public bool LightCeilingAbsolute;
    public bool LightFloorAbsolute;
    public bool Silent;
    public bool NoAttack;
    public short LightCeiling;
    public short LightFloor;
    public int DamageAmount;
    public int DamageInterval;
    public int Leakiness;
    public string SkyFloor = string.Empty;
    public string SkyCeiling = string.Empty;
}
