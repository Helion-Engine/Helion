namespace Helion.Maps.Specials.Compatibility;

public class LineSpecialCompatibility
{
    public static readonly LineSpecialCompatibility Default = new() { CompatibilityType = LineSpecialCompatibilityType.None };
    public static readonly LineSpecialCompatibility DefaultVanilla = new() { CompatibilityType = LineSpecialCompatibilityType.None, IsVanilla = true };
    public static readonly LineSpecialCompatibility VanillaProjectileTrigger = new() { CompatibilityType = LineSpecialCompatibilityType.ProjectileTrigger, IsVanilla = true };

    public LineSpecialCompatibilityType CompatibilityType { get; set; }
    public bool IsVanilla { get; set; }
}
