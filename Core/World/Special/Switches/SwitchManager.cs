using System.Collections.Generic;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.World.Special.Switches
{
    public class SwitchManager
    {
        private List<SwitchData> m_switchData = new List<SwitchData>();

        public SwitchManager()
        {
            m_switchData.Add(new SwitchData("SW1COMP", "SW2COMP"));
            m_switchData.Add(new SwitchData("SW1METAL", "SW2METAL"));
            m_switchData.Add(new SwitchData("SW1MET2", "SW2MET2"));
            m_switchData.Add(new SwitchData("SW1BRCOM", "SW2BRCOM"));
            m_switchData.Add(new SwitchData("SW1COMP", "SW2COMP"));
            m_switchData.Add(new SwitchData("SW1GARG", "SW2GARG"));
            m_switchData.Add(new SwitchData("SW1STON1", "SW2STON1"));
            m_switchData.Add(new SwitchData("SW1SLAD", "SW2SLAD"));
            m_switchData.Add(new SwitchData("SW1PIPE", "SW2PIPE"));
            m_switchData.Add(new SwitchData("SW1STRTN", "SW2STRTN"));
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
            foreach (SwitchData data in m_switchData)
            {
                if (data.IsMatch(side.Middle.Texture))
                {
                    side.Middle.Texture = data.GetOpposingTexture(side.Middle.Texture);
                    break;
                }
            }
        }

        private void SetTwoSidedLineSwitch(TwoSided side)
        {
            foreach (SwitchData data in m_switchData)
            {
                if (data.IsMatch(side.Upper.Texture))
                {
                    side.Upper.Texture = data.GetOpposingTexture(side.Upper.Texture);
                    break;
                }

                if (data.IsMatch(side.Middle.Texture))
                {
                    side.Middle.Texture = data.GetOpposingTexture(side.Middle.Texture);
                    break;
                }

                if (data.IsMatch(side.Lower.Texture))
                {
                    side.Lower.Texture = data.GetOpposingTexture(side.Lower.Texture);
                    break;
                }
            }
        }
    }
}