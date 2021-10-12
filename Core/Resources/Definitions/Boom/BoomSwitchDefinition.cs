using Helion.Resources.Archives.Entries;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Boom;

public class BoomSwitchDefinition
{
    private const int SwitchLength = 20;
    public readonly IList<BoomSwitch> Switches = new List<BoomSwitch>();

    public void Parse(Entry entry)
    {
        byte[] data = entry.ReadData();
        for (int i = 0; i < data.Length; i += SwitchLength)
        {
            if (data.Length - i < SwitchLength)
                break;

            string off = BoomParseUtil.GetString(data, i, BoomParseUtil.NameLength);
            string on = BoomParseUtil.GetString(data, i + 9, BoomParseUtil.NameLength);

            if (string.IsNullOrEmpty(on) || string.IsNullOrEmpty(off))
                continue;

            Switches.Add(new(on, off));
        }
    }
}

