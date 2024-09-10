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
    public int? Slot { get; set; }
    public int? SlotPriority { get; set; }
    public int? SwitchPriority { get; set; }
    public bool? InitialOwned { get; set; }
    public bool? InitialRaised { get; set; }
    public string? CarouselIcon { get; set; }
    public int? AllowSwitchWithOwnedWeapon { get; set; }
    public int? NoSwitchWithOwnedWeapon { get; set; }
    public int? AllowSwitchWithOwnedItem { get; set; }
    public int? NoSwitchWithOwnedItem { get; set; }
}
