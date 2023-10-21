using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class ListedConfigSection : IOptionSection
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public OptionSectionType OptionType { get; }
    private readonly List<(IConfigValue CfgValue, OptionMenuAttribute Attr)> m_configValues = new();
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private int m_renderHeight;
    private int m_currentRowIndex;
    private bool m_hasSelectableRow;
    private bool m_rowIsSelected;
    private StringBuilder m_rowEditText = new();

    public ListedConfigSection(IConfig config, OptionSectionType optionType, SoundManager soundManager)
    {
        m_config = config;
        OptionType = optionType;
        m_soundManager = soundManager;
    }

    public void Add(IConfigValue value, OptionMenuAttribute attr)
    {
        m_configValues.Add((value, attr));
        m_hasSelectableRow |= !attr.Disabled;
    }

    public void HandleInput(IConsumableInput input)
    {
        if (!m_hasSelectableRow)
            return;
        
        if (m_rowIsSelected)
        {
            UpdateSelectedRow(input);
        }
        else
        {
            if (input.Manager.IsKeyPressed(Key.Up))
                AdvanceToValidRow(-1);
            if (input.Manager.IsKeyPressed(Key.Down))
                AdvanceToValidRow(1);

            if (input.ConsumeKeyPressed(Key.Enter))
            {
                m_rowIsSelected = true;
                m_rowEditText.Clear();
                m_rowEditText.Append(m_configValues[m_currentRowIndex].CfgValue);
            }
        }
    }

    private bool CurrentRowAllowsTextInput()
    {
        IConfigValue cfgValue = m_configValues[m_currentRowIndex].CfgValue;
        return cfgValue is ConfigValue<double> or ConfigValue<float> or ConfigValue<int> or ConfigValue<uint> or ConfigValue<string>;
    }

    private void UpdateSelectedRow(IConsumableInput input)
    {
        bool doneEditingRow = false;

        if (CurrentRowAllowsTextInput())
        {
            m_rowEditText.Append(input.ConsumeTypedCharacters());
            
            if (input.ConsumePressOrContinuousHold(Key.Backspace) && m_rowEditText.Length > 0)
                m_rowEditText.Remove(m_rowEditText.Length - 1, 1);
        }
        
        if (input.ConsumeKeyPressed(Key.Enter))
        {
            m_soundManager.PlayStaticSound(MenuSounds.Choose);
            SubmitRowChanges();
            doneEditingRow = true;
        }

        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(MenuSounds.Clear);
            doneEditingRow = true;
        }

        if (doneEditingRow)
            m_rowIsSelected = false;
        
        // Everything should be consumed by the row.
        input.ConsumeAll();
    }

    private void SubmitRowChanges()
    {
        string newValue = m_rowEditText.ToString();
        
        // If we erase it and submit an empty field, we will treat this the
        // same as exiting, since it could have unintended side effects like
        // an empty string being converted into something "falsey".
        if (newValue == "")
            return;
        
        IConfigValue cfgValue = m_configValues[m_currentRowIndex].CfgValue;
        ConfigSetResult result = cfgValue.Set(newValue);

        Log.ConditionalTrace($"Config value with '{newValue}'for update result: {result}");
    }

    private void AdvanceToValidRow(int direction)
    {
        const int MaxOverflowCounter = 10000;
        
        for (int overflowCounter = 0; overflowCounter < MaxOverflowCounter + 1; overflowCounter++)
        {
            if (overflowCounter == MaxOverflowCounter)
            {
                Debug.Assert(m_hasSelectableRow, $"No selectable row detected in options menu type {OptionType}");
                throw new("Unexpected infinite row looping in options menu");
            }
            
            m_currentRowIndex += direction;
            if (m_currentRowIndex < 0)
                m_currentRowIndex += m_configValues.Count;
            m_currentRowIndex %= m_configValues.Count;

            if (!m_configValues[m_currentRowIndex].Attr.Disabled)
                break;
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
        if (m_configValues.Empty())
            return;
        
        // This is for the case where we start off on a row that is disabled but
        // we know there's at least one valid row.
        if (m_hasSelectableRow && m_configValues[m_currentRowIndex].Attr.Disabled)
            AdvanceToValidRow(1);
        
        int y = startY;
        
        hud.Text(OptionType.ToString(), Fonts.SmallGray, m_config.Hud.GetLargeFontSize(), (0, y), out Dimension headerArea, both: Align.TopMiddle, color: Color.Red);
        y += headerArea.Height + m_config.Hud.GetScaled(8);

        int fontSize = m_config.Hud.GetSmallFontSize();

        for (int i = 0; i < m_configValues.Count; i++)
        {
            (IConfigValue cfgValue, OptionMenuAttribute attr) = m_configValues[i];
            (Color attrColor, Color valueColor) = attr.Disabled ? (Color.DarkGray, Color.DarkGray) : (Color.Red, Color.White);
            
            if (i == m_currentRowIndex && m_rowIsSelected)
                attrColor = Color.Yellow;
            
            hud.Text(attr.Name, Fonts.SmallGray, fontSize, (-16, y), out Dimension attrArea, window: Align.TopMiddle, anchor: Align.TopRight, color: attrColor);

            Dimension valueArea;
            if (i == m_currentRowIndex && m_rowIsSelected)
            {
                hud.Text(m_rowEditText.ToString(), Fonts.SmallGray, fontSize, (16, y), out valueArea, window: Align.TopMiddle, anchor: Align.TopLeft, color: valueColor);
                if (CurrentRowAllowsTextInput())
                    hud.Text("_", Fonts.SmallGray, fontSize, (16 + valueArea.Width + 1, y), out _, window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Yellow);
            }
            else
            {
                hud.Text(cfgValue.ToString(), Fonts.SmallGray, fontSize, (16, y), out valueArea, window: Align.TopMiddle, anchor: Align.TopLeft, color: valueColor);
            }
            
            if (i == m_currentRowIndex && m_hasSelectableRow)
            {
                Vec2I topRightCorner = (-16 - attrArea.Width - 12, y);
                hud.Text(">", Fonts.SmallGray, fontSize, topRightCorner, window: Align.TopMiddle, anchor: Align.TopRight, color: Color.White);    
            }

            int maxHeight = Math.Max(attrArea.Height, valueArea.Height);
            y += maxHeight + m_config.Hud.GetScaled(3);
        }

        m_renderHeight = y - startY;
    }

    public int GetRenderHeight() => m_renderHeight;
}