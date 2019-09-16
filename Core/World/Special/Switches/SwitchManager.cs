using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.World.Special.Switches
{
    public class SwitchManager
    {
        private DefinitionEntries m_definition;

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
            var animSwitch = m_definition.Animdefs.AnimatedSwitches.FirstOrDefault(x => x.IsMatch(side.Middle.Texture));
            if (animSwitch != null)
                side.Middle.Texture = animSwitch.GetOpposingTexture(side.Middle.Texture);             
        }

        private void SetTwoSidedLineSwitch(TwoSided side)
        {
            foreach (var animSwitch in m_definition.Animdefs.AnimatedSwitches)
            {
                if (animSwitch.IsMatch(side.Upper.Texture))
                {
                    side.Upper.Texture = animSwitch.GetOpposingTexture(side.Upper.Texture);
                    break;
                }

                if (animSwitch.IsMatch(side.Middle.Texture))
                {
                    side.Middle.Texture = animSwitch.GetOpposingTexture(side.Middle.Texture);
                    break;
                }

                if (animSwitch.IsMatch(side.Lower.Texture))
                {
                    side.Lower.Texture = animSwitch.GetOpposingTexture(side.Lower.Texture);
                    break;
                }
            }
        }
    }
}