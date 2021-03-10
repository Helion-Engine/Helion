using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Helion.Menus.Base;

namespace Helion.Menus
{
    public class Menu : IEnumerable<IMenuComponent>
    {
        public IMenuComponent? CurrentComponent => ComponentIndex != null ? Components[ComponentIndex.Value] : null;
        protected ImmutableList<IMenuComponent> Components = ImmutableList<IMenuComponent>.Empty;
        protected int? ComponentIndex = null;

        public IEnumerator<IMenuComponent> GetEnumerator() => Components.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
