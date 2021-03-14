using System;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;

namespace Helion.Menus.Impl
{
    public class MainMenu : Menu
    {
        private static readonly Func<Menu?> TodoAction = () => null;
        
        public MainMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) : 
            base(config, console, soundManager, archiveCollection, 8)
        {
            Components = Components.AddRange(new[] 
            {
                new MenuImageComponent("M_DOOM", paddingY: 8),
                CreateMenuOption("M_NGAME", -6, 2, CreateNewGameMenu()),
                CreateMenuOption("M_OPTION", -15, 2, TodoAction),
                CreateMenuOption("M_LOADG", 1, 2, TodoAction),
                CreateMenuOption("M_SAVEG", 1, 2, TodoAction),
                CreateMenuOption("M_QUITG", -3, 2, () => new QuitGameMenu(config, Console, soundManager, ArchiveCollection))
            });

            SetToFirstActiveComponent();

            static IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?> action)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action);
            }
        }

        private Func<Menu?> CreateNewGameMenu()
        {
            bool hasEpisodes = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes.Any(e => !e.PicName.Empty());
            return hasEpisodes ?
                () => new NewGameEpisodeMenu(Config, Console, SoundManager, ArchiveCollection) :
                () => new NewGameSkillMenu(Config, Console, SoundManager, ArchiveCollection, null);
        }
    }
}
