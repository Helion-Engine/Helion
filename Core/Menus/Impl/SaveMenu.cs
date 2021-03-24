using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Language;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Save;
using NLog;

namespace Helion.Menus.Impl
{
    public class SaveMenu : Menu
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const int MaxRows = 6;
        private const string SaveHeaderImage = "M_SGTTL";
        private const string LoadHeaderImage = "M_LGTTL";
        private const string SaveMessage = "Game saved.";

        public bool IsTypingName { get; private set; }

        private readonly GameLayer m_parent;
        private readonly SaveGameManager m_saveGameManager;
        private readonly bool m_isSave;
        private SaveGame? m_deleteSave;

        public SaveMenu(GameLayer parent, Config config, HelionConsole console, SoundManager soundManager, 
            ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld, bool isSave) 
            : base(config, console, soundManager, archiveCollection, 16, true)
        {
            m_parent = parent;
            m_saveGameManager = saveManager;
            m_isSave = isSave;

            List<SaveGame> savedGames = saveManager.GetMatchingSaveGames(saveManager.GetSaveGames(), archiveCollection).ToList();
            if (isSave)
                CreateSaveRows(savedGames, hasWorld);
            else
                CreateLoadRows(savedGames);

            SetToFirstActiveComponent();
        }

        private void CreateLoadRows(List<SaveGame> savedGames)
        {
            Components = Components.Add(new MenuImageComponent(LoadHeaderImage, paddingY: 16));

            if (savedGames.Empty())
            {
                SetNoSaveGames();
            }
            else
            {
                IEnumerable<IMenuComponent> saveRowComponents = CreateLoadRowComponents(savedGames);
                Components = Components.AddRange(saveRowComponents);
                SetToFirstActiveComponent();
            }
        }

        private void CreateSaveRows(List<SaveGame> savedGames, bool hasWorld)
        {
            Components = Components.Add(new MenuImageComponent(SaveHeaderImage, paddingY: 16));

            if (m_isSave && !hasWorld)
            {
                Components = Components.Add(new MenuSmallTextComponent("No game active to save."));
                return;
            }

            if (!savedGames.Empty())
            {
                IEnumerable<IMenuComponent> saveRowComponents = CreateSaveRowComponents(savedGames);
                Components = Components.AddRange(saveRowComponents);
            }

            if (savedGames.Count < MaxRows)
            {
                MenuSaveRowComponent saveRowComponent = new("Empty slot", CreateNewSaveGame());
                Components = Components.Add(saveRowComponent);
            }
        }

        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            if (input.ConsumeKeyPressed(Key.Enter) && ComponentIndex.HasValue)
            {
                var action = Components[ComponentIndex.Value].Action;
                if (action != null)
                    action();
            }
        }

        private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
        {
            return savedGames.Take(MaxRows)
                .Select(save =>
                {
                    string displayName = save.Model?.MapName ?? "Unknown";
                    return new MenuSaveRowComponent(displayName, UpdateSaveGame(save), CreateDeleteCommand(save));
                });
        }

        private Func<Menu?> UpdateSaveGame(SaveGame save)
        {
            return () =>
            {
                if (save.Model == null)
                {
                    Log.Error("Invalid save game.");
                    return null;
                }    

                if (GetWorld(out IWorld? world) && world != null)
                {
                    m_saveGameManager.WriteSaveGame(world, save.Model.Text, save);
                    m_parent.Remove<MenuLayer>();
                    DisplayMessage(world, SaveMessage);
                }
                else
                {
                    Log.Error("Failed to get world for save game.");
                }

                return null;
            };
        }

        private Func<Menu?> CreateNewSaveGame()
        {  
            return () =>
            {
                if (GetWorld(out IWorld? world) && world != null)
                {
                    m_saveGameManager.WriteNewSaveGame(world, "new");
                    m_parent.Remove<MenuLayer>();
                    DisplayMessage(world, SaveMessage);
                }
                else
                {
                    Log.Error("Failed to get world for save game.");
                }

                return null;
            };
        }

        private bool GetWorld(out IWorld? world)
        {
            if (m_parent.TryGetLayer(out SinglePlayerWorldLayer? worldLayer))
            {
                world = worldLayer.World;
                return true;
            }

            world = null;
            return false;
        }

        private static void DisplayMessage(IWorld world, string message)
        {
            world.DisplayMessage(world.EntityManager.Players[0], null, message, LanguageMessageType.None);
        }

        private void SetNoSaveGames()
        {
            Components = Components.Add(new MenuSmallTextComponent("There are no saved games."));
        }

        private IEnumerable<IMenuComponent> CreateLoadRowComponents(IEnumerable<SaveGame> savedGames)
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

                if (!Components.Any(x => x is MenuSaveRowComponent))
                    SetNoSaveGames();

                SoundManager.PlayStaticSound(Constants.MenuSounds.Choose);
            }
        }
    }
}
