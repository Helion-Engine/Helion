
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Layer
{
    public class CycleImageLayer : ImageLayer
    {
        private readonly GameLayer m_parent;
        private readonly SoundManager m_soundManager;
        private IList<string> m_images;
        private int m_imageIndex;
        private bool m_initRenderPages;

        public CycleImageLayer(GameLayer parent, SoundManager soundManager, IList<string> images)
            : base(images[0])
        {
            m_parent = parent;
            m_soundManager = soundManager;
            m_images = images;
        }

        public override void Render(RenderCommands commands)
        {
            if (!m_initRenderPages)
            {
                m_initRenderPages = true;
                m_images = LayerUtil.GetRenderPages(new(commands), m_images, false);
            }

            base.Render(commands);
        }

        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            if (input.HasAnyKeyPressed())
            {
                m_imageIndex++;
                m_soundManager.PlayStaticSound(Constants.MenuSounds.Choose);
                if (m_imageIndex >= m_images.Count)
                    m_parent.Remove<CycleImageLayer>();
                else            
                    Image = m_images[m_imageIndex];
            }
        }
    }
}
