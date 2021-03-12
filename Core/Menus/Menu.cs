using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Helion.Audio;
using Helion.Menus.Base;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using static Helion.Util.Assertion.Assert;

namespace Helion.Menus
{
    public abstract class Menu : IEnumerable<IMenuComponent>
    {
        public readonly int TopPixelPadding;
        protected readonly Config Config;
        protected readonly HelionConsole Console;
        protected ImmutableList<IMenuComponent> Components = ImmutableList<IMenuComponent>.Empty;
        protected int? ComponentIndex;

        public IMenuComponent? CurrentComponent => ComponentIndex != null ? Components[ComponentIndex.Value] : null;

        protected Menu(Config config, HelionConsole console, int topPixelPadding)
        {
            Precondition(topPixelPadding >= 0, "Should not have a menu with negative top pixel padding");

            Config = config;
            Console = console;
            TopPixelPadding = topPixelPadding;
        }

        public void MoveToNextComponent()
        {
            // We want to searching at the element after the current one, but
            // if it's the case where we have nothing selected, we want to keep
            // the logic the same. To do this, we start at -1 if there is no
            // index so we force it onto the first element (which may be 0).
            int currentIndex = ComponentIndex ?? -1;

            for (int iter = 1; iter <= Components.Count; iter++)
            {
                int index = (currentIndex + iter) % Components.Count;
                if (Components[index].HasAction)
                {
                    ComponentIndex = index;
                    return;
                }
            }
        }
        
        public void MoveToPreviousComponent()
        {
            if (ComponentIndex == null)
            {
                MoveToNextComponent();
                return;
            }
            
            for (int iter = 1; iter <= Components.Count; iter++)
            {
                int index = (ComponentIndex.Value - iter) % Components.Count;
                if (index < 0)
                    index += Components.Count;
                
                if (Components[index].HasAction)
                {
                    ComponentIndex = index;
                    return;
                }
            }
        }
        
        public void SetToFirstActiveComponent()
        {
            ComponentIndex = null;

            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i].HasAction)
                {
                    ComponentIndex = i;
                    return;
                }
            }
        }
        
        public IEnumerator<IMenuComponent> GetEnumerator() => Components.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
