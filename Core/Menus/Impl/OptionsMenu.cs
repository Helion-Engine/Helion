using Helion.Audio.Sounds;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;

namespace Helion.Menus.Impl
{
    public class OptionsMenu : Menu
    {
        public OptionsMenu(IConfig config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) : 
            base(config, console, soundManager, archiveCollection, 64)
        {
            Components = Components.AddRange(new[] 
            {
                new MenuSmallTextComponent("Please use the console."),
                new MenuSmallTextComponent("I do not want to implement this yet.")
            });
        }
    }
}
