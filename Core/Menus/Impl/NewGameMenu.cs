using System;
using Helion.Maps.Shared;
using Helion.Menus.Base;
using Helion.Util.Configs;
using Helion.Util.Consoles;

namespace Helion.Menus.Impl
{
    public class NewGameMenu : Menu
    {
        public NewGameMenu(Config config, HelionConsole console) : 
            base(config, console, 16)
        {
            Components = Components.AddRange(new[] 
            {
                CreateMenuOption("M_NEWG", 0, 10),
                CreateMenuOption("M_SKILL", 0, 10),
                CreateMenuOption("M_JKILL", -1, 2, CreateWorld(SkillLevel.VeryEasy)),
                CreateMenuOption("M_ROUGH", 0, 2, CreateWorld(SkillLevel.Easy)),
                CreateMenuOption("M_HURT", -32, 2, CreateWorld(SkillLevel.Medium)),
                CreateMenuOption("M_ULTRA", -25, 2, CreateWorld(SkillLevel.Hard)),
                CreateMenuOption("M_NMARE", -61, 2, CreateWorld(SkillLevel.Nightmare))
            });

            SetToFirstActiveComponent();

            IMenuComponent CreateMenuOption(string image, int offsetX, int paddingY, Func<Menu?>? action = null)
            {
                return new MenuImageComponent(image, offsetX, paddingY, "M_SKULL1", "M_SKULL2", action);
            }

            Func<Menu?> CreateWorld(SkillLevel skillLevel)
            {
                config.Game.Skill.Set(skillLevel);
                
                return () =>
                {
                    console.ClearInputText();
                    console.AddInput("STARTGAME");
                    console.SubmitInputText();
                    return null;
                };
            }
        }
    }
}
