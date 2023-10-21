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
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Options;

public class OptionsLayer : IGameLayer
{
    private const string TiledBackgroundFlat = "FLOOR5_1";
    
    private readonly GameLayerManager m_manager;
    private readonly IConfig m_config;
    private readonly SoundManager m_soundManager;
    private readonly List<IOptionSection> m_sections;
    private int m_currentSectionIndex;

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

    private static ListedConfigSection GetOrMakeListedConfigSectionOrThrow(Dictionary<OptionSectionType, IOptionSection> sectionMap, 
        OptionSectionType section)
    {
        if (sectionMap.TryGetValue(section, out IOptionSection optionSection))
            return optionSection as ListedConfigSection ?? throw new($"Expected a listed config for {optionSection.GetType().FullName}");

        ListedConfigSection listedConfigSection = new(section);
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
        m_sections[m_currentSectionIndex].HandleInput(input);
        
        if (input.ConsumeKeyPressed(Key.Escape))
        {
            m_soundManager.PlayStaticSound(Constants.MenuSounds.Choose);
            m_manager.Remove(this);
            return;
        }
        
        // Switch pages if needed.
        if (m_sections.Count > 0)
        {
            if (input.ConsumeKeyPressed(Key.PageUp))
                m_currentSectionIndex = (m_currentSectionIndex + m_sections.Count - 1) % m_sections.Count;
            if (input.ConsumeKeyPressed(Key.PageDown))
                m_currentSectionIndex = (m_currentSectionIndex + 1) % m_sections.Count;
        }

        // We don't want any input leaking into the layers below this.
        input.ConsumeAll();
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        // Nothing right now.
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

        hud.Image("M_OPTION", (0, 0), out HudBox titleArea, both: Align.TopMiddle, scale: 3.0f);
        int y = titleArea.Height + 10;

        hud.Text("Press \"page up\" or \"page down\" to change pages", "SmallFont", 16, (0, y),
            out Dimension pageInstrArea, both: Align.TopMiddle);
        y += pageInstrArea.Height + 32;

        if (m_currentSectionIndex < m_sections.Count)
            m_sections[m_currentSectionIndex].Render(ctx, hud, y);
        else
            hud.Text("Unexpected error: no config or keys", "SmallFont", 16, (0, y), out _, both: Align.TopMiddle);
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}