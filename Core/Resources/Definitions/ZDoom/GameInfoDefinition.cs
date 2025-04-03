using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Zdoom;

/// <summary>
/// ZDOOM GAMEINFO lump, only used here to get the WAD title (uncommon)
/// </summary>
/// <seealso href="https://zdoom.org/wiki/GAMEINFO"/>
public class GameInfoDefinition
{
    public string? StartupTitle { get; set; }

    private static readonly string StartupTitleName = "startuptitle";

    public void Parse(string data)
    {
        SimpleParser parser = new();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string item = parser.ConsumeString();

            if (item.EqualsIgnoreCase(StartupTitleName))
                StartupTitle = ConsumeStringValue(parser);
            else
                parser.ConsumeLine();
        }
    }

    private static string? ConsumeStringValue(SimpleParser parser)
    {
        try
        {
            parser.ConsumeString("=");
            return parser.ConsumeString();
        }
        catch
        {
            return null;
        }
    }
}
