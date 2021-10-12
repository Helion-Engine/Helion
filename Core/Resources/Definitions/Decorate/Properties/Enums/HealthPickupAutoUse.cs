namespace Helion.Resources.Definitions.Decorate.Properties.Enums;

public enum HealthPickupAutoUse
{
    Never = 0,
    WouldDieAndWithAutoUseFlag = 1,
    WouldDieAndWithAutoUseFlagOrDeathmatch = 2,
    AutoUseUnderFiftyHealth = 3,
}
