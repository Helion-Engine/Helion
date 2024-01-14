using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Render.Common.Enums;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using NLog;
using System;
using System.Linq;

namespace Helion.Menus.Impl;

public class NewGameEpisodeMenu : Menu
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const int OffsetX = 48;
    private const int EpisodeHeight = 16;

    private static int GetTopPixelPadding(ArchiveCollection archiveCollection)
    {
        int topPixelPadding = 31;
        var episodes = archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
        int count = episodes.Count - 4;
        if (count <= 0)
            return topPixelPadding;
        return Math.Max(topPixelPadding - count * EpisodeHeight / 4, 0);
    }

    public NewGameEpisodeMenu(IConfig config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) :
        base(config, console, soundManager, archiveCollection, GetTopPixelPadding(archiveCollection), true)
    {
        var episodes = archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
        if (episodes.Count == 0)
        {
            Log.Error("Expected at least one episode definition");
            return;
        }

        Components = Components.Add(new MenuImageComponent("M_EPISOD", 54, 6, imageAlign: Align.TopLeft, overrideY: 24));

        foreach (EpisodeDef episode in episodes)
        {
            if (episode.Optional && ArchiveCollection.Entries.FindByName(episode.PicName) == null)
                continue;

            IMenuComponent component = MakeMenuComponent(episode);
            Components = Components.Add(component);
        }

        SetToFirstActiveComponent();

        IMenuComponent MakeMenuComponent(EpisodeDef episode)
        {
            if (ArchiveCollection.IWadInfo.IWadType == IWadType.DoomShareware &&
                !episode.StartMap.Equals("e1m1", StringComparison.OrdinalIgnoreCase))
            {
                string[] lines = archiveCollection.Definitions.Language.GetMessages("$SWSTRING");
                return new MenuImageComponent(episode.PicName, OffsetX, 0, "M_SKULL1", "M_SKULL2",
                    () => new MessageMenu(config, Console, soundManager, ArchiveCollection, lines),
                    imageAlign: Align.TopLeft);
            }

            return new MenuImageComponent(episode.PicName, OffsetX, 0, "M_SKULL1", "M_SKULL2",
                    () => new NewGameSkillMenu(config, console, soundManager, archiveCollection, episode.StartMap),
                    imageAlign: Align.TopLeft, title: ArchiveCollection.Language.GetMessage(episode.Name), overrideY: EpisodeHeight);
        }
    }
}
