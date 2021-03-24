using System;

namespace Helion.Menus.Base
{
    public interface IMenuComponent
    {
        /// <summary>
        /// An action that can be performed if the user chooses to (for example
        /// if the user presses enter while hovering over this). If null, this
        /// means it has no action. The function will do stuff, and potentially
        /// return a menu in case it opens another sub-menu. If no new sub-menu
        /// is generated, it will return null.
        /// </summary>
        Func<Menu?>? Action { get; }

        Func<Menu?>? DeleteAction { get { return null; } }

        /// <summary>
        /// True if this has an action, false if not.
        /// </summary>
        bool HasAction => Action != null;

        bool PlaySelectedSound { get { return true; } }
    }
}
