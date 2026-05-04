using Helion.Audio;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using System.IO;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigAudio: ConfigElement<ConfigAudio>
{
    [ConfigInfo("Music volume for FluidSynth MIDI. 0.0 is Off, 2.0 is Maximum.")]
    [OptionMenu(OptionSectionType.Audio, "Music Volume (FluidSynth)", sliderMin: 0, sliderMax: 2.0, sliderStep: .05)]
    public readonly ConfigValue<double> MusicVolume = new(1.0, Clamp(0, 2.0));

    [ConfigInfo("Music volume for OPL, MOD, MP3, etc. 0.0 is Off, 2.0 is Maximum.")]
    [OptionMenu(OptionSectionType.Audio, "Music Volume (Other)", sliderMin: 0, sliderMax: 2.0, sliderStep: .05)]
    public readonly ConfigValue<double> ZMusicVolume = new(1.0, Clamp(0, 2.0));

    [ConfigInfo("Sound effect volume. 0.0 is Off, 2.0 is Maximum.")]
    [OptionMenu(OptionSectionType.Audio, "Sound Volume", sliderMin: 0, sliderMax: 2.0, sliderStep: .05)]
    public readonly ConfigValue<double> SoundVolume = new(1.0, Clamp(0, 2.0));

    [ConfigInfo("Enables sound velocity.")]
    [OptionMenu(OptionSectionType.Audio, "Sound Velocity", spacer: true)]
    public readonly ConfigValue<bool> Velocity = new(false);

    [ConfigInfo("Maximum number of sounds that can be played at once.")]
    [OptionMenu(OptionSectionType.Audio, "Max Sounds")]
    public readonly ConfigValue<int> MaxSounds = new(32, GreaterOrEqual(1));

    [ConfigInfo("Limit same sounds. 0 = off.")]
    [OptionMenu(OptionSectionType.Audio, "Same Sound Limit")]
    public readonly ConfigValue<int> SameSoundLimit = new(0, GreaterOrEqual(0));

    [ConfigInfo("Limit same sounds window in ticks.")]
    [OptionMenu(OptionSectionType.Audio, "Same Sound Window")]
    public readonly ConfigValue<int> SameSoundWindow = new(1, GreaterOrEqual(1));

    [ConfigInfo("Which audio resampler to use when playing sounds.")]
    [OptionMenu(OptionSectionType.Audio, "Sound Resampler", isDynamicStringCycle: true)]
    public readonly ConfigValue<string> Resampler = new("Default");

    [ConfigInfo("Randomize sound pitch.")]
    [OptionMenu(OptionSectionType.Audio, "Randomize Pitch", spacer: true)]
    public readonly ConfigValue<RandomPitch> RandomizePitch = new(RandomPitch.None);

    [ConfigInfo("Randomized pitch scale value.")]
    [OptionMenu(OptionSectionType.Audio, "Random Pitch Scale", sliderMin: 0.1, sliderMax: 10, sliderStep: .1)]
    public readonly ConfigValue<double> RandomPitchScale = new(1, Clamp(0.1, 10));

    [ConfigInfo("Scale for sound pitch.")]
    [OptionMenu(OptionSectionType.Audio, "Pitch Scale", sliderMin: 0.1, sliderMax: 10, sliderStep: .1)]
    public readonly ConfigValue<double> Pitch = new(1, Clamp(0.1, 10));

    [ConfigInfo("Log sound errors.")]
    [OptionMenu(OptionSectionType.Audio, "Log Sound Errors", spacer: true)]
    public readonly ConfigValue<bool> LogErrors = new(false);

    [ConfigInfo("Main device to use for audio.")]
    public readonly ConfigValue<string> Device = new(IAudioSystem.DefaultAudioDevice);

    [ConfigInfo("Synthesizer to use for music.")]
    [OptionMenu(OptionSectionType.Audio, "Music Synthesizer")]
    public readonly ConfigValue<Synth> Synthesizer = new(Synth.FluidSynth);

    [ConfigInfo("SoundFont file to use for MIDI/MUS music playback (FluidSynth only).")]
    [OptionMenu(OptionSectionType.Audio, "SoundFont File", dialogType: DialogType.SoundFontPicker)]
    public readonly ConfigValue<string> SoundFontFile = new($"SoundFonts{Path.DirectorySeparatorChar}Default.sf2");

    [ConfigInfo("Enable chorus effect in MIDI/MUS playback (FluidSynth only).")]
    [OptionMenu(OptionSectionType.Audio, "Enable Chorus")]
    public readonly ConfigValue<bool> EnableChorus = new(true);

    [ConfigInfo("Enable reverb effect in MIDI/MUS playback (FluidSynth only).")]
    [OptionMenu(OptionSectionType.Audio, "Enable Reverb")]
    public readonly ConfigValue<bool> EnableReverb = new(true);

    // Music volume is treated as a multiple of sound effects volume, because effects volume controls the master gain.
    public double FluidSynthVolumeNormalized => SoundVolume == 0 ? MusicVolume : (MusicVolume / SoundVolume);
    public double DefaultMusicVolumeNormalized => SoundVolume == 0 ? ZMusicVolume : (ZMusicVolume / SoundVolume);
}
