using System;

namespace NFluidsynth
{
    // settings.h

    [Flags]
    public enum FluidHint
    {
        BoundedBelow = 1,
        BoundedAbove = 2,
        Toggled = 4,
        SampleRate = 8,
        Logarithmic = 0x10,
        Integer = 0x20,
        FileName = 1,
        OptionList = 2,
    }

    public enum FluidTypes
    {
        NoType = -1,
        NumericType,
        IntegerType,
        StringType,
        SetType,
    }

    // synth.h

    public enum FluidChorusMod
    {
        Sine = 0,
        Triangle = 1,
    }

    public enum FluidInterpolation
    {
        None = 0,
        Linear = 1,
        FourthOrder = 4,
        SeventhOrder = 7,
    }

    // midi.h

    public enum FluidMidiRouterRuleType
    {
        Note,
        CC,
        ProgramChange,
        PitchBend,
        ChannelPressure,
        KeyPressure,
        TotalCount, // huh, doesn't make sense as "real" enum
    }

    public enum FluidPlayerStatus
    {
        Ready,
        Playing,
        Done,
    }

    // event.h

    public enum FluidSequencerEventType
    {
        Note = 0,
        NoteOn,
        NoteOff,
        AllSoundsOff,
        AllNotesOff,
        BankSelect,
        ProgramChange,
        ProgramSelect,
        PitchBend,
        PitchWheelSensitivity,
        Modulation,
        Sustain,
        ControlChange,
        Pan,
        Volume,
        ReverbSend,
        ChorusSend,
        Timer,
        AnyControlChange,
        ChannelPressure,
        KeyPressure,
        SystemReset,
        Unregistering,
    }
}