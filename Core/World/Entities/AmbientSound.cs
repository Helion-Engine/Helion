using Helion.Audio;
using Helion.Resources.Definitions.SoundInfo;
using System;

namespace Helion.World.Entities;

public class AmbientSound : Entity
{
    public AmbientSoundInfo? AmbientSoundInfo;
    public int Ticks;

    public override void Tick()
    {
        base.Tick();

        if (AmbientSoundInfo == null)
            return;

        switch (AmbientSoundInfo.Mode)
        {
            case AmbientSoundMode.Continuous:
                if (Ticks > 0)
                    return;

                Ticks = int.MaxValue;
                CreateAmbientSound(AmbientSoundInfo);
                break;
            case AmbientSoundMode.Periodic:
                if (Ticks > 1)
                {
                    if (AudioSource == null || !AudioSource.IsPlaying())
                        Ticks--;
                    return;
                }

                Ticks = AmbientSoundInfo.MinTicks;
                CreateAmbientSound(AmbientSoundInfo);
                break;
            case AmbientSoundMode.Random:
                {
                    if (Ticks > 1)
                    {
                        if (AudioSource == null || !AudioSource.IsPlaying())
                            Ticks--;
                        return;
                    }

                    if (Ticks == 1)
                        CreateAmbientSound(AmbientSoundInfo);
                    var range = Math.Max(AmbientSoundInfo.MaxTicks - AmbientSoundInfo.MinTicks, 0);
                    Ticks = AmbientSoundInfo.MinTicks + (int)(range * (World.Random.NextByte() / 255f));
                    break;
                }
        }
    }

    private void CreateAmbientSound(AmbientSoundInfo info)
    {
        var attenution = info.Type == AmbientSoundType.Point ? Attenuation.Default : Attenuation.None;
        World.SoundManager.CreateSoundOn(this, info.LogicalSound, new(this, info.Mode == AmbientSoundMode.Continuous, attenution, info.Volume,
            attenuationFactor: info.Attenuation));
    }
}
