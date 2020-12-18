using System.Linq;
using Helion.Resource;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Util;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Walls;

namespace Helion.Worlds.Special.Switches
{
    public class SwitchManager
    {
        private readonly Resources m_resources;

        public SwitchManager(Resources resources)
        {
            m_resources = resources;
        }

        public bool IsLineSwitch(Line line) => GetLineLineSwitchTexture(line).Item1 != Constants.NoTextureIndex;

        public void SetLineSwitch(Line line)
        {
            (int, WallLocation) switchSet = GetLineLineSwitchTexture(line);
            if (switchSet.Item1 != Constants.NoTextureIndex)
            {
                if (line.Front is TwoSided twoSided)
                {
                    switch (switchSet.Item2)
                    {
                        case WallLocation.Upper:
                            twoSided.Upper.TextureHandle = switchSet.Item1;
                            break;
                        case WallLocation.Middle:
                            twoSided.Middle.TextureHandle = switchSet.Item1;
                            break;
                        case WallLocation.Lower:
                            twoSided.Lower.TextureHandle = switchSet.Item1;
                            break;
                    }
                }
                else
                {
                    line.Front.Middle.TextureHandle = switchSet.Item1;
                }
            }
        }

        private (int, WallLocation) GetLineLineSwitchTexture(Line line)
        {
            if (line.Front is TwoSided twoSided)
            {
                foreach (var animSwitch in m_resources.Animations.AnimatedSwitches)
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
                var switchList = m_resources.Animations.AnimatedSwitches;
                AnimatedSwitch? animSwitch = switchList.FirstOrDefault(sw => sw.IsMatch(line.Front.Middle.TextureHandle));
                if (animSwitch != null)
                    return (animSwitch.GetOpposingTexture(line.Front.Middle.Texture), WallLocation.Middle);
            }

            return (Constants.NoTextureIndex, WallLocation.None);
        }
    }
}