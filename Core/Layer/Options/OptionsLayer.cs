using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Dialogs;
using Helion.Layer.Options.Sections;
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

public class OptionsLayer : IGameLayer, IAnimationLayer
{
    public event EventHandler? OnRestartApplication;
    public InterpolationAnimation<IAnimationLayer> Animation { get; }
    public bool ClearOnExit { get; set; }
    public long LastClosedNanos;

    private const string TiledBackgroundFlat = "FLOOR5_1";
    private const int BackIndex = 0;
    private const int ForwardIndex = 1;
    private const string Font = Fonts.SmallGray;
    private const string FooterFont = Fonts.Console;
    private const string ScrollBarFont = Fonts.Small;

    private readonly GameLayerManager m_manager;
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly IWindow m_window;
    private readonly List<IOptionSection> m_sections;
    private readonly BoxList m_backForwardPos = new();
    private readonly List<string> m_footerLines = [];
    private readonly StringBuilder m_footerStringBuilder = new();
    private Dimension m_windowSize;
    private Vec2I m_cursorPos;
    private int m_currentSectionIndex;
    private int m_scrollOffset;
    private int m_headerHeight;
    private int m_footerHeight;
    private int m_footerScrollPadding;
    private int m_messageTicks;
    private string m_message = string.Empty;
    private string m_sectionMessage = string.Empty;
    private string m_selectedRowDescription = string.Empty;
    private string m_lastSelectedRowDescription = string.Empty;
    private bool m_locked;
    private LockOptions m_lockOptions;
    private bool m_resetMouse;
    private bool m_setMouse;
    private bool m_didMouseWheelScroll;
    private IDialog? m_dialog;

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
        m_config.Hud.Scale.OnChanged += Scale_OnChanged;

        Animation = new(TimeSpan.FromMilliseconds(200), this);
        Animation.OnStart += Animation_OnStart;
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
        m_cursorPos = (hud.Dimension.Width / 2, m_config.Hud.GetScaled(45));
        m_window.SetMousePosition(m_cursorPos);
    }

    private ListedConfigSection GetOrMakeListedConfigSectionOrThrow(Dictionary<OptionSectionType, IOptionSection> sectionMap,
        OptionSectionType section)
    {
        if (sectionMap.TryGetValue(section, out var optionSection))
            return optionSection as ListedConfigSection ?? throw new($"Expected a listed config for {optionSection.GetType().FullName}");

        ListedConfigSection listedConfigSection = new(m_config, section, m_soundManager);
        listedConfigSection.OnAttributeChanged += ListedConfigSection_OnAttributeChanged;
        sectionMap[section] = listedConfigSection;
        return listedConfigSection;
    }

    private void ListedConfigSection_OnAttributeChanged(object? sender, ConfigInfoAttribute configAttr)
    {
        if (configAttr.RestartRequired)
        {
            m_dialog = new MessageDialog(m_config.Hud, "Restart required", ["Restart required for this change to take effect.", "", "Restart now?"], "Yes", "No");
            m_dialog.OnClose += RestartDialog_OnClose;
            return;
        }

        if (configAttr.GetSetWarningString(out var warningString))
            ShowMessage(warningString);
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
        Dictionary<OptionSectionType, IOptionSection> sectionMap = new();

        // This takes all the common section types and turns them into the
        // generic list of values that users can tweak. It does not handle
        // sections that require special logic, like key bindings.
        foreach ((IConfigValue value, OptionMenuAttribute attr, ConfigInfoAttribute configAttr) in m_config.GetAllConfigFields())
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

        if (input.ConsumeKeyPressed(Key.Escape))
        {
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
        if (endY + m_headerHeight > Math.Abs(m_scrollOffset) + (m_windowSize.Height - m_footerScrollPadding))
        {
            m_scrollOffset = (endY + m_headerHeight - m_windowSize.Height + m_footerScrollPadding);
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
        Animation.Tick();
        m_windowSize = hud.Dimension;
        m_backForwardPos.Clear();
        ctx.ClearDepth();
        hud.Clear(Color.Gray);

        SetMouseFromRender(hud);

        FillBackgroundRepeatingImages(ctx, hud);

        int fontSize = m_config.Hud.GetMediumFontSize();
        int largeFontSize = m_config.Hud.GetLargeFontSize();
        int smallPad = m_config.Hud.GetScaled(2);

        m_footerScrollPadding = hud.MeasureText(" ", FooterFont, fontSize).Height * 4 + (m_config.Hud.GetScaled(8) * 2);

        hud.Text($"{m_currentSectionIndex + 1}/{m_sections.Count}", Font, fontSize, (smallPad, smallPad),
            out _, both: Align.TopLeft, color: Color.Red);

        m_headerHeight = 0;
        int y = m_scrollOffset;
        int titleY = m_scrollOffset;
        hud.Text("Options", Font, largeFontSize, (smallPad, smallPad + titleY),
                   out var titleArea, both: Align.TopMiddle, color: Color.White);
        m_headerHeight += titleArea.Height + m_config.Hud.GetScaled(5);

        int padding = m_config.Hud.GetScaled(8);
        if (m_sections.Count > 1)
        {
            int xOffset = (hud.Dimension.Width - titleArea.Width) / 2;
            var arrowSize = hud.MeasureText("<-", Font, fontSize);
            int yOffset = titleArea.Height - arrowSize.Height;
            Vec2I backArrowPos = (xOffset - arrowSize.Width - padding, titleY + yOffset);
            Vec2I forwardArrowPos = (xOffset + titleArea.Width + padding, titleY + yOffset);
            hud.Text("<-", Font, fontSize, backArrowPos, color: Color.White);
            hud.Text("->", Font, fontSize, forwardArrowPos, color: Color.White);

            m_backForwardPos.Add(new Box2I(backArrowPos, (backArrowPos.X + arrowSize.Width, backArrowPos.Y + arrowSize.Height)), BackIndex);
            m_backForwardPos.Add(new Box2I(forwardArrowPos, (forwardArrowPos.X + arrowSize.Width, forwardArrowPos.Y + arrowSize.Height)), ForwardIndex);
        }

        hud.Text(m_sectionMessage.Length > 0 ? m_sectionMessage : "Press left or right to change pages.", Font, fontSize, (0, m_headerHeight + y),
            out Dimension pageInstrArea, both: Align.TopMiddle, color: Color.Red);

        m_headerHeight += pageInstrArea.Height + m_config.Hud.GetScaled(16);
        if (m_lastSelectedRowDescription != m_selectedRowDescription)
            GenerateFooterLines(m_selectedRowDescription, FooterFont, fontSize, hud, m_footerLines, m_footerStringBuilder, out m_footerHeight);
        m_lastSelectedRowDescription = m_selectedRowDescription;

        y += m_headerHeight;

        if (m_currentSectionIndex < m_sections.Count)
        {
            var section = m_sections[m_currentSectionIndex];
            section.Render(ctx, hud, y, m_didMouseWheelScroll);
            m_didMouseWheelScroll = false;

            RenderScrollBar(hud, fontSize, section);

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
                hover = section.OnClickableItem(m_cursorPos) || m_backForwardPos.GetIndex(m_cursorPos, out _);
            }

            string cursor = hover ? "pointer" : "cursor";
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
        else if (m_footerLines.Count > 0)
        {
            RenderFooter(m_footerLines, hud.Height - m_footerHeight, FooterFont, fontSize, hud);
        }
    }

    private void GenerateFooterLines(string inputText, string font, int fontSize, IHudRenderContext hud, List<string> lines, StringBuilder builder,
        out int requiredHeight)
    {   
        // Setting descriptions may be verbose, and may need multiple lines to render.  This method precomputes 
        // the dimensions we'll need for a footer, so we can reserve room when doing rendering and scroll offset
        // calculations.  It also returns the split text, since we need to figure that out anyway and are going to
        // need it later when we actually render the footer.
        LineWrap.Calculate(inputText, font, fontSize, hud.Width, hud, lines, builder, out requiredHeight);

        // Calculate how much room we need for the footer, with padding both above and below the text
        int padding = m_config.Hud.GetScaled(8);
        requiredHeight += padding * 2;
    }

    private void RenderFooter(List<string> lines, int startY, string font, int fontSize, IHudRenderContext hud)
    {
        int padding = m_config.Hud.GetScaled(8);

        // Make a box at the bottom of the HUD, then write the text lines over the box
        hud.FillBox(
            new(new Vec2I(0, startY), new Vec2I(hud.Dimension.Width, hud.Dimension.Height)),
            Color.Black,
            alpha: 0.7f);

        int y = hud.Height - m_footerHeight + padding;

        foreach (string line in lines)
        {
            Dimension tokenSize = hud.MeasureText(line, font, fontSize);
            hud.Text(line, font, fontSize, (0, y), out Dimension drawArea, both: Align.TopMiddle, color: Color.White);
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

    private void RenderScrollBar(IHudRenderContext hud, int fontSize, IOptionSection section)
    {
        if (!ScrollRequired(hud.Dimension.Height, section))
            return;

        int scrollHeight = section.GetRenderHeight() + m_headerHeight + m_footerScrollPadding;
        int maxScrollOffset = scrollHeight - hud.Dimension.Height;

        if (maxScrollOffset < 0)
        {
            return;
        }

        int actualScrollOffset = Math.Abs(m_scrollOffset);
        int barPosition = (int)(actualScrollOffset / (float)maxScrollOffset * hud.Dimension.Height);

        const string Bar = "|";
        var textDimension = hud.MeasureText(Bar, ScrollBarFont, fontSize);

        if (barPosition + textDimension.Height > hud.Dimension.Height)
        {
            barPosition = hud.Dimension.Height - textDimension.Height;
        }

        hud.Text(Bar, ScrollBarFont, fontSize, (0, barPosition), both: Align.TopRight);
    }

    private bool ScrollRequired(int windowHeight, IOptionSection section) =>
        section.GetRenderHeight() - (windowHeight - m_headerHeight - m_footerScrollPadding) > 0;

    public void Dispose()
    {
        // Nothing to dispose.
    }
}