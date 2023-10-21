using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public event EventHandler<ConfigInfoAttribute>? OnAttributeChanged;
    
    public OptionSectionType OptionType { get; }
    private readonly List<(IConfigValue CfgValue, OptionMenuAttribute Attr, ConfigInfoAttribute ConfigAttr)> m_configValues = new();
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly Stopwatch m_stopwatch = new();
    private readonly StringBuilder m_rowEditText = new();
    private int m_renderHeight;
    private int m_currentRowIndex;
    private int? m_currentEnumIndex;
    private bool m_hasSelectableRow;
    private bool m_rowIsSelected;

    public ListedConfigSection(IConfig config, OptionSectionType optionType, SoundManager soundManager)
    {
        m_config = config;
        OptionType = optionType;
        m_soundManager = soundManager;
    }

    public void Add(IConfigValue value, OptionMenuAttribute attr, ConfigInfoAttribute configAttr)
    {
        m_configValues.Add((value, attr, configAttr));
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
                m_currentEnumIndex = null;
                m_stopwatch.Restart();
                m_rowEditText.Clear();
                m_rowEditText.Append(m_configValues[m_currentRowIndex].CfgValue);
            }
        }
    }

    private bool CurrentRowAllowsTextInput()
    {
        IConfigValue cfgValue = m_configValues[m_currentRowIndex].CfgValue;
        bool isBool = cfgValue.ValueType == typeof(bool);
        bool isEnum = cfgValue.ValueType.BaseType != null && cfgValue.ValueType.BaseType == typeof(Enum);
        return !(isBool || isEnum);
    }

    private void UpdateBoolOption(IConsumableInput input, ConfigValue<bool> cfgValue)
    {
        if (!input.ConsumeKeyPressed(Key.Left) && !input.ConsumeKeyPressed(Key.Right)) 
            return;
        
        bool newValue = !cfgValue.Value;
        cfgValue.Set(newValue);
        m_rowEditText.Clear();
        m_rowEditText.Append(newValue);
    }

    private void UpdateEnumOption(IConsumableInput input, IConfigValue cfgValue)
    {
        bool left = input.ConsumeKeyPressed(Key.Left);
        bool right = input.ConsumeKeyPressed(Key.Right);
        if (!left && !right) 
            return;
        
        object currentEnumValue = cfgValue.ObjectValue;
        Array enumValues = Enum.GetValues(cfgValue.ValueType);
        if (enumValues.Length <= 0) 
            return;
        
        int enumIndex = 0;
        if (m_currentEnumIndex.HasValue)
        {
            enumIndex = m_currentEnumIndex.Value;
        }
        else
        {
            for (; enumIndex < enumValues.Length; enumIndex++)
            {
                object enumVal = enumValues.GetValue(enumIndex);
                if (enumVal?.Equals(currentEnumValue) ?? false)
                    break;
            }
        }

        if (enumIndex >= enumValues.Length) 
            return;

        if (right)
            enumIndex = (enumIndex + 1) % enumValues.Length;
        
        if (left)
        {
            if (enumIndex == 0) 
                enumIndex = enumValues.Length - 1;
            else
                enumIndex--;
        }

        object nextEnumValue = enumValues.GetValue(enumIndex);
        m_rowEditText.Clear();
        m_rowEditText.Append(nextEnumValue);
        m_currentEnumIndex = enumIndex;
    }

    private void UpdateTextEditableOption(IConsumableInput input)
    {
        m_rowEditText.Append(input.ConsumeTypedCharacters());
            
        if (input.ConsumePressOrContinuousHold(Key.Backspace) && m_rowEditText.Length > 0)
            m_rowEditText.Remove(m_rowEditText.Length - 1, 1);
    }

    private void UpdateSelectedRow(IConsumableInput input)
    {
        bool doneEditingRow = false;

        IConfigValue cfgValue = m_configValues[m_currentRowIndex].CfgValue;

        if (cfgValue is ConfigValue<bool> boolCfgValue)
            UpdateBoolOption(input, boolCfgValue);
        else if (cfgValue.ValueType.BaseType == typeof(Enum))
            UpdateEnumOption(input, cfgValue);
        else
            UpdateTextEditableOption(input);
        
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
        var configAttr = m_configValues[m_currentRowIndex].ConfigAttr;
        ConfigSetResult result = cfgValue.Set(newValue);

        if (result == ConfigSetResult.Set)
            OnAttributeChanged?.Invoke(this, configAttr);

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

    private bool Flash()
    {
        const long Duration = 400;
        const long HalfDuration = Duration / 2;
        return m_stopwatch.ElapsedMilliseconds % Duration < HalfDuration;
    }
    
    private void RenderEditAndUnderscore(IHudRenderContext hud, int fontSize, Vec2I pos, out Dimension renderArea, Color textColor)
    {
        hud.Text(m_rowEditText.ToString(), Fonts.SmallGray, fontSize, (16, pos.Y), out renderArea, window: Align.TopMiddle, 
            anchor: Align.TopLeft, color: textColor);

        if (Flash())
        {
            int x = pos.X + renderArea.Width + 1;
            hud.Text("_", Fonts.SmallGray, fontSize, (x, pos.Y), out _, window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Yellow);
        }
    }

    private void RenderEditAndSelectionArrows(IHudRenderContext hud, int fontSize, Vec2I pos, out Dimension renderArea, Color textColor)
    {
        if (Flash())
        {
            // To prevent the word from moving, the left arrow needs to be drawn to the
            // left of the word. We have to calculate this.
            Dimension leftArrowArea = hud.MeasureText("<", Fonts.SmallGray, fontSize);
            
            hud.Text("<", Fonts.SmallGray, fontSize, (pos.X - leftArrowArea.Width - 4, pos.Y), out _, window: Align.TopMiddle, 
                anchor: Align.TopLeft, color: Color.Yellow);
            
            hud.Text(m_rowEditText.ToString(), Fonts.SmallGray, fontSize, (pos.X, pos.Y), out Dimension textArea, window: Align.TopMiddle, 
                anchor: Align.TopLeft, color: textColor);
            int accumulatedWidth = textArea.Width + 4;
            
            hud.Text(">", Fonts.SmallGray, fontSize, (pos.X + accumulatedWidth, pos.Y), out Dimension rightArrowArea, window: Align.TopMiddle, 
                anchor: Align.TopLeft, color: Color.Yellow);
            accumulatedWidth += rightArrowArea.Width;

            int maxHeight = Math.Max(Math.Max(leftArrowArea.Height, textArea.Height), rightArrowArea.Height);
            renderArea = (accumulatedWidth, maxHeight);
        }
        else
        {
            hud.Text(m_rowEditText.ToString(), Fonts.SmallGray, fontSize, pos, out renderArea, window: Align.TopMiddle, 
                anchor: Align.TopLeft, color: textColor);
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
            (IConfigValue cfgValue, OptionMenuAttribute attr, ConfigInfoAttribute configAttr) = m_configValues[i];
            (Color attrColor, Color valueColor) = attr.Disabled ? (Color.Gray, Color.Gray) : (Color.Red, Color.White);
            
            if (i == m_currentRowIndex && m_rowIsSelected)
                attrColor = Color.Yellow;
            
            hud.Text(attr.Name, Fonts.SmallGray, fontSize, (-16, y), out Dimension attrArea, window: Align.TopMiddle, 
                anchor: Align.TopRight, color: attrColor);

            Dimension valueArea;
            if (i == m_currentRowIndex && m_rowIsSelected)
            {
                if (CurrentRowAllowsTextInput())
                    RenderEditAndUnderscore(hud, fontSize, (16, y), out valueArea, valueColor);
                else
                    RenderEditAndSelectionArrows(hud, fontSize, (16, y), out valueArea, valueColor);
            }
            else
            {
                hud.Text(cfgValue.ToString(), Fonts.SmallGray, fontSize, (16, y), out valueArea, window: Align.TopMiddle, 
                    anchor: Align.TopLeft, color: valueColor);
            }
            
            if (i == m_currentRowIndex && m_hasSelectableRow && !m_rowIsSelected)
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