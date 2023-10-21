using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Audio.Sounds;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
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
    private const string TiledBackgroundFlat = "FLOOR5_1";
    
    private readonly GameLayerManager m_manager;
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly List<IOptionSection> m_sections;
    private int m_currentSectionIndex;
    private int m_scrollOffset;
    private int m_ticks;
    private int m_windowHeight;
    private int m_headerHeight;

    public OptionsLayer(GameLayerManager manager, IConfig config, SoundManager soundManager)
    {
        m_manager = manager;
        m_config = config;
        m_soundManager = soundManager;
        m_sections = GenerateSections();
    }

    private List<(IConfigValue, OptionMenuAttribute)> GetAllConfigFields()
    {
        List<(IConfigValue, OptionMenuAttribute)> fields = new();
        RecursivelyGetConfigFieldsOrThrow(m_config, fields);
        return fields;
    }

    private static void RecursivelyGetConfigFieldsOrThrow(object obj, List<(IConfigValue, OptionMenuAttribute)> fields)
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

    private static void PopulateComponentsRecursively(object obj, List<(IConfigValue, OptionMenuAttribute)> fields, int depth = 1)
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
                if (attribute != null)
                    fields.Add((configValue, attribute));
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

        ListedConfigSection listedConfigSection = new(m_config, section);
        sectionMap[section] = listedConfigSection;
        return listedConfigSection;
    }
    
    private List<IOptionSection> GenerateSections()
    {
        Dictionary<OptionSectionType, IOptionSection> sectionMap = new();
        
        // This takes all the common section types and turns them into the
        // generic list of values that users can tweak. It does not handle
        // sections that require special logic, like key bindings.
        foreach ((IConfigValue value, OptionMenuAttribute attr) in GetAllConfigFields())
        {
            ListedConfigSection cfgSection = GetOrMakeListedConfigSectionOrThrow(sectionMap, attr.Section);
            cfgSection.Add(value, attr);
        }
        
        // Key bindings are a special type of option section handled specially.
        sectionMap[OptionSectionType.Keys] = new KeyBindingSection(m_config);

        // We want to sort by the section type where the lower the enumeration
        // value, the closer to the front of the list it is. This is because
        // the enumeration values tell us in which order the sections should
        // be seen.
        List<IOptionSection> sections = new();
        foreach (OptionSectionType section in Enum.GetValues<OptionSectionType>())
            if (sectionMap.TryGetValue(section, out IOptionSection? optionSection))
                sections.Add(optionSection);
        
        return sections;
    }

    public void HandleInput(IConsumableInput input)
    {
        var section = m_sections[m_currentSectionIndex];
        section.HandleInput(input);
        
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(MenuSounds.Choose);
            m_manager.Remove(this);
            return;
        }

        // Switch pages if needed.
        if (m_sections.Count > 0)
        {
            if (ScrollRequired(m_windowHeight, section))
            {
                int scrollAmount = GetScrollAmount();
                m_scrollOffset += input.ConsumeScroll() * scrollAmount;

                if (input.ConsumePressOrContinuousHold(Key.Up))
                    m_scrollOffset += scrollAmount;
                if (input.ConsumePressOrContinuousHold(Key.Down))
                    m_scrollOffset -= scrollAmount;

                if (input.ConsumeKeyPressed(Key.Home))
                    m_scrollOffset = 0;
                if (input.ConsumeKeyPressed(Key.End) && m_currentSectionIndex < m_sections.Count)
                    m_scrollOffset = -(section.GetRenderHeight() - m_windowHeight + m_headerHeight) - scrollAmount;

                m_scrollOffset = Math.Min(0, m_scrollOffset);
            }

            if (input.ConsumeKeyPressed(Key.Left))
            {
                m_scrollOffset = 0;
                m_currentSectionIndex = (m_currentSectionIndex + m_sections.Count - 1) % m_sections.Count;
            }

            if (input.ConsumeKeyPressed(Key.Right))
            {
                m_scrollOffset = 0;
                m_currentSectionIndex = (m_currentSectionIndex + 1) % m_sections.Count;
            }
        }

        // We don't want any input leaking into the layers below this.
        input.ConsumeAll();
    }

    private int GetScrollAmount() => (int)(16 * m_config.Hud.Scale);

    public void RunLogic(TickerInfo tickerInfo)
    {
        m_ticks += tickerInfo.Ticks;
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
        ctx.ClearDepth();
        hud.Clear(Color.Gray);
        FillBackgroundRepeatingImages(ctx, hud);
        int fontSize = m_config.Hud.GetMediumFontSize();

        m_windowHeight = hud.Dimension.Height;

        m_headerHeight = 0;
        int y = m_scrollOffset;
        hud.Image("M_OPTION", (0, y), out HudBox titleArea, both: Align.TopMiddle, scale: 3.0f);
        m_headerHeight += titleArea.Height + m_config.Hud.GetScaled(5);

        hud.Text("Press \"left\" or \"right\" to change pages", Fonts.Small, fontSize, (0, m_headerHeight + y),
            out Dimension pageInstrArea, both: Align.TopMiddle);
        m_headerHeight += pageInstrArea.Height + m_config.Hud.GetScaled(16);
        y += m_headerHeight;


        if (m_currentSectionIndex < m_sections.Count)
        {
            var section = m_sections[m_currentSectionIndex];
            section.Render(ctx, hud, y);

            RenderScrollBar(hud, fontSize, section);

        }
        else
            hud.Text("Unexpected error: no config or keys", Fonts.Small, fontSize, (0, y), out _, both: Align.TopMiddle);
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

        hud.Text(Bar, Fonts.Small, fontSize,
            (hud.Dimension.Width - textDimension.Width - m_config.Hud.GetScaled(4), y));
    }

    private bool ScrollRequired(int windowHeight, IOptionSection section) =>
        section.GetRenderHeight() - (windowHeight - m_headerHeight) > 0;

    private bool Flash() => m_ticks / (int)(TicksPerSecond / 3) % 2 == 0;

    public void Dispose()
    {
        // Nothing to dispose.
    }
}