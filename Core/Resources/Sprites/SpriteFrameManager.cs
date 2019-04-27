using Helion.Util;
using System.Collections.Generic;

namespace Helion.Resources.Sprites
{
    /// <summary>
    /// A manager of sprite rotations, which collects information about the 
    /// different sprite rotations that exist and compiles them into an easy to
    /// poll format.
    /// </summary>
    /// <remarks>
    /// This is primarily for the renderer, since we have a heavy optimization
    /// that uses this information to massively accelerate entity rendering on
    /// the GPU.
    /// </remarks>
    public class SpriteFrameManager
    {
        private Dictionary<UpperString, SpriteRotations> frameToSprites = new Dictionary<UpperString, SpriteRotations>();
        private List<SpriteFrameManagerListener> listeners = new List<SpriteFrameManagerListener>();

        private static bool SupportedSpriteNamespace(ResourceNamespace resourceNamespace)
        {
            return resourceNamespace == ResourceNamespace.Global ||
                   resourceNamespace == ResourceNamespace.Sprites;
        }

        private static bool IsValidRotationIndex(char c) => ('0' <= c && c <= '8');

        private static bool AreRotationReflections(char first, char second)
        {
            switch (first)
            {
            case '2':
                return second == '8';
            case '3':
                return second == '7';
            case '4':
                return second == '6';
            default:
                return false;
            }
        }

        private static bool IsSixLetterFrame(UpperString frame)
        {
            return frame.Length == 6 && IsValidRotationIndex(frame[5]);
        }

        private static bool IsEightLetterFrame(UpperString frame)
        {
            return frame.Length == 8 &&
                   IsValidRotationIndex(frame[5]) &&
                   AreRotationReflections(frame[5], frame[7]);
        }

        private static bool IsSixOrEightLetterFrame(UpperString frame)
        {
            return IsSixLetterFrame(frame) || IsEightLetterFrame(frame);
        }

        private void NotifyListeners(UpperString frame, SpriteRotations rotations)
        {
            SpriteFrameManagerEvent spriteEvent = SpriteFrameManagerEvent.Create(frame, rotations);
            listeners.ForEach(l => l.HandleSpriteEvent(spriteEvent));
        }

        /// <summary>
        /// Tracks a sprite rotation.
        /// </summary>
        /// <param name="name">The name of the sprite. This need not be upper
        /// case, the function will take care of upper casing it.</param>
        /// <param name="resourceNamespace">A namespace which is used for
        /// pruning of graphics that we do not want to index.</param>
        public void Track(string name, ResourceNamespace resourceNamespace)
        {
            // Due to a significant amount of false positives, we have to do
            // some filtering of our candidates. While we certainly have more
            // than enough memory to easily index all the rotations, it ends
            // up making the data on the GPU very large and makes it difficult
            // to do some optimizations (especially for NVIDIA GPUs) since only
            // so much information can fit in a uniform buffer object.
            if (!SupportedSpriteNamespace(resourceNamespace) || !IsSixOrEightLetterFrame(name))
                return;

            UpperString frameBase = name.Substring(0, 5);

            if (frameToSprites.TryGetValue(frameBase, out SpriteRotations rotations))
            {
                rotations.AddRotation(name);
                NotifyListeners(frameBase, rotations);
            }
            else
            {
                SpriteRotations spriteRotations = new SpriteRotations(name);
                frameToSprites[frameBase] = spriteRotations;
                NotifyListeners(frameBase, spriteRotations);
            }
        }

        /// <summary>
        /// Registers a listener for events.
        /// </summary>
        /// <param name="listener">The listener. This should not be registered
        /// already.</param>
        public void Register(SpriteFrameManagerListener listener)
        {
            if (listeners.Contains(listener))
                Assert.Fail($"Trying to add the same sprite manager listener twice: {listener}");
            else
                listeners.Add(listener);
            // TODO: Emit current state to the listener.
        }

        /// <summary>
        /// Unregisters a listener. If it is not listening, nothing happens.
        /// </summary>
        /// <param name="listener">The listener to unregister if it is 
        /// registered.</param>
        public void Unregister(SpriteFrameManagerListener listener)
        {
            listeners.Remove(listener);
        }
    }
}
