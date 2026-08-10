using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Special.Switches;

namespace Helion.World.Special.Specials;

public class SwitchChangeSpecial : DefaultSoundSource, ISpecial
{
    private const int SwitchDelayTicks = 35;

    public IWorld World;
    public Line Line;
    private bool m_init = true;
    private bool m_repeat;
    private int m_switchDelayTics;
    private int m_startTextureHandle = Constants.NoTextureIndex;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SwitchChangeSpecial(IWorld world, Line line, SwitchType type)
    {
        Set(world, line, type);
    }

    public SwitchChangeSpecial(IWorld world, Line line, SwitchChangeSpecialModel model)
    {
        Set(world, line, model);
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public void Set(IWorld world, Line line, SwitchType type)
    {
        World = world;
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

    public void Set(IWorld world, Line line, SwitchChangeSpecialModel model)
    {
        World = world;
        Line = line;
        m_repeat = model.Repeat;
        m_switchDelayTics = model.Tics;
        if (model.Texture == Constants.NoTextureIndex)
            m_startTextureHandle = SwitchManager.GetLineLineSwitchTexture(World.ArchiveCollection, Line, SwitchTextureType.Off).TextureHandle;
    }

    public void Free()
    {
        World = null!;
        m_init = true;
        m_startTextureHandle = Constants.NoTextureIndex;
        m_switchDelayTics = 0;
    }

    public SwitchChangeSpecialModel ToSpecialModel()
    {
        return new()
        {
            LineId = Line.Id,
            Repeat = m_repeat,
            Tics = m_switchDelayTics,
            Texture = m_startTextureHandle
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

        if (m_init)
        {
            m_startTextureHandle = SwitchManager.GetLineLineSwitchTexture(World.ArchiveCollection, Line, SwitchTextureType.Current).TextureHandle;
            SwitchManager.SetLineSwitch(World, Line, SwitchTextureType.Flip);
        }
        else
        {
            if (m_startTextureHandle != Constants.NoTextureIndex)
                SwitchManager.SetLineSwitch(World, Line, m_startTextureHandle);
        }

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
            PlaySwitchSound(World.SoundManager, Line);
        }

        return SpecialTickStatus.Destroy;
    }

    public void Toggle()
    {
        if (!Line.Flags.Repeat)
            return;

        PlaySwitchSound(World.SoundManager, Line);
        SwitchManager.SetLineSwitch(World, Line, SwitchTextureType.Flip);
    }

    public bool Use(Entity entity)
    {
        return false;
    }

    private void PlaySwitchSound(WorldSoundManager soundManager, Line line)
    {
        Vec2D pos = line.Segment.FromTime(0.5);
        SetPosition(pos.To3D(line.Front.Sector.ToFloorZ(pos)));
        soundManager.CreateSoundOn(this, Constants.SwitchNormSound, new SoundParams(this));
    }
}
