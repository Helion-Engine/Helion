using System.Linq;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World.Special.Switches
{
    public static class SwitchManager
    {
        public static bool IsLineSwitch(DefinitionEntries definition, Line line) => GetLineLineSwitchTexture(definition, line).Item1 != Constants.NoTextureIndex;

        public static void SetLineSwitch(DefinitionEntries definition, Line line)
        {
            (int, WallLocation) switchSet = GetLineLineSwitchTexture(definition, line);
            if (switchSet.Item1 != Constants.NoTextureIndex)
            {
                if (line.Front is TwoSided twoSided)
                {
                    switch (switchSet.Item2)
                    {
                        case WallLocation.Upper:
                            twoSided.Upper.SetTexture(switchSet.Item1, SideDataTypes.UpperTexture);
                            break;
                        case WallLocation.Middle:
                            twoSided.Middle.SetTexture(switchSet.Item1, SideDataTypes.MiddleTexture);
                            break;
                        case WallLocation.Lower:
                            twoSided.Lower.SetTexture(switchSet.Item1, SideDataTypes.LowerTexture);
                            break;
                    }
                }
                else
                {
                    line.Front.Middle.SetTexture(switchSet.Item1, SideDataTypes.MiddleTexture);
                }
            }
        }

        private static (int, WallLocation) GetLineLineSwitchTexture(DefinitionEntries definition, Line line)
        {
            if (line.Front is TwoSided twoSided)
            {
                foreach (var animSwitch in definition.Animdefs.AnimatedSwitches)
                {
                    if (twoSided.Upper.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(twoSided.Upper.TextureHandle))
                        return (animSwitch.GetOpposingTexture(twoSided.Upper.TextureHandle), WallLocation.Upper);

                    if (twoSided.Middle.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(twoSided.Middle.TextureHandle))
                        return (animSwitch.GetOpposingTexture(twoSided.Middle.TextureHandle), WallLocation.Middle);

                    if (twoSided.Lower.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(twoSided.Lower.TextureHandle))
                        return (animSwitch.GetOpposingTexture(twoSided.Lower.TextureHandle), WallLocation.Lower);
                }
            }
            else
            {
                var switchList = definition.Animdefs.AnimatedSwitches;
                AnimatedSwitch? animSwitch = switchList.FirstOrDefault(sw => sw.IsMatch(line.Front.Middle.TextureHandle));
                if (animSwitch != null)
                    return (animSwitch.GetOpposingTexture(line.Front.Middle.TextureHandle), WallLocation.Middle);
            }

            return (Constants.NoTextureIndex, WallLocation.None);
        }
    }
}