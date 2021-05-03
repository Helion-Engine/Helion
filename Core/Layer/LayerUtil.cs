using Helion.Render.Shared.Drawers.Helper;
using System.Collections.Generic;
using System.Linq;

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
