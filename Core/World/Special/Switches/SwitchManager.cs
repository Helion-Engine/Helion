using System.Linq;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Animdefs.Switches;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.World.Special.Switches
{
    public class SwitchManager
    {
        private readonly DefinitionEntries m_definition;

        public SwitchManager(DefinitionEntries definition)
        {
            m_definition = definition;
        }

        public void SetLineSwitch(Line line)
        {
            if (line.Front is TwoSided twoSided)
                SetTwoSidedLineSwitch(twoSided);
            else
                SetOneSidedLineSwitch(line.Front);
        }

        private void SetOneSidedLineSwitch(Side side)
        {
            var switchList = m_definition.Animdefs.AnimatedSwitches;
            AnimatedSwitch? animSwitch = switchList.FirstOrDefault(sw => sw.IsMatch(side.Middle.TextureHandle));
            if (animSwitch != null)
                side.Middle.TextureHandle = animSwitch.GetOpposingTexture(side.Middle.TextureHandle);
        }

        private void SetTwoSidedLineSwitch(TwoSided side)
        {
            foreach (var animSwitch in m_definition.Animdefs.AnimatedSwitches)
            {
                if (side.Upper.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Upper.TextureHandle))
                {
                    side.Upper.TextureHandle = animSwitch.GetOpposingTexture(side.Upper.TextureHandle);
                    break;
                }

                if (side.Middle.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Middle.TextureHandle))
                {
                    side.Middle.TextureHandle = animSwitch.GetOpposingTexture(side.Middle.TextureHandle);
                    break;
                }

                if (side.Lower.TextureHandle != Constants.NoTextureIndex && animSwitch.IsMatch(side.Lower.TextureHandle))
                {
                    side.Lower.TextureHandle = animSwitch.GetOpposingTexture(side.Lower.TextureHandle);
                    break;
                }
            }
        }
    }
}