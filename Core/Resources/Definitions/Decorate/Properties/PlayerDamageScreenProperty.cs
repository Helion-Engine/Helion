using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate.Properties
{
    public class PlayerDamageScreenProperty
    {
        public readonly string Color;
        public readonly string DamageType;
        public readonly double Intensity;

        public PlayerDamageScreenProperty(string color, string damageType, double intensity)
        {
            Precondition(!color.Empty(), "Cannot have an empty damage screen property color");
            Precondition(!damageType.Empty(), "Cannot have an empty damage screen type");

            Color = color;
            DamageType = damageType;
            Intensity = intensity;
        }
    }
}