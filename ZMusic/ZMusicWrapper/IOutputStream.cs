namespace ZMusicWrapper
{
    using System;

    /// <summary>
    /// A factory that can create <see cref="IOutputStream"/> objects
    /// </summary>
    public interface IOutputStreamFactory
    {
        /// <summary>
        /// Returns an output stream configured to the requested sample rate and channel count
        /// </summary>
        /// <param name="sampleRate">Sample rate, e.g. 44100 hz</param>
        /// <param name="channelCount">Channel count</param>
        /// <returns><see cref="IOutputStream"/> configured for the specified sample rate and channels</returns>
        public IOutputStream GetOutputStream(int sampleRate, int channelCount);
    }

    /// <summary>
    /// Represents an output stream that, when played, periodically executes a callback asking for more data.
    /// </summary>
    public interface IOutputStream : IDisposable
    {
        /// <summary>
        /// Plays the specified output, asking for new blocks of data as needed
        /// </summary>
        /// <param name="fillBlockAction">An action that, when called, fills a buffer of 16-bit integers with streaming audio data.
        /// This action should return "false" if it has no more data.</param>
        void Play(Func<short[], bool> fillBlockAction);

        /// <summary>
        /// When called, the output stream must end playback and stop asking for more buffer data.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets the number of channels supported by the output stream
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// Gets the sample rate supported by the output stream
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Gets the block length the output stream will use when asking for more data
        /// </summary>
        int BlockLength { get; }

        /// <summary>
        /// Change the volume, if supported; else fail silently
        /// </summary>
        /// <param name="newVolume">New volume, on a scale where 1.0f represents "no gain".</param>
        void SetVolume(float newVolume);
    }
}
