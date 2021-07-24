using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Layer;
using Helion.Menus.Base;
using Helion.Render.Legacy.Commands.Alignment;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.World.Save;

namespace Helion.Menus.Impl
{
    public class MainMenu : Menu
    {
        private const int OffsetX = 97;
        private const int PaddingY = 1;
     
        private readonly IGameLayer m_parent;

        public MainMenu(IGameLayer parent, Config config, HelionConsole console, SoundManager soundManager,
            ArchiveCollection archiveCollection, SaveGameManager saveManager)
            : base(config, console, soundManager, archiveCollection, 0)
        {
            m_parent = parent;

            int offsetY = 64;
            if (archiveCollection.IWadType != IWadBaseType.Doom1 && archiveCollection.IWadType != IWadBaseType.ChexQuest)
                offsetY += 8;

            List<IMenuComponent> components = new();

            components.Add(new MenuImageComponent("M_DOOM", offsetX: 94, paddingTopY: 2, imageAlign: Align.TopLeft, addToOffsetY: false));
            components.Add(CreateMenuOption("M_NGAME", OffsetX, offsetY, CreateNewGameMenu()));
            components.Add(CreateMenuOption("M_OPTION", OffsetX, PaddingY, () => new OptionsMenu(config, Console, soundManager, ArchiveCollection)));
            components.Add(CreateMenuOption("M_LOADG", OffsetX, PaddingY, () => new SaveMenu(m_parent, config, Console, soundManager, ArchiveCollection, saveManager, false, false)));
            components.Add(CreateMenuOption("M_SAVEG", OffsetX, PaddingY, CreateSaveMenu(saveManager)));

            if (archiveCollection.Definitions.MapInfoDefinition.GameDefinition.DrawReadThis)
                components.Add(CreateMenuOption("M_RDTHIS", OffsetX, PaddingY, ShowReadThis()));
            components.Add(CreateMenuOption("M_QUITG", OffsetX, PaddingY, () => new QuitGameMenu(config, Console, soundManager, ArchiveCollection)));

            Components = Components.AddRange(components);

            SetToFirstActiveComponent();

            static IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?> action)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action, imageAlign: Align.TopLeft);
            }
        }

        private Func<Menu?> CreateSaveMenu(SaveGameManager saveManager)
        {
            return () =>
            {
                throw new NotImplementedException("TODO");
                // return GameLayerManager.CreateSaveMenu(m_parent, Config, Console, SoundManager, ArchiveCollection, saveManager, true);
            };
        }

        private Func<Menu?> CreateNewGameMenu()
        {
            bool hasEpisodes = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes.Any(e => !e.PicName.Empty());
            return hasEpisodes ?
                () => new NewGameEpisodeMenu(Config, Console, SoundManager, ArchiveCollection) :
                () => new NewGameSkillMenu(Config, Console, SoundManager, ArchiveCollection, GetDefaultEpisode());
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
                throw new NotImplementedException("TODO");
                // if (m_parent.Contains<ImageLayer>())
                //     return null;
                // if (ArchiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages.Count == 0)
                //     return null;
                //
                // m_parent.Add(new CycleImageLayer(m_parent, SoundManager, ArchiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages));
                // return null;
            };
        }
    }
}
