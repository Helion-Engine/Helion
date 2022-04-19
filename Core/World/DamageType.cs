namespace Helion.World;

public enum DamageType
{
    // Runs through normal entity damage rules (e.g. a baron ball will not damage a baron or hell knight)
    Normal,
    // Ignores any damage rules and will apply
    AlwaysApply
}
