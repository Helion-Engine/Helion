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
    public class LoadMenu : Menu
    {
        private const int MaxRows = 6;
        private const string HeaderImage = "M_LGTTL";
        
        public LoadMenu(Config config, HelionConsole console, SoundManager soundManager, 
            ArchiveCollection archiveCollection, SaveGameManager saveManager, 
            int topPixelPadding = 16, bool leftAlign = true) 
            : base(config, console, soundManager, archiveCollection, topPixelPadding, leftAlign)
        {
            Components = Components.Add(new MenuImageComponent(HeaderImage, paddingY: 16));

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
                    return new MenuSaveRowComponent(name, CreateConsoleCommand($"loadgame {name}"));
                });
        }

        private Func<Menu?> CreateConsoleCommand(string command)
        {
            return () =>
            {
                Console.ClearInputText();
                Console.AddInput(command);
                Console.SubmitInputText();
                return null;
            };
        }
    }
}
