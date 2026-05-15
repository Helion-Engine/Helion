using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Resources.Archives.Entries;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Sound;
using System;

namespace Helion.World.Special.Specials;

public struct QuakeSpecial : ISpecial
{
    public readonly IWorld m_world;
    public readonly double Intensity;
    public readonly int DamageRadius;
    public readonly int TremorRadius;
    public readonly WeakEntity Location;
    public readonly string Sound;
    public int Duration;

    private readonly Entity? m_quakeEntity;

    public QuakeSpecial(IWorld world, double intensity, int duration, int damageRadius, int tremorRadius, Entity location, string sound)
    {
        m_world = world;
        Intensity = Math.Clamp(intensity, 0, 9);
        DamageRadius = damageRadius;
        TremorRadius = tremorRadius;
        Location = new(location);
        Sound = sound;
        Duration = duration;

        if (Sound.Length > 0)
        {
            m_quakeEntity = world.EntityManager.Create("HelionSoundObject", Vec3D.Zero);
            if (m_quakeEntity != null)
                m_world.SoundManager.CreateSoundOn(m_quakeEntity, Sound, new(m_quakeEntity, loop: true, channel: SoundChannel.Default));
        }
    }

    public QuakeSpecialModel ToSpecialModel()
    {
        return new()
        {
            Intensity = Intensity,
            DamageRadius = DamageRadius,
            TremorRadius = TremorRadius,
            EntityId = Location.Get()?.Id ?? -1,
            Sound = Sound,
            Duration = Duration
        };
    }

    public SpecialTickStatus Tick()
    {
        var entity = Location.Get();
        if (entity == null)
        {
            HandleDestroy();
            return SpecialTickStatus.Destroy;
        }

        m_quakeEntity?.Position = entity.Position;

        foreach (var player in m_world.EntityManager.Players)
        {
            var distance = player.Position.ApproximateDistance2D(entity.Position);
            if (distance < TremorRadius)
                m_world.SpecialManager.RegisterQuake(player, new(Intensity, (Intensity, Intensity, 0)));

            if (distance < DamageRadius && player.Position.Z <= player.HighestFloorZ)
            {
                if (m_world.Random.NextByte() < 50)
                    m_world.DamageEntity(player, null, m_world.Random.NextHitDice(1), DamageType.AlwaysApply);

                var angle = m_world.Random.NextAngle();
                m_world.ApplyVelocity(player, player.Velocity + Vec3D.UnitSphere(angle, 0) * Intensity);
            }
        }

        Duration--;
        if (Duration <= 0)
        {
            HandleDestroy();
            return SpecialTickStatus.Destroy;
        }

        return SpecialTickStatus.Continue; 
    }

    private readonly void HandleDestroy()
    {
        if (Sound.Length > 0 && m_quakeEntity != null)
            m_world.SoundManager.StopSoundBySource(m_quakeEntity, SoundChannel.Default, Sound);

        if (m_quakeEntity != null)
            m_world.EntityManager.Destroy(m_quakeEntity);
    }

    public readonly bool Use(Entity entity)
    {
        return false;
    }
}
