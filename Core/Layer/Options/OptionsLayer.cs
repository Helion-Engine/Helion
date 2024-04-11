using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using static Helion.Util.Constants;

namespace Helion.Layer.Options;

public class OptionsLayer : IGameLayer
{
    public bool ClearOnExit { get; set; }

    private const string TiledBackgroundFlat = "FLOOR5_1";
    private const int BackIndex = 0;
    private const int ForwardIndex = 1;
    
    private readonly GameLayerManager m_manager;
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly IWindow m_window;
    private readonly List<IOptionSection> m_sections;
    private readonly BoxList m_backForwardPos = new();
    private Dimension m_windowSize;
    private Vec2I m_cursorPos;
    private int m_currentSectionIndex;
    private int m_scrollOffset;
    private int m_headerHeight;
    private int m_messageTicks;
    private string m_message = string.Empty;
    private string m_sectionMessage = string.Empty;
    private bool m_locked;
    private bool m_resetMouse;
    private bool m_setMouse;
    private bool m_didMouseWheelScroll;

    public OptionsLayer(GameLayerManager manager, IConfig config, SoundManager soundManager, IWindow window)
    {
        m_manager = manager;
        m_config = config;
        m_soundManager = soundManager;
        m_window = window;
        m_sections = GenerateSections();

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged += WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged += WindowVirtualDimension_OnChanged;
    }

    public void OnShow()
    {
        ClearMessage();
        if (m_currentSectionIndex < m_sections.Count)
            m_sections[m_currentSectionIndex].OnShow();
    }

    public void SetMouseStartPosition()
    {
        m_resetMouse = m_cursorPos == Vec2I.Zero;
        m_setMouse = true;
    }

    private void WindowVirtualDimension_OnChanged(object? sender, Dimension e) => HandleResize();

    private void WindowVirtualEnable_OnChanged(object? sender, bool e) => HandleResize();

    private void WindowState_OnChanged(object? sender, RenderWindowState e) => HandleResize();

    private void HandleResize()
    {
        m_resetMouse = true;
        m_scrollOffset = 0;
    }

    private void ResetMousePosition(IHudRenderContext hud)
    {
        m_cursorPos = (hud.Dimension.Width / 2, m_config.Hud.GetScaled(45));
        m_window.SetMousePosition(m_cursorPos);
    }

    private List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> GetAllConfigFields()
    {
        List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields = new();
        RecursivelyGetConfigFieldsOrThrow(m_config, fields);
        return fields;
    }

    private static void RecursivelyGetConfigFieldsOrThrow(object obj, List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields)
    {
        foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
        {
            MethodInfo? getMethod = propertyInfo.GetMethod;
            if (getMethod?.IsPublic == null)
                continue;

            if (!getMethod.ReturnType.Name.StartsWith("Config", StringComparison.OrdinalIgnoreCase))
                continue;

            object? childObj = getMethod.Invoke(obj, null);
            if (childObj == null)
                continue;

            PopulateComponentsRecursively(childObj, fields);
        }
    }

    private static void PopulateComponentsRecursively(object obj, List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields, int depth = 1)
    {
        const int RecursiveOverflowLimit = 100;
        if (depth > RecursiveOverflowLimit)
            throw new($"Overflow when trying to get options from the config: {obj} ({obj.GetType()})");
        
        foreach (FieldInfo fieldInfo in obj.GetType().GetFields())
        {
            if (!fieldInfo.IsPublic)
                continue;

            object? childObj = fieldInfo.GetValue(obj);
            if (childObj == null || childObj == obj)
                continue;

            if (childObj is IConfigValue configValue)
            {
                OptionMenuAttribute? attribute = fieldInfo.GetCustomAttribute<OptionMenuAttribute>();
                ConfigInfoAttribute? configAttribute = fieldInfo.GetCustomAttribute<ConfigInfoAttribute>();
                if (attribute != null && configAttribute != null)
                    fields.Add((configValue, attribute, configAttribute));
                continue;
            }

            PopulateComponentsRecursively(childObj, fields, depth + 1);
        }
    }

    private ListedConfigSection GetOrMakeListedConfigSectionOrThrow(Dictionary<OptionSectionType, IOptionSection> sectionMap, 
        OptionSectionType section)
    {
        if (sectionMap.TryGetValue(section, out IOptionSection optionSection))
            return optionSection as ListedConfigSection ?? throw new($"Expected a listed config for {optionSection.GetType().FullName}");

        ListedConfigSection listedConfigSection = new(m_config, section, m_soundManager);
        listedConfigSection.OnAttributeChanged += ListedConfigSection_OnAttributeChanged;
        sectionMap[section] = listedConfigSection;
        return listedConfigSection;
    }

    private void ListedConfigSection_OnAttributeChanged(object? sender, ConfigInfoAttribute configAttr)
    {
        if (configAttr.GetSetWarningString(out var warningString))
            ShowMessage(warningString);
    }

    private void ShowMessage(string message)
    {
        m_message = message;
        m_messageTicks = (int)TicksPerSecond * 5;
    }

    private void ClearMessage()
    {
        m_message = string.Empty;
        m_messageTicks = 0;
    }

    private List<IOptionSection> GenerateSections()
    {
        Dictionary<OptionSectionType, IOptionSection> sectionMap = new();
        
        // This takes all the common section types and turns them into the
        // generic list of values that users can tweak. It does not handle
        // sections that require special logic, like key bindings.
        foreach ((IConfigValue value, OptionMenuAttribute attr, ConfigInfoAttribute configAttr) in GetAllConfigFields())
        {
            ListedConfigSection cfgSection = GetOrMakeListedConfigSectionOrThrow(sectionMap, attr.Section);
            cfgSection.Add(value, attr, configAttr);
        }
        
        // Key bindings are a special type of option section handled specially.
        sectionMap[OptionSectionType.Keys] = new KeyBindingSection(m_config, m_soundManager);

        // We want to sort by the section type where the lower the enumeration
        // value, the closer to the front of the list it is. This is because
        // the enumeration values tell us in which order the sections should
        // be seen.
        List<IOptionSection> sections = new();
        foreach (OptionSectionType section in Enum.GetValues<OptionSectionType>())
        {
            if (!sectionMap.TryGetValue(section, out IOptionSection? optionSection))
                continue;
            sections.Add(optionSection);
            optionSection.OnLockChanged += OptionSection_OnLockChanged;
            optionSection.OnRowChanged += OptionSection_OnRowChanged;
            optionSection.OnError += OptionSection_OnError;
        }
        
        return sections;
    }

    private void OptionSection_OnError(object? sender, string error)
    {
        ShowMessage(error);
    }

    private void OptionSection_OnRowChanged(object? sender, RowEvent e)
    {
        if (e.Index == 0)
            m_scrollOffset = 0;
    }

    private void OptionSection_OnLockChanged(object? sender, LockEvent e)
    {
        if (e.Lock == Lock.Locked)
            ClearMessage();

        m_locked = e.Lock == Lock.Locked;
        m_sectionMessage = e.Message;
    }

    public void HandleInput(IConsumableInput input)
    {
        var section = m_sections[m_currentSectionIndex];

        if (m_locked)
        {
            section.HandleInput(input);
            return;
        }
        
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(MenuSounds.Choose);
            m_manager.Remove(this);

            if (ClearOnExit)
                m_manager.Remove(m_manager.MenuLayer);
            ClearOnExit = false;
            return;
        }

        bool checkScroll = true;

        // Switch pages if needed.
        if (m_sections.Count > 0)
        {
            if (input.ConsumeKeyPressed(Key.Home))
            {
                m_scrollOffset = 0;
                section.SetToFirstSelection();
                checkScroll = false;
            }

            if (input.ConsumeKeyPressed(Key.End))
                section.SetToLastSelection();

            bool scrollRequired = ScrollRequired(m_windowSize.Height, section);
            if (checkScroll && scrollRequired)
                ScrollToVisibleArea(section);

            if (scrollRequired)
            {
                int scrollAmount = GetScrollAmount();
                int consumeScroll = input.ConsumeScroll();
                if (consumeScroll != 0)
                    m_didMouseWheelScroll = true;
                m_scrollOffset += consumeScroll * scrollAmount;
                m_scrollOffset = Math.Clamp(m_scrollOffset, -(section.GetRenderHeight() + m_headerHeight - m_windowSize.Height + scrollAmount), 0);                
            }

            int buttonIndex = -1;
            if (!m_locked && m_backForwardPos.GetIndex(m_cursorPos, out int checkButtonIndex) && input.ConsumeKeyPressed(Key.MouseLeft))
                buttonIndex = checkButtonIndex;

            section.HandleInput(input);

            if (input.ConsumePressOrContinuousHold(Key.Left) || input.ConsumePressOrContinuousHold(Key.MouseCustom4) || buttonIndex == BackIndex)
            {
                m_soundManager.PlayStaticSound(MenuSounds.Change);
                m_scrollOffset = 0;
                section.SetToFirstSelection();
                m_currentSectionIndex = (m_currentSectionIndex + m_sections.Count - 1) % m_sections.Count;
                m_sections[m_currentSectionIndex].OnShow();
            }

            if (input.ConsumePressOrContinuousHold(Key.Right) || input.ConsumePressOrContinuousHold(Key.MouseCustom5) || buttonIndex == ForwardIndex)
            {
                m_soundManager.PlayStaticSound(MenuSounds.Change);
                m_scrollOffset = 0;
                section.SetToFirstSelection();
                m_currentSectionIndex = (m_currentSectionIndex + 1) % m_sections.Count;
                m_sections[m_currentSectionIndex].OnShow();
            }
        }

        // We don't want any input leaking into the layers below this.
        input.ConsumeAll();
    }

    private void ScrollToVisibleArea(IOptionSection section)
    {
        int scrollAmount = GetScrollAmount();
        (int startY, int endY) = section.GetSelectedRenderY();
        if (endY + m_headerHeight > Math.Abs(m_scrollOffset) + m_windowSize.Height)
        {
            m_scrollOffset = (endY + m_headerHeight - m_windowSize.Height);
            m_scrollOffset = -(int)Math.Ceiling((m_scrollOffset / (double)scrollAmount)) * scrollAmount;
        }

        if (startY + m_headerHeight < Math.Abs(m_scrollOffset))
        {
            m_scrollOffset = (startY + m_headerHeight);
            m_scrollOffset = -(int)Math.Floor((m_scrollOffset / (double)scrollAmount)) * scrollAmount;
        }
    }

    private int GetScrollAmount() => (int)(16 * m_config.Hud.Scale);

    public void RunLogic(TickerInfo tickerInfo)
    {
        if (m_messageTicks > 0)
        {
            m_messageTicks -= tickerInfo.Ticks;
            if (m_messageTicks <= 0)
                ClearMessage();
        }
    }

    private static void FillBackgroundRepeatingImages(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        if (!hud.Textures.HasImage(TiledBackgroundFlat))
            return;

        (int w, int h) = ctx.Surface.Dimension;
        for (int y = 0; y < (h / 64) + 1; y++)
            for (int x = 0; x < (w / 64) + 1; x++)
                hud.Image(TiledBackgroundFlat, (x * 64, y * 64));
        
        hud.FillBox((0, 0, w, h), Color.Black, alpha: 0.8f);
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        m_windowSize = hud.Dimension;
        m_backForwardPos.Clear();
        ctx.ClearDepth();
        hud.Clear(Color.Gray);

        SetMouseFromRender(hud);

        FillBackgroundRepeatingImages(ctx, hud);

        int fontSize = m_config.Hud.GetMediumFontSize();
        int largeFontSize = m_config.Hud.GetLargeFontSize();
        int smallPad = m_config.Hud.GetScaled(2);
        hud.Text($"{m_currentSectionIndex + 1}/{m_sections.Count}", Fonts.SmallGray, fontSize, (smallPad, smallPad),
            out _, both: Align.TopLeft, color: Color.Red);

        m_headerHeight = 0;
        int y = m_scrollOffset;
        int titleY = m_scrollOffset;
        hud.Text("Options", Fonts.SmallGray, largeFontSize, (smallPad, smallPad + titleY),
                   out var titleArea, both: Align.TopMiddle, color: Color.White);
        m_headerHeight += titleArea.Height + m_config.Hud.GetScaled(5);

        int padding = m_config.Hud.GetScaled(8);
        if (m_sections.Count > 1)
        {
            int xOffset = (hud.Dimension.Width - titleArea.Width) / 2;
            var arrowSize = hud.MeasureText("<-", Fonts.SmallGray, fontSize);
            int yOffset = titleArea.Height - arrowSize.Height;
            Vec2I backArrowPos = (xOffset - arrowSize.Width - padding, titleY + yOffset);
            Vec2I forwardArrowPos = (xOffset + titleArea.Width + padding, titleY + yOffset);
            hud.Text("<-", Fonts.SmallGray, fontSize, backArrowPos, color: Color.White);
            hud.Text("->", Fonts.SmallGray, fontSize, forwardArrowPos, color: Color.White);

            m_backForwardPos.Add(new Box2I(backArrowPos, (backArrowPos.X + arrowSize.Width, backArrowPos.Y + arrowSize.Height)), BackIndex);
            m_backForwardPos.Add(new Box2I(forwardArrowPos, (forwardArrowPos.X + arrowSize.Width, forwardArrowPos.Y + arrowSize.Height)), ForwardIndex);
        }

        hud.Text(m_sectionMessage.Length > 0 ? m_sectionMessage : "Press left or right to change pages.", Fonts.SmallGray, fontSize, (0, m_headerHeight + y),
            out Dimension pageInstrArea, both: Align.TopMiddle, color: Color.Red);
        m_headerHeight += pageInstrArea.Height + m_config.Hud.GetScaled(16);
        y += m_headerHeight;

        if (m_currentSectionIndex < m_sections.Count)
        {
            var section = m_sections[m_currentSectionIndex];
            section.Render(ctx, hud, y, m_didMouseWheelScroll);
            m_didMouseWheelScroll = false;

            RenderScrollBar(hud, fontSize, section);

            if (m_locked)
                return;

            bool hover = section.OnClickableItem(m_cursorPos) || m_backForwardPos.GetIndex(m_cursorPos, out _);

            string cursor = hover ? "pointer" : "cursor";
            if (hud.Textures.TryGet(cursor, out var cursorHandle, ResourceNamespace.Graphics))
            {
                int size = hover ? 32 : 24;
                float scale = size / (float)cursorHandle.Dimension.Height;
                hud.Image(cursor, m_cursorPos, resourceNamespace: ResourceNamespace.Graphics, scale: scale);
            }
        }
        else
            hud.Text("Unexpected error: no config or keys", Fonts.Small, fontSize, (0, y), out _, both: Align.TopMiddle);


        if (m_message.Length > 0)
        {
            var dim = hud.MeasureText(m_message, Fonts.SmallGray, fontSize);
            hud.FillBox(new(new Vec2I(0, hud.Dimension.Height - dim.Height - (padding * 2)), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)), Color.Black, alpha: 0.7f);
            hud.Text(m_message, Fonts.SmallGray, fontSize, (0, -padding), both: Align.BottomMiddle, color: Color.Yellow);
        }
    }

    private void SetMouseFromRender(IHudRenderContext hud)
    {
        bool set = false;

        if (m_resetMouse)
        {
            ResetMousePosition(hud);
            m_resetMouse = false;
            set = true;
        }

        if (m_setMouse)
        {
            m_window.SetMousePosition(m_cursorPos);
            m_setMouse = false;
            set = true;
        }

        if (set)
            return;

        m_cursorPos = m_window.InputManager.MousePosition; 
    }

    private void RenderScrollBar(IHudRenderContext hud, int fontSize, IOptionSection section)
    {
        if (!ScrollRequired(hud.Dimension.Height, section))
            return;

        int scrollHeight = section.GetRenderHeight();
        if (scrollHeight <= hud.Dimension.Height)
            return;

        const string Bar = "|";
        var textDimension = hud.MeasureText(Bar, Fonts.Small, fontSize);

        int scrollAmount = GetScrollAmount();
        int scrollDiff = scrollHeight - (hud.Dimension.Height - m_headerHeight);
        int total = scrollDiff / scrollAmount;
        if (scrollDiff % scrollAmount != 0)
            total++;

        if (total == 0)
            return;

        int screenScrollAmount = hud.Dimension.Height / total;

        int y = -(total - (total - (m_scrollOffset / scrollAmount))) * screenScrollAmount;
        if (y + textDimension.Height > hud.Dimension.Height)
            y = hud.Dimension.Height - textDimension.Height;

        hud.Text(Bar, Fonts.Small, fontSize, (0, y), both: Align.TopRight);
    }

    private bool ScrollRequired(int windowHeight, IOptionSection section) =>
        section.GetRenderHeight() - (windowHeight - m_headerHeight) > 0;

    public void Dispose()
    {
        // Nothing to dispose.
    }
}