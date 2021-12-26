namespace Helion.Maps.Specials;

public enum SectorDataType
{
    Boom,
    ZDoom
}

interface ISectorBits
{
    int SectorTypeMask { get; }
    // Flags use of alternate MBF21 types
    int AltSectorTypeFlag { get; }
    int SectorDamageMask { get; }
    int SectorDamageShift { get; }
    int SecretFlag { get; }
    int IceFlag { get; }
    int WindFlag { get; }
    int KillAllMonsters { get; }

    // Alternate MBF21 flags
    int KillPlayer { get; }
    int KillAllPlayersAndExit { get; }
    int KillAllPlayserAndSecretExit { get; }
}

class BoomSectorBits : ISectorBits
{
    public int SectorTypeMask => 0x1F;
    public int AltSectorTypeFlag => 4096;
    public int SectorDamageMask => 0x60;
    public int SectorDamageShift => 5;
    public int SecretFlag => 0x80;
    public int IceFlag => 0x100;
    public int WindFlag => 0x200;
    public int KillAllMonsters => 8192;
    public int KillPlayer => 32;
    public int KillAllPlayersAndExit => 64;
    public int KillAllPlayserAndSecretExit => 96;
}

class ZDoomSectorBits : ISectorBits
{
    public int SectorTypeMask => 0x6F;
    public int AltSectorTypeFlag => 0; // No idea if this will exist in GZDoom in the future
    public int SectorDamageMask => 0x300;
    public int SectorDamageShift => 8;
    public int SecretFlag => 0x400;
    public int IceFlag => 0x800;
    public int WindFlag => 0x1000;
    public int KillAllMonsters => 8192;
    public int KillPlayer => 0;
    public int KillAllPlayersAndExit => 0;
    public int KillAllPlayserAndSecretExit => 0;
}

public static class SectorSpecialData
{
    private static readonly ISectorBits[] Bits = new ISectorBits[] { new BoomSectorBits(), new ZDoomSectorBits() };

    public static int GetType(int sectorType, SectorDataType type)
    {
        ISectorBits bits = Bits[(int)type];
        int alt = sectorType & bits.AltSectorTypeFlag;
        if (alt != 0)
            return alt;

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
        sectorData.SectorEffect = GetSectorEffect(sectorType, bits);

        if ((sectorType & bits.AltSectorTypeFlag) != 0)
            SetAltSectorTypeData(sectorType & ~bits.AltSectorTypeFlag, sectorData, bits);
        else
            sectorData.DamageAmount = GetDamageAmount(sectorType, bits);

        if ((sectorType & bits.KillAllMonsters) != 0)
            sectorData.InstantKillEffect |= InstantKillEffect.KillMonsters;
    }

    private static void SetAltSectorTypeData(int sectorType, SectorData sectorData, ISectorBits bits)
    {
        if (sectorType == bits.KillAllPlayserAndSecretExit)
            sectorData.InstantKillEffect = InstantKillEffect.KillAllPlayersSecretExit;
        else if (sectorType == bits.KillAllPlayersAndExit)
            sectorData.InstantKillEffect = InstantKillEffect.KillAllPlayersExit;
        else if (sectorType == bits.KillPlayer)
            sectorData.InstantKillEffect = InstantKillEffect.KillPlayer;
        else
            sectorData.InstantKillEffect = InstantKillEffect.KillUnprotectedPlayer;
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
