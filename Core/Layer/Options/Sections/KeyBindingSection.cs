using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class KeyBindingSection : IOptionSection
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public event EventHandler<LockEvent>? OnLockChanged;
    public event EventHandler<RowEvent>? OnRowChanged;

    public OptionSectionType OptionType => OptionSectionType.Keys;
    private readonly IConfig m_config;
    private readonly BoxList m_menuPositionList = new();
    private readonly SoundManager m_soundManager;
    private readonly List<(string Command, List<Key> Keys)> m_commandToKeys = new();
    private readonly HashSet<string> m_mappedCommands = new();
    private readonly HashSet<string> m_allCommands;
    private Vec2I m_mousePos;
    private int m_renderHeight;
    private (int, int) m_selectedRender;
    private int m_currentRow;
    private int m_lastY;
    private bool m_updatingKeyBinding;
    private bool m_updateRow;
    private bool m_updateMouse;

    public KeyBindingSection(IConfig config, SoundManager soundManager)
    {
        m_config = config;
        m_soundManager = soundManager;
        m_allCommands = GetAllCommandNames();
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

            commandNames.Add(commandName.WithWordSpaces());
        }

        return commandNames;
    }

    private void CheckForConfigUpdates()
    {
        // This is anti-perf when we re-create everything all the time :(
        m_commandToKeys.Clear();
        m_mappedCommands.Clear();
        
        // These should always exist, and should report being unbound if not by
        // having an empty list of keys assigned to it.
        foreach (string command in InGameCommands)
        {
            string cmd = command.WithWordSpaces();
            if (m_mappedCommands.Contains(cmd)) 
                continue;
            
            m_commandToKeys.Add((cmd, new()));
            m_mappedCommands.Add(cmd);
        }
        
        foreach ((Key key, string command) in m_config.Keys.GetKeyMapping())
        {
            List<Key> keys;
            string cmd = command.WithWordSpaces();
            
            if (!m_mappedCommands.Contains(cmd))
            {
                keys = new();
                m_commandToKeys.Add((cmd, keys));
                m_mappedCommands.Add(cmd);
            }
            else
            {
                int index = 0;
                for (; index < m_commandToKeys.Count; index++)
                    if (cmd == m_commandToKeys[index].Command)
                        break;

                if (index != m_commandToKeys.Count)
                {
                    keys = m_commandToKeys[index].Keys;
                }
                else
                {
                    keys = new();
                    m_commandToKeys.Add((cmd, keys));
                    m_mappedCommands.Add(cmd);
                }
            }

            if (!keys.Contains(key))
                keys.Add(key);
        }

        foreach (string command in m_allCommands.Where(cmd => !m_mappedCommands.Contains(cmd)))
        {
            string cmd = command.WithWordSpaces();
            m_commandToKeys.Add((cmd, new()));
            m_mappedCommands.Add(cmd);
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
            foreach (Key key in Enum.GetValues<Key>())
            {
                if (!input.ConsumeKeyPressed(key)) 
                    continue;
                        
                (string command, List<Key> keys) = m_commandToKeys[m_currentRow];
                if (!keys.Contains(key))
                {
                    m_config.Keys.Add(key, command);
                    keys.Add(key);
                    m_soundManager.PlayStaticSound(MenuSounds.Choose);
                }
                        
                break;
            } 
        }

        m_updatingKeyBinding = false;
        OnLockChanged?.Invoke(this, new(Lock.Unlocked));
    }

    private void UnbindCurrentRow()
    {
        (string command, List<Key> keys) = m_commandToKeys[m_currentRow];
        
        foreach (Key key in keys)
            m_config.Keys.Remove(key, command);
        
        // Clear our local cache, which will be updated anyways but this allows
        // for instant rendering feedback. We don't remove the row because we
        // can then render "No binding" in its place.
        keys.Clear();
    }

    public void HandleInput(IConsumableInput input)
    {
        CheckForConfigUpdates();

        if (m_commandToKeys.Count == 0)
            return;
        
        if (m_updatingKeyBinding)
        {
            if (input.HasAnyKeyPressed())
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
                m_currentRow = m_currentRow != 0 ? (m_currentRow - 1) % m_commandToKeys.Count : m_commandToKeys.Count - 1;
            }
            if (input.ConsumePressOrContinuousHold(Key.Down))
            {
                m_soundManager.PlayStaticSound(MenuSounds.Cursor);
                m_currentRow = (m_currentRow + 1) % m_commandToKeys.Count;
            }

            if (input.ConsumeKeyPressed(Key.Enter) || input.ConsumeKeyPressed(Key.MouseLeft))
            {
                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                m_updatingKeyBinding = true;
                OnLockChanged?.Invoke(this, new(Lock.Locked, "Press any key to add binding. Escape to cancel."));
            }

            if (input.ConsumeKeyPressed(Key.Delete))
            {
                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                UnbindCurrentRow();
            }

            if (m_currentRow != lastRow)
                m_updateRow = true;
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
        m_menuPositionList.Clear();

        if (startY != m_lastY)
        {
            m_lastY = startY;
            m_updateMouse = true;
        }

        int fontSize = m_config.Hud.GetSmallFontSize();
        hud.Text("Key Bindings", Fonts.SmallGray, m_config.Hud.GetLargeFontSize(), (0, startY), out Dimension headerArea, 
            both: Align.TopMiddle, color: Color.Red);
        int y = startY + headerArea.Height + m_config.Hud.GetScaled(8);
        int xOffset = m_config.Hud.GetScaled(4) * 2;
        int smallPad = m_config.Hud.GetScaled(1);

        hud.Text("Scroll with the mouse wheel or holding up/down", Fonts.SmallGray, fontSize, (0, y), out Dimension scrollArea, 
            both: Align.TopMiddle, color: Color.Firebrick);
        y += scrollArea.Height + smallPad;
        
        hud.Text("Press delete to clear all bindings", Fonts.SmallGray, fontSize, (0, y), out Dimension instructionArea, 
            both: Align.TopMiddle, color: Color.Firebrick);
        y += instructionArea.Height + smallPad;
        
        hud.Text("Press enter to start binding and press a key", Fonts.SmallGray, fontSize, (0, y), out Dimension enterArea, 
            both: Align.TopMiddle, color: Color.Firebrick);
        y += enterArea.Height + m_config.Hud.GetScaled(12);

        for (int cmdIndex = 0; cmdIndex < m_commandToKeys.Count; cmdIndex++)
        {
            (string command, List<Key> keys) = m_commandToKeys[cmdIndex];

            Dimension commandArea;
            if (cmdIndex == m_currentRow && m_updatingKeyBinding)
            {
                hud.Text(command, Fonts.SmallGray, fontSize, (-xOffset, y), out commandArea,
                    window: Align.TopMiddle, anchor: Align.TopRight, color: Color.Yellow);
            }
            else
            {
                hud.Text(command, Fonts.SmallGray, fontSize, (-xOffset, y), out commandArea,
                    window: Align.TopMiddle, anchor: Align.TopRight, color: Color.Red);
            }

            if (cmdIndex == m_currentRow)
                m_selectedRender = (y - startY, y + commandArea.Height - startY);

            if (cmdIndex == m_currentRow && !m_updatingKeyBinding)
            {
                var arrowSize = hud.MeasureText("<", Fonts.SmallGray, fontSize);
                Vec2I arrowLeft = (-xOffset - commandArea.Width - m_config.Hud.GetScaled(2), y);
                hud.Text(">", Fonts.SmallGray, fontSize, arrowLeft, window: Align.TopMiddle,
                    anchor: Align.TopRight, color: Color.White);
                Vec2I arrowRight = (-xOffset + arrowSize.Width + m_config.Hud.GetScaled(2), y);
                hud.Text("<", Fonts.SmallGray, fontSize, arrowRight, window: Align.TopMiddle, 
                    anchor: Align.TopRight, color: Color.White);
            }

            if (keys.Empty())
            {
                hud.Text("No binding", Fonts.SmallGray, fontSize, (xOffset, y), out Dimension noBindingArea,
                    window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Gray);

                y += Math.Max(noBindingArea.Height, commandArea.Height);
            }
            else
            {
                Dimension totalKeyArea = (0, 0);
                for (int keyIndex = 0; keyIndex < keys.Count; keyIndex++)
                {
                    Key key = keys[keyIndex];
                    hud.Text(key.ToString(), Fonts.SmallGray, fontSize, (xOffset + totalKeyArea.Width, y),
                        out Dimension keyArea,
                        window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.White);
                    totalKeyArea.Width += keyArea.Width;

                    if (keyIndex != keys.Count - 1)
                    {
                        hud.Text(", ", Fonts.SmallGray, fontSize, (xOffset + totalKeyArea.Width, y),
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

        m_renderHeight = y - startY;

        if (m_updateRow)
        {
            OnRowChanged?.Invoke(this, new(m_currentRow));
            m_updateRow = false;
        }
    }

    public int GetRenderHeight() => m_renderHeight;
    public (int, int) GetSelectedRenderY() => m_selectedRender;
    public void SetToFirstSelection() => m_currentRow = 0;
    public void SetToLastSelection() => m_currentRow = m_commandToKeys.Count - 1;
}