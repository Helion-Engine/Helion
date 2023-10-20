using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Maps.Shared;
using Helion.Menus.Base;
using Helion.Render.Common.Enums;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;

namespace Helion.Menus.Impl;

public class NewGameSkillMenu : Menu
{
    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly string? m_episode;
    private readonly List<SkillDef> m_confirmSkills = new();
    private SkillDef? m_confirmSkillLevel;

    private const int OffsetX = 48;
    private const int PaddingY = 0;

    public NewGameSkillMenu(IConfig config, HelionConsole console, SoundManager soundManager,
            ArchiveCollection archiveCollection, string? episode) :
        base(config, console, soundManager, archiveCollection, 6, true)
    {
        m_config = config;
        m_console = console;
        m_episode = episode;

        Components = Components.AddRange(new[]
        {
            CreateMenuOption("M_NEWG", 96, 8, overrideY: 16),
            CreateMenuOption("M_SKILL", 54, 8, paddingBottomY: 8, overrideY: 24),
        });

        var defaultSkillDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(SkillLevel.None);
        int indexOffset = 0;
        int index = 0;

        foreach (var skillDef in archiveCollection.Definitions.MapInfoDefinition.MapInfo.Skills)
        {
            if (skillDef == defaultSkillDef)
                indexOffset = index;

            IMenuComponent component;
            string title = ArchiveCollection.Language.GetMessage(skillDef.Name);

            if (skillDef.MustConfirm)
            {
                m_confirmSkills.Add(skillDef);
                component = CreateMenuOption(skillDef.PicName, OffsetX, PaddingY, Confirm(skillDef),
                    overrideY: 16, title: title);
            }
            else
            {
                component = CreateMenuOption(skillDef.PicName, OffsetX, PaddingY, CreateWorld(skillDef),
                    overrideY: 16, title: title);
            }

            Components = Components.Add(component);
            index++;
        }

        // Menu title etc are menu components so offset by the index of the default difficulty
        SetToFirstActiveComponent();
        ComponentIndex += indexOffset;

        IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?>? action = null, int paddingBottomY = 0, 
            int? overrideY = null, bool addToOffsetY = true, string title = "")
        {
            return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action,
                imageAlign: Align.TopLeft, paddingBottomY: paddingBottomY, overrideY: overrideY, addToOffsetY: addToOffsetY, 
                title: title);
        }

        Func<Menu?> Confirm(SkillDef skillDef)
        {
            return () =>
            {
                m_confirmSkillLevel = skillDef;
                string mustConfirmMessage = skillDef.MustConfirmMessage ?? "$NIGHTMARE";
                string[] confirm = ArchiveCollection.Definitions.Language.GetMessages(mustConfirmMessage)
                    .Union(ArchiveCollection.Definitions.Language.GetMessages("$CONFIRM_YN")).ToArray();

                var messageMenu = new MessageMenu(config, Console, soundManager, ArchiveCollection, confirm, true);
                messageMenu.Cleared += MessageMenu_Cleared;
                return messageMenu;
            };
        }

        Func<Menu?> CreateWorld(SkillDef skillDef)
        {
            return () =>
            {
                DoNewGame(skillDef);
                return null;
            };
        }
    }

    private void MessageMenu_Cleared(object? sender, bool confirmed)
    {
        if (confirmed && m_confirmSkillLevel != null)
            DoNewGame(m_confirmSkillLevel);
    }

    private void DoNewGame(SkillDef skillDef)
    {
        m_config.Game.SelectedSkillDefinition = skillDef;
        m_config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
        m_console.SubmitInputText($"map {m_episode ?? "MAP01"}");
    }
}
