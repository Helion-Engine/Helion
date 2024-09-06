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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Menus.Impl;

public class SaveMenu : Menu
{
    public const string SaveMessage = "Game saved.";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MaxRows = 9;
    private const string SaveHeaderImage = "M_SGTTL";
    private const string LoadHeaderImage = "M_LGTTL";
    private const string UnknownSavedGameName = "Unknown";
    private const string EmptySlot = "Empty slot";

    public bool IsTypingName { get; private set; }

    private readonly MenuLayer m_parent;
    private readonly SaveGameManager m_saveGameManager;
    private readonly bool m_isSave;
    private readonly bool m_canSave;

    private bool m_hasRowLock;
    private string m_previousDisplayName = string.Empty;
    private string m_defaultSavedGameName = string.Empty;
    private StringBuilder m_customNameBuilder = new StringBuilder();

    private SaveGame? m_deleteSave;

    public SaveMenu(MenuLayer parent, IConfig config, HelionConsole console, SoundManager soundManager,
        ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld, bool isSave, bool clearOnClose)
        : base(config, console, soundManager, archiveCollection, 8, true, clearOnClose: clearOnClose)
    {
        m_parent = parent;
        m_saveGameManager = saveManager;
        m_canSave = hasWorld;
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
            MenuSaveRowComponent saveRowComponent = new(EmptySlot, string.Empty, isAutoSave: false);
            saveRowComponent.Action = CreateNewSaveGame(() => saveRowComponent.Text);
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

        if (ComponentIndex.HasValue)
        {
            MenuSaveRowComponent? savedGameRow = Components[ComponentIndex.Value] as MenuSaveRowComponent;
            if (savedGameRow != null)
            {
                if (m_isSave)
                {
                    if (m_hasRowLock)
                    {
                        EditRow(savedGameRow, input);
                    }
                    else if (input.ConsumeKeyPressed(Key.Enter) && !savedGameRow.IsAutoSave)
                    {
                        m_customNameBuilder.Clear();
                        m_previousDisplayName = savedGameRow.Text;

                        m_defaultSavedGameName = (GetWorld(out IWorld? world) && world != null)
                            ? world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection)
                            : UnknownSavedGameName;

                        if (savedGameRow.Text == EmptySlot || savedGameRow.Text == savedGameRow.MapName)
                        {
                            // New saved game, or saved game with default (map) name; update to current map name
                            m_customNameBuilder.Append(m_defaultSavedGameName);
                            savedGameRow.Text = m_defaultSavedGameName;
                        }
                        else
                        {
                            // saved game with non-default name; preserve name
                            m_customNameBuilder.Append(savedGameRow.Text);
                        }

                        m_hasRowLock = true;
                        SoundManager.PlayStaticSound(Constants.MenuSounds.Choose);
                    }
                }
                else if (!m_isSave && input.ConsumeKeyPressed(Key.Enter)) // Load
                {
                    savedGameRow.Action?.Invoke();
                }
            }
        }
    }

    public void EditRow(MenuSaveRowComponent savedGameRow, IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            // The user has decided not to save.
            // Undo any customizations they've made to the display name of the saved game, and leave edit mode.
            savedGameRow.Text = m_previousDisplayName;
            m_hasRowLock = false;
            SoundManager.PlayStaticSound(Constants.MenuSounds.Backup);
        }
        else if (input.ConsumeKeyPressed(Key.Enter))
        {
            // If there's any text in the field, use that as the name, else force the defualt.
            savedGameRow.Text = m_customNameBuilder.Length > 0
                ? m_customNameBuilder.ToString()
                : m_defaultSavedGameName;

            savedGameRow.Action?.Invoke();
            m_hasRowLock = false;
        }
        else
        {
            // Handle all other typed input.
            if (input.ConsumeKeyPressed(Key.Backspace))
            {
                if (m_customNameBuilder.ToString() == m_defaultSavedGameName)
                {
                    m_customNameBuilder.Clear();
                }

                if (m_customNameBuilder.Length > 0)
                {
                    m_customNameBuilder.Remove(m_customNameBuilder.Length - 1, 1);
                }
            }

            var chars = input.ConsumeTypedCharacters();
            m_customNameBuilder.Append(chars);

            savedGameRow.Text = m_customNameBuilder.ToString() + Blink();
        }

        // Ensure we have no remaining input whatsoever.
        input.ConsumeAll();
    }

    private static string Blink()
    {
        const string editStr = "_";
        if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 500 % 2 == 0)
        {
            return editStr;
        }
        return string.Empty;
    }

    private IEnumerable<IMenuComponent> CreateSaveRowComponents(IEnumerable<SaveGame> savedGames)
    {
        return savedGames.Take(MaxRows)
            .Select(save =>
            {
                string displayName = save.Model?.Text ?? UnknownSavedGameName;
                MenuSaveRowComponent saveRow = new(displayName, save.Model?.MapName ?? UnknownSavedGameName, save.IsAutoSave, null, CreateDeleteCommand(save));
                saveRow.Action = new Func<Menu?>(UpdateSaveGame(save, new(() => saveRow.Text)));
                return saveRow;
            });
    }

    private Func<Menu?> UpdateSaveGame(SaveGame save, Func<string> getName)
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
                SaveGameEvent saveGameEvent = m_saveGameManager.WriteSaveGame(world, getName(), save);
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

    private Func<Menu?> CreateNewSaveGame(Func<string> getName)
    {
        return () =>
        {
            if (GetWorld(out IWorld? world) && world != null)
            {
                SaveGameEvent saveGameEvent = m_saveGameManager.WriteNewSaveGame(world, getName());
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
                string displayName = save.Model?.Text ?? UnknownSavedGameName;
                string fileName = System.IO.Path.GetFileName(save.FileName);
                return new MenuSaveRowComponent(displayName, string.Empty, save.IsAutoSave, CreateConsoleCommand($"load {fileName}"),
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
