using Helion.Util;

namespace Helion.World.Special.Switches
{
    public class SwitchData
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
}