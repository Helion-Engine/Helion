using System;
using OpenTK.Audio.OpenAL;
using static Helion.Audio.AudioHelper;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components;

public class OpenALBuffer : IDisposable
{
    public int BufferId;

    private OpenALBuffer(int sampleRate, Span<byte> sampleData, ALFormat format)
    {
        OpenALDebug.Start("Creating buffer");
        BufferId = AL.GenBuffer();
        OpenALDebug.End("Creating buffer");

        // Note: We only support DMX sounds currently!
        OpenALDebug.Start("Setting buffer data");
        AL.BufferData(BufferId, format, sampleData, sampleRate);
        OpenALDebug.End("Setting buffer data");
    }

    ~OpenALBuffer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public static OpenALBuffer? Create(byte[] data, out string? error)
    {
        error = null;
        if (IsWave(data) && TryReadWav(data, out WavFormat wavFormat, out Span<byte> sampleData) && GetFormat(wavFormat, out ALFormat format, out error))
            return new OpenALBuffer(wavFormat.SampleRate, sampleData, format);
        else if (IsDoomSound(data) && TryReadDoomSound(data, out int sampleRate, out sampleData, out error))
            return new OpenALBuffer(sampleRate, sampleData, ALFormat.Mono8);
        else if (error == null)
            error = "Unsupported format.";

        return null;
    }

    private static bool GetFormat(WavFormat wavFormat, out ALFormat format, out string? error)
    {
        format = 0;
        if (wavFormat.Channels == 1)
        {
            if (wavFormat.BitsPerSample == 8)
                format = ALFormat.Mono8;
            else if (wavFormat.BitsPerSample == 16)
                format = ALFormat.Mono16;
        }
        else if (wavFormat.Channels == 2)
        {
            if (wavFormat.BitsPerSample == 8)
                format = ALFormat.Stereo8;
            else if (wavFormat.BitsPerSample == 16)
                format = ALFormat.Stereo16;
        }
        else
        {
            error = $"Unsupported wave channels:{wavFormat.Channels}. Must be 1 or 2.";
            return false;
        }

        if (format == 0)
        {
            error = $"Unsupported wave bit rate:{wavFormat.BitsPerSample}. Must be 8 or 16.";
            return false;
        }

        error = null;
        return true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        OpenALDebug.Start("Deleting sound buffer");
        AL.DeleteBuffer(BufferId);
        OpenALDebug.End("Deleting sound buffer");
    }
}
