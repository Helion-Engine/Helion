using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using NLog;

namespace Helion.Menus.Impl
{
    public class NewGameEpisodeMenu : Menu
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public NewGameEpisodeMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) : 
            base(config, console, soundManager, archiveCollection, 40)
        {
            var episodes = archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
            if (episodes.Count == 0)
            {
                Log.Error("Expected at least one episode definition");
                return;
            }
            
            Components = Components.Add(new MenuImageComponent("M_EPISOD", 0, 8));
            
            foreach (EpisodeDef episode in episodes)
            {
                MenuImageComponent component = MakeMenuComponent(episode);
                Components = Components.Add(component);
            }

            SetToFirstActiveComponent();

            MenuImageComponent MakeMenuComponent(EpisodeDef episode)
            {
                return new(episode.PicName, 0, 2, "M_SKULL1", "M_SKULL2", 
                        () => new NewGameSkillMenu(config, console, soundManager, archiveCollection, episode.StartMap));
            }
        }
    }
}
