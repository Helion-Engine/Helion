using System;

namespace Helion.Audio
{
    /// <summary>
    /// An instantiation of a contextual component for some apparatus, to which
    /// all sounds that are played through this context are bound to it and are
    /// stopped when the context is disposed.
    /// </summary>
    /// <remarks>
    /// This can be thought of as an instance allocated from the sound engine
    /// for some component that manages its own sounds. For example, using the
    /// layered system might have a world and a menu layer. It's very easy for
    /// sounds coming from the world to accidentally bleed into the menus when
    /// not paying attention to stopping all the sounds. To avoid this issue,
    /// all sounds that come from a context will be halted when the context is
    /// to be cleaned up. This means if we kill a layer (ex: we quit a game and
    /// toss out, say, a SinglePlayerWorldLayer) then all of the sounds will be
    /// halted and never bleed into other areas. In ports like ZDoom and such,
    /// Randy said on the forums that it's all done on another thread and this
    /// coding style has led to weird sound leakage issues. This design pattern
    /// will hopefully remedy this.
    /// </remarks>
    public interface IAudioContext : IDisposable
    {
        /// <summary>
        /// The listener to which all the sounds are played with respect to.
        /// </summary>
        IAudioListener Listener { get; }
        
        /// <summary>
        /// Creates a new audio source from this context. This means when the
        /// context is disposed, so will this sound.
        /// </summary>
        /// <param name="sound">The name of the sound.</param>
        /// <returns>An audio source that we can play the sound from.</returns>
        IAudioSource Create(string sound);
    }
}