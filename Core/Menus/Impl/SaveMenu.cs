using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Layer.Menus;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using Helion.World;
using Helion.World.Save;
using NLog;

namespace Helion.Menus.Impl;

public class SaveMenu : Menu
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MaxRows = 9;
    private const string SaveHeaderImage = "M_SGTTL";
    private const string LoadHeaderImage = "M_LGTTL";
    public const string SaveMessage = "Game saved.";

    public bool IsTypingName { get; private set; }

    private readonly MenuLayer m_parent;
    private readonly SaveGameManager m_saveGameManager;
    private readonly bool m_isSave;
    private readonly bool m_canSave;

    private SaveGame? m_deleteSave;

    public SaveMenu(MenuLayer parent, IConfig config, HelionConsole console, SoundManager soundManager,
        ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld, bool isSave, bool clearOnClose)
        : base(config, console, soundManager, archiveCollection, 8, true, clearOnClose: clearOnClose)
    {
        m_parent = parent;
        m_saveGameManager = saveManager;
        m_canSave = hasWorld;
        m_isSave = isSave;

        List<SaveGame> savedGames = saveManager.GetMatchingSaveGames(saveManager.GetSaveGames(saveManager.GetSaveDir()), archiveCollection).ToList();
        if (config.Game.UseSavedGameOrganizer)
        {
            string organizedDir = saveManager.GetOrganizedSaveDir(archiveCollection);

            if (SaveGameManager.EnsureDirectoryExists(organizedDir))
            {
                savedGames.AddRange(saveManager.GetMatchingSaveGames(saveManager.GetSaveGames(organizedDir), archiveCollection));
            }
        }

        if (isSave)
            CreateSaveRows(savedGames, hasWorld);
        else
            CreateLoadRows(savedGames);

        SetToFirstActiveComponent();
    }

    private void CreateLoadRows(List<SaveGame> savedGames)
    {
        Components = Components.Add(new MenuImageComponent(LoadHeaderImage));

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
        Components = Components.Add(new MenuImageComponent(SaveHeaderImage));

        if (m_isSave && !hasWorld)
        {
            string[] text = ArchiveCollection.Definitions.Language.GetMessages("$SAVEDEAD");
            for (int i = 0; i < text.Length; i++)
            {
                Components = Components.Add(new MenuSmallTextComponent(text[i]));
                if (i != text.Length - 1)
                    Components = Components.Add(new MenuPaddingComponent(8));
            }
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

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.Manager.HasAnyKeyPressed() && m_isSave && !m_canSave)
        {
            m_parent.Close();
            return;
        }

        if (input.ConsumeKeyPressed(Key.Enter) && ComponentIndex.HasValue)
            Components[ComponentIndex.Value].Action?.Invoke();
    }

    private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
    {
        return savedGames.Take(MaxRows)
            .Select(save =>
            {
                string displayName = save.Model?.Text ?? "Unknown";
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
                SaveGameEvent saveGameEvent = m_saveGameManager.WriteSaveGame(world, world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection), save);
                m_parent.Close();

                HandleSaveEvent(world, saveGameEvent);
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
                SaveGameEvent saveGameEvent = m_saveGameManager.WriteNewSaveGame(world, world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection));
                m_parent.Manager.Remove(m_parent);

                HandleSaveEvent(world, saveGameEvent);
            }
            else
            {
                Log.Error("Failed to get world for save game.");
            }

            return null;

        };
    }

    private static void HandleSaveEvent(IWorld world, SaveGameEvent saveGameEvent)
    {
        if (saveGameEvent.Success)
        {
            DisplayMessage(world, SaveMessage);
            return;
        }

        DisplayMessage(world, $"Failed to save {saveGameEvent.FileName}");
        if (saveGameEvent.Exception != null)
            throw saveGameEvent.Exception;
    }

    private bool GetWorld(out IWorld? world)
    {
        world = m_parent.Manager.WorldLayer?.World;
        return world != null;
    }

    private static void DisplayMessage(IWorld world, string message)
    {
        world.DisplayMessage(world.EntityManager.Players[0], null, message);
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
                string displayName = save.Model?.Text ?? "Unknown";
                string fileName = Path.GetFileName(save.FileName);
                string? saveDir = Path.GetDirectoryName(save.FileName);
                return new MenuSaveRowComponent(displayName, CreateConsoleCommand($"load {fileName} {saveDir}"),
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
