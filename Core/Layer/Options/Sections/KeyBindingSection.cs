using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class KeyBindingSection : IOptionSection
{
    private const string Font = Fonts.SmallGray;

    struct CommandKeys
    {
        public readonly string Command;
        public readonly string Name;
        public readonly List<Key> Keys;

        public CommandKeys(string command, string name)
        {
            Command = command;
            Name = name;
            Keys = new List<Key>();
        }

        public CommandKeys(string command, string name, List<Key> keys)
        {
            Command = command;
            Name = name;
            Keys = keys;
        }
    }

    private static readonly Dictionary<string, string> RemapNames = new()
    {
        { Constants.Input.AutoMapIncrease, "AutoMap Zoom In" },
        { Constants.Input.AutoMapDecrease, "AutoMap Zoom Out" },
        { Constants.Input.AutoMapUp, "AutoMap Up" },
        { Constants.Input.AutoMapDown, "AutoMap Down" },
        { Constants.Input.AutoMapLeft, "AutoMap Left" },
        { Constants.Input.AutoMapRight, "AutoMap Right" },
        { Constants.Input.AutoMapAddMarker, "AutoMap Add Marker" },
        { Constants.Input.AutoMapRemoveNearbyMarkers, "AutoMap Remove Nearby" },
        { Constants.Input.AutoMapClearAllMarkers, "AutoMap Clear All" },
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public event EventHandler<LockEvent>? OnLockChanged;
    public event EventHandler<RowEvent>? OnRowChanged;
    public event EventHandler<string>? OnError;

    public OptionSectionType OptionType => OptionSectionType.Keys;
    public bool CurrentlyBinding => m_updatingKeyBinding;
    private readonly Key[] AllKeys = Enum.GetValues<Key>();
    private readonly IConfig m_config;
    private readonly BoxList m_menuPositionList = new();
    private readonly SoundManager m_soundManager;
    private readonly List<CommandKeys> m_commandToKeys = new();
    private readonly HashSet<string> m_mappedCommands = new();
    private readonly HashSet<string> m_allCommands;
    private readonly StringBuilder m_builder = new();
    private Vec2I m_mousePos;
    private int m_renderHeight;
    private (int, int) m_selectedRender;
    private int m_currentRow;
    private int m_lastY;
    private bool m_updatingKeyBinding;
    private bool m_updateRow;
    private bool m_updateMouse;
    private bool m_configUpdated;

    public KeyBindingSection(IConfig config, SoundManager soundManager)
    {
        m_config = config;
        m_soundManager = soundManager;
        m_allCommands = GetAllCommandNames();
        m_configUpdated = true;
    }

    public void OnShow()
    {
        OnRowChanged?.Invoke(this, new(m_currentRow, string.Empty));
        m_configUpdated = true;
    }

    public void ResetSelection() => m_currentRow = 0;

    public bool OnClickableItem(Vec2I mousePosition) =>
        m_menuPositionList.GetIndex(mousePosition, out _);

    private static HashSet<string> GetAllCommandNames()
    {
        HashSet<string> commandNames = new();

        foreach (FieldInfo fieldInfo in typeof(Input).GetFields())
        {
            if (fieldInfo is not { IsPublic: true, IsStatic: true })
                continue;

            if (fieldInfo.GetValue(null) is not string commandName)
            {
                Log.Error($"Unable to get constant command name field '{fieldInfo.Name}' for options menu, should never happen");
                continue;
            }

            commandNames.Add(commandName);
        }

        return commandNames;
    }

    private void MapCommands()
    {
        m_commandToKeys.Clear();
        m_mappedCommands.Clear();

        // These should always exist, and should report being unbound if not by
        // having an empty list of keys assigned to it.
        foreach (string command in BaseCommands)
            MapCommand(command);

        // Search for items that were bound by the user
        foreach ((_, string command) in m_config.Keys.GetKeyMapping())
            MapCommand(command);
    }

    private void MapCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || m_mappedCommands.Contains(command))
            return;

        if (!RemapNames.TryGetValue(command, out var name))
            name = command.WithWordSpaces(m_builder);

        m_commandToKeys.Add(new(command, name));
        m_mappedCommands.Add(command);
    }

    private void CheckForConfigUpdates()
    {
        if (!m_configUpdated)
            return;

        m_configUpdated = false;
        MapCommands();
        foreach ((Key key, string command) in m_config.Keys.GetKeyMapping())
        {
            List<Key> keys;
            string name = command.WithWordSpaces(m_builder);

            if (!m_mappedCommands.Contains(command))
                continue;

            int index = 0;
            for (; index < m_commandToKeys.Count; index++)
                if (command == m_commandToKeys[index].Command)
                    break;

            if (index != m_commandToKeys.Count)
            {
                keys = m_commandToKeys[index].Keys;
            }
            else
            {
                keys = new();
                m_commandToKeys.Add(new(command, name, keys));
                m_mappedCommands.Add(command);
            }

            if (!keys.Contains(key))
                keys.Add(key);
        }

        foreach (string command in m_allCommands.Where(cmd => !m_mappedCommands.Contains(cmd)))
        {
            string name = command.WithWordSpaces(m_builder);
            m_commandToKeys.Add(new(command, name));
            m_mappedCommands.Add(command);
        }
    }


    private void TryUpdateKeyBindingsFromPress(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(MenuSounds.Clear);
            // We won't ever set Escape, and instead abort from setting.
        }
        else
        {
            if (TryConsumeAnyKey(input, out var key))
            {
                var commandKeys = m_commandToKeys[m_currentRow];
                if (!commandKeys.Keys.Contains(key))
                {
                    m_configUpdated = true;
                    m_config.Keys.Add(key, commandKeys.Command);
                    commandKeys.Keys.Add(key);
                    m_soundManager.PlayStaticSound(MenuSounds.Choose);
                }

                CommandKeys? unbound = null;
                foreach (var item in m_commandToKeys)
                {
                    for (int i = 0; i < item.Keys.Count; i++)
                    {
                        if (item.Keys[i] != key || item.Command.EqualsIgnoreCase(commandKeys.Command))
                            continue;

                        // Don't unbind if key is in automap section and this key is not
                        if (item.Command.StartsWith("AutoMap", StringComparison.OrdinalIgnoreCase) !=
                            commandKeys.Command.StartsWith("AutoMap", StringComparison.OrdinalIgnoreCase))
                            continue;

                        unbound = item;
                        m_config.Keys.Remove(key, item.Command);
                        item.Keys.RemoveAt(i);
                        i--;
                    }
                }

                if (unbound != null)
                    OnError?.Invoke(this, $"{key.ToString()} was unbound from {unbound.Value.Command}");

                WriteConfigFile();
            }
        }

        m_updatingKeyBinding = false;
        OnLockChanged?.Invoke(this, new(Lock.Unlocked));
    }

    private void WriteConfigFile()
    {
        if (m_config is FileConfig fileConfig)
            fileConfig.Write();
    }

    private bool TryConsumeAnyKey(IConsumableInput input, out Key key)
    {
        for (int i = 0; i < AllKeys.Length; i++)
        {
            key = AllKeys[i];
            if (input.ConsumeKeyPressed(key))
                return true;
        }

        int scroll = input.ConsumeScroll();
        if (scroll != 0)
        {
            key = scroll < 0 ? Key.MouseWheelDown : Key.MouseWheelUp;
            return true;
        }

        key = Key.Unknown;
        return false;
    }

    private void UnbindCurrentRow()
    {
        m_configUpdated = true;
        var commandKeys = m_commandToKeys[m_currentRow];
        foreach (Key key in commandKeys.Keys)
            m_config.Keys.Remove(key, commandKeys.Command);

        // Clear our local cache, which will be updated anyways but this allows
        // for instant rendering feedback. We don't remove the row because we
        // can then render "No binding" in its place.
        commandKeys.Keys.Clear();
        WriteConfigFile();
    }

    private void ResetCurrentRowDefault()
    {
        m_config.Keys.ReloadDefaults(m_commandToKeys[m_currentRow].Command);
        WriteConfigFile();
        m_configUpdated = true;
    }

    public void HandleInput(IConsumableInput input)
    {
        CheckForConfigUpdates();

        if (m_commandToKeys.Count == 0)
            return;

        if (m_updatingKeyBinding)
        {
            if (input.HasAnyKeyPressed() || input.Scroll != 0)
                TryUpdateKeyBindingsFromPress(input);

            input.ConsumeAll();
        }
        else
        {
            int lastRow = m_currentRow;
            if (m_mousePos != input.Manager.MousePosition || m_updateMouse)
            {
                m_updateMouse = false;
                m_mousePos = input.Manager.MousePosition;
                if (m_menuPositionList.GetIndex(m_mousePos, out int rowIndex))
                    m_currentRow = rowIndex;
            }

            if (input.ConsumePressOrContinuousHold(Key.Up))
            {
                m_soundManager.PlayStaticSound(MenuSounds.Cursor);
                m_currentRow = m_currentRow > 0 ? (m_currentRow - 1) : m_commandToKeys.Count;
            }
            if (input.ConsumePressOrContinuousHold(Key.Down))
            {
                m_soundManager.PlayStaticSound(MenuSounds.Cursor);
                m_currentRow = (m_currentRow + 1) % (m_commandToKeys.Count + 1);
            }

            bool mousePress = input.ConsumeKeyPressed(Key.MouseLeft);
            if (mousePress || input.ConsumeKeyPressed(Key.Enter))
            {
                if (mousePress)
                {
                    if (!m_menuPositionList.GetIndex(input.Manager.MousePosition, out int rowIndex))
                        return;
                    m_currentRow = rowIndex;
                }

                if (m_currentRow == m_commandToKeys.Count)
                {
                    ResetAllKeyBindings();
                    return;
                }

                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                m_updatingKeyBinding = true;
                OnLockChanged?.Invoke(this, new(Lock.Locked, "Press any key/button to add binding. Escape to cancel."));
            }

            if (input.ConsumeKeyPressed(Key.Delete) && m_currentRow < m_commandToKeys.Count)
            {
                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                UnbindCurrentRow();
            }

            if (input.ConsumeKeyPressed(Key.R) && m_currentRow < m_commandToKeys.Count)
            {
                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                ResetCurrentRowDefault();
            }

            if (m_currentRow != lastRow)
                m_updateRow = true;
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY, bool didMouseWheelScroll)
    {
        m_menuPositionList.Clear();

        if (didMouseWheelScroll && startY != m_lastY)
        {
            m_lastY = startY;
            m_updateMouse = true;
        }

        int fontSize = m_config.Window.GetMenuSmallFontSize();
        hud.Text("Key Bindings", Font, m_config.Window.GetMenuLargeFontSize(), (0, startY), out Dimension headerArea,
            both: Align.TopMiddle, color: Color.Red);
        int y = startY + headerArea.Height + m_config.Window.GetMenuScaled(8);
        int xOffset = m_config.Window.GetMenuScaled(4) * 2;
        int smallPad = m_config.Window.GetMenuScaled(1);

        hud.Text("Scroll with the mouse wheel or hold up/down arrows", Font, fontSize, (0, y), out Dimension scrollArea,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += scrollArea.Height + smallPad;

        hud.Text("Press delete to clear all bindings", Font, fontSize, (0, y), out Dimension instructionArea,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += instructionArea.Height + smallPad;

        hud.Text("Press R to reset bindings", Font, fontSize, (0, y), out Dimension clearArea,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += clearArea.Height + smallPad;

        hud.Text("Press enter to start binding and press a key or button", Font, fontSize, (0, y), out Dimension enterArea,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += enterArea.Height + m_config.Window.GetMenuScaled(12);

        for (int cmdIndex = 0; cmdIndex < m_commandToKeys.Count; cmdIndex++)
        {
            var commandKeys = m_commandToKeys[cmdIndex];

            Dimension commandArea;
            if (cmdIndex == m_currentRow && m_updatingKeyBinding)
            {
                hud.Text(commandKeys.Name, Font, fontSize, (-xOffset, y), out commandArea,
                    window: Align.TopMiddle, anchor: Align.TopRight, color: Color.Yellow);
            }
            else
            {
                hud.Text(commandKeys.Name, Font, fontSize, (-xOffset, y), out commandArea,
                    window: Align.TopMiddle, anchor: Align.TopRight, color: Color.Red);
            }

            if (cmdIndex == m_currentRow)
                m_selectedRender = (y - startY, y + commandArea.Height - startY);

            if (cmdIndex == m_currentRow && !m_updatingKeyBinding)
            {
                var arrowSize = hud.MeasureText("<", Font, fontSize);
                Vec2I arrowLeft = (-xOffset - commandArea.Width - m_config.Window.GetMenuScaled(2), y);
                hud.Text(">", Font, fontSize, arrowLeft, window: Align.TopMiddle,
                    anchor: Align.TopRight, color: Color.White);
                Vec2I arrowRight = (-xOffset + arrowSize.Width + m_config.Window.GetMenuScaled(2), y);
                hud.Text("<", Font, fontSize, arrowRight, window: Align.TopMiddle,
                    anchor: Align.TopRight, color: Color.White);
            }

            if (commandKeys.Keys.Empty())
            {
                hud.Text("No binding", Font, fontSize, (xOffset, y), out Dimension noBindingArea,
                    window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Gray);

                int rowHeight = Math.Max(noBindingArea.Height, commandArea.Height);
                var rowDimensions = new Box2I((0, y), (hud.Dimension.Width, y + rowHeight));
                m_menuPositionList.Add(rowDimensions, cmdIndex);
                y += Math.Max(noBindingArea.Height, commandArea.Height);
            }
            else
            {
                Dimension totalKeyArea = (0, 0);
                for (int keyIndex = 0; keyIndex < commandKeys.Keys.Count; keyIndex++)
                {
                    Key key = commandKeys.Keys[keyIndex];
                    hud.Text(key.ToString(), Font, fontSize, (xOffset + totalKeyArea.Width, y),
                        out Dimension keyArea,
                        window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.White);
                    totalKeyArea.Width += keyArea.Width;

                    if (keyIndex != commandKeys.Keys.Count - 1)
                    {
                        hud.Text(", ", Font, fontSize, (xOffset + totalKeyArea.Width, y),
                            out Dimension commaArea,
                            window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Red);
                        totalKeyArea.Width += commaArea.Width;
                    }
                }

                int rowHeight = Math.Max(totalKeyArea.Height, commandArea.Height);
                var rowDimensions = new Box2I((0, y), (hud.Dimension.Width, y + rowHeight));
                m_menuPositionList.Add(rowDimensions, cmdIndex);

                y += rowHeight;
            }
        }

        string resetText = m_currentRow != m_commandToKeys.Count
            ? "Reload All Defaults"
            : "> Reload All Defaults <";
        hud.Text(resetText, Font, fontSize, (0, y), out Dimension area, window: Align.TopMiddle, anchor: Align.TopMiddle, color: Color.Yellow);
        m_menuPositionList.Add(new Box2I((0, y), (hud.Dimension.Width, y + area.Height)), m_commandToKeys.Count);
        y += area.Height + m_config.Window.GetMenuScaled(3);

        // Handle case where the "Reset" row is selected; this affects auto-scrolling.
        if (m_currentRow == m_commandToKeys.Count)
            m_selectedRender = (y - startY, y + area.Height - startY);

        m_renderHeight = y - startY;

        if (m_updateRow)
        {
            OnRowChanged?.Invoke(this, new(m_currentRow, string.Empty));
            m_updateRow = false;
        }
    }

    private void ResetAllKeyBindings()
    {
        m_config.Keys.SetInitialDefaultKeyBindings();
        m_configUpdated = true;
        WriteConfigFile();
    }

    public int GetRenderHeight() => m_renderHeight;
    public (int, int) GetSelectedRenderY() => m_selectedRender;
    public void SetToFirstSelection() => m_currentRow = 0;
    public void SetToLastSelection() => m_currentRow = m_commandToKeys.Count - 1;
}
