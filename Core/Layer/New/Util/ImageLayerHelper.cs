using System.Collections.Generic;
using System.Linq;
using Helion.Render.Common.Renderers;
using Helion.Resources;

namespace Helion.Layer.New.Util;

public static class ImageLayerHelper
{
    public static void InitRenderPages(this List<string> pages, IHudRenderContext hud, bool repeatIfNotExists, ref bool initRenderPages)
    {
        initRenderPages = true;
        
        if (pages.Count == 0)
            return;

        string lastPage = pages[0];
        List<string> newPages = new() { lastPage };

        foreach (string page in pages.Skip(1))
        {
            if (!hud.Textures.HasImage(page, ResourceNamespace.Undefined))
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

        pages.Clear();
        pages.AddRange(newPages);
    }
}