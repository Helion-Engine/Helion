﻿using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Dialogs;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class ListedConfigSection : IOptionSection
{
    private const string Font = Fonts.SmallGray;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public event EventHandler<LockEvent>? OnLockChanged;
    public event EventHandler<RowEvent>? OnRowChanged;
    public event EventHandler<ConfigInfoAttribute>? OnAttributeChanged;
    public event EventHandler<string>? OnError;

    public OptionSectionType OptionType { get; }
    private readonly List<(IConfigValue CfgValue, OptionMenuAttribute Attr, ConfigInfoAttribute ConfigAttr)> m_configValues = new();
    private readonly BoxList m_menuPositionList = new();
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly Stopwatch m_stopwatch = new();
    private readonly StringBuilder m_rowEditText = new();
    private Vec2I m_mousePos;
    private int m_renderHeight;
    private (int, int) m_selectedRender;
    private int m_currentRowIndex;
    private int? m_currentEnumIndex;
    private int m_lastY;
    private bool m_rowIsSelected;
    private bool m_updateRow;
    private bool m_updateMouse;
    private IConfigValue? m_currentEditValue;
    private IDialog? m_dialog;

    public ListedConfigSection(IConfig config, OptionSectionType optionType, SoundManager soundManager)
    {
        m_config = config;
        OptionType = optionType;
        m_soundManager = soundManager;

        SetDisableStates();
        m_config.Window.State.OnChanged += WindowState_OnChanged;
    }

    private void WindowState_OnChanged(object? sender, RenderWindowState windowState) =>
        SetDisableStates();

    private void SetDisableStates()
    {
        m_config.Game.MaxAutoSaves.OptionDisabled = !m_config.Game.AutoSave;
        m_config.Game.QuickSaveConfirm.OptionDisabled = m_config.Game.SeparateQuickSaves;
        m_config.Game.RotateQuickSaves.OptionDisabled = !m_config.Game.SeparateQuickSaves;
        m_config.Game.MaxQuickSaves.OptionDisabled = !m_config.Game.SeparateQuickSaves || !m_config.Game.RotateQuickSaves;

        m_config.Window.Dimension.OptionDisabled = m_config.Window.State != RenderWindowState.Normal;

        bool paletteMode = m_config.Window.ColorMode.Value == RenderColorMode.Palette;
        m_config.Render.Filter.Texture.OptionDisabled = paletteMode;
        m_config.Render.Anisotropy.OptionDisabled = paletteMode;
        m_config.Render.LightMode.OptionDisabled = paletteMode;

        m_config.Mouse.ForwardBackwardSpeed.OptionDisabled = m_config.Mouse.Look.Value == true;
    }

    public void ResetSelection() => m_currentRowIndex = 0;

    public bool OnClickableItem(Vec2I mousePosition)
    {
        if (m_dialog != null)
            return m_dialog.OnClickableItem(mousePosition);

        return m_menuPositionList.GetIndex(mousePosition, out _);
    }

    public void Add(IConfigValue value, OptionMenuAttribute attr, ConfigInfoAttribute configAttr)
    {
        m_configValues.Add((value, attr, configAttr));
    }

    public void ResetSelectedRowDefaults()
    {
        var configValue = m_configValues[m_currentRowIndex];
        configValue.CfgValue.Set(configValue.CfgValue.ObjectDefaultValue);
    }

    public void ResetSectionDefaults()
    {
        foreach (var configValue in m_configValues)
        {
            // Some settings are pretty disruptive to the user, like which monitor we're displaying on
            // or which color mode we are rendering in (requires restart), so we won't auto-default those.
            if (!configValue.Attr.AllowBulkReset)
                continue;

            configValue.CfgValue.Set(configValue.CfgValue.ObjectDefaultValue);
        }
    }

    public void HandleInput(IConsumableInput input)
    {
        if (m_rowIsSelected)
        {
            if (m_dialog != null)
            {
                m_mousePos = input.Manager.MousePosition;
                m_dialog.HandleInput(input);
                return;
            }

            UpdateSelectedRow(input);
        }
        else
        {
            int lastRow = m_currentRowIndex;

            if (m_mousePos != input.Manager.MousePosition || m_updateMouse)
            {
                m_updateMouse = false;
                m_mousePos = input.Manager.MousePosition;
                if (m_menuPositionList.GetIndex(m_mousePos, out int rowIndex))
                    m_currentRowIndex = rowIndex;
            }

            if (input.ConsumePressOrContinuousHold(Key.Up))
                AdvanceToValidRow(-1);
            if (input.ConsumePressOrContinuousHold(Key.Down))
                AdvanceToValidRow(1);

            if (input.ConsumeKeyPressed(Key.R) && m_currentRowIndex < m_configValues.Count)
            {
                ResetSelectedRowDefaults();
                return;
            }

            bool mousePress = input.ConsumeKeyPressed(Key.MouseLeft);
            if (mousePress || input.ConsumeKeyPressed(Key.Enter))
            {
                if (mousePress)
                {
                    if (!m_menuPositionList.GetIndex(input.Manager.MousePosition, out int rowIndex))
                        return;
                    m_currentRowIndex = rowIndex;
                }

                if (m_currentRowIndex >= m_configValues.Count)
                {
                    ResetSectionDefaults();
                    return;
                }

                if (IsConfigDisabled(m_currentRowIndex))
                    return;

                var configData = m_configValues[m_currentRowIndex];
                m_soundManager.PlayStaticSound(MenuSounds.Choose);
                m_rowIsSelected = true;
                m_currentEnumIndex = null;
                m_currentEditValue = configData.CfgValue.Clone();
                m_stopwatch.Restart();

                var lockOptions = LockOptions.None;
                if (IsColor(configData.CfgValue, out var color))
                {
                    lockOptions |= LockOptions.AllowMouse;
                    m_dialog = new ColorDialog(m_config.Window, configData.CfgValue, configData.Attr, color);
                    m_dialog.OnClose += Dialog_OnClose;
                }
                else if (configData.Attr.DialogType != DialogType.Default)
                {
                    switch (configData.Attr.DialogType)
                    {
                        case DialogType.TexturePicker:
                            m_dialog = new TexturePickerDialog(m_config.Window, configData.CfgValue, configData.Attr);
                            break;
                        case DialogType.SoundFontPicker:
                            m_dialog = new FileListDialog(m_config.Window, configData.CfgValue, configData.Attr, ".SF2,.SF3");
                            break;
                        default:
                            throw new NotImplementedException($"Unimplemented dialog type: {configData.Attr.DialogType}");
                    }

                    lockOptions |= LockOptions.AllowMouse;
                    m_dialog.OnClose += Dialog_OnClose;
                }


                if (configData.CfgValue.ObjectValue is FileInfo)
                {
                    lockOptions |= LockOptions.AllowMouse;
                    // For now, the only one of these we have is for SoundFonts.  
                    m_dialog = new FileListDialog(m_config.Window, configData.CfgValue, configData.Attr, ".SF2,.SF3");
                    m_dialog.OnClose += Dialog_OnClose;
                }

                if (m_currentEditValue is ConfigValue<bool> boolCfgValue)
                    UpdateBoolOption(input, boolCfgValue, true);

                m_rowEditText.Clear();
                bool isCycleValue = m_currentEditValue is ConfigValue<bool> || m_currentEditValue.ValueType.BaseType == typeof(Enum);
                if (isCycleValue)
                {
                    m_rowEditText.Append(GetConfigDisplayValue(m_currentEditValue, configData.Attr));
                    OnLockChanged?.Invoke(this, new(Lock.Locked, "Press left/right or mouse wheel to change values. Enter to confirm.", lockOptions));
                }
                else
                {
                    OnLockChanged?.Invoke(this, new(Lock.Locked, "Type a new value. Enter to confirm.", lockOptions));
                }
            }

            if (lastRow != m_currentRowIndex)
                m_updateRow = true;
        }
    }

    private void Dialog_OnClose(object? sender, DialogCloseArgs e)
    {
        if (m_dialog == null)
            return;

        if (e.Accepted)
        {
            if (sender is ColorDialog colorDialog)
            {
                m_rowEditText.Clear();
                m_rowEditText.Append(colorDialog.SelectedColor.ToString());
            }
            if (sender is FileListDialog fileDialog)
            {
                if (fileDialog.SelectedFile?.Exists == true)
                {
                    m_rowEditText.Clear();
                    m_rowEditText.Append(fileDialog.SelectedFile.ToString());
                }
            }
            if (sender is TexturePickerDialog textureDialog)
            {
                m_rowEditText.Clear();
                m_rowEditText.Append(textureDialog.SelectedTexture);
            }
            SubmitEditRow();
        }
        else
        {
            ReleaseEditRow();
        }

        m_dialog.OnClose -= Dialog_OnClose;
        m_dialog.Dispose();
        m_dialog = null;
    }

    private bool CurrentRowAllowsTextInput()
    {
        IConfigValue cfgValue = m_configValues[m_currentRowIndex].CfgValue;
        bool isBool = cfgValue.ValueType == typeof(bool);
        bool isEnum = cfgValue.ValueType.BaseType != null && cfgValue.ValueType.BaseType == typeof(Enum);
        return !(isBool || isEnum);
    }

    private void UpdateBoolOption(IConsumableInput input, ConfigValue<bool> cfgValue, bool force)
    {
        int scroll = input.ConsumeScroll();
        if (!force && !input.ConsumeKeyPressed(Key.Left) && !input.ConsumeKeyPressed(Key.Right) && scroll == 0)
            return;

        m_soundManager.PlayStaticSound(MenuSounds.Change);
        bool newValue = !cfgValue.Value;
        cfgValue.Set(newValue);
        m_rowEditText.Clear();
        m_rowEditText.Append(newValue);
    }

    private void UpdateEnumOption(IConsumableInput input, IConfigValue cfgValue)
    {
        bool left = input.ConsumeKeyPressed(Key.Left);
        bool right = input.ConsumeKeyPressed(Key.Right);
        int scroll = input.ConsumeScroll();
        if (m_currentEnumIndex.HasValue && !left && !right && scroll == 0)
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
                var enumVal = enumValues.GetValue(enumIndex);
                if (enumVal?.Equals(currentEnumValue) ?? false)
                    break;
            }
        }

        if (enumIndex >= enumValues.Length)
            return;

        if (scroll != 0)
        {
            enumIndex -= scroll;
            enumIndex %= enumValues.Length;
            if (enumIndex < 0)
                enumIndex = Math.Clamp(enumValues.Length - enumIndex, 0, enumValues.Length - 1);
        }

        if (right)
            enumIndex = (enumIndex + 1) % enumValues.Length;

        if (left)
        {
            if (enumIndex == 0)
                enumIndex = enumValues.Length - 1;
            else
                enumIndex--;
        }

        object nextEnumValue = GetEnumDescription(enumValues.GetValue(enumIndex) ?? "");
        m_rowEditText.Clear();
        m_rowEditText.Append(nextEnumValue);
        m_currentEnumIndex = enumIndex;
        m_soundManager.PlayStaticSound(MenuSounds.Change);
    }

    private static string GetConfigDisplayValue(IConfigValue configValue, OptionMenuAttribute attr)
    {
        if (!configValue.ValueType.IsAssignableFrom(typeof(double)))
            return GetEnumDescription(configValue.ObjectValue).ToString() ?? "??";

        var doubleValue = Convert.ToDouble(configValue.ObjectValue);
        if (configValue.ValueType == typeof(double) && doubleValue - Math.Truncate(doubleValue) == 0)
            return doubleValue.ToString() + Parsing.DecimalFormat.NumberDecimalSeparator + "0";

        return doubleValue.ToString();
    }

    private static string GetConfigDisplayDefaultValue(IConfigValue configValue, OptionMenuAttribute attr)
    {
        if (!configValue.ValueType.IsAssignableFrom(typeof(double)))
            return GetEnumDescription(configValue.ObjectDefaultValue).ToString()
                ?? configValue.ObjectDefaultValue?.ToString()
                ?? string.Empty;

        var doubleValue = Convert.ToDouble(configValue.ObjectDefaultValue);
        if (configValue.ValueType == typeof(double) && doubleValue - Math.Truncate(doubleValue) == 0)
            return doubleValue.ToString() + Parsing.DecimalFormat.NumberDecimalSeparator + "0";

        return doubleValue.ToString();
    }

    private static object GetEnumDescription(object value)
    {
        var type = value.GetType();
        if (type.BaseType != typeof(Enum))
            return value;

        var fi = type.GetField(value.ToString() ?? "");
        if (fi == null)
            return value;
        var descAttr = fi.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr != null)
            return descAttr.Description;
        return value;
    }

    private void UpdateTextEditableOption(IConsumableInput input)
    {
        m_rowEditText.Append(input.ConsumeTypedCharacters());

        if (input.ConsumePressOrContinuousHold(Key.Backspace) && m_rowEditText.Length > 0)
            m_rowEditText.Remove(m_rowEditText.Length - 1, 1);
    }

    private void UpdateSelectedRow(IConsumableInput input)
    {
        if (m_currentEditValue == null)
            return;

        IConfigValue cfgValue = m_currentEditValue;

        if (cfgValue is ConfigValue<bool> boolCfgValue)
            UpdateBoolOption(input, boolCfgValue, false);
        else if (cfgValue.ValueType.BaseType == typeof(Enum))
            UpdateEnumOption(input, cfgValue);
        else
            UpdateTextEditableOption(input);

        bool mousePress = input.ConsumeKeyPressed(Key.MouseLeft);
        if (mousePress || input.ConsumeKeyPressed(Key.Enter))
            SubmitEditRow();

        if (input.ConsumeKeyPressed(Key.Escape) || input.ConsumeKeyPressed(Key.MouseRight))
            ReleaseEditRow();

        // Everything should be consumed by the row.
        input.ConsumeAll();
    }

    private void ReleaseEditRow()
    {
        m_soundManager.PlayStaticSound(MenuSounds.Clear);
        m_rowIsSelected = false;
        OnLockChanged?.Invoke(this, new(Lock.Unlocked));
    }

    private void SubmitEditRow()
    {
        m_soundManager.PlayStaticSound(MenuSounds.Choose);
        SubmitRowChanges();
        m_rowIsSelected = false;
        OnLockChanged?.Invoke(this, new(Lock.Unlocked));
    }

    private void SubmitRowChanges()
    {
        if (m_currentEditValue == null)
            return;

        string newValue = m_rowEditText.ToString();

        // If we erase it and submit an empty field, we will treat this the
        // same as exiting, since it could have unintended side effects like
        // an empty string being converted into something "falsey".
        if (newValue == "")
            return;

        (var cfgValue, var attr, var configAttr) = m_configValues[m_currentRowIndex];
        ConfigSetResult result;

        // This is a hack for enums. The string we render for the user may
        // not be a valid enum when setting, so we use the index instead.
        if (m_currentEnumIndex.HasValue)
        {
            var enumValue = Enum.GetValues(cfgValue.ValueType).GetValue(m_currentEnumIndex.Value);
            if (enumValue == null)
                result = ConfigSetResult.NotSetByBadConversion;
            else
                result = cfgValue.Set(enumValue);
        }
        else
        {
            result = cfgValue.Set(newValue);
        }

        if (result == ConfigSetResult.Set)
            OnAttributeChanged?.Invoke(this, configAttr);
        else if (result != ConfigSetResult.Unchanged)
            OnError?.Invoke(this, "Enter a valid value");

        Log.ConditionalTrace($"Config value with '{newValue}'for update result: {result}");
        m_currentEditValue = null;
        SetDisableStates();
    }

    private void AdvanceToValidRow(int direction)
    {
        m_currentRowIndex += direction;
        if (m_currentRowIndex < 0)
            m_currentRowIndex += m_configValues.Count + 1;
        m_currentRowIndex %= m_configValues.Count + 1;

        m_soundManager.PlayStaticSound(MenuSounds.Cursor);
    }

    private bool Flash()
    {
        const long Duration = 400;
        const long HalfDuration = Duration / 2;
        return m_stopwatch.ElapsedMilliseconds % Duration < HalfDuration;
    }

    private void RenderEditAndUnderscore(IHudRenderContext hud, int fontSize, Vec2I pos, out Dimension renderArea, Color textColor)
    {
        hud.Text(m_rowEditText.ToString(), Font, fontSize, (pos.X, pos.Y), out renderArea, window: Align.TopMiddle,
            anchor: Align.TopLeft, color: textColor);

        if (Flash())
        {
            int x = pos.X + renderArea.Width + 1;
            hud.Text("_", Font, fontSize, (x, pos.Y), out _, window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Yellow);
        }
    }

    private void RenderEditAndSelectionArrows(IHudRenderContext hud, int fontSize, Vec2I pos, out Dimension renderArea, Color textColor)
    {
        if (Flash())
        {
            // To prevent the word from moving, the left arrow needs to be drawn to the
            // left of the word. We have to calculate this.
            Dimension leftArrowArea = hud.MeasureText("<", Font, fontSize);

            hud.Text("<", Font, fontSize, (pos.X - leftArrowArea.Width - 4, pos.Y), out _, window: Align.TopMiddle,
                anchor: Align.TopLeft, color: Color.Yellow);

            hud.Text(m_rowEditText.ToString(), Font, fontSize, (pos.X, pos.Y), out Dimension textArea, window: Align.TopMiddle,
                anchor: Align.TopLeft, color: textColor);
            int accumulatedWidth = textArea.Width + 4;

            hud.Text(">", Font, fontSize, (pos.X + accumulatedWidth, pos.Y), out Dimension rightArrowArea, window: Align.TopMiddle,
                anchor: Align.TopLeft, color: Color.Yellow);
            accumulatedWidth += rightArrowArea.Width;

            int maxHeight = Math.Max(Math.Max(leftArrowArea.Height, textArea.Height), rightArrowArea.Height);
            renderArea = (accumulatedWidth, maxHeight);
        }
        else
        {
            hud.Text(m_rowEditText.ToString(), Font, fontSize, pos, out renderArea, window: Align.TopMiddle,
                anchor: Align.TopLeft, color: textColor);
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY, bool didMouseWheelScroll)
    {
        m_menuPositionList.Clear();
        if (m_configValues.Empty())
            return;

        if (didMouseWheelScroll && startY != m_lastY)
        {
            m_lastY = startY;
            m_updateMouse = true;
        }

        int y = startY;
        int fontSize = m_config.Window.GetMenuSmallFontSize();
        int smallPad = m_config.Window.GetMenuScaled(1);

        hud.Text("Press enter to modify the selected setting", Font, fontSize, (0, y), out Dimension textDimension,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += textDimension.Height + smallPad;

        hud.Text("Press R to reset the selected setting to its default", Font, fontSize, (0, y), out textDimension,
            both: Align.TopMiddle, color: Color.Firebrick);
        y += textDimension.Height + m_config.Window.GetMenuScaled(8);

        hud.Text(OptionType.ToString(), Font, m_config.Window.GetMenuLargeFontSize(), (0, y), out Dimension headerArea, both: Align.TopMiddle, color: Color.Red);
        y += headerArea.Height + m_config.Window.GetMenuScaled(8);

        int offsetX = m_config.Window.GetMenuScaled(8);
        int spacerY = m_config.Window.GetMenuScaled(8);
        int smallSpacer = m_config.Window.GetMenuScaled(2);
        int colorBoxHeight = hud.MeasureText("I", Font, fontSize).Height;

        for (int i = 0; i < m_configValues.Count; i++)
        {
            (IConfigValue cfgValue, OptionMenuAttribute attr, _) = m_configValues[i];
            (Color attrColor, Color valueColorDefault, Color valueColorCustomized) = IsConfigDisabled(i)
                ? (Color.Gray, Color.Gray, Color.Gray)
                : (Color.Red, Color.White, Color.Yellow);

            if (attr.Spacer)
                y += spacerY;

            if (i == m_currentRowIndex && m_rowIsSelected)
                attrColor = Color.Yellow;

            string name = attr.Name;
            if (cfgValue is ConfigValueHeader header)
            {
                attrColor = Color.White;
                name = header.HeaderText;
            }

            name = GetEllipsesText(hud, name, Font, fontSize, hud.Dimension.Width / 2 - offsetX);
            hud.Text(name, Font, fontSize, (-offsetX, y), out Dimension attrArea, window: Align.TopMiddle,
                anchor: Align.TopRight, color: attrColor);

            int valueOffsetX = offsetX;
            if (IsColor(cfgValue, out _))
            {
                RenderColorBox(hud, cfgValue, offsetX, y, colorBoxHeight);
                valueOffsetX += colorBoxHeight + smallSpacer;
            }

            Dimension valueArea;
            string displayValue = GetConfigDisplayValue(cfgValue, attr);
            string displayDefaultValue = GetConfigDisplayDefaultValue(cfgValue, attr);
            Color valueColor = displayValue == displayDefaultValue ? valueColorDefault : valueColorCustomized;

            if (i == m_currentRowIndex && m_rowIsSelected)
            {
                if (CurrentRowAllowsTextInput())
                    RenderEditAndUnderscore(hud, fontSize, (valueOffsetX, y), out valueArea, valueColor);
                else
                    RenderEditAndSelectionArrows(hud, fontSize, (valueOffsetX, y), out valueArea, valueColor);
            }
            else
            {
                hud.Text(displayValue, Font, fontSize, (valueOffsetX, y), out valueArea, window: Align.TopMiddle,
                    anchor: Align.TopLeft, color: valueColor);
            }

            if (i == m_currentRowIndex)
                m_selectedRender = (y - startY, y + valueArea.Height - startY);

            if (i == m_currentRowIndex && !m_rowIsSelected)
            {
                var arrowSize = hud.MeasureText("<", Font, fontSize);
                Vec2I arrowLeft = (-offsetX - attrArea.Width - m_config.Window.GetMenuScaled(2), y);
                hud.Text(">", Font, fontSize, arrowLeft, window: Align.TopMiddle, anchor: Align.TopRight, color: Color.White);
                Vec2I arrowRight = (-offsetX + arrowSize.Width + m_config.Window.GetMenuScaled(2), y);
                hud.Text("<", Font, fontSize, arrowRight, window: Align.TopMiddle, anchor: Align.TopRight, color: Color.White);
            }

            int maxHeight = Math.Max(attrArea.Height, valueArea.Height);
            var rowDimensions = new Box2I((0, y), (hud.Dimension.Width, y + maxHeight));
            m_menuPositionList.Add(rowDimensions, i);

            y += maxHeight + m_config.Window.GetMenuScaled(3);
        }

        string resetText = m_currentRowIndex != m_configValues.Count
            ? "Reload All Defaults"
            : "> Reload All Defaults <";
        hud.Text(resetText, Font, fontSize, (0, y), out Dimension area, window: Align.TopMiddle, anchor: Align.TopMiddle, color: Color.Yellow);
        m_menuPositionList.Add(new Box2I((0, y), (hud.Dimension.Width, y + area.Height)), m_configValues.Count);
        y += area.Height + m_config.Window.GetMenuScaled(3);

        // Handle case where the "Reset" row is selected; this affects auto-scrolling.
        if (m_currentRowIndex == m_configValues.Count)
            m_selectedRender = (y - startY, y + area.Height - startY);

        m_renderHeight = y - startY;

        if (m_updateRow)
        {
            InvokeRowChange();
            m_updateRow = false;
        }

        m_dialog?.Render(ctx, hud);
    }

    public void InvokeRowChange()
    {
        string message = string.Empty;
        if (m_currentRowIndex >= m_configValues.Count)
        {
            message = "Reset all configuration options in this section to their default values.";
        }
        else if (!string.IsNullOrEmpty(m_configValues[m_currentRowIndex].ConfigAttr.Description))
        {
            message = $"{m_configValues[m_currentRowIndex].ConfigAttr.Description} (Default: {GetConfigDisplayDefaultValue(m_configValues[m_currentRowIndex].CfgValue, m_configValues[m_currentRowIndex].Attr)})";
        }

        OnRowChanged?.Invoke(this, new(m_currentRowIndex, message));
    }

    private static bool IsColor(IConfigValue cfgValue, out Vec3I value)
    {
        if (cfgValue.ObjectValue.GetType() == typeof(Vec3I))
        {
            value = (Vec3I)cfgValue.ObjectValue;
            return true;
        }

        value = default;
        return false;
    }

    private static void RenderColorBox(IHudRenderContext hud, IConfigValue cfgValue, int x, int y, int boxSize)
    {
        var boxColor = new Color((Vec3I)cfgValue.ObjectValue);
        hud.FillBox((x, y, x + boxSize, y + boxSize), Color.White, window: Align.TopMiddle,
            anchor: Align.TopLeft);
        hud.FillBox((x + 1, y + 1, x + boxSize - 1, y + boxSize - 1), boxColor, window: Align.TopMiddle,
            anchor: Align.TopLeft);
    }

    public static string GetEllipsesText(IHudRenderContext hud, string text, string font, int fontSize, int maxWidth)
    {
        int nameWidth = hud.MeasureText(text, Font, fontSize).Width;
        if (nameWidth <= maxWidth)
            return text;

        var textSpan = text.AsSpan();
        int sub = 1;
        while (sub < textSpan.Length && hud.MeasureText(textSpan, Font, fontSize).Width > maxWidth)
        {
            textSpan = text.AsSpan(0, text.Length - sub);
            sub++;
        }

        if (textSpan.Length <= 3)
            return text;

        return string.Concat(text.AsSpan(0, textSpan.Length - 3), "...");
    }

    private bool IsConfigDisabled(int rowIndex)
    {
        (IConfigValue cfgValue, OptionMenuAttribute attr, _) = m_configValues[rowIndex];
        return cfgValue.OptionDisabled || attr.Disabled;
    }

    public void OnShow()
    {
        InvokeRowChange();
    }

    public int GetRenderHeight() => m_renderHeight;
    public (int, int) GetSelectedRenderY() => m_selectedRender;
    public void SetToFirstSelection() => m_currentRowIndex = 0;
    public void SetToLastSelection() => m_currentRowIndex = m_configValues.Count - 1;
}