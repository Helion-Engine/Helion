namespace Helion.Maps.Specials;

public enum SectorDataType
{
    Boom,
    ZDoom
}

interface ISectorBits
{
    int SectorTypeMask { get; }
    int SectorDamageMask { get; }
    int SectorDamageShift { get; }
    int SecretFlag { get; }
    int IceFlag { get; }
    int WindFlag { get; }
}

class BoomSectorBits : ISectorBits
{
    public int SectorTypeMask => 0x1F;
    public int SectorDamageMask => 0x60;
    public int SectorDamageShift => 5;
    public int SecretFlag => 0x80;
    public int IceFlag => 0x100;
    public int WindFlag => 0x200;
}

class ZDoomSectorBits : ISectorBits
{
    public int SectorTypeMask => 0x6F;
    public int SectorDamageMask => 0x300;
    public int SectorDamageShift => 8;
    public int SecretFlag => 0x400;
    public int IceFlag => 0x800;
    public int WindFlag => 0x1000;
}

public static class SectorSpecialData
{
    private static readonly ISectorBits[] Bits = new ISectorBits[] { new BoomSectorBits(), new ZDoomSectorBits() };

    public static int GetType(int sectorType, SectorDataType type)
    {
        ISectorBits bits = Bits[(int)type];
        return sectorType & bits.SectorTypeMask;
    }

    public static void SetSectorData(int sectorType, SectorData sectorData, SectorDataType type)
    {
        if (sectorType == 0)
        {
            sectorData.Clear();
            return;
        }

        ISectorBits bits = Bits[(int)type];
        sectorData.Secret = (sectorType & bits.SecretFlag) != 0;
        sectorData.DamageAmount = GetDamageAmount(sectorType, bits);
        sectorData.SectorEffect = GetSectorEffect(sectorType, bits);
    }

    private static int GetDamageAmount(int sectorType, ISectorBits bits)
    {
        return ((sectorType & bits.SectorDamageMask) >> bits.SectorDamageShift) switch
        {
            1 => 5,
            2 => 10,
            3 => 20,
            _ => 0,
        };
    }

    private static SectorEffect GetSectorEffect(int sectorType, ISectorBits bits)
    {
        SectorEffect sectorEffect = SectorEffect.None;
        if ((sectorType & bits.IceFlag) != 0)
            sectorEffect |= SectorEffect.Ice;
        if ((sectorType & bits.WindFlag) != 0)
            sectorEffect |= SectorEffect.Wind;

        return sectorEffect;
    }
}

