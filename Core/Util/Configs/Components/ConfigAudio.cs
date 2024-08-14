using Helion.Audio;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigAudio
{
    [ConfigInfo("Music volume. 0.0 is Off, 1.0 is Maximum.")]
    [OptionMenu(OptionSectionType.Audio, "Music Volume")]
    public readonly ConfigValue<double> MusicVolume = new(1.0, ClampNormalized);

    [ConfigInfo("Sound effect volume. 0.0 is Off, 1.0 is Maximum.")]
    [OptionMenu(OptionSectionType.Audio, "Sound Volume")]
    public readonly ConfigValue<double> SoundVolume = new(1.0, ClampNormalized);

    [ConfigInfo("Overall volume. 0.0 is Off, 1.0 is Maximum.")]
    public readonly ConfigValue<double> Volume = new(1.0, ClampNormalized);

    [ConfigInfo("Randomize sound pitch.")]
    [OptionMenu(OptionSectionType.Audio, "Randomize Pitch", spacer: true)]
    public readonly ConfigValue<RandomPitch> RandomizePitch = new(RandomPitch.None);

    [ConfigInfo("Randomized pitch scale value.")]
    [OptionMenu(OptionSectionType.Audio, "Random Pitch Scale")]
    public readonly ConfigValue<double> RandomPitchScale = new(1, Clamp(0.1, 10));

    [ConfigInfo("Scale sound pitch.")]
    [OptionMenu(OptionSectionType.Audio, "Pitch Scale")]
    public readonly ConfigValue<double> Pitch = new(1, Clamp(0.1, 10));

    [ConfigInfo("Log sound errors.")]
    [OptionMenu(OptionSectionType.Audio, "Log Sound Errors", spacer: true)]
    public readonly ConfigValue<bool> LogErrors = new(false);

    [ConfigInfo("Main device to use for audio.")]
    public readonly ConfigValue<string> Device = new(IAudioSystem.DefaultAudioDevice);
}
