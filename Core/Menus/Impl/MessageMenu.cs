using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using System.Collections.Generic;

namespace Helion.Menus.Impl
{
    public class MessageMenu  : Menu
    {
        public MessageMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection,
            IList<string> text)
            : base(config, console, soundManager, archiveCollection, 90)
        {
            for (int i = 0; i < text.Count; i++)
            {
                Components = Components.Add(new MenuSmallTextComponent(text[i]));
                if (i != text.Count - 1)
                    Components = Components.Add(new MenuPaddingComponent(8));
            }

            SetToFirstActiveComponent();
        }
    }
}
