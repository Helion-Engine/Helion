namespace Helion.World.Sound;

/// <summary>
/// All the different sound channels that are supported on some entity.
/// </summary>
/// <remarks>
/// The channels have been documented with their values to indicate to
/// anyone that we shouldn't be added values between them or before them
/// because channels 0-7 rely on having these exact numbers. We end up
/// doing casting of Channel7 to an integer and then iterate down to zero
/// to find free channels, so if any of these channels go outside of the
/// range of 0-7 then we will run into some pretty bad issues.
/// Further, there are arrays that depend on the count of the number of
/// elements in this enumeration, so if there ever are any changes then
/// those will need to be updated appropriately.
/// </remarks>
public enum SoundChannelType
{
    Auto = 0,
    Weapon = 1,
    Voice = 2,
    Item = 3,
    Default = 4,
    Channel5 = 5,
    Channel6 = 6,
    Channel7 = 7,
}
