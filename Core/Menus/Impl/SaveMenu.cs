using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus.Base;
using Helion.Models;
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

        public readonly List<SaveGameRow> SavedGames;
        public int CurrentRow { get; private set; }
        public bool IsTypingName { get; private set; }

        public bool NoSavedGames => SavedGames.Empty();
        public SaveGameRow? CurrentSaveGameRow => SavedGames.Empty() ? null : SavedGames[CurrentRow];

        public SaveMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection,
            SaveGameManager saveManager, int topPixelPadding = 16, bool leftAlign = true) 
            : base(config, console, soundManager, archiveCollection, topPixelPadding, leftAlign)
        {
            SavedGames = saveManager.GetSaveGames().Select(s => new SaveGameRow(s))
                .Take(MaxRows)
                .ToList();
            
            Components = Components.Add(new MenuImageComponent(SaveGameImage, paddingY: 16));
            IEnumerable<IMenuComponent> saveRowComponents = CreateSaveRowComponents();
            Components = Components.AddRange(saveRowComponents);
            
            if (!saveRowComponents.Empty())
                SetToFirstActiveComponent();
        }

        private IEnumerable<IMenuComponent> CreateSaveRowComponents()
        {
            return SavedGames.Select(saveGameRow =>
                {
                    return new MenuSaveRowComponent(saveGameRow.Path, () => null);
                }).ToList();
        }

        public override void HandleInput(InputEvent input)
        {
            if (IsTypingName)
            {
                HandleNameTyping(input);
                input.ConsumeAll();
            }

            if (input.ConsumeKeyPressed(Key.Down))
                HandleMoveDown();
            if (input.ConsumeKeyPressed(Key.Up))
                HandleMoveUp();
            if (input.ConsumeKeyPressed(Key.Enter))
                HandleSelectRow();

            base.HandleInput(input);
        }

        private void HandleNameTyping(InputEvent input)
        {
            // TODO
            
            if (input.ConsumeKeyPressed(Key.Enter))
            {
                // TODO
            }
        }

        private void HandleMoveDown()
        {
            if (NoSavedGames)
                return;
            
            // TODO
        }

        private void HandleMoveUp()
        {
            if (NoSavedGames)
                return;
            
            // TODO
        }

        private void HandleSelectRow()
        {
            if (NoSavedGames)
                return;
            
            // TODO
        }
    }

    public record SaveGameRow
    {
        public readonly string Path;
        public readonly SaveGameModel? Model;

        internal SaveGameRow(SaveGame saveGame)
        {
            Path = System.IO.Path.GetFileName(saveGame.FileName);
            Model = saveGame.Model;
        }
    }
}
