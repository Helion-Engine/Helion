using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Menus.Base;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using static Helion.Util.Assertion.Assert;

namespace Helion.Menus
{
    /// <summary>
    /// A menu that can be interacted with.
    /// </summary>
    public abstract class Menu : IEnumerable<IMenuComponent>
    {
        /// <summary>
        /// How many pixels are padded from the top.
        /// </summary>
        public readonly int TopPixelPadding;
        
        /// <summary>
        /// If true, all the contents are evaluated by their width, and then
        /// aligned to the leftmost largest one.
        /// </summary>
        public readonly bool LeftAlign;
        
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly SoundManager SoundManager;
        protected readonly HelionConsole Console;
        protected ImmutableList<IMenuComponent> Components = ImmutableList<IMenuComponent>.Empty;
        protected int? ComponentIndex { get; set; }

        public IMenuComponent? CurrentComponent => ComponentIndex != null ? Components[ComponentIndex.Value] : null;

        protected Menu(Config config, HelionConsole console, SoundManager soundManager, ArchiveCollection archiveCollection,
            int topPixelPadding = 0, bool leftAlign = false)
        {
            Precondition(topPixelPadding >= 0, "Should not have a menu with negative top pixel padding");

            Config = config;
            Console = console;
            SoundManager = soundManager;
            ArchiveCollection = archiveCollection;
            TopPixelPadding = topPixelPadding;
            LeftAlign = leftAlign;
        }

        public void RemoveComponent(IMenuComponent component)
        {
            if (ComponentIndex.HasValue && ComponentIndex.Value == Components.Count - 1 &&
                Components[ComponentIndex.Value] == component)
            {
                ComponentIndex--;
                if (ComponentIndex < 0)
                    ComponentIndex = null;
                if (ComponentIndex != null && Components[ComponentIndex.Value].Action == null)
                    ComponentIndex = null;
            }

            Components = Components.Remove(component);
        }

        public virtual void HandleInput(InputEvent input)
        {
            // Up to any children to handle input if they want.
        }

        protected void PlayNextOptionSound()
        {
            SoundManager.PlayStaticSound(Constants.MenuSounds.Cursor);
        }

        /// <summary>
        /// Moves to the next component that has an action. Wraps around if at
        /// the end of the list.
        /// </summary>
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
                    if (ComponentIndex == null || ComponentIndex != index)
                        PlayNextOptionSound();
                    
                    ComponentIndex = index;
                    return;
                }
            }
        }
        
        /// <summary>
        /// Moves to the previous component that has an action. Wraps around if
        /// it is at the end of the list.
        /// </summary>
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
                    if (ComponentIndex != index)
                        PlayNextOptionSound();
                    
                    ComponentIndex = index;
                    return;
                }
            }
        }
        
        protected void SetToFirstActiveComponent()
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
