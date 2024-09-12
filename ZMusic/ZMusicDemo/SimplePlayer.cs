namespace ZMusicDemo
{
    using Helion.Client.Music;
    using OpenTK.Audio.OpenAL;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using ZMusicWrapper;

    internal static class SimplePlayer
    {
        public static unsafe void Play(string[] fileNames)
        {
            ALDevice device = ALC.OpenDevice(null);
            ALContext context = ALC.CreateContext(device, (int*)null);
            _ = ALC.MakeContextCurrent(context);
            _ = AL.GetError();

            Queue<string> fileQueue = new(fileNames);
            byte[]? genMidiLumpBytes = null;

            using (ZMusicPlayer player = new ZMusicPlayer(new AudioStreamFactory(), MidiDevice.FluidSynth, "Default.sf2", null))
            {
                while (fileQueue.TryDequeue(out string? fileName))
                {
                    if (Directory.Exists(fileName))
                    {
                        string[] files = Directory.GetFiles(fileName);
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(file).Equals("GENMIDI.LMP", StringComparison.OrdinalIgnoreCase))
                            {
                                genMidiLumpBytes = File.ReadAllBytes(file);
                                player.SetOPLPatchSet(genMidiLumpBytes[8..]);
                                player.PreferredDevice = MidiDevice.OPL3;
                                continue;
                            }

                            fileQueue.Enqueue(file);
                        }
                        continue;
                    }
                    if (!File.Exists(fileName))
                    {
                        Console.WriteLine($"Could not find file {fileName}");
                        continue;
                    }

                    Console.WriteLine($"Playing {fileName}");
                    byte[] data = File.ReadAllBytes(fileName);
                    player.Play(data, loop: false, stopped: () => Console.WriteLine("Done playing song."));

                    if (fileQueue.TryPeek(out _))
                    {
                        Console.WriteLine("Press any key to advance to the next track.");
                    }
                    else
                    {
                        Console.WriteLine("Press any key to exit.");
                    }

                    Console.ReadKey();
                }

                player.Stop();
            }

            ALC.DestroyContext(context);
            _ = ALC.CloseDevice(device);
        }
    }
}