namespace Helion.Resources.Definitions.Decorate.Properties
{
    public enum HealthPickupAutoUse
    {
        Never = 0,
        WouldDieAndWithAutoUseFlag = 1,
        WouldDieAndWithAutoUseFlagOrDeathmatch = 2,
        AutoUseUnderFiftyHealth = 3,
    }
}