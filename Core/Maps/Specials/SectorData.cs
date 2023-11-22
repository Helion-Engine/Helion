namespace Helion.Maps.Specials;

public class SectorData
{
    public static readonly SectorData Default = new();

    public int DamageAmount { get; set; }
    public SectorEffect SectorEffect { get; set; }
    public InstantKillEffect InstantKillEffect { get; set; }

    public void Clear()
    {
        DamageAmount = 0;
        SectorEffect = SectorEffect.None;
        InstantKillEffect = InstantKillEffect.None;
    }
}
