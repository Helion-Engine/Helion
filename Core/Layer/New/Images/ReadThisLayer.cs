using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Sounds;
using Helion.Graphics;
using Helion.Layer.New.Util;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Window;

namespace Helion.Layer.New.Images;

public class ReadThisLayer(ArchiveCollection archiveCollection, SoundManager soundManager) : GameLayer
{
    protected override double Priority => (double)LayerPriority.ReadThis;
    private readonly List<string> m_images = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.InfoPages.ToList();
    private int m_imageIndex;
    private bool m_initRenderPages;

    public override bool? ShouldFocus()
    {
        return false;
    }

    public override void HandleInput(IConsumableInput input)
    {
        if (!input.Manager.HasAnyKeyPressed())
            return;
        
        input.ConsumeAll();

        m_imageIndex++;
        soundManager.PlayStaticSound(Constants.MenuSounds.Choose);
    }

    public override void RunLogic()
    {
        if (m_imageIndex >= m_images.Count)
            Dispose();
    }

    public override void Render(IHudRenderContext ctx)
    {
        if (!m_initRenderPages)
            m_images.InitRenderPages(ctx, repeatIfNotExists: false, ref m_initRenderPages);
        
        if (m_imageIndex >= m_images.Count)
            return;
        
        ctx.Clear(Color.Black);
        ctx.DoomVirtualResolution(_ =>
        {
            ctx.Image(m_images[m_imageIndex], (0, 0));
        }, 0);
    }
}