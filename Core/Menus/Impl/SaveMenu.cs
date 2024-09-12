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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Helion.Menus.Impl;

public class SaveMenu : Menu
{
    public const string SaveMessage = "Game saved.";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int RowsPerPage = 8;
    private const string SaveHeaderImage = "M_SGTTL";
    private const string LoadHeaderImage = "M_LGTTL";
    private const string UnknownSavedGameName = "Unknown";
    private const string EmptySlotText = "Empty slot";
    private const string NoSavedGamesText = "There are no saved games.";
    private static readonly string[] DeleteConfirmationText = ["Are you sure you want to delete this save?", "Press Y to confirm."];

    public bool IsTypingName { get; private set; }

    private readonly MenuLayer m_parent;
    private readonly SaveGameManager m_saveGameManager;
    private readonly List<SaveGame> m_saveGames;
    private int m_currentPage = 1;
    private readonly bool m_isSave;
    private readonly bool m_canSave;

    private bool m_hasRowLock;
    private string m_previousDisplayName = string.Empty;
    private string m_defaultSavedGameName = string.Empty;
    private readonly StringBuilder m_customNameBuilder = new();
    private readonly Stopwatch m_tickStopwatch = new();

    private SaveGame? m_deleteSave;

    public SaveMenu(MenuLayer parent, IConfig config, HelionConsole console, SoundManager soundManager,
        ArchiveCollection archiveCollection, SaveGameManager saveManager, bool hasWorld, bool isSave, bool clearOnClose)
        : base(config, console, soundManager, archiveCollection, 8, true, clearOnClose: clearOnClose)
    {
        m_parent = parent;
        m_saveGameManager = saveManager;
        m_canSave = hasWorld;
        m_isSave = isSave;

        m_saveGames = saveManager.GetMatchingSaveGames(saveManager.GetSaveGames()).ToList();
        UpdateMenuComponents(setTop: true);
    }

    private int GetPageCount()
    {
        // add a single row to the first page when saving
        int rowCount = m_isSave
            ? m_saveGames.Count + 1
            : m_saveGames.Count;
        return (int)Math.Ceiling(rowCount * 1d / RowsPerPage);
    }

    private IEnumerable<SaveGame> GetCurrentPageSaveGames()
    {
        if (m_isSave)
        {
            // add a single row to the first page when saving
            return m_saveGames
                .Skip(RowsPerPage * (m_currentPage - 1) - 1)
                .Take(m_currentPage == 1 ? RowsPerPage - 1 : RowsPerPage);
        }
        else
        {
            return m_saveGames
                .Skip(RowsPerPage * (m_currentPage - 1))
                .Take(RowsPerPage);
        }
    }

    private readonly IMenuComponent SaveHeader = new MenuImageComponent(SaveHeaderImage);
    private readonly IMenuComponent LoadHeader = new MenuImageComponent(LoadHeaderImage);
    private readonly ImmutableArray<IMenuComponent> NoSavedGamesComponents = [
        new MenuPaddingComponent(8),
        new MenuSmallTextComponent(NoSavedGamesText)
    ];

    private List<IMenuComponent> GetPaginationFooter() => [
        new MenuPaddingComponent(5),
        new MenuSmallTextComponent($"<- Page {m_currentPage}/{GetPageCount()} ->")
    ];

    /// <summary>
    /// Updates the menu components on init or after a delete or page change.
    /// </summary>
    private void UpdateMenuComponents(bool setTop = false, bool setBottom = false)
    {
        var newComponents = (m_isSave)
            ? GenerateSaveMenuComponents()
            : GenerateLoadMenuComponents();
        Components = [.. newComponents];

        if (setTop)
            SetToFirstActiveComponent();
        if (setBottom)
            SetToLastActiveComponent();
        // try to preserve index when deleting or changing page
        else if (ComponentIndex.HasValue)
        {
            while (ComponentIndex >= 0 && (ComponentIndex >= Components.Count || !Components[ComponentIndex.Value].HasAction))
                ComponentIndex--;
            if (ComponentIndex < 0)
                ComponentIndex = null;
        }
    }

    private List<IMenuComponent> GenerateSaveMenuComponents()
    {
        List<IMenuComponent> newComponents = [SaveHeader];

        if (m_isSave && !m_canSave)
        {
            newComponents.Add(new MenuPaddingComponent(8));
            string[] text = ArchiveCollection.Definitions.Language.GetMessages("$SAVEDEAD");
            for (int i = 0; i < text.Length; i++)
            {
                newComponents.Add(new MenuSmallTextComponent(text[i]));
                if (i != text.Length - 1)
                    newComponents.Add(new MenuPaddingComponent(8));
            }
        }
        else
        {
            // show empty slot on page 1
            if (m_currentPage == 1)
            {
                MenuSaveRowComponent saveRowComponent = new(EmptySlotText, string.Empty, false);
                saveRowComponent.Action = CreateNewSaveGame(() => saveRowComponent.Text);
                newComponents.Add(saveRowComponent);
            }
            var saveRowComponents = GetCurrentPageSaveGames().Select(save =>
            {
                string displayName = save.Model?.Text ?? UnknownSavedGameName;
                string mapName = save.Model?.MapName ?? UnknownSavedGameName;
                MenuSaveRowComponent saveRow = new(displayName, mapName, save.IsAutoSave || save.IsQuickSave,
                    null, CreateDeleteCommand(save));
                saveRow.Action = new Func<Menu?>(UpdateSaveGame(save, new(() => saveRow.Text)));
                return saveRow;
            });
            newComponents.AddRange(saveRowComponents);
            if (GetPageCount() > 1)
                newComponents.AddRange(GetPaginationFooter());
        }

        return newComponents;
    }

    private List<IMenuComponent> GenerateLoadMenuComponents()
    {
        List<IMenuComponent> newComponents = [LoadHeader];

        if (m_saveGames.Empty())
            newComponents.AddRange(NoSavedGamesComponents);
        else
        {
            var saveRowComponents = GetCurrentPageSaveGames().Select(save =>
            {
                string displayName = save.Model?.Text ?? UnknownSavedGameName;
                string fileName = System.IO.Path.GetFileName(save.FileName);
                return new MenuSaveRowComponent(displayName, string.Empty, save.IsAutoSave || save.IsQuickSave,
                    CreateConsoleCommand($"load \"{fileName}\""), CreateDeleteCommand(save), save);
            });
            newComponents.AddRange(saveRowComponents);
            if (GetPageCount() > 1)
                newComponents.AddRange(GetPaginationFooter());
        }

        return newComponents;
    }


    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.Manager.HasAnyKeyPressed() && m_isSave && !m_canSave)
        {
            m_parent.Close();
            return;
        }

        if (ComponentIndex.HasValue && Components[ComponentIndex.Value] is MenuSaveRowComponent savedGameRow)
        {
            if (m_isSave)
            {
                if (m_hasRowLock)
                {
                    // We're already in "name edit mode"
                    EditRow(savedGameRow, input);
                }
                else if (input.ConsumeKeyPressed(Key.Enter))
                {
                    if (savedGameRow.IsAutoOrQuickSave)
                    {
                        SoundManager.PlayStaticSound(Constants.MenuSounds.Invalid);
                    }
                    else
                    {
                        m_customNameBuilder.Clear();
                        m_previousDisplayName = savedGameRow.Text;

                        m_defaultSavedGameName = (GetWorld(out IWorld? world) && world != null)
                            ? world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection)
                            : UnknownSavedGameName;

                        if (savedGameRow.Text == EmptySlotText || savedGameRow.Text == savedGameRow.MapName)
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
                        m_tickStopwatch.Restart();
                        SoundManager.PlayStaticSound(Constants.MenuSounds.Choose);
                    }
                }
                else
                {
                    ConsumeAndHandlePageChange(input);
                }
            }
            // load screen
            else
            {
                if (input.ConsumeKeyPressed(Key.Enter)) // Load
                    savedGameRow.Action?.Invoke();
                else
                    ConsumeAndHandlePageChange(input);
            }
        }
    }

    private void ConsumeAndHandlePageChange(IConsumableInput input)
    {
        bool changed = false;

        if (input.ConsumeKeyPressed(Key.Left) || input.ConsumeKeyPressed(Key.PageUp))
        {
            m_currentPage--;
            if (m_currentPage < 1)
                m_currentPage = GetPageCount();
            changed = true;
        }
        else if (input.ConsumeKeyPressed(Key.Right) || input.ConsumeKeyPressed(Key.PageDown))
        {
            m_currentPage++;
            if (m_currentPage > GetPageCount())
                m_currentPage = 1;
            changed = true;
        }

        if (changed)
            UpdateMenuComponents();
    }

    public void EditRow(MenuSaveRowComponent savedGameRow, IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            // The user has decided not to save.
            // Undo any customizations they've made to the display name of the saved game, and leave edit mode.
            savedGameRow.Text = m_previousDisplayName;
            m_hasRowLock = false;
            m_tickStopwatch.Stop();
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
            m_tickStopwatch.Stop();
        }
        else
        {
            // Handle all other typed input.
            if (input.ConsumePressOrContinuousHold(Key.Backspace))
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

    private string Blink()
    {
        const string editStr = "_";
        if (m_tickStopwatch.ElapsedMilliseconds / 500 % 2 == 0)
        {
            return editStr;
        }
        return string.Empty;
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
            MessageMenu confirm = new(Config, Console, SoundManager, ArchiveCollection,
                DeleteConfirmationText, isYesNoConfirm: true, clearMenus: false);
            confirm.Cleared += Confirm_Cleared;
            return confirm;
        };
    }

    private void Confirm_Cleared(object? sender, bool confirmed)
    {
        if (confirmed && m_deleteSave != null)
        {
            m_saveGameManager.DeleteSaveGame(m_deleteSave);
            m_saveGames.Remove(m_deleteSave);

            // move to the previous page if this one is going away
            int newPageCount = GetPageCount();
            if (m_currentPage > newPageCount)
            {
                m_currentPage = newPageCount;
                UpdateMenuComponents(setBottom: true);
            }
            else
            {
                UpdateMenuComponents();
            }
            SoundManager.PlayStaticSound(Constants.MenuSounds.Choose);
        }
    }
}
