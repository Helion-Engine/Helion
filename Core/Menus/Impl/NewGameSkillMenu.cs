using System;
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
        public NewGameSkillMenu(Config config, HelionConsole console, SoundManager soundManager, 
                ArchiveCollection archiveCollection, string? episode) : 
            base(config, console, soundManager, archiveCollection, 16, true)
        {
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
                IMenuComponent component = CreateMenuOption(skill.PicName, 0, 2, CreateWorld(skillLevel));
                Components = Components.Add(component);
                var currentSkill = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skillLevel);
                if (currentSkill != null && currentSkill == defaultSkillDef)
                    indexOffset = index;
            });

            // Menu title etc are menu components so offset by the index of the default difficulty
            SetToFirstActiveComponent();
            ComponentIndex += indexOffset;

            IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?>? action = null)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action);
            }

            Func<Menu?> CreateWorld(SkillLevel skillLevel)
            {
                return () =>
                {
                    PlaySelectedSound();
                    config.Game.Skill.Set(skillLevel);
                    console.SubmitInputText($"map {episode ?? "MAP01"}");
                    return null;
                };
            }
        }
    }
}
