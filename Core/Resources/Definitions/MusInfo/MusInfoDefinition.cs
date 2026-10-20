using Helion;
using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.MusInfo;
using Helion.Util.Parser;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MusInfo;

public class MusInfoDefinition
{
    public readonly List<MusInfoMap> Items = [];

    public void Parse(string text)
    {
        SimpleParser parser = new();
        parser.Parse(text);

        while (!parser.IsDone())
        {
            var mapName = parser.ConsumeString();
            var musInfoMap = new MusInfoMap(mapName);

            while (parser.PeekInteger(out _))
            {
                var number = parser.ConsumeInteger();
                var music = parser.ConsumeStringSpan();
                if (music.Length > 8)
                    music = music[..8];

                musInfoMap.Music.Add(new MusInfoDef(number, music.ToString()));
            }

            if (musInfoMap.Music.Count > 0)
                Items.Add(musInfoMap);
        }
    }
}
