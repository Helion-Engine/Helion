using Helion.Menus;
using Helion.Menus.Base;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;

namespace Helion.Render.Shared.Drawers
{
    public class MenuDrawer
    {
        private readonly ArchiveCollection m_archiveCollection;

        public MenuDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public void Draw(Menu menu, RenderCommands renderCommands)
        {
            foreach (IMenuComponent menuComponent in menu)
            {
                // TODO
            }
        }
    }
}
