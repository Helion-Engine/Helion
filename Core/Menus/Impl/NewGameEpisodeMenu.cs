using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using NLog;
using System;
using Helion.Render.Legacy.Commands.Alignment;

namespace Helion.Menus.Impl;

public class NewGameEpisodeMenu : Menu
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const int OffsetX = 48;
    private const int PaddingY = 1;

    public NewGameEpisodeMenu(IConfig config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) :
        base(config, console, soundManager, archiveCollection, 30, true)
    {
        var episodes = archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
        if (episodes.Count == 0)
        {
            Log.Error("Expected at least one episode definition");
            return;
        }

        Components = Components.Add(new MenuImageComponent("M_EPISOD", 54, 8, imageAlign: Align.TopLeft, paddingBottomY: 8));

        foreach (EpisodeDef episode in episodes)
        {
            if (episode.Optional && ArchiveCollection.Entries.FindByName(episode.PicName) == null)
                continue;

            MenuImageComponent component = MakeMenuComponent(episode);
            Components = Components.Add(component);
        }

        SetToFirstActiveComponent();

        MenuImageComponent MakeMenuComponent(EpisodeDef episode)
        {
            if (ArchiveCollection.IWadInfo.IWadType == IWadType.DoomShareware &&
                !episode.StartMap.Equals("e1m1", StringComparison.OrdinalIgnoreCase))
            {
                string[] lines = archiveCollection.Definitions.Language.GetMessages("$SWSTRING");
                return new(episode.PicName, OffsetX, PaddingY, "M_SKULL1", "M_SKULL2",
                    () => new MessageMenu(config, Console, soundManager, ArchiveCollection, lines),
                    imageAlign: Align.TopLeft);
            }

            return new(episode.PicName, OffsetX, PaddingY, "M_SKULL1", "M_SKULL2",
                    () => new NewGameSkillMenu(config, console, soundManager, archiveCollection, episode.StartMap),
                    imageAlign: Align.TopLeft);
        }
    }
}

