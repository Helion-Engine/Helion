using Helion.Audio;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Special.Switches;

namespace Helion.World.Special.Specials
{
    public class SwitchChangeSpecial : ISpecial
    {
        private readonly SwitchManager m_manager;
        private readonly SoundManager m_soundManager;
        private readonly Line m_line;
        private bool m_repeat;
        private int m_switchDelayTics;

        public SwitchChangeSpecial(SwitchManager manager, SoundManager soundManager, Line line, SwitchType type)
        {
            m_manager = manager;
            m_soundManager = soundManager;
            m_line = line;
            m_repeat = line.Flags.Repeat;

            if (type == SwitchType.Exit)
            {
                // The level is about to exit so everything will be stopped
                // Force play the switch exit sound and Tick to switch the line texture
                IAudioSource? sound = soundManager.CreateSoundAt(Vec3D.Zero, Constants.SwitchExitSound, 
                    SoundChannelType.Auto, new SoundParams(Attenuation.None));
                sound?.Play();
                Tick();
            }
            else
            {
                Vec2D pos = line.Segment.FromTime(0.5);
                soundManager.CreateSoundAt(pos.To3D(line.Front.Sector.ToFloorZ(pos)), Constants.SwitchNormSound,
                    SoundChannelType.Auto, new SoundParams(Attenuation.Default));
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
                m_line.Activated = false;
                Vec2D pos = m_line.Segment.FromTime(0.5);
                m_soundManager.CreateSoundAt(pos.To3D(m_line.Front.Sector.ToFloorZ(pos)), Constants.SwitchNormSound, SoundChannelType.Auto, new SoundParams(Attenuation.Default));
            }

            return SpecialTickStatus.Destroy;
        }

        public void Use()
        {
        }
    }
}