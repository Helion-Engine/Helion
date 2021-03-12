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
            
            archiveCollection.Definitions.MapInfoDefinition.MapInfo.Skills.ForEach((skill, index) =>
            {
                SkillLevel skillLevel = (SkillLevel)index;
                IMenuComponent component = CreateMenuOption(skill.PicName, 0, 2, CreateWorld(skillLevel));
                Components = Components.Add(component);
            });

            SetToFirstActiveComponent();

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

                    console.ClearInputText();
                    console.AddInput($"map {episode ?? "MAP01"}");
                    console.SubmitInputText();
                    
                    return null;
                };
            }
        }
    }
}
