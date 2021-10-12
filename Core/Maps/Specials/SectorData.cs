namespace Helion.Maps.Specials;

public class SectorData
{
    public bool Secret { get; set; }
    public int DamageAmount { get; set; }
    public SectorEffect SectorEffect { get; set; }

    public void Clear()
    {
        Secret = false;
        DamageAmount = 0;
        SectorEffect = SectorEffect.None;
    }
}
