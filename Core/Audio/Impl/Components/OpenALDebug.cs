using System.Diagnostics;

namespace Helion.Audio.Impl.Components;

/// <summary>
/// A helper class for running OpenAL code to assist in debugging.
/// </summary>
public static class OpenALDebug
{
    [Conditional("DEBUG")]
    public static void Start(string debugText) =>
        OpenALAudioSystem.CheckForErrors("[Running: {0}]", debugText);

    [Conditional("DEBUG")]
    public static void End(string debugText) =>
        OpenALAudioSystem.CheckForErrors("[Done: {0}]", debugText);
}
