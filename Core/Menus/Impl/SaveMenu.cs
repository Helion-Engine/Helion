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
        private const string HeaderImage = "M_SGTTL";
        private const string SaveMessage = "Game saved.";

        public bool IsTypingName { get; private set; }

        private readonly GameLayer m_parent;
        private readonly SaveGameManager m_saveGameManager;

        public SaveMenu(GameLayer parent, Config config, HelionConsole console, SoundManager soundManager, 
            ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld) 
            : base(config, console, soundManager, archiveCollection, 16, true)
        {
            m_parent = parent;
            m_saveGameManager = saveManager;
            Components = Components.Add(new MenuImageComponent(HeaderImage, paddingY: 16));

            if (!hasWorld)
            {
                Components = Components.Add(new MenuSmallTextComponent("No game active to save."));
                return;
            }

            List<SaveGame> savedGames = saveManager.GetMatchingSaveGames(saveManager.GetSaveGames(), archiveCollection).ToList();
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
            
            SetToFirstActiveComponent();
        }

        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            if (input.ConsumeKeyPressed(Key.Enter) && ComponentIndex.HasValue)
            {
                Components[ComponentIndex.Value]?.Action();
            }
        }

        private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
        {
            return savedGames.Take(MaxRows)
                .Select(save =>
                {
                    string displayName = save.Model?.MapName ?? "Unknown";
                    return new MenuSaveRowComponent(displayName, UpdateSaveGame(save));
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
    }
}
