using Helion.Util;

namespace Helion.Resources.Sprites
{
    /// <summary>
    /// A single rotation that contains the frame and whether it is a dummy
    /// representation or a real existing sprite that has a backing image.
    /// </summary>
    public struct SpriteRotation
    {
        /// <summary>
        /// The full name of the frame (ex: "POSSA2A8").
        /// </summary>
        public UpperString FullFrame { get; }

        /// <summary>
        /// True if this is backed by an image somewhere, false if this is set
        /// to
        /// </summary>
        public bool Exists { get; }

        public SpriteRotation(UpperString fullFrame, bool exists = true)
        {
            FullFrame = fullFrame;
            Exists = exists;
        }
    }

    /// <summary>
    /// A collection of all the different sprite rotations for a single sprite
    /// frame.
    /// </summary>
    public class SpriteRotations
    {
        // Note: Before bumping this to 16, make sure that the GPU code that
        // uses this for optimization can handle it, or things may possibly
        // break quite badly.
        public const int TotalRotations = 8;

        /// <summary>
        /// A list of all the rotations.
        /// </summary>
        public SpriteRotation[] Rotations { get; } = new SpriteRotation[TotalRotations];

        private int totalFrames;

        public SpriteRotations(UpperString rotation)
        {
            AddRotation(rotation, true);

            Assert.Postcondition(totalFrames == 1, $"Passed in a frame that couldn't be indexed: {rotation}");
        }

        private void FillAllRotations(UpperString rotation, bool existStatus)
        {
            for (int i = 0; i < Rotations.Length; i++)
                if (!Rotations[i].Exists)
                    Rotations[i] = new SpriteRotation(rotation, existStatus);
        }

        private void AddRotationWithPossibleMirror(UpperString frame, int targetRotation, int mirrorRotation)
        {
            if (!Rotations[targetRotation].Exists)
                Rotations[targetRotation] = new SpriteRotation(frame);
            if (frame.Length == 8 && !Rotations[mirrorRotation].Exists)
                Rotations[mirrorRotation] = new SpriteRotation(frame);
        }

        /// <summary>
        /// Adds a rotation to the sprite rotation. It is okay if you add any
        /// duplicates, it will ignore duplicates.
        /// </summary>
        /// <remarks>
        /// Duplicates are ignored because it's very likely that sprites will
        /// be duplicated from other loaded files when overriding images.
        /// </remarks>
        /// <param name="fullFrame">The frame to track. This should be the full
        /// frame (ex: "PLAYA3A7", or "PLAYA5").</param>
        /// <param name="fillAllRotations"></param>
        public void AddRotation(UpperString fullFrame, bool fillAllRotations = false)
        {
            if (fullFrame.Length < 6)
                return;

            char rotationIndexChar = fullFrame[5];
            if (rotationIndexChar < '0' || '8' < rotationIndexChar)
                return;

            if (fillAllRotations)
                FillAllRotations(fullFrame, false);

            totalFrames++;

            switch (rotationIndexChar)
            {
            case '0':
                FillAllRotations(fullFrame, true);
                break;
            case '1':
                Rotations[0] = new SpriteRotation(fullFrame);
                break;
            case '2':
                AddRotationWithPossibleMirror(fullFrame, 1, 7);
                break;
            case '3':
                AddRotationWithPossibleMirror(fullFrame, 2, 6);
                break;
            case '4':
                AddRotationWithPossibleMirror(fullFrame, 3, 5);
                break;
            case '5':
                Rotations[4] = new SpriteRotation(fullFrame);
                break;
            case '6':
                AddRotationWithPossibleMirror(fullFrame, 5, 3);
                break;
            case '7':
                AddRotationWithPossibleMirror(fullFrame, 6, 2);
                break;
            case '8':
                AddRotationWithPossibleMirror(fullFrame, 7, 1);
                break;
            default:
                Assert.Fail($"Rotation index should not be reached for {fullFrame}");
                break;
            }
        }

        /// <summary>
        /// Gets the rotation type, which tells you what kind of sprites you
        /// are safely able to poll.
        /// </summary>
        /// <returns>The sprite rotation type.</returns>
        public SpriteRotationType GetRotationType()
        {
            switch (totalFrames)
            {
            case 1:
                return SpriteRotationType.Single;
            case 5:
            case 6:
            case 7:
                return SpriteRotationType.OctantReflect;
            default:
                // We want this to break through and not be an error since some
                // mod developers may include rotation 0, and rotation 1 to 8. 
                // Right now this is bad form on their part, but at least it
                // will let the mod run (albeit in a probably buggy way... but 
                // that is  their fault and we will have warned them).
                return SpriteRotationType.Octant;
            }
        }
    }
}
