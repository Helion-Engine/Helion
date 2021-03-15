
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Layer
{
    public class CycleImageLayer : ImageLayer
    {
        private readonly GameLayer m_parent;
        private readonly SoundManager m_soundManager;
        private readonly IList<string> m_images;
        private int m_imageIndex;

        public CycleImageLayer(GameLayer parent, SoundManager soundManager, IList<string> images)
            : base(images[0])
        {
            m_parent = parent;
            m_soundManager = soundManager;
            m_images = images;
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
