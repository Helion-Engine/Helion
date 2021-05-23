using System.Collections.Generic;
using System.Linq;
using Helion.Render.OpenGL.Legacy.Shared.Drawers.Helper;

namespace Helion.Layer
{
    public static class LayerUtil
    {
        public static IList<string> GetRenderPages(DrawHelper draw, IList<string> pages,
            bool repeatIfNotExists)
        {
            if (pages.Count == 0)
                return pages;

            string lastPage = pages[0];
            List<string> newPages = new() { lastPage };

            foreach (string page in pages.Skip(1))
            {
                if (!draw.DrawInfoProvider.ImageExists(page))
                {
                    if (repeatIfNotExists)
                        newPages.Add(lastPage);
                }
                else
                {
                    lastPage = page;
                    newPages.Add(page);
                }
            }

            return newPages;
        }
    }
}
