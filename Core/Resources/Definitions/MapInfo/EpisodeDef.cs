using Helion.Util.Extensions;

namespace Helion.Resources.Definitions.MapInfo;

public class EpisodeDef
{
    public string StartMap { get; set; } = string.Empty;
    public string PicName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool Optional { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not EpisodeDef episode)
            return false;

        return StartMap.EqualsIgnoreCase(episode.StartMap);
    }
}
