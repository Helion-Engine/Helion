using System.Collections.Generic;
using Helion.Audio.Sounds;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Window;

namespace Helion.Layer.Images;

public class CycleImageLayer : ImageLayer
{
    private readonly IGameLayerParent m_parent;
    private readonly SoundManager m_soundManager;
    private IList<string> m_images;
    private int m_imageIndex;
    private bool m_initRenderPages;

    public CycleImageLayer(IGameLayerParent parent, SoundManager soundManager, IList<string> images) :
        base(images[0])
    {
        m_parent = parent;
        m_soundManager = soundManager;
        m_images = images;
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (!input.Manager.HasAnyKeyPressed())
            return;

        m_imageIndex++;
        m_soundManager.PlayStaticSound(Constants.MenuSounds.Choose);

        if (m_imageIndex >= m_images.Count)
            m_parent.Remove(this);
        else
            Image = m_images[m_imageIndex];
    }

    public override void Render(IHudRenderContext hud)
    {
        base.Render(hud);

        if (m_initRenderPages)
            return;

        m_initRenderPages = true;
        m_images = LayerUtil.GetRenderPages(hud, m_images, false);
    }
}

