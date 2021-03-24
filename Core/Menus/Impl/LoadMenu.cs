using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Layer;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Util;
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

        private readonly SaveGameManager m_saveGameManager;
        private SaveGame? m_deleteSave;
        
        public LoadMenu(Config config, HelionConsole console, SoundManager soundManager, 
            ArchiveCollection archiveCollection, SaveGameManager saveManager, 
            int topPixelPadding = 16, bool leftAlign = true) 
            : base(config, console, soundManager, archiveCollection, topPixelPadding, leftAlign)
        {
            m_saveGameManager = saveManager;
            Components = Components.Add(new MenuImageComponent(HeaderImage, paddingY: 16));

            List<SaveGame> savedGames = saveManager.GetMatchingSaveGames(saveManager.GetSaveGames(), archiveCollection).ToList();
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
                    string displayName = save.Model?.MapName ?? "Unknown";
                    string fileName = System.IO.Path.GetFileName(save.FileName);
                    return new MenuSaveRowComponent(displayName, CreateConsoleCommand($"loadgame {fileName}"),
                        CreateDeleteCommand(save), save);
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

        private Func<Menu?> CreateDeleteCommand(SaveGame saveGame)
        {
            return () =>
            {
                m_deleteSave = saveGame;
                MessageMenu confirm = new MessageMenu(Config, Console, SoundManager, ArchiveCollection,
                    new string[] { "Are you sure you want to delete this save?", "Press Y to confirm." },
                    isYesNoConfirm: true, clearMenus: false);
                confirm.Cleared += Confirm_Cleared;
                return confirm;
            };
        }

        private void Confirm_Cleared(object? sender, bool confirmed)
        {
            if (confirmed && m_deleteSave != null)
            {
                m_saveGameManager.DeleteSaveGame(m_deleteSave);
                if (ComponentIndex.HasValue)
                    RemoveComponent(Components[ComponentIndex.Value]);

                SoundManager.PlayStaticSound(Constants.MenuSounds.Choose);            
            }
        }
    }
}
