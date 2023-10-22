using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Geometry;
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
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class KeyBindingSection : IOptionSection
{
    public OptionSectionType OptionType => OptionSectionType.Keys;
    private readonly IConfig m_config;
    private readonly List<(string Command, List<Key> Keys)> m_commandToKeys = new();
    private readonly HashSet<string> m_mappedCommands = new();
    private int m_renderHeight;
    private (int, int) m_selectedRender;
    private int m_currentRow;
    private bool m_updatingKeyBinding;

    public KeyBindingSection(IConfig config)
    {
        m_config = config;
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
            if (!m_mappedCommands.Contains(command))
            {
                m_commandToKeys.Add((command, new()));
                m_mappedCommands.Add(command);
            }
        }
        
        foreach ((Key key, string command) in m_config.Keys.GetKeyMapping())
        {
            List<Key> keys;
            if (!m_mappedCommands.Contains(command))
            {
                keys = new();
                m_commandToKeys.Add((command, keys));
                m_mappedCommands.Add(command);
            }
            else
            {
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
                    m_commandToKeys.Add((command, keys));
                    m_mappedCommands.Add(command);
                }
            }

            if (!keys.Contains(key))
                keys.Add(key);
        }
    }

    private void TryUpdateKeyBindingsFromPress(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.Escape))
        {
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
                }
                        
                break;
            } 
        }

        m_updatingKeyBinding = false;
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
            if (input.ConsumePressOrContinuousHold(Key.Up))
                m_currentRow = m_currentRow != 0 ? (m_currentRow - 1) % m_commandToKeys.Count : m_commandToKeys.Count - 1;
            if (input.ConsumePressOrContinuousHold(Key.Down))
                m_currentRow = (m_currentRow + 1) % m_commandToKeys.Count;

            int scrollAmount = input.ConsumeScroll();
            if (scrollAmount != 0)
            {
                m_currentRow -= scrollAmount;
                m_currentRow %= m_commandToKeys.Count;

                if (m_currentRow < 0)
                    m_currentRow = Math.Min(m_commandToKeys.Count + m_currentRow, m_commandToKeys.Count - 1);
            }

            if (input.ConsumeKeyPressed(Key.Enter))
                m_updatingKeyBinding = true;

            if (input.ConsumeKeyPressed(Key.Delete))
                UnbindCurrentRow();
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
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

                y += Math.Max(totalKeyArea.Height, commandArea.Height);
            }
        }

        m_renderHeight = y - startY;
    }

    public int GetRenderHeight() => m_renderHeight;
    public (int, int) GetSelectedRenderY() => m_selectedRender;
    public void SetToFirstSelection() => m_currentRow = 0;
    public void SetToLastSelection() => m_currentRow = m_commandToKeys.Count - 1;
}