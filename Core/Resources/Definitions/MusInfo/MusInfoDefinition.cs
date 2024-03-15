using Helion;
using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.MusInfo;
using Helion.Util.Parser;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MusInfo;

public class MusInfoDefinition
{
    public readonly List<MusInfoMap> Items = new();

    public void Parse(string text)
    {
        SimpleParser parser = new();
        parser.Parse(text);

        while (!parser.IsDone())
        {
            string mapName = parser.ConsumeString();
            MusInfoMap musInfoMap = new MusInfoMap(mapName);

            while (parser.PeekInteger(out _))
            {
                int number = parser.ConsumeInteger();
                string music = parser.ConsumeString();

                musInfoMap.Music.Add(new MusInfoDef(number, music));
            }

            if (musInfoMap.Music.Count > 0)
                Items.Add(musInfoMap);
        }
    }
}
