using OpenTK.Audio.OpenAL;
using System;
using ZMusicWrapper;
using static Helion.Audio.AudioHelper;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl.Components;

public class OpenALBuffer : IDisposable
{
    public int BufferId;
    public int Bytes;
    public int SampleRate;

    private OpenALBuffer(int sampleRate, Span<byte> sampleData, ALFormat format)
    {
        OpenALDebug.Start("Creating buffer");
        BufferId = AL.GenBuffer();
        SampleRate = sampleRate;
        Bytes = sampleData.Length;
        OpenALDebug.End("Creating buffer");
        // Note: We only support DMX sounds currently!
        OpenALDebug.Start("Setting buffer data");
        AL.BufferData<byte>(BufferId, format, sampleData, sampleRate);
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
        if (data.Length == 0)
            return null;

        if (IsWave(data) && TryReadWav(data, out var wavFormat, out var sampleData) && GetFormat(wavFormat, out var format, out error))
        {
            return new(wavFormat.SampleRate, sampleData, format);
        }
        else if (IsDoomSound(data) && TryReadDoomSound(data, out var sampleRate, out sampleData, out error))
        {
            return new(sampleRate, sampleData, ALFormat.Mono8);
        }
        else 
        if (TryConvertThroughZMusic(data, out sampleRate, out var stereo, out var sampleType, out var convertedData) &&
            GetFormat(stereo, sampleType, out format, out error))
        {
            return new(sampleRate, convertedData, format);
        }

        error ??= "Unsupported format.";
        return null;
    }

    private static bool GetFormat(bool stereo, SampleType sampleType, out ALFormat format, out string? error)
    {
        format = 0;
        if (stereo)
        {
            switch (sampleType)
            {
                case SampleType.UInt8:
                    format = ALFormat.Stereo8;
                    break;
                case SampleType.Int16:
                    format = ALFormat.Stereo16;
                    break;
                case SampleType.Float32:
                    format = ALFormat.StereoFloat32Ext;
                    break;
            }
        }
        else
        {
            switch (sampleType)
            {
                case SampleType.UInt8:
                    format = ALFormat.Mono8;
                    break;
                case SampleType.Int16:
                    format = ALFormat.Mono16;
                    break;
                case SampleType.Float32:
                    format = ALFormat.MonoFloat32Ext;
                    break;
            }
        }

        if (format == 0)
        {
            error = $"Unsupported format stereo:{stereo} sample:{sampleType}";
            return false;
        }

        error = null;
        return true;
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
