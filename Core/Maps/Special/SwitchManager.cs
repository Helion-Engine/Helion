using Helion.Maps.Geometry.Lines;
using Helion.Util;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Maps.Special
{
    class SwitchData
    {
        public SwitchData(CIString switch1, CIString switch2)
        {
            SwitchTexture1 = switch1;
            SwitchTexture2 = switch2;
        }

        public bool IsMatch(CIString texture) => texture == SwitchTexture1 || texture == SwitchTexture2;
        public CIString GetOpposingTexture(CIString texture) => texture == SwitchTexture1 ? SwitchTexture2 : SwitchTexture1;

        public CIString SwitchTexture1;
        public CIString SwitchTexture2;
    }

    enum TexturePlacement
    {
        Upper,
        Middle,
        Lower,
    }

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
            foreach (var data in m_switchData)
            {
                if (data.IsMatch(line.Front.UpperTexture))
                {
                    line.Front.UpperTexture = data.GetOpposingTexture(line.Front.UpperTexture);
                    break;
                }

                if (data.IsMatch(line.Front.MiddleTexture))
                {
                    line.Front.MiddleTexture = data.GetOpposingTexture(line.Front.MiddleTexture);
                    break;
                }

                if (data.IsMatch(line.Front.LowerTexture))
                {
                    line.Front.LowerTexture = data.GetOpposingTexture(line.Front.LowerTexture);
                    break;
                }
            }
        }
    }
}
