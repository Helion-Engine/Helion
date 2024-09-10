namespace ZMusicDemo
{
    using System;
    using System.IO;

    public class Program
    {
        // This is just a simple demo program to test ZMusic and OpenAL integration.

        public static unsafe void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: 'ZMusicDemo <songFileName1> <songFileName2> ... <songFileNameN>'");
                return;
            }

            SimplePlayer.Play(args);
        }
    }
}
