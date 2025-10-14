namespace Helion.Resources.Definitions.Decorate.Properties.Enums;

public enum RenderStyle
{
    None = 0,
    Normal,
    Fuzzy,
    Translucent,
    Add,
    ColorAdd,
    // Use ColorAdd when FullBright, otherwise Translucent
    ColorAddFullBright,
    // Use ColorAdd after death, otherwise Normal (Currently set from +DEHEXPLOSION)
    ColorAddExplosion,
    Count
}
