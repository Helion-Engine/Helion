using System;
using Helion.Util.Bytes;

namespace Helion.Util.Sounds.Mus;

/// <summary>
/// A helper class for converting a MUS file to MIDI.
/// </summary>
/// <remarks>
/// Inspired from Slade's addition by Ben Ryves.
/// </remarks>
public static class MusToMidi
{
    private const int MaxChannels = 16;
    private const int MusDrumChannel = 15;
    private const int MidiDrumChannel = 9;
    private const int MidiTrackLengthOffset = 18;

    private static readonly byte[] EndTrack = { 0xFF, 0x2F, 0x00 };
    private static readonly byte[] MidiHeader =
    {
        (byte)'M', (byte)'T', (byte)'h', (byte)'d',
        0, 0, 0, 6, 0, 0, 0, 1, 0, 70,
        (byte)'M', (byte)'T', (byte)'r', (byte)'k',
        0, 0, 0, 0
    };

    /// <summary>
    /// Converts the MUS bytes to MIDI bytes.
    /// </summary>
    /// <param name="musData">The MUS data.</param>
    /// <returns>A new byte array of MIDI data.</returns>
    public static byte[]? Convert(byte[] musData)
    {
        if (musData.Length > 3 && musData[0] == 'M' && musData[1] == 'U' && musData[2] == 'S')
        {
            try
            {
                return ConvertOrThrow(musData);
            }
            catch
            {
                return null;
            }
        }

        if (musData.Length > 4 && musData[0] == 'M' && musData[1] == 'T' && musData[2] == 'h' && musData[3] == 'd')
            return musData;

        return null;
    }

    private static void WriteTime(ref uint queuedTime, ByteWriter writer)
    {
        uint buffer = queuedTime & 0x7F;
        while ((queuedTime >>= 7) != 0)
        {
            buffer <<= 8;
            buffer |= (queuedTime & 0x7F) | 0x80;
        }

        while (true)
        {
            writer.Byte((byte)(buffer & 0xFF));

            if ((buffer & 0x80) != 0)
                buffer >>= 8;
            else
            {
                queuedTime = 0;
                return;
            }
        }
    }

    private static void WritePressKey(byte channel, byte key, byte velocity, ref uint queuedTime,
        ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Byte((byte)((byte)MidiEvent.PressKey | channel),
                    (byte)(key & 0x7F),
                    (byte)(velocity & 0x7F));
    }

    private static void WriteReleaseKey(byte channel, byte key, ref uint queuedTime, ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Byte((byte)((byte) MidiEvent.ReleaseKey | channel),
                    (byte)(key & 0x7F),
                    0);
    }

    private static void WritePitchWheel(byte channel, short wheel, ref uint queuedTime, ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Byte((byte)((byte) MidiEvent.PitchWheel | channel),
                    (byte)(wheel & 0x7F),
                    (byte)((wheel >> 7) & 0x7F));
    }

    private static void WriteChangePatch(byte channel, byte patch, ref uint queuedTime, ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Byte((byte)((byte) MidiEvent.ChangePatch | channel),
                    (byte)(patch & 0x7F));
    }

    private static void WriteChangeControllerValue(byte channel, byte control, byte value,
        ref uint queuedTime, ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Byte((byte)((byte) MidiEvent.ChangeController | channel),
                    (byte)(control & 0x7F),
                    (byte)((value & 0x80) != 0 ? 0x7F : value));
    }

    private static void WriteChangeControllerNoValue(byte channel, byte control, ref uint queuedTime,
        ByteWriter writer)
    {
        WriteChangeControllerValue(channel, control, 0, ref queuedTime, writer);
    }

    private static void WriteTrackEnd(ref uint queuedTime, ByteWriter writer)
    {
        WriteTime(ref queuedTime, writer);
        writer.Bytes(EndTrack);
    }

    private static int AllocateMidiChannel(int[] channelMap)
    {
        int max = -1;
        for (int i = 0; i < MaxChannels; i++)
            if (channelMap[i] > max)
                max = channelMap[i];

        int result = max + 1;
        if (result == MidiDrumChannel)
            result++;

        return result;
    }

    private static int GetMidiChannel(int musChannel, int[] channelMap)
    {
        if (musChannel == MusDrumChannel)
            return MidiDrumChannel;
        if (channelMap[musChannel] == -1)
            channelMap[musChannel] = AllocateMidiChannel(channelMap);
        return channelMap[musChannel];
    }

    private static uint ProcessTimeDelay(ByteReader reader)
    {
        uint timeDelay = 0;

        while (true)
        {
            byte working = reader.ReadByte();
            timeDelay = (uint)((timeDelay * 128) + (working & 0x7F));
            if ((working & 0x80) == 0)
                break;
        }

        return timeDelay;
    }

    private static byte[] ConvertOrThrow(byte[] musData)
    {
        uint queuedTime = 0;
        bool hitScoreEnd = false;
        byte[] controllerMap = { 0x00, 0x20, 0x01, 0x07, 0x0A, 0x0B, 0x5B, 0x5D, 0x40, 0x43, 0x78, 0x7B, 0x7E, 0x7F, 0x79 };
        int[] channelMap = new int[MaxChannels];
        Array.Fill(channelMap, -1);
        byte[] channelVelocities = new byte[MaxChannels];
        Array.Fill(channelVelocities, (byte)0x7F);

        ByteWriter trackWriter = new();
        ByteReader reader = new(musData);
        MusHeader header = new(reader);
        reader.Offset(header.ScoreStart);

        while (!hitScoreEnd)
        {
            while (!hitScoreEnd)
            {
                byte key;
                byte controllerNumber;
                byte eventDescriptor = reader.ReadByte();
                byte channel = (byte)GetMidiChannel(eventDescriptor & 0x0F, channelMap);
                MusEvent musEvent = (MusEvent)(eventDescriptor & 0x70);

                switch (musEvent)
                {
                case MusEvent.ReleaseKey:
                    key = reader.ReadByte();
                    WriteReleaseKey(channel, key, ref queuedTime, trackWriter);
                    break;

                case MusEvent.PressKey:
                    key = reader.ReadByte();
                    if ((key & 0x80) != 0)
                        channelVelocities[channel] = (byte)(reader.ReadByte() & 0x7F);
                    WritePressKey(channel, key, channelVelocities[channel], ref queuedTime, trackWriter);
                    break;

                case MusEvent.PitchWheel:
                    key = reader.ReadByte();
                    WritePitchWheel(channel, (short)(key * 64), ref queuedTime, trackWriter);
                    break;

                case MusEvent.SystemEvent:
                    controllerNumber = reader.ReadByte();
                    WriteChangeControllerNoValue(channel, controllerMap[controllerNumber], ref queuedTime, trackWriter);
                    break;

                case MusEvent.ChangeController:
                    controllerNumber = reader.ReadByte();
                    byte controllerValue = reader.ReadByte();
                    if (controllerNumber == 0)
                        WriteChangePatch(channel, controllerValue, ref queuedTime, trackWriter);
                    else
                        WriteChangeControllerValue(channel, controllerMap[controllerNumber], controllerValue, ref queuedTime, trackWriter);
                    break;

                case MusEvent.ScoreEnd:
                    hitScoreEnd = true;
                    break;
                }

                if ((eventDescriptor & 0x80) != 0)
                    break;
            }

            if (!hitScoreEnd)
                queuedTime += ProcessTimeDelay(reader);
        }

        WriteTrackEnd(ref queuedTime, trackWriter);

        // A lot of this convoluted logic below is to get around writing to
        // a weird location. This should be cleaned up if possible by some
        // methods being added to the appropriate classes.
        byte[] midiData = trackWriter.GetData();
        int numBytes = midiData.Length;

        ByteWriter midiWriter = new();
        midiWriter.Bytes(MidiHeader);
        midiWriter.Bytes(midiData);
        byte[] outData = midiWriter.GetData();

        // Since the writer is little endian only, we have to manually
        // write in big endian. This also writes it to a position that
        // is unexpected, but is required to work.
        outData[MidiTrackLengthOffset] = (byte)((numBytes >> 24) & 0xFF);
        outData[MidiTrackLengthOffset + 1] = (byte)((numBytes >> 16) & 0xFF);
        outData[MidiTrackLengthOffset + 2] = (byte)((numBytes >> 8) & 0xFF);
        outData[MidiTrackLengthOffset + 3] = (byte)(numBytes & 0xFF);

        return outData;
    }
}

