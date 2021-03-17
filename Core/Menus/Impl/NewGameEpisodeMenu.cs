using Helion.Audio.Sounds;
using Helion.Layer;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using NLog;
using System;

namespace Helion.Menus.Impl
{
    public class NewGameEpisodeMenu : Menu
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public NewGameEpisodeMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) : 
            base(config, console, soundManager, archiveCollection, 40, true)
        {
            var episodes = archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
            if (episodes.Count == 0)
            {
                Log.Error("Expected at least one episode definition");
                return;
            }
            
            Components = Components.Add(new MenuImageComponent("M_EPISOD", 12, 12));
            
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
                if (ArchiveCollection.GetIWadInfo().IWadType == IWadType.DoomShareware && 
                    !episode.StartMap.Equals("e1m1", StringComparison.OrdinalIgnoreCase))
                {
                    string[] lines = archiveCollection.Definitions.Language.GetDefaultMessage("$SWSTRING").Split(new char[] { '\n' });
                    return new(episode.PicName, 0, 2, "M_SKULL1", "M_SKULL2",
                        () => new MessageMenu(config, Console, soundManager, ArchiveCollection, lines));
                }

                return new(episode.PicName, 0, 2, "M_SKULL1", "M_SKULL2", 
                        () => new NewGameSkillMenu(config, console, soundManager, archiveCollection, episode.StartMap));
            }
        }
    }
}
