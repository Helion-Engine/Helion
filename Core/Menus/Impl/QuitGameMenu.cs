using System;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus.Base;
using Helion.Menus.Base.Text;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;

namespace Helion.Menus.Impl
{
    public class QuitGameMenu : Menu
    {
        private readonly Func<Menu?> m_quitAction;
        
        public QuitGameMenu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection) :
            base(config, console, soundManager, archiveCollection, 90)
        {
            m_quitAction = () =>
            {
                Console.ClearInputText();
                Console.AddInput("exit");
                Console.SubmitInputText();
                return null;
            };
            
            Components = Components.AddRange(new IMenuComponent[]
            {
                new MenuSmallTextComponent("This is unacceptable."),
                new MenuPaddingComponent(8),
                new MenuSmallTextComponent("Press Enter to quit.", m_quitAction)
            });

            SetToFirstActiveComponent();
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.ConsumeKeyPressed(Key.Y))
                m_quitAction();
            
            base.HandleInput(input);
        }
    }
}
