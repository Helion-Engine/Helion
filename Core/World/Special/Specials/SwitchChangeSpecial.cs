using Helion.Audio;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Special.Switches;

namespace Helion.World.Special.Specials
{
    public class SwitchChangeSpecial : ISpecial
    {
        private readonly SwitchManager m_manager;
        private readonly WorldSoundManager m_soundManager;
        private readonly Line m_line;
        private bool m_repeat;
        private int m_switchDelayTics;

        public SwitchChangeSpecial(SwitchManager manager, WorldSoundManager soundManager, Line line, SwitchType type)
        {
            m_manager = manager;
            m_soundManager = soundManager;
            m_line = line;
            m_repeat = line.Flags.Repeat;

            if (type == SwitchType.Exit)
            {
                // The level is about to exit so everything will be stopped
                // Force play the switch exit sound and Tick to switch the line texture
                soundManager.PlayStaticSound(Constants.SwitchExitSound);
                Tick();
            }
            else
            {
                PlaySwitchSound(soundManager, line);
            }
        }

        public SpecialTickStatus Tick()
        {
            if (m_switchDelayTics > 0)
            {
                m_switchDelayTics--;
                return SpecialTickStatus.Continue;
            }

            m_manager.SetLineSwitch(m_line);

            if (m_repeat)
            {
                m_switchDelayTics = 35;
                m_repeat = false;
                return SpecialTickStatus.Continue;
            }

            if (m_line.Flags.Repeat)
            {
                m_line.SetActivated(false);
                PlaySwitchSound(m_soundManager, m_line);
            }

            return SpecialTickStatus.Destroy;
        }

        public void Use(Entity entity)
        {
        }

        private static void PlaySwitchSound(WorldSoundManager soundManager, Line line)
        {
            Vec2D pos = line.Segment.FromTime(0.5);
            DefaultSoundSource soundSource = new DefaultSoundSource(pos.To3D(line.Front.Sector.ToFloorZ(pos)));
            soundManager.CreateSoundOn(soundSource, Constants.SwitchNormSound, SoundChannelType.Auto, new SoundParams(soundSource));
        }
    }
}