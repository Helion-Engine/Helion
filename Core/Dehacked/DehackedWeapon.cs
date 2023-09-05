namespace Helion.Dehacked;

public class DehackedWeapon
{
    public int WeaponNumber { get; set; }
    public int? AmmoType { get; set; }
    public int? DeselectFrame { get; set; }
    public int? SelectFrame { get; set; }
    public int? BobbingFrame { get; set; }
    public int? ShootingFrame { get; set; }
    public int? FiringFrame { get; set; }
    public int? AmmoPerShot { get; set; }
    public int? MinAmmo { get; set; }
    public uint? Mbf21Bits { get; set; }
}
