using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Layer.Images;
using Helion.Layer.Menus;
using Helion.Layer.Options;
using Helion.Menus.Base;
using Helion.Render.Common.Enums;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.World.Save;

namespace Helion.Menus.Impl;

public class MainMenu : Menu
{
    private const int OffsetX = 97;
    private const int PaddingY = 1;

    private readonly MenuLayer m_parent;
    private readonly SoundManager m_soundManager;

    private readonly OptionsLayer m_optionsLayer;

    public MainMenu(MenuLayer parent, IWindow window, IConfig config, HelionConsole console, SoundManager soundManager,
        ArchiveCollection archiveCollection, SaveGameManager saveManager, OptionsLayer optionsLayer, IScreenshotGenerator screenshotGenerator)
        : base(window, config, console, soundManager, archiveCollection)
    {
        m_parent = parent;
        m_soundManager = soundManager;
        m_optionsLayer = optionsLayer;

        int offsetY = 64;
        if (archiveCollection.IWadType != IWadBaseType.Doom1 && archiveCollection.IWadType != IWadBaseType.ChexQuest)
            offsetY += 8;

        List<IMenuComponent> components = new()
        {
            new MenuImageComponent("M_DOOM", offsetX: 94, paddingTopY: 2, imageAlign: Align.TopLeft, addToOffsetY: false),
            CreateMenuOption("M_NGAME", OffsetX, offsetY, CreateNewGameMenu()),
            CreateMenuOption("M_OPTION", OffsetX, PaddingY, CreateOptionsLayer()),
            CreateMenuOption("M_LOADG", OffsetX, PaddingY, () => new SaveMenu(m_parent, window, config, Console, soundManager, ArchiveCollection, saveManager, screenshotGenerator, false, false, false)),
            CreateMenuOption("M_SAVEG", OffsetX, PaddingY, CreateSaveMenu(saveManager, screenshotGenerator))
        };

        if (archiveCollection.Definitions.MapInfoDefinition.GameDefinition.DrawReadThis)
            components.Add(CreateMenuOption("M_RDTHIS", OffsetX, PaddingY, ShowReadThis()));
        components.Add(CreateMenuOption("M_QUITG", OffsetX, PaddingY, () => new QuitGameMenu(window, config, Console, soundManager, ArchiveCollection)));

        Components = Components.AddRange(components);

        SetToFirstActiveComponent();

        static IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?> action)
        {
            const int MenuItemHeight = 16;
            return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action, imageAlign: Align.TopLeft, overrideY: MenuItemHeight, upscaleWithText: true);
        }
    }

    private Func<Menu?> CreateOptionsLayer()
    {
        return () =>
        {
            m_optionsLayer.Animation.AnimateIn();
            m_parent.Manager.Add(m_optionsLayer);        
            return null;
        };
    }

    private Func<Menu?> CreateSaveMenu(SaveGameManager saveManager, IScreenshotGenerator screenshotGenerator)
    {
        return () =>
        {
            return new SaveMenu(m_parent, Window, Config, Console, SoundManager, ArchiveCollection, saveManager, screenshotGenerator,
                m_parent.Manager.CanSave, true, false);
        };
    }

    private Func<Menu?> CreateNewGameMenu()
    {
        var episodes = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes;
        bool hasEpisodes = episodes.Any(e => !e.PicName.Empty());
        return hasEpisodes ?
            () => new NewGameEpisodeMenu(Window, Config, Console, SoundManager, ArchiveCollection) :
            () => new NewGameSkillMenu(Window,Config, Console, SoundManager, ArchiveCollection, GetDefaultEpisode());
    }

    private string? GetDefaultEpisode()
    {
        if (ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes.Count > 0)
            return ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes[0].StartMap;

        return null;
    }

    private Func<Menu?> ShowReadThis()
    {
        return () =>
        {
            if (m_parent.Manager.ReadThisLayer == null)
            {
                if (ReadThisLayer.TryCreate(m_parent.Manager, m_soundManager, ArchiveCollection, out var layer))
                    m_parent.Manager.Add(layer);
            }

            return null;
        };
    }
}
