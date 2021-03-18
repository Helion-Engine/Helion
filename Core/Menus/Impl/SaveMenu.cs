using System;
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
        private const int MaxRows = 6;
        private const string HeaderImage = "M_SGTTL";

        public bool IsTypingName { get; private set; }

        public SaveMenu(Config config, HelionConsole console, SoundManager soundManager, 
            ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld) 
            : base(config, console, soundManager, archiveCollection, 16, true)
        {
            Components = Components.Add(new MenuImageComponent(HeaderImage, paddingY: 16));

            if (!hasWorld)
            {
                Components = Components.Add(new MenuSmallTextComponent("No game active to save."));
                return;
            }

            List<SaveGame> savedGames = saveManager.GetSaveGames();
            if (!savedGames.Empty())
            {
                IEnumerable<IMenuComponent> saveRowComponents = CreateSaveRowComponents(savedGames);
                Components = Components.AddRange(saveRowComponents);
            }

            if (savedGames.Count < MaxRows)
            {
                MenuSaveRowComponent saveRowComponent = new("Empty slot", CreateConsoleCommand("savegame"));
                Components = Components.Add(saveRowComponent);
            }
            
            SetToFirstActiveComponent();
        }

        private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
        {
            return savedGames.Take(MaxRows)
                .Select(save =>
                {
                    string displayName = save.Model?.Text ?? "Unknown";
                    string saveName = System.IO.Path.GetFileName(save.FileName);
                    return new MenuSaveRowComponent(displayName, CreateConsoleCommand($"savegame {saveName}"));
                });
        }

        private Func<Menu?> CreateConsoleCommand(string command)
        {
            return () =>
            {
                Console.SubmitInputText(command);
                return null;
            };
        }
    }
}
