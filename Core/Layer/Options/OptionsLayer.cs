using Helion.Audio.Sounds;
using Helion.Audio.Impl;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Dialogs;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Resources;
using Helion.Strings;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.Collections.Generic;
using Helion.Resources.Archives.Collection;
using static Helion.Util.Constants;
using System.Linq;

namespace Helion.Layer.Options;

public class OptionsLayer : IGameLayer, IAnimationLayer
{
    public event EventHandler? OnRestartApplication;
    public InterpolationAnimation<IAnimationLayer> Animation { get; }
    public bool ClearOnExit { get; set; }
    public long LastClosedNanos;
    public bool CurrentlyBindingKey => m_sections[m_currentSectionIndex] is KeyBindingSection { CurrentlyBinding: true };

    private const int BackIndex = 0;
    private const int ForwardIndex = 1;
    private const string Font = Fonts.Options.Font;

    private readonly GameLayerManager m_manager;
    private readonly IConfig m_config;
    private readonly PathsManager m_pathsManager;
    private readonly SoundManager m_soundManager;
    private readonly IWindow m_window;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly List<IOptionSection> m_sections;
    private readonly BoxList m_backForwardPos = new();
    private readonly List<StringSlice> m_footerLines = [];
    private readonly List<string> m_sbarLayoutNames = [];
    private readonly TabHeader m_tabHeader;
    private Dimension m_windowSize;
    private Vec2I m_cursorPos;
    private int m_currentSectionIndex;
    private int m_scrollOffset;
    private int m_headerHeight;
    private int m_footerHeight;
    private int m_footerScrollPadding;
    private int m_messageTicks;
    private int m_maxHeaderWidth = -1;
    private string m_message = string.Empty;
    private string m_sectionMessage = string.Empty;
    private string m_selectedRowDescription = string.Empty;
    private string m_lastSelectedRowDescription = string.Empty;
    private bool m_locked;
    private LockOptions m_lockOptions;
    private bool m_resetMouse;
    private bool m_setMouse;
    private bool m_didMouseWheelScroll;
    private bool m_sectionChange;
    private MessageDialog? m_dialog;

    public OptionsLayer(GameLayerManager manager, IConfig config, PathsManager pathsManager, SoundManager soundManager, IWindow window, ArchiveCollection archiveCollection)
    {
        m_manager = manager;
        m_config = config;
        m_pathsManager = pathsManager;
        m_soundManager = soundManager;
        m_window = window;
        m_archiveCollection = archiveCollection;
        m_sections = GenerateSections();
        m_tabHeader = new TabHeader(m_config.Window, [.. m_sections.Select(x => x.Title)], Font, Color.Gray, new Color(32, 32, 32), new Color(0, 255, 128), Color.Gray, new Color(192, 192, 192), 0.5f);
        m_tabHeader.SelectedIndexChanged += TabHeader_SelectedIndexChanged;

        m_config.Window.State.OnChanged += WindowState_OnChanged;
        m_config.Window.Virtual.Enable.OnChanged += WindowVirtualEnable_OnChanged;
        m_config.Window.Virtual.Dimension.OnChanged += WindowVirtualDimension_OnChanged;
        m_config.Window.MenuScale.OnChanged += Scale_OnChanged;

        Animation = new(TimeSpan.FromMilliseconds(200), this);
        Animation.OnStart += Animation_OnStart;
    }

    private void TabHeader_SelectedIndexChanged(object? sender, EventArgs e)
    {
        m_currentSectionIndex = m_tabHeader.SelectedIndex;
        m_sectionChange = true;
    }

    private void Config_MapRestartRequired(object? sender, EventArgs e)
    {
        ShowMessage(ConfigInfoAttribute.MapRestartRequiredMessage);
    }

    private void Animation_OnStart(object? sender, IAnimationLayer e)
    {
        if (Animation.State == InterpolationAnimationState.Out)
            LastClosedNanos = Ticker.NanoTime();
    }

    public bool ShouldRemove()
    {
        return Animation.State == InterpolationAnimationState.OutComplete;
    }

    public void OnShow()
    {
        m_sbarLayoutNames.Clear();
        string fallbackName = string.Empty;
        var sbarDef = m_archiveCollection.Definitions.StatusBarDefinition;
        
        foreach (var layout in sbarDef.StatusBars)
        {
            if (string.IsNullOrEmpty(layout.Name)) continue;
            m_sbarLayoutNames.Add(layout.Name);
            
            if (string.IsNullOrEmpty(fallbackName) && !layout.FullscreenRender) fallbackName = layout.Name;
        }

        if (string.IsNullOrEmpty(fallbackName) && m_sbarLayoutNames.Count > 0) fallbackName = m_sbarLayoutNames[0];

        string currentLayout = m_config.Hud.StatusBarLayout.Value;
        bool isValid = false;
        
        foreach (string layoutName in m_sbarLayoutNames)
        {
            if (!string.Equals(layoutName, currentLayout, StringComparison.OrdinalIgnoreCase)) continue;
            
            isValid = true;
            break;
        }

        if (!isValid && !string.IsNullOrEmpty(fallbackName)) m_config.Hud.StatusBarLayout.Set(fallbackName);

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

    private void Scale_OnChanged(object? sender, double e) => m_lastSelectedRowDescription = string.Empty;

    private void HandleResize()
    {
        m_resetMouse = true;
        m_scrollOffset = 0;
    }

    private void ResetMousePosition(IHudRenderContext hud)
    {
        m_cursorPos = (hud.Dimension.Width / 2, m_config.Window.GetMenuScaled(45));
        m_window.SetMousePosition(m_cursorPos);
    }

    private ListedConfigSection GetOrMakeListedConfigSectionOrThrow(Dictionary<OptionSectionType, IOptionSection> sectionMap,
        OptionSectionType section)
    {
        if (sectionMap.TryGetValue(section, out var optionSection))
            return optionSection as ListedConfigSection ?? throw new($"Expected a listed config for {optionSection.GetType().FullName}");

        ListedConfigSection listedConfigSection = (section == OptionSectionType.Compat)
            ? new CompatibilitySection(m_config, section, m_pathsManager, m_soundManager, m_window.InputManager)
            : new ListedConfigSection(m_config, section, m_pathsManager, m_soundManager, m_window.InputManager);

        listedConfigSection.OnAttributeChanged += ListedConfigSection_OnAttributeChanged;
        sectionMap[section] = listedConfigSection;
        return listedConfigSection;
    }

    private void ListedConfigSection_OnAttributeChanged(object? sender, ConfigInfoAttribute configAttr)
    {
        if (configAttr.RestartRequired)
        {
            ShowRestartRequired();
            return;
        }

        if (configAttr.GetSetWarningString(out var warningString))
            ShowMessage(warningString);
    }

    private void ShowRestartRequired()
    {
        m_dialog = new MessageDialog(m_config.Window, "Restart required", ["Restart required for this change to take effect.", "", "Restart now?"], "Yes", "No");
        m_dialog.OnClose += RestartDialog_OnClose;
    }

    private void RestartDialog_OnClose(object? sender, DialogCloseArgs e)
    {
        m_dialog = null;
        m_soundManager.PlayStaticSound(e.Accepted ? MenuSounds.Choose : MenuSounds.Clear);
        if (e.Accepted)
            OnRestartApplication?.Invoke(this, EventArgs.Empty);
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
        Dictionary<OptionSectionType, IOptionSection> sectionMap = [];

        // This takes all the common section types and turns them into the
        // generic list of values that users can tweak. It does not handle
        // sections that require special logic, like key bindings.
        foreach ((IConfigValue value, OptionMenuAttribute attr, ConfigInfoAttribute configAttr) in m_config.GetAllConfigFields())
        {
            ListedConfigSection cfgSection = GetOrMakeListedConfigSectionOrThrow(sectionMap, attr.Section);

            if (attr.Section == OptionSectionType.Hud) 
                cfgSection.DynamicOptionProvider = (name) => name == "Status Bar Layout" ? m_sbarLayoutNames : null;
            if (attr.Section == OptionSectionType.Audio) 
                cfgSection.DynamicOptionProvider = (name) => name == "Sound Resampler" ? OpenALSoftResamplerType.MenuOptions : null;

            cfgSection.Add(value, attr, configAttr);
        }

        // Key bindings are a special type of option section handled specially.
        sectionMap[OptionSectionType.Keys] = new KeyBindingSection(m_config, m_soundManager);

        // We want to sort by the section type where the lower the enumeration
        // value, the closer to the front of the list it is. This is because
        // the enumeration values tell us in which order the sections should
        // be seen.
        List<IOptionSection> sections = [];
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

        m_selectedRowDescription = e.SelectedRowDescription;
    }

    private void OptionSection_OnLockChanged(object? sender, LockEvent e)
    {
        if (e.Lock == Lock.Locked)
            ClearMessage();

        m_locked = e.Lock == Lock.Locked;
        m_lockOptions = e.Options;
        m_sectionMessage = e.Message;
    }

    public void HandleInput(IConsumableInput input)
    {
        if (m_dialog != null)
        {
            m_dialog.HandleInput(input);
            return;
        }

        var section = m_sections[m_currentSectionIndex];

        if (m_locked)
        {
            section.HandleInput(input);
            return;
        }

        if (input.ConsumeKeyPressed(Key.Escape) || input.ConsumeKeyPressed(Key.ButtonB))
        {
            // Ensure that the user isn't closing out of the menus without some kind of binding to get back into the menus.
            m_config.Keys.EnsureMenuKey();
            if (m_config.Keys.Changed && m_config is FileConfig fileConfig)
            {
                fileConfig.Write();
            }

            m_soundManager.PlayStaticSound(MenuSounds.Choose);
            Animation.AnimateOut();

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
                m_scrollOffset = Math.Clamp(m_scrollOffset, -(Math.Abs(section.GetRenderHeight() + m_headerHeight + m_footerScrollPadding - m_windowSize.Height + scrollAmount)), 0);
            }

            int buttonIndex = -1;
            if (!m_locked && m_backForwardPos.GetIndex(m_cursorPos, out int checkButtonIndex) && input.ConsumeKeyPressed(Key.MouseLeft))
                buttonIndex = checkButtonIndex;

            m_tabHeader.HandleInput(input, m_cursorPos);
            section.HandleInput(input);

            var index = m_currentSectionIndex;

            if (input.ConsumePressOrContinuousHold(Key.Left) || input.ConsumePressOrContinuousHold(Key.MouseCustom4)
                || input.ConsumePressOrContinuousHold(Key.DPadLeft) || buttonIndex == BackIndex)
            {
                m_sectionChange = true;
                m_currentSectionIndex = (m_currentSectionIndex + m_sections.Count - 1) % m_sections.Count;
            }

            if (input.ConsumePressOrContinuousHold(Key.Right) || input.ConsumePressOrContinuousHold(Key.MouseCustom5)
                || input.ConsumePressOrContinuousHold(Key.DPadRight) || buttonIndex == ForwardIndex)
            {
                m_sectionChange = true;
                m_currentSectionIndex = (m_currentSectionIndex + 1) % m_sections.Count;
            }

            if (index != m_currentSectionIndex)
                m_tabHeader.SetSelectedIndex(m_currentSectionIndex);

            if (m_sectionChange)
            {
                m_scrollOffset = 0;
                m_soundManager.PlayStaticSound(MenuSounds.Change);
                m_sections[m_currentSectionIndex].OnShow();
                m_sectionChange = false;
                section.SetToFirstSelection();
            }
        }

        // We don't want any input leaking into the layers below this.
        input.ConsumeAll();
    }

    private void ScrollToVisibleArea(IOptionSection section)
    {
        var scrollOffset = Math.Abs(m_scrollOffset);
        (int startY, int endY) = section.GetSelectedRenderY();
        
        var pageSize = m_windowSize.Height - m_headerHeight - m_footerHeight;
        var visibleTopY = scrollOffset;
        var visibleBottomY = scrollOffset + pageSize;

        if (startY < visibleTopY)
            scrollOffset = -startY;
        else if (endY > visibleBottomY)
            scrollOffset = -endY + pageSize;
        else
            return;

        m_scrollOffset = scrollOffset;
    }

    private int GetScrollAmount() => (int)(16 * m_config.Window.MenuScale);

    public void RunLogic(TickerInfo tickerInfo)
    {
        if (m_messageTicks > 0)
        {
            m_messageTicks -= tickerInfo.Ticks;
            if (m_messageTicks <= 0)
                ClearMessage();
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        Animation.Tick();
        m_windowSize = hud.Dimension;
        m_backForwardPos.Clear();
        ctx.ClearDepth();
        ctx.DepthTest(false);
        hud.Clear(Color.Gray);

        SetMouseFromRender(hud);

        hud.RenderFullscreenImageStretched(DefaultBackgroundImage);

        int footerFontSize = m_config.Window.GetMenuSmallFontSize();
        int fontSize = m_config.Window.GetMenuMediumFontSize();
        int smallPad = m_config.Window.GetMenuScaled(2);

        m_footerScrollPadding = hud.MeasureText(" ", Font, footerFontSize).Height * 4 + (m_config.Window.GetMenuScaled(8) * 2);
        m_headerHeight = 0;

        var padding = m_config.Window.GetMenuScaled(8);

        if (m_lastSelectedRowDescription != m_selectedRowDescription)
            GenerateFooterLines(m_selectedRowDescription, Font, footerFontSize, hud, m_footerLines, out m_footerHeight);
        m_lastSelectedRowDescription = m_selectedRowDescription;

        m_tabHeader.SetMaxRows(3);
        m_tabHeader.SetMaxHeight((int)(m_windowSize.Height * 0.3));
        var tabHeaderHeight = m_tabHeader.Render(hud, fontSize, padding);
        m_headerHeight = tabHeaderHeight + padding;

        if (tabHeaderHeight == 0)
            RenderFallBackHeader(hud, fontSize, smallPad, padding);

        var y = m_headerHeight;

        if (m_currentSectionIndex < m_sections.Count)
        {
            var sectionStartY = y + m_scrollOffset;
            var section = m_sections[m_currentSectionIndex];

            var scissorBox = new Box2I(new Vec2I(0, 0), new Vec2I(m_windowSize.Width, m_windowSize.Height - m_headerHeight));
            ctx.Scissor(scissorBox, true);
            section.Render(ctx, hud, sectionStartY, m_didMouseWheelScroll);
            ctx.Scissor(scissorBox, false);

            m_didMouseWheelScroll = false;
            RenderScrollBar(hud, section,  m_headerHeight);

            hud.FillBox(new(new Vec2I(0, m_headerHeight), new Vec2I(hud.Dimension.Width, m_headerHeight + 1)), Color.DarkGray);

            if (m_locked && m_lockOptions == LockOptions.None)
                return;

            bool hover;
            if (m_dialog != null)
            {
                m_dialog.Render(ctx, hud);
                hover = m_dialog.OnClickableItem(m_cursorPos);
            }
            else
            {
                hover = section.OnClickableItem(m_cursorPos) || m_backForwardPos.GetIndex(m_cursorPos, out _) || m_tabHeader.MouseHover(m_cursorPos);
            }

            string cursor = hover ? "helion-pointer" : "helion-cursor";
            if (hud.Textures.TryGet(cursor, out var cursorHandle, ResourceNamespace.Graphics))
            {
                int size = hover ? 32 : 24;
                float scale = size / (float)cursorHandle.Dimension.Height;
                hud.Image(cursor, m_cursorPos, resourceNamespace: ResourceNamespace.Graphics, scale: scale);
            }
        }
        else
            hud.Text("Unexpected error: no config or keys", Font, fontSize, (0, y), out _, both: Align.TopMiddle);


        if (m_message.Length > 0)
        {
            var dim = hud.MeasureText(m_message, Font, fontSize);
            hud.FillBox(new(new Vec2I(0, hud.Dimension.Height - dim.Height - (padding * 2)), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)), Color.Black, alpha: 0.7f);
            hud.Text(m_message, Font, fontSize, (0, -padding), both: Align.BottomMiddle, color: Color.Yellow);
        }
        else if (m_footerLines.Count > 0 && tabHeaderHeight > 0)
        {
            RenderFooter(m_footerLines, hud.Height - m_footerHeight, Font, footerFontSize, hud);
        }
    }

    private void RenderFallBackHeader(IHudRenderContext hud, int fontSize, int smallPad, int padding)
    {
        m_headerHeight = smallPad;
        m_footerHeight = 0;

        hud.Text($"{m_currentSectionIndex + 1}/{m_sections.Count}", Font, m_config.Window.GetMenuSmallFontSize(), (smallPad, smallPad * 2),
            out _, both: Align.TopLeft, color: Color.Red);

        hud.Text(m_sections[m_currentSectionIndex].Title, Font, fontSize, (smallPad, smallPad),
           out var titleArea, both: Align.TopMiddle, color: Color.White);
        m_headerHeight += titleArea.Height;

        m_maxHeaderWidth = GetMaxHeaderWidth(hud, Font, fontSize);

        int xOffset = (hud.Dimension.Width - m_maxHeaderWidth) / 2;
        var arrowSize = hud.MeasureText("<-", Font, fontSize);
        int yOffset = titleArea.Height - arrowSize.Height;
        Vec2I backArrowPos = (xOffset - arrowSize.Width - padding, yOffset);
        Vec2I forwardArrowPos = (xOffset + m_maxHeaderWidth + padding, yOffset);
        hud.Text("<-", Font, fontSize, backArrowPos, color: Color.White);
        hud.Text("->", Font, fontSize, forwardArrowPos, color: Color.White);

        m_backForwardPos.Add(new Box2I(backArrowPos, (backArrowPos.X + arrowSize.Width, backArrowPos.Y + arrowSize.Height)), BackIndex);
        m_backForwardPos.Add(new Box2I(forwardArrowPos, (forwardArrowPos.X + arrowSize.Width, forwardArrowPos.Y + arrowSize.Height)), ForwardIndex);
    }

    private int GetMaxHeaderWidth(IHudRenderContext hud, string font, int fontSize)
    {
        var max = 0;
        foreach (var section in m_sections)
        {
            var width = hud.MeasureText(section.Title, font, fontSize).Width;
            max = Math.Max(width, max);
        }

        return max;
    }

    private void GenerateFooterLines(string inputText, string font, int fontSize, IHudRenderContext hud, List<StringSlice> lines,
        out int requiredHeight)
    {
        // Setting descriptions may be verbose, and may need multiple lines to render.  This method precomputes 
        // the dimensions we'll need for a footer, so we can reserve room when doing rendering and scroll offset
        // calculations.  It also returns the split text, since we need to figure that out anyway and are going to
        // need it later when we actually render the footer.
        hud.LineWrap(inputText, font, fontSize, hud.Width, lines, out requiredHeight);

        // Calculate how much room we need for the footer, with padding both above and below the text
        int padding = m_config.Window.GetMenuScaled(8);
        requiredHeight += padding * 2;
    }

    private void RenderFooter(List<StringSlice> lines, int startY, string font, int fontSize, IHudRenderContext hud)
    {
        int padding = m_config.Window.GetMenuScaled(8);

        // Make a box at the bottom of the HUD, then write the text lines over the box
        hud.FillBox(
            new(new Vec2I(0, startY), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)),
            Color.Black,
            alpha: 0.7f);

        int y = hud.Height - m_footerHeight + padding;

        foreach (var line in lines)
        {
            var span = line.AsSpan();
            Dimension tokenSize = hud.MeasureText(span, font, fontSize);
            hud.Text(span, font, fontSize, (0, y), out Dimension drawArea, both: Align.TopMiddle, color: Color.White);
            y += drawArea.Height;
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

    private void RenderScrollBar(IHudRenderContext hud, IOptionSection section, int y)
    {
        if (!ScrollRequired(hud.Dimension.Height, section))
            return;

        var maxRenderHeight = hud.Dimension.Height - m_headerHeight;
        var scrollHeight = section.GetRenderHeight() + m_footerScrollPadding;
        var maxScrollOffset = scrollHeight - maxRenderHeight;

        if (maxScrollOffset < 0)
            return;

        var actualScrollOffset = Math.Abs(m_scrollOffset);
        var barPosition = (int)(actualScrollOffset / (float)maxScrollOffset * maxRenderHeight) + y;

        var barDim = new Dimension(m_config.Window.GetMenuScaled(4), m_config.Window.GetMenuScaled(20));

        if (barPosition + barDim.Height > hud.Dimension.Height)
            barPosition = hud.Dimension.Height - barDim.Height;

        hud.FillBox(new(new Vec2I(-barDim.Width, y), new Vec2I(0, hud.Dimension.Height)), Color.Black, window: Align.TopRight, alpha: 0.7f);
        hud.FillBox(new(new Vec2I(-barDim.Width, barPosition), new Vec2I(0, barPosition + barDim.Height)), Color.Gray, window: Align.TopRight);
    }

    private bool ScrollRequired(int windowHeight, IOptionSection section) =>
        section.GetRenderHeight() - (windowHeight - m_headerHeight - m_footerScrollPadding) > 0;

    public void Dispose()
    {
        // Nothing to dispose.
        GC.SuppressFinalize(this);
    }
}
