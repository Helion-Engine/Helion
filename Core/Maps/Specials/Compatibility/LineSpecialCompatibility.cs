namespace Helion.Maps.Specials.Compatibility
{
    public class LineSpecialCompatibility
    {
        public static LineSpecialCompatibility Default { get; private set; } = new() { CompatibilityType = LineSpecialCompatibilityType.None };

        public LineSpecialCompatibilityType CompatibilityType;
    }
}
