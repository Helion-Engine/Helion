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
    private const int SwitchDelayTicks = 35;

    private readonly IWorld m_world;
    public  Line Line { get; private set; }
    private bool m_init = true;
    private bool m_repeat;
    private int m_switchDelayTics;

    public SwitchChangeSpecial(IWorld world, Line line, SwitchType type)
    {
        m_world = world;
        Line = line;
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
        Line = line;
        m_repeat = model.Repeat;
        m_switchDelayTics = model.Tics;
    }

    public ISpecialModel ToSpecialModel()
    {
        return new SwitchChangeSpecialModel()
        {
            LineId = Line.Id,
            Repeat = m_repeat,
            Tics = m_switchDelayTics
        };
    }

    public void ResetDelay()
    {
        m_switchDelayTics = SwitchDelayTicks;
    }

    public SpecialTickStatus Tick()
    {
        if (m_switchDelayTics > 0)
        {
            m_switchDelayTics--;
            return SpecialTickStatus.Continue;
        }

        SwitchManager.SetLineSwitch(m_world.ArchiveCollection, Line, !m_init);

        if (m_repeat)
        {
            m_switchDelayTics = SwitchDelayTicks;
            m_repeat = false;
            m_init = false;
            return SpecialTickStatus.Continue;
        }

        if (Line.Flags.Repeat)
        {
            Line.SetActivated(false);
            PlaySwitchSound(m_world.SoundManager, Line);
        }

        return SpecialTickStatus.Destroy;
    }

    public bool Use(Entity entity)
    {
        return false;
    }

    private void PlaySwitchSound(WorldSoundManager soundManager, Line line)
    {
        Vec2D pos = line.Segment.FromTime(0.5);
        DefaultSoundSource soundSource = new(pos.To3D(line.Front.Sector.ToFloorZ(pos)));
        soundManager.CreateSoundOn(soundSource, Constants.SwitchNormSound, SoundChannelType.Auto,
            m_world.DataCache.GetSoundParams(soundSource));
    }
}
