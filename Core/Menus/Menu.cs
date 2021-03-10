using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Helion.Menus.Base;
using static Helion.Util.Assertion.Assert;

namespace Helion.Menus
{
    public abstract class Menu : IEnumerable<IMenuComponent>
    {
        public readonly int TopPixelPadding;
        protected ImmutableList<IMenuComponent> Components = ImmutableList<IMenuComponent>.Empty;
        protected int? ComponentIndex;

        public IMenuComponent? CurrentComponent => ComponentIndex != null ? Components[ComponentIndex.Value] : null;

        protected Menu(int topPixelPadding)
        {
            Precondition(topPixelPadding >= 0, "Should not have a menu with negative top pixel padding");
            
            TopPixelPadding = topPixelPadding;
        }

        public void MoveToNextComponent()
        {
            Console.WriteLine(":)");
            
            int currentIndex = ComponentIndex ?? 0;

            for (int iter = 0; iter < Components.Count; iter++)
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
            // TODO
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
