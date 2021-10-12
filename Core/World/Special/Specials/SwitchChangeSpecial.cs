using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Special.Switches;

namespace Helion.World.Special.Specials;

public class SwitchChangeSpecial : ISpecial
{
    private readonly IWorld m_world;
    private readonly Line m_line;
    private bool m_repeat;
    private int m_switchDelayTics;

    public SwitchChangeSpecial(IWorld world, Line line, SwitchType type)
    {
        m_world = world;
        m_line = line;
        m_repeat = line.Flags.Repeat;

        if (type == SwitchType.Exit)
        {
            // The level is about to exit so everything will be stopped
            // Force play the switch exit sound
            world.SoundManager.PlayStaticSound(Constants.SwitchExitSound);
        }
        else
        {
            PlaySwitchSound(world.SoundManager, line);
        }
    }

    public SwitchChangeSpecial(IWorld world, Line line, SwitchChangeSpecialModel model)
    {
        m_world = world;
        m_line = line;
        m_repeat = model.Repeat;
        m_switchDelayTics = model.Tics;
    }

    public ISpecialModel ToSpecialModel()
    {
        return new SwitchChangeSpecialModel()
        {
            LineId = m_line.Id,
            Repeat = m_repeat,
            Tics = m_switchDelayTics
        };
    }

    public SpecialTickStatus Tick()
    {
        if (m_switchDelayTics > 0)
        {
            m_switchDelayTics--;
            return SpecialTickStatus.Continue;
        }

        SwitchManager.SetLineSwitch(m_world.ArchiveCollection.Definitions, m_line);

        if (m_repeat)
        {
            m_switchDelayTics = 35;
            m_repeat = false;
            return SpecialTickStatus.Continue;
        }

        if (m_line.Flags.Repeat)
        {
            m_line.SetActivated(false);
            PlaySwitchSound(m_world.SoundManager, m_line);
        }

        return SpecialTickStatus.Destroy;
    }

    public bool Use(Entity entity)
    {
        return false;
    }

    private static void PlaySwitchSound(WorldSoundManager soundManager, Line line)
    {
        Vec2D pos = line.Segment.FromTime(0.5);
        DefaultSoundSource soundSource = new DefaultSoundSource(pos.To3D(line.Front.Sector.ToFloorZ(pos)));
        soundManager.CreateSoundOn(soundSource, Constants.SwitchNormSound, SoundChannelType.Auto,
            DataCache.Instance.GetSoundParams(soundSource));
    }
}

