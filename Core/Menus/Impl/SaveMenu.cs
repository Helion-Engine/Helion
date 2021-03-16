using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.World.Save;

namespace Helion.Menus.Impl
{
    public class SaveMenu : Menu
    {
        public const int MaxRows = 6;
        public const string SaveGameImage = "M_SGTTL";

        public bool IsTypingName { get; private set; }

        public bool NoSavedGames => Components.Count == 1;

        public SaveMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection,
            SaveGameManager saveManager, int topPixelPadding = 16, bool leftAlign = true) 
            : base(config, console, soundManager, archiveCollection, topPixelPadding, leftAlign)
        {
            Components = Components.Add(new MenuImageComponent(SaveGameImage, paddingY: 16));

            List<SaveGame> savedGames = saveManager.GetSaveGames();
            if (savedGames.Empty())
                Components = Components.Add(new MenuSmallTextComponent("There are no saved games."));
            else
            {
                IEnumerable<IMenuComponent> saveRowComponents = CreateSaveRowComponents(savedGames);
                Components = Components.AddRange(saveRowComponents);
                
                SetToFirstActiveComponent();
            }
        }

        private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
        {
            return savedGames.Take(MaxRows)
                .Select(save =>
                {
                    string name = System.IO.Path.GetFileName(save.FileName);
                    return new MenuSaveRowComponent(name, () => null);
                });
        }
    }
}
