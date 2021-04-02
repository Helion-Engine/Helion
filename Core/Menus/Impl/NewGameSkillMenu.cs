﻿using System;
using Helion.Audio.Sounds;
using Helion.Maps.Shared;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using MoreLinq;

namespace Helion.Menus.Impl
{
    public class NewGameSkillMenu : Menu
    {
        private SkillLevel m_confirmSkillLevel = SkillLevel.None;
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly string? m_episode;

        public NewGameSkillMenu(Config config, HelionConsole console, SoundManager soundManager, 
                ArchiveCollection archiveCollection, string? episode) : 
            base(config, console, soundManager, archiveCollection, 16, true)
        {
            m_config = config;
            m_console = console;
            m_episode = episode;

            Components = Components.AddRange(new[] 
            {
                // TODO: X offsets are hardcoded for now (and are probably not even right).
                CreateMenuOption("M_NEWG", 24, 10),
                CreateMenuOption("M_SKILL", 8, 10),
            });

            var defaultSkillDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(SkillLevel.None);
            int indexOffset = 0;

            archiveCollection.Definitions.MapInfoDefinition.MapInfo.Skills.ForEach((skill, index) =>
            {
                SkillLevel skillLevel = (SkillLevel)(index + 1);
                if (skill == defaultSkillDef)
                    indexOffset = index;

                IMenuComponent component;
                if (skill.MustConfirm)
                {
                    m_confirmSkillLevel = skillLevel;
                    component = CreateMenuOption(skill.PicName, 0, 2, Confirm());
                }
                else
                {
                    component = CreateMenuOption(skill.PicName, 0, 2, CreateWorld(skillLevel));
                }

                Components = Components.Add(component);
 
            });

            // Menu title etc are menu components so offset by the index of the default difficulty
            SetToFirstActiveComponent();
            ComponentIndex += indexOffset;

            IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?>? action = null)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action);
            }

            Func<Menu?> Confirm()
            {
                return () =>
                {
                    string[] confirm = ArchiveCollection.Definitions.Language.GetDefaultMessages("$NIGHTMARE");
                    var messageMenu = new MessageMenu(config, Console, soundManager, ArchiveCollection, confirm, true);
                    messageMenu.Cleared += MessageMenu_Cleared;
                    return messageMenu;
                }; 
            }

            Func<Menu?> CreateWorld(SkillLevel skillLevel)
            {
                return () =>
                {
                    DoNewGame(skillLevel);
                    return null;
                };
            }
        }

        private void MessageMenu_Cleared(object? sender, bool confirmed)
        {
            if (confirmed)
                DoNewGame(m_confirmSkillLevel);
        }

        private void DoNewGame(SkillLevel skillLevel)
        {
            m_config.Game.Skill.Set(skillLevel);
            m_console.SubmitInputText($"map {m_episode ?? "MAP01"}");
        }
    }
}
